---
title: "도메인 이벤트 사양"
---

Functorium 프레임워크가 제공하는 도메인 이벤트 관련 공개 타입의 API 사양입니다. 설계 원칙과 구현 패턴은 [도메인 이벤트 가이드](../guides/domain/07-domain-events)를 참조하십시오.

## 요약

### 주요 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `IDomainEvent` | `Functorium.Domains.Events` | 도메인 이벤트 기본 인터페이스 (`INotification` 확장) |
| `DomainEvent` | `Functorium.Domains.Events` | 도메인 이벤트 기반 abstract record (불변성, 값 동등성) |
| `IHasDomainEvents` | `Functorium.Domains.Events` | Aggregate의 이벤트 조회 전용 마커 인터페이스 |
| `IDomainEventDrain` | `Functorium.Domains.Events` | 이벤트 정리 인터페이스 (internal, 인프라 전용) |
| `IDomainEventCollector` | `Functorium.Applications.Events` | Scoped 범위에서 Aggregate를 추적하여 이벤트를 수집 |
| `IDomainEventPublisher` | `Functorium.Applications.Events` | 도메인 이벤트 발행자 인터페이스 (`FinT` 반환) |
| `IDomainEventHandler<TEvent>` | `Functorium.Applications.Events` | 도메인 이벤트 핸들러 인터페이스 (`INotificationHandler` 확장) |
| `IDomainEventBatchHandler<TEvent>` | `Functorium.Applications.Events` | 동일 타입 이벤트의 배치 처리를 위한 선택적(opt-in) 핸들러 |
| `PublishResult` | `Functorium.Applications.Events` | 다중 이벤트 발행 결과 (부분 성공/실패 추적) |
| `ObservableDomainEventPublisher` | `Functorium.Adapters.Observabilities.Events` | `IDomainEventPublisher` 관찰성 데코레이터 |
| `ObservableDomainEventNotificationPublisher` | `Functorium.Adapters.Observabilities.Events` | Handler 관점 관찰성을 제공하는 `INotificationPublisher` 구현체 |
| `IUsecaseLogEnricher<TRequest, TResponse>` | `Functorium.Applications.Observabilities` | Usecase 로그에 비즈니스 컨텍스트 필드를 추가하는 Enricher |
| `IDomainEventLogEnricher<TEvent>` | `Functorium.Applications.Observabilities` | 도메인 이벤트 핸들러 로그에 비즈니스 컨텍스트 필드를 추가하는 Enricher |
| `LogEnricherContext` | `Functorium.Applications.Observabilities` | LogContext Push 팩토리를 관리하는 정적 유틸리티 |
| `LogEnricherRootAttribute` | `Functorium.Applications.Observabilities` | 소스 생성기에서 ctx 루트 레벨로 승격할 필드를 지정 |
| `LogEnricherIgnoreAttribute` | `Functorium.Applications.Usecases` | 소스 생성기에서 LogEnricher 자동 생성 대상에서 제외 |

---

## 이벤트 계약 (IDomainEvent, DomainEvent)

### IDomainEvent

도메인 이벤트의 기본 인터페이스입니다. Mediator의 `INotification`을 확장하여 Pub/Sub 통합을 제공합니다.

```csharp
namespace Functorium.Domains.Events;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
    Ulid EventId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `OccurredAt` | `DateTimeOffset` | 이벤트 발생 시각 |
| `EventId` | `Ulid` | 이벤트 고유 식별자 (중복 처리 방지 및 추적용) |
| `CorrelationId` | `string?` | 요청 추적 ID (동일 요청에서 발생한 이벤트 추적) |
| `CausationId` | `string?` | 원인 이벤트 ID (이 이벤트를 유발한 이전 이벤트의 ID) |

### DomainEvent

도메인 이벤트의 기반 abstract record입니다. 불변성과 값 기반 동등성을 제공합니다.

```csharp
namespace Functorium.Domains.Events;

