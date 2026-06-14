using System.Runtime.InteropServices;

namespace Winit.Win32;

internal sealed class EventLoopThreadExecutor(uint threadId, HWND targetWindow)
{
    public bool InEventLoopThread => PInvoke.GetCurrentThreadId() == threadId;

    public void Execute(Action action)
    {
        if (InEventLoopThread)
        {
            action();
            return;
        }

        GCHandle handle = GCHandle.Alloc(action);
        if (PInvoke.PostMessage(
                targetWindow,
                EventLoop.ExecMessage,
                new WPARAM(unchecked((nuint)GCHandle.ToIntPtr(handle))),
                default))
        {
            return;
        }

        handle.Free();
        throw Win32Error.EventLoop();
    }

    public static void ExecuteQueued(nuint actionHandle)
    {
        GCHandle handle = GCHandle.FromIntPtr(unchecked((nint)actionHandle));
        try
        {
            if (handle.Target is Action action)
            {
                action();
            }
        }
        finally
        {
            handle.Free();
        }
    }
}
