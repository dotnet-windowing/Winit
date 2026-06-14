namespace Winit.X11.Util;

using System.Collections;
using System.Numerics;

internal static class Keys
{
    public static unsafe Keymap QueryKeymap(this XConnection xconn)
    {
        byte[] keys = new byte[Keymap.KeyCount];
        fixed (byte* keysPtr = keys)
        {
            _ = PInvoke.XQueryKeymap(xconn.Display, keysPtr);
        }

        return new Keymap(keys);
    }
}

internal sealed class Keymap : IEnumerable<uint>
{
    public const int KeyCount = 32;
    private readonly byte[] _keys;

    public Keymap(ReadOnlySpan<byte> keys)
    {
        if (keys.Length != KeyCount)
        {
            throw new ArgumentException($"X11 keymaps must contain {KeyCount} bytes.", nameof(keys));
        }

        _keys = keys.ToArray();
    }

    public IEnumerator<uint> GetEnumerator()
    {
        for (int index = 0; index < _keys.Length; index++)
        {
            byte item = _keys[index];
            while (item != 0)
            {
                byte bit = FirstBit(item);
                item ^= bit;
                uint shift = (uint)BitOperations.TrailingZeroCount(bit) + ((uint)index * 8);
                yield return shift;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static byte FirstBit(byte value)
    {
        return (byte)(value & unchecked((byte)-value));
    }
}
