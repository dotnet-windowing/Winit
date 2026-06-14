namespace Winit.Core;

public enum KeyCode
{
    Unidentified = 0,
    Backquote = 1,
    Backslash = 2,
    BracketLeft = 3,
    BracketRight = 4,
    Comma = 5,
    Digit0 = 6,
    Digit1 = 7,
    Digit2 = 8,
    Digit3 = 9,
    Digit4 = 10,
    Digit5 = 11,
    Digit6 = 12,
    Digit7 = 13,
    Digit8 = 14,
    Digit9 = 15,
    Equal = 16,
    IntlBackslash = 17,
    IntlRo = 18,
    IntlYen = 19,
    KeyA = 20,
    KeyB = 21,
    KeyC = 22,
    KeyD = 23,
    KeyE = 24,
    KeyF = 25,
    KeyG = 26,
    KeyH = 27,
    KeyI = 28,
    KeyJ = 29,
    KeyK = 30,
    KeyL = 31,
    KeyM = 32,
    KeyN = 33,
    KeyO = 34,
    KeyP = 35,
    KeyQ = 36,
    KeyR = 37,
    KeyS = 38,
    KeyT = 39,
    KeyU = 40,
    KeyV = 41,
    KeyW = 42,
    KeyX = 43,
    KeyY = 44,
    KeyZ = 45,
    Minus = 46,
    Period = 47,
    Quote = 48,
    Semicolon = 49,
    Slash = 50,
    AltLeft = 51,
    AltRight = 52,
    Backspace = 53,
    CapsLock = 54,
    ContextMenu = 55,
    ControlLeft = 56,
    ControlRight = 57,
    Enter = 58,
    MetaLeft = 59,
    MetaRight = 60,
    ShiftLeft = 61,
    ShiftRight = 62,
    Space = 63,
    Tab = 64,
    Convert = 65,
    KanaMode = 66,
    Lang1 = 67,
    Lang2 = 68,
    Lang3 = 69,
    Lang4 = 70,
    Lang5 = 71,
    NonConvert = 72,
    Delete = 73,
    End = 74,
    Help = 75,
    Home = 76,
    Insert = 77,
    PageDown = 78,
    PageUp = 79,
    ArrowDown = 80,
    ArrowLeft = 81,
    ArrowRight = 82,
    ArrowUp = 83,
    NumLock = 84,
    Numpad0 = 85,
    Numpad1 = 86,
    Numpad2 = 87,
    Numpad3 = 88,
    Numpad4 = 89,
    Numpad5 = 90,
    Numpad6 = 91,
    Numpad7 = 92,
    Numpad8 = 93,
    Numpad9 = 94,
    NumpadAdd = 95,
    NumpadBackspace = 96,
    NumpadClear = 97,
    NumpadClearEntry = 98,
    NumpadComma = 99,
    NumpadDecimal = 100,
    NumpadDivide = 101,
    NumpadEnter = 102,
    NumpadEqual = 103,
    NumpadHash = 104,
    NumpadMemoryAdd = 105,
    NumpadMemoryClear = 106,
    NumpadMemoryRecall = 107,
    NumpadMemoryStore = 108,
    NumpadMemorySubtract = 109,
    NumpadMultiply = 110,
    NumpadParenLeft = 111,
    NumpadParenRight = 112,
    NumpadStar = 113,
    NumpadSubtract = 114,
    Escape = 115,
    Fn = 116,
    FnLock = 117,
    PrintScreen = 118,
    ScrollLock = 119,
    Pause = 120,
    BrowserBack = 121,
    BrowserFavorites = 122,
    BrowserForward = 123,
    BrowserHome = 124,
    BrowserRefresh = 125,
    BrowserSearch = 126,
    BrowserStop = 127,
    Eject = 128,
    LaunchApp1 = 129,
    LaunchApp2 = 130,
    LaunchMail = 131,
    MediaPlayPause = 132,
    MediaSelect = 133,
    MediaStop = 134,
    MediaTrackNext = 135,
    MediaTrackPrevious = 136,
    Power = 137,
    Sleep = 138,
    AudioVolumeDown = 139,
    AudioVolumeMute = 140,
    AudioVolumeUp = 141,
    WakeUp = 142,
    Hyper = 143,
    Super = 144,
    Turbo = 145,
    Abort = 146,
    Resume = 147,
    Suspend = 148,
    Again = 149,
    Copy = 150,
    Cut = 151,
    Find = 152,
    Open = 153,
    Paste = 154,
    Props = 155,
    Select = 156,
    Undo = 157,
    Hiragana = 158,
    Katakana = 159,
    F1 = 160,
    F2 = 161,
    F3 = 162,
    F4 = 163,
    F5 = 164,
    F6 = 165,
    F7 = 166,
    F8 = 167,
    F9 = 168,
    F10 = 169,
    F11 = 170,
    F12 = 171,
    F13 = 172,
    F14 = 173,
    F15 = 174,
    F16 = 175,
    F17 = 176,
    F18 = 177,
    F19 = 178,
    F20 = 179,
    F21 = 180,
    F22 = 181,
    F23 = 182,
    F24 = 183,
    F25 = 184,
    F26 = 185,
    F27 = 186,
    F28 = 187,
    F29 = 188,
    F30 = 189,
    F31 = 190,
    F32 = 191,
    F33 = 192,
    F34 = 193,
    F35 = 194,
    BrightnessDown = 195,
    BrightnessUp = 196,
    DisplayToggleIntExt = 197,
    KeyboardLayoutSelect = 198,
    LaunchAssistant = 199,
    LaunchControlPanel = 200,
    LaunchScreenSaver = 201,
    MailForward = 202,
    MailReply = 203,
    MailSend = 204,
    MediaFastForward = 205,
    MediaPause = 206,
    MediaPlay = 207,
    MediaRecord = 208,
    MediaRewind = 209,
    MicrophoneMuteToggle = 210,
    PrivacyScreenToggle = 211,
    KeyboardBacklightToggle = 212,
    SelectTask = 213,
    ShowAllWindows = 214,
    ZoomToggle = 215,
}

