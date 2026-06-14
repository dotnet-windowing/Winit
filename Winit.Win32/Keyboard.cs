using System.Globalization;
using Winit.Core;

namespace Winit.Win32;

internal static unsafe class Keyboard
{
    internal const uint MapvkVkToVscEx = 4;
    private const uint MapvkVscToVkEx = 3;
    private const uint ToUnicodeNoStateChange = 0x0004;
    private static readonly Lock s_altGraphLock = new();
    private static readonly Dictionary<nint, bool> s_altGraphByLayout = [];
    internal const ushort VkBack = 0x08;
    internal const ushort VkTab = 0x09;
    internal const ushort VkClear = 0x0C;
    internal const ushort VkReturn = 0x0D;
    internal const ushort VkShift = 0x10;
    internal const ushort VkControl = 0x11;
    internal const ushort VkMenu = 0x12;
    internal const ushort VkPause = 0x13;
    internal const ushort VkCapital = 0x14;
    internal const ushort VkEscape = 0x1B;
    internal const ushort VkSpace = 0x20;
    internal const ushort VkPageUp = 0x21;
    internal const ushort VkPageDown = 0x22;
    internal const ushort VkEnd = 0x23;
    internal const ushort VkHome = 0x24;
    internal const ushort VkLeft = 0x25;
    internal const ushort VkUp = 0x26;
    internal const ushort VkRight = 0x27;
    internal const ushort VkDown = 0x28;
    internal const ushort VkPrint = 0x2A;
    internal const ushort VkPrintScreen = 0x2C;
    internal const ushort VkInsert = 0x2D;
    internal const ushort VkDelete = 0x2E;
    internal const ushort VkHelp = 0x2F;
    internal const ushort VkLWin = 0x5B;
    internal const ushort VkRWin = 0x5C;
    internal const ushort VkApps = 0x5D;
    internal const ushort VkNumpad0 = 0x60;
    internal const ushort VkNumpad9 = 0x69;
    internal const ushort VkMultiply = 0x6A;
    internal const ushort VkAdd = 0x6B;
    internal const ushort VkSeparator = 0x6C;
    internal const ushort VkSubtract = 0x6D;
    internal const ushort VkDecimal = 0x6E;
    internal const ushort VkDivide = 0x6F;
    internal const ushort VkF1 = 0x70;
    internal const ushort VkF4 = 0x73;
    internal const ushort VkF24 = 0x87;
    internal const ushort VkNumLock = 0x90;
    internal const ushort VkScroll = 0x91;
    internal const ushort VkLShift = 0xA0;
    internal const ushort VkRShift = 0xA1;
    internal const ushort VkLControl = 0xA2;
    internal const ushort VkRControl = 0xA3;
    internal const ushort VkLMenu = 0xA4;
    internal const ushort VkRMenu = 0xA5;
    internal const ushort VkBrowserBack = 0xA6;
    internal const ushort VkBrowserForward = 0xA7;
    internal const ushort VkBrowserRefresh = 0xA8;
    internal const ushort VkBrowserStop = 0xA9;
    internal const ushort VkBrowserSearch = 0xAA;
    internal const ushort VkBrowserFavorites = 0xAB;
    internal const ushort VkBrowserHome = 0xAC;
    internal const ushort VkVolumeMute = 0xAD;
    internal const ushort VkVolumeDown = 0xAE;
    internal const ushort VkVolumeUp = 0xAF;
    internal const ushort VkMediaNextTrack = 0xB0;
    internal const ushort VkMediaPrevTrack = 0xB1;
    internal const ushort VkMediaStop = 0xB2;
    internal const ushort VkMediaPlayPause = 0xB3;
    internal const ushort VkLaunchMail = 0xB4;
    internal const ushort VkLaunchMediaSelect = 0xB5;
    internal const ushort VkLaunchApp1 = 0xB6;
    internal const ushort VkLaunchApp2 = 0xB7;
    internal const ushort VkOem1 = 0xBA;
    internal const ushort VkOemPlus = 0xBB;
    internal const ushort VkOemComma = 0xBC;
    internal const ushort VkOemMinus = 0xBD;
    internal const ushort VkOemPeriod = 0xBE;
    internal const ushort VkOem2 = 0xBF;
    internal const ushort VkOem3 = 0xC0;
    internal const ushort VkOem4 = 0xDB;
    internal const ushort VkOem5 = 0xDC;
    internal const ushort VkOem6 = 0xDD;
    internal const ushort VkOem7 = 0xDE;
    internal const ushort VkOem102 = 0xE2;

    public static bool IsKeyMessage(uint message)
    {
        return message is PInvoke.WM_KEYDOWN or PInvoke.WM_SYSKEYDOWN or PInvoke.WM_KEYUP or PInvoke.WM_SYSKEYUP;
    }

    public static ElementState ElementStateForMessage(uint message)
    {
        return message is PInvoke.WM_KEYDOWN or PInvoke.WM_SYSKEYDOWN
            ? ElementState.Pressed
            : ElementState.Released;
    }

    public static Modifiers CurrentModifiers()
    {
        ModifiersState state = KeyboardLayoutCache.Shared.GetAgnosticModifiers();
        ModifiersKeys pressed = ModifiersKeys.None;

        AddModifier(VkLShift, ModifiersKeys.LShift);
        AddModifier(VkRShift, ModifiersKeys.RShift);
        AddModifier(VkLControl, ModifiersKeys.LControl);
        AddModifier(VkRControl, ModifiersKeys.RControl);
        AddModifier(VkLMenu, ModifiersKeys.LAlt);
        AddModifier(VkRMenu, ModifiersKeys.RAlt);
        AddModifier(VkLWin, ModifiersKeys.LMeta);
        AddModifier(VkRWin, ModifiersKeys.RMeta);

        return new Modifiers(state, pressed);

        void AddModifier(ushort virtualKey, ModifiersKeys keyFlag)
        {
            if (!IsKeyPressed(virtualKey))
            {
                return;
            }

            pressed |= keyFlag;
        }
    }

    public static bool IsModifierVirtualKey(ushort virtualKey)
    {
        return virtualKey is VkShift or VkControl or VkMenu
            or VkLShift or VkRShift
            or VkLControl or VkRControl
            or VkLMenu or VkRMenu
            or VkLWin or VkRWin;
    }

    public static KeyEvent CreateKeyEvent(WPARAM wParam, LPARAM lParam, ElementState state)
    {
        ushort virtualKey = unchecked((ushort)wParam.Value);
        KeyLParam keyLParam = KeyLParam.From(lParam);
        ushort scanCode = keyLParam.ScanCode;
        KeyCode keyCode = KeyCodeFromScanCode(scanCode, keyLParam.Extended);
        PhysicalKey physicalKey = keyCode == KeyCode.Unidentified
            ? PhysicalKey.From(new NativeKeyCode(new NativeKeyCode.Windows(scanCode == 0 ? virtualKey : scanCode)))
            : PhysicalKey.From(keyCode);
        KeyLocation location = Location(virtualKey, keyCode, keyLParam);
        Key keyWithoutModifiers = KeyWithoutModifiers(virtualKey, keyCode, keyLParam);
        Key logicalKey = LogicalKey(virtualKey, keyCode, keyLParam, keyWithoutModifiers);
        bool textAllowed = state == ElementState.Pressed && TextInputAllowed();
        string? text = textAllowed && logicalKey.TryGetValue(out Key.Character character) ? character.Value : null;

        return new KeyEvent(
            physicalKey,
            logicalKey,
            text,
            location,
            state,
            state == ElementState.Pressed && keyLParam.IsRepeat,
            text,
            keyWithoutModifiers);
    }

