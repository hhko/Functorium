---
title: "Domain Events"
---

This document explains how to define, publish, and subscribe to domain events in the Functorium framework.

## Introduction

"Where should inventory deduction and email sending be handled after an order is created?"
"How do you connect side effects without coupling between Aggregates?"
"What happens to an already committed transaction if an event handler fails?"

Domain events are the core mechanism for connecting state changes within Aggregates to external concerns. This document covers the complete flow from event definition, publishing, and handler implementation to transaction integration.

### What You Will Learn

This document covers the following topics:

1. **Role and characteristics of domain events** -- Eventual consistency between Aggregates and separation of concerns
2. **IDomainEvent / DomainEvent type hierarchy** -- Event traceability (EventId, CorrelationId, CausationId)
3. **Nested class event definition pattern** -- Explicit ownership in the form of `Product.CreatedEvent`
4. **UsecaseTransactionPipeline integration** -- Automatic event publishing flow after SaveChanges
5. **Event handler implementation and testing** -- `IDomainEventHandler<T>` pattern and unit testing

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [Entity/Aggregate Core Patterns](../06b-entity-aggregate-core) -- How to use `AddDomainEvent()` in AggregateRoot
- [Error System: Basics and Naming](../08a-error-system) -- `Fin<T>` return pattern

> Domain events are immutable objects representing "facts that have already occurred." They connect side effects across Aggregate boundaries (stock deduction, notification sending, etc.) without coupling, and `UsecaseTransactionPipeline` automatically handles publishing after SaveChanges.

## Summary

### Key Commands

```csharp
// Domain event definition (nested record within Aggregate)
public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;

// Event publishing (inside AggregateRoot)
AddDomainEvent(new CreatedEvent(Id, totalAmount));

// Event Handler implementation
public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>

// Handler registration (DI)
services.RegisterDomainEventHandlersFromAssembly(AssemblyReference.Assembly);
```

### Key Procedures

1. **Event definition**: Inherit `DomainEvent` as `sealed record` inside Aggregate Root, use past-tense names
2. **Event publishing**: Call `AddDomainEvent()` immediately after state changes
3. **Event handler creation**: Implement `IDomainEventHandler<T>`, `On{EventName}` naming pattern
4. **Handler registration**: Scan-register with `RegisterDomainEventHandlersFromAssembly`
5. **Automatic processing**: `UsecaseTransactionPipeline` automatically performs event publishing after SaveChanges

### Key Concepts

| Concept | Description |
|------|------|
| `IDomainEvent` | Extends `INotification`, includes `OccurredAt`, `EventId`, `CorrelationId`, `CausationId` |
| `DomainEvent` | Base abstract record, auto-sets timestamp/ID |
| `IHasDomainEvents` | Read-only event querying (public) |
| `IDomainEventDrain` | Event cleanup interface (internal, infrastructure only) |
| `UsecaseTransactionPipeline` | Automatic SaveChanges -> event publishing |
| Nested class events | Explicit ownership in `Product.CreatedEvent` form |

---

## Why Domain Events

Domain events are a tactical pattern in DDD (Domain-Driven Design) that **explicitly represents "significant occurrences in the domain."**

### Problems Domain Events Solve

**Eventual Consistency Between Aggregates:**
Only one Aggregate is changed per transaction, and changes to other Aggregates are handled asynchronously via events. This enables collaboration between domains without breaking Aggregate boundaries.

**Separation of Concerns:**
Separates core domain logic from side effects (logging, notifications, external system integration). Order creation logic focuses only on "creating the order," while email sending and stock deduction are handled by event handlers.

**Audit Trail:**
Records what happened in the domain via events. Each event includes the occurrence time (`OccurredAt`), enabling chronological tracking of domain changes.

**Extensibility:**
When new side effects are needed, simply add a new event handler without modifying existing code (Open-Closed Principle).

Now that we understand the problems domain events solve, let us examine what types are used to represent events in Functorium.

---

## What Are Domain Events (WHAT)

Domain events represent significant occurrences in the domain. They can only be published from AggregateRoot.

### Characteristics of Domain Events

The core characteristics that domain events should have are as follows.

