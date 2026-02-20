using Functorium.Domains.Errors;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Domain.AggregateRoots.Orders;

/// <summary>
/// 주문 도메인 모델 (Aggregate Root)
/// OrderId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// 다중 주문 라인(OrderLine)을 포함하며, 교차 Aggregate 참조(CustomerId)를 사용합니다.
/// </summary>
[GenerateEntityId]
public sealed class Order : AggregateRoot<OrderId>, IAuditable
{
    #region Domain Events

    /// <summary>
    /// 주문 라인 정보 (이벤트 페이로드용)
    /// </summary>
    public sealed record OrderLineInfo(
        ProductId ProductId,
        Quantity Quantity,
        Money UnitPrice,
        Money LineTotal);

    /// <summary>
    /// 주문 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(
        OrderId OrderId,
        CustomerId CustomerId,
        Seq<OrderLineInfo> OrderLines,
        Money TotalAmount) : DomainEvent;

    /// <summary>
    /// 주문 확인 이벤트
    /// </summary>
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;

    /// <summary>
    /// 주문 배송 이벤트
    /// </summary>
    public sealed record ShippedEvent(OrderId OrderId) : DomainEvent;

    /// <summary>
    /// 주문 배달 완료 이벤트
    /// </summary>
    public sealed record DeliveredEvent(OrderId OrderId) : DomainEvent;

    /// <summary>
    /// 주문 취소 이벤트
    /// </summary>
    public sealed record CancelledEvent(OrderId OrderId) : DomainEvent;

    #endregion

    // 교차 Aggregate 참조 (Customer의 ID를 값으로 참조)
    public CustomerId CustomerId { get; private set; }

    // 주문 라인 컬렉션
    private readonly List<OrderLine> _orderLines = [];
    public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();

    // Value Object 속성
    public Money TotalAmount { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private Order(
        OrderId id,
        CustomerId customerId,
        IEnumerable<OrderLine> orderLines,
        Money totalAmount,
        ShippingAddress shippingAddress)
        : base(id)
    {
        CustomerId = customerId;
        _orderLines.AddRange(orderLines);
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 주문 라인 목록과 배송 주소를 받아 주문을 생성합니다.
    /// 불변 조건: 주문 라인은 최소 1개 이상 필요합니다.
    /// TotalAmount = 모든 LineTotal의 합계로 자동 계산됩니다.
    /// </summary>
    public static Fin<Order> Create(
        CustomerId customerId,
        IEnumerable<OrderLine> orderLines,
        ShippingAddress shippingAddress)
    {
        var lines = orderLines.ToList();
        if (lines.Count == 0)
            return DomainError.For<Order, int>(
                new Custom("EmptyOrderLines"),
                currentValue: 0,
                message: "Order must contain at least one order line");

        var totalAmount = Money.CreateFromValidated(lines.Sum(l => (decimal)l.LineTotal));
        var order = new Order(OrderId.New(), customerId, lines, totalAmount, shippingAddress);

        var lineInfos = Seq(lines.Select(l => new OrderLineInfo(l.ProductId, l.Quantity, l.UnitPrice, l.LineTotal)));
        order.AddDomainEvent(new CreatedEvent(order.Id, customerId, lineInfos, totalAmount));
        return order;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Order CreateFromValidated(
        OrderId id,
        CustomerId customerId,
        IEnumerable<OrderLine> orderLines,
        Money totalAmount,
        ShippingAddress shippingAddress,
        OrderStatus status,
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        return new Order(id, customerId, orderLines, totalAmount, shippingAddress)
        {
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// 주문을 확인합니다. (Pending → Confirmed)
    /// </summary>
    public Fin<Unit> Confirm() => TransitionTo(OrderStatus.Confirmed, new ConfirmedEvent(Id));

    /// <summary>
    /// 주문을 배송합니다. (Confirmed → Shipped)
    /// </summary>
    public Fin<Unit> Ship() => TransitionTo(OrderStatus.Shipped, new ShippedEvent(Id));

    /// <summary>
    /// 주문을 배달 완료합니다. (Shipped → Delivered)
    /// </summary>
    public Fin<Unit> Deliver() => TransitionTo(OrderStatus.Delivered, new DeliveredEvent(Id));

    /// <summary>
    /// 주문을 취소합니다. (Pending/Confirmed → Cancelled)
    /// </summary>
    public Fin<Unit> Cancel() => TransitionTo(OrderStatus.Cancelled, new CancelledEvent(Id));

    private Fin<Unit> TransitionTo(OrderStatus target, DomainEvent domainEvent)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<Order, string, string>(
                new Custom("InvalidOrderStatusTransition"),
                value1: Status,
                value2: target,
                message: $"Cannot transition from '{Status}' to '{target}'");

        Status = target;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(domainEvent);
        return unit;
    }
}
