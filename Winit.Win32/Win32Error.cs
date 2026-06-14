using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;

namespace Winit.Win32;

internal static class Win32Error
{
    public static OsRequestException Request(
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        return new OsRequestException(Os(file, line));
    }

    public static EventLoopOsException EventLoop(
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        return new EventLoopOsException(Os(file, line));
    }

    public static OsException Os(
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        int error = Marshal.GetLastPInvokeError();
        return new OsException(file, line, new Win32Exception(error));
    }
}