| Property | Description | Example |
|------|------|------|
| **Past Tense** | Represents facts that have already occurred | `CreatedEvent`, `ConfirmedEvent` |
| **Immutable** | Cannot be changed once created | Defined as `sealed record` |
| **Includes Time Information** | Records occurrence time | `OccurredAt` property |
| **Event Identification** | Prevents duplication via unique ID | `EventId` (Ulid) |
| **Request Tracking** | Links events from the same request | `CorrelationId` |
| **Causation** | Tracks cause-effect between events | `CausationId` |

### IDomainEvent / DomainEvent

**Location**: `Functorium.Domains.Events`

The key point to note in the following code is that `IDomainEvent` extends `INotification`, naturally integrating with Mediator Pub/Sub.

```csharp
// Interface -- Pub/Sub integration by extending Mediator.INotification
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
    Ulid EventId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}

// Base record
public abstract record DomainEvent(
    DateTimeOffset OccurredAt,
    Ulid EventId,
    string? CorrelationId,
    string? CausationId) : IDomainEvent
{
    protected DomainEvent() : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), null, null) { }
    protected DomainEvent(string? correlationId) : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, null) { }
    protected DomainEvent(string? correlationId, string? causationId) : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, causationId) { }
}
```

All convenience constructors are `protected`. `DomainEvent` is not created directly but used by inheriting as `sealed record`.

**Event Traceability:**
- `EventId`: Unique event identifier. Used for deduplication (idempotency) and event tracking.
- `CorrelationId`: Tracks events from the same request as a group.
- `CausationId`: The ID of the previous event that caused this event, tracking causation between events.

### CorrelationId Propagation Flow

`CorrelationId` is a business-level identifier connecting all domain events from a single request:

```
HTTP Request
  -> Middleware: Generate CorrelationId or extract from header
    -> Usecase execution
      → Entity.AddDomainEvent(new CreatedEvent(...) { CorrelationId = correlationId })
        -> Event Handler: Track events with same CorrelationId
```

The roles of the two identifiers are distinguished as follows.

| Identifier | Level | Purpose |
|--------|------|------|
| `CorrelationId` | Business | Grouping events from the same request |
| OpenTelemetry `TraceId` | Infrastructure | Distributed system request tracing (span-based) |

The two identifiers are independent but complementary. `CorrelationId` tracks business flows, `TraceId` analyzes infrastructure performance.

### Event Naming Conventions

Event names use past tense:

| Domain Action | Event Name |
|------------|------------|
| Creation | `CreatedEvent` |
| Confirmation | `ConfirmedEvent` |
| Cancellation | `CancelledEvent` |
| Shipping | `ShippedEvent` |

### Functorium Type Hierarchy

```
IDomainEvent : INotification (interface)
├── OccurredAt (DateTimeOffset)
├── EventId (Ulid)
├── CorrelationId (string?)
└── CausationId (string?)
    │
    └── DomainEvent (abstract record)
        ├── Default constructor: Auto-sets OccurredAt, EventId
        ├── CorrelationId constructor: Specifies request tracking ID
        ├── Full constructor: Specifies CorrelationId + CausationId
        └── User-defined events inherit from this
```

### IHasDomainEvents / IDomainEventDrain Pattern

The two interfaces managing domain events in AggregateRoot are separated:

```csharp
// Read-only contract of the domain layer -- allows only event querying
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
}

// Event cleanup interface for infrastructure (internal)
internal interface IDomainEventDrain : IHasDomainEvents
{
    void ClearDomainEvents();
}
```

**Design Principle**: Domain events are **immutable facts.** The domain contract (`IHasDomainEvents`) does not allow event deletion, and event cleanup is separated as an infrastructure concern (`IDomainEventDrain`).

| Interface | Visibility | Role |
|-----------|--------|------|
| `IHasDomainEvents` | `public` | Query event list from domain layer |
| `IDomainEventDrain` | `internal` | Cleanup after event publishing (infrastructure only) |

> **Note**: Although `IDomainEventDrain` is `internal`, `AggregateRoot<TId>.ClearDomainEvents()` is `public`. This is an intentional design to allow test code to directly call `order.ClearDomainEvents()` to clean up previous events. In production code, `ClearDomainEvents()` should only be called by infrastructure (Publisher).

