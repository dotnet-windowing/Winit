using Winit.Core;
using AndroidKeycode = global::Android.Views.Keycode;
using AndroidKeyEvent = global::Android.Views.KeyEvent;

namespace Winit.Android;

internal static class KeyCodes
{
    internal static KeyEvent BuildKeyEvent(AndroidKeycode keyCode, AndroidKeyEvent? e, ElementState state)
    {
        NativeKeyCode nativeCode = new(new NativeKeyCode.Android(checked((uint)(int)keyCode)));
        KeyCode code = ToKeyCode(keyCode);
        PhysicalKey physicalKey = code != KeyCode.Unidentified
            ? PhysicalKey.From(code)
            : PhysicalKey.From(nativeCode);
        string? text = state == ElementState.Pressed ? Text(e) : null;
        Key logicalKey = text is not null ? Key.FromCharacter(text) : ToLogicalKey(keyCode, nativeCode);

        return new KeyEvent(
            physicalKey,
            logicalKey,
            text,
            Location(keyCode),
            state,
            e?.RepeatCount > 0,
            text,
            logicalKey);
    }

    private static string? Text(AndroidKeyEvent? e)
    {
        int unicode = e?.UnicodeChar ?? 0;
        return unicode > 0 ? char.ConvertFromUtf32(unicode) : null;
    }

    private static Key ToLogicalKey(AndroidKeycode keyCode, NativeKeyCode nativeCode)
    {
        NamedKey named = ToNamedKey(keyCode);
        return named != NamedKey.Unidentified
            ? Key.From(named)
            : Key.From(nativeCode.ToNativeKey());
    }

    private static KeyLocation Location(AndroidKeycode keyCode)
    {
        return keyCode switch
        {
            AndroidKeycode.AltLeft or AndroidKeycode.CtrlLeft or AndroidKeycode.MetaLeft or
                AndroidKeycode.ShiftLeft => KeyLocation.Left,
            AndroidKeycode.AltRight or AndroidKeycode.CtrlRight or AndroidKeycode.MetaRight or
                AndroidKeycode.ShiftRight => KeyLocation.Right,
            AndroidKeycode.Numpad0 or AndroidKeycode.Numpad1 or AndroidKeycode.Numpad2 or
                AndroidKeycode.Numpad3 or AndroidKeycode.Numpad4 or AndroidKeycode.Numpad5 or
                AndroidKeycode.Numpad6 or AndroidKeycode.Numpad7 or AndroidKeycode.Numpad8 or
                AndroidKeycode.Numpad9 or AndroidKeycode.NumpadAdd or AndroidKeycode.NumpadSubtract or
                AndroidKeycode.NumpadMultiply or AndroidKeycode.NumpadDivide or AndroidKeycode.NumpadDot or
                AndroidKeycode.NumpadEnter or AndroidKeycode.NumpadEquals or AndroidKeycode.NumpadComma or
                AndroidKeycode.NumpadLeftParen or AndroidKeycode.NumpadRightParen => KeyLocation.Numpad,
            _ => KeyLocation.Standard,
        };
    }

