using System.Text;
using Winit.X11.Util;

namespace Winit.X11;

internal static class Activation
{
    public static string RequestActivationToken(this XConnection xconn, string windowTitle)
    {
        ThrowIfContainsNull(windowTitle, nameof(windowTitle));

        string host = Environment.MachineName;
        if (string.IsNullOrEmpty(host))
        {
            host = "winit";
        }

        string activationToken = $"{host}{Environment.ProcessId}_TIME{xconn.Timestamp}";
        string notification = "new: ID=" +
            QuoteString(activationToken) +
            " NAME=" +
            QuoteString(windowTitle) +
            " SCREEN=" +
            xconn.DefaultScreen;

        xconn.SendStartupNotificationMessage(Encoding.UTF8.GetBytes(notification + '\0'));
        return activationToken;
    }

    public static void RemoveActivationToken(this XConnection xconn, XlibWindow window, string startupId)
    {
        ThrowIfContainsNull(startupId, nameof(startupId));

        xconn.ChangePropertyBytes(
            window,
            xconn.Atoms[AtomName.NetStartupId],
            new Atom(PInvoke.XaString),
            Encoding.UTF8.GetBytes(startupId));

        string message = "remove: ID=" + QuoteString(startupId);
        xconn.SendStartupNotificationMessage(Encoding.UTF8.GetBytes(message + '\0'));
    }

    private static unsafe void SendStartupNotificationMessage(this XConnection xconn, ReadOnlySpan<byte> message)
    {
        XSetWindowAttributes attributes = new()
        {
            OverrideRedirect = 1,
            EventMask = PInvoke.StructureNotifyMask | PInvoke.PropertyChangeMask,
        };

        XlibWindow window = PInvoke.XCreateWindow(
            xconn.Display,
            xconn.RootWindow,
            -100,
            -100,
            1,
            1,
            0,
            PInvoke.CopyFromParent,
            PInvoke.InputOutput,
            0,
            PInvoke.CWOverrideRedirect | PInvoke.CWEventMask,
            &attributes);

        if (window.Value == 0)
        {
            throw new InvalidOperationException("XCreateWindow failed while creating startup notification window.");
        }

        try
        {
            Atom messageType = xconn.Atoms[AtomName.NetStartupInfoBegin];
            Span<byte> buffer = stackalloc byte[20];
            for (int offset = 0; offset < message.Length; offset += 20)
            {
                buffer.Clear();
                ReadOnlySpan<byte> chunk = message.Slice(offset, Math.Min(20, message.Length - offset));
                chunk.CopyTo(buffer);

                xconn.SendClientMessageBytes(
                    window,
                    xconn.RootWindow,
                    messageType,
                    PInvoke.PropertyChangeMask,
                    buffer);

                messageType = xconn.Atoms[AtomName.NetStartupInfo];
            }
        }
        finally
        {
            _ = PInvoke.XDestroyWindow(xconn.Display, window);
        }
    }

    private static string QuoteString(string value)
    {
        StringBuilder builder = new(value.Length + 2);
        builder.Append('"');
        foreach (char c in value)
        {
            if (c == '"')
            {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static void ThrowIfContainsNull(string value, string paramName)
    {
        if (value.Contains('\0'))
        {
            throw new ArgumentException("Activation strings must not contain a null byte.", paramName);
        }
    }
}
