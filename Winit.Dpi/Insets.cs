using System.Numerics;

namespace Winit.Dpi;

public static class LogicalInsets
{
    public static LogicalInsets<TPixel> New<TPixel>(TPixel top, TPixel left, TPixel bottom, TPixel right)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new LogicalInsets<TPixel>(top, left, bottom, right);
    }
}

public readonly record struct LogicalInsets<TPixel>(TPixel Top, TPixel Left, TPixel Bottom, TPixel Right)
    where TPixel : struct, INumberBase<TPixel>
{
    public static LogicalInsets<TPixel> FromPhysical<TSource>(
        PhysicalInsets<TSource> physical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return physical.ToLogical<TPixel>(scaleFactor);
    }

    public PhysicalInsets<TTarget> ToPhysical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        double top = Pixel.ToDouble(Top) * scaleFactor;
        double left = Pixel.ToDouble(Left) * scaleFactor;
        double bottom = Pixel.ToDouble(Bottom) * scaleFactor;
        double right = Pixel.ToDouble(Right) * scaleFactor;
        return new PhysicalInsets<double>(top, left, bottom, right).Cast<TTarget>();
    }

    public LogicalInsets<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new LogicalInsets<TTarget>(
            Pixel.Cast<TPixel, TTarget>(Top),
            Pixel.Cast<TPixel, TTarget>(Left),
            Pixel.Cast<TPixel, TTarget>(Bottom),
            Pixel.Cast<TPixel, TTarget>(Right));
    }
}

public static class PhysicalInsets
{
    public static PhysicalInsets<TPixel> New<TPixel>(TPixel top, TPixel left, TPixel bottom, TPixel right)
        where TPixel : struct, INumberBase<TPixel>
    {
        return new PhysicalInsets<TPixel>(top, left, bottom, right);
    }
}

public readonly record struct PhysicalInsets<TPixel>(TPixel Top, TPixel Left, TPixel Bottom, TPixel Right)
    where TPixel : struct, INumberBase<TPixel>
{
    public static PhysicalInsets<TPixel> FromLogical<TSource>(
        LogicalInsets<TSource> logical,
        double scaleFactor)
        where TSource : struct, INumberBase<TSource>
    {
        return logical.ToPhysical<TPixel>(scaleFactor);
    }

    public LogicalInsets<TTarget> ToLogical<TTarget>(double scaleFactor)
        where TTarget : struct, INumberBase<TTarget>
    {
        Dpi.ThrowIfInvalidScaleFactor(scaleFactor);
        double top = Pixel.ToDouble(Top) / scaleFactor;
        double left = Pixel.ToDouble(Left) / scaleFactor;
        double bottom = Pixel.ToDouble(Bottom) / scaleFactor;
        double right = Pixel.ToDouble(Right) / scaleFactor;
        return new LogicalInsets<double>(top, left, bottom, right).Cast<TTarget>();
    }

    public PhysicalInsets<TTarget> Cast<TTarget>()
        where TTarget : struct, INumberBase<TTarget>
    {
        return new PhysicalInsets<TTarget>(
            Pixel.Cast<TPixel, TTarget>(Top),
            Pixel.Cast<TPixel, TTarget>(Left),
            Pixel.Cast<TPixel, TTarget>(Bottom),
            Pixel.Cast<TPixel, TTarget>(Right));
    }
}
