using System.Collections;

namespace Functorium.Abstractions.Utilities;

public static class IEnumerableUtilities
{
    extension(IEnumerable source)
    {
        public bool HasAny
        {
            get
            {
                foreach (object? _ in source)
                    return true;
                return false;
            }
        }

        public bool IsEmpty => !source.HasAny;
    }

    public static string Join<TValue>(this IEnumerable<TValue> items, char separator)
    {
        return string.Join(separator, items);
    }

    public static string Join<TValue>(this IEnumerable<TValue> items, string separator)
    {
        return string.Join(separator, items);
    }
}
