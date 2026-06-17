using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;

namespace Winit.Wayland;

internal readonly record struct WaylandGlobal(uint Name, string Interface, uint Version);

internal sealed unsafe class WinitState : IDisposable
{
    private readonly GCHandle _registryHandle;
    private bool _coreGlobalsBound;
    private bool _disposed;

    private WinitState(WaylandConnection connection)
    {
        Connection = connection;
        _registryHandle = GCHandle.Alloc(this);
    }

    public WaylandConnection Connection { get; }

    public List<WaylandGlobal> Globals { get; } = [];

    public List<MonitorHandle> Monitors { get; } = [];

    public List<WinitSeatState> Seats { get; } = [];

    public Dictionary<WindowId, Window> Windows { get; } = [];

    public Dictionary<nint, FrameSurfaceData> FrameSurfaces { get; } = [];

    public EventSink EventsSink { get; } = new();

    public List<WindowCompositorUpdate> WindowCompositorUpdates { get; } = [];

    public bool DispatchedEvents { get; set; } = true;

    public WlCompositor Compositor { get; private set; }

    public WlShm Shm { get; private set; }

    public WaylandCursorTheme? CursorTheme { get; private set; }

    public ViewporterState? ViewporterState { get; private set; }

    public FractionalScalingManager? FractionalScalingManager { get; private set; }

    public CursorShapeManager? CursorShapeManager { get; private set; }

    public PointerConstraintsState? PointerConstraints { get; private set; }

    public PointerGesturesState? PointerGestures { get; private set; }

    public RelativePointerState? RelativePointer { get; private set; }

    public XdgActivationState? XdgActivation { get; private set; }

    public XdgDecorationManagerState? XdgDecorationManager { get; private set; }

    public XdgToplevelIconManagerState? XdgToplevelIconManager { get; private set; }

    public TextInputState? TextInputState { get; private set; }

    public BgrEffectManager? BlurManager { get; private set; }

    public TabletManager? TabletState { get; private set; }

    public WlSubcompositor Subcompositor { get; private set; }

    public XdgWmBase XdgWmBase { get; private set; }

    public static WinitState New()
    {
        WaylandConnection connection = WaylandConnection.ConnectToEnv();
        WinitState state = new(connection);

        try
        {
            state.InstallRegistryDispatcher();
            state.Connection.Roundtrip();
            state.BindCoreGlobals();
            state.Connection.Roundtrip();
            return state;
        }
        catch
        {
            state.Dispose();
            throw;
        }
    }

    public WaylandGlobal? FindGlobal(string interfaceName)
    {
        return Globals.FirstOrDefault(global => global.Interface == interfaceName) is { Interface: not null } global
            ? global
            : null;
    }

    public WlProxy BindGlobal(WaylandGlobal global, WlInterface* @interface, uint maxVersion)
    {
        if (@interface is null)
        {
            throw new InvalidOperationException($"Wayland interface metadata is missing for {global.Interface}.");
        }

        uint version = Math.Min(global.Version, maxVersion);
        WlArgument* args = stackalloc WlArgument[4];
        args[0].Uint = global.Name;
        args[1].String = @interface->Name;
        args[2].Uint = version;
        args[3].Object = 0;

        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            Connection.Registry,
            WlRegistryRequest.Bind,
            @interface,
            version,
            WlProxyMarshalFlags.None,
            args);
        Connection.CheckError();

        if (proxy.IsNull)
        {
            throw new InvalidOperationException($"Failed to bind Wayland global {global.Interface}@{global.Name}.");
        }

