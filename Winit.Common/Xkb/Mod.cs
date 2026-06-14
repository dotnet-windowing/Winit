using System.Text;
using Winit.Core;

namespace Winit.Common.Xkb;

public enum XkbError
{
    XkbNotFound,
}

public static class Xkb
{
    private static int s_resetDeadKeys;

    public static void ResetDeadKeys()
    {
        Interlocked.Exchange(ref s_resetDeadKeys, 1);
    }

    internal static bool TakeResetDeadKeys()
    {
        return Interlocked.Exchange(ref s_resetDeadKeys, 0) != 0;
    }
}

public sealed class Context : IDisposable
{
    private readonly XkbContext _context;
    private readonly List<byte> _scratchBuffer = new(8);
    private XkbComposeTable? _composeTable;
    private XkbComposeState? _composeState1;
    private XkbComposeState? _composeState2;
    private XkbKeymap? _keymap;
    private XkbState? _state;
    private bool _disposed;

    private Context(XkbContext context)
    {
        _context = context;
        _composeTable = XkbComposeTable.New(context);
        _composeState1 = _composeTable?.NewState();
        _composeState2 = _composeTable?.NewState();

        if (_composeTable is null || _composeState1 is null || _composeState2 is null)
        {
            _composeState2?.Dispose();
            _composeState1?.Dispose();
            _composeTable?.Dispose();
            _composeState2 = null;
            _composeState1 = null;
            _composeTable = null;
        }
    }

    public int CoreKeyboardId { get; private set; }

    public static Context New()
    {
        return new Context(XkbContext.New());
    }

    public static unsafe Context FromX11Xkb(nint xcbConnection)
    {
        int result = PInvoke.xkb_x11_setup_xkb_extension(
            xcbConnection,
            1,
            2,
            PInvoke.XkbX11SetupXkbExtensionNoFlags,
            null,
            null,
            null,
            null);

        if (result != 1)
        {
            throw new InvalidOperationException("libxkbcommon-x11 could not initialize the XKB extension.");
        }

        Context context = New();
        context.CoreKeyboardId = PInvoke.xkb_x11_get_core_keyboard_device_id(xcbConnection);
        context.SetKeymapFromX11(xcbConnection);
        return context;
    }

    public XkbState? State => _state;

    public XkbKeymap? Keymap => _keymap;

    public void SetKeymapFromX11(nint xcbConnection)
    {
        XkbKeymap? keymap = XkbKeymap.FromX11Keymap(_context, xcbConnection, CoreKeyboardId);
        XkbState? state = keymap is null ? null : XkbState.NewX11(xcbConnection, keymap, CoreKeyboardId);

        _state?.Dispose();
        _keymap?.Dispose();
        _state = state;
        _keymap = keymap;
    }

    public KeyContext? KeyContext()
    {
        if (_state is null || _keymap is null)
        {
            return null;
        }

        return new KeyContext(_state, _keymap, _composeState1, _composeState2, _scratchBuffer);
    }

    public KeyContext? KeyContextWithState(XkbState state)
    {
        if (_keymap is null)
        {
            return null;
        }

        return new KeyContext(state, _keymap, _composeState1, _composeState2, _scratchBuffer);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _state?.Dispose();
        _keymap?.Dispose();
        _composeState2?.Dispose();
        _composeState1?.Dispose();
        _composeTable?.Dispose();
        _context.Dispose();
        _disposed = true;
    }
}

public sealed class KeyContext
{
    private readonly XkbState _state;
    private readonly XkbKeymap _keymap;
    private readonly XkbComposeState? _composeState1;
    private readonly XkbComposeState? _composeState2;
    private readonly List<byte> _scratchBuffer;

    internal KeyContext(
        XkbState state,
        XkbKeymap keymap,
        XkbComposeState? composeState1,
        XkbComposeState? composeState2,
        List<byte> scratchBuffer)
    {
        _state = state;
        _keymap = keymap;
        _composeState1 = composeState1;
        _composeState2 = composeState2;
        _scratchBuffer = scratchBuffer;
    }

    public KeyEvent ProcessKeyEvent(uint keycode, ElementState stateValue, bool repeat)
    {
        uint keysym = _state.GetOneSymRaw(keycode);
        string? text = stateValue == ElementState.Pressed ? StateUtf8(keycode) : null;
        PhysicalKey physicalKey = XkbKeymap.RawKeycodeToPhysicalKey(keycode);
        Key logicalKey = KeyFromKeysymOrText(keysym, text);

        uint layout = _state.Layout(keycode);
        uint keyWithoutModifiersSym = _keymap.GetKeysymByLevel(layout, keycode, 0);
        string? textWithoutModifiers = KeysymToUtf8Raw(keyWithoutModifiersSym);
        Key keyWithoutModifiers = KeyFromKeysymOrText(keyWithoutModifiersSym, textWithoutModifiers);

        return new KeyEvent(
            physicalKey,
            logicalKey,
            text,
            XkbKeymap.KeyLocation(physicalKey, keysym),
            stateValue,
            repeat,
            text,
            keyWithoutModifiers);
    }

    public string? KeysymToUtf8Raw(uint keysym)
    {
        _scratchBuffer.Clear();
        _scratchBuffer.EnsureCapacity(8);

        while (true)
        {
            int capacity = _scratchBuffer.Capacity;
            byte[] buffer = new byte[capacity];
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    int written = PInvoke.xkb_keysym_to_utf8(keysym, (sbyte*)ptr, (nuint)capacity);
                    if (written == 0)
                    {
                        return null;
                    }

                    if (written == -1 || written >= capacity)
                    {
                        _scratchBuffer.EnsureCapacity(capacity + 8);
                        continue;
                    }

                    return Encoding.UTF8.GetString(buffer, 0, Math.Max(0, written - 1));
                }
            }
        }
    }

    private string? StateUtf8(uint keycode)
    {
        _scratchBuffer.Clear();
        _scratchBuffer.EnsureCapacity(8);

        while (true)
        {
            int capacity = _scratchBuffer.Capacity;
            byte[] buffer = new byte[capacity];
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    int written = _state.GetUtf8Raw(keycode, (sbyte*)ptr, (nuint)capacity);
                    if (written == 0)
                    {
                        return null;
                    }

                    if (written == -1 || written >= capacity)
                    {
                        _scratchBuffer.EnsureCapacity(capacity + 8);
                        continue;
                    }

                    return Encoding.UTF8.GetString(buffer, 0, Math.Max(0, written - 1));
                }
            }
        }
    }

    private static Key KeyFromKeysymOrText(uint keysym, string? text)
    {
        Key key = XkbKeymap.KeysymToKey(keysym);
        return IsUnidentified(key) && text is { Length: > 0 }
            ? Key.FromCharacter(text)
            : key;
    }

    private static bool IsUnidentified(Key key)
    {
        return key.TryGetValue(out Key.Unidentified _);
    }
}

internal sealed class XkbContext : IDisposable
{
    private nint _context;

    private XkbContext(nint context)
    {
        _context = context;
    }

    public nint Handle => _context;

    public static XkbContext New()
    {
        nint context = PInvoke.xkb_context_new(PInvoke.XkbContextNoFlags);
        if (context == 0)
        {
            throw new InvalidOperationException("libxkbcommon could not create an XKB context.");
        }

        return new XkbContext(context);
    }

    public void Dispose()
    {
        nint context = _context;
        if (context == 0)
        {
            return;
        }

        _context = 0;
        PInvoke.xkb_context_unref(context);
    }
}
