using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.X11.Ime;

internal sealed class ImeContextCreationException(string message) : Exception(message);

internal sealed class ImeContext : IDisposable
{
    private readonly GCHandle _clientDataHandle;
    private bool _disposed;

    private ImeContext(nint ic, XRectangle icArea, bool allowed, GCHandle clientDataHandle)
    {
        Ic = ic;
        IcArea = icArea;
        IsAllowed = allowed;
        _clientDataHandle = clientDataHandle;
    }

    public nint Ic { get; }

    public XRectangle IcArea { get; private set; }

    public bool IsAllowed { get; }

    public static ImeContext New(
        XConnection xconn,
        InputMethod im,
        XlibWindow window,
        XRectangle? icArea,
        ImeInner eventSender,
        bool allowed)
    {
        ImeContextClientData clientData = new(window, eventSender);
        GCHandle clientDataHandle = GCHandle.Alloc(clientData);
        nint clientDataPtr = GCHandle.ToIntPtr(clientDataHandle);

        Style style = allowed ? im.PreeditStyle : im.NoneStyle;
        nint ic = style.Kind switch
        {
            StyleKind.Preedit => CreatePreeditIc(xconn, im.Im, style.Value, window, clientDataPtr),
            StyleKind.Nothing => CreateBasicIc(im.Im, style.Value, window),
            StyleKind.None => CreateBasicIc(im.Im, style.Value, window),
            _ => 0,
        };

        if (ic == 0)
        {
            clientDataHandle.Free();
            throw new ImeContextCreationException("XCreateIC returned NULL.");
        }

        xconn.CheckErrors();

        ImeContext context = new(ic, default, allowed, clientDataHandle);
        if (icArea is { } area)
        {
            context.SetArea(xconn, area.X, area.Y, area.Width, area.Height);
        }

        return context;
    }

    public void Focus(XConnection xconn)
    {
        PInvoke.XSetICFocus(Ic);
        xconn.CheckErrors();
    }

    public void Unfocus(XConnection xconn)
    {
        PInvoke.XUnsetICFocus(Ic);
        xconn.CheckErrors();
    }

    public unsafe void SetArea(XConnection xconn, short x, short y, ushort width, ushort height)
    {
        XRectangle icArea = new()
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };

        if (!IsAllowed || IcArea == icArea)
        {
            return;
        }

        IcArea = icArea;
        XPoint icSpot = new()
        {
            X = SaturatingAdd(x, width),
            Y = SaturatingAdd(y, height),
        };

        fixed (byte* spotLocationName = ImeNative.XNSpotLocation)
        fixed (byte* areaName = ImeNative.XNArea)
        fixed (byte* preeditAttributesName = ImeNative.XNPreeditAttributes)
        {
            XRectangle areaForCall = IcArea;
            nint preeditAttributes = PInvoke.XVaCreateNestedListPreeditArea(
                0,
                (sbyte*)spotLocationName,
                &icSpot,
                (sbyte*)areaName,
                &areaForCall,
                0);

            if (preeditAttributes == 0)
            {
                return;
            }

            try
            {
                _ = PInvoke.XSetICValuesPreeditAttributes(
                    Ic,
                    (sbyte*)preeditAttributesName,
                    preeditAttributes,
                    0);
            }
            finally
            {
                _ = PInvoke.XFree(preeditAttributes);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_clientDataHandle.IsAllocated)
        {
            _clientDataHandle.Free();
        }

        _disposed = true;
    }

    private static unsafe nint CreateBasicIc(nint im, nuint style, XlibWindow window)
    {
        fixed (byte* inputStyleName = ImeNative.XNInputStyle)
        fixed (byte* clientWindowName = ImeNative.XNClientWindow)
        {
            return PInvoke.XCreateICBasic(
                im,
                (sbyte*)inputStyleName,
                style,
                (sbyte*)clientWindowName,
                window,
                0);
        }
    }

    private static unsafe nint CreatePreeditIc(
        XConnection xconn,
        nint im,
        nuint style,
        XlibWindow window,
        nint clientData)
    {
        PreeditCallbacks callbacks = PreeditCallbacks.New(clientData);
        fixed (byte* startCallbackName = ImeNative.XNPreeditStartCallback)
        fixed (byte* doneCallbackName = ImeNative.XNPreeditDoneCallback)
        fixed (byte* caretCallbackName = ImeNative.XNPreeditCaretCallback)
        fixed (byte* drawCallbackName = ImeNative.XNPreeditDrawCallback)
        fixed (byte* inputStyleName = ImeNative.XNInputStyle)
        fixed (byte* clientWindowName = ImeNative.XNClientWindow)
        fixed (byte* preeditAttributesName = ImeNative.XNPreeditAttributes)
        {
            nint preeditAttributes = PInvoke.XVaCreateNestedListPreeditCallbacks(
                0,
                (sbyte*)startCallbackName,
                &callbacks.StartCallback,
                (sbyte*)doneCallbackName,
                &callbacks.DoneCallback,
                (sbyte*)caretCallbackName,
                &callbacks.CaretCallback,
                (sbyte*)drawCallbackName,
                &callbacks.DrawCallback,
                0);

            if (preeditAttributes == 0)
            {
                throw new ImeContextCreationException("XVaCreateNestedList returned NULL.");
            }

            try
            {
                return PInvoke.XCreateICWithPreeditAttributes(
                    im,
                    (sbyte*)inputStyleName,
                    style,
                    (sbyte*)clientWindowName,
                    window,
                    (sbyte*)preeditAttributesName,
                    preeditAttributes,
                    0);
            }
            finally
            {
                _ = PInvoke.XFree(preeditAttributes);
            }
        }
    }

