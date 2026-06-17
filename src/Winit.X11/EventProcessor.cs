using Winit.Core;
using Winit.Dpi;
using Winit.Common.Xkb;
using Winit.X11.Util;

namespace Winit.X11;

internal sealed class EventProcessor(EventLoop target)
{
    public const int MaxModReplayLen = 32;
    public const byte KeycodeOffset = 8;
    private uint? _heldKeyPress;
    private uint? _firstTouch;
    private uint _touchCount;
    private ModifiersState _modifiers = ModifiersState.None;
    private readonly Dnd _dnd = new(target.XConnection);
    private readonly Xmodmap.ModifierKeymap _xmodmap = Xmodmap.ModifierKeymap.New(target.XConnection);
    private readonly List<uint> _xfilteredModifiers = [];
    private bool _isComposing;

    public void Dispatch(IApplicationHandler app, in XEvent xevent)
    {
        XEvent localEvent = xevent;
        if (FilterEvent(ref localEvent))
        {
            return;
        }

        if (target.XConnection.XkbFirstEvent == localEvent.Type)
        {
            DispatchXkbEvent(app, in localEvent);
            return;
        }

        if (target.XConnection.RandrFirstEvent == localEvent.Type)
        {
            ProcessDpiChange(app);
            return;
        }

        switch (localEvent.Type)
        {
            case PInvoke.ClientMessage:
                DispatchClientMessage(app, localEvent.ClientMessage);
                break;
            case PInvoke.ConfigureNotify:
                DispatchConfigureNotify(app, localEvent.Configure);
                break;
            case PInvoke.ReparentNotify:
                DispatchReparentNotify(localEvent.Reparent);
                break;
            case PInvoke.MapNotify:
                DispatchMapNotify(app, localEvent.Map);
                break;
            case PInvoke.Expose:
                DispatchExpose(app, localEvent.Expose);
                break;
            case PInvoke.VisibilityNotify:
                DispatchVisibilityNotify(app, localEvent.Visibility);
                break;
            case PInvoke.FocusIn:
                DispatchFocus(app, localEvent.FocusChange, hasFocus: true);
                break;
            case PInvoke.FocusOut:
                DispatchFocus(app, localEvent.FocusChange, hasFocus: false);
                break;
            case PInvoke.DestroyNotify:
                DispatchDestroyNotify(app, localEvent.DestroyWindow);
                break;
            case PInvoke.MotionNotify:
                DispatchMotionNotify(app, localEvent.Motion);
                break;
            case PInvoke.EnterNotify:
                DispatchCrossing(app, localEvent.Crossing, entered: true);
                break;
            case PInvoke.LeaveNotify:
                DispatchCrossing(app, localEvent.Crossing, entered: false);
                break;
            case PInvoke.ButtonPress:
                DispatchButton(app, localEvent.Button, ElementState.Pressed);
                break;
            case PInvoke.ButtonRelease:
                DispatchButton(app, localEvent.Button, ElementState.Released);
                break;
            case PInvoke.KeyPress:
                DispatchKey(app, localEvent.Key, ElementState.Pressed);
                break;
            case PInvoke.KeyRelease:
                DispatchKey(app, localEvent.Key, ElementState.Released);
                break;
            case PInvoke.PropertyNotify:
                DispatchPropertyNotify(app, localEvent.Property);
                break;
            case PInvoke.SelectionNotify:
                DispatchSelectionNotify(app, localEvent.Selection);
                break;
        }
    }

    public void DrainImeEvents(IApplicationHandler app)
    {
        foreach ((XlibWindow window, Ime.ImeEvent imeEvent) in target.DrainImeEvents())
        {
            if (!target.TryGetWindow(window, out Window? targetWindow))
            {
                continue;
            }

            WindowEvent? windowEvent = null;
            if (imeEvent.TryGetValue(out Ime.ImeEvent.Enabled _))
            {
                windowEvent = new WindowEvent(new WindowEvent.Ime(new Winit.Core.Ime(new Winit.Core.Ime.Enabled())));
            }
            else if (imeEvent.TryGetValue(out Ime.ImeEvent.Start _))
            {
                _isComposing = true;
                windowEvent = new WindowEvent(new WindowEvent.Ime(new Winit.Core.Ime(new Winit.Core.Ime.Preedit(string.Empty, null))));
            }
            else if (imeEvent.TryGetValue(out Ime.ImeEvent.Update update) && _isComposing)
            {
                windowEvent = new WindowEvent(new WindowEvent.Ime(new Winit.Core.Ime(new Winit.Core.Ime.Preedit(
                    update.Text,
                    (update.Position, update.Position)))));
            }
            else if (imeEvent.TryGetValue(out Ime.ImeEvent.End _))
            {
                _isComposing = false;
                windowEvent = new WindowEvent(new WindowEvent.Ime(new Winit.Core.Ime(new Winit.Core.Ime.Preedit(string.Empty, null))));
            }
            else if (imeEvent.TryGetValue(out Ime.ImeEvent.Disabled _))
            {
                _isComposing = false;
                windowEvent = new WindowEvent(new WindowEvent.Ime(new Winit.Core.Ime(new Winit.Core.Ime.Disabled())));
            }

            if (windowEvent is { } value)
            {
                app.WindowEvent(target, targetWindow.Id, value);
            }
        }
    }

