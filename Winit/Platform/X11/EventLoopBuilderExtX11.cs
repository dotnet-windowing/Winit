#if !ANDROID
namespace Winit.Platform.X11;

public static class EventLoopBuilderExtX11
{
    public static EventLoopBuilder WithX11(this EventLoopBuilder builder)
    {
        builder.UseBackend(new Winit.X11.EventLoopBuilder().WithX11());
        return builder;
    }

    public static EventLoopBuilder WithAnyThread(this EventLoopBuilder builder, bool anyThread)
    {
        builder.WithAnyThread(anyThread);
        return builder;
    }
}
#endif
