using Winit.Core;

namespace Winit.Wayland;

internal sealed class WaylandCustomCursor : ICustomCursorProvider
{
    private WaylandCustomCursor(CursorImage image)
    {
        Image = image;
    }

    public CursorImage Image { get; }

    public bool IsAnimated => false;

    public static WaylandCustomCursor Create(CustomCursorSource source)
    {
        if (source.TryGetValue(out CustomCursorSource.Image image))
        {
            return new WaylandCustomCursor(image.Value);
        }

        throw new NotSupportedException("Wayland custom cursors currently support image sources only.");
    }
}

internal sealed unsafe class WaylandCursorTheme : IDisposable
{
    private WlCursorTheme _theme;

    private WaylandCursorTheme(WlCursorTheme theme)
    {
        _theme = theme;
    }

    public static WaylandCursorTheme? TryLoad(WlShm shm, int size)
    {
        try
        {
            WlCursorTheme theme = PInvoke.WlCursorThemeLoad(null, size, shm);
            return theme.IsNull ? null : new WaylandCursorTheme(theme);
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    public WaylandThemeCursor? GetCursor(CursorIcon icon)
    {
        foreach (string name in CursorNames(icon))
        {
            using Utf8Buffer nameBuffer = Utf8Buffer.FromString(name);
            WlCursor cursor = PInvoke.WlCursorThemeGetCursor(_theme, nameBuffer.Pointer);
            if (cursor.IsNull)
            {
                continue;
            }

            WlCursorData* cursorData = (WlCursorData*)cursor.Value;
            if (cursorData->ImageCount == 0 || cursorData->Images is null || cursorData->Images[0] == 0)
            {
                continue;
            }

            WlCursorImage image = new(cursorData->Images[0]);
            WlCursorImageData* imageData = (WlCursorImageData*)image.Value;
            WlBuffer buffer = PInvoke.WlCursorImageGetBuffer(image);
            if (buffer.IsNull)
            {
                continue;
            }

            return new WaylandThemeCursor(
                buffer,
                checked((int)imageData->Width),
                checked((int)imageData->Height),
                checked((int)imageData->HotspotX),
                checked((int)imageData->HotspotY));
        }

        return null;
    }

    public void Dispose()
    {
        WlCursorTheme theme = _theme;
        if (theme.IsNull)
        {
            return;
        }

        _theme = WlCursorTheme.Null;
        PInvoke.WlCursorThemeDestroy(theme);
    }

    private static IEnumerable<string> CursorNames(CursorIcon icon)
    {
        yield return icon.Name();
        foreach (string altName in icon.AltNames())
        {
            yield return altName;
        }
    }
}

internal readonly record struct WaylandThemeCursor(
    WlBuffer Buffer,
    int Width,
    int Height,
    int HotspotX,
    int HotspotY);

internal sealed unsafe class WaylandCursorBuffer : IDisposable
{
    private readonly nint _mapping;
    private readonly nuint _length;
    private WlBuffer _buffer;
    private bool _disposed;

    private WaylandCursorBuffer(WlBuffer buffer, nint mapping, nuint length, int width, int height, int hotspotX, int hotspotY)
    {
        _buffer = buffer;
        _mapping = mapping;
        _length = length;
        Width = width;
        Height = height;
        HotspotX = hotspotX;
        HotspotY = hotspotY;
    }

    public WlBuffer Buffer => _buffer;

    public int Width { get; }

    public int Height { get; }

    public int HotspotX { get; }

    public int HotspotY { get; }

    public static WaylandCursorBuffer Create(WinitState state, WaylandCustomCursor cursor)
    {
        CursorImage image = cursor.Image;
        int width = image.Width;
        int height = image.Height;
        int stride = checked(width * 4);
        nuint length = checked((nuint)(stride * height));

        int fd;
        using (Utf8Buffer name = Utf8Buffer.FromString("winit-cursor"))
        {
            fd = PInvoke.MemfdCreate(name.Pointer, MemFdFlags.CloExec);
        }

        if (fd < 0)
        {
            throw new InvalidOperationException($"memfd_create failed for Wayland cursor errno={System.Runtime.InteropServices.Marshal.GetLastPInvokeError()}.");
        }

        try
        {
            if (PInvoke.Ftruncate(fd, (nint)length) != 0)
            {
                throw new InvalidOperationException($"ftruncate failed for Wayland cursor errno={System.Runtime.InteropServices.Marshal.GetLastPInvokeError()}.");
            }

            void* mapping = PInvoke.Mmap(null, length, MmapProtection.Read | MmapProtection.Write, MmapFlags.Shared, fd, 0);
            if ((nint)mapping == MmapFlags.Failed)
            {
                throw new InvalidOperationException($"mmap failed for Wayland cursor errno={System.Runtime.InteropServices.Marshal.GetLastPInvokeError()}.");
            }

            try
            {
                WriteArgb8888(image.Buffer.Span, new Span<byte>(mapping, checked((int)length)));
                WlShmPool pool = CreatePool(state, fd, checked((int)length));
                try
                {
                    WlBuffer buffer = CreateBuffer(pool, width, height, stride);
                    return new WaylandCursorBuffer(
                        buffer,
                        (nint)mapping,
                        length,
                        width,
                        height,
                        image.HotspotX,
                        image.HotspotY);
                }
                finally
                {
                    DestroyPool(pool);
                }
            }
            catch
            {
                _ = PInvoke.Munmap(mapping, length);
                throw;
            }
        }
        finally
        {
            _ = PInvoke.Close(fd);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_buffer.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _buffer,
                WlBufferRequest.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_buffer),
                WlProxyMarshalFlags.Destroy,
                null);
            _buffer = WlBuffer.Null;
        }

        if (_mapping != 0)
        {
            _ = PInvoke.Munmap((void*)_mapping, _length);
        }
    }

