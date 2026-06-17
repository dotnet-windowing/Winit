using System.Text;
using Winit.Common.Xkb;
using Winit.Core;
using Winit.Dpi;
using Winit.X11.Util;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.X11;

public sealed unsafe class Window : IWindow, IWindowExtX11
{
    private readonly EventLoop _eventLoop;
    private readonly XConnection _xconn;
    private readonly XlibWindow _window;
    private readonly nuint? _visualId;
    private PhysicalSize<uint> _surfaceSize;
    private Size? _minSurfaceSize;
    private Size? _maxSurfaceSize;
    private Size? _surfaceResizeIncrements;
    private Size? _baseSize;
    private bool _visible;
    private bool _resizable;
    private bool _maximized;
    private bool _minimized;
    private Fullscreen? _fullscreen;
    private bool _decorations;
    private WindowLevel _windowLevel;
    private Theme? _theme;
    private Cursor _selectedCursor;
    private bool _cursorVisible = true;
    private CursorGrabMode _cursorGrabMode = CursorGrabMode.None;
    private PhysicalPosition<int> _surfacePosition;
    private PhysicalPosition<int>? _surfacePositionRelativeParent;
    private readonly PhysicalPosition<int>? _initialRequestedPosition;
    private PhysicalPosition<int>? _xMoveOffset;
    private PhysicalPosition<int>? _lastXMovePosition;
    private bool _hasFocus;
    private double _scaleFactor;
    private FrameExtentsHeuristic? _frameExtents;
    private PhysicalPosition<int>? _restorePosition;
    private (uint MonitorId, uint ModeId)? _desktopVideoMode;
    private ImeCapabilities? _imeCapabilities;
    private (Position Position, Size Size)? _imeCursorArea;
    private (ImeHint Hint, ImePurpose Purpose)? _imeHintAndPurpose;
    private ImeSurroundingText? _imeSurroundingText;
    private nuint? _syncCounterId;

    private Window(
        EventLoop eventLoop,
        XlibWindow window,
        WindowAttributes attributes,
        PhysicalSize<uint> surfaceSize,
        double scaleFactor)
    {
        WindowAttributesX11? x11 = attributes.Platform as WindowAttributesX11;
        _eventLoop = eventLoop;
        _xconn = eventLoop.XConnection;
        _window = window;
        _visualId = x11?.VisualId?.Value;
        _surfaceSize = surfaceSize;
        _minSurfaceSize = attributes.MinSurfaceSize;
        _maxSurfaceSize = attributes.MaxSurfaceSize;
        _surfaceResizeIncrements = attributes.SurfaceResizeIncrements;
        _baseSize = x11?.BaseSize;
        _visible = attributes.Visible;
        _resizable = attributes.Resizable;
        _maximized = attributes.Maximized;
        _fullscreen = attributes.Fullscreen;
        _decorations = attributes.Decorations;
        _windowLevel = attributes.WindowLevel;
        _theme = attributes.PreferredTheme;
        _selectedCursor = attributes.Cursor;
        _initialRequestedPosition = attributes.Position?.ToPhysical<int>(scaleFactor);
        _surfacePosition = _initialRequestedPosition ?? new PhysicalPosition<int>(0, 0);
        _scaleFactor = scaleFactor;
        Id = WindowId.FromRaw(window.Value);
        Title = attributes.Title;
    }

    ~Window()
    {
        _ = PInvoke.XDestroyWindow(_xconn.Display, _window);
    }

    public WindowId Id { get; }

    public double ScaleFactor => _scaleFactor;

    public PhysicalPosition<int> SurfacePosition => CachedFrameExtents().SurfacePosition();

    public PhysicalPosition<int> OuterPosition => OuterPositionPhysical();

    public PhysicalSize<uint> SurfaceSize => _surfaceSize;

    public PhysicalSize<uint> OuterSize => CachedFrameExtents().SurfaceSizeToOuter(_surfaceSize);

    public PhysicalInsets<uint> SafeArea => new(0, 0, 0, 0);

    public PhysicalSize<uint>? SurfaceResizeIncrements => _surfaceResizeIncrements?.ToPhysical<uint>(ScaleFactor);

    public bool? IsVisible => _visible;

    public bool IsResizable => _resizable;

    public WindowButtons EnabledButtons => WindowButtons.All;

    public bool? IsMinimized => NetWmStateContains(AtomName.NetWmStateHidden, _minimized);

    public bool IsMaximized =>
        NetWmStateContains(AtomName.NetWmStateMaximizedHorz, _maximized) &&
        NetWmStateContains(AtomName.NetWmStateMaximizedVert, _maximized);

    public Fullscreen? Fullscreen => _fullscreen;

    public bool IsDecorated => _decorations;

    public ImeCapabilities? ImeCapabilities => _imeCapabilities;

    public bool HasFocus => _hasFocus;

    public Theme? Theme => _theme;

    public string Title { get; private set; }

    public CoreMonitorHandle? CurrentMonitor => Monitor.CurrentMonitor(_xconn, OuterPosition, OuterSize);

    public IEnumerable<CoreMonitorHandle> AvailableMonitors => Monitor.AvailableMonitors(_xconn);

    public CoreMonitorHandle? PrimaryMonitor => Monitor.PrimaryMonitor(_xconn);

    public object? DisplayHandle => RawDisplayHandle.FromXlib(_xconn.Display, _xconn.DefaultScreen);

    public object? WindowHandle => RawWindowHandle.FromXlib(_window.Value, _visualId);

    internal XlibWindow XWindow => _window;

