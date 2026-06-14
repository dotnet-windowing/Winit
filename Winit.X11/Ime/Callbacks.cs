using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.X11.Ime;

internal static unsafe class Callbacks
{
    public static void SetInstantiateCallback(XConnection xconn, nint clientData)
    {
        _ = PInvoke.XRegisterIMInstantiateCallback(
            xconn.Display,
            0,
            0,
            0,
            &XimInstantiateCallback,
            clientData);
        xconn.CheckErrors();
    }

    public static void UnsetInstantiateCallback(XConnection xconn, nint clientData)
    {
        _ = PInvoke.XUnregisterIMInstantiateCallback(
            xconn.Display,
            0,
            0,
            0,
            &XimInstantiateCallback,
            clientData);
        xconn.CheckErrors();
    }

    public static void SetDestroyCallback(XConnection xconn, nint im, ImeInner inner)
    {
        fixed (byte* destroyCallbackName = ImeNative.XNDestroyCallback)
        fixed (XIMCallback* callback = &inner.DestroyCallback)
        {
            _ = PInvoke.XSetIMValuesDestroyCallback(
                im,
                (sbyte*)destroyCallbackName,
                callback,
                0);
        }

        xconn.CheckErrors();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void XimInstantiateCallback(nint display, nint clientData, nint callData)
    {
        ImeInner? inner = ImeInner.FromClientData(clientData);
        if (inner is null)
        {
            return;
        }

        try
        {
            ReplaceIm(inner);
            UnsetInstantiateCallback(inner.XConnection, clientData);
            inner.IsFallback = false;
        }
        catch
        {
            if (inner.IsDestroyed)
            {
                Environment.FailFast("Failed to reopen X input method.");
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void XimDestroyCallback(nint xim, nint clientData, nint callData)
    {
        ImeInner? inner = ImeInner.FromClientData(clientData);
        if (inner is null)
        {
            return;
        }

        inner.IsDestroyed = true;
        if (inner.IsFallback)
        {
            return;
        }

        try
        {
            SetInstantiateCallback(inner.XConnection, clientData);
            ReplaceIm(inner);
            inner.IsFallback = true;
        }
        catch (Exception error)
        {
            Environment.FailFast("Failed to open fallback X input method.", error);
        }
    }

    private static void ReplaceIm(ImeInner inner)
    {
        XConnection xconn = inner.XConnection;
        InputMethodResult result = inner.PotentialInputMethods.OpenIm(xconn, null);
        bool isFallback = result.IsFallback;
        InputMethod newIm = result.Ok() ?? throw new ImeCreationException("Failed to reopen X input method.");

        try
        {
            SetDestroyCallback(xconn, newIm.Im, inner);
        }
        catch
        {
            _ = Ime.CloseIm(xconn, newIm.Im);
            throw;
        }

        Dictionary<XlibWindow, ImeContext?> newContexts = [];
        try
        {
            foreach ((XlibWindow window, ImeContext? oldContext) in inner.Contexts)
            {
                XRectangle? area = oldContext?.IcArea;
                bool allowed = oldContext?.IsAllowed ?? false;
                ImeContext newContext = ImeContext.New(xconn, newIm, window, area, inner, allowed);
                newContexts[window] = newContext;
            }
        }
        catch
        {
            _ = Ime.CloseIm(xconn, newIm.Im);
            foreach (ImeContext context in newContexts.Values.OfType<ImeContext>())
            {
                _ = Ime.DestroyIc(xconn, context.Ic);
                context.Dispose();
            }

            throw;
        }

        _ = inner.DestroyAllContextsIfNecessary();
        _ = inner.CloseImIfNecessary();
        inner.Im = newIm;
        inner.Contexts = newContexts;
        inner.IsDestroyed = false;
        inner.IsFallback = isFallback;
    }
}
