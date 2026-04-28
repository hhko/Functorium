namespace PaginationAndSorting;

/// <summary>
/// 페이지네이션과 정렬 데모에 사용할 Product DTO.
/// </summary>
public sealed record ProductDto(
    string Id,
    string Name,
    decimal Price,
    string Category);
