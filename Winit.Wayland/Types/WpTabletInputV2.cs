using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;
using Winit.Dpi;

namespace Winit.Wayland;

internal sealed unsafe class TabletManager : IDisposable
{
    private ZwpTabletManagerV2 _manager;
    private bool _disposed;

    private TabletManager(ZwpTabletManagerV2 manager)
    {
        _manager = manager;
    }

    public static TabletManager Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, ZwpInterfaces.TabletManagerV2, maxVersion: 1);
        return new TabletManager(new ZwpTabletManagerV2(proxy.Value));
    }

    public WinitTabletSeat GetTabletSeat(WinitState state, WlSeat seat)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = seat.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            ZwpTabletManagerV2Request.GetTabletSeat,
            ZwpInterfaces.TabletSeatV2,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zwp_tablet_manager_v2.get_tablet_seat failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        WinitTabletSeat tabletSeat = new(state, new ZwpTabletSeatV2(proxy.Value));
        tabletSeat.InstallDispatcher();
        return tabletSeat;
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
                ZwpTabletManagerV2Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = ZwpTabletManagerV2.Null;
        }
    }
}

internal sealed unsafe class WinitTabletSeat : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private readonly List<IDisposable> _children = [];
    private ZwpTabletSeatV2 _seat;
    private bool _disposed;

    public WinitTabletSeat(WinitState state, ZwpTabletSeatV2 seat)
    {
        _state = state;
        _seat = seat;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _seat,
            &TabletSeatDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_tablet_seat_v2.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (IDisposable child in _children.ToArray())
        {
            child.Dispose();
        }

        _children.Clear();

        if (!_seat.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _seat,
                ZwpTabletSeatV2Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_seat),
                WlProxyMarshalFlags.Destroy,
                null);
            _seat = ZwpTabletSeatV2.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void AddTablet(nint value)
    {
        AddNoopChild(value, ZwpTabletV2Request.Destroy, "zwp_tablet_v2");
    }

    private void AddTool(nint value)
    {
        if (value == 0)
        {
            return;
        }

        WlProxy proxy = new(value);
        PInvoke.WlProxySetQueue(proxy, _state.Connection.EventQueue);
        WinitTabletTool tool = new(_state, new ZwpTabletToolV2(value));
        tool.InstallDispatcher();
        _children.Add(tool);
    }

    private void AddPad(nint value)
    {
        if (value == 0)
        {
            return;
        }

        WlProxy proxy = new(value);
        PInvoke.WlProxySetQueue(proxy, _state.Connection.EventQueue);
        WinitTabletPad pad = new(_state, new ZwpTabletPadV2(value));
        pad.InstallDispatcher();
        _children.Add(pad);
    }

    private void AddNoopChild(nint value, uint destroyOpcode, string name)
    {
        if (value == 0)
        {
            return;
        }

        WlProxy proxy = new(value);
        PInvoke.WlProxySetQueue(proxy, _state.Connection.EventQueue);
        TabletNoopProxy child = new(proxy, destroyOpcode, name);
        child.InstallDispatcher();
        _children.Add(child);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TabletSeatDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null || args is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitTabletSeat seat || seat._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case ZwpTabletSeatV2Event.TabletAdded:
                seat.AddTablet(args[0].Object);
                break;
            case ZwpTabletSeatV2Event.ToolAdded:
                seat.AddTool(args[0].Object);
                break;
            case ZwpTabletSeatV2Event.PadAdded:
                seat.AddPad(args[0].Object);
                break;
        }

        return 0;
    }
}

