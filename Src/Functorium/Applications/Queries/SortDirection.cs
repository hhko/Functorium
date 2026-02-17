namespace Functorium.Applications.Queries;

/// <summary>
/// 정렬 방향.
/// </summary>
public enum SortDirection { Ascending, Descending }

/// <summary>
/// 정렬 필드와 방향.
/// </summary>
public sealed record SortField(string FieldName, SortDirection Direction = SortDirection.Ascending);
