using System.Runtime.InteropServices;
using CoreIme = Winit.Core.Ime;

namespace Winit.X11.Ime;

internal static class ImeNative
{
    public static readonly object GlobalLock = new();

    public const nuint XimPreeditStyle = PInvoke.XIMPreeditCallbacks | PInvoke.XIMStatusNothing;
    public const nuint XimNothingStyle = PInvoke.XIMPreeditNothing | PInvoke.XIMStatusNothing;
    public const nuint XimNoneStyle = PInvoke.XIMPreeditNone | PInvoke.XIMStatusNone;

    public static ReadOnlySpan<byte> XNQueryInputStyle => "queryInputStyle\0"u8;
    public static ReadOnlySpan<byte> XNInputStyle => "inputStyle\0"u8;
    public static ReadOnlySpan<byte> XNClientWindow => "clientWindow\0"u8;
    public static ReadOnlySpan<byte> XNPreeditAttributes => "preeditAttributes\0"u8;
    public static ReadOnlySpan<byte> XNPreeditStartCallback => "preeditStartCallback\0"u8;
    public static ReadOnlySpan<byte> XNPreeditDoneCallback => "preeditDoneCallback\0"u8;
    public static ReadOnlySpan<byte> XNPreeditDrawCallback => "preeditDrawCallback\0"u8;
    public static ReadOnlySpan<byte> XNPreeditCaretCallback => "preeditCaretCallback\0"u8;
    public static ReadOnlySpan<byte> XNSpotLocation => "spotLocation\0"u8;
    public static ReadOnlySpan<byte> XNArea => "area\0"u8;
    public static ReadOnlySpan<byte> XNDestroyCallback => "destroyCallback\0"u8;
}

internal readonly record struct ImeEvent
{
    public readonly record struct Enabled;
    public readonly record struct Start;
    public readonly record struct Update(string Text, nuint Position);
    public readonly record struct End;
    public readonly record struct Disabled;

    private const byte EnabledTag = 0;
    private const byte StartTag = 1;
    private const byte UpdateTag = 2;
    private const byte EndTag = 3;
    private const byte DisabledTag = 4;

    private readonly byte _tag;
    private readonly Enabled _enabled;
    private readonly Start _start;
    private readonly Update _update;
    private readonly End _end;
    private readonly Disabled _disabled;

    public ImeEvent(Enabled value) { _tag = EnabledTag; _enabled = value; _start = default; _update = default; _end = default; _disabled = default; }
    public ImeEvent(Start value) { _tag = StartTag; _enabled = default; _start = value; _update = default; _end = default; _disabled = default; }
    public ImeEvent(Update value) { _tag = UpdateTag; _enabled = default; _start = default; _update = value; _end = default; _disabled = default; }
    public ImeEvent(End value) { _tag = EndTag; _enabled = default; _start = default; _update = default; _end = value; _disabled = default; }
    public ImeEvent(Disabled value) { _tag = DisabledTag; _enabled = default; _start = default; _update = default; _end = default; _disabled = value; }

    public bool TryGetValue(out Enabled value) { value = _enabled; return _tag == EnabledTag; }
    public bool TryGetValue(out Start value) { value = _start; return _tag == StartTag; }
    public bool TryGetValue(out Update value) { value = _update; return _tag == UpdateTag; }
    public bool TryGetValue(out End value) { value = _end; return _tag == EndTag; }
    public bool TryGetValue(out Disabled value) { value = _disabled; return _tag == DisabledTag; }
}

internal sealed class ImeCreationException(string message) : Exception(message);

internal sealed class Ime : IDisposable
{
    private readonly XConnection _xconn;
    private readonly ImeInner _inner;
    private bool _disposed;

    private Ime(XConnection xconn, ImeInner inner)
    {
        _xconn = xconn;
        _inner = inner;
    }

