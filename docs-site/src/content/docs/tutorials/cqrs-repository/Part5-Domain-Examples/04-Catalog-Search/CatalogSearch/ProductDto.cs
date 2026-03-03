namespace CatalogSearch;

/// <summary>
/// 카탈로그 상품 조회용 DTO.
/// </summary>
public sealed record ProductDto(
    string Id,
    string Name,
    string Category,
    decimal Price,
    int Stock);
