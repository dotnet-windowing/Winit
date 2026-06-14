using Winit.Dpi;

namespace Winit.Core;

public record struct StartCause
{
    public readonly record struct ResumeTimeReached(Instant Start, Instant RequestedResume);

    public readonly record struct WaitCancelled(Instant Start, Instant? RequestedResume);

    public readonly record struct Poll;

    public readonly record struct Init;

    private const byte ResumeTimeReachedTag = 0;
    private const byte WaitCancelledTag = 1;
    private const byte PollTag = 2;
    private const byte InitTag = 3;

    private byte _tag;
    private ResumeTimeReached _resumeTimeReached;
    private WaitCancelled _waitCancelled;
    private Poll _poll;
    private Init _init;

    public StartCause(ResumeTimeReached value)
    {
        this = default;
        _tag = ResumeTimeReachedTag;
        _resumeTimeReached = value;
    }

    public StartCause(WaitCancelled value)
    {
        this = default;
        _tag = WaitCancelledTag;
        _waitCancelled = value;
    }

    public StartCause(Poll value)
    {
        this = default;
        _tag = PollTag;
        _poll = value;
    }

    public StartCause(Init value)
    {
        this = default;
        _tag = InitTag;
        _init = value;
    }

    public bool TryGetValue(out ResumeTimeReached value)
    {
        value = _resumeTimeReached;
        return _tag == ResumeTimeReachedTag;
    }

    public bool TryGetValue(out WaitCancelled value)
    {
        value = _waitCancelled;
        return _tag == WaitCancelledTag;
    }

    public bool TryGetValue(out Poll value)
    {
        value = _poll;
        return _tag == PollTag;
    }

    public bool TryGetValue(out Init value)
    {
        value = _init;
        return _tag == InitTag;
    }
}

public record struct WindowEvent
{
    public readonly record struct ActivationTokenDone(AsyncRequestSerial Serial, ActivationToken Token);

    public readonly record struct SurfaceResized(PhysicalSize<uint> Size);

    public readonly record struct Moved(PhysicalPosition<int> Position);

    public readonly record struct CloseRequested;

    public readonly record struct Destroyed;

    public readonly record struct DragEntered(IReadOnlyList<string> Paths, PhysicalPosition<double> Position);

    public readonly record struct DragMoved(PhysicalPosition<double> Position);

    public readonly record struct DragDropped(IReadOnlyList<string> Paths, PhysicalPosition<double> Position);

    public readonly record struct DragLeft(PhysicalPosition<double>? Position);

    public readonly record struct Focused(bool IsFocused);

    public readonly record struct KeyboardInput(DeviceId? DeviceId, KeyEvent Event, bool IsSynthetic);

    public readonly record struct ModifiersChanged(Modifiers Modifiers);

    public readonly record struct Ime(global::Winit.Core.Ime Value);

    public readonly record struct PointerMoved(
        DeviceId? DeviceId,
        PhysicalPosition<double> Position,
        bool Primary,
        PointerSource Source);

    public readonly record struct PointerEntered(
        DeviceId? DeviceId,
        PhysicalPosition<double> Position,
        bool Primary,
        PointerKind Kind);

    public readonly record struct PointerLeft(
        DeviceId? DeviceId,
        PhysicalPosition<double>? Position,
        bool Primary,
        PointerKind Kind);

    public readonly record struct MouseWheel(DeviceId? DeviceId, MouseScrollDelta Delta, TouchPhase Phase);

    public readonly record struct PointerButton(
        DeviceId? DeviceId,
        ElementState State,
        PhysicalPosition<double> Position,
        bool Primary,
        ButtonSource Button);

    public readonly record struct HoldGesture(DeviceId? DeviceId, TouchPhase Phase);

    public readonly record struct PinchGesture(DeviceId? DeviceId, double Delta, TouchPhase Phase);

    public readonly record struct PanGesture(DeviceId? DeviceId, PhysicalPosition<float> Delta, TouchPhase Phase);

    public readonly record struct DoubleTapGesture(DeviceId? DeviceId);

    public readonly record struct RotationGesture(DeviceId? DeviceId, float Delta, TouchPhase Phase);

    public readonly record struct TouchpadPressure(DeviceId? DeviceId, float Pressure, long Stage);

    public readonly record struct ScaleFactorChanged(double ScaleFactor, SurfaceSizeWriter SurfaceSizeWriter);

    public readonly record struct ThemeChanged(Theme Theme);

    public readonly record struct Occluded(bool IsOccluded);

    public readonly record struct RedrawRequested;

