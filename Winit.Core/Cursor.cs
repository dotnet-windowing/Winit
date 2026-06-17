namespace Winit.Core;

public enum CursorIcon
{
    Default,
    ContextMenu,
    Help,
    Pointer,
    Progress,
    Wait,
    Cell,
    Crosshair,
    Text,
    VerticalText,
    Alias,
    Copy,
    Move,
    NoDrop,
    NotAllowed,
    Grab,
    Grabbing,
    EResize,
    NResize,
    NeResize,
    NwResize,
    SResize,
    SeResize,
    SwResize,
    WResize,
    EwResize,
    NsResize,
    NeswResize,
    NwseResize,
    ColResize,
    RowResize,
    AllScroll,
    ZoomIn,
    ZoomOut,
}

public static class CursorIconExtensions
{
    public static string Name(this CursorIcon icon)
    {
        return icon switch
        {
            CursorIcon.Default => "default",
            CursorIcon.ContextMenu => "context-menu",
            CursorIcon.Help => "help",
            CursorIcon.Pointer => "pointer",
            CursorIcon.Progress => "progress",
            CursorIcon.Wait => "wait",
            CursorIcon.Cell => "cell",
            CursorIcon.Crosshair => "crosshair",
            CursorIcon.Text => "text",
            CursorIcon.VerticalText => "vertical-text",
            CursorIcon.Alias => "alias",
            CursorIcon.Copy => "copy",
            CursorIcon.Move => "move",
            CursorIcon.NoDrop => "no-drop",
            CursorIcon.NotAllowed => "not-allowed",
            CursorIcon.Grab => "grab",
            CursorIcon.Grabbing => "grabbing",
            CursorIcon.EResize => "e-resize",
            CursorIcon.NResize => "n-resize",
            CursorIcon.NeResize => "ne-resize",
            CursorIcon.NwResize => "nw-resize",
            CursorIcon.SResize => "s-resize",
            CursorIcon.SeResize => "se-resize",
            CursorIcon.SwResize => "sw-resize",
            CursorIcon.WResize => "w-resize",
            CursorIcon.EwResize => "ew-resize",
            CursorIcon.NsResize => "ns-resize",
            CursorIcon.NeswResize => "nesw-resize",
            CursorIcon.NwseResize => "nwse-resize",
            CursorIcon.ColResize => "col-resize",
            CursorIcon.RowResize => "row-resize",
            CursorIcon.AllScroll => "all-scroll",
            CursorIcon.ZoomIn => "zoom-in",
            CursorIcon.ZoomOut => "zoom-out",
            _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, null),
        };
    }

    public static IReadOnlyList<string> AltNames(this CursorIcon icon)
    {
        return icon switch
        {
            CursorIcon.Default => ["left_ptr", "arrow", "top_left_arrow", "left_arrow"],
            CursorIcon.Help => ["question_arrow", "whats_this"],
            CursorIcon.Pointer => ["hand2", "hand1", "hand", "pointing_hand"],
            CursorIcon.Progress => ["left_ptr_watch", "half-busy"],
            CursorIcon.Wait => ["watch"],
            CursorIcon.Cell => ["plus"],
            CursorIcon.Crosshair => ["cross"],
            CursorIcon.Text => ["xterm", "ibeam"],
            CursorIcon.Alias => ["link"],
            CursorIcon.NoDrop => ["circle"],
            CursorIcon.NotAllowed => ["crossed_circle", "forbidden"],
            CursorIcon.Grab => ["openhand", "fleur"],
            CursorIcon.Grabbing => ["closedhand"],
            CursorIcon.EResize => ["right_side"],
            CursorIcon.NResize => ["top_side"],
            CursorIcon.NeResize => ["top_right_corner"],
            CursorIcon.NwResize => ["top_left_corner"],
            CursorIcon.SResize => ["bottom_side"],
            CursorIcon.SeResize => ["bottom_right_corner"],
            CursorIcon.SwResize => ["bottom_left_corner"],
            CursorIcon.WResize => ["left_side"],
            CursorIcon.EwResize => ["h_double_arrow", "size_hor"],
            CursorIcon.NsResize => ["v_double_arrow", "size_ver"],
            CursorIcon.NeswResize => ["fd_double_arrow", "size_bdiag"],
            CursorIcon.NwseResize => ["bd_double_arrow", "size_fdiag"],
            CursorIcon.ColResize => ["split_h", "h_double_arrow", "sb_h_double_arrow"],
            CursorIcon.RowResize => ["split_v", "v_double_arrow", "sb_v_double_arrow"],
            CursorIcon.AllScroll => ["size_all"],
            _ => [],
        };
    }

    public static bool TryParseCursorIcon(string name, out CursorIcon icon)
    {
        foreach (CursorIcon candidate in Enum.GetValues<CursorIcon>())
        {
            if (candidate.Name() == name || candidate.AltNames().Contains(name))
            {
                icon = candidate;
                return true;
            }
        }

        icon = default;
        return false;
    }
}

