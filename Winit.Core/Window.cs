using System.Text;
using Winit.Dpi;

namespace Winit.Core;

public readonly record struct WindowId(nuint Raw)
{
    public nuint IntoRaw()
    {
        return Raw;
    }

    public static WindowId FromRaw(nuint id)
    {
        return new WindowId(id);
    }
}

public sealed class WindowAttributes
{
    public Size? SurfaceSize { get; set; }

    public Size? MinSurfaceSize { get; set; }

    public Size? MaxSurfaceSize { get; set; }

    public Size? SurfaceResizeIncrements { get; set; }

    public Position? Position { get; set; }

    public bool Resizable { get; set; } = true;

    public WindowButtons EnabledButtons { get; set; } = WindowButtons.All;

    public string Title { get; set; } = "winit window";

    public bool Maximized { get; set; }

    public bool Visible { get; set; } = true;

    public bool Transparent { get; set; }

    public bool Blur { get; set; }

    public bool Decorations { get; set; } = true;

    public Icon? WindowIcon { get; set; }

    public Theme? PreferredTheme { get; set; }

    public bool ContentProtected { get; set; }

    public WindowLevel WindowLevel { get; set; } = WindowLevel.Normal;

    public bool Active { get; set; } = true;

    public Cursor Cursor { get; set; } = Cursor.Default;

    public object? ParentWindow { get; set; }

    public Fullscreen? Fullscreen { get; set; }

    public IPlatformWindowAttributes? Platform { get; set; }

    public static WindowAttributes Default => new();

    public WindowAttributes Clone()
    {
        return new WindowAttributes
        {
            SurfaceSize = SurfaceSize,
            MinSurfaceSize = MinSurfaceSize,
            MaxSurfaceSize = MaxSurfaceSize,
            SurfaceResizeIncrements = SurfaceResizeIncrements,
            Position = Position,
            Resizable = Resizable,
            EnabledButtons = EnabledButtons,
            Title = Title,
            Maximized = Maximized,
            Visible = Visible,
            Transparent = Transparent,
            Blur = Blur,
            Decorations = Decorations,
            WindowIcon = WindowIcon,
            PreferredTheme = PreferredTheme,
            ContentProtected = ContentProtected,
            WindowLevel = WindowLevel,
            Active = Active,
            Cursor = Cursor,
            ParentWindow = ParentWindow,
            Fullscreen = Fullscreen,
            Platform = Platform?.Clone(),
        };
    }

    public WindowAttributes WithSurfaceSize(Size size)
    {
        return With(attributes => attributes.SurfaceSize = size);
    }

    public WindowAttributes WithMinSurfaceSize(Size minSize)
    {
        return With(attributes => attributes.MinSurfaceSize = minSize);
    }

    public WindowAttributes WithMaxSurfaceSize(Size maxSize)
    {
        return With(attributes => attributes.MaxSurfaceSize = maxSize);
    }

    public WindowAttributes WithSurfaceResizeIncrements(Size surfaceResizeIncrements)
    {
        return With(attributes => attributes.SurfaceResizeIncrements = surfaceResizeIncrements);
    }

    public WindowAttributes WithPosition(Position position)
    {
        return With(attributes => attributes.Position = position);
    }

    public WindowAttributes WithResizable(bool resizable)
    {
        return With(attributes => attributes.Resizable = resizable);
    }

    public WindowAttributes WithEnabledButtons(WindowButtons buttons)
    {
        return With(attributes => attributes.EnabledButtons = buttons);
    }

    public WindowAttributes WithTitle(string title)
    {
        return With(attributes => attributes.Title = title);
    }

    public WindowAttributes WithFullscreen(Fullscreen? fullscreen)
    {
        return With(attributes => attributes.Fullscreen = fullscreen);
    }

    public WindowAttributes WithMaximized(bool maximized)
    {
        return With(attributes => attributes.Maximized = maximized);
    }

    public WindowAttributes WithVisible(bool visible)
    {
        return With(attributes => attributes.Visible = visible);
    }