    public static IReadOnlyList<KeyEvent> SynthesizeKeyEvents(ElementState state, bool asyncState)
    {
        Span<byte> keyboardState = stackalloc byte[256];
        if (asyncState)
        {
            for (int virtualKey = 0; virtualKey < keyboardState.Length; virtualKey++)
            {
                keyboardState[virtualKey] = (PInvoke.GetAsyncKeyState(virtualKey) & unchecked((short)0x8000)) != 0
                    ? (byte)0x80
                    : (byte)0;
            }
        }
        else
        {
            fixed (byte* statePtr = keyboardState)
            {
                if (!PInvoke.GetKeyboardState(statePtr))
                {
                    return [];
                }
            }
        }

        List<KeyEvent> events = [];
        if (IsSyntheticKeyPressed(keyboardState, VkCapital))
        {
            AddSyntheticEvent(events, VkCapital, state);
        }

        if (state == ElementState.Pressed)
        {
            AddNonModifierSyntheticEvents(events, keyboardState, state);
            AddModifierSyntheticEvents(events, keyboardState, state);
        }
        else
        {
            AddModifierSyntheticEvents(events, keyboardState, state);
            AddNonModifierSyntheticEvents(events, keyboardState, state);
        }

        return events;
    }

    private static void AddNonModifierSyntheticEvents(
        List<KeyEvent> events,
        ReadOnlySpan<byte> keyboardState,
        ElementState state)
    {
        for (ushort virtualKey = 0; virtualKey <= byte.MaxValue; virtualKey++)
        {
            if (virtualKey is VkControl or VkLControl or VkRControl or
                VkShift or VkLShift or VkRShift or
                VkMenu or VkLMenu or VkRMenu or
                VkCapital)
            {
                continue;
            }

            if (IsSyntheticKeyPressed(keyboardState, virtualKey))
            {
                AddSyntheticEvent(events, virtualKey, state);
            }
        }
    }

    private static void AddModifierSyntheticEvents(
        List<KeyEvent> events,
        ReadOnlySpan<byte> keyboardState,
        ElementState state)
    {
        ReadOnlySpan<ushort> modifiers =
        [
            VkLControl,
            VkLShift,
            VkLMenu,
            VkLWin,
            VkRControl,
            VkRShift,
            VkRMenu,
            VkRWin,
        ];

        foreach (ushort virtualKey in modifiers)
        {
            if (IsSyntheticKeyPressed(keyboardState, virtualKey))
            {
                AddSyntheticEvent(events, virtualKey, state);
            }
        }
    }

    private static bool IsSyntheticKeyPressed(ReadOnlySpan<byte> keyboardState, ushort virtualKey)
    {
        return (keyboardState[virtualKey] & 0x80) != 0;
    }

    private static void AddSyntheticEvent(List<KeyEvent> events, ushort virtualKey, ElementState state)
    {
        KeyEvent? keyEvent = CreateSyntheticKeyEvent(virtualKey, state);
        if (keyEvent is { } value)
        {
            events.Add(value);
        }
    }

    private static KeyEvent? CreateSyntheticKeyEvent(ushort virtualKey, ElementState state)
    {
        nint keyboardLayout = PInvoke.GetKeyboardLayout(0);
        uint scanCode = PInvoke.MapVirtualKeyExW(virtualKey, MapvkVkToVscEx, keyboardLayout);
        if (scanCode == 0)
        {
            return null;
        }

        bool extended = (scanCode & 0xff00) != 0;
        LPARAM lParam = KeyboardLParam(unchecked((ushort)(scanCode & 0xff)), extended, state);
        return CreateKeyEvent(new WPARAM((uint)virtualKey), lParam, state);
    }

    private static LPARAM KeyboardLParam(ushort scanCode, bool extended, ElementState state)
    {
        nuint value = (nuint)scanCode << 16;
        if (extended)
        {
            value |= 1u << 24;
        }

        if (state == ElementState.Released)
        {
            value |= 1u << 30;
            value |= 1u << 31;
        }

        return new LPARAM(unchecked((nint)value));
    }

    private static Key LogicalKey(ushort virtualKey, KeyCode keyCode, KeyLParam keyLParam, Key fallback)
    {
        if (TryText(virtualKey, keyLParam, applyModifiers: true, out string? text))
        {
            return Key.FromCharacter(text);
        }

        if (TryNamedKey(virtualKey, keyCode, keyLParam, out NamedKey named))
        {
            return Key.From(named);
        }

        return fallback.TryGetValue(out Key.Unidentified _)
            ? fallback
            : Key.From(new NativeKey(new NativeKey.Windows(virtualKey)));
    }

    private static Key KeyWithoutModifiers(ushort virtualKey, KeyCode keyCode, KeyLParam keyLParam)
    {
        if (TryText(virtualKey, keyLParam, applyModifiers: false, out string? text))
        {
            return Key.FromCharacter(text);
        }

        if (TryNamedKey(virtualKey, keyCode, keyLParam, out NamedKey named))
        {
            return Key.From(named);
        }

        return Key.From(new NativeKey(new NativeKey.Windows(virtualKey)));
    }

    private static bool TryNamedKey(ushort virtualKey, KeyCode keyCode, KeyLParam keyLParam, out NamedKey named)
    {
        nint keyboardLayout = PInvoke.GetKeyboardLayout(0);
        if ((virtualKey == VkRMenu || virtualKey == VkMenu && keyLParam.Extended) &&
            LayoutHasAltGraph(keyboardLayout))
        {
            named = NamedKey.AltGraph;
            return true;
        }

        named = virtualKey switch
        {
            VkBack => NamedKey.Backspace,
            VkTab => NamedKey.Tab,
            VkClear => NamedKey.Clear,
            VkReturn => NamedKey.Enter,
            VkShift or VkLShift or VkRShift => NamedKey.Shift,
            VkControl or VkLControl or VkRControl => NamedKey.Control,
            VkMenu or VkLMenu or VkRMenu => NamedKey.Alt,
            VkPause => NamedKey.Pause,
            VkCapital => NamedKey.CapsLock,
            VkEscape => NamedKey.Escape,
            VkPageUp => NamedKey.PageUp,
            VkPageDown => NamedKey.PageDown,
            VkEnd => NamedKey.End,
            VkHome => NamedKey.Home,
            VkLeft => NamedKey.ArrowLeft,
            VkUp => NamedKey.ArrowUp,
            VkRight => NamedKey.ArrowRight,
            VkDown => NamedKey.ArrowDown,
            VkPrint or VkPrintScreen => NamedKey.PrintScreen,
            VkInsert => NamedKey.Insert,
            VkDelete => NamedKey.Delete,
            VkHelp => NamedKey.Help,
            VkLWin or VkRWin => NamedKey.Meta,
            VkApps => NamedKey.ContextMenu,
            VkNumLock => NamedKey.NumLock,
            VkScroll => NamedKey.ScrollLock,
            VkBrowserBack => NamedKey.BrowserBack,
            VkBrowserForward => NamedKey.BrowserForward,
            VkBrowserRefresh => NamedKey.BrowserRefresh,
            VkBrowserStop => NamedKey.BrowserStop,
            VkBrowserSearch => NamedKey.BrowserSearch,
            VkBrowserFavorites => NamedKey.BrowserFavorites,
            VkBrowserHome => NamedKey.BrowserHome,
            VkVolumeMute => NamedKey.AudioVolumeMute,
            VkVolumeDown => NamedKey.AudioVolumeDown,
            VkVolumeUp => NamedKey.AudioVolumeUp,
            VkMediaNextTrack => NamedKey.MediaTrackNext,
            VkMediaPrevTrack => NamedKey.MediaTrackPrevious,
            VkMediaStop => NamedKey.MediaStop,
            VkMediaPlayPause => NamedKey.MediaPlayPause,
            VkLaunchMail => NamedKey.LaunchMail,
            VkLaunchMediaSelect => NamedKey.LaunchMediaPlayer,
            VkLaunchApp1 => NamedKey.LaunchApplication1,
            VkLaunchApp2 => NamedKey.LaunchApplication2,
            _ when virtualKey >= VkF1 && virtualKey <= VkF24 => NamedKey.F1 + (virtualKey - VkF1),
            _ => NamedKey.Unidentified,
        };

        if (named != NamedKey.Unidentified)
        {
            return true;
        }

        named = keyCode switch
        {
            KeyCode.NumpadEnter => NamedKey.Enter,
            KeyCode.NumLock => NamedKey.NumLock,
            _ => NamedKey.Unidentified,
        };

        return named != NamedKey.Unidentified;
    }