public enum KeyLocation
{
    Standard = 0x00,
    Left = 0x01,
    Right = 0x02,
    Numpad = 0x03,
}

public enum NamedKey
{
    Unidentified = 0,
    Alt = 1,
    AltGraph = 2,
    CapsLock = 3,
    Control = 4,
    Fn = 5,
    FnLock = 6,
    Meta = 7,
    NumLock = 8,
    ScrollLock = 9,
    Shift = 10,
    Symbol = 11,
    SymbolLock = 12,
    Hyper = 13,
    Super = 14,
    Enter = 15,
    Tab = 16,
    ArrowDown = 17,
    ArrowLeft = 18,
    ArrowRight = 19,
    ArrowUp = 20,
    End = 21,
    Home = 22,
    PageDown = 23,
    PageUp = 24,
    Backspace = 25,
    Clear = 26,
    Copy = 27,
    CrSel = 28,
    Cut = 29,
    Delete = 30,
    EraseEof = 31,
    ExSel = 32,
    Insert = 33,
    Paste = 34,
    Redo = 35,
    Undo = 36,
    Accept = 37,
    Again = 38,
    Attn = 39,
    Cancel = 40,
    ContextMenu = 41,
    Escape = 42,
    Execute = 43,
    Find = 44,
    Help = 45,
    Pause = 46,
    Play = 47,
    Props = 48,
    Select = 49,
    ZoomIn = 50,
    ZoomOut = 51,
    BrightnessDown = 52,
    BrightnessUp = 53,
    Eject = 54,
    LogOff = 55,
    Power = 56,
    PowerOff = 57,
    PrintScreen = 58,
    Hibernate = 59,
    Standby = 60,
    WakeUp = 61,
    AllCandidates = 62,
    Alphanumeric = 63,
    CodeInput = 64,
    Compose = 65,
    Convert = 66,
    Dead = 67,
    FinalMode = 68,
    GroupFirst = 69,
    GroupLast = 70,
    GroupNext = 71,
    GroupPrevious = 72,
    ModeChange = 73,
    NextCandidate = 74,
    NonConvert = 75,
    PreviousCandidate = 76,
    Process = 77,
    SingleCandidate = 78,
    HangulMode = 79,
    HanjaMode = 80,
    JunjaMode = 81,
    Eisu = 82,
    Hankaku = 83,
    Hiragana = 84,
    HiraganaKatakana = 85,
    KanaMode = 86,
    KanjiMode = 87,
    Katakana = 88,
    Romaji = 89,
    Zenkaku = 90,
    ZenkakuHankaku = 91,
    Soft1 = 92,
    Soft2 = 93,
    Soft3 = 94,
    Soft4 = 95,
    ChannelDown = 96,
    ChannelUp = 97,
    Close = 98,
    MailForward = 99,
    MailReply = 100,
    MailSend = 101,
    MediaClose = 102,
    MediaFastForward = 103,
    MediaPause = 104,
    MediaPlay = 105,
    MediaPlayPause = 106,
    MediaRecord = 107,
    MediaRewind = 108,
    MediaStop = 109,
    MediaTrackNext = 110,
    MediaTrackPrevious = 111,
    New = 112,
    Open = 113,
    Print = 114,
    Save = 115,
    SpellCheck = 116,
    Key11 = 117,
    Key12 = 118,
    AudioBalanceLeft = 119,
    AudioBalanceRight = 120,
    AudioBassBoostDown = 121,
    AudioBassBoostToggle = 122,
    AudioBassBoostUp = 123,
    AudioFaderFront = 124,
    AudioFaderRear = 125,
    AudioSurroundModeNext = 126,
    AudioTrebleDown = 127,
    AudioTrebleUp = 128,
    AudioVolumeDown = 129,
    AudioVolumeUp = 130,
    AudioVolumeMute = 131,
    MicrophoneToggle = 132,
    MicrophoneVolumeDown = 133,
    MicrophoneVolumeUp = 134,
    MicrophoneVolumeMute = 135,
    SpeechCorrectionList = 136,
    SpeechInputToggle = 137,
    LaunchApplication1 = 138,
    LaunchApplication2 = 139,
    LaunchCalendar = 140,
    LaunchContacts = 141,
    LaunchMail = 142,
    LaunchMediaPlayer = 143,
    LaunchMusicPlayer = 144,
    LaunchPhone = 145,
    LaunchScreenSaver = 146,
    LaunchSpreadsheet = 147,
    LaunchWebBrowser = 148,
    LaunchWebCam = 149,
    LaunchWordProcessor = 150,
    BrowserBack = 151,
    BrowserFavorites = 152,
    BrowserForward = 153,
    BrowserHome = 154,
    BrowserRefresh = 155,
    BrowserSearch = 156,
    BrowserStop = 157,
    AppSwitch = 158,
    Call = 159,
    Camera = 160,
    CameraFocus = 161,
    EndCall = 162,
    GoBack = 163,
    GoHome = 164,
    HeadsetHook = 165,
    LastNumberRedial = 166,
    Notification = 167,
    MannerMode = 168,
    VoiceDial = 169,
    TV = 170,
    TV3DMode = 171,
    TVAntennaCable = 172,
    TVAudioDescription = 173,
    TVAudioDescriptionMixDown = 174,
    TVAudioDescriptionMixUp = 175,
    TVContentsMenu = 176,
    TVDataService = 177,
    TVInput = 178,
    TVInputComponent1 = 179,
    TVInputComponent2 = 180,
    TVInputComposite1 = 181,
    TVInputComposite2 = 182,
    TVInputHDMI1 = 183,
    TVInputHDMI2 = 184,
    TVInputHDMI3 = 185,
    TVInputHDMI4 = 186,
    TVInputVGA1 = 187,
    TVMediaContext = 188,
    TVNetwork = 189,
    TVNumberEntry = 190,
    TVPower = 191,
    TVRadioService = 192,
    TVSatellite = 193,
    TVSatelliteBS = 194,
    TVSatelliteCS = 195,
    TVSatelliteToggle = 196,
    TVTerrestrialAnalog = 197,
    TVTerrestrialDigital = 198,
    TVTimer = 199,
    AVRInput = 200,
    AVRPower = 201,
    ColorF0Red = 202,
    ColorF1Green = 203,
    ColorF2Yellow = 204,
    ColorF3Blue = 205,
    ColorF4Grey = 206,
    ColorF5Brown = 207,
    ClosedCaptionToggle = 208,
    Dimmer = 209,
    DisplaySwap = 210,
    DVR = 211,
    Exit = 212,
    FavoriteClear0 = 213,
    FavoriteClear1 = 214,
    FavoriteClear2 = 215,
    FavoriteClear3 = 216,
    FavoriteRecall0 = 217,
    FavoriteRecall1 = 218,
    FavoriteRecall2 = 219,
    FavoriteRecall3 = 220,
    FavoriteStore0 = 221,
    FavoriteStore1 = 222,
    FavoriteStore2 = 223,
    FavoriteStore3 = 224,
    Guide = 225,
    GuideNextDay = 226,
    GuidePreviousDay = 227,
    Info = 228,
    InstantReplay = 229,
    Link = 230,
    ListProgram = 231,
    LiveContent = 232,
    Lock = 233,
    MediaApps = 234,
    MediaAudioTrack = 235,
    MediaLast = 236,
    MediaSkipBackward = 237,
    MediaSkipForward = 238,
    MediaStepBackward = 239,
    MediaStepForward = 240,
    MediaTopMenu = 241,
    NavigateIn = 242,
    NavigateNext = 243,
    NavigateOut = 244,
    NavigatePrevious = 245,
    NextFavoriteChannel = 246,
    NextUserProfile = 247,
    OnDemand = 248,
    Pairing = 249,
    PinPDown = 250,
    PinPMove = 251,
    PinPToggle = 252,
    PinPUp = 253,
    PlaySpeedDown = 254,
    PlaySpeedReset = 255,
    PlaySpeedUp = 256,
    RandomToggle = 257,
    RcLowBattery = 258,
    RecordSpeedNext = 259,
    RfBypass = 260,
    ScanChannelsToggle = 261,
    ScreenModeNext = 262,
    Settings = 263,
    SplitScreenToggle = 264,
    STBInput = 265,
    STBPower = 266,
    Subtitle = 267,
    Teletext = 268,
    VideoModeNext = 269,
    Wink = 270,
    ZoomToggle = 271,
    F1 = 272,
    F2 = 273,
    F3 = 274,
    F4 = 275,
    F5 = 276,
    F6 = 277,
    F7 = 278,
    F8 = 279,
    F9 = 280,
    F10 = 281,
    F11 = 282,
    F12 = 283,
    F13 = 284,
    F14 = 285,
    F15 = 286,
    F16 = 287,
    F17 = 288,
    F18 = 289,
    F19 = 290,
    F20 = 291,
    F21 = 292,
    F22 = 293,
    F23 = 294,
    F24 = 295,
    F25 = 296,
    F26 = 297,
    F27 = 298,
    F28 = 299,
    F29 = 300,
    F30 = 301,
    F31 = 302,
    F32 = 303,
    F33 = 304,
    F34 = 305,
    F35 = 306,
}

