namespace Cqrs02Pipeline.Demo.Domain;

/// <summary>
/// 상품 도메인 모델
/// </summary>
public sealed record class Product(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt = null);