public abstract record DomainEvent(
    DateTimeOffset OccurredAt,
    Ulid EventId,
    string? CorrelationId,
    string? CausationId) : IDomainEvent
{
    protected DomainEvent()
        : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), null, null) { }

    protected DomainEvent(string? correlationId)
        : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, null) { }

    protected DomainEvent(string? correlationId, string? causationId)
        : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, causationId) { }
}
```

| 생성자 | 설명 |
|--------|------|
| `DomainEvent()` | 현재 시각과 새 `EventId`로 생성 (`CorrelationId`, `CausationId`는 `null`) |
| `DomainEvent(string? correlationId)` | 지정된 `CorrelationId`로 생성 |
| `DomainEvent(string? correlationId, string? causationId)` | 지정된 `CorrelationId`와 `CausationId`로 생성 |

### IHasDomainEvents

도메인 이벤트를 가진 Aggregate를 추적하기 위한 읽기 전용 마커 인터페이스입니다.

```csharp
namespace Functorium.Domains.Events;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `DomainEvents` | `IReadOnlyList<IDomainEvent>` | Aggregate에 등록된 도메인 이벤트 목록 (읽기 전용) |

### IDomainEventDrain (internal)

이벤트 발행 후 Aggregate의 이벤트를 제거하는 인프라 인터페이스입니다. 도메인 계약(`IHasDomainEvents`)과 분리하여 이벤트 정리가 인프라 관심사임을 명시합니다.

```csharp
namespace Functorium.Domains.Events;

internal interface IDomainEventDrain : IHasDomainEvents
{
    void ClearDomainEvents();
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `ClearDomainEvents()` | `void` | 모든 도메인 이벤트 제거 |

> **접근 수준:** `internal`입니다. 애플리케이션 코드에서 직접 호출하지 마십시오. `AggregateRoot<TId>`가 이 인터페이스를 구현하며, 인프라 코드(Publisher)가 발행 후 자동으로 호출합니다.

### 이벤트 정의 예제

```csharp
// Aggregate 내 중첩 record로 정의
public class Order : AggregateRoot<OrderId>
{
    public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;

    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        order.AddDomainEvent(new CreatedEvent(id, totalAmount));
        return order;
    }
}
```

---

## 이벤트 수집 (IDomainEventCollector)

Scoped 범위에서 Aggregate를 추적하여 도메인 이벤트를 수집하는 인터페이스입니다. Repository의 Create/Update에서 `Track()`을 호출하고, `UsecaseTransactionPipeline`에서 `GetTrackedAggregates()`로 이벤트를 수집합니다.

```csharp
namespace Functorium.Applications.Events;