public record struct NativeKeyCode
{
    public readonly record struct Unidentified;

    public readonly record struct Android(uint Code);

    public readonly record struct MacOS(ushort Code);

    public readonly record struct Windows(ushort Code);

    public readonly record struct Xkb(uint Code);

    public readonly record struct Ohos(uint Code);

    private const byte UnidentifiedTag = 0;
    private const byte AndroidTag = 1;
    private const byte MacOSTag = 2;
    private const byte WindowsTag = 3;
    private const byte XkbTag = 4;
    private const byte OhosTag = 5;

    private byte _tag;
    private Unidentified _unidentified;
    private Android _android;
    private MacOS _macOS;
    private Windows _windows;
    private Xkb _xkb;
    private Ohos _ohos;

    public NativeKeyCode(Unidentified value)
    {
        _tag = UnidentifiedTag;
        _unidentified = value;
        _android = default;
        _macOS = default;
        _windows = default;
        _xkb = default;
        _ohos = default;
    }

    public NativeKeyCode(Android value)
    {
        _tag = AndroidTag;
        _unidentified = default;
        _android = value;
        _macOS = default;
        _windows = default;
        _xkb = default;
        _ohos = default;
    }

    public NativeKeyCode(MacOS value)
    {
        _tag = MacOSTag;
        _unidentified = default;
        _android = default;
        _macOS = value;
        _windows = default;
        _xkb = default;
        _ohos = default;
    }

