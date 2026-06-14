using System.Text;

namespace Winit.X11.Util;

internal static class Wm
{
    public const long MoveResizeTopLeft = 0;
    public const long MoveResizeTop = 1;
    public const long MoveResizeTopRight = 2;
    public const long MoveResizeRight = 3;
    public const long MoveResizeBottomRight = 4;
    public const long MoveResizeBottom = 5;
    public const long MoveResizeBottomLeft = 6;
    public const long MoveResizeLeft = 7;
    public const long MoveResizeMove = 8;

    private static readonly object s_lock = new();
    private static HashSet<nuint> s_supportedHints = [];
    private static string? s_wmName;

    public static bool HintIsSupported(Atom hint)
    {
        lock (s_lock)
        {
            return s_supportedHints.Contains(hint.Value);
        }
    }

    public static bool WmNameIsOneOf(params string[] names)
    {
        lock (s_lock)
        {
            return s_wmName is not null && names.Contains(s_wmName);
        }
    }

    public static void UpdateCachedWmInfo(this XConnection xconn, XlibWindow root)
    {
        HashSet<nuint> supportedHints = GetSupportedHints(xconn, root);
        string? wmName = GetWmName(xconn, root);

        lock (s_lock)
        {
            s_supportedHints = supportedHints;
            s_wmName = wmName;
        }
    }

    private static HashSet<nuint> GetSupportedHints(XConnection xconn, XlibWindow root)
    {
        try
        {
            Atom supportedAtom = xconn.Atoms[AtomName.NetSupported];
            nuint[] atoms = xconn.GetProperty32(root, supportedAtom, new Atom(PInvoke.XaAtom));
            return [.. atoms];
        }
        catch (GetPropertyException)
        {
            return [];
        }
    }

    private static string? GetWmName(XConnection xconn, XlibWindow root)
    {
        Atom checkAtom = xconn.Atoms[AtomName.NetSupportingWmCheck];
        Atom wmNameAtom = xconn.Atoms[AtomName.NetWmName];
        Atom windowAtom = new(PInvoke.XaWindow);

        XlibWindow rootWindowWmCheck;
        try
        {
            nuint[] wmCheck = xconn.GetProperty32(root, checkAtom, windowAtom);
            if (wmCheck.Length == 0)
            {
                return null;
            }

            rootWindowWmCheck = new XlibWindow(wmCheck[0]);
        }
        catch (GetPropertyException)
        {
            return null;
        }

        try
        {
            nuint[] childWmCheck = xconn.GetProperty32(rootWindowWmCheck, checkAtom, windowAtom);
            if (childWmCheck.Length == 0 || childWmCheck[0] != rootWindowWmCheck.Value)
            {
                return null;
            }
        }
        catch (GetPropertyException)
        {
            return null;
        }

        byte[] name;
        try
        {
            name = xconn.GetPropertyBytes(rootWindowWmCheck, wmNameAtom, xconn.Atoms[AtomName.Utf8String]);
        }
        catch (GetPropertyException error) when (error.IsActualPropertyType(new Atom(PInvoke.XaString)))
        {
            name = xconn.GetPropertyBytes(rootWindowWmCheck, wmNameAtom, new Atom(PInvoke.XaString));
        }
        catch (GetPropertyException)
        {
            return null;
        }

        return Encoding.UTF8.GetString(name).TrimEnd('\0');
    }
}

internal enum StateOperation : long
{
    Remove = 0,
    Add = 1,
    Toggle = 2,
}

internal static class StateOperationExtensions
{
    public static StateOperation ToStateOperation(this bool value)
    {
        return value ? StateOperation.Add : StateOperation.Remove;
    }
}
