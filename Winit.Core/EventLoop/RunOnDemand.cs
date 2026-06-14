namespace Winit.Core;

public interface IEventLoopExtRunOnDemand
{
    void RunAppOnDemand(IApplicationHandler app);
}
