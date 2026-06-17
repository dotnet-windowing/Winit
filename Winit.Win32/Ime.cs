using System.Text;
using Winit.Dpi;

namespace Winit.Win32;

internal sealed unsafe class ImeContext : IDisposable
{
    private const int SmImeEnabled = 82;
    private const uint AttrTargetConverted = 0x01;
    private const uint AttrTargetNotConverted = 0x03;
    private const uint CfsPoint = 0x0002;
    private const uint CfsExclude = 0x0080;
    private const uint GcsCompAttr = 0x0010;
    private const uint GcsCompStr = 0x0008;
    private const uint GcsCursorPos = 0x0080;
    private const uint GcsResultStr = 0x0800;
    private const uint IaceChildren = 0x0001;
    private const uint IaceDefault = 0x0010;

    private readonly HWND _hwnd;
    private readonly nint _himc;

    private ImeContext(HWND hwnd, nint himc)
    {
        _hwnd = hwnd;
        _himc = himc;
    }

    public static ImeContext Current(HWND hwnd)
    {
        return new ImeContext(hwnd, PInvoke.ImmGetContext(hwnd));
    }

    public static void SetImeAllowed(HWND hwnd, bool allowed)
    {
        if (!SystemHasIme())
        {
            return;
        }

        _ = PInvoke.ImmAssociateContextEx(hwnd, nint.Zero, allowed ? IaceDefault : IaceChildren);
    }

    public static bool SystemHasIme()
    {
        return PInvoke.GetSystemMetrics(SmImeEnabled) != 0;
    }

    public string? GetComposedText()
    {
        return GetCompositionString(GcsResultStr);
    }

    public (string Text, nuint? First, nuint? Last)? GetComposingTextAndCursor()
    {
        string? text = GetCompositionString(GcsCompStr);
        if (text is null)
        {
            return null;
        }

        byte[] attrs = GetCompositionData(GcsCompAttr) ?? [];
        nuint? first = null;
        nuint? last = null;
        int utf16Index = 0;
        int utf8Boundary = 0;

        while (utf16Index < text.Length)
        {
            if (utf16Index >= attrs.Length)
            {
                break;
            }

            byte attr = attrs[utf16Index];
            bool targeted = attr == AttrTargetConverted || attr == AttrTargetNotConverted;
            if (first is null && targeted)
            {
                first = (nuint)utf8Boundary;
            }
            else if (first is not null && last is null && !targeted)
            {
                last = (nuint)utf8Boundary;
            }

            int charLength = char.IsHighSurrogate(text[utf16Index])
                && utf16Index + 1 < text.Length
                && char.IsLowSurrogate(text[utf16Index + 1])
                    ? 2
                    : 1;
            utf8Boundary += Encoding.UTF8.GetByteCount(text.AsSpan(utf16Index, charLength));
            utf16Index += charLength;
        }

        if (first is not null && last is null)
        {
            last = (nuint)Encoding.UTF8.GetByteCount(text);
        }
        else if (first is null)
        {
            nuint? cursor = GetCompositionCursor(text);
            first = cursor;
            last = cursor;
        }

        return (text, first, last);
    }

    public void SetImeCursorArea(Position position, Size size, double scaleFactor)
    {
        if (_himc == nint.Zero || !SystemHasIme())
        {
            return;
        }

        PhysicalPosition<int> physicalPosition = position.ToPhysical<int>(scaleFactor);
        PhysicalSize<int> physicalSize = size.ToPhysical<int>(scaleFactor);
        RECT area = new()
        {
            left = physicalPosition.X,
            top = physicalPosition.Y,
            right = physicalPosition.X + physicalSize.Width,
            bottom = physicalPosition.Y + physicalSize.Height,
        };
        CandidateForm candidateForm = new()
        {
            Index = 0,
            Style = CfsExclude,
            CurrentPosition = new NativePoint { X = physicalPosition.X, Y = physicalPosition.Y },
            Area = area,
        };
        CompositionForm compositionForm = new()
        {
            Style = CfsPoint,
            CurrentPosition = new NativePoint
            {
                X = physicalPosition.X,
                Y = physicalPosition.Y + physicalSize.Height,
            },
            Area = area,
        };

        _ = PInvoke.ImmSetCompositionWindow(_himc, ref compositionForm);
        _ = PInvoke.ImmSetCandidateWindow(_himc, ref candidateForm);
    }

    public void Dispose()
    {
        if (_himc != nint.Zero)
        {
            _ = PInvoke.ImmReleaseContext(_hwnd, _himc);
        }
    }

    private nuint? GetCompositionCursor(string text)
    {
        int cursor = PInvoke.ImmGetCompositionStringW(_himc, GcsCursorPos, null, 0);
        return cursor >= 0 ? Utf8ByteIndexFromUtf16Index(text, cursor) : null;
    }

    private string? GetCompositionString(uint gcsMode)
    {
        byte[]? data = GetCompositionData(gcsMode);
        if (data is null || data.Length % sizeof(char) != 0)
        {
            return null;
        }

        if (data.Length == 0)
        {
            return string.Empty;
        }

        char[] chars = new char[data.Length / sizeof(char)];
        Buffer.BlockCopy(data, 0, chars, 0, data.Length);
        return new string(chars);
    }

    private byte[]? GetCompositionData(uint gcsMode)
    {
        if (_himc == nint.Zero)
        {
            return null;
        }

        int size = PInvoke.ImmGetCompositionStringW(_himc, gcsMode, null, 0);
        if (size < 0)
        {
            return null;
        }

        if (size == 0)
        {
            return [];
        }

        byte[] buffer = new byte[size];
        fixed (byte* bufferPtr = buffer)
        {
            int read = PInvoke.ImmGetCompositionStringW(_himc, gcsMode, bufferPtr, (uint)buffer.Length);
            if (read < 0)
            {
                return null;
            }

            if (read != buffer.Length)
            {
                Array.Resize(ref buffer, read);
            }
        }

        return buffer;
    }

    private static nuint Utf8ByteIndexFromUtf16Index(string text, int utf16Index)
    {
        utf16Index = Math.Clamp(utf16Index, 0, text.Length);
        return (nuint)Encoding.UTF8.GetByteCount(text.AsSpan(0, utf16Index));
    }

}