    public NativeKeyCode(Windows value)
    {
        _tag = WindowsTag;
        _unidentified = default;
        _android = default;
        _macOS = default;
        _windows = value;
        _xkb = default;
        _ohos = default;
    }

    public NativeKeyCode(Xkb value)
    {
        _tag = XkbTag;
        _unidentified = default;
        _android = default;
        _macOS = default;
        _windows = default;
        _xkb = value;
        _ohos = default;
    }

    public NativeKeyCode(Ohos value)
    {
        _tag = OhosTag;
        _unidentified = default;
        _android = default;
        _macOS = default;
        _windows = default;
        _xkb = default;
        _ohos = value;
    }

    public bool TryGetValue(out Unidentified value)
    {
        value = _unidentified;
        return _tag == UnidentifiedTag;
    }

    public bool TryGetValue(out Android value)
    {
        value = _android;
        return _tag == AndroidTag;
    }

    public bool TryGetValue(out MacOS value)
    {
        value = _macOS;
        return _tag == MacOSTag;
    }

    public bool TryGetValue(out Windows value)
    {
        value = _windows;
        return _tag == WindowsTag;
    }

    public bool TryGetValue(out Xkb value)
    {
        value = _xkb;
        return _tag == XkbTag;
    }

    public bool TryGetValue(out Ohos value)
    {
        value = _ohos;
        return _tag == OhosTag;
    }

