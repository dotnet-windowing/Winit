using System.Runtime.InteropServices;

namespace Winit.X11;

internal readonly record struct Atom(nuint Value)
{
    public bool IsNone => Value == 0;

    public static Atom None => new(0);
}

internal readonly record struct Xid(nuint Value);

internal readonly record struct XlibWindow(nuint Value);

[StructLayout(LayoutKind.Sequential)]
internal struct XSyncValue
{
    public int Hi;
    public uint Lo;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIEventMask
{
    public int DeviceId;
    public int MaskLen;
    public byte* Mask;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIValuatorState
{
    public int MaskLen;
    public byte* Mask;
    public double* Values;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIButtonState
{
    public int MaskLen;
    public byte* Mask;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XIModifierState
{
    public int Base;
    public int Latched;
    public int Locked;
    public int Effective;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XIGroupState
{
    public int Base;
    public int Latched;
    public int Locked;
    public int Effective;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIDeviceEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public int Extension;
    public int EvType;
    public nuint Time;
    public int DeviceId;
    public int SourceId;
    public int Detail;
    public XlibWindow Root;
    public XlibWindow Event;
    public XlibWindow Child;
    public double RootX;
    public double RootY;
    public double EventX;
    public double EventY;
    public int Flags;
    public XIButtonState Buttons;
    public XIValuatorState Valuators;
    public XIModifierState Mods;
    public XIGroupState Group;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XICrossingEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public int Extension;
    public int EvType;
    public nuint Time;
    public int DeviceId;
    public int SourceId;
    public int Detail;
    public XlibWindow Root;
    public XlibWindow Event;
    public XlibWindow Child;
    public double RootX;
    public double RootY;
    public double EventX;
    public double EventY;
    public int Mode;
    public int Focus;
    public int SameScreen;
    public XIButtonState Buttons;
    public XIModifierState Mods;
    public XIGroupState Group;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIRawEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public int Extension;
    public int EvType;
    public nuint Time;
    public int DeviceId;
    public int SourceId;
    public int Detail;
    public int Flags;
    public XIValuatorState Valuators;
    public double* RawValues;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XIAnyClassInfo
{
    public int Type;
    public int SourceId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XIValuatorClassInfo
{
    public int Type;
    public int SourceId;
    public int Number;
    public Atom Label;
    public double Min;
    public double Max;
    public double Value;
    public int Resolution;
    public int Mode;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XIScrollClassInfo
{
    public int Type;
    public int SourceId;
    public int Number;
    public int ScrollType;
    public double Increment;
    public int Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIDeviceInfo
{
    public int DeviceId;
    public sbyte* Name;
    public int Use;
    public int Attachment;
    public int Enabled;
    public int NumClasses;
    public XIAnyClassInfo** Classes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XIHierarchyInfo
{
    public int DeviceId;
    public int Attachment;
    public int Use;
    public int Enabled;
    public int Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIHierarchyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public int Extension;
    public int EvType;
    public nuint Time;
    public int Flags;
    public int NumInfo;
    public XIHierarchyInfo* Info;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XkbAnyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Time;
    public int XkbType;
    public uint Device;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XkbNewKeyboardNotifyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Time;
    public int XkbType;
    public int Device;
    public int OldDevice;
    public int MinKeyCode;
    public int MaxKeyCode;
    public int OldMinKeyCode;
    public int OldMaxKeyCode;
    public uint Changed;
    public sbyte ReqMajor;
    public sbyte ReqMinor;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XkbStateNotifyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Time;
    public int XkbType;
    public int Device;
    public uint Changed;
    public int Group;
    public int BaseGroup;
    public int LatchedGroup;
    public int LockedGroup;
    public uint Mods;
    public uint BaseMods;
    public uint LatchedMods;
    public uint LockedMods;
    public int CompatState;
    public byte GrabMods;
    public byte CompatGrabMods;
    public byte LookupMods;
    public byte CompatLookupMods;
    public int PtrButtons;
    public byte Keycode;
    public sbyte EventType;
    public sbyte ReqMajor;
    public sbyte ReqMinor;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XkbStateRec
{
    public byte Group;
    public byte LockedGroup;
    public ushort BaseGroup;
    public ushort LatchedGroup;
    public byte Mods;
    public byte BaseMods;
    public byte LatchedMods;
    public byte LockedMods;
    public byte CompatState;
    public byte GrabMods;
    public byte CompatGrabMods;
    public byte LookupMods;
    public byte CompatLookupMods;
    public ushort PtrButtons;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XPoint
{
    public short X;
    public short Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XRectangle : IEquatable<XRectangle>
{
    public short X;
    public short Y;
    public ushort Width;
    public ushort Height;

    public bool Equals(XRectangle other)
    {
        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is XRectangle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    public static bool operator ==(XRectangle left, XRectangle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(XRectangle left, XRectangle right)
    {
        return !left.Equals(right);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIMCallback
{
    public nint ClientData;
    public delegate* unmanaged[Cdecl]<nint, nint, nint, void> Callback;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIMStyles
{
    public ushort CountStyles;
    public nuint* SupportedStyles;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIMText
{
    public ushort Length;
    public nint Feedback;
    public int EncodingIsWchar;
    public sbyte* MultiByte;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XIMPreeditDrawCallbackStruct
{
    public int Caret;
    public int ChgFirst;
    public int ChgLength;
    public XIMText* Text;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XIMPreeditCaretCallbackStruct
{
    public int Position;
    public int Direction;
    public int Style;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XModifierKeymap
{
    public int MaxKeyPerMod;
    public byte* ModifierMap;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSizeHints
{
    public nint Flags;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int MinWidth;
    public int MinHeight;
    public int MaxWidth;
    public int MaxHeight;
    public int WidthInc;
    public int HeightInc;
    public int MinAspectX;
    public int MinAspectY;
    public int MaxAspectX;
    public int MaxAspectY;
    public int BaseWidth;
    public int BaseHeight;
    public int WinGravity;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XWMHints
{
    public nint Flags;
    public int Input;
    public int InitialState;
    public nuint IconPixmap;
    public XlibWindow IconWindow;
    public int IconX;
    public int IconY;
    public nuint IconMask;
    public nuint WindowGroup;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSetWindowAttributes
{
    public nuint BackgroundPixmap;
    public nuint BackgroundPixel;
    public nuint BorderPixmap;
    public nuint BorderPixel;
    public int BitGravity;
    public int WinGravity;
    public int BackingStore;
    public nuint BackingPlanes;
    public nuint BackingPixel;
    public int SaveUnder;
    public nint EventMask;
    public nint DoNotPropagateMask;
    public int OverrideRedirect;
    public nuint Colormap;
    public nuint Cursor;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XcursorImage
{
    public uint Version;
    public uint Size;
    public uint Width;
    public uint Height;
    public uint Xhot;
    public uint Yhot;
    public uint Delay;
    public uint* Pixels;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XRRModeInfo
{
    public uint Id;
    public uint Width;
    public uint Height;
    public nuint DotClock;
    public uint HSyncStart;
    public uint HSyncEnd;
    public uint HTotal;
    public uint HSkew;
    public uint VSyncStart;
    public uint VSyncEnd;
    public uint VTotal;
    public sbyte* Name;
    public uint NameLength;
    public nuint ModeFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XRRScreenResources
{
    public nuint Timestamp;
    public nuint ConfigTimestamp;
    public int NCrtc;
    public uint* Crtcs;
    public int NOutput;
    public uint* Outputs;
    public int NMode;
    public XRRModeInfo* Modes;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XRROutputInfo
{
    public nuint Timestamp;
    public uint Crtc;
    public sbyte* Name;
    public int NameLen;
    public nuint MmWidth;
    public nuint MmHeight;
    public ushort Connection;
    public ushort SubpixelOrder;
    public int NCrtc;
    public uint* Crtcs;
    public int NClone;
    public uint* Clones;
    public int NMode;
    public int NPreferred;
    public uint* Modes;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XRRCrtcInfo
{
    public nuint Timestamp;
    public int X;
    public int Y;
    public uint Width;
    public uint Height;
    public uint Mode;
    public ushort Rotation;
    public int NOutput;
    public uint* Outputs;
    public ushort Rotations;
    public int NPossible;
    public uint* Possible;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XAnyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XErrorEvent
{
    public int Type;
    public nint Display;
    public nuint ResourceId;
    public nuint Serial;
    public byte ErrorCode;
    public byte RequestCode;
    public byte MinorCode;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XExposeEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int Count;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XDestroyWindowEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Event;
    public XlibWindow Window;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XConfigureEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Event;
    public XlibWindow Window;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int BorderWidth;
    public XlibWindow Above;
    public int OverrideRedirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMapEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Event;
    public XlibWindow Window;
    public int OverrideRedirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XReparentEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Event;
    public XlibWindow Window;
    public XlibWindow Parent;
    public int X;
    public int Y;
    public int OverrideRedirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XFocusChangeEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public int Mode;
    public int Detail;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XButtonEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public XlibWindow Root;
    public XlibWindow Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int XRoot;
    public int YRoot;
    public uint State;
    public uint Button;
    public int SameScreen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMotionEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public XlibWindow Root;
    public XlibWindow Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int XRoot;
    public int YRoot;
    public uint State;
    public byte IsHint;
    public int SameScreen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XCrossingEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public XlibWindow Root;
    public XlibWindow Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int XRoot;
    public int YRoot;
    public int Mode;
    public int Detail;
    public int SameScreen;
    public int Focus;
    public uint State;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XKeyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public XlibWindow Root;
    public XlibWindow Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int XRoot;
    public int YRoot;
    public uint State;
    public uint Keycode;
    public int SameScreen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XClientMessageData
{
    public long L0;
    public long L1;
    public long L2;
    public long L3;
    public long L4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XClientMessageEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public Atom MessageType;
    public int Format;
    public XClientMessageData Data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XPropertyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public Atom Atom;
    public nuint Time;
    public int State;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSelectionEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Requestor;
    public Atom Selection;
    public Atom Target;
    public Atom Property;
    public nuint Time;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XVisibilityEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public XlibWindow Window;
    public int State;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XGenericEventCookie
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public int Extension;
    public int EvType;
    public uint Cookie;
    public void* Data;
}

[StructLayout(LayoutKind.Explicit, Size = 192)]
internal struct XEvent
{
    [FieldOffset(0)]
    public int Type;

    [FieldOffset(0)]
    public XAnyEvent Any;

    [FieldOffset(0)]
    public XExposeEvent Expose;

    [FieldOffset(0)]
    public XDestroyWindowEvent DestroyWindow;

    [FieldOffset(0)]
    public XConfigureEvent Configure;

    [FieldOffset(0)]
    public XMapEvent Map;

    [FieldOffset(0)]
    public XReparentEvent Reparent;

    [FieldOffset(0)]
    public XFocusChangeEvent FocusChange;

    [FieldOffset(0)]
    public XButtonEvent Button;

    [FieldOffset(0)]
    public XMotionEvent Motion;

    [FieldOffset(0)]
    public XCrossingEvent Crossing;

    [FieldOffset(0)]
    public XKeyEvent Key;

    [FieldOffset(0)]
    public XClientMessageEvent ClientMessage;

    [FieldOffset(0)]
    public XPropertyEvent Property;

    [FieldOffset(0)]
    public XSelectionEvent Selection;

    [FieldOffset(0)]
    public XVisibilityEvent Visibility;

    [FieldOffset(0)]
    public XGenericEventCookie GenericEventCookie;

    [FieldOffset(0)]
    public XkbAnyEvent XkbAny;

    [FieldOffset(0)]
    public XkbNewKeyboardNotifyEvent XkbNewKeyboard;

    [FieldOffset(0)]
    public XkbStateNotifyEvent XkbState;
}

internal static unsafe partial class PInvoke
{
    private const string LibC = "libc.so.6";
    private const string X11 = "libX11.so.6";
    private const string X11Xcb = "libX11-xcb.so.1";
    private const string XRandr = "libXrandr.so.2";
    private const string Xcursor = "libXcursor.so.1";
    private const string Xext = "libXext.so.6";
    private const string Xi = "libXi.so.6";

    public const int KeyPress = 2;
    public const int KeyRelease = 3;
    public const int ButtonPress = 4;
    public const int ButtonRelease = 5;
    public const int MotionNotify = 6;
    public const int EnterNotify = 7;
    public const int LeaveNotify = 8;
    public const int FocusIn = 9;
    public const int FocusOut = 10;
    public const int Expose = 12;
    public const int VisibilityNotify = 15;
    public const int DestroyNotify = 17;
    public const int UnmapNotify = 18;
    public const int MapNotify = 19;
    public const int ReparentNotify = 21;
    public const int ConfigureNotify = 22;
    public const int PropertyNotify = 28;
    public const int SelectionNotify = 31;
    public const int ClientMessage = 33;
    public const int GenericEvent = 35;
    public const int VisibilityFullyObscured = 2;
    public const int CopyFromParent = 0;
    public const uint InputOutput = 1;
    public const int RevertToParent = 2;
    public const int PropModeReplace = 0;
    public const long IconicState = 3;
    public const nuint CurrentTime = 0;
    public const nint PMinSize = 1 << 4;
    public const nint PMaxSize = 1 << 5;
    public const nint PResizeInc = 1 << 6;
    public const nint PBaseSize = 1 << 8;
    public const nint XUrgencyHint = 1 << 8;
    public const nuint CWOverrideRedirect = 1 << 9;
    public const nuint CWEventMask = 1 << 11;
    public const ushort RRConnected = 0;
    public const nuint XaAtom = 4;
    public const nuint XaCardinal = 6;
    public const nuint XaString = 31;
    public const nuint XaWindow = 33;
    public const nuint XaWmClass = 67;
    public const int XiAllDevices = 0;
    public const int XiAllMasterDevices = 1;
    public const int XiValuatorClass = 2;
    public const int XiScrollClass = 3;
    public const int XiTouchClass = 8;
    public const int XiScrollTypeVertical = 1;
    public const int XiScrollTypeHorizontal = 2;
    public const int XiMasterAdded = 1 << 0;
    public const int XiMasterRemoved = 1 << 1;
    public const int XiSlaveAdded = 1 << 2;
    public const int XiSlaveRemoved = 1 << 3;
    public const int XiSlavePointer = 3;
    public const int XiSlaveKeyboard = 4;
    public const int XiFloatingSlave = 5;
    public const int XiButtonPress = 4;
    public const int XiButtonRelease = 5;
    public const int XiMotion = 6;
    public const int XiEnter = 7;
    public const int XiLeave = 8;
    public const int XiFocusIn = 9;
    public const int XiFocusOut = 10;
    public const int XiHierarchyChanged = 11;
    public const int XiRawKeyPress = 13;
    public const int XiRawKeyRelease = 14;
    public const int XiRawButtonPress = 15;
    public const int XiRawButtonRelease = 16;
    public const int XiRawMotion = 17;
    public const int XiTouchBegin = 18;
    public const int XiTouchUpdate = 19;
    public const int XiTouchEnd = 20;
    public const int XiPointerEmulated = 1 << 16;

    public const nint NoEventMask = 0;
    public const nint KeyPressMask = 1 << 0;
    public const nint KeyReleaseMask = 1 << 1;
    public const nint ButtonPressMask = 1 << 2;
    public const nint ButtonReleaseMask = 1 << 3;
    public const nint EnterWindowMask = 1 << 4;
    public const nint LeaveWindowMask = 1 << 5;
    public const nint PointerMotionMask = 1 << 6;
    public const nint PointerMotionHintMask = 1 << 7;
    public const nint Button1MotionMask = 1 << 8;
    public const nint Button2MotionMask = 1 << 9;
    public const nint Button3MotionMask = 1 << 10;
    public const nint Button4MotionMask = 1 << 11;
    public const nint Button5MotionMask = 1 << 12;
    public const nint ButtonMotionMask = 1 << 13;
    public const nint KeymapStateMask = 1 << 14;
    public const nint ExposureMask = 1 << 15;
    public const nint VisibilityChangeMask = 1 << 16;
    public const nint StructureNotifyMask = 1 << 17;
    public const nint SubstructureNotifyMask = 1 << 19;
    public const nint SubstructureRedirectMask = 1 << 20;
    public const nint FocusChangeMask = 1 << 21;
    public const nint PropertyChangeMask = 1 << 22;
    public const int GrabModeAsync = 1;
    public const int GrabSuccess = 0;
    public const uint XkbUseCoreKbd = 0x0100;
    public const int XkbNewKeyboardNotify = 0;
    public const int XkbMapNotify = 1;
    public const int XkbStateNotify = 2;
    public const uint XkbNewKeyboardNotifyMask = 1 << XkbNewKeyboardNotify;
    public const uint XkbMapNotifyMask = 1 << XkbMapNotify;
    public const uint XkbStateNotifyMask = 1 << XkbStateNotify;
    public const uint XkbNewKeyboardKeycodesMask = 1 << 0;
    public const uint XkbNewKeyboardGeometryMask = 1 << 1;
    public const int RRScreenChangeNotifyMask = 1 << 0;
    public const int RRCrtcChangeNotifyMask = 1 << 1;
    public const int RROutputPropertyNotifyMask = 1 << 3;
    public const int LC_CTYPE = 0;
    public const int XBufferOverflow = -1;
    public const int XIMAbsolutePosition = 10;
    public const nuint XIMPreeditCallbacks = 0x0002;
    public const nuint XIMPreeditNothing = 0x0008;
    public const nuint XIMPreeditNone = 0x0010;
    public const nuint XIMStatusNothing = 0x0400;
    public const nuint XIMStatusNone = 0x0800;

    [LibraryImport(LibC, EntryPoint = "setlocale")]
    public static partial sbyte* SetLocale(int category, sbyte* locale);

    [LibraryImport(X11)]
    public static partial int XInitThreads();

    [LibraryImport(X11)]
    public static partial nint XOpenDisplay(sbyte* displayName);

    [LibraryImport(X11)]
    public static partial int XCloseDisplay(nint display);

    [LibraryImport(X11)]
    public static partial sbyte* XResourceManagerString(nint display);

    [LibraryImport(X11)]
    public static partial int XDefaultScreen(nint display);

    [LibraryImport(X11)]
    public static partial int XDefaultDepth(nint display, int screenNumber);

    [LibraryImport(X11)]
    public static partial nuint XRootWindow(nint display, int screenNumber);

    [LibraryImport(X11)]
    public static partial nuint XBlackPixel(nint display, int screenNumber);

    [LibraryImport(X11)]
    public static partial nuint XWhitePixel(nint display, int screenNumber);

    [LibraryImport(X11)]
    public static partial XlibWindow XCreateSimpleWindow(
        nint display,
        nuint parent,
        int x,
        int y,
        uint width,
        uint height,
        uint borderWidth,
        nuint border,
        nuint background);

    [LibraryImport(X11)]
    public static partial XlibWindow XCreateWindow(
        nint display,
        XlibWindow parent,
        int x,
        int y,
        uint width,
        uint height,
        uint borderWidth,
        int depth,
        uint @class,
        nint visual,
        nuint valueMask,
        XSetWindowAttributes* attributes);

    [LibraryImport(X11)]
    public static partial int XDestroyWindow(nint display, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int XMapWindow(nint display, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int XUnmapWindow(nint display, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int XMoveWindow(nint display, XlibWindow window, int x, int y);

    [LibraryImport(X11)]
    public static partial int XResizeWindow(nint display, XlibWindow window, uint width, uint height);

    [LibraryImport(X11)]
    public static partial int XMoveResizeWindow(nint display, XlibWindow window, int x, int y, uint width, uint height);

    [LibraryImport(X11)]
    public static partial int XChangeWindowAttributes(
        nint display,
        XlibWindow window,
        nuint valueMask,
        XSetWindowAttributes* attributes);

    [LibraryImport(X11)]
    public static partial int XRaiseWindow(nint display, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int XLowerWindow(nint display, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int XClearArea(
        nint display,
        XlibWindow window,
        int x,
        int y,
        uint width,
        uint height,
        int exposures);

    [LibraryImport(X11)]
    public static partial int XSetInputFocus(nint display, XlibWindow focus, int revertTo, nuint time);

    [LibraryImport(X11)]
    public static partial int XDefineCursor(nint display, XlibWindow window, nuint cursor);

    [LibraryImport(X11)]
    public static partial int XUndefineCursor(nint display, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int XFreeCursor(nint display, nuint cursor);

    [LibraryImport(X11)]
    public static partial int XQueryPointer(
        nint display,
        XlibWindow window,
        XlibWindow* rootReturn,
        XlibWindow* childReturn,
        int* rootXReturn,
        int* rootYReturn,
        int* winXReturn,
        int* winYReturn,
        uint* maskReturn);

    [LibraryImport(X11)]
    public static partial int XQueryKeymap(nint display, byte* keysReturn);

    [LibraryImport(X11)]
    public static partial int XGetGeometry(
        nint display,
        XlibWindow drawable,
        XlibWindow* rootReturn,
        int* xReturn,
        int* yReturn,
        uint* widthReturn,
        uint* heightReturn,
        uint* borderWidthReturn,
        uint* depthReturn);

    [LibraryImport(X11)]
    public static partial int XQueryTree(
        nint display,
        XlibWindow window,
        XlibWindow* rootReturn,
        XlibWindow* parentReturn,
        XlibWindow** childrenReturn,
        uint* nchildrenReturn);

    [LibraryImport(X11)]
    public static partial int XUngrabPointer(nint display, nuint time);

    [LibraryImport(X11)]
    public static partial int XGrabPointer(
        nint display,
        XlibWindow grabWindow,
        int ownerEvents,
        uint eventMask,
        int pointerMode,
        int keyboardMode,
        XlibWindow confineTo,
        nuint cursor,
        nuint time);

    [LibraryImport(X11)]
    public static partial int XWarpPointer(
        nint display,
        XlibWindow srcWindow,
        XlibWindow destWindow,
        int srcX,
        int srcY,
        uint srcWidth,
        uint srcHeight,
        int destX,
        int destY);

    [LibraryImport(X11)]
    public static partial int XTranslateCoordinates(
        nint display,
        XlibWindow srcWindow,
        XlibWindow destWindow,
        int srcX,
        int srcY,
        int* destXReturn,
        int* destYReturn,
        XlibWindow* childReturn);

    [LibraryImport(X11)]
    public static partial int XStoreName(nint display, XlibWindow window, sbyte* windowName);

    [LibraryImport(X11)]
    public static partial int XChangeProperty(
        nint display,
        XlibWindow window,
        Atom property,
        Atom type,
        int format,
        int mode,
        byte* data,
        int nelements);

    [LibraryImport(X11)]
    public static partial int XDeleteProperty(nint display, XlibWindow window, Atom property);

    [LibraryImport(X11)]
    public static partial int XGetWindowProperty(
        nint display,
        XlibWindow window,
        Atom property,
        nint longOffset,
        nint longLength,
        int delete,
        Atom reqType,
        Atom* actualTypeReturn,
        int* actualFormatReturn,
        nuint* nitemsReturn,
        nuint* bytesAfterReturn,
        byte** propReturn);

    [LibraryImport(X11)]
    public static partial int XConvertSelection(
        nint display,
        Atom selection,
        Atom target,
        Atom property,
        XlibWindow requestor,
        nuint time);

    [LibraryImport(X11)]
    public static partial int XSelectInput(nint display, XlibWindow window, nint eventMask);

    [LibraryImport(X11)]
    public static partial int XSetWMProtocols(nint display, XlibWindow window, Atom* protocols, int count);

    [LibraryImport(X11)]
    public static partial void XSetWMNormalHints(nint display, XlibWindow window, XSizeHints* hints);

    [LibraryImport(X11)]
    public static partial XWMHints* XGetWMHints(nint display, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int XSetWMHints(nint display, XlibWindow window, XWMHints* wmHints);

    [LibraryImport(X11)]
    public static partial int XSendEvent(
        nint display,
        XlibWindow window,
        int propagate,
        nint eventMask,
        XEvent* eventSend);

    [LibraryImport(X11)]
    public static partial int XIconifyWindow(nint display, XlibWindow window, int screenNumber);

    [LibraryImport(X11)]
    public static partial int XConnectionNumber(nint display);

    [LibraryImport(X11Xcb)]
    public static partial nint XGetXCBConnection(nint display);

    [LibraryImport(X11)]
    public static partial Atom XInternAtom(nint display, sbyte* atomName, int onlyIfExists);

    [LibraryImport(X11)]
    public static partial XlibWindow XGetSelectionOwner(nint display, Atom selection);

    [LibraryImport(X11)]
    public static partial int XQueryExtension(
        nint display,
        sbyte* name,
        int* majorOpcodeReturn,
        int* firstEventReturn,
        int* firstErrorReturn);

    [LibraryImport(X11)]
    public static partial int XkbQueryExtension(
        nint display,
        int* opcodeReturn,
        int* eventBaseReturn,
        int* errorBaseReturn,
        int* majorReturn,
        int* minorReturn);

    [LibraryImport(X11)]
    public static partial int XkbSelectEvents(nint display, uint deviceSpec, uint affect, uint values);

    [LibraryImport(X11)]
    public static partial int XkbGetState(nint display, uint deviceSpec, XkbStateRec* stateReturn);

    [LibraryImport(X11)]
    public static partial int XkbSetDetectableAutoRepeat(nint display, int detectable, int* supportedReturn);

    [LibraryImport(X11)]
    public static partial int XSupportsLocale();

    [LibraryImport(X11)]
    public static partial sbyte* XSetLocaleModifiers(sbyte* modifierList);

    [LibraryImport(X11)]
    public static partial nint XOpenIM(nint display, nint rdb, nint resName, nint resClass);

    [LibraryImport(X11)]
    public static partial int XCloseIM(nint im);

    [LibraryImport(X11, EntryPoint = "XGetIMValues")]
    public static partial sbyte* XGetIMValuesQueryInputStyle(
        nint im,
        sbyte* queryInputStyleName,
        XIMStyles** styles,
        nint terminator);

    [LibraryImport(X11, EntryPoint = "XSetIMValues")]
    public static partial sbyte* XSetIMValuesDestroyCallback(
        nint im,
        sbyte* destroyCallbackName,
        XIMCallback* callback,
        nint terminator);

    [LibraryImport(X11, EntryPoint = "XCreateIC")]
    public static partial nint XCreateICBasic(
        nint im,
        sbyte* inputStyleName,
        nuint inputStyle,
        sbyte* clientWindowName,
        XlibWindow clientWindow,
        nint terminator);

    [LibraryImport(X11, EntryPoint = "XCreateIC")]
    public static partial nint XCreateICWithPreeditAttributes(
        nint im,
        sbyte* inputStyleName,
        nuint inputStyle,
        sbyte* clientWindowName,
        XlibWindow clientWindow,
        sbyte* preeditAttributesName,
        nint preeditAttributes,
        nint terminator);

    [LibraryImport(X11)]
    public static partial int XDestroyIC(nint ic);

    [LibraryImport(X11)]
    public static partial void XSetICFocus(nint ic);

    [LibraryImport(X11)]
    public static partial void XUnsetICFocus(nint ic);

    [LibraryImport(X11, EntryPoint = "XVaCreateNestedList")]
    public static partial nint XVaCreateNestedListPreeditCallbacks(
        int unused,
        sbyte* startCallbackName,
        XIMCallback* startCallback,
        sbyte* doneCallbackName,
        XIMCallback* doneCallback,
        sbyte* caretCallbackName,
        XIMCallback* caretCallback,
        sbyte* drawCallbackName,
        XIMCallback* drawCallback,
        nint terminator);

    [LibraryImport(X11, EntryPoint = "XVaCreateNestedList")]
    public static partial nint XVaCreateNestedListPreeditArea(
        int unused,
        sbyte* spotLocationName,
        XPoint* spotLocation,
        sbyte* areaName,
        XRectangle* area,
        nint terminator);

    [LibraryImport(X11, EntryPoint = "XSetICValues")]
    public static partial sbyte* XSetICValuesPreeditAttributes(
        nint ic,
        sbyte* preeditAttributesName,
        nint preeditAttributes,
        nint terminator);

    [LibraryImport(X11)]
    public static partial int XFilterEvent(XEvent* @event, XlibWindow window);

    [LibraryImport(X11)]
    public static partial int Xutf8LookupString(
        nint ic,
        XKeyEvent* eventStruct,
        sbyte* bufferReturn,
        int bytesBuffer,
        nuint* keysymReturn,
        int* statusReturn);

    [LibraryImport(X11)]
    public static partial int XRegisterIMInstantiateCallback(
        nint display,
        nint rdb,
        nint resName,
        nint resClass,
        delegate* unmanaged[Cdecl]<nint, nint, nint, void> callback,
        nint clientData);

    [LibraryImport(X11)]
    public static partial int XUnregisterIMInstantiateCallback(
        nint display,
        nint rdb,
        nint resName,
        nint resClass,
        delegate* unmanaged[Cdecl]<nint, nint, nint, void> callback,
        nint clientData);

    [LibraryImport(X11)]
    public static partial int XGetAtomNames(nint display, Atom* atoms, int count, sbyte** namesReturn);

    [LibraryImport(X11)]
    public static partial XModifierKeymap* XGetModifierMapping(nint display);

    [LibraryImport(X11)]
    public static partial int XFreeModifiermap(XModifierKeymap* modmap);

    [LibraryImport(Xext)]
    public static partial int XSyncQueryExtension(nint display, int* eventBaseReturn, int* errorBaseReturn);

    [LibraryImport(Xext)]
    public static partial int XSyncInitialize(nint display, int* majorVersion, int* minorVersion);

    [LibraryImport(Xext)]
    public static partial nuint XSyncCreateCounter(nint display, XSyncValue initialValue);

    [LibraryImport(Xext)]
    public static partial void XSyncSetCounter(nint display, nuint counter, XSyncValue value);

    [LibraryImport(X11)]
    public static partial int XFlush(nint display);

    [LibraryImport(X11)]
    public static partial int XSync(nint display, int discard);

    [LibraryImport(X11)]
    public static partial int XPending(nint display);

    [LibraryImport(X11)]
    public static partial int XNextEvent(nint display, XEvent* eventReturn);

    [LibraryImport(X11)]
    public static partial int XGetEventData(nint display, XGenericEventCookie* cookie);

    [LibraryImport(X11)]
    public static partial void XFreeEventData(nint display, XGenericEventCookie* cookie);

    [LibraryImport(X11)]
    public static partial int XGetErrorText(nint display, int code, sbyte* buffer, int length);

    [LibraryImport(X11)]
    public static partial int XFree(nint data);

    [LibraryImport(X11)]
    public static partial delegate* unmanaged[Cdecl]<nint, XErrorEvent*, int> XSetErrorHandler(
        delegate* unmanaged[Cdecl]<nint, XErrorEvent*, int> handler);

    [LibraryImport(XRandr)]
    public static partial int XRRQueryExtension(nint display, int* eventBaseReturn, int* errorBaseReturn);

    [LibraryImport(XRandr)]
    public static partial int XRRQueryVersion(nint display, int* majorVersion, int* minorVersion);

    [LibraryImport(XRandr)]
    public static partial void XRRSelectInput(nint display, XlibWindow window, int mask);

    [LibraryImport(XRandr)]
    public static partial uint XRRGetOutputPrimary(nint display, XlibWindow window);

    [LibraryImport(XRandr)]
    public static partial nint XRRGetScreenResources(nint display, XlibWindow window);

    [LibraryImport(XRandr)]
    public static partial nint XRRGetScreenResourcesCurrent(nint display, XlibWindow window);

    [LibraryImport(XRandr)]
    public static partial void XRRFreeScreenResources(nint resources);

    [LibraryImport(XRandr)]
    public static partial nint XRRGetOutputInfo(nint display, nint resources, uint output);

    [LibraryImport(XRandr)]
    public static partial void XRRFreeOutputInfo(nint outputInfo);

    [LibraryImport(XRandr)]
    public static partial nint XRRGetCrtcInfo(nint display, nint resources, uint crtc);

    [LibraryImport(XRandr)]
    public static partial void XRRFreeCrtcInfo(nint crtcInfo);

    [LibraryImport(XRandr)]
    public static partial int XRRSetCrtcConfig(
        nint display,
        nint resources,
        uint crtc,
        nuint timestamp,
        int x,
        int y,
        uint mode,
        ushort rotation,
        uint* outputs,
        int noutputs);

    [LibraryImport(Xcursor)]
    public static partial nuint XcursorLibraryLoadCursor(nint display, sbyte* file);

    [LibraryImport(Xcursor)]
    public static partial XcursorImage* XcursorImageCreate(int width, int height);

    [LibraryImport(Xcursor)]
    public static partial void XcursorImageDestroy(XcursorImage* image);

    [LibraryImport(Xcursor)]
    public static partial nuint XcursorImageLoadCursor(nint display, XcursorImage* image);

    [LibraryImport(Xi)]
    public static partial int XIQueryVersion(nint display, int* majorVersionInOut, int* minorVersionInOut);

    [LibraryImport(Xi)]
    public static partial int XISelectEvents(nint display, XlibWindow window, XIEventMask* masks, int numMasks);

    [LibraryImport(Xi)]
    public static partial XIDeviceInfo* XIQueryDevice(nint display, int deviceId, int* ndevicesReturn);

    [LibraryImport(Xi)]
    public static partial void XIFreeDeviceInfo(XIDeviceInfo* info);
}
