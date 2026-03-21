using Functorium.Domains.Events;

namespace Functorium.Applications.Observabilities;

/// <summary>
/// 도메인 이벤트 핸들러 로그에 비즈니스 컨텍스트 필드를 추가하는 Enricher 인터페이스.
/// ObservableDomainEventNotificationPublisher가 Handler 처리 시
/// LogContext에 커스텀 속성을 자동으로 Push합니다.
/// </summary>
/// <typeparam name="TEvent">대상 도메인 이벤트 타입</typeparam>
public interface IDomainEventLogEnricher<in TEvent> : IDomainEventLogEnricher
    where TEvent : IDomainEvent
{
    /// <summary>
    /// 이벤트 핸들러 로그 출력 전에 호출됩니다.
    /// LogContext.PushProperty로 추가 속성을 Push하고 IDisposable을 반환하세요.
    /// 반환된 IDisposable은 Request + Response 로그 모두에 적용된 후 Dispose됩니다.
    /// </summary>
    IDisposable? EnrichLog(TEvent domainEvent);

    // 비제네릭 브릿지 (Default Interface Method)
    IDisposable? IDomainEventLogEnricher.EnrichLog(IDomainEvent domainEvent)
        => EnrichLog((TEvent)domainEvent);
}

/// <summary>
/// IDomainEventLogEnricher의 비제네릭 베이스 인터페이스.
/// ObservableDomainEventNotificationPublisher가 런타임 타입 해석 후 호출에 사용합니다.
/// 직접 구현하지 마세요 — IDomainEventLogEnricher&lt;TEvent&gt;를 구현하세요.
/// </summary>
public interface IDomainEventLogEnricher
{
    IDisposable? EnrichLog(IDomainEvent domainEvent);
}
