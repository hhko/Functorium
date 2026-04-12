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

1. **In Repository,** call `IDomainEventCollector.Track(aggregate)` during Create/Update
2. **In UsecaseTransactionPipeline,** after SaveChanges, query Aggregates with events via `GetTrackedAggregates()`
3. **Publish collected events** via `IDomainEventPublisher`

---

## Event Publishing (IDomainEventPublisher)

The domain event publisher interface. Uses the same `FinT` return pattern as Repository/Port.

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

**Generic constraint:** `TEvent` must implement `IDomainEvent`.

### PublishResult

A result record that tracks partial success/failure when publishing multiple events.

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

The domain event handler interface. Extends Mediator's `INotificationHandler<TEvent>` to provide source generator compatibility.

```csharp
namespace Functorium.Applications.Events;

public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
    // Inherited from INotificationHandler<TEvent>:
    // ValueTask Handle(TEvent notification, CancellationToken cancellationToken)
}
```

**Generic constraint:** `TEvent` must implement `IDomainEvent`.

| Inherited Method | Return Type | Description |
|------------|----------|------|
| `Handle(TEvent notification, CancellationToken cancellationToken)` | `ValueTask` | Processes domain events (inherited from `INotificationHandler<TEvent>`) |

### Usage Example

```csharp
public sealed class OnOrderCreated : IDomainEventHandler<Order.CreatedEvent>
{
    public async ValueTask Handle(Order.CreatedEvent notification, CancellationToken cancellationToken)
    {
        // Handle side effects such as inventory deduction, notification dispatch, etc.
    }
}
```

---

## Observable Publishers

### ObservableDomainEventPublisher

An `IDomainEventPublisher` decorator with integrated observability (logging, tracing, metrics). Provides observability for event publishing in the Adapter Layer.

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
| `meterFactory` | `IMeterFactory` | Meter factory |
| `openTelemetryOptions` | `IOptions<OpenTelemetryOptions>` | OpenTelemetry configuration |

**Observability items:**

| Item | Name Pattern | Description |
|------|----------|------|
| Meter | `{ServiceNamespace}.adapter.event` | Adapter Layer event Meter |
| Counter (Request) | `adapter.event.requests` | Event publish request count |
| Counter (Response) | `adapter.event.responses` | Event publish response count |
| Histogram (Duration) | `adapter.event.duration` | Event publish processing time (seconds) |

### ObservableDomainEventNotificationPublisher

An `INotificationPublisher` implementation that provides handler-perspective observability (logging, tracing, metrics) for domain event handlers.

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
| `loggerFactory` | `ILoggerFactory` | Factory for creating per-handler Loggers |
| `meterFactory` | `IMeterFactory` | Meter factory |
| `openTelemetryOptions` | `IOptions<OpenTelemetryOptions>` | OpenTelemetry configuration |
| `serviceProvider` | `IServiceProvider` | DI container for resolving `IDomainEventCtxEnricher` |

**Behavior:**
- Applies observability only to Notifications that are `IDomainEvent`.
- Notifications that are not `IDomainEvent` are published via default ForeachAwait without observability.
- Before handler processing, resolves `IDomainEventCtxEnricher<TEvent>` from DI and automatically pushes custom properties to LogContext.

**Observability items:**

| Item | Name Pattern | Description |
|------|----------|------|
| Meter | `{ServiceNamespace}.application` | Application Layer Meter |
| Counter (Request) | `application.usecase.event.requests` | Handler request count |
| Counter (Response) | `application.usecase.event.responses` | Handler response count |
| Histogram (Duration) | `application.usecase.event.duration` | Handler processing time (seconds) |

> **Mediator 3.0 constraint:** Mediator 3.0 does not support `IPipelineBehavior` for `INotification`, and the source generator directly uses concrete types rather than the `INotificationPublisher` interface. Therefore, Scrutor's Decorate pattern does not work, and `NotificationPublisherType` configuration must be used.

### DI Registration