public interface IDomainEventCollector
{
    void Track(IHasDomainEvents aggregate);
    void TrackRange(IEnumerable<IHasDomainEvents> aggregates);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `Track(IHasDomainEvents aggregate)` | `void` | Aggregate를 추적 대상으로 등록 (이미 등록된 경우 무시) |
| `TrackRange(IEnumerable<IHasDomainEvents> aggregates)` | `void` | 여러 Aggregate를 추적 대상으로 일괄 등록 |
| `GetTrackedAggregates()` | `IReadOnlyList<IHasDomainEvents>` | 추적 중인 Aggregate 중 도메인 이벤트가 있는 것들을 반환 |

### 사용 흐름

1. **Repository에서** Create/Update 시 `IDomainEventCollector.Track(aggregate)` 호출
2. **UsecaseTransactionPipeline에서** SaveChanges 후 `GetTrackedAggregates()`로 이벤트가 있는 Aggregate 조회
3. **IDomainEventPublisher를** 통해 수집된 이벤트 발행

---

## 이벤트 발행 (IDomainEventPublisher)

도메인 이벤트 발행자 인터페이스입니다. Repository/Port와 동일한 `FinT` 반환 패턴을 사용합니다.

```csharp
namespace Functorium.Applications.Events;

public interface IDomainEventPublisher
{
    FinT<IO, Unit> Publish<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    FinT<IO, Seq<PublishResult>> PublishTrackedEvents(
        CancellationToken cancellationToken = default);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `Publish<TEvent>(TEvent, CancellationToken)` | `FinT<IO, Unit>` | 단일 도메인 이벤트를 발행 |
| `PublishTrackedEvents(CancellationToken)` | `FinT<IO, Seq<PublishResult>>` | `IDomainEventCollector`에서 추적된 모든 Aggregate의 이벤트를 발행하고 클리어 |

**제네릭 제약 조건:** `TEvent`는 `IDomainEvent`를 구현해야 합니다.

### PublishResult

다중 이벤트 발행 시 부분 성공/실패를 추적하는 결과 record입니다.

```csharp
namespace Functorium.Applications.Events;

public sealed record PublishResult(
    Seq<IDomainEvent> SuccessfulEvents,
    Seq<(IDomainEvent Event, Error Error)> FailedEvents)
{
    public bool IsAllSuccessful { get; }
    public bool HasFailures { get; }
    public int TotalCount { get; }
    public int SuccessCount { get; }
    public int FailureCount { get; }

    public static PublishResult Empty { get; }
    public static PublishResult Success(Seq<IDomainEvent> events);
    public static PublishResult Failure(Seq<(IDomainEvent Event, Error Error)> failures);
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `SuccessfulEvents` | `Seq<IDomainEvent>` | 성공적으로 발행된 이벤트 목록 |
| `FailedEvents` | `Seq<(IDomainEvent Event, Error Error)>` | 발행 실패한 이벤트와 에러 목록 |
| `IsAllSuccessful` | `bool` | 모든 이벤트가 성공적으로 발행되었는지 여부 (`FailedEvents.IsEmpty`) |
| `HasFailures` | `bool` | 실패한 이벤트가 있는지 여부 |
| `TotalCount` | `int` | 발행된 총 이벤트 수 |
| `SuccessCount` | `int` | 성공한 이벤트 수 |
| `FailureCount` | `int` | 실패한 이벤트 수 |

| 팩토리 메서드 | 반환 타입 | 설명 |
|--------------|----------|------|
| `Empty` | `PublishResult` | 빈 결과 (이벤트 없음) |
| `Success(Seq<IDomainEvent>)` | `PublishResult` | 모든 이벤트가 성공한 결과 |
| `Failure(Seq<(IDomainEvent, Error)>)` | `PublishResult` | 모든 이벤트가 실패한 결과 |

---

## 이벤트 핸들러 (IDomainEventHandler\<TEvent\>)

도메인 이벤트 핸들러 인터페이스입니다. Mediator의 `INotificationHandler<TEvent>`를 확장하여 소스 생성기 호환을 제공합니다.

```csharp
namespace Functorium.Applications.Events;

public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
    // INotificationHandler<TEvent>에서 상속:
    // ValueTask Handle(TEvent notification, CancellationToken cancellationToken)
}
```

**제네릭 제약 조건:** `TEvent`는 `IDomainEvent`를 구현해야 합니다.

| 상속 메서드 | 반환 타입 | 설명 |
|------------|----------|------|
| `Handle(TEvent notification, CancellationToken cancellationToken)` | `ValueTask` | 도메인 이벤트를 처리 (`INotificationHandler<TEvent>`에서 상속) |

### 사용 예제

```csharp
public sealed class OnOrderCreated : IDomainEventHandler<Order.CreatedEvent>
{
    public async ValueTask Handle(Order.CreatedEvent notification, CancellationToken cancellationToken)
    {
        // 재고 차감, 알림 발송 등 부수 효과 처리
    }
}
```

---

## 이벤트 배치 핸들러 (IDomainEventBatchHandler\<TEvent\>)

동일 타입 도메인 이벤트의 배치 처리를 위한 선택적(opt-in) 핸들러입니다. Publisher에서 직접 호출되며 Mediator 라우팅을 사용하지 않습니다. 개별 `IDomainEventHandler<TEvent>`와 독립적으로 공존합니다.

```csharp
namespace Functorium.Applications.Events;

public interface IDomainEventBatchHandler<TEvent> where TEvent : IDomainEvent
{
    ValueTask HandleBatch(Seq<TEvent> events, CancellationToken cancellationToken);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `HandleBatch(Seq<TEvent> events, CancellationToken cancellationToken)` | `ValueTask` | 동일 타입 이벤트 시퀀스를 배치로 처리 |

**제네릭 제약 조건:** `TEvent`는 `IDomainEvent`를 구현해야 합니다.

### Publisher의 배치 처리 흐름

`DomainEventPublisher.PublishTrackedEvents()`는 수집된 이벤트를 타입별로 그룹핑한 뒤 배치 핸들러를 확인합니다:

1. Aggregate에서 이벤트 수집 → 타입별 그룹화 (`GroupBy(e => e.GetType())`)
2. 동일 타입 이벤트가 2개 이상이고 `IDomainEventBatchHandler<T>`가 DI에 등록됨 → **배치 핸들러 1회 호출, 개별 발행 스킵**
3. 위 조건 미충족 → 이벤트마다 개별 Mediator.Publish (기존 동작)

| 조건 | 이벤트 처리 방식 |
|------|-----------------|
| 동일 타입 2개 이상 + BatchHandler 등록 | `HandleBatch(Seq<TEvent>)` **1회** 호출 |
| 동일 타입 2개 이상 + BatchHandler 미등록 | Mediator.Publish **N회** 개별 발행 |
| 동일 타입 1개 | Mediator.Publish **1회** 개별 발행 |

### 사용 예제

```csharp
public sealed class ProductDeletedBatchHandler : IDomainEventBatchHandler<Product.DeletedEvent>
{
    private readonly ILogger<ProductDeletedBatchHandler> _logger;

    public ProductDeletedBatchHandler(ILogger<ProductDeletedBatchHandler> logger)
        => _logger = logger;

    public ValueTask HandleBatch(Seq<Product.DeletedEvent> events, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent:Batch] {Count} products deleted in bulk: [{ProductIds}]",
            events.Count,
            string.Join(", ", events.Select(e => e.ProductId.ToString())));

        return ValueTask.CompletedTask;
    }
}
```

### DI 등록

`RegisterDomainEventHandlersFromAssembly`가 `IDomainEventHandler<T>`와 `IDomainEventBatchHandler<T>`를 함께 스캔합니다. 별도 등록은 불필요합니다.

### 배치 핸들러 관찰 가능성

배치 핸들러 호출 시 `DomainEventPublisher`가 배치 단위로 관찰 가능성을 자동 적용합니다:

| 항목 | 이름 패턴 | 설명 |
|------|----------|------|
| Counter (Request) | `application.event.batch.requests` | 배치 핸들러 요청 수 |
| Counter (Response) | `application.event.batch.responses` | 배치 핸들러 응답 수 |
| Histogram (Duration) | `application.event.batch.duration` | 배치 핸들러 처리 시간 (초) |

---

## Observable 발행자

### ObservableDomainEventPublisher

관찰성(로깅, 추적, 메트릭)이 통합된 `IDomainEventPublisher` 데코레이터입니다. Adapter Layer에서 이벤트 발행에 대한 관찰 가능성을 제공합니다.

```csharp
namespace Functorium.Adapters.Observabilities.Events;

