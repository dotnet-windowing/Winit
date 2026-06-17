using Winit.Core;

namespace Winit.X11.Util;

internal static class IconUtil
{
    public static nuint[] RgbaToCardinals(RgbaIcon icon)
    {
        ReadOnlySpan<byte> rgba = icon.Buffer.Span;
        nuint[] data = new nuint[checked(rgba.Length / 4 + 2)];
        data[0] = icon.Width;
        data[1] = icon.Height;

        for (int i = 0, pixel = 2; i < rgba.Length; i += 4, pixel++)
        {
            nuint r = rgba[i];
            nuint g = rgba[i + 1];
            nuint b = rgba[i + 2];
            nuint a = rgba[i + 3];
            data[pixel] = (a << 24) | (r << 16) | (g << 8) | b;
        }

        return data;
    }
}
