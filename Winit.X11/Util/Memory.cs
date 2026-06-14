namespace Winit.X11.Util;

internal static class Memory
{
}

internal unsafe sealed class XSmartPointer<T> : IDisposable
    where T : unmanaged
{
    private readonly XConnection _xconn;

    private XSmartPointer(XConnection xconn, T* ptr)
    {
        _xconn = xconn;
        Ptr = ptr;
    }

    public T* Ptr { get; private set; }

    public static XSmartPointer<T>? New(XConnection xconn, T* ptr)
    {
        return ptr is null ? null : new XSmartPointer<T>(xconn, ptr);
    }

    public void Dispose()
    {
        T* ptr = Ptr;
        if (ptr is null)
        {
            return;
        }

        Ptr = null;
        _ = PInvoke.XFree((nint)ptr);
    }
}
