---
title: "Domain Events Specification"
---

This is the API specification for domain event related public types provided by the Functorium framework. For design principles and implementation patterns, see the [Domain Events Guide](../guides/domain/07-domain-events).

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `IDomainEvent` | `Functorium.Domains.Events` | Domain event base interface (`INotification` extension) |
| `DomainEvent` | `Functorium.Domains.Events` | Domain event base abstract record (immutability, value equality) |
| `IHasDomainEvents` | `Functorium.Domains.Events` | Read-only marker interface for tracking Aggregate events |
| `IDomainEventDrain` | `Functorium.Domains.Events` | Event cleanup interface (internal, infrastructure only) |
| `IDomainEventCollector` | `Functorium.Applications.Events` | Collects events by tracking Aggregates within Scoped scope |
| `IDomainEventPublisher` | `Functorium.Applications.Events` | Domain event publisher interface (`FinT` return) |
| `IDomainEventHandler<TEvent>` | `Functorium.Applications.Events` | Domain event handler interface (`INotificationHandler` extension) |
| `PublishResult` | `Functorium.Applications.Events` | Multiple event publishing result (partial success/failure tracking) |
| `ObservableDomainEventPublisher` | `Functorium.Adapters.Events` | `IDomainEventPublisher` observability decorator |
| `ObservableDomainEventNotificationPublisher` | `Functorium.Adapters.Events` | `INotificationPublisher` implementation providing handler-perspective observability |
| `IUsecaseCtxEnricher<TRequest, TResponse>` | `Functorium.Abstractions.Observabilities` | Enricher that adds business context fields to Usecase logs |
| `IDomainEventCtxEnricher<TEvent>` | `Functorium.Abstractions.Observabilities` | Enricher that adds business context fields to domain event handler logs |
| `CtxEnricherContext` | `Functorium.Abstractions.Observabilities` | Static utility that manages LogContext Push factory |
| `CtxRootAttribute` | `Functorium.Abstractions.Observabilities` | Specifies fields to promote to ctx root level in source generators |
| `CtxIgnoreAttribute` | `Functorium.Applications.Usecases` | Excludes from CtxEnricher auto-generation target in source generators |

---

## Event Contract (IDomainEvent, DomainEvent)

### IDomainEvent

The base interface for domain events. Extends Mediator's `INotification` to provide Pub/Sub integration.

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

| Property | Type | Description |
|------|------|------|
| `OccurredAt` | `DateTimeOffset` | Event occurrence time |
| `EventId` | `Ulid` | Unique event identifier (for deduplication and tracking) |
| `CorrelationId` | `string?` | Request tracking ID (traces events from the same request) |
| `CausationId` | `string?` | Cause event ID (ID of the preceding event that triggered this event) |

### DomainEvent

The base abstract record for domain events. Provides immutability and value-based equality.

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

| Constructor | Description |
|--------|------|
| `DomainEvent()` | Creates with current time and new `EventId` (`CorrelationId` and `CausationId` are `null`) |
| `DomainEvent(string? correlationId)` | Creates with specified `CorrelationId` |
| `DomainEvent(string? correlationId, string? causationId)` | Creates with specified `CorrelationId` and `CausationId` |

### IHasDomainEvents

A read-only marker interface for tracking Aggregates that have domain events.

```csharp
namespace Functorium.Domains.Events;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
}
```

| Property | Type | Description |
|------|------|------|
| `DomainEvents` | `IReadOnlyList<IDomainEvent>` | Domain event list registered in the Aggregate (read-only) |

### IDomainEventDrain (internal)

An infrastructure interface for removing Aggregate events after publishing. Separated from the domain contract (`IHasDomainEvents`) to explicitly designate event cleanup as an infrastructure concern.

```csharp
namespace Functorium.Domains.Events;

internal interface IDomainEventDrain : IHasDomainEvents
{
    void ClearDomainEvents();
}
```

| Method | Return Type | Description |
|--------|----------|------|
| `ClearDomainEvents()` | `void` | Removes all domain events |

> **Access level:** `internal`. Do not call directly from application code. `AggregateRoot<TId>` implements this interface, and infrastructure code (Publisher) automatically calls it after publishing.

### Event Definition Example

