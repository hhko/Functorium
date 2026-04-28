namespace InventoryManagement;

/// <summary>
/// 상품 조회용 DTO.
/// </summary>
public sealed record ProductDto(
    string Id,
    string Name,
    decimal Price,
    int Stock);
