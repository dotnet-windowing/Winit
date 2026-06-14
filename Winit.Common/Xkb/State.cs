using System.Text;
using Winit.Core;

namespace Winit.Common.Xkb;

public sealed class XkbState : IDisposable
{
    private nint _state;

    private XkbState(nint state)
    {
        _state = state;
    }

    public nint Handle => _state;

    public static XkbState? NewX11(nint xcbConnection, XkbKeymap keymap, int coreKeyboardId)
    {
        nint state = PInvoke.xkb_x11_state_new_from_device(keymap.Handle, xcbConnection, coreKeyboardId);
        return state == 0 ? null : new XkbState(state);
    }

    public uint GetOneSymRaw(uint keycode)
    {
        return PInvoke.xkb_state_key_get_one_sym(_state, keycode);
    }

    public uint Layout(uint keycode)
    {
        return PInvoke.xkb_state_key_get_layout(_state, keycode);
    }

    public unsafe int GetUtf8Raw(uint keycode, sbyte* buffer, nuint length)
    {
        return PInvoke.xkb_state_key_get_utf8(_state, keycode, buffer, length);
    }

    public ModifiersState Modifiers()
    {
        ModifiersState state = ModifiersState.None;

        if (ModNameIsActive("Shift"))
        {
            state |= ModifiersState.Shift;
        }

        if (ModNameIsActive("Control"))
        {
            state |= ModifiersState.Control;
        }

        if (ModNameIsActive("Mod1"))
        {
            state |= ModifiersState.Alt;
        }

        if (ModNameIsActive("Mod4"))
        {
            state |= ModifiersState.Meta;
        }

        return state;
    }

    public uint DepressedModifiers()
    {
        return PInvoke.xkb_state_serialize_mods(_state, PInvoke.XkbStateModsDepressed);
    }

    public uint LatchedModifiers()
    {
        return PInvoke.xkb_state_serialize_mods(_state, PInvoke.XkbStateModsLatched);
    }

    public uint LockedModifiers()
    {
        return PInvoke.xkb_state_serialize_mods(_state, PInvoke.XkbStateModsLocked);
    }

    public void UpdateModifiers(uint depressed, uint latched, uint locked, uint group)
    {
        _ = PInvoke.xkb_state_update_mask(_state, depressed, latched, locked, 0, 0, group);
    }

    public void UpdateModifiers(
        uint depressed,
        uint latched,
        uint locked,
        uint depressedLayout,
        uint latchedLayout,
        uint lockedLayout)
    {
        _ = PInvoke.xkb_state_update_mask(
            _state,
            depressed,
            latched,
            locked,
            depressedLayout,
            latchedLayout,
            lockedLayout);
    }

    public void Dispose()
    {
        nint state = _state;
        if (state == 0)
        {
            return;
        }

        _state = 0;
        PInvoke.xkb_state_unref(state);
    }

    private bool ModNameIsActive(string name)
    {
        byte[] utf8 = Encoding.ASCII.GetBytes(name + '\0');
        unsafe
        {
            fixed (byte* ptr = utf8)
            {
                return PInvoke.xkb_state_mod_name_is_active(
                    _state,
                    (sbyte*)ptr,
                    PInvoke.XkbStateModsEffective) == 1;
            }
        }
    }
}