    private const byte ActivationTokenDoneTag = 0;
    private const byte SurfaceResizedTag = 1;
    private const byte MovedTag = 2;
    private const byte CloseRequestedTag = 3;
    private const byte DestroyedTag = 4;
    private const byte DragEnteredTag = 5;
    private const byte DragMovedTag = 6;
    private const byte DragDroppedTag = 7;
    private const byte DragLeftTag = 8;
    private const byte FocusedTag = 9;
    private const byte KeyboardInputTag = 10;
    private const byte ModifiersChangedTag = 11;
    private const byte ImeTag = 12;
    private const byte PointerMovedTag = 13;
    private const byte PointerEnteredTag = 14;
    private const byte PointerLeftTag = 15;
    private const byte MouseWheelTag = 16;
    private const byte PointerButtonTag = 17;
    private const byte HoldGestureTag = 18;
    private const byte PinchGestureTag = 19;
    private const byte PanGestureTag = 20;
    private const byte DoubleTapGestureTag = 21;
    private const byte RotationGestureTag = 22;
    private const byte TouchpadPressureTag = 23;
    private const byte ScaleFactorChangedTag = 24;
    private const byte ThemeChangedTag = 25;
    private const byte OccludedTag = 26;
    private const byte RedrawRequestedTag = 27;

    private byte _tag;
    private ActivationTokenDone _activationTokenDone;
    private SurfaceResized _surfaceResized;
    private Moved _moved;
    private CloseRequested _closeRequested;
    private Destroyed _destroyed;
    private DragEntered _dragEntered;
    private DragMoved _dragMoved;
    private DragDropped _dragDropped;
    private DragLeft _dragLeft;
    private Focused _focused;
    private KeyboardInput _keyboardInput;
    private ModifiersChanged _modifiersChanged;
    private Ime _ime;
    private PointerMoved _pointerMoved;
    private PointerEntered _pointerEntered;
    private PointerLeft _pointerLeft;
    private MouseWheel _mouseWheel;
    private PointerButton _pointerButton;
    private HoldGesture _holdGesture;
    private PinchGesture _pinchGesture;
    private PanGesture _panGesture;
    private DoubleTapGesture _doubleTapGesture;
    private RotationGesture _rotationGesture;
    private TouchpadPressure _touchpadPressure;
    private ScaleFactorChanged _scaleFactorChanged;
    private ThemeChanged _themeChanged;
    private Occluded _occluded;
    private RedrawRequested _redrawRequested;

    public WindowEvent(ActivationTokenDone value)
    {
        this = default;
        _tag = ActivationTokenDoneTag;
        _activationTokenDone = value;
    }

    public WindowEvent(SurfaceResized value)
    {
        this = default;
        _tag = SurfaceResizedTag;
        _surfaceResized = value;
    }

    public WindowEvent(Moved value)
    {
        this = default;
        _tag = MovedTag;
        _moved = value;
    }

    public WindowEvent(CloseRequested value)
    {
        this = default;
        _tag = CloseRequestedTag;
        _closeRequested = value;
    }

    public WindowEvent(Destroyed value)
    {
        this = default;
        _tag = DestroyedTag;
        _destroyed = value;
    }

    public WindowEvent(DragEntered value)
    {
        this = default;
        _tag = DragEnteredTag;
        _dragEntered = value;
    }

    public WindowEvent(DragMoved value)
    {
        this = default;
        _tag = DragMovedTag;
        _dragMoved = value;
    }

    public WindowEvent(DragDropped value)
    {
        this = default;
        _tag = DragDroppedTag;
        _dragDropped = value;
    }

    public WindowEvent(DragLeft value)
    {
        this = default;
        _tag = DragLeftTag;
        _dragLeft = value;
    }

    public WindowEvent(Focused value)
    {
        this = default;
        _tag = FocusedTag;
        _focused = value;
    }

    public WindowEvent(KeyboardInput value)
    {
        this = default;
        _tag = KeyboardInputTag;
        _keyboardInput = value;
    }

    public WindowEvent(ModifiersChanged value)
    {
        this = default;
        _tag = ModifiersChangedTag;
        _modifiersChanged = value;
    }

    public WindowEvent(Ime value)
    {
        this = default;
        _tag = ImeTag;
        _ime = value;
    }

    public WindowEvent(PointerMoved value)
    {
        this = default;
        _tag = PointerMovedTag;
        _pointerMoved = value;
    }

    public WindowEvent(PointerEntered value)
    {
        this = default;
        _tag = PointerEnteredTag;
        _pointerEntered = value;
    }

