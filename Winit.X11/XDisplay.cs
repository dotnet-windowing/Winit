using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Winit.X11;

internal sealed unsafe partial class XConnection : IDisposable
{
    private readonly Lock _cursorLock = new();
    private readonly Dictionary<string, nuint> _cursorCache = [];
    private int _disposed;
    private nuint? _hiddenCursor;

    private XConnection(
        nint display,
        nint xcbConnection,
        int defaultScreen,
        int defaultDepth,
        XlibWindow rootWindow,
        Atoms atoms,
        (int Major, int Minor)? randrVersion,
        int? randrFirstEvent,
        (int Major, int Minor)? syncVersion,
        int? xinput2Opcode,
        int? xkbFirstEvent,
        Atom? xsettingsScreen,
        string? resourceManagerString)
    {
        Display = display;
        XcbConnection = xcbConnection;
        DefaultScreen = defaultScreen;
        DefaultDepth = defaultDepth;
        RootWindow = rootWindow;
        Atoms = atoms;
        RandrVersion = randrVersion;
        RandrFirstEvent = randrFirstEvent;
        SyncVersion = syncVersion;
        XInput2Opcode = xinput2Opcode;
        XkbFirstEvent = xkbFirstEvent;
        XSettingsScreen = xsettingsScreen;
        ResourceManagerString = resourceManagerString;
    }

    public nint Display { get; }

    public nint XcbConnection { get; }

    public int DefaultScreen { get; }

    public int DefaultDepth { get; }

    public XlibWindow RootWindow { get; }

    public Atoms Atoms { get; }

    public (int Major, int Minor)? RandrVersion { get; }

    public int? RandrFirstEvent { get; }

    public (int Major, int Minor)? SyncVersion { get; }

    public int? XInput2Opcode { get; }

    public int? XkbFirstEvent { get; }

    public Atom? XSettingsScreen { get; }

    public string? ResourceManagerString { get; private set; }

    public nuint Timestamp { get; private set; }

    public XError? LatestError { get; private set; }

    internal Lock CursorLock => _cursorLock;

    internal Dictionary<string, nuint> CursorCache => _cursorCache;

    internal nuint? HiddenCursor
    {
        get => _hiddenCursor;
        set => _hiddenCursor = value;
    }