internal sealed unsafe class WinitTabletTool : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private readonly List<TabletPendingEvent> _pending = [];
    private ZwpTabletToolV2 _tool;
    private bool _disposed;
    private TabletToolKind _kind;
    private WlSurface _surface;
    private LogicalPosition<double> _position;
    private Force? _force;
    private ushort? _twist;
    private TabletToolTilt? _tilt;

    public WinitTabletTool(WinitState state, ZwpTabletToolV2 tool)
    {
        _state = state;
        _tool = tool;
        _selfHandle = GCHandle.Alloc(this);
    }

    private TabletToolData ToolState => new(_force, null, _twist, _tilt, null);

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _tool,
            &TabletToolDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_tablet_tool_v2.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_tool.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _tool,
                ZwpTabletToolV2Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_tool),
                WlProxyMarshalFlags.Destroy,
                null);
            _tool = ZwpTabletToolV2.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void SetType(ZwpTabletToolV2Type type)
    {
        _kind = type switch
        {
            ZwpTabletToolV2Type.Pen => TabletToolKind.Pen,
            ZwpTabletToolV2Type.Eraser => TabletToolKind.Eraser,
            ZwpTabletToolV2Type.Brush => TabletToolKind.Brush,
            ZwpTabletToolV2Type.Pencil => TabletToolKind.Pencil,
            ZwpTabletToolV2Type.Airbrush => TabletToolKind.Airbrush,
            ZwpTabletToolV2Type.Finger => TabletToolKind.Finger,
            ZwpTabletToolV2Type.Mouse => TabletToolKind.Mouse,
            ZwpTabletToolV2Type.Lens => TabletToolKind.Lens,
            _ => _kind,
        };
    }

    private void Frame()
    {
        foreach (TabletPendingEvent pending in _pending)
        {
            if (pending.Kind == TabletPendingEventKind.Enter)
            {
                _surface = pending.Surface;
            }

            if (_surface.IsNull || !_state.TryGetWindow(_surface, out Window window))
            {
                continue;
            }

            PhysicalPosition<double> position = _position.ToPhysical<double>(window.ScaleFactor);
            WindowEvent windowEvent = pending.Kind switch
            {
                TabletPendingEventKind.Enter => new WindowEvent(new WindowEvent.PointerEntered(
                    null,
                    position,
                    true,
                    new PointerKind(new PointerKind.TabletTool(_kind)))),
                TabletPendingEventKind.Moved => new WindowEvent(new WindowEvent.PointerMoved(
                    null,
                    position,
                    true,
                    new PointerSource(new PointerSource.TabletTool(_kind, ToolState)))),
                TabletPendingEventKind.Button => new WindowEvent(new WindowEvent.PointerButton(
                    null,
                    pending.State,
                    position,
                    true,
                    new ButtonSource(new ButtonSource.TabletTool(_kind, pending.Button, ToolState)))),
                TabletPendingEventKind.Left => new WindowEvent(new WindowEvent.PointerLeft(
                    null,
                    position,
                    true,
                    new PointerKind(new PointerKind.TabletTool(_kind)))),
                _ => default,
            };

            _state.PushWindowEvent(window.Id, windowEvent);

            if (pending.Kind == TabletPendingEventKind.Left)
            {
                _surface = WlSurface.Null;
                ResetToolState();
            }
        }

        _pending.Clear();
    }

    private void ResetToolState()
    {
        _force = null;
        _twist = null;
        _tilt = null;
    }

    private static TabletToolButton ButtonFor(uint button)
    {
        return button switch
        {
            0x14b => new TabletToolButton(new TabletToolButton.Contact()),
            0x14c => new TabletToolButton(new TabletToolButton.Barrel()),
            0x149 => new TabletToolButton(new TabletToolButton.Other(1)),
            _ => new TabletToolButton(new TabletToolButton.Other(checked((ushort)Math.Min(button, ushort.MaxValue)))),
        };
    }

    private static sbyte FixedToSByte(WlFixed value)
    {
        return (sbyte)Math.Clamp(Math.Round(value.ToDouble()), sbyte.MinValue, sbyte.MaxValue);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TabletToolDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null || args is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitTabletTool tool || tool._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case ZwpTabletToolV2Event.Type:
                tool.SetType((ZwpTabletToolV2Type)args[0].Uint);
                break;
            case ZwpTabletToolV2Event.ProximityIn:
                tool._pending.Add(TabletPendingEvent.Enter(args[0].Uint, new WlSurface(args[2].Object)));
                break;
            case ZwpTabletToolV2Event.ProximityOut:
                tool._pending.Add(TabletPendingEvent.Left());
                break;
            case ZwpTabletToolV2Event.Down:
                tool._pending.Add(TabletPendingEvent.ButtonEvent(
                    ElementState.Pressed,
                    new TabletToolButton(new TabletToolButton.Contact()),
                    args[0].Uint));
                break;
            case ZwpTabletToolV2Event.Up:
                tool._pending.Add(TabletPendingEvent.ButtonEvent(
                    ElementState.Released,
                    new TabletToolButton(new TabletToolButton.Contact()),
                    null));
                break;
            case ZwpTabletToolV2Event.Motion:
                tool._position = new LogicalPosition<double>(
                    new WlFixed(args[0].Fixed).ToDouble(),
                    new WlFixed(args[1].Fixed).ToDouble());
                tool._pending.Add(TabletPendingEvent.Moved());
                break;
            case ZwpTabletToolV2Event.Pressure:
                tool._force = new Force(new Force.Normalized(args[0].Uint / (double)ushort.MaxValue));
                break;
            case ZwpTabletToolV2Event.Tilt:
                tool._tilt = new TabletToolTilt(
                    FixedToSByte(new WlFixed(args[0].Fixed)),
                    FixedToSByte(new WlFixed(args[1].Fixed)));
                break;
            case ZwpTabletToolV2Event.Rotation:
                tool._twist = checked((ushort)Math.Clamp(
                    Math.Round(new WlFixed(args[0].Fixed).ToDouble()),
                    0.0,
                    ushort.MaxValue));
                break;
            case ZwpTabletToolV2Event.Button:
                if ((ZwpTabletToolV2ButtonState)args[2].Uint is { } state &&
                    state is ZwpTabletToolV2ButtonState.Pressed or ZwpTabletToolV2ButtonState.Released)
                {
                    tool._pending.Add(TabletPendingEvent.ButtonEvent(
                        state == ZwpTabletToolV2ButtonState.Pressed ? ElementState.Pressed : ElementState.Released,
                        ButtonFor(args[1].Uint),
                        args[0].Uint));
                }
                break;
            case ZwpTabletToolV2Event.Frame:
                tool.Frame();
                break;
        }

        return 0;
    }
}

