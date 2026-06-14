namespace Winit.X11.Util;

internal static class ClientMsg
{
    public static unsafe void SendClientMessage(
        this XConnection xconn,
        XlibWindow window,
        XlibWindow targetWindow,
        Atom messageType,
        nint eventMask,
        ReadOnlySpan<long> data)
    {
        XEvent xevent = default;
        xevent.ClientMessage.Type = PInvoke.ClientMessage;
        xevent.ClientMessage.Display = xconn.Display;
        xevent.ClientMessage.Window = window;
        xevent.ClientMessage.MessageType = messageType;
        xevent.ClientMessage.Format = 32;
        xevent.ClientMessage.Data.L0 = data.Length > 0 ? data[0] : 0;
        xevent.ClientMessage.Data.L1 = data.Length > 1 ? data[1] : 0;
        xevent.ClientMessage.Data.L2 = data.Length > 2 ? data[2] : 0;
        xevent.ClientMessage.Data.L3 = data.Length > 3 ? data[3] : 0;
        xevent.ClientMessage.Data.L4 = data.Length > 4 ? data[4] : 0;

        _ = PInvoke.XSendEvent(xconn.Display, targetWindow, 0, eventMask, &xevent);
    }

    public static unsafe void SendClientMessageBytes(
        this XConnection xconn,
        XlibWindow window,
        XlibWindow targetWindow,
        Atom messageType,
        nint eventMask,
        ReadOnlySpan<byte> data)
    {
        XEvent xevent = default;
        xevent.ClientMessage.Type = PInvoke.ClientMessage;
        xevent.ClientMessage.Display = xconn.Display;
        xevent.ClientMessage.Window = window;
        xevent.ClientMessage.MessageType = messageType;
        xevent.ClientMessage.Format = 8;

        byte* dataPtr = (byte*)&xevent.ClientMessage.Data;
        int count = Math.Min(data.Length, 20);
        for (int i = 0; i < count; i++)
        {
            dataPtr[i] = data[i];
        }

        _ = PInvoke.XSendEvent(xconn.Display, targetWindow, 0, eventMask, &xevent);
    }
}