    public NativeKey ToNativeKey()
    {
        return NativeKey.From(this);
    }

    public bool Is(NativeKey key)
    {
        return ToNativeKey().Equals(key);
    }

    public override string ToString()
    {
        if (TryGetValue(out Android android))
        {
            return $"Android(0x{android.Code:X4})";
        }

        if (TryGetValue(out MacOS macOS))
        {
            return $"MacOS(0x{macOS.Code:X4})";
        }

        if (TryGetValue(out Windows windows))
        {
            return $"Windows(0x{windows.Code:X4})";
        }

        if (TryGetValue(out Xkb xkb))
        {
            return $"Xkb(0x{xkb.Code:X4})";
        }

        return TryGetValue(out Ohos ohos) ? $"OpenHarmony(0x{ohos.Code:X4})" : "Unidentified";
    }
}

public record struct NativeKey
{
    public readonly record struct Unidentified;

    public readonly record struct Android(uint Code);

    public readonly record struct MacOS(ushort Code);

    public readonly record struct Windows(ushort Code);

    public readonly record struct Xkb(uint Code);

    public readonly record struct Web(string Code);

    public readonly record struct Ohos(uint Code);

    private const byte UnidentifiedTag = 0;
    private const byte AndroidTag = 1;
    private const byte MacOSTag = 2;
    private const byte WindowsTag = 3;
    private const byte XkbTag = 4;
    private const byte WebTag = 5;
    private const byte OhosTag = 6;

    private byte _tag;
    private Unidentified _unidentified;
    private Android _android;
    private MacOS _macOS;
    private Windows _windows;
    private Xkb _xkb;
    private Web _web;
    private Ohos _ohos;

    public NativeKey(Unidentified value)
    {
        _tag = UnidentifiedTag;
        _unidentified = value;
        _android = default;
        _macOS = default;
        _windows = default;
        _xkb = default;
        _web = default;
        _ohos = default;
    }

    public NativeKey(Android value)
    {
        _tag = AndroidTag;
        _unidentified = default;
        _android = value;
        _macOS = default;
        _windows = default;
        _xkb = default;
        _web = default;
        _ohos = default;
    }

    public NativeKey(MacOS value)
    {
        _tag = MacOSTag;
        _unidentified = default;
        _android = default;
        _macOS = value;
        _windows = default;
        _xkb = default;
        _web = default;
        _ohos = default;
    }

    public NativeKey(Windows value)
    {
        _tag = WindowsTag;
        _unidentified = default;
        _android = default;
        _macOS = default;
        _windows = value;
        _xkb = default;
        _web = default;
        _ohos = default;
    }

    public NativeKey(Xkb value)
    {
        _tag = XkbTag;
        _unidentified = default;
        _android = default;
        _macOS = default;
        _windows = default;
        _xkb = value;
        _web = default;
        _ohos = default;
    }

