namespace RawWindowHandles;

public interface IHasDisplayHandle
{
    RawDisplayHandle? DisplayHandle { get; }
}

public interface IHasWindowHandle : IHasDisplayHandle
{
    RawWindowHandle? WindowHandle { get; }
}

public sealed class OwnedDisplayHandle(RawDisplayHandle? handle) : IEquatable<OwnedDisplayHandle>
{
    public RawDisplayHandle? Handle { get; } = handle;

    public bool Equals(OwnedDisplayHandle? other)
    {
        return other is not null && EqualityComparer<RawDisplayHandle?>.Default.Equals(Handle, other.Handle);
    }

    public override bool Equals(object? obj)
    {
        return obj is OwnedDisplayHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Handle?.GetHashCode() ?? 0;
    }
}

public record struct RawDisplayHandle
{
    public readonly record struct Android;

    public readonly record struct Windows;

    public readonly record struct Xlib(nint? Display, int Screen);

    public readonly record struct Wayland(nint Display);

    private const byte AndroidTag = 0;
    private const byte WindowsTag = 1;
    private const byte XlibTag = 2;
    private const byte WaylandTag = 3;

    private byte _tag;
    private Android _android;
    private Windows _windows;
    private Xlib _xlib;
    private Wayland _wayland;

    public RawDisplayHandle(Android value)
    {
        this = default;
        _tag = AndroidTag;
        _android = value;
    }

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

    public RawDisplayHandle(Wayland value)
    {
        this = default;
        _tag = WaylandTag;
        _wayland = value;
    }

    public static RawDisplayHandle FromAndroid()
    {
        return new RawDisplayHandle(new Android());
    }

    public static RawDisplayHandle FromWindows()
    {
        return new RawDisplayHandle(new Windows());
    }

    public static RawDisplayHandle FromXlib(nint? display, int screen)
    {
        return new RawDisplayHandle(new Xlib(display, screen));
    }

    public static RawDisplayHandle FromWayland(nint display)
    {
        return new RawDisplayHandle(new Wayland(display));
    }

    public bool TryGetValue(out Android value)
    {
        value = _android;
        return _tag == AndroidTag;
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

    public bool TryGetValue(out Wayland value)
    {
        value = _wayland;
        return _tag == WaylandTag;
    }
}

public record struct RawWindowHandle
{
    public readonly record struct AndroidNdk(nint ANativeWindow);

    public readonly record struct Win32(nint Hwnd, nint? HInstance);

    public readonly record struct Xlib(nuint Window, nuint? VisualId);

    public readonly record struct Wayland(nint Surface);

    private const byte AndroidNdkTag = 0;
    private const byte Win32Tag = 1;
    private const byte XlibTag = 2;
    private const byte WaylandTag = 3;

    private byte _tag;
    private AndroidNdk _androidNdk;
    private Win32 _win32;
    private Xlib _xlib;
    private Wayland _wayland;

    public RawWindowHandle(AndroidNdk value)
    {
        this = default;
        _tag = AndroidNdkTag;
        _androidNdk = value;
    }

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

    public RawWindowHandle(Wayland value)
    {
        this = default;
        _tag = WaylandTag;
        _wayland = value;
    }

    public static RawWindowHandle FromAndroidNdk(nint aNativeWindow)
    {
        return new RawWindowHandle(new AndroidNdk(aNativeWindow));
    }

    public static RawWindowHandle FromWin32(nint hwnd, nint? hInstance = null)
    {
        return new RawWindowHandle(new Win32(hwnd, hInstance));
    }

    public static RawWindowHandle FromXlib(nuint window, nuint? visualId = null)
    {
        return new RawWindowHandle(new Xlib(window, visualId));
    }

    public static RawWindowHandle FromWayland(nint surface)
    {
        return new RawWindowHandle(new Wayland(surface));
    }

    public bool TryGetValue(out AndroidNdk value)
    {
        value = _androidNdk;
        return _tag == AndroidNdkTag;
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

    public bool TryGetValue(out Wayland value)
    {
        value = _wayland;
        return _tag == WaylandTag;
    }
}
