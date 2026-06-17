using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;
using Winit.Dpi;

namespace Winit.Wayland;

internal sealed unsafe class WinitTouchData : IDisposable
{
    private readonly WinitState _state;
    private readonly WinitSeatState _seat;
    private readonly Dictionary<int, TouchPoint> _touchMap = [];
    private readonly GCHandle _selfHandle;
    private bool _disposed;
    private int? _firstTouchId;

    private WinitTouchData(WinitState state, WinitSeatState seat, WlTouch touch)
    {
        _state = state;
        _seat = seat;
        Touch = touch;
        _selfHandle = GCHandle.Alloc(this);
    }

    public WlTouch Touch { get; private set; }

    public static WinitTouchData Create(WinitState state, WinitSeatState seat)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        uint version = PInvoke.WlProxyGetVersion(seat.Seat);
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            seat.Seat,
            WlSeatRequest.GetTouch,
            WlCoreInterfaces.Touch,
            version,
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_seat.get_touch failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        WinitTouchData touch = new(state, seat, new WlTouch(proxy.Value));
        touch.InstallDispatcher();
        return touch;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!Touch.IsNull)
        {
            uint version = PInvoke.WlProxyGetVersion(Touch);
            if (version >= 3)
            {
                PInvoke.WlProxyMarshalArrayFlags(
                    Touch,
                    WlTouchRequest.Release,
                    null,
                    version,
                    WlProxyMarshalFlags.Destroy,
                    null);
            }
            else
            {
                PInvoke.WlProxyDestroy(Touch);
            }

            Touch = WlTouch.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            Touch,
            &TouchDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_touch.");
        }
    }

    private void Down(WlSurface surface, int id, WlFixed x, WlFixed y)
    {
        if (!_state.TryGetWindow(surface, out Window window))
        {
            return;
        }

        LogicalPosition<double> logical = new(x.ToDouble(), y.ToDouble());
        if (_touchMap.Count == 0)
        {
            _firstTouchId = id;
        }

        bool primary = _firstTouchId == id;
        _touchMap[id] = new TouchPoint(surface, logical);

        PhysicalPosition<double> position = logical.ToPhysical<double>(window.ScaleFactor);
        FingerId fingerId = FingerId.FromRaw(unchecked((nuint)id));
        _state.PushWindowEvent(
            window.Id,
            new WindowEvent(new WindowEvent.PointerEntered(
                null,
                position,
                primary,
                new PointerKind(new PointerKind.Touch(fingerId)))));
        _state.PushWindowEvent(
            window.Id,
            new WindowEvent(new WindowEvent.PointerButton(
                null,
                ElementState.Pressed,
                position,
                primary,
                new ButtonSource(new ButtonSource.Touch(fingerId, null)))));
    }

    private void Up(int id)
    {
        if (!_touchMap.Remove(id, out TouchPoint touchPoint))
        {
            return;
        }

        bool primary = _firstTouchId == id;
        if (_touchMap.Count == 0)
        {
            _firstTouchId = null;
        }

        if (!_state.TryGetWindow(touchPoint.Surface, out Window window))
        {
            return;
        }

        PhysicalPosition<double> position = touchPoint.Location.ToPhysical<double>(window.ScaleFactor);
        FingerId fingerId = FingerId.FromRaw(unchecked((nuint)id));
        _state.PushWindowEvent(
            window.Id,
            new WindowEvent(new WindowEvent.PointerButton(
                null,
                ElementState.Released,
                position,
                primary,
                new ButtonSource(new ButtonSource.Touch(fingerId, null)))));
        _state.PushWindowEvent(
            window.Id,
            new WindowEvent(new WindowEvent.PointerLeft(
                null,
                position,
                primary,
                new PointerKind(new PointerKind.Touch(fingerId)))));
    }

    private void Motion(int id, WlFixed x, WlFixed y)
    {
        if (!_touchMap.TryGetValue(id, out TouchPoint touchPoint))
        {
            return;
        }

        if (!_state.TryGetWindow(touchPoint.Surface, out Window window))
        {
            return;
        }

        LogicalPosition<double> logical = new(x.ToDouble(), y.ToDouble());
        _touchMap[id] = touchPoint with { Location = logical };

        FingerId fingerId = FingerId.FromRaw(unchecked((nuint)id));
        _state.PushWindowEvent(
            window.Id,
            new WindowEvent(new WindowEvent.PointerMoved(
                null,
                logical.ToPhysical<double>(window.ScaleFactor),
                _firstTouchId == id,
                new PointerSource(new PointerSource.Touch(fingerId, null)))));
    }

    private void Cancel()
    {
        foreach ((int id, TouchPoint touchPoint) in _touchMap.ToArray())
        {
            if (!_state.TryGetWindow(touchPoint.Surface, out Window window))
            {
                continue;
            }

            FingerId fingerId = FingerId.FromRaw(unchecked((nuint)id));
            _state.PushWindowEvent(
                window.Id,
                new WindowEvent(new WindowEvent.PointerLeft(
                    null,
                    touchPoint.Location.ToPhysical<double>(window.ScaleFactor),
                    _firstTouchId == id,
                    new PointerKind(new PointerKind.Touch(fingerId)))));
        }

        _touchMap.Clear();
        _firstTouchId = null;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TouchDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitTouchData touch || touch._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case WlTouchEvent.Down:
                touch.Down(new WlSurface(args[2].Object), args[3].Int, new WlFixed(args[4].Fixed), new WlFixed(args[5].Fixed));
                break;
            case WlTouchEvent.Up:
                touch.Up(args[2].Int);
                break;
            case WlTouchEvent.Motion:
                touch.Motion(args[1].Int, new WlFixed(args[2].Fixed), new WlFixed(args[3].Fixed));
                break;
            case WlTouchEvent.Cancel:
                touch.Cancel();
                break;
        }

        return 0;
    }
}

internal readonly record struct TouchPoint(WlSurface Surface, LogicalPosition<double> Location);
