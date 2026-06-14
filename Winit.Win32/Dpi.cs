namespace Winit.Win32;

internal static class Dpi
{
    public const uint BaseDpi = 96;
    private static int s_dpiAwarenessSet;

    public static double DpiToScaleFactor(uint dpi)
    {
        return (double)dpi / BaseDpi;
    }

    public static unsafe uint HwndDpi(HWND hwnd)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
        {
            return BaseDpi;
        }

        uint dpi = PInvoke.GetDpiForWindow(hwnd);
        return dpi == 0 ? BaseDpi : dpi;
    }

    public static void BecomeDpiAware()
    {
        if (Interlocked.Exchange(ref s_dpiAwarenessSet, 1) != 0)
        {
            return;
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 15063))
        {
            if (PInvoke.SetProcessDpiAwarenessContext(new DPI_AWARENESS_CONTEXT(-4)))
            {
                return;
            }

            PInvoke.SetProcessDpiAwarenessContext(new DPI_AWARENESS_CONTEXT(-3));
            return;
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(6))
        {
            PInvoke.SetProcessDPIAware();
        }
    }

    public static unsafe uint? MonitorDpi(HMONITOR monitor)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
        {
            return null;
        }

        uint dpiX;
        uint dpiY;
        HRESULT result = PInvoke.GetDpiForMonitor(
            monitor,
            MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
            &dpiX,
            &dpiY);

        return result.Succeeded ? dpiX : null;
    }
}
