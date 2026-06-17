using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Winit.Core;
using Winit.Dpi;

namespace Winit.Wayland;

internal sealed unsafe class TextInputState : IDisposable
{
    private ZwpTextInputManagerV3 _manager;
    private bool _disposed;

    private TextInputState(ZwpTextInputManagerV3 manager)
    {
        _manager = manager;
    }

    public static TextInputState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, ZwpInterfaces.TextInputManagerV3, maxVersion: 1);
        return new TextInputState(new ZwpTextInputManagerV3(proxy.Value));
    }

    public WinitTextInput GetTextInput(WinitState state, WlSeat seat)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Object = 0;
        args[1].Object = seat.Value;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _manager,
            ZwpTextInputManagerV3Request.GetTextInput,
            ZwpInterfaces.TextInputV3,
            PInvoke.WlProxyGetVersion(_manager),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("zwp_text_input_manager_v3.get_text_input failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        WinitTextInput textInput = new(state, new ZwpTextInputV3(proxy.Value));
        textInput.InstallDispatcher();
        return textInput;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_manager.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _manager,
                ZwpTextInputManagerV3Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_manager),
                WlProxyMarshalFlags.Destroy,
                null);
            _manager = ZwpTextInputManagerV3.Null;
        }
    }
}

internal sealed unsafe class WinitTextInput : IDisposable
{
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private ZwpTextInputV3 _textInput;
    private WlSurface _surface;
    private string? _pendingCommit;
    private Preedit? _pendingPreedit;
    private PendingDeleteSurroundingText? _pendingDelete;
    private bool _lastPreeditEmpty = true;
    private bool _disposed;

    public WinitTextInput(WinitState state, ZwpTextInputV3 textInput)
    {
        _state = state;
        _textInput = textInput;
        _selfHandle = GCHandle.Alloc(this);
    }

