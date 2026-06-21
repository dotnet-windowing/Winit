#if ANDROID
using Winit.Core;

namespace Winit.Platform.Android;

public static class EventLoopExtAndroid
{
    public static Winit.Android.AndroidApp AndroidApp(this EventLoop eventLoop)
    {
        return AndroidEventLoop(eventLoop.Backend).AndroidApp;
    }

    public static Winit.Android.AndroidApp AndroidApp(this IActiveEventLoop eventLoop)
    {
        if (eventLoop.AsAny() is Winit.Android.IActiveEventLoopExtAndroid eventLoopExt)
        {
            return eventLoopExt.AndroidApp;
        }

        throw new PlatformNotSupportedException("Android event loop extensions require the Android backend.");
    }

    private static Winit.Android.IEventLoopExtAndroid AndroidEventLoop(IPlatformEventLoop eventLoop)
    {
        if (eventLoop.AsAny() is Winit.Android.IEventLoopExtAndroid androidEventLoop)
        {
            return androidEventLoop;
        }

        throw new PlatformNotSupportedException("Android event loop extensions require the Android backend.");
    }
}
#endif
