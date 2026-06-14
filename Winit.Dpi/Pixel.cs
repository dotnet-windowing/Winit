using System.Numerics;

namespace Winit.Dpi;

public interface IPixel<TSelf>
    where TSelf : IPixel<TSelf>
{
    static abstract TSelf FromDouble(double value);

    double ToDouble();
}

public static class Pixel
{
    public static bool IsSupported<TPixel>()
        where TPixel : struct, INumberBase<TPixel>
    {
        Type type = typeof(TPixel);

        return type == typeof(byte)
            || type == typeof(ushort)
            || type == typeof(uint)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(int)
            || type == typeof(float)
            || type == typeof(double);
    }

    public static TPixel FromDouble<TPixel>(double value)
        where TPixel : struct, INumberBase<TPixel>
    {
        ThrowIfUnsupported<TPixel>();

        if (typeof(TPixel) == typeof(float))
        {
            return (TPixel)(object)(float)value;
        }

        if (typeof(TPixel) == typeof(double))
        {
            return (TPixel)(object)value;
        }

        double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        return TPixel.CreateSaturating(rounded);
    }

    public static double ToDouble<TPixel>(TPixel value)
        where TPixel : struct, INumberBase<TPixel>
    {
        ThrowIfUnsupported<TPixel>();

        if (typeof(TPixel) == typeof(float))
        {
            return (float)(object)value;
        }

        if (typeof(TPixel) == typeof(double))
        {
            return (double)(object)value;
        }

        return double.CreateSaturating(value);
    }

    public static TTarget Cast<TSource, TTarget>(TSource value)
        where TSource : struct, INumberBase<TSource>
        where TTarget : struct, INumberBase<TTarget>
    {
        return FromDouble<TTarget>(ToDouble(value));
    }

    private static void ThrowIfUnsupported<TPixel>()
        where TPixel : struct, INumberBase<TPixel>
    {
        if (!IsSupported<TPixel>())
        {
            throw new NotSupportedException($"{typeof(TPixel).FullName} is not a supported pixel type.");
        }
    }
}
