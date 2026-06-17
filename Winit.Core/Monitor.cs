using Winit.Dpi;

namespace Winit.Core;

public sealed class MonitorHandle(IMonitorHandleProvider provider) : IEquatable<MonitorHandle>
{
    public IMonitorHandleProvider Provider { get; } = provider;

    public UInt128 Id => Provider.Id;

    public ulong NativeId => Provider.NativeId;

    public string? Name => Provider.Name;

    public PhysicalPosition<int>? Position => Provider.Position;

    public double ScaleFactor => Provider.ScaleFactor;

    public VideoMode? CurrentVideoMode => Provider.CurrentVideoMode;

    public IEnumerable<VideoMode> VideoModes => Provider.VideoModes;

    public bool Equals(MonitorHandle? other)
    {
        return other is not null && Provider.Id == other.Provider.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is MonitorHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Provider.Id.GetHashCode();
    }
}

public interface IMonitorHandleProvider : IAsAny
{
    UInt128 Id { get; }

    ulong NativeId { get; }

    string? Name { get; }

    PhysicalPosition<int>? Position { get; }

    double ScaleFactor { get; }

    VideoMode? CurrentVideoMode { get; }

    IEnumerable<VideoMode> VideoModes { get; }
}

public readonly record struct VideoMode
{
    public VideoMode(PhysicalSize<uint> size, ushort? bitDepth, uint? refreshRateMillihertz)
    {
        if (bitDepth == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitDepth), bitDepth, "Bit depth must be non-zero when present.");
        }

        if (refreshRateMillihertz == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(refreshRateMillihertz),
                refreshRateMillihertz,
                "Refresh rate must be non-zero when present.");
        }

        Size = size;
        BitDepth = bitDepth;
        RefreshRateMillihertz = refreshRateMillihertz;
    }

    public PhysicalSize<uint> Size { get; }

    public ushort? BitDepth { get; }

    public uint? RefreshRateMillihertz { get; }

    public override string ToString()
    {
        string refreshRate = RefreshRateMillihertz is { } rate ? $"@ {rate} mHz " : string.Empty;
        string bitDepth = BitDepth is { } depth ? $"({depth} bpp)" : string.Empty;
        return $"{Size.Width}x{Size.Height} {refreshRate}{bitDepth}";
    }
}

public record struct Fullscreen
{
    public readonly record struct Exclusive(MonitorHandle Monitor, VideoMode VideoMode);

    public readonly record struct Borderless(MonitorHandle? Monitor);

    private const byte ExclusiveTag = 0;
    private const byte BorderlessTag = 1;

    private byte _tag;
    private Exclusive _exclusive;
    private Borderless _borderless;

    public Fullscreen(Exclusive value)
    {
        this = default;
        _tag = ExclusiveTag;
        _exclusive = value;
    }

    public Fullscreen(Borderless value)
    {
        this = default;
        _tag = BorderlessTag;
        _borderless = value;
    }

    public static Fullscreen FromExclusive(MonitorHandle monitor, VideoMode videoMode)
    {
        return new Fullscreen(new Exclusive(monitor, videoMode));
    }

    public static Fullscreen FromBorderless(MonitorHandle? monitor = null)
    {
        return new Fullscreen(new Borderless(monitor));
    }

    public bool TryGetValue(out Exclusive value)
    {
        value = _exclusive;
        return _tag == ExclusiveTag;
    }

    public bool TryGetValue(out Borderless value)
    {
        value = _borderless;
        return _tag == BorderlessTag;
    }
}
