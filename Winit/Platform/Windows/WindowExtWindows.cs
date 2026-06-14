#if WINDOWS
using Winit.Core;

namespace Winit.Platform.Windows;

public static class WindowExtWindows
{
    public static void SetEnable(this IWindow window, bool enabled)
    {
        WindowsWindow(window).SetEnable(enabled);
    }

    public static void SetTaskbarIcon(this IWindow window, Icon? taskbarIcon)
    {
        WindowsWindow(window).SetTaskbarIcon(taskbarIcon);
    }

    public static void SetSkipTaskbar(this IWindow window, bool skip)
    {
        WindowsWindow(window).SetSkipTaskbar(skip);
    }

    public static void SetUndecoratedShadow(this IWindow window, bool shadow)
    {
        WindowsWindow(window).SetUndecoratedShadow(shadow);
    }

    public static void SetSystemBackdrop(this IWindow window, Winit.Win32.BackdropType backdropType)
    {
        WindowsWindow(window).SetSystemBackdrop(backdropType);
    }

    public static void SetBorderColor(this IWindow window, Winit.Win32.Color? color)
    {
        WindowsWindow(window).SetBorderColor(color);
    }

    public static void SetTitleBackgroundColor(this IWindow window, Winit.Win32.Color? color)
    {
        WindowsWindow(window).SetTitleBackgroundColor(color);
    }

    public static void SetTitleTextColor(this IWindow window, Winit.Win32.Color color)
    {
        WindowsWindow(window).SetTitleTextColor(color);
    }

    public static void SetCornerPreference(this IWindow window, Winit.Win32.CornerPreference preference)
    {
        WindowsWindow(window).SetCornerPreference(preference);
    }

    public static void SetUseSystemScrollSpeed(this IWindow window, bool shouldUse)
    {
        WindowsWindow(window).SetUseSystemScrollSpeed(shouldUse);
    }

    private static Winit.Win32.Window WindowsWindow(IWindow window)
    {
        if (window.AsAny() is Winit.Win32.Window windowsWindow)
        {
            return windowsWindow;
        }

        throw new PlatformNotSupportedException("Windows window extensions require the Win32 backend.");
    }
}
#endif
