using System.Runtime.InteropServices;

namespace Winit.X11.Ime;

internal sealed class ImeInner : IDisposable
{
    private readonly GCHandle _selfHandle;
    private readonly Lock _eventLock = new();
    private readonly Queue<(XlibWindow Window, ImeEvent Event)> _events = [];
    private bool _disposed;

    public ImeInner(XConnection xconn, PotentialInputMethods potentialInputMethods)
    {
        XConnection = xconn;
        PotentialInputMethods = potentialInputMethods;
        _selfHandle = GCHandle.Alloc(this);
    }

    public XConnection XConnection { get; }

    public InputMethod? Im { get; set; }

    public PotentialInputMethods PotentialInputMethods { get; }

    public Dictionary<XlibWindow, ImeContext?> Contexts { get; set; } = [];

    public XIMCallback DestroyCallback;

    public bool IsDestroyed { get; set; }

    public bool IsFallback { get; set; }

    public nint ClientData => GCHandle.ToIntPtr(_selfHandle);

    public void InitializeDestroyCallback()
    {
        DestroyCallback = new XIMCallback
        {
            ClientData = ClientData,
            Callback = &Callbacks.XimDestroyCallback,
        };
    }

    public bool CloseImIfNecessary()
    {
        if (IsDestroyed || Im is null)
        {
            return false;
        }

        return Ime.CloseIm(XConnection, Im.Im);
    }

    public bool DestroyIcIfNecessary(nint ic)
    {
        return !IsDestroyed && Ime.DestroyIc(XConnection, ic);
    }

    public bool DestroyAllContextsIfNecessary()
    {
        foreach (ImeContext context in Contexts.Values.OfType<ImeContext>())
        {
            _ = DestroyIcIfNecessary(context.Ic);
            context.Dispose();
        }

        return !IsDestroyed;
    }

    public void EnqueueEvent(XlibWindow window, ImeEvent @event)
    {
        lock (_eventLock)
        {
            _events.Enqueue((window, @event));
        }
    }

    public List<(XlibWindow Window, ImeEvent Event)> DrainEvents()
    {
        List<(XlibWindow Window, ImeEvent Event)> events = [];
        lock (_eventLock)
        {
            while (_events.TryDequeue(out (XlibWindow Window, ImeEvent Event) item))
            {
                events.Add(item);
            }
        }

        return events;
    }

    public static ImeInner? FromClientData(nint clientData)
    {
        if (clientData == 0)
        {
            return null;
        }

        GCHandle handle = GCHandle.FromIntPtr(clientData);
        return handle.Target as ImeInner;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }

        _disposed = true;
    }
}
