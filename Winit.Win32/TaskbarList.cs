namespace Winit.Win32;

internal static unsafe class TaskbarList
{
    private const int SOk = 0;
    private const uint CoInitApartmentThreaded = 0x2;
    private const uint ClsctxAll = 0x17;

    private static readonly Guid s_clsidTaskbarList = new("56FDF344-FD6D-11D0-958A-006097C9A090");
    private static readonly Guid s_iidITaskbarList = new("56FDF342-FD6D-11D0-958A-006097C9A090");
    private static readonly Guid s_iidITaskbarList2 = new("602D4995-B13A-429B-A66E-1935E44F4317");

    [ThreadStatic]
    private static bool s_comInitialized;

    [ThreadStatic]
    private static ITaskbarList* s_taskbarList;

    [ThreadStatic]
    private static ITaskbarList2* s_taskbarList2;

    public static void SetSkipTaskbar(HWND hwnd, bool skip)
    {
        ITaskbarList* taskbarList = GetTaskbarList();
        if (taskbarList is null)
        {
            return;
        }

        if (skip)
        {
            _ = taskbarList->lpVtbl->DeleteTab(taskbarList, hwnd);
        }
        else
        {
            _ = taskbarList->lpVtbl->AddTab(taskbarList, hwnd);
        }
    }

    public static void MarkFullscreenWindow(HWND hwnd, bool fullscreen)
    {
        ITaskbarList2* taskbarList = GetTaskbarList2();
        if (taskbarList is null)
        {
            return;
        }

        _ = taskbarList->lpVtbl->MarkFullscreenWindow(taskbarList, hwnd, fullscreen);
    }

    private static ITaskbarList* GetTaskbarList()
    {
        if (s_taskbarList is not null)
        {
            return s_taskbarList;
        }

        if (!EnsureComInitialized())
        {
            return null;
        }

        Guid clsid = s_clsidTaskbarList;
        Guid iid = s_iidITaskbarList;
        void* instance = null;
        int hr = PInvoke.CoCreateInstance(&clsid, 0, ClsctxAll, &iid, &instance);
        if (hr != SOk || instance is null)
        {
            return null;
        }

        ITaskbarList* taskbarList = (ITaskbarList*)instance;
        if (taskbarList->lpVtbl->HrInit(taskbarList) != SOk)
        {
            ReleaseUnknown((IUnknown*)taskbarList);
            return null;
        }

        s_taskbarList = taskbarList;
        return taskbarList;
    }

    private static ITaskbarList2* GetTaskbarList2()
    {
        if (s_taskbarList2 is not null)
        {
            return s_taskbarList2;
        }

        if (!EnsureComInitialized())
        {
            return null;
        }

        Guid clsid = s_clsidTaskbarList;
        Guid iid = s_iidITaskbarList2;
        void* instance = null;
        int hr = PInvoke.CoCreateInstance(&clsid, 0, ClsctxAll, &iid, &instance);
        if (hr != SOk || instance is null)
        {
            return null;
        }

        ITaskbarList2* taskbarList = (ITaskbarList2*)instance;
        if (taskbarList->lpVtbl->parent.HrInit((ITaskbarList*)taskbarList) != SOk)
        {
            ReleaseUnknown((IUnknown*)taskbarList);
            return null;
        }

        s_taskbarList2 = taskbarList;
        return taskbarList;
    }

    private static bool EnsureComInitialized()
    {
        if (s_comInitialized)
        {
            return true;
        }

        int hr = PInvoke.CoInitializeEx(0, CoInitApartmentThreaded);
        if (hr < 0)
        {
            return false;
        }

        s_comInitialized = true;
        return true;
    }

    private static void ReleaseUnknown(IUnknown* unknown)
    {
        if (unknown is not null && unknown->lpVtbl is not null)
        {
            _ = unknown->lpVtbl->Release(unknown);
        }
    }
}