    private static bool TryText(ushort virtualKey, KeyLParam keyLParam, bool applyModifiers, out string text)
    {
        if (TryKeyboardLayoutText(virtualKey, keyLParam, applyModifiers, out text))
        {
            return true;
        }

        return TryUsKeyboardTextFallback(virtualKey, applyModifiers, out text);
    }

    private static bool TryKeyboardLayoutText(
        ushort virtualKey,
        KeyLParam keyLParam,
        bool applyModifiers,
        out string text)
    {
        Span<byte> keyboardState = stackalloc byte[256];
        if (applyModifiers)
        {
            fixed (byte* state = keyboardState)
            {
                if (!PInvoke.GetKeyboardState(state))
                {
                    text = string.Empty;
                    return false;
                }
            }
        }

        nint keyboardLayout = PInvoke.GetKeyboardLayout(0);
        uint scanCode = keyLParam.ScanCode;
        if (scanCode == 0)
        {
            scanCode = PInvoke.MapVirtualKeyExW(virtualKey, MapvkVkToVscEx, keyboardLayout);
        }
        else if (keyLParam.Extended)
        {
            scanCode |= 0x0100;
        }

        Span<char> buffer = stackalloc char[8];
        fixed (byte* state = keyboardState)
        fixed (char* chars = buffer)
        {
            int length = PInvoke.ToUnicodeEx(
                virtualKey,
                scanCode,
                state,
                chars,
                buffer.Length,
                ToUnicodeNoStateChange,
                keyboardLayout);
            if (length > 0)
            {
                text = new string(chars, 0, Math.Min(length, buffer.Length));
                return !string.IsNullOrEmpty(text);
            }
        }

        text = string.Empty;
        return false;
    }

