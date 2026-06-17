using Winit.Core;
using Winit.Dpi;

namespace Winit.Wayland;

internal readonly record struct FrameGeometry(int X, int Y, int Width, int Height);

internal enum FrameSurfaceRole
{
    Top,
    Left,
    Right,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    MinimizeButton,
    MaximizeButton,
    CloseButton,
}

internal readonly record struct FrameSurfaceData(Window Window, FrameSurfaceRole Role);

internal sealed unsafe class ClientSideFrame : IDisposable
{
    private const int Border = 4;
    private const int Titlebar = 32;

    private readonly WinitState _state;
    private readonly WlSurface _parent;
    private readonly FrameSurface _top;
    private readonly FrameSurface _left;
    private readonly FrameSurface _right;
    private readonly FrameSurface _bottom;
    private readonly FrameSurface _topLeft;
    private readonly FrameSurface _topRight;
    private readonly FrameSurface _bottomLeft;
    private readonly FrameSurface _bottomRight;
    private readonly FrameSurface _minimizeButton;
    private readonly FrameSurface _maximizeButton;
    private readonly FrameSurface _closeButton;
    private bool _disposed;
    private bool _hidden;
    private LogicalSize<uint> _clientSize;
    private Theme? _theme;
    private string _title = string.Empty;
    private bool _canMaximize = true;
    private bool _canMinimize = true;
    private bool _resizable = true;
    private double _scaleFactor = 1.0;

    private ClientSideFrame(
        WinitState state,
        WlSurface parent,
        FrameSurface top,
        FrameSurface left,
        FrameSurface right,
        FrameSurface bottom,
        FrameSurface topLeft,
        FrameSurface topRight,
        FrameSurface bottomLeft,
        FrameSurface bottomRight,
        FrameSurface minimizeButton,
        FrameSurface maximizeButton,
        FrameSurface closeButton)
    {
        _state = state;
        _parent = parent;
        _top = top;
        _left = left;
        _right = right;
        _bottom = bottom;
        _topLeft = topLeft;
        _topRight = topRight;
        _bottomLeft = bottomLeft;
        _bottomRight = bottomRight;
        _minimizeButton = minimizeButton;
        _maximizeButton = maximizeButton;
        _closeButton = closeButton;
    }

    public bool IsHidden => _hidden;

    public static ClientSideFrame? TryCreate(WinitState state, Window window, WlSurface parent)
    {
        if (state.Subcompositor.IsNull || parent.IsNull)
        {
            return null;
        }

        FrameSurface? top = null;
        FrameSurface? left = null;
        FrameSurface? right = null;
        FrameSurface? bottom = null;
        FrameSurface? topLeft = null;
        FrameSurface? topRight = null;
        FrameSurface? bottomLeft = null;
        FrameSurface? bottomRight = null;
        FrameSurface? minimizeButton = null;
        FrameSurface? maximizeButton = null;
        FrameSurface? closeButton = null;
        try
        {
            top = FrameSurface.Create(state, window, parent, FrameSurfaceRole.Top);
            left = FrameSurface.Create(state, window, parent, FrameSurfaceRole.Left);
            right = FrameSurface.Create(state, window, parent, FrameSurfaceRole.Right);
            bottom = FrameSurface.Create(state, window, parent, FrameSurfaceRole.Bottom);
            topLeft = FrameSurface.Create(state, window, parent, FrameSurfaceRole.TopLeft);
            topRight = FrameSurface.Create(state, window, parent, FrameSurfaceRole.TopRight);
            bottomLeft = FrameSurface.Create(state, window, parent, FrameSurfaceRole.BottomLeft);
            bottomRight = FrameSurface.Create(state, window, parent, FrameSurfaceRole.BottomRight);
            minimizeButton = FrameSurface.Create(state, window, parent, FrameSurfaceRole.MinimizeButton);
            maximizeButton = FrameSurface.Create(state, window, parent, FrameSurfaceRole.MaximizeButton);
            closeButton = FrameSurface.Create(state, window, parent, FrameSurfaceRole.CloseButton);
            return new ClientSideFrame(
                state,
                parent,
                top,
                left,
                right,
                bottom,
                topLeft,
                topRight,
                bottomLeft,
                bottomRight,
                minimizeButton,
                maximizeButton,
                closeButton);
        }
        catch
        {
            closeButton?.Dispose();
            maximizeButton?.Dispose();
            minimizeButton?.Dispose();
            bottomRight?.Dispose();
            bottomLeft?.Dispose();
            topRight?.Dispose();
            topLeft?.Dispose();
            bottom?.Dispose();
            right?.Dispose();
            left?.Dispose();
            top?.Dispose();
            return null;
        }
    }

