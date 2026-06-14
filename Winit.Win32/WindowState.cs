using Winit.Core;
using Winit.Dpi;

namespace Winit.Win32;

internal sealed class WindowState
{
    public WindowState(
        WindowAttributes attributes,
        WindowAttributesWindows winAttributes,
        double scaleFactor,
        Theme currentTheme,
        Theme? preferredTheme)
    {
        Mouse = new MouseProperties(attributes.Cursor);
        MinSize = attributes.MinSurfaceSize;
        MaxSize = attributes.MaxSurfaceSize;
        SurfaceResizeIncrements = attributes.SurfaceResizeIncrements;
        WindowIcon = attributes.WindowIcon;
        TaskbarIcon = winAttributes.TaskbarIcon;
        ScaleFactor = scaleFactor;
        Fullscreen = attributes.Fullscreen;
        CurrentTheme = currentTheme;
        PreferredTheme = preferredTheme;
        WindowFlags = WindowFlagsExtensions.FromAttributes(attributes, winAttributes);
        ImeState = ImeState.Disabled;
        SkipTaskbar = winAttributes.SkipTaskbar;
        UseSystemWheelSpeed = winAttributes.UseSystemWheelSpeed;
    }

    public MouseProperties Mouse { get; }

    public Size? MinSize { get; set; }

    public Size? MaxSize { get; set; }

    public PhysicalSize<uint> SurfaceSize { get; set; }

    public Size? SurfaceResizeIncrements { get; set; }

    public Icon? WindowIcon { get; set; }

    public Icon? TaskbarIcon { get; set; }

    public SavedWindow? SavedWindow { get; set; }

    public double ScaleFactor { get; set; }

    public ModifiersState ModifiersState { get; set; }

    public ModifiersKeys PressedModifiers { get; set; }

    public Fullscreen? Fullscreen { get; set; }

    public Theme CurrentTheme { get; set; }

    public Theme? PreferredTheme { get; set; }

    public WindowFlags WindowFlags { get; private set; }

    public ImeState ImeState { get; set; }

    public ImeCapabilities? ImeCapabilities { get; set; }

    public KeyEventBuilder KeyEventBuilder { get; } = new();

    public MinimalIme MinimalIme { get; } = new();

    public bool IsActive { get; private set; }

    public bool IsFocused { get; private set; }

    public bool RedrawRequested { get; set; }

    public bool Dragging { get; set; }

    public bool SkipTaskbar { get; set; }

    public bool UseSystemWheelSpeed { get; set; }

    public uint LastTabletDownButtonState { get; set; }

    public bool HasActiveFocus => IsActive && IsFocused;

    public void SetWindowFlags(HWND hwnd, Func<WindowFlags, WindowFlags> update)
    {
        WindowFlags oldFlags = WindowFlags;
        WindowFlags newFlags = update(oldFlags);
        WindowFlags = newFlags;
        oldFlags.ApplyDiff(hwnd, newFlags);
    }

    public void SetWindowFlagsInPlace(Func<WindowFlags, WindowFlags> update)
    {
        WindowFlags = update(WindowFlags);
    }

    public bool SetActive(bool isActive)
    {
        bool old = HasActiveFocus;
        IsActive = isActive;
        return old != HasActiveFocus;
    }

    public bool SetFocused(bool isFocused)
    {
        bool old = HasActiveFocus;
        IsFocused = isFocused;
        return old != HasActiveFocus;
    }
}

internal sealed class MouseProperties(Cursor selectedCursor)
{
    public Cursor SelectedCursor { get; set; } = selectedCursor;

    public uint CaptureCount { get; set; }

    public CursorFlags CursorFlags { get; private set; }

    public PhysicalPosition<double>? LastPosition { get; set; }

    public void SetCursorFlags(Func<CursorFlags, CursorFlags> update)
    {
        CursorFlags = update(CursorFlags);
    }
}

