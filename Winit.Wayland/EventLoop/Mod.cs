using System.Threading;
using RawWindowHandles;
using Winit.Core;
using Winit.Dpi;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.Wayland;

public sealed class EventLoopBuilder : IPlatformEventLoopBuilder, IEventLoopBuilderExtWayland
{
    private readonly PlatformSpecificEventLoopAttributes _attributes = new();

    public EventLoopBuilder WithWayland()
    {
        return this;
    }

    public EventLoopBuilder WithAnyThread(bool anyThread)
    {
        _attributes.AnyThread = anyThread;
        return this;
    }

    public IPlatformEventLoop Build()
    {
        return new EventLoop(_attributes);
    }
}

public sealed unsafe class EventLoop :
    IPlatformEventLoop,
    IEventLoopExtRunOnDemand,
    IEventLoopExtPumpEvents,
    IActiveEventLoopExtWayland,
    IEventLoopExtWayland,
    IDisposable
{
    private static readonly int s_mainThreadId = Environment.CurrentManagedThreadId;
    private static int s_created;

    private readonly WinitState _state;
    private readonly int _proxyWakeFd;
    private readonly int _eventWakeFd;
    private PumpEventNotifier? _pumpEventNotifier;
    private ControlFlow _controlFlow = ControlFlow.Default;
    private bool _loopInitialized;
    private bool _exiting;
    private bool _disposed;

    public EventLoop(PlatformSpecificEventLoopAttributes attributes)
    {
        if (Interlocked.Exchange(ref s_created, 1) != 0)
        {
            throw new EventLoopRecreationException();
        }

        if (!attributes.AnyThread && Environment.CurrentManagedThreadId != s_mainThreadId)
        {
            throw new InvalidOperationException(
                "Initializing the event loop outside of the main thread is a cross-platform compatibility hazard.");
        }

        _state = WinitState.New();
        _proxyWakeFd = PInvoke.EventFd(0, EventFdFlags.NonBlock | EventFdFlags.CloExec);
        if (_proxyWakeFd < 0)
        {
            _state.Dispose();
            throw new InvalidOperationException("eventfd failed while creating the Wayland event loop proxy wakeup handle.");
        }

        _eventWakeFd = PInvoke.EventFd(0, EventFdFlags.NonBlock | EventFdFlags.CloExec);
        if (_eventWakeFd < 0)
        {
            _ = PInvoke.Close(_proxyWakeFd);
            _state.Dispose();
            throw new InvalidOperationException("eventfd failed while creating the Wayland event loop internal wakeup handle.");
        }
    }

    ~EventLoop()
    {
        Dispose();
    }

    public IEnumerable<CoreMonitorHandle> AvailableMonitors =>
        _state.Monitors.Select(monitor => new CoreMonitorHandle(monitor)).ToArray();

    public CoreMonitorHandle? PrimaryMonitor => null;

    public Theme? SystemTheme => null;

    public ControlFlow ControlFlow
    {
        get => _controlFlow;
        set => _controlFlow = value;
    }

    public bool Exiting => _exiting;

    public bool IsWayland => true;

    public OwnedDisplayHandle OwnedDisplayHandle =>
        new(RawDisplayHandle.FromWayland(_state.Connection.Display.Value));

    public RawDisplayHandle? DisplayHandle => RawDisplayHandle.FromWayland(_state.Connection.Display.Value);

    internal WinitState State => _state;

    public EventLoopProxy CreateProxy()
    {
        return new EventLoopProxy(new EventLoopProxyProvider(_proxyWakeFd));
    }

    public IWindow CreateWindow(WindowAttributes windowAttributes)
    {
        return Window.Create(this, windowAttributes);
    }

    public CustomCursor CreateCustomCursor(CustomCursorSource customCursor)
    {
        return new CustomCursor(WaylandCustomCursor.Create(customCursor));
    }

    public void ListenDeviceEvents(DeviceEvents allowed)
    {
        _ = allowed;
    }

    public void Exit()
    {
        _exiting = true;
        WakeUp();
    }

    public void RunApp(IApplicationHandler app)
    {
        RunAppOnDemand(app);
    }

    public void RunAppOnDemand(IApplicationHandler app)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _exiting = false;

        while (!_exiting)
        {
            _ = PumpAppEvents(null, app);
        }

        _state.Connection.Roundtrip();
        _loopInitialized = false;
    }

    public PumpStatus PumpAppEvents(TimeSpan? timeout, IApplicationHandler app)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pumpEventNotifier?.Pause();

        if (!_loopInitialized)
        {
            _loopInitialized = true;
            _exiting = false;
            app.NewEvents(this, new StartCause(new StartCause.Init()));
            app.CanCreateSurfaces(this);
            DispatchPendingEvents(app);
            app.AboutToWait(this);
            _state.Connection.Flush();
        }

        if (!_exiting)
        {
            PumpOnce(app, timeout);
        }

        if (_exiting)
        {
            _loopInitialized = false;
            return new PumpStatus(new PumpStatus.Exit(0));
        }

        if (timeout is not null)
        {
            _pumpEventNotifier ??= PumpEventNotifier.Spawn(_state.Connection, _eventWakeFd);
            _pumpEventNotifier?.StartMonitoring();
        }

        return new PumpStatus(new PumpStatus.Continue());
    }

    internal void RegisterWindow(Window window)
    {
        _state.RegisterWindow(window);
    }

    internal void RemoveWindow(WindowId windowId)
    {
        _state.RemoveWindow(windowId);
    }

    internal void QueueWindowEvent(WindowId windowId, WindowEvent windowEvent)
    {
        _state.PushWindowEvent(windowId, windowEvent);
        WakeUp();
    }

    internal void WakeUp()
    {
        WriteWakeFd(_eventWakeFd);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pumpEventNotifier?.Dispose();
        _pumpEventNotifier = null;

        if (_proxyWakeFd >= 0)
        {
            _ = PInvoke.Close(_proxyWakeFd);
        }

        if (_eventWakeFd >= 0)
        {
            _ = PInvoke.Close(_eventWakeFd);
        }

        _state.Dispose();
        GC.SuppressFinalize(this);
    }

    private void DispatchPendingEvents(IApplicationHandler app)
    {
        DispatchCompositorUpdates(app);

        foreach (Event pendingEvent in _state.EventsSink.Drain())
        {
            if (pendingEvent.TryGetValue(out Event.Window window))
            {
                app.WindowEvent(this, window.WindowId, window.WindowEvent);
            }
            else if (pendingEvent.TryGetValue(out Event.Device device))
            {
                app.DeviceEvent(this, null, device.DeviceEvent);
            }
        }

        DispatchRedrawRequests(app);
        _state.DispatchedEvents = false;
    }

    private void DispatchCompositorUpdates(IApplicationHandler app)
    {
        foreach (WindowCompositorUpdate compositorUpdate in _state.DrainWindowCompositorUpdates())
        {
            if (!_state.Windows.TryGetValue(compositorUpdate.WindowId, out Window? window))
            {
                continue;
            }

            WindowCompositorUpdate update = compositorUpdate;
            if (update.ScaleChanged)
            {
                PhysicalSize<uint> surfaceSize = window.SurfaceSize;
                double scaleFactor = window.ScaleFactor;
                SurfaceSizeState sizeState = new(surfaceSize);
                app.WindowEvent(
                    this,
                    window.Id,
                    new WindowEvent(new WindowEvent.ScaleFactorChanged(
                        scaleFactor,
                        SurfaceSizeWriter.Create(sizeState))));

                PhysicalSize<uint> requestedSurfaceSize = sizeState.SurfaceSize;
                if (requestedSurfaceSize != surfaceSize)
                {
                    _ = window.RequestSurfaceSize(Size.FromPhysical(requestedSurfaceSize));
                    update.Resized = true;
                }
            }

            if (update.Resized || update.ScaleChanged)
            {
                app.WindowEvent(
                    this,
                    window.Id,
                    new WindowEvent(new WindowEvent.SurfaceResized(window.SurfaceSize)));
                window.RequestRedraw();
            }

            if (update.CloseWindow)
            {
                app.WindowEvent(
                    this,
                    window.Id,
                    new WindowEvent(new WindowEvent.CloseRequested()));
            }
        }
    }

    private void DispatchRedrawRequests(IApplicationHandler app)
    {
        foreach (Window window in _state.Windows.Values.ToArray())
        {
            if (!window.TakeRedrawRequested())
            {
                continue;
            }

            app.WindowEvent(
                this,
                window.Id,
                new WindowEvent(new WindowEvent.RedrawRequested()));
        }
    }

    private void PumpOnce(IApplicationHandler app, TimeSpan? timeout)
    {
        _state.Connection.PrepareRead();
        _state.Connection.Flush();

        Instant waitStart = Instant.Now();
        int pollTimeout = _state.DispatchedEvents ? 0 : TimeoutFor(timeout);
        PollResult pollResult = PollWaylandAndWakeFd(pollTimeout);

        bool proxyWakeReadable = pollResult.ProxyWakeReadable;
        bool eventWakeReadable = pollResult.EventWakeReadable;

        if (proxyWakeReadable)
        {
            DrainWakeFd(_proxyWakeFd);
        }

        if (eventWakeReadable)
        {
            DrainWakeFd(_eventWakeFd);
        }

        if (pollResult.DisplayReadable)
        {
            _state.Connection.ReadEvents();
            _state.Connection.DispatchPending();
        }
        else
        {
            _state.Connection.CancelRead();
        }

        bool keyboardRepeatDispatched = _state.DispatchKeyboardRepeats();
        bool waitWithoutEvents = timeout is null &&
            ControlFlow.TryGetValue(out ControlFlow.Wait _) &&
            !keyboardRepeatDispatched &&
            !proxyWakeReadable &&
            !eventWakeReadable &&
            !_state.DispatchedEvents;
        if (pollResult.TimedOut && waitWithoutEvents)
        {
            return;
        }

        if (waitWithoutEvents)
        {
            return;
        }

        app.NewEvents(this, StartCauseForWake(waitStart));

        if (proxyWakeReadable)
        {
            app.ProxyWakeUp(this);
        }

        if (pollResult.DisplayReadable || keyboardRepeatDispatched || eventWakeReadable || proxyWakeReadable)
        {
            DispatchPendingEvents(app);
        }
        else if (ControlFlow.TryGetValue(out ControlFlow.Poll _))
        {
            Thread.Yield();
        }

        app.AboutToWait(this);
        _state.Connection.CheckError();
    }

    private PollResult PollWaylandAndWakeFd(int timeout)
    {
        PollFd* fds = stackalloc PollFd[3];
        fds[0] = new PollFd
        {
            Fd = _state.Connection.FileDescriptor,
            Events = PollEvents.In,
        };
        fds[1] = new PollFd
        {
            Fd = _proxyWakeFd,
            Events = PollEvents.In,
        };
        fds[2] = new PollFd
        {
            Fd = _eventWakeFd,
            Events = PollEvents.In,
        };

        int result = PInvoke.Poll(fds, 3, timeout);
        if (result < 0)
        {
            throw new InvalidOperationException("poll failed in the Wayland event loop.");
        }

        return new PollResult(
            result == 0,
            (fds[0].Revents & (PollEvents.In | PollEvents.Err | PollEvents.Hup)) != 0,
            (fds[1].Revents & (PollEvents.In | PollEvents.Err | PollEvents.Hup)) != 0,
            (fds[2].Revents & (PollEvents.In | PollEvents.Err | PollEvents.Hup)) != 0);
    }

    private static void DrainWakeFd(int fd)
    {
        ulong value;
        _ = PInvoke.Read(fd, &value, sizeof(ulong));
    }

    private static void WriteWakeFd(int fd)
    {
        ulong value = 1;
        _ = PInvoke.Write(fd, &value, sizeof(ulong));
    }

    private int TimeoutForControlFlow()
    {
        if (ControlFlow.TryGetValue(out ControlFlow.Poll _))
        {
            return 0;
        }

        if (ControlFlow.TryGetValue(out ControlFlow.WaitUntil waitUntil))
        {
            long ticks = waitUntil.Instant.Timestamp - Instant.Now().Timestamp;
            if (ticks <= 0)
            {
                return 0;
            }

            double milliseconds = ticks * 1000.0 / TimeProvider.System.TimestampFrequency;
            return milliseconds >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)Math.Ceiling(milliseconds));
        }

        return -1;
    }

    private int TimeoutFor(TimeSpan? pumpTimeout)
    {
        int controlFlowTimeout = TimeoutForControlFlow();
        if (_state.KeyboardRepeatTimeoutMilliseconds() is { } repeatTimeout)
        {
            controlFlowTimeout = MinTimeout(controlFlowTimeout, repeatTimeout);
        }

        if (pumpTimeout is not { } timeout)
        {
            return controlFlowTimeout;
        }

        int requestedTimeout = TimeoutToPollMilliseconds(timeout);
        return MinTimeout(controlFlowTimeout, requestedTimeout);
    }

    private static int MinTimeout(int left, int right)
    {
        if (left < 0)
        {
            return right;
        }

        if (right < 0)
        {
            return left;
        }

        return Math.Min(left, right);
    }

    private static int TimeoutToPollMilliseconds(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return 0;
        }

        double milliseconds = timeout.TotalMilliseconds;
        return milliseconds >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)Math.Ceiling(milliseconds));
    }

    private StartCause StartCauseForWake(Instant waitStart)
    {
        if (ControlFlow.TryGetValue(out ControlFlow.Poll _))
        {
            return new StartCause(new StartCause.Poll());
        }

        if (ControlFlow.TryGetValue(out ControlFlow.WaitUntil waitUntil) &&
            Instant.Now().Timestamp >= waitUntil.Instant.Timestamp)
        {
            return new StartCause(new StartCause.ResumeTimeReached(waitStart, waitUntil.Instant));
        }

        Instant? requestedResume = ControlFlow.TryGetValue(out ControlFlow.WaitUntil requested)
            ? requested.Instant
            : null;
        return new StartCause(new StartCause.WaitCancelled(waitStart, requestedResume));
    }

    private readonly record struct PollResult(
        bool TimedOut,
        bool DisplayReadable,
        bool ProxyWakeReadable,
        bool EventWakeReadable);

    private sealed unsafe class PumpEventNotifier : IDisposable
    {
        private readonly object _lock = new();
        private readonly WlDisplay _display;
        private readonly int _displayFd;
        private readonly int _eventWakeFd;
        private readonly int _workerWakeFd;
        private readonly Thread _thread;
        private PumpEventNotifierAction _action = PumpEventNotifierAction.Pause;
        private bool _disposed;

        private PumpEventNotifier(WaylandConnection connection, int eventWakeFd, int workerWakeFd)
        {
            _display = connection.Display;
            _displayFd = connection.FileDescriptor;
            _eventWakeFd = eventWakeFd;
            _workerWakeFd = workerWakeFd;
            _thread = new Thread(ThreadMain)
            {
                IsBackground = true,
                Name = "winit wayland pump_events mon",
            };
        }

        public static PumpEventNotifier? Spawn(WaylandConnection connection, int eventWakeFd)
        {
            int workerWakeFd = PInvoke.EventFd(0, EventFdFlags.NonBlock | EventFdFlags.CloExec);
            if (workerWakeFd < 0)
            {
                return null;
            }

            PumpEventNotifier notifier = new(connection, eventWakeFd, workerWakeFd);
            try
            {
                notifier._thread.Start();
                return notifier;
            }
            catch
            {
                _ = PInvoke.Close(workerWakeFd);
                return null;
            }
        }

        public void StartMonitoring()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _action = PumpEventNotifierAction.Monitor;
                System.Threading.Monitor.Pulse(_lock);
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _action = PumpEventNotifierAction.Pause;
            }

            WriteWakeFd(_workerWakeFd);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _action = PumpEventNotifierAction.Shutdown;
                System.Threading.Monitor.Pulse(_lock);
            }

            WriteWakeFd(_workerWakeFd);
            _thread.Join();
            _ = PInvoke.Close(_workerWakeFd);
        }

        private void ThreadMain()
        {
            PollFd* fds = stackalloc PollFd[2];
            while (WaitForMonitorRequest())
            {
                fds[0] = new PollFd
                {
                    Fd = _displayFd,
                    Events = PollEvents.In,
                };
                fds[1] = new PollFd
                {
                    Fd = _workerWakeFd,
                    Events = PollEvents.In,
                };

                int result = PInvoke.Poll(fds, 2, -1);
                if (result < 0)
                {
                    continue;
                }

                bool workerWakeReadable = (fds[1].Revents & (PollEvents.In | PollEvents.Err | PollEvents.Hup)) != 0;
                if (workerWakeReadable)
                {
                    DrainWakeFd(_workerWakeFd);
                    if (IsShutdown())
                    {
                        return;
                    }

                    continue;
                }

                bool displayReadable = (fds[0].Revents & (PollEvents.In | PollEvents.Err | PollEvents.Hup)) != 0;
                if (displayReadable)
                {
                    WriteWakeFd(_eventWakeFd);
                }
            }
        }

        private bool WaitForMonitorRequest()
        {
            lock (_lock)
            {
                while (_action == PumpEventNotifierAction.Pause && !_disposed)
                {
                    System.Threading.Monitor.Wait(_lock);
                }

                if (_disposed || _action == PumpEventNotifierAction.Shutdown)
                {
                    return false;
                }

                _action = PumpEventNotifierAction.Pause;
                return true;
            }
        }

        private bool IsShutdown()
        {
            lock (_lock)
            {
                return _disposed || _action == PumpEventNotifierAction.Shutdown;
            }
        }
    }

    private enum PumpEventNotifierAction
    {
        Monitor,
        Pause,
        Shutdown,
    }
}
