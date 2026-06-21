using RawWindowHandles;
using Winit.Core;
using Winit.Dpi;
using AndroidConfiguration = global::Android.Content.Res.Configuration;
using AndroidRect = global::Android.Graphics.Rect;
using AndroidViewStates = global::Android.Views.ViewStates;
using AndroidWindowManagerFlags = global::Android.Views.WindowManagerFlags;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.Android;

public sealed class Window : IWindow, IWindowExtAndroid
{
    private readonly EventLoop _eventLoop;
    private readonly AndroidApp _app;
    private readonly Lock _lock = new();
    private ImeCapabilities? _imeCapabilities;
    private string _title;
    private bool _visible;
    private bool _contentProtected;

    internal Window(EventLoop eventLoop, WindowAttributes attributes)
    {
        _eventLoop = eventLoop;
        _app = eventLoop.AndroidApp;
        _title = attributes.Title;
        _visible = attributes.Visible;
        Id = EventLoop.GlobalWindowId;
        SetTitle(attributes.Title);
        SetVisible(attributes.Visible);
        SetContentProtected(attributes.ContentProtected);
    }

    public WindowId Id { get; }

    public double ScaleFactor => _app.ScaleFactor;

    public PhysicalPosition<int> SurfacePosition => new(0, 0);

    public PhysicalPosition<int> OuterPosition => new(0, 0);

    public PhysicalSize<uint> SurfaceSize => _app.SurfaceSize;

    public PhysicalSize<uint> OuterSize => SurfaceSize;

    public PhysicalInsets<uint> SafeArea => new(0, 0, 0, 0);

    public PhysicalSize<uint>? SurfaceResizeIncrements => null;

    public bool? IsVisible
    {
        get
        {
            lock (_lock)
            {
                return _visible;
            }
        }
    }

    public bool IsResizable => false;

    public WindowButtons EnabledButtons => WindowButtons.All;

    public bool? IsMinimized => null;

    public bool IsMaximized => false;

    public Fullscreen? Fullscreen => null;

    public bool IsDecorated => true;

    public ImeCapabilities? ImeCapabilities
    {
        get
        {
            lock (_lock)
            {
                return _imeCapabilities;
            }
        }
    }

    public bool HasFocus => _eventLoop.HasFocus;

    public Theme? Theme => null;

    public string Title
    {
        get
        {
            lock (_lock)
            {
                return _title;
            }
        }
    }

    public CoreMonitorHandle? CurrentMonitor => null;

    public IEnumerable<CoreMonitorHandle> AvailableMonitors => [];

    public CoreMonitorHandle? PrimaryMonitor => null;

    public RawDisplayHandle? DisplayHandle => _app.DisplayHandle;

    public RawWindowHandle? WindowHandle
    {
        get
        {
            nint nativeWindow = _app.NativeWindow;
            return nativeWindow != 0 ? RawWindowHandle.FromAndroidNdk(nativeWindow) : null;
        }
    }

    public AndroidRect ContentRect => _app.ContentRect;

    public AndroidConfiguration? Configuration => _app.Activity.Resources?.Configuration;

    public void RequestRedraw()
    {
        _eventLoop.RequestRedraw();
    }

    public void PrePresentNotify()
    {
    }

    public void ResetDeadKeys()
    {
    }

    public PhysicalSize<uint>? RequestSurfaceSize(Size size)
    {
        return SurfaceSize;
    }

    public void SetOuterPosition(Position position)
    {
    }

    public void SetMinSurfaceSize(Size? minSize)
    {
    }

    public void SetMaxSurfaceSize(Size? maxSize)
    {
    }

    public void SetSurfaceResizeIncrements(Size? increments)
    {
    }

    public void SetTitle(string title)
    {
        lock (_lock)
        {
            _title = title;
        }

        _app.Activity.RunOnUiThread(() => _app.Activity.Title = title);
    }

    public void SetTransparent(bool transparent)
    {
    }

