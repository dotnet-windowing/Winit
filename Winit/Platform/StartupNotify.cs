#if !ANDROID
using Winit.Core;

namespace Winit.Platform.StartupNotify;

public static class EventLoopExtStartupNotify
{
    private const string X11Variable = "DESKTOP_STARTUP_ID";
    private const string WaylandVariable = "XDG_ACTIVATION_TOKEN";

    public static ActivationToken? ReadTokenFromEnv(this IActiveEventLoop eventLoop)
    {
        string variable = eventLoop.AsAny() is Winit.Wayland.IActiveEventLoopExtWayland
            ? WaylandVariable
            : X11Variable;
        string? token = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrEmpty(token) ? null : ActivationToken.FromRaw(token);
    }

    public static void ResetActivationTokenEnv()
    {
        Environment.SetEnvironmentVariable(X11Variable, null);
        Environment.SetEnvironmentVariable(WaylandVariable, null);
    }

    public static void SetActivationTokenEnv(ActivationToken token)
    {
        string raw = token.AsRaw();
        Environment.SetEnvironmentVariable(X11Variable, raw);
        Environment.SetEnvironmentVariable(WaylandVariable, raw);
    }
}

public static class WindowExtStartupNotify
{
    public static AsyncRequestSerial RequestActivationToken(this IWindow window)
    {
        object backend = window.AsAny();
        if (backend is Winit.Wayland.IWindowExtWayland waylandWindow)
        {
            return waylandWindow.RequestActivationToken();
        }

        if (backend is Winit.X11.IWindowExtX11 x11Window)
        {
            return x11Window.RequestActivationToken();
        }

        throw new PlatformNotSupportedException("startup notify is not supported by this window backend.");
    }
}

public static class WindowAttributesExtStartupNotify
{
    public static WindowAttributes WithActivationToken(this WindowAttributes attributes, ActivationToken token)
    {
        if (attributes.Platform is Winit.Wayland.WindowAttributesWayland wayland)
        {
            Winit.Wayland.WindowAttributesWayland clone = wayland.CloneWayland();
            clone.ActivationToken = token;
            return attributes.WithPlatformAttributes(clone);
        }

        if (attributes.Platform is Winit.X11.WindowAttributesX11 x11)
        {
            Winit.X11.WindowAttributesX11 clone = x11.CloneX11();
            clone.ActivationToken = token;
            return attributes.WithPlatformAttributes(clone);
        }

        if (HasNonEmptyEnvironment("WAYLAND_DISPLAY") ||
            HasNonEmptyEnvironment("WAYLAND_SOCKET") ||
            HasNonEmptyEnvironment("XDG_ACTIVATION_TOKEN"))
        {
            return attributes.WithPlatformAttributes(new Winit.Wayland.WindowAttributesWayland
            {
                ActivationToken = token,
            });
        }

        if (HasNonEmptyEnvironment("DISPLAY") || HasNonEmptyEnvironment("DESKTOP_STARTUP_ID"))
        {
            return attributes.WithPlatformAttributes(new Winit.X11.WindowAttributesX11
            {
                ActivationToken = token,
            });
        }

        return attributes.WithPlatformAttributes(new Winit.Wayland.WindowAttributesWayland
        {
            ActivationToken = token,
        });
    }

    private static bool HasNonEmptyEnvironment(string variable)
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable));
    }
}
#endif