    public LogicalSize<uint> OuterSize(LogicalSize<uint> clientSize)
    {
        int border = EffectiveBorder;
        return _hidden
            ? clientSize
            : new LogicalSize<uint>(
                clientSize.Width + (uint)(border * 2),
                clientSize.Height + Titlebar + (uint)border);
    }

    public (int Width, int Height) ClientSizeFromOuterSize(int width, int height)
    {
        int border = EffectiveBorder;
        return _hidden || width <= 0 || height <= 0
            ? (width, height)
            : (
                Math.Max(1, width - border * 2),
                Math.Max(1, height - Titlebar - border));
    }

    public (int Width, int Height) ClientBoundsFromOuterBounds(int width, int height)
    {
        if (_hidden)
        {
            return (width, height);
        }

        int border = EffectiveBorder;
        int clientWidth = width > 0 ? Math.Max(1, width - border * 2) : width;
        int clientHeight = height > 0 ? Math.Max(1, height - Titlebar - border) : height;
        return (clientWidth, clientHeight);
    }

    public FrameGeometry Geometry(LogicalSize<uint> clientSize)
    {
        LogicalSize<uint> outer = OuterSize(clientSize);
        return _hidden
            ? new FrameGeometry(0, 0, checked((int)clientSize.Width), checked((int)clientSize.Height))
            : new FrameGeometry(
                -EffectiveBorder,
                -Titlebar,
                checked((int)outer.Width),
                checked((int)outer.Height));
    }

    public void SetHidden(bool hidden)
    {
        if (_hidden == hidden)
        {
            return;
        }

        _hidden = hidden;
        if (hidden)
        {
            _top.Unmap();
            _left.Unmap();
            _right.Unmap();
            _bottom.Unmap();
            _topLeft.Unmap();
            _topRight.Unmap();
            _bottomLeft.Unmap();
            _bottomRight.Unmap();
            _minimizeButton.Unmap();
            _maximizeButton.Unmap();
            _closeButton.Unmap();
            return;
        }

        Resize(_clientSize);
    }

    public void SetTheme(Theme? theme)
    {
        if (_theme == theme)
        {
            return;
        }

        _theme = theme;
        Resize(_clientSize);
    }

    public void SetTitle(string title)
    {
        if (_title == title)
        {
            return;
        }

        _title = title;
        Resize(_clientSize);
    }

    public void SetResizable(bool resizable)
    {
        if (_resizable == resizable)
        {
            return;
        }

        _resizable = resizable;
        Resize(_clientSize);
    }

    public void SetCapabilities(bool canMaximize, bool canMinimize)
    {
        if (_canMaximize == canMaximize && _canMinimize == canMinimize)
        {
            return;
        }

        _canMaximize = canMaximize;
        _canMinimize = canMinimize;
        Resize(_clientSize);
    }

    public void SetScalingFactor(double scaleFactor)
    {
        if (scaleFactor <= 0.0 || Math.Abs(_scaleFactor - scaleFactor) < double.Epsilon)
        {
            return;
        }

        _scaleFactor = scaleFactor;
        Resize(_clientSize);
    }

    public bool Refresh()
    {
        return false;
    }

