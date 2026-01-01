using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;
using CqrsObservability.Messages;

namespace InventoryService.Adapters.Messaging;

/// <summary>
/// 주문 서비스 메시징 인터페이스
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 상속
/// </summary>
public interface IOrderMessaging : IAdapter
{
    /// <summary>
    /// 주문 완료 알림 (Fire and Forget 패턴)
    /// </summary>
    /// <param name="orderCompletedEvent">주문 완료 이벤트</param>
    /// <returns>Unit (Fire and Forget이므로 응답 없음)</returns>
    FinT<IO, Unit> NotifyOrderCompleted(OrderCompletedEvent orderCompletedEvent);
}

