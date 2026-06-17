#if !WINDOWS
using Winit.Core;

namespace Winit.Platform.Wayland;

public static class WindowAttributesExtWayland
{
    public static WindowAttributes WithName(this WindowAttributes attributes, string general, string instance)
    {
        return WithWaylandAttributes(attributes, wayland => wayland.Name = new Winit.Wayland.ApplicationName(
            general,
            instance));
    }

    public static WindowAttributes WithActivationToken(this WindowAttributes attributes, ActivationToken token)
    {
        return WithWaylandAttributes(attributes, wayland => wayland.ActivationToken = token);
    }

    public static WindowAttributes WithPreferCsd(this WindowAttributes attributes, bool preferCsd)
    {
        return WithWaylandAttributes(attributes, wayland => wayland.PreferCsd = preferCsd);
    }

    private static WindowAttributes WithWaylandAttributes(
        WindowAttributes attributes,
        Action<Winit.Wayland.WindowAttributesWayland> configure)
    {
        Winit.Wayland.WindowAttributesWayland wayland =
            attributes.Platform is Winit.Wayland.WindowAttributesWayland existing
                ? existing.CloneWayland()
                : new Winit.Wayland.WindowAttributesWayland();

        configure(wayland);
        return attributes.WithPlatformAttributes(wayland);
    }
}
#endif