    public WindowAttributes WithTransparent(bool transparent)
    {
        return With(attributes => attributes.Transparent = transparent);
    }

    public WindowAttributes WithBlur(bool blur)
    {
        return With(attributes => attributes.Blur = blur);
    }

    public WindowAttributes WithDecorations(bool decorations)
    {
        return With(attributes => attributes.Decorations = decorations);
    }

    public WindowAttributes WithWindowLevel(WindowLevel level)
    {
        return With(attributes => attributes.WindowLevel = level);
    }

    public WindowAttributes WithWindowIcon(Icon? windowIcon)
    {
        return With(attributes => attributes.WindowIcon = windowIcon);
    }

    public WindowAttributes WithTheme(Theme? theme)
    {
        return With(attributes => attributes.PreferredTheme = theme);
    }

    public WindowAttributes WithContentProtected(bool isProtected)
    {
        return With(attributes => attributes.ContentProtected = isProtected);
    }

    public WindowAttributes WithActive(bool active)
    {
        return With(attributes => attributes.Active = active);
    }

    public WindowAttributes WithCursor(Cursor cursor)
    {
        return With(attributes => attributes.Cursor = cursor);
    }

    public WindowAttributes WithCursor(CursorIcon cursorIcon)
    {
        return WithCursor(Cursor.From(cursorIcon));
    }

    public WindowAttributes WithParentWindow(object? parentWindow)
    {
        return With(attributes => attributes.ParentWindow = parentWindow);
    }

    public WindowAttributes WithPlatformAttributes(IPlatformWindowAttributes platform)
    {
        return With(attributes => attributes.Platform = platform);
    }

    private WindowAttributes With(Action<WindowAttributes> configure)
    {
        WindowAttributes attributes = Clone();
        configure(attributes);
        return attributes;
    }
}

public interface IPlatformWindowAttributes : IAsAny
{
    IPlatformWindowAttributes Clone();
}

public interface IWindow : IAsAny
{
    WindowId Id { get; }

    double ScaleFactor { get; }

    PhysicalPosition<int> SurfacePosition { get; }

    PhysicalPosition<int> OuterPosition { get; }

    PhysicalSize<uint> SurfaceSize { get; }

    PhysicalSize<uint> OuterSize { get; }

    PhysicalInsets<uint> SafeArea { get; }

    PhysicalSize<uint>? SurfaceResizeIncrements { get; }

    bool? IsVisible { get; }

    bool IsResizable { get; }

    WindowButtons EnabledButtons { get; }

    bool? IsMinimized { get; }

    bool IsMaximized { get; }

    Fullscreen? Fullscreen { get; }

    bool IsDecorated { get; }

    ImeCapabilities? ImeCapabilities { get; }

    bool HasFocus { get; }

    Theme? Theme { get; }

    string Title { get; }

    MonitorHandle? CurrentMonitor { get; }

    IEnumerable<MonitorHandle> AvailableMonitors { get; }

    MonitorHandle? PrimaryMonitor { get; }

    object? DisplayHandle { get; }

    object? WindowHandle { get; }

    void RequestRedraw();

    void PrePresentNotify();

    void ResetDeadKeys();

    PhysicalSize<uint>? RequestSurfaceSize(Size size);

    void SetOuterPosition(Position position);

    void SetMinSurfaceSize(Size? minSize);

    void SetMaxSurfaceSize(Size? maxSize);

    void SetSurfaceResizeIncrements(Size? increments);

    void SetTitle(string title);

    void SetTransparent(bool transparent);

    void SetBlur(bool blur);

    void SetVisible(bool visible);

    void SetResizable(bool resizable);

    void SetEnabledButtons(WindowButtons buttons);

    void SetMinimized(bool minimized);

    void SetMaximized(bool maximized);

    void SetFullscreen(Fullscreen? fullscreen);

    void SetDecorations(bool decorations);

    void SetWindowLevel(WindowLevel level);