    public WindowEvent(PointerLeft value)
    {
        this = default;
        _tag = PointerLeftTag;
        _pointerLeft = value;
    }

    public WindowEvent(MouseWheel value)
    {
        this = default;
        _tag = MouseWheelTag;
        _mouseWheel = value;
    }

    public WindowEvent(PointerButton value)
    {
        this = default;
        _tag = PointerButtonTag;
        _pointerButton = value;
    }

    public WindowEvent(HoldGesture value)
    {
        this = default;
        _tag = HoldGestureTag;
        _holdGesture = value;
    }

    public WindowEvent(PinchGesture value)
    {
        this = default;
        _tag = PinchGestureTag;
        _pinchGesture = value;
    }

    public WindowEvent(PanGesture value)
    {
        this = default;
        _tag = PanGestureTag;
        _panGesture = value;
    }

    public WindowEvent(DoubleTapGesture value)
    {
        this = default;
        _tag = DoubleTapGestureTag;
        _doubleTapGesture = value;
    }

    public WindowEvent(RotationGesture value)
    {
        this = default;
        _tag = RotationGestureTag;
        _rotationGesture = value;
    }

    public WindowEvent(TouchpadPressure value)
    {
        this = default;
        _tag = TouchpadPressureTag;
        _touchpadPressure = value;
    }

    public WindowEvent(ScaleFactorChanged value)
    {
        this = default;
        _tag = ScaleFactorChangedTag;
        _scaleFactorChanged = value;
    }

    public WindowEvent(ThemeChanged value)
    {
        this = default;
        _tag = ThemeChangedTag;
        _themeChanged = value;
    }

    public WindowEvent(Occluded value)
    {
        this = default;
        _tag = OccludedTag;
        _occluded = value;
    }

    public WindowEvent(RedrawRequested value)
    {
        this = default;
        _tag = RedrawRequestedTag;
        _redrawRequested = value;
    }

    public bool TryGetValue(out ActivationTokenDone value) { value = _activationTokenDone; return _tag == ActivationTokenDoneTag; }

    public bool TryGetValue(out SurfaceResized value) { value = _surfaceResized; return _tag == SurfaceResizedTag; }

    public bool TryGetValue(out Moved value) { value = _moved; return _tag == MovedTag; }

    public bool TryGetValue(out CloseRequested value) { value = _closeRequested; return _tag == CloseRequestedTag; }

    public bool TryGetValue(out Destroyed value) { value = _destroyed; return _tag == DestroyedTag; }

    public bool TryGetValue(out DragEntered value) { value = _dragEntered; return _tag == DragEnteredTag; }

    public bool TryGetValue(out DragMoved value) { value = _dragMoved; return _tag == DragMovedTag; }

    public bool TryGetValue(out DragDropped value) { value = _dragDropped; return _tag == DragDroppedTag; }

    public bool TryGetValue(out DragLeft value) { value = _dragLeft; return _tag == DragLeftTag; }

    public bool TryGetValue(out Focused value) { value = _focused; return _tag == FocusedTag; }

    public bool TryGetValue(out KeyboardInput value) { value = _keyboardInput; return _tag == KeyboardInputTag; }

    public bool TryGetValue(out ModifiersChanged value) { value = _modifiersChanged; return _tag == ModifiersChangedTag; }

    public bool TryGetValue(out Ime value) { value = _ime; return _tag == ImeTag; }

    public bool TryGetValue(out PointerMoved value) { value = _pointerMoved; return _tag == PointerMovedTag; }

    public bool TryGetValue(out PointerEntered value) { value = _pointerEntered; return _tag == PointerEnteredTag; }

    public bool TryGetValue(out PointerLeft value) { value = _pointerLeft; return _tag == PointerLeftTag; }

    public bool TryGetValue(out MouseWheel value) { value = _mouseWheel; return _tag == MouseWheelTag; }

    public bool TryGetValue(out PointerButton value) { value = _pointerButton; return _tag == PointerButtonTag; }

    public bool TryGetValue(out HoldGesture value) { value = _holdGesture; return _tag == HoldGestureTag; }

    public bool TryGetValue(out PinchGesture value) { value = _pinchGesture; return _tag == PinchGestureTag; }

    public bool TryGetValue(out PanGesture value) { value = _panGesture; return _tag == PanGestureTag; }

    public bool TryGetValue(out DoubleTapGesture value) { value = _doubleTapGesture; return _tag == DoubleTapGestureTag; }

    public bool TryGetValue(out RotationGesture value) { value = _rotationGesture; return _tag == RotationGestureTag; }

