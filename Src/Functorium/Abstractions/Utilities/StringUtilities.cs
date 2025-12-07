namespace Functorium.Abstractions.Utilities;

public static class StringUtilities
{
    public static bool Empty(this string? str)
        => string.IsNullOrWhiteSpace(str);

    public static bool NotEmpty(this string? str)
        => !string.IsNullOrWhiteSpace(str);

    public static bool NotContains(this string str, string subStr, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        => !str.Contains(subStr, stringComparison);

    public static bool NotEquals(this string str, string otherStr, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        => !str.Equals(otherStr, StringComparison.OrdinalIgnoreCase);

    public static int ConvertToInt(this string str)
        => Convert.ToInt32(str);

    public static double ConvertToDouble(this string str)
        => Convert.ToDouble(str);

    public static bool TryConvertToDouble(this string str)
        => Double.TryParse(str, out double ret);

    public static string Replace(this string str, string[] oldStrList, string newStr)
        => oldStrList.Aggregate(str, (cur, oldStr) => cur.Replace(oldStr, newStr));
}
