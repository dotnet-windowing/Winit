namespace Winit.X11.Util;

using System.Numerics;

internal static class Util
{
    public static bool MaybeChange<T>(ref T? field, T value)
        where T : struct, IEquatable<T>
    {
        if (field.HasValue && field.Value.Equals(value))
        {
            return false;
        }

        field = value;
        return true;
    }

    public static bool HasFlag<T>(T bitset, T flag)
        where T : struct, IBinaryInteger<T>
    {
        return (bitset & flag) == flag;
    }
}