    public bool TryGetValue(out TouchpadPressure value) { value = _touchpadPressure; return _tag == TouchpadPressureTag; }

    public bool TryGetValue(out ScaleFactorChanged value) { value = _scaleFactorChanged; return _tag == ScaleFactorChangedTag; }

    public bool TryGetValue(out ThemeChanged value) { value = _themeChanged; return _tag == ThemeChangedTag; }

    public bool TryGetValue(out Occluded value) { value = _occluded; return _tag == OccludedTag; }

    public bool TryGetValue(out RedrawRequested value) { value = _redrawRequested; return _tag == RedrawRequestedTag; }
}

public record struct PointerKind
{
    public readonly record struct Mouse;

    public readonly record struct Touch(FingerId FingerId);

    public readonly record struct TabletTool(TabletToolKind Kind);

    public readonly record struct Unknown;

    private const byte MouseTag = 0;
    private const byte TouchTag = 1;
    private const byte TabletToolTag = 2;
    private const byte UnknownTag = 3;

    private byte _tag;
    private Mouse _mouse;
    private Touch _touch;
    private TabletTool _tabletTool;
    private Unknown _unknown;

    public PointerKind(Mouse value) { this = default; _tag = MouseTag; _mouse = value; }

    public PointerKind(Touch value) { this = default; _tag = TouchTag; _touch = value; }

    public PointerKind(TabletTool value) { this = default; _tag = TabletToolTag; _tabletTool = value; }

    public PointerKind(Unknown value) { this = default; _tag = UnknownTag; _unknown = value; }

    public static PointerKind From(PointerSource source)
    {
        if (source.TryGetValue(out PointerSource.Touch touch))
        {
            return new PointerKind(new Touch(touch.FingerId));
        }

        if (source.TryGetValue(out PointerSource.TabletTool tabletTool))
        {
            return new PointerKind(new TabletTool(tabletTool.Kind));
        }

        return source.TryGetValue(out PointerSource.Mouse _)
            ? new PointerKind(new Mouse())
            : new PointerKind(new Unknown());
    }

    public bool TryGetValue(out Mouse value) { value = _mouse; return _tag == MouseTag; }

    public bool TryGetValue(out Touch value) { value = _touch; return _tag == TouchTag; }

    public bool TryGetValue(out TabletTool value) { value = _tabletTool; return _tag == TabletToolTag; }

    public bool TryGetValue(out Unknown value) { value = _unknown; return _tag == UnknownTag; }
}

public record struct PointerSource
{
    public readonly record struct Mouse;

    public readonly record struct Touch(FingerId FingerId, Force? Force);

    public readonly record struct TabletTool(TabletToolKind Kind, TabletToolData Data);

    public readonly record struct Unknown;

    private const byte MouseTag = 0;
    private const byte TouchTag = 1;
    private const byte TabletToolTag = 2;
    private const byte UnknownTag = 3;

    private byte _tag;
    private Mouse _mouse;
    private Touch _touch;
    private TabletTool _tabletTool;
    private Unknown _unknown;

    public PointerSource(Mouse value) { this = default; _tag = MouseTag; _mouse = value; }

    public PointerSource(Touch value) { this = default; _tag = TouchTag; _touch = value; }

    public PointerSource(TabletTool value) { this = default; _tag = TabletToolTag; _tabletTool = value; }

    public PointerSource(Unknown value) { this = default; _tag = UnknownTag; _unknown = value; }

    public bool TryGetValue(out Mouse value) { value = _mouse; return _tag == MouseTag; }

    public bool TryGetValue(out Touch value) { value = _touch; return _tag == TouchTag; }

    public bool TryGetValue(out TabletTool value) { value = _tabletTool; return _tag == TabletToolTag; }

    public bool TryGetValue(out Unknown value) { value = _unknown; return _tag == UnknownTag; }
}

public record struct ButtonSource
{
    public readonly record struct Mouse(MouseButton Button);

    public readonly record struct Touch(FingerId FingerId, Force? Force);

    public readonly record struct TabletTool(TabletToolKind Kind, TabletToolButton Button, TabletToolData Data);

    public readonly record struct Unknown(ushort Code);

    private const byte MouseTag = 0;
    private const byte TouchTag = 1;
    private const byte TabletToolTag = 2;
    private const byte UnknownTag = 3;

    private byte _tag;
    private Mouse _mouse;
    private Touch _touch;
    private TabletTool _tabletTool;
    private Unknown _unknown;

    public ButtonSource(Mouse value) { this = default; _tag = MouseTag; _mouse = value; }