    private static KeyCode ToKeyCode(AndroidKeycode keyCode)
    {
        return keyCode switch
        {
            AndroidKeycode.Grave => KeyCode.Backquote,
            AndroidKeycode.Backslash => KeyCode.Backslash,
            AndroidKeycode.LeftBracket => KeyCode.BracketLeft,
            AndroidKeycode.RightBracket => KeyCode.BracketRight,
            AndroidKeycode.Comma => KeyCode.Comma,
            AndroidKeycode.Num0 => KeyCode.Digit0,
            AndroidKeycode.Num1 => KeyCode.Digit1,
            AndroidKeycode.Num2 => KeyCode.Digit2,
            AndroidKeycode.Num3 => KeyCode.Digit3,
            AndroidKeycode.Num4 => KeyCode.Digit4,
            AndroidKeycode.Num5 => KeyCode.Digit5,
            AndroidKeycode.Num6 => KeyCode.Digit6,
            AndroidKeycode.Num7 => KeyCode.Digit7,
            AndroidKeycode.Num8 => KeyCode.Digit8,
            AndroidKeycode.Num9 => KeyCode.Digit9,
            AndroidKeycode.Equals => KeyCode.Equal,
            AndroidKeycode.A => KeyCode.KeyA,
            AndroidKeycode.B => KeyCode.KeyB,
            AndroidKeycode.C => KeyCode.KeyC,
            AndroidKeycode.D => KeyCode.KeyD,
            AndroidKeycode.E => KeyCode.KeyE,
            AndroidKeycode.F => KeyCode.KeyF,
            AndroidKeycode.G => KeyCode.KeyG,
            AndroidKeycode.H => KeyCode.KeyH,
            AndroidKeycode.I => KeyCode.KeyI,
            AndroidKeycode.J => KeyCode.KeyJ,
            AndroidKeycode.K => KeyCode.KeyK,
            AndroidKeycode.L => KeyCode.KeyL,
            AndroidKeycode.M => KeyCode.KeyM,
            AndroidKeycode.N => KeyCode.KeyN,
            AndroidKeycode.O => KeyCode.KeyO,
            AndroidKeycode.P => KeyCode.KeyP,
            AndroidKeycode.Q => KeyCode.KeyQ,
            AndroidKeycode.R => KeyCode.KeyR,
            AndroidKeycode.S => KeyCode.KeyS,
            AndroidKeycode.T => KeyCode.KeyT,
            AndroidKeycode.U => KeyCode.KeyU,
            AndroidKeycode.V => KeyCode.KeyV,
            AndroidKeycode.W => KeyCode.KeyW,
            AndroidKeycode.X => KeyCode.KeyX,
            AndroidKeycode.Y => KeyCode.KeyY,
            AndroidKeycode.Z => KeyCode.KeyZ,
            AndroidKeycode.Minus => KeyCode.Minus,
            AndroidKeycode.Period => KeyCode.Period,
            AndroidKeycode.Apostrophe => KeyCode.Quote,
            AndroidKeycode.Semicolon => KeyCode.Semicolon,
            AndroidKeycode.Slash => KeyCode.Slash,
            AndroidKeycode.AltLeft => KeyCode.AltLeft,
            AndroidKeycode.AltRight => KeyCode.AltRight,
            AndroidKeycode.Del => KeyCode.Backspace,
            AndroidKeycode.CapsLock => KeyCode.CapsLock,
            AndroidKeycode.CtrlLeft => KeyCode.ControlLeft,
            AndroidKeycode.CtrlRight => KeyCode.ControlRight,
            AndroidKeycode.Enter => KeyCode.Enter,
            AndroidKeycode.MetaLeft => KeyCode.MetaLeft,
            AndroidKeycode.MetaRight => KeyCode.MetaRight,
            AndroidKeycode.ShiftLeft => KeyCode.ShiftLeft,
            AndroidKeycode.ShiftRight => KeyCode.ShiftRight,
            AndroidKeycode.Space => KeyCode.Space,
            AndroidKeycode.Tab => KeyCode.Tab,
            AndroidKeycode.ForwardDel => KeyCode.Delete,
            AndroidKeycode.MoveEnd => KeyCode.End,
            AndroidKeycode.MoveHome => KeyCode.Home,
            AndroidKeycode.Insert => KeyCode.Insert,
            AndroidKeycode.PageDown => KeyCode.PageDown,
            AndroidKeycode.PageUp => KeyCode.PageUp,
            AndroidKeycode.DpadDown => KeyCode.ArrowDown,
            AndroidKeycode.DpadLeft => KeyCode.ArrowLeft,
            AndroidKeycode.DpadRight => KeyCode.ArrowRight,
            AndroidKeycode.DpadUp => KeyCode.ArrowUp,
            AndroidKeycode.NumLock => KeyCode.NumLock,
            AndroidKeycode.Numpad0 => KeyCode.Numpad0,
            AndroidKeycode.Numpad1 => KeyCode.Numpad1,
            AndroidKeycode.Numpad2 => KeyCode.Numpad2,
            AndroidKeycode.Numpad3 => KeyCode.Numpad3,
            AndroidKeycode.Numpad4 => KeyCode.Numpad4,
            AndroidKeycode.Numpad5 => KeyCode.Numpad5,
            AndroidKeycode.Numpad6 => KeyCode.Numpad6,
            AndroidKeycode.Numpad7 => KeyCode.Numpad7,
            AndroidKeycode.Numpad8 => KeyCode.Numpad8,
            AndroidKeycode.Numpad9 => KeyCode.Numpad9,
            AndroidKeycode.NumpadAdd => KeyCode.NumpadAdd,
            AndroidKeycode.NumpadSubtract => KeyCode.NumpadSubtract,
            AndroidKeycode.NumpadMultiply => KeyCode.NumpadMultiply,
            AndroidKeycode.NumpadDivide => KeyCode.NumpadDivide,
            AndroidKeycode.NumpadDot => KeyCode.NumpadDecimal,
            AndroidKeycode.NumpadEnter => KeyCode.NumpadEnter,
            AndroidKeycode.NumpadEquals => KeyCode.NumpadEqual,
            AndroidKeycode.NumpadComma => KeyCode.NumpadComma,
            AndroidKeycode.NumpadLeftParen => KeyCode.NumpadParenLeft,
            AndroidKeycode.NumpadRightParen => KeyCode.NumpadParenRight,
            AndroidKeycode.Escape => KeyCode.Escape,
            AndroidKeycode.Sysrq => KeyCode.PrintScreen,
            AndroidKeycode.ScrollLock => KeyCode.ScrollLock,
            AndroidKeycode.Break => KeyCode.Pause,
            AndroidKeycode.MediaPlayPause => KeyCode.MediaPlayPause,
            AndroidKeycode.MediaStop => KeyCode.MediaStop,
            AndroidKeycode.MediaNext => KeyCode.MediaTrackNext,
            AndroidKeycode.MediaPrevious => KeyCode.MediaTrackPrevious,
            AndroidKeycode.Power => KeyCode.Power,
            AndroidKeycode.VolumeDown => KeyCode.AudioVolumeDown,
            AndroidKeycode.VolumeMute => KeyCode.AudioVolumeMute,
            AndroidKeycode.VolumeUp => KeyCode.AudioVolumeUp,
            AndroidKeycode.F1 => KeyCode.F1,
            AndroidKeycode.F2 => KeyCode.F2,
            AndroidKeycode.F3 => KeyCode.F3,
            AndroidKeycode.F4 => KeyCode.F4,
            AndroidKeycode.F5 => KeyCode.F5,
            AndroidKeycode.F6 => KeyCode.F6,
            AndroidKeycode.F7 => KeyCode.F7,
            AndroidKeycode.F8 => KeyCode.F8,
            AndroidKeycode.F9 => KeyCode.F9,
            AndroidKeycode.F10 => KeyCode.F10,
            AndroidKeycode.F11 => KeyCode.F11,
            AndroidKeycode.F12 => KeyCode.F12,
            _ => KeyCode.Unidentified,
        };
    }