public sealed class ObservableDomainEventPublisher : IDomainEventPublisher, IDisposable
{
    public ObservableDomainEventPublisher(
        ActivitySource activitySource,
        IDomainEventPublisher inner,
        IDomainEventCollector collector,
        ILogger<ObservableDomainEventPublisher> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions);

    public FinT<IO, Unit> Publish<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    public FinT<IO, Seq<PublishResult>> PublishTrackedEvents(
        CancellationToken cancellationToken = default);

    public void Dispose();
}
```

| 생성자 매개변수 | 타입 | 설명 |
|----------------|------|------|
| `activitySource` | `ActivitySource` | 분산 추적용 ActivitySource (DI 주입) |
| `inner` | `IDomainEventPublisher` | 데코레이트할 실제 발행자 |
| `collector` | `IDomainEventCollector` | 추적 이벤트 건수 계산용 수집기 |
| `logger` | `ILogger<ObservableDomainEventPublisher>` | 로거 |
| `meterFactory` | `IMeterFactory` | Meter 팩토리 |
| `openTelemetryOptions` | `IOptions<OpenTelemetryOptions>` | OpenTelemetry 설정 |

**관찰성 항목:**

| 항목 | 이름 패턴 | 설명 |
|------|----------|------|
| Meter | `{ServiceNamespace}.adapter.event` | Adapter Layer 이벤트 Meter |
| Counter (Request) | `adapter.event.requests` | 이벤트 발행 요청 수 |
| Counter (Response) | `adapter.event.responses` | 이벤트 발행 응답 수 |
| Histogram (Duration) | `adapter.event.duration` | 이벤트 발행 처리 시간 (초) |

### ObservableDomainEventNotificationPublisher

도메인 이벤트 핸들러에 대한 Handler 관점 관찰성(로깅, 추적, 메트릭)을 제공하는 `INotificationPublisher` 구현체입니다.

```csharp
namespace Functorium.Adapters.Observabilities.Events;