    public ZwpTextInputV3 TextInput => _textInput;

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _textInput,
            &TextInputDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for zwp_text_input_v3.");
        }
    }

    public void SetState(TextInputClientState? state, bool sendEnable)
    {
        if (_textInput.IsNull)
        {
            return;
        }

        if (state is null)
        {
            Disable();
            Commit();
            return;
        }

        if (sendEnable)
        {
            Enable();
        }

        if (state.ContentType is { } contentType)
        {
            SetContentType(contentType);
        }

        if (state.CursorArea is { } cursorArea)
        {
            SetCursorRectangle(cursorArea.Position, cursorArea.Size);
        }

        if (state.SurroundingText is { } surroundingText)
        {
            SetSurroundingText(surroundingText);
        }

        Commit();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_textInput.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _textInput,
                ZwpTextInputV3Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_textInput),
                WlProxyMarshalFlags.Destroy,
                null);
            _textInput = ZwpTextInputV3.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void Enter(WlSurface surface)
    {
        _surface = surface;
        if (!_state.TryGetWindow(surface, out Window window))
        {
            return;
        }

        TextInputClientState? textInputState = window.TextInputState;
        if (textInputState is not null)
        {
            SetState(textInputState, sendEnable: true);
            _state.PushWindowEvent(
                window.Id,
                new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Enabled()))));
        }

        window.TextInputEntered(this);
    }

    private void Leave(WlSurface surface)
    {
        _surface = WlSurface.Null;
        _lastPreeditEmpty = true;

        Disable();
        Commit();

        WindowId windowId = WindowId.FromRaw((nuint)surface.Value);
        if (!_state.Windows.TryGetValue(windowId, out Window? window))
        {
            return;
        }

        window.TextInputLeft(this);
        _state.PushWindowEvent(
            windowId,
            new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Disabled()))));
    }

    private void PreeditString(string text, int cursorBegin, int cursorEnd)
    {
        nuint? begin = ValidUtf8Boundary(text, cursorBegin);
        nuint? end = ValidUtf8Boundary(text, cursorEnd);
        _pendingPreedit = new Preedit(text, begin, end);
    }

    private void CommitString(string text)
    {
        _pendingPreedit = null;
        _pendingCommit = text;
    }

    private void DeleteSurroundingText(uint beforeLength, uint afterLength)
    {
        _pendingDelete = new PendingDeleteSurroundingText(beforeLength, afterLength);
    }

    private void Done()
    {
        if (_surface.IsNull)
        {
            ClearPending();
            return;
        }

        WindowId windowId = WindowId.FromRaw((nuint)_surface.Value);
        if (!_state.Windows.TryGetValue(windowId, out Window? window) ||
            window.TextInputState is null)
        {
            ClearPending();
            return;
        }

        if (_pendingDelete is { } delete)
        {
            _state.PushWindowEvent(
                windowId,
                new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.DeleteSurrounding(
                    delete.Before,
                    delete.After)))));
            _pendingDelete = null;
        }

        if (_pendingCommit is not null || (_pendingPreedit is null && !_lastPreeditEmpty))
        {
            _state.PushWindowEvent(
                windowId,
                new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Preedit(string.Empty, null)))));
            _lastPreeditEmpty = true;
        }

        if (_pendingCommit is { } commit)
        {
            _state.PushWindowEvent(
                windowId,
                new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Commit(commit)))));
            _pendingCommit = null;
        }

        if (_pendingPreedit is { } preedit)
        {
            (nuint Begin, nuint End)? cursorRange = preedit.CursorBegin is { } begin
                ? (begin, preedit.CursorEnd ?? begin)
                : null;

            _lastPreeditEmpty = false;
            _state.PushWindowEvent(
                windowId,
                new WindowEvent(new WindowEvent.Ime(new Ime(new Ime.Preedit(
                    preedit.Text,
                    cursorRange)))));
            _pendingPreedit = null;
        }
    }

    private void Enable()
    {
        PInvoke.WlProxyMarshalArray(_textInput, ZwpTextInputV3Request.Enable, null);
    }

    private void Disable()
    {
        if (!_textInput.IsNull)
        {
            PInvoke.WlProxyMarshalArray(_textInput, ZwpTextInputV3Request.Disable, null);
        }
    }

    private void Commit()
    {
        if (!_textInput.IsNull)
        {
            PInvoke.WlProxyMarshalArray(_textInput, ZwpTextInputV3Request.Commit, null);
            _state.Connection.Flush();
        }
    }

    private void SetContentType(ContentType contentType)
    {
        WlArgument* args = stackalloc WlArgument[2];
        args[0].Uint = (uint)contentType.Hint;
        args[1].Uint = (uint)contentType.Purpose;
        PInvoke.WlProxyMarshalArray(_textInput, ZwpTextInputV3Request.SetContentType, args);
    }

    private void SetCursorRectangle(LogicalPosition<int> position, LogicalSize<int> size)
    {
        WlArgument* args = stackalloc WlArgument[4];
        args[0].Int = position.X;
        args[1].Int = position.Y;
        args[2].Int = size.Width;
        args[3].Int = size.Height;
        PInvoke.WlProxyMarshalArray(_textInput, ZwpTextInputV3Request.SetCursorRectangle, args);
    }

    private void SetSurroundingText(ImeSurroundingText surroundingText)
    {
        using Utf8Buffer text = Utf8Buffer.FromString(surroundingText.Text);
        WlArgument* args = stackalloc WlArgument[3];
        args[0].String = text.Pointer;
        args[1].Int = checked((int)surroundingText.Cursor);
        args[2].Int = checked((int)surroundingText.Anchor);
        PInvoke.WlProxyMarshalArray(_textInput, ZwpTextInputV3Request.SetSurroundingText, args);
    }

    private void ClearPending()
    {
        _pendingCommit = null;
        _pendingPreedit = null;
        _pendingDelete = null;
    }

    private static nuint? ValidUtf8Boundary(string text, int byteIndex)
    {
        if (byteIndex < 0)
        {
            return null;
        }

        int byteCount = Encoding.UTF8.GetByteCount(text);
        if (byteIndex > byteCount)
        {
            return null;
        }

        if (byteIndex == 0 || byteIndex == byteCount)
        {
            return (nuint)byteIndex;
        }

        int current = 0;
        for (int i = 0; i < text.Length;)
        {
            if (current == byteIndex)
            {
                return (nuint)byteIndex;
            }

            int charLength = char.IsHighSurrogate(text[i]) &&
                i + 1 < text.Length &&
                char.IsLowSurrogate(text[i + 1])
                    ? 2
                    : 1;

            current += Encoding.UTF8.GetByteCount(text.AsSpan(i, charLength));
            if (current > byteIndex)
            {
                return null;
            }

            i += charLength;
        }

        return current == byteIndex ? (nuint)byteIndex : null;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TextInputDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null || args is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not WinitTextInput textInput ||
            textInput._disposed)
        {
            return 0;
        }

        switch (opcode)
        {
            case ZwpTextInputV3Event.Enter:
                textInput.Enter(new WlSurface(args[0].Object));
                break;
            case ZwpTextInputV3Event.Leave:
                textInput.Leave(new WlSurface(args[0].Object));
                break;
            case ZwpTextInputV3Event.PreeditString:
                textInput.PreeditString(
                    Marshal.PtrToStringUTF8((nint)args[0].String) ?? string.Empty,
                    args[1].Int,
                    args[2].Int);
                break;
            case ZwpTextInputV3Event.CommitString:
                textInput.CommitString(Marshal.PtrToStringUTF8((nint)args[0].String) ?? string.Empty);
                break;
            case ZwpTextInputV3Event.DeleteSurroundingText:
                textInput.DeleteSurroundingText(args[0].Uint, args[1].Uint);
                break;
            case ZwpTextInputV3Event.Done:
                textInput.Done();
                break;
        }

        return 0;
    }

    private readonly record struct Preedit(string Text, nuint? CursorBegin, nuint? CursorEnd);

    private readonly record struct PendingDeleteSurroundingText(nuint Before, nuint After);
}

