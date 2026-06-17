using System.Runtime.InteropServices;

namespace Winit.Wayland;

internal readonly record struct WlDisplay(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlDisplay Null => new(0);
}

internal readonly record struct WlEventQueue(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlEventQueue Null => new(0);
}

internal readonly record struct WlProxy(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlProxy Null => new(0);
}

internal readonly record struct WlRegistry(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlRegistry Null => new(0);

    public static implicit operator WlProxy(WlRegistry value) => new(value.Value);
}

internal readonly record struct WlCallback(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlCallback Null => new(0);

    public static implicit operator WlProxy(WlCallback value) => new(value.Value);
}

internal readonly record struct WlCompositor(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlCompositor Null => new(0);

    public static implicit operator WlProxy(WlCompositor value) => new(value.Value);
}

internal readonly record struct WlSurface(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlSurface Null => new(0);

    public static implicit operator WlProxy(WlSurface value) => new(value.Value);
}

internal readonly record struct WlSeat(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlSeat Null => new(0);

    public static implicit operator WlProxy(WlSeat value) => new(value.Value);
}

internal readonly record struct WlPointer(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlPointer Null => new(0);

    public static implicit operator WlProxy(WlPointer value) => new(value.Value);
}

internal readonly record struct WlKeyboard(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlKeyboard Null => new(0);

    public static implicit operator WlProxy(WlKeyboard value) => new(value.Value);
}

internal readonly record struct WlTouch(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlTouch Null => new(0);

    public static implicit operator WlProxy(WlTouch value) => new(value.Value);
}

internal readonly record struct WlOutput(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlOutput Null => new(0);

    public static implicit operator WlProxy(WlOutput value) => new(value.Value);
}

internal readonly record struct WlShm(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlShm Null => new(0);

    public static implicit operator WlProxy(WlShm value) => new(value.Value);
}

internal readonly record struct WlBuffer(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlBuffer Null => new(0);

    public static implicit operator WlProxy(WlBuffer value) => new(value.Value);
}

internal readonly record struct WlShmPool(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlShmPool Null => new(0);

    public static implicit operator WlProxy(WlShmPool value) => new(value.Value);
}

internal readonly record struct WlRegion(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlRegion Null => new(0);

    public static implicit operator WlProxy(WlRegion value) => new(value.Value);
}

internal readonly record struct WlSubcompositor(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlSubcompositor Null => new(0);

    public static implicit operator WlProxy(WlSubcompositor value) => new(value.Value);
}

internal readonly record struct WlSubsurface(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlSubsurface Null => new(0);

    public static implicit operator WlProxy(WlSubsurface value) => new(value.Value);
}

internal readonly record struct WlDataOffer(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlDataOffer Null => new(0);

    public static implicit operator WlProxy(WlDataOffer value) => new(value.Value);
}

internal readonly record struct WlDataSource(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlDataSource Null => new(0);

    public static implicit operator WlProxy(WlDataSource value) => new(value.Value);
}

internal readonly record struct WlDataDevice(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlDataDevice Null => new(0);

    public static implicit operator WlProxy(WlDataDevice value) => new(value.Value);
}

internal readonly record struct WlDataDeviceManager(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlDataDeviceManager Null => new(0);

    public static implicit operator WlProxy(WlDataDeviceManager value) => new(value.Value);
}

internal readonly record struct WlCursorTheme(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlCursorTheme Null => new(0);
}

internal readonly record struct WlCursor(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlCursor Null => new(0);
}

internal readonly record struct WlCursorImage(nint Value)
{
    public bool IsNull => Value == 0;

    public static WlCursorImage Null => new(0);
}

internal readonly record struct WpViewporter(nint Value)
{
    public bool IsNull => Value == 0;

    public static WpViewporter Null => new(0);

    public static implicit operator WlProxy(WpViewporter value) => new(value.Value);
}

internal readonly record struct WpViewport(nint Value)
{
    public bool IsNull => Value == 0;

    public static WpViewport Null => new(0);

    public static implicit operator WlProxy(WpViewport value) => new(value.Value);
}

internal readonly record struct WpFractionalScaleManagerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static WpFractionalScaleManagerV1 Null => new(0);

    public static implicit operator WlProxy(WpFractionalScaleManagerV1 value) => new(value.Value);
}

internal readonly record struct WpFractionalScaleV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static WpFractionalScaleV1 Null => new(0);

    public static implicit operator WlProxy(WpFractionalScaleV1 value) => new(value.Value);
}

internal readonly record struct WpCursorShapeManagerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static WpCursorShapeManagerV1 Null => new(0);

    public static implicit operator WlProxy(WpCursorShapeManagerV1 value) => new(value.Value);
}

internal readonly record struct WpCursorShapeDeviceV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static WpCursorShapeDeviceV1 Null => new(0);

    public static implicit operator WlProxy(WpCursorShapeDeviceV1 value) => new(value.Value);
}

internal readonly record struct ZwpPointerConstraintsV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpPointerConstraintsV1 Null => new(0);

    public static implicit operator WlProxy(ZwpPointerConstraintsV1 value) => new(value.Value);
}

internal readonly record struct ZwpLockedPointerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpLockedPointerV1 Null => new(0);

    public static implicit operator WlProxy(ZwpLockedPointerV1 value) => new(value.Value);
}

internal readonly record struct ZwpConfinedPointerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpConfinedPointerV1 Null => new(0);

    public static implicit operator WlProxy(ZwpConfinedPointerV1 value) => new(value.Value);
}

internal readonly record struct ZwpTextInputManagerV3(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTextInputManagerV3 Null => new(0);

    public static implicit operator WlProxy(ZwpTextInputManagerV3 value) => new(value.Value);
}

internal readonly record struct ZwpTextInputV3(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTextInputV3 Null => new(0);

    public static implicit operator WlProxy(ZwpTextInputV3 value) => new(value.Value);
}

internal readonly record struct ZwpPointerGesturesV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpPointerGesturesV1 Null => new(0);

    public static implicit operator WlProxy(ZwpPointerGesturesV1 value) => new(value.Value);
}

internal readonly record struct ZwpPointerGesturePinchV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpPointerGesturePinchV1 Null => new(0);

    public static implicit operator WlProxy(ZwpPointerGesturePinchV1 value) => new(value.Value);
}

internal readonly record struct ZwpPointerGestureHoldV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpPointerGestureHoldV1 Null => new(0);

    public static implicit operator WlProxy(ZwpPointerGestureHoldV1 value) => new(value.Value);
}

internal readonly record struct ZwpRelativePointerManagerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpRelativePointerManagerV1 Null => new(0);

    public static implicit operator WlProxy(ZwpRelativePointerManagerV1 value) => new(value.Value);
}

internal readonly record struct ZwpRelativePointerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpRelativePointerV1 Null => new(0);

    public static implicit operator WlProxy(ZwpRelativePointerV1 value) => new(value.Value);
}

internal readonly record struct ZwpTabletManagerV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletManagerV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletManagerV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletSeatV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletSeatV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletSeatV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletToolV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletToolV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletToolV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletPadV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletPadV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletPadV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletPadGroupV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletPadGroupV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletPadGroupV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletPadRingV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletPadRingV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletPadRingV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletPadStripV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletPadStripV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletPadStripV2 value) => new(value.Value);
}

internal readonly record struct ZwpTabletPadDialV2(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZwpTabletPadDialV2 Null => new(0);

    public static implicit operator WlProxy(ZwpTabletPadDialV2 value) => new(value.Value);
}

internal readonly record struct ExtBackgroundEffectManagerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ExtBackgroundEffectManagerV1 Null => new(0);

    public static implicit operator WlProxy(ExtBackgroundEffectManagerV1 value) => new(value.Value);
}

