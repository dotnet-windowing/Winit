using Winit.Core;

namespace Winit.X11.Util;

internal static unsafe class CursorUtil
{
    public static void SetCursorIcon(this XConnection xconn, XlibWindow window, CursorIcon? cursor)
    {
        nuint xcursor = cursor is { } icon
            ? xconn.GetCursor(icon)
            : xconn.GetHiddenCursor();

        if (xcursor == 0)
        {
            _ = PInvoke.XUndefineCursor(xconn.Display, window);
        }
        else
        {
            _ = PInvoke.XDefineCursor(xconn.Display, window, xcursor);
        }

        xconn.Flush();
    }

    public static void SetCustomCursor(this XConnection xconn, XlibWindow window, X11CustomCursor cursor)
    {
        _ = PInvoke.XDefineCursor(xconn.Display, window, cursor.Raw);
        xconn.Flush();
    }

    private static nuint GetCursor(this XConnection xconn, CursorIcon icon)
    {
        string primaryName = icon.Name();
        lock (xconn.CursorLock)
        {
            if (xconn.CursorCache.TryGetValue(primaryName, out nuint cached))
            {
                return cached;
            }

            foreach (string name in iconNames(icon))
            {
                nuint cursor = LoadCursorByName(xconn.Display, name);
                if (cursor != 0)
                {
                    xconn.CursorCache[primaryName] = cursor;
                    return cursor;
                }
            }

            xconn.CursorCache[primaryName] = 0;
            return 0;
        }

        static IEnumerable<string> iconNames(CursorIcon icon)
        {
            yield return icon.Name();
            foreach (string altName in icon.AltNames())
            {
                yield return altName;
            }
        }
    }

    private static nuint GetHiddenCursor(this XConnection xconn)
    {
        lock (xconn.CursorLock)
        {
            if (xconn.HiddenCursor is { } hiddenCursor)
            {
                return hiddenCursor;
            }

            XcursorImage* image;
            try
            {
                image = PInvoke.XcursorImageCreate(1, 1);
            }
            catch (DllNotFoundException)
            {
                xconn.HiddenCursor = 0;
                return 0;
            }
            catch (EntryPointNotFoundException)
            {
                xconn.HiddenCursor = 0;
                return 0;
            }

            if (image is null)
            {
                xconn.HiddenCursor = 0;
                return 0;
            }

            try
            {
                image->Size = 1;
                image->Width = 1;
                image->Height = 1;
                image->Xhot = 0;
                image->Yhot = 0;
                image->Delay = 0;
                image->Pixels[0] = 0;
                nuint cursor = PInvoke.XcursorImageLoadCursor(xconn.Display, image);
                xconn.HiddenCursor = cursor;
                return cursor;
            }
            finally
            {
                PInvoke.XcursorImageDestroy(image);
            }
        }
    }

    private static nuint LoadCursorByName(nint display, string name)
    {
        try
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(name + '\0');
            fixed (byte* namePtr = utf8)
            {
                return PInvoke.XcursorLibraryLoadCursor(display, (sbyte*)namePtr);
            }
        }
        catch (DllNotFoundException)
        {
            return 0;
        }
        catch (EntryPointNotFoundException)
        {
            return 0;
        }
    }
}

internal sealed unsafe class X11CustomCursor : ICustomCursorProvider
{
    private readonly XConnection _xconn;
    private int _disposed;

    private X11CustomCursor(XConnection xconn, nuint cursor)
    {
        _xconn = xconn;
        Raw = cursor;
    }

    ~X11CustomCursor()
    {
        Dispose();
    }

    public nuint Raw { get; }

    public bool IsAnimated => false;

    public static X11CustomCursor Create(XConnection xconn, CustomCursorSource source)
    {
        try
        {
            if (source.TryGetValue(out CustomCursorSource.Image image))
            {
                return FromImage(xconn, image.Value);
            }
        }
        catch (DllNotFoundException error)
        {
            throw new NotSupportedException("X11 custom cursors require libXcursor.", error);
        }
        catch (EntryPointNotFoundException error)
        {
            throw new NotSupportedException("X11 custom cursors require libXcursor image APIs.", error);
        }

        throw new NotSupportedException("X11 custom cursors only support image sources currently.");
    }

    public object AsAny()
    {
        return this;
    }

    private static X11CustomCursor FromImage(XConnection xconn, CursorImage cursor)
    {
        XcursorImage* image = PInvoke.XcursorImageCreate(cursor.Width, cursor.Height);
        if (image is null)
        {
            throw new InvalidOperationException("XcursorImageCreate failed.");
        }

        try
        {
            image->Size = Math.Max(cursor.Width, cursor.Height);
            image->Width = cursor.Width;
            image->Height = cursor.Height;
            image->Xhot = cursor.HotspotX;
            image->Yhot = cursor.HotspotY;
            image->Delay = 0;

            ReadOnlySpan<byte> rgba = cursor.Buffer.Span;
            for (int i = 0, pixel = 0; i < rgba.Length; i += 4, pixel++)
            {
                uint r = rgba[i];
                uint g = rgba[i + 1];
                uint b = rgba[i + 2];
                uint a = rgba[i + 3];
                image->Pixels[pixel] = (a << 24) | (r << 16) | (g << 8) | b;
            }

            nuint xcursor = PInvoke.XcursorImageLoadCursor(xconn.Display, image);
            if (xcursor == 0)
            {
                throw new InvalidOperationException("XcursorImageLoadCursor failed.");
            }

            return new X11CustomCursor(xconn, xcursor);
        }
        finally
        {
            PInvoke.XcursorImageDestroy(image);
        }
    }

    private void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && Raw != 0)
        {
            _ = PInvoke.XFreeCursor(_xconn.Display, Raw);
        }
    }
}
