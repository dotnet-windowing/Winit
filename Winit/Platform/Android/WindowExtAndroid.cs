#if ANDROID
using Winit.Core;

namespace Winit.Platform.Android;

public static class WindowExtAndroid
{
    public static bool IsAndroid(this IWindow window)
    {
        return window.AsAny() is Winit.Android.IWindowExtAndroid;
    }

    public static global::Android.Graphics.Rect ContentRect(this IWindow window)
    {
        return AndroidWindow(window).ContentRect;
    }

    public static global::Android.Content.Res.Configuration? Configuration(this IWindow window)
    {
        return AndroidWindow(window).Configuration;
    }

    private static Winit.Android.IWindowExtAndroid AndroidWindow(IWindow window)
    {
        if (window.AsAny() is Winit.Android.IWindowExtAndroid androidWindow)
        {
            return androidWindow;
        }

        throw new PlatformNotSupportedException("Android window extensions require the Android backend.");
    }
}
#endif
