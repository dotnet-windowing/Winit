using System.Drawing;
using System.Runtime.InteropServices;

namespace Winit.Win32;

[StructLayout(LayoutKind.Sequential)]
internal struct NativePoint
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativePointL
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    public HWND hwnd;
    public uint message;
    public WPARAM wParam;
    public LPARAM lParam;
    public uint time;
    public NativePoint pt;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WINDOWPOS
{
    public HWND hwnd;
    public HWND hwndInsertAfter;
    public int x;
    public int y;
    public int cx;
    public int cy;
    public SET_WINDOW_POS_FLAGS flags;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NCCALCSIZE_PARAMS
{
    public RECT rgrc0;
    public RECT rgrc1;
    public RECT rgrc2;
    public WINDOWPOS* lppos;
}

internal unsafe delegate BOOL MONITORENUMPROC(HMONITOR monitor, HDC hdc, RECT* place, LPARAM data);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct WNDCLASSEXW
{
    public uint cbSize;
    public WNDCLASS_STYLES style;
    public delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT> lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public HINSTANCE hInstance;
    public nint hIcon;
    public HCURSOR hCursor;
    public nint hbrBackground;
    public PCWSTR lpszMenuName;
    public PCWSTR lpszClassName;
    public nint hIconSm;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MINMAXINFO
{
    public NativePoint ptReserved;
    public NativePoint ptMaxSize;
    public NativePoint ptMaxPosition;
    public NativePoint ptMinTrackSize;
    public NativePoint ptMaxTrackSize;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowPlacement
{
    public uint Length;
    public uint Flags;
    public uint ShowCommand;
    public NativePoint MinPosition;
    public NativePoint MaxPosition;
    public RECT NormalPosition;
    public RECT Device;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    public uint cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct MONITORINFOEXW
{
    public MONITORINFO monitorInfo;
    public fixed char szDevice[32];
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DEVMODEW
{
    public fixed char dmDeviceName[32];
    public ushort dmSpecVersion;
    public ushort dmDriverVersion;
    public ushort dmSize;
    public ushort dmDriverExtra;
    public DEVMODE_FIELD_FLAGS dmFields;
    public NativePointL dmPosition;
    public uint dmDisplayOrientation;
    public uint dmDisplayFixedOutput;
    public short dmColor;
    public short dmDuplex;
    public short dmYResolution;
    public short dmTTOption;
    public short dmCollate;
    public fixed char dmFormName[32];
    public ushort dmLogPixels;
    public uint dmBitsPerPel;
    public uint dmPelsWidth;
    public uint dmPelsHeight;
    public uint dmDisplayFlags;
    public uint dmDisplayFrequency;
    public uint dmICMMethod;
    public uint dmICMIntent;
    public uint dmMediaType;
    public uint dmDitherType;
    public uint dmReserved1;
    public uint dmReserved2;
    public uint dmPanningWidth;
    public uint dmPanningHeight;
}

internal readonly record struct DPI_AWARENESS_CONTEXT(nint Value);

[StructLayout(LayoutKind.Sequential)]
internal struct CompositionForm
{
    public uint Style;
    public NativePoint CurrentPosition;
    public RECT Area;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CandidateForm
{
    public uint Index;
    public uint Style;
    public NativePoint CurrentPosition;
    public RECT Area;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct HighContrast
{
    public uint Size;
    public uint Flags;
    public nint DefaultScheme;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IconInfo
{
    public BOOL Icon;
    public uint XHotspot;
    public uint YHotspot;
    public nint MaskBitmap;
    public nint ColorBitmap;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TrackMouseEventData
{
    public uint Size;
    public uint Flags;
    public nint Window;
    public uint HoverTime;
}

[StructLayout(LayoutKind.Sequential)]
internal struct FlashWindowInfo
{
    public uint Size;
    public nint Window;
    public uint Flags;
    public uint Count;
    public uint Timeout;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TouchInput
{
    public int X;
    public int Y;
    public nint Source;
    public uint Id;
    public uint Flags;
    public uint Mask;
    public uint Time;
    public nint ExtraInfo;
    public uint ContactX;
    public uint ContactY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PointerInfo
{
    public uint PointerType;
    public uint PointerId;
    public uint FrameId;
    public uint PointerFlags;
    public nint SourceDevice;
    public HWND Target;
    public NativePoint PixelLocation;
    public NativePoint HimetricLocation;
    public NativePoint PixelLocationRaw;
    public NativePoint HimetricLocationRaw;
    public uint Time;
    public uint HistoryCount;
    public int InputData;
    public uint KeyStates;
    public ulong PerformanceCount;
    public int ButtonChangeType;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PointerTouchInfo
{
    public PointerInfo PointerInfo;
    public uint TouchFlags;
    public uint TouchMask;
    public RECT Contact;
    public RECT ContactRaw;
    public uint Orientation;
    public uint Pressure;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PointerPenInfo
{
    public PointerInfo PointerInfo;
    public uint PenFlags;
    public uint PenMask;
    public uint Pressure;
    public uint Rotation;
    public int TiltX;
    public int TiltY;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct RawInputDevice(ushort UsagePage, ushort Usage, uint Flags, nint Target);

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputHeader
{
    public uint Type;
    public uint Size;
    public long Device;
    public nuint Parameter;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputDeviceList
{
    public nint Device;
    public uint Type;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawDeviceInfo
{
    public uint Size;
    public uint Type;
    public RawDeviceInfoUnion Data;
}

[StructLayout(LayoutKind.Explicit)]
internal struct RawDeviceInfoUnion
{
    [FieldOffset(0)]
    public RawDeviceInfoMouse Mouse;

    [FieldOffset(0)]
    public RawDeviceInfoKeyboard Keyboard;

    [FieldOffset(0)]
    public RawDeviceInfoHid Hid;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawDeviceInfoMouse
{
    public uint Id;
    public uint NumberOfButtons;
    public uint SampleRate;
    public BOOL HasHorizontalWheel;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawDeviceInfoKeyboard
{
    public uint Type;
    public uint SubType;
    public uint KeyboardMode;
    public uint NumberOfFunctionKeys;
    public uint NumberOfIndicators;
    public uint NumberOfKeysTotal;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawDeviceInfoHid
{
    public uint VendorId;
    public uint ProductId;
    public uint VersionNumber;
    public ushort UsagePage;
    public ushort Usage;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputData
{
    public RawInputHeader Header;
    public RawInputUnion Data;
}

[StructLayout(LayoutKind.Explicit)]
internal struct RawInputUnion
{
    [FieldOffset(0)]
    public RawMouse Mouse;

    [FieldOffset(0)]
    public RawKeyboard Keyboard;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawMouse
{
    public ushort Flags;
    public ushort ButtonFlags;
    public ushort ButtonData;
    public ushort RawButtonsHigh;
    public uint RawButtons;
    public int LastX;
    public int LastY;
    public uint ExtraInformation;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawKeyboard
{
    public ushort MakeCode;
    public ushort Flags;
    public ushort Reserved;
    public ushort VKey;
    public uint Message;
    public uint ExtraInformation;
}

[Flags]
internal enum WNDCLASS_STYLES : uint
{
    CS_VREDRAW = 0x0001,
    CS_HREDRAW = 0x0002,
}

[Flags]
internal enum WINDOW_STYLE : uint
{
    WS_OVERLAPPED = 0x00000000,
    WS_POPUP = 0x80000000,
    WS_CHILD = 0x40000000,
    WS_MINIMIZE = 0x20000000,
    WS_VISIBLE = 0x10000000,
    WS_CLIPSIBLINGS = 0x04000000,
    WS_CLIPCHILDREN = 0x02000000,
    WS_MAXIMIZE = 0x01000000,
    WS_BORDER = 0x00800000,
    WS_CAPTION = 0x00C00000,
    WS_SYSMENU = 0x00080000,
    WS_SIZEBOX = 0x00040000,
    WS_MINIMIZEBOX = 0x00020000,
    WS_MAXIMIZEBOX = 0x00010000,
    WS_OVERLAPPEDWINDOW = WS_CAPTION | WS_SYSMENU | WS_SIZEBOX | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
}

[Flags]
internal enum WINDOW_EX_STYLE : uint
{
    WS_EX_TOPMOST = 0x00000008,
    WS_EX_ACCEPTFILES = 0x00000010,
    WS_EX_TRANSPARENT = 0x00000020,
    WS_EX_WINDOWEDGE = 0x00000100,
    WS_EX_APPWINDOW = 0x00040000,
    WS_EX_LAYERED = 0x00080000,
    WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
}

[Flags]
internal enum SET_WINDOW_POS_FLAGS : uint
{
    SWP_NOSIZE = 0x0001,
    SWP_NOMOVE = 0x0002,
    SWP_NOZORDER = 0x0004,
    SWP_NOACTIVATE = 0x0010,
    SWP_FRAMECHANGED = 0x0020,
    SWP_ASYNCWINDOWPOS = 0x4000,
}

internal enum SHOW_WINDOW_CMD : int
{
    SW_HIDE = 0,
    SW_SHOWNOACTIVATE = 4,
    SW_SHOW = 5,
    SW_MINIMIZE = 6,
    SW_RESTORE = 9,
    SW_MAXIMIZE = 3,
}

[Flags]
internal enum MENU_ITEM_FLAGS : uint
{
    MF_BYCOMMAND = 0x00000000,
    MF_ENABLED = 0x00000000,
    MF_DISABLED = 0x00000002,
}

internal enum WINDOW_LONG_PTR_INDEX : int
{
    GWL_STYLE = -16,
    GWL_EXSTYLE = -20,
}

internal enum MONITOR_FROM_FLAGS : uint
{
    MONITOR_DEFAULTTONULL = 0,
    MONITOR_DEFAULTTOPRIMARY = 1,
    MONITOR_DEFAULTTONEAREST = 2,
}

internal enum MONITOR_DPI_TYPE
{
    MDT_EFFECTIVE_DPI = 0,
}

internal enum ENUM_DISPLAY_SETTINGS_MODE : uint
{
    ENUM_CURRENT_SETTINGS = 0xFFFFFFFF,
}

[Flags]
internal enum ENUM_DISPLAY_SETTINGS_FLAGS : uint
{
    EDS_RAWMODE = 0x00000002,
}

[Flags]
internal enum DEVMODE_FIELD_FLAGS : uint
{
    DM_BITSPERPEL = 0x00040000,
    DM_PELSWIDTH = 0x00080000,
    DM_PELSHEIGHT = 0x00100000,
    DM_DISPLAYFREQUENCY = 0x00400000,
}

internal static unsafe partial class PInvoke
{
    public const int CW_USEDEFAULT = unchecked((int)0x80000000);
    public const uint WM_APP = 0x8000;
    public const uint WM_CAPTURECHANGED = 0x0215;
    public const uint WM_CHAR = 0x0102;
    public const uint WM_CLOSE = 0x0010;
    public const uint WM_DEADCHAR = 0x0103;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_DPICHANGED = 0x02E0;
    public const uint WM_ENTERSIZEMOVE = 0x0231;
    public const uint WM_EXITSIZEMOVE = 0x0232;
    public const uint WM_GETMINMAXINFO = 0x0024;
    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_MENUCHAR = 0x0120;
    public const uint WM_NCCALCSIZE = 0x0083;
    public const uint WM_NCACTIVATE = 0x0086;
    public const uint WM_NCDESTROY = 0x0082;
    public const uint WM_PAINT = 0x000F;
    public const uint WM_KILLFOCUS = 0x0008;
    public const uint WM_LBUTTONDOWN = 0x0201;
    public const uint WM_LBUTTONUP = 0x0202;
    public const uint WM_MBUTTONDOWN = 0x0207;
    public const uint WM_MBUTTONUP = 0x0208;
    public const uint WM_MOUSEHWHEEL = 0x020E;
    public const uint WM_MOUSELEAVE = 0x02A3;
    public const uint WM_MOUSEMOVE = 0x0200;
    public const uint WM_MOUSEWHEEL = 0x020A;
    public const uint WM_NCLBUTTONDOWN = 0x00A1;
    public const uint WM_POINTERUPDATE = 0x0245;
    public const uint WM_POINTERDOWN = 0x0246;
    public const uint WM_POINTERUP = 0x0247;
    public const uint WM_RBUTTONDOWN = 0x0204;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint WM_SETCURSOR = 0x0020;
    public const uint WM_SETFOCUS = 0x0007;
    public const uint WM_SETTINGCHANGE = 0x001A;
    public const uint WM_SIZE = 0x0005;
    public const uint WM_SIZING = 0x0214;
    public const uint WM_SYSCOMMAND = 0x0112;
    public const uint WM_SYSCHAR = 0x0106;
    public const uint WM_SYSDEADCHAR = 0x0107;
    public const uint WM_SYSKEYDOWN = 0x0104;
    public const uint WM_SYSKEYUP = 0x0105;
    public const uint WM_TOUCH = 0x0240;
    public const uint WM_WINDOWPOSCHANGING = 0x0046;
    public const uint WM_WINDOWPOSCHANGED = 0x0047;
    public const uint WM_XBUTTONDOWN = 0x020B;
    public const uint WM_XBUTTONUP = 0x020C;
    public const uint RDW_INTERNALPAINT = 0x0002;
    public const int WHEEL_DELTA = 120;
    public const uint WM_KEYFIRST = 0x0100;
    public const uint WM_KEYLAST = 0x0109;

    public static readonly PCWSTR IDC_ARROW = new((char*)32512);
    public static readonly PCWSTR IDC_IBEAM = new((char*)32513);
    public static readonly PCWSTR IDC_WAIT = new((char*)32514);
    public static readonly PCWSTR IDC_CROSS = new((char*)32515);
    public static readonly PCWSTR IDC_APPSTARTING = new((char*)32650);
    public static readonly PCWSTR IDC_HELP = new((char*)32651);
    public static readonly PCWSTR IDC_NO = new((char*)32648);
    public static readonly PCWSTR IDC_HAND = new((char*)32649);
    public static readonly PCWSTR IDC_SIZEALL = new((char*)32646);
    public static readonly PCWSTR IDC_SIZENESW = new((char*)32643);
    public static readonly PCWSTR IDC_SIZENS = new((char*)32645);
    public static readonly PCWSTR IDC_SIZENWSE = new((char*)32642);
    public static readonly PCWSTR IDC_SIZEWE = new((char*)32644);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AdjustWindowRectEx(
        ref RECT rect,
        WINDOW_STYLE style,
        [MarshalAs(UnmanagedType.Bool)] bool menu,
        WINDOW_EX_STYLE exStyle);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial HWND CreateWindowEx(
        WINDOW_EX_STYLE exStyle,
        PCWSTR className,
        PCWSTR windowName,
        WINDOW_STYLE style,
        int x,
        int y,
        int width,
        int height,
        HWND parent,
        HMENU menu,
        HINSTANCE instance,
        void* parameter);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial ushort RegisterClassEx(WNDCLASSEXW windowClass);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW", SetLastError = true)]
    public static partial LRESULT DefWindowProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(HWND hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RedrawWindow(HWND hwnd, RECT* updateRect, nint updateRgn, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    public static partial LRESULT DispatchMessage(MSG message);

    [LibraryImport("user32.dll", EntryPoint = "TranslateMessage")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TranslateMessage(MSG message);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessage(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam);

    [LibraryImport("user32.dll", EntryPoint = "PostThreadMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostThreadMessage(uint threadId, uint message, WPARAM wParam, LPARAM lParam);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(HWND hwnd, SHOW_WINDOW_CMD command);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateWindow(HWND hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(
        HWND hwnd,
        HWND insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        SET_WINDOW_POS_FLAGS flags);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    public static partial int SetWindowLong(HWND hwnd, WINDOW_LONG_PTR_INDEX index, int value);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(HWND hwnd, out RECT rect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(HWND hwnd, out RECT rect);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW", SetLastError = true)]
    public static partial int GetWindowTextLength(HWND hwnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial int GetWindowText(HWND hwnd, PWSTR text, int maxCount);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowText(HWND hwnd, PCWSTR text);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindowVisible(HWND hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(HWND hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsZoomed(HWND hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(HWND hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ClientToScreen(HWND hwnd, ref Point point);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetCursorPos(int x, int y);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial HMENU GetSystemMenu(HWND hwnd, [MarshalAs(UnmanagedType.Bool)] bool revert);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint EnableMenuItem(HMENU menu, uint item, MENU_ITEM_FLAGS flags);

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW", SetLastError = true)]
    public static partial HCURSOR LoadCursor(HINSTANCE instance, PCWSTR cursorName);

    [LibraryImport("user32.dll")]
    public static partial HCURSOR SetCursor(HCURSOR cursor);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial HWND SetCapture(HWND hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReleaseCapture();

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    public static partial LRESULT SendMessage(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam);

    [LibraryImport("user32.dll")]
    public static partial short GetKeyState(int virtualKey);

    public static short GetKeyState(ushort virtualKey) => GetKeyState((int)virtualKey);

    [LibraryImport("user32.dll")]
    public static partial short GetAsyncKeyState(int virtualKey);

    [LibraryImport("user32.dll")]
    public static partial uint GetDpiForWindow(HWND hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT value);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetProcessDPIAware();

    [LibraryImport("shcore.dll")]
    public static partial HRESULT GetDpiForMonitor(
        HMONITOR monitor,
        MONITOR_DPI_TYPE dpiType,
        uint* dpiX,
        uint* dpiY);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumDisplayMonitors(
        HDC hdc,
        RECT* clip,
        MONITORENUMPROC callback,
        LPARAM data);

    [LibraryImport("user32.dll")]
    public static partial HMONITOR MonitorFromPoint(Point point, MONITOR_FROM_FLAGS flags);

    [LibraryImport("user32.dll")]
    public static partial HMONITOR MonitorFromWindow(HWND hwnd, MONITOR_FROM_FLAGS flags);

    [LibraryImport("user32.dll")]
    public static partial HMONITOR MonitorFromRect(ref RECT rect, MONITOR_FROM_FLAGS flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfo(HMONITOR monitor, MONITORINFO* info);

    [LibraryImport("user32.dll", EntryPoint = "EnumDisplaySettingsExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumDisplaySettingsEx(
        string deviceName,
        ENUM_DISPLAY_SETTINGS_MODE modeNumber,
        ref DEVMODEW devMode,
        ENUM_DISPLAY_SETTINGS_FLAGS flags);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PeekMessageW(
        out MSG message,
        HWND hwnd,
        uint filterMin,
        uint filterMax,
        uint removeMessage);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint MsgWaitForMultipleObjectsEx(
        uint count,
        nint* handles,
        uint milliseconds,
        uint wakeMask,
        uint flags);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial nint CreateWaitableTimerEx(
        nint timerAttributes,
        PCWSTR timerName,
        uint flags,
        uint desiredAccess);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWaitableTimer(
        nint timer,
        long* dueTime,
        int period,
        nint completionRoutine,
        nint argumentToCompletionRoutine,
        [MarshalAs(UnmanagedType.Bool)] bool resume);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetKeyboardState(byte* keyState);

    [LibraryImport("user32.dll")]
    public static partial nint GetKeyboardLayout(uint threadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int ToUnicodeEx(
        uint virtualKey,
        uint scanCode,
        byte* keyState,
        char* buffer,
        int bufferLength,
        uint flags,
        nint keyboardLayout);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint MapVirtualKeyExW(uint code, uint mapType, nint keyboardLayout);

    [LibraryImport("user32.dll")]
    public static partial uint MapVirtualKeyW(uint code, uint mapType);

    [LibraryImport("user32.dll")]
    public static partial int ToUnicode(
        uint virtualKey,
        uint scanCode,
        byte* keyState,
        char* buffer,
        int bufferLength,
        uint flags);

    [LibraryImport("imm32.dll", SetLastError = true)]
    public static partial nint ImmGetContext(HWND hwnd);

    [LibraryImport("imm32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ImmReleaseContext(HWND hwnd, nint himc);

    [LibraryImport("imm32.dll", SetLastError = true)]
    public static partial int ImmGetCompositionStringW(nint himc, uint index, void* buffer, uint bufferLength);

    [LibraryImport("imm32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ImmSetCompositionWindow(nint himc, ref CompositionForm compositionForm);

    [LibraryImport("imm32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ImmSetCandidateWindow(nint himc, ref CandidateForm candidateForm);

    [LibraryImport("imm32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ImmAssociateContextEx(HWND hwnd, nint himc, uint flags);

    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterRawInputDevices(
        RawInputDevice* rawInputDevices,
        uint numDevices,
        uint size);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetRawInputDeviceList(
        RawInputDeviceList* rawInputDeviceList,
        ref uint numDevices,
        uint size);

    [LibraryImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", SetLastError = true)]
    public static partial uint GetRawInputDeviceInfo(
        nint rawInputDevice,
        uint command,
        void* data,
        ref uint size);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetRawInputData(
        nint rawInput,
        uint command,
        void* data,
        ref uint size,
        uint headerSize);

    [LibraryImport("ole32.dll")]
    public static partial int OleInitialize(nint reserved);

    [LibraryImport("ole32.dll")]
    public static partial int CoInitializeEx(nint reserved, uint coInit);

    [LibraryImport("ole32.dll")]
    public static partial void CoUninitialize();

    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(
        Guid* clsid,
        nint outer,
        uint clsContext,
        Guid* iid,
        void** instance);

    [LibraryImport("ole32.dll")]
    public static partial int RegisterDragDrop(HWND hwnd, IDropTarget* dropTarget);

    [LibraryImport("ole32.dll")]
    public static partial int RevokeDragDrop(HWND hwnd);

    [LibraryImport("ole32.dll")]
    public static partial void ReleaseStgMedium(ref STGMEDIUM medium);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint DragQueryFileW(nint drop, uint file, char* buffer, uint bufferLength);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ScreenToClient(HWND hwnd, ref NativePoint point);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterTouchWindow(HWND hwnd, uint flags);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetTouchInputInfo(
        nint touchInput,
        uint inputCount,
        TouchInput* inputs,
        int inputSize);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseTouchInputHandle(nint touchInput);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetPointerInfo(uint pointerId, out PointerInfo pointerInfo);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetPointerFrameInfoHistory(
        uint pointerId,
        ref uint entriesCount,
        ref uint pointerCount,
        PointerInfo* pointerInfo);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SkipPointerFrameMessages(uint pointerId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetPointerDeviceRects(
        nint device,
        out RECT pointerDeviceRect,
        out RECT displayRect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetPointerTouchInfo(uint pointerId, out PointerTouchInfo touchInfo);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetPointerPenInfo(uint pointerId, out PointerPenInfo penInfo);

    [LibraryImport("uxtheme.dll", EntryPoint = "#132")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShouldAppsUseDarkModeNative();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfoW(
        uint action,
        uint parameter,
        ref HighContrast highContrast,
        uint update);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfoW(uint action, uint parameter, ref uint value, uint update);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(nint icon);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyCursor(nint cursor);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint CreateIcon(
        nint instance,
        int width,
        int height,
        byte planes,
        byte bitsPixel,
        byte[] andBits,
        byte[] xorBits);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint GetDC(nint hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int ReleaseDC(nint hwnd, nint hdc);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial nint CreateCompatibleBitmap(nint hdc, int width, int height);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int SetBitmapBits(nint bitmap, uint byteCount, byte[] bits);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial nint CreateBitmap(int width, int height, uint planes, uint bitsPixel, byte[] bits);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint CreateIconIndirect(ref IconInfo iconInfo);

    [LibraryImport("user32.dll", EntryPoint = "LoadImageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial nint LoadImage(
        HINSTANCE instance,
        PCWSTR name,
        uint type,
        int desiredWidth,
        int desiredHeight,
        uint load);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint objectHandle);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TrackMouseEvent(ref TrackMouseEventData eventTrack);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnableWindow(HWND hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [LibraryImport("user32.dll")]
    public static partial nint GetActiveWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out Point point);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ClipCursor(RECT* rect);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint TrackPopupMenu(
        HMENU menu,
        uint flags,
        int x,
        int y,
        int reserved,
        HWND hwnd,
        nint rect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetMenuDefaultItem(HMENU menu, uint item, uint byPosition);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FlashWindowEx(ref FlashWindowInfo flashInfo);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowDisplayAffinity(HWND hwnd, uint affinity);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial uint RegisterWindowMessageW(string message);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GetModuleHandleW(char* moduleName);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowPlacement(HWND hwnd, ref WindowPlacement placement);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPlacement(HWND hwnd, ref WindowPlacement placement);

    [LibraryImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial int ChangeDisplaySettingsExW(
        string? deviceName,
        ref DEVMODEW devMode,
        HWND hwnd,
        uint flags,
        nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial int ChangeDisplaySettingsExW(
        string? deviceName,
        nint devMode,
        HWND hwnd,
        uint flags,
        nint lParam);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, ref int attributeValue, uint attributeSize);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, ref uint attributeValue, uint attributeSize);
}
