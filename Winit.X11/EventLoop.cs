using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RawWindowHandles;
using Winit.Common.Xkb;
using Winit.Core;
using Winit.X11.Util;
using CoreMonitorHandle = Winit.Core.MonitorHandle;

namespace Winit.X11;

public sealed unsafe class EventLoop : IPlatformEventLoop, IActiveEventLoopExtX11, IEventLoopExtX11
{
    private static readonly int s_mainThreadId = Environment.CurrentManagedThreadId;
    private static readonly object s_backendLock = new();
    private static readonly List<XlibErrorHook> s_xlibErrorHooks = [];
    private static XConnection? s_backend;
    private static int s_created;
    private readonly XConnection _xconn;
    private readonly Dictionary<nuint, Window> _windows = [];
    private readonly Dictionary<DeviceId, Device> _devices = [];
    private readonly EventProcessor _eventProcessor;
    private readonly Context? _xkbContext;
    private readonly Ime.Ime? _ime;
    private readonly XlibWindow _proxyWindow;
    private readonly Lock _activationLock = new();
    private readonly Queue<(Window Window, AsyncRequestSerial Serial)> _activationRequests = [];
    private ControlFlow _controlFlow = ControlFlow.Default;
    private DeviceEvents _deviceEvents = DeviceEvents.WhenFocused;
    private bool _exiting;

    public EventLoop(PlatformSpecificEventLoopAttributes attributes)
    {
        if (Interlocked.Exchange(ref s_created, 1) != 0)
        {
            throw new EventLoopRecreationException();
        }

        if (!attributes.AnyThread && Environment.CurrentManagedThreadId != s_mainThreadId)
        {
            throw new InvalidOperationException(
                "Initializing the event loop outside of the main thread is a cross-platform compatibility hazard.");
        }

        _xconn = GetBackend();
        _xconn.UpdateCachedWmInfo(_xconn.RootWindow);
        _xkbContext = CreateXkbContext(_xconn);
        InitializeLocaleForIme();
        _ime = CreateIme(_xconn);
        _eventProcessor = new EventProcessor(this);
        _proxyWindow = CreateProxyWindow(_xconn);
        _ = _xconn.SelectXrandrInput(_xconn.RootWindow);
        SelectDeviceHierarchyEvents();
        SelectXkbEvents();
        InitDevice(PInvoke.XiAllDevices);
        UpdateDeviceEventFilter(focused: false);
    }

    ~EventLoop()
    {
        if (_proxyWindow.Value != 0)
        {
            _ = PInvoke.XDestroyWindow(_xconn.Display, _proxyWindow);
        }
    }

    public IEnumerable<CoreMonitorHandle> AvailableMonitors => Monitor.AvailableMonitors(_xconn);

    public CoreMonitorHandle? PrimaryMonitor => Monitor.PrimaryMonitor(_xconn);

    public Theme? SystemTheme => null;

    public ControlFlow ControlFlow
    {
        get => _controlFlow;
        set => _controlFlow = value;
    }

    public bool Exiting => _exiting;

    public bool IsX11 => true;

    public OwnedDisplayHandle OwnedDisplayHandle => new(RawDisplayHandle.FromXlib(_xconn.Display, _xconn.DefaultScreen));

    public RawDisplayHandle? DisplayHandle => RawDisplayHandle.FromXlib(_xconn.Display, _xconn.DefaultScreen);

    internal XConnection XConnection => _xconn;

    internal XlibWindow RootWindow => _xconn.RootWindow;

    internal Atoms Atoms => _xconn.Atoms;

    internal Context? XkbContext => _xkbContext;

    internal DeviceEvents DeviceEvents => _deviceEvents;

    internal IReadOnlyCollection<Window> Windows => _windows.Values;

    internal Ime.Ime? Ime => _ime;

    internal bool TryGetDevice(DeviceId deviceId, [NotNullWhen(true)] out Device? device)
    {
        return _devices.TryGetValue(deviceId, out device);
    }

    public EventLoopProxy CreateProxy()
    {
        return new EventLoopProxy(new EventLoopProxyProvider(_xconn, _proxyWindow, _xconn.Atoms[AtomName.WinitWakeUp]));
    }

    public IWindow CreateWindow(WindowAttributes windowAttributes)
    {
        return Window.Create(this, windowAttributes);
    }

    public Winit.Core.CustomCursor CreateCustomCursor(CustomCursorSource customCursor)
    {
        return new Winit.Core.CustomCursor(X11CustomCursor.Create(_xconn, customCursor));
    }