        PInvoke.WlProxySetQueue(proxy, Connection.EventQueue);
        return proxy;
    }

    public void RegisterWindow(Window window)
    {
        Windows[window.Id] = window;
    }

    public void RemoveWindow(WindowId windowId)
    {
        Windows.Remove(windowId);
    }

    public void RegisterFrameSurface(WlSurface surface, Window window, FrameSurfaceRole role)
    {
        if (!surface.IsNull)
        {
            FrameSurfaces[surface.Value] = new FrameSurfaceData(window, role);
        }
    }

    public void RemoveFrameSurface(WlSurface surface)
    {
        if (!surface.IsNull)
        {
            FrameSurfaces.Remove(surface.Value);
        }
    }

    public bool TryGetFrameSurface(WlSurface surface, out FrameSurfaceData data)
    {
        if (!surface.IsNull && FrameSurfaces.TryGetValue(surface.Value, out data))
        {
            return true;
        }

        data = default;
        return false;
    }

    public bool TryGetWindow(WlSurface surface, out Window window)
    {
        if (!surface.IsNull)
        {
            WindowId windowId = WindowId.FromRaw((nuint)surface.Value);
            if (Windows.TryGetValue(windowId, out Window? found))
            {
                window = found;
                return true;
            }
        }

        window = null!;
        return false;
    }

    public bool TryGetMonitor(WlOutput output, out MonitorHandle monitor)
    {
        if (!output.IsNull)
        {
            foreach (MonitorHandle candidate in Monitors)
            {
                if (candidate.Output.Value == output.Value)
                {
                    monitor = candidate;
                    return true;
                }
            }
        }

        monitor = null!;
        return false;
    }

    public bool TryGetPointerForWindow(WindowId windowId, out WinitPointerData pointer)
    {
        foreach (WinitSeatState seat in Seats)
        {
            if (seat.TryGetPointerForWindow(windowId, out pointer))
            {
                return true;
            }
        }

        pointer = null!;
        return false;
    }

    public bool ForEachPointerForWindow(WindowId windowId, Action<WinitPointerData> action)
    {
        bool found = false;
        foreach (WinitSeatState seat in Seats)
        {
            if (seat.TryGetPointerForWindow(windowId, out WinitPointerData pointer))
            {
                found = true;
                action(pointer);
            }
        }

        return found;
    }

    public void ReleasePointerGrabForSurface(WlSurface surface)
    {
        foreach (WinitSeatState seat in Seats)
        {
            seat.ReleasePointerGrabForSurface(surface);
        }
    }

    public WlSurface CreateSurface()
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            Compositor,
            WlCompositorRequest.CreateSurface,
            WlCoreInterfaces.Surface,
            PInvoke.WlProxyGetVersion(Compositor),
            WlProxyMarshalFlags.None,
            args);
        Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_compositor.create_surface failed.");
        }

        PInvoke.WlProxySetQueue(proxy, Connection.EventQueue);
        return new WlSurface(proxy.Value);
    }

    public WlRegion CreateRegion()
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            Compositor,
            WlCompositorRequest.CreateRegion,
            WlCoreInterfaces.Region,
            PInvoke.WlProxyGetVersion(Compositor),
            WlProxyMarshalFlags.None,
            args);
        Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_compositor.create_region failed.");
        }

        PInvoke.WlProxySetQueue(proxy, Connection.EventQueue);
        return new WlRegion(proxy.Value);
    }

    public void PushWindowEvent(WindowId windowId, WindowEvent windowEvent)
    {
        DispatchedEvents = true;
        EventsSink.PushWindowEvent(windowId, windowEvent);
    }

    public void PushDeviceEvent(DeviceEvent deviceEvent)
    {
        DispatchedEvents = true;
        EventsSink.PushDeviceEvent(deviceEvent);
    }

    public void ScaleFactorChanged(WlSurface surface, double scaleFactor, bool isLegacy)
    {
        if (TryGetWindow(surface, out Window window))
        {
            window.HandleScaleFactorChanged(scaleFactor, isLegacy);
        }
    }

    public WindowCompositorUpdate[] DrainWindowCompositorUpdates()
    {
        if (WindowCompositorUpdates.Count == 0)
        {
            return [];
        }

        WindowCompositorUpdate[] updates = [.. WindowCompositorUpdates];
        WindowCompositorUpdates.Clear();
        return updates;
    }

    public void QueueWindowResized(WindowId windowId)
    {
        DispatchedEvents = true;
        ref WindowCompositorUpdate update = ref CompositorUpdateFor(windowId);
        update.Resized = true;
    }

    public void QueueScaleFactorChanged(WindowId windowId)
    {
        DispatchedEvents = true;
        ref WindowCompositorUpdate update = ref CompositorUpdateFor(windowId);
        update.ScaleChanged = true;
    }

    public void QueueClose(WindowId windowId)
    {
        DispatchedEvents = true;
        ref WindowCompositorUpdate update = ref CompositorUpdateFor(windowId);
        update.CloseWindow = true;
    }

    public int? KeyboardRepeatTimeoutMilliseconds()
    {
        Instant now = Instant.Now();
        int? minTimeout = null;
        foreach (WinitSeatState seat in Seats)
        {
            if (seat.KeyboardRepeatTimeoutMilliseconds(now) is not { } timeout)
            {
                continue;
            }

            minTimeout = minTimeout is { } current ? Math.Min(current, timeout) : timeout;
        }

        return minTimeout;
    }

    public bool DispatchKeyboardRepeats()
    {
        bool dispatched = false;
        Instant now = Instant.Now();
        foreach (WinitSeatState seat in Seats)
        {
            dispatched |= seat.DispatchKeyboardRepeat(now);
        }

        DispatchedEvents |= dispatched;
        return dispatched;
    }

    private ref WindowCompositorUpdate CompositorUpdateFor(WindowId windowId)
    {
        for (int i = 0; i < WindowCompositorUpdates.Count; i++)
        {
            if (WindowCompositorUpdates[i].WindowId.Equals(windowId))
            {
                return ref CollectionsMarshal.AsSpan(WindowCompositorUpdates)[i];
            }
        }

        WindowCompositorUpdates.Add(new WindowCompositorUpdate(windowId));
        return ref CollectionsMarshal.AsSpan(WindowCompositorUpdates)[^1];
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (Window window in Windows.Values.ToArray())
        {
            window.DestroyFromEventLoop();
        }

        Windows.Clear();
        FrameSurfaces.Clear();

        foreach (WinitSeatState seat in Seats.ToArray())
        {
            seat.Dispose();
        }

        Seats.Clear();

        foreach (MonitorHandle monitor in Monitors.ToArray())
        {
            monitor.Dispose();
        }

        Monitors.Clear();

        CursorTheme?.Dispose();
        CursorTheme = null;

        FractionalScalingManager?.Dispose();
        FractionalScalingManager = null;

        CursorShapeManager?.Dispose();
        CursorShapeManager = null;

        PointerConstraints?.Dispose();
        PointerConstraints = null;

        PointerGestures?.Dispose();
        PointerGestures = null;

        RelativePointer?.Dispose();
        RelativePointer = null;

        XdgActivation?.Dispose();
        XdgActivation = null;

        XdgDecorationManager?.Dispose();
        XdgDecorationManager = null;

        XdgToplevelIconManager?.Dispose();
        XdgToplevelIconManager = null;

        TextInputState?.Dispose();
        TextInputState = null;

        BlurManager?.Dispose();
        BlurManager = null;

        TabletState?.Dispose();
        TabletState = null;

        ViewporterState?.Dispose();
        ViewporterState = null;

        if (!XdgWmBase.IsNull)
        {
            DestroyXdgWmBase(XdgWmBase);
            XdgWmBase = XdgWmBase.Null;
        }

        if (!Subcompositor.IsNull)
        {
            DestroySubcompositor(Subcompositor);
            Subcompositor = WlSubcompositor.Null;
        }

        if (!Shm.IsNull)
        {
            PInvoke.WlProxyDestroy(Shm);
            Shm = WlShm.Null;
        }

        if (!Compositor.IsNull)
        {
            ReleaseCompositor(Compositor);
            Compositor = WlCompositor.Null;
        }

        Connection.Dispose();

        if (_registryHandle.IsAllocated)
        {
            _registryHandle.Free();
        }
    }

    private void InstallRegistryDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            Connection.Registry,
            &RegistryDispatcher,
            (void*)GCHandle.ToIntPtr(_registryHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_registry.");
        }
    }

    private void BindCoreGlobals()
    {
        Compositor = new WlCompositor(
            BindRequired("wl_compositor", WlCoreInterfaces.Compositor, maxVersion: 6).Value);
        Shm = new WlShm(BindRequired("wl_shm", WlCoreInterfaces.Shm, maxVersion: 1).Value);
        CursorTheme = WaylandCursorTheme.TryLoad(Shm, size: 24);
        XdgWmBase = new XdgWmBase(BindRequired("xdg_wm_base", XdgInterfaces.WmBase, maxVersion: 7).Value);
        InstallXdgWmBaseDispatcher();

        if (FindGlobal("wp_viewporter") is { } viewporter)
        {
            ViewporterState = global::Winit.Wayland.ViewporterState.Bind(this, viewporter);
        }

        if (FindGlobal("wp_fractional_scale_manager_v1") is { } fractionalScale)
        {
            FractionalScalingManager = global::Winit.Wayland.FractionalScalingManager.Bind(this, fractionalScale);
        }

        if (FindGlobal("wp_cursor_shape_manager_v1") is { } cursorShape)
        {
            CursorShapeManager = global::Winit.Wayland.CursorShapeManager.Bind(this, cursorShape);
            foreach (WinitSeatState seat in Seats)
            {
                seat.EnsureCursorShape();
            }
        }

        if (FindGlobal("zwp_pointer_constraints_v1") is { } pointerConstraints)
        {
            PointerConstraints = PointerConstraintsState.Bind(this, pointerConstraints);
        }

        if (FindGlobal("zwp_relative_pointer_manager_v1") is { } relativePointer)
        {
            RelativePointer = RelativePointerState.Bind(this, relativePointer);
            foreach (WinitSeatState seat in Seats)
            {
                seat.EnsureRelativePointer();
            }
        }

        if (FindGlobal("zwp_pointer_gestures_v1") is { Version: >= 3 } pointerGestures)
        {
            PointerGestures = PointerGesturesState.Bind(this, pointerGestures);
            foreach (WinitSeatState seat in Seats)
            {
                seat.EnsurePointerGestures();
            }
        }

        if (FindGlobal("xdg_activation_v1") is { } activation)
        {
            XdgActivation = XdgActivationState.Bind(this, activation);
        }

        if (FindGlobal("zxdg_decoration_manager_v1") is { } decorationManager)
        {
            XdgDecorationManager = XdgDecorationManagerState.Bind(this, decorationManager);
        }

        if (FindGlobal("xdg_toplevel_icon_manager_v1") is { } toplevelIconManager)
        {
            XdgToplevelIconManager = XdgToplevelIconManagerState.Bind(this, toplevelIconManager);
        }

        if (FindGlobal("zwp_text_input_manager_v3") is { } textInputManager)
        {
            TextInputState = global::Winit.Wayland.TextInputState.Bind(this, textInputManager);
            foreach (WinitSeatState seat in Seats)
            {
                seat.EnsureTextInput();
            }
        }

        if (FindGlobal("zwp_tablet_manager_v2") is { } tabletManager)
        {
            TabletState = TabletManager.Bind(this, tabletManager);
            foreach (WinitSeatState seat in Seats)
            {
                seat.EnsureTabletSeat();
            }
        }

        BlurManager = BgrEffectManager.TryBind(this);

        if (FindGlobal("wl_subcompositor") is { } subcompositor)
        {
            Subcompositor = new WlSubcompositor(
                BindGlobal(subcompositor, WlCoreInterfaces.Subcompositor, maxVersion: 1).Value);
        }

        _coreGlobalsBound = true;
    }

    private void InstallXdgWmBaseDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            XdgWmBase,
            &XdgWmBaseDispatcher,
            (void*)GCHandle.ToIntPtr(_registryHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for xdg_wm_base.");
        }
    }

    private void Pong(uint serial)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Uint = serial;
        PInvoke.WlProxyMarshalArray(XdgWmBase, XdgWmBaseRequest.Pong, args);
    }

    private WlProxy BindRequired(string interfaceName, WlInterface* @interface, uint maxVersion)
    {
        WaylandGlobal global = FindGlobal(interfaceName)
            ?? throw new InvalidOperationException($"Wayland compositor did not advertise {interfaceName}.");
        return BindGlobal(global, @interface, maxVersion);
    }

    private void AddGlobal(uint name, string interfaceName, uint version)
    {
        WaylandGlobal global = new(name, interfaceName, version);
        Globals.RemoveAll(existing => existing.Name == name);
        Globals.Add(global);

        if (interfaceName == "wl_output")
        {
            RemoveMonitor(name);
            Monitors.Add(MonitorHandle.Bind(this, global));
        }
        else if (interfaceName == "wl_seat")
        {
            RemoveSeat(name);
            Seats.Add(WinitSeatState.Bind(this, global));
        }

        if (_coreGlobalsBound)
        {
            BindOptionalGlobal(global);
        }
    }

    private void RemoveGlobal(uint name)
    {
        WaylandGlobal? removed = Globals.FirstOrDefault(global => global.Name == name);
        Globals.RemoveAll(global => global.Name == name);
        switch (removed?.Interface)
        {
            case "wp_viewporter":
                ViewporterState?.Dispose();
                ViewporterState = null;
                break;
            case "wp_fractional_scale_manager_v1":
                FractionalScalingManager?.Dispose();
                FractionalScalingManager = null;
                break;
            case "wp_cursor_shape_manager_v1":
                CursorShapeManager?.Dispose();
                CursorShapeManager = null;
                break;
            case "zwp_pointer_constraints_v1":
                PointerConstraints?.Dispose();
                PointerConstraints = null;
                break;
            case "zwp_relative_pointer_manager_v1":
                RelativePointer?.Dispose();
                RelativePointer = null;
                break;
            case "zwp_pointer_gestures_v1":
                PointerGestures?.Dispose();
                PointerGestures = null;
                break;
            case "xdg_activation_v1":
                XdgActivation?.Dispose();
                XdgActivation = null;
                break;
            case "zxdg_decoration_manager_v1":
                XdgDecorationManager?.Dispose();
                XdgDecorationManager = null;
                break;
            case "xdg_toplevel_icon_manager_v1":
                XdgToplevelIconManager?.Dispose();
                XdgToplevelIconManager = null;
                break;
            case "zwp_text_input_manager_v3":
                TextInputState?.Dispose();
                TextInputState = null;
                break;
            case "zwp_tablet_manager_v2":
                TabletState?.Dispose();
                TabletState = null;
                break;
            case "wl_subcompositor":
                if (!Subcompositor.IsNull)
                {
                    DestroySubcompositor(Subcompositor);
                    Subcompositor = WlSubcompositor.Null;
                }

                break;
            case "ext_background_effect_manager_v1":
            case "org_kde_kwin_blur_manager":
                BlurManager?.Dispose();
                BlurManager = BgrEffectManager.TryBind(this);
                break;
        }

        RemoveMonitor(name);
        RemoveSeat(name);
    }

    private static void DestroyXdgWmBase(XdgWmBase wmBase)
    {
        PInvoke.WlProxyMarshalArrayFlags(
            wmBase,
            XdgWmBaseRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(wmBase),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    private static void DestroySubcompositor(WlSubcompositor subcompositor)
    {
        PInvoke.WlProxyMarshalArrayFlags(
            subcompositor,
            WlSubcompositorRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(subcompositor),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    private static void ReleaseCompositor(WlCompositor compositor)
    {
        uint version = PInvoke.WlProxyGetVersion(compositor);
        if (version >= 6)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                compositor,
                WlCompositorRequest.Release,
                null,
                version,
                WlProxyMarshalFlags.Destroy,
                null);
        }
        else
        {
            PInvoke.WlProxyDestroy(compositor);
        }
    }

    private void BindOptionalGlobal(WaylandGlobal global)
    {
        switch (global.Interface)
        {
            case "wp_viewporter" when ViewporterState is null:
                ViewporterState = global::Winit.Wayland.ViewporterState.Bind(this, global);
                break;
            case "wp_fractional_scale_manager_v1" when FractionalScalingManager is null:
                FractionalScalingManager = global::Winit.Wayland.FractionalScalingManager.Bind(this, global);
                break;
            case "wp_cursor_shape_manager_v1" when CursorShapeManager is null:
                CursorShapeManager = global::Winit.Wayland.CursorShapeManager.Bind(this, global);
                foreach (WinitSeatState seat in Seats)
                {
                    seat.EnsureCursorShape();
                }

                break;
            case "zwp_pointer_constraints_v1" when PointerConstraints is null:
                PointerConstraints = PointerConstraintsState.Bind(this, global);
                break;
            case "zwp_relative_pointer_manager_v1" when RelativePointer is null:
                RelativePointer = RelativePointerState.Bind(this, global);
                foreach (WinitSeatState seat in Seats)
                {
                    seat.EnsureRelativePointer();
                }

                break;
            case "zwp_pointer_gestures_v1" when global.Version >= 3 && PointerGestures is null:
                PointerGestures = PointerGesturesState.Bind(this, global);
                foreach (WinitSeatState seat in Seats)
                {
                    seat.EnsurePointerGestures();
                }

                break;
            case "xdg_activation_v1" when XdgActivation is null:
                XdgActivation = XdgActivationState.Bind(this, global);
                break;
            case "zxdg_decoration_manager_v1" when XdgDecorationManager is null:
                XdgDecorationManager = XdgDecorationManagerState.Bind(this, global);
                break;
            case "xdg_toplevel_icon_manager_v1" when XdgToplevelIconManager is null:
                XdgToplevelIconManager = XdgToplevelIconManagerState.Bind(this, global);
                break;
            case "zwp_text_input_manager_v3" when TextInputState is null:
                TextInputState = global::Winit.Wayland.TextInputState.Bind(this, global);
                foreach (WinitSeatState seat in Seats)
                {
                    seat.EnsureTextInput();
                }

                break;
            case "zwp_tablet_manager_v2" when TabletState is null:
                TabletState = TabletManager.Bind(this, global);
                foreach (WinitSeatState seat in Seats)
                {
                    seat.EnsureTabletSeat();
                }

                break;
            case "wl_subcompositor" when Subcompositor.IsNull:
                Subcompositor = new WlSubcompositor(
                    BindGlobal(global, WlCoreInterfaces.Subcompositor, maxVersion: 1).Value);
                break;
            case "ext_background_effect_manager_v1" when BlurManager is null:
            case "org_kde_kwin_blur_manager" when BlurManager is null:
                BlurManager = BgrEffectManager.TryBind(this);
                break;
        }
    }

    private void RemoveMonitor(uint name)
    {
        int index = Monitors.FindIndex(monitor => monitor.NativeId == name);
        if (index < 0)
        {
            return;
        }

        MonitorHandle monitor = Monitors[index];
        Monitors.RemoveAt(index);
        foreach (Window window in Windows.Values.ToArray())
        {
            window.HandleSurfaceLeave(monitor);
        }

        monitor.Dispose();
    }

    private void RemoveSeat(uint name)
    {
        int index = Seats.FindIndex(seat => seat.NativeId == name);
        if (index < 0)
        {
            return;
        }

        WinitSeatState seat = Seats[index];
        Seats.RemoveAt(index);
        seat.Dispose();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int RegistryDispatcher(
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

        WinitState? state = GCHandle.FromIntPtr((nint)implementation).Target as WinitState;
        if (state is null || state._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case WlRegistryEvent.Global:
            {
                string interfaceName = Marshal.PtrToStringUTF8((nint)args[1].String) ?? string.Empty;
                state.AddGlobal(args[0].Uint, interfaceName, args[2].Uint);
                break;
            }
            case WlRegistryEvent.GlobalRemove:
                state.RemoveGlobal(args[0].Uint);
                break;
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int XdgWmBaseDispatcher(
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

        WinitState? state = GCHandle.FromIntPtr((nint)implementation).Target as WinitState;
        if (state is null || state._disposed)
        {
            return 0;
        }

        if (opcode == XdgWmBaseEvent.Ping)
        {
            state.Pong(args[0].Uint);
        }

        return 0;
    }
}

internal struct WindowCompositorUpdate(WindowId windowId)
{
    public WindowId WindowId { get; } = windowId;

    public bool Resized { get; set; }

    public bool ScaleChanged { get; set; }

    public bool CloseWindow { get; set; }
}

internal sealed unsafe class WaylandConnection : IDisposable
{
    private bool _disposed;

    private WaylandConnection(WlDisplay display, WlEventQueue eventQueue, WlRegistry registry)
    {
        Display = display;
        EventQueue = eventQueue;
        Registry = registry;
    }

    public WlDisplay Display { get; }

    public WlEventQueue EventQueue { get; }

    public WlRegistry Registry { get; private set; }

    public int FileDescriptor => PInvoke.WlDisplayGetFd(Display);

    public static WaylandConnection ConnectToEnv()
    {
        WlDisplay display = PInvoke.WlDisplayConnect(null);
        if (display.IsNull && Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is { Length: > 0 } displayName)
        {
            using Utf8Buffer name = Utf8Buffer.FromString(displayName);
            display = PInvoke.WlDisplayConnect(name.Pointer);
        }

        if (display.IsNull)
        {
            throw new InvalidOperationException("Failed to connect to Wayland display from the environment.");
        }

        WlEventQueue queue = PInvoke.WlDisplayCreateQueue(display);
        if (queue.IsNull)
        {
            PInvoke.WlDisplayDisconnect(display);
            throw new InvalidOperationException("Failed to create Wayland event queue.");
        }

        WlRegistry registry = GetRegistry(display);
        if (registry.IsNull)
        {
            PInvoke.WlEventQueueDestroy(queue);
            PInvoke.WlDisplayDisconnect(display);
            throw new InvalidOperationException("Failed to obtain Wayland registry.");
        }

        PInvoke.WlProxySetQueue(registry, queue);
        return new WaylandConnection(display, queue, registry);
    }

    private static WlRegistry GetRegistry(WlDisplay display)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            new WlProxy(display.Value),
            WlDisplayRequest.GetRegistry,
            WlCoreInterfaces.Registry,
            version: 1,
            WlProxyMarshalFlags.None,
            args);
        return new WlRegistry(proxy.Value);
    }

    public void Roundtrip()
    {
        int result = PInvoke.WlDisplayRoundtripQueue(Display, EventQueue);
        if (result < 0)
        {
            CheckError();
            throw new InvalidOperationException("wl_display_roundtrip_queue failed.");
        }
    }

    public void Dispatch()
    {
        int result = PInvoke.WlDisplayDispatchQueue(Display, EventQueue);
        if (result < 0)
        {
            CheckError();
            throw new InvalidOperationException("wl_display_dispatch_queue failed.");
        }
    }

    public void PrepareRead()
    {
        while (PInvoke.WlDisplayPrepareReadQueue(Display, EventQueue) != 0)
        {
            DispatchPending();
        }
    }

    public void CancelRead()
    {
        PInvoke.WlDisplayCancelRead(Display);
    }

    public void ReadEvents()
    {
        int result = PInvoke.WlDisplayReadEvents(Display);
        if (result < 0)
        {
            CheckError();
            throw new InvalidOperationException("wl_display_read_events failed.");
        }
    }

    public void DispatchPending()
    {
        int result = PInvoke.WlDisplayDispatchQueuePending(Display, EventQueue);
        if (result < 0)
        {
            CheckError();
            throw new InvalidOperationException("wl_display_dispatch_queue_pending failed.");
        }
    }

    public void Flush()
    {
        int result = PInvoke.WlDisplayFlush(Display);
        if (result < 0)
        {
            CheckError();
        }
    }

    public void CheckError()
    {
        int error = PInvoke.WlDisplayGetError(Display);
        if (error == 0)
        {
            return;
        }

        WlInterface* protocolInterface = null;
        uint protocolId = 0;
        uint protocolCode = PInvoke.WlDisplayGetProtocolError(Display, &protocolInterface, &protocolId);
        string? protocolName = protocolInterface is null
            ? null
            : Marshal.PtrToStringUTF8((nint)protocolInterface->Name);

        string protocol = protocolName is null
            ? string.Empty
            : $" protocol={protocolName}@{protocolId} code={protocolCode}";
        throw new InvalidOperationException($"Wayland display error errno={error}{protocol}.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!Registry.IsNull)
        {
            PInvoke.WlProxyDestroy(Registry);
            Registry = WlRegistry.Null;
        }

        if (!EventQueue.IsNull)
        {
            PInvoke.WlEventQueueDestroy(EventQueue);
        }

        if (!Display.IsNull)
        {
            PInvoke.WlDisplayDisconnect(Display);
        }
    }
}
