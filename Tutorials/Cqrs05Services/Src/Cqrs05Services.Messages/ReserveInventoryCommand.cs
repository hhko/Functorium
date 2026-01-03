namespace Cqrs05Services.Messages;

/// <summary>
/// 재고 예약 명령 메시지 (Fire and Forget 패턴)
/// </summary>
public sealed record ReserveInventoryCommand(
    Guid OrderId,
    Guid ProductId,
    int Quantity);

