using System.Globalization;
using System.Text;
using Winit.Core;
using Winit.Dpi;
using Winit.X11.Util;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.X11;

internal static unsafe class Monitor
{
    public static IEnumerable<CoreMonitorHandle> AvailableMonitors(XConnection xconn)
    {
        return QueryMonitorList(xconn).Select(monitor => new CoreMonitorHandle(monitor)).ToArray();
    }

    public static CoreMonitorHandle? PrimaryMonitor(XConnection xconn)
    {
        MonitorHandle? monitor = QueryMonitorList(xconn).FirstOrDefault(monitor => monitor.IsPrimary);
        monitor ??= QueryMonitorList(xconn).FirstOrDefault();
        return monitor is null ? null : new CoreMonitorHandle(monitor);
    }

    public static CoreMonitorHandle? CurrentMonitor(
        XConnection xconn,
        PhysicalPosition<int> position,
        PhysicalSize<uint> size)
    {
        List<MonitorHandle> monitors = QueryMonitorList(xconn);
        if (monitors.Count == 0)
        {
            return null;
        }

        AaRect windowRect = new(position, size);
        MonitorHandle matched = monitors[0];
        long largestOverlap = 0;

        foreach (MonitorHandle monitor in monitors)
        {
            long overlappingArea = windowRect.GetOverlappingArea(monitor.Rect);
            if (overlappingArea > largestOverlap)
            {
                largestOverlap = overlappingArea;
                matched = monitor;
            }
        }

        return new CoreMonitorHandle(matched);
    }

    public static double? ScaleFactorForWindow(
        XConnection xconn,
        PhysicalPosition<int> position,
        PhysicalSize<uint> size)
    {
        List<MonitorHandle> monitors = QueryMonitorList(xconn);
        return MatchMonitor(monitors, position, size)?.ScaleFactor;
    }

    internal static List<MonitorHandle> QueryMonitorList(XConnection xconn)
    {
        if (xconn.RandrVersion is null)
        {
            return [];
        }

        nint resourcesPtr = 0;
        try
        {
            resourcesPtr = ShouldUseCurrentScreenResources(xconn.RandrVersion.Value)
                ? PInvoke.XRRGetScreenResourcesCurrent(xconn.Display, xconn.RootWindow)
                : PInvoke.XRRGetScreenResources(xconn.Display, xconn.RootWindow);
            if (resourcesPtr == 0)
            {
                return [];
            }

            XRRScreenResources* resources = (XRRScreenResources*)resourcesPtr;
            uint primaryOutput = QueryPrimaryOutput(xconn);
            List<MonitorHandle> monitors = new(Math.Max(0, resources->NCrtc));

            ReadOnlySpan<uint> crtcs = resources->NCrtc > 0
                ? new ReadOnlySpan<uint>(resources->Crtcs, resources->NCrtc)
                : [];

            foreach (uint crtcId in crtcs)
            {
                nint crtcInfoPtr = PInvoke.XRRGetCrtcInfo(xconn.Display, resourcesPtr, crtcId);
                if (crtcInfoPtr == 0)
                {
                    continue;
                }

                try
                {
                    XRRCrtcInfo* crtc = (XRRCrtcInfo*)crtcInfoPtr;
                    MonitorHandle? monitor = CreateMonitorHandle(xconn, resources, resourcesPtr, crtcId, crtc, primaryOutput);
                    if (monitor is not null)
                    {
                        monitors.Add(monitor);
                    }
                }
                finally
                {
                    PInvoke.XRRFreeCrtcInfo(crtcInfoPtr);
                }
            }

            if (!monitors.Any(static monitor => monitor.IsPrimary) && monitors.Count > 0)
            {
                monitors[0].IsPrimary = true;
            }

            return monitors;
        }
        catch (DllNotFoundException)
        {
            return [];
        }
        catch (EntryPointNotFoundException)
        {
            return [];
        }
        finally
        {
            if (resourcesPtr != 0)
            {
                PInvoke.XRRFreeScreenResources(resourcesPtr);
            }
        }
    }

    private static MonitorHandle? MatchMonitor(
        IReadOnlyList<MonitorHandle> monitors,
        PhysicalPosition<int> position,
        PhysicalSize<uint> size)
    {
        if (monitors.Count == 0)
        {
            return null;
        }

        AaRect windowRect = new(position, size);
        MonitorHandle matched = monitors[0];
        long largestOverlap = 0;

        foreach (MonitorHandle monitor in monitors)
        {
            long overlappingArea = windowRect.GetOverlappingArea(monitor.Rect);
            if (overlappingArea > largestOverlap)
            {
                largestOverlap = overlappingArea;
                matched = monitor;
            }
        }

        return matched;
    }

    private static MonitorHandle? CreateMonitorHandle(
        XConnection xconn,
        XRRScreenResources* resources,
        nint resourcesPtr,
        uint crtcId,
        XRRCrtcInfo* crtc,
        uint primaryOutput)
    {
        if (crtc->Width == 0 || crtc->Height == 0 || crtc->NOutput == 0 || crtc->Outputs is null)
        {
            return null;
        }

        uint output = crtc->Outputs[0];
        nint outputInfoPtr = PInvoke.XRRGetOutputInfo(xconn.Display, resourcesPtr, output);
        if (outputInfoPtr == 0)
        {
            return null;
        }

        try
        {
            XRROutputInfo* outputInfo = (XRROutputInfo*)outputInfoPtr;
            if (outputInfo->Connection != PInvoke.RRConnected)
            {
                return null;
            }

            string? name = DecodeOutputName(outputInfo);
            if (name is null)
            {
                return null;
            }

            PhysicalPosition<int> position = new(crtc->X, crtc->Y);
            PhysicalSize<uint> size = new(crtc->Width, crtc->Height);
            return new MonitorHandle
            {
                NativeId = crtcId,
                Name = name,
                Position = position,
                IsPrimary = output == primaryOutput,
                ScaleFactor = GetScaleFactor(xconn, crtc, outputInfo),
                Rect = new AaRect(position, size),
                ModeHandles = GetVideoModes(xconn, resources, outputInfo, crtc->Mode),
            };
        }
        finally
        {
            PInvoke.XRRFreeOutputInfo(outputInfoPtr);
        }
    }