    internal nuint? SyncCounterId => _syncCounterId;

    internal static Window Create(EventLoop eventLoop, WindowAttributes attributes)
    {
        XConnection xconn = eventLoop.XConnection;
        WindowAttributesX11? x11 = attributes.Platform as WindowAttributesX11;
        Size logicalDefaultSize = new(new LogicalSize<double>(800.0, 600.0));
        PhysicalSize<uint> preliminarySize = (attributes.SurfaceSize ?? logicalDefaultSize).ToPhysical<uint>(1.0);
        PhysicalPosition<int> preliminaryPosition = attributes.Position?.ToPhysical<int>(1.0) ?? new PhysicalPosition<int>(0, 0);
        double scaleFactor = Monitor.ScaleFactorForWindow(xconn, preliminaryPosition, preliminarySize) ?? 1.0;
        PhysicalSize<uint> size = (attributes.SurfaceSize ?? logicalDefaultSize).ToPhysical<uint>(scaleFactor);
        PhysicalPosition<int> position = attributes.Position?.ToPhysical<int>(scaleFactor) ?? preliminaryPosition;
        nuint border = PInvoke.XBlackPixel(xconn.Display, xconn.DefaultScreen);
        nuint background = attributes.Transparent
            ? 0
            : PInvoke.XWhitePixel(xconn.Display, xconn.DefaultScreen);
        XlibWindow parent = x11?.EmbedWindow is { } embedWindow
            ? new XlibWindow(embedWindow.Value)
            : eventLoop.RootWindow;

        XlibWindow window = PInvoke.XCreateSimpleWindow(
            xconn.Display,
            parent.Value,
            position.X,
            position.Y,
            Math.Max(1, size.Width),
            Math.Max(1, size.Height),
            0,
            border,
            background);

        if (window.Value == 0)
        {
            throw new InvalidOperationException("XCreateSimpleWindow returned 0.");
        }

        nint eventMask =
            PInvoke.ExposureMask |
            PInvoke.StructureNotifyMask |
            PInvoke.VisibilityChangeMask |
            PInvoke.PropertyChangeMask |
            PInvoke.FocusChangeMask |
            PInvoke.KeyPressMask |
            PInvoke.KeyReleaseMask |
            PInvoke.ButtonPressMask |
            PInvoke.ButtonReleaseMask |
            PInvoke.PointerMotionMask |
            PInvoke.EnterWindowMask |
            PInvoke.LeaveWindowMask;

        _ = PInvoke.XSelectInput(xconn.Display, window, eventMask);
        SelectXInput2Events(xconn, window);
        StoreName(xconn, window, attributes.Title);
        SetDndAware(xconn, window);
        SetWmClass(xconn, window, attributes, x11);
        SetPid(xconn, window);
        SetWindowTypes(xconn, window, x11?.WindowTypes ?? [WindowType.Normal]);
        SetWmProtocols(xconn, window);
        nuint? syncCounterId = CreateSyncRequestCounter(xconn, window);
        if (x11?.ActivationToken is { } activationToken)
        {
            xconn.RemoveActivationToken(window, activationToken.AsRaw());
        }

        if (x11?.OverrideRedirect == true)
        {
            SetOverrideRedirect(xconn, window, true);
        }

        if (x11?.EmbedWindow is not null)
        {
            SetEmbedInfo(xconn, window);
        }

        Window result = new(eventLoop, window, attributes, size, scaleFactor);
        result._syncCounterId = syncCounterId;
        eventLoop.RegisterWindow(result);
        eventLoop.CreateImeContext(result, withIme: false);

        result.SetDecorations(attributes.Decorations);
        result.SetResizable(attributes.Resizable);
        result.SetTheme(attributes.PreferredTheme);
        result.SetCursor(attributes.Cursor);
        result.SetWindowIcon(attributes.WindowIcon);

        if (attributes.Visible)
        {
            _ = PInvoke.XMapWindow(xconn.Display, window);
            _ = PInvoke.XRaiseWindow(xconn.Display, window);
            if (attributes.Active)
            {
                result.FocusWindow();
            }
        }

        int detectableAutoRepeatSupported = 0;
        _ = PInvoke.XkbSetDetectableAutoRepeat(xconn.Display, 1, &detectableAutoRepeatSupported);

        if (attributes.WindowLevel != WindowLevel.Normal)
        {
            result.SetWindowLevel(attributes.WindowLevel);
        }

        if (attributes.Maximized)
        {
            result.SetMaximized(true);
        }

        if (attributes.Fullscreen is not null)
        {
            result.SetFullscreen(attributes.Fullscreen);
        }

        xconn.Flush();
        return result;
    }

    public void RequestRedraw()
    {
        _ = PInvoke.XClearArea(_xconn.Display, _window, 0, 0, 0, 0, exposures: 1);
        _xconn.Flush();
    }

    public void PrePresentNotify()
    {
    }

    public void ResetDeadKeys()
    {
        Xkb.ResetDeadKeys();
    }

    public PhysicalSize<uint>? RequestSurfaceSize(Size size)
    {
        RequestSurfaceSizePhysical(size.ToPhysical<uint>(ScaleFactor));
        if (!_resizable)
        {
            UpdateNormalHints();
        }

        _xconn.Flush();
        return _surfaceSize;
    }

