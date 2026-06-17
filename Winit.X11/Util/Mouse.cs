namespace Winit.X11.Util;

using System.Numerics;

internal static class Mouse
{
}

internal struct Delta<T>
    where T : struct, IFloatingPointIeee754<T>
{
    private T _x;
    private T _y;

    public void SetX(T x)
    {
        _x = x;
    }

    public void SetY(T y)
    {
        _y = y;
    }

    public readonly (T X, T Y)? Consume()
    {
        bool xZero = T.Abs(_x) < T.Epsilon;
        bool yZero = T.Abs(_y) < T.Epsilon;

        return (xZero, yZero) switch
        {
            (true, true) => null,
            (false, true) => (_x, T.Zero),
            (true, false) => (T.Zero, _y),
            _ => (_x, _y),
        };
    }
}
