using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;

namespace Winit.Wayland;

internal sealed unsafe class XdgToplevelIconManagerState : IDisposable
{
    private XdgToplevelIconManagerV1 _manager;
    private bool _disposed;

    private XdgToplevelIconManagerState(XdgToplevelIconManagerV1 manager)
    {
        _manager = manager;
    }

    public static XdgToplevelIconManagerState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, XdgToplevelIconInterfaces.ManagerV1, maxVersion: 1);
        XdgToplevelIconManagerState manager = new(new XdgToplevelIconManagerV1(proxy.Value));
        manager.InstallDispatcher();
        return manager;
    }

    public ToplevelIcon? SetIcon(WinitState state, XdgToplevel toplevel, Icon? icon)
    {
        if (_manager.IsNull || toplevel.IsNull)
        {
            return null;
        }

        if (icon is null)
        {
            SetIcon(toplevel, XdgToplevelIconV1.Null);
            state.Connection.Flush();
            return null;
        }

        if (icon.Provider.AsAny() is not RgbaIcon rgbaIcon)
        {
            throw new NotSupportedException("this icon is unsupported on Wayland.");
        }

        ToplevelIcon toplevelIcon = ToplevelIcon.Create(state, rgbaIcon);
        XdgToplevelIconV1 iconProxy = XdgToplevelIconV1.Null;
        try
        {
            iconProxy = CreateIcon(state);
            toplevelIcon.AddBuffer(iconProxy);
            SetIcon(toplevel, iconProxy);
            state.Connection.Flush();
            return toplevelIcon;
        }
        catch
        {
            toplevelIcon.Dispose();
            throw;
        }
        finally
        {
            DestroyIcon(iconProxy);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_manager.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _manager,
                XdgToplevelIconManagerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = XdgToplevelIconManagerV1.Null;
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _manager,
            &NoopDispatcher,
            null,
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for xdg_toplevel_icon_manager_v1.");
        }
    }

    private XdgToplevelIconV1 CreateIcon(WinitState state)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            XdgToplevelIconManagerV1Request.CreateIcon,
            XdgToplevelIconInterfaces.IconV1,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("xdg_toplevel_icon_manager_v1.create_icon failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        XdgToplevelIconV1 icon = new(proxy.Value);
        int result = PInvoke.WlProxyAddDispatcher(icon, &NoopDispatcher, null, null);
        if (result != 0)
        {
            DestroyIcon(icon);
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for xdg_toplevel_icon_v1.");
        }

        return icon;
    }

    private void SetIcon(XdgToplevel toplevel, XdgToplevelIconV1 icon)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = toplevel.Value;
        args[1].Object = icon.Value;
        PInvoke.WlProxyMarshalArray(_manager, XdgToplevelIconManagerV1Request.SetIcon, args);
    }

    private static void DestroyIcon(XdgToplevelIconV1 icon)
    {
        if (icon.IsNull)
        {
            return;
        }

        PInvoke.WlProxyMarshalArrayFlags(
            icon,
            XdgToplevelIconV1Request.Destroy,
            null,
            PInvoke.WlProxyGetVersion(icon),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int NoopDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = implementation;
        _ = target;
        _ = opcode;
        _ = message;
        _ = args;
        return 0;
    }
}

internal sealed unsafe class ToplevelIcon : IDisposable
{
    private readonly nint _mapping;
    private readonly nuint _length;
    private WlBuffer _buffer;
    private bool _disposed;

    private ToplevelIcon(WlBuffer buffer, nint mapping, nuint length)
    {
        _buffer = buffer;
        _mapping = mapping;
        _length = length;
    }

    public static ToplevelIcon Create(WinitState state, RgbaIcon icon)
    {
        if (icon.Width != icon.Height)
        {
            throw new NotSupportedException("Wayland toplevel icons must be square.");
        }

        int width = checked((int)icon.Width);
        int height = checked((int)icon.Height);
        int stride = checked(width * 4);
        nuint length = checked((nuint)(stride * height));

        int fd;
        using (Utf8Buffer name = Utf8Buffer.FromString("winit-toplevel-icon"))
        {
            fd = PInvoke.MemfdCreate(name.Pointer, MemFdFlags.CloExec);
        }

        if (fd < 0)
        {
            throw new InvalidOperationException($"memfd_create failed for Wayland toplevel icon errno={Marshal.GetLastPInvokeError()}.");
        }

        try
        {
            if (PInvoke.Ftruncate(fd, (nint)length) != 0)
            {
                throw new InvalidOperationException($"ftruncate failed for Wayland toplevel icon errno={Marshal.GetLastPInvokeError()}.");
            }

            void* mapping = PInvoke.Mmap(null, length, MmapProtection.Read | MmapProtection.Write, MmapFlags.Shared, fd, 0);
            if ((nint)mapping == MmapFlags.Failed)
            {
                throw new InvalidOperationException($"mmap failed for Wayland toplevel icon errno={Marshal.GetLastPInvokeError()}.");
            }

            try
            {
                WriteArgb8888(icon.Buffer.Span, new Span<byte>(mapping, checked((int)length)));
                WlShmPool pool = CreatePool(state, fd, checked((int)length));
                try
                {
                    WlBuffer buffer = CreateBuffer(pool, width, height, stride);
                    return new ToplevelIcon(buffer, (nint)mapping, length);
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

    public void AddBuffer(XdgToplevelIconV1 icon)
    {
        if (_buffer.IsNull || icon.IsNull)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = _buffer.Value;
        args[1].Int = 1;
        PInvoke.WlProxyMarshalArray(icon, XdgToplevelIconV1Request.AddBuffer, args);
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
            throw new InvalidOperationException("wl_shm.create_pool failed for Wayland toplevel icon.");
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
            throw new InvalidOperationException("wl_shm_pool.create_buffer failed for Wayland toplevel icon.");
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
