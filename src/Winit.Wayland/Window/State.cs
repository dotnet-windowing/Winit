using Winit.Core;
using Winit.Dpi;

namespace Winit.Wayland;

public sealed class WindowState
{
    private const uint DefaultWidth = 800;
    private const uint DefaultHeight = 600;

    private LogicalSize<uint> _surfaceSize;
    private LogicalSize<uint> _statelessSize;
    private LogicalSize<uint> _pendingConfigureSize;
    private ConfigureBounds? _pendingConfigureBounds;
    private LogicalSize<uint>? _surfaceResizeIncrements;
    private Size? _initialSurfaceSize;
    private ToplevelStateFlags _toplevelStateFlags;
    private ToplevelStateFlags _pendingConfigureStateFlags;
    private bool _pendingConfigureNeedsResize;
    private readonly List<MonitorHandle> _surfaceOutputs = [];
    private readonly HashSet<uint> _seatFocus = [];

    public WindowState(WindowAttributes attributes)
    {
        _initialSurfaceSize = attributes.SurfaceSize
            ?? Size.FromLogical(new LogicalSize<uint>(DefaultWidth, DefaultHeight));
        _surfaceSize = _initialSurfaceSize.Value.ToLogical<uint>(ScaleFactor);
        _statelessSize = _surfaceSize;
        _pendingConfigureSize = _surfaceSize;
        MinSurfaceSize = attributes.MinSurfaceSize;
        MaxSurfaceSize = attributes.MaxSurfaceSize;
        SurfaceResizeIncrements = attributes.SurfaceResizeIncrements?.ToLogical<uint>(ScaleFactor);
        IsResizable = true;
        EnabledButtons = attributes.EnabledButtons;
        Title = attributes.Title;
        IsVisible = attributes.Visible;
        IsDecorated = attributes.Decorations;
        Theme = attributes.PreferredTheme;
        Fullscreen = null;
        IsMaximized = false;
        Cursor = attributes.Cursor;
    }

    public double ScaleFactor { get; private set; } = 1.0;

    internal WlOutputTransform PreferredBufferTransform { get; private set; } = WlOutputTransform.Normal;

    public PhysicalSize<uint> SurfaceSize => _surfaceSize.ToPhysical<uint>(ScaleFactor);

    internal LogicalSize<uint> LogicalSurfaceSize => _surfaceSize;

    public Size? MinSurfaceSize { get; set; }

    public Size? MaxSurfaceSize { get; set; }

    public LogicalSize<uint>? SurfaceResizeIncrements
    {
        get => _surfaceResizeIncrements;
        set => _surfaceResizeIncrements = value;
    }

    public PhysicalSize<uint>? PhysicalSurfaceResizeIncrements =>
        _surfaceResizeIncrements?.ToPhysical<uint>(ScaleFactor);

    public bool? IsVisible { get; set; }

    public bool IsResizable { get; set; }

    public WindowButtons EnabledButtons { get; set; }

    public bool? IsMinimized { get; set; }

    public bool IsMaximized { get; set; }

    public Fullscreen? Fullscreen { get; set; }

    public bool IsDecorated { get; set; }

    internal ZxdgToplevelDecorationV1Mode? ConfiguredDecorationMode { get; set; }

    internal ToplevelWmCapabilities WmCapabilities { get; set; } = ToplevelWmCapabilities.All;

    public bool HasFocus => _seatFocus.Count > 0;

    public Theme? Theme { get; set; }

    public string Title { get; set; }

    public bool Transparent { get; set; }

    public bool Blur { get; set; }

    public WindowLevel WindowLevel { get; set; }

    public bool ContentProtected { get; set; }

    public Cursor Cursor { get; set; }

    public CursorGrabMode CursorGrabMode { get; set; }

    public bool CursorVisible { get; set; } = true;

    internal TextInputClientState? TextInputClientState { get; set; }

    public bool Configured { get; set; }

