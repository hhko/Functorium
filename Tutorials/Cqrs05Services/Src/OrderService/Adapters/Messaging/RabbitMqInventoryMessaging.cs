using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;
using Cqrs05Services.Messages;
using static LanguageExt.Prelude;
using Wolverine;

namespace OrderService.Adapters.Messaging;

/// <summary>
/// RabbitMQ 기반 재고 서비스 메시징 구현
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 구현
/// GeneratePipeline 애트리뷰트로 파이프라인 버전 자동 생성
/// </summary>
[GeneratePipeline]
public class RabbitMqInventoryMessaging : IInventoryMessaging
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
    public RabbitMqInventoryMessaging(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    /// <summary>
    /// 재고 확인 (Request/Reply 패턴)
    /// </summary>
    /// <param name="request">재고 확인 요청</param>
    /// <returns>재고 확인 응답</returns>
    public virtual FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.liftAsync(async () =>
        {
            try
            {
                var response = await _messageBus.InvokeAsync<CheckInventoryResponse>(request);
                return Fin.Succ(response);
            }
            catch (Exception ex)
            {
                return Fin.Fail<CheckInventoryResponse>(Error.New(ex.Message));
            }
        });
    }

    /// <summary>
    /// 재고 예약 (Fire and Forget 패턴)
    /// </summary>
    /// <param name="command">재고 예약 명령</param>
    /// <returns>Unit (Fire and Forget이므로 응답 없음)</returns>
    public virtual FinT<IO, Unit> ReserveInventory(ReserveInventoryCommand command)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.liftAsync(async () =>
        {
            try
            {
                await _messageBus.SendAsync(command);
                return Fin.Succ(unit);
            }
            catch (Exception ex)
            {
                return Fin.Fail<Unit>(Error.New(ex.Message));
            }
        });
    }
}