    private static NamedKey ToNamedKey(AndroidKeycode keyCode)
    {
        return keyCode switch
        {
            AndroidKeycode.AltLeft or AndroidKeycode.AltRight => NamedKey.Alt,
            AndroidKeycode.CapsLock => NamedKey.CapsLock,
            AndroidKeycode.CtrlLeft or AndroidKeycode.CtrlRight => NamedKey.Control,
            AndroidKeycode.MetaLeft or AndroidKeycode.MetaRight => NamedKey.Meta,
            AndroidKeycode.NumLock => NamedKey.NumLock,
            AndroidKeycode.ScrollLock => NamedKey.ScrollLock,
            AndroidKeycode.ShiftLeft or AndroidKeycode.ShiftRight => NamedKey.Shift,
            AndroidKeycode.Enter => NamedKey.Enter,
            AndroidKeycode.Tab => NamedKey.Tab,
            AndroidKeycode.DpadDown => NamedKey.ArrowDown,
            AndroidKeycode.DpadLeft => NamedKey.ArrowLeft,
            AndroidKeycode.DpadRight => NamedKey.ArrowRight,
            AndroidKeycode.DpadUp => NamedKey.ArrowUp,
            AndroidKeycode.MoveEnd => NamedKey.End,
            AndroidKeycode.MoveHome => NamedKey.Home,
            AndroidKeycode.PageDown => NamedKey.PageDown,
            AndroidKeycode.PageUp => NamedKey.PageUp,
            AndroidKeycode.Del => NamedKey.Backspace,
            AndroidKeycode.ForwardDel => NamedKey.Delete,
            AndroidKeycode.Clear => NamedKey.Clear,
            AndroidKeycode.Escape => NamedKey.Escape,
            AndroidKeycode.MediaPlayPause => NamedKey.MediaPlayPause,
            AndroidKeycode.MediaStop => NamedKey.MediaStop,
            AndroidKeycode.MediaNext => NamedKey.MediaTrackNext,
            AndroidKeycode.MediaPrevious => NamedKey.MediaTrackPrevious,
            AndroidKeycode.Power => NamedKey.Power,
            AndroidKeycode.VolumeDown => NamedKey.AudioVolumeDown,
            AndroidKeycode.VolumeMute => NamedKey.AudioVolumeMute,
            AndroidKeycode.VolumeUp => NamedKey.AudioVolumeUp,
            AndroidKeycode.F1 => NamedKey.F1,
            AndroidKeycode.F2 => NamedKey.F2,
            AndroidKeycode.F3 => NamedKey.F3,
            AndroidKeycode.F4 => NamedKey.F4,
            AndroidKeycode.F5 => NamedKey.F5,
            AndroidKeycode.F6 => NamedKey.F6,
            AndroidKeycode.F7 => NamedKey.F7,
            AndroidKeycode.F8 => NamedKey.F8,
            AndroidKeycode.F9 => NamedKey.F9,
            AndroidKeycode.F10 => NamedKey.F10,
            AndroidKeycode.F11 => NamedKey.F11,
            AndroidKeycode.F12 => NamedKey.F12,
            _ => NamedKey.Unidentified,
        };
    }
}