```csharp
// Defined as nested records within the Aggregate
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

## Event Collection (IDomainEventCollector)

An interface that collects domain events by tracking Aggregates within a Scoped scope. `Track()` is called in Repository Create/Update, and events are collected via `GetTrackedAggregates()` in `UsecaseTransactionPipeline`.

```csharp
namespace Functorium.Applications.Events;

public interface IDomainEventCollector
{
    void Track(IHasDomainEvents aggregate);
    void TrackRange(IEnumerable<IHasDomainEvents> aggregates);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
    void TrackEvent(IDomainEvent domainEvent);
    IReadOnlyList<IDomainEvent> GetDirectlyTrackedEvents();
}
```

| Method | Return Type | Description |
|--------|----------|------|
| `Track(IHasDomainEvents aggregate)` | `void` | Registers an Aggregate as a tracking target (ignored if already registered) |
| `TrackRange(IEnumerable<IHasDomainEvents> aggregates)` | `void` | Batch registers multiple Aggregates as tracking targets |
| `GetTrackedAggregates()` | `IReadOnlyList<IHasDomainEvents>` | Returns tracked Aggregates that have domain events |
| `TrackEvent(IDomainEvent domainEvent)` | `void` | Directly tracks bulk events created by Domain Services |
| `GetDirectlyTrackedEvents()` | `IReadOnlyList<IDomainEvent>` | Directly tracks bulk events created by Domain Services |

### Usage Flow

1. **Repository에서** Create/Update 시 `IDomainEventCollector.Track(aggregate)` 호출
2. **UsecaseTransactionPipeline에서** SaveChanges 후 `GetTrackedAggregates()`로 이벤트가 있는 Aggregate 조회
3. **IDomainEventPublisher를** 통해 수집된 이벤트 발행

---

## Event Publishing (IDomainEventPublisher)

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

| Method | Return Type | Description |
|--------|----------|------|
| `Publish<TEvent>(TEvent, CancellationToken)` | `FinT<IO, Unit>` | Publishes a single domain event |
| `PublishTrackedEvents(CancellationToken)` | `FinT<IO, Seq<PublishResult>>` | Publishes events from all Aggregates tracked by `IDomainEventCollector` and clears them |

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

| Property | Type | Description |
|------|------|------|
| `SuccessfulEvents` | `Seq<IDomainEvent>` | List of successfully published events |
| `FailedEvents` | `Seq<(IDomainEvent Event, Error Error)>` | List of failed events and their errors |
| `IsAllSuccessful` | `bool` | Whether all events were successfully published (`FailedEvents.IsEmpty`) |
| `HasFailures` | `bool` | Whether there are any failed events |
| `TotalCount` | `int` | Total number of published events |
| `SuccessCount` | `int` | Number of successful events |
| `FailureCount` | `int` | Number of failed events |

| Factory Method | Return Type | Description |
|--------------|----------|------|
| `Empty` | `PublishResult` | Empty result (no events) |
| `Success(Seq<IDomainEvent>)` | `PublishResult` | Result where all events succeeded |
| `Failure(Seq<(IDomainEvent, Error)>)` | `PublishResult` | Result where all events failed |

---

## Event Handler (IDomainEventHandler\<TEvent\>)

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

| Inherited Method | Return Type | Description |
|------------|----------|------|
| `Handle(TEvent notification, CancellationToken cancellationToken)` | `ValueTask` | 도메인 이벤트를 처리 (`INotificationHandler<TEvent>`에서 상속) |

### Usage Example

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

## Observable Publishers

### ObservableDomainEventPublisher

관찰성(로깅, 추적, 메트릭)이 통합된 `IDomainEventPublisher` 데코레이터입니다. Adapter Layer에서 이벤트 발행에 대한 관찰 가능성을 제공합니다.

```csharp
namespace Functorium.Adapters.Events;

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

| Constructor Parameter | Type | Description |
|----------------|------|------|
| `activitySource` | `ActivitySource` | ActivitySource for distributed tracing (DI injected) |
| `inner` | `IDomainEventPublisher` | Actual publisher to decorate |
| `collector` | `IDomainEventCollector` | Collector for calculating tracked event counts |
| `logger` | `ILogger<ObservableDomainEventPublisher>` | Logger |
| `meterFactory` | `IMeterFactory` | Meter 팩토리 |
| `openTelemetryOptions` | `IOptions<OpenTelemetryOptions>` | OpenTelemetry 설정 |

**관찰성 항목:**

