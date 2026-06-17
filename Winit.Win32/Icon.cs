using Winit.Core;
using Winit.Dpi;

namespace Winit.Win32;

public sealed class WinIcon : IIconProvider, IDisposable
{
    private const uint ImageIcon = 1;
    private const uint LoadDefaultSize = 0x00000040;
    private const uint LoadFromFile = 0x00000010;

    private nint _handle;

    private WinIcon(nint handle)
    {
        _handle = handle;
    }

    ~WinIcon()
    {
        Dispose();
    }

    public nint Handle => _handle;

    public static WinIcon FromPath(string path, PhysicalSize<uint>? size = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        (int width, int height) = IconSize(size);
        unsafe
        {
            fixed (char* pathPtr = path)
            {
                nint handle = PInvoke.LoadImage(
                    HINSTANCE.Null,
                    new PCWSTR(pathPtr),
                    ImageIcon,
                    width,
                    height,
                    LoadDefaultSize | LoadFromFile);
                return handle != 0 ? new WinIcon(handle) : throw BadIcon();
            }
        }
    }

    public static WinIcon FromResource(ushort resourceId, PhysicalSize<uint>? size = null)
    {
        if (resourceId == 0)
        {
            throw new BadIconException("resource ordinal id 0 is invalid");
        }

        (int width, int height) = IconSize(size);
        unsafe
        {
            nint handle = PInvoke.LoadImage(
                ModuleInstance(),
                new PCWSTR((char*)resourceId),
                ImageIcon,
                width,
                height,
                LoadDefaultSize);
            return handle != 0 ? new WinIcon(handle) : throw BadIcon();
        }
    }

    public static WinIcon FromResourceName(string resourceName, PhysicalSize<uint>? size = null)
    {
        ArgumentNullException.ThrowIfNull(resourceName);
        if (resourceName.Length != 0 &&
            ushort.TryParse(resourceName, out ushort ordinal) &&
            ordinal != 0)
        {
            throw new BadIconException("numeric resource names must be loaded with FromResource");
        }

        (int width, int height) = IconSize(size);
        unsafe
        {
            fixed (char* resourceNamePtr = resourceName)
            {
                nint handle = PInvoke.LoadImage(
                    ModuleInstance(),
                    new PCWSTR(resourceNamePtr),
                    ImageIcon,
                    width,
                    height,
                    LoadDefaultSize);
                return handle != 0 ? new WinIcon(handle) : throw BadIcon();
            }
        }
    }

    public Icon ToIcon()
    {
        return new Icon(this);
    }

    public void Dispose()
    {
        nint handle = Interlocked.Exchange(ref _handle, 0);
        if (handle != 0)
        {
            PInvoke.DestroyIcon(handle);
            GC.SuppressFinalize(this);
        }
    }

    private static (int Width, int Height) IconSize(PhysicalSize<uint>? size)
    {
        return size is { } value
            ? (checked((int)value.Width), checked((int)value.Height))
            : (0, 0);
    }

    private static unsafe HINSTANCE ModuleInstance()
    {
        return new HINSTANCE(PInvoke.GetModuleHandleW(null));
    }

    private static BadIconException BadIcon()
    {
        return new BadIconException("failed to load Windows icon", Win32Error.Os());
    }
}

internal sealed class Win32IconProvider : IIconProvider, IDisposable
{
    private nint _handle;

    private Win32IconProvider(nint handle)
    {
        _handle = handle;
    }

    ~Win32IconProvider()
    {
        Dispose();
    }

    public nint Handle => _handle;

    public static Win32IconProvider FromRgba(RgbaIcon icon)
    {
        return new Win32IconProvider(IconFactory.CreateIcon(icon));
    }

    public void Dispose()
    {
        nint handle = Interlocked.Exchange(ref _handle, 0);
        if (handle != 0)
        {
            PInvoke.DestroyIcon(handle);
            GC.SuppressFinalize(this);
        }
    }
}

internal sealed class Win32CustomCursorProvider : ICustomCursorProvider, IDisposable
{
    private nint _handle;

    private Win32CustomCursorProvider(nint handle)
    {
        _handle = handle;
    }

    ~Win32CustomCursorProvider()
    {
        Dispose();
    }

    public bool IsAnimated => false;

    public nint Handle => _handle;

    public static Win32CustomCursorProvider FromImage(CursorImage image)
    {
        return new Win32CustomCursorProvider(IconFactory.CreateCursor(image));
    }

    public void Dispose()
    {
        nint handle = Interlocked.Exchange(ref _handle, 0);
        if (handle != 0)
        {
            PInvoke.DestroyCursor(handle);
            GC.SuppressFinalize(this);
        }
    }
}

internal static class IconFactory
{
    private const int PixelSize = 4;

    public static nint CreateIcon(RgbaIcon icon)
    {
        byte[] bgra = icon.Buffer.ToArray();
        byte[] andMask = new byte[bgra.Length / PixelSize];
        for (int i = 0, pixel = 0; i < bgra.Length; i += PixelSize, pixel++)
        {
            andMask[pixel] = unchecked((byte)(bgra[i + 3] - byte.MaxValue));
            (bgra[i], bgra[i + 2]) = (bgra[i + 2], bgra[i]);
        }

        nint handle = PInvoke.CreateIcon(
            0,
            checked((int)icon.Width),
            checked((int)icon.Height),
            1,
            PixelSize * 8,
            andMask,
            bgra);
        return handle != 0 ? handle : throw Win32Error.Request();
    }

    public static nint CreateCursor(CursorImage image)
    {
        byte[] bgra = image.Buffer.ToArray();
        for (int i = 0; i < bgra.Length; i += PixelSize)
        {
            (bgra[i], bgra[i + 2]) = (bgra[i + 2], bgra[i]);
        }

        int width = image.Width;
        int height = image.Height;
        nint screenDc = PInvoke.GetDC(0);
        if (screenDc == 0)
        {
            throw Win32Error.Request();
        }

        nint colorBitmap = PInvoke.CreateCompatibleBitmap(screenDc, width, height);
        PInvoke.ReleaseDC(0, screenDc);
        if (colorBitmap == 0)
        {
            throw Win32Error.Request();
        }

        nint maskBitmap = 0;
        try
        {
            if (PInvoke.SetBitmapBits(colorBitmap, (uint)bgra.Length, bgra) == 0 && bgra.Length != 0)
            {
                throw Win32Error.Request();
            }

            byte[] maskBits = new byte[((((width + 15) >> 4) << 1) * height)];
            Array.Fill(maskBits, byte.MaxValue);
            maskBitmap = PInvoke.CreateBitmap(width, height, 1, 1, maskBits);
            if (maskBitmap == 0)
            {
                throw Win32Error.Request();
            }

            IconInfo iconInfo = new()
            {
                Icon = false,
                XHotspot = image.HotspotX,
                YHotspot = image.HotspotY,
                MaskBitmap = maskBitmap,
                ColorBitmap = colorBitmap,
            };
            nint cursor = PInvoke.CreateIconIndirect(ref iconInfo);
            return cursor != 0 ? cursor : throw Win32Error.Request();
        }
        finally
        {
            if (maskBitmap != 0)
            {
                PInvoke.DeleteObject(maskBitmap);
            }

            PInvoke.DeleteObject(colorBitmap);
        }
    }
}
