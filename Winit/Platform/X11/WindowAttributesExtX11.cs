#if !ANDROID
using Winit.Core;
using Winit.Dpi;

namespace Winit.Platform.X11;

public static class WindowAttributesExtX11
{
    public static WindowAttributes WithX11Visual(this WindowAttributes attributes, Winit.X11.XVisualId visualId)
    {
        return WithX11Attributes(attributes, x11 => x11.VisualId = visualId);
    }

    public static WindowAttributes WithX11Screen(this WindowAttributes attributes, int screenId)
    {
        return WithX11Attributes(attributes, x11 => x11.ScreenId = screenId);
    }

    public static WindowAttributes WithName(this WindowAttributes attributes, string general, string instance)
    {
        return WithX11Attributes(
            attributes,
            x11 =>
            {
                x11.GeneralName = general;
                x11.InstanceName = instance;
            });
    }

    public static WindowAttributes WithOverrideRedirect(this WindowAttributes attributes, bool overrideRedirect)
    {
        return WithX11Attributes(attributes, x11 => x11.OverrideRedirect = overrideRedirect);
    }

    public static WindowAttributes WithX11WindowTypes(
        this WindowAttributes attributes,
        IEnumerable<Winit.X11.WindowType> windowTypes)
    {
        return WithX11Attributes(attributes, x11 => x11.WindowTypes = windowTypes.ToList());
    }

    public static WindowAttributes WithBaseSize(this WindowAttributes attributes, Size baseSize)
    {
        return WithX11Attributes(attributes, x11 => x11.BaseSize = baseSize);
    }

    public static WindowAttributes WithEmbedParentWindow(this WindowAttributes attributes, Winit.X11.XWindow parentWindow)
    {
        return WithX11Attributes(attributes, x11 => x11.EmbedWindow = parentWindow);
    }

    public static WindowAttributes WithActivationToken(this WindowAttributes attributes, ActivationToken token)
    {
        return WithX11Attributes(attributes, x11 => x11.ActivationToken = token);
    }

    private static WindowAttributes WithX11Attributes(
        WindowAttributes attributes,
        Action<Winit.X11.WindowAttributesX11> configure)
    {
        Winit.X11.WindowAttributesX11 x11 = attributes.Platform is Winit.X11.WindowAttributesX11 existing
            ? existing.CloneX11()
            : new Winit.X11.WindowAttributesX11();

        configure(x11);
        return attributes.WithPlatformAttributes(x11);
    }
}
#endif