```csharp
services.AddMediator(options =>
{
    options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
});

// RegisterDomainEventHandlersFromAssembly scans for handlers.
```

---

## Ctx Enricher (IUsecaseCtxEnricher, IDomainEventCtxEnricher)

### IUsecaseCtxEnricher\<TRequest, TResponse\>

The interface for enrichers that add business context fields to Usecase logs. The built-in `UsecaseLoggingPipeline` automatically pushes custom properties to `LogContext` when outputting Request/Response logs.

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
| `EnrichRequestLog(TRequest request)` | `IDisposable?` | Push properties to LogContext before Request log output |
| `EnrichResponseLog(TRequest request, TResponse response)` | `IDisposable?` | Push properties to LogContext before Response log output |

**Generic constraint:** `TResponse` must implement `IFinResponse`.

### IDomainEventCtxEnricher\<TEvent\>

The interface for enrichers that add business context fields to domain event handler logs. `ObservableDomainEventNotificationPublisher` automatically pushes custom properties to `LogContext` during Handler processing.

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

| Interface | Method | Description |
|-----------|--------|------|
| `IDomainEventCtxEnricher<TEvent>` | `EnrichLog(TEvent domainEvent)` | Type-safe event log Enrichment |
| `IDomainEventCtxEnricher` (non-generic) | `EnrichLog(IDomainEvent domainEvent)` | Bridge interface used for runtime type resolution and invocation |

> **Implementation rule:** `IDomainEventCtxEnricher`(non-generic) directly. `IDomainEventCtxEnricher<TEvent>` automatically provides the non-generic bridge via Default Interface Method.

**Generic constraint:** `TEvent` must implement `IDomainEvent`.

### CtxEnricherContext

A static utility class that manages the LogContext Push factory. Serves as a bridge connecting `LogContext.PushProperty` from logging frameworks like Serilog to the framework.

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
| `SetPushPropertyFactory(Func<string, object?, IDisposable>)` | `void` | Set LogContext Push factory (called once at application startup) |
| `PushProperty(string name, object? value)` | `IDisposable` | Push property to LogContext with specified name and value |

> **Initialization:** If the factory is not set, `PushProperty` returns a no-op `NullDisposable`.

---

## Source Generator Attributes (\[CtxRoot\], \[CtxIgnore\])

### CtxRootAttribute

An attribute that instructs the source generator to promote the field to `ctx` root level (`ctx.{field}`) when generating CtxEnricher.

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
| `Interface` | The field is promoted to `ctx.{field}` in all Request/Response implementing this interface |
| `Property` | The property is promoted to `ctx.{field}` |
| `Parameter` | The record constructor parameter is promoted to `ctx.{field}` |

### CtxIgnoreAttribute

An attribute that instructs the source generator to exclude from CtxEnricher auto-generation target.

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
| `Class` | The entire Request record is excluded from CtxEnricher auto-generation |
| `Property` | The property is excluded from CtxEnricher auto-generation |
| `Parameter` | The record constructor parameter is excluded from CtxEnricher auto-generation |

### Attribute Usage Example

```csharp
// Apply [CtxRoot] to interface — promoted in all implementing Requests
[CtxRoot]
public interface IHasOrderId
{
    OrderId OrderId { get; }
}

// Apply to individual properties
public sealed record CreateOrderCommand(
    [property: CtxRoot] OrderId OrderId,    // Promoted to ctx.OrderId
    [property: CtxIgnore] string Payload,    // Excluded from Enricher
    Money TotalAmount) : ICommandRequest<CreateOrderResponse>;
```

---

## Related Documents

- [Domain Events Guide](../guides/domain/07-domain-events) -- Design principles, implementation patterns, UsecaseTransactionPipeline integration
- [Entity and Aggregate Specification](./01-entity-aggregate) -- `AggregateRoot<TId>`'s `AddDomainEvent()`, `IHasDomainEvents`
- [Observability Specification](./08-observability) -- Observability field/tag standards, Meter definition rules
