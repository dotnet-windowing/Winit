namespace Winit.Core;

public interface IApplicationHandler
{
    void NewEvents(IActiveEventLoop eventLoop, StartCause cause)
    {
    }

    void Resumed(IActiveEventLoop eventLoop)
    {
    }

    void CanCreateSurfaces(IActiveEventLoop eventLoop);

    void ProxyWakeUp(IActiveEventLoop eventLoop)
    {
    }

    void WindowEvent(IActiveEventLoop eventLoop, WindowId windowId, WindowEvent windowEvent);

    void DeviceEvent(IActiveEventLoop eventLoop, DeviceId? deviceId, DeviceEvent deviceEvent)
    {
    }

    void AboutToWait(IActiveEventLoop eventLoop)
    {
    }

    void Suspended(IActiveEventLoop eventLoop)
    {
    }

    void DestroySurfaces(IActiveEventLoop eventLoop)
    {
    }

    void MemoryWarning(IActiveEventLoop eventLoop)
    {
    }

    IApplicationHandlerExtMacOS? MacOSHandler => null;
}
