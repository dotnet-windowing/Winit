using Winit.Core;

namespace Winit;

public sealed class EventLoopBuilder
{
    internal EventLoopBuilder(IPlatformEventLoopBuilder backend)
    {
        Backend = backend;
    }

    internal IPlatformEventLoopBuilder Backend { get; private set; }

    internal bool AnyThread { get; private set; }

    internal void UseBackend(IPlatformEventLoopBuilder backend)
    {
        Backend = backend;
        ApplySharedAttributes();
    }

    internal void WithAnyThread(bool anyThread)
    {
        AnyThread = anyThread;
        ApplySharedAttributes();
    }

    public EventLoop Build()
    {
        ApplySharedAttributes();
        return new EventLoop(Backend.Build());
    }

    private void ApplySharedAttributes()
    {
#if ANDROID
        // Android does not expose the cross-platform AnyThread setting.
#elif WINDOWS
        if (Backend is Winit.Win32.EventLoopBuilder windowsBuilder)
        {
            windowsBuilder.WithAnyThread(AnyThread);
        }
#else
        if (Backend is Winit.Wayland.EventLoopBuilder waylandBuilder)
        {
            waylandBuilder.WithAnyThread(AnyThread);
        }
        else if (Backend is Winit.X11.EventLoopBuilder x11Builder)
        {
            x11Builder.WithAnyThread(AnyThread);
        }
#endif
    }
}
