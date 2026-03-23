using Functorium.Domains.Events;

namespace Functorium.Applications.Events;

/// <summary>
/// 동일 타입 도메인 이벤트의 배치 처리를 위한 선택적(opt-in) 핸들러.
/// Publisher에서 직접 호출되며 Mediator 라우팅을 사용하지 않습니다.
/// 개별 IDomainEventHandler와 독립적으로 공존합니다.
/// </summary>
/// <remarks>
/// 배치 핸들러는 성능 최적화가 필요한 시나리오에서 사용합니다.
/// 개별 핸들러(IDomainEventHandler)는 항상 호출되므로,
/// 배치 핸들러와 개별 핸들러의 관심사가 다른 경우에만 사용하십시오.
/// <code>
/// public class ProductSearchIndexBatchHandler : IDomainEventBatchHandler&lt;Product.CreatedEvent&gt;
/// {
///     public ValueTask HandleBatch(Seq&lt;Product.CreatedEvent&gt; events, CancellationToken ct)
///     {
///         // 검색 인덱스 벌크 업데이트 등 배치 최적화 로직
///     }
/// }
/// </code>
/// </remarks>
/// <typeparam name="TEvent">처리할 도메인 이벤트 타입</typeparam>
public interface IDomainEventBatchHandler<TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// 동일 타입 도메인 이벤트를 배치로 처리합니다.
    /// </summary>
    /// <param name="events">동일 타입 이벤트 시퀀스</param>
    /// <param name="cancellationToken">취소 토큰</param>
    ValueTask HandleBatch(Seq<TEvent> events, CancellationToken cancellationToken);
}
