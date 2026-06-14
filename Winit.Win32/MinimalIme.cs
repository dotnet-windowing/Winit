namespace Winit.Win32;

internal sealed class MinimalIme
{
    private const uint WmImeEndComposition = 0x010E;

    private readonly List<char> _utf16Parts = new(16);
    private bool _gettingImeText;

    public void Reset()
    {
        _gettingImeText = false;
        _utf16Parts.Clear();
    }

    public string? ProcessMessage(HWND hwnd, uint message, WPARAM wParam, out bool handled)
    {
        handled = false;

        if (message == WmImeEndComposition)
        {
            _gettingImeText = true;
            return null;
        }

        if (message is not (PInvoke.WM_CHAR or PInvoke.WM_SYSCHAR) || !_gettingImeText)
        {
            return null;
        }

        handled = true;
        _utf16Parts.Add(unchecked((char)wParam.Value));
        bool moreCharComing = NextKeyboardMessage(hwnd)?.message is PInvoke.WM_CHAR or PInvoke.WM_SYSCHAR;
        if (moreCharComing)
        {
            return null;
        }

        string text = new(_utf16Parts.ToArray());
        _utf16Parts.Clear();
        _gettingImeText = false;
        return text;
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
}