    public void SetOuterPosition(Position position)
    {
        PhysicalPosition<int> outerPosition = position.ToPhysical<int>(ScaleFactor);
        PhysicalPosition<int> surfacePosition = CachedFrameExtents().SurfacePosition();
        _surfacePosition = new PhysicalPosition<int>(
            outerPosition.X + surfacePosition.X,
            outerPosition.Y + surfacePosition.Y);

        PhysicalPosition<int> xMovePosition = XMovePositionForOuterPosition(outerPosition);
        _lastXMovePosition = xMovePosition;
        _ = PInvoke.XMoveWindow(_xconn.Display, _window, xMovePosition.X, xMovePosition.Y);
        _xconn.Flush();
    }

    public void SetMinSurfaceSize(Size? minSize)
    {
        _minSurfaceSize = minSize;
        UpdateNormalHints();
        _xconn.Flush();
    }

    public void SetMaxSurfaceSize(Size? maxSize)
    {
        _maxSurfaceSize = maxSize;
        UpdateNormalHints();
        _xconn.Flush();
    }

    public void SetSurfaceResizeIncrements(Size? increments)
    {
        _surfaceResizeIncrements = increments;
        UpdateNormalHints();
        _xconn.Flush();
    }

    public void SetTitle(string title)
    {
        Title = title;
        StoreName(_xconn, _window, title);
        _xconn.Flush();
    }

    public void SetTransparent(bool transparent)
    {
    }

    public void SetBlur(bool blur)
    {
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;

        if (visible)
        {
            _ = PInvoke.XMapWindow(_xconn.Display, _window);
            _ = PInvoke.XRaiseWindow(_xconn.Display, _window);
        }
        else
        {
            _ = PInvoke.XUnmapWindow(_xconn.Display, _window);
        }

        _xconn.Flush();
    }

    public void SetResizable(bool resizable)
    {
        if (!resizable && Wm.WmNameIsOneOf("Xfwm4"))
        {
            return;
        }

        _resizable = resizable;
        SetMaximizable(resizable);
        UpdateNormalHints();
        _xconn.Flush();
    }

    public void SetEnabledButtons(WindowButtons buttons)
    {
    }

    public void SetMinimized(bool minimized)
    {
        _minimized = minimized;

        if (minimized)
        {
            _xconn.SendClientMessage(
                _window,
                _xconn.RootWindow,
                _xconn.Atoms[AtomName.WmChangeState],
                PInvoke.SubstructureRedirectMask | PInvoke.SubstructureNotifyMask,
                [PInvoke.IconicState, 0, 0, 0, 0]);
        }
        else
        {
            _ = PInvoke.XMapWindow(_xconn.Display, _window);
            _xconn.SendClientMessage(
                _window,
                _xconn.RootWindow,
                _xconn.Atoms[AtomName.NetActiveWindow],
                PInvoke.SubstructureRedirectMask | PInvoke.SubstructureNotifyMask,
                [1, 0, 0, 0, 0]);
        }

        _xconn.Flush();
    }

    public void SetMaximized(bool maximized)
    {
        _maximized = maximized;
        SetNetWm(
            maximized.ToStateOperation(),
            _xconn.Atoms[AtomName.NetWmStateMaximizedHorz],
            _xconn.Atoms[AtomName.NetWmStateMaximizedVert]);
        _xconn.Flush();
    }

    public void SetFullscreen(Fullscreen? fullscreen)
    {
        Fullscreen? oldFullscreen = _fullscreen;
        if (EqualityComparer<Fullscreen?>.Default.Equals(oldFullscreen, fullscreen))
        {
            return;
        }

        HandleExclusiveModeTransition(oldFullscreen, fullscreen);
        _fullscreen = fullscreen;
        InvalidateCachedFrameExtents();

        if (fullscreen is null)
        {
            SetNetWm(false.ToStateOperation(), _xconn.Atoms[AtomName.NetWmStateFullscreen]);
            if (_restorePosition is { } restorePosition)
            {
                _restorePosition = null;
                SetSurfacePositionPhysical(restorePosition);
            }

            _xconn.Flush();
            return;
        }

        FullscreenTarget? target = ResolveFullscreenTarget(fullscreen.Value);
        if (target is { NativeMode: { } nativeMode })
        {
            _ = _xconn.SetCrtcConfig(checked((uint)target.Monitor.NativeId), nativeMode);
        }

        if (target is { Monitor.Position: { } position })
        {
            _restorePosition ??= OuterPosition;
            SetSurfacePositionPhysical(position);
        }

        SetNetWm(true.ToStateOperation(), _xconn.Atoms[AtomName.NetWmStateFullscreen]);

        FocusWindow();

        _xconn.Flush();
    }

    private void HandleExclusiveModeTransition(Fullscreen? oldFullscreen, Fullscreen? newFullscreen)
    {
        bool oldExclusive = oldFullscreen.HasValue && oldFullscreen.Value.TryGetValue(out Fullscreen.Exclusive _);
        bool newExclusive = false;
        Fullscreen.Exclusive newExclusiveValue = default;
        if (newFullscreen.HasValue && newFullscreen.Value.TryGetValue(out Fullscreen.Exclusive exclusive))
        {
            newExclusive = true;
            newExclusiveValue = exclusive;
        }

        if (!oldExclusive && newExclusive)
        {
            if (newExclusiveValue.Monitor.Provider.AsAny() is MonitorHandle monitor &&
                _xconn.GetCrtcMode(checked((uint)monitor.NativeId)) is { } desktopMode)
            {
                _desktopVideoMode = (checked((uint)monitor.NativeId), desktopMode);
            }
        }
        else if (oldExclusive && !newExclusive && _desktopVideoMode is { } desktopVideoMode)
        {
            _ = _xconn.SetCrtcConfig(desktopVideoMode.MonitorId, desktopVideoMode.ModeId);
            _desktopVideoMode = null;
        }
    }

