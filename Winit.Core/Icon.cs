namespace Winit.Core;

public interface IIconProvider : IAsAny;

public sealed class Icon(IIconProvider provider)
{
    public IIconProvider Provider { get; } = provider;

    public static Icon From(RgbaIcon value)
    {
        return new Icon(value);
    }
}

public sealed class RgbaIcon : IIconProvider, IEquatable<RgbaIcon>
{
    private const int PixelSize = 4;

    private readonly byte[] _rgba;

    public RgbaIcon(IEnumerable<byte> rgba, uint width, uint height)
        : this(rgba.ToArray(), width, height)
    {
    }

    public RgbaIcon(byte[] rgba, uint width, uint height)
    {
        if (rgba.Length % PixelSize != 0)
        {
            throw new BadIconException(
                $"The length of the rgba argument ({rgba.Length}) isn't divisible by 4, making it impossible to interpret as 32bpp RGBA pixels.");
        }

        nuint pixelCount = (nuint)(rgba.Length / PixelSize);
        nuint expectedPixelCount = (nuint)width * height;
        if (pixelCount != expectedPixelCount)
        {
            throw new BadIconException(
                $"The specified dimensions ({width}x{height}) don't match the number of pixels supplied by the rgba argument ({pixelCount}). For those dimensions, the expected pixel count is {expectedPixelCount}.");
        }

        _rgba = rgba;
        Width = width;
        Height = height;
    }

    public uint Width { get; }

    public uint Height { get; }

    public ReadOnlyMemory<byte> Buffer => _rgba;

    public Memory<byte> BufferMut => _rgba;

    public bool Equals(RgbaIcon? other)
    {
        return other is not null
            && Width == other.Width
            && Height == other.Height
            && _rgba.AsSpan().SequenceEqual(other._rgba);
    }

    public override bool Equals(object? obj)
    {
        return obj is RgbaIcon other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Width);
        hash.Add(Height);
        foreach (byte value in _rgba)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}

