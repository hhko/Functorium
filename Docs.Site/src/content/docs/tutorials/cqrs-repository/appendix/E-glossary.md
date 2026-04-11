---
title: "Glossary"
---
## A

### Aggregate Root
The top-level entity that defines the consistency boundary in a domain model. External access to internal entities is only allowed through the Aggregate Root. In Functorium, it is implemented as `AggregateRoot<TId>`.

```csharp
public class Order : AggregateRoot<OrderId>
{
    public void AddItem(Product product, int qty) { /* ... */ }
}
```

### AggregateRoot\<TId\>
Functorium's Aggregate Root abstract class. Inherits from `Entity<TId>` and includes domain event management functionality.

### AuditableEntity
An entity that implements the `IAuditable` interface. Automatically tracks audit information such as creation time and modification time.

---

## C

### Command
A request that changes the system's state. Returns no value or only a result. Handles write operations in CQRS.

### CQS (Command Query Separation)
A principle defined by Bertrand Meyer. Separates methods into Commands (state change, no return) and Queries (no state change, value return).

### CQRS (Command Query Responsibility Segregation)
A pattern where Greg Young extended CQS to the architecture level. Separates read and write models to optimize each independently.

### CursorPagedResult\<T\>
A type that holds cursor-based pagination results. Includes a cursor value pointing to the next page.

### CursorPageRequest
A cursor-based pagination request. Contains the last cursor value from the previous page and the page size. Outperforms the Offset approach on deep pages.

---

## D

### Dapper
A lightweight ORM (Object-Relational Mapper). Provides object mapping while writing SQL directly. Used for read performance optimization in Query-side adapters.

### DapperQueryBase
Functorium's Dapper-based Query adapter abstract class. Generates SQL to implement IQueryPort.

### Domain Event
An immutable object representing a meaningful occurrence in the domain. Implements the `IDomainEvent` interface.

```csharp
public record OrderCreatedEvent(OrderId OrderId) : IDomainEvent;
```

### DomainEventCollector
A service that collects and publishes domain events. Publishes events added by Aggregate Roots after transaction commit.

### DTO (Data Transfer Object)
An object for transferring data between layers. In CQRS, the Query side returns DTOs optimized for reads.

---

## E

### EF Core (Entity Framework Core)
Microsoft's ORM framework. Used for Command-side Repository implementation.

### EfCoreRepositoryBase
Functorium's EF Core-based Repository abstract class. Separates domain models from DB models through `ToDomain`/`ToModel` conversion methods.

### Entity\<TId\>
Functorium's entity abstract class. The base class for domain objects identified by ID.

---

## F

### Fin\<T\>
LanguageExt's Result type. Represents success(T) or failure(Error). Check state with `IsSucc`/`IsFail`.

### FinResponse\<T\>
Functorium's Usecase return type. Based on `Fin<T>` and compatible with the Mediator pipeline.

### FinT\<IO, T\>
LanguageExt's monad transformer. Wraps `IO<Fin<T>>` to support functional composition. The return type of Repository methods.

---

## G

### guard()
A LanguageExt function. Fails the FinT pipeline if the condition is not met.

```csharp
from _ in guard(order.CanCancel(), Error.New("Cannot cancel"))
```

---

## I

### IAuditable
An interface for tracking audit information (creation time, modification time, etc.).

### ICommandRequest\<TSuccess\>
The request interface for Command Usecases. Inherits from `ICommand<FinResponse<TSuccess>>`.

### ICommandUsecase\<TCommand, TSuccess\>
The handler interface for Command Usecases. Inherits from `ICommandHandler`.

### IDomainEvent
The marker interface for domain events.

### IDomainEventCollector
An interface for collecting and tracking domain events. Collects Aggregate events through the Track method.

### ICacheable
An interface for Usecase response caching. Defining `CacheKey` and `Expiration` allows `UsecaseCachingPipeline` to automatically cache responses.

### IEntityId\<TId\>
The Entity ID interface. Implemented based on Ulid.

