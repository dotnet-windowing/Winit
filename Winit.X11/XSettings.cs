using Winit.X11.Util;

namespace Winit.X11;

internal static class XSettings
{
    private const string DpiName = "Xft/DPI";
    private const double DpiMultiplier = 1024.0;
    private const byte LittleEndian = (byte)'l';
    private const byte BigEndian = (byte)'B';
    private static readonly byte[] s_dpiNameBytes = System.Text.Encoding.ASCII.GetBytes(DpiName);

    public static double? GetXftDpi(this XConnection xconn)
    {
        if (xconn.XSettingsScreen is { } xsettingsScreen)
        {
            try
            {
                double? dpi = xconn.XSettingsDpi(xsettingsScreen);
                if (dpi is not null)
                {
                    return dpi;
                }
            }
            catch (XSettingsParseException)
            {
            }
            catch (GetPropertyException)
            {
            }
        }

        return GetXftDpiFromResourceManager(xconn.ResourceManagerString);
    }

    private static double? XSettingsDpi(this XConnection xconn, Atom xsettingsScreen)
    {
        XlibWindow owner = PInvoke.XGetSelectionOwner(xconn.Display, xsettingsScreen);
        if (owner.Value == 0)
        {
            return null;
        }

        byte[] data = xconn.GetPropertyBytes(
            owner,
            xconn.Atoms[AtomName.XSettingsSettings],
            xconn.Atoms[AtomName.XSettingsSettings]);
        return ReadDpiSetting(data);
    }

    private static double? ReadDpiSetting(ReadOnlySpan<byte> data)
    {
        Parser parser = new(data);
        int totalSettings = parser.Int32();
        if (totalSettings < 0)
        {
            throw new XSettingsParseException("Negative XSettings count.");
        }

        for (int i = 0; i < totalSettings; i++)
        {
            SettingType type = (SettingType)parser.Int8();
            parser.Advance(1);

            short nameLength = parser.Int16();
            if (nameLength < 0)
            {
                throw new XSettingsParseException("Negative XSettings name length.");
            }

            ReadOnlySpan<byte> name = parser.Advance(nameLength);
            parser.Pad(name.Length, 4);
            parser.Advance(4);

            bool isDpi = name.SequenceEqual(s_dpiNameBytes);
            switch (type)
            {
                case SettingType.Integer:
                {
                    int value = parser.Int32();
                    if (isDpi)
                    {
                        return value / DpiMultiplier;
                    }

                    break;
                }

                case SettingType.String:
                {
                    int dataLength = parser.Int32();
                    if (dataLength < 0)
                    {
                        throw new XSettingsParseException("Negative XSettings string length.");
                    }

                    parser.Advance(dataLength);
                    parser.Pad(dataLength, 4);
                    break;
                }

                case SettingType.Color:
                    parser.Advance(8);
                    break;

                default:
                    throw new XSettingsParseException($"Invalid XSettings type {(int)type}.");
            }
        }

        return null;
    }

    private static double? GetXftDpiFromResourceManager(string? resourceManager)
    {
        if (string.IsNullOrWhiteSpace(resourceManager))
        {
            return null;
        }

        foreach (string line in resourceManager.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int separator = line.IndexOf(':');
            if (separator < 0)
            {
                continue;
            }

            string key = line[..separator].Trim();
            if (!key.Equals("Xft.dpi", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = line[(separator + 1)..].Trim();
            if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double dpi))
            {
                return dpi;
            }
        }

        return null;
    }

    private enum SettingType
    {
        Integer = 0,
        String = 1,
        Color = 2,
    }

    private ref struct Parser
    {
        private ReadOnlySpan<byte> _bytes;
        private readonly bool _littleEndian;

        public Parser(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8)
            {
                throw new XSettingsParseException("XSettings header is too short.");
            }

            _littleEndian = bytes[0] switch
            {
                LittleEndian => true,
                BigEndian => false,
                _ => BitConverter.IsLittleEndian,
            };
            _bytes = bytes[8..];
        }

        public sbyte Int8()
        {
            return unchecked((sbyte)Advance(1)[0]);
        }

        public short Int16()
        {
            ReadOnlySpan<byte> bytes = Advance(2);
            return _littleEndian
                ? System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(bytes)
                : System.Buffers.Binary.BinaryPrimitives.ReadInt16BigEndian(bytes);
        }

        public int Int32()
        {
            ReadOnlySpan<byte> bytes = Advance(4);
            return _littleEndian
                ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes)
                : System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes);
        }

        public ReadOnlySpan<byte> Advance(int count)
        {
            if (count < 0 || count > _bytes.Length)
            {
                throw new XSettingsParseException($"XSettings parser ran out of bytes; wanted {count}, found {_bytes.Length}.");
            }

            ReadOnlySpan<byte> result = _bytes[..count];
            _bytes = _bytes[count..];
            return result;
        }

        public void Pad(int size, int alignment)
        {
            Advance((alignment - (size % alignment)) % alignment);
        }
    }
}

internal sealed class XSettingsParseException(string message) : Exception(message)
{
}