    private static void WriteArgb8888(ReadOnlySpan<byte> rgba, Span<byte> destination)
    {
        for (int source = 0, target = 0; source < rgba.Length; source += 4, target += 4)
        {
            byte alpha = rgba[source + 3];
            destination[target] = Premultiply(rgba[source + 2], alpha);
            destination[target + 1] = Premultiply(rgba[source + 1], alpha);
            destination[target + 2] = Premultiply(rgba[source], alpha);
            destination[target + 3] = alpha;
        }
    }

    private static byte Premultiply(byte channel, byte alpha)
    {
        return (byte)(channel * alpha / byte.MaxValue);
    }

    private static WlShmPool CreatePool(WinitState state, int fd, int size)
    {
        WlArgument* args = stackalloc WlArgument[3];
        args[0].Object = 0;
        args[1].Fd = fd;
        args[2].Int = size;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            state.Shm,
            WlShmRequest.CreatePool,
            WlCoreInterfaces.ShmPool,
            PInvoke.WlProxyGetVersion(state.Shm),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_shm.create_pool failed for Wayland cursor.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new WlShmPool(proxy.Value);
    }

    private static WlBuffer CreateBuffer(WlShmPool pool, int width, int height, int stride)
    {
        WlArgument* args = stackalloc WlArgument[6];
        args[0].Object = 0;
        args[1].Int = 0;
        args[2].Int = width;
        args[3].Int = height;
        args[4].Int = stride;
        args[5].Uint = (uint)WlShmFormat.Argb8888;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            pool,
            WlShmPoolRequest.CreateBuffer,
            WlCoreInterfaces.Buffer,
            PInvoke.WlProxyGetVersion(pool),
            WlProxyMarshalFlags.None,
            args);
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_shm_pool.create_buffer failed for Wayland cursor.");
        }

        return new WlBuffer(proxy.Value);
    }

    private static void DestroyPool(WlShmPool pool)
    {
        if (pool.IsNull)
        {
            return;
        }

        PInvoke.WlProxyMarshalArrayFlags(
            pool,
            WlShmPoolRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(pool),
            WlProxyMarshalFlags.Destroy,
            null);
    }
}
