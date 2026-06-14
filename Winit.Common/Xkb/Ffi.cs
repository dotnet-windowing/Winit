using System.Runtime.InteropServices;

namespace Winit.Common.Xkb;

internal static unsafe partial class PInvoke
{
    private const string XkbCommon = "libxkbcommon.so.0";
    private const string XkbCommonX11 = "libxkbcommon-x11.so.0";

    public const int XkbContextNoFlags = 0;
    public const int XkbKeymapCompileNoFlags = 0;
    public const int XkbKeymapFormatTextV1 = 1;
    public const int XkbComposeCompileNoFlags = 0;
    public const int XkbComposeStateNoFlags = 0;
    public const int XkbStateModsDepressed = 1;
    public const int XkbStateModsLatched = 2;
    public const int XkbStateModsLocked = 4;
    public const int XkbStateModsEffective = 8;
    public const int XkbX11SetupXkbExtensionNoFlags = 0;
    public const uint XkbModInvalid = uint.MaxValue;

    [LibraryImport(XkbCommon)]
    public static partial nint xkb_context_new(int flags);

    [LibraryImport(XkbCommon)]
    public static partial void xkb_context_unref(nint context);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_keysym_to_utf8(uint keysym, sbyte* buffer, nuint size);

    [LibraryImport(XkbCommon)]
    public static partial nint xkb_state_new(nint keymap);

    [LibraryImport(XkbCommon)]
    public static partial void xkb_state_unref(nint state);

    [LibraryImport(XkbCommon)]
    public static partial uint xkb_state_key_get_one_sym(nint state, uint keycode);

    [LibraryImport(XkbCommon)]
    public static partial uint xkb_state_key_get_layout(nint state, uint keycode);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_state_key_get_utf8(nint state, uint keycode, sbyte* buffer, nuint size);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_state_update_mask(
        nint state,
        uint depressedMods,
        uint latchedMods,
        uint lockedMods,
        uint depressedLayout,
        uint latchedLayout,
        uint lockedLayout);

    [LibraryImport(XkbCommon)]
    public static partial uint xkb_state_serialize_mods(nint state, int components);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_state_mod_name_is_active(nint state, sbyte* name, int type);

    [LibraryImport(XkbCommon)]
    public static partial void xkb_keymap_unref(nint keymap);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_keymap_key_repeats(nint keymap, uint keycode);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_keymap_key_get_syms_by_level(
        nint keymap,
        uint keycode,
        uint layout,
        uint level,
        uint** symsOut);

    [LibraryImport(XkbCommon)]
    public static partial uint xkb_keymap_mod_get_index(nint keymap, sbyte* name);

    [LibraryImport(XkbCommon)]
    public static partial nint xkb_compose_table_new_from_locale(nint context, sbyte* locale, int flags);

    [LibraryImport(XkbCommon)]
    public static partial void xkb_compose_table_unref(nint table);

    [LibraryImport(XkbCommon)]
    public static partial nint xkb_compose_state_new(nint table, int flags);

    [LibraryImport(XkbCommon)]
    public static partial void xkb_compose_state_unref(nint state);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_compose_state_feed(nint state, uint keysym);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_compose_state_get_status(nint state);

    [LibraryImport(XkbCommon)]
    public static partial int xkb_compose_state_get_utf8(nint state, sbyte* buffer, nuint size);

    [LibraryImport(XkbCommon)]
    public static partial void xkb_compose_state_reset(nint state);

    [LibraryImport(XkbCommonX11)]
    public static partial int xkb_x11_setup_xkb_extension(
        nint connection,
        ushort majorXkbVersion,
        ushort minorXkbVersion,
        int flags,
        ushort* majorXkbVersionOut,
        ushort* minorXkbVersionOut,
        byte* baseEventOut,
        byte* baseErrorOut);

    [LibraryImport(XkbCommonX11)]
    public static partial int xkb_x11_get_core_keyboard_device_id(nint connection);

    [LibraryImport(XkbCommonX11)]
    public static partial nint xkb_x11_keymap_new_from_device(nint context, nint connection, int deviceId, int flags);

    [LibraryImport(XkbCommonX11)]
    public static partial nint xkb_x11_state_new_from_device(nint keymap, nint connection, int deviceId);
}
