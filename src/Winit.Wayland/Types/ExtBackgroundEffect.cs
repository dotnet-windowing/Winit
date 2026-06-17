using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.Wayland;

internal sealed unsafe class ExtBackgroundEffectManager : IDisposable
{
    private readonly GCHandle _selfHandle;
    private ExtBackgroundEffectManagerV1 _manager;
    private ExtBackgroundEffectManagerV1Capability _capabilities;
    private bool _disposed;

    private ExtBackgroundEffectManager(ExtBackgroundEffectManagerV1 manager)
    {
        _manager = manager;
        _selfHandle = GCHandle.Alloc(this);
    }

    public static ExtBackgroundEffectManager Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, ExtInterfaces.BackgroundEffectManagerV1, maxVersion: 1);
        ExtBackgroundEffectManager manager = new(new ExtBackgroundEffectManagerV1(proxy.Value));
        manager.InstallDispatcher();
        return manager;
    }

    public ExtBackgroundEffectSurface Blur(WinitState state, WlSurface surface)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = surface.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            ExtBackgroundEffectManagerV1Request.GetBackgroundEffect,
            ExtInterfaces.BackgroundEffectSurfaceV1,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("ext_background_effect_manager_v1.get_background_effect failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new ExtBackgroundEffectSurface(new ExtBackgroundEffectSurfaceV1(proxy.Value));
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
                ExtBackgroundEffectManagerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = ExtBackgroundEffectManagerV1.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _manager,
            &ManagerDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for ext_background_effect_manager_v1.");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int ManagerDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not ExtBackgroundEffectManager manager ||
            manager._disposed)
        {
            return 0;
        }

        if (opcode == ExtBackgroundEffectManagerV1Event.Capabilities)
        {
            manager._capabilities = (ExtBackgroundEffectManagerV1Capability)args[0].Uint;
        }

        return 0;
    }
}

internal sealed unsafe class ExtBackgroundEffectSurface : IDisposable
{
    private ExtBackgroundEffectSurfaceV1 _surface;
    private bool _disposed;

    public ExtBackgroundEffectSurface(ExtBackgroundEffectSurfaceV1 surface)
    {
        _surface = surface;
    }

    public void SetBlurRegion(WlRegion region)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = region.Value;
        PInvoke.WlProxyMarshalArray(_surface, ExtBackgroundEffectSurfaceV1Request.SetBlurRegion, args);
    }

    public void ClearBlurRegion()
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        PInvoke.WlProxyMarshalArray(_surface, ExtBackgroundEffectSurfaceV1Request.SetBlurRegion, args);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_surface.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _surface,
                ExtBackgroundEffectSurfaceV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_surface),
                WlProxyMarshalFlags.Destroy,
                null);
            _surface = ExtBackgroundEffectSurfaceV1.Null;
        }
    }
}