Now that we understand the event structure and characteristics, let us look at how to define and publish events.

---

## Event Definition (HOW)

Domain events are defined as **nested classes** within the corresponding Entity:

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Domain Events

    // Domain event (nested class)
    public sealed record CreatedEvent(OrderId OrderId, CustomerId CustomerId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId, string Reason) : DomainEvent;

    #endregion

    // Entity implementation...
}
```

**Advantages**:
- Event ownership is explicit in the type system (`Order.CreatedEvent`)
- IntelliSense shows all related events when typing `Order.`
- Eliminates Entity name duplication (`OrderCreatedEvent` -> `Order.CreatedEvent`)
- **Event publishing origin is explicit in Handler**: When a Handler inherits `IDomainEventHandler<Product.CreatedEvent>`, reading the code alone immediately reveals "this is an event published by the Product Entity"

**Usage Example**:
```csharp
// Inside Entity (concise)
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));

// From outside (explicit)
public void Handle(Order.CreatedEvent @event) { ... }
```

---

## Event Publishing (HOW)

Now that event definition is covered, let us examine the flow of collecting events inside Aggregates and the pipeline automatically publishing them.

### Collecting Events in AggregateRoot

Events are collected using `AddDomainEvent()` within AggregateRoot.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;
    public sealed record ShippedEvent(OrderId OrderId, Address ShippingAddress) : DomainEvent;

    #endregion

    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    private Order(OrderId id, Money totalAmount) : base(id)
    {
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    // Create: Receives already validated Value Objects directly
    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        // Publish creation event (concise from inside)
        order.AddDomainEvent(new CreatedEvent(id, totalAmount));
        return order;
    }

    public sealed record InvalidStatus : DomainErrorKind.Custom;

    public Fin<Unit> Ship(Address address)
    {
        if (Status != OrderStatus.Confirmed)
            return DomainError.For<Order>(
                new InvalidStatus(),
                Status.ToString(),
                "Order must be confirmed before shipping");

        Status = OrderStatus.Shipped;
        // Publish shipping event
        AddDomainEvent(new ShippedEvent(Id, address));
        return unit;
    }
}
```

### UsecaseTransactionPipeline Integration

`IDomainEvent` extends Mediator's `INotification` to support Pub/Sub integration.

**SaveChanges and domain event publishing are handled automatically by `UsecaseTransactionPipeline`.** There is no need to directly inject `IUnitOfWork` or `IDomainEventPublisher` in the Usecase.

The key point to note in the following code is that the Usecase only injects the Repository and does not directly call SaveChanges or event publishing.

```csharp
internal sealed class Usecase(
    IProductRepository productRepository)   // Only inject Repository
    : ICommandUsecase<Request, Response>
{
    private readonly IProductRepository _productRepository = productRepository;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // ... existing validation logic ...

        FinT<IO, Response> usecase =
            from exists in _productRepository.ExistsByName(productName)
            from _ in guard(!exists, /* error */)
            from product in _productRepository.Create(newProduct)  // Repository automatically calls IDomainEventCollector.Track()
            select new Response(...);
        // SaveChanges + domain event publishing are automatically handled by UsecaseTransactionPipeline

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### Pipeline Publishing Flow

`UsecaseTransactionPipeline` automatically handles UoW commit and domain event publishing for Command Usecases (Queries are excluded at compile time via the `where ICommand<TResponse>` constraint).

**Event collection timing**: During Usecase execution, the Repository's `Create`/`Update` methods call `IDomainEventCollector.Track(aggregate)` to register changed Aggregates for tracking. (`IDomainEventCollector` is defined in the `Functorium.Applications.Events` namespace, and the Adapter Layer's `DomainEventCollector` implements it.) Inside the Aggregate, events are collected via `AddDomainEvent()`.

**Pipeline execution order**:

1. **Handler execution** (`next()`) -> On failure, no commit, immediately returns failure response
2. **`UoW.SaveChanges()`** -> Transaction commit. On failure, no event publishing, returns failure response
3. **`IDomainEventPublisher.PublishTrackedEvents()`** -> Collects events from tracked Aggregates in `IDomainEventCollector`, publishes via Mediator, calls `ClearDomainEvents()` after publishing

```
Usecase Handler execution
  -> Repository.Create(entity)  --> IDomainEventCollector.Track(entity)
  -> entity.AddDomainEvent(...)  --> Events accumulate in entity.DomainEvents
  -> Handler complete (success)
    -> UoW.SaveChanges()         --> DB commit
    -> PublishTrackedEvents()    --> Publish tracked Aggregate events -> ClearDomainEvents()
