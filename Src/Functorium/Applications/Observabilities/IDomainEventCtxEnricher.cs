using Functorium.Domains.Events;

namespace Functorium.Applications.Observabilities;

/// <summary>
/// 도메인 이벤트 핸들러의 비즈니스 컨텍스트 필드를 3-Pillar에 전파하는 Enricher 인터페이스.
/// ObservableDomainEventNotificationPublisher가 Handler 처리 시
/// ctx.* 필드를 모든 대상 Pillar에 동시 전파합니다.
/// </summary>
/// <typeparam name="TEvent">대상 도메인 이벤트 타입</typeparam>
public interface IDomainEventCtxEnricher<in TEvent> : IDomainEventCtxEnricher
    where TEvent : IDomainEvent
{
    /// <summary>
    /// 이벤트 핸들러 실행 전에 호출됩니다.
    /// CtxEnricherContext.Push로 ctx.* 필드를 대상 Pillar에 전파하고 IDisposable을 반환합니다.
    /// 반환된 IDisposable은 Handler 전체 실행(Request + Response 로그 포함)에 적용된 후 Dispose됩니다.
    /// </summary>
    IDisposable? Enrich(TEvent domainEvent);

    // 비제네릭 브릿지 (Default Interface Method)
    IDisposable? IDomainEventCtxEnricher.Enrich(IDomainEvent domainEvent)
        => Enrich((TEvent)domainEvent);
}

/// <summary>
/// IDomainEventCtxEnricher의 비제네릭 베이스 인터페이스.
/// ObservableDomainEventNotificationPublisher가 런타임 타입 해석 후 호출에 사용합니다.
/// 직접 구현하지 마세요 — IDomainEventCtxEnricher&lt;TEvent&gt;를 구현하세요.
/// </summary>
public interface IDomainEventCtxEnricher
{
    IDisposable? Enrich(IDomainEvent domainEvent);
}
