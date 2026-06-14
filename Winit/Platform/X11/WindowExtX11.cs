#if !WINDOWS
using Winit.Core;

namespace Winit.Platform.X11;

public static class WindowExtX11
{
    public static bool IsX11(this IWindow window)
    {
        return window.AsAny() is Winit.X11.IWindowExtX11;
    }

    public static AsyncRequestSerial RequestActivationToken(this IWindow window)
    {
        return X11Window(window).RequestActivationToken();
    }

    private static Winit.X11.IWindowExtX11 X11Window(IWindow window)
    {
        if (window.AsAny() is Winit.X11.IWindowExtX11 x11Window)
        {
            return x11Window;
        }

        throw new PlatformNotSupportedException("X11 window extensions require the X11 backend.");
    }
}
#endif
