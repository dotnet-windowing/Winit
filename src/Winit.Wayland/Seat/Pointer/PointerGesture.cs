using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;
using Winit.Dpi;

namespace Winit.Wayland;

internal sealed unsafe class PointerGesturesState : IDisposable
{
    private ZwpPointerGesturesV1 _pointerGestures;
    private bool _disposed;

    private PointerGesturesState(ZwpPointerGesturesV1 pointerGestures)
    {
        _pointerGestures = pointerGestures;
    }

    public static PointerGesturesState Bind(WinitState state, WaylandGlobal global)
    {
        if (global.Version < 3)
        {
            throw new NotSupportedException("zwp_pointer_gestures_v1 v3 is required.");
        }

        WlProxy proxy = state.BindGlobal(global, ZwpInterfaces.PointerGesturesV1, maxVersion: 3);
        return new PointerGesturesState(new ZwpPointerGesturesV1(proxy.Value));
    }

    public WinitPointerGesturePinch GetPinchGesture(WinitState state, WlPointer pointer)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = pointer.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _pointerGestures,
            ZwpPointerGesturesV1Request.GetPinchGesture,
            ZwpInterfaces.PointerGesturePinchV1,
            PInvoke.WlProxyGetVersion(_pointerGestures),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zwp_pointer_gestures_v1.get_pinch_gesture failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        WinitPointerGesturePinch pinch = new(state, new ZwpPointerGesturePinchV1(proxy.Value));
        pinch.InstallDispatcher();
        return pinch;
    }

    public WinitPointerGestureHold GetHoldGesture(WinitState state, WlPointer pointer)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = pointer.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _pointerGestures,
            ZwpPointerGesturesV1Request.GetHoldGesture,
            ZwpInterfaces.PointerGestureHoldV1,
            PInvoke.WlProxyGetVersion(_pointerGestures),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zwp_pointer_gestures_v1.get_hold_gesture failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        WinitPointerGestureHold hold = new(state, new ZwpPointerGestureHoldV1(proxy.Value));
        hold.InstallDispatcher();
        return hold;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_pointerGestures.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _pointerGestures,
                ZwpPointerGesturesV1Request.Release,
                null,
                PInvoke.WlProxyGetVersion(_pointerGestures),
                WlProxyMarshalFlags.Destroy,
                null);
            _pointerGestures = ZwpPointerGesturesV1.Null;
        }
    }
}

internal sealed unsafe class WinitPointerGesturePinch : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private ZwpPointerGesturePinchV1 _pinch;
    private WindowId? _windowId;
    private double _previousPinch = 1.0;
    private bool _disposed;

    public WinitPointerGesturePinch(WinitState state, ZwpPointerGesturePinchV1 pinch)
    {
        _state = state;
        _pinch = pinch;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _pinch,
            &PinchDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_pointer_gesture_pinch_v1.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_pinch.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _pinch,
                ZwpPointerGesturePinchV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_pinch),
                WlProxyMarshalFlags.Destroy,
                null);
            _pinch = ZwpPointerGesturePinchV1.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void Begin(WlSurface surface, uint fingers)
    {
        if (fingers != 2 || !_state.TryGetWindow(surface, out Window window))
        {
            return;
        }

        _windowId = window.Id;
        _previousPinch = 1.0;
        PushGestures(window.Id, TouchPhase.Started, new PhysicalPosition<float>(0.0f, 0.0f), 0.0, 0.0f);
    }

    private void Update(WlFixed dx, WlFixed dy, WlFixed scale, WlFixed rotation)
    {
        if (_windowId is not { } windowId || !_state.Windows.TryGetValue(windowId, out Window? window))
        {
            return;
        }

        PhysicalPosition<float> panDelta = new LogicalPosition<float>(
            (float)dx.ToDouble(),
            (float)dy.ToDouble()).ToPhysical<float>(window.ScaleFactor);
        double pinch = scale.ToDouble();
        double pinchDelta = pinch - _previousPinch;
        _previousPinch = pinch;
        float rotationDelta = (float)-rotation.ToDouble();

        PushGestures(windowId, TouchPhase.Moved, panDelta, pinchDelta, rotationDelta);
    }

    private void End(int cancelled)
    {
        if (_windowId is not { } windowId)
        {
            return;
        }

        _windowId = null;
        _previousPinch = 1.0;

        TouchPhase phase = cancelled == 0 ? TouchPhase.Ended : TouchPhase.Cancelled;
        PushGestures(windowId, phase, new PhysicalPosition<float>(0.0f, 0.0f), 0.0, 0.0f);
    }

    private void PushGestures(
        WindowId windowId,
        TouchPhase phase,
        PhysicalPosition<float> panDelta,
        double pinchDelta,
        float rotationDelta)
    {
        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.PanGesture(null, panDelta, phase)));
        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.PinchGesture(null, pinchDelta, phase)));
        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.RotationGesture(null, rotationDelta, phase)));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int PinchDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitPointerGesturePinch pinch ||
            pinch._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case ZwpPointerGesturePinchV1Event.Begin:
                pinch.Begin(new WlSurface(args[2].Object), args[3].Uint);
                break;
            case ZwpPointerGesturePinchV1Event.Update:
                pinch.Update(
                    new WlFixed(args[1].Fixed),
                    new WlFixed(args[2].Fixed),
                    new WlFixed(args[3].Fixed),
                    new WlFixed(args[4].Fixed));
                break;
            case ZwpPointerGesturePinchV1Event.End:
                pinch.End(args[2].Int);
                break;
        }

        return 0;
    }
}

internal sealed unsafe class WinitPointerGestureHold : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private ZwpPointerGestureHoldV1 _hold;
    private WindowId? _windowId;
    private bool _disposed;

    public WinitPointerGestureHold(WinitState state, ZwpPointerGestureHoldV1 hold)
    {
        _state = state;
        _hold = hold;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _hold,
            &HoldDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_pointer_gesture_hold_v1.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_hold.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _hold,
                ZwpPointerGestureHoldV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_hold),
                WlProxyMarshalFlags.Destroy,
                null);
            _hold = ZwpPointerGestureHoldV1.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void Begin(WlSurface surface, uint fingers)
    {
        if (fingers < 2 || !_state.TryGetWindow(surface, out Window window))
        {
            return;
        }

        _windowId = window.Id;
        PushHold(window.Id, TouchPhase.Started);
    }

    private void End(int cancelled)
    {
        if (_windowId is not { } windowId)
        {
            return;
        }

        _windowId = null;
        PushHold(windowId, cancelled == 0 ? TouchPhase.Ended : TouchPhase.Cancelled);
    }

    private void PushHold(WindowId windowId, TouchPhase phase)
    {
        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.HoldGesture(null, phase)));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int HoldDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitPointerGestureHold hold ||
            hold._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case ZwpPointerGestureHoldV1Event.Begin:
                hold.Begin(new WlSurface(args[2].Object), args[3].Uint);
                break;
            case ZwpPointerGestureHoldV1Event.End:
                hold.End(args[2].Int);
                break;
        }

        return 0;
    }
}
