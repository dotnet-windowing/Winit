using Winit.Core;

namespace Winit.Wayland;

public readonly record struct WaylandDisplay(nint Handle);

public readonly record struct WaylandSurface(nint Handle);

public sealed record ApplicationName(string General, string Instance);

public interface IActiveEventLoopExtWayland
{
    bool IsWayland { get; }
}

public interface IEventLoopExtWayland
{
    bool IsWayland { get; }
}

public interface IEventLoopBuilderExtWayland
{
    EventLoopBuilder WithWayland();

    EventLoopBuilder WithAnyThread(bool anyThread);
}

public interface IWindowExtWayland
{
    AsyncRequestSerial RequestActivationToken();

    nint? XdgToplevel();

    WaylandSurface WaylandSurface();

    WaylandDisplay WaylandDisplay();
}

public sealed class WindowAttributesWayland : IPlatformWindowAttributes
{
    public ApplicationName? Name { get; set; }

    public ActivationToken? ActivationToken { get; set; }

    public bool PreferCsd { get; set; }

    public WindowAttributesWayland WithName(string general, string instance)
    {
        WindowAttributesWayland attributes = CloneWayland();
        attributes.Name = new ApplicationName(general, instance);
        return attributes;
    }

    public WindowAttributesWayland WithActivationToken(ActivationToken token)
    {
        WindowAttributesWayland attributes = CloneWayland();
        attributes.ActivationToken = token;
        return attributes;
    }

    public WindowAttributesWayland WithPreferCsd(bool preferCsd)
    {
        WindowAttributesWayland attributes = CloneWayland();
        attributes.PreferCsd = preferCsd;
        return attributes;
    }

    public WindowAttributesWayland CloneWayland()
    {
        return new WindowAttributesWayland
        {
            Name = Name,
            ActivationToken = ActivationToken,
            PreferCsd = PreferCsd,
        };
    }

    public IPlatformWindowAttributes Clone()
    {
        return CloneWayland();
    }
}

public sealed class PlatformSpecificEventLoopAttributes
{
    public bool AnyThread { get; set; }
}

internal static class WaylandUtil
{
    public static uint LogicalToPhysicalRounded(uint value, double scaleFactor)
    {
        return checked((uint)Math.Round(value * scaleFactor));
    }
}
