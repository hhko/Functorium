using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;
using Cqrs05Services.Messages;
using static LanguageExt.Prelude;
using Wolverine;

namespace InventoryService.Adapters.Messaging;

/// <summary>
/// RabbitMQ 기반 주문 서비스 메시징 구현
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 구현
/// GeneratePipeline 애트리뷰트로 파이프라인 버전 자동 생성
/// </summary>
[GeneratePipeline]
public class RabbitMqOrderMessaging : IOrderMessaging
{
    private readonly IMessageBus _messageBus;

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "Messaging";

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="messageBus">Wolverine 메시지 버스</param>
    public RabbitMqOrderMessaging(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    /// <summary>
    /// 주문 완료 알림 (Fire and Forget 패턴)
    /// </summary>
    /// <param name="orderCompletedEvent">주문 완료 이벤트</param>
    /// <returns>Unit (Fire and Forget이므로 응답 없음)</returns>
    public virtual FinT<IO, Unit> NotifyOrderCompleted(OrderCompletedEvent orderCompletedEvent)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                await _messageBus.SendAsync(orderCompletedEvent);
                return Fin.Succ(unit);
            }
            catch (Exception ex)
            {
                return Fin.Fail<Unit>(Error.New(ex.Message));
            }
        });
    }
}