    public void SetBlur(bool blur)
    {
    }

    public void SetVisible(bool visible)
    {
        lock (_lock)
        {
            _visible = visible;
        }

        _app.Activity.RunOnUiThread(() =>
        {
            _app.SurfaceView.Visibility = visible ? AndroidViewStates.Visible : AndroidViewStates.Invisible;
        });
    }

    public void SetResizable(bool resizable)
    {
    }

    public void SetEnabledButtons(WindowButtons buttons)
    {
    }

    public void SetMinimized(bool minimized)
    {
    }

    public void SetMaximized(bool maximized)
    {
    }

    public void SetFullscreen(Fullscreen? fullscreen)
    {
    }

    public void SetDecorations(bool decorations)
    {
    }

    public void SetWindowLevel(WindowLevel level)
    {
    }

    public void SetWindowIcon(Icon? windowIcon)
    {
    }

    public void RequestImeUpdate(ImeRequest request)
    {
        lock (_lock)
        {
            if (request.TryGetValue(out ImeRequest.Enable enable))
            {
                if (_imeCapabilities is not null)
                {
                    throw new ImeRequestException(ImeRequestError.AlreadyEnabled);
                }

                _imeCapabilities = enable.Value.Capabilities;
                _app.ShowSoftInput(showImplicit: true);
                return;
            }

            if (request.TryGetValue(out ImeRequest.Update _))
            {
                if (_imeCapabilities is null)
                {
                    throw new ImeRequestException(ImeRequestError.NotEnabled);
                }

                return;
            }

            if (request.TryGetValue(out ImeRequest.Disable _))
            {
                _imeCapabilities = null;
                _app.HideSoftInput(implicitOnly: true);
            }
        }
    }

    public void FocusWindow()
    {
        _app.Activity.RunOnUiThread(() => _app.SurfaceView.RequestFocus());
    }

    public void RequestUserAttention(UserAttentionType? requestType)
    {
    }

    public void SetTheme(Theme? theme)
    {
    }

    public void SetContentProtected(bool isProtected)
    {
        lock (_lock)
        {
            _contentProtected = isProtected;
        }

        _app.Activity.RunOnUiThread(() =>
        {
            if (isProtected)
            {
                _app.Activity.Window?.SetFlags(AndroidWindowManagerFlags.Secure, AndroidWindowManagerFlags.Secure);
            }
            else
            {
                _app.Activity.Window?.ClearFlags(AndroidWindowManagerFlags.Secure);
            }
        });
    }

    public void SetCursor(Cursor cursor)
    {
    }

    public void SetCursorPosition(Position position)
    {
        throw new NotSupportedRequestException("set cursor position is not supported by the Android backend");
    }

    public void SetCursorGrab(CursorGrabMode mode)
    {
        if (mode != CursorGrabMode.None)
        {
            throw new NotSupportedRequestException("cursor grab is not supported by the Android backend");
        }
    }

    public void SetCursorVisible(bool visible)
    {
    }

    public void DragWindow()
    {
        throw new NotSupportedRequestException("drag window is not supported by the Android backend");
    }

    public void DragResizeWindow(ResizeDirection direction)
    {
        throw new NotSupportedRequestException("drag resize is not supported by the Android backend");
    }

    public void ShowWindowMenu(Position position)
    {
    }

    public void SetCursorHittest(bool hittest)
    {
        if (!hittest)
        {
            throw new NotSupportedRequestException("cursor hittest is not supported by the Android backend");
        }
    }
}

public sealed class WindowAttributesAndroid : IPlatformWindowAttributes
{
    public IPlatformWindowAttributes Clone()
    {
        return CloneAndroid();
    }

    public WindowAttributesAndroid CloneAndroid()
    {
        return new WindowAttributesAndroid();
    }
}

public interface IWindowExtAndroid
{
    AndroidRect ContentRect { get; }

    AndroidConfiguration? Configuration { get; }
}
