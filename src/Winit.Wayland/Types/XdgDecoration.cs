using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.Wayland;

internal sealed unsafe class XdgDecorationManagerState : IDisposable
{
    private ZxdgDecorationManagerV1 _manager;
    private bool _disposed;

    private XdgDecorationManagerState(ZxdgDecorationManagerV1 manager)
    {
        _manager = manager;
    }

    public static XdgDecorationManagerState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, ZxdgDecorationInterfaces.ManagerV1, maxVersion: 2);
        return new XdgDecorationManagerState(new ZxdgDecorationManagerV1(proxy.Value));
    }

    public ToplevelDecoration GetToplevelDecoration(WinitState state, XdgToplevel toplevel, Window window)
    {
        if (_manager.IsNull)
        {
            throw new ObjectDisposedException(nameof(XdgDecorationManagerState));
        }

        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = toplevel.Value;

        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            ZxdgDecorationManagerV1Request.GetToplevelDecoration,
            ZxdgDecorationInterfaces.ToplevelDecorationV1,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zxdg_decoration_manager_v1.get_toplevel_decoration failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        ToplevelDecoration decoration = new(new ZxdgToplevelDecorationV1(proxy.Value), window);
        decoration.InstallDispatcher();
        return decoration;
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
                ZxdgDecorationManagerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = ZxdgDecorationManagerV1.Null;
        }
    }
}

internal sealed unsafe class ToplevelDecoration : IDisposable
{
    private readonly Window _window;
    private readonly GCHandle _selfHandle;
    private ZxdgToplevelDecorationV1 _decoration;
    private bool _disposed;

    public ToplevelDecoration(ZxdgToplevelDecorationV1 decoration, Window window)
    {
        _decoration = decoration;
        _window = window;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void SetMode(ZxdgToplevelDecorationV1Mode mode)
    {
        if (_decoration.IsNull)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[1];
        args[0].Uint = (uint)mode;
        PInvoke.WlProxyMarshalArray(_decoration, ZxdgToplevelDecorationV1Request.SetMode, args);
    }

    public void UnsetMode()
    {
        if (!_decoration.IsNull)
        {
            PInvoke.WlProxyMarshalArray(_decoration, ZxdgToplevelDecorationV1Request.UnsetMode, null);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_decoration.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _decoration,
                ZxdgToplevelDecorationV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_decoration),
                WlProxyMarshalFlags.Destroy,
                null);
            _decoration = ZxdgToplevelDecorationV1.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    internal void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _decoration,
            &DecorationDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zxdg_toplevel_decoration_v1.");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int DecorationDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not ToplevelDecoration decoration ||
            decoration._disposed)
        {
            return 0;
        }

        if (opcode == ZxdgToplevelDecorationV1Event.Configure)
        {
            decoration._window.HandleDecorationConfigure((ZxdgToplevelDecorationV1Mode)args[0].Uint);
        }

        return 0;
    }
}
