using Winit.Core;

namespace Winit.Win32;

[Flags]
internal enum WindowsModifiers : byte
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
    Alt = 1 << 2,
    CapsLock = 1 << 3,
}

internal static class WindowsModifiersExtensions
{
    public static WindowsModifiers ActiveModifiers(ReadOnlySpan<byte> keyState)
    {
        WindowsModifiers result = WindowsModifiers.None;
        if (IsDown(keyState, Keyboard.VkShift) ||
            IsDown(keyState, Keyboard.VkLShift) ||
            IsDown(keyState, Keyboard.VkRShift))
        {
            result |= WindowsModifiers.Shift;
        }

        if (IsDown(keyState, Keyboard.VkControl) ||
            IsDown(keyState, Keyboard.VkLControl) ||
            IsDown(keyState, Keyboard.VkRControl))
        {
            result |= WindowsModifiers.Control;
        }

        if (IsDown(keyState, Keyboard.VkMenu) ||
            IsDown(keyState, Keyboard.VkLMenu) ||
            IsDown(keyState, Keyboard.VkRMenu))
        {
            result |= WindowsModifiers.Alt;
        }

        if ((keyState[Keyboard.VkCapital] & 1) != 0)
        {
            result |= WindowsModifiers.CapsLock;
        }

        return result;
    }

    public static void ApplyToKeyboardState(this WindowsModifiers modifiers, Span<byte> keyState)
    {
        SetDown(keyState, Keyboard.VkShift, modifiers.HasFlag(WindowsModifiers.Shift));
        if (!modifiers.HasFlag(WindowsModifiers.Shift))
        {
            SetDown(keyState, Keyboard.VkLShift, false);
            SetDown(keyState, Keyboard.VkRShift, false);
        }

        SetDown(keyState, Keyboard.VkControl, modifiers.HasFlag(WindowsModifiers.Control));
        if (!modifiers.HasFlag(WindowsModifiers.Control))
        {
            SetDown(keyState, Keyboard.VkLControl, false);
            SetDown(keyState, Keyboard.VkRControl, false);
        }

        SetDown(keyState, Keyboard.VkMenu, modifiers.HasFlag(WindowsModifiers.Alt));
        if (!modifiers.HasFlag(WindowsModifiers.Alt))
        {
            SetDown(keyState, Keyboard.VkLMenu, false);
            SetDown(keyState, Keyboard.VkRMenu, false);
        }

        keyState[Keyboard.VkCapital] = modifiers.HasFlag(WindowsModifiers.CapsLock)
            ? (byte)(keyState[Keyboard.VkCapital] | 1)
            : (byte)(keyState[Keyboard.VkCapital] & ~1);
    }

    public static WindowsModifiers RemoveOnlyControl(this WindowsModifiers modifiers)
    {
        return modifiers.HasFlag(WindowsModifiers.Alt)
            ? modifiers
            : modifiers & ~WindowsModifiers.Control;
    }

    private static bool IsDown(ReadOnlySpan<byte> keyState, ushort virtualKey)
    {
        return (keyState[virtualKey] & 0x80) != 0;
    }

    private static void SetDown(Span<byte> keyState, ushort virtualKey, bool down)
    {
        keyState[virtualKey] = down
            ? (byte)(keyState[virtualKey] | 0x80)
            : (byte)(keyState[virtualKey] & ~0x80);
    }
}

internal sealed unsafe class KeyboardLayout
{
    private const int ModifiersEnd = 1 << 4;
    private const ushort LangJapanese = 0x11;
    private const ushort LangKorean = 0x12;