```

### Transaction Considerations

The following summarizes behavior based on success/failure combinations of save and event publishing.

| Situation | Behavior |
|------|------|
| Save success, event publishing success | Normal processing |
| Save failure | No event publishing (pipeline returns Fail response) |
| Save success, event publishing failure | Save is committed, success response maintained (eventual consistency) |

- Event publishing runs only after `SaveChanges()` succeeds (guaranteed by pipeline)
- On publishing failure, business logic is already committed (eventual consistency, warning logged)
- If strong consistency is needed, consider the Outbox pattern

> **Reference**: For pipeline details, see [11-usecases-and-cqrs.md -- Transactions and Event Publishing](../application/11-usecases-and-cqrs#transactions-and-event-publishing-usecasetransactionpipeline).

Now that we understand the event publishing flow, let us look at how to implement handlers that receive published events and process side effects.

---

## Event Handler Implementation (HOW)

### What is an Event Handler?

An Event Handler is an **Event-Driven Use Case.** Like Command/Query Use Cases, it belongs to the Application Layer, but the trigger is different:

| Use Case Type | Trigger | Role |
|---------------|--------|------|
| Command | External request (write) | State change |
| Query | External request (read) | Data retrieval |
| **Event Handler** | Domain event | Perform side effects |

### Advantages of Nested Class Events

When domain events are defined as nested classes of an Entity (`Product.CreatedEvent`), the **event publishing origin** becomes clear from the Handler declaration alone:

```csharp
// Just looking at the Handler declaration reveals "this is a CreatedEvent published by Product"
public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>
```

| Comparison | Nested Class Event | Independent Class Event |
|------|-------------------|-------------------|
| Handler declaration | `IDomainEventHandler<Product.CreatedEvent>` | `IDomainEventHandler<ProductCreatedEvent>` |
| Identifying publisher | **Explicit in the type system** (`Product.`) | Depends on naming conventions |
| IntelliSense | Shows related event list when typing `Product.` | Requires searching among all events |
| Cohesion | Entity and events placed together | Events scattered in separate files/folders |

### Naming Conventions

| Handler Type | Naming Pattern | Example |
|------------|----------|------|
| Command/Query Handler | `{Command/Query}Handler` | `CreateProductHandler`, `GetProductHandler` |
| Domain Event Handler | `On{EventName}` | `OnProductCreated`, `OnOrderConfirmed` |

Domain Event Handlers use only the `On` prefix:
- The `On` prefix already indicates it is an event handler, so the `Handler` suffix is redundant
- Naturally distinguished from Command/Query Handlers
- Concise and improved readability

| Category | Pattern | Example |
|------|------|------|
| File name | `On{EventName}.cs` | `OnProductCreated.cs` |
| Class name | `On{EventName}` | `OnProductCreated` |

### Folder Location

Event Handlers are placed alongside Commands and Queries in the Usecases folder of the related entity:

```
Usecases/
└── Products/
    ├── CreateProductCommand.cs      # Command
    ├── GetProductByIdQuery.cs       # Query
    └── OnProductCreated.cs          # Event Handler
```

### Basic Structure

```csharp
using Functorium.Applications.Events;

namespace {Project}.Application.Usecases.{Entity};

/// <summary>
/// {Event} handler - {description of processing}
/// </summary>
public sealed class On{EventName} : IDomainEventHandler<{Entity}.{Event}>
{
    public On{EventName}(/* dependency injection */)
    {
    }