    public ButtonSource(Touch value) { this = default; _tag = TouchTag; _touch = value; }

    public ButtonSource(TabletTool value) { this = default; _tag = TabletToolTag; _tabletTool = value; }

    public ButtonSource(Unknown value) { this = default; _tag = UnknownTag; _unknown = value; }

    public MouseButton? MouseButton()
    {
        if (TryGetValue(out Mouse mouse))
        {
            return mouse.Button;
        }

        if (TryGetValue(out Touch _))
        {
            return global::Winit.Core.MouseButton.Left;
        }

        return TryGetValue(out TabletTool tabletTool) ? tabletTool.Button.ToMouseButton() : null;
    }

    public bool TryGetValue(out Mouse value) { value = _mouse; return _tag == MouseTag; }

    public bool TryGetValue(out Touch value) { value = _touch; return _tag == TouchTag; }

    public bool TryGetValue(out TabletTool value) { value = _tabletTool; return _tag == TabletToolTag; }

    public bool TryGetValue(out Unknown value) { value = _unknown; return _tag == UnknownTag; }
}

public readonly record struct DeviceId(long Raw)
{
    public long IntoRaw()
    {
        return Raw;
    }

    public static DeviceId FromRaw(long id)
    {
        return new DeviceId(id);
    }
}

public readonly record struct FingerId(nuint Raw)
{
    public nuint IntoRaw()
    {
        return Raw;
    }

    public static FingerId FromRaw(nuint id)
    {
        return new FingerId(id);
    }
}

public record struct DeviceEvent
{
    public readonly record struct PointerMotion((double X, double Y) Delta);

    public readonly record struct MouseWheel(MouseScrollDelta Delta);

    public readonly record struct Button(uint ButtonId, ElementState State);

    public readonly record struct Key(RawKeyEvent RawKeyEvent);

    private const byte PointerMotionTag = 0;
    private const byte MouseWheelTag = 1;
    private const byte ButtonTag = 2;
    private const byte KeyTag = 3;

    private byte _tag;
    private PointerMotion _pointerMotion;
    private MouseWheel _mouseWheel;
    private Button _button;
    private Key _key;

    public DeviceEvent(PointerMotion value) { this = default; _tag = PointerMotionTag; _pointerMotion = value; }

    public DeviceEvent(MouseWheel value) { this = default; _tag = MouseWheelTag; _mouseWheel = value; }

    public DeviceEvent(Button value) { this = default; _tag = ButtonTag; _button = value; }

    public DeviceEvent(Key value) { this = default; _tag = KeyTag; _key = value; }

    public bool TryGetValue(out PointerMotion value) { value = _pointerMotion; return _tag == PointerMotionTag; }

    public bool TryGetValue(out MouseWheel value) { value = _mouseWheel; return _tag == MouseWheelTag; }

    public bool TryGetValue(out Button value) { value = _button; return _tag == ButtonTag; }

    public bool TryGetValue(out Key value) { value = _key; return _tag == KeyTag; }
}

public readonly record struct RawKeyEvent(PhysicalKey PhysicalKey, ElementState State);

public readonly record struct KeyEvent(
    PhysicalKey PhysicalKey,
    Key LogicalKey,
    string? Text,
    KeyLocation Location,
    ElementState State,
    bool Repeat,
    string? TextWithAllModifiers,
    Key KeyWithoutModifiers);

public readonly record struct Modifiers(ModifiersState State, ModifiersKeys PressedMods)
{
    public static Modifiers From(ModifiersState state)
    {
        return new Modifiers(state, ModifiersKeys.None);
    }

    public ModifiersKeyState LShiftState()
    {
        return ModState(ModifiersKeys.LShift);
    }

    public ModifiersKeyState RShiftState()
    {
        return ModState(ModifiersKeys.RShift);
    }

    public ModifiersKeyState LAltState()
    {
        return ModState(ModifiersKeys.LAlt);
    }

    public ModifiersKeyState RAltState()
    {
        return ModState(ModifiersKeys.RAlt);
    }

    public ModifiersKeyState LControlState()
    {
        return ModState(ModifiersKeys.LControl);
    }

    public ModifiersKeyState RControlState()
    {
        return ModState(ModifiersKeys.RControl);
    }

    public ModifiersKeyState LSuperState()
    {
        return ModState(ModifiersKeys.LMeta);
    }

    public ModifiersKeyState RSuperState()
    {
        return ModState(ModifiersKeys.RMeta);
    }

    private ModifiersKeyState ModState(ModifiersKeys modifier)
    {
        return (PressedMods & modifier) != 0 ? ModifiersKeyState.Pressed : ModifiersKeyState.Unknown;
    }
}