    private const ushort VkLButton = 0x01;
    private const ushort VkRButton = 0x02;
    private const ushort VkCancel = 0x03;
    private const ushort VkMButton = 0x04;
    private const ushort VkXButton1 = 0x05;
    private const ushort VkXButton2 = 0x06;
    private const ushort VkKanaHangul = 0x15;
    private const ushort VkJunja = 0x17;
    private const ushort VkFinal = 0x18;
    private const ushort VkHanjaKanji = 0x19;
    private const ushort VkConvert = 0x1C;
    private const ushort VkNonConvert = 0x1D;
    private const ushort VkAccept = 0x1E;
    private const ushort VkModeChange = 0x1F;
    private const ushort VkSelect = 0x29;
    private const ushort VkExecute = 0x2B;
    private const ushort VkSleep = 0x5F;
    private const ushort VkNavigationView = 0x88;
    private const ushort VkNavigationMenu = 0x89;
    private const ushort VkNavigationUp = 0x8A;
    private const ushort VkNavigationDown = 0x8B;
    private const ushort VkNavigationLeft = 0x8C;
    private const ushort VkNavigationRight = 0x8D;
    private const ushort VkNavigationAccept = 0x8E;
    private const ushort VkNavigationCancel = 0x8F;
    private const ushort VkOemNecEqual = 0x92;
    private const ushort VkOemFjMasshou = 0x93;
    private const ushort VkOemFjTouroku = 0x94;
    private const ushort VkOemFjLoya = 0x95;
    private const ushort VkOemFjRoya = 0x96;
    private const ushort VkOem8 = 0xDF;
    private const ushort VkOemAx = 0xE1;
    private const ushort VkIcoHelp = 0xE3;
    private const ushort VkIco00 = 0xE4;
    private const ushort VkProcessKey = 0xE5;
    private const ushort VkIcoClear = 0xE6;
    private const ushort VkPacket = 0xE7;
    private const ushort VkOemReset = 0xE9;
    private const ushort VkOemJump = 0xEA;
    private const ushort VkOemPa1 = 0xEB;
    private const ushort VkOemPa2 = 0xEC;
    private const ushort VkOemPa3 = 0xED;
    private const ushort VkOemWsCtrl = 0xEE;
    private const ushort VkOemCuSel = 0xEF;
    private const ushort VkOemAttn = 0xF0;
    private const ushort VkOemFinish = 0xF1;
    private const ushort VkOemCopy = 0xF2;
    private const ushort VkOemAuto = 0xF3;
    private const ushort VkOemEnlw = 0xF4;
    private const ushort VkOemBackTab = 0xF5;
    private const ushort VkAttn = 0xF6;
    private const ushort VkCrSel = 0xF7;
    private const ushort VkExSel = 0xF8;
    private const ushort VkErEof = 0xF9;
    private const ushort VkPlay = 0xFA;
    private const ushort VkZoom = 0xFB;
    private const ushort VkNoName = 0xFC;
    private const ushort VkPa1 = 0xFD;
    private const ushort VkOemClear = 0xFE;

    private static readonly ushort[] s_numpadVirtualKeys =
    [
        Keyboard.VkNumpad0,
        Keyboard.VkNumpad0 + 1,
        Keyboard.VkNumpad0 + 2,
        Keyboard.VkNumpad0 + 3,
        Keyboard.VkNumpad0 + 4,
        Keyboard.VkNumpad0 + 5,
        Keyboard.VkNumpad0 + 6,
        Keyboard.VkNumpad0 + 7,
        Keyboard.VkNumpad0 + 8,
        Keyboard.VkNumpad9,
        Keyboard.VkMultiply,
        Keyboard.VkAdd,
        Keyboard.VkSeparator,
        Keyboard.VkSubtract,
        Keyboard.VkDecimal,
        Keyboard.VkDivide,
    ];

    private readonly Dictionary<ushort, Key> _numLockOnKeys = [];
    private readonly Dictionary<ushort, Key> _numLockOffKeys = [];
    private readonly Dictionary<WindowsModifiers, Dictionary<KeyCode, Key>> _keys = [];

    private KeyboardLayout(nint hkl)
    {
        Hkl = hkl;
    }

    public nint Hkl { get; }

    public bool HasAltGraph { get; private set; }

