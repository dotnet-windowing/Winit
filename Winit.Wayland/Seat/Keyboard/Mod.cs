using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Winit.Common.Xkb;
using Winit.Core;

namespace Winit.Wayland;

internal sealed unsafe class KeyboardState : IDisposable
{
    private readonly WinitState _state;
    private readonly WinitSeatState _seat;
    private readonly GCHandle _selfHandle;
    private bool _disposed;
    private WindowId? _focusedWindow;
    private ModifiersState _modifiers;
    private bool _modifiersPending;
    private uint? _currentRepeat;
    private Instant? _nextRepeat;

    private KeyboardState(WinitState state, WinitSeatState seat, WlKeyboard keyboard)
    {
        _state = state;
        _seat = seat;
        Keyboard = keyboard;
        XkbContext = Context.New();
        RepeatInfo = RepeatInfo.Default;
        _selfHandle = GCHandle.Alloc(this);
    }

    public WlKeyboard Keyboard { get; private set; }

    public Context XkbContext { get; }

    public RepeatInfo RepeatInfo { get; private set; }

    public static KeyboardState Create(WinitState state, WinitSeatState seat)
    {
        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        uint version = PInvoke.WlProxyGetVersion(seat.Seat);
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            seat.Seat,
            WlSeatRequest.GetKeyboard,
            WlCoreInterfaces.Keyboard,
            version,
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_seat.get_keyboard failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        KeyboardState keyboard = new(state, seat, new WlKeyboard(proxy.Value));
        keyboard.InstallDispatcher();
        return keyboard;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_focusedWindow is { } windowId && _state.Windows.TryGetValue(windowId, out Window? window))
        {
            if (window.RemoveSeatFocus(_seat.ObjectId))
            {
                _state.PushWindowEvent(windowId, new WindowEvent(new WindowEvent.Focused(false)));
            }
        }

        if (!Keyboard.IsNull)
        {
            uint version = PInvoke.WlProxyGetVersion(Keyboard);
            if (version >= 3)
            {
                PInvoke.WlProxyMarshalArrayFlags(
                    Keyboard,
                    WlKeyboardRequest.Release,
                    null,
                    version,
                    WlProxyMarshalFlags.Destroy,
                    null);
            }
            else
            {
                PInvoke.WlProxyDestroy(Keyboard);
            }

            Keyboard = WlKeyboard.Null;
        }

