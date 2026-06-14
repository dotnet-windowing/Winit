namespace Winit.X11.Util;

using Winit.Core;

internal enum Modifier
{
    Alt,
    Ctrl,
    Shift,
    Logo,
}

internal sealed class ModifierKeymap
{
    private const int ShiftOffset = 0;
    private const int ControlOffset = 2;
    private const int AltOffset = 3;
    private const int LogoOffset = 6;
    private const int NumMods = 8;

    private readonly Dictionary<byte, Modifier> _keys = [];

    public static ModifierKeymap New(XConnection xconn)
    {
        ModifierKeymap keymap = new();
        keymap.ReloadFromXConnection(xconn);
        return keymap;
    }

    public Modifier? GetModifier(byte keycode)
    {
        return _keys.TryGetValue(keycode, out Modifier modifier) ? modifier : null;
    }

    public unsafe void ReloadFromXConnection(XConnection xconn)
    {
        XModifierKeymap* keymap = PInvoke.XGetModifierMapping(xconn.Display);
        if (keymap is null)
        {
            return;
        }

        try
        {
            ResetFromXKeymap(keymap);
        }
        finally
        {
            _ = PInvoke.XFreeModifiermap(keymap);
        }
    }

    private unsafe void ResetFromXKeymap(XModifierKeymap* keymap)
    {
        _keys.Clear();
        if (keymap->ModifierMap is null || keymap->MaxKeyPerMod <= 0)
        {
            return;
        }

        int keysPerMod = keymap->MaxKeyPerMod;
        ReadOnlySpan<byte> keys = new(keymap->ModifierMap, checked(keysPerMod * NumMods));
        ReadKeys(keys, ShiftOffset, keysPerMod, Modifier.Shift);
        ReadKeys(keys, ControlOffset, keysPerMod, Modifier.Ctrl);
        ReadKeys(keys, AltOffset, keysPerMod, Modifier.Alt);
        ReadKeys(keys, LogoOffset, keysPerMod, Modifier.Logo);
    }

    private void ReadKeys(ReadOnlySpan<byte> keys, int offset, int keysPerMod, Modifier modifier)
    {
        int start = offset * keysPerMod;
        int end = start + keysPerMod;
        for (int i = start; i < end; i++)
        {
            byte keycode = keys[i];
            if (keycode != 0)
            {
                _keys[keycode] = modifier;
            }
        }
    }
}

internal sealed class ModifierKeyState
{
    private readonly Dictionary<byte, Modifier> _keys = [];
    private ModifiersState _state = ModifiersState.None;

    public ModifiersState Modifiers => _state;

    public void UpdateKeymap(ModifierKeymap mods)
    {
        foreach (byte key in _keys.Keys.ToArray())
        {
            Modifier? modifier = mods.GetModifier(key);
            if (modifier is { } value)
            {
                _keys[key] = value;
            }
            else
            {
                _keys.Remove(key);
            }
        }

        ResetState();
    }

    public ModifiersState? UpdateState(ModifiersState state, Modifier? except)
    {
        ModifiersState newState = state;
        if (except is { } value)
        {
            SetModifier(ref newState, value, GetModifier(_state, value));
        }

        if (_state == newState)
        {
            return null;
        }

        foreach (byte key in _keys.Keys.ToArray())
        {
            if (!GetModifier(newState, _keys[key]))
            {
                _keys.Remove(key);
            }
        }

        _state = newState;
        return newState;
    }

    public void KeyEvent(ElementState state, byte keycode, Modifier modifier)
    {
        if (state == ElementState.Pressed)
        {
            KeyPress(keycode, modifier);
        }
        else
        {
            KeyRelease(keycode);
        }
    }

    public void KeyPress(byte keycode, Modifier modifier)
    {
        _keys[keycode] = modifier;
        SetModifier(ref _state, modifier, true);
    }

    public void KeyRelease(byte keycode)
    {
        if (!_keys.Remove(keycode, out Modifier modifier))
        {
            return;
        }

        if (!_keys.Values.Any(value => value == modifier))
        {
            SetModifier(ref _state, modifier, false);
        }
    }

    private void ResetState()
    {
        ModifiersState state = ModifiersState.None;
        foreach (Modifier modifier in _keys.Values)
        {
            SetModifier(ref state, modifier, true);
        }

        _state = state;
    }

    private static bool GetModifier(ModifiersState state, Modifier modifier)
    {
        return modifier switch
        {
            Modifier.Alt => state.AltKey(),
            Modifier.Ctrl => state.ControlKey(),
            Modifier.Shift => state.ShiftKey(),
            Modifier.Logo => state.MetaKey(),
            _ => false,
        };
    }

    private static void SetModifier(ref ModifiersState state, Modifier modifier, bool value)
    {
        ModifiersState flag = modifier switch
        {
            Modifier.Alt => ModifiersState.Alt,
            Modifier.Ctrl => ModifiersState.Control,
            Modifier.Shift => ModifiersState.Shift,
            Modifier.Logo => ModifiersState.Meta,
            _ => ModifiersState.None,
        };

        if (value)
        {
            state |= flag;
        }
        else
        {
            state &= ~flag;
        }
    }
}