    void SetWindowIcon(Icon? windowIcon);

    void RequestImeUpdate(ImeRequest request);

    void FocusWindow();

    void RequestUserAttention(UserAttentionType? requestType);

    void SetTheme(Theme? theme);

    void SetContentProtected(bool isProtected);

    void SetCursor(Cursor cursor);

    void SetCursor(CursorIcon cursorIcon)
    {
        SetCursor(Cursor.From(cursorIcon));
    }

    void SetCursorPosition(Position position);

    void SetCursorGrab(CursorGrabMode mode);

    void SetCursorVisible(bool visible);

    void DragWindow();

    void DragResizeWindow(ResizeDirection direction);

    void ShowWindowMenu(Position position);

    void SetCursorHittest(bool hittest);

    void SetImeCursorArea(Position position, Size size)
    {
        if (ImeCapabilities?.CursorArea() != true)
        {
            return;
        }

        try
        {
            RequestImeUpdate(new ImeRequest(new ImeRequest.Update(
                ImeRequestData.Default.WithCursorArea(position, size))));
        }
        catch (ImeRequestException)
        {
        }
    }

    void SetImeAllowed(bool allowed)
    {
        ImeRequest request;

        if (allowed)
        {
            Position position = new LogicalPosition<double>(0.0, 0.0);
            Size size = new LogicalSize<double>(0.0, 0.0);
            ImeCapabilities capabilities = global::Winit.Core.ImeCapabilities.New()
                .WithHintAndPurpose()
                .WithCursorArea();
            ImeRequestData requestData = ImeRequestData.Default
                .WithHintAndPurpose(ImeHint.None, ImePurpose.Normal)
                .WithCursorArea(position, size);
            ImeEnableRequest? enableRequest = ImeEnableRequest.Create(capabilities, requestData);

            if (enableRequest is null)
            {
                return;
            }

            request = new ImeRequest(new ImeRequest.Enable(enableRequest.Value));
        }
        else
        {
            request = new ImeRequest(new ImeRequest.Disable());
        }

        try
        {
            RequestImeUpdate(request);
        }
        catch (ImeRequestException)
        {
        }
    }

    void SetImePurpose(ImePurpose purpose)
    {
        if (ImeCapabilities?.HintAndPurpose() != true)
        {
            return;
        }

        try
        {
            RequestImeUpdate(new ImeRequest(new ImeRequest.Update(
                ImeRequestData.Default.WithHintAndPurpose(ImeHint.None, purpose))));
        }
        catch (ImeRequestException)
        {
        }
    }
}

public static class WindowExtensions
{
    public static bool WindowEquals(this IWindow window, IWindow other)
    {
        return window.Id == other.Id;
    }

    public static int WindowHashCode(this IWindow window)
    {
        return window.Id.GetHashCode();
    }
}

public enum CursorGrabMode
{
    None,
    Confined,
    Locked,
}

public enum ResizeDirection
{
    East,
    North,
    NorthEast,
    NorthWest,
    South,
    SouthEast,
    SouthWest,
    West,
}