        XkbContext.Dispose();

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            Keyboard,
            &KeyboardDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for wl_keyboard.");
        }
    }

    private void Keymap(WlKeyboardKeymapFormat format, int fd, uint size)
    {
        try
        {
            if (format != WlKeyboardKeymapFormat.XkbV1 || size == 0)
            {
                return;
            }

            void* mapping = PInvoke.Mmap(
                null,
                size,
                MmapProtection.Read,
                MmapFlags.Private,
                fd,
                0);
            if ((nint)mapping == MmapFlags.Failed)
            {
                throw new InvalidOperationException(
                    $"mmap failed for wl_keyboard.keymap fd={fd} size={size} errno={Marshal.GetLastPInvokeError()}.");
            }

            try
            {
                ReadOnlySpan<byte> bytes = new(mapping, checked((int)size));
                string keymap = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                XkbContext.SetKeymapFromString(keymap);
                StopRepeat();
            }
            finally
            {
                _ = PInvoke.Munmap(mapping, size);
            }
        }
        finally
        {
            _ = PInvoke.Close(fd);
        }
    }

    private void Enter(WlSurface surface)
    {
        if (!_state.TryGetWindow(surface, out Window window))
        {
            _focusedWindow = null;
            return;
        }

        _focusedWindow = window.Id;
        StopRepeat();

        if (window.AddSeatFocus(_seat.ObjectId))
        {
            _state.PushWindowEvent(window.Id, new WindowEvent(new WindowEvent.Focused(true)));
        }

        if (_modifiersPending)
        {
            _modifiersPending = false;
            _state.PushWindowEvent(
                window.Id,
                new WindowEvent(new WindowEvent.ModifiersChanged(Winit.Core.Modifiers.From(_modifiers))));
        }
    }

    private void Leave(WlSurface surface)
    {
        WindowId? focused = _focusedWindow;
        _focusedWindow = null;
        StopRepeat();

        WindowId windowId = !surface.IsNull
            ? WindowId.FromRaw((nuint)surface.Value)
            : focused.GetValueOrDefault();
        if (windowId.Equals(default(WindowId)) || !_state.Windows.TryGetValue(windowId, out Window? window))
        {
            return;
        }

        if (window.RemoveSeatFocus(_seat.ObjectId))
        {
            _state.PushWindowEvent(
                windowId,
                new WindowEvent(new WindowEvent.ModifiersChanged(Winit.Core.Modifiers.From(ModifiersState.None))));
            _state.PushWindowEvent(windowId, new WindowEvent(new WindowEvent.Focused(false)));
        }
    }

    private void Key(uint rawKey, WlKeyboardKeyState keyState)
    {
        if (_focusedWindow is not { } windowId)
        {
            return;
        }

        uint keycode = rawKey + 8;
        ElementState state = keyState == WlKeyboardKeyState.Released
            ? ElementState.Released
            : ElementState.Pressed;
        bool repeat = keyState == WlKeyboardKeyState.Repeated;

        if (keyState == WlKeyboardKeyState.Released && _currentRepeat == keycode)
        {
            StopRepeat();
        }

        KeyEvent keyEvent = XkbContext.KeyContext()?.ProcessKeyEvent(keycode, state, repeat)
            ?? FallbackKeyEvent(keycode, state, repeat);
        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.KeyboardInput(null, keyEvent, false)));

        if (state == ElementState.Pressed)
        {
            StartRepeatIfNeeded(keycode);
        }
    }

    private void Modifiers(uint depressed, uint latched, uint locked, uint group)
    {
        if (XkbContext.State is null)
        {
            return;
        }

        XkbContext.State.UpdateModifiers(depressed, latched, locked, 0, 0, group);
        _modifiers = XkbContext.State.Modifiers();

        if (_focusedWindow is not { } windowId)
        {
            _modifiersPending = true;
            return;
        }

        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.ModifiersChanged(Winit.Core.Modifiers.From(_modifiers))));
    }

    private void SetRepeatInfo(int rate, int delay)
    {
        if (rate <= 0)
        {
            RepeatInfo = new RepeatInfo(new RepeatInfo.Disable());
            StopRepeat();
            return;
        }

        RepeatInfo = new RepeatInfo(new RepeatInfo.Repeat(
            TimeSpan.FromMilliseconds(Math.Max(0, delay)),
            TimeSpan.FromTicks(TimeSpan.TicksPerSecond / rate)));
    }

    public int? RepeatTimeoutMilliseconds(Instant now)
    {
        if (_currentRepeat is null || _nextRepeat is not { } next)
        {
            return null;
        }

        long ticks = next.Timestamp - now.Timestamp;
        if (ticks <= 0)
        {
            return 0;
        }

        double milliseconds = ticks * 1000.0 / TimeProvider.System.TimestampFrequency;
        return milliseconds >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)Math.Ceiling(milliseconds));
    }

    public bool DispatchRepeat(Instant now)
    {
        if (_focusedWindow is not { } windowId ||
            _currentRepeat is not { } keycode ||
            _nextRepeat is not { } next ||
            next.Timestamp > now.Timestamp)
        {
            return false;
        }

        if (!RepeatInfo.TryGetValue(out RepeatInfo.Repeat repeatInfo))
        {
            StopRepeat();
            return false;
        }

        KeyEvent keyEvent = XkbContext.KeyContext()?.ProcessKeyEvent(keycode, ElementState.Pressed, repeat: true)
            ?? FallbackKeyEvent(keycode, ElementState.Pressed, repeat: true);
        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.KeyboardInput(null, keyEvent, false)));

        _nextRepeat = now.CheckedAdd(repeatInfo.Gap) ?? now;
        return true;
    }

    private void StartRepeatIfNeeded(uint keycode)
    {
        if (!RepeatInfo.TryGetValue(out RepeatInfo.Repeat repeatInfo) ||
            XkbContext.Keymap?.KeyRepeats(keycode) != true)
        {
            return;
        }

        _currentRepeat = keycode;
        _nextRepeat = Instant.Now().CheckedAdd(repeatInfo.Delay) ?? Instant.Now();
    }

    private void StopRepeat()
    {
        _currentRepeat = null;
        _nextRepeat = null;
    }

    private static KeyEvent FallbackKeyEvent(uint keycode, ElementState state, bool repeat)
    {
        PhysicalKey physicalKey = XkbKeymap.RawKeycodeToPhysicalKey(keycode);
        Key logicalKey = new(new Key.Unidentified(new NativeKey(new NativeKey.Unidentified())));
        return new KeyEvent(
            physicalKey,
            logicalKey,
            null,
            XkbKeymap.KeyLocation(physicalKey, 0),
            state,
            repeat,
            null,
            logicalKey);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int KeyboardDispatcher(
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

        if (GCHandle.FromIntPtr((nint)implementation).Target is not KeyboardState keyboard || keyboard._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case WlKeyboardEvent.Keymap:
                keyboard.Keymap((WlKeyboardKeymapFormat)args[0].Uint, args[1].Fd, args[2].Uint);
                break;
            case WlKeyboardEvent.Enter:
                keyboard.Enter(new WlSurface(args[1].Object));
                break;
            case WlKeyboardEvent.Leave:
                keyboard.Leave(new WlSurface(args[1].Object));
                break;
            case WlKeyboardEvent.Key:
                keyboard.Key(args[2].Uint, (WlKeyboardKeyState)args[3].Uint);
                break;
            case WlKeyboardEvent.Modifiers:
                keyboard.Modifiers(args[1].Uint, args[2].Uint, args[3].Uint, args[4].Uint);
                break;
            case WlKeyboardEvent.RepeatInfo:
                keyboard.SetRepeatInfo(args[0].Int, args[1].Int);
                break;
        }

        return 0;
    }
}

internal record struct RepeatInfo
{
    public readonly record struct Repeat(TimeSpan Delay, TimeSpan Gap);

    public readonly record struct Disable;

    private const byte RepeatTag = 0;
    private const byte DisableTag = 1;

    private byte _tag;
    private Repeat _repeat;
    private Disable _disable;

    public RepeatInfo(Repeat value)
    {
        this = default;
        _tag = RepeatTag;
        _repeat = value;
    }

    public RepeatInfo(Disable value)
    {
        this = default;
        _tag = DisableTag;
        _disable = value;
    }

    public static RepeatInfo Default => new(new Repeat(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(40)));

    public bool TryGetValue(out Repeat value)
    {
        value = _repeat;
        return _tag == RepeatTag;
    }

    public bool TryGetValue(out Disable value)
    {
        value = _disable;
        return _tag == DisableTag;
    }
}
