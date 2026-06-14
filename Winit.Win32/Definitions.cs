using System.Runtime.InteropServices;

namespace Winit.Win32;

internal readonly record struct BOOL(int Value)
{
    public static implicit operator bool(BOOL value) => value.Value != 0;

    public static implicit operator BOOL(bool value) => new(value ? 1 : 0);

    public static bool operator true(BOOL value) => value.Value != 0;

    public static bool operator false(BOOL value) => value.Value == 0;

    public static bool operator !(BOOL value) => value.Value == 0;
}

internal readonly record struct HRESULT(int Value)
{
    public bool Succeeded => Value >= 0;
}

internal readonly record struct HWND(nint Value)
{
    public static HWND Null => new(0);
}

internal readonly record struct HDC(nint Value)
{
    public static HDC Null => new(0);
}

internal readonly record struct HMONITOR(nint Value);

internal readonly record struct HMENU(nint Value)
{
    public static HMENU Null => new(0);
}

internal readonly record struct HINSTANCE(nint Value)
{
    public static HINSTANCE Null => new(0);
}

internal readonly record struct HCURSOR(nint Value)
{
    public static HCURSOR Null => new(0);
}

internal readonly record struct WPARAM(nuint Value)
{
    public WPARAM(uint value)
        : this((nuint)value)
    {
    }
}

internal readonly record struct LPARAM(nint Value)
{
    public LPARAM(nuint value)
        : this(unchecked((nint)value))
    {
    }
}

internal readonly record struct LRESULT(nint Value)
{
    public LRESULT(int value)
        : this((nint)value)
    {
    }
}

internal readonly unsafe struct PCWSTR(char* value)
{
    public readonly char* Value = value;

    public static PCWSTR Null => new(null);
}

internal readonly unsafe struct PWSTR(char* value)
{
    public readonly char* Value = value;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FORMATETC
{
    public ushort cfFormat;
    public nint ptd;
    public uint dwAspect;
    public int lindex;
    public uint tymed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct STGMEDIUM
{
    public uint tymed;
    public nint unionmember;
    public nint pUnkForRelease;
}

#pragma warning disable CS0649
internal unsafe struct IUnknown
{
    public IUnknownVtbl* lpVtbl;
}

internal unsafe struct IUnknownVtbl
{
    public delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, int> QueryInterface;
    public delegate* unmanaged[Stdcall]<IUnknown*, uint> AddRef;
    public delegate* unmanaged[Stdcall]<IUnknown*, uint> Release;
}

internal unsafe struct IDataObject
{
    public IDataObjectVtbl* lpVtbl;
}

internal unsafe struct IDataObjectVtbl
{
    public IUnknownVtbl parent;
    public delegate* unmanaged[Stdcall]<IDataObject*, FORMATETC*, STGMEDIUM*, int> GetData;
    public delegate* unmanaged[Stdcall]<IDataObject*, FORMATETC*, STGMEDIUM*, int> GetDataHere;
    public delegate* unmanaged[Stdcall]<IDataObject*, FORMATETC*, int> QueryGetData;
    public delegate* unmanaged[Stdcall]<IDataObject*, FORMATETC*, FORMATETC*, int> GetCanonicalFormatEtc;
    public delegate* unmanaged[Stdcall]<IDataObject*, FORMATETC*, FORMATETC*, BOOL, int> SetData;
    public delegate* unmanaged[Stdcall]<IDataObject*, uint, void**, int> EnumFormatEtc;
    public delegate* unmanaged[Stdcall]<IDataObject*, FORMATETC*, uint, void*, uint*, int> DAdvise;
    public delegate* unmanaged[Stdcall]<IDataObject*, uint, int> DUnadvise;
    public delegate* unmanaged[Stdcall]<IDataObject*, void**, int> EnumDAdvise;
}

internal unsafe struct IDropTarget
{
    public IDropTargetVtbl* lpVtbl;
}

internal unsafe struct IDropTargetVtbl
{
    public IUnknownVtbl parent;
    public delegate* unmanaged[Stdcall]<IDropTarget*, IDataObject*, uint, NativePointL, uint*, int> DragEnter;
    public delegate* unmanaged[Stdcall]<IDropTarget*, uint, NativePointL, uint*, int> DragOver;
    public delegate* unmanaged[Stdcall]<IDropTarget*, int> DragLeave;
    public delegate* unmanaged[Stdcall]<IDropTarget*, IDataObject*, uint, NativePointL, uint*, int> Drop;
}

internal unsafe struct ITaskbarList
{
    public ITaskbarListVtbl* lpVtbl;
}

internal unsafe struct ITaskbarListVtbl
{
    public IUnknownVtbl parent;
    public delegate* unmanaged[Stdcall]<ITaskbarList*, int> HrInit;
    public delegate* unmanaged[Stdcall]<ITaskbarList*, HWND, int> AddTab;
    public delegate* unmanaged[Stdcall]<ITaskbarList*, HWND, int> DeleteTab;
    public delegate* unmanaged[Stdcall]<ITaskbarList*, HWND, int> ActivateTab;
    public delegate* unmanaged[Stdcall]<ITaskbarList*, HWND, int> SetActiveAlt;
}

internal unsafe struct ITaskbarList2
{
    public ITaskbarList2Vtbl* lpVtbl;
}

internal unsafe struct ITaskbarList2Vtbl
{
    public ITaskbarListVtbl parent;
    public delegate* unmanaged[Stdcall]<ITaskbarList2*, HWND, BOOL, int> MarkFullscreenWindow;
}
#pragma warning restore CS0649