    public ValueTask Handle({Entity}.{Event} notification, CancellationToken cancellationToken)
    {
        // Side effect processing: logging, notifications, external system integration, etc.
        return ValueTask.CompletedTask;
    }
}
```

### Complete Example

The key point to note in the following code is that by implementing `IDomainEventHandler<Product.CreatedEvent>`, the handler declaration alone immediately reveals which Aggregate's event is being handled.

```csharp
using Functorium.Applications.Events;
using LayeredArch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Product.CreatedEvent handler - logs product creation.
/// </summary>
public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>
{
    private readonly ILogger<OnProductCreated> _logger;

    public OnProductCreated(ILogger<OnProductCreated> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.CreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Product created: {ProductId}, Name: {Name}, Price: {Price}",
            notification.ProductId,
            notification.Name,
            notification.Price);

        return ValueTask.CompletedTask;
    }
}
```

### Handler Observability

Event Handlers are Event-Driven Usecases. Therefore, the same observability patterns as Command/Query Usecases apply. `ObservableDomainEventNotificationPublisher` automatically provides Handler-perspective observability.

#### ObservableDomainEventNotificationPublisher Configuration

To enable Handler-perspective observability, set `NotificationPublisherType`:

```csharp
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
});
services.RegisterDomainEventPublisher();
```

- `NotificationPublisherType`: The Publisher type used when Mediator publishes `INotification`. Specifying `ObservableDomainEventNotificationPublisher` automatically applies per-Handler Logging (Event ID 1001-1004), Metrics, and Tracing.
- `RegisterDomainEventPublisher()`: Registers 3 services in DI: `IDomainEventPublisher`, `IDomainEventCollector`, `ObservableDomainEventNotificationPublisher`.

#### Auto-Generated Observability

| Signal | Auto-Generated Content |
|------|--------------|
| **Logging** | Handler Request/Response logs (Event ID 1001-1004), `request.category.type: "event"` |
| **Metrics** | `application.usecase.event.requests/responses/duration` Counter/Histogram |
| **Tracing** | `application usecase.event {Handler}.Handle` Span |

#### IDomainEventCtxEnricher\<TEvent\> -- Adding Business Context Fields

In addition to the auto-generated standard observability, `DomainEventCtxEnricherGenerator` detects `IDomainEventHandler<T>` implementation classes and auto-generates `ctx.*` fields suited to the business context:

```csharp
// Handler definition -> DomainEventCtxEnricherGenerator detects and auto-generates Enricher
public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
{
    public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct) { ... }
}

// Auto-generated: OrderPlacedEventCtxEnricher
//   ctx.customer_id (Root), ctx.order_placed_event.order_id, ctx.order_placed_event.total_amount, ...
```

- `[CtxRoot]`: Applied to event properties/interfaces -> Promoted to `ctx.{field}` Root Level.
- `[CtxIgnore]`: Applied to event classes/properties -> Excluded from generation.
- `partial void OnEnrichLog()`: Extension point for adding computed fields to auto-generated Enrichers.

DI registration:
```csharp
services.AddScoped<
    IDomainEventCtxEnricher<OrderPlacedEvent>,
    OrderPlacedEventCtxEnricher>();
```

> **Details**: See [Logging Manual - IDomainEventCtxEnricher](../observability/19-observability-logging#idomaineventctxenrichertvent--event-handler-log-enrichment).

### Usage Scenarios

| Scenario | Description |
|----------|------|
| Logging/Auditing | Record domain events |
| Notification sending | Email, push notifications, etc. |
| External system integration | Payment, shipping system calls |
| Cache invalidation | Update related caches |
| Search index update | Synchronize Elasticsearch, etc. |

### Handler Registration

> **Caution**: `Mediator.SourceGenerator` only auto-registers handlers within the project where the package is referenced.
> Handlers in other assemblies (e.g., the Application layer) must be registered explicitly.

Scrutor is used to scan and register handlers from assemblies:

```csharp
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    // Enable Handler-perspective observability
    options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
});
// Register IDomainEventPublisher, IDomainEventCollector, ObservableDomainEventNotificationPublisher
services.RegisterDomainEventPublisher();

// Register domain event handlers from the Application layer
services.RegisterDomainEventHandlersFromAssembly(
    YourApp.Application.AssemblyReference.Assembly);

