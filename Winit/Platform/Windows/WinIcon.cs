#if !ANDROID
using Winit.Dpi;

namespace Winit.Platform.Windows;

public static class WinIcon
{
    public static Winit.Win32.WinIcon FromPath(string path, PhysicalSize<uint>? size = null)
    {
        EnsureWindows();
        return Winit.Win32.WinIcon.FromPath(path, size);
    }

    public static Winit.Win32.WinIcon FromResource(ushort resourceId, PhysicalSize<uint>? size = null)
    {
        EnsureWindows();
        return Winit.Win32.WinIcon.FromResource(resourceId, size);
    }

    public static Winit.Win32.WinIcon FromResourceName(string resourceName, PhysicalSize<uint>? size = null)
    {
        EnsureWindows();
        return Winit.Win32.WinIcon.FromResourceName(resourceName, size);
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("WinIcon requires Windows.");
        }
    }
}
#endif
