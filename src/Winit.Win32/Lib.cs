using Winit.Core;

namespace Winit.Win32;

public enum BackdropType
{
    Auto = 0,
    None = 1,
    MainWindow = 2,
    TransientWindow = 3,
    TabbedWindow = 4,
}

public readonly record struct Color(uint Value)
{
    public static Color None => new(0xfffffffe);

    public static Color SystemDefault => new(0xffffffff);

    public static Color FromRgb(byte red, byte green, byte blue)
    {
        return new Color((uint)(red | (green << 8) | (blue << 16)));
    }
}

public enum CornerPreference
{
    Default = 0,
    DoNotRound = 1,
    Round = 2,
    RoundSmall = 3,
}

public sealed class WindowAttributesWindows : IPlatformWindowAttributes
{
    public nint? Owner { get; set; }

    public nint? Menu { get; set; }

    public Icon? TaskbarIcon { get; set; }

    public bool NoRedirectionBitmap { get; set; }

    public bool DragAndDrop { get; set; } = true;

    public bool SkipTaskbar { get; set; }

    public string ClassName { get; set; } = "Window Class";

    public bool DecorationShadow { get; set; }

    public BackdropType BackdropType { get; set; } = BackdropType.Auto;

    public bool ClipChildren { get; set; } = true;

    public Color? BorderColor { get; set; }

    public Color? TitleBackgroundColor { get; set; }

    public Color? TitleTextColor { get; set; }

    public CornerPreference? CornerPreference { get; set; }

    public bool UseSystemWheelSpeed { get; set; } = true;

    public WindowAttributesWindows WithOwnerWindow(nint owner)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.Owner = owner;
        return attributes;
    }

    public WindowAttributesWindows WithMenu(nint menu)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.Menu = menu;
        return attributes;
    }

    public WindowAttributesWindows WithTaskbarIcon(Icon? taskbarIcon)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.TaskbarIcon = taskbarIcon;
        return attributes;
    }

    public WindowAttributesWindows WithNoRedirectionBitmap(bool flag)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.NoRedirectionBitmap = flag;
        return attributes;
    }

    public WindowAttributesWindows WithDragAndDrop(bool flag)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.DragAndDrop = flag;
        return attributes;
    }

    public WindowAttributesWindows WithSkipTaskbar(bool skip)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.SkipTaskbar = skip;
        return attributes;
    }

    public WindowAttributesWindows WithClassName(string className)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.ClassName = className;
        return attributes;
    }

    public WindowAttributesWindows WithUndecoratedShadow(bool shadow)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.DecorationShadow = shadow;
        return attributes;
    }

    public WindowAttributesWindows WithSystemBackdrop(BackdropType backdropType)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.BackdropType = backdropType;
        return attributes;
    }

    public WindowAttributesWindows WithClipChildren(bool flag)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.ClipChildren = flag;
        return attributes;
    }

    public WindowAttributesWindows WithBorderColor(Color? color)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.BorderColor = color ?? Color.None;
        return attributes;
    }

    public WindowAttributesWindows WithTitleBackgroundColor(Color? color)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.TitleBackgroundColor = color ?? Color.None;
        return attributes;
    }

    public WindowAttributesWindows WithTitleTextColor(Color color)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.TitleTextColor = color;
        return attributes;
    }

    public WindowAttributesWindows WithCornerPreference(CornerPreference preference)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.CornerPreference = preference;
        return attributes;
    }

    public WindowAttributesWindows WithUseSystemScrollSpeed(bool shouldUse)
    {
        WindowAttributesWindows attributes = CloneWindows();
        attributes.UseSystemWheelSpeed = shouldUse;
        return attributes;
    }

    public WindowAttributesWindows CloneWindows()
    {
        return new WindowAttributesWindows
        {
            Owner = Owner,
            Menu = Menu,
            TaskbarIcon = TaskbarIcon,
            NoRedirectionBitmap = NoRedirectionBitmap,
            DragAndDrop = DragAndDrop,
            SkipTaskbar = SkipTaskbar,
            ClassName = ClassName,
            DecorationShadow = DecorationShadow,
            BackdropType = BackdropType,
            ClipChildren = ClipChildren,
            BorderColor = BorderColor,
            TitleBackgroundColor = TitleBackgroundColor,
            TitleTextColor = TitleTextColor,
            CornerPreference = CornerPreference,
            UseSystemWheelSpeed = UseSystemWheelSpeed,
        };
    }

    public IPlatformWindowAttributes Clone()
    {
        return CloneWindows();
    }
}

public sealed class PlatformSpecificEventLoopAttributes
{
    public bool AnyThread { get; set; }

    public bool DpiAware { get; set; } = true;

    public Func<nint, bool>? MsgHook { get; set; }
}

public sealed class EventLoopBuilder : IPlatformEventLoopBuilder
{
    public PlatformSpecificEventLoopAttributes PlatformSpecific { get; } = new();

    public EventLoopBuilder WithAnyThread(bool anyThread)
    {
        PlatformSpecific.AnyThread = anyThread;
        return this;
    }

    public EventLoopBuilder WithDpiAware(bool dpiAware)
    {
        PlatformSpecific.DpiAware = dpiAware;
        return this;
    }

    public EventLoopBuilder WithMsgHook(Func<nint, bool>? callback)
    {
        PlatformSpecific.MsgHook = callback;
        return this;
    }

    public IPlatformEventLoop Build()
    {
        return new EventLoop(PlatformSpecific);
    }
}