    public unsafe void DispatchXInput2(IApplicationHandler app, XGenericEventCookie cookie)
    {
        if (cookie.Data is null)
        {
            return;
        }

        switch (cookie.EvType)
        {
            case PInvoke.XiButtonPress:
                XIDeviceEvent* deviceEvent = (XIDeviceEvent*)cookie.Data;
                UpdateModifiersFromXInput2Event(app, deviceEvent->Mods, deviceEvent->Group, force: false);
                DispatchXInput2Button(app, deviceEvent, ElementState.Pressed);
                break;
            case PInvoke.XiButtonRelease:
                deviceEvent = (XIDeviceEvent*)cookie.Data;
                UpdateModifiersFromXInput2Event(app, deviceEvent->Mods, deviceEvent->Group, force: false);
                DispatchXInput2Button(app, deviceEvent, ElementState.Released);
                break;
            case PInvoke.XiMotion:
                deviceEvent = (XIDeviceEvent*)cookie.Data;
                UpdateModifiersFromXInput2Event(app, deviceEvent->Mods, deviceEvent->Group, force: false);
                DispatchXInput2Motion(app, deviceEvent);
                break;
            case PInvoke.XiEnter:
                DispatchXInput2Crossing(app, (XICrossingEvent*)cookie.Data, entered: true);
                break;
            case PInvoke.XiLeave:
                XICrossingEvent* crossingEvent = (XICrossingEvent*)cookie.Data;
                UpdateModifiersFromXInput2Event(app, crossingEvent->Mods, crossingEvent->Group, force: false);
                DispatchXInput2Crossing(app, crossingEvent, entered: false);
                break;
            case PInvoke.XiFocusIn:
                DispatchXInput2Focus(app, (XICrossingEvent*)cookie.Data, hasFocus: true);
                break;
            case PInvoke.XiFocusOut:
                DispatchXInput2Focus(app, (XICrossingEvent*)cookie.Data, hasFocus: false);
                break;
            case PInvoke.XiTouchBegin:
            case PInvoke.XiTouchUpdate:
            case PInvoke.XiTouchEnd:
                DispatchXInput2Touch(app, (XIDeviceEvent*)cookie.Data, cookie.EvType);
                break;
            case PInvoke.XiHierarchyChanged:
                DispatchXInput2HierarchyChanged((XIHierarchyEvent*)cookie.Data);
                break;
            case PInvoke.XiRawButtonPress:
                XIRawEvent* rawEvent = (XIRawEvent*)cookie.Data;
                target.XConnection.SetTimestamp(rawEvent->Time);
                DispatchRawButton(app, rawEvent, ElementState.Pressed);
                break;
            case PInvoke.XiRawButtonRelease:
                rawEvent = (XIRawEvent*)cookie.Data;
                target.XConnection.SetTimestamp(rawEvent->Time);
                DispatchRawButton(app, rawEvent, ElementState.Released);
                break;
            case PInvoke.XiRawKeyPress:
                rawEvent = (XIRawEvent*)cookie.Data;
                target.XConnection.SetTimestamp(rawEvent->Time);
                DispatchRawKey(app, rawEvent, ElementState.Pressed);
                break;
            case PInvoke.XiRawKeyRelease:
                rawEvent = (XIRawEvent*)cookie.Data;
                target.XConnection.SetTimestamp(rawEvent->Time);
                DispatchRawKey(app, rawEvent, ElementState.Released);
                break;
            case PInvoke.XiRawMotion:
                rawEvent = (XIRawEvent*)cookie.Data;
                target.XConnection.SetTimestamp(rawEvent->Time);
                DispatchRawMotion(app, rawEvent);
                break;
        }
    }

    private unsafe bool FilterEvent(ref XEvent xevent)
    {
        if (target.Ime is null)
        {
            return false;
        }

        XlibWindow window = xevent.Any.Window;
        bool keyEvent = xevent.Type is PInvoke.KeyPress or PInvoke.KeyRelease;
        if (keyEvent && !target.Ime.IsImeAllowed(window))
        {
            return false;
        }

        XEvent localEvent = xevent;
        bool filtered = PInvoke.XFilterEvent(&localEvent, window) != 0;
        if (filtered && keyEvent && _xmodmap.IsModifier(xevent.Key.Keycode))
        {
            if (_xfilteredModifiers.Count == MaxModReplayLen)
            {
                _xfilteredModifiers.RemoveAt(_xfilteredModifiers.Count - 1);
            }

            _xfilteredModifiers.Insert(0, xevent.Key.Keycode);
        }

        return filtered;
    }

