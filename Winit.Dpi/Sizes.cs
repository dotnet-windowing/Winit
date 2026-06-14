using System.Numerics;

namespace Winit.Dpi;

public static class LogicalSize
{
    public static LogicalSize<TPixel> New<TPixel>(TPixel width, TPixel height)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new LogicalSize<TPixel>(width, height);
    }
}

public readonly record struct LogicalSize<TPixel>(TPixel Width, TPixel Height)
    where TPixel : struct, INumberBase<TPixel>
{
    public static LogicalSize<TPixel> FromPhysical<TSource>(
        PhysicalSize<TSource> physical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return physical.ToLogical<TPixel>(scaleFactor);
    }

    public static LogicalSize<TPixel> FromTuple<TSource>((TSource Width, TSource Height) value)
        where TSource : struct, INumberBase<TSource>
    {
        return new LogicalSize<TPixel>(
            Pixel.Cast<TSource, TPixel>(value.Width),
            Pixel.Cast<TSource, TPixel>(value.Height));
    }

    public static LogicalSize<TPixel> FromArray<TSource>(ReadOnlySpan<TSource> values)
        where TSource : struct, INumberBase<TSource>
    {
        if (values.Length != 2)
        {
            throw new ArgumentException("Size arrays must contain exactly 2 values.", nameof(values));
        }

        return new LogicalSize<TPixel>(
            Pixel.Cast<TSource, TPixel>(values[0]),
            Pixel.Cast<TSource, TPixel>(values[1]));
    }

    public PhysicalSize<TTarget> ToPhysical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        double width = Pixel.ToDouble(Width) * scaleFactor;
        double height = Pixel.ToDouble(Height) * scaleFactor;
        return new PhysicalSize<double>(width, height).Cast<TTarget>();
    }

    public LogicalSize<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new LogicalSize<TTarget>(
            Pixel.Cast<TPixel, TTarget>(Width),
            Pixel.Cast<TPixel, TTarget>(Height));
    }

    public (TTarget Width, TTarget Height) ToTuple<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return (Pixel.Cast<TPixel, TTarget>(Width), Pixel.Cast<TPixel, TTarget>(Height));
    }

    public TTarget[] ToArray<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return
        [
            Pixel.Cast<TPixel, TTarget>(Width),
            Pixel.Cast<TPixel, TTarget>(Height),
        ];
    }

    public static implicit operator LogicalSize<TPixel>((TPixel Width, TPixel Height) value)
    {
        return new LogicalSize<TPixel>(value.Width, value.Height);
    }
}

public static class PhysicalSize
{
    public static PhysicalSize<TPixel> New<TPixel>(TPixel width, TPixel height)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new PhysicalSize<TPixel>(width, height);
    }
}

public readonly record struct PhysicalSize<TPixel>(TPixel Width, TPixel Height)
    where TPixel : struct, INumberBase<TPixel>
{
    public static PhysicalSize<TPixel> FromLogical<TSource>(
        LogicalSize<TSource> logical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return logical.ToPhysical<TPixel>(scaleFactor);
    }

    public static PhysicalSize<TPixel> FromTuple<TSource>((TSource Width, TSource Height) value)
        where TSource : struct, INumberBase<TSource>
    {
        return new PhysicalSize<TPixel>(
            Pixel.Cast<TSource, TPixel>(value.Width),
            Pixel.Cast<TSource, TPixel>(value.Height));
    }

    public static PhysicalSize<TPixel> FromArray<TSource>(ReadOnlySpan<TSource> values)
        where TSource : struct, INumberBase<TSource>
    {
        if (values.Length != 2)
        {
            throw new ArgumentException("Size arrays must contain exactly 2 values.", nameof(values));
        }

        return new PhysicalSize<TPixel>(
            Pixel.Cast<TSource, TPixel>(values[0]),
            Pixel.Cast<TSource, TPixel>(values[1]));
    }

    public LogicalSize<TTarget> ToLogical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        double width = Pixel.ToDouble(Width) / scaleFactor;
        double height = Pixel.ToDouble(Height) / scaleFactor;
        return new LogicalSize<double>(width, height).Cast<TTarget>();
    }

    public PhysicalSize<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new PhysicalSize<TTarget>(
            Pixel.Cast<TPixel, TTarget>(Width),
            Pixel.Cast<TPixel, TTarget>(Height));
    }

    public (TTarget Width, TTarget Height) ToTuple<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return (Pixel.Cast<TPixel, TTarget>(Width), Pixel.Cast<TPixel, TTarget>(Height));
    }

    public TTarget[] ToArray<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return
        [
            Pixel.Cast<TPixel, TTarget>(Width),
            Pixel.Cast<TPixel, TTarget>(Height),
        ];
    }

    public static implicit operator PhysicalSize<TPixel>((TPixel Width, TPixel Height) value)
    {
        return new PhysicalSize<TPixel>(value.Width, value.Height);
    }
}
