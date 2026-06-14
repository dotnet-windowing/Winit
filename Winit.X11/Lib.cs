using Winit.Core;
using Winit.Dpi;

namespace Winit.X11;

public enum WindowType
{
    Desktop,
    Dock,
    Toolbar,
    Menu,
    Utility,
    Splash,
    Dialog,
    DropdownMenu,
    PopupMenu,
    Tooltip,
    Notification,
    Combo,
    Dnd,
    Normal,
}

public readonly record struct XVisualId(uint Value);

public readonly record struct XWindow(uint Value);

public delegate bool XlibErrorHook(nint display, nint errorEvent);

internal sealed record ApplicationName(string General, string Instance);

public static class X11
{
    public static void RegisterXlibErrorHook(XlibErrorHook hook)
    {
        EventLoop.RegisterXlibErrorHook(hook);
    }
}

public interface IActiveEventLoopExtX11
{
    bool IsX11 { get; }
}

public interface IEventLoopExtX11
{
    bool IsX11 { get; }
}

public interface IEventLoopBuilderExtX11
{
    EventLoopBuilder WithX11();

    EventLoopBuilder WithAnyThread(bool anyThread);
}

public interface IWindowExtX11
{
    AsyncRequestSerial RequestActivationToken();
}

public sealed class WindowAttributesX11 : IPlatformWindowAttributes
{
    public string? GeneralName { get; set; }

    public string? InstanceName { get; set; }

    public ActivationToken? ActivationToken { get; set; }

    public XVisualId? VisualId { get; set; }

    public int? ScreenId { get; set; }

    public Size? BaseSize { get; set; }

    public bool OverrideRedirect { get; set; }

    public List<WindowType> WindowTypes { get; set; } = [WindowType.Normal];

    public XWindow? EmbedWindow { get; set; }

    public WindowAttributesX11 WithName(string general, string instance)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.GeneralName = general;
        attributes.InstanceName = instance;
        return attributes;
    }

    public WindowAttributesX11 WithActivationToken(ActivationToken token)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.ActivationToken = token;
        return attributes;
    }

    public WindowAttributesX11 WithX11Visual(XVisualId visualId)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.VisualId = visualId;
        return attributes;
    }

    public WindowAttributesX11 WithX11Screen(int screenId)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.ScreenId = screenId;
        return attributes;
    }

    public WindowAttributesX11 WithBaseSize(Size baseSize)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.BaseSize = baseSize;
        return attributes;
    }

    public WindowAttributesX11 WithOverrideRedirect(bool overrideRedirect)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.OverrideRedirect = overrideRedirect;
        return attributes;
    }

    public WindowAttributesX11 WithX11WindowTypes(IEnumerable<WindowType> windowTypes)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.WindowTypes = windowTypes.ToList();
        return attributes;
    }

    public WindowAttributesX11 WithEmbedParentWindow(XWindow parentWindow)
    {
        WindowAttributesX11 attributes = CloneX11();
        attributes.EmbedWindow = parentWindow;
        return attributes;
    }

    public WindowAttributesX11 CloneX11()
    {
        return new WindowAttributesX11
        {
            GeneralName = GeneralName,
            InstanceName = InstanceName,
            ActivationToken = ActivationToken,
            VisualId = VisualId,
            ScreenId = ScreenId,
            BaseSize = BaseSize,
            OverrideRedirect = OverrideRedirect,
            WindowTypes = [.. WindowTypes],
            EmbedWindow = EmbedWindow,
        };
    }

    public IPlatformWindowAttributes Clone()
    {
        return CloneX11();
    }
}

public sealed class PlatformSpecificEventLoopAttributes
{
    public bool AnyThread { get; set; }
}
