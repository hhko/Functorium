using System.Collections;

namespace Functorium.Abstractions.Utilities;

public static class IEnumerableUtilities
{
    public static string Join<TValue>(this IEnumerable<TValue> items, char separator)
    {
        return string.Join(separator, items);
    }

    public static string Join<TValue>(this IEnumerable<TValue> items, string separator)
    {
        return string.Join(separator, items);
    }

    public static bool Any(this IEnumerable source)
    {
        foreach (object? _ in source)
        {
            return true;
        }

        return false;
    }

    public static bool IsEmpty(this IEnumerable source)
    {
        return source.Any() is false;
    }
}
