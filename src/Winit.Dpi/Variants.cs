using System.Numerics;

namespace Winit.Dpi;

public readonly record struct Size
{
    public readonly record struct Physical(PhysicalSize<uint> Value);

    public readonly record struct Logical(LogicalSize<double> Value);

    private const byte PhysicalTag = 0;
    private const byte LogicalTag = 1;

    private readonly byte _tag;
    private readonly Physical _physical;
    private readonly Logical _logical;

    public Size(Physical value)
    {
        _tag = PhysicalTag;
        _physical = value;
        _logical = default;
    }

    public Size(Logical value)
    {
        _tag = LogicalTag;
        _physical = default;
        _logical = value;
    }

    public Size(PhysicalSize<uint> value)
        : this(new Physical(value))
    {
    }

    public Size(LogicalSize<double> value)
        : this(new Logical(value))
    {
    }

    public bool IsPhysical => _tag == PhysicalTag;

    public bool IsLogical => _tag == LogicalTag;

    public static Size New(Size size)
    {
        return size;
    }

    public static Size New<TPixel>(PhysicalSize<TPixel> size)
        where TPixel : struct, INumberBase<TPixel>
    {
        return FromPhysical(size);
    }

    public static Size New<TPixel>(LogicalSize<TPixel> size)
        where TPixel : struct, INumberBase<TPixel>
    {
        return FromLogical(size);
    }

    public static Size FromPhysical<TPixel>(PhysicalSize<TPixel> size)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new Size(new Physical(size.Cast<uint>()));
    }

    public static Size FromLogical<TPixel>(LogicalSize<TPixel> size)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new Size(new Logical(size.Cast<double>()));
    }

    public bool TryGetValue(out Physical value)
    {
        value = _physical;
        return IsPhysical;
    }

    public bool TryGetValue(out Logical value)
    {
        value = _logical;
        return IsLogical;
    }

    public LogicalSize<TPixel> ToLogical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.ToLogical<TPixel>(scaleFactor),
            LogicalTag => _logical.Value.Cast<TPixel>(),
            _ => throw new InvalidOperationException("Invalid size tag."),
        };
    }

    public PhysicalSize<TPixel> ToPhysical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.Cast<TPixel>(),
            LogicalTag => _logical.Value.ToPhysical<TPixel>(scaleFactor),
            _ => throw new InvalidOperationException("Invalid size tag."),
        };
    }

    public static Size Clamp(Size input, Size min, Size max, double scaleFactor)
    {
        PhysicalSize<double> inputPhysical = input.ToPhysical<double>(scaleFactor);
        PhysicalSize<double> minPhysical = min.ToPhysical<double>(scaleFactor);
        PhysicalSize<double> maxPhysical = max.ToPhysical<double>(scaleFactor);

        double width = Math.Clamp(inputPhysical.Width, minPhysical.Width, maxPhysical.Width);
        double height = Math.Clamp(inputPhysical.Height, minPhysical.Height, maxPhysical.Height);

        return FromPhysical(new PhysicalSize<double>(width, height));
    }

    public static implicit operator Size(PhysicalSize<uint> size)
    {
        return new Size(size);
    }

    public static implicit operator Size(LogicalSize<double> size)
    {
        return new Size(size);
    }
}

