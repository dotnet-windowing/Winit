using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.Views;
using Winit.Core;
using Winit.Dpi;
using Winit.Platform.Android;
using AndroidApp = Winit.Android.AndroidApp;
using CoreWindowId = Winit.Core.WindowId;

namespace AndroidWindow;

[Activity(
    Label = "Winit Android",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges =
        ConfigChanges.Density |
        ConfigChanges.Keyboard |
        ConfigChanges.KeyboardHidden |
        ConfigChanges.Orientation |
        ConfigChanges.ScreenLayout |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode)]
public sealed class MainActivity : WinitActivity
{
    protected override IApplicationHandler CreateApplicationHandler() => new TestApp();
}

internal sealed class TestApp : IApplicationHandler
{
    private IWindow? _window;
    private int _redraws;
    private string _lastEvent = "waiting";

    public void NewEvents(IActiveEventLoop eventLoop, StartCause cause)
    {
        if (cause.TryGetValue(out StartCause.Init _))
        {
            _lastEvent = "init";
        }
    }

    public void Resumed(IActiveEventLoop eventLoop)
    {
        _lastEvent = "resumed";
    }

    public void CanCreateSurfaces(IActiveEventLoop eventLoop)
    {
        _window ??= eventLoop.CreateWindow(WindowAttributes.Default.WithTitle("Winit Android"));
        _lastEvent = "can create surfaces";
        _window.RequestRedraw();
    }

    public void WindowEvent(IActiveEventLoop eventLoop, CoreWindowId windowId, WindowEvent windowEvent)
    {
        if (windowEvent.TryGetValue(out WindowEvent.SurfaceResized resized))
        {
            _lastEvent = $"resized {resized.Size.Width}x{resized.Size.Height}";
            _window?.RequestRedraw();
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.PointerButton pointerButton))
        {
            _lastEvent = $"touch {pointerButton.State} {pointerButton.Position.X:0},{pointerButton.Position.Y:0}";
            _window?.RequestRedraw();
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.PointerMoved pointerMoved))
        {
            _lastEvent = $"move {pointerMoved.Position.X:0},{pointerMoved.Position.Y:0}";
            _window?.RequestRedraw();
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.KeyboardInput keyboard))
        {
            _lastEvent = $"key {keyboard.Event.State} {keyboard.Event.PhysicalKey.ToKeyCode()}";
            _window?.RequestRedraw();
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.Focused focused))
        {
            _lastEvent = $"focused {focused.IsFocused}";
            _window?.RequestRedraw();
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.RedrawRequested _))
        {
            Draw(eventLoop);
        }
    }

    public void DestroySurfaces(IActiveEventLoop eventLoop)
    {
        _lastEvent = "destroy surfaces";
    }

    private void Draw(IActiveEventLoop eventLoop)
    {
        AndroidApp app = eventLoop.AndroidApp();
        ISurfaceHolder? holder = app.SurfaceView.Holder;
        Canvas? canvas = null;

        try
        {
            canvas = holder?.LockCanvas();
            if (canvas is null)
            {
                return;
            }

            _redraws++;
            int width = canvas.Width;
            int height = canvas.Height;
            float phase = (_redraws % 360) / 360.0f;

            using Paint background = new();
            background.SetShader(new LinearGradient(
                0,
                0,
                width,
                height,
                Color.HSVToColor(new[] { phase * 360.0f, 0.65f, 0.24f }),
                Color.HSVToColor(new[] { (phase * 360.0f + 95.0f) % 360.0f, 0.75f, 0.52f }),
                Shader.TileMode.Clamp!));
            canvas.DrawRect(0, 0, width, height, background);

            using Paint title = new() { AntiAlias = true, Color = Color.White, TextSize = 52.0f };
            using Paint body = new() { AntiAlias = true, Color = Color.Rgb(228, 236, 244), TextSize = 30.0f };

            canvas.DrawText("Winit Android", 44.0f, 90.0f, title);
            canvas.DrawText($"redraws: {_redraws}", 44.0f, 150.0f, body);
            canvas.DrawText($"event: {_lastEvent}", 44.0f, 196.0f, body);

            PhysicalSize<uint> surfaceSize = _window?.SurfaceSize ?? new PhysicalSize<uint>(0, 0);
            canvas.DrawText($"surface: {surfaceSize.Width}x{surfaceSize.Height}", 44.0f, 242.0f, body);
            canvas.DrawText($"scale: {_window?.ScaleFactor:0.##}", 44.0f, 288.0f, body);
            canvas.DrawText("Touch or press keys to redraw.", 44.0f, height - 58.0f, body);
        }
        finally
        {
            if (canvas is not null)
            {
                holder?.UnlockCanvasAndPost(canvas);
            }
        }
    }
}
