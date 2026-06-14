using Winit.Dpi;

namespace Winit.X11.Util;

internal readonly record struct AaRect(long X, long Y, long Width, long Height)
{
    public AaRect(PhysicalPosition<int> position, PhysicalSize<uint> size)
        : this(position.X, position.Y, size.Width, size.Height)
    {
    }

    public bool ContainsPoint(long x, long y)
    {
        return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
    }

    public long GetOverlappingArea(AaRect other)
    {
        long xOverlap = Math.Max(0, Math.Min(X + Width, other.X + other.Width) - Math.Max(X, other.X));
        long yOverlap = Math.Max(0, Math.Min(Y + Height, other.Y + other.Height) - Math.Max(Y, other.Y));
        return xOverlap * yOverlap;
    }
}

internal readonly record struct FrameExtents(uint Left, uint Right, uint Top, uint Bottom)
{
    public static FrameExtents FromBorder(uint border)
    {
        return new FrameExtents(border, border, border, border);
    }
}

internal enum FrameExtentsHeuristicPath
{
    Supported,
    UnsupportedNested,
    UnsupportedBordered,
}

internal readonly record struct FrameExtentsHeuristic(
    FrameExtents FrameExtents,
    FrameExtentsHeuristicPath HeuristicPath)
{
    public PhysicalPosition<int> SurfacePosition()
    {
        return HeuristicPath != FrameExtentsHeuristicPath.UnsupportedBordered
            ? new PhysicalPosition<int>(checked((int)FrameExtents.Left), checked((int)FrameExtents.Top))
            : new PhysicalPosition<int>(0, 0);
    }

    public PhysicalPosition<int> InnerPositionToOuter(int x, int y)
    {
        PhysicalPosition<int> surfacePosition = SurfacePosition();
        return new PhysicalPosition<int>(x - surfacePosition.X, y - surfacePosition.Y);
    }

    public PhysicalSize<uint> SurfaceSizeToOuter(PhysicalSize<uint> size)
    {
        return new PhysicalSize<uint>(
            SaturatingAdd(size.Width, SaturatingAdd(FrameExtents.Left, FrameExtents.Right)),
            SaturatingAdd(size.Height, SaturatingAdd(FrameExtents.Top, FrameExtents.Bottom)));
    }

    private static uint SaturatingAdd(uint left, uint right)
    {
        ulong result = (ulong)left + right;
        return result > uint.MaxValue ? uint.MaxValue : (uint)result;
    }
}

internal readonly record struct TranslateCoordinatesResult(PhysicalPosition<int> Destination, XlibWindow Child);

internal readonly record struct GeometryResult(
    XlibWindow Root,
    int X,
    int Y,
    uint Width,
    uint Height,
    uint BorderWidth,
    uint Depth);

internal static unsafe class GeometryExtensions
{
    public static bool TryTranslateCoordinates(
        this XConnection xconn,
        XlibWindow srcWindow,
        XlibWindow destWindow,
        int srcX,
        int srcY,
        out TranslateCoordinatesResult result)
    {
        int destX = 0;
        int destY = 0;
        XlibWindow child = default;
        if (PInvoke.XTranslateCoordinates(
                xconn.Display,
                srcWindow,
                destWindow,
                srcX,
                srcY,
                &destX,
                &destY,
                &child) == 0)
        {
            result = default;
            return false;
        }

        result = new TranslateCoordinatesResult(new PhysicalPosition<int>(destX, destY), child);
        return true;
    }

    public static bool TryTranslateCoordinatesRoot(
        this XConnection xconn,
        XlibWindow window,
        out TranslateCoordinatesResult result)
    {
        return xconn.TryTranslateCoordinates(window, xconn.RootWindow, 0, 0, out result);
    }

    public static bool TryGetGeometry(this XConnection xconn, XlibWindow window, out GeometryResult geometry)
    {
        XlibWindow root = default;
        int x = 0;
        int y = 0;
        uint width = 0;
        uint height = 0;
        uint borderWidth = 0;
        uint depth = 0;

        if (PInvoke.XGetGeometry(
                xconn.Display,
                window,
                &root,
                &x,
                &y,
                &width,
                &height,
                &borderWidth,
                &depth) == 0)
        {
            geometry = default;
            return false;
        }

        geometry = new GeometryResult(root, x, y, width, height, borderWidth, depth);
        return true;
    }

