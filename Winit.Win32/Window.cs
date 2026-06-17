using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;
using Winit.Dpi;
using DrawingPoint = System.Drawing.Point;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.Win32;

public sealed unsafe class Window : IWindow, IDisposable
{
    private const ushort HtClient = 1;
    private const nuint HtCaption = 2;
    private const nuint HtLeft = 10;
    private const nuint HtRight = 11;
    private const nuint HtTop = 12;
    private const nuint HtTopLeft = 13;
    private const nuint HtTopRight = 14;
    private const nuint HtBottom = 15;
    private const nuint HtBottomLeft = 16;
    private const nuint HtBottomRight = 17;
    private const ushort XButton1 = 1;
    private const ushort XButton2 = 2;
    private const nuint IconSmall = 0;
    private const nuint IconBig = 1;
    private const uint HoverDefault = 0xFFFFFFFF;
    private const uint TmeLeave = 0x00000002;
    private const uint TwfWantPalm = 0x00000002;
    private const uint TouchEventMove = 0x0001;
    private const uint TouchEventDown = 0x0002;
    private const uint TouchEventUp = 0x0004;
    private const uint TouchEventPrimary = 0x0010;
    private const uint PenFlagBarrel = 0x00000001;
    private const uint PenFlagEraser = 0x00000004;
    private const uint PenMaskPressure = 0x00000001;
    private const uint PenMaskRotation = 0x00000002;
    private const uint PenMaskTiltX = 0x00000004;
    private const uint PenMaskTiltY = 0x00000008;
    private const uint PointerTypeTouch = 2;
    private const uint PointerTypePen = 3;
    private const uint PointerFlagPrimary = 0x00002000;
    private const uint PointerFlagDown = 0x00010000;
    private const uint PointerFlagUpdate = 0x00020000;
    private const uint PointerFlagUp = 0x00040000;
    private const uint WmSetIcon = 0x0080;
    private const uint WmImeStartComposition = 0x010D;
    private const uint WmImeEndComposition = 0x010E;
    private const uint WmImeComposition = 0x010F;
    private const uint WmImeSetContext = 0x0281;
    private const uint SpiGetWheelScrollLines = 0x0068;
    private const uint SpiGetWheelScrollChars = 0x006C;
    private const uint WheelPageScroll = uint.MaxValue;
    private const uint DefaultScrollLinesPerWheelDelta = 3;
    private const uint DefaultScrollCharsPerWheelDelta = 3;
    private const uint ScRestore = 0xF120;
    private const uint ScMove = 0xF010;
    private const uint ScSize = 0xF000;
    private const uint ScMinimize = 0xF020;
    private const uint ScMaximize = 0xF030;
    private const uint ScClose = 0xF060;
    private const uint ScScreenSave = 0xF140;
    private const uint TpmLeftAlign = 0x0000;
    private const uint TpmReturnCmd = 0x0100;
    private const uint SysCommandMask = 0xFFF0;
    private const uint MncClose = 1;
    private const uint SizeMaximized = 2;
    private const nuint WmszLeft = 1;
    private const nuint WmszRight = 2;
    private const nuint WmszTop = 3;
    private const nuint WmszTopLeft = 4;
    private const nuint WmszTopRight = 5;
    private const nuint WmszBottom = 6;
    private const nuint WmszBottomLeft = 7;
    private const nuint WmszBottomRight = 8;
    private const uint FlashwStop = 0x0000;
    private const uint FlashwTray = 0x0002;
    private const uint FlashwAll = 0x0003;
    private const uint FlashwTimerNoFg = 0x000C;
    private const uint WdaNone = 0x00000000;
    private const uint WdaExcludeFromCapture = 0x00000011;
    private const uint CdsFullscreen = 0x00000004;
    private const int DispChangeSuccessful = 0;
    private const uint DmBitsPerPel = 0x00040000;
    private const uint DmPelsWidth = 0x00080000;
    private const uint DmPelsHeight = 0x00100000;
    private const uint DmDisplayFrequency = 0x00400000;
    private const uint GcsCompStr = 0x0008;
    private const uint GcsResultStr = 0x0800;
    private const uint DwmwaWindowCornerPreference = 33;
    private const uint DwmwaBorderColor = 34;
    private const uint DwmwaCaptionColor = 35;
    private const uint DwmwaTextColor = 36;
    private const uint DwmwaSystemBackdropType = 38;
    private static readonly nint s_iscShowUiCompositionWindow = unchecked((nint)0x80000000);
    private static readonly uint s_taskbarCreatedMessage = PInvoke.RegisterWindowMessageW("TaskbarCreated");
    private static readonly ConcurrentDictionary<nint, Window> s_windows = new();
    private static readonly ConcurrentDictionary<string, byte> s_registeredClasses = new(StringComparer.Ordinal);

    private readonly EventLoop _eventLoop;
    private readonly WindowState _state;
    private readonly WindowAttributesWindows _platformAttributes;
    private HWND _hwnd;
    private bool _disposed;
    private string _title;
    private FileDropHandler? _fileDropTarget;

    private Window(
        EventLoop eventLoop,
        HWND hwnd,
        WindowAttributes attributes,
        WindowState state,
        WindowAttributesWindows platformAttributes)
    {
        _eventLoop = eventLoop;
        _state = state;
        _platformAttributes = platformAttributes;
        _hwnd = hwnd;
        _title = attributes.Title;
    }

    public static Window Create(EventLoop eventLoop, WindowAttributes attributes)
    {
        WindowAttributesWindows platformAttributes = attributes.Platform is WindowAttributesWindows winAttributes
            ? winAttributes
            : new WindowAttributesWindows();
        RegisterWindowClass(platformAttributes.ClassName);
        double scaleFactor = 1.0;
        Theme currentTheme = DarkMode.TryTheme(HWND.Null, attributes.PreferredTheme, refreshTitleBar: false);
        WindowState state = new(attributes, platformAttributes, scaleFactor, currentTheme, attributes.PreferredTheme);
        Size requestedSurfaceSize = attributes.SurfaceSize ?? new PhysicalSize<uint>(800, 600);
        if (attributes.MinSurfaceSize is not null || attributes.MaxSurfaceSize is not null)
        {
            requestedSurfaceSize = Size.Clamp(
                requestedSurfaceSize,
                attributes.MinSurfaceSize ?? new PhysicalSize<uint>(0, 0),
                attributes.MaxSurfaceSize ?? new PhysicalSize<uint>(uint.MaxValue, uint.MaxValue),
                scaleFactor);
        }

        PhysicalSize<uint> surfaceSize = requestedSurfaceSize.ToPhysical<uint>(scaleFactor);
        Position? requestedPosition = attributes.Position;
        PhysicalPosition<int>? position = requestedPosition?.ToPhysical<int>(scaleFactor);

        (WINDOW_STYLE style, WINDOW_EX_STYLE exStyle) = state.WindowFlags.ToWindowStyles();

        RECT rect = new()
        {
            left = 0,
            top = 0,
            right = checked((int)surfaceSize.Width),
            bottom = checked((int)surfaceSize.Height),
        };

        if (!PInvoke.AdjustWindowRectEx(ref rect, style, false, exStyle))
        {
            throw Win32Error.Request();
        }

        int width = rect.right - rect.left;
        int height = rect.bottom - rect.top;
        int x = position?.X ?? PInvoke.CW_USEDEFAULT;
        int y = position?.Y ?? PInvoke.CW_USEDEFAULT;

        HWND parent = attributes.ParentWindow is not null
            ? HwndFromObject(attributes.ParentWindow)
            : platformAttributes.Owner is { } owner
                ? new HWND(owner)
                : HWND.Null;
        HMENU menu = platformAttributes.Menu is { } menuValue ? new HMENU(menuValue) : HMENU.Null;

        fixed (char* className = platformAttributes.ClassName)
        fixed (char* title = attributes.Title)
        {
            HWND hwnd = PInvoke.CreateWindowEx(
                exStyle,
                new PCWSTR(className),
                new PCWSTR(title),
                style,
                x,
                y,
                width,
                height,
                parent,
                menu,
                HINSTANCE.Null,
                null);

            if (hwnd == HWND.Null)
            {
                throw Win32Error.Request();
            }

            Window window = new(eventLoop, hwnd, attributes, state, platformAttributes);
            s_windows[Util.HwndValue(hwnd)] = window;
            state.SurfaceSize = window.SurfaceSize;
            window.SetWindowIcon(attributes.WindowIcon);
            window.SetTaskbarIcon(platformAttributes.TaskbarIcon);
            window.SetCursor(attributes.Cursor);
            window.ApplyPlatformVisualAttributes();
            ImeContext.SetImeAllowed(hwnd, false);
            PInvoke.RegisterTouchWindow(hwnd, TwfWantPalm);
            if (platformAttributes.DragAndDrop)
            {
                window._fileDropTarget = FileDropHandler.Register(window);
            }

            if (attributes.ContentProtected)
            {
                window.SetContentProtected(true);
            }

            if (attributes.Visible)
            {
                PInvoke.ShowWindow(
                    hwnd,
                    attributes.Active ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
                PInvoke.UpdateWindow(hwnd);
            }

            if (attributes.Maximized && attributes.Fullscreen is null)
            {
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_MAXIMIZE);
            }

            if (attributes.Fullscreen is { } initialFullscreen)
            {
                window._state.Fullscreen = null;
                window.SetFullscreen(initialFullscreen);
                if (attributes.Active)
                {
                    PInvoke.SetForegroundWindow(hwnd);
                }
            }

            return window;
        }
    }

    internal static void DispatchBufferedScaleFactorChanged(
        EventLoop eventLoop,
        IApplicationHandler app,
        WindowId windowId,
        double scaleFactor,
        PhysicalSize<uint> surfaceSize)
    {
        SurfaceSizeState state = new(surfaceSize);
        app.WindowEvent(
            eventLoop,
            windowId,
            new WindowEvent(new WindowEvent.ScaleFactorChanged(scaleFactor, SurfaceSizeWriter.Create(state))));

        if (!s_windows.TryGetValue(unchecked((nint)windowId.IntoRaw()), out Window? window))
        {
            return;
        }

        PhysicalSize<uint> requestedSurfaceSize = state.SurfaceSize;
        if (requestedSurfaceSize != surfaceSize)
        {
            window.SetOuterSizeForSurface(requestedSurfaceSize);
        }
    }

    internal HWND Hwnd => _hwnd;

    internal void DispatchWindowEvent(WindowEvent windowEvent)
    {
        _eventLoop.SendWindowEvent(Id, windowEvent);
    }

    private bool DispatchToEventLoopThread(Action action)
    {
        if (_eventLoop.ThreadExecutor.InEventLoopThread)
        {
            return false;
        }

        _eventLoop.ThreadExecutor.Execute(action);
        return true;
    }

    public nint RawHwnd => Util.HwndValue(_hwnd);

    public WindowId Id => WindowId.FromRaw((nuint)Util.HwndValue(_hwnd));

    public double ScaleFactor => Dpi.DpiToScaleFactor(Dpi.HwndDpi(_hwnd));

    public PhysicalPosition<int> SurfacePosition => new(0, 0);

    public PhysicalPosition<int> OuterPosition
    {
        get
        {
            RECT rect = WindowRect();
            return new PhysicalPosition<int>(rect.left, rect.top);
        }
    }

    public PhysicalSize<uint> SurfaceSize
    {
        get
        {
            RECT rect = ClientRect();
            return new PhysicalSize<uint>(checked((uint)(rect.right - rect.left)), checked((uint)(rect.bottom - rect.top)));
        }
    }

