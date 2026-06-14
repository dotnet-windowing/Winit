using System.Numerics;

namespace Winit.Dpi;

public static class LogicalPosition
{
    public static LogicalPosition<TPixel> New<TPixel>(TPixel x, TPixel y)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new LogicalPosition<TPixel>(x, y);
    }
}

public readonly record struct LogicalPosition<TPixel>(TPixel X, TPixel Y)
    where TPixel : struct, INumberBase<TPixel>
{
    public static LogicalPosition<TPixel> FromPhysical<TSource>(
        PhysicalPosition<TSource> physical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return physical.ToLogical<TPixel>(scaleFactor);
    }

    public static LogicalPosition<TPixel> FromTuple<TSource>((TSource X, TSource Y) value)
        where TSource : struct, INumberBase<TSource>
    {
        return new LogicalPosition<TPixel>(
            Pixel.Cast<TSource, TPixel>(value.X),
            Pixel.Cast<TSource, TPixel>(value.Y));
    }

    public static LogicalPosition<TPixel> FromArray<TSource>(ReadOnlySpan<TSource> values)
        where TSource : struct, INumberBase<TSource>
    {
        if (values.Length != 2)
        {
            throw new ArgumentException("Position arrays must contain exactly 2 values.", nameof(values));
        }

        return new LogicalPosition<TPixel>(
            Pixel.Cast<TSource, TPixel>(values[0]),
            Pixel.Cast<TSource, TPixel>(values[1]));
    }

    public PhysicalPosition<TTarget> ToPhysical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        double x = Pixel.ToDouble(X) * scaleFactor;
        double y = Pixel.ToDouble(Y) * scaleFactor;
        return new PhysicalPosition<double>(x, y).Cast<TTarget>();
    }

    public LogicalPosition<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new LogicalPosition<TTarget>(
            Pixel.Cast<TPixel, TTarget>(X),
            Pixel.Cast<TPixel, TTarget>(Y));
    }

    public (TTarget X, TTarget Y) ToTuple<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return (Pixel.Cast<TPixel, TTarget>(X), Pixel.Cast<TPixel, TTarget>(Y));
    }

    public TTarget[] ToArray<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return
        [
            Pixel.Cast<TPixel, TTarget>(X),
            Pixel.Cast<TPixel, TTarget>(Y),
        ];
    }

    public static implicit operator LogicalPosition<TPixel>((TPixel X, TPixel Y) value)
    {
        return new LogicalPosition<TPixel>(value.X, value.Y);
    }
}

public static class PhysicalPosition
{
    public static PhysicalPosition<TPixel> New<TPixel>(TPixel x, TPixel y)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new PhysicalPosition<TPixel>(x, y);
    }
}

public readonly record struct PhysicalPosition<TPixel>(TPixel X, TPixel Y)
    where TPixel : struct, INumberBase<TPixel>
{
    public static PhysicalPosition<TPixel> FromLogical<TSource>(
        LogicalPosition<TSource> logical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return logical.ToPhysical<TPixel>(scaleFactor);
    }

    public static PhysicalPosition<TPixel> FromTuple<TSource>((TSource X, TSource Y) value)
        where TSource : struct, INumberBase<TSource>
    {
        return new PhysicalPosition<TPixel>(
            Pixel.Cast<TSource, TPixel>(value.X),
            Pixel.Cast<TSource, TPixel>(value.Y));
    }

    public static PhysicalPosition<TPixel> FromArray<TSource>(ReadOnlySpan<TSource> values)
        where TSource : struct, INumberBase<TSource>
    {
        if (values.Length != 2)
        {
            throw new ArgumentException("Position arrays must contain exactly 2 values.", nameof(values));
        }

        return new PhysicalPosition<TPixel>(
            Pixel.Cast<TSource, TPixel>(values[0]),
            Pixel.Cast<TSource, TPixel>(values[1]));
    }

    public LogicalPosition<TTarget> ToLogical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        double x = Pixel.ToDouble(X) / scaleFactor;
        double y = Pixel.ToDouble(Y) / scaleFactor;
        return new LogicalPosition<double>(x, y).Cast<TTarget>();
    }

    public PhysicalPosition<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new PhysicalPosition<TTarget>(
            Pixel.Cast<TPixel, TTarget>(X),
            Pixel.Cast<TPixel, TTarget>(Y));
    }

    public (TTarget X, TTarget Y) ToTuple<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return (Pixel.Cast<TPixel, TTarget>(X), Pixel.Cast<TPixel, TTarget>(Y));
    }

    public TTarget[] ToArray<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return
        [
            Pixel.Cast<TPixel, TTarget>(X),
            Pixel.Cast<TPixel, TTarget>(Y),
        ];
    }

    public static implicit operator PhysicalPosition<TPixel>((TPixel X, TPixel Y) value)
    {
        return new PhysicalPosition<TPixel>(value.X, value.Y);
    }
}