| Item | Name Pattern | Description |
|------|----------|------|
| Meter | `{ServiceNamespace}.adapter.event` | Adapter Layer 이벤트 Meter |
| Counter (Request) | `adapter.event.requests` | 이벤트 발행 요청 수 |
| Counter (Response) | `adapter.event.responses` | 이벤트 발행 응답 수 |
| Histogram (Duration) | `adapter.event.duration` | 이벤트 발행 처리 시간 (초) |

### ObservableDomainEventNotificationPublisher

도메인 이벤트 핸들러에 대한 Handler 관점 관찰성(로깅, 추적, 메트릭)을 제공하는 `INotificationPublisher` 구현체입니다.

```csharp
namespace Functorium.Adapters.Events;

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

| Constructor Parameter | Type | Description |
|----------------|------|------|
| `activitySource` | `ActivitySource` | ActivitySource for distributed tracing (DI injected) |
| `loggerFactory` | `ILoggerFactory` | 핸들러별 Logger 생성용 팩토리 |
| `meterFactory` | `IMeterFactory` | Meter 팩토리 |
| `openTelemetryOptions` | `IOptions<OpenTelemetryOptions>` | OpenTelemetry 설정 |
| `serviceProvider` | `IServiceProvider` | DI container for resolving `IDomainEventCtxEnricher` |

**동작 방식:**
- `IDomainEvent`인 Notification에만 관찰성을 적용합니다.
- `IDomainEvent`가 아닌 Notification은 관찰성 없이 기본 ForeachAwait 방식으로 발행합니다.
- 핸들러 처리 전 `IDomainEventCtxEnricher<TEvent>`를 DI에서 해석하여 LogContext에 커스텀 속성을 자동 Push합니다.

**관찰성 항목:**

| Item | Name Pattern | Description |
|------|----------|------|
| Meter | `{ServiceNamespace}.application` | Application Layer Meter |
| Counter (Request) | `application.usecase.event.requests` | 핸들러 요청 수 |
| Counter (Response) | `application.usecase.event.responses` | 핸들러 응답 수 |
| Histogram (Duration) | `application.usecase.event.duration` | 핸들러 처리 시간 (초) |

> **Mediator 3.0 제약:** Mediator 3.0은 `INotification`에 `IPipelineBehavior`를 지원하지 않으며, 소스 생성기가 `INotificationPublisher` 인터페이스가 아닌 구체 타입을 직접 사용합니다. 따라서 Scrutor의 Decorate 패턴이 동작하지 않으며, `NotificationPublisherType` 설정을 사용해야 합니다.

### DI Registration

```csharp
services.AddMediator(options =>
{
    options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
});

// RegisterDomainEventHandlersFromAssembly가 핸들러를 스캔합니다.
```

---

## Ctx Enricher (IUsecaseCtxEnricher, IDomainEventCtxEnricher)

### IUsecaseCtxEnricher\<TRequest, TResponse\>

Enricher that adds business context fields to Usecase logs 인터페이스입니다. 내장 `UsecaseLoggingPipeline`이 Request/Response 로그 출력 시 `LogContext`에 커스텀 속성을 자동으로 Push합니다.

```csharp
namespace Functorium.Abstractions.Observabilities;

public interface IUsecaseCtxEnricher<in TRequest, in TResponse>
    where TResponse : IFinResponse
{
    IDisposable? EnrichRequestLog(TRequest request);
    IDisposable? EnrichResponseLog(TRequest request, TResponse response);
}
```

| Method | Return Type | Description |
|--------|----------|------|
| `EnrichRequestLog(TRequest request)` | `IDisposable?` | Request 로그 출력 전 LogContext에 속성 Push |
| `EnrichResponseLog(TRequest request, TResponse response)` | `IDisposable?` | Response 로그 출력 전 LogContext에 속성 Push |

**제네릭 제약 조건:** `TResponse`는 `IFinResponse`를 구현해야 합니다.

### IDomainEventCtxEnricher\<TEvent\>

Enricher that adds business context fields to domain event handler logs 인터페이스입니다. `ObservableDomainEventNotificationPublisher`가 Handler 처리 시 `LogContext`에 커스텀 속성을 자동으로 Push합니다.

```csharp
namespace Functorium.Abstractions.Observabilities;

public interface IDomainEventCtxEnricher<in TEvent> : IDomainEventCtxEnricher
    where TEvent : IDomainEvent
{
    IDisposable? EnrichLog(TEvent domainEvent);
}