    public bool RedrawRequested { get; set; }

    public FrameCallbackState FrameCallbackState { get; private set; }

    public bool RequestFrameCallback()
    {
        if (FrameCallbackState == FrameCallbackState.Requested)
        {
            return false;
        }

        FrameCallbackState = FrameCallbackState.Requested;
        return true;
    }

    public void FrameCallbackReceived()
    {
        FrameCallbackState = FrameCallbackState.Received;
    }

    public void FrameCallbackReset()
    {
        FrameCallbackState = FrameCallbackState.None;
    }

    internal void SetPendingConfigure(int width, int height, ToplevelStateFlags stateFlags)
    {
        ApplyInitialSurfaceSize(consume: true);

        bool configured = Configured;
        bool constrain = width <= 0 || height <= 0;
        bool hasConfiguredSize = width > 0 && height > 0;
        bool stateless = IsStateless(stateFlags);
        LogicalSize<uint> fallbackSize = stateless ? _statelessSize : _surfaceSize;
        uint pendingWidth = hasConfiguredSize ? checked((uint)width) : fallbackSize.Width;
        uint pendingHeight = hasConfiguredSize ? checked((uint)height) : fallbackSize.Height;

        if (constrain && _pendingConfigureBounds is { } bounds)
        {
            if (bounds.Width is { } boundWidth)
            {
                pendingWidth = Math.Min(pendingWidth, boundWidth);
            }

            if (bounds.Height is { } boundHeight)
            {
                pendingHeight = Math.Min(pendingHeight, boundHeight);
            }
        }

        LogicalSize<uint> pendingSize = new(Math.Max(1, pendingWidth), Math.Max(1, pendingHeight));
        if ((constrain || stateFlags.HasFlag(ToplevelStateFlags.Resizing)) &&
            !stateFlags.HasFlag(ToplevelStateFlags.Maximized) &&
            !stateFlags.HasFlag(ToplevelStateFlags.Fullscreen) &&
            !stateFlags.HasFlag(ToplevelStateFlags.Tiled) &&
            _surfaceResizeIncrements is { Width: > 0, Height: > 0 } increments)
        {
            LogicalSize<uint> minSize = MinSurfaceSize?.ToLogical<uint>(ScaleFactor)
                ?? new LogicalSize<uint>(1, 1);
            uint deltaWidth = pendingSize.Width > minSize.Width ? pendingSize.Width - minSize.Width : 0;
            uint deltaHeight = pendingSize.Height > minSize.Height ? pendingSize.Height - minSize.Height : 0;
            pendingSize = new LogicalSize<uint>(
                minSize.Width + (deltaWidth / increments.Width) * increments.Width,
                minSize.Height + (deltaHeight / increments.Height) * increments.Height);
        }

        _pendingConfigureNeedsResize = !configured ||
            StateChangeRequiresResize(_toplevelStateFlags, stateFlags);
        _pendingConfigureStateFlags = stateFlags;
        _pendingConfigureSize = pendingSize;
    }

    public void SetPendingConfigureBounds(int width, int height)
    {
        uint? pendingWidth = width > 0 ? checked((uint)width) : null;
        uint? pendingHeight = height > 0 ? checked((uint)height) : null;
        _pendingConfigureBounds = pendingWidth is not null || pendingHeight is not null
            ? new ConfigureBounds(pendingWidth, pendingHeight)
            : null;
    }

    public bool ApplyConfigure()
    {
        bool changed = _pendingConfigureNeedsResize || _surfaceSize != _pendingConfigureSize;
        _surfaceSize = _pendingConfigureSize;
        _toplevelStateFlags = _pendingConfigureStateFlags;
        if (IsStateless(_toplevelStateFlags))
        {
            _statelessSize = _surfaceSize;
        }

        IsMaximized = _toplevelStateFlags.HasFlag(ToplevelStateFlags.Maximized);
        Fullscreen = _toplevelStateFlags.HasFlag(ToplevelStateFlags.Fullscreen)
            ? Winit.Core.Fullscreen.FromBorderless()
            : null;
        Configured = true;
        _pendingConfigureNeedsResize = false;
        return changed;
    }

