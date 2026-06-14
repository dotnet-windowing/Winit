using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;
using Winit.Dpi;

namespace Winit.Win32;

internal sealed unsafe class FileDropHandler : IDisposable
{
    private const int SOk = 0;
    private const int SFalse = 1;
    private const int ENoInterface = unchecked((int)0x80004002);
    private const int EPointer = unchecked((int)0x80004003);
    private const ushort CfHdrop = 15;
    private const uint DvaspectContent = 1;
    private const uint TymedHglobal = 1;
    private const int DvEFormatEtc = unchecked((int)0x80040064);
    private const int OleEWrongComObj = unchecked((int)0x8004000E);
    private const int RpcEChangedMode = unchecked((int)0x80010106);
    private const uint DropEffectNone = 0;
    private const uint DropEffectCopy = 1;
    private const uint DragQueryFileCount = 0xFFFFFFFF;

    private static readonly Guid s_iidIUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid s_iidIDropTarget = new("00000122-0000-0000-C000-000000000046");
    private static readonly IDropTargetVtbl* s_dropTargetVtbl = CreateDropTargetVtbl();

    [ThreadStatic]
    private static bool s_oleInitialized;

    private readonly Window _window;
    private FileDropHandlerData* _data;
    private bool _registered;
    private bool _disposed;

    private FileDropHandler(Window window)
    {
        _window = window;
        GCHandle windowHandle = GCHandle.Alloc(window);
        _data = (FileDropHandlerData*)NativeMemory.AllocZeroed((nuint)sizeof(FileDropHandlerData));
        _data->Interface.lpVtbl = s_dropTargetVtbl;
        _data->RefCount = 1;
        _data->WindowHandle = GCHandle.ToIntPtr(windowHandle);
        _data->CursorEffect = DropEffectNone;
    }

    public static FileDropHandler Register(Window window)
    {
        EnsureOleInitialized();

        FileDropHandler target = new(window);
        int hr = PInvoke.RegisterDragDrop(window.Hwnd, &target._data->Interface);
        if (hr != SOk)
        {
            target.ReleaseOwnReference();
            Marshal.ThrowExceptionForHR(hr);
        }

        target._registered = true;
        return target;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_registered)
        {
            _ = PInvoke.RevokeDragDrop(_window.Hwnd);
            _registered = false;
        }

