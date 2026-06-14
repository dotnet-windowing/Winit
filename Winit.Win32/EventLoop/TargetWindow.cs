using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Winit.Win32;

internal sealed unsafe class EventTargetWindow
{
    private const nint HwndMessage = -3;
    private const string WindowClassName = "Winit.EventTarget";

    private static readonly ConcurrentDictionary<nint, EventLoop> s_eventLoops = new();
    private static int s_registeredClass;

    private EventTargetWindow(HWND hwnd)
    {
        Hwnd = hwnd;
    }

    public HWND Hwnd { get; }

    public static EventTargetWindow Create(EventLoop eventLoop)
    {
        RegisterWindowClass();

        fixed (char* className = WindowClassName)
        fixed (char* title = string.Empty)
        {
            HWND hwnd = PInvoke.CreateWindowEx(
                default,
                new PCWSTR(className),
                new PCWSTR(title),
                default,
                0,
                0,
                0,
                0,
                new HWND(HwndMessage),
                HMENU.Null,
                HINSTANCE.Null,
                null);

            if (hwnd == HWND.Null)
            {
                throw Win32Error.EventLoop();
            }

            s_eventLoops[Util.HwndValue(hwnd)] = eventLoop;
            return new EventTargetWindow(hwnd);
        }
    }

    private static void RegisterWindowClass()
    {
        if (Interlocked.Exchange(ref s_registeredClass, 1) != 0)
        {
            return;
        }

        fixed (char* className = WindowClassName)
        {
            WNDCLASSEXW windowClass = new()
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = &WndProc,
                hInstance = HINSTANCE.Null,
                lpszClassName = new PCWSTR(className),
            };

            if (PInvoke.RegisterClassEx(windowClass) == 0)
            {
                Interlocked.Exchange(ref s_registeredClass, 0);
                throw Win32Error.EventLoop();
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        try
        {
            return WndProcInner(hwnd, message, wParam, lParam);
        }
        catch (Exception exception)
        {
            if (s_eventLoops.TryGetValue(Util.HwndValue(hwnd), out EventLoop? eventLoop))
            {
                eventLoop.StoreCallbackException(exception);
            }

            return new LRESULT(-1);
        }
    }

    private static LRESULT WndProcInner(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (!s_eventLoops.TryGetValue(Util.HwndValue(hwnd), out EventLoop? eventLoop))
        {
            if (message == EventLoop.ExecMessage)
            {
                EventLoopThreadExecutor.ExecuteQueued(wParam.Value);
                return new LRESULT(0);
            }

            return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
        }

        switch (message)
        {
            case RawInput.WmInput:
                RawInput.Handle(eventLoop, lParam.Value);
                return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
            case EventLoop.ExecMessage:
                EventLoopThreadExecutor.ExecuteQueued(wParam.Value);
                return new LRESULT(0);
            case PInvoke.WM_NCDESTROY:
                s_eventLoops.TryRemove(Util.HwndValue(hwnd), out _);
                break;
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }

}