    public void Resize(LogicalSize<uint> clientSize)
    {
        _clientSize = clientSize;
        if (_hidden || _disposed || _parent.IsNull)
        {
            return;
        }

        int width = checked((int)Math.Max(1, clientSize.Width));
        int height = checked((int)Math.Max(1, clientSize.Height));
        int border = EffectiveBorder;
        uint titleColor = _theme == Theme.Light ? 0xffe8eaed : 0xff2f3338;
        uint borderColor = _theme == Theme.Light ? 0xffc8ccd2 : 0xff1e2227;
        uint minimizeColor = _theme == Theme.Light ? 0xff8a8f98 : 0xffb6bbc4;
        uint maximizeColor = _theme == Theme.Light ? 0xff5c84c7 : 0xff79a8ee;
        uint closeColor = 0xffd94f45;
        uint titleTextColor = _theme == Theme.Light ? 0xff1f2328 : 0xfff0f3f6;
        const int buttonSize = 20;
        const int buttonGap = 8;
        const int buttonY = -26;

        _top.Update(-border, -Titlebar, width + border * 2, Titlebar, titleColor, _scaleFactor, _title, titleTextColor);
        if (_resizable)
        {
            _left.Update(-border, 0, border, height, borderColor, _scaleFactor);
            _right.Update(width, 0, border, height, borderColor, _scaleFactor);
            _bottom.Update(-border, height, width + border * 2, border, borderColor, _scaleFactor);
            _topLeft.Update(-border, -Titlebar, border, Titlebar, borderColor, _scaleFactor);
            _topRight.Update(width, -Titlebar, border, Titlebar, borderColor, _scaleFactor);
            _bottomLeft.Update(-border, height, border, border, borderColor, _scaleFactor);
            _bottomRight.Update(width, height, border, border, borderColor, _scaleFactor);
        }
        else
        {
            _left.Unmap();
            _right.Unmap();
            _bottom.Unmap();
            _topLeft.Unmap();
            _topRight.Unmap();
            _bottomLeft.Unmap();
            _bottomRight.Unmap();
        }

        _closeButton.Update(width - buttonGap - buttonSize, buttonY, buttonSize, buttonSize, closeColor, _scaleFactor);
        if (_canMaximize)
        {
            _maximizeButton.Update(width - buttonGap * 2 - buttonSize * 2, buttonY, buttonSize, buttonSize, maximizeColor, _scaleFactor);
        }
        else
        {
            _maximizeButton.Unmap();
        }

        if (_canMinimize)
        {
            _minimizeButton.Update(width - buttonGap * 3 - buttonSize * 3, buttonY, buttonSize, buttonSize, minimizeColor, _scaleFactor);
        }
        else
        {
            _minimizeButton.Unmap();
        }
    }

    private int EffectiveBorder => _resizable ? Border : 0;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _closeButton.Dispose();
        _maximizeButton.Dispose();
        _minimizeButton.Dispose();
        _bottomRight.Dispose();
        _bottomLeft.Dispose();
        _topRight.Dispose();
        _topLeft.Dispose();
        _bottom.Dispose();
        _right.Dispose();
        _left.Dispose();
        _top.Dispose();
    }
}

internal sealed unsafe class FrameSurface : IDisposable
{
    private readonly WinitState _state;
    private readonly FrameSurfaceRole _role;
    private WlSurface _surface;
    private WlSubsurface _subsurface;
    private SolidBuffer? _buffer;
    private int _width;
    private int _height;
    private int _bufferScale = 1;
    private uint _color;
    private string? _title;
    private uint _titleColor;
    private bool _disposed;

    private FrameSurface(WinitState state, Window window, FrameSurfaceRole role, WlSurface surface, WlSubsurface subsurface)
    {
        _ = window;
        _state = state;
        _role = role;
        _surface = surface;
        _subsurface = subsurface;
    }

