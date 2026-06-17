#if WINDOWS
using Winit.Core;

namespace Winit.Platform.Windows;

public static class DeviceIdExtWindows
{
    public static string? PersistentIdentifier(this DeviceId deviceId)
    {
        return Winit.Win32.DeviceIdExtWindows.PersistentIdentifier(deviceId);
    }
}
#endif
