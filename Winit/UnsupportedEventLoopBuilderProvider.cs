using Winit.Core;

namespace Winit;

internal sealed class UnsupportedEventLoopBuilderProvider : IPlatformEventLoopBuilder
{
    public IPlatformEventLoop Build()
    {
        throw new PlatformNotSupportedException("This platform is not supported by Winit.");
    }
}
