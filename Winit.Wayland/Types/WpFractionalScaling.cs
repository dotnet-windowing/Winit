using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.Wayland;

internal sealed unsafe class FractionalScalingManager : IDisposable
{
    private WpFractionalScaleManagerV1 _manager;
    private bool _disposed;

    private FractionalScalingManager(WpFractionalScaleManagerV1 manager)
    {
        _manager = manager;
    }

    public static FractionalScalingManager Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, WpInterfaces.FractionalScaleManagerV1, maxVersion: 1);
        return new FractionalScalingManager(new WpFractionalScaleManagerV1(proxy.Value));
    }

    public FractionalScaling GetFractionalScale(WinitState state, WlSurface surface)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = surface.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            WpFractionalScaleManagerV1Request.GetFractionalScale,
            WpInterfaces.FractionalScaleV1,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wp_fractional_scale_manager_v1.get_fractional_scale failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        FractionalScaling fractionalScaling = new(state, surface, new WpFractionalScaleV1(proxy.Value));
        fractionalScaling.InstallDispatcher();
        return fractionalScaling;
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
                WpFractionalScaleManagerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = WpFractionalScaleManagerV1.Null;
        }
    }
}

internal sealed unsafe class FractionalScaling : IDisposable
{
    private const double ScaleDenominator = 120.0;

    private readonly WinitState _state;
    private readonly WlSurface _surface;
    private readonly GCHandle _selfHandle;
    private WpFractionalScaleV1 _fractionalScale;
    private bool _disposed;

    public FractionalScaling(WinitState state, WlSurface surface, WpFractionalScaleV1 fractionalScale)
    {
        _state = state;
        _surface = surface;
        _fractionalScale = fractionalScale;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _fractionalScale,
            &FractionalScaleDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wp_fractional_scale_v1.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_fractionalScale.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _fractionalScale,
                WpFractionalScaleV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_fractionalScale),
                WlProxyMarshalFlags.Destroy,
                null);
            _fractionalScale = WpFractionalScaleV1.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void PreferredScale(uint scale)
    {
        _state.ScaleFactorChanged(_surface, scale / ScaleDenominator, isLegacy: false);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int FractionalScaleDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not FractionalScaling scaling || scaling._disposed)
        {
            return 0;
        }

        if (opcode == WpFractionalScaleV1Event.PreferredScale)
        {
            scaling.PreferredScale(args[0].Uint);
        }

        return 0;
    }
}
