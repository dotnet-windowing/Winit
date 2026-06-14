#if WINDOWS
namespace Winit.Platform.Windows;

public static class EventLoopBuilderExtWindows
{
    public static EventLoopBuilder WithAnyThread(this EventLoopBuilder builder, bool anyThread)
    {
        WindowsBuilder(builder).WithAnyThread(anyThread);
        return builder;
    }

    public static EventLoopBuilder WithDpiAware(this EventLoopBuilder builder, bool dpiAware)
    {
        WindowsBuilder(builder).WithDpiAware(dpiAware);
        return builder;
    }

    public static EventLoopBuilder WithMsgHook(this EventLoopBuilder builder, Func<nint, bool>? callback)
    {
        WindowsBuilder(builder).WithMsgHook(callback);
        return builder;
    }

    private static Winit.Win32.EventLoopBuilder WindowsBuilder(EventLoopBuilder builder)
    {
        if (builder.Backend is Winit.Win32.EventLoopBuilder windowsBuilder)
        {
            return windowsBuilder;
        }

        throw new PlatformNotSupportedException("Windows event loop builder is not available.");
    }
}
#endif
