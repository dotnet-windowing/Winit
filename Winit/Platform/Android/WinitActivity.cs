#if ANDROID
using Winit.Core;
using AndroidActivity = global::Android.App.Activity;
using AndroidBundle = global::Android.OS.Bundle;
using AndroidApp = Winit.Android.AndroidApp;

namespace Winit.Platform.Android;

public abstract class WinitActivity : AndroidActivity
{
    protected AndroidApp? AndroidApp { get; private set; }

    protected EventLoop? EventLoop { get; private set; }

    protected abstract IApplicationHandler CreateApplicationHandler();

    protected virtual void ConfigureEventLoopBuilder(EventLoopBuilder builder)
    {
    }

    protected override void OnCreate(AndroidBundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        AndroidApp androidApp = new(this);
        EventLoopBuilder builder = EventLoop.Builder().WithAndroidApp(androidApp);
        ConfigureEventLoopBuilder(builder);

        EventLoop eventLoop = builder.Build();
        AndroidApp = androidApp;
        EventLoop = eventLoop;

        androidApp.OnCreate(savedInstanceState);
        eventLoop.RegisterApp(CreateApplicationHandler());
    }

    protected override void OnStart()
    {
        base.OnStart();
        AndroidApp?.OnStart();
    }

    protected override void OnResume()
    {
        base.OnResume();
        AndroidApp?.OnResume();
    }

    protected override void OnPause()
    {
        AndroidApp?.OnPause();
        base.OnPause();
    }

    protected override void OnStop()
    {
        AndroidApp?.OnStop();
        base.OnStop();
    }

    protected override void OnDestroy()
    {
        AndroidApp?.OnDestroy();
        base.OnDestroy();
    }

    public override void OnLowMemory()
    {
        base.OnLowMemory();
        AndroidApp?.OnLowMemory();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        AndroidApp?.OnWindowFocusChanged(hasFocus);
    }
}
#endif