internal readonly record struct ExtBackgroundEffectSurfaceV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ExtBackgroundEffectSurfaceV1 Null => new(0);

    public static implicit operator WlProxy(ExtBackgroundEffectSurfaceV1 value) => new(value.Value);
}

internal readonly record struct OrgKdeKwinBlurManager(nint Value)
{
    public bool IsNull => Value == 0;

    public static OrgKdeKwinBlurManager Null => new(0);

    public static implicit operator WlProxy(OrgKdeKwinBlurManager value) => new(value.Value);
}

internal readonly record struct OrgKdeKwinBlur(nint Value)
{
    public bool IsNull => Value == 0;

    public static OrgKdeKwinBlur Null => new(0);

    public static implicit operator WlProxy(OrgKdeKwinBlur value) => new(value.Value);
}

internal readonly record struct XdgWmBase(nint Value)
{
    public bool IsNull => Value == 0;

    public static XdgWmBase Null => new(0);

    public static implicit operator WlProxy(XdgWmBase value) => new(value.Value);
}

internal readonly record struct XdgSurface(nint Value)
{
    public bool IsNull => Value == 0;

    public static XdgSurface Null => new(0);

    public static implicit operator WlProxy(XdgSurface value) => new(value.Value);
}

internal readonly record struct XdgToplevel(nint Value)
{
    public bool IsNull => Value == 0;

    public static XdgToplevel Null => new(0);

    public static implicit operator WlProxy(XdgToplevel value) => new(value.Value);
}

internal readonly record struct XdgActivationV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static XdgActivationV1 Null => new(0);

    public static implicit operator WlProxy(XdgActivationV1 value) => new(value.Value);
}

internal readonly record struct XdgActivationTokenV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static XdgActivationTokenV1 Null => new(0);

    public static implicit operator WlProxy(XdgActivationTokenV1 value) => new(value.Value);
}

internal readonly record struct XdgToplevelIconManagerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static XdgToplevelIconManagerV1 Null => new(0);

    public static implicit operator WlProxy(XdgToplevelIconManagerV1 value) => new(value.Value);
}

internal readonly record struct XdgToplevelIconV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static XdgToplevelIconV1 Null => new(0);

    public static implicit operator WlProxy(XdgToplevelIconV1 value) => new(value.Value);
}

internal readonly record struct ZxdgDecorationManagerV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZxdgDecorationManagerV1 Null => new(0);

    public static implicit operator WlProxy(ZxdgDecorationManagerV1 value) => new(value.Value);
}

internal readonly record struct ZxdgToplevelDecorationV1(nint Value)
{
    public bool IsNull => Value == 0;

    public static ZxdgToplevelDecorationV1 Null => new(0);

    public static implicit operator WlProxy(ZxdgToplevelDecorationV1 value) => new(value.Value);
}

