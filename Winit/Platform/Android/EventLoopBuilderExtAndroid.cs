#if ANDROID
namespace Winit.Platform.Android;

public static class EventLoopBuilderExtAndroid
{
    public static EventLoopBuilder WithAndroidApp(this EventLoopBuilder builder, Winit.Android.AndroidApp app)
    {
        AndroidBuilder(builder).WithAndroidApp(app);
        return builder;
    }

    public static EventLoopBuilder HandleVolumeKeys(this EventLoopBuilder builder)
    {
        AndroidBuilder(builder).HandleVolumeKeys();
        return builder;
    }

    private static Winit.Android.EventLoopBuilder AndroidBuilder(EventLoopBuilder builder)
    {
        if (builder.Backend is Winit.Android.EventLoopBuilder androidBuilder)
        {
            return androidBuilder;
        }

        throw new PlatformNotSupportedException("Android event loop builder is not available.");
    }
}
#endif