public sealed class ObservableDomainEventNotificationPublisher : INotificationPublisher, IDisposable
{
    public ObservableDomainEventNotificationPublisher(
        ActivitySource activitySource,
        ILoggerFactory loggerFactory,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        IServiceProvider serviceProvider);

    public ValueTask Publish<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification;

    public void Dispose();
}
```

| 생성자 매개변수 | 타입 | 설명 |
|----------------|------|------|
| `activitySource` | `ActivitySource` | 분산 추적용 ActivitySource (DI 주입) |
| `loggerFactory` | `ILoggerFactory` | 핸들러별 로거 생성용 팩토리 |
| `meterFactory` | `IMeterFactory` | Meter 팩토리 |
| `openTelemetryOptions` | `IOptions<OpenTelemetryOptions>` | OpenTelemetry 설정 |
| `serviceProvider` | `IServiceProvider` | `IDomainEventLogEnricher` 해석용 DI 컨테이너 |

**동작 방식:**
- `IDomainEvent`인 Notification에만 관찰성을 적용합니다.
- `IDomainEvent`가 아닌 Notification은 관찰성 없이 기본 ForeachAwait 방식으로 발행합니다.
- 핸들러 처리 전 `IDomainEventLogEnricher<TEvent>`를 DI에서 해석하여 LogContext에 커스텀 속성을 자동 Push합니다.

**관찰성 항목:**

| 항목 | 이름 패턴 | 설명 |
|------|----------|------|
| Meter | `{ServiceNamespace}.application` | Application Layer Meter |
| Counter (Request) | `application.usecase.event.requests` | 핸들러 요청 수 |
| Counter (Response) | `application.usecase.event.responses` | 핸들러 응답 수 |
| Histogram (Duration) | `application.usecase.event.duration` | 핸들러 처리 시간 (초) |

> **Mediator 3.0 제약:** Mediator 3.0은 `INotification`에 `IPipelineBehavior`를 지원하지 않으며, 소스 생성기가 `INotificationPublisher` 인터페이스가 아닌 구체 타입을 직접 사용합니다. 따라서 Scrutor의 Decorate 패턴이 동작하지 않으며, `NotificationPublisherType` 설정을 사용해야 합니다.

### DI 등록

```csharp
services.AddMediator(options =>
{
    options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
});

// RegisterDomainEventHandlersFromAssembly가 개별 핸들러와 배치 핸들러를 함께 스캔합니다.
```

---

## Log Enricher (IUsecaseLogEnricher, IDomainEventLogEnricher)

### IUsecaseLogEnricher\<TRequest, TResponse\>

Usecase 로그에 비즈니스 컨텍스트 필드를 추가하는 Enricher 인터페이스입니다. 내장 `UsecaseLoggingPipeline`이 Request/Response 로그 출력 시 `LogContext`에 커스텀 속성을 자동으로 Push합니다.

```csharp
namespace Functorium.Applications.Observabilities;

