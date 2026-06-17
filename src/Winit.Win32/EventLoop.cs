using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Winit.Core;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.Win32;

public sealed class EventLoop : IPlatformEventLoop, IEventLoopExtPumpEvents, IEventLoopExtRunOnDemand
{
    internal const uint WakeUpMessage = PInvoke.WM_APP + 1;
    internal const uint ExecMessage = PInvoke.WM_APP + 2;
    private const uint PmRemove = 0x0001;
    private const uint QsAllInput = 0x04FF;
    private const uint MwmoInputAvailable = 0x0004;
    private const uint Infinite = 0xFFFFFFFF;
    private const uint WaitFailed = 0xFFFFFFFF;
    private const uint CreateWaitableTimerHighResolution = 0x00000002;
    private const uint TimerAllAccess = 0x001F0003;
    private static readonly TimeSpan s_fiftyDays = TimeSpan.FromDays(50);
    private static readonly TimeSpan s_minWait = TimeSpan.FromTicks(1);

    private static readonly uint s_mainThreadId = PInvoke.GetCurrentThreadId();
    private static int s_created;

    private readonly EventTargetWindow _eventTargetWindow;
    private readonly EventLoopRunner _runner;
    private readonly OwnedDisplayHandle _ownedDisplayHandle = new(RawDisplayHandle.FromWindows());
    private readonly Func<nint, bool>? _msgHook;
    private nint _highResolutionTimer;
    private DeviceEvents _deviceEvents = DeviceEvents.WhenFocused;

    public EventLoop(PlatformSpecificEventLoopAttributes attributes)
    {
        if (Interlocked.Exchange(ref s_created, 1) != 0)
        {
            throw new EventLoopRecreationException();
        }

        uint threadId = PInvoke.GetCurrentThreadId();
        _msgHook = attributes.MsgHook;

        if (!attributes.AnyThread && threadId != s_mainThreadId)
        {
            throw new InvalidOperationException(
                "Initializing the event loop outside of the main thread is a cross-platform compatibility hazard.");
        }

        if (attributes.DpiAware)
        {
            Dpi.BecomeDpiAware();
        }

        _eventTargetWindow = EventTargetWindow.Create(this);
        ThreadExecutor = new EventLoopThreadExecutor(threadId, _eventTargetWindow.Hwnd);
        _runner = new EventLoopRunner(this, threadId, _eventTargetWindow.Hwnd);
        RegisterRawInput(_deviceEvents);
    }

    ~EventLoop()
    {
        CloseHighResolutionTimer();
    }

    public IEnumerable<CoreMonitorHandle> AvailableMonitors => Monitor.AvailableMonitors();

    public CoreMonitorHandle? PrimaryMonitor => Monitor.PrimaryMonitor();

    public Theme? SystemTheme => DarkMode.SystemTheme;

    public ControlFlow ControlFlow
    {
        get => _runner.ControlFlow;
        set => _runner.ControlFlow = value;
    }

    public bool Exiting => _runner.Exiting;

    public OwnedDisplayHandle OwnedDisplayHandle => _ownedDisplayHandle;

    public object? DisplayHandle => RawDisplayHandle.FromWindows();

    internal EventLoopThreadExecutor ThreadExecutor { get; }

    public EventLoopProxy CreateProxy()
    {
        return new EventLoopProxy(new EventLoopProxyProvider(_runner.ThreadId));
    }

    public IWindow CreateWindow(WindowAttributes windowAttributes)
    {
        return Window.Create(this, windowAttributes);
    }

    public CustomCursor CreateCustomCursor(CustomCursorSource customCursor)
    {
        if (customCursor.TryGetValue(out CustomCursorSource.Image image))
        {
            return new CustomCursor(Win32CustomCursorProvider.FromImage(image.Value));
        }

        throw new NotSupportedRequestException("animated and URL custom cursors are not implemented by the Win32 backend yet");
    }

    public void ListenDeviceEvents(DeviceEvents allowed)
    {
        _deviceEvents = allowed;
        RegisterRawInput(allowed);
    }

