#if !ANDROID
using Winit.Core;

namespace Winit.Platform.Windows;

public static class DeviceIdExtWindows
{
    public static string? PersistentIdentifier(this DeviceId deviceId)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows device identifiers require the Win32 backend.");
        }

        return Winit.Win32.DeviceIdExtWindows.PersistentIdentifier(deviceId);
    }
}
#endif
