using LayeredArch.Domain.ValueObjects;

namespace LayeredArch.Domain.Entities;

/// <summary>
/// 상품 도메인 모델 (Aggregate Root)
/// ProductId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    // Value Object 속성
    public ProductName Name { get; private set; }
    public Money Price { get; private set; }
    public Quantity StockQuantity { get; private set; }

    // VO 없는 필드 (단순 문자열)
    public string Description { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // ORM용 기본 생성자
#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    // 내부 생성자: 이미 검증된 VO를 받음
    private Product(
        ProductId id,
        ProductName name,
        string description,
        Money price,
        Quantity stockQuantity)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// Application Layer에서 VO 생성 후 호출
    /// </summary>
    public static Product Create(
        ProductName name,
        string description,
        Money price,
        Quantity stockQuantity)
    {
        var product = new Product(ProductId.New(), name, description, price, stockQuantity);
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name, price));
        return product;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Product CreateFromValidated(
        ProductId id,
        ProductName name,
        string description,
        Money price,
        Quantity stockQuantity,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        return new Product(id, name, description, price, stockQuantity)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// 상품 정보를 업데이트합니다.
    /// </summary>
    public Product Update(
        ProductName name,
        string description,
        Money price,
        Quantity stockQuantity)
    {
        var oldPrice = Price;

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedEvent(Id, name, oldPrice, price));

        return this;
    }
}

/// <summary>
/// 상품 생성 이벤트
/// </summary>
public sealed record ProductCreatedEvent(ProductId ProductId, ProductName Name, Money Price) : DomainEvent;

/// <summary>
/// 상품 업데이트 이벤트
/// </summary>
public sealed record ProductUpdatedEvent(ProductId ProductId, ProductName Name, Money OldPrice, Money NewPrice) : DomainEvent;
