namespace Winit.Wayland;

internal sealed unsafe class ViewporterState : IDisposable
{
    private WpViewporter _viewporter;
    private bool _disposed;

    private ViewporterState(WpViewporter viewporter)
    {
        _viewporter = viewporter;
    }

    public static ViewporterState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, WpInterfaces.Viewporter, maxVersion: 1);
        return new ViewporterState(new WpViewporter(proxy.Value));
    }

    public WaylandViewport GetViewport(WinitState state, WlSurface surface)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = surface.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _viewporter,
            WpViewporterRequest.GetViewport,
            WpInterfaces.Viewport,
            PInvoke.WlProxyGetVersion(_viewporter),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wp_viewporter.get_viewport failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new WaylandViewport(new WpViewport(proxy.Value));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_viewporter.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _viewporter,
                WpViewporterRequest.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_viewporter),
                WlProxyMarshalFlags.Destroy,
                null);
            _viewporter = WpViewporter.Null;
        }
    }
}

internal sealed unsafe class WaylandViewport : IDisposable
{
    private WpViewport _viewport;
    private bool _disposed;

    public WaylandViewport(WpViewport viewport)
    {
        _viewport = viewport;
    }

    public void SetDestination(int width, int height)
    {
        if (_viewport.IsNull || width <= 0 || height <= 0)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[2];
        args[0].Int = width;
        args[1].Int = height;
        PInvoke.WlProxyMarshalArray(_viewport, WpViewportRequest.SetDestination, args);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_viewport.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _viewport,
                WpViewportRequest.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_viewport),
                WlProxyMarshalFlags.Destroy,
                null);
            _viewport = WpViewport.Null;
        }
    }
}