public record struct Ime
{
    public readonly record struct Enabled;

    public readonly record struct Preedit(string Text, (nuint Begin, nuint End)? CursorRange);

    public readonly record struct Commit(string Text);

    public readonly record struct DeleteSurrounding(nuint BeforeBytes, nuint AfterBytes);

    public readonly record struct Disabled;

    private const byte EnabledTag = 0;
    private const byte PreeditTag = 1;
    private const byte CommitTag = 2;
    private const byte DeleteSurroundingTag = 3;
    private const byte DisabledTag = 4;

    private byte _tag;
    private Enabled _enabled;
    private Preedit _preedit;
    private Commit _commit;
    private DeleteSurrounding _deleteSurrounding;
    private Disabled _disabled;

    public Ime(Enabled value) { this = default; _tag = EnabledTag; _enabled = value; }

    public Ime(Preedit value) { this = default; _tag = PreeditTag; _preedit = value; }

    public Ime(Commit value) { this = default; _tag = CommitTag; _commit = value; }

    public Ime(DeleteSurrounding value) { this = default; _tag = DeleteSurroundingTag; _deleteSurrounding = value; }

    public Ime(Disabled value) { this = default; _tag = DisabledTag; _disabled = value; }

    public bool TryGetValue(out Enabled value) { value = _enabled; return _tag == EnabledTag; }

    public bool TryGetValue(out Preedit value) { value = _preedit; return _tag == PreeditTag; }

    public bool TryGetValue(out Commit value) { value = _commit; return _tag == CommitTag; }

    public bool TryGetValue(out DeleteSurrounding value) { value = _deleteSurrounding; return _tag == DeleteSurroundingTag; }

    public bool TryGetValue(out Disabled value) { value = _disabled; return _tag == DisabledTag; }
}

public enum TouchPhase
{
    Started,
    Moved,
    Ended,
    Cancelled,
}

public record struct Force
{
    public readonly record struct Calibrated(double Force, double MaxPossibleForce);

    public readonly record struct Normalized(double Force);

    private const byte CalibratedTag = 0;
    private const byte NormalizedTag = 1;

    private byte _tag;
    private Calibrated _calibrated;
    private Normalized _normalized;

    public Force(Calibrated value) { this = default; _tag = CalibratedTag; _calibrated = value; }

    public Force(Normalized value) { this = default; _tag = NormalizedTag; _normalized = value; }

    public double NormalizedValue(TabletToolAngle? angle = null)
    {
        if (TryGetValue(out Calibrated calibrated))
        {
            double force = angle is { } value ? calibrated.Force / Math.Sin(value.Altitude) : calibrated.Force;
            return force / calibrated.MaxPossibleForce;
        }

        return _normalized.Force;
    }

    public bool TryGetValue(out Calibrated value) { value = _calibrated; return _tag == CalibratedTag; }

    public bool TryGetValue(out Normalized value) { value = _normalized; return _tag == NormalizedTag; }
}

public enum TabletToolKind
{
    Pen,
    Eraser,
    Brush,
    Pencil,
    Airbrush,
    Finger,
    Mouse,
    Lens,
}

public readonly record struct TabletToolData(
    Force? Force,
    float? TangentialForce,
    ushort? Twist,
    TabletToolTilt? Tilt,
    TabletToolAngle? Angle)
{
    public TabletToolTilt? GetTilt()
    {
        return Tilt ?? Angle?.Tilt();
    }

    public TabletToolAngle? GetAngle()
    {
        return Angle ?? Tilt?.Angle();
    }
}

public readonly record struct TabletToolTilt(sbyte X, sbyte Y)
{
    public TabletToolAngle Angle()
    {
        const double Pi0_5 = Math.PI / 2.0;
        const double Pi1_5 = 3.0 * Math.PI / 2.0;
        const double Pi2 = 2.0 * Math.PI;

        double x = X * Math.PI / 180.0;
        double y = Y * Math.PI / 180.0;
        double azimuth = 0.0;

        if (X == 0)
        {
            azimuth = Y.CompareTo((sbyte)0) switch
            {
                > 0 => Pi0_5,
                < 0 => Pi1_5,
                _ => azimuth,
            };
        }
        else if (Y == 0)
        {
            if (X < 0)
            {
                azimuth = Math.PI;
            }
        }
        else if (Math.Abs(X) == 90 || Math.Abs(Y) == 90)
        {
            azimuth = 0.0;
        }
        else
        {
            azimuth = Math.Atan2(Math.Tan(y), Math.Tan(x));

            if (azimuth < 0.0)
            {
                azimuth += Pi2;
            }
        }

        double altitude;

        if (Math.Abs(X) == 90 || Math.Abs(Y) == 90)
        {
            altitude = 0.0;
        }
        else if (X == 0)
        {
            altitude = Pi0_5 - Math.Abs(y);
        }
        else if (Y == 0)
        {
            altitude = Pi0_5 - Math.Abs(x);
        }
        else
        {
            altitude = Math.Atan(1.0 / Math.Sqrt(Math.Pow(Math.Tan(x), 2) + Math.Pow(Math.Tan(y), 2)));
        }

        return new TabletToolAngle(altitude, azimuth);
    }
}