    public static FrameSurface Create(WinitState state, Window window, WlSurface parent, FrameSurfaceRole role)
    {
        WlSurface surface = state.CreateSurface();
        WlSubsurface subsurface = WlSubsurface.Null;
        try
        {
            WlArgument* args = stackalloc WlArgument[3];
            args[0].Object = 0;
            args[1].Object = surface.Value;
            args[2].Object = parent.Value;
            WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
                state.Subcompositor,
                WlSubcompositorRequest.GetSubsurface,
                WlCoreInterfaces.Subsurface,
                PInvoke.WlProxyGetVersion(state.Subcompositor),
                WlProxyMarshalFlags.None,
                args);
            state.Connection.CheckError();
            if (proxy.IsNull)
            {
                throw new InvalidOperationException("wl_subcompositor.get_subsurface failed.");
            }

            subsurface = new WlSubsurface(proxy.Value);
            PInvoke.WlProxySetQueue(subsurface, state.Connection.EventQueue);
            PInvoke.WlProxyMarshalArray(subsurface, WlSubsurfaceRequest.SetDesync, null);
            FrameSurface frameSurface = new(state, window, role, surface, subsurface);
            state.RegisterFrameSurface(surface, window, role);
            return frameSurface;
        }
        catch
        {
            if (!subsurface.IsNull)
            {
                DestroySubsurface(subsurface);
            }

            if (!surface.IsNull)
            {
                DestroySurface(surface);
            }

            throw;
        }
    }

    public void Update(
        int x,
        int y,
        int width,
        int height,
        uint argb,
        double scaleFactor,
        string? title = null,
        uint titleColor = 0)
    {
        if (_disposed)
        {
            return;
        }

        width = Math.Max(1, width);
        height = Math.Max(1, height);
        int bufferScale = Math.Max(1, (int)Math.Round(scaleFactor));
        int bufferWidth = checked(width * bufferScale);
        int bufferHeight = checked(height * bufferScale);

        WlArgument* positionArgs = stackalloc WlArgument[2];
        positionArgs[0].Int = x;
        positionArgs[1].Int = y;
        PInvoke.WlProxyMarshalArray(_subsurface, WlSubsurfaceRequest.SetPosition, positionArgs);

        title = _role == FrameSurfaceRole.Top ? title ?? string.Empty : null;
        if (_buffer is null ||
            _width != bufferWidth ||
            _height != bufferHeight ||
            _color != argb ||
            _title != title ||
            _titleColor != titleColor)
        {
            SolidBuffer? oldBuffer = _buffer;
            _buffer = SolidBuffer.Create(_state, bufferWidth, bufferHeight, argb, _role, title, titleColor);
            _width = bufferWidth;
            _height = bufferHeight;
            _color = argb;
            _title = title;
            _titleColor = titleColor;
            oldBuffer?.Dispose();
        }

        _bufferScale = bufferScale;
        Attach(_buffer.Buffer, width, height, bufferWidth, bufferHeight);
    }

    public void Unmap()
    {
        if (_disposed || _surface.IsNull)
        {
            return;
        }

        WlArgument* attachArgs = stackalloc WlArgument[3];
        attachArgs[0].Object = 0;
        attachArgs[1].Int = 0;
        attachArgs[2].Int = 0;
        PInvoke.WlProxyMarshalArray(_surface, WlSurfaceRequest.Attach, attachArgs);
        PInvoke.WlProxyMarshalArray(_surface, WlSurfaceRequest.Commit, null);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _buffer?.Dispose();
        _buffer = null;

        if (!_surface.IsNull)
        {
            _state.RemoveFrameSurface(_surface);
        }

        if (!_subsurface.IsNull)
        {
            DestroySubsurface(_subsurface);
            _subsurface = WlSubsurface.Null;
        }

        if (!_surface.IsNull)
        {
            DestroySurface(_surface);
            _surface = WlSurface.Null;
        }
    }

    private void Attach(WlBuffer buffer, int logicalWidth, int logicalHeight, int bufferWidth, int bufferHeight)
    {
        WlArgument* scaleArgs = stackalloc WlArgument[1];
        scaleArgs[0].Int = _bufferScale;
        PInvoke.WlProxyMarshalArray(_surface, WlSurfaceRequest.SetBufferScale, scaleArgs);

        WlArgument* attachArgs = stackalloc WlArgument[3];
        attachArgs[0].Object = buffer.Value;
        attachArgs[1].Int = 0;
        attachArgs[2].Int = 0;
        PInvoke.WlProxyMarshalArray(_surface, WlSurfaceRequest.Attach, attachArgs);

        WlArgument* damageArgs = stackalloc WlArgument[4];
        damageArgs[0].Int = 0;
        damageArgs[1].Int = 0;
        uint surfaceVersion = PInvoke.WlProxyGetVersion(_surface);
        damageArgs[2].Int = surfaceVersion >= 4 ? bufferWidth : logicalWidth;
        damageArgs[3].Int = surfaceVersion >= 4 ? bufferHeight : logicalHeight;
        PInvoke.WlProxyMarshalArray(
            _surface,
            surfaceVersion >= 4 ? WlSurfaceRequest.DamageBuffer : WlSurfaceRequest.Damage,
            damageArgs);
        PInvoke.WlProxyMarshalArray(_surface, WlSurfaceRequest.Commit, null);
    }

    private static void DestroySubsurface(WlSubsurface subsurface)
    {
        PInvoke.WlProxyMarshalArrayFlags(
            subsurface,
            WlSubsurfaceRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(subsurface),
            WlProxyMarshalFlags.Destroy,
            null);
    }

    private static void DestroySurface(WlSurface surface)
    {
        PInvoke.WlProxyMarshalArrayFlags(
            surface,
            WlSurfaceRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(surface),
            WlProxyMarshalFlags.Destroy,
            null);
    }
}

