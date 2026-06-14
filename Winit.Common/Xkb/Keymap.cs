using Winit.Core;

namespace Winit.Common.Xkb;

public sealed class XkbKeymap : IDisposable
{
    private nint _keymap;
    private readonly ModIndices _modIndices;

    private XkbKeymap(nint keymap)
    {
        _keymap = keymap;
        _modIndices = new ModIndices(
            ModIndexForName(keymap, "Shift"),
            ModIndexForName(keymap, "Lock"),
            ModIndexForName(keymap, "Control"),
            ModIndexForName(keymap, "Mod1"),
            ModIndexForName(keymap, "Mod2"),
            ModIndexForName(keymap, "Mod3"),
            ModIndexForName(keymap, "Mod4"),
            ModIndexForName(keymap, "Mod5"));
    }

    public nint Handle => _keymap;

    public ModIndices ModIndices => _modIndices;

    internal static XkbKeymap? FromX11Keymap(XkbContext context, nint xcbConnection, int coreKeyboardId)
    {
        nint keymap = PInvoke.xkb_x11_keymap_new_from_device(
            context.Handle,
            xcbConnection,
            coreKeyboardId,
            PInvoke.XkbKeymapCompileNoFlags);
        return keymap == 0 ? null : new XkbKeymap(keymap);
    }

    public static PhysicalKey RawKeycodeToPhysicalKey(uint keycode)
    {
        return ScancodeToPhysicalKey(keycode > 8 ? keycode - 8 : 0);
    }