    public static XConnection New(delegate* unmanaged[Cdecl]<nint, XErrorEvent*, int> errorHandler)
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new XNotSupported("The X11 backend is only supported on Linux.");
        }

        _ = PInvoke.XInitThreads();
        _ = PInvoke.XSetErrorHandler(errorHandler);

        nint display = PInvoke.XOpenDisplay(null);
        if (display == 0)
        {
            throw new XNotSupported("XOpenDisplay failed. Is DISPLAY set?");
        }

        try
        {
            int defaultScreen = PInvoke.XDefaultScreen(display);
            int defaultDepth = PInvoke.XDefaultDepth(display, defaultScreen);
            nint xcbConnection = PInvoke.XGetXCBConnection(display);
            XlibWindow rootWindow = new(PInvoke.XRootWindow(display, defaultScreen));
            Atoms atoms = Atoms.New(display);
            _ = PInvoke.XSelectInput(display, rootWindow, PInvoke.PropertyChangeMask);
            (int Major, int Minor)? randrVersion = TryQueryRandrVersion(display);
            int? randrFirstEvent = TryQueryRandrFirstEvent(display);
            (int Major, int Minor)? syncVersion = TryQuerySyncVersion(display);
            int? xinput2Opcode = TryQueryXInput2Opcode(display);
            int? xkbFirstEvent = TryQueryXkbFirstEvent(display);
            Atom? xsettingsScreen = TryCreateXSettingsScreen(display, defaultScreen);
            string? resourceManagerString = GetResourceManagerString(display);

            return new XConnection(
                display,
                xcbConnection,
                defaultScreen,
                defaultDepth,
                rootWindow,
                atoms,
                randrVersion,
                randrFirstEvent,
                syncVersion,
                xinput2Opcode,
                xkbFirstEvent,
                xsettingsScreen,
                resourceManagerString);
        }
        catch
        {
            _ = PInvoke.XCloseDisplay(display);
            throw;
        }
    }

    public void SetLatestError(XError error)
    {
        LatestError = error;
    }

    public void SetTimestamp(nuint timestamp)
    {
        if (timestamp != PInvoke.CurrentTime)
        {
            Timestamp = timestamp;
        }
    }

    public void CheckErrors()
    {
        XError? error = LatestError;
        LatestError = null;

        if (error is not null)
        {
            throw error;
        }
    }

    public void Flush()
    {
        _ = PInvoke.XFlush(Display);
    }

    public void Sync(bool discard)
    {
        _ = PInvoke.XSync(Display, discard ? 1 : 0);
    }

    public void ReloadResourceManagerString()
    {
        ResourceManagerString = GetResourceManagerString(Display);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (nuint cursor in _cursorCache.Values)
        {
            _ = PInvoke.XFreeCursor(Display, cursor);
        }

        if (_hiddenCursor is { } hiddenCursor)
        {
            _ = PInvoke.XFreeCursor(Display, hiddenCursor);
        }

        _ = PInvoke.XCloseDisplay(Display);
    }

    private static (int Major, int Minor)? TryQueryRandrVersion(nint display)
    {
        try
        {
            int major = 0;
            int minor = 0;
            return PInvoke.XRRQueryVersion(display, &major, &minor) != 0
                ? (major, minor)
                : null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static int? TryQueryRandrFirstEvent(nint display)
    {
        try
        {
            int eventBase = 0;
            int errorBase = 0;
            return PInvoke.XRRQueryExtension(display, &eventBase, &errorBase) != 0
                ? eventBase
                : null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static int? TryQueryXInput2Opcode(nint display)
    {
        try
        {
            byte[] extensionName = Encoding.ASCII.GetBytes("XInputExtension\0");
            fixed (byte* extensionNamePtr = extensionName)
            {
                int opcode = 0;
                int firstEvent = 0;
                int firstError = 0;
                if (PInvoke.XQueryExtension(display, (sbyte*)extensionNamePtr, &opcode, &firstEvent, &firstError) == 0)
                {
                    return null;
                }

                int major = 2;
                int minor = 0;
                return PInvoke.XIQueryVersion(display, &major, &minor) != 0 ? opcode : null;
            }
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static (int Major, int Minor)? TryQuerySyncVersion(nint display)
    {
        try
        {
            int eventBase = 0;
            int errorBase = 0;
            if (PInvoke.XSyncQueryExtension(display, &eventBase, &errorBase) == 0)
            {
                return null;
            }

            int major = 3;
            int minor = 1;
            return PInvoke.XSyncInitialize(display, &major, &minor) != 0 ? (major, minor) : null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static int? TryQueryXkbFirstEvent(nint display)
    {
        try
        {
            int opcode = 0;
            int eventBase = 0;
            int errorBase = 0;
            int major = 1;
            int minor = 0;
            return PInvoke.XkbQueryExtension(display, &opcode, &eventBase, &errorBase, &major, &minor) != 0
                ? eventBase
                : null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static Atom? TryCreateXSettingsScreen(nint display, int defaultScreen)
    {
        byte[] name = Encoding.UTF8.GetBytes($"_XSETTINGS_S{defaultScreen}\0");
        fixed (byte* namePtr = name)
        {
            Atom xsettingsScreen = PInvoke.XInternAtom(display, (sbyte*)namePtr, 0);
            if (xsettingsScreen.IsNone)
            {
                return null;
            }

            XlibWindow owner = PInvoke.XGetSelectionOwner(display, xsettingsScreen);
            if (owner.Value != 0)
            {
                _ = PInvoke.XSelectInput(display, owner, PInvoke.PropertyChangeMask);
            }

            return xsettingsScreen;
        }
    }

    private static string? GetResourceManagerString(nint display)
    {
        sbyte* value = PInvoke.XResourceManagerString(display);
        if (value is null)
        {
            return null;
        }

        return Marshal.PtrToStringUTF8((nint)value);
    }
}

internal sealed class XNotSupported(string message) : Exception(message);

internal sealed class XError(
    string description,
    byte errorCode,
    byte requestCode,
    byte minorCode,
    nuint resourceId,
    nuint serial)
    : Exception($"{description} (error={errorCode}, request={requestCode}, minor={minorCode}, resource={resourceId}, serial={serial})")
{
    public byte ErrorCode { get; } = errorCode;

    public byte RequestCode { get; } = requestCode;

    public byte MinorCode { get; } = minorCode;

    public nuint ResourceId { get; } = resourceId;

    public nuint Serial { get; } = serial;

    public static unsafe XError FromEvent(nint display, XErrorEvent* eventPtr)
    {
        Span<byte> buffer = stackalloc byte[1024];
        fixed (byte* bufferPtr = buffer)
        {
            _ = PInvoke.XGetErrorText(display, eventPtr->ErrorCode, (sbyte*)bufferPtr, buffer.Length);
        }

        int length = buffer.IndexOf((byte)0);
        if (length < 0)
        {
            length = buffer.Length;
        }

        string description = Encoding.UTF8.GetString(buffer[..length]);
        return new XError(
            description,
            eventPtr->ErrorCode,
            eventPtr->RequestCode,
            eventPtr->MinorCode,
            eventPtr->ResourceId,
            eventPtr->Serial);
    }
}