    public NativeKey(Web value)
    {
        _tag = WebTag;
        _unidentified = default;
        _android = default;
        _macOS = default;
        _windows = default;
        _xkb = default;
        _web = value;
        _ohos = default;
    }

    public NativeKey(Ohos value)
    {
        _tag = OhosTag;
        _unidentified = default;
        _android = default;
        _macOS = default;
        _windows = default;
        _xkb = default;
        _web = default;
        _ohos = value;
    }

    public static NativeKey From(NativeKeyCode code)
    {
        if (code.TryGetValue(out NativeKeyCode.Android android))
        {
            return new NativeKey(new Android(android.Code));
        }

        if (code.TryGetValue(out NativeKeyCode.MacOS macOS))
        {
            return new NativeKey(new MacOS(macOS.Code));
        }

        if (code.TryGetValue(out NativeKeyCode.Windows windows))
        {
            return new NativeKey(new Windows(windows.Code));
        }

        if (code.TryGetValue(out NativeKeyCode.Xkb xkb))
        {
            return new NativeKey(new Xkb(xkb.Code));
        }

        if (code.TryGetValue(out NativeKeyCode.Ohos ohos))
        {
            return new NativeKey(new Ohos(ohos.Code));
        }

        return new NativeKey(new Unidentified());
    }

    public bool TryGetValue(out Unidentified value)
    {
        value = _unidentified;
        return _tag == UnidentifiedTag;
    }

    public bool TryGetValue(out Android value)
    {
        value = _android;
        return _tag == AndroidTag;
    }

    public bool TryGetValue(out MacOS value)
    {
        value = _macOS;
        return _tag == MacOSTag;
    }

    public bool TryGetValue(out Windows value)
    {
        value = _windows;
        return _tag == WindowsTag;
    }

    public bool TryGetValue(out Xkb value)
    {
        value = _xkb;
        return _tag == XkbTag;
    }

    public bool TryGetValue(out Web value)
    {
        value = _web;
        return _tag == WebTag;
    }

    public bool TryGetValue(out Ohos value)
    {
        value = _ohos;
        return _tag == OhosTag;
    }

    public bool Is(NativeKeyCode code)
    {
        return code.Is(this);
    }

    public override string ToString()
    {
        if (TryGetValue(out Android android))
        {
            return $"Android(0x{android.Code:X4})";
        }

        if (TryGetValue(out MacOS macOS))
        {
            return $"MacOS(0x{macOS.Code:X4})";
        }

        if (TryGetValue(out Windows windows))
        {
            return $"Windows(0x{windows.Code:X4})";
        }

        if (TryGetValue(out Xkb xkb))
        {
            return $"Xkb(0x{xkb.Code:X4})";
        }

        if (TryGetValue(out Web web))
        {
            return $"Web({web.Code})";
        }

        return TryGetValue(out Ohos ohos) ? $"OpenHarmony({ohos.Code})" : "Unidentified";
    }
}

public record struct PhysicalKey
{
    public readonly record struct Code(KeyCode KeyCode);

    public readonly record struct Unidentified(NativeKeyCode NativeKeyCode);

    private const byte CodeTag = 0;
    private const byte UnidentifiedTag = 1;

    private byte _tag;
    private Code _code;
    private Unidentified _unidentified;

    public PhysicalKey(Code value)
    {
        _tag = CodeTag;
        _code = value;
        _unidentified = default;
    }

    public PhysicalKey(Unidentified value)
    {
        _tag = UnidentifiedTag;
        _code = default;
        _unidentified = value;
    }

    public bool TryGetValue(out Code value)
    {
        value = _code;
        return _tag == CodeTag;
    }

    public bool TryGetValue(out Unidentified value)
    {
        value = _unidentified;
        return _tag == UnidentifiedTag;
    }

    public static PhysicalKey From(KeyCode code)
    {
        return new PhysicalKey(new Code(code));
    }

    public static PhysicalKey From(NativeKeyCode code)
    {
        return new PhysicalKey(new Unidentified(code));
    }

    public KeyCode ToKeyCode()
    {
        return TryGetValue(out Code code) ? code.KeyCode : KeyCode.Unidentified;
    }

    public bool Is(KeyCode code)
    {
        return TryGetValue(out Code value) && value.KeyCode == code;
    }