internal sealed unsafe class WinitTabletPad : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private readonly List<IDisposable> _children = [];
    private ZwpTabletPadV2 _pad;
    private bool _disposed;

    public WinitTabletPad(WinitState state, ZwpTabletPadV2 pad)
    {
        _state = state;
        _pad = pad;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _pad,
            &TabletPadDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_tablet_pad_v2.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (IDisposable child in _children.ToArray())
        {
            child.Dispose();
        }

        _children.Clear();

        if (!_pad.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _pad,
                ZwpTabletPadV2Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_pad),
                WlProxyMarshalFlags.Destroy,
                null);
            _pad = ZwpTabletPadV2.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void AddGroup(nint value)
    {
        if (value == 0)
        {
            return;
        }

        WlProxy proxy = new(value);
        PInvoke.WlProxySetQueue(proxy, _state.Connection.EventQueue);
        WinitTabletPadGroup group = new(_state, new ZwpTabletPadGroupV2(value));
        group.InstallDispatcher();
        _children.Add(group);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TabletPadDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null || args is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitTabletPad pad || pad._disposed)
        {
            return 0;
        }

        if (opcode == ZwpTabletPadV2Event.Group)
        {
            pad.AddGroup(args[0].Object);
        }

        return 0;
    }
}

internal sealed unsafe class WinitTabletPadGroup : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private readonly List<IDisposable> _children = [];
    private ZwpTabletPadGroupV2 _group;
    private bool _disposed;

    public WinitTabletPadGroup(WinitState state, ZwpTabletPadGroupV2 group)
    {
        _state = state;
        _group = group;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _group,
            &TabletPadGroupDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_tablet_pad_group_v2.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (IDisposable child in _children.ToArray())
        {
            child.Dispose();
        }

        _children.Clear();

        if (!_group.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _group,
                ZwpTabletPadGroupV2Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_group),
                WlProxyMarshalFlags.Destroy,
                null);
            _group = ZwpTabletPadGroupV2.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void AddNoopChild(nint value, uint destroyOpcode, string name)
    {
        if (value == 0)
        {
            return;
        }

        WlProxy proxy = new(value);
        PInvoke.WlProxySetQueue(proxy, _state.Connection.EventQueue);
        TabletNoopProxy child = new(proxy, destroyOpcode, name);
        child.InstallDispatcher();
        _children.Add(child);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TabletPadGroupDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null || args is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitTabletPadGroup group ||
            group._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case ZwpTabletPadGroupV2Event.Ring:
                group.AddNoopChild(args[0].Object, ZwpTabletPadRingV2Request.Destroy, "zwp_tablet_pad_ring_v2");
                break;
            case ZwpTabletPadGroupV2Event.Strip:
                group.AddNoopChild(args[0].Object, ZwpTabletPadStripV2Request.Destroy, "zwp_tablet_pad_strip_v2");
                break;
            case ZwpTabletPadGroupV2Event.Dial:
                group.AddNoopChild(args[0].Object, ZwpTabletPadDialV2Request.Destroy, "zwp_tablet_pad_dial_v2");
                break;
        }

        return 0;
    }
}

internal sealed unsafe class TabletNoopProxy : IDisposable
{
    private readonly GCHandle _selfHandle;
    private readonly uint _destroyOpcode;
    private readonly string _name;
    private WlProxy _proxy;
    private bool _disposed;

    public TabletNoopProxy(WlProxy proxy, uint destroyOpcode, string name)
    {
        _proxy = proxy;
        _destroyOpcode = destroyOpcode;
        _name = name;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _proxy,
            &NoopDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException($"wl_proxy_add_dispatcher failed for {_name}.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_proxy.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _proxy,
                _destroyOpcode,
                null,
                PInvoke.WlProxyGetVersion(_proxy),
                WlProxyMarshalFlags.Destroy,
                null);
            _proxy = WlProxy.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
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

internal enum TabletPendingEventKind
{
    Enter,
    Left,
    Moved,
    Button,
}

internal readonly record struct TabletPendingEvent(
    TabletPendingEventKind Kind,
    uint? Serial,
    WlSurface Surface,
    TabletToolButton Button,
    ElementState State)
{
    public static TabletPendingEvent Enter(uint serial, WlSurface surface)
    {
        return new TabletPendingEvent(TabletPendingEventKind.Enter, serial, surface, default, default);
    }

    public static TabletPendingEvent Left()
    {
        return new TabletPendingEvent(TabletPendingEventKind.Left, null, WlSurface.Null, default, default);
    }

    public static TabletPendingEvent Moved()
    {
        return new TabletPendingEvent(TabletPendingEventKind.Moved, null, WlSurface.Null, default, default);
    }

    public static TabletPendingEvent ButtonEvent(ElementState state, TabletToolButton button, uint? serial)
    {
        return new TabletPendingEvent(TabletPendingEventKind.Button, serial, WlSurface.Null, button, state);
    }
}
