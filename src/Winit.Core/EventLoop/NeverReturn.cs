using System.Diagnostics.CodeAnalysis;

namespace Winit.Core;

public interface IEventLoopExtNeverReturn
{
    [DoesNotReturn]
    void RunAppNeverReturn(IApplicationHandler app);
}