    public static PhysicalKey ScancodeToPhysicalKey(uint scancode)
    {
        KeyCode? code = scancode switch
        {
            1 => KeyCode.Escape,
            2 => KeyCode.Digit1,
            3 => KeyCode.Digit2,
            4 => KeyCode.Digit3,
            5 => KeyCode.Digit4,
            6 => KeyCode.Digit5,
            7 => KeyCode.Digit6,
            8 => KeyCode.Digit7,
            9 => KeyCode.Digit8,
            10 => KeyCode.Digit9,
            11 => KeyCode.Digit0,
            12 => KeyCode.Minus,
            13 => KeyCode.Equal,
            14 => KeyCode.Backspace,
            15 => KeyCode.Tab,
            16 => KeyCode.KeyQ,
            17 => KeyCode.KeyW,
            18 => KeyCode.KeyE,
            19 => KeyCode.KeyR,
            20 => KeyCode.KeyT,
            21 => KeyCode.KeyY,
            22 => KeyCode.KeyU,
            23 => KeyCode.KeyI,
            24 => KeyCode.KeyO,
            25 => KeyCode.KeyP,
            26 => KeyCode.BracketLeft,
            27 => KeyCode.BracketRight,
            28 => KeyCode.Enter,
            29 => KeyCode.ControlLeft,
            30 => KeyCode.KeyA,
            31 => KeyCode.KeyS,
            32 => KeyCode.KeyD,
            33 => KeyCode.KeyF,
            34 => KeyCode.KeyG,
            35 => KeyCode.KeyH,
            36 => KeyCode.KeyJ,
            37 => KeyCode.KeyK,
            38 => KeyCode.KeyL,
            39 => KeyCode.Semicolon,
            40 => KeyCode.Quote,
            41 => KeyCode.Backquote,
            42 => KeyCode.ShiftLeft,
            43 => KeyCode.Backslash,
            44 => KeyCode.KeyZ,
            45 => KeyCode.KeyX,
            46 => KeyCode.KeyC,
            47 => KeyCode.KeyV,
            48 => KeyCode.KeyB,
            49 => KeyCode.KeyN,
            50 => KeyCode.KeyM,
            51 => KeyCode.Comma,
            52 => KeyCode.Period,
            53 => KeyCode.Slash,
            54 => KeyCode.ShiftRight,
            55 => KeyCode.NumpadMultiply,
            56 => KeyCode.AltLeft,
            57 => KeyCode.Space,
            58 => KeyCode.CapsLock,
            59 => KeyCode.F1,
            60 => KeyCode.F2,
            61 => KeyCode.F3,
            62 => KeyCode.F4,
            63 => KeyCode.F5,
            64 => KeyCode.F6,
            65 => KeyCode.F7,
            66 => KeyCode.F8,
            67 => KeyCode.F9,
            68 => KeyCode.F10,
            69 => KeyCode.NumLock,
            70 => KeyCode.ScrollLock,
            71 => KeyCode.Numpad7,
            72 => KeyCode.Numpad8,
            73 => KeyCode.Numpad9,
            74 => KeyCode.NumpadSubtract,
            75 => KeyCode.Numpad4,
            76 => KeyCode.Numpad5,
            77 => KeyCode.Numpad6,
            78 => KeyCode.NumpadAdd,
            79 => KeyCode.Numpad1,
            80 => KeyCode.Numpad2,
            81 => KeyCode.Numpad3,
            82 => KeyCode.Numpad0,
            83 => KeyCode.NumpadDecimal,
            85 => KeyCode.Lang5,
            86 => KeyCode.IntlBackslash,
            87 => KeyCode.F11,
            88 => KeyCode.F12,
            89 => KeyCode.IntlRo,
            90 => KeyCode.Lang3,
            91 => KeyCode.Lang4,
            92 => KeyCode.Convert,
            93 => KeyCode.KanaMode,
            94 => KeyCode.NonConvert,
            96 => KeyCode.NumpadEnter,
            97 => KeyCode.ControlRight,
            98 => KeyCode.NumpadDivide,
            99 => KeyCode.PrintScreen,
            100 => KeyCode.AltRight,
            102 => KeyCode.Home,
            103 => KeyCode.ArrowUp,
            104 => KeyCode.PageUp,
            105 => KeyCode.ArrowLeft,
            106 => KeyCode.ArrowRight,
            107 => KeyCode.End,
            108 => KeyCode.ArrowDown,
            109 => KeyCode.PageDown,
            110 => KeyCode.Insert,
            111 => KeyCode.Delete,
            113 => KeyCode.AudioVolumeMute,
            114 => KeyCode.AudioVolumeDown,
            115 => KeyCode.AudioVolumeUp,
            116 => KeyCode.Power,
            117 => KeyCode.NumpadEqual,
            119 => KeyCode.Pause,
            120 => KeyCode.ShowAllWindows,
            121 => KeyCode.NumpadComma,
            122 => KeyCode.Lang1,
            123 => KeyCode.Lang2,
            124 => KeyCode.IntlYen,
            125 => KeyCode.MetaLeft,
            126 => KeyCode.MetaRight,
            127 => KeyCode.ContextMenu,
            128 => KeyCode.BrowserStop,
            129 => KeyCode.Again,
            130 => KeyCode.Props,
            131 => KeyCode.Undo,
            132 => KeyCode.Select,
            133 => KeyCode.Copy,
            134 => KeyCode.Open,
            135 => KeyCode.Paste,
            136 => KeyCode.Find,
            137 => KeyCode.Cut,
            138 => KeyCode.Help,
            140 => KeyCode.LaunchApp2,
            142 => KeyCode.Sleep,
            143 => KeyCode.WakeUp,
            144 => KeyCode.LaunchApp1,
            155 => KeyCode.LaunchMail,
            156 => KeyCode.BrowserFavorites,
            158 => KeyCode.BrowserBack,
            159 => KeyCode.BrowserForward,
            161 => KeyCode.Eject,
            163 => KeyCode.MediaTrackNext,
            164 => KeyCode.MediaPlayPause,
            165 => KeyCode.MediaTrackPrevious,
            166 => KeyCode.MediaStop,
            167 => KeyCode.MediaRecord,
            168 => KeyCode.MediaRewind,
            171 => KeyCode.MediaSelect,
            172 => KeyCode.BrowserHome,
            173 => KeyCode.BrowserRefresh,
            179 => KeyCode.NumpadParenLeft,
            180 => KeyCode.NumpadParenRight,
            183 => KeyCode.F13,
            184 => KeyCode.F14,
            185 => KeyCode.F15,
            186 => KeyCode.F16,
            187 => KeyCode.F17,
            188 => KeyCode.F18,
            189 => KeyCode.F19,
            190 => KeyCode.F20,
            191 => KeyCode.F21,
            192 => KeyCode.F22,
            193 => KeyCode.F23,
            194 => KeyCode.F24,
            201 => KeyCode.MediaPause,
            207 => KeyCode.MediaPlay,
            208 => KeyCode.MediaFastForward,
            217 => KeyCode.BrowserSearch,
            224 => KeyCode.BrightnessDown,
            225 => KeyCode.BrightnessUp,
            227 => KeyCode.DisplayToggleIntExt,
            228 => KeyCode.KeyboardBacklightToggle,
            231 => KeyCode.MailSend,
            232 => KeyCode.MailReply,
            233 => KeyCode.MailForward,
            240 => null,
            248 => KeyCode.MicrophoneMuteToggle,
            372 => KeyCode.ZoomToggle,
            579 => KeyCode.LaunchControlPanel,
            580 => KeyCode.SelectTask,
            581 => KeyCode.LaunchScreenSaver,
            583 => KeyCode.LaunchAssistant,
            584 => KeyCode.KeyboardLayoutSelect,
            633 => KeyCode.PrivacyScreenToggle,
            _ => null,
        };

        if (scancode == 240)
        {
            return PhysicalKey.From(new NativeKeyCode(new NativeKeyCode.Unidentified()));
        }

        return code is { } value
            ? PhysicalKey.From(value)
            : PhysicalKey.From(new NativeKeyCode(new NativeKeyCode.Xkb(scancode)));
    }

