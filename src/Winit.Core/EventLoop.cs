using System.Threading;

namespace Winit.Core;

public readonly record struct Instant(long Timestamp)
{
    public static Instant Now()
    {
        return new Instant(TimeProvider.System.GetTimestamp());
    }

    public Instant? CheckedAdd(TimeSpan duration)
    {
        try
        {
            double timestampDelta = duration.TotalSeconds * TimeProvider.System.TimestampFrequency;

            if (timestampDelta > long.MaxValue || timestampDelta < long.MinValue)
            {
                return null;
            }

            return new Instant(checked(Timestamp + (long)timestampDelta));
        }
        catch (OverflowException)
        {
            return null;
        }
    }
}

public interface IActiveEventLoop : IAsAny
{
    EventLoopProxy CreateProxy();

    IWindow CreateWindow(WindowAttributes windowAttributes);

    CustomCursor CreateCustomCursor(CustomCursorSource customCursor);

    IEnumerable<MonitorHandle> AvailableMonitors { get; }

    MonitorHandle? PrimaryMonitor { get; }

    Theme? SystemTheme { get; }

    ControlFlow ControlFlow { get; set; }

    bool Exiting { get; }

    OwnedDisplayHandle OwnedDisplayHandle { get; }

    object? DisplayHandle { get; }

    void ListenDeviceEvents(DeviceEvents allowed);

    void Exit();
}

public interface IPlatformEventLoop : IActiveEventLoop
{
    void RunApp(IApplicationHandler app);
}

public interface IPlatformEventLoopBuilder : IAsAny
{
    IPlatformEventLoop Build();
}

public sealed class EventLoopProxy(IEventLoopProxyProvider provider)
{
    public IEventLoopProxyProvider Provider { get; } = provider;

    public void WakeUp()
    {
        Provider.WakeUp();
    }
}

public interface IEventLoopProxyProvider : IAsAny
{
    void WakeUp();
}

public sealed class OwnedDisplayHandle(object? handle) : IEquatable<OwnedDisplayHandle>
{
    public object? Handle { get; } = handle;

    public bool Equals(OwnedDisplayHandle? other)
    {
        return other is not null && EqualityComparer<object?>.Default.Equals(Handle, other.Handle);
    }

    public override bool Equals(object? obj)
    {
        return obj is OwnedDisplayHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Handle?.GetHashCode() ?? 0;
    }
}

public readonly record struct AsyncRequestSerial(nuint Serial)
{
    private static long s_currentSerial;

    public static AsyncRequestSerial Get()
    {
        long serial = Interlocked.Increment(ref s_currentSerial) - 1;
        return new AsyncRequestSerial((nuint)serial);
    }
}

public record struct ControlFlow
{
    public readonly record struct Poll;

    public readonly record struct Wait;

    public readonly record struct WaitUntil(Instant Instant);

    private const byte WaitTag = 0;
    private const byte PollTag = 1;
    private const byte WaitUntilTag = 2;

    private byte _tag;
    private Poll _poll;
    private Wait _wait;
    private WaitUntil _waitUntil;

    public ControlFlow(Poll value)
    {
        _tag = PollTag;
        _poll = value;
        _wait = default;
        _waitUntil = default;
    }

    public ControlFlow(Wait value)
    {
        _tag = WaitTag;
        _poll = default;
        _wait = value;
        _waitUntil = default;
    }

    public ControlFlow(WaitUntil value)
    {
        _tag = WaitUntilTag;
        _poll = default;
        _wait = default;
        _waitUntil = value;
    }

    public static ControlFlow Default => new(new Wait());

    public static ControlFlow WaitDuration(TimeSpan timeout)
    {
        Instant? instant = Instant.Now().CheckedAdd(timeout);
        return instant is { } value ? new ControlFlow(new WaitUntil(value)) : new ControlFlow(new Wait());
    }

    public bool TryGetValue(out Poll value)
    {
        value = _poll;
        return _tag == PollTag;
    }

    public bool TryGetValue(out Wait value)
    {
        value = _wait;
        return _tag == WaitTag;
    }

    public bool TryGetValue(out WaitUntil value)
    {
        value = _waitUntil;
        return _tag == WaitUntilTag;
    }
}

public enum DeviceEvents
{
    WhenFocused = 0,
    Always = 1,
    Never = 2,
}