    public Key GetKey(
        WindowsModifiers modifiers,
        bool numLockOn,
        ushort virtualKey,
        PhysicalKey physicalKey)
    {
        Key nativeFallback = KeyFromWindows(virtualKey);

        if (virtualKey != Keyboard.VkMenu)
        {
            Key keyFromVirtualKey = VkeyToNonCharacterKey(virtualKey, Hkl, HasAltGraph);
            if (!IsUnidentified(keyFromVirtualKey))
            {
                return keyFromVirtualKey;
            }
        }

        Dictionary<ushort, Key> numLockKeys = numLockOn ? _numLockOnKeys : _numLockOffKeys;
        if (numLockKeys.TryGetValue(virtualKey, out Key numpadKey))
        {
            return numpadKey;
        }

        if (physicalKey.TryGetValue(out PhysicalKey.Code code) &&
            _keys.TryGetValue(modifiers, out Dictionary<KeyCode, Key>? keysForModifiers) &&
            keysForModifiers.TryGetValue(code.KeyCode, out Key key))
        {
            return key;
        }

        return nativeFallback;
    }

    public static KeyboardLayout Prepare(nint hkl)
    {
        KeyboardLayout layout = new(hkl);
        byte[] keyState = new byte[256];

        for (uint virtualKey = 0; virtualKey < 256; virtualKey++)
        {
            uint scanCode = PInvoke.MapVirtualKeyExW(virtualKey, Keyboard.MapvkVkToVscEx, hkl);
            if (scanCode == 0)
            {
                continue;
            }

            if (!TryKeyCodeFromScanCode(scanCode, out KeyCode keyCode) ||
                IsNumpadSpecific(unchecked((ushort)virtualKey)) ||
                !Keyboard.IsNumpadKeyCode(keyCode))
            {
                continue;
            }

            ushort mapVirtualKey = KeyCodeToVirtualKey(keyCode, hkl);
            if (mapVirtualKey == 0)
            {
                continue;
            }

            Key mapValue = VkeyToNonCharacterKey(unchecked((ushort)virtualKey), hkl, hasAltGraph: false);
            if (!IsUnidentified(mapValue))
            {
                layout._numLockOffKeys[mapVirtualKey] = mapValue;
            }
        }

        foreach (ushort virtualKey in s_numpadVirtualKeys)
        {
            uint scanCode = PInvoke.MapVirtualKeyExW(virtualKey, Keyboard.MapvkVkToVscEx, hkl);
            ToUnicodeResult unicode = ToUnicodeString(keyState, virtualKey, scanCode, hkl);
            if (unicode.TryGetString(out string? text))
            {
                layout._numLockOnKeys[virtualKey] = Key.FromCharacter(text);
            }
        }

        for (int modifierBits = 0; modifierBits < ModifiersEnd; modifierBits++)
        {
            WindowsModifiers modifiers = (WindowsModifiers)modifierBits;
            Dictionary<KeyCode, Key> keysForModifiers = new(capacity: 256);
            Array.Clear(keyState);
            modifiers.ApplyToKeyboardState(keyState);

            for (uint virtualKey = 0; virtualKey < 256; virtualKey++)
            {
                uint scanCode = PInvoke.MapVirtualKeyExW(virtualKey, Keyboard.MapvkVkToVscEx, hkl);
                if (scanCode == 0 || !TryKeyCodeFromScanCode(scanCode, out KeyCode keyCode))
                {
                    continue;
                }

                Key preliminaryKey = VkeyToNonCharacterKey(
                    unchecked((ushort)virtualKey),
                    hkl,
                    hasAltGraph: false);
                if (!IsUnidentified(preliminaryKey))
                {
                    keysForModifiers[keyCode] = preliminaryKey;
                    continue;
                }

                ToUnicodeResult unicode = ToUnicodeString(keyState, virtualKey, scanCode, hkl);
                Key key;
                if (unicode.TryGetString(out string? text))
                {
                    key = Key.FromCharacter(text);
                }
                else if (unicode.TryGetDead(out char? deadChar))
                {
                    key = new Key(new Key.Dead(deadChar));
                }
                else if (!modifiers.HasFlag(WindowsModifiers.Alt) &&
                         !modifiers.HasFlag(WindowsModifiers.Control) &&
                         keyCode == KeyCode.NumpadDivide)
                {
                    key = Key.FromCharacter("/");
                }
                else
                {
                    key = preliminaryKey;
                }

                WindowsModifiers ctrlAlt = WindowsModifiers.Control | WindowsModifiers.Alt;
                if (!layout.HasAltGraph &&
                    modifiers == ctrlAlt &&
                    layout._keys.TryGetValue(WindowsModifiers.None, out Dictionary<KeyCode, Key>? simpleKeys) &&
                    simpleKeys.TryGetValue(keyCode, out Key keyWithoutAltGraph) &&
                    keyWithoutAltGraph.TryGetValue(out Key.Character noAltGraphCharacter) &&
                    key.TryGetValue(out Key.Character altGraphCharacter) &&
                    altGraphCharacter.Value != noAltGraphCharacter.Value)
                {
                    layout.HasAltGraph = true;
                }

                keysForModifiers[keyCode] = key;
            }

            layout._keys[modifiers] = keysForModifiers;
        }

        if (layout.HasAltGraph)
        {
            foreach (Dictionary<KeyCode, Key> keys in layout._keys.Values)
            {
                if (keys.ContainsKey(KeyCode.AltRight))
                {
                    keys[KeyCode.AltRight] = Key.From(NamedKey.AltGraph);
                }
            }
        }

        return layout;
    }

