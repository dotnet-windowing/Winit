using Winit.Core;

namespace Winit.Wayland;

internal record struct Event
{
    public readonly record struct Window(WindowId WindowId, WindowEvent WindowEvent);

    public readonly record struct Device(DeviceEvent DeviceEvent);

    private const byte WindowTag = 0;
    private const byte DeviceTag = 1;

    private byte _tag;
    private Window _window;
    private Device _device;

    public Event(Window value)
    {
        this = default;
        _tag = WindowTag;
        _window = value;
    }

    public Event(Device value)
    {
        this = default;
        _tag = DeviceTag;
        _device = value;
    }

    public bool TryGetValue(out Window value)
    {
        value = _window;
        return _tag == WindowTag;
    }

    public bool TryGetValue(out Device value)
    {
        value = _device;
        return _tag == DeviceTag;
    }
}

internal sealed class EventSink
{
    private readonly Queue<Event> _events = [];

    public bool IsEmpty => _events.Count == 0;

    public void PushWindowEvent(WindowId windowId, WindowEvent windowEvent)
    {
        _events.Enqueue(new Event(new Event.Window(windowId, windowEvent)));
    }

    public void PushDeviceEvent(DeviceEvent deviceEvent)
    {
        _events.Enqueue(new Event(new Event.Device(deviceEvent)));
    }

    public void Append(EventSink other)
    {
        while (other._events.Count > 0)
        {
            _events.Enqueue(other._events.Dequeue());
        }
    }

    public IEnumerable<Event> Drain()
    {
        while (_events.Count > 0)
        {
            yield return _events.Dequeue();
        }
    }
}
