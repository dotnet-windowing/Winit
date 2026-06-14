namespace Winit.Core;

public record struct RawDisplayHandle
{
    public readonly record struct Windows;

    private const byte WindowsTag = 0;

    private byte _tag;
    private Windows _windows;

    public RawDisplayHandle(Windows value)
    {
        this = default;
        _tag = WindowsTag;
        _windows = value;
    }

    public static RawDisplayHandle FromWindows()
    {
        return new RawDisplayHandle(new Windows());
    }

    public bool TryGetValue(out Windows value)
    {
        value = _windows;
        return _tag == WindowsTag;
    }
}

public record struct RawWindowHandle
{
    public readonly record struct Win32(nint Hwnd, nint? HInstance);

    private const byte Win32Tag = 0;

    private byte _tag;
    private Win32 _win32;

    public RawWindowHandle(Win32 value)
    {
        this = default;
        _tag = Win32Tag;
        _win32 = value;
    }

    public static RawWindowHandle FromWin32(nint hwnd, nint? hInstance = null)
    {
        return new RawWindowHandle(new Win32(hwnd, hInstance));
    }

    public bool TryGetValue(out Win32 value)
    {
        value = _win32;
        return _tag == Win32Tag;
    }
}
