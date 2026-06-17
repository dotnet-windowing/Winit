using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;
using Winit.Dpi;

namespace Winit.Wayland;

internal sealed unsafe class WinitPointerData : IDisposable
{
    private const uint FrameDoubleClickMilliseconds = 500;

    private readonly WinitState _state;
    private readonly WinitSeatState _seat;
    private readonly GCHandle _selfHandle;
    private WlSurface _cursorSurface;
    private WaylandViewport? _cursorViewport;
    private CursorShapeDevice? _cursorShapeDevice;
    private WaylandCursorBuffer? _customCursorBuffer;
    private LockedPointer? _lockedPointer;
    private ConfinedPointer? _confinedPointer;
    private WlSurface _grabbedSurface;
    private bool _disposed;
    private WindowId? _focusedWindow;
    private FrameSurfaceRole? _focusedFrameRole;
    private PhysicalPosition<double> _position;
    private AxisFrame _axisFrame;
    private TouchPhase _axisPhase = TouchPhase.Ended;
    private WindowId? _lastFrameClickWindow;
    private FrameSurfaceRole? _lastFrameClickRole;
    private MouseButton? _lastFrameClickButton;
    private uint _lastFrameClickTime;
    private WindowId? _pendingFrameMoveWindow;

    private WinitPointerData(
        WinitState state,
        WinitSeatState seat,
        WlPointer pointer,
        WlSurface cursorSurface,
        WaylandViewport? cursorViewport)
    {
        _state = state;
        _seat = seat;
        Pointer = pointer;
        _cursorSurface = cursorSurface;
        _cursorViewport = cursorViewport;
        _selfHandle = GCHandle.Alloc(this);
    }

    public WlPointer Pointer { get; private set; }

    public WinitSeatState Seat => _seat;

    public uint LatestButtonSerial { get; private set; }

    public uint LatestEnterSerial { get; private set; }

    public WindowId? FocusedWindow => _focusedWindow;

    public void ApplyCursor(Cursor cursor, bool visible)
    {
        if (!visible)
        {
            HideCursor();
            return;
        }

        if (cursor.TryGetValue(out Cursor.Custom custom))
        {
            if (custom.Value.Provider.AsAny() is WaylandCustomCursor waylandCursor)
            {
                ApplyCustomCursor(waylandCursor);
            }

            return;
        }

        if (cursor.TryGetValue(out Cursor.Icon icon))
        {
            ApplyNamedCursor(icon.Value);
        }
    }

    public void HideCursor()
    {
        if (Pointer.IsNull)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[4];
        args[0].Uint = LatestEnterSerial;
        args[1].Object = 0;
        args[2].Int = 0;
        args[3].Int = 0;
        PInvoke.WlProxyMarshalArray(Pointer, WlPointerRequest.SetCursor, args);
        _state.Connection.Flush();
    }

    public static WinitPointerData Create(WinitState state, WinitSeatState seat)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        uint version = PInvoke.WlProxyGetVersion(seat.Seat);
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            seat.Seat,
            WlSeatRequest.GetPointer,
            WlCoreInterfaces.Pointer,
            version,
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_seat.get_pointer failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        WlSurface cursorSurface = WlSurface.Null;
        WaylandViewport? cursorViewport = null;
        WinitPointerData? pointer = null;
        try
        {
            cursorSurface = state.CreateSurface();
            cursorViewport = state.ViewporterState?.GetViewport(state, cursorSurface);
            pointer = new WinitPointerData(
                state,
                seat,
                new WlPointer(proxy.Value),
                cursorSurface,
                cursorViewport);
            cursorViewport = null;
            pointer.InstallDispatcher();
            return pointer;
        }
        catch
        {
            cursorViewport?.Dispose();
            if (pointer is not null)
            {
                pointer.Dispose();
            }
            else
            {
                if (!cursorSurface.IsNull)
                {
                    DestroySurface(cursorSurface);
                }

                ReleasePointer(new WlPointer(proxy.Value));
            }

            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _confinedPointer?.Dispose();
        _confinedPointer = null;
        _lockedPointer?.Dispose();
        _lockedPointer = null;

        _cursorShapeDevice?.Dispose();
        _cursorShapeDevice = null;

        _customCursorBuffer?.Dispose();
        _customCursorBuffer = null;

        _cursorViewport?.Dispose();
        _cursorViewport = null;

        if (!_cursorSurface.IsNull)
        {
            DestroySurface(_cursorSurface);
            _cursorSurface = WlSurface.Null;
        }

        if (!Pointer.IsNull)
        {
            ReleasePointer(Pointer);
            Pointer = WlPointer.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            Pointer,
            &PointerDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_pointer.");
        }
    }

