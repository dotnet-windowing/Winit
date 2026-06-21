using System.Threading;
using RawWindowHandles;
using Winit.Core;
using Winit.Dpi;
using AndroidKeycode = global::Android.Views.Keycode;
using AndroidKeyEvent = global::Android.Views.KeyEvent;
using AndroidMotionEvent = global::Android.Views.MotionEvent;
using AndroidMotionEventActions = global::Android.Views.MotionEventActions;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.Android;

public sealed class EventLoopBuilder : IPlatformEventLoopBuilder, IEventLoopBuilderExtAndroid
{
    private readonly PlatformSpecificEventLoopAttributes _attributes = new();

    public EventLoopBuilder WithAndroidApp(AndroidApp app)
    {
        _attributes.AndroidApp = app;
        return this;
    }

    public EventLoopBuilder HandleVolumeKeys()
    {
        _attributes.IgnoreVolumeKeys = false;
        return this;
    }

    public IPlatformEventLoop Build()
    {
        return new EventLoop(_attributes);
    }
}

public sealed class EventLoop : IPlatformEventLoop, IEventLoopExtRegister, IEventLoopExtAndroid, IActiveEventLoopExtAndroid
{
    internal static readonly WindowId GlobalWindowId = WindowId.FromRaw(0);

    private static int s_created;

    private readonly AndroidApp _androidApp;
    private readonly bool _ignoreVolumeKeys;
    private readonly Lock _lock = new();
    private IApplicationHandler? _app;
    private Window? _window;
    private ControlFlow _controlFlow = ControlFlow.Default;
    private bool _dispatching;
    private bool _exiting;
    private bool _focused;
    private bool _paused = true;
    private bool _proxyWakeUp;
    private bool _redrawRequested;
    private bool _surfaceAvailable;
    private bool _surfacesCreated;
    private FingerId? _primaryFinger;

    public EventLoop(PlatformSpecificEventLoopAttributes attributes)
    {
        if (Interlocked.Exchange(ref s_created, 1) != 0)
        {
            throw new EventLoopRecreationException();
        }

        _androidApp = attributes.AndroidApp
            ?? throw new InvalidOperationException(
                "An AndroidApp is required to create an EventLoop on Android. Use WithAndroidApp(app).");
        _ignoreVolumeKeys = attributes.IgnoreVolumeKeys;
    }

    public AndroidApp AndroidApp => _androidApp;

    public IEnumerable<CoreMonitorHandle> AvailableMonitors => [];

    public CoreMonitorHandle? PrimaryMonitor => null;

    public Theme? SystemTheme => null;

    public ControlFlow ControlFlow
    {
        get => _controlFlow;
        set => _controlFlow = value;
    }

    public bool Exiting => _exiting;

    public OwnedDisplayHandle OwnedDisplayHandle => _androidApp.OwnedDisplayHandle;

    public RawDisplayHandle? DisplayHandle => _androidApp.DisplayHandle;

    internal bool HasFocus => _focused;

    public EventLoopProxy CreateProxy()
    {
        return new EventLoopProxy(new EventLoopProxyProvider(this));
    }

    public IWindow CreateWindow(WindowAttributes windowAttributes)
    {
        ArgumentNullException.ThrowIfNull(windowAttributes);

        lock (_lock)
        {
            if (_window is not null)
            {
                return _window;
            }

            _window = new Window(this, windowAttributes);
            return _window;
        }
    }

    public CustomCursor CreateCustomCursor(CustomCursorSource customCursor)
    {
        throw new NotSupportedRequestException("custom cursors are not supported by the Android backend");
    }

    public void ListenDeviceEvents(DeviceEvents allowed)
    {
    }

    public void Exit()
    {
        _exiting = true;
    }

    public void RunApp(IApplicationHandler app)
    {
        RegisterApp(app);
    }

    public void RegisterApp(IApplicationHandler app)
    {
        ArgumentNullException.ThrowIfNull(app);

        lock (_lock)
        {
            if (_app is not null)
            {
                throw new InvalidOperationException("an application handler is already registered");
            }

            _app = app;
        }

        PostDispatch(new StartCause(new StartCause.Init()), null);
        _androidApp.Attach(this);
    }

    internal void RequestRedraw()
    {
        lock (_lock)
        {
            _redrawRequested = true;
        }

        PostDispatch(new StartCause(new StartCause.Poll()), null);
    }

    internal void HandleStart()
    {
        PostDispatch(new StartCause(new StartCause.Poll()), app => app.Resumed(this));
    }

    internal void HandleResume()
    {
        lock (_lock)
        {
            _paused = false;
        }
    }

    internal void HandlePause()
    {
        lock (_lock)
        {
            _paused = true;
        }
    }

