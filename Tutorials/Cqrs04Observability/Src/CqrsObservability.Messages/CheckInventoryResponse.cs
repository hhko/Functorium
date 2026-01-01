namespace CqrsObservability.Messages;

/// <summary>
/// 재고 확인 응답 메시지 (Request/Reply 패턴)
/// </summary>
public sealed record CheckInventoryResponse(
    Guid ProductId,
    bool IsAvailable,
    int AvailableQuantity);

