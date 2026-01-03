using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;
using Cqrs05Services.Messages;

namespace OrderService.Adapters.Messaging;

/// <summary>
/// 재고 서비스 메시징 인터페이스
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 상속
/// </summary>
public interface IInventoryMessaging : IAdapter
{
    /// <summary>
    /// 재고 확인 (Request/Reply 패턴)
    /// </summary>
    /// <param name="request">재고 확인 요청</param>
    /// <returns>재고 확인 응답</returns>
    FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request);

    /// <summary>
    /// 재고 예약 (Fire and Forget 패턴)
    /// </summary>
    /// <param name="command">재고 예약 명령</param>
    /// <returns>Unit (Fire and Forget이므로 응답 없음)</returns>
    FinT<IO, Unit> ReserveInventory(ReserveInventoryCommand command);
}