[Flags]
internal enum CursorFlags : byte
{
    None = 0,
    Grabbed = 1 << 0,
    Hidden = 1 << 1,
    InWindow = 1 << 2,
    Locked = 1 << 3,
}

internal static class CursorFlagsExtensions
{
    public static CursorFlags With(this CursorFlags flags, CursorFlags flag, bool enabled)
    {
        return enabled ? flags | flag : flags & ~flag;
    }
}

[Flags]
internal enum WindowFlags : uint
{
    None = 0,
    Resizable = 1 << 0,
    Minimizable = 1 << 1,
    Maximizable = 1 << 2,
    Closable = 1 << 3,
    Visible = 1 << 4,
    OnTaskbar = 1 << 5,
    AlwaysOnTop = 1 << 6,
    AlwaysOnBottom = 1 << 7,
    NoBackBuffer = 1 << 8,
    Transparent = 1 << 9,
    Child = 1 << 10,
    Maximized = 1 << 11,
    Popup = 1 << 12,
    MarkerExclusiveFullscreen = 1 << 13,
    MarkerBorderlessFullscreen = 1 << 14,
    MarkerRetainStateOnSize = 1 << 15,
    MarkerInSizeMove = 1 << 16,
    Minimized = 1 << 17,
    IgnoreCursorEvent = 1 << 18,
    MarkerDecorations = 1 << 19,
    MarkerUndecoratedShadow = 1 << 20,
    MarkerActivate = 1 << 21,
    ClipChildren = 1 << 22,
    ExclusiveFullscreenOrMask = AlwaysOnTop,
}

internal enum ImeState
{
    Disabled,
    Enabled,
    Preedit,
}

internal sealed class SavedWindow(WindowPlacement placement)
{
    public WindowPlacement Placement { get; } = placement;
}

internal static class WindowFlagsExtensions
{
    private const uint ScClose = 0xF060;
    private static readonly HWND s_hwndTopmost = new(-1);
    private static readonly HWND s_hwndNoTopmost = new(-2);
    private static readonly HWND s_hwndBottom = new(1);

    public static WindowFlags FromAttributes(WindowAttributes attributes, WindowAttributesWindows winAttributes)
    {
        WindowFlags flags = WindowFlags.None;
        flags = flags.With(WindowFlags.MarkerDecorations, attributes.Decorations);
        flags = flags.With(WindowFlags.MarkerUndecoratedShadow, winAttributes.DecorationShadow);
        flags = flags.With(WindowFlags.AlwaysOnTop, attributes.WindowLevel == WindowLevel.AlwaysOnTop);
        flags = flags.With(WindowFlags.AlwaysOnBottom, attributes.WindowLevel == WindowLevel.AlwaysOnBottom);
        flags = flags.With(WindowFlags.NoBackBuffer, winAttributes.NoRedirectionBitmap);
        flags = flags.With(WindowFlags.MarkerActivate, attributes.Active);
        flags = flags.With(WindowFlags.Transparent, attributes.Transparent);
        flags = flags.With(WindowFlags.Visible, attributes.Visible);
        flags = flags.With(WindowFlags.Maximized, attributes.Maximized);
        flags = flags.With(WindowFlags.Resizable, attributes.Resizable);
        flags = flags.With(WindowFlags.Minimizable, attributes.EnabledButtons.HasFlag(WindowButtons.Minimize));
        flags = flags.With(WindowFlags.Maximizable, attributes.EnabledButtons.HasFlag(WindowButtons.Maximize));
        flags = flags.With(WindowFlags.Closable, attributes.EnabledButtons.HasFlag(WindowButtons.Close));
        flags = flags.With(WindowFlags.ClipChildren, winAttributes.ClipChildren);

        if (attributes.ParentWindow is not null)
        {
            flags |= WindowFlags.Child;
        }
        else if (winAttributes.Owner is not null)
        {
            flags |= WindowFlags.Popup;
        }
        else if (!winAttributes.SkipTaskbar)
        {
            flags |= WindowFlags.OnTaskbar;
        }

        if (attributes.Fullscreen is { } fullscreen)
        {
            flags = flags.With(
                WindowFlags.MarkerExclusiveFullscreen,
                fullscreen.TryGetValue(out Fullscreen.Exclusive _));
            flags = flags.With(
                WindowFlags.MarkerBorderlessFullscreen,
                fullscreen.TryGetValue(out Fullscreen.Borderless _));
        }

        return flags;
    }