    private void Enter(uint serial, WlSurface surface, WlFixed surfaceX, WlFixed surfaceY)
    {
        if (_state.TryGetFrameSurface(surface, out FrameSurfaceData frame))
        {
            LatestEnterSerial = serial;
            _focusedWindow = frame.Window.Id;
            _focusedFrameRole = frame.Role;
            _position = ToPhysical(frame.Window, surfaceX, surfaceY);
            ApplyFrameCursor(frame.Role);
            return;
        }

        if (!_state.TryGetWindow(surface, out Window window))
        {
            return;
        }

        LatestEnterSerial = serial;
        _focusedWindow = window.Id;
        _focusedFrameRole = null;
        ClearPendingFrameMove();
        _position = ToPhysical(window, surfaceX, surfaceY);

        ApplyCursor(window.CurrentCursor, window.CursorVisible);
        if (window.CursorGrabMode != CursorGrabMode.None)
        {
            SetCursorGrabMode(window.CursorGrabMode, surface);
        }

        _state.PushWindowEvent(
            window.Id,
            new WindowEvent(new WindowEvent.PointerEntered(
                null,
                _position,
                true,
                new PointerKind(new PointerKind.Mouse()))));
    }

    private void Leave(WlSurface surface)
    {
        if (_state.TryGetFrameSurface(surface, out _))
        {
            _focusedFrameRole = null;
            _focusedWindow = null;
            ClearPendingFrameMove();
            ClearFrameClickTracking();
            _axisPhase = TouchPhase.Ended;
            return;
        }

        WindowId? focused = _focusedWindow;
        _focusedWindow = null;
        _focusedFrameRole = null;
        ClearPendingFrameMove();
        ClearFrameClickTracking();
        _axisPhase = TouchPhase.Ended;

        WindowId windowId = !surface.IsNull
            ? WindowId.FromRaw((nuint)surface.Value)
            : focused.GetValueOrDefault();
        if (windowId.Equals(default(WindowId)))
        {
            return;
        }

        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.PointerLeft(
                null,
                _position,
                true,
                new PointerKind(new PointerKind.Mouse()))));
    }

    private void Motion(WlFixed surfaceX, WlFixed surfaceY)
    {
        if (_focusedWindow is not { } windowId || !_state.Windows.TryGetValue(windowId, out Window? window))
        {
            return;
        }

        _position = ToPhysical(window, surfaceX, surfaceY);
        if (_focusedFrameRole is { } role)
        {
            ApplyFrameCursor(role);
            if (role == FrameSurfaceRole.Top && TryTakePendingFrameMove(windowId))
            {
                ClearFrameClickTracking();
                window.DragWindow();
            }

            return;
        }

        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.PointerMoved(
                null,
                _position,
                true,
                new PointerSource(new PointerSource.Mouse()))));
    }

    private void Button(uint serial, uint time, uint button, WlPointerButtonState state)
    {
        if (_focusedWindow is not { } windowId)
        {
            return;
        }

        LatestButtonSerial = serial;
        if (_focusedFrameRole is { } role)
        {
            HandleFrameButton(windowId, role, time, button, state);
            return;
        }

        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.PointerButton(
                null,
                state == WlPointerButtonState.Pressed ? ElementState.Pressed : ElementState.Released,
                _position,
                true,
                WaylandButtonToWinit(button))));
    }

    private void HandleFrameButton(
        WindowId windowId,
        FrameSurfaceRole role,
        uint time,
        uint button,
        WlPointerButtonState state)
    {
        MouseButton? mouseButton = WaylandButtonToWinit(button).MouseButton();
        if (state != WlPointerButtonState.Pressed)
        {
            if (mouseButton == MouseButton.Left)
            {
                ClearPendingFrameMove();
            }

            return;
        }

        if (!_state.Windows.TryGetValue(windowId, out Window? window))
        {
            return;
        }

        if (mouseButton == MouseButton.Left)
        {
            switch (role)
            {
                case FrameSurfaceRole.Top:
                    if (IsFrameDoubleClick(windowId, role, mouseButton.Value, time))
                    {
                        ClearPendingFrameMove();
                        ClearFrameClickTracking();
                        if (window.CanMaximize)
                        {
                            window.SetMaximized(!window.IsMaximized);
                        }

                        break;
                    }

                    TrackFrameClick(windowId, role, mouseButton.Value, time);
                    SetPendingFrameMove(windowId);
                    break;
                case FrameSurfaceRole.CloseButton:
                    ClearPendingFrameMove();
                    ClearFrameClickTracking();
                    _state.QueueClose(windowId);
                    break;
                case FrameSurfaceRole.MaximizeButton:
                    ClearPendingFrameMove();
                    ClearFrameClickTracking();
                    if (window.CanMaximize)
                    {
                        window.SetMaximized(!window.IsMaximized);
                    }

                    break;
                case FrameSurfaceRole.MinimizeButton:
                    ClearPendingFrameMove();
                    ClearFrameClickTracking();
                    if (window.CanMinimize)
                    {
                        window.SetMinimized(true);
                    }

                    break;
                default:
                    ClearPendingFrameMove();
                    ClearFrameClickTracking();
                    if (window.IsResizable)
                    {
                        window.DragResizeWindow(ResizeDirectionForFrame(role));
                    }

                    break;
            }
        }
        else if (mouseButton == MouseButton.Right && role == FrameSurfaceRole.Top)
        {
            ClearPendingFrameMove();
            ClearFrameClickTracking();
            if (window.CanShowWindowMenu)
            {
                window.ShowWindowMenu(Position.FromPhysical(_position));
            }
        }
        else
        {
            ClearPendingFrameMove();
            ClearFrameClickTracking();
        }
    }

    private bool IsFrameDoubleClick(WindowId windowId, FrameSurfaceRole role, MouseButton button, uint time)
    {
        return _lastFrameClickWindow is { } lastWindow &&
            lastWindow.Equals(windowId) &&
            _lastFrameClickRole == role &&
            _lastFrameClickButton == button &&
            unchecked(time - _lastFrameClickTime) <= FrameDoubleClickMilliseconds;
    }

    private void TrackFrameClick(WindowId windowId, FrameSurfaceRole role, MouseButton button, uint time)
    {
        _lastFrameClickWindow = windowId;
        _lastFrameClickRole = role;
        _lastFrameClickButton = button;
        _lastFrameClickTime = time;
    }

    private void ClearFrameClickTracking()
    {
        _lastFrameClickWindow = null;
        _lastFrameClickRole = null;
        _lastFrameClickButton = null;
        _lastFrameClickTime = 0;
    }

    private void SetPendingFrameMove(WindowId windowId)
    {
        _pendingFrameMoveWindow = windowId;
    }

    private bool TryTakePendingFrameMove(WindowId windowId)
    {
        if (_pendingFrameMoveWindow is not { } pending || !pending.Equals(windowId))
        {
            return false;
        }

        _pendingFrameMoveWindow = null;
        return true;
    }

    private void ClearPendingFrameMove()
    {
        _pendingFrameMoveWindow = null;
    }

    private void ApplyFrameCursor(FrameSurfaceRole role)
    {
        if (IsResizeFrameRole(role) &&
            _focusedWindow is { } windowId &&
            _state.Windows.TryGetValue(windowId, out Window? window) &&
            !window.IsResizable)
        {
            ApplyCursor(new Cursor(new Cursor.Icon(CursorIcon.Default)), visible: true);
            return;
        }

        CursorIcon icon = role switch
        {
            FrameSurfaceRole.Left => CursorIcon.WResize,
            FrameSurfaceRole.Right => CursorIcon.EResize,
            FrameSurfaceRole.Bottom => CursorIcon.SResize,
            FrameSurfaceRole.TopLeft => CursorIcon.NwResize,
            FrameSurfaceRole.TopRight => CursorIcon.NeResize,
            FrameSurfaceRole.BottomLeft => CursorIcon.SwResize,
            FrameSurfaceRole.BottomRight => CursorIcon.SeResize,
            _ => CursorIcon.Default,
        };
        ApplyCursor(new Cursor(new Cursor.Icon(icon)), visible: true);
    }

    private static bool IsResizeFrameRole(FrameSurfaceRole role)
    {
        return role is
            FrameSurfaceRole.Left or
            FrameSurfaceRole.Right or
            FrameSurfaceRole.Bottom or
            FrameSurfaceRole.TopLeft or
            FrameSurfaceRole.TopRight or
            FrameSurfaceRole.BottomLeft or
            FrameSurfaceRole.BottomRight;
    }

    private static ResizeDirection ResizeDirectionForFrame(FrameSurfaceRole role)
    {
        return role switch
        {
            FrameSurfaceRole.Left => ResizeDirection.West,
            FrameSurfaceRole.Right => ResizeDirection.East,
            FrameSurfaceRole.Bottom => ResizeDirection.South,
            FrameSurfaceRole.TopLeft => ResizeDirection.NorthWest,
            FrameSurfaceRole.TopRight => ResizeDirection.NorthEast,
            FrameSurfaceRole.BottomLeft => ResizeDirection.SouthWest,
            FrameSurfaceRole.BottomRight => ResizeDirection.SouthEast,
            _ => ResizeDirection.South,
        };
    }

    private void Axis(WlPointerAxis axis, WlFixed value)
    {
        ref AxisData data = ref AxisDataFor(axis);
        data.Absolute += -value.ToDouble();
        data.HasAbsolute = true;
    }

    private void AxisStop(WlPointerAxis axis)
    {
        AxisDataFor(axis).Stop = true;
    }

    private void AxisDiscrete(WlPointerAxis axis, int discrete)
    {
        AxisDataFor(axis).Discrete += -discrete;
    }

    private void AxisValue120(WlPointerAxis axis, int value120)
    {
        AxisDataFor(axis).Value120 += -value120;
    }

    private void Frame()
    {
        if (_focusedWindow is not { } windowId ||
            !_state.Windows.TryGetValue(windowId, out Window? window))
        {
            _axisFrame = default;
            return;
        }

        AxisData horizontal = _axisFrame.Horizontal;
        AxisData vertical = _axisFrame.Vertical;
        if (!horizontal.HasAny && !vertical.HasAny)
        {
            return;
        }

        bool hasValue120 = horizontal.Value120 != 0 || vertical.Value120 != 0;
        bool hasDiscrete = horizontal.Discrete != 0 || vertical.Discrete != 0;
        bool stopped = horizontal.Stop || vertical.Stop;
        MouseScrollDelta delta = hasValue120
            ? new MouseScrollDelta(new MouseScrollDelta.LineDelta(
                horizontal.Value120 / 120.0f,
                vertical.Value120 / 120.0f))
            : hasDiscrete
                ? new MouseScrollDelta(new MouseScrollDelta.LineDelta(
                    horizontal.Discrete,
                    vertical.Discrete))
                : new MouseScrollDelta(new MouseScrollDelta.PixelDelta(
                    new LogicalPosition<double>(
                        horizontal.Absolute,
                        vertical.Absolute).ToPhysical<double>(window.ScaleFactor)));
        TouchPhase phase = stopped
            ? TouchPhase.Ended
            : hasValue120 || hasDiscrete
                ? TouchPhase.Moved
                : _axisPhase is TouchPhase.Started or TouchPhase.Moved
                    ? TouchPhase.Moved
                    : TouchPhase.Started;
        _axisPhase = phase;

        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.MouseWheel(null, delta, phase)));
        _axisFrame = default;
    }

    private ref AxisData AxisDataFor(WlPointerAxis axis)
    {
        return ref axis == WlPointerAxis.HorizontalScroll
            ? ref _axisFrame.Horizontal
            : ref _axisFrame.Vertical;
    }

    private static PhysicalPosition<double> ToPhysical(Window window, WlFixed x, WlFixed y)
    {
        return new LogicalPosition<double>(x.ToDouble(), y.ToDouble()).ToPhysical<double>(window.ScaleFactor);
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

    private static void ReleasePointer(WlPointer pointer)
    {
        uint version = PInvoke.WlProxyGetVersion(pointer);
        if (version >= 3)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                pointer,
                WlPointerRequest.Release,
                null,
                version,
                WlProxyMarshalFlags.Destroy,
                null);
        }
        else
        {
            PInvoke.WlProxyDestroy(pointer);
        }
    }

    private void ApplyNamedCursor(CursorIcon icon)
    {
        if (_cursorShapeDevice?.SetShape(LatestEnterSerial, icon) == true)
        {
            _customCursorBuffer?.Dispose();
            _customCursorBuffer = null;
            _state.Connection.Flush();
            return;
        }

        if (_state.CursorTheme?.GetCursor(icon) is not { } cursor)
        {
            return;
        }

        AttachCursor(cursor.Buffer, cursor.Width, cursor.Height, cursor.HotspotX, cursor.HotspotY);
        _customCursorBuffer?.Dispose();
        _customCursorBuffer = null;
    }

    private void ApplyCustomCursor(WaylandCustomCursor cursor)
    {
        WaylandCursorBuffer buffer = WaylandCursorBuffer.Create(_state, cursor);
        AttachCursor(buffer.Buffer, buffer.Width, buffer.Height, buffer.HotspotX, buffer.HotspotY);

        WaylandCursorBuffer? oldBuffer = _customCursorBuffer;
        _customCursorBuffer = buffer;
        oldBuffer?.Dispose();
    }

    private void AttachCursor(WlBuffer buffer, int width, int height, int hotspotX, int hotspotY)
    {
        if (Pointer.IsNull || _cursorSurface.IsNull || buffer.IsNull)
        {
            return;
        }

        WlArgument* attachArgs = stackalloc WlArgument[3];
        attachArgs[0].Object = buffer.Value;
        attachArgs[1].Int = 0;
        attachArgs[2].Int = 0;
        PInvoke.WlProxyMarshalArray(_cursorSurface, WlSurfaceRequest.Attach, attachArgs);

        double scaleFactor = CursorScaleFactor();
        int cursorHotspotX = hotspotX;
        int cursorHotspotY = hotspotY;
        LogicalSize<int> logicalSize = new PhysicalSize<int>(width, height).ToLogical<int>(scaleFactor);
        if (_cursorViewport is not null)
        {
            _cursorViewport.SetDestination(Math.Max(1, logicalSize.Width), Math.Max(1, logicalSize.Height));

            LogicalPosition<int> logicalHotspot =
                new PhysicalPosition<int>(hotspotX, hotspotY).ToLogical<int>(scaleFactor);
            cursorHotspotX = logicalHotspot.X;
            cursorHotspotY = logicalHotspot.Y;
        }
        else
        {
            WlArgument* scaleArgs = stackalloc WlArgument[1];
            scaleArgs[0].Int = checked((int)Math.Max(1, Math.Round(scaleFactor)));
            PInvoke.WlProxyMarshalArray(_cursorSurface, WlSurfaceRequest.SetBufferScale, scaleArgs);
        }

        WlArgument* damageArgs = stackalloc WlArgument[4];
        damageArgs[0].Int = 0;
        damageArgs[1].Int = 0;
        uint surfaceVersion = PInvoke.WlProxyGetVersion(_cursorSurface);
        damageArgs[2].Int = surfaceVersion >= 4 ? width : Math.Max(1, logicalSize.Width);
        damageArgs[3].Int = surfaceVersion >= 4 ? height : Math.Max(1, logicalSize.Height);
        PInvoke.WlProxyMarshalArray(
            _cursorSurface,
            surfaceVersion >= 4 ? WlSurfaceRequest.DamageBuffer : WlSurfaceRequest.Damage,
            damageArgs);
        PInvoke.WlProxyMarshalArray(_cursorSurface, WlSurfaceRequest.Commit, null);

        WlArgument* cursorArgs = stackalloc WlArgument[4];
        cursorArgs[0].Uint = LatestEnterSerial;
        cursorArgs[1].Object = _cursorSurface.Value;
        cursorArgs[2].Int = cursorHotspotX;
        cursorArgs[3].Int = cursorHotspotY;
        PInvoke.WlProxyMarshalArray(Pointer, WlPointerRequest.SetCursor, cursorArgs);
        _state.Connection.Flush();
    }

    private double CursorScaleFactor()
    {
        return _focusedWindow is { } windowId && _state.Windows.TryGetValue(windowId, out Window? window)
            ? window.ScaleFactor
            : 1.0;
    }

    public void EnsureCursorShape(CursorShapeManager manager)
    {
        if (_cursorShapeDevice is not null || Pointer.IsNull)
        {
            return;
        }

        _cursorShapeDevice = manager.GetPointer(_state, Pointer);
    }

    public void SetCursorGrabMode(CursorGrabMode mode, WlSurface surface)
    {
        if (Pointer.IsNull || surface.IsNull)
        {
            return;
        }

        if (mode == CursorGrabMode.None)
        {
            ReleaseCursorGrab();
            return;
        }

        PointerConstraintsState constraints = _state.PointerConstraints
            ?? throw new NotSupportedException("zwp_pointer_constraints_v1 is not available.");

        switch (mode)
        {
            case CursorGrabMode.Confined:
                _lockedPointer?.Dispose();
                _lockedPointer = null;
                _confinedPointer ??= constraints.ConfinePointer(_state, surface, Pointer);
                break;
            case CursorGrabMode.Locked:
                _confinedPointer?.Dispose();
                _confinedPointer = null;
                _lockedPointer ??= constraints.LockPointer(_state, surface, Pointer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        _grabbedSurface = surface;
        _state.Connection.Flush();
    }

    public void ReleaseCursorGrab()
    {
        _confinedPointer?.Dispose();
        _confinedPointer = null;
        _lockedPointer?.Dispose();
        _lockedPointer = null;
        _grabbedSurface = WlSurface.Null;
        _state.Connection.Flush();
    }

    public void ReleaseCursorGrabForSurface(WlSurface surface)
    {
        if (!_grabbedSurface.IsNull && _grabbedSurface.Value == surface.Value)
        {
            ReleaseCursorGrab();
        }
    }

    public void SetLockedCursorPosition(double surfaceX, double surfaceY)
    {
        _lockedPointer?.SetCursorPositionHint(surfaceX, surfaceY);
        _state.Connection.Flush();
    }

    private static ButtonSource WaylandButtonToWinit(uint button)
    {
        const uint BtnMouse = 0x110;
        const uint BtnJoystick = 0x120;

        if (button >= BtnMouse && button < BtnJoystick &&
            MouseButtonExtensions.TryFromByte(checked((byte)(button - BtnMouse))) is { } mouse)
        {
            return new ButtonSource(new ButtonSource.Mouse(mouse));
        }

        return new ButtonSource(new ButtonSource.Unknown(checked((ushort)Math.Min(button, ushort.MaxValue))));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int PointerDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitPointerData pointer || pointer._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case WlPointerEvent.Enter:
                pointer.Enter(args[0].Uint, new WlSurface(args[1].Object), new WlFixed(args[2].Fixed), new WlFixed(args[3].Fixed));
                break;
            case WlPointerEvent.Leave:
                pointer.Leave(new WlSurface(args[1].Object));
                break;
            case WlPointerEvent.Motion:
                pointer.Motion(new WlFixed(args[1].Fixed), new WlFixed(args[2].Fixed));
                break;
            case WlPointerEvent.Button:
                pointer.Button(args[0].Uint, args[1].Uint, args[2].Uint, (WlPointerButtonState)args[3].Uint);
                break;
            case WlPointerEvent.Axis:
                pointer.Axis((WlPointerAxis)args[1].Uint, new WlFixed(args[2].Fixed));
                if (PInvoke.WlProxyGetVersion(pointer.Pointer) < 5)
                {
                    pointer.Frame();
                }
                break;
            case WlPointerEvent.Frame:
                pointer.Frame();
                break;
            case WlPointerEvent.AxisStop:
                pointer.AxisStop((WlPointerAxis)args[1].Uint);
                break;
            case WlPointerEvent.AxisDiscrete:
                pointer.AxisDiscrete((WlPointerAxis)args[0].Uint, args[1].Int);
                break;
            case WlPointerEvent.AxisValue120:
                pointer.AxisValue120((WlPointerAxis)args[0].Uint, args[1].Int);
                break;
        }

        return 0;
    }

    private struct AxisFrame
    {
        public AxisData Horizontal;
        public AxisData Vertical;
    }

    private struct AxisData
    {
        public double Absolute;
        public int Discrete;
        public int Value120;
        public bool Stop;
        public bool HasAbsolute;

        public readonly bool HasAny => HasAbsolute || Discrete != 0 || Value120 != 0 || Stop;
    }
}
