using System.Collections.Concurrent;
using Winit;
using Winit.Core;
using Winit.Dpi;
#if WINDOWS
using Winit.Platform.Windows;
using WinBackdropType = Winit.Win32.BackdropType;
using WinColor = Winit.Win32.Color;
using WinCornerPreference = Winit.Win32.CornerPreference;
#endif

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        EventLoopBuilder builder = EventLoop.Builder();
#if WINDOWS
        builder.WithDpiAware(true);
#endif
        EventLoop eventLoop = builder.Build();

        eventLoop.ListenDeviceEvents(DeviceEvents.Always);
        eventLoop.RunApp(new WindowExample(eventLoop.CreateProxy()));
    }
}

internal sealed class WindowExample : IApplicationHandler
{
    private const string BaseTitle = "Winit C# Window Example";
    private static readonly TimeSpan s_consoleShortcutSuppressDuration = TimeSpan.FromMilliseconds(750);

    private readonly EventLoopProxy _proxy;
    private readonly ConcurrentQueue<ConsoleKeyInfo> _consoleKeys = new();
    private readonly Icon _primaryIcon = CreateIcon(40, 130, 220);
    private readonly Icon _alternateIcon = CreateIcon(220, 90, 70);

    private readonly CursorIcon[] _cursorIcons =
    [
        CursorIcon.Default,
        CursorIcon.Pointer,
        CursorIcon.Crosshair,
        CursorIcon.Text,
        CursorIcon.Wait,
        CursorIcon.Grab,
        CursorIcon.Grabbing,
        CursorIcon.Move,
        CursorIcon.NotAllowed,
        CursorIcon.EwResize,
        CursorIcon.NsResize,
        CursorIcon.ZoomIn,
    ];

    private readonly CursorGrabMode[] _cursorGrabModes =
    [
        CursorGrabMode.None,
        CursorGrabMode.Confined,
        CursorGrabMode.Locked,
    ];

    private readonly Theme?[] _themePreferences =
    [
        Theme.Dark,
        Theme.Light,
        null,
    ];

    private readonly WindowLevel[] _windowLevels =
    [
        WindowLevel.Normal,
        WindowLevel.AlwaysOnTop,
        WindowLevel.AlwaysOnBottom,
    ];

#if WINDOWS
    private readonly WinBackdropType[] _backdrops =
    [
        WinBackdropType.Auto,
        WinBackdropType.None,
        WinBackdropType.MainWindow,
        WinBackdropType.TransientWindow,
        WinBackdropType.TabbedWindow,
    ];

    private readonly WinCornerPreference[] _corners =
    [
        WinCornerPreference.Default,
        WinCornerPreference.DoNotRound,
        WinCornerPreference.Round,
        WinCornerPreference.RoundSmall,
    ];

    private readonly (WinColor? Border, WinColor? Caption, WinColor Text, string Name)[] _colorSchemes =
    [
        (
            WinColor.FromRgb(40, 130, 220),
            WinColor.FromRgb(18, 48, 84),
            WinColor.FromRgb(245, 248, 255),
            "blue"
        ),
        (
            WinColor.FromRgb(36, 142, 115),
            WinColor.FromRgb(20, 72, 62),
            WinColor.FromRgb(246, 255, 250),
            "green"
        ),
        (
            WinColor.FromRgb(190, 84, 42),
            WinColor.FromRgb(96, 46, 31),
            WinColor.FromRgb(255, 248, 242),
            "copper"
        ),
        (null, null, WinColor.SystemDefault, "system")
    ];
#endif

    private readonly (LogicalSize<double> Size, string Name)[] _sizes =
    [
        (new LogicalSize<double>(720.0, 420.0), "720x420"),
        (new LogicalSize<double>(900.0, 620.0), "900x620"),
        (new LogicalSize<double>(1180.0, 720.0), "1180x720"),
        (new LogicalSize<double>(1440.0, 900.0), "1440x900"),
    ];

    private IWindow? _window;
    private CustomCursor? _customCursor;
    private PhysicalPosition<double> _lastPointerPosition;
    private bool _stopping;
    private bool _resizable = true;
    private bool _decorated = true;
    private bool _visible = true;
    private bool _transparent;
    private bool _blur;
    private bool _fullscreen;
    private bool _maximized;
    private bool _minimized;
    private bool _cursorVisible = true;
    private bool _usingCustomCursor;
#if WINDOWS
    private bool _skipTaskbar;
    private bool _shadow = true;
    private bool _systemWheelSpeed = true;
#endif
    private bool _contentProtected;
    private bool _imeAllowed;
#if WINDOWS
    private bool _enabled = true;
#endif
    private bool _constraintsEnabled = true;
    private bool _usingAlternateIcon;
    private bool _cursorHittest = true;
    private bool _attentionCritical;
    private DeviceEvents _deviceEvents = DeviceEvents.Always;
    private int _cursorIconIndex;
    private int _cursorGrabIndex;
    private int _themeIndex;
    private int _levelIndex;
#if WINDOWS
    private int _backdropIndex;
    private int _cornerIndex = 2;
    private int _colorSchemeIndex;
#endif
    private int _sizeIndex = 1;
    private int _redrawCount;
    private int _pointerMoveCount;
    private int _deviceMotionCount;
    private KeyCode? _suppressedWindowShortcut;
    private long _suppressWindowShortcutUntilTicks;

    public WindowExample(EventLoopProxy proxy)
    {
        _proxy = proxy;
        PrintHelp();

        Thread consoleThread = new(ReadConsoleKeys)
        {
            IsBackground = true,
            Name = "Winit window example console input",
        };
        consoleThread.Start();
    }

