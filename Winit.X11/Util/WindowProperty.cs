namespace Winit.X11.Util;

internal static class WindowProperty
{
    private const int PropertyBufferSize = 1024;

    public static byte[] GetPropertyBytes(this XConnection xconn, XlibWindow window, Atom property, Atom propertyType)
    {
        return GetProperty(xconn, window, property, propertyType, 8).Bytes;
    }

    public static nuint[] GetProperty32(this XConnection xconn, XlibWindow window, Atom property, Atom propertyType)
    {
        return GetProperty(xconn, window, property, propertyType, 32).Words;
    }

    public static void ChangePropertyBytes(
        this XConnection xconn,
        XlibWindow window,
        Atom property,
        Atom propertyType,
        ReadOnlySpan<byte> value)
    {
        unsafe
        {
            fixed (byte* valuePtr = value)
            {
                _ = PInvoke.XChangeProperty(
                    xconn.Display,
                    window,
                    property,
                    propertyType,
                    8,
                    PInvoke.PropModeReplace,
                    valuePtr,
                    value.Length);
            }
        }
    }

    public static void ChangeProperty32(
        this XConnection xconn,
        XlibWindow window,
        Atom property,
        Atom propertyType,
        ReadOnlySpan<nuint> value)
    {
        unsafe
        {
            fixed (nuint* valuePtr = value)
            {
                _ = PInvoke.XChangeProperty(
                    xconn.Display,
                    window,
                    property,
                    propertyType,
                    32,
                    PInvoke.PropModeReplace,
                    (byte*)valuePtr,
                    value.Length);
            }
        }
    }

    public static void DeleteProperty(this XConnection xconn, XlibWindow window, Atom property)
    {
        _ = PInvoke.XDeleteProperty(xconn.Display, window, property);
    }

    private static unsafe PropertyData GetProperty(
        XConnection xconn,
        XlibWindow window,
        Atom property,
        Atom propertyType,
        int expectedFormat)
    {
        List<byte> bytes = [];
        List<nuint> words = [];
        nint offset = 0;

        while (true)
        {
            Atom actualType = Atom.None;
            int actualFormat = 0;
            nuint itemCount = 0;
            nuint bytesAfter = 0;
            byte* propertyData = null;

            int status = PInvoke.XGetWindowProperty(
                xconn.Display,
                window,
                property,
                offset,
                PropertyBufferSize,
                0,
                propertyType,
                &actualType,
                &actualFormat,
                &itemCount,
                &bytesAfter,
                &propertyData);

            try
            {
                if (status != 0 || actualType.IsNone)
                {
                    return new PropertyData([.. bytes], [.. words]);
                }

                if (actualType != propertyType)
                {
                    throw new GetPropertyException(actualType);
                }

                if (actualFormat != expectedFormat)
                {
                    throw new GetPropertyException(actualFormat);
                }

                if (propertyData is not null && itemCount > 0)
                {
                    if (actualFormat == 8)
                    {
                        ReadOnlySpan<byte> chunk = new(propertyData, checked((int)itemCount));
                        bytes.AddRange(chunk);
                    }
                    else if (actualFormat == 32)
                    {
                        ReadOnlySpan<nuint> chunk = new((nuint*)propertyData, checked((int)itemCount));
                        words.AddRange(chunk);
                    }
                }

                if (bytesAfter == 0)
                {
                    return new PropertyData([.. bytes], [.. words]);
                }
            }
            finally
            {
                if (propertyData is not null)
                {
                    _ = PInvoke.XFree((nint)propertyData);
                }
            }

            offset += PropertyBufferSize;
        }
    }

    private readonly record struct PropertyData(byte[] Bytes, nuint[] Words);
}

internal sealed class GetPropertyException : Exception
{
    public GetPropertyException(Atom actualType)
        : base($"Window property type mismatch: actual atom {actualType.Value}.")
    {
        ActualType = actualType;
    }

    public GetPropertyException(int actualFormat)
        : base($"Window property format mismatch: actual format {actualFormat}.")
    {
        ActualFormat = actualFormat;
    }

    public Atom? ActualType { get; }

    public int? ActualFormat { get; }

    public bool IsActualPropertyType(Atom atom)
    {
        return ActualType == atom;
    }
}
