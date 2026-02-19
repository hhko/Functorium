using Ardalis.SmartEnum;

namespace Functorium.Applications.Queries;

/// <summary>
/// 정렬 방향.
/// </summary>
public sealed class SortDirection : SmartEnum<SortDirection, string>
{
    public static readonly SortDirection Ascending = new(nameof(Ascending), "asc");
    public static readonly SortDirection Descending = new(nameof(Descending), "desc");

    private SortDirection(string name, string value) : base(name, value) { }

    /// <summary>
    /// 대소문자 무시하여 "asc"/"desc" 문자열을 파싱합니다.
    /// null 또는 빈 문자열이면 Ascending을 반환합니다.
    /// </summary>
    public static SortDirection Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return Ascending;

        return TryFromValue(value.ToLowerInvariant(), out var result)
            ? result
            : throw new SmartEnumNotFoundException($"No {nameof(SortDirection)} with Value '{value}' found.");
    }
}
