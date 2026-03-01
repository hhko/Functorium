namespace Functorium.Applications.Queries;

/// <summary>
/// Keyset(Cursor) 기반 페이지네이션 결과.
/// </summary>
public sealed record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    string? PrevCursor,
    bool HasMore);