public static class ResizeDirectionExtensions
{
    public static CursorIcon ToCursorIcon(this ResizeDirection direction)
    {
        return direction switch
        {
            ResizeDirection.East => CursorIcon.EResize,
            ResizeDirection.North => CursorIcon.NResize,
            ResizeDirection.NorthEast => CursorIcon.NeResize,
            ResizeDirection.NorthWest => CursorIcon.NwResize,
            ResizeDirection.South => CursorIcon.SResize,
            ResizeDirection.SouthEast => CursorIcon.SeResize,
            ResizeDirection.SouthWest => CursorIcon.SwResize,
            ResizeDirection.West => CursorIcon.WResize,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }
}

public enum Theme
{
    Light,
    Dark,
}

public enum UserAttentionType
{
    Informational = 0,
    Critical = 1,
}

[Flags]
public enum WindowButtons : uint
{
    None = 0,
    Close = 1 << 0,
    Minimize = 1 << 1,
    Maximize = 1 << 2,
    All = Close | Minimize | Maximize,
}

public enum WindowLevel
{
    Normal = 0,
    AlwaysOnBottom = 1,
    AlwaysOnTop = 2,
}

public enum ImePurpose
{
    Normal,
    Password,
    Terminal,
    Number,
    Phone,
    Url,
    Email,
    Pin,
    Date,
    Time,
    DateTime,
}

[Flags]
public enum ImeHint : uint
{
    None = 0,
    Completion = 0x1,
    Spellcheck = 0x2,
    AutoCapitalization = 0x4,
    Lowercase = 0x8,
    Uppercase = 0x10,
    Titlecase = 0x20,
    HiddenText = 0x40,
    SensitiveData = 0x80,
    Latin = 0x100,
    Multiline = 0x200,
}

public sealed class ImeSurroundingText : IEquatable<ImeSurroundingText>
{
    public const int MaxTextBytes = 4000;

    public ImeSurroundingText(string text, nuint cursor, nuint anchor)
    {
        ArgumentNullException.ThrowIfNull(text);

        int byteCount = Encoding.UTF8.GetByteCount(text);
        if (byteCount >= MaxTextBytes)
        {
            throw new ImeSurroundingTextException(ImeSurroundingTextError.TextTooLong);
        }

        if (!IsUtf8Boundary(text, cursor, byteCount))
        {
            throw new ImeSurroundingTextException(ImeSurroundingTextError.CursorBadPosition);
        }

        if (!IsUtf8Boundary(text, anchor, byteCount))
        {
            throw new ImeSurroundingTextException(ImeSurroundingTextError.AnchorBadPosition);
        }

        Text = text;
        Cursor = cursor;
        Anchor = anchor;
    }

    public string Text { get; }

    public nuint Cursor { get; }

    public nuint Anchor { get; }

    public string IntoText()
    {
        return Text;
    }

    public bool Equals(ImeSurroundingText? other)
    {
        return other is not null && Text == other.Text && Cursor == other.Cursor && Anchor == other.Anchor;
    }

    public override bool Equals(object? obj)
    {
        return obj is ImeSurroundingText other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Text, Cursor, Anchor);
    }

    private static bool IsUtf8Boundary(string text, nuint byteIndex, int byteCount)
    {
        if (byteIndex > (nuint)byteCount)
        {
            return false;
        }

        if (byteIndex == 0 || byteIndex == (nuint)byteCount)
        {
            return true;
        }

        int currentByteIndex = 0;
        int target = checked((int)byteIndex);

        for (int i = 0; i < text.Length;)
        {
            if (currentByteIndex == target)
            {
                return true;
            }

            int charLength = char.IsHighSurrogate(text[i])
                && i + 1 < text.Length
                && char.IsLowSurrogate(text[i + 1])
                    ? 2
                    : 1;

            currentByteIndex += Encoding.UTF8.GetByteCount(text.AsSpan(i, charLength));

            if (currentByteIndex > target)
            {
                return false;
            }

            i += charLength;
        }

        return currentByteIndex == target;
    }
}

public record struct ImeRequest
{
    public readonly record struct Enable(ImeEnableRequest Value);

    public readonly record struct Update(ImeRequestData Value);

    public readonly record struct Disable;

    private const byte DisableTag = 0;
    private const byte EnableTag = 1;
    private const byte UpdateTag = 2;

    private byte _tag;
    private Enable _enable;
    private Update _update;
    private Disable _disable;

    public ImeRequest(Enable value)
    {
        this = default;
        _tag = EnableTag;
        _enable = value;
    }

    public ImeRequest(Update value)
    {
        this = default;
        _tag = UpdateTag;
        _update = value;
    }

    public ImeRequest(Disable value)
    {
        this = default;
        _tag = DisableTag;
        _disable = value;
    }

    public bool TryGetValue(out Enable value)
    {
        value = _enable;
        return _tag == EnableTag;
    }