    private void DispatchClientMessage(IApplicationHandler app, XClientMessageEvent xevent)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        if (xevent.MessageType == target.Atoms[AtomName.WmProtocols])
        {
            nuint protocol = unchecked((nuint)(ulong)xevent.Data.L0);
            if (protocol == target.Atoms[AtomName.WmDeleteWindow].Value)
            {
                app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.CloseRequested()));
                return;
            }

            if (protocol == target.Atoms[AtomName.NetWmPing].Value)
            {
                target.XConnection.SendClientMessage(
                    target.RootWindow,
                    target.RootWindow,
                    xevent.MessageType,
                    PInvoke.SubstructureNotifyMask | PInvoke.SubstructureRedirectMask,
                    [xevent.Data.L0, xevent.Data.L1, xevent.Data.L2, xevent.Data.L3, xevent.Data.L4]);
                return;
            }

            if (protocol == target.Atoms[AtomName.NetWmSyncRequest].Value)
            {
                DispatchSyncRequest(window, xevent);
                return;
            }
        }

        DispatchDndClientMessage(app, window, xevent);
    }

    private void DispatchSyncRequest(Window window, XClientMessageEvent xevent)
    {
        if (window.SyncCounterId is not { } syncCounterId)
        {
            return;
        }

        XSyncValue value = new()
        {
            Lo = unchecked((uint)(xevent.Data.L2 & 0xffffffff)),
            Hi = unchecked((int)(uint)(xevent.Data.L3 & 0xffffffff)),
        };
        PInvoke.XSyncSetCounter(target.XConnection.Display, syncCounterId, value);
    }

    private void DispatchDndClientMessage(IApplicationHandler app, Window window, XClientMessageEvent xevent)
    {
        if (xevent.MessageType == target.Atoms[AtomName.XdndEnter])
        {
            DispatchDndEnter(xevent);
            return;
        }

        if (xevent.MessageType == target.Atoms[AtomName.XdndPosition])
        {
            DispatchDndPosition(xevent);
            return;
        }

        if (xevent.MessageType == target.Atoms[AtomName.XdndDrop])
        {
            DispatchDndDrop(app, window, xevent);
            return;
        }

        if (xevent.MessageType == target.Atoms[AtomName.XdndLeave])
        {
            DispatchDndLeave(app, window);
        }
    }

    private void DispatchDndEnter(XClientMessageEvent xevent)
    {
        XlibWindow sourceWindow = new(unchecked((nuint)xevent.Data.L0));
        long flags = xevent.Data.L1;
        _dnd.Version = flags >> 24;
        bool hasMoreTypes = (flags & 1) != 0;

        if (!hasMoreTypes)
        {
            _dnd.TypeList =
            [
                new Atom(unchecked((nuint)xevent.Data.L2)),
                new Atom(unchecked((nuint)xevent.Data.L3)),
                new Atom(unchecked((nuint)xevent.Data.L4)),
            ];
            return;
        }

        try
        {
            _dnd.TypeList = _dnd.GetTypeList(sourceWindow);
        }
        catch (GetPropertyException)
        {
            _dnd.TypeList = null;
        }
    }

    private unsafe void DispatchDndPosition(XClientMessageEvent xevent)
    {
        XlibWindow sourceWindow = new(unchecked((nuint)xevent.Data.L0));
        long packedCoordinates = xevent.Data.L2;
        int rootX = (short)((packedCoordinates >> 16) & 0xffff);
        int rootY = (short)(packedCoordinates & 0xffff);
        int dstX = rootX;
        int dstY = rootY;
        XlibWindow child = default;

        if (PInvoke.XTranslateCoordinates(
                target.XConnection.Display,
                target.RootWindow,
                xevent.Window,
                rootX,
                rootY,
                &dstX,
                &dstY,
                &child) != 0)
        {
            _dnd.Position = new PhysicalPosition<double>(dstX, dstY);
        }
        else
        {
            _dnd.Position = new PhysicalPosition<double>(rootX, rootY);
        }

        bool accepted = _dnd.TypeList?.Contains(target.Atoms[AtomName.TextUriList]) == true;
        if (!accepted)
        {
            _dnd.SendStatus(xevent.Window, sourceWindow, DndState.Rejected);
            _dnd.Reset();
            return;
        }

        _dnd.SourceWindow = sourceWindow;
        nuint time = _dnd.Version == 0 ? PInvoke.CurrentTime : unchecked((nuint)xevent.Data.L3);
        target.XConnection.SetTimestamp(time);
        _dnd.ConvertSelection(xevent.Window, time);
        _dnd.SendStatus(xevent.Window, sourceWindow, DndState.Accepted);
    }

    private void DispatchDndDrop(IApplicationHandler app, Window window, XClientMessageEvent xevent)
    {
        XlibWindow sourceWindow;
        DndState state;
        if (_dnd.SourceWindow is { } currentSourceWindow)
        {
            sourceWindow = currentSourceWindow;
            state = DndState.Accepted;
            if (_dnd.Paths is { } paths)
            {
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.DragDropped(paths, _dnd.Position)));
            }
        }
        else
        {
            sourceWindow = new XlibWindow(unchecked((nuint)xevent.Data.L0));
            state = DndState.Rejected;
        }

        _dnd.SendFinished(xevent.Window, sourceWindow, state);
        _dnd.Reset();
    }

    private void DispatchDndLeave(IApplicationHandler app, Window window)
    {
        if (_dnd.Dragging)
        {
            app.WindowEvent(
                target,
                window.Id,
                new WindowEvent(new WindowEvent.DragLeft(_dnd.Position)));
        }

        _dnd.Reset();
    }

    private void DispatchSelectionNotify(IApplicationHandler app, XSelectionEvent xevent)
    {
        if (xevent.Property != target.Atoms[AtomName.XdndSelection] ||
            !target.TryGetWindow(xevent.Requestor, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent.Time);
        _dnd.Paths = null;
        try
        {
            byte[] data = _dnd.ReadData(xevent.Requestor);
            if (!Dnd.TryParseData(data, out IReadOnlyList<string> paths))
            {
                return;
            }

            WindowEvent windowEvent;
            if (_dnd.Dragging)
            {
                windowEvent = new WindowEvent(new WindowEvent.DragMoved(_dnd.Position));
            }
            else
            {
                _dnd.Dragging = true;
                windowEvent = new WindowEvent(new WindowEvent.DragEntered(paths, _dnd.Position));
            }

            _dnd.Paths = paths;
            app.WindowEvent(target, window.Id, windowEvent);
        }
        catch (GetPropertyException)
        {
        }
    }

    private void DispatchConfigureNotify(IApplicationHandler app, XConfigureEvent xevent)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        if (xevent.SendEvent != 0)
        {
            PhysicalPosition<int> position = new(xevent.X, xevent.Y);
            if (window.UpdateSyntheticSurfacePosition(position, out PhysicalPosition<int> outerPosition))
            {
                app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Moved(outerPosition)));
            }
        }
        else
        {
            window.UpdateSurfacePositionRelativeParent(new PhysicalPosition<int>(xevent.X, xevent.Y));
        }

        if (xevent.Width > 0 && xevent.Height > 0)
        {
            PhysicalSize<uint> size = new((uint)xevent.Width, (uint)xevent.Height);
            if (window.UpdateSurfaceSize(size))
            {
                app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.SurfaceResized(size)));
            }
        }
    }

    private void DispatchReparentNotify(XReparentEvent xevent)
    {
        if (target.TryGetWindow(xevent.Window, out Window? window))
        {
            target.XConnection.UpdateCachedWmInfo(target.RootWindow);
            window.InvalidateCachedFrameExtents();
        }
    }

    private void DispatchMapNotify(IApplicationHandler app, XMapEvent xevent)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Focused(window.HasFocus)));
    }

    private void DispatchExpose(IApplicationHandler app, XExposeEvent xevent)
    {
        if (xevent.Count != 0 || !target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.RedrawRequested()));
    }

    private void DispatchVisibilityNotify(IApplicationHandler app, XVisibilityEvent xevent)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        bool occluded = xevent.State == PInvoke.VisibilityFullyObscured;
        app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Occluded(occluded)));
    }

    private void DispatchFocus(IApplicationHandler app, XFocusChangeEvent xevent, bool hasFocus)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window) || !window.UpdateFocus(hasFocus))
        {
            return;
        }

        if (hasFocus)
        {
            _ = target.Ime?.Focus(xevent.Window);
        }
        else
        {
            _ = target.Ime?.Unfocus(xevent.Window);
        }

        target.UpdateDeviceEventFilter(target.HasFocusedWindow);
        if (hasFocus)
        {
            app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Focused(true)));
            HandlePressedKeys(app, window.Id, ElementState.Pressed);
            UpdateModifiersFromQuery(app, window.Id);
        }
        else
        {
            if (target.XkbContext?.State is { } xkbState)
            {
                xkbState.UpdateModifiers(0, 0, 0, 0, 0, 0);
                SendModifiers(app, window.Id, xkbState.Modifiers(), force: true);
            }

            HandlePressedKeys(app, window.Id, ElementState.Released);
            _heldKeyPress = null;
            app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Focused(false)));
        }
    }

    private void DispatchPropertyNotify(IApplicationHandler app, XPropertyEvent xevent)
    {
        if (xevent.Atom == target.Atoms[AtomName.ResourceManager] ||
            xevent.Atom == target.Atoms[AtomName.XSettingsSettings])
        {
            ProcessDpiChange(app);
        }
    }

    private void DispatchDestroyNotify(IApplicationHandler app, XDestroyWindowEvent xevent)
    {
        if (!target.RemoveWindow(xevent.Window, out Window? window))
        {
            return;
        }

        target.RemoveImeContext(xevent.Window);
        app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Destroyed()));
    }

    private void DispatchMotionNotify(IApplicationHandler app, XMotionEvent xevent)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent.Time);
        PhysicalPosition<double> position = new(xevent.X, xevent.Y);
        app.WindowEvent(
            target,
            window.Id,
            new WindowEvent(new WindowEvent.PointerMoved(
                null,
                position,
                true,
                new PointerSource(new PointerSource.Mouse()))));
    }

    private void DispatchCrossing(IApplicationHandler app, XCrossingEvent xevent, bool entered)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent.Time);
        PhysicalPosition<double> position = new(xevent.X, xevent.Y);
        WindowEvent windowEvent = entered
            ? new WindowEvent(new WindowEvent.PointerEntered(
                null,
                position,
                true,
                new PointerKind(new PointerKind.Mouse())))
            : new WindowEvent(new WindowEvent.PointerLeft(
                null,
                position,
                true,
                new PointerKind(new PointerKind.Mouse())));

        app.WindowEvent(target, window.Id, windowEvent);
    }

    private void DispatchButton(IApplicationHandler app, XButtonEvent xevent, ElementState state)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent.Time);
        WindowEvent? windowEvent = ButtonEvent(xevent, state);
        if (windowEvent is null)
        {
            return;
        }

        app.WindowEvent(target, window.Id, windowEvent.Value);
    }

    private static WindowEvent? ButtonEvent(XButtonEvent xevent, ElementState state)
    {
        PhysicalPosition<double> position = new(xevent.X, xevent.Y);

        return xevent.Button switch
        {
            1 => PointerButton(state, position, MouseButton.Left),
            2 => PointerButton(state, position, MouseButton.Middle),
            3 => PointerButton(state, position, MouseButton.Right),
            4 when state == ElementState.Pressed => MouseWheel(0.0f, 1.0f),
            5 when state == ElementState.Pressed => MouseWheel(0.0f, -1.0f),
            6 when state == ElementState.Pressed => MouseWheel(1.0f, 0.0f),
            7 when state == ElementState.Pressed => MouseWheel(-1.0f, 0.0f),
            >= 4 and <= 7 => null,
            >= 8 and <= 36 => MouseButtonExtensions.TryFromByte((byte)(xevent.Button - 5)) is { } button
                ? PointerButton(state, position, button)
                : null,
            >= 37 and <= 255 => PointerButton(state, position, new ButtonSource(new ButtonSource.Unknown((ushort)xevent.Button))),
            _ => null,
        };
    }

    private static WindowEvent PointerButton(ElementState state, PhysicalPosition<double> position, MouseButton button)
    {
        return PointerButton(state, position, new ButtonSource(new ButtonSource.Mouse(button)));
    }

    private static WindowEvent PointerButton(ElementState state, PhysicalPosition<double> position, ButtonSource button)
    {
        return new WindowEvent(new WindowEvent.PointerButton(null, state, position, true, button));
    }

    private static WindowEvent MouseWheel(float x, float y)
    {
        return new WindowEvent(new WindowEvent.MouseWheel(
            null,
            new MouseScrollDelta(new MouseScrollDelta.LineDelta(x, y)),
            TouchPhase.Moved));
    }

    private void DispatchKey(IApplicationHandler app, XKeyEvent xevent, ElementState state)
    {
        if (!target.TryGetWindow(xevent.Window, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent.Time);
        uint keycode = xevent.Keycode;
        if (keycode == 0)
        {
            return;
        }

        bool repeats = target.XkbContext?.Keymap?.KeyRepeats(keycode) ?? true;
        bool repeat = false;

        if (repeats)
        {
            bool isLatestHeld = _heldKeyPress == keycode;
            if (state == ElementState.Pressed)
            {
                _heldKeyPress = keycode;
                repeat = isLatestHeld;
            }
            else if (isLatestHeld)
            {
                _heldKeyPress = null;
            }
        }

        bool replay = ConsumeFilteredModifier(keycode);
        if (!replay)
        {
            UpdateModifiersFromCoreEvent(app, window.Id, unchecked((ushort)xevent.State));
        }

        if (_isComposing && target.Ime?.GetContext(xevent.Window) is { } ic)
        {
            string written = target.XConnection.LookupUtf8(ic, ref xevent);
            if (written.Length > 0)
            {
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.Ime(new Winit.Core.Ime(new Winit.Core.Ime.Preedit(string.Empty, null)))));

                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.Ime(new Winit.Core.Ime(new Winit.Core.Ime.Commit(written)))));

                _isComposing = false;
            }

            return;
        }

        KeyEvent keyEvent = target.XkbContext?.KeyContext()?.ProcessKeyEvent(keycode, state, repeat)
            ?? FallbackKeyEvent(keycode, state, repeat);

        if (replay)
        {
            SendSyntheticModifierFromCore(app, window.Id, unchecked((ushort)xevent.State));
        }

        app.WindowEvent(
            target,
            window.Id,
            new WindowEvent(new WindowEvent.KeyboardInput(null, keyEvent, false)));

        if (replay)
        {
            SendModifiers(app, window.Id, _modifiers, force: true);
        }
        else if (target.XkbContext?.State is { } xkbState)
        {
            SendModifiers(app, window.Id, xkbState.Modifiers(), force: false);
        }
    }

    private static KeyEvent FallbackKeyEvent(uint keycode, ElementState state, bool repeat)
    {
        PhysicalKey physicalKey = XkbKeymap.RawKeycodeToPhysicalKey(keycode);
        Key logicalKey = new(new Key.Unidentified(new NativeKey(new NativeKey.Unidentified())));

        return new KeyEvent(
            physicalKey,
            logicalKey,
            null,
            XkbKeymap.KeyLocation(physicalKey, 0),
            state,
            repeat,
            null,
            logicalKey);
    }

    private void HandlePressedKeys(IApplicationHandler app, WindowId windowId, ElementState state)
    {
        if (target.XkbContext is not { } context ||
            context.Keymap is not { } keymap ||
            target.XConnection.XcbConnection == 0)
        {
            return;
        }

        using XkbState? xkbState = XkbState.NewX11(
            target.XConnection.XcbConnection,
            keymap,
            context.CoreKeyboardId);
        if (xkbState is null)
        {
            return;
        }

        KeyContext? keyContext = context.KeyContextWithState(xkbState);
        if (keyContext is null)
        {
            return;
        }

        foreach (uint keycode in target.XConnection.QueryKeymap())
        {
            if (keycode < KeycodeOffset)
            {
                continue;
            }

            KeyEvent keyEvent = keyContext.ProcessKeyEvent(keycode, state, repeat: false);
            app.WindowEvent(
                target,
                windowId,
                new WindowEvent(new WindowEvent.KeyboardInput(null, keyEvent, true)));
        }
    }

    private unsafe void DispatchXInput2Button(IApplicationHandler app, XIDeviceEvent* xevent, ElementState state)
    {
        if ((xevent->Flags & PInvoke.XiPointerEmulated) != 0 ||
            !target.TryGetWindow(xevent->Event, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent->Time);
        DeviceId deviceId = DeviceId.FromRaw(xevent->DeviceId);
        PhysicalPosition<double> position = new(xevent->EventX, xevent->EventY);
        WindowEvent? windowEvent = XInput2ButtonEvent(deviceId, (uint)xevent->Detail, state, position);
        if (windowEvent is not null)
        {
            app.WindowEvent(target, window.Id, windowEvent.Value);
        }
    }

    private unsafe void DispatchXInput2Motion(IApplicationHandler app, XIDeviceEvent* xevent)
    {
        DeviceId sourceId = DeviceId.FromRaw(xevent->SourceId);
        if (!target.TryGetDevice(sourceId, out Device? physicalDevice) ||
            physicalDevice.Type != DeviceType.Mouse)
        {
            return;
        }

        if (!target.TryGetWindow(xevent->Event, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent->Time);
        DeviceId deviceId = DeviceId.FromRaw(xevent->DeviceId);
        PhysicalPosition<double> position = new(xevent->EventX, xevent->EventY);
        app.WindowEvent(
            target,
            window.Id,
            new WindowEvent(new WindowEvent.PointerMoved(
                deviceId,
                position,
                true,
                new PointerSource(new PointerSource.Mouse()))));

        DispatchXInput2ScrollAxes(app, window, deviceId, physicalDevice, xevent->Valuators);
    }

    private unsafe void DispatchXInput2Crossing(IApplicationHandler app, XICrossingEvent* xevent, bool entered)
    {
        if (!target.TryGetWindow(xevent->Event, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent->Time);
        if (entered)
        {
            target.ResetScrollPositionsForSource(xevent->SourceId);
        }

        DeviceId deviceId = DeviceId.FromRaw(xevent->DeviceId);
        PhysicalPosition<double> position = new(xevent->EventX, xevent->EventY);
        WindowEvent windowEvent = entered
            ? new WindowEvent(new WindowEvent.PointerEntered(
                deviceId,
                position,
                true,
                new PointerKind(new PointerKind.Mouse())))
            : new WindowEvent(new WindowEvent.PointerLeft(
                deviceId,
                position,
                true,
                new PointerKind(new PointerKind.Mouse())));

        app.WindowEvent(target, window.Id, windowEvent);
    }

    private unsafe void DispatchXInput2ScrollAxes(
        IApplicationHandler app,
        Window window,
        DeviceId deviceId,
        Device physicalDevice,
        XIValuatorState valuators)
    {
        if (valuators.Mask is null || valuators.Values is null || valuators.MaskLen <= 0)
        {
            return;
        }

        double* value = valuators.Values;
        for (int i = 0; i < valuators.MaskLen * 8; i++)
        {
            if (!XiMaskIsSet(valuators.Mask, i))
            {
                continue;
            }

            double current = *value;
            value++;
            if (physicalDevice.TryUpdateScrollAxis(i, current, out MouseScrollDelta delta))
            {
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.MouseWheel(deviceId, delta, TouchPhase.Moved)));
            }
        }
    }

    private unsafe void DispatchXInput2Focus(IApplicationHandler app, XICrossingEvent* xevent, bool hasFocus)
    {
        if (!target.TryGetWindow(xevent->Event, out Window? window) || !window.UpdateFocus(hasFocus))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent->Time);
        if (hasFocus)
        {
            _ = target.Ime?.Focus(xevent->Event);
        }
        else
        {
            _ = target.Ime?.Unfocus(xevent->Event);
        }

        target.UpdateDeviceEventFilter(target.HasFocusedWindow);
        if (hasFocus)
        {
            app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Focused(true)));
            HandlePressedKeys(app, window.Id, ElementState.Pressed);
            UpdateModifiersFromQuery(app, window.Id);
        }
        else
        {
            if (target.XkbContext?.State is { } xkbState)
            {
                xkbState.UpdateModifiers(0, 0, 0, 0, 0, 0);
                SendModifiers(app, window.Id, xkbState.Modifiers(), force: true);
            }

            HandlePressedKeys(app, window.Id, ElementState.Released);
            _heldKeyPress = null;
            app.WindowEvent(target, window.Id, new WindowEvent(new WindowEvent.Focused(false)));
        }
    }

    private void UpdateModifiersFromXInput2Event(
        IApplicationHandler app,
        XIModifierState mods,
        XIGroupState group,
        bool force)
    {
        if (target.XkbContext?.State is not { } xkbState)
        {
            return;
        }

        xkbState.UpdateModifiers(
            unchecked((uint)mods.Base),
            unchecked((uint)mods.Latched),
            unchecked((uint)mods.Locked),
            unchecked((uint)group.Base),
            unchecked((uint)group.Latched),
            unchecked((uint)group.Locked));

        Window? activeWindow = target.Windows.FirstOrDefault(static window => window.HasFocus);
        if (activeWindow is null)
        {
            return;
        }

        SendModifiers(app, activeWindow.Id, xkbState.Modifiers(), force);
    }

    private void SendModifiers(
        IApplicationHandler app,
        WindowId windowId,
        ModifiersState modifiers,
        bool force)
    {
        if (!force && _modifiers == modifiers)
        {
            return;
        }

        _modifiers = modifiers;
        app.WindowEvent(
            target,
            windowId,
            new WindowEvent(new WindowEvent.ModifiersChanged(Modifiers.From(modifiers))));
    }

    private void UpdateModifiersFromQuery(IApplicationHandler app, WindowId windowId)
    {
        if (target.XkbContext?.State is not { } xkbState)
        {
            return;
        }

        unsafe
        {
            XkbStateRec state = default;
            if (PInvoke.XkbGetState(target.XConnection.Display, PInvoke.XkbUseCoreKbd, &state) != 0)
            {
                xkbState.UpdateModifiers(
                    state.BaseMods,
                    state.LatchedMods,
                    state.LockedMods,
                    state.BaseGroup,
                    state.LatchedGroup,
                    state.LockedGroup);
            }
        }

        SendModifiers(app, windowId, xkbState.Modifiers(), force: true);
    }

    private void UpdateModifiersFromCoreEvent(IApplicationHandler app, WindowId windowId, ushort state)
    {
        if (target.XkbContext?.State is not { } xkbState ||
            target.XkbContext.Keymap is not { } keymap)
        {
            return;
        }

        uint xkbMask = XkbModMaskFromCore(state, keymap.ModIndices);
        uint depressed = xkbState.DepressedModifiers() & xkbMask;
        uint latched = xkbState.LatchedModifiers() & xkbMask;
        uint locked = xkbState.LockedModifiers() & xkbMask;
        depressed |= ~(depressed | latched | locked) & xkbMask;

        xkbState.UpdateModifiers(depressed, latched, locked, 0, 0, CoreKeyboardGroup(state));
        SendModifiers(app, windowId, xkbState.Modifiers(), force: false);
    }

    private void SendSyntheticModifierFromCore(IApplicationHandler app, WindowId windowId, ushort state)
    {
        if (target.XkbContext?.Keymap is not { } keymap || target.XConnection.XcbConnection == 0)
        {
            return;
        }

        using XkbState? xkbState = XkbState.NewX11(
            target.XConnection.XcbConnection,
            keymap,
            target.XkbContext.CoreKeyboardId);
        if (xkbState is null)
        {
            return;
        }

        uint mask = XkbModMaskFromCore(state, keymap.ModIndices);
        xkbState.UpdateModifiers(mask, 0, 0, 0, 0, CoreKeyboardGroup(state));
        SendModifiers(app, windowId, xkbState.Modifiers(), force: false);
    }

    private bool ConsumeFilteredModifier(uint keycode)
    {
        for (int i = _xfilteredModifiers.Count - 1; i >= 0; i--)
        {
            if (_xfilteredModifiers[i] != keycode)
            {
                continue;
            }

            _xfilteredModifiers.RemoveRange(i, _xfilteredModifiers.Count - i);
            return true;
        }

        return false;
    }

    private static uint CoreKeyboardGroup(ushort state)
    {
        return (uint)((state >> 13) & 3);
    }

    private static uint XkbModMaskFromCore(ushort state, ModIndices modIndices)
    {
        uint depressed = 0;
        SetModMask(ref depressed, modIndices.Shift, state, 1 << 0);
        SetModMask(ref depressed, modIndices.Caps, state, 1 << 1);
        SetModMask(ref depressed, modIndices.Ctrl, state, 1 << 2);
        SetModMask(ref depressed, modIndices.Alt, state, 1 << 3);
        SetModMask(ref depressed, modIndices.Num, state, 1 << 4);
        SetModMask(ref depressed, modIndices.Mod3, state, 1 << 5);
        SetModMask(ref depressed, modIndices.Logo, state, 1 << 6);
        SetModMask(ref depressed, modIndices.Mod5, state, 1 << 7);
        return depressed;
    }

    private static void SetModMask(ref uint depressed, uint? index, ushort state, int coreMask)
    {
        if (index is { } value && (state & coreMask) != 0)
        {
            depressed |= 1u << (int)value;
        }
    }

    private void DispatchXkbEvent(IApplicationHandler app, in XEvent xevent)
    {
        switch (xevent.XkbAny.XkbType)
        {
            case PInvoke.XkbNewKeyboardNotify:
                DispatchXkbNewKeyboardNotify(app, xevent.XkbNewKeyboard);
                break;
            case PInvoke.XkbMapNotify:
                DispatchXkbMapNotify(app, xevent.XkbAny);
                break;
            case PInvoke.XkbStateNotify:
                DispatchXkbStateNotify(app, xevent.XkbState);
                break;
        }
    }

    private void DispatchXkbNewKeyboardNotify(IApplicationHandler app, XkbNewKeyboardNotifyEvent xevent)
    {
        target.XConnection.SetTimestamp(xevent.Time);
        Context? xkbContext = target.XkbContext;
        if (xkbContext is null || xevent.Device != xkbContext.CoreKeyboardId)
        {
            return;
        }

        bool keycodesChanged = (xevent.Changed & PInvoke.XkbNewKeyboardKeycodesMask) != 0;
        bool geometryChanged = (xevent.Changed & PInvoke.XkbNewKeyboardGeometryMask) != 0;
        if (!keycodesChanged && !geometryChanged)
        {
            return;
        }

        ReloadXkbKeymapAndSendModifiers(app, force: true);
    }

    private void DispatchXkbMapNotify(IApplicationHandler app, XkbAnyEvent xevent)
    {
        target.XConnection.SetTimestamp(xevent.Time);
        ReloadXkbKeymapAndSendModifiers(app, force: true);
    }

    private void DispatchXkbStateNotify(IApplicationHandler app, XkbStateNotifyEvent xevent)
    {
        target.XConnection.SetTimestamp(xevent.Time);
        if (target.XkbContext?.State is not { } state)
        {
            return;
        }

        state.UpdateModifiers(
            xevent.BaseMods,
            xevent.LatchedMods,
            xevent.LockedMods,
            unchecked((uint)xevent.BaseGroup),
            unchecked((uint)xevent.LatchedGroup),
            unchecked((uint)xevent.LockedGroup));

        if (ActiveWindow() is { } window)
        {
            SendModifiers(app, window.Id, state.Modifiers(), force: true);
        }
    }

    private void ReloadXkbKeymapAndSendModifiers(IApplicationHandler app, bool force)
    {
        Context? xkbContext = target.XkbContext;
        if (xkbContext is null || target.XConnection.XcbConnection == 0)
        {
            return;
        }

        xkbContext.SetKeymapFromX11(target.XConnection.XcbConnection);
        _xmodmap.ReloadFromXConnection(target.XConnection);
        if (ActiveWindow() is { } window && xkbContext.State is { } state)
        {
            SendModifiers(app, window.Id, state.Modifiers(), force);
        }
    }

    private Window? ActiveWindow()
    {
        return target.Windows.FirstOrDefault(static window => window.HasFocus);
    }

    private unsafe void DispatchXInput2Touch(IApplicationHandler app, XIDeviceEvent* xevent, int phase)
    {
        if (!target.TryGetWindow(xevent->Event, out Window? window))
        {
            return;
        }

        target.XConnection.SetTimestamp(xevent->Time);
        DeviceId deviceId = DeviceId.FromRaw(xevent->DeviceId);
        uint touchId = (uint)xevent->Detail;
        FingerId fingerId = FingerId.FromRaw(touchId);
        PhysicalPosition<double> position = new(xevent->EventX, xevent->EventY);
        bool isFirstTouch = IsFirstTouch(touchId, phase);

        if (isFirstTouch)
        {
            app.WindowEvent(
                target,
                window.Id,
                new WindowEvent(new WindowEvent.PointerMoved(
                    null,
                    position,
                    true,
                    new PointerSource(new PointerSource.Mouse()))));
        }

        switch (phase)
        {
            case PInvoke.XiTouchBegin:
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.PointerEntered(
                        deviceId,
                        position,
                        isFirstTouch,
                        new PointerKind(new PointerKind.Touch(fingerId)))));
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.PointerButton(
                        deviceId,
                        ElementState.Pressed,
                        position,
                        isFirstTouch,
                        new ButtonSource(new ButtonSource.Touch(fingerId, null)))));
                break;

            case PInvoke.XiTouchUpdate:
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.PointerMoved(
                        deviceId,
                        position,
                        isFirstTouch,
                        new PointerSource(new PointerSource.Touch(fingerId, null)))));
                break;

            case PInvoke.XiTouchEnd:
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.PointerButton(
                        deviceId,
                        ElementState.Released,
                        position,
                        isFirstTouch,
                        new ButtonSource(new ButtonSource.Touch(fingerId, null)))));
                app.WindowEvent(
                    target,
                    window.Id,
                    new WindowEvent(new WindowEvent.PointerLeft(
                        deviceId,
                        position,
                        isFirstTouch,
                        new PointerKind(new PointerKind.Touch(fingerId)))));
                break;
        }
    }

    private static WindowEvent? XInput2ButtonEvent(
        DeviceId deviceId,
        uint button,
        ElementState state,
        PhysicalPosition<double> position)
    {
        return button switch
        {
            1 => PointerButton(deviceId, state, position, MouseButton.Left),
            2 => PointerButton(deviceId, state, position, MouseButton.Middle),
            3 => PointerButton(deviceId, state, position, MouseButton.Right),
            4 when state == ElementState.Pressed => MouseWheel(deviceId, 0.0f, 1.0f),
            5 when state == ElementState.Pressed => MouseWheel(deviceId, 0.0f, -1.0f),
            6 when state == ElementState.Pressed => MouseWheel(deviceId, 1.0f, 0.0f),
            7 when state == ElementState.Pressed => MouseWheel(deviceId, -1.0f, 0.0f),
            >= 4 and <= 7 => null,
            >= 8 and <= 36 => MouseButtonExtensions.TryFromByte((byte)(button - 5)) is { } mouseButton
                ? PointerButton(deviceId, state, position, mouseButton)
                : null,
            >= 37 and <= 255 => PointerButton(
                deviceId,
                state,
                position,
                new ButtonSource(new ButtonSource.Unknown((ushort)button))),
            _ => null,
        };
    }

    private static WindowEvent PointerButton(
        DeviceId? deviceId,
        ElementState state,
        PhysicalPosition<double> position,
        MouseButton button)
    {
        return PointerButton(deviceId, state, position, new ButtonSource(new ButtonSource.Mouse(button)));
    }

    private static WindowEvent PointerButton(
        DeviceId? deviceId,
        ElementState state,
        PhysicalPosition<double> position,
        ButtonSource button)
    {
        return new WindowEvent(new WindowEvent.PointerButton(deviceId, state, position, true, button));
    }

    private static WindowEvent MouseWheel(DeviceId? deviceId, float x, float y)
    {
        return new WindowEvent(new WindowEvent.MouseWheel(
            deviceId,
            new MouseScrollDelta(new MouseScrollDelta.LineDelta(x, y)),
            TouchPhase.Moved));
    }

    private bool IsFirstTouch(uint id, int phase)
    {
        switch (phase)
        {
            case PInvoke.XiTouchBegin:
                if (_touchCount == 0)
                {
                    _firstTouch = id;
                }

                _touchCount++;
                break;
            case PInvoke.XiTouchEnd:
                if (_firstTouch == id)
                {
                    _firstTouch = null;
                }

                _touchCount = _touchCount == 0 ? 0 : _touchCount - 1;
                break;
        }

        return _firstTouch == id;
    }

    private unsafe void DispatchXInput2HierarchyChanged(XIHierarchyEvent* xevent)
    {
        target.XConnection.SetTimestamp(xevent->Time);
        if (xevent->Info is null || xevent->NumInfo <= 0)
        {
            return;
        }

        for (int i = 0; i < xevent->NumInfo; i++)
        {
            XIHierarchyInfo info = xevent->Info[i];
            if ((info.Flags & (PInvoke.XiSlaveAdded | PInvoke.XiMasterAdded)) != 0)
            {
                target.InitDevice(info.DeviceId);
            }
            else if ((info.Flags & (PInvoke.XiSlaveRemoved | PInvoke.XiMasterRemoved)) != 0)
            {
                target.RemoveDevice(info.DeviceId);
            }
        }
    }

    private unsafe void DispatchRawButton(IApplicationHandler app, XIRawEvent* xevent, ElementState state)
    {
        if ((xevent->Flags & PInvoke.XiPointerEmulated) != 0)
        {
            return;
        }

        app.DeviceEvent(
            target,
            DeviceId.FromRaw(xevent->DeviceId),
            new DeviceEvent(new DeviceEvent.Button((uint)xevent->Detail, state)));
    }

    private unsafe void DispatchRawKey(IApplicationHandler app, XIRawEvent* xevent, ElementState state)
    {
        uint keycode = (uint)xevent->Detail;
        if (keycode < KeycodeOffset)
        {
            return;
        }

        PhysicalKey physicalKey = XkbKeymap.RawKeycodeToPhysicalKey(keycode);
        app.DeviceEvent(
            target,
            DeviceId.FromRaw(xevent->SourceId),
            new DeviceEvent(new DeviceEvent.Key(new RawKeyEvent(physicalKey, state))));
    }

    private unsafe void DispatchRawMotion(IApplicationHandler app, XIRawEvent* xevent)
    {
        DeviceId sourceId = DeviceId.FromRaw(xevent->SourceId);
        if (!target.TryGetDevice(sourceId, out Device? physicalDevice) ||
            physicalDevice.Type != DeviceType.Mouse)
        {
            return;
        }

        if (xevent->Valuators.Mask is null || xevent->RawValues is null || xevent->Valuators.MaskLen <= 0)
        {
            return;
        }

        double dx = 0.0;
        double dy = 0.0;
        float wheelX = 0.0f;
        float wheelY = 0.0f;
        double* value = xevent->RawValues;

        for (int i = 0; i < xevent->Valuators.MaskLen * 8; i++)
        {
            if (!XiMaskIsSet(xevent->Valuators.Mask, i))
            {
                continue;
            }

            double current = *value;
            value++;
            switch (i)
            {
                case 0:
                    dx = current;
                    break;
                case 1:
                    dy = current;
                    break;
                case 2:
                    wheelX = (float)current;
                    break;
                case 3:
                    wheelY = (float)current;
                    break;
            }
        }

        DeviceId deviceId = DeviceId.FromRaw(xevent->DeviceId);
        if (dx != 0.0 || dy != 0.0)
        {
            app.DeviceEvent(
                target,
                deviceId,
                new DeviceEvent(new DeviceEvent.PointerMotion((dx, dy))));
        }

        if (wheelX != 0.0f || wheelY != 0.0f)
        {
            app.DeviceEvent(
                target,
                deviceId,
                new DeviceEvent(new DeviceEvent.MouseWheel(
                    new MouseScrollDelta(new MouseScrollDelta.LineDelta(wheelX, wheelY)))));
        }
    }

    private static unsafe bool XiMaskIsSet(byte* mask, int eventType)
    {
        return (mask[eventType >> 3] & (1 << (eventType & 7))) != 0;
    }

    private void ProcessDpiChange(IApplicationHandler app)
    {
        target.XConnection.ReloadResourceManagerString();
        foreach (Window window in target.Windows.ToArray())
        {
            window.RefreshDpi(app, target);
        }

        target.XConnection.Flush();
    }
}