    private static ToUnicodeResult ToUnicodeString(
        byte[] keyState,
        uint virtualKey,
        uint scanCode,
        nint hkl)
    {
        Span<char> buffer = stackalloc char[8];
        fixed (byte* state = keyState)
        fixed (char* chars = buffer)
        {
            int length = PInvoke.ToUnicodeEx(
                virtualKey,
                scanCode,
                state,
                chars,
                buffer.Length,
                0,
                hkl);
            if (length < 0)
            {
                length = PInvoke.ToUnicodeEx(
                    virtualKey,
                    scanCode,
                    state,
                    chars,
                    buffer.Length,
                    0,
                    hkl);
                if (length > 0)
                {
                    string deadText = new(chars, 0, Math.Min(length, buffer.Length));
                    if (deadText.Length > 0)
                    {
                        return ToUnicodeResult.Dead(deadText[0]);
                    }
                }

                return ToUnicodeResult.Dead(null);
            }

            if (length > 0)
            {
                return ToUnicodeResult.Str(new string(chars, 0, Math.Min(length, buffer.Length)));
            }
        }

        return ToUnicodeResult.None;
    }

    private static bool TryKeyCodeFromScanCode(uint extendedScanCode, out KeyCode keyCode)
    {
        bool extended = (extendedScanCode & 0xe000) == 0xe000;
        ushort scanCode = unchecked((ushort)(extendedScanCode & 0xff));
        keyCode = Keyboard.KeyCodeFromScanCode(scanCode, extended);
        return keyCode != KeyCode.Unidentified;
    }

    private static bool IsNumpadSpecific(ushort virtualKey)
    {
        return virtualKey is >= Keyboard.VkNumpad0 and <= Keyboard.VkNumpad9 or
            Keyboard.VkAdd or
            Keyboard.VkSubtract or
            Keyboard.VkDivide or
            Keyboard.VkDecimal or
            Keyboard.VkSeparator;
    }

    private static ushort KeyCodeToVirtualKey(KeyCode keyCode, nint hkl)
    {
        ushort primaryLangId = PrimaryLangId(hkl);
        bool isKorean = primaryLangId == LangKorean;
        bool isJapanese = primaryLangId == LangJapanese;

        if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F24)
        {
            return unchecked((ushort)(Keyboard.VkF1 + ((int)keyCode - (int)KeyCode.F1)));
        }

