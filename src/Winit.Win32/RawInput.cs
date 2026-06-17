using Winit.Core;

namespace Winit.Win32;

internal static unsafe class RawInput
{
    public const uint WmInput = 0x00FF;

    private const ushort HidUsagePageGeneric = 0x01;
    private const ushort HidUsageGenericMouse = 0x02;
    private const ushort HidUsageGenericKeyboard = 0x06;
    private const uint RidevRemove = 0x00000001;
    private const uint RidevInputSink = 0x00000100;
    private const uint RidevDevNotify = 0x00002000;
    private const uint RidiDeviceInfo = 0x2000000B;
    private const uint RidiDeviceName = 0x20000007;
    private const uint RidInput = 0x10000003;
    private const uint RimTypeMouse = 0;
    private const uint RimTypeKeyboard = 1;
    private const ushort MouseMoveAbsolute = 0x0001;
    private const ushort RiMouseButton1Down = 0x0001;
    private const ushort RiMouseButton1Up = 0x0002;
    private const ushort RiMouseButton2Down = 0x0004;
    private const ushort RiMouseButton2Up = 0x0008;
    private const ushort RiMouseButton3Down = 0x0010;
    private const ushort RiMouseButton3Up = 0x0020;
    private const ushort RiMouseButton4Down = 0x0040;
    private const ushort RiMouseButton4Up = 0x0080;
    private const ushort RiMouseButton5Down = 0x0100;
    private const ushort RiMouseButton5Up = 0x0200;
    private const ushort RiMouseWheel = 0x0400;
    private const ushort RiMouseHWheel = 0x0800;
    private const ushort RiKeyE0 = 0x0002;
    private const ushort RiKeyE1 = 0x0004;
    private const uint MapvkVkToVscEx = 4;
    private const uint WheelDelta = 120;

    public static bool Register(HWND hwnd, DeviceEvents filter)
    {
        nint target = (nint)hwnd.Value;
        uint flags = filter switch
        {
            DeviceEvents.Never => RidevRemove,
            DeviceEvents.WhenFocused => RidevDevNotify,
            DeviceEvents.Always => RidevDevNotify | RidevInputSink,
            _ => RidevDevNotify,
        };

        if (filter == DeviceEvents.Never)
        {
            target = 0;
        }

        RawInputDevice[] devices =
        [
            new RawInputDevice(HidUsagePageGeneric, HidUsageGenericMouse, flags, target),
            new RawInputDevice(HidUsagePageGeneric, HidUsageGenericKeyboard, flags, target),
        ];

        fixed (RawInputDevice* devicesPtr = devices)
        {
            return PInvoke.RegisterRawInputDevices(devicesPtr, (uint)devices.Length, (uint)sizeof(RawInputDevice));
        }
    }

    public static void Handle(EventLoop eventLoop, nint rawInputHandle)
    {
        uint size = 0;
        uint headerSize = (uint)sizeof(RawInputHeader);
        uint status = PInvoke.GetRawInputData(rawInputHandle, RidInput, null, ref size, headerSize);
        if (status == uint.MaxValue || size == 0)
        {
            return;
        }

        byte[] buffer = new byte[size];
        fixed (byte* bufferPtr = buffer)
        {
            status = PInvoke.GetRawInputData(rawInputHandle, RidInput, bufferPtr, ref size, headerSize);
            if (status == uint.MaxValue || status == 0)
            {
                return;
            }

            RawInputData* data = (RawInputData*)bufferPtr;
            DeviceId deviceId = DeviceId.FromRaw(data->Header.Device);
            if (data->Header.Type == RimTypeMouse)
            {
                HandleMouse(eventLoop, deviceId, data->Data.Mouse);
            }
            else if (data->Header.Type == RimTypeKeyboard)
            {
                HandleKeyboard(eventLoop, deviceId, data->Data.Keyboard);
            }
        }
    }

    public static string? GetDeviceName(long rawDeviceHandle)
    {
        if (rawDeviceHandle == 0)
        {
            return null;
        }

        uint minimumSize = 0;
        uint status = PInvoke.GetRawInputDeviceInfo((nint)rawDeviceHandle, RidiDeviceName, null, ref minimumSize);
        if (status != 0 || minimumSize == 0)
        {
            return null;
        }

        char[] buffer = new char[minimumSize];
        fixed (char* bufferPtr = buffer)
        {
            status = PInvoke.GetRawInputDeviceInfo(
                (nint)rawDeviceHandle,
                RidiDeviceName,
                bufferPtr,
                ref minimumSize);
        }

        if (status == uint.MaxValue || status == 0)
        {
            return null;
        }

        int length = Array.IndexOf(buffer, '\0');
        if (length < 0)
        {
            length = checked((int)Math.Min(status, (uint)buffer.Length));
        }

        return new string(buffer, 0, length);
    }

    public static RawDeviceInfo? GetDeviceInfo(long rawDeviceHandle)
    {
        if (rawDeviceHandle == 0)
        {
            return null;
        }

        RawDeviceInfo info = new()
        {
            Size = (uint)sizeof(RawDeviceInfo),
        };
        uint size = info.Size;
        uint status = PInvoke.GetRawInputDeviceInfo(
            (nint)rawDeviceHandle,
            RidiDeviceInfo,
            &info,
            ref size);
        return status == uint.MaxValue || status == 0 ? null : info;
    }

