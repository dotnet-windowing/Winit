#if !ANDROID
using Winit.Core;

namespace Winit.Platform.Wayland;

public static class WindowExtWayland
{
    public static bool IsWayland(this IWindow window)
    {
        return window.AsAny() is Winit.Wayland.IWindowExtWayland;
    }

    public static AsyncRequestSerial RequestActivationToken(this IWindow window)
    {
        return WaylandWindow(window).RequestActivationToken();
    }

    public static nint? XdgToplevel(this IWindow window)
    {
        return WaylandWindow(window).XdgToplevel();
    }

    public static Winit.Wayland.WaylandSurface WaylandSurface(this IWindow window)
    {
        return WaylandWindow(window).WaylandSurface();
    }

    public static Winit.Wayland.WaylandDisplay WaylandDisplay(this IWindow window)
    {
        return WaylandWindow(window).WaylandDisplay();
    }

    private static Winit.Wayland.IWindowExtWayland WaylandWindow(IWindow window)
    {
        if (window.AsAny() is Winit.Wayland.IWindowExtWayland waylandWindow)
        {
            return waylandWindow;
        }

        throw new PlatformNotSupportedException("Wayland window extensions require the Wayland backend.");
    }
}
#endif
