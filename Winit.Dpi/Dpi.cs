namespace Winit.Dpi;

public static class Dpi
{
    public static bool ValidateScaleFactor(double scaleFactor)
    {
        return double.IsPositive(scaleFactor) && double.IsNormal(scaleFactor);
    }

    internal static void ThrowIfInvalidScaleFactor(double scaleFactor)
    {
        if (!ValidateScaleFactor(scaleFactor))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scaleFactor),
                scaleFactor,
                "Scale factor must be a normal positive double.");
        }
    }
}