    public bool Is(NativeKeyCode code)
    {
        return TryGetValue(out Unidentified value) && value.NativeKeyCode.Equals(code);
    }
}

public record struct Key
{
    public readonly record struct Named(NamedKey NamedKey);

    public readonly record struct Character(string Value);

    public readonly record struct Unidentified(NativeKey NativeKey);

    public readonly record struct Dead(char? Value);

    private const byte NamedTag = 0;
    private const byte CharacterTag = 1;
    private const byte UnidentifiedTag = 2;
    private const byte DeadTag = 3;

    private byte _tag;
    private Named _named;
    private Character _character;
    private Unidentified _unidentified;
    private Dead _dead;

    public Key(Named value)
    {
        _tag = NamedTag;
        _named = value;
        _character = default;
        _unidentified = default;
        _dead = default;
    }

    public Key(Character value)
    {
        _tag = CharacterTag;
        _named = default;
        _character = value;
        _unidentified = default;
        _dead = default;
    }

    public Key(Unidentified value)
    {
        _tag = UnidentifiedTag;
        _named = default;
        _character = default;
        _unidentified = value;
        _dead = default;
    }

    public Key(Dead value)
    {
        _tag = DeadTag;
        _named = default;
        _character = default;
        _unidentified = default;
        _dead = value;
    }

    public static Key From(NamedKey namedKey)
    {
        return new Key(new Named(namedKey));
    }

    public static Key From(NativeKey nativeKey)
    {
        return new Key(new Unidentified(nativeKey));
    }

    public static Key FromCharacter(string value)
    {
        return new Key(new Character(value));
    }

    public bool Is(NamedKey namedKey)
    {
        return TryGetValue(out Named named) && named.NamedKey == namedKey;
    }

    public bool Is(string text)
    {
        return TryGetValue(out Character character) && character.Value == text;
    }

    public bool Is(NativeKey nativeKey)
    {
        return TryGetValue(out Unidentified unidentified) && unidentified.NativeKey.Equals(nativeKey);
    }

    public string? ToText()
    {
        if (TryGetValue(out Named named))
        {
            return named.NamedKey switch
            {
                NamedKey.Enter => "\r",
                NamedKey.Backspace => "\b",
                NamedKey.Tab => "\t",
                NamedKey.Escape => "\e",
                _ => null,
            };
        }

        return TryGetValue(out Character character) ? character.Value : null;
    }

    public bool TryGetValue(out Named value)
    {
        value = _named;
        return _tag == NamedTag;
    }

    public bool TryGetValue(out Character value)
    {
        value = _character;
        return _tag == CharacterTag;
    }

    public bool TryGetValue(out Unidentified value)
    {
        value = _unidentified;
        return _tag == UnidentifiedTag;
    }

    public bool TryGetValue(out Dead value)
    {
        value = _dead;
        return _tag == DeadTag;
    }
}

[Flags]
public enum ModifiersState : uint
{
    None = 0,
    Shift = 0b100,
    Control = 0b100 << 3,
    Alt = 0b100 << 6,
    Meta = 0b100 << 9,
    Super = Meta,
}

public static class ModifiersStateExtensions
{
    public static bool ShiftKey(this ModifiersState state)
    {
        return (state & ModifiersState.Shift) != 0;
    }

    public static bool ControlKey(this ModifiersState state)
    {
        return (state & ModifiersState.Control) != 0;
    }

    public static bool AltKey(this ModifiersState state)
    {
        return (state & ModifiersState.Alt) != 0;
    }

    public static bool MetaKey(this ModifiersState state)
    {
        return (state & ModifiersState.Meta) != 0;
    }
}

public enum ModifiersKeyState
{
    Unknown = 0,
    Pressed = 1,
}

[Flags]
public enum ModifiersKeys : byte
{
    None = 0,
    LShift = 0b0000_0001,
    RShift = 0b0000_0010,
    LControl = 0b0000_0100,
    RControl = 0b0000_1000,
    LAlt = 0b0001_0000,
    RAlt = 0b0010_0000,
    LMeta = 0b0100_0000,
    RMeta = 0b1000_0000,
    LSuper = LMeta,
    RSuper = RMeta,
}
