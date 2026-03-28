using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace InventoryService.Domain;

/// <summary>
/// 재고 항목 도메인 모델
/// </summary>
public sealed record class InventoryItem(
    Guid Id,
    Guid ProductId,
    int Quantity,
    int ReservedQuantity)
{
    /// <summary>
    /// 사용 가능한 재고 수량
    /// </summary>
    public int AvailableQuantity => Quantity - ReservedQuantity;

    /// <summary>
    /// 재고 예약
    /// </summary>
    /// <param name="amount">예약할 수량</param>
    /// <returns>예약 후 업데이트된 InventoryItem 또는 에러</returns>
    public Fin<InventoryItem> ReserveQuantity(int amount)
    {
        if (amount <= 0)
        {
            return Fin.Fail<InventoryItem>(Error.New("예약 수량은 0보다 커야 합니다."));
        }

        if (amount > AvailableQuantity)
        {
            return Fin.Fail<InventoryItem>(Error.New($"재고가 부족합니다. 사용 가능한 수량: {AvailableQuantity}, 요청 수량: {amount}"));
        }

        return Fin.Succ(this with { ReservedQuantity = ReservedQuantity + amount });
    }
}