public interface IUsecaseLogEnricher<in TRequest, in TResponse>
    where TResponse : IFinResponse
{
    IDisposable? EnrichRequestLog(TRequest request);
    IDisposable? EnrichResponseLog(TRequest request, TResponse response);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `EnrichRequestLog(TRequest request)` | `IDisposable?` | Request 로그 출력 전 LogContext에 속성 Push |
| `EnrichResponseLog(TRequest request, TResponse response)` | `IDisposable?` | Response 로그 출력 전 LogContext에 속성 Push |

**제네릭 제약 조건:** `TResponse`는 `IFinResponse`를 구현해야 합니다.

### IDomainEventLogEnricher\<TEvent\>

도메인 이벤트 핸들러 로그에 비즈니스 컨텍스트 필드를 추가하는 Enricher 인터페이스입니다. `ObservableDomainEventNotificationPublisher`가 Handler 처리 시 `LogContext`에 커스텀 속성을 자동으로 Push합니다.

```csharp
namespace Functorium.Applications.Observabilities;

public interface IDomainEventLogEnricher<in TEvent> : IDomainEventLogEnricher
    where TEvent : IDomainEvent
{
    IDisposable? EnrichLog(TEvent domainEvent);
}

public interface IDomainEventLogEnricher
{
    IDisposable? EnrichLog(IDomainEvent domainEvent);
}
```

| 인터페이스 | 메서드 | 설명 |
|-----------|--------|------|
| `IDomainEventLogEnricher<TEvent>` | `EnrichLog(TEvent domainEvent)` | 타입 안전한 이벤트 로그 Enrichment |
| `IDomainEventLogEnricher` (비제네릭) | `EnrichLog(IDomainEvent domainEvent)` | 런타임 타입 해석 후 호출에 사용하는 브릿지 인터페이스 |

> **구현 규칙:** `IDomainEventLogEnricher`(비제네릭)를 직접 구현하지 마십시오. `IDomainEventLogEnricher<TEvent>`를 구현하면 Default Interface Method로 비제네릭 브릿지가 자동 제공됩니다.

**제네릭 제약 조건:** `TEvent`는 `IDomainEvent`를 구현해야 합니다.

### LogEnricherContext

LogContext Push 팩토리를 관리하는 정적 유틸리티 클래스입니다. Serilog 등 로깅 프레임워크의 `LogContext.PushProperty`를 프레임워크와 연결하는 브릿지 역할을 합니다.

```csharp
namespace Functorium.Applications.Observabilities;

public static class LogEnricherContext
{
    public static void SetPushPropertyFactory(Func<string, object?, IDisposable> factory);
    public static IDisposable PushProperty(string name, object? value);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `SetPushPropertyFactory(Func<string, object?, IDisposable>)` | `void` | LogContext Push 팩토리 설정 (애플리케이션 시작 시 1회 호출) |
| `PushProperty(string name, object? value)` | `IDisposable` | 지정된 이름과 값으로 LogContext에 속성 Push |

> **초기화:** 팩토리가 설정되지 않으면 `PushProperty`는 아무 동작도 하지 않는 `NullDisposable`을 반환합니다.

---

## 소스 생성기 어트리뷰트 (\[LogEnricherRoot\], \[LogEnricherIgnore\])

### LogEnricherRootAttribute

소스 생성기에서 LogEnricher 생성 시 해당 필드를 `ctx` 루트 레벨(`ctx.{field}`)로 승격할 것을 지시하는 어트리뷰트입니다.

```csharp
namespace Functorium.Applications.Observabilities;

[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class LogEnricherRootAttribute : Attribute;
```

| 대상 | 동작 |
|------|------|
| `Interface` | 해당 인터페이스를 구현하는 모든 Request/Response에서 해당 필드가 `ctx.{field}`로 승격 |
| `Property` | 해당 프로퍼티가 `ctx.{field}`로 승격 |
| `Parameter` | record 생성자 파라미터가 `ctx.{field}`로 승격 |

### LogEnricherIgnoreAttribute

소스 생성기에서 LogEnricher 자동 생성 대상에서 제외할 것을 지시하는 어트리뷰트입니다.

```csharp
namespace Functorium.Applications.Usecases;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class LogEnricherIgnoreAttribute : Attribute;
```

| 대상 | 동작 |
|------|------|
| `Class` | 해당 Request record 전체가 LogEnricher 자동 생성에서 제외 |
| `Property` | 해당 프로퍼티가 LogEnricher 자동 생성에서 제외 |
| `Parameter` | record 생성자 파라미터가 LogEnricher 자동 생성에서 제외 |

### 어트리뷰트 사용 예제

```csharp
// 인터페이스에 [LogEnricherRoot] 적용 — 구현하는 모든 Request에서 승격
[LogEnricherRoot]
public interface IHasOrderId
{
    OrderId OrderId { get; }
}

// 개별 프로퍼티에 적용
public sealed record CreateOrderCommand(
    [property: LogEnricherRoot] OrderId OrderId,    // ctx.OrderId로 승격
    [property: LogEnricherIgnore] string Payload,    // Enricher에서 제외
    Money TotalAmount) : ICommandRequest<CreateOrderResponse>;
```

---

## 관련 문서

- [도메인 이벤트 가이드](../guides/domain/07-domain-events) — 설계 원칙, 구현 패턴, UsecaseTransactionPipeline 통합
- [엔티티와 애그리거트 사양](./01-entity-aggregate) — `AggregateRoot<TId>`의 `AddDomainEvent()`, `IHasDomainEvents`
- [Observability 사양](./08-observability) — 관찰성 필드/태그 표준, Meter 정의 규칙