    public void Exit()
    {
        _runner.SetExitCode(0);
    }

    public void RunApp(IApplicationHandler app)
    {
        RunAppOnDemand(app);
    }

    public void RunAppOnDemand(IApplicationHandler app)
    {
        _runner.ClearExit();

        int exitCode;
        try
        {
            using IDisposable appRegistration = _runner.SetApp(app);

            while (true)
            {
                WaitForMessages(null);
                _runner.ThrowPendingException();
                if (_runner.ExitCode is { } codeAfterWait)
                {
                    exitCode = codeAfterWait;
                    break;
                }

                DispatchPeekedMessages();
                _runner.ThrowPendingException();
                if (_runner.ExitCode is { } codeAfterDispatch)
                {
                    exitCode = codeAfterDispatch;
                    break;
                }
            }

            _runner.LoopDestroyed();
            _runner.ThrowPendingException();
        }
        catch
        {
            _runner.ResetRunner();
            throw;
        }

        _runner.ResetRunner();
        if (exitCode != 0)
        {
            throw new EventLoopExitFailureException(exitCode);
        }
    }

    public PumpStatus PumpAppEvents(TimeSpan? timeout, IApplicationHandler app)
    {
        try
        {
            using IDisposable appRegistration = _runner.SetApp(app);

            _runner.WakeUp();
            _runner.ThrowPendingException();

            if (!_runner.Exiting)
            {
                WaitForMessages(timeout);
                _runner.ThrowPendingException();
            }

            if (!_runner.Exiting)
            {
                DispatchPeekedMessages();
                _runner.ThrowPendingException();
            }

            if (_runner.ExitCode is { } exitCode)
            {
                _runner.LoopDestroyed();
                _runner.ThrowPendingException();
                _runner.ResetRunner();
                return new PumpStatus(new PumpStatus.Exit(exitCode));
            }

            _runner.PrepareWait();
            _runner.ThrowPendingException();
            return new PumpStatus(new PumpStatus.Continue());
        }
        catch
        {
            _runner.ResetRunner();
            throw;
        }
    }

    internal void SendWindowEvent(WindowId windowId, WindowEvent windowEvent)
    {
        _runner.SendEvent(new EventLoopEvent(new EventLoopEvent.Window(windowId, windowEvent)));
    }

    internal void SendDeviceEvent(DeviceId? deviceId, DeviceEvent deviceEvent)
    {
        _runner.SendEvent(new EventLoopEvent(new EventLoopEvent.Device(deviceId, deviceEvent)));
    }

    internal bool ShouldBufferEvents()
    {
        return _runner.ShouldBuffer();
    }

    internal void StoreCallbackException(Exception exception)
    {
        _runner.StoreException(exception);
    }

    private void RegisterRawInput(DeviceEvents allowed)
    {
        _ = RawInput.Register(_eventTargetWindow.Hwnd, allowed);
    }

    private void DispatchPeekedMessages()
    {
        _runner.InterruptMessageDispatch = false;
        while (PInvoke.PeekMessageW(out MSG msg, HWND.Null, 0, 0, PmRemove))
        {
            if (msg.message == WakeUpMessage)
            {
                _runner.SendEvent(new EventLoopEvent(new EventLoopEvent.WakeUp()));
                _runner.ThrowPendingException();
                continue;
            }

            if (msg.message == ExecMessage && msg.hwnd == _eventTargetWindow.Hwnd)
            {
                PInvoke.DispatchMessage(msg);
                _runner.ThrowPendingException();
                continue;
            }

            if (MessageHandledByHook(ref msg))
            {
                continue;
            }

            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
            _runner.ThrowPendingException();

            if (_runner.Exiting || _runner.InterruptMessageDispatch)
            {
                break;
            }
        }
    }

    private unsafe bool MessageHandledByHook(ref MSG msg)
    {
        if (_msgHook is null)
        {
            return false;
        }

        fixed (MSG* msgPtr = &msg)
        {
            return _msgHook((nint)msgPtr);
        }
    }

