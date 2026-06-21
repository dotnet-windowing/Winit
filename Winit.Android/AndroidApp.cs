using RawWindowHandles;
using Winit.Dpi;
using AndroidActivity = global::Android.App.Activity;
using AndroidBundle = global::Android.OS.Bundle;
using AndroidContext = global::Android.Content.Context;
using AndroidKeycode = global::Android.Views.Keycode;
using AndroidKeyEvent = global::Android.Views.KeyEvent;
using AndroidMotionEvent = global::Android.Views.MotionEvent;
using AndroidRect = global::Android.Graphics.Rect;
using AndroidSurface = global::Android.Views.Surface;
using AndroidSurfaceView = global::Android.Views.SurfaceView;
using AndroidViewStates = global::Android.Views.ViewStates;
using ISurfaceHolder = global::Android.Views.ISurfaceHolder;
using ISurfaceHolderCallback = global::Android.Views.ISurfaceHolderCallback;
using JNIEnv = global::Android.Runtime.JNIEnv;

namespace Winit.Android;

public sealed class AndroidApp : IDisposable
{
    private readonly Lock _lock = new();
    private readonly OwnedDisplayHandle _ownedDisplayHandle = new(RawDisplayHandle.FromAndroid());
    private readonly global::Android.OS.Handler _mainHandler =
        new(global::Android.OS.Looper.MainLooper ?? throw new InvalidOperationException("Android main looper is unavailable."));
    private WinitSurfaceView? _surfaceView;
    private EventLoop? _eventLoop;
    private nint _nativeWindow;
    private PhysicalSize<uint> _surfaceSize;
    private bool _disposed;

    public AndroidApp(AndroidActivity activity)
    {
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    public AndroidActivity Activity { get; }

    public WinitSurfaceView SurfaceView => EnsureSurfaceView();

    public OwnedDisplayHandle OwnedDisplayHandle => _ownedDisplayHandle;

    public RawDisplayHandle? DisplayHandle => RawDisplayHandle.FromAndroid();

    public nint NativeWindow
    {
        get
        {
            lock (_lock)
            {
                return _nativeWindow;
            }
        }
    }

    public PhysicalSize<uint> SurfaceSize
    {
        get
        {
            lock (_lock)
            {
                return _surfaceSize;
            }
        }
    }

    public double ScaleFactor => Activity.Resources?.DisplayMetrics?.Density ?? 1.0;

    public AndroidRect ContentRect
    {
        get
        {
            AndroidRect rect = new();
            SurfaceView.GetWindowVisibleDisplayFrame(rect);
            return rect;
        }
    }

    public void Attach(EventLoop eventLoop)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_eventLoop is not null && !ReferenceEquals(_eventLoop, eventLoop))
            {
                throw new InvalidOperationException("AndroidApp is already attached to an EventLoop.");
            }

