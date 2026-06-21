#if !WINDOWS && !ANDROID
using Winit.Core;

namespace Winit.Platform.Wayland;

public static class EventLoopExtWayland
{
    public static bool IsWayland(this EventLoop eventLoop)
    {
        return eventLoop.Backend is Winit.Wayland.IEventLoopExtWayland;
    }

    public static bool IsWayland(this IActiveEventLoop eventLoop)
    {
        return eventLoop.AsAny() is Winit.Wayland.IActiveEventLoopExtWayland;
    }
}
#endif
