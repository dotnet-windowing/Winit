namespace Winit.Core;

public interface IAsAny
{
    object AsAny()
    {
        return this;
    }
}

public static class AsAnyExtensions
{
    public static T? Cast<T>(this IAsAny value)
        where T : class
    {
        return value.AsAny() as T;
    }

    public static bool TryCast<T>(this IAsAny value, out T? result)
        where T : class
    {
        result = value.AsAny() as T;
        return result is not null;
    }
}

