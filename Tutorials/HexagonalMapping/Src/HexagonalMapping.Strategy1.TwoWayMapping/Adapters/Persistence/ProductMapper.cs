using HexagonalMapping.Domain.Entities;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Adapters.Persistence;

/// <summary>
/// 양방향 매퍼: Domain ↔ Adapter 모델 변환을 담당합니다.
/// Two-Way Mapping 전략의 핵심 구성요소입니다.
/// </summary>
public static class ProductMapper
{
    /// <summary>
    /// Domain → Adapter (저장용)
    /// </summary>
    public static ProductEntity ToEntity(Product product) => new()
    {
        Id = product.Id.Value,
        Name = product.Name,
        Price = product.Price.Amount,
        Currency = product.Price.Currency,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Adapter → Domain (조회용)
    /// </summary>
    public static Product ToDomain(ProductEntity entity) =>
        Product.Reconstitute(
            entity.Id,
            entity.Name,
            entity.Price,
            entity.Currency);

    /// <summary>
    /// 기존 엔티티 업데이트 (Domain → Adapter)
    /// </summary>
    public static void UpdateEntity(ProductEntity entity, Product product)
    {
        entity.Name = product.Name;
        entity.Price = product.Price.Amount;
        entity.Currency = product.Price.Currency;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