    private FullscreenTarget? ResolveFullscreenTarget(Fullscreen fullscreen)
    {
        if (fullscreen.TryGetValue(out Fullscreen.Exclusive exclusive))
        {
            if (exclusive.Monitor.Provider.AsAny() is not MonitorHandle monitor)
            {
                return null;
            }

            uint? nativeMode = monitor.ModeHandles
                .FirstOrDefault(mode => mode.Mode.Equals(exclusive.VideoMode))
                ?.NativeMode;
            return new FullscreenTarget(monitor, nativeMode);
        }

        if (fullscreen.TryGetValue(out Fullscreen.Borderless borderless))
        {
            CoreMonitorHandle? coreMonitor = borderless.Monitor ?? CurrentMonitor ?? PrimaryMonitor;
            return coreMonitor?.Provider.AsAny() is MonitorHandle monitor
                ? new FullscreenTarget(monitor, null)
                : null;
        }

        return null;
    }

    private void SetSurfacePositionPhysical(PhysicalPosition<int> position)
    {
        _surfacePosition = position;
        _ = PInvoke.XMoveWindow(_xconn.Display, _window, position.X, position.Y);
    }

    public void SetDecorations(bool decorations)
    {
        _decorations = decorations;
        InvalidateCachedFrameExtents();
        MotifHints hints = _xconn.GetMotifHints(_window);
        hints.SetDecorations(decorations);
        _xconn.SetMotifHints(_window, hints);
        _xconn.Flush();
    }

    public void SetWindowLevel(WindowLevel level)
    {
        _windowLevel = level;
        ToggleAtom(AtomName.NetWmStateAbove, level == WindowLevel.AlwaysOnTop);
        ToggleAtom(AtomName.NetWmStateBelow, level == WindowLevel.AlwaysOnBottom);
        _xconn.Flush();
    }

    public void SetWindowIcon(Winit.Core.Icon? windowIcon)
    {
        Atom iconAtom = _xconn.Atoms[AtomName.NetWmIcon];
        if (windowIcon is null)
        {
            _xconn.ChangeProperty32(_window, iconAtom, new Atom(PInvoke.XaCardinal), []);
            _xconn.Flush();
            return;
        }

        if (windowIcon.Provider.AsAny() is not RgbaIcon rgbaIcon)
        {
            throw new InvalidOperationException("X11 only supports RGBA window icons currently.");
        }

        _xconn.ChangeProperty32(
            _window,
            iconAtom,
            new Atom(PInvoke.XaCardinal),
            IconUtil.RgbaToCardinals(rgbaIcon));
        _xconn.Flush();
    }

    public void RequestImeUpdate(ImeRequest request)
    {
        if (request.TryGetValue(out ImeRequest.Enable enable))
        {
            if (_imeCapabilities is not null)
            {
                throw new ImeRequestException(ImeRequestError.AlreadyEnabled);
            }

            ImeCapabilities capabilities = enable.Value.Capabilities;
            _imeCapabilities = capabilities;
            _eventLoop.SetImeAllowed(this, true);
            ApplyImeRequestData(capabilities, enable.Value.RequestData);
            return;
        }

        if (request.TryGetValue(out ImeRequest.Update update))
        {
            if (_imeCapabilities is not { } capabilities)
            {
                throw new ImeRequestException(ImeRequestError.NotEnabled);
            }

            ApplyImeRequestData(capabilities, update.Value);
            return;
        }

        if (request.TryGetValue(out ImeRequest.Disable _))
        {
            _imeCapabilities = null;
            _imeCursorArea = null;
            _imeHintAndPurpose = null;
            _imeSurroundingText = null;
            _eventLoop.SetImeAllowed(this, false);
        }
    }

    public void FocusWindow()
    {
        if (!IsVisibleAndNotMinimized())
        {
            return;
        }

        _xconn.SendClientMessage(
            _window,
            _xconn.RootWindow,
            _xconn.Atoms[AtomName.NetActiveWindow],
            PInvoke.SubstructureRedirectMask | PInvoke.SubstructureNotifyMask,
            [1, 0, 0, 0, 0]);
        _xconn.Flush();
    }

    public void RequestUserAttention(UserAttentionType? requestType)
    {
        SetUrgency(requestType is not null);
        _xconn.Flush();
    }

    public AsyncRequestSerial RequestActivationToken()
    {
        AsyncRequestSerial serial = AsyncRequestSerial.Get();
        _eventLoop.QueueActivationRequest(this, serial);
        return serial;
    }

    public void SetTheme(Theme? theme)
    {
        _theme = theme;
        string variant = theme switch
        {
            Winit.Core.Theme.Light => "light",
            Winit.Core.Theme.Dark => "dark",
            null => "dark",
            _ => "dark",
        };

        byte[] value = Encoding.UTF8.GetBytes(variant);
        _xconn.ChangePropertyBytes(
            _window,
            _xconn.Atoms[AtomName.GtkThemeVariant],
            _xconn.Atoms[AtomName.Utf8String],
            value);
        _xconn.Flush();
    }

    public void SetContentProtected(bool isProtected)
    {
    }

    public void SetCursor(Cursor cursor)
    {
        _selectedCursor = cursor;
        if (_cursorVisible)
        {
            ApplyCursor(cursor);
        }
    }

