using Winit.Core;

namespace Winit.X11;

public sealed class EventLoopBuilder : IPlatformEventLoopBuilder, IEventLoopBuilderExtX11
{
    private readonly PlatformSpecificEventLoopAttributes _attributes = new();

    public EventLoopBuilder WithX11()
    {
        return this;
    }

    public EventLoopBuilder WithAnyThread(bool anyThread)
    {
        _attributes.AnyThread = anyThread;
        return this;
    }

    public IPlatformEventLoop Build()
    {
        return new EventLoop(_attributes);
    }
}
