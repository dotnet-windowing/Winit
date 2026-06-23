using System.Diagnostics.CodeAnalysis;
using RawWindowHandles;
using Winit.Core;

namespace Winit;

public sealed class EventLoop
{
    private readonly IPlatformEventLoop _backend;

    internal EventLoop(IPlatformEventLoop backend)
    {
        _backend = backend;
    }

    public static EventLoopBuilder Builder()
    {
        return new EventLoopBuilder(CreatePlatformBuilder());
    }

    public static EventLoop New()
    {
        return Builder().Build();
    }

    public EventLoopProxy CreateProxy()
    {
        return _backend.CreateProxy();
    }

    public OwnedDisplayHandle OwnedDisplayHandle()
    {
        return _backend.OwnedDisplayHandle;
    }

    public void ListenDeviceEvents(DeviceEvents allowed)
    {
        _backend.ListenDeviceEvents(allowed);
    }

    public ControlFlow ControlFlow
    {
        get => _backend.ControlFlow;
        set => _backend.ControlFlow = value;
    }

    public CustomCursor CreateCustomCursor(CustomCursorSource customCursor)
    {
        return _backend.CreateCustomCursor(customCursor);
    }

    public void RunApp(IApplicationHandler app)
    {
        _backend.RunApp(app);
    }

    public void RunAppOnDemand(IApplicationHandler app)
    {
        if (_backend is IEventLoopExtRunOnDemand runOnDemand)
        {
            runOnDemand.RunAppOnDemand(app);
            return;
        }

        throw new EventLoopNotSupportedException(
            new NotSupportedRequestException("run app on demand is not supported by this backend"));
    }

    public PumpStatus PumpAppEvents(TimeSpan? timeout, IApplicationHandler app)
    {
        if (_backend is IEventLoopExtPumpEvents pumpEvents)
        {
            return pumpEvents.PumpAppEvents(timeout, app);
        }

        throw new EventLoopNotSupportedException(
            new NotSupportedRequestException("pump events are not supported by this backend"));
    }

    public void RegisterApp(IApplicationHandler app)
    {
        if (_backend is IEventLoopExtRegister register)
        {
            register.RegisterApp(app);
            return;
        }

        throw new EventLoopNotSupportedException(
            new NotSupportedRequestException("register app is not supported by this backend"));
    }

    [DoesNotReturn]
    public void RunAppNeverReturn(IApplicationHandler app)
    {
        if (_backend is IEventLoopExtNeverReturn neverReturn)
        {
            neverReturn.RunAppNeverReturn(app);
        }

        throw new EventLoopNotSupportedException(
            new NotSupportedRequestException("run app never return is not supported by this backend"));
    }

    internal IPlatformEventLoop Backend => _backend;

    private static IPlatformEventLoopBuilder CreatePlatformBuilder()
    {
#if ANDROID
        return new Winit.Android.EventLoopBuilder();
#else
        if (OperatingSystem.IsWindows())
        {
            return new Winit.Win32.EventLoopBuilder();
        }

        if (OperatingSystem.IsLinux())
        {
            if (HasNonEmptyEnvironment("WAYLAND_DISPLAY") || HasNonEmptyEnvironment("WAYLAND_SOCKET"))
            {
                return new Winit.Wayland.EventLoopBuilder();
            }

            if (HasNonEmptyEnvironment("DISPLAY"))
            {
                return new Winit.X11.EventLoopBuilder();
            }

            return new UnsupportedEventLoopBuilderProvider();
        }

        return new UnsupportedEventLoopBuilderProvider();
#endif
    }

    private static bool HasNonEmptyEnvironment(string variable)
    {
        return Environment.GetEnvironmentVariable(variable) is { Length: > 0 };
    }
}
