using System.Runtime.ExceptionServices;
using Winit.Core;
using Winit.Dpi;

namespace Winit.Win32;

internal enum RunnerState
{
    Uninitialized,
    Idle,
    HandlingMainEvents,
    Destroyed,
}

internal sealed class EventLoopRunner(EventLoop eventLoop, uint threadId, HWND threadMsgTarget)
{
    private readonly Queue<EventLoopEvent> _eventBuffer = new();
    private IApplicationHandler? _application;
    private ExceptionDispatchInfo? _pendingException;
    private bool _handlingEvent;
    private int? _exitCode;
    private RunnerState _runnerState = RunnerState.Uninitialized;
    private Instant _lastEventsCleared = Instant.Now();

    public uint ThreadId { get; } = threadId;

    public HWND ThreadMsgTarget { get; } = threadMsgTarget;

    public bool InterruptMessageDispatch { get; set; }

    public ControlFlow ControlFlow { get; set; } = ControlFlow.Default;

    public bool Exiting => _exitCode is not null;

    public int? ExitCode => _exitCode;

    public IDisposable SetApp(IApplicationHandler app)
    {
        if (_application is not null)
        {
            throw new InvalidOperationException("an application handler is already registered");
        }

        _application = app;
        return new AppRegistration(this);
    }

    public void ResetRunner()
    {
        InterruptMessageDispatch = false;
        _runnerState = RunnerState.Uninitialized;
        _pendingException = null;
        _exitCode = null;
        _application = null;
        _handlingEvent = false;
    }

    public void ClearExit()
    {
        _exitCode = null;
    }

    public void SetExitCode(int code)
    {
        _exitCode = code;
    }

    public void StoreException(Exception exception)
    {
        _pendingException ??= ExceptionDispatchInfo.Capture(exception);
    }

    public void ThrowPendingException()
    {
        ExceptionDispatchInfo? exception = _pendingException;
        if (exception is null)
        {
            return;
        }

        _pendingException = null;
        exception.Throw();
    }

    public void PrepareWait()
    {
        MoveStateTo(RunnerState.Idle);
    }

    public void WakeUp()
    {
        MoveStateTo(RunnerState.HandlingMainEvents);
    }

    public void SendEvent(EventLoopEvent @event)
    {
        if (@event.IsRedrawRequested)
        {
            CallEventHandler((app, activeEventLoop) => @event.Dispatch(activeEventLoop, app));
            InterruptMessageDispatch = true;
        }
        else if (ShouldBuffer())
        {
            _eventBuffer.Enqueue(@event.BufferScaleFactor());
        }
        else
        {
            CallEventHandler((app, activeEventLoop) => @event.Dispatch(activeEventLoop, app));
            DispatchBufferedEvents();
        }
    }

    public void LoopDestroyed()
    {
        MoveStateTo(RunnerState.Destroyed);
    }

    internal bool ShouldBuffer()
    {
        return _application is null || _handlingEvent;
    }

    private void CallEventHandler(Action<IApplicationHandler, EventLoop> invoke)
    {
        if (_application is not { } app)
        {
            StoreException(new InvalidOperationException("no application handler is registered"));
            return;
        }

        if (_handlingEvent)
        {
            StoreException(new InvalidOperationException("application handler re-entered unexpectedly"));
            return;
        }

        _handlingEvent = true;
        try
        {
            invoke(app, eventLoop);
        }
        catch (Exception exception)
        {
            StoreException(exception);
        }
        finally
        {
            _handlingEvent = false;
        }
    }

    private void DispatchBufferedEvents()
    {
        while (_eventBuffer.Count > 0)
        {
            EventLoopEvent @event = _eventBuffer.Dequeue();
            CallEventHandler((app, activeEventLoop) => @event.Dispatch(activeEventLoop, app));
        }
    }

    private void MoveStateTo(RunnerState newRunnerState)
    {
        RunnerState oldRunnerState = _runnerState;
        _runnerState = newRunnerState;

        if (oldRunnerState == newRunnerState)
        {
            return;
        }

        switch (oldRunnerState, newRunnerState)
        {
            case (RunnerState.Uninitialized, RunnerState.HandlingMainEvents):
                CallNewEvents(init: true);
                break;
            case (RunnerState.Uninitialized, RunnerState.Idle):
                CallNewEvents(init: true);
                CallEventHandler((app, activeEventLoop) => app.AboutToWait(activeEventLoop));
                _lastEventsCleared = Instant.Now();
                break;
            case (RunnerState.Uninitialized, RunnerState.Destroyed):
                CallNewEvents(init: true);
                CallEventHandler((app, activeEventLoop) => app.AboutToWait(activeEventLoop));
                _lastEventsCleared = Instant.Now();
                break;
            case (_, RunnerState.Uninitialized):
                throw new InvalidOperationException("cannot move runner state to Uninitialized");
            case (RunnerState.Idle, RunnerState.HandlingMainEvents):
                CallNewEvents(init: false);
                break;
            case (RunnerState.Idle, RunnerState.Destroyed):
                break;
            case (RunnerState.HandlingMainEvents, RunnerState.Idle):
                CallEventHandler((app, activeEventLoop) => app.AboutToWait(activeEventLoop));
                _lastEventsCleared = Instant.Now();
                break;
            case (RunnerState.HandlingMainEvents, RunnerState.Destroyed):
                CallEventHandler((app, activeEventLoop) => app.AboutToWait(activeEventLoop));
                _lastEventsCleared = Instant.Now();
                break;
            case (RunnerState.Destroyed, _):
                throw new InvalidOperationException("cannot move runner state from Destroyed");
        }
    }

