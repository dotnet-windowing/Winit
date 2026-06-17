using Winit.Core;

namespace Winit.Wayland;

internal sealed unsafe class EventLoopProxyProvider(int eventFd) : IEventLoopProxyProvider
{
    public void WakeUp()
    {
        ulong value = 1;
        _ = PInvoke.Write(eventFd, &value, sizeof(ulong));
    }
}