    public static WindowFlags With(this WindowFlags flags, WindowFlags flag, bool enabled)
    {
        return enabled ? flags | flag : flags & ~flag;
    }

    public static WindowButtons ToWindowButtons(this WindowFlags flags)
    {
        WindowButtons buttons = WindowButtons.None;
        if (flags.HasFlag(WindowFlags.Closable))
        {
            buttons |= WindowButtons.Close;
        }

        if (flags.HasFlag(WindowFlags.Minimizable))
        {
            buttons |= WindowButtons.Minimize;
        }

        if (flags.HasFlag(WindowFlags.Maximizable))
        {
            buttons |= WindowButtons.Maximize;
        }

        return buttons;
    }

    public static (WINDOW_STYLE Style, WINDOW_EX_STYLE ExStyle) ToWindowStyles(this WindowFlags flags)
    {
        WINDOW_STYLE style =
            WINDOW_STYLE.WS_CAPTION |
            WINDOW_STYLE.WS_BORDER |
            WINDOW_STYLE.WS_CLIPSIBLINGS |
            WINDOW_STYLE.WS_SYSMENU;
        WINDOW_EX_STYLE exStyle =
            WINDOW_EX_STYLE.WS_EX_WINDOWEDGE |
            WINDOW_EX_STYLE.WS_EX_ACCEPTFILES;

        if (flags.HasFlag(WindowFlags.Resizable))
        {
            style |= WINDOW_STYLE.WS_SIZEBOX;
        }

        if (flags.HasFlag(WindowFlags.Maximizable))
        {
            style |= WINDOW_STYLE.WS_MAXIMIZEBOX;
        }

        if (flags.HasFlag(WindowFlags.Minimizable))
        {
            style |= WINDOW_STYLE.WS_MINIMIZEBOX;
        }

        if (flags.HasFlag(WindowFlags.Visible))
        {
            style |= WINDOW_STYLE.WS_VISIBLE;
        }

        if (flags.HasFlag(WindowFlags.OnTaskbar))
        {
            exStyle |= WINDOW_EX_STYLE.WS_EX_APPWINDOW;
        }

        if (flags.HasFlag(WindowFlags.AlwaysOnTop))
        {
            exStyle |= WINDOW_EX_STYLE.WS_EX_TOPMOST;
        }

        if (flags.HasFlag(WindowFlags.NoBackBuffer))
        {
            exStyle |= WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP;
        }

        if (flags.HasFlag(WindowFlags.Child))
        {
            style |= WINDOW_STYLE.WS_CHILD;

            if (!flags.HasFlag(WindowFlags.MarkerDecorations))
            {
                style &= ~(WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_BORDER);
                exStyle &= ~WINDOW_EX_STYLE.WS_EX_WINDOWEDGE;
            }
        }

        if (flags.HasFlag(WindowFlags.Popup))
        {
            style |= WINDOW_STYLE.WS_POPUP;
        }

        if (flags.HasFlag(WindowFlags.Minimized))
        {
            style |= WINDOW_STYLE.WS_MINIMIZE;
        }

        if (flags.HasFlag(WindowFlags.Maximized))
        {
            style |= WINDOW_STYLE.WS_MAXIMIZE;
        }

        if (flags.HasFlag(WindowFlags.IgnoreCursorEvent))
        {
            exStyle |= WINDOW_EX_STYLE.WS_EX_TRANSPARENT | WINDOW_EX_STYLE.WS_EX_LAYERED;
        }

        if (flags.HasFlag(WindowFlags.ClipChildren))
        {
            style |= WINDOW_STYLE.WS_CLIPCHILDREN;
        }

        if (flags.HasFlag(WindowFlags.MarkerExclusiveFullscreen) ||
            flags.HasFlag(WindowFlags.MarkerBorderlessFullscreen))
        {
            style &= ~WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
        }

        return (style, exStyle);
    }

