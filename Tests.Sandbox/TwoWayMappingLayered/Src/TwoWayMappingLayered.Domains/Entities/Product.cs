using TwoWayMappingLayered.Domains.ValueObjects;

namespace TwoWayMappingLayered.Domains.Entities;

/// <summary>
/// 상품 도메인 엔티티
///
/// Two-Way Mapping 전략의 핵심:
/// - 기술적 어노테이션(EF Core, JSON 등) 없음
/// - 순수한 비즈니스 로직만 포함
/// - Value Object로 도메인 개념 표현
///
/// HappyCoders 문서 원문:
/// "In my experience, this variant is the most suitable."
/// (제 경험상, 이 방식이 가장 적합합니다.)
/// </summary>
public sealed class Product
{
    public ProductId Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    private Product(
        ProductId id,
        string name,
        string description,
        Money price,
        int stockQuantity,
        DateTime createdAt,
        DateTime? updatedAt = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 새 상품 생성 (Factory Method)
    /// 비즈니스 규칙 적용
    /// </summary>
    public static Product Create(
        string name,
        string description,
        Money price,
        int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        return new Product(
            ProductId.New(),
            name,
            description,
            price,
            stockQuantity,
            DateTime.UtcNow);
    }

    /// <summary>
    /// 기존 데이터로 상품 복원 (Two-Way Mapping에서 사용)
    /// Adapter → Domain 변환 시 호출
    /// </summary>
    public static Product Reconstitute(
        ProductId id,
        string name,
        string description,
        Money price,
        int stockQuantity,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        return new Product(id, name, description, price, stockQuantity, createdAt, updatedAt);
    }

    /// <summary>
    /// 가격 업데이트 (비즈니스 로직)
    /// Two-Way Mapping: 비즈니스 메서드는 Domain에만 존재
    /// Fluent API 패턴: 메서드 체이닝을 위해 self 반환
    /// </summary>
    public Product UpdatePrice(Money newPrice)
    {
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// 상품 정보 업데이트 (비즈니스 로직)
    /// Fluent API 패턴: LINQ 표현식에서 사용 가능하도록 self 반환
    /// </summary>
    public Product Update(string name, string description, Money price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// 포맷된 가격 문자열
    /// Two-Way Mapping: 비즈니스 표현 로직은 Domain에만 존재
    /// </summary>
    public string FormattedPrice => Price.Formatted;
}
