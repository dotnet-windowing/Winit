#if !WINDOWS
namespace Winit.Platform.X11;

public static class EventLoopBuilderExtX11
{
    public static EventLoopBuilder WithX11(this EventLoopBuilder builder)
    {
        X11Builder(builder).WithX11();
        return builder;
    }

    public static EventLoopBuilder WithAnyThread(this EventLoopBuilder builder, bool anyThread)
    {
        X11Builder(builder).WithAnyThread(anyThread);
        return builder;
    }

    private static Winit.X11.EventLoopBuilder X11Builder(EventLoopBuilder builder)
    {
        if (builder.Backend is Winit.X11.EventLoopBuilder x11Builder)
        {
            return x11Builder;
        }

        throw new PlatformNotSupportedException("X11 event loop builder is not available.");
    }
}
#endif
