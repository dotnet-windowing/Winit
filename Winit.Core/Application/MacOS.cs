namespace Winit.Core;

public interface IApplicationHandlerExtMacOS : IApplicationHandler
{
    void StandardKeyBinding(IActiveEventLoop eventLoop, WindowId windowId, string action)
    {
    }
}
