namespace Functorium.Applications.Queries;

/// <summary>
/// Keyset(Cursor) 기반 페이지네이션 요청.
/// Offset 기반 대비 deep page에서 O(1) 성능을 제공합니다.
/// </summary>
public sealed record CursorPageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 10_000;

    /// <summary>이 커서 이후의 항목을 조회 (forward pagination)</summary>
    public string? After { get; }

    /// <summary>이 커서 이전의 항목을 조회 (backward pagination)</summary>
    public string? Before { get; }

    public int PageSize { get; }

    public CursorPageRequest(string? after = null, string? before = null, int pageSize = DefaultPageSize)
    {
        After = after;
        Before = before;
        PageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }
}