        return keyCode switch
        {
            KeyCode.AltLeft => Keyboard.VkLMenu,
            KeyCode.AltRight => Keyboard.VkRMenu,
            KeyCode.Backspace => Keyboard.VkBack,
            KeyCode.CapsLock => Keyboard.VkCapital,
            KeyCode.ContextMenu => Keyboard.VkApps,
            KeyCode.ControlLeft => Keyboard.VkLControl,
            KeyCode.ControlRight => Keyboard.VkRControl,
            KeyCode.Enter => Keyboard.VkReturn,
            KeyCode.MetaLeft => Keyboard.VkLWin,
            KeyCode.MetaRight => Keyboard.VkRWin,
            KeyCode.ShiftLeft => Keyboard.VkRShift,
            KeyCode.ShiftRight => Keyboard.VkLShift,
            KeyCode.Space => Keyboard.VkSpace,
            KeyCode.Tab => Keyboard.VkTab,
            KeyCode.Convert => VkConvert,
            KeyCode.KanaMode => VkKanaHangul,
            KeyCode.Lang1 when isKorean => VkKanaHangul,
            KeyCode.Lang1 when isJapanese => VkKanaHangul,
            KeyCode.Lang2 when isKorean => VkHanjaKanji,
            KeyCode.Lang3 when isJapanese => VkOemFinish,
            KeyCode.NonConvert => VkNonConvert,
            KeyCode.Delete => Keyboard.VkDelete,
            KeyCode.End => Keyboard.VkEnd,
            KeyCode.Help => Keyboard.VkHelp,
            KeyCode.Home => Keyboard.VkHome,
            KeyCode.Insert => Keyboard.VkInsert,
            KeyCode.PageDown => Keyboard.VkPageDown,
            KeyCode.PageUp => Keyboard.VkPageUp,
            KeyCode.ArrowDown => Keyboard.VkDown,
            KeyCode.ArrowLeft => Keyboard.VkLeft,
            KeyCode.ArrowRight => Keyboard.VkRight,
            KeyCode.ArrowUp => Keyboard.VkUp,
            KeyCode.NumLock => Keyboard.VkNumLock,
            KeyCode.Numpad0 => Keyboard.VkNumpad0,
            KeyCode.Numpad1 => Keyboard.VkNumpad0 + 1,
            KeyCode.Numpad2 => Keyboard.VkNumpad0 + 2,
            KeyCode.Numpad3 => Keyboard.VkNumpad0 + 3,
            KeyCode.Numpad4 => Keyboard.VkNumpad0 + 4,
            KeyCode.Numpad5 => Keyboard.VkNumpad0 + 5,
            KeyCode.Numpad6 => Keyboard.VkNumpad0 + 6,
            KeyCode.Numpad7 => Keyboard.VkNumpad0 + 7,
            KeyCode.Numpad8 => Keyboard.VkNumpad0 + 8,
            KeyCode.Numpad9 => Keyboard.VkNumpad9,
            KeyCode.NumpadAdd => Keyboard.VkAdd,
            KeyCode.NumpadBackspace => Keyboard.VkBack,
            KeyCode.NumpadClear => Keyboard.VkClear,
            KeyCode.NumpadComma => Keyboard.VkSeparator,
            KeyCode.NumpadDecimal => Keyboard.VkDecimal,
            KeyCode.NumpadDivide => Keyboard.VkDivide,
            KeyCode.NumpadEnter => Keyboard.VkReturn,
            KeyCode.NumpadMultiply => Keyboard.VkMultiply,
            KeyCode.NumpadSubtract => Keyboard.VkSubtract,
            KeyCode.Escape => Keyboard.VkEscape,
            KeyCode.PrintScreen => Keyboard.VkPrintScreen,
            KeyCode.ScrollLock => Keyboard.VkScroll,
            KeyCode.Pause => Keyboard.VkPause,
            KeyCode.BrowserBack => Keyboard.VkBrowserBack,
            KeyCode.BrowserFavorites => Keyboard.VkBrowserFavorites,
            KeyCode.BrowserForward => Keyboard.VkBrowserForward,
            KeyCode.BrowserHome => Keyboard.VkBrowserHome,
            KeyCode.BrowserRefresh => Keyboard.VkBrowserRefresh,
            KeyCode.BrowserSearch => Keyboard.VkBrowserSearch,
            KeyCode.BrowserStop => Keyboard.VkBrowserStop,
            KeyCode.LaunchApp1 => Keyboard.VkLaunchApp1,
            KeyCode.LaunchApp2 => Keyboard.VkLaunchApp2,
            KeyCode.LaunchMail => Keyboard.VkLaunchMail,
            KeyCode.MediaPlayPause => Keyboard.VkMediaPlayPause,
            KeyCode.MediaSelect => Keyboard.VkLaunchMediaSelect,
            KeyCode.MediaStop => Keyboard.VkMediaStop,
            KeyCode.MediaTrackNext => Keyboard.VkMediaNextTrack,
            KeyCode.MediaTrackPrevious => Keyboard.VkMediaPrevTrack,
            KeyCode.AudioVolumeDown => Keyboard.VkVolumeDown,
            KeyCode.AudioVolumeMute => Keyboard.VkVolumeMute,
            KeyCode.AudioVolumeUp => Keyboard.VkVolumeUp,
            KeyCode.Select => VkSelect,
            _ => 0,
        };
    }

    private static Key VkeyToNonCharacterKey(ushort virtualKey, nint hkl, bool hasAltGraph)
    {
        Key nativeCode = KeyFromWindows(virtualKey);
        ushort primaryLangId = PrimaryLangId(hkl);
        bool isKorean = primaryLangId == LangKorean;
        bool isJapanese = primaryLangId == LangJapanese;

        if (virtualKey == VkKanaHangul && isKorean)
        {
            return Key.From(NamedKey.HangulMode);
        }

        if (virtualKey == VkKanaHangul && isJapanese)
        {
            return Key.From(NamedKey.KanaMode);
        }

        if (virtualKey == VkHanjaKanji && isKorean)
        {
            return Key.From(NamedKey.HanjaMode);
        }

        if (virtualKey == VkHanjaKanji && isJapanese)
        {
            return Key.From(NamedKey.KanjiMode);
        }

        return virtualKey switch
        {
            VkLButton or VkRButton or VkMButton or VkXButton1 or VkXButton2 => KeyFromNativeUnidentified(),
            VkCancel => nativeCode,
            Keyboard.VkBack => Key.From(NamedKey.Backspace),
            Keyboard.VkTab => Key.From(NamedKey.Tab),
            Keyboard.VkClear => Key.From(NamedKey.Clear),
            Keyboard.VkReturn => Key.From(NamedKey.Enter),
            Keyboard.VkShift => Key.From(NamedKey.Shift),
            Keyboard.VkControl => Key.From(NamedKey.Control),
            Keyboard.VkMenu => Key.From(NamedKey.Alt),
            Keyboard.VkPause => Key.From(NamedKey.Pause),
            Keyboard.VkCapital => Key.From(NamedKey.CapsLock),
            VkJunja => Key.From(NamedKey.JunjaMode),
            VkFinal => Key.From(NamedKey.FinalMode),
            Keyboard.VkEscape => Key.From(NamedKey.Escape),
            VkConvert => Key.From(NamedKey.Convert),
            VkNonConvert => Key.From(NamedKey.NonConvert),
            VkAccept => Key.From(NamedKey.Accept),
            VkModeChange => Key.From(NamedKey.ModeChange),
            Keyboard.VkSpace => Key.FromCharacter(" "),
            Keyboard.VkPageUp => Key.From(NamedKey.PageUp),
            Keyboard.VkPageDown => Key.From(NamedKey.PageDown),
            Keyboard.VkEnd => Key.From(NamedKey.End),
            Keyboard.VkHome => Key.From(NamedKey.Home),
            Keyboard.VkLeft => Key.From(NamedKey.ArrowLeft),
            Keyboard.VkUp => Key.From(NamedKey.ArrowUp),
            Keyboard.VkRight => Key.From(NamedKey.ArrowRight),
            Keyboard.VkDown => Key.From(NamedKey.ArrowDown),
            VkSelect => Key.From(NamedKey.Select),
            Keyboard.VkPrint => Key.From(NamedKey.Print),
            VkExecute => Key.From(NamedKey.Execute),
            Keyboard.VkPrintScreen => Key.From(NamedKey.PrintScreen),
            Keyboard.VkInsert => Key.From(NamedKey.Insert),
            Keyboard.VkDelete => Key.From(NamedKey.Delete),
            Keyboard.VkHelp => Key.From(NamedKey.Help),
            Keyboard.VkLWin or Keyboard.VkRWin => Key.From(NamedKey.Meta),
            Keyboard.VkApps => Key.From(NamedKey.ContextMenu),
            VkSleep => Key.From(NamedKey.Standby),
            _ when virtualKey >= Keyboard.VkF1 && virtualKey <= Keyboard.VkF24 =>
                Key.From(NamedKey.F1 + (virtualKey - Keyboard.VkF1)),
            VkNavigationView or VkNavigationMenu or VkNavigationUp or VkNavigationDown or
                VkNavigationLeft or VkNavigationRight or VkNavigationAccept or VkNavigationCancel => nativeCode,
            Keyboard.VkNumLock => Key.From(NamedKey.NumLock),
            Keyboard.VkScroll => Key.From(NamedKey.ScrollLock),
            VkOemNecEqual or VkOemFjMasshou or VkOemFjTouroku or VkOemFjLoya or VkOemFjRoya => nativeCode,
            Keyboard.VkLShift or Keyboard.VkRShift => Key.From(NamedKey.Shift),
            Keyboard.VkLControl or Keyboard.VkRControl => Key.From(NamedKey.Control),
            Keyboard.VkLMenu => Key.From(NamedKey.Alt),
            Keyboard.VkRMenu => Key.From(hasAltGraph ? NamedKey.AltGraph : NamedKey.Alt),
            Keyboard.VkBrowserBack => Key.From(NamedKey.BrowserBack),
            Keyboard.VkBrowserForward => Key.From(NamedKey.BrowserForward),
            Keyboard.VkBrowserRefresh => Key.From(NamedKey.BrowserRefresh),
            Keyboard.VkBrowserStop => Key.From(NamedKey.BrowserStop),
            Keyboard.VkBrowserSearch => Key.From(NamedKey.BrowserSearch),
            Keyboard.VkBrowserFavorites => Key.From(NamedKey.BrowserFavorites),
            Keyboard.VkBrowserHome => Key.From(NamedKey.BrowserHome),
            Keyboard.VkVolumeMute => Key.From(NamedKey.AudioVolumeMute),
            Keyboard.VkVolumeDown => Key.From(NamedKey.AudioVolumeDown),
            Keyboard.VkVolumeUp => Key.From(NamedKey.AudioVolumeUp),
            Keyboard.VkMediaNextTrack => Key.From(NamedKey.MediaTrackNext),
            Keyboard.VkMediaPrevTrack => Key.From(NamedKey.MediaTrackPrevious),
            Keyboard.VkMediaStop => Key.From(NamedKey.MediaStop),
            Keyboard.VkMediaPlayPause => Key.From(NamedKey.MediaPlayPause),
            Keyboard.VkLaunchMail => Key.From(NamedKey.LaunchMail),
            Keyboard.VkLaunchMediaSelect => Key.From(NamedKey.LaunchMediaPlayer),
            Keyboard.VkLaunchApp1 => Key.From(NamedKey.LaunchApplication1),
            Keyboard.VkLaunchApp2 => Key.From(NamedKey.LaunchApplication2),
            Keyboard.VkOem1 or Keyboard.VkOemPlus or Keyboard.VkOemComma or Keyboard.VkOemMinus or
                Keyboard.VkOemPeriod or Keyboard.VkOem2 or Keyboard.VkOem3 or Keyboard.VkOem4 or
                Keyboard.VkOem5 or Keyboard.VkOem6 or Keyboard.VkOem7 or VkOem8 or VkOemAx or
                Keyboard.VkOem102 => nativeCode,
            VkIcoHelp or VkIco00 => nativeCode,
            VkProcessKey => Key.From(NamedKey.Process),
            VkIcoClear or VkPacket or VkOemReset or VkOemJump or VkOemPa1 or VkOemPa2 or
                VkOemPa3 or VkOemWsCtrl or VkOemCuSel => nativeCode,
            VkOemAttn => Key.From(NamedKey.Attn),
            VkOemFinish => isJapanese ? Key.From(NamedKey.Katakana) : nativeCode,
            VkOemCopy => Key.From(NamedKey.Copy),
            VkOemAuto => Key.From(NamedKey.Hankaku),
            VkOemEnlw => Key.From(NamedKey.Zenkaku),
            VkOemBackTab => Key.From(NamedKey.Romaji),
            VkAttn => Key.From(NamedKey.KanaMode),
            VkCrSel => Key.From(NamedKey.CrSel),
            VkExSel => Key.From(NamedKey.ExSel),
            VkErEof => Key.From(NamedKey.EraseEof),
            VkPlay => Key.From(NamedKey.Play),
            VkZoom => Key.From(NamedKey.ZoomToggle),
            VkNoName or VkPa1 => nativeCode,
            VkOemClear => Key.From(NamedKey.Clear),
            _ => nativeCode,
        };
    }

    private static ushort PrimaryLangId(nint hkl)
    {
        nuint raw = unchecked((nuint)hkl);
        return unchecked((ushort)((raw & 0xffff) & 0x03ff));
    }

    private static bool IsUnidentified(Key key)
    {
        return key.TryGetValue(out Key.Unidentified _);
    }

    private static Key KeyFromWindows(ushort virtualKey)
    {
        return Key.From(new NativeKey(new NativeKey.Windows(virtualKey)));
    }

    private static Key KeyFromNativeUnidentified()
    {
        return Key.From(new NativeKey(new NativeKey.Unidentified()));
    }

    private readonly record struct ToUnicodeResult
    {
        private const byte NoneTag = 0;
        private const byte StringTag = 1;
        private const byte DeadTag = 2;

        private readonly byte _tag;
        private readonly string? _text;
        private readonly char? _dead;

        private ToUnicodeResult(byte tag, string? text, char? dead)
        {
            _tag = tag;
            _text = text;
            _dead = dead;
        }

        public static ToUnicodeResult None => new(NoneTag, null, null);

        public static ToUnicodeResult Str(string value)
        {
            return new ToUnicodeResult(StringTag, value, null);
        }

        public static ToUnicodeResult Dead(char? value)
        {
            return new ToUnicodeResult(DeadTag, null, value);
        }

        public bool TryGetString(out string value)
        {
            value = _text!;
            return _tag == StringTag;
        }

        public bool TryGetDead(out char? value)
        {
            value = _dead;
            return _tag == DeadTag;
        }
    }
}

