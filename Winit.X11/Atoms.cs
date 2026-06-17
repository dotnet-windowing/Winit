using System.Text;

namespace Winit.X11;

internal enum AtomName
{
    Card32,
    Utf8String,
    WmChangeState,
    WmClientMachine,
    WmDeleteWindow,
    WmProtocols,
    WmState,
    XimServers,
    NetWmIcon,
    NetWmMoveResize,
    NetWmName,
    NetWmPid,
    NetWmPing,
    NetWmSyncRequest,
    NetWmSyncRequestCounter,
    NetWmState,
    NetWmStateAbove,
    NetWmStateBelow,
    NetWmStateFullscreen,
    NetWmStateHidden,
    NetWmStateMaximizedHorz,
    NetWmStateMaximizedVert,
    NetWmWindowType,
    NetStartupInfoBegin,
    NetStartupInfo,
    NetStartupId,
    NetWmWindowTypeDesktop,
    NetWmWindowTypeDock,
    NetWmWindowTypeToolbar,
    NetWmWindowTypeMenu,
    NetWmWindowTypeUtility,
    NetWmWindowTypeSplash,
    NetWmWindowTypeDialog,
    NetWmWindowTypeDropdownMenu,
    NetWmWindowTypePopupMenu,
    NetWmWindowTypeTooltip,
    NetWmWindowTypeNotification,
    NetWmWindowTypeCombo,
    NetWmWindowTypeDnd,
    NetWmWindowTypeNormal,
    XdndAware,
    XdndEnter,
    XdndLeave,
    XdndDrop,
    XdndPosition,
    XdndStatus,
    XdndActionPrivate,
    XdndSelection,
    XdndFinished,
    XdndTypeList,
    TextUriList,
    None,
    ResourceManager,
    GtkThemeVariant,
    MotifWmHints,
    NetActiveWindow,
    NetClientList,
    NetFrameExtents,
    NetSupported,
    NetSupportingWmCheck,
    XEmbed,
    XSettingsSettings,
    AbsX,
    AbsY,
    AbsPressure,
    AbsTiltX,
    AbsTiltY,
    WinitWakeUp,
}