    private void CallNewEvents(bool init)
    {
        StartCause startCause = StartCause(init);
        CallEventHandler((app, activeEventLoop) => app.NewEvents(activeEventLoop, startCause));

        if (init)
        {
            CallEventHandler((app, activeEventLoop) => app.CanCreateSurfaces(activeEventLoop));
        }

        DispatchBufferedEvents();
    }

    private StartCause StartCause(bool init)
    {
        if (init)
        {
            return new StartCause(new StartCause.Init());
        }

        if (ControlFlow.TryGetValue(out ControlFlow.Poll _) && _exitCode is null)
        {
            return new StartCause(new StartCause.Poll());
        }

        if (_exitCode is not null || ControlFlow.TryGetValue(out ControlFlow.Wait _))
        {
            return new StartCause(new StartCause.WaitCancelled(_lastEventsCleared, null));
        }

        if (ControlFlow.TryGetValue(out ControlFlow.WaitUntil waitUntil))
        {
            return Instant.Now().Timestamp < waitUntil.Instant.Timestamp
                ? new StartCause(new StartCause.WaitCancelled(_lastEventsCleared, waitUntil.Instant))
                : new StartCause(new StartCause.ResumeTimeReached(_lastEventsCleared, waitUntil.Instant));
        }

        return new StartCause(new StartCause.WaitCancelled(_lastEventsCleared, null));
    }

    private sealed class AppRegistration(EventLoopRunner runner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            runner._application = null;
            _disposed = true;
        }
    }
}

internal readonly record struct EventLoopEvent
{
    public readonly record struct Window(WindowId WindowId, WindowEvent Event);

    public readonly record struct Device(DeviceId? DeviceId, DeviceEvent Event);

    public readonly record struct WakeUp;

    public readonly record struct BufferedScaleFactorChanged(
        WindowId WindowId,
        double ScaleFactor,
        PhysicalSize<uint> SurfaceSize);

    private const byte WindowTag = 0;
    private const byte DeviceTag = 1;
    private const byte WakeUpTag = 2;
    private const byte BufferedScaleFactorChangedTag = 3;

    private readonly byte _tag;
    private readonly Window _window;
    private readonly Device _device;
    private readonly WakeUp _wakeUp;
    private readonly BufferedScaleFactorChanged _bufferedScaleFactorChanged;

    public EventLoopEvent(Window value)
    {
        this = default;
        _tag = WindowTag;
        _window = value;
    }

    public EventLoopEvent(Device value)
    {
        this = default;
        _tag = DeviceTag;
        _device = value;
    }

    public EventLoopEvent(WakeUp value)
    {
        this = default;
        _tag = WakeUpTag;
        _wakeUp = value;
    }

    public EventLoopEvent(BufferedScaleFactorChanged value)
    {
        this = default;
        _tag = BufferedScaleFactorChangedTag;
        _bufferedScaleFactorChanged = value;
    }

    public bool IsRedrawRequested =>
        _tag == WindowTag && _window.Event.TryGetValue(out WindowEvent.RedrawRequested _);

    public EventLoopEvent BufferScaleFactor()
    {
        if (_tag == WindowTag &&
            _window.Event.TryGetValue(out WindowEvent.ScaleFactorChanged scaleFactorChanged) &&
            scaleFactorChanged.SurfaceSizeWriter.TryGetSurfaceSize(out PhysicalSize<uint> surfaceSize))
        {
            return new EventLoopEvent(
                new BufferedScaleFactorChanged(_window.WindowId, scaleFactorChanged.ScaleFactor, surfaceSize));
        }

        return this;
    }

    public void Dispatch(EventLoop eventLoop, IApplicationHandler app)
    {
        switch (_tag)
        {
            case WindowTag:
                app.WindowEvent(eventLoop, _window.WindowId, _window.Event);
                break;
            case DeviceTag:
                app.DeviceEvent(eventLoop, _device.DeviceId, _device.Event);
                break;
            case WakeUpTag:
                _ = _wakeUp;
                app.ProxyWakeUp(eventLoop);
                break;
            case BufferedScaleFactorChangedTag:
                global::Winit.Win32.Window.DispatchBufferedScaleFactorChanged(
                    eventLoop,
                    app,
                    _bufferedScaleFactorChanged.WindowId,
                    _bufferedScaleFactorChanged.ScaleFactor,
                    _bufferedScaleFactorChanged.SurfaceSize);
                break;
            default:
                throw new InvalidOperationException("invalid event tag");
        }
    }
}
