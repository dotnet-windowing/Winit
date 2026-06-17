using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using RawWindowHandles;
using Winit.Common.Xkb;
using Winit.Core;
using Winit.Dpi;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.Wayland;

public sealed unsafe class Window : IWindow, IWindowExtWayland, IDisposable
{
    private const int MaxTitleUtf8Bytes = 1024;

    private readonly EventLoop _eventLoop;
    private readonly GCHandle _selfHandle;
    private readonly Lock _lock = new();
    private readonly List<WinitTextInput> _textInputs = [];
    private readonly bool _preferCsd;
    private WindowState _state;
    private WaylandViewport? _viewport;
    private FractionalScaling? _fractionalScaling;
    private ToplevelDecoration? _toplevelDecoration;
    private ToplevelIcon? _toplevelIcon;
    private SurfaceBlurEffect? _blurEffect;
    private ClientSideFrame? _clientSideFrame;
    private FrameCallback? _frameCallback;
    private bool _attentionRequested;
    private bool _clientSideFrameFailed;
    private bool _destroyed;

    private Window(
        EventLoop eventLoop,
        WlSurface surface,
        XdgSurface xdgSurface,
        XdgToplevel xdgToplevel,
        WindowAttributes attributes)
    {
        _eventLoop = eventLoop;
        SurfaceHandle = surface;
        XdgSurfaceHandle = xdgSurface;
        XdgToplevelHandle = xdgToplevel;
        _state = new WindowState(attributes);
        _preferCsd = (attributes.Platform as WindowAttributesWayland)?.PreferCsd ?? false;
        Id = WindowId.FromRaw((nuint)surface.Value);
        _selfHandle = GCHandle.Alloc(this);
    }

    ~Window()
    {
        DestroyFromEventLoop(sendDestroyedEvent: false);
    }

    internal WlSurface SurfaceHandle { get; private set; }

    internal XdgSurface XdgSurfaceHandle { get; private set; }

    internal XdgToplevel XdgToplevelHandle { get; private set; }

    public WindowId Id { get; }

    public double ScaleFactor => _state.ScaleFactor;

    public PhysicalPosition<int> SurfacePosition => new(0, 0);

    public PhysicalPosition<int> OuterPosition => new(0, 0);

    public PhysicalSize<uint> SurfaceSize => _state.SurfaceSize;

    public PhysicalSize<uint> OuterSize
    {
        get
        {
            lock (_lock)
            {
                return (_clientSideFrame?.OuterSize(_state.LogicalSurfaceSize) ?? _state.LogicalSurfaceSize)
                    .ToPhysical<uint>(_state.ScaleFactor);
            }
        }
    }

    public PhysicalInsets<uint> SafeArea => new(0, 0, 0, 0);

    public PhysicalSize<uint>? SurfaceResizeIncrements => _state.PhysicalSurfaceResizeIncrements;

    public bool? IsVisible => null;

    public bool IsResizable => _state.IsResizable;

    public WindowButtons EnabledButtons => WindowButtons.All;

    public bool? IsMinimized => null;

    public bool IsMaximized => _state.IsMaximized;

    public Fullscreen? Fullscreen
    {
        get
        {
            lock (_lock)
            {
                if (_state.Fullscreen is null)
                {
                    return null;
                }

                CoreMonitorHandle? monitor = _state.CurrentMonitor is { } current
                    ? new CoreMonitorHandle(current)
                    : null;
                return Winit.Core.Fullscreen.FromBorderless(monitor);
            }
        }
    }

    public bool IsDecorated
    {
        get
        {
            lock (_lock)
            {
                return _state.ConfiguredDecorationMode switch
                {
                    ZxdgToplevelDecorationV1Mode.ClientSide => _clientSideFrame is { IsHidden: false },
                    ZxdgToplevelDecorationV1Mode.ServerSide => true,
                    _ => _clientSideFrame is { IsHidden: false } || _state.IsDecorated,
                };
            }
        }
    }

    public ImeCapabilities? ImeCapabilities
    {
        get
        {
            lock (_lock)
            {
                return _state.TextInputClientState?.Capabilities;
            }
        }
    }

    public bool HasFocus => _state.HasFocus;

    public Theme? Theme => _state.Theme;

    public string Title => _state.Title;

    internal Cursor CurrentCursor => _state.Cursor;

    internal bool CursorVisible => _state.CursorVisible;

    internal CursorGrabMode CursorGrabMode => _state.CursorGrabMode;

    internal bool CanShowWindowMenu => _state.WmCapabilities.HasFlag(ToplevelWmCapabilities.WindowMenu);

    internal bool CanMaximize => _state.WmCapabilities.HasFlag(ToplevelWmCapabilities.Maximize);

    internal bool CanMinimize => _state.WmCapabilities.HasFlag(ToplevelWmCapabilities.Minimize);

    internal TextInputClientState? TextInputState
    {
        get
        {
            lock (_lock)
            {
                return _state.TextInputClientState;
            }
        }
    }

    private bool IsConfigured
    {
        get
        {
            lock (_lock)
            {
                return _state.Configured;
            }
        }
    }

    public CoreMonitorHandle? CurrentMonitor
    {
        get
        {
            lock (_lock)
            {
                return _state.CurrentMonitor is { } monitor ? new CoreMonitorHandle(monitor) : null;
            }
        }
    }

    public IEnumerable<CoreMonitorHandle> AvailableMonitors => _eventLoop.AvailableMonitors;

    public CoreMonitorHandle? PrimaryMonitor => _eventLoop.PrimaryMonitor;

    public RawDisplayHandle? DisplayHandle => RawDisplayHandle.FromWayland(_eventLoop.State.Connection.Display.Value);

    public RawWindowHandle? WindowHandle => RawWindowHandle.FromWayland(SurfaceHandle.Value);