    private static bool TryUsKeyboardTextFallback(ushort virtualKey, bool applyModifiers, out string text)
    {
        bool shift = applyModifiers && IsKeyPressed(VkShift);
        bool caps = applyModifiers && IsKeyToggled(VkCapital);

        if (virtualKey is >= (ushort)'A' and <= (ushort)'Z')
        {
            char letter = (char)('a' + virtualKey - 'A');
            if (shift ^ caps)
            {
                letter = char.ToUpperInvariant(letter);
            }

            text = letter.ToString();
            return true;
        }

        if (virtualKey is >= (ushort)'0' and <= (ushort)'9')
        {
            ReadOnlySpan<char> shifted = ")!@#$%^&*(";
            char digit = shift ? shifted[virtualKey - '0'] : (char)virtualKey;
            text = digit.ToString();
            return true;
        }

        if (virtualKey is >= VkNumpad0 and <= VkNumpad9)
        {
            text = ((char)('0' + virtualKey - VkNumpad0)).ToString();
            return true;
        }

        char? punctuation = virtualKey switch
        {
            VkSpace => ' ',
            VkMultiply => '*',
            VkAdd => '+',
            VkSeparator => ',',
            VkSubtract => '-',
            VkDecimal => '.',
            VkDivide => '/',
            VkOem1 => shift ? ':' : ';',
            VkOemPlus => shift ? '+' : '=',
            VkOemComma => shift ? '<' : ',',
            VkOemMinus => shift ? '_' : '-',
            VkOemPeriod => shift ? '>' : '.',
            VkOem2 => shift ? '?' : '/',
            VkOem3 => shift ? '~' : '`',
            VkOem4 => shift ? '{' : '[',
            VkOem5 => shift ? '|' : '\\',
            VkOem6 => shift ? '}' : ']',
            VkOem7 => shift ? '"' : '\'',
            VkOem102 => shift ? '|' : '\\',
            _ => null,
        };

        if (punctuation is { } value)
        {
            text = value.ToString();
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TextInputAllowed()
    {
        Modifiers modifiers = CurrentModifiers();
        bool altGraph = IsAltGraphActive();
        return !modifiers.State.MetaKey() &&
            ((!modifiers.State.ControlKey() && !modifiers.State.AltKey()) || altGraph);
    }

    private static bool IsAltGraphActive()
    {
        nint keyboardLayout = PInvoke.GetKeyboardLayout(0);
        return LayoutHasAltGraph(keyboardLayout) && IsKeyPressed(VkRMenu) && IsKeyPressed(VkControl);
    }

    private static bool LayoutHasAltGraph(nint keyboardLayout)
    {
        lock (s_altGraphLock)
        {
            if (s_altGraphByLayout.TryGetValue(keyboardLayout, out bool hasAltGraph))
            {
                return hasAltGraph;
            }

            hasAltGraph = DetectAltGraph(keyboardLayout);
            s_altGraphByLayout[keyboardLayout] = hasAltGraph;
            return hasAltGraph;
        }
    }

    internal static bool DetectAltGraph(nint keyboardLayout)
    {
        Span<byte> keyboardState = stackalloc byte[256];
        keyboardState[VkControl] = 0x80;
        keyboardState[VkLControl] = 0x80;
        keyboardState[VkMenu] = 0x80;
        keyboardState[VkRMenu] = 0x80;
        Span<char> buffer = stackalloc char[8];

        fixed (byte* state = keyboardState)
        fixed (char* chars = buffer)
        {
            foreach (ushort virtualKey in AltGraphProbeKeys())
            {
                uint scanCode = PInvoke.MapVirtualKeyExW(virtualKey, MapvkVkToVscEx, keyboardLayout);
                if (scanCode == 0)
                {
                    continue;
                }

                int length = PInvoke.ToUnicodeEx(
                    virtualKey,
                    scanCode,
                    state,
                    chars,
                    buffer.Length,
                    ToUnicodeNoStateChange,
                    keyboardLayout);
                if (length > 0)
                {
                    for (int index = 0; index < Math.Min(length, buffer.Length); index++)
                    {
                        if (!char.IsControl(chars[index]))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static IEnumerable<ushort> AltGraphProbeKeys()
    {
        for (ushort key = (ushort)'0'; key <= (ushort)'9'; key++)
        {
            yield return key;
        }

        for (ushort key = (ushort)'A'; key <= (ushort)'Z'; key++)
        {
            yield return key;
        }

        yield return VkOem1;
        yield return VkOemPlus;
        yield return VkOemComma;
        yield return VkOemMinus;
        yield return VkOemPeriod;
        yield return VkOem2;
        yield return VkOem3;
        yield return VkOem4;
        yield return VkOem5;
        yield return VkOem6;
        yield return VkOem7;
        yield return VkOem102;
    }

    internal static KeyLocation Location(ushort virtualKey, KeyCode keyCode, ushort scanCode, bool extended)
    {
        return virtualKey switch
        {
            VkLShift or VkLControl or VkLMenu or VkLWin => KeyLocation.Left,
            VkRShift or VkRControl or VkRMenu or VkRWin => KeyLocation.Right,
            VkShift => scanCode == 0x36 ? KeyLocation.Right : KeyLocation.Left,
            VkControl or VkMenu => extended ? KeyLocation.Right : KeyLocation.Left,
            >= VkNumpad0 and <= VkNumpad9 => KeyLocation.Numpad,
            VkMultiply or VkAdd or VkSeparator or VkSubtract or VkDecimal or VkDivide => KeyLocation.Numpad,
            VkReturn when keyCode == KeyCode.NumpadEnter => KeyLocation.Numpad,
            _ => KeyLocation.Standard,
        };
    }

    private static KeyLocation Location(ushort virtualKey, KeyCode keyCode, KeyLParam keyLParam)
    {
        return Location(virtualKey, keyCode, keyLParam.ScanCode, keyLParam.Extended);
    }

    internal static KeyLocation LocationFromScancode(ushort extendedScanCode, nint hkl)
    {
        bool extended = (extendedScanCode & 0xe000) == 0xe000;
        uint virtualKey = PInvoke.MapVirtualKeyExW(extendedScanCode, MapvkVscToVkEx, hkl);
        return unchecked((ushort)virtualKey) switch
        {
            VkLShift or VkLControl or VkLMenu or VkLWin => KeyLocation.Left,
            VkRShift or VkRControl or VkRMenu or VkRWin => KeyLocation.Right,
            VkReturn when extended => KeyLocation.Numpad,
            VkInsert or VkDelete or VkEnd or VkDown or VkPageDown or VkLeft or VkClear or
                VkRight or VkHome or VkUp or VkPageUp => extended ? KeyLocation.Standard : KeyLocation.Numpad,
            >= VkNumpad0 and <= VkNumpad9 => KeyLocation.Numpad,
            VkDecimal or VkDivide or VkMultiply or VkSubtract or VkAdd => KeyLocation.Numpad,
            _ => KeyLocation.Standard,
        };
    }

    internal static Key VirtualKeyToNonCharacterKey(ushort virtualKey, nint hkl, bool hasAltGraph)
    {
        NativeKey nativeCode = new(new NativeKey.Windows(virtualKey));
        return virtualKey switch
        {
            VkBack => Key.From(NamedKey.Backspace),
            VkTab => Key.From(NamedKey.Tab),
            VkClear => Key.From(NamedKey.Clear),
            VkReturn => Key.From(NamedKey.Enter),
            VkShift or VkLShift or VkRShift => Key.From(NamedKey.Shift),
            VkControl or VkLControl or VkRControl => Key.From(NamedKey.Control),
            VkMenu or VkLMenu => Key.From(NamedKey.Alt),
            VkRMenu => Key.From(hasAltGraph ? NamedKey.AltGraph : NamedKey.Alt),
            VkPause => Key.From(NamedKey.Pause),
            VkCapital => Key.From(NamedKey.CapsLock),
            VkEscape => Key.From(NamedKey.Escape),
            VkSpace => Key.FromCharacter(" "),
            VkPageUp => Key.From(NamedKey.PageUp),
            VkPageDown => Key.From(NamedKey.PageDown),
            VkEnd => Key.From(NamedKey.End),
            VkHome => Key.From(NamedKey.Home),
            VkLeft => Key.From(NamedKey.ArrowLeft),
            VkUp => Key.From(NamedKey.ArrowUp),
            VkRight => Key.From(NamedKey.ArrowRight),
            VkDown => Key.From(NamedKey.ArrowDown),
            VkPrint or VkPrintScreen => Key.From(NamedKey.PrintScreen),
            VkInsert => Key.From(NamedKey.Insert),
            VkDelete => Key.From(NamedKey.Delete),
            VkHelp => Key.From(NamedKey.Help),
            VkLWin or VkRWin => Key.From(NamedKey.Meta),
            VkApps => Key.From(NamedKey.ContextMenu),
            VkNumLock => Key.From(NamedKey.NumLock),
            VkScroll => Key.From(NamedKey.ScrollLock),
            VkBrowserBack => Key.From(NamedKey.BrowserBack),
            VkBrowserForward => Key.From(NamedKey.BrowserForward),
            VkBrowserRefresh => Key.From(NamedKey.BrowserRefresh),
            VkBrowserStop => Key.From(NamedKey.BrowserStop),
            VkBrowserSearch => Key.From(NamedKey.BrowserSearch),
            VkBrowserFavorites => Key.From(NamedKey.BrowserFavorites),
            VkBrowserHome => Key.From(NamedKey.BrowserHome),
            VkVolumeMute => Key.From(NamedKey.AudioVolumeMute),
            VkVolumeDown => Key.From(NamedKey.AudioVolumeDown),
            VkVolumeUp => Key.From(NamedKey.AudioVolumeUp),
            VkMediaNextTrack => Key.From(NamedKey.MediaTrackNext),
            VkMediaPrevTrack => Key.From(NamedKey.MediaTrackPrevious),
            VkMediaStop => Key.From(NamedKey.MediaStop),
            VkMediaPlayPause => Key.From(NamedKey.MediaPlayPause),
            VkLaunchMail => Key.From(NamedKey.LaunchMail),
            VkLaunchMediaSelect => Key.From(NamedKey.LaunchMediaPlayer),
            VkLaunchApp1 => Key.From(NamedKey.LaunchApplication1),
            VkLaunchApp2 => Key.From(NamedKey.LaunchApplication2),
            _ when virtualKey >= VkF1 && virtualKey <= VkF24 => Key.From(NamedKey.F1 + (virtualKey - VkF1)),
            _ => Key.From(nativeCode),
        };
    }

    internal static bool TryKeyText(
        ushort virtualKey,
        KeyCode keyCode,
        WindowsModifiers modifiers,
        nint keyboardLayout,
        out Key key)
    {
        Span<byte> keyboardState = stackalloc byte[256];
        modifiers.ApplyToKeyboardState(keyboardState);
        uint scanCode = PInvoke.MapVirtualKeyExW(virtualKey, MapvkVkToVscEx, keyboardLayout);
        if (scanCode == 0)
        {
            key = default;
            return false;
        }

        Span<char> buffer = stackalloc char[8];
        fixed (byte* state = keyboardState)
        fixed (char* chars = buffer)
        {
            int length = PInvoke.ToUnicodeEx(
                virtualKey,
                scanCode,
                state,
                chars,
                buffer.Length,
                0,
                keyboardLayout);
            if (length < 0)
            {
                length = PInvoke.ToUnicodeEx(
                    virtualKey,
                    scanCode,
                    state,
                    chars,
                    buffer.Length,
                    0,
                    keyboardLayout);
                key = length > 0
                    ? new Key(new Key.Dead(chars[0]))
                    : new Key(new Key.Dead(null));
                return true;
            }

            if (length > 0)
            {
                key = Key.FromCharacter(new string(chars, 0, Math.Min(length, buffer.Length)));
                return true;
            }
        }

        if (!modifiers.HasFlag(WindowsModifiers.Alt) &&
            !modifiers.HasFlag(WindowsModifiers.Control) &&
            keyCode == KeyCode.NumpadDivide)
        {
            key = Key.FromCharacter("/");
            return true;
        }

        key = default;
        return false;
    }

    internal static KeyCode KeyCodeFromScanCode(ushort scanCode, bool extended)
    {
        return scanCode switch
        {
            0x01 => KeyCode.Escape,
            0x02 => KeyCode.Digit1,
            0x03 => KeyCode.Digit2,
            0x04 => KeyCode.Digit3,
            0x05 => KeyCode.Digit4,
            0x06 => KeyCode.Digit5,
            0x07 => KeyCode.Digit6,
            0x08 => KeyCode.Digit7,
            0x09 => KeyCode.Digit8,
            0x0A => KeyCode.Digit9,
            0x0B => KeyCode.Digit0,
            0x0C => KeyCode.Minus,
            0x0D => KeyCode.Equal,
            0x0E => KeyCode.Backspace,
            0x0F => KeyCode.Tab,
            0x10 => KeyCode.KeyQ,
            0x11 => KeyCode.KeyW,
            0x12 => KeyCode.KeyE,
            0x13 => KeyCode.KeyR,
            0x14 => KeyCode.KeyT,
            0x15 => KeyCode.KeyY,
            0x16 => KeyCode.KeyU,
            0x17 => KeyCode.KeyI,
            0x18 => KeyCode.KeyO,
            0x19 => KeyCode.KeyP,
            0x1A => KeyCode.BracketLeft,
            0x1B => KeyCode.BracketRight,
            0x1C => extended ? KeyCode.NumpadEnter : KeyCode.Enter,
            0x1D => extended ? KeyCode.ControlRight : KeyCode.ControlLeft,
            0x1E => KeyCode.KeyA,
            0x1F => KeyCode.KeyS,
            0x20 => KeyCode.KeyD,
            0x21 => KeyCode.KeyF,
            0x22 => KeyCode.KeyG,
            0x23 => KeyCode.KeyH,
            0x24 => KeyCode.KeyJ,
            0x25 => KeyCode.KeyK,
            0x26 => KeyCode.KeyL,
            0x27 => KeyCode.Semicolon,
            0x28 => KeyCode.Quote,
            0x29 => KeyCode.Backquote,
            0x2A => KeyCode.ShiftLeft,
            0x2B => KeyCode.Backslash,
            0x2C => KeyCode.KeyZ,
            0x2D => KeyCode.KeyX,
            0x2E => KeyCode.KeyC,
            0x2F => KeyCode.KeyV,
            0x30 => KeyCode.KeyB,
            0x31 => KeyCode.KeyN,
            0x32 => KeyCode.KeyM,
            0x33 => KeyCode.Comma,
            0x34 => KeyCode.Period,
            0x35 => extended ? KeyCode.NumpadDivide : KeyCode.Slash,
            0x36 => KeyCode.ShiftRight,
            0x37 => extended ? KeyCode.PrintScreen : KeyCode.NumpadMultiply,
            0x38 => extended ? KeyCode.AltRight : KeyCode.AltLeft,
            0x39 => KeyCode.Space,
            0x3A => KeyCode.CapsLock,
            >= 0x3B and <= 0x44 => KeyCode.F1 + (scanCode - 0x3B),
            0x45 => extended ? KeyCode.Pause : KeyCode.NumLock,
            0x46 => KeyCode.ScrollLock,
            0x47 => extended ? KeyCode.Home : KeyCode.Numpad7,
            0x48 => extended ? KeyCode.ArrowUp : KeyCode.Numpad8,
            0x49 => extended ? KeyCode.PageUp : KeyCode.Numpad9,
            0x4A => KeyCode.NumpadSubtract,
            0x4B => extended ? KeyCode.ArrowLeft : KeyCode.Numpad4,
            0x4C => KeyCode.Numpad5,
            0x4D => extended ? KeyCode.ArrowRight : KeyCode.Numpad6,
            0x4E => KeyCode.NumpadAdd,
            0x4F => extended ? KeyCode.End : KeyCode.Numpad1,
            0x50 => extended ? KeyCode.ArrowDown : KeyCode.Numpad2,
            0x51 => extended ? KeyCode.PageDown : KeyCode.Numpad3,
            0x52 => extended ? KeyCode.Insert : KeyCode.Numpad0,
            0x53 => extended ? KeyCode.Delete : KeyCode.NumpadDecimal,
            0x56 => KeyCode.IntlBackslash,
            0x57 => KeyCode.F11,
            0x58 => KeyCode.F12,
            0x5B => KeyCode.MetaLeft,
            0x5C => KeyCode.MetaRight,
            0x5D => KeyCode.ContextMenu,
            _ => KeyCode.Unidentified,
        };
    }

    internal static bool IsNumpadKeyCode(KeyCode keyCode)
    {
        return keyCode is
            KeyCode.NumpadDecimal or
            KeyCode.NumpadDivide or
            KeyCode.NumpadMultiply or
            KeyCode.NumpadAdd or
            KeyCode.NumpadSubtract or
            KeyCode.NumpadComma or
            >= KeyCode.Numpad0 and <= KeyCode.Numpad9;
    }

    internal static bool IsKeyPressed(ushort virtualKey)
    {
        return (PInvoke.GetKeyState(virtualKey) & unchecked((short)0x8000)) != 0;
    }

    private static bool IsKeyToggled(ushort virtualKey)
    {
        return (PInvoke.GetKeyState(virtualKey) & 1) != 0;
    }
    private readonly record struct KeyLParam(ushort ScanCode, bool Extended, bool IsRepeat)
    {
        public static KeyLParam From(LPARAM lParam)
        {
            nuint value = unchecked((nuint)lParam.Value);
            bool previousState = ((value >> 30) & 1) != 0;
            bool transitionState = ((value >> 31) & 1) != 0;
            return new KeyLParam(
                unchecked((ushort)((value >> 16) & 0xff)),
                ((value >> 24) & 1) != 0,
                previousState ^ transitionState);
        }
    }
}

internal readonly record struct MessageAsKeyEvent(KeyEvent Event, bool IsSynthetic);

internal sealed class KeyEventBuilder
{
    private readonly PendingEventQueue<MessageAsKeyEvent> _pending = new();
    private PartialKeyEventInfo? _eventInfo;

    public IReadOnlyList<MessageAsKeyEvent> ProcessMessage(
        HWND hwnd,
        uint message,
        WPARAM wParam,
        LPARAM lParam,
        out bool handled)
    {
        handled = false;

        switch (message)
        {
            case PInvoke.WM_SETFOCUS:
                return _pending.CompleteMulti(SynthesizeKeyboardState(ElementState.Pressed, GetAsyncKeyboardState()));
            case PInvoke.WM_KILLFOCUS:
                return _pending.CompleteMulti(SynthesizeKeyboardState(ElementState.Released, GetKeyboardState()));
            case PInvoke.WM_KEYDOWN:
            case PInvoke.WM_SYSKEYDOWN:
                if (message == PInvoke.WM_SYSKEYDOWN && wParam.Value == Keyboard.VkF4)
                {
                    return [];
                }

                handled = true;
                return ProcessKeyDown(hwnd, wParam, lParam);
            case PInvoke.WM_DEADCHAR:
            case PInvoke.WM_SYSDEADCHAR:
                handled = true;
                return ProcessDeadChar();
            case PInvoke.WM_CHAR:
            case PInvoke.WM_SYSCHAR:
                handled = true;
                return ProcessChar(hwnd, wParam);
            case PInvoke.WM_KEYUP:
            case PInvoke.WM_SYSKEYUP:
                handled = true;
                return ProcessKeyUp(hwnd, wParam, lParam);
            default:
                return [];
        }
    }

    private IReadOnlyList<MessageAsKeyEvent> ProcessKeyDown(HWND hwnd, WPARAM wParam, LPARAM lParam)
    {
        PendingMessageToken token = _pending.AddPending();
        MSG? nextMessage = NextKeyboardMessage(hwnd);
        PartialKeyEventInfo? finishedEventInfo = PartialKeyEventInfo.FromMessage(
            wParam,
            lParam,
            ElementState.Pressed);

        _eventInfo = null;
        if (nextMessage is { } next)
        {
            bool nextBelongsToThis = next.message is not (
                PInvoke.WM_KEYDOWN or
                PInvoke.WM_SYSKEYDOWN or
                PInvoke.WM_KEYUP or
                PInvoke.WM_SYSKEYUP);
            if (nextBelongsToThis)
            {
                _eventInfo = finishedEventInfo;
                finishedEventInfo = null;
            }
            else if (IsCurrentFake(finishedEventInfo, next))
            {
                finishedEventInfo = null;
            }
        }

        return finishedEventInfo is { } info
            ? _pending.CompletePending(token, new MessageAsKeyEvent(info.FinalizeEvent(), false))
            : _pending.RemovePending(token);
    }

    private IReadOnlyList<MessageAsKeyEvent> ProcessDeadChar()
    {
        PendingMessageToken token = _pending.AddPending();
        PartialKeyEventInfo? eventInfo = _eventInfo;
        _eventInfo = null;
        return eventInfo is { } info
            ? _pending.CompletePending(token, new MessageAsKeyEvent(info.FinalizeEvent(), false))
            : _pending.RemovePending(token);
    }

    private IReadOnlyList<MessageAsKeyEvent> ProcessChar(HWND hwnd, WPARAM wParam)
    {
        if (_eventInfo is null)
        {
            return [];
        }

        PendingMessageToken token = _pending.AddPending();
        AppendText(_eventInfo.Utf16Parts, wParam.Value);

        MSG? nextMessage = NextKeyboardMessage(hwnd);
        bool moreCharComing = nextMessage?.message is PInvoke.WM_CHAR or PInvoke.WM_SYSCHAR;
        if (moreCharComing)
        {
            return _pending.RemovePending(token);
        }

        PartialKeyEventInfo eventInfo = _eventInfo;
        _eventInfo = null;
        KeyboardLayout layout = KeyboardLayoutCache.Shared.GetCurrentLayout();
        byte[] keyboardState = GetKeyboardState();
        WindowsModifiers modifiers = WindowsModifiersExtensions.ActiveModifiers(keyboardState);
        bool controlOn = layout.HasAltGraph
            ? !modifiers.HasFlag(WindowsModifiers.Alt) && modifiers.HasFlag(WindowsModifiers.Control)
            : modifiers.HasFlag(WindowsModifiers.Control);

        if (!controlOn)
        {
            eventInfo.Text = PartialText.FromSystem(eventInfo.Utf16Parts);
        }
        else
        {
            WindowsModifiers withoutControl = modifiers.RemoveOnlyControl();
            bool numLockOn = (keyboardState[Keyboard.VkNumLock] & 1) != 0;
            Key key = layout.GetKey(withoutControl, numLockOn, eventInfo.VirtualKey, eventInfo.PhysicalKey);
            eventInfo.Text = PartialText.FromText(key.ToText());
        }

        return _pending.CompletePending(token, new MessageAsKeyEvent(eventInfo.FinalizeEvent(), false));
    }

    private IReadOnlyList<MessageAsKeyEvent> ProcessKeyUp(HWND hwnd, WPARAM wParam, LPARAM lParam)
    {
        PendingMessageToken token = _pending.AddPending();
        PartialKeyEventInfo eventInfo = PartialKeyEventInfo.FromMessage(
            wParam,
            lParam,
            ElementState.Released);

        MSG? nextMessage = NextKeyboardMessage(hwnd);
        bool valid = nextMessage is not { } next || !IsCurrentFake(eventInfo, next);
        return valid
            ? _pending.CompletePending(token, new MessageAsKeyEvent(eventInfo.FinalizeEvent(), false))
            : _pending.RemovePending(token);
    }

    private static IReadOnlyList<MessageAsKeyEvent> SynthesizeKeyboardState(
        ElementState state,
        byte[] keyboardState)
    {
        List<MessageAsKeyEvent> events = [];
        KeyboardLayout layout = KeyboardLayoutCache.Shared.GetCurrentLayout();
        bool capsLockOn = (keyboardState[Keyboard.VkCapital] & 1) != 0;
        bool numLockOn = (keyboardState[Keyboard.VkNumLock] & 1) != 0;

        if (IsDown(keyboardState, Keyboard.VkCapital))
        {
            AddSynthetic(events, Keyboard.VkCapital, state, capsLockOn, numLockOn, layout);
        }

        if (state == ElementState.Pressed)
        {
            AddNonModifierSyntheticEvents(events, keyboardState, state, capsLockOn, numLockOn, layout);
            AddModifierSyntheticEvents(events, keyboardState, state, capsLockOn, numLockOn, layout);
        }
        else
        {
            AddModifierSyntheticEvents(events, keyboardState, state, capsLockOn, numLockOn, layout);
            AddNonModifierSyntheticEvents(events, keyboardState, state, capsLockOn, numLockOn, layout);
        }

        return events;
    }

    private static void AddNonModifierSyntheticEvents(
        List<MessageAsKeyEvent> events,
        byte[] keyboardState,
        ElementState state,
        bool capsLockOn,
        bool numLockOn,
        KeyboardLayout layout)
    {
        for (ushort virtualKey = 0; virtualKey <= byte.MaxValue; virtualKey++)
        {
            if (virtualKey is Keyboard.VkControl or Keyboard.VkLControl or Keyboard.VkRControl or
                Keyboard.VkShift or Keyboard.VkLShift or Keyboard.VkRShift or
                Keyboard.VkMenu or Keyboard.VkLMenu or Keyboard.VkRMenu or
                Keyboard.VkCapital)
            {
                continue;
            }

            if (IsDown(keyboardState, virtualKey))
            {
                AddSynthetic(events, virtualKey, state, capsLockOn, numLockOn, layout);
            }
        }
    }

    private static void AddModifierSyntheticEvents(
        List<MessageAsKeyEvent> events,
        byte[] keyboardState,
        ElementState state,
        bool capsLockOn,
        bool numLockOn,
        KeyboardLayout layout)
    {
        ReadOnlySpan<ushort> modifiers =
        [
            Keyboard.VkLControl,
            Keyboard.VkLShift,
            Keyboard.VkLMenu,
            Keyboard.VkRControl,
            Keyboard.VkRShift,
            Keyboard.VkRMenu,
        ];

        foreach (ushort virtualKey in modifiers)
        {
            if (IsDown(keyboardState, virtualKey))
            {
                AddSynthetic(events, virtualKey, state, capsLockOn, numLockOn, layout);
            }
        }
    }

    private static void AddSynthetic(
        List<MessageAsKeyEvent> events,
        ushort virtualKey,
        ElementState state,
        bool capsLockOn,
        bool numLockOn,
        KeyboardLayout layout)
    {
        uint scanCode = PInvoke.MapVirtualKeyExW(virtualKey, 4, layout.Hkl);
        if (scanCode == 0)
        {
            return;
        }

        ushort extendedScanCode = unchecked((ushort)scanCode);
        PhysicalKey physicalKey = PhysicalKeyFromExtendedScanCode(extendedScanCode);
        WindowsModifiers modifiers = capsLockOn ? WindowsModifiers.CapsLock : WindowsModifiers.None;
        Key logicalKey = layout.GetKey(modifiers, numLockOn, virtualKey, physicalKey);
        Key keyWithoutModifiers = layout.GetKey(WindowsModifiers.None, false, virtualKey, physicalKey);
        string? text = state == ElementState.Pressed ? logicalKey.ToText() : null;
        KeyEvent keyEvent = new(
            physicalKey,
            logicalKey,
            text,
            Keyboard.LocationFromScancode(extendedScanCode, layout.Hkl),
            state,
            false,
            text,
            keyWithoutModifiers);
        events.Add(new MessageAsKeyEvent(keyEvent, true));
    }

    private static bool IsCurrentFake(PartialKeyEventInfo current, MSG nextMessage)
    {
        KeyboardLayout layout = KeyboardLayoutCache.Shared.GetCurrentLayout();
        bool currentIsControl = current.LogicalKey.TryGetValue(out PartialLogicalKey.This currentKey) &&
            currentKey.Value.Is(NamedKey.Control);
        if (!layout.HasAltGraph || !currentIsControl)
        {
            return false;
        }

        ushort nextScanCode = ExtendedScancodeFromLParam(nextMessage.lParam);
        return nextScanCode == 0xe038;
    }

    private static MSG? NextKeyboardMessage(HWND hwnd)
    {
        return PInvoke.PeekMessageW(
            out MSG message,
            hwnd,
            PInvoke.WM_KEYFIRST,
            PInvoke.WM_KEYLAST,
            0)
            ? message
            : null;
    }

    private static byte[] GetKeyboardState()
    {
        byte[] keyboardState = new byte[256];
        unsafe
        {
            fixed (byte* statePtr = keyboardState)
            {
                _ = PInvoke.GetKeyboardState(statePtr);
            }
        }

        return keyboardState;
    }

    private static byte[] GetAsyncKeyboardState()
    {
        byte[] keyboardState = new byte[256];
        for (int virtualKey = 0; virtualKey < keyboardState.Length; virtualKey++)
        {
            short asyncState = PInvoke.GetAsyncKeyState(virtualKey);
            keyboardState[virtualKey] = (asyncState & unchecked((short)0x8000)) != 0 ? (byte)0x80 : (byte)0;
            if (virtualKey is Keyboard.VkCapital or Keyboard.VkNumLock or Keyboard.VkScroll)
            {
                keyboardState[virtualKey] |= (byte)(PInvoke.GetKeyState(virtualKey) & 1);
            }
        }

        return keyboardState;
    }

    private static bool IsDown(ReadOnlySpan<byte> keyboardState, ushort virtualKey)
    {
        return (keyboardState[virtualKey] & 0x80) != 0;
    }

    private static void AppendText(List<ushort> utf16Parts, nuint value)
    {
        uint codepoint = unchecked((uint)value);
        if (codepoint <= char.MaxValue)
        {
            utf16Parts.Add(unchecked((ushort)codepoint));
            return;
        }

        string text = char.ConvertFromUtf32(unchecked((int)codepoint));
        foreach (char ch in text)
        {
            utf16Parts.Add(ch);
        }
    }

    private static PhysicalKey PhysicalKeyFromExtendedScanCode(ushort extendedScanCode)
    {
        bool extended = (extendedScanCode & 0xe000) == 0xe000;
        ushort scanCode = unchecked((ushort)(extendedScanCode & 0xff));
        KeyCode keyCode = Keyboard.KeyCodeFromScanCode(scanCode, extended);
        return keyCode == KeyCode.Unidentified
            ? PhysicalKey.From(new NativeKeyCode(new NativeKeyCode.Windows(extendedScanCode)))
            : PhysicalKey.From(keyCode);
    }

    private static ushort ExtendedScancodeFromLParam(LPARAM lParam)
    {
        KeyMessageLParam keyLParam = KeyMessageLParam.From(lParam);
        return NewExtendedScancode(keyLParam.ScanCode, keyLParam.Extended);
    }

    private static ushort NewExtendedScancode(ushort scanCode, bool extended)
    {
        return unchecked((ushort)(scanCode | (extended ? 0xe000 : 0)));
    }

    private sealed class PartialKeyEventInfo
    {
        private PartialKeyEventInfo(
            ushort virtualKey,
            ElementState keyState,
            bool isRepeat,
            PhysicalKey physicalKey,
            KeyLocation location,
            PartialLogicalKey logicalKey,
            Key keyWithoutModifiers)
        {
            VirtualKey = virtualKey;
            KeyState = keyState;
            IsRepeat = isRepeat;
            PhysicalKey = physicalKey;
            Location = location;
            LogicalKey = logicalKey;
            KeyWithoutModifiers = keyWithoutModifiers;
        }

        public ushort VirtualKey { get; }

        public ElementState KeyState { get; }

        public bool IsRepeat { get; }

        public PhysicalKey PhysicalKey { get; }

        public KeyLocation Location { get; }

        public PartialLogicalKey LogicalKey { get; }

        public Key KeyWithoutModifiers { get; }

        public List<ushort> Utf16Parts { get; } = new(8);

        public PartialText Text { get; set; } = PartialText.FromSystem([]);

        public static PartialKeyEventInfo FromMessage(WPARAM wParam, LPARAM lParam, ElementState state)
        {
            KeyboardLayout layout = KeyboardLayoutCache.Shared.GetCurrentLayout();
            KeyMessageLParam keyLParam = KeyMessageLParam.From(lParam);
            ushort virtualKey = unchecked((ushort)wParam.Value);
            ushort extendedScanCode = keyLParam.ScanCode == 0
                ? unchecked((ushort)PInvoke.MapVirtualKeyExW(virtualKey, 4, layout.Hkl))
                : NewExtendedScancode(keyLParam.ScanCode, keyLParam.Extended);
            PhysicalKey physicalKey = PhysicalKeyFromExtendedScanCode(extendedScanCode);
            byte[] keyboardState = GetKeyboardState();
            WindowsModifiers modifiers = WindowsModifiersExtensions.ActiveModifiers(keyboardState);
            WindowsModifiers modifiersWithoutControl = modifiers.RemoveOnlyControl();
            bool numLockOn = (keyboardState[Keyboard.VkNumLock] & 1) != 0;

            Key? codeAsKey = null;
            if (modifiers.HasFlag(WindowsModifiers.Control) && physicalKey.TryGetValue(out PhysicalKey.Code code))
            {
                codeAsKey = code.KeyCode switch
                {
                    KeyCode.NumLock => Key.From(NamedKey.NumLock),
                    KeyCode.Pause => Key.From(NamedKey.Pause),
                    _ => null,
                };
            }

            Key preliminaryLogicalKey = layout.GetKey(modifiersWithoutControl, numLockOn, virtualKey, physicalKey);
            bool keyIsCharacter = preliminaryLogicalKey.TryGetValue(out Key.Character character) &&
                character.Value != " ";
            bool pressed = state == ElementState.Pressed;
            PartialLogicalKey logicalKey = codeAsKey is { } codeKey
                ? new PartialLogicalKey(new PartialLogicalKey.This(codeKey))
                : pressed && keyIsCharacter && !modifiers.HasFlag(WindowsModifiers.Control)
                    ? new PartialLogicalKey(new PartialLogicalKey.TextOr(preliminaryLogicalKey))
                    : new PartialLogicalKey(new PartialLogicalKey.This(preliminaryLogicalKey));

            Key keyWithoutModifiers;
            if (codeAsKey is { } key)
            {
                keyWithoutModifiers = key;
            }
            else
            {
                keyWithoutModifiers = layout.GetKey(WindowsModifiers.None, false, virtualKey, physicalKey);
                if (keyWithoutModifiers.TryGetValue(out Key.Dead dead))
                {
                    keyWithoutModifiers = dead.Value is { } ch
                        ? Key.FromCharacter(ch.ToString())
                        : Key.From(new NativeKey(new NativeKey.Unidentified()));
                }
            }

            return new PartialKeyEventInfo(
                virtualKey,
                state,
                keyLParam.IsRepeat,
                physicalKey,
                Keyboard.LocationFromScancode(extendedScanCode, layout.Hkl),
                logicalKey,
                keyWithoutModifiers);
        }

        public KeyEvent FinalizeEvent()
        {
            string? textWithAllModifiers = KeyEventBuilderHelpers.DecodeUtf16(Utf16Parts);
            string? text = Text.ResolveText();
            Key logicalKey;
            if (LogicalKey.TryGetValue(out PartialLogicalKey.TextOr textOr))
            {
                logicalKey = text is { } value
                    ? TextElementCount(value) > 1
                        ? textOr.Fallback
                        : Key.FromCharacter(value)
                    : Key.From(new NativeKey(new NativeKey.Windows(VirtualKey)));
            }
            else
            {
                LogicalKey.TryGetValue(out PartialLogicalKey.This thisKey);
                logicalKey = thisKey.Value;
            }

            return new KeyEvent(
                PhysicalKey,
                logicalKey,
                text,
                Location,
                KeyState,
                IsRepeat,
                textWithAllModifiers,
                KeyWithoutModifiers);
        }

        private static int TextElementCount(string text)
        {
            return new StringInfo(text).LengthInTextElements;
        }
    }

    private readonly record struct KeyMessageLParam(ushort ScanCode, bool Extended, bool IsRepeat)
    {
        public static KeyMessageLParam From(LPARAM lParam)
        {
            nuint value = unchecked((nuint)lParam.Value);
            bool previousState = ((value >> 30) & 1) != 0;
            bool transitionState = ((value >> 31) & 1) != 0;
            return new KeyMessageLParam(
                unchecked((ushort)((value >> 16) & 0xff)),
                ((value >> 24) & 1) != 0,
                previousState ^ transitionState);
        }
    }
}

internal record struct PartialText
{
    public readonly record struct SystemText(IReadOnlyList<ushort> Value);

    public readonly record struct Text(string? Value);

    private const byte SystemTextTag = 0;
    private const byte TextTag = 1;

    private byte _tag;
    private SystemText _systemText;
    private Text _text;

    public PartialText(SystemText value)
    {
        _tag = SystemTextTag;
        _systemText = value;
        _text = default;
    }

    public PartialText(Text value)
    {
        _tag = TextTag;
        _systemText = default;
        _text = value;
    }

    public static PartialText FromSystem(IReadOnlyList<ushort> value)
    {
        return new PartialText(new SystemText(value));
    }

    public static PartialText FromText(string? value)
    {
        return new PartialText(new Text(value));
    }

    public string? ResolveText()
    {
        return _tag == SystemTextTag ? KeyEventBuilderHelpers.DecodeUtf16(_systemText.Value) : _text.Value;
    }
}

internal record struct PartialLogicalKey
{
    public readonly record struct TextOr(Key Fallback);

    public readonly record struct This(Key Value);

    private const byte TextOrTag = 0;
    private const byte ThisTag = 1;

    private byte _tag;
    private TextOr _textOr;
    private This _this;

    public PartialLogicalKey(TextOr value)
    {
        _tag = TextOrTag;
        _textOr = value;
        _this = default;
    }

    public PartialLogicalKey(This value)
    {
        _tag = ThisTag;
        _textOr = default;
        _this = value;
    }

    public bool TryGetValue(out TextOr value)
    {
        value = _textOr;
        return _tag == TextOrTag;
    }

    public bool TryGetValue(out This value)
    {
        value = _this;
        return _tag == ThisTag;
    }
}

internal static class KeyEventBuilderHelpers
{
    public static string? DecodeUtf16(IReadOnlyList<ushort> utf16)
    {
        if (utf16.Count == 0)
        {
            return null;
        }

        char[] chars = new char[utf16.Count];
        for (int i = 0; i < utf16.Count; i++)
        {
            chars[i] = unchecked((char)utf16[i]);
        }

        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsHighSurrogate(chars[i]))
            {
                if (i + 1 >= chars.Length || !char.IsLowSurrogate(chars[i + 1]))
                {
                    return null;
                }

                i++;
                continue;
            }

            if (char.IsLowSurrogate(chars[i]))
            {
                return null;
            }
        }

        return new string(chars);
    }
}

internal readonly record struct PendingMessageToken(uint Value);

internal sealed class PendingEventQueue<T>
{
    private readonly Lock _lock = new();
    private readonly List<IdentifiedPendingMessage<T>> _pending = [];
    private uint _nextId;

    public PendingMessageToken AddPending()
    {
        PendingMessageToken token = NextToken();
        lock (_lock)
        {
            _pending.Add(new IdentifiedPendingMessage<T>(token, new PendingMessage<T>()));
        }

        return token;
    }

    public IReadOnlyList<T> CompletePending(PendingMessageToken token, T message)
    {
        lock (_lock)
        {
            bool targetIsFirst = false;
            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].Token != token)
                {
                    continue;
                }

                _pending[i] = new IdentifiedPendingMessage<T>(token, new PendingMessage<T>(message));
                targetIsFirst = i == 0;
                break;
            }

            return targetIsFirst ? DrainPending() : [];
        }
    }

    public IReadOnlyList<T> CompleteMulti(IReadOnlyList<T> messages)
    {
        lock (_lock)
        {
            if (_pending.Count == 0)
            {
                return messages;
            }

            foreach (T message in messages)
            {
                _pending.Add(new IdentifiedPendingMessage<T>(
                    NextToken(),
                    new PendingMessage<T>(message)));
            }

            return [];
        }
    }

    public IReadOnlyList<T> RemovePending(PendingMessageToken token)
    {
        lock (_lock)
        {
            bool wasFirst = _pending.Count > 0 && _pending[0].Token == token;
            _pending.RemoveAll(message => message.Token == token);
            return wasFirst ? DrainPending() : [];
        }
    }

    private List<T> DrainPending()
    {
        List<T> messages = new(_pending.Count);
        foreach (IdentifiedPendingMessage<T> pending in _pending)
        {
            if (!pending.Message.TryGetValue(out T message))
            {
                throw new InvalidOperationException("found an incomplete pending keyboard event");
            }

            messages.Add(message);
        }

        _pending.Clear();
        return messages;
    }

    private PendingMessageToken NextToken()
    {
        return new PendingMessageToken(_nextId++);
    }

    private readonly record struct IdentifiedPendingMessage<TMessage>(
        PendingMessageToken Token,
        PendingMessage<TMessage> Message);

    private readonly record struct PendingMessage<TMessage>
    {
        private readonly bool _complete;
        private readonly TMessage? _message;

        public PendingMessage(TMessage message)
        {
            _complete = true;
            _message = message;
        }

        public bool TryGetValue(out TMessage message)
        {
            message = _message!;
            return _complete;
        }
    }
}
