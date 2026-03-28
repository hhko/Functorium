using Functorium.Domains.Entities;
using LanguageExt;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain;

/// <summary>
/// 주문 도메인 모델
/// Entity<OrderId>를 상속하여 Identity 패턴 적용
/// </summary>
[GenerateEntityId]
public sealed class Order : Entity<OrderId>
{
    public Guid ProductId { get; private set; }
    public Quantity Quantity { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    // EF Core용 기본 생성자
    private Order() { }

    private Order(OrderId id, Guid productId, Quantity quantity, DateTime createdAt)
        : base(id)
    {
        ProductId = productId;
        Quantity = quantity;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// 주문 생성 - 새로운 OrderId 자동 생성
    /// </summary>
    public static Fin<Order> Create(Guid productId, Quantity quantity) =>
        Fin.Succ(new Order(OrderId.New(), productId, quantity, DateTime.UtcNow));

    /// <summary>
    /// 검증된 값으로 주문 생성 - 기존 OrderId 사용 (재구성용)
    /// </summary>
    public static Order CreateFromValidated(OrderId id, Guid productId, Quantity quantity, DateTime createdAt) =>
        new(id, productId, quantity, createdAt);
}

