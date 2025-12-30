namespace ResultPattern.Demo.Domain;

/// <summary>
/// Product 도메인 엔티티
/// </summary>
public sealed record Product(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt = null);
