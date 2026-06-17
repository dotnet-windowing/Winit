namespace Winit.Wayland;

internal sealed unsafe class KWinBlurManager : IDisposable
{
    private OrgKdeKwinBlurManager _manager;
    private bool _disposed;

    private KWinBlurManager(OrgKdeKwinBlurManager manager)
    {
        _manager = manager;
    }

    public static KWinBlurManager Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, OrgKdeInterfaces.KwinBlurManager, maxVersion: 1);
        return new KWinBlurManager(new OrgKdeKwinBlurManager(proxy.Value));
    }

    public KWinBlur Blur(WinitState state, WlSurface surface)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = surface.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            OrgKdeKwinBlurManagerRequest.Create,
            OrgKdeInterfaces.KwinBlur,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("org_kde_kwin_blur_manager.create failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new KWinBlur(new OrgKdeKwinBlur(proxy.Value));
    }

    public void Unset(WlSurface surface)
    {
        if (_manager.IsNull)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = surface.Value;
        PInvoke.WlProxyMarshalArray(_manager, OrgKdeKwinBlurManagerRequest.Unset, args);
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
            PInvoke.WlProxyDestroy(_manager);
            _manager = OrgKdeKwinBlurManager.Null;
        }
    }
}

internal sealed unsafe class KWinBlur : IDisposable
{
    private OrgKdeKwinBlur _blur;
    private bool _disposed;

    public KWinBlur(OrgKdeKwinBlur blur)
    {
        _blur = blur;
    }

    public void SetRegion(WlRegion region)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = region.Value;
        PInvoke.WlProxyMarshalArray(_blur, OrgKdeKwinBlurRequest.SetRegion, args);
    }

    public void ClearRegion()
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        PInvoke.WlProxyMarshalArray(_blur, OrgKdeKwinBlurRequest.SetRegion, args);
    }

    public void Commit()
    {
        PInvoke.WlProxyMarshalArray(_blur, OrgKdeKwinBlurRequest.Commit, null);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_blur.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _blur,
                OrgKdeKwinBlurRequest.Release,
                null,
                PInvoke.WlProxyGetVersion(_blur),
                WlProxyMarshalFlags.Destroy,
                null);
            _blur = OrgKdeKwinBlur.Null;
        }
    }
}
