#if !ANDROID
namespace Winit.Platform.Wayland;

public static class EventLoopBuilderExtWayland
{
    public static EventLoopBuilder WithWayland(this EventLoopBuilder builder)
    {
        builder.UseBackend(new Winit.Wayland.EventLoopBuilder().WithWayland());
        return builder;
    }

    public static EventLoopBuilder WithAnyThread(this EventLoopBuilder builder, bool anyThread)
    {
        builder.WithAnyThread(anyThread);
        return builder;
    }
}
#endif
