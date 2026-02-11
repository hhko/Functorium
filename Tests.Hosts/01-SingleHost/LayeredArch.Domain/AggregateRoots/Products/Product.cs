using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Domain.AggregateRoots.Products;

/// <summary>
/// 상품 도메인 모델 (Aggregate Root)
/// ProductId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    #region Domain Events

    /// <summary>
    /// 상품 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(ProductId ProductId, ProductName Name, Money Price) : DomainEvent;

    /// <summary>
    /// 상품 업데이트 이벤트
    /// </summary>
    public sealed record UpdatedEvent(ProductId ProductId, ProductName Name, Money OldPrice, Money NewPrice) : DomainEvent;

    /// <summary>
    /// 재고 차감 이벤트
    /// </summary>
    public sealed record StockDeductedEvent(ProductId ProductId, Quantity Quantity) : DomainEvent;

    #endregion

    // Value Object 속성
    public ProductName Name { get; private set; }
    public ProductDescription Description { get; private set; }
    public Money Price { get; private set; }
    public Quantity StockQuantity { get; private set; }

    // Tags 컬렉션 (SharedKernel Entity)
    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

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
        ProductDescription description,
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
        ProductDescription description,
        Money price,
        Quantity stockQuantity)
    {
        var product = new Product(ProductId.New(), name, description, price, stockQuantity);
        product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
        return product;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Product CreateFromValidated(
        ProductId id,
        ProductName name,
        ProductDescription description,
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
        ProductDescription description,
        Money price,
        Quantity stockQuantity)
    {
        var oldPrice = Price;

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UpdatedEvent(Id, name, oldPrice, price));

        return this;
    }

    /// <summary>
    /// 재고가 지정된 임계값 미만인지 확인합니다.
    /// </summary>
    public bool HasLowStock(Quantity threshold) => StockQuantity < threshold;

    /// <summary>
    /// 재고를 차감합니다.
    /// </summary>
    public Fin<Unit> DeductStock(Quantity quantity)
    {
        if (quantity > StockQuantity)
            return DomainError.For<Product, int>(
                new Custom("InsufficientStock"),
                currentValue: StockQuantity,
                message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

        StockQuantity = StockQuantity.Subtract(quantity);
        AddDomainEvent(new StockDeductedEvent(Id, quantity));
        return unit;
    }

    /// <summary>
    /// 태그를 추가합니다. (SharedKernel 이벤트 발행)
    /// </summary>
    public Product AddTag(Tag tag)
    {
        if (_tags.Any(t => t.Id == tag.Id))
            return this;

        _tags.Add(tag);
        AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));
        return this;
    }

    /// <summary>
    /// 태그를 제거합니다. (SharedKernel 이벤트 발행)
    /// </summary>
    public Product RemoveTag(TagId tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is null)
            return this;

        _tags.Remove(tag);
        AddDomainEvent(new TagRemovedEvent(tagId));
        return this;
    }
}