    public PhysicalSize<uint> OuterSize
    {
        get
        {
            RECT rect = WindowRect();
            return new PhysicalSize<uint>(checked((uint)(rect.right - rect.left)), checked((uint)(rect.bottom - rect.top)));
        }
    }

    public PhysicalInsets<uint> SafeArea => new(0, 0, 0, 0);

    public PhysicalSize<uint>? SurfaceResizeIncrements => _state.SurfaceResizeIncrements?.ToPhysical<uint>(ScaleFactor);

    public bool? IsVisible => PInvoke.IsWindowVisible(_hwnd);

    public bool IsResizable => _state.WindowFlags.HasFlag(WindowFlags.Resizable);

    public WindowButtons EnabledButtons => _state.WindowFlags.ToWindowButtons();

    public bool? IsMinimized => PInvoke.IsIconic(_hwnd);

    public bool IsMaximized => PInvoke.IsZoomed(_hwnd);

    public Fullscreen? Fullscreen => _state.Fullscreen;

    public bool IsDecorated => _state.WindowFlags.HasFlag(WindowFlags.MarkerDecorations);

    public ImeCapabilities? ImeCapabilities => _state.ImeCapabilities;

    public bool HasFocus => _state.HasActiveFocus;

    public Theme? Theme => _state.CurrentTheme;

    public string Title
    {
        get
        {
            int length = PInvoke.GetWindowTextLength(_hwnd);
            if (length <= 0)
            {
                return string.Empty;
            }

            Span<char> buffer = stackalloc char[length + 1];
            fixed (char* text = buffer)
            {
                int copied = PInvoke.GetWindowText(_hwnd, new PWSTR(text), buffer.Length);
                return new string(text, 0, copied);
            }
        }
    }

    public CoreMonitorHandle? CurrentMonitor => Monitor.CurrentMonitor(_hwnd);

    public IEnumerable<CoreMonitorHandle> AvailableMonitors => Monitor.AvailableMonitors();

    public CoreMonitorHandle? PrimaryMonitor => Monitor.PrimaryMonitor();

    public object? DisplayHandle => RawDisplayHandle.FromWindows();

    public object? WindowHandle => RawWindowHandle.FromWin32(Util.HwndValue(_hwnd), ModuleHandle());

    public void RequestRedraw()
    {
        if (DispatchToEventLoopThread(RequestRedraw))
        {
            return;
        }

        _state.RedrawRequested = true;
        PInvoke.RedrawWindow(_hwnd, null, 0, PInvoke.RDW_INTERNALPAINT);
    }

    public void PrePresentNotify()
    {
    }

    public void ResetDeadKeys()
    {
        if (DispatchToEventLoopThread(ResetDeadKeys))
        {
            return;
        }

        const uint mapvkVkToVsc = 0;
        const int keyboardStateLength = 256;
        const int bufferLength = 8;

        uint virtualKey = Keyboard.VkSpace;
        uint scanCode = PInvoke.MapVirtualKeyW(virtualKey, mapvkVkToVsc);
        byte* keyboardState = stackalloc byte[keyboardStateLength];
        char* buffer = stackalloc char[bufferLength];
        _ = PInvoke.ToUnicode(virtualKey, scanCode, keyboardState, buffer, bufferLength, 0);
    }

    public PhysicalSize<uint>? RequestSurfaceSize(Size size)
    {
        if (DispatchToEventLoopThread(() => RequestSurfaceSize(size)))
        {
            return _state.SurfaceSize;
        }

        PhysicalSize<uint> physical = size.ToPhysical<uint>(ScaleFactor);
        SetOuterSizeForSurface(physical);
        return SurfaceSize;
    }