    internal void HandleStop()
    {
        PostDispatch(new StartCause(new StartCause.Poll()), app => app.Suspended(this));
    }

    internal void HandleLowMemory()
    {
        PostDispatch(new StartCause(new StartCause.Poll()), app => app.MemoryWarning(this));
    }

    internal void HandleProxyWakeUp()
    {
        lock (_lock)
        {
            _proxyWakeUp = true;
        }

        PostDispatch(new StartCause(new StartCause.WaitCancelled(Instant.Now(), null)), null);
    }

    internal void HandleFocusChanged(bool focused)
    {
        bool changed;
        lock (_lock)
        {
            changed = _focused != focused;
            _focused = focused;
        }

        if (!changed)
        {
            return;
        }

        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app => app.WindowEvent(this, GlobalWindowId, new WindowEvent(new WindowEvent.Focused(focused))));
    }

    internal void HandleSurfaceCreated(PhysicalSize<uint> size)
    {
        lock (_lock)
        {
            _surfaceAvailable = true;
        }

        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app =>
            {
                if (!_surfacesCreated)
                {
                    app.CanCreateSurfaces(this);
                    _surfacesCreated = true;
                }

                DispatchSurfaceResized(app, size);
            });
    }

    internal void HandleSurfaceResized(PhysicalSize<uint> size)
    {
        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app => DispatchSurfaceResized(app, size));
    }

    internal void HandleSurfaceDestroyed()
    {
        lock (_lock)
        {
            _surfaceAvailable = false;
            _redrawRequested = false;
        }

        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app =>
            {
                if (_surfacesCreated)
                {
                    app.DestroySurfaces(this);
                    _surfacesCreated = false;
                }
            });
    }

    internal bool HandleMotionEvent(AndroidMotionEvent e)
    {
        AndroidMotionEventActions action = e.ActionMasked;

        if (action == AndroidMotionEventActions.Move)
        {
            for (int i = 0; i < e.PointerCount; i++)
            {
                DispatchPointerMoved(e, i);
            }

            return true;
        }

        int index = e.ActionIndex;
        if (index < 0 || index >= e.PointerCount)
        {
            index = 0;
        }

        switch (action)
        {
            case AndroidMotionEventActions.Down:
            case AndroidMotionEventActions.PointerDown:
                DispatchPointerButton(e, index, ElementState.Pressed);
                return true;
            case AndroidMotionEventActions.Up:
            case AndroidMotionEventActions.PointerUp:
                DispatchPointerButton(e, index, ElementState.Released);
                return true;
            case AndroidMotionEventActions.Cancel:
                DispatchPointerLeft(e, index);
                return true;
            default:
                return false;
        }
    }

    internal bool HandleKeyEvent(AndroidKeycode keyCode, AndroidKeyEvent? e, ElementState state)
    {
        if (_ignoreVolumeKeys
            && (keyCode == AndroidKeycode.VolumeDown
                || keyCode == AndroidKeycode.VolumeUp
                || keyCode == AndroidKeycode.VolumeMute))
        {
            return false;
        }

        KeyEvent keyEvent = KeyCodes.BuildKeyEvent(keyCode, e, state);
        DeviceId? deviceId = e is not null ? DeviceId.FromRaw(e.DeviceId) : null;
        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app => app.WindowEvent(
                this,
                GlobalWindowId,
                new WindowEvent(new WindowEvent.KeyboardInput(deviceId, keyEvent, IsSynthetic: false))));
        return true;
    }

    private void DispatchPointerMoved(AndroidMotionEvent e, int index)
    {
        FingerId finger = FingerId.FromRaw(checked((nuint)e.GetPointerId(index)));
        bool primary = IsPrimaryFinger(finger);
        PhysicalPosition<double> position = new(e.GetX(index), e.GetY(index));
        PointerSource source = TouchSource(finger, e.GetPressure(index));
        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app => app.WindowEvent(
                this,
                GlobalWindowId,
                new WindowEvent(new WindowEvent.PointerMoved(DeviceId.FromRaw(e.DeviceId), position, primary, source))));
    }

    private void DispatchPointerButton(AndroidMotionEvent e, int index, ElementState state)
    {
        FingerId finger = FingerId.FromRaw(checked((nuint)e.GetPointerId(index)));
        PhysicalPosition<double> position = new(e.GetX(index), e.GetY(index));
        bool primary;

        lock (_lock)
        {
            if (state == ElementState.Pressed && _primaryFinger is null)
            {
                _primaryFinger = finger;
            }

            primary = _primaryFinger == finger;

            if (state == ElementState.Released && primary)
            {
                _primaryFinger = null;
            }
        }

        PointerSource source = TouchSource(finger, e.GetPressure(index));
        ButtonSource button = new(new ButtonSource.Touch(finger, Force(e.GetPressure(index))));
        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app =>
            {
                if (state == ElementState.Pressed)
                {
                    app.WindowEvent(
                        this,
                        GlobalWindowId,
                        new WindowEvent(new WindowEvent.PointerEntered(
                            DeviceId.FromRaw(e.DeviceId),
                            position,
                            primary,
                            PointerKind.From(source))));
                }

                app.WindowEvent(
                    this,
                    GlobalWindowId,
                    new WindowEvent(new WindowEvent.PointerButton(
                        DeviceId.FromRaw(e.DeviceId),
                        state,
                        position,
                        primary,
                        button)));

                if (state == ElementState.Released)
                {
                    app.WindowEvent(
                        this,
                        GlobalWindowId,
                        new WindowEvent(new WindowEvent.PointerLeft(
                            DeviceId.FromRaw(e.DeviceId),
                            position,
                            primary,
                            PointerKind.From(source))));
                }
            });
    }

    private void DispatchPointerLeft(AndroidMotionEvent e, int index)
    {
        FingerId finger = FingerId.FromRaw(checked((nuint)e.GetPointerId(index)));
        PhysicalPosition<double> position = new(e.GetX(index), e.GetY(index));
        bool primary = IsPrimaryFinger(finger);
        PointerSource source = TouchSource(finger, e.GetPressure(index));
        PostDispatch(
            new StartCause(new StartCause.Poll()),
            app => app.WindowEvent(
                this,
                GlobalWindowId,
                new WindowEvent(new WindowEvent.PointerLeft(
                    DeviceId.FromRaw(e.DeviceId),
                    position,
                    primary,
                    PointerKind.From(source)))));
    }

    private bool IsPrimaryFinger(FingerId finger)
    {
        lock (_lock)
        {
            return _primaryFinger == finger;
        }
    }

    private static PointerSource TouchSource(FingerId finger, float pressure)
    {
        return new PointerSource(new PointerSource.Touch(finger, Force(pressure)));
    }

    private static Force Force(float pressure)
    {
        return new Force(new Force.Normalized(Math.Clamp(pressure, 0.0f, 1.0f)));
    }

    private void DispatchSurfaceResized(IApplicationHandler app, PhysicalSize<uint> size)
    {
        if (_window is null)
        {
            return;
        }

        app.WindowEvent(this, GlobalWindowId, new WindowEvent(new WindowEvent.SurfaceResized(size)));
    }

    private void DrainRedrawRequest(IApplicationHandler app)
    {
        bool shouldRedraw;
        lock (_lock)
        {
            shouldRedraw = _surfaceAvailable && !_paused && _redrawRequested;
            _redrawRequested = false;
        }

        if (shouldRedraw && _window is not null)
        {
            app.WindowEvent(this, GlobalWindowId, new WindowEvent(new WindowEvent.RedrawRequested()));
        }
    }

    private void PostDispatch(StartCause cause, Action<IApplicationHandler>? body)
    {
        _androidApp.PostToUiThread(() => Dispatch(cause, body));
    }

    private void Dispatch(StartCause cause, Action<IApplicationHandler>? body)
    {
        IApplicationHandler? app;
        lock (_lock)
        {
            if (_dispatching || _exiting)
            {
                return;
            }

            _dispatching = true;
            app = _app;
        }

        if (app is null)
        {
            lock (_lock)
            {
                _dispatching = false;
            }

            return;
        }

        try
        {
            app.NewEvents(this, cause);
            body?.Invoke(app);

            bool proxyWakeUp;
            lock (_lock)
            {
                proxyWakeUp = _proxyWakeUp;
                _proxyWakeUp = false;
            }

            if (proxyWakeUp)
            {
                app.ProxyWakeUp(this);
            }

            DrainRedrawRequest(app);
            app.AboutToWait(this);
        }
        finally
        {
            lock (_lock)
            {
                _dispatching = false;
            }
        }
    }
}

public sealed class EventLoopProxyProvider(EventLoop eventLoop) : IEventLoopProxyProvider
{
    public void WakeUp()
    {
        eventLoop.HandleProxyWakeUp();
    }
}

public sealed class PlatformSpecificEventLoopAttributes
{
    public AndroidApp? AndroidApp { get; set; }

    public bool IgnoreVolumeKeys { get; set; } = true;
}

public interface IEventLoopBuilderExtAndroid
{
    EventLoopBuilder WithAndroidApp(AndroidApp app);

    EventLoopBuilder HandleVolumeKeys();
}

public interface IEventLoopExtAndroid
{
    AndroidApp AndroidApp { get; }
}

public interface IActiveEventLoopExtAndroid
{
    AndroidApp AndroidApp { get; }
}
