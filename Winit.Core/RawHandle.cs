namespace Winit.Core;

public record struct RawDisplayHandle
{
    public readonly record struct Windows;

    public readonly record struct Xlib(nint? Display, int Screen);

    private const byte WindowsTag = 0;
    private const byte XlibTag = 1;

    private byte _tag;
    private Windows _windows;
    private Xlib _xlib;

    public RawDisplayHandle(Windows value)
    {
        this = default;
        _tag = WindowsTag;
        _windows = value;
    }

    public RawDisplayHandle(Xlib value)
    {
        this = default;
        _tag = XlibTag;
        _xlib = value;
    }

    public static RawDisplayHandle FromWindows()
    {
        return new RawDisplayHandle(new Windows());
    }

    public static RawDisplayHandle FromXlib(nint? display, int screen)
    {
        return new RawDisplayHandle(new Xlib(display, screen));
    }

    public bool TryGetValue(out Windows value)
    {
        value = _windows;
        return _tag == WindowsTag;
    }

    public bool TryGetValue(out Xlib value)
    {
        value = _xlib;
        return _tag == XlibTag;
    }
}

public record struct RawWindowHandle
{
    public readonly record struct Win32(nint Hwnd, nint? HInstance);

    public readonly record struct Xlib(nuint Window, nuint? VisualId);

    private const byte Win32Tag = 0;
    private const byte XlibTag = 1;

    private byte _tag;
    private Win32 _win32;
    private Xlib _xlib;

    public RawWindowHandle(Win32 value)
    {
        this = default;
        _tag = Win32Tag;
        _win32 = value;
    }

    public RawWindowHandle(Xlib value)
    {
        this = default;
        _tag = XlibTag;
        _xlib = value;
    }

    public static RawWindowHandle FromWin32(nint hwnd, nint? hInstance = null)
    {
        return new RawWindowHandle(new Win32(hwnd, hInstance));
    }

    public static RawWindowHandle FromXlib(nuint window, nuint? visualId = null)
    {
        return new RawWindowHandle(new Xlib(window, visualId));
    }

    public bool TryGetValue(out Win32 value)
    {
        value = _win32;
        return _tag == Win32Tag;
    }

    public bool TryGetValue(out Xlib value)
    {
        value = _xlib;
        return _tag == XlibTag;
    }
}
