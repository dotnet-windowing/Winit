using System.Drawing;
using System.Runtime.InteropServices;
using Winit.Core;
using Winit.Dpi;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.Win32;

internal static unsafe class Monitor
{
    private static readonly MONITORENUMPROC s_monitorEnumProc = MonitorEnumProc;

    public static IEnumerable<CoreMonitorHandle> AvailableMonitors()
    {
        List<CoreMonitorHandle> monitors = [];
        GCHandle data = GCHandle.Alloc(monitors);
        try
        {
            PInvoke.EnumDisplayMonitors(
                HDC.Null,
                null,
                s_monitorEnumProc,
                new LPARAM(GCHandle.ToIntPtr(data)));
            return monitors;
        }
        finally
        {
            data.Free();
        }
    }

    public static CoreMonitorHandle PrimaryMonitor()
    {
        HMONITOR monitor = PInvoke.MonitorFromPoint(new Point(0, 0), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        return new CoreMonitorHandle(new MonitorHandle(monitor));
    }

    public static CoreMonitorHandle CurrentMonitor(HWND hwnd)
    {
        HMONITOR monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        return new CoreMonitorHandle(new MonitorHandle(monitor));
    }

    internal static MONITORINFOEXW? GetMonitorInfo(HMONITOR monitor)
    {
        MONITORINFOEXW info = new();
        info.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

        return PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info) ? info : null;
    }

    private static BOOL MonitorEnumProc(HMONITOR monitor, HDC hdc, RECT* place, LPARAM data)
    {
        GCHandle handle = GCHandle.FromIntPtr(data.Value);
        if (handle.Target is List<CoreMonitorHandle> monitors)
        {
            monitors.Add(new CoreMonitorHandle(new MonitorHandle(monitor)));
        }

        return true;
    }
}

internal sealed unsafe class MonitorHandle : IMonitorHandleProvider, IEquatable<MonitorHandle>
{
    private readonly HMONITOR _monitor;

    public MonitorHandle(HMONITOR monitor)
    {
        _monitor = monitor;
    }

    public HMONITOR Raw => _monitor;

    public UInt128 Id => NativeId;

    public ulong NativeId => unchecked((ulong)(nuint)_monitor.Value);

    public string? Name
    {
        get
        {
            MONITORINFOEXW? info = Monitor.GetMonitorInfo(_monitor);
            return info is { } value ? DecodeDeviceName(value) : null;
        }
    }

    public PhysicalPosition<int>? Position
    {
        get
        {
            MONITORINFOEXW? info = Monitor.GetMonitorInfo(_monitor);
            if (info is null)
            {
                return null;
            }

            RECT rect = info.Value.monitorInfo.rcMonitor;
            return new PhysicalPosition<int>(rect.left, rect.top);
        }
    }

    public double ScaleFactor => Dpi.DpiToScaleFactor(Dpi.MonitorDpi(_monitor) ?? Dpi.BaseDpi);

    public VideoMode? CurrentVideoMode
    {
        get
        {
            string? deviceName = Name;
            if (deviceName is null)
            {
                return null;
            }

            return GetDisplaySettings(deviceName, ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS);
        }
    }

    public IEnumerable<VideoMode> VideoModes
    {
        get
        {
            string? deviceName = Name;
            if (deviceName is null)
            {
                return [];
            }

            HashSet<VideoMode> modes = [];
            for (uint index = 0; ; index++)
            {
                VideoMode? mode = GetDisplaySettings(deviceName, (ENUM_DISPLAY_SETTINGS_MODE)index);
                if (mode is null)
                {
                    break;
                }

                modes.Add(mode.Value);
            }

            return modes;
        }
    }

    public bool Equals(MonitorHandle? other)
    {
        return other is not null && _monitor == other._monitor;
    }

    public override bool Equals(object? obj)
    {
        return obj is MonitorHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _monitor.GetHashCode();
    }

    private static VideoMode? GetDisplaySettings(string deviceName, ENUM_DISPLAY_SETTINGS_MODE modeNumber)
    {
        DEVMODEW mode = new()
        {
            dmSize = (ushort)Marshal.SizeOf<DEVMODEW>(),
        };

        bool ok = PInvoke.EnumDisplaySettingsEx(
            deviceName,
            modeNumber,
            ref mode,
            ENUM_DISPLAY_SETTINGS_FLAGS.EDS_RAWMODE);
        if (!ok)
        {
            return null;
        }

        uint width = mode.dmPelsWidth;
        uint height = mode.dmPelsHeight;
        if (width == 0 || height == 0)
        {
            return null;
        }

        ushort? bitDepth = mode.dmBitsPerPel == 0 ? null : checked((ushort)mode.dmBitsPerPel);
        uint? refreshRate = mode.dmDisplayFrequency == 0
            ? null
            : checked(mode.dmDisplayFrequency * 1000u);

        return new VideoMode(new PhysicalSize<uint>(width, height), bitDepth, refreshRate);
    }

    private static string DecodeDeviceName(MONITORINFOEXW info)
    {
        char* deviceName = info.szDevice;
        int length = 0;
        while (length < 32 && deviceName[length] != '\0')
        {
            length++;
        }

        return new string(deviceName, 0, length);
    }
}