    public void NewEvents(IActiveEventLoop eventLoop, StartCause cause)
    {
        if (cause.TryGetValue(out StartCause.Init _))
        {
            Log("new events: init");
        }
    }

    public void Resumed(IActiveEventLoop eventLoop)
    {
        Log("resumed");
    }

    public void CanCreateSurfaces(IActiveEventLoop eventLoop)
    {
        if (_window is not null)
        {
            return;
        }

        _customCursor = eventLoop.CreateCustomCursor(CustomCursorSource.FromRgba(
            CreateCursorRgba(),
            width: 32,
            height: 32,
            hotspotX: 1,
            hotspotY: 1));

        WindowAttributes attributes = new WindowAttributes
        {
            Title = BaseTitle,
            SurfaceSize = _sizes[_sizeIndex].Size,
            MinSurfaceSize = new LogicalSize<double>(360.0, 260.0),
            MaxSurfaceSize = new LogicalSize<double>(1600.0, 1000.0),
            SurfaceResizeIncrements = new LogicalSize<double>(20.0, 20.0),
            Position = new LogicalPosition<double>(120.0, 120.0),
            Resizable = _resizable,
            EnabledButtons = WindowButtons.All,
            Visible = _visible,
            Transparent = _transparent,
            Blur = _blur,
            Decorations = _decorated,
            WindowIcon = _primaryIcon,
            PreferredTheme = _themePreferences[_themeIndex],
            ContentProtected = _contentProtected,
            WindowLevel = _windowLevels[_levelIndex],
            Cursor = Cursor.From(_cursorIcons[_cursorIconIndex]),
            Active = true,
        };

#if WINDOWS
        attributes = attributes
            .WithTaskbarIcon(_primaryIcon)
            .WithDragAndDrop(true)
            .WithClassName("Winit.CSharp.Example.Window")
            .WithUndecoratedShadow(_shadow)
            .WithSystemBackdrop(_backdrops[_backdropIndex])
            .WithClipChildren(true)
            .WithBorderColor(_colorSchemes[_colorSchemeIndex].Border)
            .WithTitleBackgroundColor(_colorSchemes[_colorSchemeIndex].Caption)
            .WithTitleTextColor(_colorSchemes[_colorSchemeIndex].Text)
            .WithCornerPreference(_corners[_cornerIndex])
            .WithUseSystemScrollSpeed(_systemWheelSpeed);
#endif

        _window = eventLoop.CreateWindow(attributes);
        Log($"created window id={_window.Id.IntoRaw()} scale={_window.ScaleFactor:0.##}");
        LogMonitors(eventLoop);
        UpdateTitle("created");
    }

    public void ProxyWakeUp(IActiveEventLoop eventLoop)
    {
        DrainConsoleKeys(eventLoop);
    }

