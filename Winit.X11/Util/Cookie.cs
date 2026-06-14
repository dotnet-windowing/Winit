namespace Winit.X11.Util;

internal static class Cookie
{
}

internal unsafe sealed class GenericEventCookie : IDisposable
{
    private readonly XConnection _xconn;
    private bool _disposed;

    private GenericEventCookie(XConnection xconn, XGenericEventCookie cookie)
    {
        _xconn = xconn;
        Cookie = cookie;
    }

    public XGenericEventCookie Cookie;

    public int Extension => Cookie.Extension;

    public int EventType => Cookie.EvType;

    public static GenericEventCookie? FromEvent(XConnection xconn, in XEvent xevent)
    {
        XGenericEventCookie cookie = xevent.GenericEventCookie;
        return PInvoke.XGetEventData(xconn.Display, &cookie) != 0
            ? new GenericEventCookie(xconn, cookie)
            : null;
    }

    public T* AsEvent<T>()
        where T : unmanaged
    {
        return (T*)Cookie.Data;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        fixed (XGenericEventCookie* cookie = &Cookie)
        {
            PInvoke.XFreeEventData(_xconn.Display, cookie);
        }

        _disposed = true;
    }
}
