namespace CqrsObservability.Messages;

/// <summary>
/// 주문 완료 이벤트 메시지 (Fire and Forget 패턴)
/// </summary>
public sealed record OrderCompletedEvent(
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    DateTime CompletedAt);