    public void WindowEvent(IActiveEventLoop eventLoop, WindowId windowId, WindowEvent windowEvent)
    {
        if (_window is not null && windowId != _window.Id)
        {
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.CloseRequested _))
        {
            Log("close requested");
            Stop(eventLoop);
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.Destroyed _))
        {
            Log("destroyed");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.SurfaceResized resized))
        {
            Log($"surface resized: {resized.Size.Width}x{resized.Size.Height}");
            UpdateTitle();
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.Moved moved))
        {
            Log($"moved: {moved.Position.X},{moved.Position.Y}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.Focused focused))
        {
            Log($"focused: {focused.IsFocused}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.KeyboardInput keyboard))
        {
            KeyEvent key = keyboard.Event;
            Log(
                $"key: {key.State} physical={key.PhysicalKey.ToKeyCode()} logical={FormatKey(key.LogicalKey)} text={FormatText(key.Text)} repeat={key.Repeat}");
            HandleWindowShortcut(eventLoop, key);
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.ModifiersChanged modifiers))
        {
            Log($"modifiers: state={modifiers.Modifiers.State} keys={modifiers.Modifiers.PressedMods}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.Ime ime))
        {
            Log($"ime: {FormatIme(ime.Value)}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.PointerEntered pointerEntered))
        {
            _lastPointerPosition = pointerEntered.Position;
            Log(
                $"pointer entered: {FormatPointerKind(pointerEntered.Kind)} at {FormatPosition(pointerEntered.Position)} primary={pointerEntered.Primary}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.PointerMoved pointerMoved))
        {
            _lastPointerPosition = pointerMoved.Position;
            _pointerMoveCount++;
            UpdateImeCursorArea();
            if (_pointerMoveCount % 20 == 1)
            {
                Log(
                    $"pointer moved: {FormatPointerSource(pointerMoved.Source)} at {FormatPosition(pointerMoved.Position)} primary={pointerMoved.Primary}");
            }

            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.PointerLeft pointerLeft))
        {
            string position = pointerLeft.Position is { } value ? FormatPosition(value) : "unknown";
            Log($"pointer left: {FormatPointerKind(pointerLeft.Kind)} at {position}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.PointerButton pointerButton))
        {
            Log(
                $"pointer button: {pointerButton.State} {FormatButton(pointerButton.Button)} at {FormatPosition(pointerButton.Position)} primary={pointerButton.Primary}");
            HandlePointerButton(pointerButton);
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.MouseWheel wheel))
        {
            Log($"mouse wheel: {FormatWheel(wheel.Delta)} phase={wheel.Phase}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.ScaleFactorChanged scaleFactorChanged))
        {
            string requestedSize = scaleFactorChanged.SurfaceSizeWriter.TryGetSurfaceSize(out PhysicalSize<uint> size)
                ? $"{size.Width}x{size.Height}"
                : "unknown";
            Log($"scale factor changed: {scaleFactorChanged.ScaleFactor:0.##} surface={requestedSize}");
            UpdateTitle();
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.ThemeChanged themeChanged))
        {
            Log($"theme changed: {themeChanged.Theme}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.Occluded occluded))
        {
            Log($"occluded: {occluded.IsOccluded}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.RedrawRequested _))
        {
            _redrawCount++;
            Log($"redraw requested: #{_redrawCount}");
            UpdateTitle("redraw");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.DragEntered dragEntered))
        {
            Log($"drag entered: {dragEntered.Paths.Count} file(s) at {FormatPosition(dragEntered.Position)}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.DragMoved dragMoved))
        {
            Log($"drag moved: {FormatPosition(dragMoved.Position)}");
            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.DragDropped dragDropped))
        {
            Log($"drag dropped: {dragDropped.Paths.Count} file(s) at {FormatPosition(dragDropped.Position)}");
            foreach (string path in dragDropped.Paths.Take(8))
            {
                Log($"  {path}");
            }

            return;
        }

        if (windowEvent.TryGetValue(out WindowEvent.DragLeft dragLeft))
        {
            string position = dragLeft.Position is { } value ? FormatPosition(value) : "unknown";
            Log($"drag left: {position}");
        }
    }

    public void DeviceEvent(IActiveEventLoop eventLoop, DeviceId? deviceId, DeviceEvent deviceEvent)
    {
        string device = FormatDeviceId(deviceId);

        if (deviceEvent.TryGetValue(out DeviceEvent.PointerMotion motion))
        {
            _deviceMotionCount++;
            if (_deviceMotionCount % 30 == 1)
            {
                Log($"device motion: {device} delta={motion.Delta.X:0.##},{motion.Delta.Y:0.##}");
            }

            return;
        }

        if (deviceEvent.TryGetValue(out DeviceEvent.MouseWheel wheel))
        {
            Log($"device wheel: {device} {FormatWheel(wheel.Delta)}");
            return;
        }

        if (deviceEvent.TryGetValue(out DeviceEvent.Button button))
        {
            Log($"device button: {device} button={button.ButtonId} state={button.State}");
            return;
        }

        if (deviceEvent.TryGetValue(out DeviceEvent.Key key))
        {
            Log($"device key: {device} physical={key.RawKeyEvent.PhysicalKey.ToKeyCode()} state={key.RawKeyEvent.State}");
        }
    }

    public void AboutToWait(IActiveEventLoop eventLoop)
    {
        DrainConsoleKeys(eventLoop);
    }

    public void Suspended(IActiveEventLoop eventLoop)
    {
        Log("suspended");
    }

    public void DestroySurfaces(IActiveEventLoop eventLoop)
    {
        Log("destroy surfaces");
        if (_window is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _window = null;
    }

    public void MemoryWarning(IActiveEventLoop eventLoop)
    {
        Log("memory warning");
    }

    private void ReadConsoleKeys()
    {
        while (!_stopping)
        {
            try
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                _consoleKeys.Enqueue(key);
                _proxy.WakeUp();
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }
    }

    private void DrainConsoleKeys(IActiveEventLoop eventLoop)
    {
        while (_consoleKeys.TryDequeue(out ConsoleKeyInfo key))
        {
            HandleConsoleKey(eventLoop, key);
        }
    }

    private void HandleConsoleKey(IActiveEventLoop eventLoop, ConsoleKeyInfo key)
    {
        ArmWindowShortcutSuppression(key.Key);

        switch (key.Key)
        {
            case ConsoleKey.H:
            case ConsoleKey.F1:
                PrintHelp();
                break;
            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                Stop(eventLoop);
                break;
            case ConsoleKey.R:
                ToggleResizable();
                break;
            case ConsoleKey.D:
                ToggleDecorations();
                break;
            case ConsoleKey.V:
                ToggleVisible();
                break;
            case ConsoleKey.M:
                ToggleMaximized();
                break;
            case ConsoleKey.N:
                ToggleMinimized();
                break;
            case ConsoleKey.F:
            case ConsoleKey.F11:
                ToggleFullscreen();
                break;
            case ConsoleKey.T:
                CycleTheme();
                break;
            case ConsoleKey.L:
                CycleWindowLevel();
                break;
            case ConsoleKey.S:
                CycleSurfaceSize();
                break;
            case ConsoleKey.P:
                MoveBy(40, 30);
                break;
            case ConsoleKey.LeftArrow:
                MoveBy(-40, 0);
                break;
            case ConsoleKey.RightArrow:
                MoveBy(40, 0);
                break;
            case ConsoleKey.UpArrow:
                MoveBy(0, -40);
                break;
            case ConsoleKey.DownArrow:
                MoveBy(0, 40);
                break;
            case ConsoleKey.OemPlus:
            case ConsoleKey.Add:
                ResizeBy(80, 50);
                break;
            case ConsoleKey.OemMinus:
            case ConsoleKey.Subtract:
                ResizeBy(-80, -50);
                break;
#if WINDOWS
            case ConsoleKey.B:
                CycleBackdrop();
                break;
#endif
            case ConsoleKey.C:
                CycleCursorIcon();
                break;
            case ConsoleKey.U:
                ToggleCustomCursor();
                break;
            case ConsoleKey.G:
                CycleCursorGrab();
                break;
            case ConsoleKey.Y:
                ToggleCursorVisible();
                break;
            case ConsoleKey.I:
                ToggleIme();
                break;
            case ConsoleKey.A:
                RequestAttention();
                break;
#if WINDOWS
            case ConsoleKey.K:
                ToggleSkipTaskbar();
                break;
            case ConsoleKey.E:
                ToggleEnabled();
                break;
#endif
            case ConsoleKey.O:
                ToggleContentProtected();
                break;
#if WINDOWS
            case ConsoleKey.D1:
                CycleColors();
                break;
            case ConsoleKey.D2:
                CycleCornerPreference();
                break;
            case ConsoleKey.D3:
                ToggleUndecoratedShadow();
                break;
            case ConsoleKey.D4:
                ToggleSystemWheelSpeed();
                break;
#endif
            case ConsoleKey.D5:
                ToggleTransparent();
                break;
            case ConsoleKey.D6:
                ToggleBlur();
                break;
            case ConsoleKey.D7:
                ToggleConstraints();
                break;
            case ConsoleKey.D8:
                ToggleIcon();
                break;
            case ConsoleKey.D9:
                ToggleCursorHittest();
                break;
            case ConsoleKey.D0:
                CycleDeviceEvents(eventLoop);
                break;
            case ConsoleKey.X:
                ResetDeadKeys();
                break;
            case ConsoleKey.J:
                FocusWindow();
                break;
            case ConsoleKey.F5:
                RequestRedraw();
                break;
        }
    }

    private void HandleWindowShortcut(IActiveEventLoop eventLoop, KeyEvent key)
    {
        if (key.State != ElementState.Pressed || key.Repeat)
        {
            return;
        }

        KeyCode keyCode = key.PhysicalKey.ToKeyCode();
        if (ShouldSuppressWindowShortcut(keyCode))
        {
            return;
        }

        switch (keyCode)
        {
            case KeyCode.F1:
            case KeyCode.KeyH:
                PrintHelp();
                break;
            case KeyCode.Escape:
                if (_fullscreen)
                {
                    ToggleFullscreen();
                }
                else
                {
                    Stop(eventLoop);
                }

                break;
            case KeyCode.KeyQ:
                Stop(eventLoop);
                break;
            case KeyCode.KeyR:
                ToggleResizable();
                break;
            case KeyCode.KeyD:
                ToggleDecorations();
                break;
            case KeyCode.KeyV:
                ToggleVisible();
                break;
            case KeyCode.KeyM:
                ToggleMaximized();
                break;
            case KeyCode.KeyN:
                ToggleMinimized();
                break;
            case KeyCode.KeyF:
            case KeyCode.F11:
                ToggleFullscreen();
                break;
            case KeyCode.KeyT:
                CycleTheme();
                break;
            case KeyCode.KeyL:
                CycleWindowLevel();
                break;
            case KeyCode.KeyS:
                CycleSurfaceSize();
                break;
            case KeyCode.KeyP:
                MoveBy(40, 30);
                break;
            case KeyCode.ArrowLeft:
                MoveBy(-40, 0);
                break;
            case KeyCode.ArrowRight:
                MoveBy(40, 0);
                break;
            case KeyCode.ArrowUp:
                MoveBy(0, -40);
                break;
            case KeyCode.ArrowDown:
                MoveBy(0, 40);
                break;
            case KeyCode.Equal:
                ResizeBy(80, 50);
                break;
            case KeyCode.Minus:
                ResizeBy(-80, -50);
                break;
#if WINDOWS
            case KeyCode.KeyB:
                CycleBackdrop();
                break;
#endif
            case KeyCode.KeyC:
                CycleCursorIcon();
                break;
            case KeyCode.KeyU:
                ToggleCustomCursor();
                break;
            case KeyCode.KeyG:
                CycleCursorGrab();
                break;
            case KeyCode.KeyY:
                ToggleCursorVisible();
                break;
            case KeyCode.KeyI:
                ToggleIme();
                break;
            case KeyCode.KeyA:
                RequestAttention();
                break;
#if WINDOWS
            case KeyCode.KeyK:
                ToggleSkipTaskbar();
                break;
            case KeyCode.KeyE:
                ToggleEnabled();
                break;
#endif
            case KeyCode.KeyO:
                ToggleContentProtected();
                break;
#if WINDOWS
            case KeyCode.Digit1:
                CycleColors();
                break;
            case KeyCode.Digit2:
                CycleCornerPreference();
                break;
            case KeyCode.Digit3:
                ToggleUndecoratedShadow();
                break;
            case KeyCode.Digit4:
                ToggleSystemWheelSpeed();
                break;
#endif
            case KeyCode.Digit5:
                ToggleTransparent();
                break;
            case KeyCode.Digit6:
                ToggleBlur();
                break;
            case KeyCode.Digit7:
                ToggleConstraints();
                break;
            case KeyCode.Digit8:
                ToggleIcon();
                break;
            case KeyCode.Digit9:
                ToggleCursorHittest();
                break;
            case KeyCode.Digit0:
                CycleDeviceEvents(eventLoop);
                break;
            case KeyCode.KeyX:
                ResetDeadKeys();
                break;
            case KeyCode.KeyJ:
                FocusWindow();
                break;
            case KeyCode.F5:
                RequestRedraw();
                break;
        }
    }

    private void ArmWindowShortcutSuppression(ConsoleKey consoleKey)
    {
        if (!TryMapConsoleKey(consoleKey, out KeyCode keyCode))
        {
            return;
        }

        _suppressedWindowShortcut = keyCode;
        _suppressWindowShortcutUntilTicks = DateTime.UtcNow.Add(s_consoleShortcutSuppressDuration).Ticks;
    }

    private bool ShouldSuppressWindowShortcut(KeyCode keyCode)
    {
        if (_suppressedWindowShortcut != keyCode)
        {
            return false;
        }

        _suppressedWindowShortcut = null;
        if (DateTime.UtcNow.Ticks > _suppressWindowShortcutUntilTicks)
        {
            return false;
        }

        Log($"window shortcut suppressed once: {keyCode}");
        return true;
    }

    private static bool TryMapConsoleKey(ConsoleKey consoleKey, out KeyCode keyCode)
    {
        keyCode = consoleKey switch
        {
            ConsoleKey.H => KeyCode.KeyH,
            ConsoleKey.F1 => KeyCode.F1,
            ConsoleKey.Q => KeyCode.KeyQ,
            ConsoleKey.Escape => KeyCode.Escape,
            ConsoleKey.R => KeyCode.KeyR,
            ConsoleKey.D => KeyCode.KeyD,
            ConsoleKey.V => KeyCode.KeyV,
            ConsoleKey.M => KeyCode.KeyM,
            ConsoleKey.N => KeyCode.KeyN,
            ConsoleKey.F => KeyCode.KeyF,
            ConsoleKey.F11 => KeyCode.F11,
            ConsoleKey.T => KeyCode.KeyT,
            ConsoleKey.L => KeyCode.KeyL,
            ConsoleKey.S => KeyCode.KeyS,
            ConsoleKey.P => KeyCode.KeyP,
            ConsoleKey.LeftArrow => KeyCode.ArrowLeft,
            ConsoleKey.RightArrow => KeyCode.ArrowRight,
            ConsoleKey.UpArrow => KeyCode.ArrowUp,
            ConsoleKey.DownArrow => KeyCode.ArrowDown,
            ConsoleKey.OemPlus or ConsoleKey.Add => KeyCode.Equal,
            ConsoleKey.OemMinus or ConsoleKey.Subtract => KeyCode.Minus,
            ConsoleKey.B => KeyCode.KeyB,
            ConsoleKey.C => KeyCode.KeyC,
            ConsoleKey.U => KeyCode.KeyU,
            ConsoleKey.G => KeyCode.KeyG,
            ConsoleKey.Y => KeyCode.KeyY,
            ConsoleKey.I => KeyCode.KeyI,
            ConsoleKey.A => KeyCode.KeyA,
            ConsoleKey.K => KeyCode.KeyK,
            ConsoleKey.E => KeyCode.KeyE,
            ConsoleKey.O => KeyCode.KeyO,
            ConsoleKey.D1 => KeyCode.Digit1,
            ConsoleKey.D2 => KeyCode.Digit2,
            ConsoleKey.D3 => KeyCode.Digit3,
            ConsoleKey.D4 => KeyCode.Digit4,
            ConsoleKey.D5 => KeyCode.Digit5,
            ConsoleKey.D6 => KeyCode.Digit6,
            ConsoleKey.D7 => KeyCode.Digit7,
            ConsoleKey.D8 => KeyCode.Digit8,
            ConsoleKey.D9 => KeyCode.Digit9,
            ConsoleKey.D0 => KeyCode.Digit0,
            ConsoleKey.X => KeyCode.KeyX,
            ConsoleKey.J => KeyCode.KeyJ,
            ConsoleKey.F5 => KeyCode.F5,
            _ => KeyCode.Unidentified,
        };

        return keyCode != KeyCode.Unidentified;
    }

    private void HandlePointerButton(WindowEvent.PointerButton pointerButton)
    {
        if (pointerButton.State != ElementState.Pressed)
        {
            return;
        }

        MouseButton? mouseButton = pointerButton.Button.MouseButton();
        if (mouseButton == MouseButton.Right)
        {
            PhysicalPosition<int> position = pointerButton.Position.Cast<int>();
            TryWindow("show window menu", window => window.ShowWindowMenu(position), updateTitle: false);
        }
        else if (mouseButton == MouseButton.Middle)
        {
            TryWindow("drag window", window => window.DragWindow(), updateTitle: false);
        }
    }

    private void ToggleResizable()
    {
        _resizable = !_resizable;
        TryWindow($"resizable={_resizable}", window => window.SetResizable(_resizable));
    }

    private void ToggleDecorations()
    {
        _decorated = !_decorated;
        TryWindow($"decorations={_decorated}", window => window.SetDecorations(_decorated));
    }

    private void ToggleVisible()
    {
        _visible = !_visible;
        TryWindow($"visible={_visible}", window => window.SetVisible(_visible));
    }

    private void ToggleMaximized()
    {
        _maximized = !(_window?.IsMaximized ?? _maximized);
        TryWindow($"maximized={_maximized}", window => window.SetMaximized(_maximized));
    }

    private void ToggleMinimized()
    {
        _minimized = !_minimized;
        TryWindow($"minimized={_minimized}", window => window.SetMinimized(_minimized));
    }

    private void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        TryWindow(
            $"fullscreen={_fullscreen}",
            window => window.SetFullscreen(_fullscreen ? Fullscreen.FromBorderless(window.CurrentMonitor) : null));
    }

    private void CycleTheme()
    {
        _themeIndex = (_themeIndex + 1) % _themePreferences.Length;
        Theme? theme = _themePreferences[_themeIndex];
        TryWindow($"theme={FormatTheme(theme)}", window => window.SetTheme(theme));
    }

    private void CycleWindowLevel()
    {
        _levelIndex = (_levelIndex + 1) % _windowLevels.Length;
        WindowLevel level = _windowLevels[_levelIndex];
        TryWindow($"level={level}", window => window.SetWindowLevel(level));
    }

    private void CycleSurfaceSize()
    {
        _sizeIndex = (_sizeIndex + 1) % _sizes.Length;
        (LogicalSize<double> size, string name) = _sizes[_sizeIndex];
        TryWindow($"surface size={name}", window =>
        {
            PhysicalSize<uint>? actual = window.RequestSurfaceSize(size);
            if (actual is { } value)
            {
                Log($"requested surface size accepted as {value.Width}x{value.Height}");
            }
        });
    }

    private void ResizeBy(int deltaWidth, int deltaHeight)
    {
        TryWindow($"resize by {deltaWidth},{deltaHeight}", window =>
        {
            PhysicalSize<uint> current = window.SurfaceSize;
            uint width = ClampToUInt((long)current.Width + deltaWidth, 240, 2000);
            uint height = ClampToUInt((long)current.Height + deltaHeight, 180, 1400);
            _ = window.RequestSurfaceSize(new PhysicalSize<uint>(width, height));
        });
    }

    private void MoveBy(int deltaX, int deltaY)
    {
        TryWindow($"move by {deltaX},{deltaY}", window =>
        {
            PhysicalPosition<int> position = window.OuterPosition;
            window.SetOuterPosition(new PhysicalPosition<int>(position.X + deltaX, position.Y + deltaY));
        });
    }

#if WINDOWS
    private void CycleBackdrop()
    {
        _backdropIndex = (_backdropIndex + 1) % _backdrops.Length;
        WinBackdropType backdrop = _backdrops[_backdropIndex];
        TryWindow($"backdrop={backdrop}", window => window.SetSystemBackdrop(backdrop));
    }
#endif

    private void CycleCursorIcon()
    {
        _usingCustomCursor = false;
        _cursorIconIndex = (_cursorIconIndex + 1) % _cursorIcons.Length;
        CursorIcon cursor = _cursorIcons[_cursorIconIndex];
        TryWindow($"cursor={cursor.Name()}", window => window.SetCursor(cursor));
    }

    private void ToggleCustomCursor()
    {
        _usingCustomCursor = !_usingCustomCursor;
        TryWindow($"custom cursor={_usingCustomCursor}", window =>
        {
            if (_usingCustomCursor && _customCursor is not null)
            {
                window.SetCursor(Cursor.From(_customCursor));
            }
            else
            {
                window.SetCursor(_cursorIcons[_cursorIconIndex]);
            }
        });
    }

    private void CycleCursorGrab()
    {
        _cursorGrabIndex = (_cursorGrabIndex + 1) % _cursorGrabModes.Length;
        CursorGrabMode mode = _cursorGrabModes[_cursorGrabIndex];
        TryWindow($"cursor grab={mode}", window => window.SetCursorGrab(mode));
    }

    private void ToggleCursorVisible()
    {
        _cursorVisible = !_cursorVisible;
        TryWindow($"cursor visible={_cursorVisible}", window => window.SetCursorVisible(_cursorVisible));
    }

    private void ToggleIme()
    {
        _imeAllowed = !_imeAllowed;
        TryWindow($"ime={_imeAllowed}", window =>
        {
            window.SetImeAllowed(_imeAllowed);
            if (_imeAllowed)
            {
                window.SetImePurpose(ImePurpose.Terminal);
                UpdateImeCursorArea();
            }
        });
    }

    private void RequestAttention()
    {
        _attentionCritical = !_attentionCritical;
        UserAttentionType type = _attentionCritical
            ? UserAttentionType.Critical
            : UserAttentionType.Informational;
        TryWindow($"attention={type}", window => window.RequestUserAttention(type));
    }

#if WINDOWS
    private void ToggleSkipTaskbar()
    {
        _skipTaskbar = !_skipTaskbar;
        TryWindow($"skip taskbar={_skipTaskbar}", window => window.SetSkipTaskbar(_skipTaskbar));
    }

    private void ToggleEnabled()
    {
        _enabled = !_enabled;
        TryWindow($"enabled={_enabled}", window => window.SetEnable(_enabled));
    }

    private void CycleColors()
    {
        _colorSchemeIndex = (_colorSchemeIndex + 1) % _colorSchemes.Length;
        (WinColor? border, WinColor? caption, WinColor text, string name) = _colorSchemes[_colorSchemeIndex];
        TryWindow($"dwm colors={name}", window =>
        {
            window.SetBorderColor(border);
            window.SetTitleBackgroundColor(caption);
            window.SetTitleTextColor(text);
        });
    }

    private void CycleCornerPreference()
    {
        _cornerIndex = (_cornerIndex + 1) % _corners.Length;
        WinCornerPreference corner = _corners[_cornerIndex];
        TryWindow($"corner={corner}", window => window.SetCornerPreference(corner));
    }

    private void ToggleUndecoratedShadow()
    {
        _shadow = !_shadow;
        TryWindow($"undecorated shadow={_shadow}", window => window.SetUndecoratedShadow(_shadow));
    }

    private void ToggleSystemWheelSpeed()
    {
        _systemWheelSpeed = !_systemWheelSpeed;
        TryWindow($"system wheel speed={_systemWheelSpeed}", window => window.SetUseSystemScrollSpeed(_systemWheelSpeed));
    }
#endif

    private void ToggleContentProtected()
    {
        _contentProtected = !_contentProtected;
        TryWindow($"content protected={_contentProtected}", window => window.SetContentProtected(_contentProtected));
    }

    private void ToggleTransparent()
    {
        _transparent = !_transparent;
        TryWindow($"transparent={_transparent}", window => window.SetTransparent(_transparent));
    }

    private void ToggleBlur()
    {
        _blur = !_blur;
        TryWindow($"blur={_blur}", window => window.SetBlur(_blur));
    }

    private void ToggleConstraints()
    {
        _constraintsEnabled = !_constraintsEnabled;
        TryWindow($"constraints={_constraintsEnabled}", window =>
        {
            if (_constraintsEnabled)
            {
                window.SetMinSurfaceSize(new LogicalSize<double>(360.0, 260.0));
                window.SetMaxSurfaceSize(new LogicalSize<double>(1600.0, 1000.0));
                window.SetSurfaceResizeIncrements(new LogicalSize<double>(20.0, 20.0));
            }
            else
            {
                window.SetMinSurfaceSize(null);
                window.SetMaxSurfaceSize(null);
                window.SetSurfaceResizeIncrements(null);
            }
        });
    }

    private void ToggleIcon()
    {
        _usingAlternateIcon = !_usingAlternateIcon;
        Icon icon = _usingAlternateIcon ? _alternateIcon : _primaryIcon;
        TryWindow($"icon={(_usingAlternateIcon ? "alternate" : "primary")}", window =>
        {
            window.SetWindowIcon(icon);
#if WINDOWS
            window.SetTaskbarIcon(icon);
#endif
        });
    }

    private void ToggleCursorHittest()
    {
        _cursorHittest = !_cursorHittest;
        TryWindow($"cursor hittest={_cursorHittest}", window => window.SetCursorHittest(_cursorHittest));
    }

    private void CycleDeviceEvents(IActiveEventLoop eventLoop)
    {
        _deviceEvents = _deviceEvents switch
        {
            DeviceEvents.Always => DeviceEvents.WhenFocused,
            DeviceEvents.WhenFocused => DeviceEvents.Never,
            _ => DeviceEvents.Always,
        };
        eventLoop.ListenDeviceEvents(_deviceEvents);
        Log($"device events={_deviceEvents}");
        UpdateTitle("device events");
    }

    private void ResetDeadKeys()
    {
        TryWindow("reset dead keys", window => window.ResetDeadKeys());
    }

    private void FocusWindow()
    {
        TryWindow("focus window", window => window.FocusWindow());
    }

    private void RequestRedraw()
    {
        TryWindow("request redraw", window => window.RequestRedraw(), updateTitle: false);
    }

    private void UpdateImeCursorArea()
    {
        if (!_imeAllowed || _window is null)
        {
            return;
        }

        try
        {
            _window.SetImeCursorArea(
                _lastPointerPosition.Cast<int>(),
                new LogicalSize<double>(24.0, 24.0));
        }
        catch (Exception exception)
        {
            Log($"ime cursor area failed: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private void TryWindow(string label, Action<IWindow> action, bool updateTitle = true)
    {
        if (_window is null)
        {
            Log($"{label}: no window");
            return;
        }

        try
        {
            action(_window);
            Log($"{label}: ok");
            if (updateTitle)
            {
                UpdateTitle(label);
            }
        }
        catch (Exception exception)
        {
            Log($"{label}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private void UpdateTitle(string? status = null)
    {
        if (_window is null)
        {
            return;
        }

        try
        {
            PhysicalSize<uint> size = _window.SurfaceSize;
            string cursor = _usingCustomCursor ? "custom" : _cursorIcons[_cursorIconIndex].Name();
            string title = string.Concat(
                BaseTitle,
                " | ",
                size.Width,
                "x",
                size.Height,
                " | theme=",
                FormatTheme(_themePreferences[_themeIndex]),
                " | level=",
                _windowLevels[_levelIndex],
                " | cursor=",
                cursor,
                " | grab=",
                _cursorGrabModes[_cursorGrabIndex],
                " | redraws=",
                _redrawCount);
            if (!string.IsNullOrWhiteSpace(status))
            {
                title += $" | {status}";
            }

            _window.SetTitle(title);
        }
        catch (Exception exception)
        {
            Log($"update title failed: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private void Stop(IActiveEventLoop eventLoop)
    {
        _stopping = true;
        eventLoop.Exit();
    }

    private void LogMonitors(IActiveEventLoop eventLoop)
    {
        MonitorHandle? primary = eventLoop.PrimaryMonitor;
        Log($"primary monitor: {FormatMonitor(primary)}");

        int index = 0;
        foreach (MonitorHandle monitor in eventLoop.AvailableMonitors)
        {
            Log($"monitor[{index++}]: {FormatMonitor(monitor)}");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Winit C# Window Example controls");
        Console.WriteLine("  H/F1 help, Q/Esc quit, F5 redraw, F/F11 fullscreen");
        Console.WriteLine("  R resizable, D decorations, V visible, M maximize, N minimize");
        Console.WriteLine("  S cycle size, +/- resize, P move, arrows move");
#if WINDOWS
        Console.WriteLine("  T theme, L level, B backdrop, 1 DWM colors, 2 corners");
        Console.WriteLine("  C cursor icon, U custom cursor, G cursor grab, Y cursor visible, 9 cursor hittest");
        Console.WriteLine("  I IME, A attention, K skip taskbar, E enable, O content protection");
        Console.WriteLine("  3 shadow, 4 system wheel speed, 5 transparent, 6 blur, 7 constraints, 8 icon");
#else
        Console.WriteLine("  T theme, L level, C cursor icon, U custom cursor, G cursor grab, Y cursor visible");
        Console.WriteLine("  I IME, A attention, O content protection, 5 transparent, 6 blur, 7 constraints, 8 icon, 9 cursor hittest");
#endif
        Console.WriteLine("  0 device event mode, X reset dead keys, J focus");
        Console.WriteLine("  Right click opens the system menu, middle click starts system move, drag files over the window.");
        Console.WriteLine();
    }

    private static Icon CreateIcon(byte red, byte green, byte blue)
    {
        const int size = 32;
        byte[] rgba = new byte[size * size * 4];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = (y * size + x) * 4;
                double dx = x - 15.5;
                double dy = y - 15.5;
                bool inside = (dx * dx) + (dy * dy) <= 15.0 * 15.0;
                if (!inside)
                {
                    rgba[index + 3] = 0;
                    continue;
                }

                bool mark = x == y || x == size - y - 1 || (x is >= 14 and <= 17 && y is >= 8 and <= 24);
                rgba[index] = mark ? (byte)255 : red;
                rgba[index + 1] = mark ? (byte)255 : green;
                rgba[index + 2] = mark ? (byte)255 : blue;
                rgba[index + 3] = 255;
            }
        }

        return Icon.From(new RgbaIcon(rgba, size, size));
    }

    private static byte[] CreateCursorRgba()
    {
        const int size = 32;
        byte[] rgba = new byte[size * size * 4];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool fill = x <= 4 && y <= 24
                    || y <= 4 && x <= 24
                    || Math.Abs(x - y) <= 1 && x <= 23
                    || (x is >= 10 and <= 17 && y is >= 18 and <= 25);
                bool outline = x <= 6 && y <= 26
                    || y <= 6 && x <= 26
                    || Math.Abs(x - y) <= 3 && x <= 25;

                int index = (y * size + x) * 4;
                if (fill)
                {
                    rgba[index] = 255;
                    rgba[index + 1] = 255;
                    rgba[index + 2] = 255;
                    rgba[index + 3] = 255;
                }
                else if (outline)
                {
                    rgba[index] = 0;
                    rgba[index + 1] = 0;
                    rgba[index + 2] = 0;
                    rgba[index + 3] = 220;
                }
            }
        }

        return rgba;
    }

    private static uint ClampToUInt(long value, uint min, uint max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return (uint)value;
    }

    private static string FormatText(string? text)
    {
        return text is null ? "<null>" : $"\"{text}\"";
    }

    private static string FormatTheme(Theme? theme)
    {
        return theme?.ToString() ?? "System";
    }

    private static string FormatKey(Key key)
    {
        if (key.TryGetValue(out Key.Named named))
        {
            return named.NamedKey.ToString();
        }

        if (key.TryGetValue(out Key.Character character))
        {
            return character.Value;
        }

        if (key.TryGetValue(out Key.Dead dead))
        {
            return dead.Value is { } value ? $"Dead({value})" : "Dead";
        }

        return key.TryGetValue(out Key.Unidentified unidentified)
            ? unidentified.NativeKey.ToString()
            : "unknown";
    }

    private static string FormatIme(Winit.Core.Ime ime)
    {
        if (ime.TryGetValue(out Winit.Core.Ime.Enabled _))
        {
            return "enabled";
        }

        if (ime.TryGetValue(out Winit.Core.Ime.Preedit preedit))
        {
            string cursor = preedit.CursorRange is { } range ? $" cursor={range.Begin}-{range.End}" : string.Empty;
            return $"preedit \"{preedit.Text}\"{cursor}";
        }

        if (ime.TryGetValue(out Winit.Core.Ime.Commit commit))
        {
            return $"commit \"{commit.Text}\"";
        }

        if (ime.TryGetValue(out Winit.Core.Ime.DeleteSurrounding deleteSurrounding))
        {
            return $"delete surrounding before={deleteSurrounding.BeforeBytes} after={deleteSurrounding.AfterBytes}";
        }

        return "disabled";
    }

    private static string FormatPointerSource(PointerSource source)
    {
        if (source.TryGetValue(out PointerSource.Mouse _))
        {
            return "mouse";
        }

        if (source.TryGetValue(out PointerSource.Touch touch))
        {
            return $"touch:{touch.FingerId.IntoRaw()}";
        }

        if (source.TryGetValue(out PointerSource.TabletTool tablet))
        {
            return $"tablet:{tablet.Kind}";
        }

        return "unknown";
    }

    private static string FormatPointerKind(PointerKind kind)
    {
        if (kind.TryGetValue(out PointerKind.Mouse _))
        {
            return "mouse";
        }

        if (kind.TryGetValue(out PointerKind.Touch touch))
        {
            return $"touch:{touch.FingerId.IntoRaw()}";
        }

        if (kind.TryGetValue(out PointerKind.TabletTool tablet))
        {
            return $"tablet:{tablet.Kind}";
        }

        return "unknown";
    }

    private static string FormatButton(ButtonSource button)
    {
        if (button.TryGetValue(out ButtonSource.Mouse mouse))
        {
            return mouse.Button.ToString();
        }

        if (button.TryGetValue(out ButtonSource.Touch touch))
        {
            return $"touch:{touch.FingerId.IntoRaw()}";
        }

        if (button.TryGetValue(out ButtonSource.TabletTool tablet))
        {
            return $"tablet:{tablet.Kind}:{tablet.Button}";
        }

        return button.TryGetValue(out ButtonSource.Unknown unknown)
            ? $"unknown:{unknown.Code}"
            : "unknown";
    }

    private static string FormatWheel(MouseScrollDelta delta)
    {
        if (delta.TryGetValue(out MouseScrollDelta.LineDelta line))
        {
            return $"line={line.X:0.##},{line.Y:0.##}";
        }

        if (delta.TryGetValue(out MouseScrollDelta.PixelDelta pixel))
        {
            return $"pixel={FormatPosition(pixel.Position)}";
        }

        return "unknown";
    }

    private static string FormatPosition<T>(PhysicalPosition<T> position)
        where T : struct, System.Numerics.INumberBase<T>
    {
        return $"{position.X},{position.Y}";
    }

    private static string FormatDeviceId(DeviceId? deviceId)
    {
        if (deviceId is not { } value)
        {
            return "none";
        }

#if WINDOWS
        string? persistent = value.PersistentIdentifier();
        if (persistent is not null)
        {
            return $"{value.IntoRaw()}:{persistent}";
        }
#endif

        return value.IntoRaw().ToString();
    }

    private static string FormatMonitor(MonitorHandle? monitor)
    {
        if (monitor is null)
        {
            return "none";
        }

        string name = monitor.Name ?? "<unnamed>";
        string position = monitor.Position is { } value ? $"{value.X},{value.Y}" : "unknown";
        VideoMode? mode = monitor.CurrentVideoMode;
        string size = mode is { } current ? current.ToString() : "unknown";
        return $"{name} native={monitor.NativeId} pos={position} scale={monitor.ScaleFactor:0.##} mode={size}";
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}