public record struct Cursor
{
    public readonly record struct Icon(CursorIcon Value);

    public readonly record struct Custom(CustomCursor Value);

    private const byte IconTag = 0;
    private const byte CustomTag = 1;

    private byte _tag;
    private Icon _icon;
    private Custom _custom;

    public Cursor(Icon value)
    {
        this = default;
        _tag = IconTag;
        _icon = value;
    }

    public Cursor(Custom value)
    {
        this = default;
        _tag = CustomTag;
        _custom = value;
    }

    public static Cursor Default => new(new Icon(CursorIcon.Default));

    public static Cursor From(CursorIcon icon)
    {
        return new Cursor(new Icon(icon));
    }

    public static Cursor From(CustomCursor custom)
    {
        return new Cursor(new Custom(custom));
    }

    public bool TryGetValue(out Icon value)
    {
        value = _icon;
        return _tag == IconTag;
    }

    public bool TryGetValue(out Custom value)
    {
        value = _custom;
        return _tag == CustomTag;
    }
}

public sealed class CustomCursor(ICustomCursorProvider provider) : IEquatable<CustomCursor>
{
    public ICustomCursorProvider Provider { get; } = provider;

    public bool IsAnimated => Provider.IsAnimated;

    public bool Equals(CustomCursor? other)
    {
        return other is not null && ReferenceEquals(Provider, other.Provider);
    }

    public override bool Equals(object? obj)
    {
        return obj is CustomCursor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Provider.GetHashCode();
    }
}

public interface ICustomCursorProvider : IAsAny
{
    bool IsAnimated { get; }
}

public record struct CustomCursorSource
{
    public const ushort MaxCursorSize = CursorImage.MaxCursorSize;

    public readonly record struct Image(CursorImage Value);

    public readonly record struct Animation(CursorAnimation Value);

    public readonly record struct Url(ushort HotspotX, ushort HotspotY, string Value);

    private const byte ImageTag = 0;
    private const byte AnimationTag = 1;
    private const byte UrlTag = 2;

    private byte _tag;
    private Image _image;
    private Animation _animation;
    private Url _url;

    public CustomCursorSource(Image value)
    {
        this = default;
        _tag = ImageTag;
        _image = value;
    }

    public CustomCursorSource(Animation value)
    {
        this = default;
        _tag = AnimationTag;
        _animation = value;
    }

    public CustomCursorSource(Url value)
    {
        this = default;
        _tag = UrlTag;
        _url = value;
    }

    public static CustomCursorSource FromRgba(
        IEnumerable<byte> rgba,
        ushort width,
        ushort height,
        ushort hotspotX,
        ushort hotspotY)
    {
        return new CustomCursorSource(new Image(new CursorImage(rgba, width, height, hotspotX, hotspotY)));
    }

    public static CustomCursorSource FromAnimation(TimeSpan duration, IReadOnlyList<CustomCursor> cursors)
    {
        return new CustomCursorSource(new Animation(new CursorAnimation(duration, cursors)));
    }

    public static CustomCursorSource FromUrl(ushort hotspotX, ushort hotspotY, string url)
    {
        return new CustomCursorSource(new Url(hotspotX, hotspotY, url));
    }

