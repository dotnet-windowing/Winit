#if WINDOWS
using Winit.Core;

namespace Winit.Platform.Windows;

public static class WindowAttributesExtWindows
{
    public static WindowAttributes WithOwnerWindow(this WindowAttributes attributes, nint owner)
    {
        return WithWindowsAttributes(attributes, windows => windows.Owner = owner);
    }

    public static WindowAttributes WithMenu(this WindowAttributes attributes, nint menu)
    {
        return WithWindowsAttributes(attributes, windows => windows.Menu = menu);
    }

    public static WindowAttributes WithTaskbarIcon(this WindowAttributes attributes, Icon? taskbarIcon)
    {
        return WithWindowsAttributes(attributes, windows => windows.TaskbarIcon = taskbarIcon);
    }

    public static WindowAttributes WithNoRedirectionBitmap(this WindowAttributes attributes, bool flag)
    {
        return WithWindowsAttributes(attributes, windows => windows.NoRedirectionBitmap = flag);
    }

    public static WindowAttributes WithDragAndDrop(this WindowAttributes attributes, bool flag)
    {
        return WithWindowsAttributes(attributes, windows => windows.DragAndDrop = flag);
    }

    public static WindowAttributes WithSkipTaskbar(this WindowAttributes attributes, bool skip)
    {
        return WithWindowsAttributes(attributes, windows => windows.SkipTaskbar = skip);
    }

    public static WindowAttributes WithClassName(this WindowAttributes attributes, string className)
    {
        return WithWindowsAttributes(attributes, windows => windows.ClassName = className);
    }

    public static WindowAttributes WithUndecoratedShadow(this WindowAttributes attributes, bool shadow)
    {
        return WithWindowsAttributes(attributes, windows => windows.DecorationShadow = shadow);
    }

    public static WindowAttributes WithSystemBackdrop(
        this WindowAttributes attributes,
        Winit.Win32.BackdropType backdropType)
    {
        return WithWindowsAttributes(attributes, windows => windows.BackdropType = backdropType);
    }

    public static WindowAttributes WithClipChildren(this WindowAttributes attributes, bool flag)
    {
        return WithWindowsAttributes(attributes, windows => windows.ClipChildren = flag);
    }

    public static WindowAttributes WithBorderColor(this WindowAttributes attributes, Winit.Win32.Color? color)
    {
        return WithWindowsAttributes(attributes, windows => windows.BorderColor = color ?? Winit.Win32.Color.None);
    }

    public static WindowAttributes WithTitleBackgroundColor(
        this WindowAttributes attributes,
        Winit.Win32.Color? color)
    {
        return WithWindowsAttributes(
            attributes,
            windows => windows.TitleBackgroundColor = color ?? Winit.Win32.Color.None);
    }

    public static WindowAttributes WithTitleTextColor(this WindowAttributes attributes, Winit.Win32.Color color)
    {
        return WithWindowsAttributes(attributes, windows => windows.TitleTextColor = color);
    }

    public static WindowAttributes WithCornerPreference(
        this WindowAttributes attributes,
        Winit.Win32.CornerPreference preference)
    {
        return WithWindowsAttributes(attributes, windows => windows.CornerPreference = preference);
    }

    public static WindowAttributes WithUseSystemScrollSpeed(this WindowAttributes attributes, bool shouldUse)
    {
        return WithWindowsAttributes(attributes, windows => windows.UseSystemWheelSpeed = shouldUse);
    }

    private static WindowAttributes WithWindowsAttributes(
        WindowAttributes attributes,
        Action<Winit.Win32.WindowAttributesWindows> configure)
    {
        Winit.Win32.WindowAttributesWindows windows = attributes.Platform is Winit.Win32.WindowAttributesWindows existing
            ? existing.CloneWindows()
            : new Winit.Win32.WindowAttributesWindows();
        configure(windows);
        return attributes.WithPlatformAttributes(windows);
    }
}
#endif
