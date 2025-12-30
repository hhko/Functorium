using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;

namespace InventoryService.Domain;

/// <summary>
/// 재고 리포지토리 인터페이스
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 상속
/// </summary>
public interface IInventoryRepository : IAdapter
{
    /// <summary>
    /// 재고 항목 생성
    /// </summary>
    FinT<IO, InventoryItem> Create(InventoryItem item);

    /// <summary>
    /// 상품 ID로 재고 항목 조회.
    /// 재고 항목이 없으면 실패(Error)를 반환합니다.
    /// </summary>
    FinT<IO, InventoryItem> GetByProductId(Guid productId);

    /// <summary>
    /// 재고 수량 예약
    /// </summary>
    FinT<IO, InventoryItem> ReserveQuantity(Guid productId, int quantity);
}