internal sealed class KeyboardLayoutCache
{
    private readonly Lock _lock = new();
    private readonly Dictionary<nint, KeyboardLayout> _layouts = [];

    public static KeyboardLayoutCache Shared { get; } = new();

    public KeyboardLayout GetCurrentLayout()
    {
        nint hkl = PInvoke.GetKeyboardLayout(0);
        lock (_lock)
        {
            if (_layouts.TryGetValue(hkl, out KeyboardLayout? layout))
            {
                return layout;
            }

            layout = KeyboardLayout.Prepare(hkl);
            _layouts[hkl] = layout;
            return layout;
        }
    }

    public ModifiersState GetAgnosticModifiers()
    {
        KeyboardLayout layout = GetCurrentLayout();
        bool filterAltGraph = layout.HasAltGraph && Keyboard.IsKeyPressed(Keyboard.VkRMenu);
        ModifiersState state = ModifiersState.None;
        if (Keyboard.IsKeyPressed(Keyboard.VkShift))
        {
            state |= ModifiersState.Shift;
        }

        if (Keyboard.IsKeyPressed(Keyboard.VkControl) && !filterAltGraph)
        {
            state |= ModifiersState.Control;
        }

        if (Keyboard.IsKeyPressed(Keyboard.VkMenu) && !filterAltGraph)
        {
            state |= ModifiersState.Alt;
        }

        if (Keyboard.IsKeyPressed(Keyboard.VkLWin) || Keyboard.IsKeyPressed(Keyboard.VkRWin))
        {
            state |= ModifiersState.Meta;
        }

        return state;
    }
}