    public static void ApplyDiff(this WindowFlags oldFlags, HWND hwnd, WindowFlags newFlags)
    {
        oldFlags = oldFlags.Mask();
        newFlags = newFlags.Mask();
        WindowFlags diff = oldFlags ^ newFlags;
        if (diff == WindowFlags.None)
        {
            return;
        }

        if (newFlags.HasFlag(WindowFlags.Visible))
        {
            SHOW_WINDOW_CMD command = oldFlags.HasFlag(WindowFlags.MarkerActivate)
                ? SHOW_WINDOW_CMD.SW_SHOW
                : SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE;
            PInvoke.ShowWindow(hwnd, command);
        }

        if ((diff & (WindowFlags.AlwaysOnTop | WindowFlags.AlwaysOnBottom)) != WindowFlags.None)
        {
            HWND insertAfter = newFlags.HasFlag(WindowFlags.AlwaysOnTop)
                ? s_hwndTopmost
                : newFlags.HasFlag(WindowFlags.AlwaysOnBottom)
                    ? s_hwndBottom
                    : s_hwndNoTopmost;
            PInvoke.SetWindowPos(
                hwnd,
                insertAfter,
                0,
                0,
                0,
                0,
                SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS |
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }

        if (diff.HasFlag(WindowFlags.Maximized) || newFlags.HasFlag(WindowFlags.Maximized))
        {
            PInvoke.ShowWindow(
                hwnd,
                newFlags.HasFlag(WindowFlags.Maximized) ? SHOW_WINDOW_CMD.SW_MAXIMIZE : SHOW_WINDOW_CMD.SW_RESTORE);
        }

        if (diff.HasFlag(WindowFlags.Minimized))
        {
            PInvoke.ShowWindow(
                hwnd,
                newFlags.HasFlag(WindowFlags.Minimized) ? SHOW_WINDOW_CMD.SW_MINIMIZE : SHOW_WINDOW_CMD.SW_RESTORE);
            diff &= ~WindowFlags.Minimized;
        }

        if (diff.HasFlag(WindowFlags.Closable) || newFlags.HasFlag(WindowFlags.Closable))
        {
            MENU_ITEM_FLAGS flags = MENU_ITEM_FLAGS.MF_BYCOMMAND |
                (newFlags.HasFlag(WindowFlags.Closable)
                    ? MENU_ITEM_FLAGS.MF_ENABLED
                    : MENU_ITEM_FLAGS.MF_DISABLED);
            PInvoke.EnableMenuItem(PInvoke.GetSystemMenu(hwnd, false), ScClose, flags);
        }

        if (!newFlags.HasFlag(WindowFlags.Visible))
        {
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
        }

        if (diff != WindowFlags.None)
        {
            (WINDOW_STYLE style, WINDOW_EX_STYLE exStyle) = newFlags.ToWindowStyles();
            PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)(uint)style);
            PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)(uint)exStyle);
            PInvoke.SetWindowPos(
                hwnd,
                HWND.Null,
                0,
                0,
                0,
                0,
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }
    }

    private static WindowFlags Mask(this WindowFlags flags)
    {
        if (flags.HasFlag(WindowFlags.MarkerExclusiveFullscreen))
        {
            flags |= WindowFlags.ExclusiveFullscreenOrMask;
        }

        return flags;
    }
}