public readonly record struct Position
{
    public readonly record struct Physical(PhysicalPosition<int> Value);

    public readonly record struct Logical(LogicalPosition<double> Value);

    private const byte PhysicalTag = 0;
    private const byte LogicalTag = 1;

    private readonly byte _tag;
    private readonly Physical _physical;
    private readonly Logical _logical;

    public Position(Physical value)
    {
        _tag = PhysicalTag;
        _physical = value;
        _logical = default;
    }

    public Position(Logical value)
    {
        _tag = LogicalTag;
        _physical = default;
        _logical = value;
    }

    public Position(PhysicalPosition<int> value)
        : this(new Physical(value))
    {
    }

    public Position(LogicalPosition<double> value)
        : this(new Logical(value))
    {
    }

    public bool IsPhysical => _tag == PhysicalTag;

    public bool IsLogical => _tag == LogicalTag;

    public static Position New(Position position)
    {
        return position;
    }

    public static Position New<TPixel>(PhysicalPosition<TPixel> position)
        where TPixel : struct, INumberBase<TPixel>
    {
        return FromPhysical(position);
    }

    public static Position New<TPixel>(LogicalPosition<TPixel> position)
        where TPixel : struct, INumberBase<TPixel>
    {
        return FromLogical(position);
    }

    public static Position FromPhysical<TPixel>(PhysicalPosition<TPixel> position)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new Position(new Physical(position.Cast<int>()));
    }

    public static Position FromLogical<TPixel>(LogicalPosition<TPixel> position)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new Position(new Logical(position.Cast<double>()));
    }

    public bool TryGetValue(out Physical value)
    {
        value = _physical;
        return IsPhysical;
    }

    public bool TryGetValue(out Logical value)
    {
        value = _logical;
        return IsLogical;
    }

    public LogicalPosition<TPixel> ToLogical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.ToLogical<TPixel>(scaleFactor),
            LogicalTag => _logical.Value.Cast<TPixel>(),
            _ => throw new InvalidOperationException("Invalid position tag."),
        };
    }

    public PhysicalPosition<TPixel> ToPhysical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.Cast<TPixel>(),
            LogicalTag => _logical.Value.ToPhysical<TPixel>(scaleFactor),
            _ => throw new InvalidOperationException("Invalid position tag."),
        };
    }

    public static implicit operator Position(PhysicalPosition<int> position)
    {
        return new Position(position);
    }

    public static implicit operator Position(LogicalPosition<double> position)
    {
        return new Position(position);
    }
}

public readonly record struct Insets
{
    public readonly record struct Physical(PhysicalInsets<uint> Value);

    public readonly record struct Logical(LogicalInsets<double> Value);

    private const byte PhysicalTag = 0;
    private const byte LogicalTag = 1;

    private readonly byte _tag;
    private readonly Physical _physical;
    private readonly Logical _logical;

    public Insets(Physical value)
    {
        _tag = PhysicalTag;
        _physical = value;
        _logical = default;
    }

    public Insets(Logical value)
    {
        _tag = LogicalTag;
        _physical = default;
        _logical = value;
    }

    public Insets(PhysicalInsets<uint> value)
        : this(new Physical(value))
    {
    }

    public Insets(LogicalInsets<double> value)
        : this(new Logical(value))
    {
    }

    public bool IsPhysical => _tag == PhysicalTag;

    public bool IsLogical => _tag == LogicalTag;

    public static Insets New(Insets insets)
    {
        return insets;
    }

    public static Insets New<TPixel>(PhysicalInsets<TPixel> insets)
        where TPixel : struct, INumberBase<TPixel>
    {
        return FromPhysical(insets);
    }

    public static Insets New<TPixel>(LogicalInsets<TPixel> insets)
        where TPixel : struct, INumberBase<TPixel>
    {
        return FromLogical(insets);
    }

    public static Insets FromPhysical<TPixel>(PhysicalInsets<TPixel> insets)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new Insets(new Physical(insets.Cast<uint>()));
    }

    public static Insets FromLogical<TPixel>(LogicalInsets<TPixel> insets)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new Insets(new Logical(insets.Cast<double>()));
    }

    public bool TryGetValue(out Physical value)
    {
        value = _physical;
        return IsPhysical;
    }

    public bool TryGetValue(out Logical value)
    {
        value = _logical;
        return IsLogical;
    }

    public LogicalInsets<TPixel> ToLogical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.ToLogical<TPixel>(scaleFactor),
            LogicalTag => _logical.Value.Cast<TPixel>(),
            _ => throw new InvalidOperationException("Invalid insets tag."),
        };
    }

    public PhysicalInsets<TPixel> ToPhysical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.Cast<TPixel>(),
            LogicalTag => _logical.Value.ToPhysical<TPixel>(scaleFactor),
            _ => throw new InvalidOperationException("Invalid insets tag."),
        };
    }

    public static implicit operator Insets(PhysicalInsets<uint> insets)
    {
        return new Insets(insets);
    }

    public static implicit operator Insets(LogicalInsets<double> insets)
    {
        return new Insets(insets);
    }
}