    private static void HandleMouse(EventLoop eventLoop, DeviceId deviceId, RawMouse mouse)
    {
        if ((mouse.Flags & MouseMoveAbsolute) == 0)
        {
            double x = mouse.LastX;
            double y = mouse.LastY;
            if (x != 0.0 || y != 0.0)
            {
                eventLoop.SendDeviceEvent(
                    deviceId,
                    new DeviceEvent(new DeviceEvent.PointerMotion((x, y))));
            }
        }

        ushort buttonFlags = mouse.ButtonFlags;
        if ((buttonFlags & RiMouseWheel) != 0)
        {
            float delta = unchecked((short)mouse.ButtonData) / (float)WheelDelta;
            eventLoop.SendDeviceEvent(
                deviceId,
                new DeviceEvent(new DeviceEvent.MouseWheel(
                    new MouseScrollDelta(new MouseScrollDelta.LineDelta(0.0f, delta)))));
        }

        if ((buttonFlags & RiMouseHWheel) != 0)
        {
            float delta = -unchecked((short)mouse.ButtonData) / (float)WheelDelta;
            eventLoop.SendDeviceEvent(
                deviceId,
                new DeviceEvent(new DeviceEvent.MouseWheel(
                    new MouseScrollDelta(new MouseScrollDelta.LineDelta(delta, 0.0f)))));
        }

        SendButton(eventLoop, deviceId, buttonFlags, 0, RiMouseButton1Down, RiMouseButton1Up);
        SendButton(eventLoop, deviceId, buttonFlags, 1, RiMouseButton2Down, RiMouseButton2Up);
        SendButton(eventLoop, deviceId, buttonFlags, 2, RiMouseButton3Down, RiMouseButton3Up);
        SendButton(eventLoop, deviceId, buttonFlags, 3, RiMouseButton4Down, RiMouseButton4Up);
        SendButton(eventLoop, deviceId, buttonFlags, 4, RiMouseButton5Down, RiMouseButton5Up);
    }

    private static void SendButton(
        EventLoop eventLoop,
        DeviceId deviceId,
        ushort buttonFlags,
        uint button,
        ushort down,
        ushort up)
    {
        ElementState? state = null;
        if ((buttonFlags & down) != 0)
        {
            state = ElementState.Pressed;
        }
        else if ((buttonFlags & up) != 0)
        {
            state = ElementState.Released;
        }

        if (state is { } value)
        {
            eventLoop.SendDeviceEvent(
                deviceId,
                new DeviceEvent(new DeviceEvent.Button(button, value)));
        }
    }

    private static void HandleKeyboard(EventLoop eventLoop, DeviceId deviceId, RawKeyboard keyboard)
    {
        bool pressed = keyboard.Message is 0x0100 or 0x0104;
        bool released = keyboard.Message is 0x0101 or 0x0105;
        if (!pressed && !released)
        {
            return;
        }

        PhysicalKey? physicalKey = PhysicalKeyFromRawKeyboard(keyboard);
        if (physicalKey is not { } key)
        {
            return;
        }

        ElementState state = pressed ? ElementState.Pressed : ElementState.Released;
        eventLoop.SendDeviceEvent(
            deviceId,
            new DeviceEvent(new DeviceEvent.Key(new RawKeyEvent(key, state))));
    }

    private static PhysicalKey? PhysicalKeyFromRawKeyboard(RawKeyboard keyboard)
    {
        bool extended = (keyboard.Flags & RiKeyE0) != 0;
        bool e1 = (keyboard.Flags & RiKeyE1) != 0;
        ushort scanCode = keyboard.MakeCode;
        if (scanCode == 0)
        {
            scanCode = unchecked((ushort)PInvoke.MapVirtualKeyW(keyboard.VKey, MapvkVkToVscEx));
        }

        if (e1 && scanCode == 0x1D || extended && scanCode == 0x2A)
        {
            return null;
        }

        if (keyboard.VKey == Keyboard.VkNumLock)
        {
            return PhysicalKey.From(KeyCode.NumLock);
        }

        KeyCode keyCode = Keyboard.KeyCodeFromScanCode(scanCode, extended);
        if (keyboard.VKey == Keyboard.VkShift && IsNumpadKeyCode(keyCode))
        {
            return null;
        }

        if (keyCode != KeyCode.Unidentified)
        {
            return PhysicalKey.From(keyCode);
        }

        ushort nativeCode = extended ? unchecked((ushort)(0xe000 | scanCode)) : scanCode;
        return PhysicalKey.From(new NativeKeyCode(new NativeKeyCode.Windows(nativeCode)));
    }

    private static bool IsNumpadKeyCode(KeyCode keyCode)
    {
        return keyCode is
            KeyCode.NumpadDecimal or
            >= KeyCode.Numpad0 and <= KeyCode.Numpad9;
    }
}

public static class DeviceIdExtWindows
{
    public static string? PersistentIdentifier(this DeviceId deviceId)
    {
        return RawInput.GetDeviceName(deviceId.IntoRaw());
    }
}
