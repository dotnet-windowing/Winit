using Winit.Core;

namespace Winit;

public sealed class EventLoopBuilder
{
    internal EventLoopBuilder(IPlatformEventLoopBuilder backend)
    {
        Backend = backend;
    }

    internal IPlatformEventLoopBuilder Backend { get; }

    public EventLoop Build()
    {
        return new EventLoop(Backend.Build());
    }
}