    public static uint? PhysicalKeyToScancode(PhysicalKey physicalKey)
    {
        if (physicalKey.TryGetValue(out PhysicalKey.Code code))
        {
            return CodeToScancode(code.KeyCode);
        }

        if (physicalKey.TryGetValue(out PhysicalKey.Unidentified unidentified))
        {
            if (unidentified.NativeKeyCode.TryGetValue(out NativeKeyCode.Unidentified _))
            {
                return 240;
            }

            if (unidentified.NativeKeyCode.TryGetValue(out NativeKeyCode.Xkb xkb))
            {
                return xkb.Code;
            }
        }

        return null;
    }

    public static KeyLocation KeyLocation(PhysicalKey physicalKey, uint keysym)
    {
        KeyLocation keysymLocation = KeysymLocation(keysym);
        if (keysymLocation != Winit.Core.KeyLocation.Standard)
        {
            return keysymLocation;
        }

        KeyCode code = physicalKey.ToKeyCode();
        return code switch
        {
            KeyCode.ShiftLeft or
            KeyCode.ControlLeft or
            KeyCode.MetaLeft or
            KeyCode.AltLeft => Winit.Core.KeyLocation.Left,
            KeyCode.ShiftRight or
            KeyCode.ControlRight or
            KeyCode.MetaRight or
            KeyCode.AltRight => Winit.Core.KeyLocation.Right,
            _ when code.ToString().StartsWith("Numpad", StringComparison.Ordinal) => Winit.Core.KeyLocation.Numpad,
            _ => Winit.Core.KeyLocation.Standard,
        };
    }

