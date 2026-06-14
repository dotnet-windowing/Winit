using System.Text;
using Winit.Dpi;
using Winit.X11.Util;

namespace Winit.X11;

internal enum DndState
{
    Accepted,
    Rejected,
}

internal sealed class Dnd(XConnection xconn)
{
    public long? Version { get; set; }

    public List<Atom>? TypeList { get; set; }

    public XlibWindow? SourceWindow { get; set; }

    public PhysicalPosition<double> Position { get; set; }

    public IReadOnlyList<string>? Paths { get; set; }

    public bool Dragging { get; set; }

    public void Reset()
    {
        Version = null;
        TypeList = null;
        SourceWindow = null;
        Paths = null;
        Dragging = false;
    }

    public void SendStatus(XlibWindow thisWindow, XlibWindow targetWindow, DndState state)
    {
        (long accepted, Atom action) = state switch
        {
            DndState.Accepted => (1, xconn.Atoms[AtomName.XdndActionPrivate]),
            _ => (0, xconn.Atoms[AtomName.None]),
        };

        xconn.SendClientMessage(
            targetWindow,
            targetWindow,
            xconn.Atoms[AtomName.XdndStatus],
            PInvoke.NoEventMask,
            [(long)thisWindow.Value, accepted, 0, 0, (long)action.Value]);
    }

    public void SendFinished(XlibWindow thisWindow, XlibWindow targetWindow, DndState state)
    {
        (long accepted, Atom action) = state switch
        {
            DndState.Accepted => (1, xconn.Atoms[AtomName.XdndActionPrivate]),
            _ => (0, xconn.Atoms[AtomName.None]),
        };

        xconn.SendClientMessage(
            targetWindow,
            targetWindow,
            xconn.Atoms[AtomName.XdndFinished],
            PInvoke.NoEventMask,
            [(long)thisWindow.Value, accepted, (long)action.Value, 0, 0]);
    }

    public List<Atom> GetTypeList(XlibWindow sourceWindow)
    {
        nuint[] atoms = xconn.GetProperty32(
            sourceWindow,
            xconn.Atoms[AtomName.XdndTypeList],
            new Atom(PInvoke.XaAtom));
        return atoms.Select(static atom => new Atom(atom)).ToList();
    }

    public void ConvertSelection(XlibWindow window, nuint time)
    {
        _ = PInvoke.XConvertSelection(
            xconn.Display,
            xconn.Atoms[AtomName.XdndSelection],
            xconn.Atoms[AtomName.TextUriList],
            xconn.Atoms[AtomName.XdndSelection],
            window,
            time);
    }

    public byte[] ReadData(XlibWindow window)
    {
        return xconn.GetPropertyBytes(
            window,
            xconn.Atoms[AtomName.XdndSelection],
            xconn.Atoms[AtomName.TextUriList]);
    }

    public static bool TryParseData(ReadOnlySpan<byte> data, out IReadOnlyList<string> paths)
    {
        paths = [];
        if (data.IsEmpty)
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        List<string> result = [];
        foreach (string rawUri in decoded.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(rawUri, UriKind.Absolute, out Uri? uri) ||
                !uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(uri.Host))
            {
                return false;
            }

            try
            {
                result.Add(Path.GetFullPath(uri.LocalPath));
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        if (result.Count == 0)
        {
            return false;
        }

        paths = result;
        return true;
    }
}
