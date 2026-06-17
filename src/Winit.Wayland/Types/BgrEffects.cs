namespace Winit.Wayland;

internal sealed class BgrEffectManager : IDisposable
{
    private readonly Kind _kind;
    private readonly ExtBackgroundEffectManager? _ext;
    private readonly KWinBlurManager? _kwin;
    private bool _disposed;

    private BgrEffectManager(ExtBackgroundEffectManager ext)
    {
        _kind = Kind.Ext;
        _ext = ext;
    }

    private BgrEffectManager(KWinBlurManager kwin)
    {
        _kind = Kind.KWin;
        _kwin = kwin;
    }

    public static BgrEffectManager? TryBind(WinitState state)
    {
        if (state.FindGlobal("ext_background_effect_manager_v1") is { } ext)
        {
            return new BgrEffectManager(ExtBackgroundEffectManager.Bind(state, ext));
        }

        if (state.FindGlobal("org_kde_kwin_blur_manager") is { } kwin)
        {
            return new BgrEffectManager(KWinBlurManager.Bind(state, kwin));
        }

        return null;
    }

    public SurfaceBlurEffect NewBlurEffect(WinitState state, WlSurface surface)
    {
        return _kind switch
        {
            Kind.Ext => SurfaceBlurEffect.FromExt(_ext!.Blur(state, surface)),
            Kind.KWin => SurfaceBlurEffect.FromKWin(_kwin!.Blur(state, surface), _kwin, surface),
            _ => throw new InvalidOperationException("Unknown background effect manager kind."),
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ext?.Dispose();
        _kwin?.Dispose();
    }

    private enum Kind
    {
        Ext,
        KWin,
    }
}

internal sealed class SurfaceBlurEffect : IDisposable
{
    private readonly Kind _kind;
    private readonly ExtBackgroundEffectSurface? _ext;
    private readonly KWinBlur? _kwin;
    private readonly KWinBlurManager? _kwinManager;
    private readonly WlSurface _surface;
    private bool _disposed;

    private SurfaceBlurEffect(ExtBackgroundEffectSurface ext)
    {
        _kind = Kind.Ext;
        _ext = ext;
    }

    private SurfaceBlurEffect(KWinBlur kwin, KWinBlurManager manager, WlSurface surface)
    {
        _kind = Kind.KWin;
        _kwin = kwin;
        _kwinManager = manager;
        _surface = surface;
    }

    public static SurfaceBlurEffect FromExt(ExtBackgroundEffectSurface ext)
    {
        return new SurfaceBlurEffect(ext);
    }

    public static SurfaceBlurEffect FromKWin(KWinBlur kwin, KWinBlurManager manager, WlSurface surface)
    {
        return new SurfaceBlurEffect(kwin, manager, surface);
    }

    public bool SetBlur(WlRegion region)
    {
        switch (_kind)
        {
            case Kind.Ext:
                _ext!.SetBlurRegion(region);
                return true;
            case Kind.KWin:
                _kwin!.SetRegion(region);
                _kwin.Commit();
                return true;
            default:
                return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        switch (_kind)
        {
            case Kind.Ext:
                _ext?.Dispose();
                break;
            case Kind.KWin:
                _kwin?.ClearRegion();
                _kwin?.Commit();
                _kwin?.Dispose();
                if (!_surface.IsNull)
                {
                    _kwinManager?.Unset(_surface);
                }
                break;
        }
    }

    private enum Kind
    {
        Ext,
        KWin,
    }
}