internal sealed unsafe class SolidBuffer : IDisposable
{
    private readonly nint _mapping;
    private readonly nuint _length;
    private WlBuffer _buffer;
    private bool _disposed;

    private SolidBuffer(WlBuffer buffer, nint mapping, nuint length)
    {
        _buffer = buffer;
        _mapping = mapping;
        _length = length;
    }

    public WlBuffer Buffer => _buffer;

    public static SolidBuffer Create(
        WinitState state,
        int width,
        int height,
        uint argb,
        FrameSurfaceRole role,
        string? title = null,
        uint titleColor = 0)
    {
        int stride = checked(width * 4);
        nuint length = checked((nuint)(stride * height));

        int fd;
        using (Utf8Buffer name = Utf8Buffer.FromString("winit-csd-frame"))
        {
            fd = PInvoke.MemfdCreate(name.Pointer, MemFdFlags.CloExec);
        }

        if (fd < 0)
        {
            throw new InvalidOperationException($"memfd_create failed for Wayland frame errno={System.Runtime.InteropServices.Marshal.GetLastPInvokeError()}.");
        }

        try
        {
            if (PInvoke.Ftruncate(fd, (nint)length) != 0)
            {
                throw new InvalidOperationException($"ftruncate failed for Wayland frame errno={System.Runtime.InteropServices.Marshal.GetLastPInvokeError()}.");
            }

            void* mapping = PInvoke.Mmap(null, length, MmapProtection.Read | MmapProtection.Write, MmapFlags.Shared, fd, 0);
            if ((nint)mapping == MmapFlags.Failed)
            {
                throw new InvalidOperationException($"mmap failed for Wayland frame errno={System.Runtime.InteropServices.Marshal.GetLastPInvokeError()}.");
            }

            try
            {
                Span<byte> pixels = new(mapping, checked((int)length));
                FillArgb8888(pixels, argb);
                DrawFrameRole(pixels, width, height, role);
                if (role == FrameSurfaceRole.Top && !string.IsNullOrEmpty(title))
                {
                    DrawTitle(pixels, width, height, title, titleColor);
                }

                WlShmPool pool = CreatePool(state, fd, checked((int)length));
                try
                {
                    WlBuffer buffer = CreateBuffer(pool, width, height, stride);
                    return new SolidBuffer(buffer, (nint)mapping, length);
                }
                finally
                {
                    DestroyPool(pool);
                }
            }
            catch
            {
                _ = PInvoke.Munmap(mapping, length);
                throw;
            }
        }
        finally
        {
            _ = PInvoke.Close(fd);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_buffer.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _buffer,
                WlBufferRequest.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_buffer),
                WlProxyMarshalFlags.Destroy,
                null);
            _buffer = WlBuffer.Null;
        }

        if (_mapping != 0)
        {
            _ = PInvoke.Munmap((void*)_mapping, _length);
        }
    }

    private static void FillArgb8888(Span<byte> destination, uint argb)
    {
        byte alpha = (byte)(argb >> 24);
        byte red = Premultiply((byte)(argb >> 16), alpha);
        byte green = Premultiply((byte)(argb >> 8), alpha);
        byte blue = Premultiply((byte)argb, alpha);

        for (int i = 0; i < destination.Length; i += 4)
        {
            destination[i] = blue;
            destination[i + 1] = green;
            destination[i + 2] = red;
            destination[i + 3] = alpha;
        }
    }

    private static byte Premultiply(byte channel, byte alpha)
    {
        return (byte)(channel * alpha / byte.MaxValue);
    }

    private static void DrawFrameRole(Span<byte> pixels, int width, int height, FrameSurfaceRole role)
    {
        switch (role)
        {
            case FrameSurfaceRole.CloseButton:
                DrawX(pixels, width, height, 0xffffffff);
                break;
            case FrameSurfaceRole.MaximizeButton:
                DrawBox(pixels, width, height, 0xffffffff);
                break;
            case FrameSurfaceRole.MinimizeButton:
                DrawLine(pixels, width, height, 0xffffffff);
                break;
        }
    }

    private static void DrawTitle(Span<byte> pixels, int width, int height, string title, uint argb)
    {
        int scale = Math.Max(1, height / 24);
        int glyphWidth = 5 * scale;
        int glyphHeight = 7 * scale;
        int gap = Math.Max(1, scale);
        int x = 12 * scale;
        int y = Math.Max(0, (height - glyphHeight) / 2);
        int maxX = Math.Max(x, width - 96 * scale);

        foreach (char c in title)
        {
            if (x + glyphWidth > maxX)
            {
                break;
            }

            DrawGlyph(pixels, width, x, y, scale, c, argb);
            x += glyphWidth + gap;
        }
    }

    private static void DrawGlyph(Span<byte> pixels, int width, int x, int y, int scale, char c, uint argb)
    {
        string rows = GlyphRows(c);
        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (rows[row * 5 + col] != '1')
                {
                    continue;
                }

                FillRect(pixels, width, x + col * scale, y + row * scale, scale, scale, argb);
            }
        }
    }

    private static string GlyphRows(char c)
    {
        return char.ToUpperInvariant(c) switch
        {
            'A' => "01110100011000111111100011000110001",
            'B' => "11110100011000111110100011000111110",
            'C' => "01111100001000010000100001000001111",
            'D' => "11110100011000110001100011000111110",
            'E' => "11111100001000011110100001000011111",
            'F' => "11111100001000011110100001000010000",
            'G' => "01111100001000010111100011000101110",
            'H' => "10001100011000111111100011000110001",
            'I' => "11111001000010000100001000010011111",
            'J' => "00111000100001000010000101001001100",
            'K' => "10001100101010011000101001001010001",
            'L' => "10000100001000010000100001000011111",
            'M' => "10001110111010110101100011000110001",
            'N' => "10001110011010110011100011000110001",
            'O' => "01110100011000110001100011000101110",
            'P' => "11110100011000111110100001000010000",
            'Q' => "01110100011000110001101011001001101",
            'R' => "11110100011000111110101001001010001",
            'S' => "01111100001000001110000010000111110",
            'T' => "11111001000010000100001000010000100",
            'U' => "10001100011000110001100011000101110",
            'V' => "10001100011000110001100010101000100",
            'W' => "10001100011000110101101011101110001",
            'X' => "10001010100010000100001000101010001",
            'Y' => "10001010100010000100001000010000100",
            'Z' => "11111000010001000100010001000011111",
            '0' => "01110100011001110101110011000101110",
            '1' => "00100011000010000100001000010001110",
            '2' => "01110100010000100010001000100011111",
            '3' => "11110000010000101110000010000111110",
            '4' => "00010001100101010010111110001000010",
            '5' => "11111100001111000001000011000101110",
            '6' => "00110010001000011110100011000101110",
            '7' => "11111000010001000100010000100001000",
            '8' => "01110100011000101110100011000101110",
            '9' => "01110100011000101111000010001011100",
            ' ' => "00000000000000000000000000000000000",
            '-' => "00000000000000011111000000000000000",
            '_' => "00000000000000000000000000000011111",
            '.' => "00000000000000000000000000000000100",
            ':' => "00000001000010000000001000010000000",
            '/' => "00001000100010001000100000000000000",
            '\\' => "10000010000010000010000010000000000",
            '(' => "00010001000100001000010000010000010",
            ')' => "01000001000001000010000100100001000",
            '+' => "00000001000010011111001000010000000",
            '#' => "01010111110101011111010101111101010",
            _ => "01110100010000100010001000000000100",
        };
    }

    private static void DrawX(Span<byte> pixels, int width, int height, uint argb)
    {
        int margin = Math.Max(4, Math.Min(width, height) / 4);
        for (int i = margin; i < Math.Min(width, height) - margin; i++)
        {
            SetPixel(pixels, width, i, i, argb);
            SetPixel(pixels, width, i + 1, i, argb);
            int inverse = height - i - 1;
            SetPixel(pixels, width, i, inverse, argb);
            SetPixel(pixels, width, i + 1, inverse, argb);
        }
    }

    private static void DrawBox(Span<byte> pixels, int width, int height, uint argb)
    {
        int margin = Math.Max(4, Math.Min(width, height) / 4);
        int left = margin;
        int right = width - margin - 1;
        int top = margin;
        int bottom = height - margin - 1;
        for (int x = left; x <= right; x++)
        {
            SetPixel(pixels, width, x, top, argb);
            SetPixel(pixels, width, x, top + 1, argb);
            SetPixel(pixels, width, x, bottom, argb);
            SetPixel(pixels, width, x, bottom - 1, argb);
        }

        for (int y = top; y <= bottom; y++)
        {
            SetPixel(pixels, width, left, y, argb);
            SetPixel(pixels, width, left + 1, y, argb);
            SetPixel(pixels, width, right, y, argb);
            SetPixel(pixels, width, right - 1, y, argb);
        }
    }

    private static void DrawLine(Span<byte> pixels, int width, int height, uint argb)
    {
        int margin = Math.Max(4, width / 4);
        int y = height / 2;
        for (int x = margin; x < width - margin; x++)
        {
            SetPixel(pixels, width, x, y, argb);
            SetPixel(pixels, width, x, y + 1, argb);
        }
    }

    private static void FillRect(Span<byte> pixels, int width, int x, int y, int rectWidth, int rectHeight, uint argb)
    {
        for (int row = 0; row < rectHeight; row++)
        {
            for (int col = 0; col < rectWidth; col++)
            {
                SetPixel(pixels, width, x + col, y + row, argb);
            }
        }
    }

    private static void SetPixel(Span<byte> pixels, int width, int x, int y, uint argb)
    {
        if (x < 0 || y < 0 || x >= width)
        {
            return;
        }

        int offset = checked((y * width + x) * 4);
        if ((uint)(offset + 3) >= (uint)pixels.Length)
        {
            return;
        }

        byte alpha = (byte)(argb >> 24);
        pixels[offset] = Premultiply((byte)argb, alpha);
        pixels[offset + 1] = Premultiply((byte)(argb >> 8), alpha);
        pixels[offset + 2] = Premultiply((byte)(argb >> 16), alpha);
        pixels[offset + 3] = alpha;
    }

    private static WlShmPool CreatePool(WinitState state, int fd, int size)
    {
        WlArgument* args = stackalloc WlArgument[3];
        args[0].Object = 0;
        args[1].Fd = fd;
        args[2].Int = size;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            state.Shm,
            WlShmRequest.CreatePool,
            WlCoreInterfaces.ShmPool,
            PInvoke.WlProxyGetVersion(state.Shm),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_shm.create_pool failed for Wayland frame.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        return new WlShmPool(proxy.Value);
    }

    private static WlBuffer CreateBuffer(WlShmPool pool, int width, int height, int stride)
    {
        WlArgument* args = stackalloc WlArgument[6];
        args[0].Object = 0;
        args[1].Int = 0;
        args[2].Int = width;
        args[3].Int = height;
        args[4].Int = stride;
        args[5].Uint = (uint)WlShmFormat.Argb8888;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            pool,
            WlShmPoolRequest.CreateBuffer,
            WlCoreInterfaces.Buffer,
            PInvoke.WlProxyGetVersion(pool),
            WlProxyMarshalFlags.None,
            args);
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("wl_shm_pool.create_buffer failed for Wayland frame.");
        }

        return new WlBuffer(proxy.Value);
    }

    private static void DestroyPool(WlShmPool pool)
    {
        if (pool.IsNull)
        {
            return;
        }

        PInvoke.WlProxyMarshalArrayFlags(
            pool,
            WlShmPoolRequest.Destroy,
            null,
            PInvoke.WlProxyGetVersion(pool),
            WlProxyMarshalFlags.Destroy,
            null);
    }
}