    private static IReadOnlyList<VideoModeHandle> GetVideoModes(
        XConnection xconn,
        XRRScreenResources* resources,
        XRROutputInfo* outputInfo,
        uint currentMode)
    {
        if (resources->NMode <= 0 || outputInfo->NMode <= 0)
        {
            return [];
        }

        ReadOnlySpan<XRRModeInfo> resourceModes = new(resources->Modes, resources->NMode);
        ReadOnlySpan<uint> outputModes = new(outputInfo->Modes, outputInfo->NMode);
        HashSet<uint> outputModeIds = [.. outputModes];
        List<VideoModeHandle> modes = [];
        ushort? bitDepth = xconn.DefaultDepth > 0 ? checked((ushort)xconn.DefaultDepth) : null;

        foreach (XRRModeInfo mode in resourceModes)
        {
            if (!outputModeIds.Contains(mode.Id))
            {
                continue;
            }

            uint? refreshRate = ModeRefreshRateMillihertz(in mode);
            VideoMode videoMode = new(
                new PhysicalSize<uint>(mode.Width, mode.Height),
                bitDepth,
                refreshRate);
            modes.Add(new VideoModeHandle(mode.Id == currentMode, videoMode, mode.Id));
        }

        return modes;
    }

    private static uint QueryPrimaryOutput(XConnection xconn)
    {
        try
        {
            return PInvoke.XRRGetOutputPrimary(xconn.Display, xconn.RootWindow);
        }
        catch (EntryPointNotFoundException)
        {
            return 0;
        }
    }

    private static bool ShouldUseCurrentScreenResources((int Major, int Minor) version)
    {
        return version.Major > 1 || (version.Major == 1 && version.Minor >= 3);
    }

    private static string? DecodeOutputName(XRROutputInfo* outputInfo)
    {
        if (outputInfo->Name is null || outputInfo->NameLen <= 0)
        {
            return string.Empty;
        }

        ReadOnlySpan<byte> bytes = new(outputInfo->Name, outputInfo->NameLen);
        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static uint? ModeRefreshRateMillihertz(in XRRModeInfo mode)
    {
        if (mode.DotClock == 0 || mode.HTotal == 0 || mode.VTotal == 0)
        {
            return null;
        }

        ulong denominator = (ulong)mode.HTotal * mode.VTotal;
        if (denominator == 0)
        {
            return null;
        }

        ulong rate = (ulong)mode.DotClock * 1000 / denominator;
        return rate == 0 ? null : checked((uint)rate);
    }

    private static double GetScaleFactor(XConnection xconn, XRRCrtcInfo* crtc, XRROutputInfo* outputInfo)
    {
        string? scaleOverride = Environment.GetEnvironmentVariable("WINIT_X11_SCALE_FACTOR");

        if (!string.IsNullOrEmpty(scaleOverride))
        {
            if (scaleOverride.Equals("randr", StringComparison.OrdinalIgnoreCase))
            {
                return Randr.CalcDpiFactor((crtc->Width, crtc->Height), (outputInfo->MmWidth, outputInfo->MmHeight));
            }

            if (!double.TryParse(scaleOverride, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale) ||
                !IsValidScaleFactor(scale))
            {
                throw new InvalidOperationException(
                    "`WINIT_X11_SCALE_FACTOR` invalid; DPI factors must be positive finite numbers or `randr`.");
            }

            return scale;
        }

        if (xconn.GetXftDpi() is { } dpi)
        {
            double xftScale = dpi / 96.0;
            if (IsValidScaleFactor(xftScale))
            {
                return xftScale;
            }
        }

        return Randr.CalcDpiFactor((crtc->Width, crtc->Height), (outputInfo->MmWidth, outputInfo->MmHeight));
    }

    private static bool IsValidScaleFactor(double scale)
    {
        return !double.IsNaN(scale) && !double.IsInfinity(scale) && scale > 0.0 && scale <= 20.0;
    }
}

internal sealed class VideoModeHandle(bool current, VideoMode mode, uint nativeMode)
{
    public bool Current { get; } = current;

    public VideoMode Mode { get; } = mode;

    public uint NativeMode { get; } = nativeMode;
}

internal sealed class MonitorHandle : IMonitorHandleProvider
{
    public UInt128 Id => NativeId;

    public ulong NativeId { get; init; }

    public string? Name { get; init; }

    public PhysicalPosition<int>? Position { get; init; }

    public double ScaleFactor { get; init; } = 1.0;

    public VideoMode? CurrentVideoMode => ModeHandles.FirstOrDefault(mode => mode.Current)?.Mode;

    public IReadOnlyList<VideoModeHandle> ModeHandles { get; init; } = [];

    public IEnumerable<VideoMode> VideoModes => ModeHandles.Select(mode => mode.Mode);

    internal bool IsPrimary { get; set; }

    internal AaRect Rect { get; init; }
}