    public static Window Create(EventLoop eventLoop, WindowAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(eventLoop);
        ArgumentNullException.ThrowIfNull(attributes);

        WinitState state = eventLoop.State;
        WlSurface surface = CreateSurface(state);
        XdgSurface xdgSurface = XdgSurface.Null;
        Winit.Wayland.XdgToplevel xdgToplevel = Winit.Wayland.XdgToplevel.Null;
        Window? window = null;

        try
        {
            xdgSurface = CreateXdgSurface(state, surface);
            xdgToplevel = CreateXdgToplevel(xdgSurface);
            window = new Window(eventLoop, surface, xdgSurface, xdgToplevel, attributes);
            window._toplevelDecoration = state.XdgDecorationManager?.GetToplevelDecoration(state, xdgToplevel, window);
            window._viewport = state.ViewporterState?.GetViewport(state, surface);
            window._fractionalScaling = state.FractionalScalingManager?.GetFractionalScale(state, surface);
            window.InstallDispatchers();
            window.ApplyInitialAttributes(attributes);
            window.UpdateViewportDestination();
            eventLoop.RegisterWindow(window);
            window.Commit();
            state.Connection.Roundtrip();
            while (!window.IsConfigured)
            {
                state.Connection.Dispatch();
            }

            return window;
        }
        catch
        {
            window?.DestroyFromEventLoop(sendDestroyedEvent: false);
            if (window is null)
            {
                if (!xdgToplevel.IsNull)
                {
                    DestroyXdgToplevel(xdgToplevel);
                }

                if (!xdgSurface.IsNull)
                {
                    DestroyXdgSurface(xdgSurface);
                }

                if (!surface.IsNull)
                {
                    DestroySurface(surface);
                }
            }

            throw;
        }
    }

    public void RequestRedraw()
    {
        bool shouldWake;
        lock (_lock)
        {
            shouldWake = !_state.RedrawRequested;
            _state.RedrawRequested = true;
        }

        if (shouldWake)
        {
            _eventLoop.WakeUp();
        }
    }

    public void PrePresentNotify()
    {
        lock (_lock)
        {
            if (!_state.RequestFrameCallback())
            {
                return;
            }
        }

        FrameCallback callback = FrameCallback.Create(_eventLoop.State, SurfaceHandle, this);
        FrameCallback? oldCallback;
        lock (_lock)
        {
            oldCallback = _frameCallback;
            _frameCallback = callback;
        }

        oldCallback?.Dispose();
    }

    public void ResetDeadKeys()
    {
        Xkb.ResetDeadKeys();
    }

    public PhysicalSize<uint>? RequestSurfaceSize(Size size)
    {
        PhysicalSize<uint> requested;
        lock (_lock)
        {
            requested = _state.RequestSurfaceSize(size);
            UpdateViewportDestination();
            UpdateClientSideFrameLocked();
            ReloadWindowGeometryLocked();
        }

        Commit();
        RequestRedraw();
        return requested;
    }

    public void SetOuterPosition(Position position)
    {
        _ = position;
    }

    public void SetMinSurfaceSize(Size? minSize)
    {
        lock (_lock)
        {
            _state.MinSurfaceSize = minSize;
        }

        LogicalSize<int> size = MinSurfaceSizeToLogical(minSize);
        SetMinSize(size.Width, size.Height);
        Commit();
        RequestRedraw();
    }

    public void SetMaxSurfaceSize(Size? maxSize)
    {
        lock (_lock)
        {
            _state.MaxSurfaceSize = maxSize;
        }

        LogicalSize<int> size = MaxSurfaceSizeToLogical(maxSize);
        SetMaxSize(size.Width, size.Height);
        Commit();
        RequestRedraw();
    }

    public void SetSurfaceResizeIncrements(Size? increments)
    {
        lock (_lock)
        {
            _state.SurfaceResizeIncrements = increments?.ToLogical<uint>(ScaleFactor);
        }
    }

    public void SetTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        title = TruncateTitle(title);

        lock (_lock)
        {
            _state.Title = title;
            _clientSideFrame?.SetTitle(title);
        }

