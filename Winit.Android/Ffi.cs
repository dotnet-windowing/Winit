using System.Runtime.InteropServices;

namespace Winit.Android;

internal static partial class Ffi
{
    [LibraryImport("android")]
    internal static partial nint ANativeWindow_fromSurface(nint env, nint surface);

    [LibraryImport("android")]
    internal static partial void ANativeWindow_release(nint window);

    [LibraryImport("android")]
    internal static partial int ANativeWindow_getWidth(nint window);

    [LibraryImport("android")]
    internal static partial int ANativeWindow_getHeight(nint window);
}
