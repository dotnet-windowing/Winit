using System.Numerics;

namespace Winit.Dpi;

public static class LogicalUnit
{
    public static LogicalUnit<double> Max => new(double.MaxValue);

    public static LogicalUnit<double> Min => new(double.MinValue);

    public static LogicalUnit<double> Zero => new(0.0);

    public static LogicalUnit<TPixel> New<TPixel>(TPixel value)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new LogicalUnit<TPixel>(value);
    }
}

public readonly record struct LogicalUnit<TPixel>(TPixel Value)
    where TPixel : struct, INumberBase<TPixel>
{
    public static LogicalUnit<TPixel> FromPhysical<TSource>(
        PhysicalUnit<TSource> physical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return physical.ToLogical<TPixel>(scaleFactor);
    }

    public PhysicalUnit<TTarget> ToPhysical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        return new PhysicalUnit<double>(Pixel.ToDouble(Value) * scaleFactor).Cast<TTarget>();
    }

    public LogicalUnit<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new LogicalUnit<TTarget>(Pixel.Cast<TPixel, TTarget>(Value));
    }

    public TTarget To<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return Pixel.Cast<TPixel, TTarget>(Value);
    }

    public static implicit operator LogicalUnit<TPixel>(TPixel value)
    {
        return new LogicalUnit<TPixel>(value);
    }
}

public static class PhysicalUnit
{
    public static PhysicalUnit<double> Max => new(double.MaxValue);

    public static PhysicalUnit<double> Min => new(double.MinValue);

    public static PhysicalUnit<double> Zero => new(0.0);

    public static PhysicalUnit<TPixel> New<TPixel>(TPixel value)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new PhysicalUnit<TPixel>(value);
    }
}

public readonly record struct PhysicalUnit<TPixel>(TPixel Value)
    where TPixel : struct, INumberBase<TPixel>
{
    public static PhysicalUnit<TPixel> FromLogical<TSource>(
        LogicalUnit<TSource> logical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return logical.ToPhysical<TPixel>(scaleFactor);
    }

    public LogicalUnit<TTarget> ToLogical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        return new LogicalUnit<double>(Pixel.ToDouble(Value) / scaleFactor).Cast<TTarget>();
    }

    public PhysicalUnit<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new PhysicalUnit<TTarget>(Pixel.Cast<TPixel, TTarget>(Value));
    }

    public TTarget To<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return Pixel.Cast<TPixel, TTarget>(Value);
    }

    public static implicit operator PhysicalUnit<TPixel>(TPixel value)
    {
        return new PhysicalUnit<TPixel>(value);
    }
}

public readonly record struct PixelUnit
{
    public readonly record struct Physical(PhysicalUnit<int> Value);

    public readonly record struct Logical(LogicalUnit<double> Value);

    private const byte PhysicalTag = 0;
    private const byte LogicalTag = 1;

    private readonly byte _tag;
    private readonly Physical _physical;
    private readonly Logical _logical;

    public PixelUnit(Physical value)
    {
        _tag = PhysicalTag;
        _physical = value;
        _logical = default;
    }

    public PixelUnit(Logical value)
    {
        _tag = LogicalTag;
        _physical = default;
        _logical = value;
    }

    public PixelUnit(PhysicalUnit<int> value)
        : this(new Physical(value))
    {
    }

    public PixelUnit(LogicalUnit<double> value)
        : this(new Logical(value))
    {
    }

    public static PixelUnit Max => new(new Logical(LogicalUnit.Max));

    public static PixelUnit Min => new(new Logical(LogicalUnit.Min));

    public static PixelUnit Zero => new(new Logical(LogicalUnit.Zero));

    public bool IsPhysical => _tag == PhysicalTag;

    public bool IsLogical => _tag == LogicalTag;

    public static PixelUnit New(PixelUnit unit)
    {
        return unit;
    }

    public static PixelUnit FromPhysical<TPixel>(PhysicalUnit<TPixel> unit)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new PixelUnit(new Physical(unit.Cast<int>()));
    }

    public static PixelUnit FromLogical<TPixel>(LogicalUnit<TPixel> unit)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new PixelUnit(new Logical(unit.Cast<double>()));
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

    public LogicalUnit<TPixel> ToLogical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.ToLogical<TPixel>(scaleFactor),
            LogicalTag => _logical.Value.Cast<TPixel>(),
            _ => throw new InvalidOperationException("Invalid pixel unit tag."),
        };
    }

    public PhysicalUnit<TPixel> ToPhysical<TPixel>(double scaleFactor)
        where TPixel : struct, INumberBase<TPixel>
    {
        return _tag switch
        {
            PhysicalTag => _physical.Value.Cast<TPixel>(),
            LogicalTag => _logical.Value.ToPhysical<TPixel>(scaleFactor),
            _ => throw new InvalidOperationException("Invalid pixel unit tag."),
        };
    }

    public static implicit operator PixelUnit(PhysicalUnit<int> unit)
    {
        return new PixelUnit(unit);
    }

    public static implicit operator PixelUnit(LogicalUnit<double> unit)
    {
        return new PixelUnit(unit);
    }
}