    public static Key KeysymToKey(uint keysym)
    {
        NamedKey? named = keysym switch
        {
            0xff08 => NamedKey.Backspace,
            0xff09 => NamedKey.Tab,
            0xff0b => NamedKey.Clear,
            0xff0d => NamedKey.Enter,
            0xff13 => NamedKey.Pause,
            0xff14 => NamedKey.ScrollLock,
            0xff15 => NamedKey.PrintScreen,
            0xff1b => NamedKey.Escape,
            0xffff => NamedKey.Delete,
            0xff20 => NamedKey.Compose,
            0xff37 => NamedKey.CodeInput,
            0xff3c => NamedKey.SingleCandidate,
            0xff3d => NamedKey.AllCandidates,
            0xff3e => NamedKey.PreviousCandidate,
            0xff21 => NamedKey.KanjiMode,
            0xff22 => NamedKey.NonConvert,
            0xff23 => NamedKey.Convert,
            0xff24 => NamedKey.Romaji,
            0xff25 => NamedKey.Hiragana,
            0xff26 => NamedKey.Katakana,
            0xff27 => NamedKey.HiraganaKatakana,
            0xff28 => NamedKey.Zenkaku,
            0xff29 => NamedKey.Hankaku,
            0xff2a => NamedKey.ZenkakuHankaku,
            0xff2d => NamedKey.KanaMode,
            0xff2e => NamedKey.KanaMode,
            0xff2f => NamedKey.Alphanumeric,
            0xff30 => NamedKey.Alphanumeric,
            0xff50 => NamedKey.Home,
            0xff51 => NamedKey.ArrowLeft,
            0xff52 => NamedKey.ArrowUp,
            0xff53 => NamedKey.ArrowRight,
            0xff54 => NamedKey.ArrowDown,
            0xff55 => NamedKey.PageUp,
            0xff56 => NamedKey.PageDown,
            0xff57 => NamedKey.End,
            0xff60 => NamedKey.Select,
            0xff61 => NamedKey.PrintScreen,
            0xff62 => NamedKey.Execute,
            0xff63 => NamedKey.Insert,
            0xff65 => NamedKey.Undo,
            0xff66 => NamedKey.Redo,
            0xff67 => NamedKey.ContextMenu,
            0xff68 => NamedKey.Find,
            0xff69 => NamedKey.Cancel,
            0xff6a => NamedKey.Help,
            0xff6b => NamedKey.Pause,
            0xff7e => NamedKey.ModeChange,
            0xff7f => NamedKey.NumLock,
            0xff89 => NamedKey.Tab,
            0xff8d => NamedKey.Enter,
            0xff91 => NamedKey.F1,
            0xff92 => NamedKey.F2,
            0xff93 => NamedKey.F3,
            0xff94 => NamedKey.F4,
            0xff95 => NamedKey.Home,
            0xff96 => NamedKey.ArrowLeft,
            0xff97 => NamedKey.ArrowUp,
            0xff98 => NamedKey.ArrowRight,
            0xff99 => NamedKey.ArrowDown,
            0xff9a => NamedKey.PageUp,
            0xff9b => NamedKey.PageDown,
            0xff9c => NamedKey.End,
            0xff9e => NamedKey.Insert,
            0xff9f => NamedKey.Delete,
            >= 0xffbe and <= 0xffe0 => FunctionKeyFromKeysym(keysym),
            0xffe1 or 0xffe2 => NamedKey.Shift,
            0xffe3 or 0xffe4 => NamedKey.Control,
            0xffe5 => NamedKey.CapsLock,
            0xffe9 or 0xffea => NamedKey.Alt,
            0xffeb or 0xffec => NamedKey.Meta,
            0xffed or 0xffee => NamedKey.Hyper,
            0xfe03 or 0xfe04 or 0xfe05 => NamedKey.AltGraph,
            0xfe08 => NamedKey.GroupNext,
            0xfe0a => NamedKey.GroupPrevious,
            0xfe0c => NamedKey.GroupFirst,
            0xfe0e => NamedKey.GroupLast,
            0xfe20 => NamedKey.Tab,
            0xfd09 => NamedKey.EraseEof,
            0xfd0a => NamedKey.Play,
            0xfd0b => NamedKey.ExSel,
            0xfd0c => NamedKey.CrSel,
            0xfd0d => NamedKey.PrintScreen,
            0xfd1e => NamedKey.Attn,
            0x1008ff02 => NamedKey.BrightnessUp,
            0x1008ff03 => NamedKey.BrightnessDown,
            0x1008ff10 => NamedKey.Standby,
            0x1008ff11 => NamedKey.AudioVolumeDown,
            0x1008ff12 => NamedKey.AudioVolumeMute,
            0x1008ff13 => NamedKey.AudioVolumeUp,
            0x1008ff14 => NamedKey.MediaPlay,
            0x1008ff15 => NamedKey.MediaStop,
            0x1008ff16 => NamedKey.MediaTrackPrevious,
            0x1008ff17 => NamedKey.MediaTrackNext,
            0x1008ff18 => NamedKey.BrowserHome,
            0x1008ff19 => NamedKey.LaunchMail,
            0x1008ff1b => NamedKey.BrowserSearch,
            0x1008ff1c => NamedKey.MediaRecord,
            0x1008ff1d => NamedKey.LaunchApplication2,
            0x1008ff20 => NamedKey.LaunchCalendar,
            0x1008ff21 => NamedKey.Power,
            0x1008ff26 => NamedKey.BrowserBack,
            0x1008ff27 => NamedKey.BrowserForward,
            0x1008ff29 => NamedKey.BrowserRefresh,
            0x1008ff2a => NamedKey.Power,
            0x1008ff2b => NamedKey.WakeUp,
            0x1008ff2c => NamedKey.Eject,
            0x1008ff2d => NamedKey.LaunchScreenSaver,
            0x1008ff2e => NamedKey.LaunchWebBrowser,
            0x1008ff2f => NamedKey.Standby,
            0x1008ff30 => NamedKey.BrowserFavorites,
            0x1008ff31 => NamedKey.MediaPause,
            0x1008ff33 => NamedKey.LaunchApplication1,
            0x1008ff3e => NamedKey.MediaRewind,
            0x1008ff54 => NamedKey.LaunchApplication2,
            0x1008ff56 => NamedKey.Close,
            0x1008ff57 => NamedKey.Copy,
            0x1008ff58 => NamedKey.Cut,
            0x1008ff5c => NamedKey.LaunchSpreadsheet,
            0x1008ff61 => NamedKey.LogOff,
            0x1008ff67 => NamedKey.BrowserFavorites,
            0x1008ff68 => NamedKey.New,
            0x1008ff6b => NamedKey.Open,
            0x1008ff6d => NamedKey.Paste,
            0x1008ff6e => NamedKey.LaunchPhone,
            0x1008ff72 => NamedKey.MailReply,
            0x1008ff73 => NamedKey.BrowserRefresh,
            0x1008ff77 => NamedKey.Save,
            0x1008ff7b => NamedKey.MailSend,
            0x1008ff7c => NamedKey.SpellCheck,
            0x1008ff7d => NamedKey.SplitScreenToggle,
            0x1008ff87 => NamedKey.LaunchMediaPlayer,
            0x1008ff89 => NamedKey.LaunchWordProcessor,
            0x1008ff8b => NamedKey.ZoomIn,
            0x1008ff8c => NamedKey.ZoomOut,
            0x1008ff8f => NamedKey.LaunchWebCam,
            0x1008ff90 => NamedKey.MailForward,
            0x1008ff92 => NamedKey.LaunchMusicPlayer,
            0x1008ff97 => NamedKey.MediaFastForward,
            0x1008ff99 => NamedKey.RandomToggle,
            0x1008ff9a => NamedKey.Subtitle,
            0x1008ff9b => NamedKey.MediaAudioTrack,
            0x1008ffa7 => NamedKey.Standby,
            0x1008ffa8 => NamedKey.Hibernate,
            0x1008fe22 => NamedKey.VideoModeNext,
            0x1005ff70 => NamedKey.Copy,
            0x1005ff73 => NamedKey.Open,
            0x1005ff75 => NamedKey.Paste,
            0x1005ff76 => NamedKey.Cut,
            0x1005ff10 => NamedKey.AudioVolumeDown,
            0x1005ff12 => NamedKey.AudioVolumeMute,
            0x1005ff11 => NamedKey.AudioVolumeUp,
            0x1005ff02 => NamedKey.BrightnessDown,
            0x1005ff03 => NamedKey.BrightnessUp,
            _ => null,
        };

        if (named is { } value)
        {
            return new Key(new Key.Named(value));
        }

        return keysym == 0
            ? new Key(new Key.Unidentified(new NativeKey(new NativeKey.Unidentified())))
            : new Key(new Key.Unidentified(new NativeKey(new NativeKey.Xkb(keysym))));
    }