    private static short SaturatingAdd(short value, ushort amount)
    {
        int result = value + amount;
        return result > short.MaxValue ? short.MaxValue : (short)result;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void PreeditStartCallback(nint xim, nint clientData, nint callData)
    {
        if (ImeContextClientData.FromClientData(clientData) is not { } data)
        {
            return;
        }

        data.Text.Clear();
        data.CursorPosition = 0;
        data.EventSender.EnqueueEvent(data.Window, new ImeEvent(new ImeEvent.Start()));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void PreeditDoneCallback(nint xim, nint clientData, nint callData)
    {
        if (ImeContextClientData.FromClientData(clientData) is not { } data)
        {
            return;
        }

        data.Text.Clear();
        data.CursorPosition = 0;
        data.EventSender.EnqueueEvent(data.Window, new ImeEvent(new ImeEvent.End()));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void PreeditDrawCallback(nint xim, nint clientData, nint callData)
    {
        if (ImeContextClientData.FromClientData(clientData) is not { } data || callData == 0)
        {
            return;
        }

        XIMPreeditDrawCallbackStruct* draw = (XIMPreeditDrawCallbackStruct*)callData;
        data.CursorPosition = Math.Max(0, draw->Caret);

        int start = draw->ChgFirst;
        int end = draw->ChgFirst + draw->ChgLength;
        if (start < 0 || end < start || end > data.Text.Count)
        {
            return;
        }

        List<char> newChars = [];
        if (draw->Text is not null && draw->Text->EncodingIsWchar == 0 && draw->Text->MultiByte is not null)
        {
            string value = Marshal.PtrToStringUTF8((nint)draw->Text->MultiByte) ?? string.Empty;
            newChars.AddRange(value);
        }

        data.Text.RemoveRange(start, end - start);
        data.Text.InsertRange(start, newChars);

        nuint cursorBytePosition = CalcBytePosition(data.Text, Math.Min(data.CursorPosition, data.Text.Count));
        data.EventSender.EnqueueEvent(
            data.Window,
            new ImeEvent(new ImeEvent.Update(new string([.. data.Text]), cursorBytePosition)));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void PreeditCaretCallback(nint xim, nint clientData, nint callData)
    {
        if (ImeContextClientData.FromClientData(clientData) is not { } data || callData == 0)
        {
            return;
        }

        XIMPreeditCaretCallbackStruct* caret = (XIMPreeditCaretCallbackStruct*)callData;
        if (caret->Direction != PInvoke.XIMAbsolutePosition)
        {
            return;
        }

        data.CursorPosition = Math.Max(0, caret->Position);
        nuint cursorBytePosition = CalcBytePosition(data.Text, Math.Min(data.CursorPosition, data.Text.Count));
        data.EventSender.EnqueueEvent(
            data.Window,
            new ImeEvent(new ImeEvent.Update(new string([.. data.Text]), cursorBytePosition)));
    }

    private static nuint CalcBytePosition(List<char> text, int position)
    {
        nuint bytePosition = 0;
        for (int i = 0; i < position && i < text.Count; i++)
        {
            bytePosition += (nuint)System.Text.Encoding.UTF8.GetByteCount(text[i].ToString());
        }

        return bytePosition;
    }

    private unsafe struct PreeditCallbacks
    {
        public XIMCallback StartCallback;
        public XIMCallback DoneCallback;
        public XIMCallback DrawCallback;
        public XIMCallback CaretCallback;

        public static PreeditCallbacks New(nint clientData)
        {
            return new PreeditCallbacks
            {
                StartCallback = CreateCallback(clientData, &PreeditStartCallback),
                DoneCallback = CreateCallback(clientData, &PreeditDoneCallback),
                DrawCallback = CreateCallback(clientData, &PreeditDrawCallback),
                CaretCallback = CreateCallback(clientData, &PreeditCaretCallback),
            };
        }

        private static XIMCallback CreateCallback(
            nint clientData,
            delegate* unmanaged[Cdecl]<nint, nint, nint, void> callback)
        {
            return new XIMCallback
            {
                ClientData = clientData,
                Callback = callback,
            };
        }
    }

    private sealed class ImeContextClientData(XlibWindow window, ImeInner eventSender)
    {
        public XlibWindow Window { get; } = window;

        public ImeInner EventSender { get; } = eventSender;

        public List<char> Text { get; } = [];

        public int CursorPosition { get; set; }

        public static ImeContextClientData? FromClientData(nint clientData)
        {
            if (clientData == 0)
            {
                return null;
            }

            GCHandle handle = GCHandle.FromIntPtr(clientData);
            return handle.Target as ImeContextClientData;
        }
    }
}
