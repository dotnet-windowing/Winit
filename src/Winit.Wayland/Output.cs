using Winit.Core;
using Winit.Dpi;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.Wayland;

internal sealed unsafe class MonitorHandle : IMonitorHandleProvider, IDisposable
{
    private readonly Lock _lock = new();
    private readonly List<VideoModeHandle> _modes = [];
    private GCHandle _selfHandle;
    private bool _disposed;

    private MonitorHandle(WaylandGlobal outputGlobal, WlOutput output)
    {
        Global = outputGlobal;
        Output = output;
        _selfHandle = GCHandle.Alloc(this);
    }

    public UInt128 Id => NativeId;

    public ulong NativeId => Global.Name;

    public string? Name
    {
        get
        {
            lock (_lock)
            {
                return _name;
            }
        }
    }

    public PhysicalPosition<int>? Position
    {
        get
        {
            lock (_lock)
            {
                return _position.ToPhysical<int>(ScaleFactor);
            }
        }
    }

    public double ScaleFactor
    {
        get
        {
            lock (_lock)
            {
                return _scaleFactor;
            }
        }
    }

    public VideoMode? CurrentVideoMode
    {
        get
        {
            lock (_lock)
            {
                return _modes.FirstOrDefault(mode => mode.Current)?.Mode;
            }
        }
    }

    public IEnumerable<VideoMode> VideoModes
    {
        get
        {
            lock (_lock)
            {
                return _modes.Select(mode => mode.Mode).ToArray();
            }
        }
    }

    internal WaylandGlobal Global { get; }

    internal WlOutput Output { get; private set; }

    private string? _name;
    private string? _description;
    private string? _make;
    private string? _model;
    private LogicalPosition<int> _position = new(0, 0);
    private double _scaleFactor = 1.0;

    public static MonitorHandle Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, WlCoreInterfaces.Output, maxVersion: 4);
        MonitorHandle monitor = new(global, new WlOutput(proxy.Value));
        monitor.InstallDispatcher();
        return monitor;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!Output.IsNull)
        {
            uint version = PInvoke.WlProxyGetVersion(Output);
            if (version >= 3)
            {
                PInvoke.WlProxyMarshalArrayFlags(
                    Output,
                    WlOutputRequest.Release,
                    null,
                    version,
                    WlProxyMarshalFlags.Destroy,
                    null);
            }
            else
            {
                PInvoke.WlProxyDestroy(Output);
            }

            Output = WlOutput.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            Output,
            &OutputDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            Dispose();
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_output.");
        }
    }

    private void HandleGeometry(WlArgument* args)
    {
        lock (_lock)
        {
            _position = new LogicalPosition<int>(args[0].Int, args[1].Int);
            _make = Marshal.PtrToStringUTF8((nint)args[4].String);
            _model = Marshal.PtrToStringUTF8((nint)args[5].String);
        }
    }

    private void HandleMode(WlArgument* args)
    {
        WlOutputModeFlags flags = (WlOutputModeFlags)args[0].Uint;
        int width = args[1].Int;
        int height = args[2].Int;
        int refresh = args[3].Int;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        VideoMode mode = new(
            new PhysicalSize<uint>(checked((uint)width), checked((uint)height)),
            bitDepth: null,
            refresh > 0 ? checked((uint)refresh) : null);
        VideoModeHandle handle = new((flags & WlOutputModeFlags.Current) != 0, mode);

        lock (_lock)
        {
            int existing = _modes.FindIndex(existingMode => existingMode.Mode.Equals(mode));
            if (existing >= 0)
            {
                _modes[existing] = handle;
            }
            else
            {
                _modes.Add(handle);
            }
        }
    }

    private void HandleScale(WlArgument* args)
    {
        int scale = args[0].Int;
        if (scale <= 0)
        {
            return;
        }

        lock (_lock)
        {
            _scaleFactor = scale;
        }
    }

    private void HandleName(WlArgument* args)
    {
        lock (_lock)
        {
            _name = Marshal.PtrToStringUTF8((nint)args[0].String);
        }
    }

    private void HandleDescription(WlArgument* args)
    {
        lock (_lock)
        {
            _description = Marshal.PtrToStringUTF8((nint)args[0].String);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OutputDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not MonitorHandle monitor || monitor._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case WlOutputEvent.Geometry:
                monitor.HandleGeometry(args);
                break;
            case WlOutputEvent.Mode:
                monitor.HandleMode(args);
                break;
            case WlOutputEvent.Scale:
                monitor.HandleScale(args);
                break;
            case WlOutputEvent.Name:
                monitor.HandleName(args);
                break;
            case WlOutputEvent.Description:
                monitor.HandleDescription(args);
                break;
        }

        return 0;
    }
}

internal sealed record VideoModeHandle(bool Current, VideoMode Mode);
