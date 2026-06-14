using System.Text;

namespace Winit.X11.Util
{
    internal static class Input
    {
        public const ushort VirtualCorePointer = 2;
    }
}

namespace Winit.X11
{
    internal sealed unsafe partial class XConnection
    {
        private const int TextBufferSize = 1024;

        public string LookupUtf8(nint ic, ref XKeyEvent keyEvent)
        {
            XKeyEvent localEvent = keyEvent;
            nuint keysym = 0;
            int status = 0;
            Span<byte> stackBuffer = stackalloc byte[TextBufferSize];
            int count;
            fixed (byte* stackBufferPtr = stackBuffer)
            {
                count = PInvoke.Xutf8LookupString(
                    ic,
                    &localEvent,
                    (sbyte*)stackBufferPtr,
                    stackBuffer.Length,
                    &keysym,
                    &status);
            }

            if (status != PInvoke.XBufferOverflow)
            {
                return count > 0 ? Encoding.UTF8.GetString(stackBuffer[..count]) : string.Empty;
            }

            byte[] heapBuffer = new byte[count];
            fixed (byte* heapBufferPtr = heapBuffer)
            {
                localEvent = keyEvent;
                int newCount = PInvoke.Xutf8LookupString(
                    ic,
                    &localEvent,
                    (sbyte*)heapBufferPtr,
                    heapBuffer.Length,
                    &keysym,
                    &status);
                return newCount > 0 ? Encoding.UTF8.GetString(heapBuffer, 0, newCount) : string.Empty;
            }
        }
    }
}