```

- `NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher)`: Automatically applies Logging, Metrics, and Tracing when Handlers execute. Without this setting, Handler-perspective observability is disabled.
- `RegisterDomainEventPublisher()`: Registers 3 services in DI: `IDomainEventPublisher` (publishing), `IDomainEventCollector` (collection), `ObservableDomainEventNotificationPublisher` (observability).
- `RegisterDomainEventHandlersFromAssembly()`: Uses Scrutor's `Scan()` API to scan and register `IDomainEventHandler<T>` implementations from the specified assembly.

---

## Bulk Event Processing (Domain Service Pattern)

When N events are generated in bulk operations (`CreateRange`, `DeleteRange`), **Domain Services** can use `IDomainEventCollector.TrackEvent()` to directly register events. Events are automatically published by `UsecaseTransactionPipeline` after SaveChanges.

```csharp
// Bulk creation + direct event registration in Domain Service
public class ProductBulkOperations
{
    private readonly IDomainEventCollector _collector;

    public ProductBulkOperations(IDomainEventCollector collector)
        => _collector = collector;

    public List<Product> CreateBulk(IEnumerable<CreateProductRequest> requests)
    {
        var products = new List<Product>();
        foreach (var request in requests)
        {
            var product = Product.Create(request.Name, request.Price);
            _collector.TrackEvent(new Product.CreatedEvent(product.Id, product.Name, product.Price));
            products.Add(product);
        }
        return products;
    }
}
```

When a Usecase calls the Domain Service, `UsecaseTransactionPipeline` automatically publishes the tracked events.

---

## Test Patterns

### Event Publishing Verification

Verify that the correct events have been added to the `DomainEvents` collection after Entity state changes:

```csharp
[Fact]
public void Create_ShouldRaise_CreatedEvent()
{
    // Arrange & Act
    var order = Order.Create(Money.Create(10000m).ThrowIfFail());

    // Assert
    order.DomainEvents.ShouldContain(e => e is Order.CreatedEvent);
}

[Fact]
public void Confirm_ShouldRaise_ConfirmedEvent()
{
    // Arrange
    var order = Order.Create(Money.Create(10000m).ThrowIfFail());
    order.ClearDomainEvents();  // Remove creation events

    // Act
    var result = order.Confirm();

    // Assert
    result.IsSucc.ShouldBeTrue();
    order.DomainEvents.ShouldContain(e => e is Order.ConfirmedEvent);
}
```

### Event Data Verification

Verify that events contain the correct data:

```csharp
[Fact]
public void Create_CreatedEvent_ShouldContainCorrectData()
{
    // Arrange & Act
    var amount = Money.Create(10000m).ThrowIfFail();
    var order = Order.Create(amount);

    // Assert
    var createdEvent = order.DomainEvents
        .OfType<Order.CreatedEvent>()
        .ShouldHaveSingleItem();

    createdEvent.OrderId.ShouldBe(order.Id);
    createdEvent.TotalAmount.ShouldBe(amount);
}
```

### Event Handler Unit Tests

Event Handlers are unit tested by mocking dependencies:

```csharp
[Fact]
public async Task Handle_ShouldLogProductCreation()
{
    // Arrange
    var logger = Substitute.For<ILogger<OnProductCreated>>();
    var handler = new OnProductCreated(logger);
    var @event = new Product.CreatedEvent(ProductId.New(), "Test Product", 1000m);

    // Act
    await handler.Handle(@event, CancellationToken.None);

    // Assert
    logger.ReceivedWithAnyArgs(1).LogInformation(default!);
}
```

---

## Checklist

### Event Definition

- [ ] Are event names in past tense? (`CreatedEvent`, `UpdatedEvent`)
- [ ] Are events defined as nested records of the Aggregate Root?
- [ ] Do they inherit from `DomainEvent` base record?
- [ ] Are the necessary identifiers (EntityId) included in the event?

### Event Publishing

- [ ] Is `AddDomainEvent()` called immediately after state changes?
- [ ] Is `UsecaseTransactionPipeline` configured for automatic publishing? (Verify `UseTransaction()` registration)

### Event Handlers

- [ ] Does the Event Handler name follow the `On{EventName}` pattern?
- [ ] Is the Event Handler placed alongside Command/Query in the Usecases folder?
- [ ] Does it implement `IDomainEventHandler<T>`?
- [ ] Is the handler registered with `RegisterDomainEventHandlersFromAssembly`?
- [ ] Is `NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher)` configured?
- [ ] Has the DI registration of `IDomainEventCtxEnricher<TEvent>` auto-generated by `DomainEventCtxEnricherGenerator` been verified?

---

## Future Advanced Patterns

Advanced patterns needed as service maturity increases. Currently unimplemented and will be introduced incrementally as needed.

- **Outbox pattern**: Guarantee atomicity between DB transactions and event publishing
- **Event Versioning**: Backward compatibility strategy for event schema changes
- **Saga / Process Manager**: Long-running transaction coordination across multiple Aggregates
- **Event reprocessing strategy**: Idempotency guarantee patterns

---

## Troubleshooting

### Domain events are not received by handlers

**Cause:** The Event Handler may not be registered in the DI container. `Mediator.SourceGenerator` only auto-registers handlers within the project where the package is referenced.

**Resolution:** Handlers in other assemblies such as the Application layer must be explicitly registered with `RegisterDomainEventHandlersFromAssembly`:
```csharp
services.RegisterDomainEventHandlersFromAssembly(
    YourApp.Application.AssemblyReference.Assembly);