        ReleaseOwnReference();
    }

    private void ReleaseOwnReference()
    {
        FileDropHandlerData* data = _data;
        if (data is null)
        {
            return;
        }

        _data = null;
        ReleaseData((IUnknown*)data);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int QueryInterface(IUnknown* @this, Guid* riid, void** ppvObject)
    {
        if (ppvObject is null)
        {
            return EPointer;
        }

        *ppvObject = null;
        if (@this is null || riid is null)
        {
            return EPointer;
        }

        Guid requested = *riid;
        if (requested == s_iidIUnknown || requested == s_iidIDropTarget)
        {
            AddRefData(@this);
            *ppvObject = @this;
            return SOk;
        }

        return ENoInterface;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static uint AddRef(IUnknown* @this)
    {
        return AddRefData(@this);
    }

    private static uint AddRefData(IUnknown* @this)
    {
        if (@this is null)
        {
            return 0;
        }

        FileDropHandlerData* data = FromInterface(@this);
        return (uint)Interlocked.Increment(ref data->RefCount);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static uint Release(IUnknown* @this)
    {
        return ReleaseData(@this);
    }

    private static uint ReleaseData(IUnknown* @this)
    {
        if (@this is null)
        {
            return 0;
        }

        FileDropHandlerData* data = FromInterface(@this);
        int count = Interlocked.Decrement(ref data->RefCount);
        if (count == 0)
        {
            GCHandle windowHandle = GCHandle.FromIntPtr(data->WindowHandle);
            if (windowHandle.IsAllocated)
            {
                windowHandle.Free();
            }

            NativeMemory.Free(data);
        }

        return count < 0 ? 0u : (uint)count;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int DragEnter(
        IDropTarget* @this,
        IDataObject* dataObject,
        uint keyState,
        NativePointL point,
        uint* effect)
    {
        _ = keyState;
        FileDropHandlerData* data = FromInterface(@this);
        IReadOnlyList<string>? paths = PathsFromDataObject(dataObject);
        data->Valid = paths is not null ? 1 : 0;
        data->CursorEffect = data->Valid != 0 ? DropEffectCopy : DropEffectNone;
        if (effect is not null)
        {
            *effect = data->CursorEffect;
        }

        if (data->Valid != 0 && WindowFromData(data) is { } window)
        {
            window.DispatchWindowEvent(new WindowEvent(new WindowEvent.DragEntered(
                paths!,
                ClientPosition(window, point))));
        }

        return SOk;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int DragOver(IDropTarget* @this, uint keyState, NativePointL point, uint* effect)
    {
        _ = keyState;
        FileDropHandlerData* data = FromInterface(@this);
        if (effect is not null)
        {
            *effect = data->CursorEffect;
        }

        if (data->Valid != 0 && WindowFromData(data) is { } window)
        {
            window.DispatchWindowEvent(new WindowEvent(new WindowEvent.DragMoved(ClientPosition(window, point))));
        }

        return SOk;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int DragLeave(IDropTarget* @this)
    {
        FileDropHandlerData* data = FromInterface(@this);
        if (data->Valid != 0 && WindowFromData(data) is { } window)
        {
            window.DispatchWindowEvent(new WindowEvent(new WindowEvent.DragLeft(null)));
            data->Valid = 0;
        }

        return SOk;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int Drop(
        IDropTarget* @this,
        IDataObject* dataObject,
        uint keyState,
        NativePointL point,
        uint* effect)
    {
        _ = keyState;
        FileDropHandlerData* data = FromInterface(@this);
        if (effect is not null)
        {
            *effect = data->CursorEffect;
        }

        if (data->Valid != 0 && WindowFromData(data) is { } window)
        {
            IReadOnlyList<string> paths = PathsFromDataObject(dataObject) ?? [];
            window.DispatchWindowEvent(new WindowEvent(new WindowEvent.DragDropped(
                paths,
                ClientPosition(window, point))));
            data->Valid = 0;
        }

        return SOk;
    }

    private static FileDropHandlerData* FromInterface(void* @this)
    {
        return (FileDropHandlerData*)@this;
    }

    private static Window? WindowFromData(FileDropHandlerData* data)
    {
        if (data is null || data->WindowHandle == 0)
        {
            return null;
        }

        return GCHandle.FromIntPtr(data->WindowHandle).Target as Window;
    }

    private static PhysicalPosition<double> ClientPosition(Window window, NativePointL point)
    {
        NativePoint nativePoint = new() { X = point.X, Y = point.Y };
        _ = PInvoke.ScreenToClient(window.Hwnd, ref nativePoint);
        return new PhysicalPosition<double>(nativePoint.X, nativePoint.Y);
    }

    private static IReadOnlyList<string>? PathsFromDataObject(IDataObject* dataObject)
    {
        if (dataObject is null || dataObject->lpVtbl is null)
        {
            return null;
        }

        FORMATETC format = new()
        {
            cfFormat = CfHdrop,
            ptd = 0,
            dwAspect = DvaspectContent,
            lindex = -1,
            tymed = TymedHglobal,
        };
        STGMEDIUM medium = default;
        int hr = dataObject->lpVtbl->GetData(dataObject, &format, &medium);
        if (hr < 0)
        {
            _ = hr == DvEFormatEtc;
            return null;
        }

        try
        {
            nint hDrop = medium.unionmember;
            uint count = PInvoke.DragQueryFileW(hDrop, DragQueryFileCount, null, 0);
            List<string> paths = new(checked((int)count));
            for (uint index = 0; index < count; index++)
            {
                uint length = PInvoke.DragQueryFileW(hDrop, index, null, 0);
                char[] buffer = new char[length + 1];
                fixed (char* bufferPtr = buffer)
                {
                    _ = PInvoke.DragQueryFileW(hDrop, index, bufferPtr, (uint)buffer.Length);
                }

                paths.Add(new string(buffer, 0, checked((int)length)));
            }

            return paths;
        }
        finally
        {
            PInvoke.ReleaseStgMedium(ref medium);
        }
    }

    private static IDropTargetVtbl* CreateDropTargetVtbl()
    {
        IDropTargetVtbl* vtbl = (IDropTargetVtbl*)NativeMemory.AllocZeroed((nuint)sizeof(IDropTargetVtbl));
        vtbl->parent.QueryInterface = &QueryInterface;
        vtbl->parent.AddRef = &AddRef;
        vtbl->parent.Release = &Release;
        vtbl->DragEnter = &DragEnter;
        vtbl->DragOver = &DragOver;
        vtbl->DragLeave = &DragLeave;
        vtbl->Drop = &Drop;
        return vtbl;
    }

    private static void EnsureOleInitialized()
    {
        if (s_oleInitialized)
        {
            return;
        }

        int hr = PInvoke.OleInitialize(nint.Zero);
        if (hr == OleEWrongComObj)
        {
            throw new InvalidOperationException("OleInitialize failed with OLE_E_WRONGCOMPOBJ.");
        }

        if (hr == RpcEChangedMode)
        {
            throw new InvalidOperationException(
                "OleInitialize failed with RPC_E_CHANGED_MODE. Make sure the event loop thread is STA, "
                + "or disable drag and drop support with WindowAttributesExtWindows.WithDragAndDrop(false).");
        }

        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        s_oleInitialized = hr is SOk or SFalse;
    }

    private struct FileDropHandlerData
    {
        public IDropTarget Interface;
        public int RefCount;
        public nint WindowHandle;
        public uint CursorEffect;
        public int Valid;
    }
}