    public bool TryGetValue(out Update value)
    {
        value = _update;
        return _tag == UpdateTag;
    }

    public bool TryGetValue(out Disable value)
    {
        value = _disable;
        return _tag == DisableTag;
    }
}

public readonly record struct ImeEnableRequest(ImeCapabilities Capabilities, ImeRequestData RequestData)
{
    public static ImeEnableRequest? Create(ImeCapabilities capabilities, ImeRequestData requestData)
    {
        if (capabilities.CursorArea() ^ (requestData.CursorArea is not null))
        {
            return null;
        }

        if (capabilities.HintAndPurpose() ^ (requestData.HintAndPurpose is not null))
        {
            return null;
        }

        if (capabilities.SurroundingText() ^ (requestData.SurroundingText is not null))
        {
            return null;
        }

        return new ImeEnableRequest(capabilities, requestData);
    }

    public (ImeCapabilities Capabilities, ImeRequestData RequestData) IntoRaw()
    {
        return (Capabilities, RequestData);
    }
}

public readonly record struct ImeCapabilities
{
    private readonly ImeCapabilitiesFlags _flags;

    public static ImeCapabilities New()
    {
        return default;
    }

    public ImeCapabilities WithHintAndPurpose()
    {
        return new ImeCapabilities(_flags | ImeCapabilitiesFlags.HintAndPurpose);
    }

    public ImeCapabilities WithoutHintAndPurpose()
    {
        return new ImeCapabilities(_flags & ~ImeCapabilitiesFlags.HintAndPurpose);
    }

    public bool HintAndPurpose()
    {
        return (_flags & ImeCapabilitiesFlags.HintAndPurpose) != 0;
    }

    public ImeCapabilities WithCursorArea()
    {
        return new ImeCapabilities(_flags | ImeCapabilitiesFlags.CursorArea);
    }

    public ImeCapabilities WithoutCursorArea()
    {
        return new ImeCapabilities(_flags & ~ImeCapabilitiesFlags.CursorArea);
    }

    public bool CursorArea()
    {
        return (_flags & ImeCapabilitiesFlags.CursorArea) != 0;
    }

    public ImeCapabilities WithSurroundingText()
    {
        return new ImeCapabilities(_flags | ImeCapabilitiesFlags.SurroundingText);
    }

    public ImeCapabilities WithoutSurroundingText()
    {
        return new ImeCapabilities(_flags & ~ImeCapabilitiesFlags.SurroundingText);
    }

    public bool SurroundingText()
    {
        return (_flags & ImeCapabilitiesFlags.SurroundingText) != 0;
    }

    private ImeCapabilities(ImeCapabilitiesFlags flags)
    {
        _flags = flags;
    }

    [Flags]
    private enum ImeCapabilitiesFlags : byte
    {
        None = 0,
        HintAndPurpose = 1 << 0,
        CursorArea = 1 << 1,
        SurroundingText = 1 << 2,
    }
}

public sealed record class ImeRequestData
{
    public (ImeHint Hint, ImePurpose Purpose)? HintAndPurpose { get; init; }

    public (Position Position, Size Size)? CursorArea { get; init; }

    public ImeSurroundingText? SurroundingText { get; init; }

    public static ImeRequestData Default => new();

    public ImeRequestData WithHintAndPurpose(ImeHint hint, ImePurpose purpose)
    {
        return this with { HintAndPurpose = (hint, purpose) };
    }

    public ImeRequestData WithCursorArea(Position position, Size size)
    {
        return this with { CursorArea = (position, size) };
    }

    public ImeRequestData WithSurroundingText(ImeSurroundingText surroundingText)
    {
        return this with { SurroundingText = surroundingText };
    }
}

public readonly record struct ActivationToken(string Token)
{
    public static ActivationToken FromRaw(string token)
    {
        return new ActivationToken(token);
    }

    public string IntoRaw()
    {
        return Token;
    }

    public string AsRaw()
    {
        return Token;
    }
}