    private void WaitForMessages(TimeSpan? timeout)
    {
        _runner.PrepareWait();
        WaitForMessagesImpl(_runner.ControlFlow, timeout);
        _runner.WakeUp();
    }

    private unsafe void WaitForMessagesImpl(ControlFlow controlFlow, TimeSpan? timeout)
    {
        TimeSpan? waitTimeout = MinTimeout(ControlFlowTimeout(controlFlow), timeout);
        if (waitTimeout == TimeSpan.Zero)
        {
            return;
        }

        uint timeoutMilliseconds = Infinite;
        uint handleCount = 0;
        nint* handles = null;
        nint* highResolutionTimerHandle = stackalloc nint[1];

        if (waitTimeout is { } finiteTimeout)
        {
            TimeSpan clampedTimeout = ClampTimeout(finiteTimeout);
            timeoutMilliseconds = TimeoutMilliseconds(clampedTimeout);
            if (TrySetHighResolutionTimer(clampedTimeout))
            {
                highResolutionTimerHandle[0] = _highResolutionTimer;
                handles = highResolutionTimerHandle;
                handleCount = 1;
            }
        }

        uint result = PInvoke.MsgWaitForMultipleObjectsEx(
            handleCount,
            handles,
            timeoutMilliseconds,
            QsAllInput,
            MwmoInputAvailable);
        if (result == WaitFailed)
        {
            throw Win32Error.EventLoop();
        }
    }

    private static TimeSpan ClampTimeout(TimeSpan timeout)
    {
        if (timeout < s_minWait)
        {
            return s_minWait;
        }

        if (timeout > s_fiftyDays)
        {
            return s_fiftyDays;
        }

        return timeout;
    }

    private unsafe bool TrySetHighResolutionTimer(TimeSpan timeout)
    {
        if (_highResolutionTimer == 0)
        {
            _highResolutionTimer = PInvoke.CreateWaitableTimerEx(
                0,
                PCWSTR.Null,
                CreateWaitableTimerHighResolution,
                TimerAllAccess);
        }

        if (_highResolutionTimer == 0)
        {
            return false;
        }

        long ticks = Math.Max(1L, timeout.Ticks);
        long dueTime = -ticks;
        return PInvoke.SetWaitableTimer(_highResolutionTimer, &dueTime, 0, 0, 0, false);
    }

    private void CloseHighResolutionTimer()
    {
        nint timer = _highResolutionTimer;
        if (timer == 0)
        {
            return;
        }

        _highResolutionTimer = 0;
        _ = PInvoke.CloseHandle(timer);
    }

    private static TimeSpan? ControlFlowTimeout(ControlFlow controlFlow)
    {
        if (controlFlow.TryGetValue(out ControlFlow.Poll _))
        {
            return TimeSpan.Zero;
        }

        if (!controlFlow.TryGetValue(out ControlFlow.WaitUntil waitUntil))
        {
            return null;
        }

        long now = TimeProvider.System.GetTimestamp();
        long ticks = waitUntil.Instant.Timestamp - now;
        if (ticks <= 0)
        {
            return TimeSpan.Zero;
        }

        double seconds = ticks / (double)TimeProvider.System.TimestampFrequency;
        return seconds >= TimeSpan.MaxValue.TotalSeconds
            ? TimeSpan.MaxValue
            : TimeSpan.FromSeconds(seconds);
    }

    private static TimeSpan? MinTimeout(TimeSpan? a, TimeSpan? b)
    {
        if (a is null)
        {
            return b;
        }

        if (b is null)
        {
            return a;
        }

        return a.Value <= b.Value ? a : b;
    }

    private static uint TimeoutMilliseconds(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return 0;
        }

        double milliseconds = Math.Ceiling(timeout.TotalMilliseconds);
        if (milliseconds >= uint.MaxValue)
        {
            return Infinite;
        }

        return Math.Max(1u, (uint)milliseconds);
    }

}

