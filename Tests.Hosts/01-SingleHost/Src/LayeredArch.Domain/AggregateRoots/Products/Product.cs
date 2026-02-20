using LayeredArch.Domain.AggregateRoots.Tags;

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
    /// 태그 할당 이벤트
    /// </summary>
    public sealed record TagAssignedEvent(ProductId ProductId, TagId TagId) : DomainEvent;

    /// <summary>
    /// 태그 해제 이벤트
    /// </summary>
    public sealed record TagUnassignedEvent(ProductId ProductId, TagId TagId) : DomainEvent;

    #endregion

    // Value Object 속성
    public ProductName Name { get; private set; }
    public ProductDescription Description { get; private set; }
    public Money Price { get; private set; }

    // TagId 참조 컬렉션 (Tag는 독립 Aggregate Root)
    private readonly List<TagId> _tagIds = [];
    public IReadOnlyList<TagId> TagIds => _tagIds.AsReadOnly();

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private Product(
        ProductId id,
        ProductName name,
        ProductDescription description,
        Money price)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// Application Layer에서 VO 생성 후 호출
    /// </summary>
    public static Product Create(
        ProductName name,
        ProductDescription description,
        Money price)
    {
        var product = new Product(ProductId.New(), name, description, price);
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
        IEnumerable<TagId> tagIds,
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        var product = new Product(id, name, description, price)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        product._tagIds.AddRange(tagIds);
        return product;
    }

    /// <summary>
    /// 상품 정보를 업데이트합니다.
    /// </summary>
    public Product Update(
        ProductName name,
        ProductDescription description,
        Money price)
    {
        var oldPrice = Price;

        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UpdatedEvent(Id, name, oldPrice, price));

        return this;
    }

    /// <summary>
    /// 태그를 할당합니다. (멱등성 보장)
    /// </summary>
    public Product AssignTag(TagId tagId)
    {
        if (_tagIds.Any(t => t == tagId))
            return this;

        _tagIds.Add(tagId);
        AddDomainEvent(new TagAssignedEvent(Id, tagId));
        return this;
    }

    /// <summary>
    /// 태그를 해제합니다. (멱등성 보장)
    /// </summary>
    public Product UnassignTag(TagId tagId)
    {
        if (!_tagIds.Remove(tagId))
            return this;

        AddDomainEvent(new TagUnassignedEvent(Id, tagId));
        return this;
    }
}
