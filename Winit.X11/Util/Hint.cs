namespace Winit.X11.Util;

internal static class Hint
{
    public static Atom AsAtom(this WindowType windowType, XConnection xconn)
    {
        AtomName atomName = windowType switch
        {
            WindowType.Desktop => AtomName.NetWmWindowTypeDesktop,
            WindowType.Dock => AtomName.NetWmWindowTypeDock,
            WindowType.Toolbar => AtomName.NetWmWindowTypeToolbar,
            WindowType.Menu => AtomName.NetWmWindowTypeMenu,
            WindowType.Utility => AtomName.NetWmWindowTypeUtility,
            WindowType.Splash => AtomName.NetWmWindowTypeSplash,
            WindowType.Dialog => AtomName.NetWmWindowTypeDialog,
            WindowType.DropdownMenu => AtomName.NetWmWindowTypeDropdownMenu,
            WindowType.PopupMenu => AtomName.NetWmWindowTypePopupMenu,
            WindowType.Tooltip => AtomName.NetWmWindowTypeTooltip,
            WindowType.Notification => AtomName.NetWmWindowTypeNotification,
            WindowType.Combo => AtomName.NetWmWindowTypeCombo,
            WindowType.Dnd => AtomName.NetWmWindowTypeDnd,
            WindowType.Normal => AtomName.NetWmWindowTypeNormal,
            _ => AtomName.NetWmWindowTypeNormal,
        };
        return xconn.Atoms[atomName];
    }

    public static MotifHints GetMotifHints(this XConnection xconn, XlibWindow window)
    {
        Atom motifHints = xconn.Atoms[AtomName.MotifWmHints];
        MotifHints hints = new();

        try
        {
            nuint[] props = xconn.GetProperty32(window, motifHints, motifHints);
            hints.Flags = Get(props, 0);
            hints.Functions = Get(props, 1);
            hints.Decorations = Get(props, 2);
            hints.InputMode = Get(props, 3);
            hints.Status = Get(props, 4);
        }
        catch (GetPropertyException)
        {
        }

        return hints;
    }

    public static void SetMotifHints(this XConnection xconn, XlibWindow window, MotifHints hints)
    {
        Atom motifHints = xconn.Atoms[AtomName.MotifWmHints];
        Span<nuint> data =
        [
            hints.Flags,
            hints.Functions,
            hints.Decorations,
            hints.InputMode,
            hints.Status,
        ];
        xconn.ChangeProperty32(window, motifHints, motifHints, data);
    }

    private static nuint Get(nuint[] values, int index)
    {
        return index < values.Length ? values[index] : 0;
    }
}

internal sealed class MotifHints
{
    private const nuint MwmHintsFunctions = 1 << 0;
    private const nuint MwmHintsDecorations = 1 << 1;
    private const nuint MwmFuncAll = 1 << 0;
    private const nuint MwmFuncResize = 1 << 1;
    private const nuint MwmFuncMove = 1 << 2;
    private const nuint MwmFuncMinimize = 1 << 3;
    private const nuint MwmFuncMaximize = 1 << 4;
    private const nuint MwmFuncClose = 1 << 5;

    public nuint Flags { get; set; }

    public nuint Functions { get; set; }

    public nuint Decorations { get; set; }

    public nuint InputMode { get; set; }

    public nuint Status { get; set; }

    public void SetDecorations(bool decorations)
    {
        Flags |= MwmHintsDecorations;
        Decorations = decorations ? 1u : 0u;
    }

    public void SetMaximizable(bool maximizable)
    {
        if (maximizable)
        {
            AddFunc(MwmFuncMaximize);
        }
        else
        {
            RemoveFunc(MwmFuncMaximize);
        }
    }

    private void AddFunc(nuint func)
    {
        if ((Flags & MwmHintsFunctions) == 0)
        {
            return;
        }

        if ((Functions & MwmFuncAll) != 0)
        {
            Functions &= ~func;
        }
        else
        {
            Functions |= func;
        }
    }

    private void RemoveFunc(nuint func)
    {
        if ((Flags & MwmHintsFunctions) == 0)
        {
            Flags |= MwmHintsFunctions;
            Functions = MwmFuncAll;
        }

        if ((Functions & MwmFuncAll) != 0)
        {
            Functions |= func;
        }
        else
        {
            Functions &= ~func;
        }
    }
}
