namespace OrderService.Domain;

/// <summary>
/// 주문 도메인 모델
/// </summary>
public sealed record class Order
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 주문 생성자 - 유효성 검증 포함
    /// </summary>
    public Order(Guid id, Guid productId, int quantity, DateTime createdAt)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("주문 수량은 0보다 커야 합니다.", nameof(quantity));
        }

        Id = id;
        ProductId = productId;
        Quantity = quantity;
        CreatedAt = createdAt;
    }
}