public readonly record struct TabletToolAngle(double Altitude, double Azimuth)
{
    public static TabletToolAngle Default => new(2.0 / Math.PI, 0.0);

    public TabletToolTilt Tilt()
    {
        const double Pi0_5 = Math.PI / 2.0;
        const double Pi1_5 = 3.0 * Math.PI / 2.0;
        const double Pi2 = 2.0 * Math.PI;

        double x = 0.0;
        double y = 0.0;

        if (Altitude == 0.0)
        {
            if (Azimuth == 0.0 || Azimuth == Pi2)
            {
                x = Pi0_5;
            }
            else if (Azimuth == Pi0_5)
            {
                y = Pi0_5;
            }
            else if (Azimuth == Math.PI)
            {
                x = -Pi0_5;
            }
            else if (Azimuth == Pi1_5)
            {
                y = -Pi0_5;
            }
            else if (Azimuth > 0.0 && Azimuth < Pi0_5)
            {
                x = Pi0_5;
                y = Pi0_5;
            }
            else if (Azimuth > Pi0_5 && Azimuth < Math.PI)
            {
                x = -Pi0_5;
                y = Pi0_5;
            }
            else if (Azimuth > Math.PI && Azimuth < Pi1_5)
            {
                x = -Pi0_5;
                y = -Pi0_5;
            }
            else if (Azimuth > Pi1_5 && Azimuth < Pi2)
            {
                x = Pi0_5;
                y = -Pi0_5;
            }
        }

        if (Altitude != 0.0)
        {
            double altitude = Math.Tan(Altitude);

            x = Math.Atan(Math.Cos(Azimuth) / altitude);
            y = Math.Atan(Math.Sin(Azimuth) / altitude);
        }

        return new TabletToolTilt(ToDegreesSByte(x), ToDegreesSByte(y));
    }

    private static sbyte ToDegreesSByte(double radians)
    {
        return (sbyte)Math.Round(radians * 180.0 / Math.PI, MidpointRounding.AwayFromZero);
    }
}

public enum ElementState
{
    Pressed,
    Released,
}

public static class ElementStateExtensions
{
    public static bool IsPressed(this ElementState state)
    {
        return state == ElementState.Pressed;
    }
}

public enum MouseButton : byte
{
    Left = 0,
    Right = 1,
    Middle = 2,
    Back = 3,
    Forward = 4,
    Button6 = 5,
    Button7 = 6,
    Button8 = 7,
    Button9 = 8,
    Button10 = 9,
    Button11 = 10,
    Button12 = 11,
    Button13 = 12,
    Button14 = 13,
    Button15 = 14,
    Button16 = 15,
    Button17 = 16,
    Button18 = 17,
    Button19 = 18,
    Button20 = 19,
    Button21 = 20,
    Button22 = 21,
    Button23 = 22,
    Button24 = 23,
    Button25 = 24,
    Button26 = 25,
    Button27 = 26,
    Button28 = 27,
    Button29 = 28,
    Button30 = 29,
    Button31 = 30,
    Button32 = 31,
}

public static class MouseButtonExtensions
{
    public static MouseButton? TryFromByte(byte value)
    {
        return value <= 31 ? (MouseButton)value : null;
    }
}

public record struct TabletToolButton
{
    public readonly record struct Contact;

    public readonly record struct Barrel;

    public readonly record struct Other(ushort Code);

    private const byte ContactTag = 0;
    private const byte BarrelTag = 1;
    private const byte OtherTag = 2;

    private byte _tag;
    private Contact _contact;
    private Barrel _barrel;
    private Other _other;

    public TabletToolButton(Contact value) { this = default; _tag = ContactTag; _contact = value; }