    public PhysicalSize<uint> RequestSurfaceSize(Size size)
    {
        if (!Configured || IsStateless(_toplevelStateFlags))
        {
            _surfaceSize = size.ToLogical<uint>(ScaleFactor);
            _statelessSize = _surfaceSize;
            _pendingConfigureSize = _surfaceSize;
        }

        return SurfaceSize;
    }

    public bool SetScaleFactor(double scaleFactor)
    {
        if (scaleFactor <= 0.0 || Math.Abs(ScaleFactor - scaleFactor) < double.Epsilon)
        {
            return false;
        }

        ScaleFactor = scaleFactor;
        if (!Configured)
        {
            ApplyInitialSurfaceSize(consume: false);
        }

        return true;
    }

    internal bool SetPreferredBufferTransform(WlOutputTransform transform)
    {
        if (PreferredBufferTransform == transform)
        {
            return false;
        }

        PreferredBufferTransform = transform;
        return true;
    }

    private void ApplyInitialSurfaceSize(bool consume)
    {
        if (_initialSurfaceSize is not { } initialSurfaceSize)
        {
            return;
        }

        _surfaceSize = initialSurfaceSize.ToLogical<uint>(ScaleFactor);
        _statelessSize = _surfaceSize;
        _pendingConfigureSize = _surfaceSize;
        if (consume)
        {
            _initialSurfaceSize = null;
        }
    }

    internal MonitorHandle? CurrentMonitor => _surfaceOutputs.FirstOrDefault();

    internal void SurfaceEntered(MonitorHandle monitor)
    {
        if (_surfaceOutputs.Any(existing => existing.NativeId == monitor.NativeId))
        {
            return;
        }

        _surfaceOutputs.Add(monitor);
    }

    internal void SurfaceLeft(MonitorHandle monitor)
    {
        _surfaceOutputs.RemoveAll(existing => existing.NativeId == monitor.NativeId);
    }

    internal bool AddSeatFocus(uint seatId)
    {
        bool wasUnfocused = !HasFocus;
        _seatFocus.Add(seatId);
        return wasUnfocused && HasFocus;
    }

    internal bool RemoveSeatFocus(uint seatId)
    {
        bool hadFocus = HasFocus;
        _seatFocus.Remove(seatId);
        return hadFocus && !HasFocus;
    }

    private static bool StateChangeRequiresResize(ToplevelStateFlags oldState, ToplevelStateFlags newState)
    {
        const ToplevelStateFlags ignored = ToplevelStateFlags.Activated | ToplevelStateFlags.Suspended;
        return ((oldState ^ newState) & ~ignored) != ToplevelStateFlags.None;
    }

    private static bool IsStateless(ToplevelStateFlags state)
    {
        const ToplevelStateFlags stateful =
            ToplevelStateFlags.Maximized |
            ToplevelStateFlags.Fullscreen |
            ToplevelStateFlags.Tiled;
        return (state & stateful) == ToplevelStateFlags.None;
    }

    private readonly record struct ConfigureBounds(uint? Width, uint? Height);
}

[Flags]
internal enum ToplevelStateFlags
{
    None = 0,
    Maximized = 1 << 0,
    Fullscreen = 1 << 1,
    Resizing = 1 << 2,
    Activated = 1 << 3,
    Suspended = 1 << 4,
    Tiled = 1 << 5,
}

[Flags]
internal enum ToplevelWmCapabilities
{
    None = 0,
    WindowMenu = 1 << 0,
    Maximize = 1 << 1,
    Fullscreen = 1 << 2,
    Minimize = 1 << 3,
    All = WindowMenu | Maximize | Fullscreen | Minimize,
}

public enum FrameCallbackState
{
    None,
    Requested,
    Received,
}
