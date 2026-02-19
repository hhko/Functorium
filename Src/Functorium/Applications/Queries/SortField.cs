namespace Functorium.Applications.Queries;

/// <summary>
/// 정렬 필드와 방향.
/// </summary>
public sealed record SortField(string FieldName, SortDirection Direction);
