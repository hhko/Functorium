namespace LayeredArch.Domain.Entities;

/// <summary>
/// 상품 도메인 모델 (Aggregate Root)
/// ProductId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Product(ProductId id, string name, string description, decimal price, int stockQuantity)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 새 상품을 생성합니다.
    /// </summary>
    public static Product Create(string name, string description, decimal price, int stockQuantity)
    {
        var product = new Product(ProductId.New(), name, description, price, stockQuantity);
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name, price));
        return product;
    }

    /// <summary>
    /// 이미 검증된 데이터로 상품을 생성합니다 (Repository/ORM 복원용).
    /// </summary>
    public static Product CreateFromValidated(
        ProductId id,
        string name,
        string description,
        decimal price,
        int stockQuantity,
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
    public Product Update(string name, string description, decimal price, int stockQuantity)
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
public sealed record ProductCreatedEvent(ProductId ProductId, string Name, decimal Price) : DomainEvent;

/// <summary>
/// 상품 업데이트 이벤트
/// </summary>
public sealed record ProductUpdatedEvent(ProductId ProductId, string Name, decimal OldPrice, decimal NewPrice) : DomainEvent;