    public void ListenDeviceEvents(DeviceEvents allowed)
    {
        _deviceEvents = allowed;
        UpdateDeviceEventFilter(HasFocusedWindow);
    }

    public void Exit()
    {
        _exiting = true;
    }

    public void RunApp(IApplicationHandler app)
    {
        _exiting = false;

        app.NewEvents(this, new StartCause(new StartCause.Init()));
        app.CanCreateSurfaces(this);
        ProcessActivationRequests(app);
        _xconn.Flush();

        while (!_exiting)
        {
            if (ControlFlow.TryGetValue(out ControlFlow.Poll _) && PInvoke.XPending(_xconn.Display) == 0)
            {
                app.NewEvents(this, new StartCause(new StartCause.Poll()));
                app.AboutToWait(this);
                ProcessActivationRequests(app);
                Thread.Yield();
                continue;
            }

            app.AboutToWait(this);
            ProcessActivationRequests(app);
            if (_exiting)
            {
                break;
            }

            Instant waitStart = Instant.Now();
            XEvent xevent;
            _ = PInvoke.XNextEvent(_xconn.Display, &xevent);

            StartCause startCause = StartCauseForWake(waitStart);
            app.NewEvents(this, startCause);
            bool proxyWakeUp = DispatchOrCheckProxyWakeUp(app, in xevent);

            while (!_exiting && PInvoke.XPending(_xconn.Display) > 0)
            {
                _ = PInvoke.XNextEvent(_xconn.Display, &xevent);
                proxyWakeUp |= DispatchOrCheckProxyWakeUp(app, in xevent);
            }

            ProcessActivationRequests(app);

            if (proxyWakeUp)
            {
                app.ProxyWakeUp(this);
            }

            _xconn.CheckErrors();
        }
    }

    internal void RegisterWindow(Window window)
    {
        _windows[window.XWindow.Value] = window;
    }

    internal void CreateImeContext(Window window, bool withIme)
    {
        try
        {
            _ = _ime?.CreateContext(window.XWindow, withIme);
        }
        catch
        {
        }
    }

    internal void SetImeAllowed(Window window, bool allowed)
    {
        _ime?.SetImeAllowed(window.XWindow, allowed);
    }

    internal void SendImeArea(Window window, short x, short y, ushort width, ushort height)
    {
        _ime?.SendXimArea(window.XWindow, x, y, width, height);
    }

    internal void RemoveImeContext(XlibWindow window)
    {
        _ = _ime?.RemoveContext(window);
    }

    internal List<(XlibWindow Window, Ime.ImeEvent Event)> DrainImeEvents()
    {
        return _ime?.DrainEvents() ?? [];
    }

    internal bool TryGetWindow(XlibWindow xWindow, [NotNullWhen(true)] out Window? window)
    {
        return _windows.TryGetValue(xWindow.Value, out window);
    }

    internal bool RemoveWindow(XlibWindow xWindow, [NotNullWhen(true)] out Window? window)
    {
        return _windows.Remove(xWindow.Value, out window);
    }

    internal void InitDevice(int deviceId)
    {
        using DeviceInfo? allInfo = DeviceInfo.Get(_xconn, deviceId);
        if (allInfo is null)
        {
            return;
        }

        foreach (XIDeviceInfo info in allInfo.Infos)
        {
            DeviceId id = DeviceId.FromRaw(info.DeviceId);
            _devices[id] = Device.New(info, _xconn.Atoms);
        }
    }

    internal void RemoveDevice(int deviceId)
    {
        _devices.Remove(DeviceId.FromRaw(deviceId));
    }

    internal void ResetScrollPositionsForSource(int sourceId)
    {
        using DeviceInfo? allInfo = DeviceInfo.Get(_xconn, PInvoke.XiAllDevices);
        if (allInfo is null)
        {
            return;
        }

        foreach (XIDeviceInfo info in allInfo.Infos)
        {
            if (info.DeviceId != sourceId && info.Attachment != sourceId)
            {
                continue;
            }

            DeviceId id = DeviceId.FromRaw(info.DeviceId);
            if (_devices.TryGetValue(id, out Device? device))
            {
                device.ResetScrollPosition(info);
            }
        }
    }

    internal bool HasFocusedWindow => _windows.Values.Any(static window => window.HasFocus);

    internal void QueueActivationRequest(Window window, AsyncRequestSerial serial)
    {
        lock (_activationLock)
        {
            _activationRequests.Enqueue((window, serial));
        }

        new EventLoopProxyProvider(_xconn, _proxyWindow, _xconn.Atoms[AtomName.WinitWakeUp]).WakeUp();
    }

