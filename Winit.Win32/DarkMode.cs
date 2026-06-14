using System.Runtime.InteropServices;
using Winit.Core;

namespace Winit.Win32;

internal static class DarkMode
{
    private const uint SpiGetHighContrast = 0x0042;
    private const uint HcfHighContrastOn = 0x00000001;

    public static Theme SystemTheme => ShouldUseDarkMode() ? Theme.Dark : Theme.Light;

    public static Theme TryTheme(HWND hwnd, Theme? preferredTheme, bool refreshTitleBar)
    {
        _ = hwnd;
        _ = refreshTitleBar;
        return preferredTheme ?? SystemTheme;
    }

    public static bool ShouldUseDarkMode()
    {
        return ShouldAppsUseDarkMode() && !IsHighContrast();
    }

    private static bool ShouldAppsUseDarkMode()
    {
        try
        {
            return PInvoke.ShouldAppsUseDarkModeNative();
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool IsHighContrast()
    {
        HighContrast highContrast = new()
        {
            Size = (uint)Marshal.SizeOf<HighContrast>(),
        };

        return PInvoke.SystemParametersInfoW(
                SpiGetHighContrast,
                highContrast.Size,
                ref highContrast,
                0)
            && (highContrast.Flags & HcfHighContrastOn) != 0;
    }
}
