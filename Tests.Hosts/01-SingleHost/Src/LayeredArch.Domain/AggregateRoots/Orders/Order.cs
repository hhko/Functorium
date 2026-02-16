using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Domain.AggregateRoots.Orders;

/// <summary>
/// 주문 도메인 모델 (Aggregate Root)
/// OrderId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// 공유 VO(Money, Quantity)와 교차 Aggregate 참조(ProductId)를 사용합니다.
/// </summary>
[GenerateEntityId]
public sealed class Order : AggregateRoot<OrderId>, IAuditable
{
    #region Domain Events

    /// <summary>
    /// 주문 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(
        OrderId OrderId,
        ProductId ProductId,
        Quantity Quantity,
        Money TotalAmount) : DomainEvent;

    #endregion

    // 교차 Aggregate 참조 (Product의 ID를 값으로 참조)
    public ProductId ProductId { get; private set; }

    // Value Object 속성
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private Order(
        OrderId id,
        ProductId productId,
        Quantity quantity,
        Money unitPrice,
        Money totalAmount,
        ShippingAddress shippingAddress)
        : base(id)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// Application Layer에서 IProductCatalog로 상품 검증 후 호출
    /// TotalAmount = unitPrice * quantity로 계산
    /// </summary>
    public static Order Create(
        ProductId productId,
        Quantity quantity,
        Money unitPrice,
        ShippingAddress shippingAddress)
    {
        var totalAmount = unitPrice.Multiply(quantity);
        var order = new Order(OrderId.New(), productId, quantity, unitPrice, totalAmount, shippingAddress);
        order.AddDomainEvent(new CreatedEvent(order.Id, productId, quantity, totalAmount));
        return order;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Order CreateFromValidated(
        OrderId id,
        ProductId productId,
        Quantity quantity,
        Money unitPrice,
        Money totalAmount,
        ShippingAddress shippingAddress,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        return new Order(id, productId, quantity, unitPrice, totalAmount, shippingAddress)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