    public static KeyLocation KeysymLocation(uint keysym)
    {
        return keysym switch
        {
            0xffe1 or 0xffe3 or 0xffe7 or 0xffe9 or 0xffeb or 0xffed => Winit.Core.KeyLocation.Left,
            0xffe2 or 0xffe4 or 0xffe8 or 0xffea or 0xffec or 0xffee => Winit.Core.KeyLocation.Right,
            >= 0xff80 and <= 0xffbd => Winit.Core.KeyLocation.Numpad,
            _ => Winit.Core.KeyLocation.Standard,
        };
    }

    public bool KeyRepeats(uint keycode)
    {
        return PInvoke.xkb_keymap_key_repeats(_keymap, keycode) == 1;
    }

    public uint GetKeysymByLevel(uint layout, uint keycode, uint level)
    {
        unsafe
        {
            uint* syms;
            int count = PInvoke.xkb_keymap_key_get_syms_by_level(_keymap, keycode, layout, level, &syms);
            return count > 0 ? syms[0] : 0;
        }
    }

    public void Dispose()
    {
        nint keymap = _keymap;
        if (keymap == 0)
        {
            return;
        }

        _keymap = 0;
        PInvoke.xkb_keymap_unref(keymap);
    }

    private static unsafe uint? ModIndexForName(nint keymap, string name)
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(name + '\0');
        fixed (byte* ptr = utf8)
        {
            uint index = PInvoke.xkb_keymap_mod_get_index(keymap, (sbyte*)ptr);
            return index == PInvoke.XkbModInvalid ? null : index;
        }
    }

    private static NamedKey? FunctionKeyFromKeysym(uint keysym)
    {
        return (keysym - 0xffbe) switch
        {
            0 => NamedKey.F1,
            1 => NamedKey.F2,
            2 => NamedKey.F3,
            3 => NamedKey.F4,
            4 => NamedKey.F5,
            5 => NamedKey.F6,
            6 => NamedKey.F7,
            7 => NamedKey.F8,
            8 => NamedKey.F9,
            9 => NamedKey.F10,
            10 => NamedKey.F11,
            11 => NamedKey.F12,
            12 => NamedKey.F13,
            13 => NamedKey.F14,
            14 => NamedKey.F15,
            15 => NamedKey.F16,
            16 => NamedKey.F17,
            17 => NamedKey.F18,
            18 => NamedKey.F19,
            19 => NamedKey.F20,
            20 => NamedKey.F21,
            21 => NamedKey.F22,
            22 => NamedKey.F23,
            23 => NamedKey.F24,
            24 => NamedKey.F25,
            25 => NamedKey.F26,
            26 => NamedKey.F27,
            27 => NamedKey.F28,
            28 => NamedKey.F29,
            29 => NamedKey.F30,
            30 => NamedKey.F31,
            31 => NamedKey.F32,
            32 => NamedKey.F33,
            33 => NamedKey.F34,
            34 => NamedKey.F35,
            _ => null,
        };
    }

    private static uint? CodeToScancode(KeyCode code)
    {
        return code switch
        {
            KeyCode.Escape => 1,
            KeyCode.Digit1 => 2,
            KeyCode.Digit2 => 3,
            KeyCode.Digit3 => 4,
            KeyCode.Digit4 => 5,
            KeyCode.Digit5 => 6,
            KeyCode.Digit6 => 7,
            KeyCode.Digit7 => 8,
            KeyCode.Digit8 => 9,
            KeyCode.Digit9 => 10,
            KeyCode.Digit0 => 11,
            KeyCode.Minus => 12,
            KeyCode.Equal => 13,
            KeyCode.Backspace => 14,
            KeyCode.Tab => 15,
            KeyCode.KeyQ => 16,
            KeyCode.KeyW => 17,
            KeyCode.KeyE => 18,
            KeyCode.KeyR => 19,
            KeyCode.KeyT => 20,
            KeyCode.KeyY => 21,
            KeyCode.KeyU => 22,
            KeyCode.KeyI => 23,
            KeyCode.KeyO => 24,
            KeyCode.KeyP => 25,
            KeyCode.BracketLeft => 26,
            KeyCode.BracketRight => 27,
            KeyCode.Enter => 28,
            KeyCode.ControlLeft => 29,
            KeyCode.KeyA => 30,
            KeyCode.KeyS => 31,
            KeyCode.KeyD => 32,
            KeyCode.KeyF => 33,
            KeyCode.KeyG => 34,
            KeyCode.KeyH => 35,
            KeyCode.KeyJ => 36,
            KeyCode.KeyK => 37,
            KeyCode.KeyL => 38,
            KeyCode.Semicolon => 39,
            KeyCode.Quote => 40,
            KeyCode.Backquote => 41,
            KeyCode.ShiftLeft => 42,
            KeyCode.Backslash => 43,
            KeyCode.KeyZ => 44,
            KeyCode.KeyX => 45,
            KeyCode.KeyC => 46,
            KeyCode.KeyV => 47,
            KeyCode.KeyB => 48,
            KeyCode.KeyN => 49,
            KeyCode.KeyM => 50,
            KeyCode.Comma => 51,
            KeyCode.Period => 52,
            KeyCode.Slash => 53,
            KeyCode.ShiftRight => 54,
            KeyCode.NumpadMultiply => 55,
            KeyCode.AltLeft => 56,
            KeyCode.Space => 57,
            KeyCode.CapsLock => 58,
            KeyCode.F1 => 59,
            KeyCode.F2 => 60,
            KeyCode.F3 => 61,
            KeyCode.F4 => 62,
            KeyCode.F5 => 63,
            KeyCode.F6 => 64,
            KeyCode.F7 => 65,
            KeyCode.F8 => 66,
            KeyCode.F9 => 67,
            KeyCode.F10 => 68,
            KeyCode.NumLock => 69,
            KeyCode.ScrollLock => 70,
            KeyCode.Numpad7 => 71,
            KeyCode.Numpad8 => 72,
            KeyCode.Numpad9 => 73,
            KeyCode.NumpadSubtract => 74,
            KeyCode.Numpad4 => 75,
            KeyCode.Numpad5 => 76,
            KeyCode.Numpad6 => 77,
            KeyCode.NumpadAdd => 78,
            KeyCode.Numpad1 => 79,
            KeyCode.Numpad2 => 80,
            KeyCode.Numpad3 => 81,
            KeyCode.Numpad0 => 82,
            KeyCode.NumpadDecimal => 83,
            KeyCode.Lang5 => 85,
            KeyCode.IntlBackslash => 86,
            KeyCode.F11 => 87,
            KeyCode.F12 => 88,
            KeyCode.IntlRo => 89,
            KeyCode.Lang3 => 90,
            KeyCode.Lang4 => 91,
            KeyCode.Convert => 92,
            KeyCode.KanaMode => 93,
            KeyCode.NonConvert => 94,
            KeyCode.NumpadEnter => 96,
            KeyCode.ControlRight => 97,
            KeyCode.NumpadDivide => 98,
            KeyCode.PrintScreen => 99,
            KeyCode.AltRight => 100,
            KeyCode.Home => 102,
            KeyCode.ArrowUp => 103,
            KeyCode.PageUp => 104,
            KeyCode.ArrowLeft => 105,
            KeyCode.ArrowRight => 106,
            KeyCode.End => 107,
            KeyCode.ArrowDown => 108,
            KeyCode.PageDown => 109,
            KeyCode.Insert => 110,
            KeyCode.Delete => 111,
            KeyCode.AudioVolumeMute => 113,
            KeyCode.AudioVolumeDown => 114,
            KeyCode.AudioVolumeUp => 115,
            KeyCode.Power => 116,
            KeyCode.NumpadEqual => 117,
            KeyCode.Pause => 119,
            KeyCode.ShowAllWindows => 120,
            KeyCode.NumpadComma => 121,
            KeyCode.Lang1 => 122,
            KeyCode.Lang2 => 123,
            KeyCode.IntlYen => 124,
            KeyCode.MetaLeft => 125,
            KeyCode.MetaRight => 126,
            KeyCode.ContextMenu => 127,
            KeyCode.BrowserStop => 128,
            KeyCode.Again => 129,
            KeyCode.Props => 130,
            KeyCode.Undo => 131,
            KeyCode.Select => 132,
            KeyCode.Copy => 133,
            KeyCode.Open => 134,
            KeyCode.Paste => 135,
            KeyCode.Find => 136,
            KeyCode.Cut => 137,
            KeyCode.Help => 138,
            KeyCode.LaunchApp2 => 140,
            KeyCode.Sleep => 142,
            KeyCode.WakeUp => 143,
            KeyCode.LaunchApp1 => 144,
            KeyCode.LaunchMail => 155,
            KeyCode.BrowserFavorites => 156,
            KeyCode.BrowserBack => 158,
            KeyCode.BrowserForward => 159,
            KeyCode.Eject => 161,
            KeyCode.MediaTrackNext => 163,
            KeyCode.MediaPlayPause => 164,
            KeyCode.MediaTrackPrevious => 165,
            KeyCode.MediaStop => 166,
            KeyCode.MediaRecord => 167,
            KeyCode.MediaRewind => 168,
            KeyCode.MediaSelect => 171,
            KeyCode.BrowserHome => 172,
            KeyCode.BrowserRefresh => 173,
            KeyCode.NumpadParenLeft => 179,
            KeyCode.NumpadParenRight => 180,
            KeyCode.F13 => 183,
            KeyCode.F14 => 184,
            KeyCode.F15 => 185,
            KeyCode.F16 => 186,
            KeyCode.F17 => 187,
            KeyCode.F18 => 188,
            KeyCode.F19 => 189,
            KeyCode.F20 => 190,
            KeyCode.F21 => 191,
            KeyCode.F22 => 192,
            KeyCode.F23 => 193,
            KeyCode.F24 => 194,
            KeyCode.MediaPause => 201,
            KeyCode.MediaPlay => 207,
            KeyCode.MediaFastForward => 208,
            KeyCode.BrowserSearch => 217,
            KeyCode.BrightnessDown => 224,
            KeyCode.BrightnessUp => 225,
            KeyCode.DisplayToggleIntExt => 227,
            KeyCode.KeyboardBacklightToggle => 228,
            KeyCode.MailSend => 231,
            KeyCode.MailReply => 232,
            KeyCode.MailForward => 233,
            KeyCode.MicrophoneMuteToggle => 248,
            KeyCode.ZoomToggle => 372,
            KeyCode.LaunchControlPanel => 579,
            KeyCode.SelectTask => 580,
            KeyCode.LaunchScreenSaver => 581,
            KeyCode.LaunchAssistant => 583,
            KeyCode.KeyboardLayoutSelect => 584,
            KeyCode.PrivacyScreenToggle => 633,
            _ => null,
        };
    }
}

public readonly record struct ModIndices(
    uint? Shift,
    uint? Caps,
    uint? Ctrl,
    uint? Alt,
    uint? Num,
    uint? Mod3,
    uint? Logo,
    uint? Mod5);
