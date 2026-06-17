using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.Wayland;

internal sealed unsafe class PointerConstraintsState : IDisposable
{
    private ZwpPointerConstraintsV1 _pointerConstraints;
    private bool _disposed;

    private PointerConstraintsState(ZwpPointerConstraintsV1 pointerConstraints)
    {
        _pointerConstraints = pointerConstraints;
    }

    public static PointerConstraintsState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, ZwpInterfaces.PointerConstraintsV1, maxVersion: 1);
        return new PointerConstraintsState(new ZwpPointerConstraintsV1(proxy.Value));
    }

    public LockedPointer LockPointer(WinitState state, WlSurface surface, WlPointer pointer)
    {
        WlArgument* args = stackalloc WlArgument[5];
        args[0].Object = 0;
        args[1].Object = surface.Value;
        args[2].Object = pointer.Value;
        args[3].Object = 0;
        args[4].Uint = (uint)ZwpPointerConstraintsV1Lifetime.Persistent;

        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _pointerConstraints,
            ZwpPointerConstraintsV1Request.LockPointer,
            ZwpInterfaces.LockedPointerV1,
            PInvoke.WlProxyGetVersion(_pointerConstraints),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zwp_pointer_constraints_v1.lock_pointer failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        LockedPointer lockedPointer = new(new ZwpLockedPointerV1(proxy.Value));
        lockedPointer.InstallDispatcher();
        return lockedPointer;
    }

    public ConfinedPointer ConfinePointer(WinitState state, WlSurface surface, WlPointer pointer)
    {
        WlArgument* args = stackalloc WlArgument[5];
        args[0].Object = 0;
        args[1].Object = surface.Value;
        args[2].Object = pointer.Value;
        args[3].Object = 0;
        args[4].Uint = (uint)ZwpPointerConstraintsV1Lifetime.Persistent;

        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _pointerConstraints,
            ZwpPointerConstraintsV1Request.ConfinePointer,
            ZwpInterfaces.ConfinedPointerV1,
            PInvoke.WlProxyGetVersion(_pointerConstraints),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zwp_pointer_constraints_v1.confine_pointer failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        ConfinedPointer confinedPointer = new(new ZwpConfinedPointerV1(proxy.Value));
        confinedPointer.InstallDispatcher();
        return confinedPointer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_pointerConstraints.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _pointerConstraints,
                ZwpPointerConstraintsV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_pointerConstraints),
                WlProxyMarshalFlags.Destroy,
                null);
            _pointerConstraints = ZwpPointerConstraintsV1.Null;
        }
    }
}

internal sealed unsafe class LockedPointer : IDisposable
{
    private ZwpLockedPointerV1 _lockedPointer;
    private bool _disposed;

    public LockedPointer(ZwpLockedPointerV1 lockedPointer)
    {
        _lockedPointer = lockedPointer;
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _lockedPointer,
            &NoopDispatcher,
            null,
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_locked_pointer_v1.");
        }
    }

    public void SetCursorPositionHint(double surfaceX, double surfaceY)
    {
        if (_lockedPointer.IsNull)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[2];
        args[0].Fixed = WlFixed.FromDouble(surfaceX).Value;
        args[1].Fixed = WlFixed.FromDouble(surfaceY).Value;
        PInvoke.WlProxyMarshalArray(
            _lockedPointer,
            ZwpLockedPointerV1Request.SetCursorPositionHint,
            args);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_lockedPointer.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _lockedPointer,
                ZwpLockedPointerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_lockedPointer),
                WlProxyMarshalFlags.Destroy,
                null);
            _lockedPointer = ZwpLockedPointerV1.Null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int NoopDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = implementation;
        _ = target;
        _ = opcode;
        _ = message;
        _ = args;
        return 0;
    }
}

internal sealed unsafe class ConfinedPointer : IDisposable
{
    private ZwpConfinedPointerV1 _confinedPointer;
    private bool _disposed;

    public ConfinedPointer(ZwpConfinedPointerV1 confinedPointer)
    {
        _confinedPointer = confinedPointer;
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _confinedPointer,
            &NoopDispatcher,
            null,
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_confined_pointer_v1.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_confinedPointer.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _confinedPointer,
                ZwpConfinedPointerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_confinedPointer),
                WlProxyMarshalFlags.Destroy,
                null);
            _confinedPointer = ZwpConfinedPointerV1.Null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int NoopDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = implementation;
        _ = target;
        _ = opcode;
        _ = message;
        _ = args;
        return 0;
    }
}