internal readonly record struct WlFixed(int Value)
{
    public double ToDouble() => Value / 256.0;

    public static WlFixed FromDouble(double value) => new(checked((int)Math.Round(value * 256.0)));
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlInterface
{
    public sbyte* Name;
    public int Version;
    public int MethodCount;
    public WlMessage* Methods;
    public int EventCount;
    public WlMessage* Events;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlMessage
{
    public sbyte* Name;
    public sbyte* Signature;
    public WlInterface** Types;
}

[StructLayout(LayoutKind.Explicit)]
internal unsafe struct WlArgument
{
    [FieldOffset(0)] public int Int;
    [FieldOffset(0)] public uint Uint;
    [FieldOffset(0)] public int Fixed;
    [FieldOffset(0)] public sbyte* String;
    [FieldOffset(0)] public nint Object;
    [FieldOffset(0)] public uint NewId;
    [FieldOffset(0)] public WlArray* Array;
    [FieldOffset(0)] public int Fd;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlArray
{
    public nuint Size;
    public nuint Alloc;
    public void* Data;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlCursorData
{
    public uint ImageCount;
    public nint* Images;
    public sbyte* Name;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WlCursorImageData
{
    public uint Width;
    public uint Height;
    public uint HotspotX;
    public uint HotspotY;
    public uint Delay;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlList
{
    public WlList* Prev;
    public WlList* Next;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlListener
{
    public WlList Link;
    public delegate* unmanaged[Cdecl]<WlListener*, void*, void> Notify;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PollFd
{
    public int Fd;
    public short Events;
    public short Revents;
}

internal static class WlProxyMarshalFlags
{
    public const uint None = 0;
    public const uint Destroy = 1;
}

internal enum WlDisplayError : uint
{
    InvalidObject = 0,
    InvalidMethod = 1,
    NoMemory = 2,
    Implementation = 3,
}

internal static class WlDisplayRequest
{
    public const uint Sync = 0;
    public const uint GetRegistry = 1;
}

internal static class WlDisplayEvent
{
    public const uint Error = 0;
    public const uint DeleteId = 1;
}

internal static class WlRegistryRequest
{
    public const uint Bind = 0;
}

internal static class WlRegistryEvent
{
    public const uint Global = 0;
    public const uint GlobalRemove = 1;
}

internal static class WlCallbackEvent
{
    public const uint Done = 0;
}

internal static class WlCompositorRequest
{
    public const uint CreateSurface = 0;
    public const uint CreateRegion = 1;
    public const uint Release = 2;
}

internal enum WlCompositorError : uint
{
    BadSurface = 0,
}

internal static class WlSurfaceRequest
{
    public const uint Destroy = 0;
    public const uint Attach = 1;
    public const uint Damage = 2;
    public const uint Frame = 3;
    public const uint SetOpaqueRegion = 4;
    public const uint SetInputRegion = 5;
    public const uint Commit = 6;
    public const uint SetBufferTransform = 7;
    public const uint SetBufferScale = 8;
    public const uint DamageBuffer = 9;
}

internal static class WlSurfaceEvent
{
    public const uint Enter = 0;
    public const uint Leave = 1;
    public const uint PreferredBufferScale = 2;
    public const uint PreferredBufferTransform = 3;
}

internal enum WlSurfaceError : uint
{
    InvalidScale = 0,
    InvalidTransform = 1,
    InvalidSize = 2,
    InvalidOffset = 3,
    DefunctRoleObject = 4,
}

internal static class WlPointerRequest
{
    public const uint SetCursor = 0;
    public const uint Release = 1;
}

internal static class WlPointerEvent
{
    public const uint Enter = 0;
    public const uint Leave = 1;
    public const uint Motion = 2;
    public const uint Button = 3;
    public const uint Axis = 4;
    public const uint Frame = 5;
    public const uint AxisSource = 6;
    public const uint AxisStop = 7;
    public const uint AxisDiscrete = 8;
    public const uint AxisValue120 = 9;
    public const uint AxisRelativeDirection = 10;
}

internal enum WlPointerAxis : uint
{
    VerticalScroll = 0,
    HorizontalScroll = 1,
}

internal enum WlPointerAxisSource : uint
{
    Wheel = 0,
    Finger = 1,
    Continuous = 2,
    WheelTilt = 3,
}

internal enum WlPointerAxisRelativeDirection : uint
{
    Identical = 0,
    Inverted = 1,
}

internal enum WlPointerButtonState : uint
{
    Released = 0,
    Pressed = 1,
}

internal static class WlKeyboardRequest
{
    public const uint Release = 0;
}

internal static class WlKeyboardEvent
{
    public const uint Keymap = 0;
    public const uint Enter = 1;
    public const uint Leave = 2;
    public const uint Key = 3;
    public const uint Modifiers = 4;
    public const uint RepeatInfo = 5;
}

internal enum WlKeyboardKeymapFormat : uint
{
    NoKeymap = 0,
    XkbV1 = 1,
}

internal enum WlKeyboardKeyState : uint
{
    Released = 0,
    Pressed = 1,
    Repeated = 2,
}

internal static class WlTouchRequest
{
    public const uint Release = 0;
}

internal static class WlTouchEvent
{
    public const uint Down = 0;
    public const uint Up = 1;
    public const uint Motion = 2;
    public const uint Frame = 3;
    public const uint Cancel = 4;
    public const uint Shape = 5;
    public const uint Orientation = 6;
}

internal static class WlSeatRequest
{
    public const uint GetPointer = 0;
    public const uint GetKeyboard = 1;
    public const uint GetTouch = 2;
    public const uint Release = 3;
}

internal static class WlSeatEvent
{
    public const uint Capabilities = 0;
    public const uint Name = 1;
}

[Flags]
internal enum WlSeatCapability : uint
{
    Pointer = 1,
    Keyboard = 2,
    Touch = 4,
}

internal static class WlOutputRequest
{
    public const uint Release = 0;
}

internal static class WlOutputEvent
{
    public const uint Geometry = 0;
    public const uint Mode = 1;
    public const uint Done = 2;
    public const uint Scale = 3;
    public const uint Name = 4;
    public const uint Description = 5;
}

[Flags]
internal enum WlOutputModeFlags : uint
{
    Current = 1,
    Preferred = 2,
}

internal enum WlOutputSubpixel
{
    Unknown = 0,
    None = 1,
    HorizontalRgb = 2,
    HorizontalBgr = 3,
    VerticalRgb = 4,
    VerticalBgr = 5,
}

internal enum WlOutputTransform
{
    Normal = 0,
    Rotate90 = 1,
    Rotate180 = 2,
    Rotate270 = 3,
    Flipped = 4,
    Flipped90 = 5,
    Flipped180 = 6,
    Flipped270 = 7,
}

internal static class WlShmRequest
{
    public const uint CreatePool = 0;
    public const uint Release = 1;
}

internal static class WlShmEvent
{
    public const uint Format = 0;
}

internal enum WlShmError : uint
{
    InvalidFormat = 0,
    InvalidStride = 1,
    InvalidFd = 2,
}

internal enum WlShmFormat : uint
{
    Argb8888 = 0,
    Xrgb8888 = 1,
    C8 = 0x20203843,
    Rgb332 = 0x38424752,
    Bgr233 = 0x38524742,
    Xrgb4444 = 0x32315258,
    Xbgr4444 = 0x32314258,
    Rgbx4444 = 0x32315852,
    Bgrx4444 = 0x32315842,
    Argb4444 = 0x32315241,
    Abgr4444 = 0x32314241,
    Rgba4444 = 0x32314152,
    Bgra4444 = 0x32314142,
    Xrgb1555 = 0x35315258,
    Xbgr1555 = 0x35314258,
    Rgbx5551 = 0x35315852,
    Bgrx5551 = 0x35315842,
    Argb1555 = 0x35315241,
    Abgr1555 = 0x35314241,
    Rgba5551 = 0x35314152,
    Bgra5551 = 0x35314142,
    Rgb565 = 0x36314752,
    Bgr565 = 0x36314742,
    Rgb888 = 0x34324752,
    Bgr888 = 0x34324742,
    Xbgr8888 = 0x34324258,
    Rgbx8888 = 0x34325852,
    Bgrx8888 = 0x34325842,
    Abgr8888 = 0x34324241,
    Rgba8888 = 0x34324152,
    Bgra8888 = 0x34324142,
    Xrgb2101010 = 0x30335258,
    Xbgr2101010 = 0x30334258,
    Rgbx1010102 = 0x30335852,
    Bgrx1010102 = 0x30335842,
    Argb2101010 = 0x30335241,
    Abgr2101010 = 0x30334241,
    Rgba1010102 = 0x30334152,
    Bgra1010102 = 0x30334142,
    Yuyv = 0x56595559,
    Yvyu = 0x55595659,
    Uyvy = 0x59565955,
    Vyuy = 0x59555956,
    Ayuv = 0x56555941,
    Nv12 = 0x3231564e,
    Nv21 = 0x3132564e,
    Nv16 = 0x3631564e,
    Nv61 = 0x3136564e,
    Yuv410 = 0x39565559,
    Yvu410 = 0x39555659,
    Yuv411 = 0x31315559,
    Yvu411 = 0x31315659,
    Yuv420 = 0x32315559,
    Yvu420 = 0x32315659,
    Yuv422 = 0x36315559,
    Yvu422 = 0x36315659,
    Yuv444 = 0x34325559,
    Yvu444 = 0x34325659,
}

internal static class WlShmPoolRequest
{
    public const uint CreateBuffer = 0;
    public const uint Destroy = 1;
    public const uint Resize = 2;
}

internal static class WlBufferRequest
{
    public const uint Destroy = 0;
}

internal static class WlBufferEvent
{
    public const uint Release = 0;
}

internal static class WlRegionRequest
{
    public const uint Destroy = 0;
    public const uint Add = 1;
    public const uint Subtract = 2;
}

internal static class WlSubcompositorRequest
{
    public const uint Destroy = 0;
    public const uint GetSubsurface = 1;
}

internal static class WlSubsurfaceRequest
{
    public const uint Destroy = 0;
    public const uint SetPosition = 1;
    public const uint PlaceAbove = 2;
    public const uint PlaceBelow = 3;
    public const uint SetSync = 4;
    public const uint SetDesync = 5;
}

internal static class WlDataOfferRequest
{
    public const uint Accept = 0;
    public const uint Receive = 1;
    public const uint Destroy = 2;
    public const uint Finish = 3;
    public const uint SetActions = 4;
}

internal static class WlDataOfferEvent
{
    public const uint Offer = 0;
    public const uint SourceActions = 1;
    public const uint Action = 2;
}

internal static class WlDataSourceRequest
{
    public const uint Offer = 0;
    public const uint Destroy = 1;
    public const uint SetActions = 2;
}

internal static class WlDataSourceEvent
{
    public const uint Target = 0;
    public const uint Send = 1;
    public const uint Cancelled = 2;
    public const uint DndDropPerformed = 3;
    public const uint DndFinished = 4;
    public const uint Action = 5;
}

internal static class WlDataDeviceRequest
{
    public const uint StartDrag = 0;
    public const uint SetSelection = 1;
    public const uint Release = 2;
}

internal static class WlDataDeviceEvent
{
    public const uint DataOffer = 0;
    public const uint Enter = 1;
    public const uint Leave = 2;
    public const uint Motion = 3;
    public const uint Drop = 4;
    public const uint Selection = 5;
}

internal static class WlDataDeviceManagerRequest
{
    public const uint CreateDataSource = 0;
    public const uint GetDataDevice = 1;
}

[Flags]
internal enum WlDataDeviceManagerDndAction : uint
{
    None = 0,
    Copy = 1,
    Move = 2,
    Ask = 4,
}

internal static class WpViewporterRequest
{
    public const uint Destroy = 0;
    public const uint GetViewport = 1;
}

internal static class WpViewportRequest
{
    public const uint Destroy = 0;
    public const uint SetSource = 1;
    public const uint SetDestination = 2;
}

internal static class WpFractionalScaleManagerV1Request
{
    public const uint Destroy = 0;
    public const uint GetFractionalScale = 1;
}

internal static class WpFractionalScaleV1Request
{
    public const uint Destroy = 0;
}

internal static class WpFractionalScaleV1Event
{
    public const uint PreferredScale = 0;
}

internal static class WpCursorShapeManagerV1Request
{
    public const uint Destroy = 0;
    public const uint GetPointer = 1;
}

internal static class WpCursorShapeDeviceV1Request
{
    public const uint Destroy = 0;
    public const uint SetShape = 1;
}

internal enum WpCursorShapeDeviceV1Shape : uint
{
    Default = 1,
    ContextMenu = 2,
    Help = 3,
    Pointer = 4,
    Progress = 5,
    Wait = 6,
    Cell = 7,
    Crosshair = 8,
    Text = 9,
    VerticalText = 10,
    Alias = 11,
    Copy = 12,
    Move = 13,
    NoDrop = 14,
    NotAllowed = 15,
    Grab = 16,
    Grabbing = 17,
    EResize = 18,
    NResize = 19,
    NeResize = 20,
    NwResize = 21,
    SResize = 22,
    SeResize = 23,
    SwResize = 24,
    WResize = 25,
    EwResize = 26,
    NsResize = 27,
    NeswResize = 28,
    NwseResize = 29,
    ColResize = 30,
    RowResize = 31,
    AllScroll = 32,
    ZoomIn = 33,
    ZoomOut = 34,
}

internal static class ZwpPointerConstraintsV1Request
{
    public const uint Destroy = 0;
    public const uint LockPointer = 1;
    public const uint ConfinePointer = 2;
}

internal enum ZwpPointerConstraintsV1Lifetime : uint
{
    OneShot = 1,
    Persistent = 2,
}

internal static class ZwpLockedPointerV1Request
{
    public const uint Destroy = 0;
    public const uint SetCursorPositionHint = 1;
    public const uint SetRegion = 2;
}

internal static class ZwpLockedPointerV1Event
{
    public const uint Locked = 0;
    public const uint Unlocked = 1;
}

internal static class ZwpConfinedPointerV1Request
{
    public const uint Destroy = 0;
    public const uint SetRegion = 1;
}

internal static class ZwpConfinedPointerV1Event
{
    public const uint Confined = 0;
    public const uint Unconfined = 1;
}

internal static class XdgWmBaseRequest
{
    public const uint Destroy = 0;
    public const uint CreatePositioner = 1;
    public const uint GetXdgSurface = 2;
    public const uint Pong = 3;
}

internal static class XdgWmBaseEvent
{
    public const uint Ping = 0;
}

internal static class XdgSurfaceRequest
{
    public const uint Destroy = 0;
    public const uint GetToplevel = 1;
    public const uint GetPopup = 2;
    public const uint SetWindowGeometry = 3;
    public const uint AckConfigure = 4;
}

internal static class XdgSurfaceEvent
{
    public const uint Configure = 0;
}

internal static class XdgToplevelRequest
{
    public const uint Destroy = 0;
    public const uint SetParent = 1;
    public const uint SetTitle = 2;
    public const uint SetAppId = 3;
    public const uint ShowWindowMenu = 4;
    public const uint Move = 5;
    public const uint Resize = 6;
    public const uint SetMaxSize = 7;
    public const uint SetMinSize = 8;
    public const uint SetMaximized = 9;
    public const uint UnsetMaximized = 10;
    public const uint SetFullscreen = 11;
    public const uint UnsetFullscreen = 12;
    public const uint SetMinimized = 13;
}

internal static class XdgToplevelEvent
{
    public const uint Configure = 0;
    public const uint Close = 1;
    public const uint ConfigureBounds = 2;
    public const uint WmCapabilities = 3;
}

internal static class XdgActivationV1Request
{
    public const uint Destroy = 0;
    public const uint GetActivationToken = 1;
    public const uint Activate = 2;
}

internal static class XdgActivationTokenV1Request
{
    public const uint SetSerial = 0;
    public const uint SetAppId = 1;
    public const uint SetSurface = 2;
    public const uint Commit = 3;
    public const uint Destroy = 4;
}

internal static class XdgActivationTokenV1Event
{
    public const uint Done = 0;
}

internal static class XdgToplevelIconManagerV1Request
{
    public const uint Destroy = 0;
    public const uint CreateIcon = 1;
    public const uint SetIcon = 2;
}

internal static class XdgToplevelIconManagerV1Event
{
    public const uint IconSize = 0;
    public const uint Done = 1;
}

internal static class XdgToplevelIconV1Request
{
    public const uint Destroy = 0;
    public const uint SetName = 1;
    public const uint AddBuffer = 2;
}

internal enum XdgToplevelIconV1Error : uint
{
    InvalidBuffer = 1,
    Immutable = 2,
    NoBuffer = 3,
}

internal static class ZxdgDecorationManagerV1Request
{
    public const uint Destroy = 0;
    public const uint GetToplevelDecoration = 1;
}

internal static class ZxdgToplevelDecorationV1Request
{
    public const uint Destroy = 0;
    public const uint SetMode = 1;
    public const uint UnsetMode = 2;
}

internal static class ZxdgToplevelDecorationV1Event
{
    public const uint Configure = 0;
}

internal enum ZxdgToplevelDecorationV1Error : uint
{
    UnconfiguredBuffer = 0,
    AlreadyConstructed = 1,
    Orphaned = 2,
    InvalidMode = 3,
}

internal enum ZxdgToplevelDecorationV1Mode : uint
{
    ClientSide = 1,
    ServerSide = 2,
}

internal static class ZwpTextInputManagerV3Request
{
    public const uint Destroy = 0;
    public const uint GetTextInput = 1;
}

internal static class ZwpTextInputV3Request
{
    public const uint Destroy = 0;
    public const uint Enable = 1;
    public const uint Disable = 2;
    public const uint SetSurroundingText = 3;
    public const uint SetTextChangeCause = 4;
    public const uint SetContentType = 5;
    public const uint SetCursorRectangle = 6;
    public const uint Commit = 7;
}

internal static class ZwpTextInputV3Event
{
    public const uint Enter = 0;
    public const uint Leave = 1;
    public const uint PreeditString = 2;
    public const uint CommitString = 3;
    public const uint DeleteSurroundingText = 4;
    public const uint Done = 5;
}

internal enum ZwpTextInputV3ChangeCause : uint
{
    InputMethod = 0,
    Other = 1,
}

[Flags]
internal enum ZwpTextInputV3ContentHint : uint
{
    None = 0,
    Completion = 1,
    Spellcheck = 2,
    AutoCapitalization = 4,
    Lowercase = 8,
    Uppercase = 16,
    Titlecase = 32,
    HiddenText = 64,
    SensitiveData = 128,
    Latin = 256,
    Multiline = 512,
}

internal enum ZwpTextInputV3ContentPurpose : uint
{
    Normal = 0,
    Alpha = 1,
    Digits = 2,
    Number = 3,
    Phone = 4,
    Url = 5,
    Email = 6,
    Name = 7,
    Password = 8,
    Pin = 9,
    Date = 10,
    Time = 11,
    Datetime = 12,
    Terminal = 13,
}

internal static class ZwpPointerGesturesV1Request
{
    public const uint GetSwipeGesture = 0;
    public const uint GetPinchGesture = 1;
    public const uint Release = 2;
    public const uint GetHoldGesture = 3;
}

internal static class ZwpPointerGesturePinchV1Request
{
    public const uint Destroy = 0;
}

internal static class ZwpPointerGesturePinchV1Event
{
    public const uint Begin = 0;
    public const uint Update = 1;
    public const uint End = 2;
}

internal static class ZwpPointerGestureHoldV1Request
{
    public const uint Destroy = 0;
}

internal static class ZwpPointerGestureHoldV1Event
{
    public const uint Begin = 0;
    public const uint End = 1;
}

internal static class ZwpRelativePointerManagerV1Request
{
    public const uint Destroy = 0;
    public const uint GetRelativePointer = 1;
}

internal static class ZwpRelativePointerV1Request
{
    public const uint Destroy = 0;
}

internal static class ZwpRelativePointerV1Event
{
    public const uint RelativeMotion = 0;
}

internal static class ZwpTabletManagerV2Request
{
    public const uint GetTabletSeat = 0;
    public const uint Destroy = 1;
}

internal static class ZwpTabletSeatV2Request
{
    public const uint Destroy = 0;
}

internal static class ZwpTabletSeatV2Event
{
    public const uint TabletAdded = 0;
    public const uint ToolAdded = 1;
    public const uint PadAdded = 2;
}

internal static class ZwpTabletToolV2Request
{
    public const uint SetCursor = 0;
    public const uint Destroy = 1;
}

internal static class ZwpTabletToolV2Event
{
    public const uint Type = 0;
    public const uint HardwareSerial = 1;
    public const uint HardwareIdWacom = 2;
    public const uint Capability = 3;
    public const uint Done = 4;
    public const uint Removed = 5;
    public const uint ProximityIn = 6;
    public const uint ProximityOut = 7;
    public const uint Down = 8;
    public const uint Up = 9;
    public const uint Motion = 10;
    public const uint Pressure = 11;
    public const uint Distance = 12;
    public const uint Tilt = 13;
    public const uint Rotation = 14;
    public const uint Slider = 15;
    public const uint Wheel = 16;
    public const uint Button = 17;
    public const uint Frame = 18;
}

internal enum ZwpTabletToolV2Type : uint
{
    Pen = 0x140,
    Eraser = 0x141,
    Brush = 0x142,
    Pencil = 0x143,
    Airbrush = 0x144,
    Finger = 0x145,
    Mouse = 0x146,
    Lens = 0x147,
}

internal enum ZwpTabletToolV2ButtonState : uint
{
    Released = 0,
    Pressed = 1,
}

internal static class ZwpTabletV2Request
{
    public const uint Destroy = 0;
}

internal static class ZwpTabletV2Event
{
    public const uint Name = 0;
    public const uint Id = 1;
    public const uint Path = 2;
    public const uint Done = 3;
    public const uint Removed = 4;
    public const uint Bustype = 5;
}

internal static class ZwpTabletPadRingV2Request
{
    public const uint SetFeedback = 0;
    public const uint Destroy = 1;
}

internal static class ZwpTabletPadRingV2Event
{
    public const uint Source = 0;
    public const uint Angle = 1;
    public const uint Stop = 2;
    public const uint Frame = 3;
}

internal static class ZwpTabletPadStripV2Request
{
    public const uint SetFeedback = 0;
    public const uint Destroy = 1;
}

internal static class ZwpTabletPadStripV2Event
{
    public const uint Source = 0;
    public const uint Position = 1;
    public const uint Stop = 2;
    public const uint Frame = 3;
}

internal static class ZwpTabletPadDialV2Request
{
    public const uint SetFeedback = 0;
    public const uint Destroy = 1;
}

internal static class ZwpTabletPadDialV2Event
{
    public const uint Delta = 0;
    public const uint Frame = 1;
}

internal static class ZwpTabletPadGroupV2Request
{
    public const uint Destroy = 0;
}

internal static class ZwpTabletPadGroupV2Event
{
    public const uint Buttons = 0;
    public const uint Ring = 1;
    public const uint Strip = 2;
    public const uint Modes = 3;
    public const uint Done = 4;
    public const uint ModeSwitch = 5;
    public const uint Dial = 6;
}

internal static class ZwpTabletPadV2Request
{
    public const uint SetFeedback = 0;
    public const uint Destroy = 1;
}

internal static class ZwpTabletPadV2Event
{
    public const uint Group = 0;
    public const uint Path = 1;
    public const uint Buttons = 2;
    public const uint Done = 3;
    public const uint Button = 4;
    public const uint Enter = 5;
    public const uint Leave = 6;
    public const uint Removed = 7;
}

internal static class ExtBackgroundEffectManagerV1Request
{
    public const uint Destroy = 0;
    public const uint GetBackgroundEffect = 1;
}

internal static class ExtBackgroundEffectManagerV1Event
{
    public const uint Capabilities = 0;
}

[Flags]
internal enum ExtBackgroundEffectManagerV1Capability : uint
{
    None = 0,
    Blur = 1,
}

internal static class ExtBackgroundEffectSurfaceV1Request
{
    public const uint Destroy = 0;
    public const uint SetBlurRegion = 1;
}

internal static class OrgKdeKwinBlurManagerRequest
{
    public const uint Create = 0;
    public const uint Unset = 1;
}

internal static class OrgKdeKwinBlurRequest
{
    public const uint Commit = 0;
    public const uint SetRegion = 1;
    public const uint Release = 2;
}

internal enum XdgToplevelResizeEdge : uint
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    TopLeft = 5,
    BottomLeft = 6,
    Right = 8,
    TopRight = 9,
    BottomRight = 10,
}

internal enum XdgToplevelState : uint
{
    Maximized = 1,
    Fullscreen = 2,
    Resizing = 3,
    Activated = 4,
    TiledLeft = 5,
    TiledRight = 6,
    TiledTop = 7,
    TiledBottom = 8,
    Suspended = 9,
    ConstrainedLeft = 10,
    ConstrainedRight = 11,
    ConstrainedTop = 12,
    ConstrainedBottom = 13,
}

internal enum XdgToplevelWmCapability : uint
{
    WindowMenu = 1,
    Maximize = 2,
    Fullscreen = 3,
    Minimize = 4,
}

internal static class PollEvents
{
    public const short In = 0x0001;
    public const short Pri = 0x0002;
    public const short Out = 0x0004;
    public const short Err = 0x0008;
    public const short Hup = 0x0010;
    public const short NVal = 0x0020;
}

internal static class EventFdFlags
{
    public const int Semaphore = 0x000001;
    public const int NonBlock = 0x000800;
    public const int CloExec = 0x080000;
}

internal static class MemFdFlags
{
    public const uint CloExec = 0x0001;
    public const uint AllowSealing = 0x0002;
}

internal static class MmapProtection
{
    public const int Read = 0x1;
    public const int Write = 0x2;
    public const int Execute = 0x4;
}

internal static class MmapFlags
{
    public const int Shared = 0x01;
    public const int Private = 0x02;
    public const int Anonymous = 0x20;

    public static nint Failed => -1;
}

internal static unsafe class WlCoreInterfaces
{
    private static readonly nint WaylandClientLibrary = NativeLibrary.Load(PInvoke.WaylandClient);

    public static WlInterface* Display => Get("wl_display_interface");
    public static WlInterface* Registry => Get("wl_registry_interface");
    public static WlInterface* Callback => Get("wl_callback_interface");
    public static WlInterface* Compositor => Get("wl_compositor_interface");
    public static WlInterface* Surface => Get("wl_surface_interface");
    public static WlInterface* Region => Get("wl_region_interface");
    public static WlInterface* Shm => Get("wl_shm_interface");
    public static WlInterface* ShmPool => Get("wl_shm_pool_interface");
    public static WlInterface* Buffer => Get("wl_buffer_interface");
    public static WlInterface* Seat => Get("wl_seat_interface");
    public static WlInterface* Pointer => Get("wl_pointer_interface");
    public static WlInterface* Keyboard => Get("wl_keyboard_interface");
    public static WlInterface* Touch => Get("wl_touch_interface");
    public static WlInterface* Output => Get("wl_output_interface");
    public static WlInterface* Subcompositor => Get("wl_subcompositor_interface");
    public static WlInterface* Subsurface => Get("wl_subsurface_interface");
    public static WlInterface* DataOffer => Get("wl_data_offer_interface");
    public static WlInterface* DataSource => Get("wl_data_source_interface");
    public static WlInterface* DataDevice => Get("wl_data_device_interface");
    public static WlInterface* DataDeviceManager => Get("wl_data_device_manager_interface");

    private static WlInterface* Get(string symbol) => (WlInterface*)NativeLibrary.GetExport(WaylandClientLibrary, symbol);
}

internal static unsafe class XdgInterfaces
{
    public static readonly WlInterface* WmBase = CreateInterface(
        "xdg_wm_base",
        version: 7,
        [
            ("destroy", string.Empty),
            ("create_positioner", "n"),
            ("get_xdg_surface", "no"),
            ("pong", "u"),
        ],
        [
            ("ping", "u"),
        ]);

    public static readonly WlInterface* Surface = CreateInterface(
        "xdg_surface",
        version: 7,
        [
            ("destroy", string.Empty),
            ("get_toplevel", "n"),
            ("get_popup", "noo"),
            ("set_window_geometry", "iiii"),
            ("ack_configure", "u"),
        ],
        [
            ("configure", "u"),
        ]);

    public static readonly WlInterface* Toplevel = CreateInterface(
        "xdg_toplevel",
        version: 7,
        [
            ("destroy", string.Empty),
            ("set_parent", "?o"),
            ("set_title", "s"),
            ("set_app_id", "s"),
            ("show_window_menu", "ouii"),
            ("move", "ou"),
            ("resize", "ouu"),
            ("set_max_size", "ii"),
            ("set_min_size", "ii"),
            ("set_maximized", string.Empty),
            ("unset_maximized", string.Empty),
            ("set_fullscreen", "?o"),
            ("unset_fullscreen", string.Empty),
            ("set_minimized", string.Empty),
        ],
        [
            ("configure", "iia"),
            ("close", string.Empty),
            ("configure_bounds", "ii"),
            ("wm_capabilities", "a"),
        ]);

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal static unsafe class XdgActivationInterfaces
{
    public static readonly WlInterface* ActivationV1 = CreateInterface(
        "xdg_activation_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("get_activation_token", "n"),
            ("activate", "so"),
        ],
        []);

    public static readonly WlInterface* ActivationTokenV1 = CreateInterface(
        "xdg_activation_token_v1",
        version: 1,
        [
            ("set_serial", "uo"),
            ("set_app_id", "s"),
            ("set_surface", "o"),
            ("commit", string.Empty),
            ("destroy", string.Empty),
        ],
        [
            ("done", "s"),
        ]);

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal static unsafe class XdgToplevelIconInterfaces
{
    public static readonly WlInterface* ManagerV1 = CreateInterface(
        "xdg_toplevel_icon_manager_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("create_icon", "n"),
            ("set_icon", "o?o"),
        ],
        [
            ("icon_size", "i"),
            ("done", string.Empty),
        ]);

    public static readonly WlInterface* IconV1 = CreateInterface(
        "xdg_toplevel_icon_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("set_name", "s"),
            ("add_buffer", "oi"),
        ],
        []);

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal static unsafe class ZxdgDecorationInterfaces
{
    public static readonly WlInterface* ManagerV1 = CreateInterface(
        "zxdg_decoration_manager_v1",
        version: 2,
        [
            ("destroy", string.Empty),
            ("get_toplevel_decoration", "no"),
        ],
        []);

    public static readonly WlInterface* ToplevelDecorationV1 = CreateInterface(
        "zxdg_toplevel_decoration_v1",
        version: 2,
        [
            ("destroy", string.Empty),
            ("set_mode", "u"),
            ("unset_mode", string.Empty),
        ],
        [
            ("configure", "u"),
        ]);

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal static unsafe class WpInterfaces
{
    public static readonly WlInterface* Viewporter = CreateInterface(
        "wp_viewporter",
        version: 1,
        [
            ("destroy", string.Empty),
            ("get_viewport", "no"),
        ],
        []);

    public static readonly WlInterface* Viewport = CreateInterface(
        "wp_viewport",
        version: 1,
        [
            ("destroy", string.Empty),
            ("set_source", "ffff"),
            ("set_destination", "ii"),
        ],
        []);

    public static readonly WlInterface* FractionalScaleManagerV1 = CreateInterface(
        "wp_fractional_scale_manager_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("get_fractional_scale", "no"),
        ],
        []);

    public static readonly WlInterface* FractionalScaleV1 = CreateInterface(
        "wp_fractional_scale_v1",
        version: 1,
        [
            ("destroy", string.Empty),
        ],
        [
            ("preferred_scale", "u"),
        ]);

    public static readonly WlInterface* CursorShapeManagerV1 = CreateInterface(
        "wp_cursor_shape_manager_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("get_pointer", "no"),
        ],
        []);

    public static readonly WlInterface* CursorShapeDeviceV1 = CreateInterface(
        "wp_cursor_shape_device_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("set_shape", "uu"),
        ],
        []);

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal static unsafe class ZwpInterfaces
{
    public static readonly WlInterface* PointerConstraintsV1 = CreateInterface(
        "zwp_pointer_constraints_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("lock_pointer", "noo?ou"),
            ("confine_pointer", "noo?ou"),
        ],
        []);

    public static readonly WlInterface* LockedPointerV1 = CreateInterface(
        "zwp_locked_pointer_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("set_cursor_position_hint", "ff"),
            ("set_region", "?o"),
        ],
        [
            ("locked", string.Empty),
            ("unlocked", string.Empty),
        ]);

    public static readonly WlInterface* ConfinedPointerV1 = CreateInterface(
        "zwp_confined_pointer_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("set_region", "?o"),
        ],
        [
            ("confined", string.Empty),
            ("unconfined", string.Empty),
        ]);

    public static readonly WlInterface* TextInputManagerV3 = CreateInterface(
        "zwp_text_input_manager_v3",
        version: 1,
        [
            ("destroy", string.Empty),
            ("get_text_input", "no"),
        ],
        []);

    public static readonly WlInterface* TextInputV3 = CreateInterface(
        "zwp_text_input_v3",
        version: 1,
        [
            ("destroy", string.Empty),
            ("enable", string.Empty),
            ("disable", string.Empty),
            ("set_surrounding_text", "sii"),
            ("set_text_change_cause", "u"),
            ("set_content_type", "uu"),
            ("set_cursor_rectangle", "iiii"),
            ("commit", string.Empty),
        ],
        [
            ("enter", "o"),
            ("leave", "o"),
            ("preedit_string", "?sii"),
            ("commit_string", "s"),
            ("delete_surrounding_text", "uu"),
            ("done", "u"),
        ]);

    public static readonly WlInterface* PointerGesturesV1 = CreateInterface(
        "zwp_pointer_gestures_v1",
        version: 3,
        [
            ("get_swipe_gesture", "no"),
            ("get_pinch_gesture", "no"),
            ("release", string.Empty),
            ("get_hold_gesture", "no"),
        ],
        []);

    public static readonly WlInterface* PointerGesturePinchV1 = CreateInterface(
        "zwp_pointer_gesture_pinch_v1",
        version: 3,
        [
            ("destroy", string.Empty),
        ],
        [
            ("begin", "uuou"),
            ("update", "uffff"),
            ("end", "uui"),
        ]);

    public static readonly WlInterface* PointerGestureHoldV1 = CreateInterface(
        "zwp_pointer_gesture_hold_v1",
        version: 3,
        [
            ("destroy", string.Empty),
        ],
        [
            ("begin", "uuou"),
            ("end", "uui"),
        ]);

    public static readonly WlInterface* RelativePointerManagerV1 = CreateInterface(
        "zwp_relative_pointer_manager_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("get_relative_pointer", "no"),
        ],
        []);

    public static readonly WlInterface* RelativePointerV1 = CreateInterface(
        "zwp_relative_pointer_v1",
        version: 1,
        [
            ("destroy", string.Empty),
        ],
        [
            ("relative_motion", "uuffff"),
        ]);

    public static readonly WlInterface* TabletManagerV2;
    public static readonly WlInterface* TabletSeatV2;
    public static readonly WlInterface* TabletToolV2;
    public static readonly WlInterface* TabletV2;
    public static readonly WlInterface* TabletPadV2;
    public static readonly WlInterface* TabletPadGroupV2;
    public static readonly WlInterface* TabletPadRingV2;
    public static readonly WlInterface* TabletPadStripV2;
    public static readonly WlInterface* TabletPadDialV2;

    static ZwpInterfaces()
    {
        TabletV2 = CreateInterface(
            "zwp_tablet_v2",
            version: 2,
            [
                ("destroy", string.Empty, null),
            ],
            [
                ("name", "s", null),
                ("id", "uu", null),
                ("path", "s", null),
                ("done", string.Empty, null),
                ("removed", string.Empty, null),
                ("bustype", "u", null),
            ]);

        TabletPadRingV2 = CreateInterface(
            "zwp_tablet_pad_ring_v2",
            version: 2,
            [
                ("set_feedback", "su", null),
                ("destroy", string.Empty, null),
            ],
            [
                ("source", "u", null),
                ("angle", "f", null),
                ("stop", string.Empty, null),
                ("frame", "u", null),
            ]);

        TabletPadStripV2 = CreateInterface(
            "zwp_tablet_pad_strip_v2",
            version: 2,
            [
                ("set_feedback", "su", null),
                ("destroy", string.Empty, null),
            ],
            [
                ("source", "u", null),
                ("position", "u", null),
                ("stop", string.Empty, null),
                ("frame", "u", null),
            ]);

        TabletPadDialV2 = CreateInterface(
            "zwp_tablet_pad_dial_v2",
            version: 2,
            [
                ("set_feedback", "su", null),
                ("destroy", string.Empty, null),
            ],
            [
                ("delta", "i", null),
                ("frame", "u", null),
            ]);

        TabletPadGroupV2 = CreateInterface(
            "zwp_tablet_pad_group_v2",
            version: 2,
            [
                ("destroy", string.Empty, null),
            ],
            [
                ("buttons", "a", null),
                ("ring", "n", new WlInterface*[] { TabletPadRingV2 }),
                ("strip", "n", new WlInterface*[] { TabletPadStripV2 }),
                ("modes", "u", null),
                ("done", string.Empty, null),
                ("mode_switch", "uuu", null),
                ("dial", "n", new WlInterface*[] { TabletPadDialV2 }),
            ]);

        TabletPadV2 = CreateInterface(
            "zwp_tablet_pad_v2",
            version: 2,
            [
                ("set_feedback", "usu", null),
                ("destroy", string.Empty, null),
            ],
            [
                ("group", "n", new WlInterface*[] { TabletPadGroupV2 }),
                ("path", "s", null),
                ("buttons", "u", null),
                ("done", string.Empty, null),
                ("button", "uuu", null),
                ("enter", "uoo", new WlInterface*[] { null, TabletV2, WlCoreInterfaces.Surface }),
                ("leave", "uo", new WlInterface*[] { null, WlCoreInterfaces.Surface }),
                ("removed", string.Empty, null),
            ]);

        TabletToolV2 = CreateInterface(
            "zwp_tablet_tool_v2",
            version: 2,
            [
                ("set_cursor", "u?oii", new WlInterface*[] { null, WlCoreInterfaces.Surface, null, null }),
                ("destroy", string.Empty, null),
            ],
            [
                ("type", "u", null),
                ("hardware_serial", "uu", null),
                ("hardware_id_wacom", "uu", null),
                ("capability", "u", null),
                ("done", string.Empty, null),
                ("removed", string.Empty, null),
                ("proximity_in", "uoo", new WlInterface*[] { null, TabletV2, WlCoreInterfaces.Surface }),
                ("proximity_out", string.Empty, null),
                ("down", "u", null),
                ("up", string.Empty, null),
                ("motion", "ff", null),
                ("pressure", "u", null),
                ("distance", "u", null),
                ("tilt", "ff", null),
                ("rotation", "f", null),
                ("slider", "i", null),
                ("wheel", "fi", null),
                ("button", "uuu", null),
                ("frame", "u", null),
            ]);

        TabletSeatV2 = CreateInterface(
            "zwp_tablet_seat_v2",
            version: 2,
            [
                ("destroy", string.Empty, null),
            ],
            [
                ("tablet_added", "n", new WlInterface*[] { TabletV2 }),
                ("tool_added", "n", new WlInterface*[] { TabletToolV2 }),
                ("pad_added", "n", new WlInterface*[] { TabletPadV2 }),
            ]);

        TabletManagerV2 = CreateInterface(
            "zwp_tablet_manager_v2",
            version: 2,
            [
                ("get_tablet_seat", "no", new WlInterface*[] { TabletSeatV2, WlCoreInterfaces.Seat }),
                ("destroy", string.Empty, null),
            ],
            []);
    }

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature, WlInterface*[]? Types)> methods,
        ReadOnlySpan<(string Name, string Signature, WlInterface*[]? Types)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature, WlInterface*[]? Types)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = CreateTypes(messages[i].Types);
        }

        return result;
    }

    private static WlInterface** CreateTypes(WlInterface*[]? types)
    {
        if (types is null || types.Length == 0)
        {
            return null;
        }

        WlInterface** result = (WlInterface**)NativeMemory.AllocZeroed(
            (nuint)types.Length,
            (nuint)sizeof(WlInterface*));
        for (int i = 0; i < types.Length; i++)
        {
            result[i] = types[i];
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal static unsafe class ExtInterfaces
{
    public static readonly WlInterface* BackgroundEffectManagerV1 = CreateInterface(
        "ext_background_effect_manager_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("get_background_effect", "no"),
        ],
        [
            ("capabilities", "u"),
        ]);

    public static readonly WlInterface* BackgroundEffectSurfaceV1 = CreateInterface(
        "ext_background_effect_surface_v1",
        version: 1,
        [
            ("destroy", string.Empty),
            ("set_blur_region", "?o"),
        ],
        []);

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal static unsafe class OrgKdeInterfaces
{
    public static readonly WlInterface* KwinBlurManager = CreateInterface(
        "org_kde_kwin_blur_manager",
        version: 1,
        [
            ("create", "no"),
            ("unset", "o"),
        ],
        []);

    public static readonly WlInterface* KwinBlur = CreateInterface(
        "org_kde_kwin_blur",
        version: 1,
        [
            ("commit", string.Empty),
            ("set_region", "?o"),
            ("release", string.Empty),
        ],
        []);

    private static WlInterface* CreateInterface(
        string name,
        int version,
        ReadOnlySpan<(string Name, string Signature)> methods,
        ReadOnlySpan<(string Name, string Signature)> events)
    {
        WlInterface* @interface = (WlInterface*)NativeMemory.AllocZeroed(1, (nuint)sizeof(WlInterface));
        @interface->Name = StableUtf8(name);
        @interface->Version = version;
        @interface->MethodCount = methods.Length;
        @interface->Methods = CreateMessages(methods);
        @interface->EventCount = events.Length;
        @interface->Events = CreateMessages(events);
        return @interface;
    }

    private static WlMessage* CreateMessages(ReadOnlySpan<(string Name, string Signature)> messages)
    {
        if (messages.Length == 0)
        {
            return null;
        }

        WlMessage* result = (WlMessage*)NativeMemory.AllocZeroed(
            (nuint)messages.Length,
            (nuint)sizeof(WlMessage));
        for (int i = 0; i < messages.Length; i++)
        {
            result[i].Name = StableUtf8(messages[i].Name);
            result[i].Signature = StableUtf8(messages[i].Signature);
            result[i].Types = null;
        }

        return result;
    }

    private static sbyte* StableUtf8(string value)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
    }
}

internal sealed unsafe class Utf8Buffer : IDisposable
{
    private nint _pointer;

    private Utf8Buffer(nint pointer)
    {
        _pointer = pointer;
    }

    public sbyte* Pointer => (sbyte*)_pointer;

    public static Utf8Buffer FromString(string? value)
    {
        return new Utf8Buffer(value is null ? 0 : Marshal.StringToCoTaskMemUTF8(value));
    }

    public void Dispose()
    {
        nint pointer = _pointer;
        if (pointer == 0)
        {
            return;
        }

        _pointer = 0;
        Marshal.FreeCoTaskMem(pointer);
    }
}

internal static unsafe partial class PInvoke
{
    internal const string WaylandClient = "libwayland-client.so.0";
    private const string WaylandCursor = "libwayland-cursor.so.0";

    private const string Libc = "libc.so.6";

    public static WlDisplay WlDisplayConnect(sbyte* name)
    {
        return new WlDisplay(WlDisplayConnectRaw(name));
    }

    public static WlDisplay WlDisplayConnectToFd(int fd)
    {
        return new WlDisplay(WlDisplayConnectToFdRaw(fd));
    }

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_connect")]
    private static partial nint WlDisplayConnectRaw(sbyte* name);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_connect_to_fd")]
    private static partial nint WlDisplayConnectToFdRaw(int fd);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_disconnect")]
    public static partial void WlDisplayDisconnect(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_get_fd")]
    public static partial int WlDisplayGetFd(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_dispatch")]
    public static partial int WlDisplayDispatch(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_dispatch_pending")]
    public static partial int WlDisplayDispatchPending(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_dispatch_queue")]
    public static partial int WlDisplayDispatchQueue(WlDisplay display, WlEventQueue queue);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_dispatch_queue_pending")]
    public static partial int WlDisplayDispatchQueuePending(WlDisplay display, WlEventQueue queue);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_prepare_read")]
    public static partial int WlDisplayPrepareRead(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_prepare_read_queue")]
    public static partial int WlDisplayPrepareReadQueue(WlDisplay display, WlEventQueue queue);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_cancel_read")]
    public static partial void WlDisplayCancelRead(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_read_events")]
    public static partial int WlDisplayReadEvents(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_flush")]
    public static partial int WlDisplayFlush(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_roundtrip")]
    public static partial int WlDisplayRoundtrip(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_roundtrip_queue")]
    public static partial int WlDisplayRoundtripQueue(WlDisplay display, WlEventQueue queue);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_create_queue")]
    public static partial WlEventQueue WlDisplayCreateQueue(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_get_error")]
    public static partial int WlDisplayGetError(WlDisplay display);

    [LibraryImport(WaylandClient, EntryPoint = "wl_display_get_protocol_error")]
    public static partial uint WlDisplayGetProtocolError(WlDisplay display, WlInterface** interfaceOut, uint* idOut);

    [LibraryImport(WaylandClient, EntryPoint = "wl_event_queue_destroy")]
    public static partial void WlEventQueueDestroy(WlEventQueue queue);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_destroy")]
    public static partial void WlProxyDestroy(WlProxy proxy);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_marshal_array")]
    public static partial void WlProxyMarshalArray(WlProxy proxy, uint opcode, WlArgument* args);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_marshal_array_constructor")]
    public static partial WlProxy WlProxyMarshalArrayConstructor(
        WlProxy proxy,
        uint opcode,
        WlArgument* args,
        WlInterface* @interface);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_marshal_array_flags")]
    public static partial WlProxy WlProxyMarshalArrayFlags(
        WlProxy proxy,
        uint opcode,
        WlInterface* @interface,
        uint version,
        uint flags,
        WlArgument* args);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_create")]
    public static partial WlProxy WlProxyCreate(WlProxy factory, WlInterface* @interface);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_create_wrapper")]
    public static partial WlProxy WlProxyCreateWrapper(WlProxy proxy);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_wrapper_destroy")]
    public static partial void WlProxyWrapperDestroy(WlProxy proxyWrapper);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_add_listener")]
    public static partial int WlProxyAddListener(WlProxy proxy, void** implementation, void* data);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_add_dispatcher")]
    public static partial int WlProxyAddDispatcher(
        WlProxy proxy,
        delegate* unmanaged[Cdecl]<void*, void*, uint, WlMessage*, WlArgument*, int> dispatcher,
        void* implementation,
        void* data);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_get_listener")]
    public static partial void* WlProxyGetListener(WlProxy proxy);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_set_user_data")]
    public static partial void WlProxySetUserData(WlProxy proxy, void* userData);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_get_user_data")]
    public static partial void* WlProxyGetUserData(WlProxy proxy);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_get_version")]
    public static partial uint WlProxyGetVersion(WlProxy proxy);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_get_id")]
    public static partial uint WlProxyGetId(WlProxy proxy);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_get_class")]
    public static partial sbyte* WlProxyGetClass(WlProxy proxy);

    [LibraryImport(WaylandClient, EntryPoint = "wl_proxy_set_queue")]
    public static partial void WlProxySetQueue(WlProxy proxy, WlEventQueue queue);

    [LibraryImport(WaylandCursor, EntryPoint = "wl_cursor_theme_load")]
    public static partial WlCursorTheme WlCursorThemeLoad(sbyte* name, int size, WlShm shm);

    [LibraryImport(WaylandCursor, EntryPoint = "wl_cursor_theme_destroy")]
    public static partial void WlCursorThemeDestroy(WlCursorTheme theme);

    [LibraryImport(WaylandCursor, EntryPoint = "wl_cursor_theme_get_cursor")]
    public static partial WlCursor WlCursorThemeGetCursor(WlCursorTheme theme, sbyte* name);

    [LibraryImport(WaylandCursor, EntryPoint = "wl_cursor_image_get_buffer")]
    public static partial WlBuffer WlCursorImageGetBuffer(WlCursorImage image);

    [LibraryImport(Libc, EntryPoint = "poll")]
    public static partial int Poll(PollFd* fds, nuint nfds, int timeout);

    [LibraryImport(Libc, EntryPoint = "eventfd")]
    public static partial int EventFd(uint initval, int flags);

    [LibraryImport(Libc, EntryPoint = "read", SetLastError = true)]
    public static partial nint Read(int fd, void* buffer, nuint count);

    [LibraryImport(Libc, EntryPoint = "write", SetLastError = true)]
    public static partial nint Write(int fd, void* buffer, nuint count);

    [LibraryImport(Libc, EntryPoint = "close", SetLastError = true)]
    public static partial int Close(int fd);

    [LibraryImport(Libc, EntryPoint = "mmap", SetLastError = true)]
    public static partial void* Mmap(void* addr, nuint length, int prot, int flags, int fd, nint offset);

    [LibraryImport(Libc, EntryPoint = "munmap", SetLastError = true)]
    public static partial int Munmap(void* addr, nuint length);

    [LibraryImport(Libc, EntryPoint = "ftruncate", SetLastError = true)]
    public static partial int Ftruncate(int fd, nint length);

    [LibraryImport(Libc, EntryPoint = "memfd_create", SetLastError = true)]
    public static partial int MemfdCreate(sbyte* name, uint flags);
}
