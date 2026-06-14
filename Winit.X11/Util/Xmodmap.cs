namespace Winit.X11.Util;

internal static class Xmodmap
{
    private const int NumMods = 8;

    internal sealed class ModifierKeymap
    {
        private readonly HashSet<byte> _modifiers = [];

        public static ModifierKeymap New(XConnection xconn)
        {
            ModifierKeymap keymap = new();
            keymap.ReloadFromXConnection(xconn);
            return keymap;
        }

        public bool IsModifier(uint keycode)
        {
            return keycode <= byte.MaxValue && _modifiers.Contains((byte)keycode);
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
            _modifiers.Clear();
            if (keymap->ModifierMap is null || keymap->MaxKeyPerMod <= 0)
            {
                return;
            }

            int keyCount = checked(keymap->MaxKeyPerMod * NumMods);
            ReadOnlySpan<byte> keys = new(keymap->ModifierMap, keyCount);
            foreach (byte key in keys)
            {
                if (key != 0)
                {
                    _modifiers.Add(key);
                }
            }
        }
    }
}