    public void SetCursorPosition(Position position)
    {
        PhysicalPosition<int> physical = position.ToPhysical<int>(ScaleFactor);
        _ = PInvoke.XWarpPointer(
            _xconn.Display,
            default,
            _window,
            0,
            0,
            0,
            0,
            physical.X,
            physical.Y);
        _xconn.Flush();
    }

    public void SetCursorGrab(CursorGrabMode mode)
    {
        if (mode == CursorGrabMode.Locked)
        {
            throw new NotSupportedException("Locked cursor is not implemented on X11.");
        }

        if (mode == _cursorGrabMode)
        {
            return;
        }

        _ = PInvoke.XUngrabPointer(_xconn.Display, PInvoke.CurrentTime);
        _cursorGrabMode = CursorGrabMode.None;

        if (mode == CursorGrabMode.Confined)
        {
            uint eventMask = checked((uint)(
                PInvoke.ButtonPressMask |
                PInvoke.ButtonReleaseMask |
                PInvoke.EnterWindowMask |
                PInvoke.LeaveWindowMask |
                PInvoke.PointerMotionMask |
                PInvoke.PointerMotionHintMask |
                PInvoke.Button1MotionMask |
                PInvoke.Button2MotionMask |
                PInvoke.Button3MotionMask |
                PInvoke.Button4MotionMask |
                PInvoke.Button5MotionMask |
                PInvoke.KeymapStateMask));

            int status = PInvoke.XGrabPointer(
                _xconn.Display,
                _window,
                ownerEvents: 1,
                eventMask,
                PInvoke.GrabModeAsync,
                PInvoke.GrabModeAsync,
                _window,
                cursor: 0,
                PInvoke.CurrentTime);
            if (status != PInvoke.GrabSuccess)
            {
                throw new InvalidOperationException($"XGrabPointer failed with status {status}.");
            }
        }

        _cursorGrabMode = mode;
        _xconn.Flush();
    }

    public void SetCursorVisible(bool visible)
    {
        if (_cursorVisible == visible)
        {
            return;
        }

        _cursorVisible = visible;
        if (visible)
        {
            ApplyCursor(_selectedCursor);
        }
        else
        {
            _xconn.SetCursorIcon(_window, null);
        }
    }

    public void DragWindow()
    {
        DragInitiate(Wm.MoveResizeMove);
    }