    public static FrameExtentsHeuristic GetFrameExtentsHeuristic(
        this XConnection xconn,
        XlibWindow window,
        XlibWindow root)
    {
        int innerYRelativeToRoot = 0;
        XlibWindow child = default;
        if (xconn.TryTranslateCoordinatesRoot(window, out TranslateCoordinatesResult coordinates))
        {
            innerYRelativeToRoot = coordinates.Destination.Y;
            child = coordinates.Child;
        }

        GeometryResult innerGeometry = xconn.TryGetGeometry(window, out GeometryResult geometry)
            ? geometry
            : new GeometryResult(root, 0, 0, 0, 0, 0, 0);

        bool nested = !(window == child || xconn.IsTopLevel(child, root) == true);
        if (xconn.GetFrameExtents(window) is { } supportedExtents)
        {
            if (!nested)
            {
                supportedExtents = new FrameExtents(0, 0, 0, 0);
            }

            return new FrameExtentsHeuristic(supportedExtents, FrameExtentsHeuristicPath.Supported);
        }

        if (nested && xconn.ClimbHierarchy(window, root) is { } outerWindow &&
            xconn.TryGetGeometry(outerWindow, out GeometryResult outerGeometry))
        {
            uint diffX = outerGeometry.Width > innerGeometry.Width
                ? outerGeometry.Width - innerGeometry.Width
                : 0;
            uint diffY = outerGeometry.Height > innerGeometry.Height
                ? outerGeometry.Height - innerGeometry.Height
                : 0;
            uint offsetY = innerYRelativeToRoot > outerGeometry.Y
                ? checked((uint)(innerYRelativeToRoot - outerGeometry.Y))
                : 0;
            uint left = diffX / 2;
            uint right = left;
            uint top = offsetY;
            uint bottom = diffY > offsetY ? diffY - offsetY : 0;
            return new FrameExtentsHeuristic(
                new FrameExtents(left, right, top, bottom),
                FrameExtentsHeuristicPath.UnsupportedNested);
        }

        return new FrameExtentsHeuristic(
            FrameExtents.FromBorder(innerGeometry.BorderWidth),
            FrameExtentsHeuristicPath.UnsupportedBordered);
    }

    private static FrameExtents? GetFrameExtents(this XConnection xconn, XlibWindow window)
    {
        Atom extentsAtom = xconn.Atoms[AtomName.NetFrameExtents];
        if (!Wm.HintIsSupported(extentsAtom))
        {
            return null;
        }

        try
        {
            nuint[] extents = xconn.GetProperty32(window, extentsAtom, new Atom(PInvoke.XaCardinal));
            if (extents.Length < 4)
            {
                return null;
            }

            return new FrameExtents(
                checked((uint)extents[0]),
                checked((uint)extents[1]),
                checked((uint)extents[2]),
                checked((uint)extents[3]));
        }
        catch (GetPropertyException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static bool? IsTopLevel(this XConnection xconn, XlibWindow window, XlibWindow root)
    {
        Atom clientListAtom = xconn.Atoms[AtomName.NetClientList];
        if (!Wm.HintIsSupported(clientListAtom))
        {
            return null;
        }

        try
        {
            nuint[] clientList = xconn.GetProperty32(root, clientListAtom, new Atom(PInvoke.XaWindow));
            return clientList.Contains(window.Value);
        }
        catch (GetPropertyException)
        {
            return null;
        }
    }

    private static XlibWindow? GetParentWindow(this XConnection xconn, XlibWindow window)
    {
        XlibWindow root = default;
        XlibWindow parent = default;
        XlibWindow* children = null;
        uint childCount = 0;

        try
        {
            if (PInvoke.XQueryTree(xconn.Display, window, &root, &parent, &children, &childCount) == 0)
            {
                return null;
            }

            return parent;
        }
        finally
        {
            if (children is not null)
            {
                _ = PInvoke.XFree((nint)children);
            }
        }
    }

    private static XlibWindow? ClimbHierarchy(this XConnection xconn, XlibWindow window, XlibWindow root)
    {
        XlibWindow outerWindow = window;
        while (true)
        {
            XlibWindow? candidate = xconn.GetParentWindow(outerWindow);
            if (candidate is null)
            {
                return null;
            }

            if (candidate.Value == root)
            {
                return outerWindow;
            }

            outerWindow = candidate.Value;
        }
    }
}
