using TwoWayMappingLayered.Adapters.Persistence.Entities;
using TwoWayMappingLayered.Domains.Entities;
using TwoWayMappingLayered.Domains.ValueObjects;

namespace TwoWayMappingLayered.Adapters.Persistence.Mappers;

/// <summary>
/// 양방향 매퍼: Domain ↔ Adapter 모델 변환
///
/// Two-Way Mapping 전략의 핵심 구성요소
///
/// HappyCoders 문서 원문:
/// "In my experience, this variant is the most suitable."
/// (제 경험상, 이 방식이 가장 적합합니다.)
///
/// 변환 방향:
/// 1. ToEntity (Domain → Adapter): 저장 시 사용
/// 2. ToDomain (Adapter → Domain): 조회 시 사용
/// </summary>
public static class ProductMapper
{
    /// <summary>
    /// Domain → Adapter (저장용)
    ///
    /// Product(Domain) → ProductEntity(Persistence)
    /// Value Object를 primitive 값으로 분해
    /// </summary>
    public static ProductEntity ToEntity(Product product) => new()
    {
        Id = (Guid)product.Id,  // implicit operator를 통한 변환
        Name = product.Name,
        Description = product.Description,
        Price = product.Price.Amount,
        Currency = product.Price.Currency,
        StockQuantity = product.StockQuantity,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };

    /// <summary>
    /// Adapter → Domain (조회용)
    ///
    /// ProductEntity(Persistence) → Product(Domain)
    /// primitive 값을 Value Object로 재구성
    /// </summary>
    public static Product ToDomain(ProductEntity entity) =>
        Product.Reconstitute(
            ProductId.FromValue(entity.Id),
            entity.Name,
            entity.Description,
            Money.FromValues(entity.Price, entity.Currency),
            entity.StockQuantity,
            entity.CreatedAt,
            entity.UpdatedAt);

    /// <summary>
    /// 기존 엔티티 업데이트 (Domain → Adapter)
    ///
    /// 업데이트 시 새 엔티티를 생성하지 않고 기존 엔티티의 값만 변경
    /// EF Core의 Change Tracking을 활용
    /// </summary>
    public static void UpdateEntity(ProductEntity entity, Product product)
    {
        entity.Name = product.Name;
        entity.Description = product.Description;
        entity.Price = product.Price.Amount;
        entity.Currency = product.Price.Currency;
        entity.StockQuantity = product.StockQuantity;
        entity.UpdatedAt = product.UpdatedAt;
    }
}
