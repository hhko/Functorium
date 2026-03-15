using Functorium.Domains.Errors;
using ECommerce.Domain.AggregateRoots.Products;
using static Functorium.Domains.Errors.DomainErrorType;

namespace ECommerce.Domain.AggregateRoots.Orders;

/// <summary>
/// 주문 라인 엔티티 (Order의 Child Entity)
/// 주문 내 개별 상품 항목을 나타냅니다.
/// OrderLineId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class OrderLine : Entity<OrderLineId>
{
    #region Error Types

    public sealed record InvalidQuantity : DomainErrorType.Custom;

    #endregion

    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal { get; private set; }

    private OrderLine(
        OrderLineId id,
        ProductId productId,
        Quantity quantity,
        Money unitPrice,
        Money lineTotal)
        : base(id)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 받아 주문 라인 생성
    /// 수량은 반드시 1 이상이어야 합니다.
    /// Quantity VO는 0 이상을 허용하지만, 주문 라인 컨텍스트에서는 0 수량이 무의미하므로 추가 검증합니다.
    /// LineTotal = unitPrice * quantity로 자동 계산됩니다.
    /// </summary>
    public static Fin<OrderLine> Create(ProductId productId, Quantity quantity, Money unitPrice)
    {
        if ((int)quantity <= 0)
            return DomainError.For<OrderLine, int>(
                new InvalidQuantity(),
                currentValue: quantity,
                message: "Order line quantity must be greater than 0");

        var lineTotal = unitPrice.Multiply(quantity);
        return new OrderLine(OrderLineId.New(), productId, quantity, unitPrice, lineTotal);
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static OrderLine CreateFromValidated(
        OrderLineId id,
        ProductId productId,
        Quantity quantity,
        Money unitPrice,
        Money lineTotal)
    {
        return new OrderLine(id, productId, quantity, unitPrice, lineTotal);
    }
}