public interface IDomainEventCtxEnricher
{
    IDisposable? EnrichLog(IDomainEvent domainEvent);
}
```

| 인터페이스 | Method | Description |
|-----------|--------|------|
| `IDomainEventCtxEnricher<TEvent>` | `EnrichLog(TEvent domainEvent)` | 타입 안전한 이벤트 로그 Enrichment |
| `IDomainEventCtxEnricher` (비제네릭) | `EnrichLog(IDomainEvent domainEvent)` | 런타임 타입 해석 후 호출에 사용하는 브릿지 인터페이스 |

> **구현 규칙:** `IDomainEventCtxEnricher`(비제네릭)를 직접 구현하지 마십시오. `IDomainEventCtxEnricher<TEvent>`를 구현하면 Default Interface Method로 비제네릭 브릿지가 자동 제공됩니다.

**제네릭 제약 조건:** `TEvent`는 `IDomainEvent`를 구현해야 합니다.

### CtxEnricherContext

Static utility that manages LogContext Push factory 클래스입니다. Serilog 등 로깅 프레임워크의 `LogContext.PushProperty`를 프레임워크와 연결하는 브릿지 역할을 합니다.

```csharp
namespace Functorium.Abstractions.Observabilities;

public static class CtxEnricherContext
{
    public static void SetPushPropertyFactory(Func<string, object?, IDisposable> factory);
    public static IDisposable PushProperty(string name, object? value);
}
```

| Method | Return Type | Description |
|--------|----------|------|
| `SetPushPropertyFactory(Func<string, object?, IDisposable>)` | `void` | LogContext Push 팩토리 설정 (애플리케이션 시작 시 1회 호출) |
| `PushProperty(string name, object? value)` | `IDisposable` | 지정된 이름과 값으로 LogContext에 속성 Push |

> **초기화:** 팩토리가 설정되지 않으면 `PushProperty`는 아무 동작도 하지 않는 `NullDisposable`을 반환합니다.

---

## Source Generator Attributes (\[CtxRoot\], \[CtxIgnore\])

### CtxRootAttribute

소스 생성기에서 CtxEnricher 생성 시 해당 필드를 `ctx` 루트 레벨(`ctx.{field}`)로 승격할 것을 지시하는 어트리뷰트입니다.

```csharp
namespace Functorium.Abstractions.Observabilities;

[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class CtxRootAttribute : Attribute;
```

| Target | Behavior |
|------|------|
| `Interface` | 해당 인터페이스를 구현하는 모든 Request/Response에서 해당 필드가 `ctx.{field}`로 승격 |
| `Property` | 해당 프로퍼티가 `ctx.{field}`로 승격 |
| `Parameter` | record 생성자 파라미터가 `ctx.{field}`로 승격 |

### CtxIgnoreAttribute

Excludes from CtxEnricher auto-generation target in source generators할 것을 지시하는 어트리뷰트입니다.

```csharp
namespace Functorium.Applications.Usecases;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class CtxIgnoreAttribute : Attribute;
```

| Target | Behavior |
|------|------|
| `Class` | 해당 Request record 전체가 CtxEnricher 자동 생성에서 제외 |
| `Property` | 해당 프로퍼티가 CtxEnricher 자동 생성에서 제외 |
| `Parameter` | record 생성자 파라미터가 CtxEnricher 자동 생성에서 제외 |

### Attribute Usage Example

```csharp
// 인터페이스에 [CtxRoot] 적용 — 구현하는 모든 Request에서 승격
[CtxRoot]
public interface IHasOrderId
{
    OrderId OrderId { get; }
}

// 개별 프로퍼티에 적용
public sealed record CreateOrderCommand(
    [property: CtxRoot] OrderId OrderId,    // ctx.OrderId로 승격
    [property: CtxIgnore] string Payload,    // Enricher에서 제외
    Money TotalAmount) : ICommandRequest<CreateOrderResponse>;
```

---

## Related Documents

- [도메인 이벤트 가이드](../guides/domain/07-domain-events) — 설계 원칙, 구현 패턴, UsecaseTransactionPipeline 통합
- [엔티티와 애그리거트 사양](./01-entity-aggregate) — `AggregateRoot<TId>`의 `AddDomainEvent()`, `IHasDomainEvents`
- [Observability 사양](./08-observability) — 관찰성 필드/태그 표준, Meter 정의 규칙