    public void DragResizeWindow(ResizeDirection direction)
    {
        long action = direction switch
        {
            ResizeDirection.East => Wm.MoveResizeRight,
            ResizeDirection.North => Wm.MoveResizeTop,
            ResizeDirection.NorthEast => Wm.MoveResizeTopRight,
            ResizeDirection.NorthWest => Wm.MoveResizeTopLeft,
            ResizeDirection.South => Wm.MoveResizeBottom,
            ResizeDirection.SouthEast => Wm.MoveResizeBottomRight,
            ResizeDirection.SouthWest => Wm.MoveResizeBottomLeft,
            ResizeDirection.West => Wm.MoveResizeLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
        DragInitiate(action);
    }

    public void ShowWindowMenu(Position position)
    {
    }

    public void SetCursorHittest(bool hittest)
    {
    }

    internal bool UpdateSurfaceSize(PhysicalSize<uint> size)
    {
        if (_surfaceSize == size)
        {
            return false;
        }

        _surfaceSize = size;
        return true;
    }

    internal bool UpdateSyntheticSurfacePosition(
        PhysicalPosition<int> position,
        out PhysicalPosition<int> outerPosition)
    {
        outerPosition = CachedFrameExtents().InnerPositionToOuter(position.X, position.Y);
        CalibrateXMoveOffset(outerPosition);

        if (_surfacePosition == position)
        {
            return false;
        }

        _surfacePosition = position;
        return true;
    }

    internal void UpdateSurfacePositionRelativeParent(PhysicalPosition<int> position)
    {
        if (_surfacePositionRelativeParent == position)
        {
            return;
        }

        _surfacePositionRelativeParent = position;
        InvalidateCachedFrameExtents();
    }

    internal PhysicalPosition<int> InnerPositionPhysical()
    {
        return _xconn.TryTranslateCoordinatesRoot(_window, out TranslateCoordinatesResult coordinates)
            ? coordinates.Destination
            : _surfacePosition;
    }

    internal void InvalidateCachedFrameExtents()
    {
        _frameExtents = null;
    }

    internal bool UpdateFocus(bool hasFocus)
    {
        if (_hasFocus == hasFocus)
        {
            return false;
        }

        _hasFocus = hasFocus;
        return true;
    }

    internal void RefreshDpi(IApplicationHandler app, EventLoop eventLoop)
    {
        double newScaleFactor = Monitor.ScaleFactorForWindow(_xconn, OuterPosition, OuterSize) ?? _scaleFactor;
        double oldScaleFactor = _scaleFactor;
        if (oldScaleFactor.Equals(newScaleFactor))
        {
            return;
        }

        PhysicalSize<uint> oldSurfaceSize = _surfaceSize;
        PhysicalSize<uint> newSurfaceSize = oldSurfaceSize
            .ToLogical<double>(oldScaleFactor)
            .ToPhysical<uint>(newScaleFactor);

        _scaleFactor = newScaleFactor;
        UpdateNormalHints();

        SurfaceSizeState state = new(newSurfaceSize);
        app.WindowEvent(
            eventLoop,
            Id,
            new WindowEvent(new WindowEvent.ScaleFactorChanged(
                newScaleFactor,
                SurfaceSizeWriter.Create(state))));

        PhysicalSize<uint> requestedSurfaceSize = state.SurfaceSize;
        if (requestedSurfaceSize != oldSurfaceSize)
        {
            RequestSurfaceSizePhysical(requestedSurfaceSize);
        }
    }

    private void RequestSurfaceSizePhysical(PhysicalSize<uint> surfaceSize)
    {
        _surfaceSize = surfaceSize;
        _ = PInvoke.XResizeWindow(_xconn.Display, _window, Math.Max(1, _surfaceSize.Width), Math.Max(1, _surfaceSize.Height));
    }

    internal string GenerateActivationToken()
    {
        return _xconn.RequestActivationToken(Title);
    }

    private void ApplyImeRequestData(ImeCapabilities capabilities, ImeRequestData requestData)
    {
        if (capabilities.CursorArea() && requestData.CursorArea is { } cursorArea)
        {
            _imeCursorArea = cursorArea;
            PhysicalPosition<short> position = cursorArea.Position.ToPhysical<short>(ScaleFactor);
            PhysicalSize<ushort> size = cursorArea.Size.ToPhysical<ushort>(ScaleFactor);
            _eventLoop.SendImeArea(this, position.X, position.Y, size.Width, size.Height);
        }

        if (capabilities.HintAndPurpose() && requestData.HintAndPurpose is { } hintAndPurpose)
        {
            _imeHintAndPurpose = hintAndPurpose;
        }

        if (capabilities.SurroundingText() && requestData.SurroundingText is { } surroundingText)
        {
            _imeSurroundingText = surroundingText;
        }
    }

    private FrameExtentsHeuristic CachedFrameExtents()
    {
        return _frameExtents ??= _xconn.GetFrameExtentsHeuristic(_window, _xconn.RootWindow);
    }

    private PhysicalPosition<int> OuterPositionPhysical()
    {
        PhysicalPosition<int> innerPosition = InnerPositionPhysical();
        return CachedFrameExtents().InnerPositionToOuter(innerPosition.X, innerPosition.Y);
    }

    private static void StoreName(XConnection xconn, XlibWindow window, string title)
    {
        if (title.Contains('\0'))
        {
            throw new ArgumentException("Window title must not contain a null byte.", nameof(title));
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(title + '\0');
        fixed (byte* titlePtr = utf8)
        {
            _ = PInvoke.XStoreName(xconn.Display, window, (sbyte*)titlePtr);
        }

        byte[] netWmTitle = Encoding.UTF8.GetBytes(title);
        xconn.ChangePropertyBytes(window, xconn.Atoms[AtomName.NetWmName], xconn.Atoms[AtomName.Utf8String], netWmTitle);
    }

    private static unsafe void SetWmProtocols(XConnection xconn, XlibWindow window)
    {
        Span<Atom> protocols =
        [
            xconn.Atoms[AtomName.WmDeleteWindow],
            xconn.Atoms[AtomName.NetWmPing],
            xconn.Atoms[AtomName.NetWmSyncRequest],
        ];
        fixed (Atom* protocolsPtr = protocols)
        {
            _ = PInvoke.XSetWMProtocols(xconn.Display, window, protocolsPtr, protocols.Length);
        }
    }

    private static nuint? CreateSyncRequestCounter(XConnection xconn, XlibWindow window)
    {
        if (xconn.SyncVersion is null)
        {
            return null;
        }

        nuint counter = PInvoke.XSyncCreateCounter(xconn.Display, default);
        if (counter == 0)
        {
            return null;
        }

        xconn.ChangeProperty32(
            window,
            xconn.Atoms[AtomName.NetWmSyncRequestCounter],
            new Atom(PInvoke.XaCardinal),
            [counter]);

        return counter;
    }

    private static unsafe void SelectXInput2Events(XConnection xconn, XlibWindow window)
    {
        if (xconn.XInput2Opcode is null)
        {
            return;
        }

        Span<byte> mask = stackalloc byte[3];
        EventLoop.SetXiMask(mask, PInvoke.XiMotion);
        EventLoop.SetXiMask(mask, PInvoke.XiButtonPress);
        EventLoop.SetXiMask(mask, PInvoke.XiButtonRelease);
        EventLoop.SetXiMask(mask, PInvoke.XiEnter);
        EventLoop.SetXiMask(mask, PInvoke.XiLeave);
        EventLoop.SetXiMask(mask, PInvoke.XiFocusIn);
        EventLoop.SetXiMask(mask, PInvoke.XiFocusOut);
        EventLoop.SetXiMask(mask, PInvoke.XiTouchBegin);
        EventLoop.SetXiMask(mask, PInvoke.XiTouchUpdate);
        EventLoop.SetXiMask(mask, PInvoke.XiTouchEnd);

        fixed (byte* maskPtr = mask)
        {
            XIEventMask eventMask = new()
            {
                DeviceId = PInvoke.XiAllMasterDevices,
                MaskLen = mask.Length,
                Mask = maskPtr,
            };
            try
            {
                _ = PInvoke.XISelectEvents(xconn.Display, window, &eventMask, 1);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }

    private static void SetDndAware(XConnection xconn, XlibWindow window)
    {
        xconn.ChangeProperty32(
            window,
            xconn.Atoms[AtomName.XdndAware],
            new Atom(PInvoke.XaAtom),
            [5]);
    }

    private static void SetWmClass(
        XConnection xconn,
        XlibWindow window,
        WindowAttributes attributes,
        WindowAttributesX11? x11)
    {
        string className = x11?.GeneralName ?? DefaultApplicationClass(attributes.Title);
        string instanceName = x11?.InstanceName ?? Environment.GetEnvironmentVariable("RESOURCE_NAME") ?? className;
        ThrowIfContainsNull(className, nameof(className));
        ThrowIfContainsNull(instanceName, nameof(instanceName));

        byte[] wmClass = Encoding.UTF8.GetBytes($"{instanceName}\0{className}\0");
        xconn.ChangePropertyBytes(window, new Atom(PInvoke.XaWmClass), new Atom(PInvoke.XaString), wmClass);
    }

    private static void SetPid(XConnection xconn, XlibWindow window)
    {
        xconn.ChangeProperty32(
            window,
            xconn.Atoms[AtomName.NetWmPid],
            new Atom(PInvoke.XaCardinal),
            [checked((nuint)Environment.ProcessId)]);

        string machineName = Environment.MachineName;
        if (!string.IsNullOrEmpty(machineName))
        {
            xconn.ChangePropertyBytes(
                window,
                xconn.Atoms[AtomName.WmClientMachine],
                new Atom(PInvoke.XaString),
                Encoding.UTF8.GetBytes(machineName));
        }
    }

    private static void SetWindowTypes(XConnection xconn, XlibWindow window, IReadOnlyList<WindowType> windowTypes)
    {
        if (windowTypes.Count == 0)
        {
            xconn.ChangeProperty32(window, xconn.Atoms[AtomName.NetWmWindowType], new Atom(PInvoke.XaAtom), []);
            return;
        }

        nuint[] atoms = windowTypes.Select(windowType => windowType.AsAtom(xconn).Value).ToArray();
        xconn.ChangeProperty32(window, xconn.Atoms[AtomName.NetWmWindowType], new Atom(PInvoke.XaAtom), atoms);
    }

    private static void SetEmbedInfo(XConnection xconn, XlibWindow window)
    {
        xconn.ChangeProperty32(
            window,
            xconn.Atoms[AtomName.XEmbed],
            xconn.Atoms[AtomName.XEmbed],
            [0, 1]);
    }

    private static unsafe void SetOverrideRedirect(XConnection xconn, XlibWindow window, bool overrideRedirect)
    {
        XSetWindowAttributes attributes = new()
        {
            OverrideRedirect = overrideRedirect ? 1 : 0,
        };
        _ = PInvoke.XChangeWindowAttributes(
            xconn.Display,
            window,
            PInvoke.CWOverrideRedirect,
            &attributes);
    }

    private static string DefaultApplicationClass(string fallbackTitle)
    {
        string? executable = Environment.GetCommandLineArgs().FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(executable))
        {
            string? fileName = Path.GetFileName(executable);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
        }

        return fallbackTitle;
    }

    private static void ThrowIfContainsNull(string value, string paramName)
    {
        if (value.Contains('\0'))
        {
            throw new ArgumentException("Value must not contain a null byte.", paramName);
        }
    }

    private void SetMaximizable(bool maximizable)
    {
        MotifHints hints = _xconn.GetMotifHints(_window);
        hints.SetMaximizable(maximizable);
        _xconn.SetMotifHints(_window, hints);
    }

    private unsafe void SetUrgency(bool urgent)
    {
        XWMHints hints = default;
        XWMHints* existing = PInvoke.XGetWMHints(_xconn.Display, _window);
        if (existing is not null)
        {
            hints = *existing;
            _ = PInvoke.XFree((nint)existing);
        }

        if (urgent)
        {
            hints.Flags |= PInvoke.XUrgencyHint;
        }
        else
        {
            hints.Flags &= ~PInvoke.XUrgencyHint;
        }

        _ = PInvoke.XSetWMHints(_xconn.Display, _window, &hints);
    }

    private unsafe void UpdateNormalHints()
    {
        XSizeHints hints = default;
        double scaleFactor = ScaleFactor;

        if (_resizable)
        {
            SetHintSize(ref hints, PInvoke.PMinSize, _minSurfaceSize, scaleFactor, static (ref XSizeHints h, int w, int v) =>
            {
                h.MinWidth = w;
                h.MinHeight = v;
            });
            SetHintSize(ref hints, PInvoke.PMaxSize, _maxSurfaceSize, scaleFactor, static (ref XSizeHints h, int w, int v) =>
            {
                h.MaxWidth = w;
                h.MaxHeight = v;
            });
        }
        else
        {
            hints.Flags |= PInvoke.PMinSize | PInvoke.PMaxSize;
            hints.MinWidth = hints.MaxWidth = CastDimensionToHint(_surfaceSize.Width);
            hints.MinHeight = hints.MaxHeight = CastDimensionToHint(_surfaceSize.Height);
        }

        SetHintSize(ref hints, PInvoke.PResizeInc, _surfaceResizeIncrements, scaleFactor, static (ref XSizeHints h, int w, int v) =>
        {
            h.WidthInc = w;
            h.HeightInc = v;
        });
        SetHintSize(ref hints, PInvoke.PBaseSize, _baseSize, scaleFactor, static (ref XSizeHints h, int w, int v) =>
        {
            h.BaseWidth = w;
            h.BaseHeight = v;
        });

        PInvoke.XSetWMNormalHints(_xconn.Display, _window, &hints);
    }

    private static void SetHintSize(
        ref XSizeHints hints,
        nint flag,
        Size? size,
        double scaleFactor,
        HintSetter setter)
    {
        if (size is null)
        {
            return;
        }

        PhysicalSize<uint> physical = size.Value.ToPhysical<uint>(scaleFactor);
        hints.Flags |= flag;
        setter(ref hints, CastDimensionToHint(physical.Width), CastDimensionToHint(physical.Height));
    }

    private static int CastDimensionToHint(uint dimension)
    {
        return dimension > int.MaxValue ? int.MaxValue : (int)dimension;
    }

    private delegate void HintSetter(ref XSizeHints hints, int width, int height);

    private void ApplyCursor(Cursor cursor)
    {
        if (cursor.TryGetValue(out Cursor.Icon icon))
        {
            _xconn.SetCursorIcon(_window, icon.Value);
            return;
        }

        if (cursor.TryGetValue(out Cursor.Custom custom))
        {
            if (custom.Value.Provider.AsAny() is not X11CustomCursor x11Cursor)
            {
                throw new InvalidOperationException("Custom cursor was not created by the X11 backend.");
            }

            _xconn.SetCustomCursor(_window, x11Cursor);
        }
    }

    private void ToggleAtom(AtomName atomName, bool enable)
    {
        SetNetWm(enable.ToStateOperation(), _xconn.Atoms[atomName]);
    }

    private void SetNetWm(StateOperation operation, Atom atom0)
    {
        SetNetWm(operation, atom0, Atom.None);
    }

    private void SetNetWm(StateOperation operation, Atom atom0, Atom atom1)
    {
        _xconn.SendClientMessage(
            _window,
            _xconn.RootWindow,
            _xconn.Atoms[AtomName.NetWmState],
            PInvoke.SubstructureRedirectMask | PInvoke.SubstructureNotifyMask,
            [(long)operation, checked((long)atom0.Value), checked((long)atom1.Value), 0, 0]);
    }

    private unsafe void DragInitiate(long action)
    {
        XlibWindow root = default;
        XlibWindow child = default;
        int rootX = 0;
        int rootY = 0;
        int winX = 0;
        int winY = 0;
        uint mask = 0;

        if (PInvoke.XQueryPointer(_xconn.Display, _window, &root, &child, &rootX, &rootY, &winX, &winY, &mask) == 0)
        {
            rootX = _surfacePosition.X;
            rootY = _surfacePosition.Y;
        }

        _ = PInvoke.XUngrabPointer(_xconn.Display, PInvoke.CurrentTime);
        _xconn.Flush();

        _xconn.SendClientMessage(
            _window,
            _xconn.RootWindow,
            _xconn.Atoms[AtomName.NetWmMoveResize],
            PInvoke.SubstructureRedirectMask | PInvoke.SubstructureNotifyMask,
            [rootX, rootY, action, 1, 1]);
        _xconn.Flush();
    }

    private bool NetWmStateContains(AtomName atomName, bool fallback)
    {
        try
        {
            Atom stateAtom = _xconn.Atoms[AtomName.NetWmState];
            Atom wanted = _xconn.Atoms[atomName];
            return _xconn
                .GetProperty32(_window, stateAtom, new Atom(PInvoke.XaAtom))
                .Any(atom => atom == wanted.Value);
        }
        catch (GetPropertyException)
        {
            return fallback;
        }
    }

    private PhysicalPosition<int> XMovePositionForOuterPosition(PhysicalPosition<int> outerPosition)
    {
        if (Wm.WmNameIsOneOf("Weston WM"))
        {
            // WSLg's Weston/Xwayland reports normal frame extents, but XMoveWindow applies
            // a separate fixed translation. Calibrate from observed ConfigureNotify results.
            PhysicalPosition<int> offset = GetXMoveOffset();
            return new PhysicalPosition<int>(outerPosition.X + offset.X, outerPosition.Y + offset.Y);
        }

        if (Wm.WmNameIsOneOf("Enlightenment", "FVWM"))
        {
            PhysicalPosition<int> surfacePosition = CachedFrameExtents().SurfacePosition();
            return new PhysicalPosition<int>(
                outerPosition.X + surfacePosition.X,
                outerPosition.Y + surfacePosition.Y);
        }

        return outerPosition;
    }

    private PhysicalPosition<int> GetXMoveOffset()
    {
        if (_xMoveOffset is { } offset)
        {
            return offset;
        }

        if (_initialRequestedPosition is { } requestedPosition)
        {
            PhysicalPosition<int> currentOuterPosition = OuterPositionPhysical();
            offset = new PhysicalPosition<int>(
                requestedPosition.X - currentOuterPosition.X,
                requestedPosition.Y - currentOuterPosition.Y);
        }
        else
        {
            offset = new PhysicalPosition<int>(0, 0);
        }

        _xMoveOffset = offset;
        return offset;
    }

    private void CalibrateXMoveOffset(PhysicalPosition<int> outerPosition)
    {
        if (!Wm.WmNameIsOneOf("Weston WM") || _lastXMovePosition is not { } xMovePosition)
        {
            return;
        }

        _xMoveOffset = new PhysicalPosition<int>(
            xMovePosition.X - outerPosition.X,
            xMovePosition.Y - outerPosition.Y);
        _lastXMovePosition = null;
    }

    private bool IsVisibleAndNotMinimized()
    {
        if (!_visible)
        {
            return false;
        }

        try
        {
            nuint[] state = _xconn.GetProperty32(
                _window,
                _xconn.Atoms[AtomName.WmState],
                _xconn.Atoms[AtomName.Card32]);
            return !state.Contains((nuint)PInvoke.IconicState);
        }
        catch (GetPropertyException)
        {
            return true;
        }
    }
}

internal sealed record class FullscreenTarget(MonitorHandle Monitor, uint? NativeMode);