### InMemoryQueryBase
Functorium's InMemory-based Query adapter abstract class. Used for testing.

### InMemoryRepositoryBase
Functorium's InMemory-based Repository abstract class. Stores data in memory using `ConcurrentDictionary`.

### IO
LanguageExt's pure functional IO effect type. Explicitly represents side effects.

### IObservablePort
A marker interface for Observability. Uses the `RequestCategory` property to distinguish Command/Query for metric collection. Both `IRepository` and `IQueryPort` inherit from it.

### IQueryPort\<TEntity, TDto\>
The Query-side adapter interface. Supports Specification-based search, pagination, and streaming.

### IQueryRequest\<TSuccess\>
The request interface for Query Usecases. Inherits from `IQuery<FinResponse<TSuccess>>`.

### IQueryUsecase\<TQuery, TSuccess\>
The handler interface for Query Usecases. Inherits from `IQueryHandler`.

### IRepository\<TAggregate, TId\>
The Command-side Repository interface. Defines 8 CRUD methods at the Aggregate Root level.

### ISoftDeletable
An interface supporting logical deletion. Sets a deletion flag instead of physically deleting.

### IUnitOfWork
The Unit of Work interface. Persists changes with `SaveChanges()` and starts explicit transactions with `BeginTransactionAsync()`.

### IUnitOfWorkTransaction
The explicit transaction scope interface. Commits with `CommitAsync()`, and uncommitted transactions are automatically rolled back on Dispose.

---

## M

### Mediator Pattern
A pattern that removes direct dependencies between requests and handlers. Functorium uses the Mediator library to dispatch Commands/Queries.

---

## P

### PagedResult\<T\>
A type that holds offset-based pagination results. Includes total count, current page, page size, and data list.

### PageRequest
An offset-based pagination request. Contains page number and page size.

### Pipeline
The Mediator's request processing pipeline. Handles cross-cutting concerns such as validation, logging, and transactions.

---

## Q

### Query
A request that returns data without changing the system's state. Handles read operations in CQRS.

### Query Adapter
An implementation of IQueryPort. Includes InMemoryQueryBase, DapperQueryBase, etc.

---

## R

### Repository Pattern
A pattern that abstracts data access logic. In CQRS, it handles Aggregate Root persistence on the Command side.

---

## S

### SortDirection
A SmartEnum representing sort direction. Has two values: `SortDirection.Ascending` and `SortDirection.Descending`.

### SortExpression
A type that represents sort criteria. Has a private constructor and is created via factory methods.
- `SortExpression.Empty` -- No sorting (default)
- `SortExpression.By("Name")` -- Single field ascending
- `SortExpression.By("Price", SortDirection.Descending)` -- Single field descending
- `.ThenBy("Name")` -- Add secondary sort

### Specification\<T\>
An abstract class that encapsulates business rules. Uses the `IsSatisfiedBy` method to determine whether a candidate object satisfies the criteria. Used as search criteria for IQueryPort.

```csharp
var spec = new ActiveOrderSpec() & new OrderByCustomerSpec(customerId);
var result = await query.Search(spec, page, sort).RunAsync();
```

### Stream
The streaming query method of IQueryPort. Returns `IAsyncEnumerable<TDto>` to process large datasets record by record without loading them entirely into memory.

---

## T

### ToFinResponse()
An extension method that converts `Fin<T>` to `FinResponse<T>`. Used for type conversion from the Repository layer to the Usecase layer.

### Transaction Pipeline
A pipeline that automatically calls SaveChanges and publishes domain events after Command Usecase execution.

---

## U

### Ulid
Universally Unique Lexicographically Sortable Identifier. Functorium's Entity IDs are Ulid-based. More sortable than UUID and generated in chronological order.

### Unit of Work
A pattern that groups multiple Repository operations into a single transaction. Commits all at once with `IUnitOfWork.SaveChanges()`.

---

## V

### Value Object
A domain object identified by its values rather than an ID. Immutable with equality comparison.

---

Let's review the books, online resources, and related libraries for further learning.

-> [Appendix F: References](../F-references/)
