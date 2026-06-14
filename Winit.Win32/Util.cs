namespace Winit.Win32;

internal static class Util
{
    public static nint HwndValue(HWND hwnd)
    {
        return unchecked((nint)hwnd.Value);
    }

    public static ushort LowWord(nint value)
    {
        return unchecked((ushort)((nuint)value & 0xffff));
    }

    public static ushort HighWord(nint value)
    {
        return unchecked((ushort)(((nuint)value >> 16) & 0xffff));
    }

    public static short SignedLowWord(nint value)
    {
        return unchecked((short)LowWord(value));
    }

    public static short SignedHighWord(nint value)
    {
        return unchecked((short)HighWord(value));
    }

    public static nint MakeLParam(int low, int high)
    {
        return unchecked((nint)((low & 0xffff) | ((high & 0xffff) << 16)));
    }
}
