using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;

namespace Winit.Wayland;

internal sealed unsafe class WinitSeatState : IDisposable
{
    private readonly WinitState _state;
    private readonly Lock _lock = new();
    private readonly GCHandle _selfHandle;
    private bool _disposed;

    private WinitSeatState(WinitState state, WaylandGlobal global, WlSeat seat)
    {
        _state = state;
        Global = global;
        Seat = seat;
        ObjectId = PInvoke.WlProxyGetId(seat);
        _selfHandle = GCHandle.Alloc(this);
    }

    public WaylandGlobal Global { get; }

    public ulong NativeId => Global.Name;

    public uint ObjectId { get; }

    public WlSeat Seat { get; private set; }

    public WlSeatCapability Capabilities
    {
        get
        {
            lock (_lock)
            {
                return _capabilities;
            }
        }
    }

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

    public bool HasPointer => (Capabilities & WlSeatCapability.Pointer) != 0;

    public bool HasKeyboard => (Capabilities & WlSeatCapability.Keyboard) != 0;

    public bool HasTouch => (Capabilities & WlSeatCapability.Touch) != 0;

    public bool TryGetPointerForWindow(WindowId windowId, out WinitPointerData pointer)
    {
        if (_pointer is { FocusedWindow: { } focusedWindow } value && focusedWindow.Equals(windowId))
        {
            pointer = value;
            return true;
        }

        pointer = null!;
        return false;
    }

    public void ReleasePointerGrabForSurface(WlSurface surface)
    {
        _pointer?.ReleaseCursorGrabForSurface(surface);
    }

    public int? KeyboardRepeatTimeoutMilliseconds(Instant now)
    {
        return _keyboardState?.RepeatTimeoutMilliseconds(now);
    }

    public bool DispatchKeyboardRepeat(Instant now)
    {
        return _keyboardState?.DispatchRepeat(now) == true;
    }

    public void EnsureTextInput()
    {
        if (_textInput is null && _state.TextInputState is { } textInputState)
        {
            _textInput = textInputState.GetTextInput(_state, Seat);
        }
    }

    public void EnsurePointerGestures()
    {
        if (_pointer is null || _state.PointerGestures is not { } pointerGestures)
        {
            return;
        }

        _pointerGesturePinch ??= pointerGestures.GetPinchGesture(_state, _pointer.Pointer);
        _pointerGestureHold ??= pointerGestures.GetHoldGesture(_state, _pointer.Pointer);
    }

    public void EnsureRelativePointer()
    {
        if (_pointer is null || _state.RelativePointer is not { } relativePointer)
        {
            return;
        }

        _relativePointer ??= relativePointer.GetRelativePointer(_state, _pointer.Pointer);
    }

    public void EnsureCursorShape()
    {
        if (_pointer is null || _state.CursorShapeManager is not { } cursorShapeManager)
        {
            return;
        }

        _pointer.EnsureCursorShape(cursorShapeManager);
    }

    public void EnsureTabletSeat()
    {
        if (_tablet is null && _state.TabletState is { } tabletState)
        {
            _tablet = tabletState.GetTabletSeat(_state, Seat);
        }
    }

    private WlSeatCapability _capabilities;
    private string? _name;
    private WinitPointerData? _pointer;
    private WinitRelativePointer? _relativePointer;
    private WinitPointerGesturePinch? _pointerGesturePinch;
    private WinitPointerGestureHold? _pointerGestureHold;
    private KeyboardState? _keyboardState;
    private WinitTouchData? _touch;
    private WinitTextInput? _textInput;
    private WinitTabletSeat? _tablet;

    public static WinitSeatState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, WlCoreInterfaces.Seat, maxVersion: 9);
        WinitSeatState seat = new(state, global, new WlSeat(proxy.Value));

        try
        {
            seat.InstallDispatcher();
            return seat;
        }
        catch
        {
            seat.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _touch?.Dispose();
        _touch = null;
        _tablet?.Dispose();
        _tablet = null;
        _textInput?.Dispose();
        _textInput = null;
        _keyboardState?.Dispose();
        _keyboardState = null;
        _relativePointer?.Dispose();
        _relativePointer = null;
        _pointerGestureHold?.Dispose();
        _pointerGestureHold = null;
        _pointerGesturePinch?.Dispose();
        _pointerGesturePinch = null;
        _pointer?.Dispose();
        _pointer = null;

        if (!Seat.IsNull)
        {
            uint version = PInvoke.WlProxyGetVersion(Seat);
            if (version >= 5)
            {
                PInvoke.WlProxyMarshalArrayFlags(
                    Seat,
                    WlSeatRequest.Release,
                    null,
                    version,
                    WlProxyMarshalFlags.Destroy,
                    null);
            }
            else
            {
                PInvoke.WlProxyDestroy(Seat);
            }

            Seat = WlSeat.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            Seat,
            &SeatDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_seat.");
        }
    }

    private void HandleCapabilities(WlSeatCapability capabilities)
    {
        WlSeatCapability previous;
        lock (_lock)
        {
            previous = _capabilities;
            _capabilities = capabilities;
        }

        if ((capabilities & WlSeatCapability.Pointer) != 0)
        {
            _pointer ??= WinitPointerData.Create(_state, this);
            EnsureCursorShape();
            EnsureRelativePointer();
            EnsurePointerGestures();
        }
        else if ((previous & WlSeatCapability.Pointer) != 0)
        {
            _relativePointer?.Dispose();
            _relativePointer = null;
            _pointerGestureHold?.Dispose();
            _pointerGestureHold = null;
            _pointerGesturePinch?.Dispose();
            _pointerGesturePinch = null;
            _pointer?.Dispose();
            _pointer = null;
        }

        if ((capabilities & WlSeatCapability.Keyboard) != 0)
        {
            _keyboardState ??= KeyboardState.Create(_state, this);
        }
        else if ((previous & WlSeatCapability.Keyboard) != 0)
        {
            _keyboardState?.Dispose();
            _keyboardState = null;
        }

        if ((capabilities & WlSeatCapability.Touch) != 0)
        {
            _touch ??= WinitTouchData.Create(_state, this);
        }
        else if ((previous & WlSeatCapability.Touch) != 0)
        {
            _touch?.Dispose();
            _touch = null;
        }

        EnsureTextInput();
        EnsureTabletSeat();
    }

    private void HandleName(string? name)
    {
        lock (_lock)
        {
            _name = name;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int SeatDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitSeatState seat || seat._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case WlSeatEvent.Capabilities:
                seat.HandleCapabilities((WlSeatCapability)args[0].Uint);
                break;
            case WlSeatEvent.Name:
                seat.HandleName(Marshal.PtrToStringUTF8((nint)args[0].String));
                break;
        }

        return 0;
    }
}