    public static Ime New(XConnection xconn)
    {
        PotentialInputMethods potentialInputMethods = PotentialInputMethods.New(xconn);
        ImeInner inner = new(xconn, potentialInputMethods);
        inner.InitializeDestroyCallback();

        InputMethodResult inputMethod = inner.PotentialInputMethods.OpenIm(
            xconn,
            () => Callbacks.SetInstantiateCallback(xconn, inner.ClientData));

        bool isFallback = inputMethod.IsFallback;
        if (inputMethod.Ok() is not { } im)
        {
            inner.Dispose();
            throw new ImeCreationException("Failed to open X input method.");
        }

        try
        {
            Callbacks.SetDestroyCallback(xconn, im.Im, inner);
            inner.Im = im;
            inner.IsFallback = isFallback;
            return new Ime(xconn, inner);
        }
        catch
        {
            _ = CloseIm(xconn, im.Im);
            inner.Dispose();
            throw;
        }
    }

    public bool IsDestroyed => _inner.IsDestroyed;

    public bool CreateContext(XlibWindow window, bool withIme)
    {
        ImeContext? context = null;
        if (!IsDestroyed)
        {
            InputMethod im = _inner.Im ?? throw new InvalidOperationException("Input method has not been opened.");
            context = ImeContext.New(_inner.XConnection, im, window, null, _inner, withIme);
            _inner.EnqueueEvent(
                window,
                context.IsAllowed
                    ? new ImeEvent(new ImeEvent.Enabled())
                    : new ImeEvent(new ImeEvent.Disabled()));
        }

        _inner.Contexts[window] = context;
        return !IsDestroyed;
    }

    public nint? GetContext(XlibWindow window)
    {
        if (IsDestroyed)
        {
            return null;
        }

        return _inner.Contexts.TryGetValue(window, out ImeContext? context) && context is not null
            ? context.Ic
            : null;
    }

    public bool RemoveContext(XlibWindow window)
    {
        if (!_inner.Contexts.Remove(window, out ImeContext? context) || context is null)
        {
            return false;
        }

        _ = _inner.DestroyIcIfNecessary(context.Ic);
        context.Dispose();
        return true;
    }

    public bool Focus(XlibWindow window)
    {
        if (IsDestroyed ||
            !_inner.Contexts.TryGetValue(window, out ImeContext? context) ||
            context is null)
        {
            return false;
        }

        context.Focus(_xconn);
        return true;
    }

    public bool Unfocus(XlibWindow window)
    {
        if (IsDestroyed ||
            !_inner.Contexts.TryGetValue(window, out ImeContext? context) ||
            context is null)
        {
            return false;
        }

        context.Unfocus(_xconn);
        return true;
    }

    public void SendXimArea(XlibWindow window, short x, short y, ushort width, ushort height)
    {
        if (IsDestroyed ||
            !_inner.Contexts.TryGetValue(window, out ImeContext? context) ||
            context is null)
        {
            return;
        }

        context.SetArea(_xconn, x, y, width, height);
    }

    public void SetImeAllowed(XlibWindow window, bool allowed)
    {
        if (IsDestroyed)
        {
            return;
        }

        if (_inner.Contexts.TryGetValue(window, out ImeContext? context) &&
            context is not null &&
            context.IsAllowed == allowed)
        {
            return;
        }

        _ = RemoveContext(window);
        _ = CreateContext(window, allowed);
    }

    public bool IsImeAllowed(XlibWindow window)
    {
        return !IsDestroyed &&
            _inner.Contexts.TryGetValue(window, out ImeContext? context) &&
            context is not null &&
            context.IsAllowed;
    }

    public List<(XlibWindow Window, ImeEvent Event)> DrainEvents()
    {
        return _inner.DrainEvents();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ = _inner.DestroyAllContextsIfNecessary();
        _ = _inner.CloseImIfNecessary();
        _inner.Dispose();
        _disposed = true;
    }

    internal static bool CloseIm(XConnection xconn, nint im)
    {
        _ = PInvoke.XCloseIM(im);
        xconn.CheckErrors();
        return true;
    }

    internal static bool DestroyIc(XConnection xconn, nint ic)
    {
        _ = PInvoke.XDestroyIC(ic);
        xconn.CheckErrors();
        return true;
    }
}