    public bool TryGetValue(out Image value)
    {
        value = _image;
        return _tag == ImageTag;
    }

    public bool TryGetValue(out Animation value)
    {
        value = _animation;
        return _tag == AnimationTag;
    }

    public bool TryGetValue(out Url value)
    {
        value = _url;
        return _tag == UrlTag;
    }
}

public sealed class CursorImage : IEquatable<CursorImage>
{
    public const ushort MaxCursorSize = 2048;
    private const int PixelSize = 4;

    private readonly byte[] _rgba;

    public CursorImage(IEnumerable<byte> rgba, ushort width, ushort height, ushort hotspotX, ushort hotspotY)
        : this(rgba.ToArray(), width, height, hotspotX, hotspotY)
    {
    }

    public CursorImage(byte[] rgba, ushort width, ushort height, ushort hotspotX, ushort hotspotY)
    {
        if (width > MaxCursorSize || height > MaxCursorSize)
        {
            throw new BadCursorImageException(
                $"The specified dimensions ({width}x{height}) are too large. The maximum is {MaxCursorSize}x{MaxCursorSize}.");
        }

        if (rgba.Length % PixelSize != 0)
        {
            throw new BadCursorImageException(
                $"The length of the rgba argument ({rgba.Length}) isn't divisible by 4, making it impossible to interpret as 32bpp RGBA pixels.");
        }

        ulong pixelCount = (ulong)(rgba.Length / PixelSize);
        ulong expectedPixelCount = (ulong)width * height;
        if (pixelCount != expectedPixelCount)
        {
            throw new BadCursorImageException(
                $"The specified dimensions ({width}x{height}) don't match the number of pixels supplied by the rgba argument ({pixelCount}). For those dimensions, the expected pixel count is {expectedPixelCount}.");
        }

        if (hotspotX >= width || hotspotY >= height)
        {
            throw new BadCursorImageException(
                $"The specified hotspot ({hotspotX}, {hotspotY}) is outside the image bounds ({width}x{height}).");
        }

        _rgba = rgba;
        Width = width;
        Height = height;
        HotspotX = hotspotX;
        HotspotY = hotspotY;
    }

    public ushort Width { get; }

    public ushort Height { get; }

    public ushort HotspotX { get; }

    public ushort HotspotY { get; }

    public ReadOnlyMemory<byte> Buffer => _rgba;

    public Memory<byte> BufferMut => _rgba;

    public bool Equals(CursorImage? other)
    {
        return other is not null
            && Width == other.Width
            && Height == other.Height
            && HotspotX == other.HotspotX
            && HotspotY == other.HotspotY
            && _rgba.AsSpan().SequenceEqual(other._rgba);
    }

    public override bool Equals(object? obj)
    {
        return obj is CursorImage other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Width);
        hash.Add(Height);
        hash.Add(HotspotX);
        hash.Add(HotspotY);
        foreach (byte value in _rgba)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}

public sealed class CursorAnimation : IEquatable<CursorAnimation>
{
    public CursorAnimation(TimeSpan duration, IReadOnlyList<CustomCursor> cursors)
    {
        if (cursors.Count == 0)
        {
            throw new BadCursorAnimationException("No cursors supplied");
        }

        if (cursors.Any(cursor => cursor.IsAnimated))
        {
            throw new BadCursorAnimationException("A supplied cursor is an animation");
        }

        Duration = duration;
        Cursors = cursors.ToArray();
    }

    public TimeSpan Duration { get; }

    public IReadOnlyList<CustomCursor> Cursors { get; }

    public (TimeSpan Duration, IReadOnlyList<CustomCursor> Cursors) IntoRaw()
    {
        return (Duration, Cursors);
    }

    public bool Equals(CursorAnimation? other)
    {
        return other is not null && Duration == other.Duration && Cursors.SequenceEqual(other.Cursors);
    }

    public override bool Equals(object? obj)
    {
        return obj is CursorAnimation other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Duration);
        foreach (CustomCursor cursor in Cursors)
        {
            hash.Add(cursor);
        }

        return hash.ToHashCode();
    }
}