    public TabletToolButton(Barrel value) { this = default; _tag = BarrelTag; _barrel = value; }

    public TabletToolButton(Other value) { this = default; _tag = OtherTag; _other = value; }

    public MouseButton? ToMouseButton()
    {
        if (TryGetValue(out Contact _))
        {
            return global::Winit.Core.MouseButton.Left;
        }

        if (TryGetValue(out Barrel _))
        {
            return global::Winit.Core.MouseButton.Right;
        }

        return _other.Code switch
        {
            1 => global::Winit.Core.MouseButton.Middle,
            3 => global::Winit.Core.MouseButton.Back,
            4 => global::Winit.Core.MouseButton.Forward,
            _ => null,
        };
    }

    public bool TryGetValue(out Contact value) { value = _contact; return _tag == ContactTag; }

    public bool TryGetValue(out Barrel value) { value = _barrel; return _tag == BarrelTag; }

    public bool TryGetValue(out Other value) { value = _other; return _tag == OtherTag; }
}

public record struct MouseScrollDelta
{
    public readonly record struct LineDelta(float X, float Y);

    public readonly record struct PixelDelta(PhysicalPosition<double> Position);

    private const byte LineDeltaTag = 0;
    private const byte PixelDeltaTag = 1;

    private byte _tag;
    private LineDelta _lineDelta;
    private PixelDelta _pixelDelta;

    public MouseScrollDelta(LineDelta value) { this = default; _tag = LineDeltaTag; _lineDelta = value; }

    public MouseScrollDelta(PixelDelta value) { this = default; _tag = PixelDeltaTag; _pixelDelta = value; }

    public bool TryGetValue(out LineDelta value) { value = _lineDelta; return _tag == LineDeltaTag; }

    public bool TryGetValue(out PixelDelta value) { value = _pixelDelta; return _tag == PixelDeltaTag; }
}

public sealed class SurfaceSizeState(PhysicalSize<uint> surfaceSize)
{
    private readonly Lock _lock = new();
    private PhysicalSize<uint> _surfaceSize = surfaceSize;

    public PhysicalSize<uint> SurfaceSize
    {
        get
        {
            lock (_lock)
            {
                return _surfaceSize;
            }
        }
        set
        {
            lock (_lock)
            {
                _surfaceSize = value;
            }
        }
    }
}

public readonly record struct SurfaceSizeWriter(WeakReference<SurfaceSizeState> NewSurfaceSize)
    : IEquatable<SurfaceSizeWriter>
{
    public static SurfaceSizeWriter Create(SurfaceSizeState state)
    {
        return new SurfaceSizeWriter(new WeakReference<SurfaceSizeState>(state));
    }

    public static SurfaceSizeWriter Create(PhysicalSize<uint> surfaceSize)
    {
        return Create(new SurfaceSizeState(surfaceSize));
    }

    public void RequestSurfaceSize(PhysicalSize<uint> newSurfaceSize)
    {
        if (!NewSurfaceSize.TryGetTarget(out SurfaceSizeState? state))
        {
            throw new IgnoredRequestException();
        }

        state.SurfaceSize = newSurfaceSize;
    }

    public bool TryRequestSurfaceSize(PhysicalSize<uint> newSurfaceSize)
    {
        if (!NewSurfaceSize.TryGetTarget(out SurfaceSizeState? state))
        {
            return false;
        }

        state.SurfaceSize = newSurfaceSize;
        return true;
    }

    public PhysicalSize<uint> SurfaceSize()
    {
        if (!NewSurfaceSize.TryGetTarget(out SurfaceSizeState? state))
        {
            throw new IgnoredRequestException();
        }

        return state.SurfaceSize;
    }

    public bool TryGetSurfaceSize(out PhysicalSize<uint> surfaceSize)
    {
        if (!NewSurfaceSize.TryGetTarget(out SurfaceSizeState? state))
        {
            surfaceSize = default;
            return false;
        }

        surfaceSize = state.SurfaceSize;
        return true;
    }

    public bool Equals(SurfaceSizeWriter other)
    {
        if (NewSurfaceSize.TryGetTarget(out SurfaceSizeState? left)
            && other.NewSurfaceSize.TryGetTarget(out SurfaceSizeState? right))
        {
            return ReferenceEquals(left, right);
        }

        return ReferenceEquals(NewSurfaceSize, other.NewSurfaceSize);
    }

    public override int GetHashCode()
    {
        return NewSurfaceSize.TryGetTarget(out SurfaceSizeState? state)
            ? state.GetHashCode()
            : NewSurfaceSize.GetHashCode();
    }
}