    public void SetOuterPosition(Position position)
    {
        if (DispatchToEventLoopThread(() => SetOuterPosition(position)))
        {
            return;
        }

        PhysicalPosition<int> physical = position.ToPhysical<int>(ScaleFactor);
        if (!PInvoke.SetWindowPos(
            _hwnd,
            HWND.Null,
            physical.X,
            physical.Y,
            0,
            0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
        {
            throw Win32Error.Request();
        }
    }

    public void SetMinSurfaceSize(Size? minSize)
    {
        if (DispatchToEventLoopThread(() => SetMinSurfaceSize(minSize)))
        {
            return;
        }

        _state.MinSize = minSize;
        _ = RequestSurfaceSize(SurfaceSize);
    }

    public void SetMaxSurfaceSize(Size? maxSize)
    {
        if (DispatchToEventLoopThread(() => SetMaxSurfaceSize(maxSize)))
        {
            return;
        }

        _state.MaxSize = maxSize;
        _ = RequestSurfaceSize(SurfaceSize);
    }

    public void SetSurfaceResizeIncrements(Size? increments)
    {
        if (DispatchToEventLoopThread(() => SetSurfaceResizeIncrements(increments)))
        {
            return;
        }

        _state.SurfaceResizeIncrements = increments;
    }

    public void SetTitle(string title)
    {
        if (DispatchToEventLoopThread(() => SetTitle(title)))
        {
            return;
        }

        fixed (char* text = title)
        {
            if (!PInvoke.SetWindowText(_hwnd, new PCWSTR(text)))
            {
                throw Win32Error.Request();
            }
        }

        _title = title;
    }

    public void SetTransparent(bool transparent)
    {
        if (DispatchToEventLoopThread(() => SetTransparent(transparent)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.Transparent, transparent));
    }

    public void SetBlur(bool blur)
    {
    }

    public void SetEnable(bool enabled)
    {
        if (DispatchToEventLoopThread(() => SetEnable(enabled)))
        {
            return;
        }

        PInvoke.EnableWindow(_hwnd, enabled);
    }

    public void SetVisible(bool visible)
    {
        if (DispatchToEventLoopThread(() => SetVisible(visible)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.Visible, visible));
    }

    public void SetResizable(bool resizable)
    {
        if (DispatchToEventLoopThread(() => SetResizable(resizable)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.Resizable, resizable));
    }

    public void SetEnabledButtons(WindowButtons buttons)
    {
        if (DispatchToEventLoopThread(() => SetEnabledButtons(buttons)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags
            .With(WindowFlags.Minimizable, buttons.HasFlag(WindowButtons.Minimize))
            .With(WindowFlags.Maximizable, buttons.HasFlag(WindowButtons.Maximize))
            .With(WindowFlags.Closable, buttons.HasFlag(WindowButtons.Close)));
    }

    public void SetMinimized(bool minimized)
    {
        if (DispatchToEventLoopThread(() => SetMinimized(minimized)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.Minimized, minimized));
    }

    public void SetMaximized(bool maximized)
    {
        if (DispatchToEventLoopThread(() => SetMaximized(maximized)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.Maximized, maximized));
    }

    public void SetFullscreen(Fullscreen? fullscreen)
    {
        if (DispatchToEventLoopThread(() => SetFullscreen(fullscreen)))
        {
            return;
        }

        Fullscreen? oldFullscreen = _state.Fullscreen;
        if (oldFullscreen == fullscreen)
        {
            return;
        }

        if (IsCurrentBorderlessFullscreen(oldFullscreen, fullscreen))
        {
            return;
        }

        ApplyDisplayModeChange(oldFullscreen, fullscreen);

        _state.Fullscreen = fullscreen;
        _state.SetWindowFlags(_hwnd, flags => flags
            .With(WindowFlags.MarkerExclusiveFullscreen, fullscreen?.TryGetValue(out Fullscreen.Exclusive _) == true)
            .With(WindowFlags.MarkerBorderlessFullscreen, fullscreen?.TryGetValue(out Fullscreen.Borderless _) == true));
        TaskbarList.MarkFullscreenWindow(_hwnd, fullscreen is not null);

        if (fullscreen is { } value)
        {
            SaveWindowPlacement();
            RECT monitorRect = MonitorRectForFullscreen(value);
            if (!PInvoke.SetWindowPos(
                _hwnd,
                HWND.Null,
                monitorRect.left,
                monitorRect.top,
                monitorRect.right - monitorRect.left,
                monitorRect.bottom - monitorRect.top,
                SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
            {
                throw Win32Error.Request();
            }
        }
        else if (_state.SavedWindow is { } savedWindow)
        {
            _state.SavedWindow = null;
            WindowPlacement placement = savedWindow.Placement;
            if (!PInvoke.SetWindowPlacement(_hwnd, ref placement))
            {
                throw Win32Error.Request();
            }
        }
    }

    public void SetDecorations(bool decorations)
    {
        if (DispatchToEventLoopThread(() => SetDecorations(decorations)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.MarkerDecorations, decorations));
    }

    public void SetWindowLevel(WindowLevel level)
    {
        if (DispatchToEventLoopThread(() => SetWindowLevel(level)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags
            .With(WindowFlags.AlwaysOnTop, level == WindowLevel.AlwaysOnTop)
            .With(WindowFlags.AlwaysOnBottom, level == WindowLevel.AlwaysOnBottom));
    }

    public void SetWindowIcon(Icon? windowIcon)
    {
        if (DispatchToEventLoopThread(() => SetWindowIcon(windowIcon)))
        {
            return;
        }

        SetIcon(windowIcon, IconSmall);
    }

    public void RequestImeUpdate(ImeRequest request)
    {
        if (DispatchToEventLoopThread(() => RequestImeUpdate(request)))
        {
            return;
        }

        ImeCapabilities? currentCapabilities = _state.ImeCapabilities;
        if (request.TryGetValue(out ImeRequest.Enable enable))
        {
            if (currentCapabilities is not null)
            {
                throw new ImeRequestException(ImeRequestError.AlreadyEnabled);
            }

            ImeCapabilities capabilities = enable.Value.Capabilities;
            ImeRequestData requestData = enable.Value.RequestData;
            _state.ImeCapabilities = capabilities;
            _state.ImeState = ImeState.Enabled;
            ImeContext.SetImeAllowed(_hwnd, true);
            ApplyImeRequestData(capabilities, requestData);
            return;
        }

        if (request.TryGetValue(out ImeRequest.Update update))
        {
            if (currentCapabilities is null)
            {
                throw new ImeRequestException(ImeRequestError.NotEnabled);
            }

            ApplyImeRequestData(currentCapabilities.Value, update.Value);
            return;
        }

        if (request.TryGetValue(out ImeRequest.Disable _))
        {
            if (currentCapabilities is null)
            {
                throw new ImeRequestException(ImeRequestError.NotEnabled);
            }

            _state.ImeCapabilities = null;
            _state.ImeState = ImeState.Disabled;
            ImeContext.SetImeAllowed(_hwnd, false);
        }
    }

    public void FocusWindow()
    {
        if (DispatchToEventLoopThread(FocusWindow))
        {
            return;
        }

        PInvoke.SetForegroundWindow(_hwnd);
    }

    public void RequestUserAttention(UserAttentionType? requestType)
    {
        if (DispatchToEventLoopThread(() => RequestUserAttention(requestType)))
        {
            return;
        }

        if (PInvoke.GetActiveWindow() == Util.HwndValue(_hwnd))
        {
            return;
        }

        uint flags = FlashwStop;
        uint count = 0;
        if (requestType == UserAttentionType.Critical)
        {
            flags = FlashwAll | FlashwTimerNoFg;
            count = uint.MaxValue;
        }
        else if (requestType == UserAttentionType.Informational)
        {
            flags = FlashwTray | FlashwTimerNoFg;
        }

        FlashWindowInfo flashInfo = new()
        {
            Size = (uint)Marshal.SizeOf<FlashWindowInfo>(),
            Window = Util.HwndValue(_hwnd),
            Flags = flags,
            Count = count,
            Timeout = 0,
        };
        PInvoke.FlashWindowEx(ref flashInfo);
    }

    public void SetTheme(Theme? theme)
    {
        if (DispatchToEventLoopThread(() => SetTheme(theme)))
        {
            return;
        }

        _state.PreferredTheme = theme;
        _state.CurrentTheme = DarkMode.TryTheme(_hwnd, theme, refreshTitleBar: true);
    }

    public void SetContentProtected(bool isProtected)
    {
        if (DispatchToEventLoopThread(() => SetContentProtected(isProtected)))
        {
            return;
        }

        PInvoke.SetWindowDisplayAffinity(_hwnd, isProtected ? WdaExcludeFromCapture : WdaNone);
    }

    public void SetCursor(Cursor cursor)
    {
        if (DispatchToEventLoopThread(() => SetCursor(cursor)))
        {
            return;
        }

        _state.Mouse.SelectedCursor = cursor;
        ApplyCursor();
    }

    public void SetCursorPosition(Position position)
    {
        if (DispatchToEventLoopThread(() => SetCursorPosition(position)))
        {
            return;
        }

        PhysicalPosition<int> physical = position.ToPhysical<int>(ScaleFactor);
        DrawingPoint point = new(physical.X, physical.Y);
        if (!PInvoke.ClientToScreen(_hwnd, ref point))
        {
            throw Win32Error.Request();
        }

        if (!PInvoke.SetCursorPos(point.X, point.Y))
        {
            throw Win32Error.Request();
        }
    }

    public void SetCursorGrab(CursorGrabMode mode)
    {
        if (DispatchToEventLoopThread(() => SetCursorGrab(mode)))
        {
            return;
        }

        _state.Mouse.SetCursorFlags(flags => flags
            .With(CursorFlags.Grabbed, mode != CursorGrabMode.None)
            .With(CursorFlags.Locked, mode == CursorGrabMode.Locked));
        RefreshCursorClip();
    }

    public void SetCursorVisible(bool visible)
    {
        if (DispatchToEventLoopThread(() => SetCursorVisible(visible)))
        {
            return;
        }

        _state.Mouse.SetCursorFlags(flags => visible ? flags & ~CursorFlags.Hidden : flags | CursorFlags.Hidden);
        ApplyCursor();
    }

    public void DragWindow()
    {
        if (DispatchToEventLoopThread(DragWindow))
        {
            return;
        }

        StartSystemMoveOrResize(HtCaption);
    }

    public void DragResizeWindow(ResizeDirection direction)
    {
        if (DispatchToEventLoopThread(() => DragResizeWindow(direction)))
        {
            return;
        }

        StartSystemMoveOrResize(HitTestForResizeDirection(direction));
    }

    public void ShowWindowMenu(Position position)
    {
        if (DispatchToEventLoopThread(() => ShowWindowMenu(position)))
        {
            return;
        }

        PhysicalPosition<int> physical = position.ToPhysical<int>(ScaleFactor);
        DrawingPoint point = new(physical.X, physical.Y);
        if (!PInvoke.ClientToScreen(_hwnd, ref point))
        {
            throw Win32Error.Request();
        }

        HMENU menu = PInvoke.GetSystemMenu(_hwnd, false);
        if (menu == HMENU.Null)
        {
            return;
        }

        PInvoke.EnableMenuItem(menu, ScRestore, MenuEnabled(IsMaximized && IsResizable));
        PInvoke.EnableMenuItem(menu, ScMove, MenuEnabled(!IsMaximized));
        PInvoke.EnableMenuItem(menu, ScSize, MenuEnabled(!IsMaximized && IsResizable));
        PInvoke.EnableMenuItem(menu, ScMinimize, MenuEnabled(true));
        PInvoke.EnableMenuItem(menu, ScMaximize, MenuEnabled(!IsMaximized && IsResizable));
        PInvoke.EnableMenuItem(menu, ScClose, MenuEnabled(true));
        PInvoke.SetMenuDefaultItem(menu, ScClose, 0);

        uint result = PInvoke.TrackPopupMenu(
            menu,
            TpmReturnCmd | TpmLeftAlign,
            point.X,
            point.Y,
            0,
            _hwnd,
            0);
        if (result != 0)
        {
            PInvoke.PostMessage(_hwnd, PInvoke.WM_SYSCOMMAND, new WPARAM(result), default);
        }
    }

    public void SetCursorHittest(bool hittest)
    {
        if (DispatchToEventLoopThread(() => SetCursorHittest(hittest)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.IgnoreCursorEvent, !hittest));
    }

    public void SetSkipTaskbar(bool skip)
    {
        if (DispatchToEventLoopThread(() => SetSkipTaskbar(skip)))
        {
            return;
        }

        _state.SkipTaskbar = skip;
        _state.SetWindowFlags(
            _hwnd,
            flags => flags.With(
                WindowFlags.OnTaskbar,
                !skip && !flags.HasFlag(WindowFlags.Child) && !flags.HasFlag(WindowFlags.Popup)));
        TaskbarList.SetSkipTaskbar(_hwnd, skip);
    }

    public void SetUndecoratedShadow(bool shadow)
    {
        if (DispatchToEventLoopThread(() => SetUndecoratedShadow(shadow)))
        {
            return;
        }

        _state.SetWindowFlags(_hwnd, flags => flags.With(WindowFlags.MarkerUndecoratedShadow, shadow));
    }

    public void SetSystemBackdrop(BackdropType backdropType)
    {
        if (DispatchToEventLoopThread(() => SetSystemBackdrop(backdropType)))
        {
            return;
        }

        SetDwmIntAttribute(DwmwaSystemBackdropType, (int)backdropType);
    }

    public void SetBorderColor(Color? color)
    {
        if (DispatchToEventLoopThread(() => SetBorderColor(color)))
        {
            return;
        }

        SetDwmUIntAttribute(DwmwaBorderColor, (color ?? Color.None).Value);
    }

    public void SetTitleBackgroundColor(Color? color)
    {
        if (DispatchToEventLoopThread(() => SetTitleBackgroundColor(color)))
        {
            return;
        }

        SetDwmUIntAttribute(DwmwaCaptionColor, (color ?? Color.None).Value);
    }

    public void SetTitleTextColor(Color color)
    {
        if (DispatchToEventLoopThread(() => SetTitleTextColor(color)))
        {
            return;
        }

        SetDwmUIntAttribute(DwmwaTextColor, color.Value);
    }

    public void SetCornerPreference(CornerPreference preference)
    {
        if (DispatchToEventLoopThread(() => SetCornerPreference(preference)))
        {
            return;
        }

        SetDwmIntAttribute(DwmwaWindowCornerPreference, (int)preference);
    }

    public void SetUseSystemScrollSpeed(bool shouldUse)
    {
        if (DispatchToEventLoopThread(() => SetUseSystemScrollSpeed(shouldUse)))
        {
            return;
        }

        _state.UseSystemWheelSpeed = shouldUse;
    }

    public void Dispose()
    {
        if (DispatchToEventLoopThread(Dispose))
        {
            return;
        }

        if (_disposed)
        {
            return;
        }

        if (_state.Fullscreen?.TryGetValue(out Fullscreen.Exclusive _) == true)
        {
            SetFullscreen(null);
        }

        _fileDropTarget?.Dispose();
        _fileDropTarget = null;
        _disposed = true;
        s_windows.TryRemove(Util.HwndValue(_hwnd), out _);
        PInvoke.DestroyWindow(_hwnd);
        _hwnd = HWND.Null;
    }

    private RECT ClientRect()
    {
        if (!PInvoke.GetClientRect(_hwnd, out RECT rect))
        {
            throw Win32Error.Request();
        }

        return rect;
    }

    private RECT WindowRect()
    {
        if (!PInvoke.GetWindowRect(_hwnd, out RECT rect))
        {
            throw Win32Error.Request();
        }

        return rect;
    }

    private void ApplyDisplayModeChange(Fullscreen? oldFullscreen, Fullscreen? newFullscreen)
    {
        if (newFullscreen?.TryGetValue(out Fullscreen.Exclusive exclusive) == true)
        {
            SetExclusiveDisplayMode(exclusive);
        }
        else if (oldFullscreen?.TryGetValue(out Fullscreen.Exclusive _) == true)
        {
            int result = PInvoke.ChangeDisplaySettingsExW(null, nint.Zero, HWND.Null, CdsFullscreen, nint.Zero);
            if (result != DispChangeSuccessful)
            {
                throw Win32Error.Request();
            }
        }
    }

    private void SetExclusiveDisplayMode(Fullscreen.Exclusive exclusive)
    {
        MonitorHandle monitor = Win32Monitor(exclusive.Monitor);
        string deviceName = monitor.Name ?? throw Win32Error.Request();
        DEVMODEW mode = new()
        {
            dmSize = (ushort)Marshal.SizeOf<DEVMODEW>(),
            dmFields = (DEVMODE_FIELD_FLAGS)(DmPelsWidth | DmPelsHeight),
            dmPelsWidth = exclusive.VideoMode.Size.Width,
            dmPelsHeight = exclusive.VideoMode.Size.Height,
        };

        if (exclusive.VideoMode.BitDepth is { } bitDepth)
        {
            mode.dmFields |= (DEVMODE_FIELD_FLAGS)DmBitsPerPel;
            mode.dmBitsPerPel = bitDepth;
        }

        if (exclusive.VideoMode.RefreshRateMillihertz is { } refreshRate)
        {
            mode.dmFields |= (DEVMODE_FIELD_FLAGS)DmDisplayFrequency;
            mode.dmDisplayFrequency = Math.Max(1u, refreshRate / 1000u);
        }

        int result = PInvoke.ChangeDisplaySettingsExW(deviceName, ref mode, HWND.Null, CdsFullscreen, 0);
        if (result != DispChangeSuccessful)
        {
            throw Win32Error.Request();
        }
    }

    private bool IsCurrentBorderlessFullscreen(Fullscreen? oldFullscreen, Fullscreen? newFullscreen)
    {
        if (oldFullscreen?.TryGetValue(out Fullscreen.Borderless oldBorderless) != true ||
            oldBorderless.Monitor is null ||
            newFullscreen?.TryGetValue(out Fullscreen.Borderless newBorderless) != true ||
            newBorderless.Monitor is not null)
        {
            return false;
        }

        MonitorHandle oldMonitor = Win32Monitor(oldBorderless.Monitor);
        MonitorHandle currentMonitor = CurrentWin32Monitor();
        return oldMonitor.NativeId == currentMonitor.NativeId;
    }

    private RECT MonitorRectForFullscreen(Fullscreen fullscreen)
    {
        MonitorHandle monitor;
        if (fullscreen.TryGetValue(out Fullscreen.Exclusive exclusive))
        {
            monitor = Win32Monitor(exclusive.Monitor);
        }
        else if (fullscreen.TryGetValue(out Fullscreen.Borderless borderless) &&
            borderless.Monitor is { } requestedMonitor)
        {
            monitor = Win32Monitor(requestedMonitor);
        }
        else
        {
            monitor = CurrentWin32Monitor();
        }

        MONITORINFOEXW? info = Monitor.GetMonitorInfo(monitor.Raw);
        if (info is not { } value)
        {
            throw Win32Error.Request();
        }

        return value.monitorInfo.rcMonitor;
    }

    private void SaveWindowPlacement()
    {
        if (_state.SavedWindow is not null)
        {
            return;
        }

        WindowPlacement placement = new()
        {
            Length = (uint)Marshal.SizeOf<WindowPlacement>(),
        };
        if (!PInvoke.GetWindowPlacement(_hwnd, ref placement))
        {
            throw Win32Error.Request();
        }

        _state.SavedWindow = new SavedWindow(placement);
    }

    private MonitorHandle CurrentWin32Monitor()
    {
        CoreMonitorHandle currentMonitor = Monitor.CurrentMonitor(_hwnd);
        return Win32Monitor(currentMonitor);
    }

    private static MonitorHandle Win32Monitor(CoreMonitorHandle monitor)
    {
        return monitor.Provider.AsAny() as MonitorHandle
            ?? throw new NotSupportedRequestException("monitor handle is not owned by the Win32 backend");
    }

    private void SetOuterSizeForSurface(PhysicalSize<uint> surfaceSize)
    {
        PhysicalSize<uint> outerSize = AdjustedOuterSizeForSurface(surfaceSize);

        if (!PInvoke.SetWindowPos(
            _hwnd,
            HWND.Null,
            0,
            0,
            checked((int)outerSize.Width),
            checked((int)outerSize.Height),
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
        {
            throw Win32Error.Request();
        }
    }

    private PhysicalSize<uint> AdjustedOuterSizeForSurface(PhysicalSize<uint> surfaceSize)
    {
        (WINDOW_STYLE style, WINDOW_EX_STYLE exStyle) = _state.WindowFlags.ToWindowStyles();
        RECT rect = new()
        {
            left = 0,
            top = 0,
            right = checked((int)surfaceSize.Width),
            bottom = checked((int)surfaceSize.Height),
        };

        if (!PInvoke.AdjustWindowRectEx(ref rect, style, false, exStyle))
        {
            throw Win32Error.Request();
        }

        return new PhysicalSize<uint>(
            checked((uint)(rect.right - rect.left)),
            checked((uint)(rect.bottom - rect.top)));
    }

    private void ApplyCursor()
    {
        if (_state.Mouse.CursorFlags.HasFlag(CursorFlags.Hidden))
        {
            PInvoke.SetCursor(HCURSOR.Null);
            return;
        }

        PInvoke.SetCursor(SelectedCursorHandle());
    }

    private void ApplyPlatformVisualAttributes()
    {
        SetSkipTaskbar(_platformAttributes.SkipTaskbar);
        SetUndecoratedShadow(_platformAttributes.DecorationShadow);
        SetSystemBackdrop(_platformAttributes.BackdropType);

        if (_platformAttributes.BorderColor is { } borderColor)
        {
            SetBorderColor(borderColor);
        }

        if (_platformAttributes.TitleBackgroundColor is { } titleBackgroundColor)
        {
            SetTitleBackgroundColor(titleBackgroundColor);
        }

        if (_platformAttributes.TitleTextColor is { } titleTextColor)
        {
            SetTitleTextColor(titleTextColor);
        }

        if (_platformAttributes.CornerPreference is { } cornerPreference)
        {
            SetCornerPreference(cornerPreference);
        }

        SetUseSystemScrollSpeed(_platformAttributes.UseSystemWheelSpeed);
    }

    private void ApplyImeRequestData(ImeCapabilities capabilities, ImeRequestData requestData)
    {
        if (capabilities.CursorArea() && requestData.CursorArea is { } cursorArea)
        {
            using ImeContext context = ImeContext.Current(_hwnd);
            context.SetImeCursorArea(cursorArea.Position, cursorArea.Size, ScaleFactor);
        }
    }

    private void HandleImeStartComposition()
    {
        if (_state.ImeCapabilities is null)
        {
            return;
        }

        _state.ImeState = ImeState.Enabled;
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Enabled()))));
    }

    private void HandleImeComposition(LPARAM lParam)
    {
        if (_state.ImeCapabilities is null || _state.ImeState == ImeState.Disabled)
        {
            return;
        }

        using ImeContext context = ImeContext.Current(_hwnd);
        if (lParam.Value == 0)
        {
            SendImePreedit(string.Empty, null);
        }

        uint flags = unchecked((uint)lParam.Value);
        if ((flags & GcsResultStr) != 0)
        {
            string? text = context.GetComposedText();
            if (text is not null)
            {
                _state.ImeState = ImeState.Enabled;
                SendImePreedit(string.Empty, null);
                _eventLoop.SendWindowEvent(
                    Id,
                    new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Commit(text)))));
            }
        }

        if ((flags & GcsCompStr) != 0)
        {
            (string Text, nuint? First, nuint? Last)? composing = context.GetComposingTextAndCursor();
            if (composing is { } value)
            {
                _state.ImeState = ImeState.Preedit;
                (nuint Begin, nuint End)? cursorRange = value.First is { } first
                    ? (first, value.Last ?? first)
                    : null;
                SendImePreedit(value.Text, cursorRange);
            }
        }
    }

    private void HandleImeEndComposition()
    {
        if (_state.ImeCapabilities is null && _state.ImeState == ImeState.Disabled)
        {
            return;
        }

        if (_state.ImeState == ImeState.Preedit)
        {
            using ImeContext context = ImeContext.Current(_hwnd);
            string? text = context.GetComposedText();
            if (text is not null)
            {
                SendImePreedit(string.Empty, null);
                _eventLoop.SendWindowEvent(
                    Id,
                    new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Commit(text)))));
            }
        }

        _state.ImeState = ImeState.Disabled;
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Disabled()))));
    }

    private void SendImePreedit(string text, (nuint Begin, nuint End)? cursorRange)
    {
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Preedit(text, cursorRange)))));
    }

    private void StartSystemMoveOrResize(nuint hitTest)
    {
        if (_state.Dragging)
        {
            return;
        }

        _state.Dragging = true;
        nint cursorLParam = 0;
        if (PInvoke.GetCursorPos(out DrawingPoint cursor))
        {
            cursorLParam = Util.MakeLParam(cursor.X, cursor.Y);
        }

        PInvoke.ReleaseCapture();
        PInvoke.PostMessage(_hwnd, PInvoke.WM_NCLBUTTONDOWN, new WPARAM(hitTest), new LPARAM(cursorLParam));
    }

    private void UpdateModifiers()
    {
        Modifiers modifiers = Keyboard.CurrentModifiers();
        if (_state.ModifiersState == modifiers.State && _state.PressedModifiers == modifiers.PressedMods)
        {
            return;
        }

        _state.ModifiersState = modifiers.State;
        _state.PressedModifiers = modifiers.PressedMods;
        _eventLoop.SendWindowEvent(Id, new WindowEvent(new WindowEvent.ModifiersChanged(modifiers)));
    }

    private void ClearModifiers()
    {
        if (_state.ModifiersState == ModifiersState.None && _state.PressedModifiers == ModifiersKeys.None)
        {
            return;
        }

        _state.ModifiersState = ModifiersState.None;
        _state.PressedModifiers = ModifiersKeys.None;
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.ModifiersChanged(new Modifiers(ModifiersState.None, ModifiersKeys.None))));
    }

    private void GainActiveFocus()
    {
        UpdateModifiers();
        RefreshCursorClip();
        _eventLoop.SendWindowEvent(Id, new WindowEvent(new WindowEvent.Focused(true)));
    }

    private void LoseActiveFocus()
    {
        ClearModifiers();
        RefreshCursorClip();
        _eventLoop.SendWindowEvent(Id, new WindowEvent(new WindowEvent.Focused(false)));
    }

    private bool ProcessKeyboardMessage(uint message, WPARAM wParam, LPARAM lParam, out LRESULT result)
    {
        if (Keyboard.IsKeyMessage(message))
        {
            UpdateModifiers();
        }

        IReadOnlyList<MessageAsKeyEvent> events = _state.KeyEventBuilder.ProcessMessage(
            _hwnd,
            message,
            wParam,
            lParam,
            out bool handled);
        foreach (MessageAsKeyEvent keyEvent in events)
        {
            _eventLoop.SendWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.KeyboardInput(null, keyEvent.Event, keyEvent.IsSynthetic)));
        }

        if (_state.ImeCapabilities is null)
        {
            string? imeText = _state.MinimalIme.ProcessMessage(_hwnd, message, wParam, out bool imeHandled);
            handled |= imeHandled;
            if (imeText is not null)
            {
                _eventLoop.SendWindowEvent(
                    Id,
                    new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Commit(imeText)))));
            }
        }
        else
        {
            _state.MinimalIme.Reset();
        }

        if (!handled)
        {
            result = default;
            return false;
        }

        result = message is PInvoke.WM_SYSKEYDOWN or PInvoke.WM_SYSKEYUP
            ? PInvoke.DefWindowProc(_hwnd, message, wParam, lParam)
            : new LRESULT(0);
        return true;
    }

    private void HandlePointerMove(LPARAM lParam)
    {
        PhysicalPosition<double> position = ClientPositionFromLParam(lParam);
        PointerMoveKind moveKind = GetPointerMoveKind(position);

        switch (moveKind)
        {
            case PointerMoveKind.Enter:
                _state.Mouse.SetCursorFlags(flags => flags | CursorFlags.InWindow);
                TrackMouseLeave();
                _eventLoop.SendWindowEvent(
                    Id,
                    new WindowEvent(new WindowEvent.PointerEntered(
                        null,
                        position,
                        true,
                        new PointerKind(new PointerKind.Mouse()))));
                break;
            case PointerMoveKind.Leave:
                _state.Mouse.SetCursorFlags(flags => flags & ~CursorFlags.InWindow);
                _eventLoop.SendWindowEvent(
                    Id,
                    new WindowEvent(new WindowEvent.PointerLeft(
                        null,
                        position,
                        true,
                        new PointerKind(new PointerKind.Mouse()))));
                break;
        }

        bool cursorMoved = _state.Mouse.LastPosition != position;
        _state.Mouse.LastPosition = position;
        if (!cursorMoved)
        {
            return;
        }

        UpdateModifiers();
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.PointerMoved(
                null,
                position,
                true,
                new PointerSource(new PointerSource.Mouse()))));
    }

    private void HandlePointerLeave()
    {
        _state.Mouse.SetCursorFlags(flags => flags & ~CursorFlags.InWindow);
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.PointerLeft(
                null,
                null,
                true,
                new PointerKind(new PointerKind.Mouse()))));
    }

    private void HandlePointerButton(LPARAM lParam, ElementState state, MouseButton button)
    {
        if (state == ElementState.Pressed)
        {
            CaptureMouse();
        }
        else
        {
            ReleaseMouse();
        }

        UpdateModifiers();
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.PointerButton(
                null,
                state,
                ClientPositionFromLParam(lParam),
                true,
                new ButtonSource(new ButtonSource.Mouse(button)))));
    }

    private void HandleTouch(WPARAM wParam, LPARAM lParam)
    {
        uint inputCount = Util.LowWord(unchecked((nint)wParam.Value));
        if (inputCount == 0)
        {
            return;
        }

        TouchInput[] inputs = new TouchInput[inputCount];
        try
        {
            fixed (TouchInput* inputsPtr = inputs)
            {
                if (!PInvoke.GetTouchInputInfo(
                    lParam.Value,
                    inputCount,
                    inputsPtr,
                    Marshal.SizeOf<TouchInput>()))
                {
                    return;
                }
            }

            foreach (TouchInput input in inputs)
            {
                NativePoint point = new()
                {
                    X = input.X / 100,
                    Y = input.Y / 100,
                };
                if (!PInvoke.ScreenToClient(_hwnd, ref point))
                {
                    continue;
                }

                double x = point.X + input.X % 100 / 100.0;
                double y = point.Y + input.Y % 100 / 100.0;
                PhysicalPosition<double> position = new(x, y);
                FingerId fingerId = FingerId.FromRaw(input.Id);
                bool primary = (input.Flags & TouchEventPrimary) != 0;

                if ((input.Flags & TouchEventDown) != 0)
                {
                    _eventLoop.SendWindowEvent(
                        Id,
                        new WindowEvent(new WindowEvent.PointerEntered(
                            null,
                            position,
                            primary,
                            new PointerKind(new PointerKind.Touch(fingerId)))));
                    _eventLoop.SendWindowEvent(
                        Id,
                        new WindowEvent(new WindowEvent.PointerButton(
                            null,
                            ElementState.Pressed,
                            position,
                            primary,
                            new ButtonSource(new ButtonSource.Touch(fingerId, null)))));
                }
                else if ((input.Flags & TouchEventUp) != 0)
                {
                    _eventLoop.SendWindowEvent(
                        Id,
                        new WindowEvent(new WindowEvent.PointerButton(
                            null,
                            ElementState.Released,
                            position,
                            primary,
                            new ButtonSource(new ButtonSource.Touch(fingerId, null)))));
                    _eventLoop.SendWindowEvent(
                        Id,
                        new WindowEvent(new WindowEvent.PointerLeft(
                            null,
                            position,
                            primary,
                            new PointerKind(new PointerKind.Touch(fingerId)))));
                }
                else if ((input.Flags & TouchEventMove) != 0)
                {
                    _eventLoop.SendWindowEvent(
                        Id,
                        new WindowEvent(new WindowEvent.PointerMoved(
                            null,
                            position,
                            primary,
                            new PointerSource(new PointerSource.Touch(fingerId, null)))));
                }
            }
        }
        finally
        {
            PInvoke.CloseTouchInputHandle(lParam.Value);
        }
    }

    private void HandlePointerMessage(uint message, WPARAM wParam)
    {
        uint pointerId = Util.LowWord(unchecked((nint)wParam.Value));
        if (TryDispatchPointerFrameHistory(pointerId))
        {
            return;
        }

        if (!PInvoke.GetPointerInfo(pointerId, out PointerInfo pointerInfo))
        {
            return;
        }

        if (!TryPointerPixelPosition(pointerInfo, out PhysicalPosition<double> position))
        {
            return;
        }

        DispatchPointerInfo(message, pointerInfo, position, useMessageKind: true);
    }

    private unsafe bool TryDispatchPointerFrameHistory(uint pointerId)
    {
        try
        {
            uint entriesCount = 0;
            uint pointerCount = 0;
            if (!PInvoke.GetPointerFrameInfoHistory(pointerId, ref entriesCount, ref pointerCount, null))
            {
                return false;
            }

            ulong totalCount = (ulong)entriesCount * pointerCount;
            if (totalCount == 0 || totalCount > int.MaxValue)
            {
                return false;
            }

            PointerInfo[] pointerInfos = new PointerInfo[totalCount];
            fixed (PointerInfo* pointerInfosPtr = pointerInfos)
            {
                if (!PInvoke.GetPointerFrameInfoHistory(pointerId, ref entriesCount, ref pointerCount, pointerInfosPtr))
                {
                    return false;
                }
            }

            for (int i = pointerInfos.Length - 1; i >= 0; i--)
            {
                PointerInfo pointerInfo = pointerInfos[i];
                if (!TryPointerHimetricPosition(pointerInfo, out PhysicalPosition<double> position))
                {
                    continue;
                }

                DispatchPointerInfo(0, pointerInfo, position, useMessageKind: false);
            }

            _ = PInvoke.SkipPointerFrameMessages(pointerId);
            return true;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private void DispatchPointerInfo(
        uint message,
        PointerInfo pointerInfo,
        PhysicalPosition<double> position,
        bool useMessageKind)
    {
        bool primary = (pointerInfo.PointerFlags & PointerFlagPrimary) != 0;
        PointerKind kind = PointerKindFromPointerInfo(pointerInfo);

        bool isDown = (pointerInfo.PointerFlags & PointerFlagDown) != 0 ||
            useMessageKind && message == PInvoke.WM_POINTERDOWN;
        bool isUp = (pointerInfo.PointerFlags & PointerFlagUp) != 0 ||
            useMessageKind && message == PInvoke.WM_POINTERUP;
        if (isDown)
        {
            _eventLoop.SendWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.PointerEntered(null, position, primary, kind)));
            _eventLoop.SendWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.PointerButton(
                    null,
                    ElementState.Pressed,
                    position,
                    primary,
                    ButtonSourceFromPointerInfo(pointerInfo, isDown: true))));
        }
        else if (isUp)
        {
            _eventLoop.SendWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.PointerButton(
                    null,
                    ElementState.Released,
                    position,
                    primary,
                    ButtonSourceFromPointerInfo(pointerInfo, isDown: false))));
            _eventLoop.SendWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.PointerLeft(null, position, primary, kind)));
        }
        else if (message == PInvoke.WM_POINTERUPDATE || (pointerInfo.PointerFlags & PointerFlagUpdate) != 0)
        {
            _eventLoop.SendWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.PointerMoved(
                    null,
                    position,
                    primary,
                    PointerSourceFromPointerInfo(pointerInfo))));
        }
    }

    private bool TryPointerPixelPosition(PointerInfo pointerInfo, out PhysicalPosition<double> position)
    {
        NativePoint point = pointerInfo.PixelLocation;
        if (!PInvoke.ScreenToClient(_hwnd, ref point))
        {
            position = default;
            return false;
        }

        position = new PhysicalPosition<double>(point.X, point.Y);
        return true;
    }

    private bool TryPointerHimetricPosition(PointerInfo pointerInfo, out PhysicalPosition<double> position)
    {
        if (!PInvoke.GetPointerDeviceRects(pointerInfo.SourceDevice, out RECT deviceRect, out RECT displayRect))
        {
            position = default;
            return false;
        }

        int deviceWidth = deviceRect.right - deviceRect.left;
        int deviceHeight = deviceRect.bottom - deviceRect.top;
        if (deviceWidth == 0 || deviceHeight == 0)
        {
            position = default;
            return false;
        }

        double himetricToPixelRatioX = (displayRect.right - displayRect.left) / (double)deviceWidth;
        double himetricToPixelRatioY = (displayRect.bottom - displayRect.top) / (double)deviceHeight;
        double x = displayRect.left + pointerInfo.HimetricLocation.X * himetricToPixelRatioX;
        double y = displayRect.top + pointerInfo.HimetricLocation.Y * himetricToPixelRatioY;
        NativePoint point = new()
        {
            X = (int)Math.Floor(x),
            Y = (int)Math.Floor(y),
        };

        if (!PInvoke.ScreenToClient(_hwnd, ref point))
        {
            position = default;
            return false;
        }

        position = new PhysicalPosition<double>(
            point.X + FractionalPart(x),
            point.Y + FractionalPart(y));
        return true;
    }

    private void HandleMouseWheel(WPARAM wParam, bool horizontal)
    {
        short wheelDelta = Util.SignedHighWord(unchecked((nint)wParam.Value));
        float value = wheelDelta / (float)PInvoke.WHEEL_DELTA;
        if (horizontal)
        {
            value = -value;
        }

        value *= ScrollWheelMultiplier(horizontal);
        UpdateModifiers();
        MouseScrollDelta delta = horizontal
            ? new MouseScrollDelta(new MouseScrollDelta.LineDelta(value, 0.0f))
            : new MouseScrollDelta(new MouseScrollDelta.LineDelta(0.0f, value));
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.MouseWheel(null, delta, TouchPhase.Moved)));
    }

    private float ScrollWheelMultiplier(bool horizontal)
    {
        if (!_state.UseSystemWheelSpeed)
        {
            return 1.0f;
        }

        uint value = horizontal ? DefaultScrollCharsPerWheelDelta : DefaultScrollLinesPerWheelDelta;
        uint action = horizontal ? SpiGetWheelScrollChars : SpiGetWheelScrollLines;
        if (!PInvoke.SystemParametersInfoW(action, 0, ref value, 0))
        {
            return horizontal ? DefaultScrollCharsPerWheelDelta : DefaultScrollLinesPerWheelDelta;
        }

        if (!horizontal && value == WheelPageScroll)
        {
            return DefaultScrollLinesPerWheelDelta;
        }

        return value;
    }

    private void CaptureMouse()
    {
        _state.Mouse.CaptureCount++;
        PInvoke.SetCapture(_hwnd);
    }

    private void ReleaseMouse()
    {
        if (_state.Mouse.CaptureCount > 0)
        {
            _state.Mouse.CaptureCount--;
        }

        if (_state.Mouse.CaptureCount == 0)
        {
            PInvoke.ReleaseCapture();
        }
    }

    private void RefreshCursorClip()
    {
        if (PInvoke.GetActiveWindow() != Util.HwndValue(_hwnd) || !_state.Mouse.CursorFlags.HasFlag(CursorFlags.Grabbed))
        {
            PInvoke.ClipCursor(null);
            return;
        }

        RECT clipRect;
        if (_state.Mouse.CursorFlags.HasFlag(CursorFlags.Locked))
        {
            if (!PInvoke.GetCursorPos(out DrawingPoint cursor))
            {
                RECT client = ClientRect();
                DrawingPoint center = new((client.right - client.left) / 2, (client.bottom - client.top) / 2);
                if (!PInvoke.ClientToScreen(_hwnd, ref center))
                {
                    throw Win32Error.Request();
                }

                cursor = center;
            }

            clipRect = new RECT
            {
                left = cursor.X,
                top = cursor.Y,
                right = cursor.X + 1,
                bottom = cursor.Y + 1,
            };
        }
        else
        {
            clipRect = ClientRect();
            DrawingPoint topLeft = new(clipRect.left, clipRect.top);
            DrawingPoint bottomRight = new(clipRect.right, clipRect.bottom);
            if (!PInvoke.ClientToScreen(_hwnd, ref topLeft) ||
                !PInvoke.ClientToScreen(_hwnd, ref bottomRight))
            {
                throw Win32Error.Request();
            }

            clipRect = new RECT
            {
                left = topLeft.X,
                top = topLeft.Y,
                right = bottomRight.X,
                bottom = bottomRight.Y,
            };
        }

        if (!PInvoke.ClipCursor(&clipRect))
        {
            throw Win32Error.Request();
        }
    }

    private void TrackMouseLeave()
    {
        TrackMouseEventData trackMouseEvent = new()
        {
            Size = (uint)Marshal.SizeOf<TrackMouseEventData>(),
            Flags = TmeLeave,
            Window = Util.HwndValue(_hwnd),
            HoverTime = HoverDefault,
        };

        PInvoke.TrackMouseEvent(ref trackMouseEvent);
    }

    private PointerMoveKind GetPointerMoveKind(PhysicalPosition<double> position)
    {
        bool wasInside = _state.Mouse.CursorFlags.HasFlag(CursorFlags.InWindow);
        RECT client = ClientRect();
        bool isInside =
            position.X >= client.left &&
            position.Y >= client.top &&
            position.X < client.right &&
            position.Y < client.bottom;

        return (wasInside, isInside) switch
        {
            (false, true) => PointerMoveKind.Enter,
            (true, false) => PointerMoveKind.Leave,
            _ => PointerMoveKind.None,
        };
    }

    private PhysicalPosition<double> ClientPositionFromLParam(LPARAM lParam)
    {
        return new PhysicalPosition<double>(
            Util.SignedLowWord(lParam.Value),
            Util.SignedHighWord(lParam.Value));
    }

    private void ApplyMinMaxInfo(nint lParam)
    {
        MINMAXINFO* minMaxInfo = (MINMAXINFO*)lParam;
        if (_state.MinSize is { } minSize)
        {
            PhysicalSize<uint> outerSize = AdjustedOuterSizeForSurface(minSize.ToPhysical<uint>(ScaleFactor));
            minMaxInfo->ptMinTrackSize.X = checked((int)outerSize.Width);
            minMaxInfo->ptMinTrackSize.Y = checked((int)outerSize.Height);
        }

        if (_state.MaxSize is { } maxSize)
        {
            PhysicalSize<uint> outerSize = AdjustedOuterSizeForSurface(maxSize.ToPhysical<uint>(ScaleFactor));
            minMaxInfo->ptMaxTrackSize.X = checked((int)outerSize.Width);
            minMaxInfo->ptMaxTrackSize.Y = checked((int)outerSize.Height);
        }
    }

    private bool HandleNcCalcSize(WPARAM wParam, LPARAM lParam)
    {
        if (wParam.Value == 0 || _state.WindowFlags.HasFlag(WindowFlags.MarkerDecorations))
        {
            return false;
        }

        NCCALCSIZE_PARAMS* parameters = (NCCALCSIZE_PARAMS*)lParam.Value;
        if (PInvoke.IsZoomed(_hwnd))
        {
            RECT rect = parameters->rgrc0;
            HMONITOR monitor = PInvoke.MonitorFromRect(ref rect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);
            if (monitor.Value != 0)
            {
                MONITORINFO info = new()
                {
                    cbSize = (uint)Marshal.SizeOf<MONITORINFO>(),
                };
                if (PInvoke.GetMonitorInfo(monitor, &info))
                {
                    parameters->rgrc0 = info.rcWork;
                }
            }
        }
        else if (_state.WindowFlags.HasFlag(WindowFlags.MarkerUndecoratedShadow))
        {
            parameters->rgrc0.top += 1;
            parameters->rgrc0.bottom += 1;
        }

        return true;
    }

    private void HandleEnterSizeMove()
    {
        _state.SetWindowFlagsInPlace(flags => flags | WindowFlags.MarkerInSizeMove);
    }

    private void HandleExitSizeMove(LPARAM lParam)
    {
        if (_state.Dragging)
        {
            _state.Dragging = false;
            PInvoke.PostMessage(_hwnd, PInvoke.WM_LBUTTONUP, default, lParam);
        }

        _state.SetWindowFlagsInPlace(flags => flags & ~WindowFlags.MarkerInSizeMove);
    }

    private void HandleWindowPosChanging(LPARAM lParam)
    {
        if (_state.Fullscreen is not { } fullscreen)
        {
            return;
        }

        WINDOWPOS* windowPos = (WINDOWPOS*)lParam.Value;
        RECT newRect = new()
        {
            left = windowPos->x,
            top = windowPos->y,
            right = windowPos->x + windowPos->cx,
            bottom = windowPos->y + windowPos->cy,
        };

        const SET_WINDOW_POS_FLAGS noMoveOrNoSize =
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
        if ((windowPos->flags & noMoveOrNoSize) != 0)
        {
            RECT current = WindowRect();
            if ((windowPos->flags & noMoveOrNoSize) == noMoveOrNoSize)
            {
                return;
            }

            if ((windowPos->flags & SET_WINDOW_POS_FLAGS.SWP_NOMOVE) != 0)
            {
                newRect.left = current.left;
                newRect.top = current.top;
                newRect.right = current.left + windowPos->cx;
                newRect.bottom = current.top + windowPos->cy;
            }
            else if ((windowPos->flags & SET_WINDOW_POS_FLAGS.SWP_NOSIZE) != 0)
            {
                newRect.right = windowPos->x - current.left + current.right;
                newRect.bottom = windowPos->y - current.top + current.bottom;
            }
        }

        HMONITOR newMonitor = PInvoke.MonitorFromRect(ref newRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);
        if (newMonitor.Value == 0)
        {
            return;
        }

        if (fullscreen.TryGetValue(out Fullscreen.Borderless borderless))
        {
            bool monitorChanged = borderless.Monitor is null ||
                Win32Monitor(borderless.Monitor).Raw != newMonitor;
            if (!monitorChanged || Monitor.GetMonitorInfo(newMonitor) is not { } info)
            {
                return;
            }

            RECT monitorRect = info.monitorInfo.rcMonitor;
            windowPos->x = monitorRect.left;
            windowPos->y = monitorRect.top;
            windowPos->cx = monitorRect.right - monitorRect.left;
            windowPos->cy = monitorRect.bottom - monitorRect.top;
            _state.Fullscreen = Winit.Core.Fullscreen.FromBorderless(new CoreMonitorHandle(new MonitorHandle(newMonitor)));
        }
        else if (fullscreen.TryGetValue(out Fullscreen.Exclusive exclusive))
        {
            MonitorHandle monitor = Win32Monitor(exclusive.Monitor);
            if (Monitor.GetMonitorInfo(monitor.Raw) is not { } info)
            {
                return;
            }

            RECT monitorRect = info.monitorInfo.rcMonitor;
            windowPos->x = monitorRect.left;
            windowPos->y = monitorRect.top;
            windowPos->cx = monitorRect.right - monitorRect.left;
            windowPos->cy = monitorRect.bottom - monitorRect.top;
        }
    }

    private void HandleSizing(WPARAM wParam, LPARAM lParam)
    {
        PhysicalSize<int>? increments = _state.SurfaceResizeIncrements?.ToPhysical<int>(ScaleFactor);
        if (increments is not { } inc || inc.Width <= 0 || inc.Height <= 0)
        {
            return;
        }

        RECT* rect = (RECT*)lParam.Value;
        RECT adjusted = *rect;
        (WINDOW_STYLE style, WINDOW_EX_STYLE exStyle) = _state.WindowFlags.ToWindowStyles();
        if (!PInvoke.AdjustWindowRectEx(ref adjusted, style, false, exStyle))
        {
            return;
        }

        int decorationWidth = rect->left - adjusted.left + adjusted.right - rect->right;
        int decorationHeight = rect->top - adjusted.top + adjusted.bottom - rect->bottom;
        int width = rect->right - rect->left - decorationWidth;
        int height = rect->bottom - rect->top - decorationHeight;
        int widthDelta = SnapToNearestIncrementDelta(width, inc.Width);
        int heightDelta = SnapToNearestIncrementDelta(height, inc.Height);

        PhysicalSize<int>? minSize = _state.MinSize?.ToPhysical<int>(ScaleFactor);
        PhysicalSize<int>? maxSize = _state.MaxSize?.ToPhysical<int>(ScaleFactor);
        int finalWidth = width + widthDelta;
        int finalHeight = height + heightDelta;
        if (minSize is { } min)
        {
            if (finalWidth < min.Width)
            {
                widthDelta += min.Width - finalWidth;
            }

            if (finalHeight < min.Height)
            {
                heightDelta += min.Height - finalHeight;
            }
        }

        if (maxSize is { } max)
        {
            if (finalWidth > max.Width)
            {
                widthDelta -= finalWidth - max.Width;
            }

            if (finalHeight > max.Height)
            {
                heightDelta -= finalHeight - max.Height;
            }
        }

        switch (wParam.Value)
        {
            case WmszLeft:
            case WmszBottomLeft:
            case WmszTopLeft:
                rect->left -= widthDelta;
                break;
            case WmszRight:
            case WmszBottomRight:
            case WmszTopRight:
                rect->right += widthDelta;
                break;
        }

        switch (wParam.Value)
        {
            case WmszTop:
            case WmszTopLeft:
            case WmszTopRight:
                rect->top -= heightDelta;
                break;
            case WmszBottom:
            case WmszBottomLeft:
            case WmszBottomRight:
                rect->bottom += heightDelta;
                break;
        }
    }

    private void HandleDpiChanged(WPARAM wParam, LPARAM lParam)
    {
        double oldScaleFactor = _state.ScaleFactor;
        uint dpi = Util.LowWord(unchecked((nint)wParam.Value));
        double newScaleFactor = Dpi.DpiToScaleFactor(dpi);
        _state.ScaleFactor = newScaleFactor;
        if (oldScaleFactor.Equals(newScaleFactor))
        {
            return;
        }

        RECT suggested = *(RECT*)lParam.Value;
        PhysicalSize<uint> oldSurfaceSize = SurfaceSize;
        PhysicalSize<uint> newSurfaceSize = _state.Fullscreen is null && !_state.WindowFlags.HasFlag(WindowFlags.Maximized)
            ? oldSurfaceSize.ToLogical<double>(oldScaleFactor).ToPhysical<uint>(newScaleFactor)
            : oldSurfaceSize;
        SurfaceSizeState state = new(newSurfaceSize);
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.ScaleFactorChanged(newScaleFactor, SurfaceSizeWriter.Create(state))));

        PhysicalSize<uint> requestedSurfaceSize = state.SurfaceSize;
        if (requestedSurfaceSize != newSurfaceSize)
        {
            SetOuterSizeForSurface(requestedSurfaceSize);
            return;
        }

        if (!PInvoke.SetWindowPos(
            _hwnd,
            HWND.Null,
            suggested.left,
            suggested.top,
            suggested.right - suggested.left,
            suggested.bottom - suggested.top,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
        {
            throw Win32Error.Request();
        }
    }

    private void HandleWindowPosChanged(LPARAM lParam)
    {
        WINDOWPOS* windowPos = (WINDOWPOS*)lParam.Value;
        RefreshCursorClip();
        if ((windowPos->flags & SET_WINDOW_POS_FLAGS.SWP_NOMOVE) == 0)
        {
            _eventLoop.SendWindowEvent(
                Id,
                new WindowEvent(new WindowEvent.Moved(new PhysicalPosition<int>(windowPos->x, windowPos->y))));
        }
    }

    private void HandleSize(WPARAM wParam, LPARAM lParam)
    {
        PhysicalSize<uint> physicalSize = new(Util.LowWord(lParam.Value), Util.HighWord(lParam.Value));
        _state.SetWindowFlagsInPlace(flags => flags.With(WindowFlags.Maximized, wParam.Value == SizeMaximized));
        RefreshCursorClip();
        if (physicalSize.Width == 0 && physicalSize.Height == 0 || physicalSize == _state.SurfaceSize)
        {
            return;
        }

        _state.SurfaceSize = physicalSize;
        _eventLoop.SendWindowEvent(
            Id,
            new WindowEvent(new WindowEvent.SurfaceResized(physicalSize)));
    }

    private void RefreshThemeFromSystem()
    {
        if (_state.PreferredTheme is not null)
        {
            return;
        }

        Theme newTheme = DarkMode.TryTheme(_hwnd, null, refreshTitleBar: false);
        if (_state.CurrentTheme == newTheme)
        {
            return;
        }

        _state.CurrentTheme = newTheme;
        _eventLoop.SendWindowEvent(Id, new WindowEvent(new WindowEvent.ThemeChanged(newTheme)));
    }

    private bool HandleSystemCommand(WPARAM wParam)
    {
        uint command = unchecked((uint)wParam.Value) & SysCommandMask;
        switch (command)
        {
            case ScRestore:
                _state.SetWindowFlagsInPlace(flags => flags
                    .With(WindowFlags.Minimized, false)
                    .With(WindowFlags.Maximized, false));
                break;
            case ScMinimize:
                _state.SetWindowFlagsInPlace(flags => flags.With(WindowFlags.Minimized, true));
                break;
            case ScMaximize:
                _state.SetWindowFlagsInPlace(flags => flags
                    .With(WindowFlags.Minimized, false)
                    .With(WindowFlags.Maximized, true));
                break;
            case ScScreenSave when _state.Fullscreen is not null:
                return true;
        }

        return false;
    }

    private void SetDwmIntAttribute(uint attribute, int value)
    {
        _ = PInvoke.DwmSetWindowAttribute(Util.HwndValue(_hwnd), attribute, ref value, sizeof(int));
    }

    private void SetDwmUIntAttribute(uint attribute, uint value)
    {
        _ = PInvoke.DwmSetWindowAttribute(Util.HwndValue(_hwnd), attribute, ref value, sizeof(uint));
    }

    private static PointerKind PointerKindFromPointerInfo(PointerInfo pointerInfo)
    {
        return pointerInfo.PointerType switch
        {
            PointerTypeTouch => new PointerKind(new PointerKind.Touch(FingerId.FromRaw(pointerInfo.PointerId))),
            PointerTypePen => new PointerKind(new PointerKind.TabletTool(TabletToolKind.Pen)),
            _ => new PointerKind(new PointerKind.Unknown()),
        };
    }

    private static PointerSource PointerSourceFromPointerInfo(PointerInfo pointerInfo)
    {
        return pointerInfo.PointerType switch
        {
            PointerTypeTouch => new PointerSource(new PointerSource.Touch(
                FingerId.FromRaw(pointerInfo.PointerId),
                ForceForTouch(pointerInfo.PointerId))),
            PointerTypePen => new PointerSource(new PointerSource.TabletTool(
                TabletToolKind.Pen,
                TabletToolInfoForPen(pointerInfo.PointerId).Data)),
            _ => new PointerSource(new PointerSource.Unknown()),
        };
    }

    private ButtonSource ButtonSourceFromPointerInfo(PointerInfo pointerInfo, bool isDown)
    {
        return pointerInfo.PointerType switch
        {
            PointerTypeTouch => new ButtonSource(new ButtonSource.Touch(
                FingerId.FromRaw(pointerInfo.PointerId),
                ForceForTouch(pointerInfo.PointerId))),
            PointerTypePen => ButtonSourceFromPen(pointerInfo.PointerId, isDown),
            _ => new ButtonSource(new ButtonSource.Unknown(0)),
        };
    }

    private ButtonSource ButtonSourceFromPen(uint pointerId, bool isDown)
    {
        (uint penFlags, TabletToolData data) = TabletToolInfoForPen(pointerId);
        uint buttonFlags = penFlags;
        uint oldPenFlags = _state.LastTabletDownButtonState;
        _state.LastTabletDownButtonState = penFlags;

        if (!isDown)
        {
            buttonFlags ^= oldPenFlags;
        }

        return new ButtonSource(new ButtonSource.TabletTool(
            TabletToolKind.Pen,
            PenFlagsToButton(buttonFlags),
            data));
    }

    private static Force? ForceForTouch(uint pointerId)
    {
        return PInvoke.GetPointerTouchInfo(pointerId, out PointerTouchInfo touchInfo)
            ? NormalizePointerPressure(touchInfo.Pressure)
            : null;
    }

    private static (uint PenFlags, TabletToolData Data) TabletToolInfoForPen(uint pointerId)
    {
        if (!PInvoke.GetPointerPenInfo(pointerId, out PointerPenInfo penInfo))
        {
            return (0, new TabletToolData(null, null, null, null, null));
        }

        Force? force = (penInfo.PenMask & PenMaskPressure) != 0
            ? NormalizePointerPressure(penInfo.Pressure)
            : null;
        ushort? twist = (penInfo.PenMask & PenMaskRotation) != 0
            ? checked((ushort)penInfo.Rotation)
            : null;
        TabletToolTilt? tilt = (penInfo.PenMask & (PenMaskTiltX | PenMaskTiltY)) != 0
            ? new TabletToolTilt(unchecked((sbyte)penInfo.TiltX), unchecked((sbyte)penInfo.TiltY))
            : null;

        return (penInfo.PenFlags, new TabletToolData(force, null, twist, tilt, null));
    }

    private static Force? NormalizePointerPressure(uint pressure)
    {
        return pressure is >= 1 and <= 1024
            ? new Force(new Force.Normalized(pressure / 1024.0))
            : null;
    }

    private static TabletToolButton PenFlagsToButton(uint flags)
    {
        if ((flags & PenFlagBarrel) != 0)
        {
            return new TabletToolButton(new TabletToolButton.Barrel());
        }

        return (flags & PenFlagEraser) != 0
            ? new TabletToolButton(new TabletToolButton.Other((ushort)PenFlagEraser))
            : new TabletToolButton(new TabletToolButton.Contact());
    }

    private static MENU_ITEM_FLAGS MenuEnabled(bool enabled)
    {
        return MENU_ITEM_FLAGS.MF_BYCOMMAND |
            (enabled ? MENU_ITEM_FLAGS.MF_ENABLED : MENU_ITEM_FLAGS.MF_DISABLED);
    }

    private static nuint HitTestForResizeDirection(ResizeDirection direction)
    {
        return direction switch
        {
            ResizeDirection.East => HtRight,
            ResizeDirection.North => HtTop,
            ResizeDirection.NorthEast => HtTopRight,
            ResizeDirection.NorthWest => HtTopLeft,
            ResizeDirection.South => HtBottom,
            ResizeDirection.SouthEast => HtBottomRight,
            ResizeDirection.SouthWest => HtBottomLeft,
            ResizeDirection.West => HtLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }

    private HCURSOR SelectedCursorHandle()
    {
        if (_state.Mouse.SelectedCursor.TryGetValue(out Cursor.Icon icon))
        {
            return PInvoke.LoadCursor(HINSTANCE.Null, SystemCursorId(icon.Value));
        }

        if (_state.Mouse.SelectedCursor.TryGetValue(out Cursor.Custom custom) &&
            custom.Value.Provider is Win32CustomCursorProvider provider &&
            provider.Handle != 0)
        {
            return new HCURSOR(provider.Handle);
        }

        return PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW);
    }

    public void SetTaskbarIcon(Icon? taskbarIcon)
    {
        if (DispatchToEventLoopThread(() => SetTaskbarIcon(taskbarIcon)))
        {
            return;
        }

        SetIcon(taskbarIcon, IconBig);
    }

    private void SetIcon(Icon? icon, nuint iconType)
    {
        if (icon is null)
        {
            PInvoke.SendMessage(_hwnd, WmSetIcon, new WPARAM(iconType), default);
            StoreIcon(null, iconType);
            return;
        }

        Icon winIcon = icon.Provider switch
        {
            RgbaIcon rgbaIcon => new Icon(Win32IconProvider.FromRgba(rgbaIcon)),
            Win32IconProvider => icon,
            WinIcon => icon,
            _ => throw new NotSupportedRequestException("this icon provider is not supported by the Win32 backend"),
        };

        nint handle = winIcon.Provider switch
        {
            Win32IconProvider provider => provider.Handle,
            WinIcon provider => provider.Handle,
            _ => 0,
        };
        PInvoke.SendMessage(_hwnd, WmSetIcon, new WPARAM(iconType), new LPARAM(handle));
        StoreIcon(winIcon, iconType);
    }

    private void StoreIcon(Icon? icon, nuint iconType)
    {
        if (iconType == IconSmall)
        {
            _state.WindowIcon = icon;
        }
        else
        {
            _state.TaskbarIcon = icon;
        }
    }

    private static PCWSTR SystemCursorId(CursorIcon icon)
    {
        return icon switch
        {
            CursorIcon.Help => PInvoke.IDC_HELP,
            CursorIcon.Pointer or CursorIcon.Alias or CursorIcon.Copy => PInvoke.IDC_HAND,
            CursorIcon.Progress => PInvoke.IDC_APPSTARTING,
            CursorIcon.Wait => PInvoke.IDC_WAIT,
            CursorIcon.Cell or CursorIcon.Crosshair => PInvoke.IDC_CROSS,
            CursorIcon.Text or CursorIcon.VerticalText => PInvoke.IDC_IBEAM,
            CursorIcon.NoDrop or CursorIcon.NotAllowed => PInvoke.IDC_NO,
            CursorIcon.Grab or CursorIcon.Grabbing or CursorIcon.Move or CursorIcon.AllScroll => PInvoke.IDC_SIZEALL,
            CursorIcon.EResize or CursorIcon.WResize or CursorIcon.EwResize or CursorIcon.ColResize => PInvoke.IDC_SIZEWE,
            CursorIcon.NResize or CursorIcon.SResize or CursorIcon.NsResize or CursorIcon.RowResize => PInvoke.IDC_SIZENS,
            CursorIcon.NeResize or CursorIcon.SwResize or CursorIcon.NeswResize => PInvoke.IDC_SIZENESW,
            CursorIcon.NwResize or CursorIcon.SeResize or CursorIcon.NwseResize => PInvoke.IDC_SIZENWSE,
            _ => PInvoke.IDC_ARROW,
        };
    }

    private static void RegisterWindowClass(string windowClassName)
    {
        if (!s_registeredClasses.TryAdd(windowClassName, 0))
        {
            return;
        }

        fixed (char* className = windowClassName)
        {
            WNDCLASSEXW windowClass = new()
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                lpfnWndProc = &WndProc,
                hInstance = HINSTANCE.Null,
                hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW),
                lpszClassName = new PCWSTR(className),
            };

            if (PInvoke.RegisterClassEx(windowClass) == 0)
            {
                s_registeredClasses.TryRemove(windowClassName, out _);
                throw Win32Error.Request();
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        try
        {
            return WndProcInner(hwnd, message, wParam, lParam);
        }
        catch (Exception exception)
        {
            if (s_windows.TryGetValue(Util.HwndValue(hwnd), out Window? window))
            {
                window._eventLoop.StoreCallbackException(exception);
            }

            return new LRESULT(-1);
        }
    }

    private static LRESULT WndProcInner(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (s_windows.TryGetValue(Util.HwndValue(hwnd), out Window? window))
        {
            if (message == s_taskbarCreatedMessage)
            {
                TaskbarList.SetSkipTaskbar(hwnd, window._state.SkipTaskbar);
                return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
            }

            if (window.ProcessKeyboardMessage(message, wParam, lParam, out LRESULT keyboardResult))
            {
                return keyboardResult;
            }

            switch (message)
            {
                case PInvoke.WM_NCCALCSIZE:
                    if (window.HandleNcCalcSize(wParam, lParam))
                    {
                        return new LRESULT(0);
                    }

                    break;
                case PInvoke.WM_ENTERSIZEMOVE:
                    window.HandleEnterSizeMove();
                    return new LRESULT(0);
                case PInvoke.WM_EXITSIZEMOVE:
                    window.HandleExitSizeMove(lParam);
                    return new LRESULT(0);
                case PInvoke.WM_NCLBUTTONDOWN:
                    if (wParam.Value == HtCaption)
                    {
                        PInvoke.PostMessage(hwnd, PInvoke.WM_MOUSEMOVE, default, default);
                    }

                    return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
                case PInvoke.WM_CLOSE:
                    window._eventLoop.SendWindowEvent(window.Id, new WindowEvent(new WindowEvent.CloseRequested()));
                    return new LRESULT(0);
                case PInvoke.WM_GETMINMAXINFO:
                    window.ApplyMinMaxInfo(lParam.Value);
                    return new LRESULT(0);
                case PInvoke.WM_WINDOWPOSCHANGING:
                    window.HandleWindowPosChanging(lParam);
                    return new LRESULT(0);
                case PInvoke.WM_SIZING:
                    window.HandleSizing(wParam, lParam);
                    return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
                case PInvoke.WM_MENUCHAR:
                    return new LRESULT((nint)(MncClose << 16));
                case PInvoke.WM_DESTROY:
                    window._fileDropTarget?.Dispose();
                    window._fileDropTarget = null;
                    window._eventLoop.SendWindowEvent(window.Id, new WindowEvent(new WindowEvent.Destroyed()));
                    s_windows.TryRemove(Util.HwndValue(hwnd), out _);
                    break;
                case PInvoke.WM_SIZE:
                    window.HandleSize(wParam, lParam);
                    return new LRESULT(0);
                case PInvoke.WM_WINDOWPOSCHANGED:
                    window.HandleWindowPosChanged(lParam);
                    break;
                case PInvoke.WM_NCACTIVATE:
                    if (window._state.SetActive(wParam.Value != 0))
                    {
                        if (wParam.Value != 0)
                        {
                            window.GainActiveFocus();
                        }
                        else
                        {
                            window.LoseActiveFocus();
                        }
                    }

                    return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
                case PInvoke.WM_SETFOCUS:
                    if (window._state.SetFocused(true))
                    {
                        window.GainActiveFocus();
                    }

                    return new LRESULT(0);
                case PInvoke.WM_KILLFOCUS:
                    if (window._state.SetFocused(false))
                    {
                        window.LoseActiveFocus();
                    }

                    return new LRESULT(0);
                case WmImeStartComposition:
                    window.HandleImeStartComposition();
                    return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
                case WmImeComposition:
                    window.HandleImeComposition(lParam);
                    return new LRESULT(0);
                case WmImeEndComposition:
                    window.HandleImeEndComposition();
                    return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
                case WmImeSetContext:
                    return PInvoke.DefWindowProc(
                        hwnd,
                        message,
                        wParam,
                        new LPARAM(lParam.Value & ~s_iscShowUiCompositionWindow));
                case PInvoke.WM_MOUSEMOVE:
                    window.HandlePointerMove(lParam);
                    return new LRESULT(0);
                case PInvoke.WM_MOUSELEAVE:
                    window.HandlePointerLeave();
                    return new LRESULT(0);
                case PInvoke.WM_LBUTTONDOWN:
                    window.HandlePointerButton(lParam, ElementState.Pressed, MouseButton.Left);
                    return new LRESULT(0);
                case PInvoke.WM_RBUTTONDOWN:
                    window.HandlePointerButton(lParam, ElementState.Pressed, MouseButton.Right);
                    return new LRESULT(0);
                case PInvoke.WM_MBUTTONDOWN:
                    window.HandlePointerButton(lParam, ElementState.Pressed, MouseButton.Middle);
                    return new LRESULT(0);
                case PInvoke.WM_XBUTTONDOWN:
                    if (MouseButtonFromXButton(wParam, out MouseButton pressedButton))
                    {
                        window.HandlePointerButton(lParam, ElementState.Pressed, pressedButton);
                        return new LRESULT(0);
                    }

                    break;
                case PInvoke.WM_LBUTTONUP:
                    window.HandlePointerButton(lParam, ElementState.Released, MouseButton.Left);
                    return new LRESULT(0);
                case PInvoke.WM_RBUTTONUP:
                    window.HandlePointerButton(lParam, ElementState.Released, MouseButton.Right);
                    return new LRESULT(0);
                case PInvoke.WM_MBUTTONUP:
                    window.HandlePointerButton(lParam, ElementState.Released, MouseButton.Middle);
                    return new LRESULT(0);
                case PInvoke.WM_XBUTTONUP:
                    if (MouseButtonFromXButton(wParam, out MouseButton releasedButton))
                    {
                        window.HandlePointerButton(lParam, ElementState.Released, releasedButton);
                        return new LRESULT(0);
                    }

                    break;
                case PInvoke.WM_CAPTURECHANGED:
                    if (lParam.Value != Util.HwndValue(hwnd))
                    {
                        window._state.Mouse.CaptureCount = 0;
                    }

                    return new LRESULT(0);
                case PInvoke.WM_MOUSEWHEEL:
                    window.HandleMouseWheel(wParam, horizontal: false);
                    return new LRESULT(0);
                case PInvoke.WM_MOUSEHWHEEL:
                    window.HandleMouseWheel(wParam, horizontal: true);
                    return new LRESULT(0);
                case PInvoke.WM_TOUCH:
                    window.HandleTouch(wParam, lParam);
                    return new LRESULT(0);
                case PInvoke.WM_POINTERDOWN:
                case PInvoke.WM_POINTERUPDATE:
                case PInvoke.WM_POINTERUP:
                    window.HandlePointerMessage(message, wParam);
                    return new LRESULT(0);
                case PInvoke.WM_DPICHANGED:
                    window.HandleDpiChanged(wParam, lParam);
                    return new LRESULT(0);
                case PInvoke.WM_SETTINGCHANGE:
                    window.RefreshThemeFromSystem();
                    break;
                case PInvoke.WM_SYSCOMMAND:
                    if (window.HandleSystemCommand(wParam))
                    {
                        return new LRESULT(0);
                    }

                    break;
                case PInvoke.WM_SETCURSOR:
                    if (Util.LowWord(lParam.Value) == HtClient)
                    {
                        window.ApplyCursor();
                        return new LRESULT(1);
                    }

                    break;
                case PInvoke.WM_PAINT:
                    window._state.RedrawRequested = window._eventLoop.ShouldBufferEvents();
                    if (!window._eventLoop.ShouldBufferEvents())
                    {
                        window._eventLoop.SendWindowEvent(window.Id, new WindowEvent(new WindowEvent.RedrawRequested()));
                    }

                    LRESULT paintResult = PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
                    if (window._state.RedrawRequested)
                    {
                        window._state.RedrawRequested = false;
                        PInvoke.RedrawWindow(hwnd, null, 0, PInvoke.RDW_INTERNALPAINT);
                    }

                    return paintResult;
                case PInvoke.WM_NCDESTROY:
                    s_windows.TryRemove(Util.HwndValue(hwnd), out _);
                    break;
            }
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }

    private static HWND HwndFromObject(object handle)
    {
        return handle switch
        {
            HWND hwnd => hwnd,
            nint raw => new HWND(raw),
            Window window => window.Hwnd,
            RawWindowHandle rawWindowHandle when rawWindowHandle.TryGetValue(out RawWindowHandle.Win32 win32) =>
                new HWND(win32.Hwnd),
            _ => HWND.Null,
        };
    }

    private static nint ModuleHandle()
    {
        return PInvoke.GetModuleHandleW(null);
    }

    private static int SnapToNearestIncrementDelta(int value, int increment)
    {
        int halfOne = increment / 2;
        int halfTwo = increment - halfOne;
        return halfOne - (value - halfTwo) % increment;
    }

    private static double FractionalPart(double value)
    {
        return value - Math.Truncate(value);
    }

    private static bool MouseButtonFromXButton(WPARAM wParam, out MouseButton button)
    {
        button = Util.HighWord(unchecked((nint)wParam.Value)) switch
        {
            XButton1 => MouseButton.Back,
            XButton2 => MouseButton.Forward,
            _ => default,
        };

        return button is MouseButton.Back or MouseButton.Forward;
    }

    private enum PointerMoveKind
    {
        Enter,
        Leave,
        None,
    }
}

internal static unsafe class TaskbarList
{
    private const int SOk = 0;
    private const uint CoInitApartmentThreaded = 0x2;
    private const uint ClsctxAll = 0x17;

    private static readonly Guid s_clsidTaskbarList = new("56FDF344-FD6D-11D0-958A-006097C9A090");
    private static readonly Guid s_iidITaskbarList = new("56FDF342-FD6D-11D0-958A-006097C9A090");
    private static readonly Guid s_iidITaskbarList2 = new("602D4995-B13A-429B-A66E-1935E44F4317");

    [ThreadStatic]
    private static bool s_comInitialized;

    [ThreadStatic]
    private static ITaskbarList* s_taskbarList;

    [ThreadStatic]
    private static ITaskbarList2* s_taskbarList2;

    public static void SetSkipTaskbar(HWND hwnd, bool skip)
    {
        ITaskbarList* taskbarList = GetTaskbarList();
        if (taskbarList is null)
        {
            return;
        }

        if (skip)
        {
            _ = taskbarList->lpVtbl->DeleteTab(taskbarList, hwnd);
        }
        else
        {
            _ = taskbarList->lpVtbl->AddTab(taskbarList, hwnd);
        }
    }

    public static void MarkFullscreenWindow(HWND hwnd, bool fullscreen)
    {
        ITaskbarList2* taskbarList = GetTaskbarList2();
        if (taskbarList is null)
        {
            return;
        }

        _ = taskbarList->lpVtbl->MarkFullscreenWindow(taskbarList, hwnd, fullscreen);
    }

    private static ITaskbarList* GetTaskbarList()
    {
        if (s_taskbarList is not null)
        {
            return s_taskbarList;
        }

        if (!EnsureComInitialized())
        {
            return null;
        }

        Guid clsid = s_clsidTaskbarList;
        Guid iid = s_iidITaskbarList;
        void* instance = null;
        int hr = PInvoke.CoCreateInstance(&clsid, 0, ClsctxAll, &iid, &instance);
        if (hr != SOk || instance is null)
        {
            return null;
        }

        ITaskbarList* taskbarList = (ITaskbarList*)instance;
        if (taskbarList->lpVtbl->HrInit(taskbarList) != SOk)
        {
            ReleaseUnknown((IUnknown*)taskbarList);
            return null;
        }

        s_taskbarList = taskbarList;
        return taskbarList;
    }

    private static ITaskbarList2* GetTaskbarList2()
    {
        if (s_taskbarList2 is not null)
        {
            return s_taskbarList2;
        }

        if (!EnsureComInitialized())
        {
            return null;
        }

        Guid clsid = s_clsidTaskbarList;
        Guid iid = s_iidITaskbarList2;
        void* instance = null;
        int hr = PInvoke.CoCreateInstance(&clsid, 0, ClsctxAll, &iid, &instance);
        if (hr != SOk || instance is null)
        {
            return null;
        }

        ITaskbarList2* taskbarList = (ITaskbarList2*)instance;
        if (taskbarList->lpVtbl->parent.HrInit((ITaskbarList*)taskbarList) != SOk)
        {
            ReleaseUnknown((IUnknown*)taskbarList);
            return null;
        }

        s_taskbarList2 = taskbarList;
        return taskbarList;
    }

    private static bool EnsureComInitialized()
    {
        if (s_comInitialized)
        {
            return true;
        }

        int hr = PInvoke.CoInitializeEx(0, CoInitApartmentThreaded);
        if (hr < 0)
        {
            return false;
        }

        s_comInitialized = true;
        return true;
    }

    private static void ReleaseUnknown(IUnknown* unknown)
    {
        if (unknown is not null && unknown->lpVtbl is not null)
        {
            _ = unknown->lpVtbl->Release(unknown);
        }
    }
}