internal sealed class TextInputClientState
{
    public TextInputClientState(ImeCapabilities capabilities, ImeRequestData requestData, double scaleFactor)
    {
        Capabilities = capabilities;
        ContentTypeValue = new ContentType(ZwpTextInputV3ContentHint.None, ZwpTextInputV3ContentPurpose.Normal);
        CursorAreaValue = (new LogicalPosition<int>(0, 0), new LogicalSize<int>(0, 0));
        SurroundingTextValue = new ImeSurroundingText(string.Empty, 0, 0);
        Update(requestData, scaleFactor);
    }

    public ImeCapabilities Capabilities { get; }

    public ContentType? ContentType => Capabilities.HintAndPurpose() ? ContentTypeValue : null;

    public (LogicalPosition<int> Position, LogicalSize<int> Size)? CursorArea =>
        Capabilities.CursorArea() ? CursorAreaValue : null;

    public ImeSurroundingText? SurroundingText =>
        Capabilities.SurroundingText() ? SurroundingTextValue : null;

    private ContentType ContentTypeValue { get; set; }

    private (LogicalPosition<int> Position, LogicalSize<int> Size) CursorAreaValue { get; set; }

    private ImeSurroundingText SurroundingTextValue { get; set; }

    public void Update(ImeRequestData requestData, double scaleFactor)
    {
        if (requestData.HintAndPurpose is { } hintAndPurpose && Capabilities.HintAndPurpose())
        {
            ContentTypeValue = global::Winit.Wayland.ContentType.From(
                hintAndPurpose.Hint,
                hintAndPurpose.Purpose);
        }

        if (requestData.CursorArea is { } cursorArea && Capabilities.CursorArea())
        {
            CursorAreaValue = (
                cursorArea.Position.ToLogical<int>(scaleFactor),
                cursorArea.Size.ToLogical<int>(scaleFactor));
        }

        if (requestData.SurroundingText is { } surroundingText && Capabilities.SurroundingText())
        {
            SurroundingTextValue = surroundingText;
        }
    }
}

internal readonly record struct ContentType(
    ZwpTextInputV3ContentHint Hint,
    ZwpTextInputV3ContentPurpose Purpose)
{
    public static ContentType From(ImeHint hint, ImePurpose purpose)
    {
        ZwpTextInputV3ContentPurpose contentPurpose = purpose switch
        {
            ImePurpose.Password => ZwpTextInputV3ContentPurpose.Password,
            ImePurpose.Terminal => ZwpTextInputV3ContentPurpose.Terminal,
            ImePurpose.Phone => ZwpTextInputV3ContentPurpose.Phone,
            ImePurpose.Number => ZwpTextInputV3ContentPurpose.Number,
            ImePurpose.Url => ZwpTextInputV3ContentPurpose.Url,
            ImePurpose.Email => ZwpTextInputV3ContentPurpose.Email,
            ImePurpose.Pin => ZwpTextInputV3ContentPurpose.Pin,
            ImePurpose.Date => ZwpTextInputV3ContentPurpose.Date,
            ImePurpose.Time => ZwpTextInputV3ContentPurpose.Time,
            ImePurpose.DateTime => ZwpTextInputV3ContentPurpose.Datetime,
            _ => ZwpTextInputV3ContentPurpose.Normal,
        };

        ZwpTextInputV3ContentHint contentHint =
            contentPurpose is ZwpTextInputV3ContentPurpose.Password or ZwpTextInputV3ContentPurpose.Pin
                ? ZwpTextInputV3ContentHint.SensitiveData
                : ZwpTextInputV3ContentHint.None;

        if ((hint & ImeHint.Completion) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.Completion;
        }

        if ((hint & ImeHint.Spellcheck) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.Spellcheck;
        }

        if ((hint & ImeHint.AutoCapitalization) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.AutoCapitalization;
        }

        if ((hint & ImeHint.Lowercase) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.Lowercase;
        }

        if ((hint & ImeHint.Uppercase) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.Uppercase;
        }

        if ((hint & ImeHint.Titlecase) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.Titlecase;
        }

        if ((hint & ImeHint.HiddenText) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.HiddenText;
        }

        if ((hint & ImeHint.SensitiveData) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.SensitiveData;
        }

        if ((hint & ImeHint.Latin) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.Latin;
        }

        if ((hint & ImeHint.Multiline) != 0)
        {
            contentHint |= ZwpTextInputV3ContentHint.Multiline;
        }

        return new ContentType(contentHint, contentPurpose);
    }
}