        SendStringRequest(XdgToplevelHandle, XdgToplevelRequest.SetTitle, title);
        Commit();
    }

    private static string TruncateTitle(string title)
    {
        if (Encoding.UTF8.GetByteCount(title) <= MaxTitleUtf8Bytes)
        {
            return title;
        }

        int bytes = 0;
        int length = 0;
        foreach (Rune rune in title.EnumerateRunes())
        {
            int runeBytes = rune.Utf8SequenceLength;
            if (bytes + runeBytes > MaxTitleUtf8Bytes)
            {
                break;
            }

            bytes += runeBytes;
            length += rune.Utf16SequenceLength;
        }

        return title[..length];
    }

    public void SetTransparent(bool transparent)
    {
        lock (_lock)
        {
            _state.Transparent = transparent;
        }

        ReloadTransparencyHint(transparent);
        Commit();
    }

    private void ReloadTransparencyHint(bool transparent)
    {
        if (SurfaceHandle.IsNull)
        {
            return;
        }

        if (transparent)
        {
            WlArgument* args = stackalloc WlArgument[1];
            args[0].Object = 0;
            PInvoke.WlProxyMarshalArray(SurfaceHandle, WlSurfaceRequest.SetOpaqueRegion, args);
            return;
        }

        WlRegion region = _eventLoop.State.CreateRegion();
        try
        {
            WlArgument* addArgs = stackalloc WlArgument[4];
            addArgs[0].Int = 0;
            addArgs[1].Int = 0;
            addArgs[2].Int = int.MaxValue;
            addArgs[3].Int = int.MaxValue;
            PInvoke.WlProxyMarshalArray(region, WlRegionRequest.Add, addArgs);

            WlArgument* opaqueArgs = stackalloc WlArgument[1];
            opaqueArgs[0].Object = region.Value;
            PInvoke.WlProxyMarshalArray(SurfaceHandle, WlSurfaceRequest.SetOpaqueRegion, opaqueArgs);
        }
        finally
        {
            DestroyRegion(region);
        }
    }

    public void SetBlur(bool blur)
    {
        lock (_lock)
        {
            _state.Blur = blur;
        }

        if (ApplyBlur(blur))
        {
            RequestRedraw();
        }
    }

    private bool ApplyBlur(bool blur)
    {
        if (SurfaceHandle.IsNull)
        {
            return false;
        }

        if (!blur)
        {
            if (_blurEffect is not null)
            {
                _blurEffect.Dispose();
                _blurEffect = null;
                Commit();
                return true;
            }

            return false;
        }

        BgrEffectManager? manager = _eventLoop.State.BlurManager;
        if (manager is null)
        {
            return false;
        }

        _blurEffect ??= manager.NewBlurEffect(_eventLoop.State, SurfaceHandle);

        WlRegion region = _eventLoop.State.CreateRegion();
        try
        {
            WlArgument* args = stackalloc WlArgument[4];
            args[0].Int = 0;
            args[1].Int = 0;
            args[2].Int = int.MaxValue;
            args[3].Int = int.MaxValue;
            PInvoke.WlProxyMarshalArray(region, WlRegionRequest.Add, args);

            if (_blurEffect.SetBlur(region))
            {
                Commit();
                return true;
            }
        }
        finally
        {
            DestroyRegion(region);
        }

        return false;
    }

    public void SetVisible(bool visible)
    {
        _ = visible;
    }

    public void SetResizable(bool resizable)
    {
        lock (_lock)
        {
            if (_state.IsResizable == resizable)
            {
                return;
            }

            _state.IsResizable = resizable;
            _clientSideFrame?.SetResizable(resizable);
            ReloadSizeHintsLocked();
        }

        Commit();
        RequestRedraw();
    }

    public void SetEnabledButtons(WindowButtons buttons)
    {
        _ = buttons;
    }

    public void SetMinimized(bool minimized)
    {
        if (minimized)
        {
            SendNoArgRequest(XdgToplevelHandle, XdgToplevelRequest.SetMinimized);
            Commit();
        }
    }

    public void SetMaximized(bool maximized)
    {
        SendNoArgRequest(
            XdgToplevelHandle,
            maximized ? XdgToplevelRequest.SetMaximized : XdgToplevelRequest.UnsetMaximized);
        Commit();
    }

    public void SetFullscreen(Fullscreen? fullscreen)
    {
        if (fullscreen?.TryGetValue(out Fullscreen.Exclusive _) == true)
        {
            return;
        }

        if (fullscreen is null)
        {
            SendNoArgRequest(XdgToplevelHandle, XdgToplevelRequest.UnsetFullscreen);
        }
        else
        {
            WlArgument* args = stackalloc WlArgument[1];
            args[0].Object = FullscreenOutput(fullscreen.Value).Value;
            PInvoke.WlProxyMarshalArray(XdgToplevelHandle, XdgToplevelRequest.SetFullscreen, args);
        }

        Commit();
    }

    public void SetDecorations(bool decorations)
    {
        lock (_lock)
        {
            _state.IsDecorated = decorations;
            UpdateClientSideFrameLocked();
            ReloadSizeHintsLocked();
            ReloadWindowGeometryLocked();
        }

        _toplevelDecoration?.SetMode(DecorationModeFor(decorations));
        Commit();
    }

    public void SetWindowLevel(WindowLevel level)
    {
        lock (_lock)
        {
            _state.WindowLevel = level;
        }
    }

    public void SetWindowIcon(Icon? windowIcon)
    {
        XdgToplevelIconManagerState? manager = _eventLoop.State.XdgToplevelIconManager;
        if (manager is null)
        {
            return;
        }

        ToplevelIcon? newIcon = manager.SetIcon(_eventLoop.State, XdgToplevelHandle, windowIcon);
        ToplevelIcon? oldIcon = _toplevelIcon;
        _toplevelIcon = newIcon;
        oldIcon?.Dispose();
        Commit();
    }

    public void RequestImeUpdate(ImeRequest request)
    {
        TextInputClientState? textInputState;
        WinitTextInput[] textInputs;
        bool? enabledChanged = null;

        lock (_lock)
        {
            if (request.TryGetValue(out ImeRequest.Enable enable))
            {
                if (_state.TextInputClientState is not null)
                {
                    throw new ImeRequestException(ImeRequestError.AlreadyEnabled);
                }

                (ImeCapabilities capabilities, ImeRequestData requestData) = enable.Value.IntoRaw();
                _state.TextInputClientState = new TextInputClientState(capabilities, requestData, ScaleFactor);
                enabledChanged = true;
            }
            else if (request.TryGetValue(out ImeRequest.Update update))
            {
                if (_state.TextInputClientState is null)
                {
                    throw new ImeRequestException(ImeRequestError.NotEnabled);
                }

                _state.TextInputClientState.Update(update.Value, ScaleFactor);
            }
            else if (request.TryGetValue(out ImeRequest.Disable _))
            {
                _state.TextInputClientState = null;
                enabledChanged = false;
            }

            textInputState = _state.TextInputClientState;
            textInputs = [.. _textInputs];
        }

        foreach (WinitTextInput textInput in textInputs)
        {
            textInput.SetState(textInputState, sendEnable: enabledChanged == true);
        }

        if (enabledChanged is { } enabled)
        {
            Ime ime = enabled
                ? new Ime(new Ime.Enabled())
                : new Ime(new Ime.Disabled());
            _eventLoop.QueueWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.Ime(ime)));
        }
    }

    public void FocusWindow()
    {
    }

    public void RequestUserAttention(UserAttentionType? requestType)
    {
        if (requestType is null || _eventLoop.State.XdgActivation is not { } activation)
        {
            return;
        }

        lock (_lock)
        {
            if (_attentionRequested)
            {
                return;
            }

            _attentionRequested = true;
        }

        try
        {
            activation.RequestUserAttention(_eventLoop.State, SurfaceHandle, this);
        }
        catch
        {
            MarkAttentionRequestFinished();
            throw;
        }
    }

    public AsyncRequestSerial RequestActivationToken()
    {
        XdgActivationState activation = _eventLoop.State.XdgActivation
            ?? throw new NotSupportedException("xdg_activation_v1 is not available.");
        return activation.RequestActivationToken(_eventLoop.State, SurfaceHandle, Id);
    }

    public void SetTheme(Theme? theme)
    {
        lock (_lock)
        {
            _state.Theme = theme;
            _clientSideFrame?.SetTheme(theme);
        }
    }

    public void SetContentProtected(bool isProtected)
    {
        lock (_lock)
        {
            _state.ContentProtected = isProtected;
        }
    }

    public void SetCursor(Cursor cursor)
    {
        lock (_lock)
        {
            _state.Cursor = cursor;
        }

        _eventLoop.State.ForEachPointerForWindow(Id, pointer => pointer.ApplyCursor(cursor, CursorVisible));
    }

    public void SetCursorPosition(Position position)
    {
        lock (_lock)
        {
            if (_state.CursorGrabMode != CursorGrabMode.Locked)
            {
                throw new NotSupportedException("Wayland cursor position changes require a locked pointer.");
            }
        }

        LogicalPosition<double> logical = position.ToLogical<double>(ScaleFactor);
        if (!_eventLoop.State.ForEachPointerForWindow(Id, pointer =>
            pointer.SetLockedCursorPosition(logical.X, logical.Y)))
        {
            throw new NotSupportedException("Wayland cursor position changes require an active locked pointer.");
        }

        RequestRedraw();
    }

    public void SetCursorGrab(CursorGrabMode mode)
    {
        if (mode != CursorGrabMode.None && _eventLoop.State.PointerConstraints is null)
        {
            throw new NotSupportedException("zwp_pointer_constraints_v1 is not available.");
        }

        lock (_lock)
        {
            _state.CursorGrabMode = mode;
        }

        if (mode == CursorGrabMode.None)
        {
            _eventLoop.State.ReleasePointerGrabForSurface(SurfaceHandle);
            return;
        }

        _eventLoop.State.ForEachPointerForWindow(Id, pointer => pointer.SetCursorGrabMode(mode, SurfaceHandle));
    }

    public void SetCursorVisible(bool visible)
    {
        lock (_lock)
        {
            _state.CursorVisible = visible;
        }

        _eventLoop.State.ForEachPointerForWindow(Id, pointer => pointer.ApplyCursor(CurrentCursor, visible));
    }

    public void DragWindow()
    {
        if (!TryGetPointerRequestContext(out WlSeat seat, out uint serial))
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = seat.Value;
        args[1].Uint = serial;
        PInvoke.WlProxyMarshalArray(XdgToplevelHandle, XdgToplevelRequest.Move, args);
        _eventLoop.State.Connection.Flush();
    }

    public void DragResizeWindow(ResizeDirection direction)
    {
        if (!TryGetPointerRequestContext(out WlSeat seat, out uint serial))
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[3];
        args[0].Object = seat.Value;
        args[1].Uint = serial;
        args[2].Uint = (uint)ResizeDirectionToXdg(direction);
        PInvoke.WlProxyMarshalArray(XdgToplevelHandle, XdgToplevelRequest.Resize, args);
        _eventLoop.State.Connection.Flush();
    }

    public void ShowWindowMenu(Position position)
    {
        if (!TryGetPointerRequestContext(out WlSeat seat, out uint serial))
        {
            return;
        }

        LogicalPosition<int> logical = position.ToLogical<int>(ScaleFactor);
        WlArgument* args = stackalloc WlArgument[4];
        args[0].Object = seat.Value;
        args[1].Uint = serial;
        args[2].Int = logical.X;
        args[3].Int = logical.Y;
        PInvoke.WlProxyMarshalArray(XdgToplevelHandle, XdgToplevelRequest.ShowWindowMenu, args);
        _eventLoop.State.Connection.Flush();
    }

    public void SetCursorHittest(bool hittest)
    {
        if (SurfaceHandle.IsNull)
        {
            return;
        }

        if (hittest)
        {
            WlArgument* args = stackalloc WlArgument[1];
            args[0].Object = 0;
            PInvoke.WlProxyMarshalArray(SurfaceHandle, WlSurfaceRequest.SetInputRegion, args);
            Commit();
            return;
        }

        WlRegion region = _eventLoop.State.CreateRegion();
        try
        {
            WlArgument* addArgs = stackalloc WlArgument[4];
            addArgs[0].Int = 0;
            addArgs[1].Int = 0;
            addArgs[2].Int = 0;
            addArgs[3].Int = 0;
            PInvoke.WlProxyMarshalArray(region, WlRegionRequest.Add, addArgs);

            WlArgument* inputArgs = stackalloc WlArgument[1];
            inputArgs[0].Object = region.Value;
            PInvoke.WlProxyMarshalArray(SurfaceHandle, WlSurfaceRequest.SetInputRegion, inputArgs);
        }
        finally
        {
            DestroyRegion(region);
        }

        Commit();
    }

    public nint? XdgToplevel()
    {
        return XdgToplevelHandle.IsNull ? null : XdgToplevelHandle.Value;
    }

    public WaylandSurface WaylandSurface()
    {
        return new WaylandSurface(SurfaceHandle.Value);
    }

    public WaylandDisplay WaylandDisplay()
    {
        return new WaylandDisplay(_eventLoop.State.Connection.Display.Value);
    }

    public void Dispose()
    {
        DestroyFromEventLoop(sendDestroyedEvent: true);
    }

    internal void DestroyFromEventLoop(bool sendDestroyedEvent = false)
    {
        bool wasRegistered;
        lock (_lock)
        {
            if (_destroyed)
            {
                return;
            }

            _destroyed = true;
            wasRegistered = _eventLoop.State.Windows.ContainsKey(Id);
        }

        _eventLoop.RemoveWindow(Id);
        _eventLoop.State.ReleasePointerGrabForSurface(SurfaceHandle);

        _fractionalScaling?.Dispose();
        _fractionalScaling = null;

        _toplevelDecoration?.Dispose();
        _toplevelDecoration = null;

        _toplevelIcon?.Dispose();
        _toplevelIcon = null;

        _blurEffect?.Dispose();
        _blurEffect = null;

        _frameCallback?.Dispose();
        _frameCallback = null;

        _clientSideFrame?.Dispose();
        _clientSideFrame = null;

        _viewport?.Dispose();
        _viewport = null;

        if (!XdgToplevelHandle.IsNull)
        {
            DestroyXdgToplevel(XdgToplevelHandle);
            XdgToplevelHandle = Winit.Wayland.XdgToplevel.Null;
        }

        if (!XdgSurfaceHandle.IsNull)
        {
            DestroyXdgSurface(XdgSurfaceHandle);
            XdgSurfaceHandle = Winit.Wayland.XdgSurface.Null;
        }

        if (!SurfaceHandle.IsNull)
        {
            DestroySurface(SurfaceHandle);
            SurfaceHandle = WlSurface.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }

        if (sendDestroyedEvent && wasRegistered)
        {
            _eventLoop.QueueWindowEvent(Id, new WindowEvent(new WindowEvent.Destroyed()));
        }

        GC.SuppressFinalize(this);
    }

    internal bool AddSeatFocus(uint seatId)
    {
        lock (_lock)
        {
            return _state.AddSeatFocus(seatId);
        }
    }

    internal bool RemoveSeatFocus(uint seatId)
    {
        lock (_lock)
        {
            return _state.RemoveSeatFocus(seatId);
        }
    }

    internal bool TakeRedrawRequested()
    {
        lock (_lock)
        {
            if (_state.FrameCallbackState == FrameCallbackState.Requested)
            {
                return false;
            }

            _state.FrameCallbackReset();
            bool redrawRequested = _state.RedrawRequested;
            _state.RedrawRequested = false;
            return redrawRequested;
        }
    }

    internal void HandleDecorationConfigure(ZxdgToplevelDecorationV1Mode mode)
    {
        lock (_lock)
        {
            _state.ConfiguredDecorationMode = mode;
            UpdateClientSideFrameLocked();
            ReloadSizeHintsLocked();
            ReloadWindowGeometryLocked();
        }
    }

    private void UpdateClientSideFrameLocked()
    {
        bool useClientSideFrame = ShouldUseClientSideFrameLocked();
        if (!useClientSideFrame)
        {
            _clientSideFrame?.Dispose();
            _clientSideFrame = null;
            return;
        }

        if (_clientSideFrame is null && !_clientSideFrameFailed)
        {
            _clientSideFrame = ClientSideFrame.TryCreate(_eventLoop.State, this, SurfaceHandle);
            _clientSideFrameFailed = _clientSideFrame is null;
        }

        if (_clientSideFrame is null)
        {
            return;
        }

        _clientSideFrame.SetHidden(!_state.IsDecorated);
        _clientSideFrame.SetTitle(_state.Title);
        _clientSideFrame.SetTheme(_state.Theme);
        _clientSideFrame.SetResizable(_state.IsResizable);
        _clientSideFrame.SetCapabilities(CanMaximize, CanMinimize);
        _clientSideFrame.SetScalingFactor(_state.ScaleFactor);
        _clientSideFrame.Resize(_state.LogicalSurfaceSize);
    }

    private bool ShouldUseClientSideFrameLocked()
    {
        if (!_state.IsDecorated)
        {
            return false;
        }

        return _state.ConfiguredDecorationMode == ZxdgToplevelDecorationV1Mode.ClientSide ||
            (_toplevelDecoration is null && _state.ConfiguredDecorationMode != ZxdgToplevelDecorationV1Mode.ServerSide);
    }

    private void ReloadWindowGeometryLocked()
    {
        FrameGeometry geometry = _clientSideFrame?.Geometry(_state.LogicalSurfaceSize)
            ?? new FrameGeometry(
                0,
                0,
                checked((int)Math.Max(1, _state.LogicalSurfaceSize.Width)),
                checked((int)Math.Max(1, _state.LogicalSurfaceSize.Height)));
        SetWindowGeometry(geometry);
    }

    private bool TryGetPointerRequestContext(out WlSeat seat, out uint serial)
    {
        if (_eventLoop.State.TryGetPointerForWindow(Id, out WinitPointerData pointer))
        {
            serial = pointer.LatestButtonSerial;
            seat = pointer.Seat.Seat;
            return !seat.IsNull;
        }

        seat = WlSeat.Null;
        serial = 0;
        return false;
    }

    private void UpdateViewportDestination()
    {
        if (_viewport is null)
        {
            return;
        }

        LogicalSize<int> size = _state.LogicalSurfaceSize.Cast<int>();
        _viewport.SetDestination(Math.Max(1, size.Width), Math.Max(1, size.Height));
    }

    private void InstallDispatchers()
    {
        void* handle = (void*)GCHandle.ToIntPtr(_selfHandle);
        if (PInvoke.WlProxyAddDispatcher(SurfaceHandle, &SurfaceDispatcher, handle, null) != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_surface.");
        }

        if (PInvoke.WlProxyAddDispatcher(XdgSurfaceHandle, &XdgSurfaceDispatcher, handle, null) != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for xdg_surface.");
        }

        if (PInvoke.WlProxyAddDispatcher(XdgToplevelHandle, &XdgToplevelDispatcher, handle, null) != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for xdg_toplevel.");
        }
    }

    private void ApplyInitialAttributes(WindowAttributes attributes)
    {
        WindowAttributesWayland? wayland = attributes.Platform as WindowAttributesWayland;

        SetTitle(attributes.Title);
        if (wayland?.Name is { } name)
        {
            SendStringRequest(XdgToplevelHandle, XdgToplevelRequest.SetAppId, name.General);
        }

        if (wayland?.ActivationToken is { } activationToken)
        {
            _eventLoop.State.XdgActivation?.Activate(activationToken, SurfaceHandle);
        }

        SetMinSurfaceSize(attributes.MinSurfaceSize);
        SetMaxSurfaceSize(attributes.MaxSurfaceSize);
        SetSurfaceResizeIncrements(attributes.SurfaceResizeIncrements);
        SetResizable(attributes.Resizable);
        SetDecorations(attributes.Decorations);
        SetTransparent(attributes.Transparent);
        SetBlur(attributes.Blur);
        SetEnabledButtons(attributes.EnabledButtons);
        SetTheme(attributes.PreferredTheme);
        SetContentProtected(attributes.ContentProtected);
        SetWindowIcon(attributes.WindowIcon);
        SetCursor(attributes.Cursor);

        if (attributes.Fullscreen is not null)
        {
            SetFullscreen(attributes.Fullscreen);
        }
        else if (attributes.Maximized)
        {
            SetMaximized(true);
        }
    }

    private void Commit()
    {
        PInvoke.WlProxyMarshalArray(SurfaceHandle, WlSurfaceRequest.Commit, null);
        _eventLoop.State.Connection.Flush();
    }

    internal void FrameCallbackDone(FrameCallback callback)
    {
        bool wakeUp;
        lock (_lock)
        {
            if (!ReferenceEquals(_frameCallback, callback))
            {
                return;
            }

            _frameCallback = null;
            _state.FrameCallbackReceived();
            wakeUp = _state.RedrawRequested;
        }

        callback.Dispose();

        if (wakeUp)
        {
            _eventLoop.WakeUp();
        }
    }

    private static void DestroyRegion(WlRegion region)
    {
        if (region.IsNull)
        {
            return;
        }

        PInvoke.WlProxyMarshalArrayFlags(
            region,
            WlRegionRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(region),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    private static void DestroyXdgToplevel(XdgToplevel toplevel)
    {
        PInvoke.WlProxyMarshalArrayFlags(
            toplevel,
            XdgToplevelRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(toplevel),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    private static void DestroyXdgSurface(XdgSurface surface)
    {
        PInvoke.WlProxyMarshalArrayFlags(
            surface,
            XdgSurfaceRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(surface),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    private static void DestroySurface(WlSurface surface)
    {
        PInvoke.WlProxyMarshalArrayFlags(
            surface,
            WlSurfaceRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(surface),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    internal void MarkAttentionRequestFinished()
    {
        lock (_lock)
        {
            _attentionRequested = false;
        }
    }

    internal void TextInputEntered(WinitTextInput textInput)
    {
        lock (_lock)
        {
            if (!_textInputs.Contains(textInput))
            {
                _textInputs.Add(textInput);
            }
        }
    }

    internal void TextInputLeft(WinitTextInput textInput)
    {
        lock (_lock)
        {
            _textInputs.Remove(textInput);
        }
    }

    private LogicalSize<int> MinSurfaceSizeToLogical(Size? minSize)
    {
        LogicalSize<int> size = minSize?.ToLogical<int>(ScaleFactor) ?? new LogicalSize<int>(1, 1);
        size = new LogicalSize<int>(Math.Max(1, size.Width), Math.Max(1, size.Height));
        return WindowGeometrySizeForClientSize(size);
    }

    private LogicalSize<int> MaxSurfaceSizeToLogical(Size? maxSize)
    {
        if (maxSize is null)
        {
            return new LogicalSize<int>(0, 0);
        }

        LogicalSize<int> size = maxSize.Value.ToLogical<int>(ScaleFactor);
        size = new LogicalSize<int>(Math.Max(1, size.Width), Math.Max(1, size.Height));
        return WindowGeometrySizeForClientSize(size);
    }

    private LogicalSize<int> WindowGeometrySizeForClientSize(LogicalSize<int> clientSize)
    {
        if (_clientSideFrame is null || _clientSideFrame.IsHidden)
        {
            return clientSize;
        }

        LogicalSize<uint> outer = _clientSideFrame.OuterSize(new LogicalSize<uint>(
            checked((uint)Math.Max(1, clientSize.Width)),
            checked((uint)Math.Max(1, clientSize.Height))));
        return outer.Cast<int>();
    }

    private void ReloadSizeHintsLocked()
    {
        LogicalSize<int> minSize;
        LogicalSize<int> maxSize;
        if (_state.IsResizable)
        {
            minSize = MinSurfaceSizeToLogical(_state.MinSurfaceSize);
            maxSize = MaxSurfaceSizeToLogical(_state.MaxSurfaceSize);
        }
        else
        {
            minSize = WindowGeometrySizeForClientSize(_state.LogicalSurfaceSize.Cast<int>());
            maxSize = minSize;
        }

        SetMinSize(minSize.Width, minSize.Height);
        SetMaxSize(maxSize.Width, maxSize.Height);
    }

    private void SetMinSize(int width, int height)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Int = width;
        args[1].Int = height;
        PInvoke.WlProxyMarshalArray(XdgToplevelHandle, XdgToplevelRequest.SetMinSize, args);
    }

    private void SetMaxSize(int width, int height)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Int = width;
        args[1].Int = height;
        PInvoke.WlProxyMarshalArray(XdgToplevelHandle, XdgToplevelRequest.SetMaxSize, args);
    }

    private void SetWindowGeometry(FrameGeometry geometry)
    {
        if (XdgSurfaceHandle.IsNull)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[4];
        args[0].Int = geometry.X;
        args[1].Int = geometry.Y;
        args[2].Int = Math.Max(1, geometry.Width);
        args[3].Int = Math.Max(1, geometry.Height);
        PInvoke.WlProxyMarshalArray(XdgSurfaceHandle, XdgSurfaceRequest.SetWindowGeometry, args);
    }

    private void SendNoArgRequest(WlProxy proxy, uint opcode)
    {
        PInvoke.WlProxyMarshalArray(proxy, opcode, null);
    }

    private void SendStringRequest(WlProxy proxy, uint opcode, string value)
    {
        using Utf8Buffer buffer = Utf8Buffer.FromString(value);
        WlArgument* args = stackalloc WlArgument[1];
        args[0].String = buffer.Pointer;
        PInvoke.WlProxyMarshalArray(proxy, opcode, args);
    }

    private void HandleXdgSurfaceConfigure(uint serial)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Uint = serial;
        PInvoke.WlProxyMarshalArray(XdgSurfaceHandle, XdgSurfaceRequest.AckConfigure, args);

        bool sizeChanged;
        lock (_lock)
        {
            sizeChanged = _state.ApplyConfigure();
            if (sizeChanged)
            {
                UpdateViewportDestination();
            }

            UpdateClientSideFrameLocked();
            ReloadSizeHintsLocked();
            ReloadWindowGeometryLocked();
        }

        if (sizeChanged)
        {
            _eventLoop.State.QueueWindowResized(Id);
        }

        RequestRedraw();
        Commit();
    }

    private void HandleXdgToplevelConfigure(int width, int height, WlArray* statesArray)
    {
        ToplevelStateFlags stateFlags = ParseToplevelStates(statesArray);
        lock (_lock)
        {
            UpdateClientSideFrameLocked();
            if (_clientSideFrame is not null)
            {
                (width, height) = _clientSideFrame.ClientSizeFromOuterSize(width, height);
            }

            _state.SetPendingConfigure(width, height, stateFlags);
        }
    }

    private void HandleXdgToplevelConfigureBounds(int width, int height)
    {
        lock (_lock)
        {
            UpdateClientSideFrameLocked();
            if (_clientSideFrame is not null)
            {
                (width, height) = _clientSideFrame.ClientBoundsFromOuterBounds(width, height);
            }

            _state.SetPendingConfigureBounds(width, height);
        }
    }

    private void HandleXdgToplevelWmCapabilities(WlArray* capabilitiesArray)
    {
        ToplevelWmCapabilities capabilities = ToplevelWmCapabilities.None;
        if (capabilitiesArray is not null && capabilitiesArray->Data is not null)
        {
            ReadOnlySpan<uint> rawCapabilities = new(
                capabilitiesArray->Data,
                checked((int)(capabilitiesArray->Size / sizeof(uint))));
            foreach (uint rawCapability in rawCapabilities)
            {
                switch ((XdgToplevelWmCapability)rawCapability)
                {
                    case XdgToplevelWmCapability.WindowMenu:
                        capabilities |= ToplevelWmCapabilities.WindowMenu;
                        break;
                    case XdgToplevelWmCapability.Maximize:
                        capabilities |= ToplevelWmCapabilities.Maximize;
                        break;
                    case XdgToplevelWmCapability.Fullscreen:
                        capabilities |= ToplevelWmCapabilities.Fullscreen;
                        break;
                    case XdgToplevelWmCapability.Minimize:
                        capabilities |= ToplevelWmCapabilities.Minimize;
                        break;
                }
            }
        }

        lock (_lock)
        {
            _state.WmCapabilities = capabilities;
            _clientSideFrame?.SetCapabilities(CanMaximize, CanMinimize);
        }
    }

    private void HandleSurfaceEnter(WlOutput output)
    {
        if (_eventLoop.State.TryGetMonitor(output, out MonitorHandle monitor))
        {
            lock (_lock)
            {
                _state.SurfaceEntered(monitor);
            }

            HandleScaleFactorChanged(monitor.ScaleFactor, isLegacy: true);
        }
    }

    private void HandleSurfaceLeave(WlOutput output)
    {
        if (_eventLoop.State.TryGetMonitor(output, out MonitorHandle monitor))
        {
            HandleSurfaceLeave(monitor);
        }
    }

    internal void HandleSurfaceLeave(MonitorHandle monitor)
    {
        lock (_lock)
        {
            _state.SurfaceLeft(monitor);
        }
    }

    internal void HandleScaleFactorChanged(double scaleFactor, bool isLegacy)
    {
        if (isLegacy && _fractionalScaling is not null)
        {
            return;
        }

        bool changed;
        lock (_lock)
        {
            changed = _state.SetScaleFactor(scaleFactor);
            if (changed)
            {
                UpdateViewportDestination();
                _clientSideFrame?.SetScalingFactor(scaleFactor);
                UpdateClientSideFrameLocked();
                ReloadSizeHintsLocked();
                ReloadWindowGeometryLocked();
            }
        }

        if (!changed)
        {
            return;
        }

        if (_fractionalScaling is null)
        {
            WlArgument* args = stackalloc WlArgument[1];
            args[0].Int = checked((int)Math.Max(1, Math.Round(scaleFactor)));
            PInvoke.WlProxyMarshalArray(SurfaceHandle, WlSurfaceRequest.SetBufferScale, args);
        }

        _eventLoop.State.QueueScaleFactorChanged(Id);
    }

    private void HandlePreferredBufferTransform(WlOutputTransform transform)
    {
        lock (_lock)
        {
            _ = _state.SetPreferredBufferTransform(transform);
        }
    }

    private static ToplevelStateFlags ParseToplevelStates(WlArray* statesArray)
    {
        ToplevelStateFlags flags = ToplevelStateFlags.None;

        if (statesArray is not null && statesArray->Data is not null)
        {
            ReadOnlySpan<uint> states = new(statesArray->Data, checked((int)(statesArray->Size / sizeof(uint))));
            foreach (uint rawState in states)
            {
                switch ((XdgToplevelState)rawState)
                {
                    case XdgToplevelState.Maximized:
                        flags |= ToplevelStateFlags.Maximized;
                        break;
                    case XdgToplevelState.Fullscreen:
                        flags |= ToplevelStateFlags.Fullscreen;
                        break;
                    case XdgToplevelState.Resizing:
                        flags |= ToplevelStateFlags.Resizing;
                        break;
                    case XdgToplevelState.Activated:
                        flags |= ToplevelStateFlags.Activated;
                        break;
                    case XdgToplevelState.Suspended:
                        flags |= ToplevelStateFlags.Suspended;
                        break;
                    case XdgToplevelState.TiledLeft:
                    case XdgToplevelState.TiledRight:
                    case XdgToplevelState.TiledTop:
                    case XdgToplevelState.TiledBottom:
                    case XdgToplevelState.ConstrainedLeft:
                    case XdgToplevelState.ConstrainedRight:
                    case XdgToplevelState.ConstrainedTop:
                    case XdgToplevelState.ConstrainedBottom:
                        flags |= ToplevelStateFlags.Tiled;
                        break;
                }
            }
        }

        return flags;
    }

    private static XdgToplevelResizeEdge ResizeDirectionToXdg(ResizeDirection direction)
    {
        return direction switch
        {
            ResizeDirection.East => XdgToplevelResizeEdge.Right,
            ResizeDirection.North => XdgToplevelResizeEdge.Top,
            ResizeDirection.NorthEast => XdgToplevelResizeEdge.TopRight,
            ResizeDirection.NorthWest => XdgToplevelResizeEdge.TopLeft,
            ResizeDirection.South => XdgToplevelResizeEdge.Bottom,
            ResizeDirection.SouthEast => XdgToplevelResizeEdge.BottomRight,
            ResizeDirection.SouthWest => XdgToplevelResizeEdge.BottomLeft,
            ResizeDirection.West => XdgToplevelResizeEdge.Left,
            _ => XdgToplevelResizeEdge.None,
        };
    }

    private static WlOutput FullscreenOutput(Fullscreen fullscreen)
    {
        if (fullscreen.TryGetValue(out Fullscreen.Borderless borderless) &&
            borderless.Monitor?.Provider.AsAny() is MonitorHandle monitor)
        {
            return monitor.Output;
        }

        return WlOutput.Null;
    }

    private ZxdgToplevelDecorationV1Mode DecorationModeFor(bool decorations)
    {
        return decorations && !_preferCsd
            ? ZxdgToplevelDecorationV1Mode.ServerSide
            : ZxdgToplevelDecorationV1Mode.ClientSide;
    }

    private static WlSurface CreateSurface(WinitState state)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            state.Compositor,
            WlCompositorRequest.CreateSurface,
            WlCoreInterfaces.Surface,
            PInvoke.WlProxyGetVersion(state.Compositor),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_compositor.create_surface failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new WlSurface(proxy.Value);
    }

    private static XdgSurface CreateXdgSurface(WinitState state, WlSurface surface)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = surface.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            state.XdgWmBase,
            XdgWmBaseRequest.GetXdgSurface,
            XdgInterfaces.Surface,
            PInvoke.WlProxyGetVersion(state.XdgWmBase),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("xdg_wm_base.get_xdg_surface failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new XdgSurface(proxy.Value);
    }

    private static XdgToplevel CreateXdgToplevel(XdgSurface surface)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            surface,
            XdgSurfaceRequest.GetToplevel,
            XdgInterfaces.Toplevel,
            PInvoke.WlProxyGetVersion(surface),
            WlProxyMarshalFlags.None,
            args);
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("xdg_surface.get_toplevel failed.");
        }

        return new XdgToplevel(proxy.Value);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int SurfaceDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not Window window || window._destroyed)
        {
            return 0;
        }

        switch (opcode)
        {
            case WlSurfaceEvent.Enter when args is not null:
                window.HandleSurfaceEnter(new WlOutput(args[0].Object));
                break;
            case WlSurfaceEvent.Leave when args is not null:
                window.HandleSurfaceLeave(new WlOutput(args[0].Object));
                break;
            case WlSurfaceEvent.PreferredBufferScale when args is not null:
                window.HandleScaleFactorChanged(args[0].Int, isLegacy: true);
                break;
            case WlSurfaceEvent.PreferredBufferTransform when args is not null:
                window.HandlePreferredBufferTransform((WlOutputTransform)args[0].Uint);
                break;
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int XdgSurfaceDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null || args is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is Window window &&
            !window._destroyed &&
            opcode == XdgSurfaceEvent.Configure)
        {
            window.HandleXdgSurfaceConfigure(args[0].Uint);
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int XdgToplevelDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not Window window || window._destroyed)
        {
            return 0;
        }

        switch (opcode)
        {
            case XdgToplevelEvent.Configure when args is not null:
                window.HandleXdgToplevelConfigure(args[0].Int, args[1].Int, args[2].Array);
                break;
            case XdgToplevelEvent.Close:
                window._eventLoop.State.QueueClose(window.Id);
                break;
            case XdgToplevelEvent.ConfigureBounds when args is not null:
                window.HandleXdgToplevelConfigureBounds(args[0].Int, args[1].Int);
                break;
            case XdgToplevelEvent.WmCapabilities when args is not null:
                window.HandleXdgToplevelWmCapabilities(args[0].Array);
                break;
        }

        return 0;
    }
}

internal sealed unsafe class FrameCallback : IDisposable
{
    private readonly Window _window;
    private readonly GCHandle _selfHandle;
    private WlCallback _callback;
    private bool _disposed;

    private FrameCallback(Window window, WlCallback callback)
    {
        _window = window;
        _callback = callback;
        _selfHandle = GCHandle.Alloc(this);
    }

    public static FrameCallback Create(WinitState state, WlSurface surface, Window window)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            surface,
            WlSurfaceRequest.Frame,
            WlCoreInterfaces.Callback,
            PInvoke.WlProxyGetVersion(surface),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_surface.frame failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        FrameCallback callback = new(window, new WlCallback(proxy.Value));
        callback.InstallDispatcher();
        return callback;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_callback.IsNull)
        {
            PInvoke.WlProxyDestroy(_callback);
            _callback = WlCallback.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _callback,
            &FrameCallbackDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            Dispose();
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_callback.");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int FrameCallbackDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;
        _ = args;

        if (implementation is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not FrameCallback callback ||
            callback._disposed)
        {
            return 0;
        }

        if (opcode == WlCallbackEvent.Done)
        {
            callback._window.FrameCallbackDone(callback);
        }

        return 0;
    }
}