    private void ProcessActivationRequests(IApplicationHandler app)
    {
        while (true)
        {
            (Window Window, AsyncRequestSerial Serial) item;
            lock (_activationLock)
            {
                if (_activationRequests.Count == 0)
                {
                    return;
                }

                item = _activationRequests.Dequeue();
            }

            if (!TryGetWindow(item.Window.XWindow, out Window? currentWindow) ||
                !ReferenceEquals(currentWindow, item.Window))
            {
                continue;
            }

            string token = item.Window.GenerateActivationToken();
            app.WindowEvent(
                this,
                item.Window.Id,
                new WindowEvent(new WindowEvent.ActivationTokenDone(
                    item.Serial,
                    ActivationToken.FromRaw(token))));
        }
    }

    internal unsafe void UpdateDeviceEventFilter(bool focused)
    {
        if (_xconn.XInput2Opcode is null)
        {
            return;
        }

        bool enabled = _deviceEvents == DeviceEvents.Always ||
            (focused && _deviceEvents == DeviceEvents.WhenFocused);
        Span<byte> mask = stackalloc byte[3];
        if (enabled)
        {
            SetXiMask(mask, PInvoke.XiRawMotion);
            SetXiMask(mask, PInvoke.XiRawButtonPress);
            SetXiMask(mask, PInvoke.XiRawButtonRelease);
            SetXiMask(mask, PInvoke.XiRawKeyPress);
            SetXiMask(mask, PInvoke.XiRawKeyRelease);
        }

        fixed (byte* maskPtr = mask)
        {
            XIEventMask eventMask = new()
            {
                DeviceId = PInvoke.XiAllMasterDevices,
                MaskLen = mask.Length,
                Mask = maskPtr,
            };
            try
            {
                _ = PInvoke.XISelectEvents(_xconn.Display, _xconn.RootWindow, &eventMask, 1);
                _xconn.Flush();
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }

    private unsafe void SelectDeviceHierarchyEvents()
    {
        if (_xconn.XInput2Opcode is null)
        {
            return;
        }

        Span<byte> mask = stackalloc byte[2];
        SetXiMask(mask, PInvoke.XiHierarchyChanged);

        fixed (byte* maskPtr = mask)
        {
            XIEventMask eventMask = new()
            {
                DeviceId = PInvoke.XiAllDevices,
                MaskLen = mask.Length,
                Mask = maskPtr,
            };
            try
            {
                _ = PInvoke.XISelectEvents(_xconn.Display, _xconn.RootWindow, &eventMask, 1);
                _xconn.Flush();
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }

    private void SelectXkbEvents()
    {
        if (_xconn.XkbFirstEvent is null || _xkbContext is null)
        {
            return;
        }

        uint events =
            PInvoke.XkbNewKeyboardNotifyMask |
            PInvoke.XkbMapNotifyMask |
            PInvoke.XkbStateNotifyMask;

        try
        {
            _ = PInvoke.XkbSelectEvents(_xconn.Display, PInvoke.XkbUseCoreKbd, events, events);
            _xconn.Flush();
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private StartCause StartCauseForWake(Instant waitStart)
    {
        if (ControlFlow.TryGetValue(out ControlFlow.Poll _))
        {
            return new StartCause(new StartCause.Poll());
        }

        if (ControlFlow.TryGetValue(out ControlFlow.WaitUntil waitUntil) &&
            Instant.Now().Timestamp >= waitUntil.Instant.Timestamp)
        {
            return new StartCause(new StartCause.ResumeTimeReached(waitStart, waitUntil.Instant));
        }

        Instant? requestedResume = ControlFlow.TryGetValue(out ControlFlow.WaitUntil requested)
            ? requested.Instant
            : null;
        return new StartCause(new StartCause.WaitCancelled(waitStart, requestedResume));
    }

    private bool DispatchOrCheckProxyWakeUp(IApplicationHandler app, in XEvent xevent)
    {
        XEvent localEvent = xevent;
        if (xevent.Type == PInvoke.ClientMessage &&
            xevent.ClientMessage.Window == _proxyWindow &&
            xevent.ClientMessage.MessageType == _xconn.Atoms[AtomName.WinitWakeUp])
        {
            return true;
        }

        if (localEvent.Type == PInvoke.GenericEvent &&
            _xconn.XInput2Opcode is { } xiOpcode &&
            localEvent.GenericEventCookie.Extension == xiOpcode)
        {
            using GenericEventCookie? cookie = GenericEventCookie.FromEvent(_xconn, in localEvent);
            if (cookie is not null)
            {
                _eventProcessor.DispatchXInput2(app, cookie.Cookie);
                _eventProcessor.DrainImeEvents(app);
            }

            return false;
        }

        _eventProcessor.Dispatch(app, in localEvent);
        _eventProcessor.DrainImeEvents(app);
        return false;
    }

    internal static void SetXiMask(Span<byte> mask, int eventType)
    {
        mask[eventType >> 3] |= (byte)(1 << (eventType & 7));
    }

    private static XlibWindow CreateProxyWindow(XConnection xconn)
    {
        XlibWindow window = PInvoke.XCreateSimpleWindow(
            xconn.Display,
            xconn.RootWindow.Value,
            0,
            0,
            1,
            1,
            0,
            0,
            0);
        xconn.Flush();
        return window;
    }

    internal static void RegisterXlibErrorHook(XlibErrorHook hook)
    {
        lock (s_xlibErrorHooks)
        {
            s_xlibErrorHooks.Add(hook);
        }
    }

    private static XConnection GetBackend()
    {
        lock (s_backendLock)
        {
            return s_backend ??= XConnection.New(&XErrorCallback);
        }
    }

    private static Context? CreateXkbContext(XConnection connection)
    {
        if (connection.XcbConnection == 0)
        {
            return null;
        }

        try
        {
            return Context.FromX11Xkb(connection.XcbConnection);
        }
        catch
        {
            return null;
        }
    }

    private static unsafe void InitializeLocaleForIme()
    {
        try
        {
            sbyte* defaultLocale = PInvoke.SetLocale(PInvoke.LC_CTYPE, null);
            Span<byte> emptyLocale = stackalloc byte[1];
            fixed (byte* emptyLocalePtr = emptyLocale)
            {
                _ = PInvoke.SetLocale(PInvoke.LC_CTYPE, (sbyte*)emptyLocalePtr);
            }

            if (PInvoke.XSupportsLocale() != 1 && defaultLocale is not null)
            {
                _ = PInvoke.SetLocale(PInvoke.LC_CTYPE, defaultLocale);
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static Winit.X11.Ime.Ime? CreateIme(XConnection xconn)
    {
        try
        {
            return Winit.X11.Ime.Ime.New(xconn);
        }
        catch
        {
            return null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int XErrorCallback(nint display, XErrorEvent* eventPtr)
    {
        bool handled = false;

        lock (s_xlibErrorHooks)
        {
            foreach (XlibErrorHook hook in s_xlibErrorHooks)
            {
                handled |= hook(display, (nint)eventPtr);
            }
        }

        if (!handled)
        {
            XError error = XError.FromEvent(display, eventPtr);
            lock (s_backendLock)
            {
                s_backend?.SetLatestError(error);
            }
        }

        return 0;
    }
}

internal sealed class EventLoopProxyProvider(XConnection xconn, XlibWindow proxyWindow, Atom wakeUpAtom)
    : IEventLoopProxyProvider
{
    public void WakeUp()
    {
        xconn.SendClientMessage(proxyWindow, proxyWindow, wakeUpAtom, PInvoke.NoEventMask, [0, 0, 0, 0, 0]);
        xconn.Flush();
    }
}

internal sealed unsafe class DeviceInfo : IDisposable
{
    private XIDeviceInfo* _info;

    private DeviceInfo(XIDeviceInfo* info, int count)
    {
        _info = info;
        Count = count;
    }

    public int Count { get; }

    public ReadOnlySpan<XIDeviceInfo> Infos => new(_info, Count);

    public static DeviceInfo? Get(XConnection xconn, int deviceId)
    {
        XIDeviceInfo* info = null;
        bool transferred = false;
        try
        {
            int count = 0;
            info = PInvoke.XIQueryDevice(xconn.Display, deviceId, &count);
            xconn.CheckErrors();

            if (info is null || count == 0)
            {
                return null;
            }

            transferred = true;
            return new DeviceInfo(info, count);
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
        catch (XError)
        {
            return null;
        }
        finally
        {
            if (info is not null && !transferred)
            {
                PInvoke.XIFreeDeviceInfo(info);
            }
        }
    }

    public void Dispose()
    {
        if (_info is null)
        {
            return;
        }

        PInvoke.XIFreeDeviceInfo(_info);
        _info = null;
    }
}

internal sealed class Device
{
    private readonly List<ScrollAxis> _scrollAxes;

    private Device(string name, List<ScrollAxis> scrollAxes, int attachment, DeviceType type)
    {
        Name = name;
        _scrollAxes = scrollAxes;
        Attachment = attachment;
        Type = type;
    }

    public string Name { get; }

    public int Attachment { get; }

    public DeviceType Type { get; }

    public static unsafe Device New(XIDeviceInfo info, Atoms atoms)
    {
        string name = Marshal.PtrToStringUTF8((nint)info.Name) ?? string.Empty;
        List<ScrollAxis> scrollAxes = [];
        DeviceType? type = null;

        if (PhysicalDevice(info))
        {
            for (int i = 0; i < info.NumClasses; i++)
            {
                XIAnyClassInfo* classInfo = ClassAt(info, i);
                if (classInfo is null)
                {
                    continue;
                }

                if (classInfo->Type == PInvoke.XiScrollClass)
                {
                    XIScrollClassInfo* scrollInfo = (XIScrollClassInfo*)classInfo;
                    scrollAxes.Add(new ScrollAxis(
                        scrollInfo->Number,
                        scrollInfo->Increment,
                        scrollInfo->ScrollType == PInvoke.XiScrollTypeHorizontal
                            ? ScrollOrientation.Horizontal
                            : ScrollOrientation.Vertical));
                }
                else if (classInfo->Type == PInvoke.XiTouchClass)
                {
                    type = DeviceType.Touch;
                }
                else if (type is null && classInfo->Type == PInvoke.XiValuatorClass)
                {
                    XIValuatorClassInfo* valuatorInfo = (XIValuatorClassInfo*)classInfo;
                    Atom atom = valuatorInfo->Label;
                    if (atom == atoms[AtomName.AbsX] ||
                        atom == atoms[AtomName.AbsY] ||
                        atom == atoms[AtomName.AbsPressure] ||
                        atom == atoms[AtomName.AbsTiltX] ||
                        atom == atoms[AtomName.AbsTiltY])
                    {
                        type = name.Contains("eraser", StringComparison.OrdinalIgnoreCase)
                            ? DeviceType.Eraser
                            : DeviceType.Pen;
                    }
                }
            }
        }

        Device device = new(name, scrollAxes, info.Attachment, type ?? DeviceType.Mouse);
        device.ResetScrollPosition(info);
        return device;
    }

    public unsafe void ResetScrollPosition(XIDeviceInfo info)
    {
        if (!PhysicalDevice(info))
        {
            return;
        }

        for (int i = 0; i < info.NumClasses; i++)
        {
            XIAnyClassInfo* classInfo = ClassAt(info, i);
            if (classInfo is null)
            {
                continue;
            }

            if (classInfo->Type != PInvoke.XiValuatorClass)
            {
                continue;
            }

            XIValuatorClassInfo* valuatorInfo = (XIValuatorClassInfo*)classInfo;
            ScrollAxis? axis = _scrollAxes.FirstOrDefault(axis => axis.Number == valuatorInfo->Number);
            if (axis is not null)
            {
                axis.Position = valuatorInfo->Value;
            }
        }
    }

    public bool TryUpdateScrollAxis(int number, double value, out MouseScrollDelta delta)
    {
        ScrollAxis? axis = _scrollAxes.FirstOrDefault(axis => axis.Number == number);
        if (axis is null || axis.Increment == 0.0)
        {
            delta = default;
            return false;
        }

        double lineDelta = (value - axis.Position) / axis.Increment;
        axis.Position = value;
        delta = axis.Orientation == ScrollOrientation.Horizontal
            ? new MouseScrollDelta(new MouseScrollDelta.LineDelta((float)-lineDelta, 0.0f))
            : new MouseScrollDelta(new MouseScrollDelta.LineDelta(0.0f, (float)-lineDelta));
        return true;
    }

    private static bool PhysicalDevice(XIDeviceInfo info)
    {
        return info.Use is PInvoke.XiSlaveKeyboard or PInvoke.XiSlavePointer or PInvoke.XiFloatingSlave;
    }

    private static unsafe XIAnyClassInfo* ClassAt(XIDeviceInfo info, int index)
    {
        return info.Classes is null || index < 0 || index >= info.NumClasses ? null : info.Classes[index];
    }
}

internal enum DeviceType
{
    Mouse,
    Touch,
    Pen,
    Eraser,
}

internal enum ScrollOrientation
{
    Vertical,
    Horizontal,
}

internal sealed class ScrollAxis(int number, double increment, ScrollOrientation orientation)
{
    public int Number { get; } = number;

    public double Increment { get; } = increment;

    public ScrollOrientation Orientation { get; } = orientation;

    public double Position { get; set; }
}
