using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;

namespace Winit.Wayland;

internal sealed unsafe class RelativePointerState : IDisposable
{
    private ZwpRelativePointerManagerV1 _manager;
    private bool _disposed;

    private RelativePointerState(ZwpRelativePointerManagerV1 manager)
    {
        _manager = manager;
    }

    public static RelativePointerState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, ZwpInterfaces.RelativePointerManagerV1, maxVersion: 1);
        return new RelativePointerState(new ZwpRelativePointerManagerV1(proxy.Value));
    }

    public WinitRelativePointer GetRelativePointer(WinitState state, WlPointer pointer)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = pointer.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            ZwpRelativePointerManagerV1Request.GetRelativePointer,
            ZwpInterfaces.RelativePointerV1,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zwp_relative_pointer_manager_v1.get_relative_pointer failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        WinitRelativePointer relativePointer = new(state, new ZwpRelativePointerV1(proxy.Value));
        relativePointer.InstallDispatcher();
        return relativePointer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_manager.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _manager,
                ZwpRelativePointerManagerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = ZwpRelativePointerManagerV1.Null;
        }
    }
}

internal sealed unsafe class WinitRelativePointer : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private ZwpRelativePointerV1 _relativePointer;
    private bool _disposed;

    public WinitRelativePointer(WinitState state, ZwpRelativePointerV1 relativePointer)
    {
        _state = state;
        _relativePointer = relativePointer;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _relativePointer,
            &RelativePointerDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_relative_pointer_v1.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_relativePointer.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _relativePointer,
                ZwpRelativePointerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_relativePointer),
                WlProxyMarshalFlags.Destroy,
                null);
            _relativePointer = ZwpRelativePointerV1.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void RelativeMotion(WlFixed dxUnaccel, WlFixed dyUnaccel)
    {
        _state.PushDeviceEvent(new DeviceEvent(new DeviceEvent.PointerMotion(
            (dxUnaccel.ToDouble(), dyUnaccel.ToDouble()))));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int RelativePointerDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitRelativePointer relativePointer ||
            relativePointer._disposed)
        {
            return 0;
        }

        if (opcode == ZwpRelativePointerV1Event.RelativeMotion)
        {
            relativePointer.RelativeMotion(new WlFixed(args[4].Fixed), new WlFixed(args[5].Fixed));
        }

        return 0;
    }
}
