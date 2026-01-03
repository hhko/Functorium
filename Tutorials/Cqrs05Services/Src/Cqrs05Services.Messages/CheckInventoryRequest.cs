namespace Cqrs05Services.Messages;

/// <summary>
/// 재고 확인 요청 메시지 (Request/Reply 패턴)
/// </summary>
public sealed record CheckInventoryRequest(
    Guid ProductId,
    int Quantity);