internal sealed class Atoms
{
    private static readonly IReadOnlyList<(AtomName Name, string WireName)> s_definitions =
    [
        (AtomName.Card32, "CARD32"),
        (AtomName.Utf8String, "UTF8_STRING"),
        (AtomName.WmChangeState, "WM_CHANGE_STATE"),
        (AtomName.WmClientMachine, "WM_CLIENT_MACHINE"),
        (AtomName.WmDeleteWindow, "WM_DELETE_WINDOW"),
        (AtomName.WmProtocols, "WM_PROTOCOLS"),
        (AtomName.WmState, "WM_STATE"),
        (AtomName.XimServers, "XIM_SERVERS"),
        (AtomName.NetWmIcon, "_NET_WM_ICON"),
        (AtomName.NetWmMoveResize, "_NET_WM_MOVERESIZE"),
        (AtomName.NetWmName, "_NET_WM_NAME"),
        (AtomName.NetWmPid, "_NET_WM_PID"),
        (AtomName.NetWmPing, "_NET_WM_PING"),
        (AtomName.NetWmSyncRequest, "_NET_WM_SYNC_REQUEST"),
        (AtomName.NetWmSyncRequestCounter, "_NET_WM_SYNC_REQUEST_COUNTER"),
        (AtomName.NetWmState, "_NET_WM_STATE"),
        (AtomName.NetWmStateAbove, "_NET_WM_STATE_ABOVE"),
        (AtomName.NetWmStateBelow, "_NET_WM_STATE_BELOW"),
        (AtomName.NetWmStateFullscreen, "_NET_WM_STATE_FULLSCREEN"),
        (AtomName.NetWmStateHidden, "_NET_WM_STATE_HIDDEN"),
        (AtomName.NetWmStateMaximizedHorz, "_NET_WM_STATE_MAXIMIZED_HORZ"),
        (AtomName.NetWmStateMaximizedVert, "_NET_WM_STATE_MAXIMIZED_VERT"),
        (AtomName.NetWmWindowType, "_NET_WM_WINDOW_TYPE"),
        (AtomName.NetStartupInfoBegin, "_NET_STARTUP_INFO_BEGIN"),
        (AtomName.NetStartupInfo, "_NET_STARTUP_INFO"),
        (AtomName.NetStartupId, "_NET_STARTUP_ID"),
        (AtomName.NetWmWindowTypeDesktop, "_NET_WM_WINDOW_TYPE_DESKTOP"),
        (AtomName.NetWmWindowTypeDock, "_NET_WM_WINDOW_TYPE_DOCK"),
        (AtomName.NetWmWindowTypeToolbar, "_NET_WM_WINDOW_TYPE_TOOLBAR"),
        (AtomName.NetWmWindowTypeMenu, "_NET_WM_WINDOW_TYPE_MENU"),
        (AtomName.NetWmWindowTypeUtility, "_NET_WM_WINDOW_TYPE_UTILITY"),
        (AtomName.NetWmWindowTypeSplash, "_NET_WM_WINDOW_TYPE_SPLASH"),
        (AtomName.NetWmWindowTypeDialog, "_NET_WM_WINDOW_TYPE_DIALOG"),
        (AtomName.NetWmWindowTypeDropdownMenu, "_NET_WM_WINDOW_TYPE_DROPDOWN_MENU"),
        (AtomName.NetWmWindowTypePopupMenu, "_NET_WM_WINDOW_TYPE_POPUP_MENU"),
        (AtomName.NetWmWindowTypeTooltip, "_NET_WM_WINDOW_TYPE_TOOLTIP"),
        (AtomName.NetWmWindowTypeNotification, "_NET_WM_WINDOW_TYPE_NOTIFICATION"),
        (AtomName.NetWmWindowTypeCombo, "_NET_WM_WINDOW_TYPE_COMBO"),
        (AtomName.NetWmWindowTypeDnd, "_NET_WM_WINDOW_TYPE_DND"),
        (AtomName.NetWmWindowTypeNormal, "_NET_WM_WINDOW_TYPE_NORMAL"),
        (AtomName.XdndAware, "XdndAware"),
        (AtomName.XdndEnter, "XdndEnter"),
        (AtomName.XdndLeave, "XdndLeave"),
        (AtomName.XdndDrop, "XdndDrop"),
        (AtomName.XdndPosition, "XdndPosition"),
        (AtomName.XdndStatus, "XdndStatus"),
        (AtomName.XdndActionPrivate, "XdndActionPrivate"),
        (AtomName.XdndSelection, "XdndSelection"),
        (AtomName.XdndFinished, "XdndFinished"),
        (AtomName.XdndTypeList, "XdndTypeList"),
        (AtomName.TextUriList, "text/uri-list"),
        (AtomName.None, "None"),
        (AtomName.ResourceManager, "RESOURCE_MANAGER"),
        (AtomName.GtkThemeVariant, "_GTK_THEME_VARIANT"),
        (AtomName.MotifWmHints, "_MOTIF_WM_HINTS"),
        (AtomName.NetActiveWindow, "_NET_ACTIVE_WINDOW"),
        (AtomName.NetClientList, "_NET_CLIENT_LIST"),
        (AtomName.NetFrameExtents, "_NET_FRAME_EXTENTS"),
        (AtomName.NetSupported, "_NET_SUPPORTED"),
        (AtomName.NetSupportingWmCheck, "_NET_SUPPORTING_WM_CHECK"),
        (AtomName.XEmbed, "_XEMBED"),
        (AtomName.XSettingsSettings, "_XSETTINGS_SETTINGS"),
        (AtomName.AbsX, "Abs X"),
        (AtomName.AbsY, "Abs Y"),
        (AtomName.AbsPressure, "Abs Pressure"),
        (AtomName.AbsTiltX, "Abs Tilt X"),
        (AtomName.AbsTiltY, "Abs Tilt Y"),
        (AtomName.WinitWakeUp, "_WINIT_WAKE_UP"),
    ];

    private readonly Dictionary<AtomName, Atom> _atoms;

    private Atoms(Dictionary<AtomName, Atom> atoms)
    {
        _atoms = atoms;
    }

    public Atom this[AtomName name] => _atoms[name];

    public static unsafe Atoms New(nint display)
    {
        Dictionary<AtomName, Atom> atoms = new(s_definitions.Count);

        foreach ((AtomName name, string wireName) in s_definitions)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(wireName + '\0');
            fixed (byte* namePtr = utf8)
            {
                atoms.Add(name, PInvoke.XInternAtom(display, (sbyte*)namePtr, 0));
            }
        }

        return new Atoms(atoms);
    }
}