internal sealed class EventLoopProxyProvider(uint threadId) : IEventLoopProxyProvider
{
    public void WakeUp()
    {
        PInvoke.PostThreadMessage(threadId, EventLoop.WakeUpMessage, default, default);
    }
}

internal sealed class EventLoopThreadExecutor(uint threadId, HWND targetWindow)
{
    public bool InEventLoopThread => PInvoke.GetCurrentThreadId() == threadId;

    public void Execute(Action action)
    {
        if (InEventLoopThread)
        {
            action();
            return;
        }

        GCHandle handle = GCHandle.Alloc(action);
        if (PInvoke.PostMessage(
                targetWindow,
                EventLoop.ExecMessage,
                new WPARAM(unchecked((nuint)GCHandle.ToIntPtr(handle))),
                default))
        {
            return;
        }

        handle.Free();
        throw Win32Error.EventLoop();
    }

    public static void ExecuteQueued(nuint actionHandle)
    {
        GCHandle handle = GCHandle.FromIntPtr(unchecked((nint)actionHandle));
        try
        {
            if (handle.Target is Action action)
            {
                action();
            }
        }
        finally
        {
            handle.Free();
        }
    }
}

internal sealed unsafe class EventTargetWindow
{
    private const nint HwndMessage = -3;
    private const string WindowClassName = "Winit.EventTarget";

    private static readonly ConcurrentDictionary<nint, EventLoop> s_eventLoops = new();
    private static int s_registeredClass;

    private EventTargetWindow(HWND hwnd)
    {
        Hwnd = hwnd;
    }

    public HWND Hwnd { get; }

    public static EventTargetWindow Create(EventLoop eventLoop)
    {
        RegisterWindowClass();

        fixed (char* className = WindowClassName)
        fixed (char* title = string.Empty)
        {
            HWND hwnd = PInvoke.CreateWindowEx(
                default,
                new PCWSTR(className),
                new PCWSTR(title),
                default,
                0,
                0,
                0,
                0,
                new HWND(HwndMessage),
                HMENU.Null,
                HINSTANCE.Null,
                null);

            if (hwnd == HWND.Null)
            {
                throw Win32Error.EventLoop();
            }

            s_eventLoops[Util.HwndValue(hwnd)] = eventLoop;
            return new EventTargetWindow(hwnd);
        }
    }

    private static void RegisterWindowClass()
    {
        if (Interlocked.Exchange(ref s_registeredClass, 1) != 0)
        {
            return;
        }

        fixed (char* className = WindowClassName)
        {
            WNDCLASSEXW windowClass = new()
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = &WndProc,
                hInstance = HINSTANCE.Null,
                lpszClassName = new PCWSTR(className),
            };

            if (PInvoke.RegisterClassEx(windowClass) == 0)
            {
                Interlocked.Exchange(ref s_registeredClass, 0);
                throw Win32Error.EventLoop();
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        try
        {
            return WndProcInner(hwnd, message, wParam, lParam);
        }
        catch (Exception exception)
        {
            if (s_eventLoops.TryGetValue(Util.HwndValue(hwnd), out EventLoop? eventLoop))
            {
                eventLoop.StoreCallbackException(exception);
            }

            return new LRESULT(-1);
        }
    }

    private static LRESULT WndProcInner(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (!s_eventLoops.TryGetValue(Util.HwndValue(hwnd), out EventLoop? eventLoop))
        {
            if (message == EventLoop.ExecMessage)
            {
                EventLoopThreadExecutor.ExecuteQueued(wParam.Value);
                return new LRESULT(0);
            }

            return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
        }

        switch (message)
        {
            case RawInput.WmInput:
                RawInput.Handle(eventLoop, lParam.Value);
                return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
            case EventLoop.ExecMessage:
                EventLoopThreadExecutor.ExecuteQueued(wParam.Value);
                return new LRESULT(0);
            case PInvoke.WM_NCDESTROY:
                s_eventLoops.TryRemove(Util.HwndValue(hwnd), out _);
                break;
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