```

### Event publishing fails after SaveChanges succeeds

**Cause:** `UsecaseTransactionPipeline` publishes events after SaveChanges succeeds. If an exception occurs in a handler, event publishing fails but data is already committed (eventual consistency).

**Resolution:** If strong consistency is needed, introduce the Outbox pattern. In the current structure, it is recommended to properly handle exceptions inside handlers and record warning logs.

### Previous events interfere with Assert in tests

**Cause:** Events published in `Create()` remain in the `DomainEvents` collection, interfering with subsequent behavior verification.

**Resolution:** Call `entity.ClearDomainEvents()` in the Arrange step of the test to clean up previous events before performing the Act:
```csharp
var order = Order.Create(...);
order.ClearDomainEvents();  // Remove creation events
order.Confirm();            // Test subsequent behavior
```

---

## FAQ

### Q1. Why are domain events defined as nested classes rather than independent classes?

Defining as nested classes makes event ownership explicit in the type system, as in `Product.CreatedEvent`. Typing `Product.` in IntelliSense displays all related events, and the publishing origin can be immediately identified from the Event Handler declaration alone.

### Q2. What is the difference between CorrelationId and OpenTelemetry TraceId?

`CorrelationId` is a business-level identifier that groups events from the same request. `TraceId` is an infrastructure-level identifier that traces requests across distributed systems. The two identifiers are independent but used complementarily.

### Q3. Can an Event Handler modify another Aggregate?

It is possible, but directly modifying another Aggregate in an Event Handler makes the transaction boundary ambiguous. If modification of another Aggregate is needed, it is recommended to issue a Command to that Aggregate or call a separate Usecase.

### Q4. Should IUnitOfWork or IDomainEventPublisher be directly injected in the Usecase?

No. `UsecaseTransactionPipeline` automatically handles SaveChanges and event publishing. Only the Repository needs to be injected in the Usecase.

### Q5. Can the execution order of event handlers be guaranteed?

Mediator's default behavior does not guarantee handler execution order. If order is important, process sequentially within a single handler or consider the Saga/Process Manager pattern.

---

## References

- [06a-aggregate-design.md](../06a-aggregate-design) - Aggregate design, [06b-entity-aggregate-core.md](../06b-entity-aggregate-core) - Entity/Aggregate core patterns, [06c-entity-aggregate-advanced.md](../06c-entity-aggregate-advanced) - Advanced patterns
- [11-usecases-and-cqrs.md](../application/11-usecases-and-cqrs) - Use Case implementation
- [11-usecases-and-cqrs.md - Transactions and Event Publishing](../application/11-usecases-and-cqrs#transactions-and-event-publishing-usecasetransactionpipeline) - Pipeline automatic processing pattern
- [13-adapters.md](../adapter/13-adapters) - UoW Adapter implementation
- [19-observability-logging.md](../observability/19-observability-logging) - Logging and Ctx Enricher
- [08-observability.md](../../spec/08-observability) - Observability specification
