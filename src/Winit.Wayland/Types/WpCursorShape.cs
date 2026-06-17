using Winit.Core;

namespace Winit.Wayland;

internal sealed unsafe class CursorShapeManager : IDisposable
{
    private WpCursorShapeManagerV1 _manager;
    private bool _disposed;

    private CursorShapeManager(WpCursorShapeManagerV1 manager)
    {
        _manager = manager;
    }

    public static CursorShapeManager Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, WpInterfaces.CursorShapeManagerV1, maxVersion: 1);
        return new CursorShapeManager(new WpCursorShapeManagerV1(proxy.Value));
    }

    public CursorShapeDevice GetPointer(WinitState state, WlPointer pointer)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = pointer.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            WpCursorShapeManagerV1Request.GetPointer,
            WpInterfaces.CursorShapeDeviceV1,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wp_cursor_shape_manager_v1.get_pointer failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new CursorShapeDevice(new WpCursorShapeDeviceV1(proxy.Value));
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
                WpCursorShapeManagerV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = WpCursorShapeManagerV1.Null;
        }
    }
}

internal sealed unsafe class CursorShapeDevice : IDisposable
{
    private WpCursorShapeDeviceV1 _device;
    private bool _disposed;

    public CursorShapeDevice(WpCursorShapeDeviceV1 device)
    {
        _device = device;
    }

    public bool SetShape(uint serial, CursorIcon icon)
    {
        if (_device.IsNull || serial == 0)
        {
            return false;
        }

        WlArgument* args = stackalloc WlArgument[2];
        args[0].Uint = serial;
        args[1].Uint = (uint)ShapeFor(icon);
        PInvoke.WlProxyMarshalArray(_device, WpCursorShapeDeviceV1Request.SetShape, args);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_device.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _device,
                WpCursorShapeDeviceV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_device),
                WlProxyMarshalFlags.Destroy,
                null);
            _device = WpCursorShapeDeviceV1.Null;
        }
    }

    private static WpCursorShapeDeviceV1Shape ShapeFor(CursorIcon icon)
    {
        return icon switch
        {
            CursorIcon.Default => WpCursorShapeDeviceV1Shape.Default,
            CursorIcon.ContextMenu => WpCursorShapeDeviceV1Shape.ContextMenu,
            CursorIcon.Help => WpCursorShapeDeviceV1Shape.Help,
            CursorIcon.Pointer => WpCursorShapeDeviceV1Shape.Pointer,
            CursorIcon.Progress => WpCursorShapeDeviceV1Shape.Progress,
            CursorIcon.Wait => WpCursorShapeDeviceV1Shape.Wait,
            CursorIcon.Cell => WpCursorShapeDeviceV1Shape.Cell,
            CursorIcon.Crosshair => WpCursorShapeDeviceV1Shape.Crosshair,
            CursorIcon.Text => WpCursorShapeDeviceV1Shape.Text,
            CursorIcon.VerticalText => WpCursorShapeDeviceV1Shape.VerticalText,
            CursorIcon.Alias => WpCursorShapeDeviceV1Shape.Alias,
            CursorIcon.Copy => WpCursorShapeDeviceV1Shape.Copy,
            CursorIcon.Move => WpCursorShapeDeviceV1Shape.Move,
            CursorIcon.NoDrop => WpCursorShapeDeviceV1Shape.NoDrop,
            CursorIcon.NotAllowed => WpCursorShapeDeviceV1Shape.NotAllowed,
            CursorIcon.Grab => WpCursorShapeDeviceV1Shape.Grab,
            CursorIcon.Grabbing => WpCursorShapeDeviceV1Shape.Grabbing,
            CursorIcon.EResize => WpCursorShapeDeviceV1Shape.EResize,
            CursorIcon.NResize => WpCursorShapeDeviceV1Shape.NResize,
            CursorIcon.NeResize => WpCursorShapeDeviceV1Shape.NeResize,
            CursorIcon.NwResize => WpCursorShapeDeviceV1Shape.NwResize,
            CursorIcon.SResize => WpCursorShapeDeviceV1Shape.SResize,
            CursorIcon.SeResize => WpCursorShapeDeviceV1Shape.SeResize,
            CursorIcon.SwResize => WpCursorShapeDeviceV1Shape.SwResize,
            CursorIcon.WResize => WpCursorShapeDeviceV1Shape.WResize,
            CursorIcon.EwResize => WpCursorShapeDeviceV1Shape.EwResize,
            CursorIcon.NsResize => WpCursorShapeDeviceV1Shape.NsResize,
            CursorIcon.NeswResize => WpCursorShapeDeviceV1Shape.NeswResize,
            CursorIcon.NwseResize => WpCursorShapeDeviceV1Shape.NwseResize,
            CursorIcon.ColResize => WpCursorShapeDeviceV1Shape.ColResize,
            CursorIcon.RowResize => WpCursorShapeDeviceV1Shape.RowResize,
            CursorIcon.AllScroll => WpCursorShapeDeviceV1Shape.AllScroll,
            CursorIcon.ZoomIn => WpCursorShapeDeviceV1Shape.ZoomIn,
            CursorIcon.ZoomOut => WpCursorShapeDeviceV1Shape.ZoomOut,
            _ => WpCursorShapeDeviceV1Shape.Default,
        };
    }
}