            _eventLoop = eventLoop;
        }

        PostToUiThread(InstallContentView);
    }

    public void OnCreate(AndroidBundle? _)
    {
    }

    public void OnStart()
    {
        EventLoop?.HandleStart();
    }

    public void OnResume()
    {
        EventLoop?.HandleResume();
    }

    public void OnPause()
    {
        EventLoop?.HandlePause();
    }

    public void OnStop()
    {
        EventLoop?.HandleStop();
    }

    public void OnDestroy()
    {
        EventLoop?.Exit();
        Dispose();
    }

    public void OnLowMemory()
    {
        EventLoop?.HandleLowMemory();
    }

    public void OnWindowFocusChanged(bool hasFocus)
    {
        EventLoop?.HandleFocusChanged(hasFocus);
    }

    public void WakeUp()
    {
        PostToUiThread(() => EventLoop?.HandleProxyWakeUp());
    }

    public void ShowSoftInput(bool showImplicit)
    {
        RunOnUiThread(() =>
        {
            SurfaceView.RequestFocus();
            global::Android.Views.InputMethods.InputMethodManager? inputMethodManager =
                Activity.GetSystemService(AndroidContext.InputMethodService)
                    as global::Android.Views.InputMethods.InputMethodManager;
            inputMethodManager?.ShowSoftInput(
                SurfaceView,
                showImplicit
                    ? global::Android.Views.InputMethods.ShowFlags.Implicit
                    : global::Android.Views.InputMethods.ShowFlags.Forced);
        });
    }

    public void HideSoftInput(bool implicitOnly)
    {
        RunOnUiThread(() =>
        {
            global::Android.Views.InputMethods.InputMethodManager? inputMethodManager =
                Activity.GetSystemService(AndroidContext.InputMethodService)
                    as global::Android.Views.InputMethods.InputMethodManager;
            inputMethodManager?.HideSoftInputFromWindow(
                SurfaceView.WindowToken,
                implicitOnly
                    ? global::Android.Views.InputMethods.HideSoftInputFlags.ImplicitOnly
                    : global::Android.Views.InputMethods.HideSoftInputFlags.None);
        });
    }

    public void Dispose()
    {
        nint nativeWindow;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            nativeWindow = _nativeWindow;
            _nativeWindow = 0;
            _surfaceSize = default;
            _eventLoop = null;
        }

        if (nativeWindow != 0)
        {
            Ffi.ANativeWindow_release(nativeWindow);
        }
    }

    internal void SurfaceCreated(ISurfaceHolder holder)
    {
        nint nativeWindow = CreateNativeWindow(holder.Surface);
        PhysicalSize<uint> size = NativeWindowSize(nativeWindow);
        nint oldWindow;

        lock (_lock)
        {
            oldWindow = _nativeWindow;
            _nativeWindow = nativeWindow;
            _surfaceSize = size;
        }

        if (oldWindow != 0)
        {
            Ffi.ANativeWindow_release(oldWindow);
        }

        EventLoop?.HandleSurfaceCreated(size);
    }

    internal void SurfaceChanged(int width, int height)
    {
        PhysicalSize<uint> size = new(checked((uint)Math.Max(width, 0)), checked((uint)Math.Max(height, 0)));

        lock (_lock)
        {
            _surfaceSize = size;
        }

        EventLoop?.HandleSurfaceResized(size);
    }

    internal void SurfaceDestroyed()
    {
        EventLoop?.HandleSurfaceDestroyed();

        nint nativeWindow;
        lock (_lock)
        {
            nativeWindow = _nativeWindow;
            _nativeWindow = 0;
            _surfaceSize = default;
        }

        if (nativeWindow != 0)
        {
            Ffi.ANativeWindow_release(nativeWindow);
        }
    }

    internal bool HandleMotionEvent(AndroidMotionEvent e)
    {
        return EventLoop?.HandleMotionEvent(e) == true;
    }

    internal bool HandleKeyEvent(AndroidKeycode keyCode, AndroidKeyEvent? e, Winit.Core.ElementState state)
    {
        return EventLoop?.HandleKeyEvent(keyCode, e, state) == true;
    }

    internal void PostToUiThread(Action action)
    {
        _mainHandler.Post(action);
    }

    private EventLoop? EventLoop
    {
        get
        {
            lock (_lock)
            {
                return _eventLoop;
            }
        }
    }

    private void InstallContentView()
    {
        WinitSurfaceView surfaceView = EnsureSurfaceView();
        if (surfaceView.Parent is null)
        {
            Activity.SetContentView(surfaceView);
        }
    }

    private WinitSurfaceView EnsureSurfaceView()
    {
        lock (_lock)
        {
            _surfaceView ??= new WinitSurfaceView(Activity, this);
            return _surfaceView;
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (Activity.MainLooper == global::Android.OS.Looper.MyLooper())
        {
            action();
            return;
        }

        Activity.RunOnUiThread(action);
    }

    private static nint CreateNativeWindow(AndroidSurface? surface)
    {
        if (surface is null || !surface.IsValid)
        {
            return 0;
        }

        return Ffi.ANativeWindow_fromSurface(JNIEnv.Handle, surface.Handle);
    }

    private static PhysicalSize<uint> NativeWindowSize(nint nativeWindow)
    {
        if (nativeWindow == 0)
        {
            return new PhysicalSize<uint>(0, 0);
        }

        return new PhysicalSize<uint>(
            checked((uint)Math.Max(Ffi.ANativeWindow_getWidth(nativeWindow), 0)),
            checked((uint)Math.Max(Ffi.ANativeWindow_getHeight(nativeWindow), 0)));
    }
}

public sealed class WinitSurfaceView : AndroidSurfaceView, ISurfaceHolderCallback
{
    private readonly AndroidApp _app;

    internal WinitSurfaceView(AndroidContext context, AndroidApp app)
        : base(context)
    {
        _app = app;
        Focusable = true;
        FocusableInTouchMode = true;
        Holder?.AddCallback(this);
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        _app.SurfaceCreated(holder);
    }

    public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int width, int height)
    {
        _app.SurfaceChanged(width, height);
    }

    public void SurfaceDestroyed(ISurfaceHolder holder)
    {
        _app.SurfaceDestroyed();
    }

    public override bool OnTouchEvent(AndroidMotionEvent? e)
    {
        return e is not null && _app.HandleMotionEvent(e) || base.OnTouchEvent(e);
    }

    public override bool OnKeyDown(AndroidKeycode keyCode, AndroidKeyEvent? e)
    {
        return _app.HandleKeyEvent(keyCode, e, Winit.Core.ElementState.Pressed) || base.OnKeyDown(keyCode, e);
    }

    public override bool OnKeyUp(AndroidKeycode keyCode, AndroidKeyEvent? e)
    {
        return _app.HandleKeyEvent(keyCode, e, Winit.Core.ElementState.Released) || base.OnKeyUp(keyCode, e);
    }

    protected override void OnWindowVisibilityChanged(AndroidViewStates visibility)
    {
        base.OnWindowVisibilityChanged(visibility);
        _app.OnWindowFocusChanged(HasWindowFocus);
    }
}
