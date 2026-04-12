---
title: "Usecase CQRS Specification"
---

This specification defines the public API for Functorium's CQRS request interfaces, `FinResponse<A>` discriminated union, FinT LINQ extensions, and caching/persistence contracts.

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `ICommandRequest<TSuccess>` | `Functorium.Applications.Usecases` | Command request marker interface |
| `ICommandUsecase<TCommand, TSuccess>` | `Functorium.Applications.Usecases` | Command Handler interface |
| `IQueryRequest<TSuccess>` | `Functorium.Applications.Usecases` | Query request marker interface |
| `IQueryUsecase<TQuery, TSuccess>` | `Functorium.Applications.Usecases` | Query Handler interface |
| `FinResponse<A>` | `Functorium.Applications.Usecases` | Succ/Fail discriminated union (supports Match, Map, Bind, LINQ) |
| `FinResponse` | `Functorium.Applications.Usecases` | Static factory class (`Succ`, `Fail`) |
| `IFinResponse` | `Functorium.Applications.Usecases` | Non-generic base interface (`IsSucc`/`IsFail`) |
| `IFinResponse<out A>` | `Functorium.Applications.Usecases` | Covariant generic interface |
| `IFinResponseFactory<TSelf>` | `Functorium.Applications.Usecases` | CRTP-based Fail creation interface |
| `IFinResponseWithError` | `Functorium.Applications.Usecases` | Error access interface (for Pipeline) |
| `FinToFinResponse` | `Functorium.Applications.Usecases` | `Fin<A>` to `FinResponse<A>` conversion extension methods |
| `FinTLinqExtensions` | `Functorium.Applications.Linq` | FinT monad transformer LINQ extension methods |
| `ICacheable` | `Functorium.Applications.Usecases` | Caching contract interface |
| `IUnitOfWork` | `Functorium.Applications.Persistence` | Persistence transaction contract |
| `IUnitOfWorkTransaction` | `Functorium.Applications.Persistence` | Explicit transaction scope |
| `CtxIgnoreAttribute` | `Functorium.Applications.Usecases` | CtxEnricher auto-generation exclusion attribute |

### Core Rules

- **`FinResponse<A>`** uses `IsSucc`/`IsFail` (not `IsSuccess`)
- Success values can only be accessed via `ThrowIfFail()` or `Match()`
- **Static factories** are `FinResponse.Succ(value)` and `FinResponse.Fail<T>(error)` (not `FinResponse<T>.Succ()`)
- Both Command and Query use `FinResponse<TSuccess>` as the return type

Having reviewed the key types and core rules in the summary, the following sections detail each type's API.

---

## CQRS Request Interfaces

Functorium's CQRS interfaces are based on [Mediator](https://github.com/martinothamar/Mediator) the [Mediator](https://github.com/martinothamar/Mediator) library's `ICommand<T>`/`IQuery<T>`, fixing the return type to `FinResponse<TSuccess>` to enforce the Result pattern.

### Command Interface

```csharp
// Request interface — implemented as record
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>>
{
}

// Handler interface — implemented as Usecase class
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess>
{
}
```

| Type Parameter | Constraint | Description |
|--------------|------|------|
| `TSuccess` | None | Data type to return on success |
| `TCommand` | `ICommandRequest<TSuccess>` | Command request type (contravariant) |

### Query Interface

```csharp
// Request interface — implemented as record
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>>
{
}

// Handler interface — implemented as Usecase class
public interface IQueryUsecase<in TQuery, TSuccess>
    : IQueryHandler<TQuery, FinResponse<TSuccess>>
    where TQuery : IQueryRequest<TSuccess>
{
}
```

| Type Parameter | Constraint | Description |
|--------------|------|------|
| `TSuccess` | None | Data type to return on success |
| `TQuery` | `IQueryRequest<TSuccess>` | Query request type (contravariant) |

### Inheritance Hierarchy

```
Mediator.ICommand<FinResponse<TSuccess>>
  └─ ICommandRequest<TSuccess>

Mediator.ICommandHandler<TCommand, FinResponse<TSuccess>>
  └─ ICommandUsecase<TCommand, TSuccess>

Mediator.IQuery<FinResponse<TSuccess>>
  └─ IQueryRequest<TSuccess>

Mediator.IQueryHandler<TQuery, FinResponse<TSuccess>>
  └─ IQueryUsecase<TQuery, TSuccess>
```

---

## FinResponse\<A\> API

`FinResponse<A>` is a discriminated union representing success (`Succ`) and failure (`Fail`). It supports `Match`, `Map`, `Bind`, and LINQ query expressions.

### Type Definition

```csharp
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
{
    public sealed record Succ(A Value) : FinResponse<A>;
    public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError;
}
```

### Nested Types

| Type | Properties | `IsSucc` | `IsFail` | Description |
|------|---------|----------|----------|------|
| `FinResponse<A>.Succ` | `A Value` | `true` | `false` | Success case |
| `FinResponse<A>.Fail` | `Error Error` | `false` | `true` | Failure case |

### Properties

| Property | Type | Description |
|---------|------|------|
| `IsSucc` | `bool` | Whether in success state |
| `IsFail` | `bool` | Whether in failure state |

### Methods

| Method | Signature | Description |
|--------|---------|------|
| `Match<B>` | `B Match<B>(Func<A, B> Succ, Func<Error, B> Fail)` | Calls function based on state (returns value) |
| `Match` | `void Match(Action<A> Succ, Action<Error> Fail)` | Calls action based on state |
| `Map<B>` | `FinResponse<B> Map<B>(Func<A, B> f)` | Transforms success value |
| `MapFail` | `FinResponse<A> MapFail(Func<Error, Error> f)` | Transforms failure value |
| `BiMap<B>` | `FinResponse<B> BiMap<B>(Func<A, B> Succ, Func<Error, Error> Fail)` | Transforms both success/failure simultaneously |
| `Bind<B>` | `FinResponse<B> Bind<B>(Func<A, FinResponse<B>> f)` | Monadic bind |
| `BiBind<B>` | `FinResponse<B> BiBind<B>(Func<A, FinResponse<B>> Succ, Func<Error, FinResponse<B>> Fail)` | Binds both success/failure simultaneously |
| `BindFail` | `FinResponse<A> BindFail(Func<Error, FinResponse<A>> Fail)` | Binds failure state |
| `IfFail` | `A IfFail(Func<Error, A> Fail)` | Alternative value function on failure |
| `IfFail` | `A IfFail(A alternative)` | Alternative value on failure |
| `IfFail` | `void IfFail(Action<Error> Fail)` | Executes action on failure |
| `IfSucc` | `void IfSucc(Action<A> Succ)` | Executes action on success |
| `ThrowIfFail` | `A ThrowIfFail()` | Throws on failure, returns value on success |
| `Select<B>` | `FinResponse<B> Select<B>(Func<A, B> f)` | LINQ `select` support (same as `Map`) |
| `SelectMany<B,C>` | `FinResponse<C> SelectMany<B, C>(Func<A, FinResponse<B>> bind, Func<A, B, C> project)` | LINQ `from ... from ... select` support |

### Static Factory Methods (`FinResponse` class)

```csharp
public static class FinResponse
{
    public static FinResponse<A> Succ<A>(A value);
    public static FinResponse<A> Succ<A>() where A : new();
    public static FinResponse<A> Fail<A>(Error error);
}
```

| Method | Description |
|--------|------|
| `Succ<A>(A value)` | Creates `FinResponse` from success value |
| `Succ<A>()` | Creates default success `FinResponse` via `new A()` (`A : new()` constraint) |
| `Fail<A>(Error error)` | Creates failure `FinResponse` |

### IFinResponseFactory Static Methods

```csharp
// Static factory implemented on FinResponse<A> (used internally by Pipeline)
public static FinResponse<A> CreateFail(Error error);
```

> Used when Pipeline calls `TResponse.CreateFail(error)`. Since it is a `static abstract` method, it can only be called on concrete types.

### Implicit Conversion Operators

| Operator | Description |
|--------|------|
| `implicit operator FinResponse<A>(A value)` | Value to `Succ` auto-conversion |
| `implicit operator FinResponse<A>(Error error)` | `Error` to `Fail` auto-conversion |
| `operator true` | `true` when `IsSucc` |
| `operator false` | `true` when `IsFail` |
| `operator \|` | Choice operator: returns left if `Succ`, otherwise returns right |

---

## IFinResponse Interface Hierarchy

is an interface hierarchy for handling `FinResponse<A>` in a type-safe manner in Pipeline and Observability.

### Hierarchy

```
IFinResponse                          Non-generic (IsSucc/IsFail access)
  └─ IFinResponse<out A>             Covariant generic

IFinResponseFactory<TSelf>            CRTP-based Fail creation (for Pipeline)

IFinResponseWithError                 Error access (for Logger/Trace Pipeline)

FinResponse<A>
  ├─ implements IFinResponse<A>
  ├─ implements IFinResponseFactory<FinResponse<A>>
  └─ Fail : implements IFinResponseWithError
```

### IFinResponse

```csharp
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}
```

is a non-generic interface for accessing `IsSucc`/`IsFail` properties without generic types in Pipeline.

### IFinResponse\<out A\>

```csharp
public interface IFinResponse<out A> : IFinResponse
{
}
```

Supports covariance (`out`) for read-only use in pipelines.

### IFinResponseFactory\<TSelf\>

```csharp
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
```

Uses CRTP (Curiously Recurring Template Pattern) to support type-safe `Fail` creation. Called as `TResponse.CreateFail(error)` in Pipeline's `UsecaseValidationPipeline` and `UsecaseExceptionPipeline`.

### IFinResponseWithError

```csharp
public interface IFinResponseWithError
{
    Error Error { get; }
}
```

is an interface for accessing `Error` information on failure. Used in Logger Pipeline and Trace Pipeline. Only `FinResponse<A>.Fail` implements this interface.

---

## Fin to FinResponse Conversion

The `FinToFinResponse` extension method class is used for cross-layer conversion from Repository (`Fin<A>`) to Usecase (`FinResponse<A>`).

### Extension Methods

| Method | Signature | Description |
|--------|---------|------|
| `ToFinResponse<A>` | `Fin<A> → FinResponse<A>` | Same type conversion |
| `ToFinResponse<A,B>` (mapper) | `Fin<A> → Func<A, B> → FinResponse<B>` | Success value mapping conversion |
| `ToFinResponse<A,B>` (factory) | `Fin<A> → Func<B> → FinResponse<B>` | Calls factory on success (ignores original value) |
| `ToFinResponse<A,B>` (onSucc/onFail) | `Fin<A> → Func<A, FinResponse<B>> → Func<Error, FinResponse<B>> → FinResponse<B>` | Custom handling for both success/failure |

```csharp
// Basic conversion
FinResponse<Product> response = fin.ToFinResponse();

// Mapping conversion
FinResponse<ProductDto> response = fin.ToFinResponse(product => new ProductDto(product));

// Factory conversion (when original value is not needed, e.g., Delete)
Fin<Unit> result = await repository.DeleteAsync(id);
return result.ToFinResponse(() => new DeleteResponse(id));
```

---

## FinT LINQ Extensions

`FinTLinqExtensions` is a partial class that unifies `Fin<A>`, `IO<A>`, and `Validation<Error, A>` types into the `FinT<M, A>` monad transformer, enabling LINQ query expression support.

### SelectMany Extension Methods

| File | Source Type | Selector Return Type | Result Type | Description |
|------|-----------|-------------------|----------|------|
| `.Fin.cs` | `Fin<A>` | `FinT<M, B>` | `FinT<M, C>` | Lifts Fin to FinT then chains |
| `.Fin.cs` | `FinT<M, A>` | `Fin<B>` | `FinT<M, C>` | Uses Fin in the middle of FinT chain |
| `.IO.cs` | `IO<A>` | `B` (Map) | `FinT<IO, B>` | Simple IO to FinT conversion |
| `.IO.cs` | `IO<A>` | `FinT<IO, B>` | `FinT<IO, C>` | Lifts IO to FinT then chains |
| `.IO.cs` | `FinT<IO, A>` | `IO<B>` | `FinT<IO, C>` | Uses IO in the middle of FinT chain |
| `.Validation.cs` | `Validation<Error, A>` | `FinT<M, B>` | `FinT<M, C>` | Validation to FinT (generic) |
| `.Validation.cs` | `Validation<Error, A>` | `B` (Map) | `FinT<M, B>` | Simple Validation to FinT conversion |
| `.Validation.cs` | `Validation<Error, A>` | `FinT<IO, B>` | `FinT<IO, C>` | Validation to FinT (IO specialized) |
| `.Validation.cs` | `FinT<M, A>` | `Validation<Error, B>` | `FinT<M, C>` | Uses Validation in the middle of FinT chain |
| `.Validation.cs` | `FinT<IO, A>` | `Validation<Error, B>` | `FinT<IO, C>` | Uses Validation in FinT chain (IO) |

### Filter Extension Methods

| File | Target Type | Return Type | Description |
|------|----------|----------|------|
| `.Fin.cs` | `Fin<A>` | `Fin<A>` | Returns `Fail` when condition is not satisfied |
| `.FinT.cs` | `FinT<M, A>` | `FinT<M, A>` | Returns `Fail` when condition is not satisfied |

```csharp
// Fin Filter
Fin<int> result = FinTest(25).Filter(x => x > 20);

// FinT Filter
FinT<IO, int> result = FinT<IO, int>.Succ(42).Filter(x => x > 20);
```

### TraverseSerial Extension Method

```csharp
public static FinT<M, Seq<B>> TraverseSerial<M, A, B>(
    this Seq<A> seq,
    Func<A, FinT<M, B>> f)
    where M : Monad<M>;
```

Sequentially traverses `Seq<A>`, transforming each element into `FinT<M, B>`. Uses `fold` to ensure each operation completes fully before the next one starts.

| Parameter | Type | Description |
|---------|------|------|
| `seq` | `Seq<A>` | Sequence to process |
| `f` | `Func<A, FinT<M, B>>` | Function that transforms each element into FinT |

> **Use case:** Suitable when you need to safely use resources that do not support concurrency, such as DbContext, in sequential order. If each item is independent and there is no resource sharing, use `Traverse` instead.

```csharp
// Usage in LINQ query expressions
FinT<IO, Response> response =
    from infos in GetFtpInfos()
    from results in infos.TraverseSerial(info => Process(info))
    select new Response(results);
```

### Difference in IO and Validation Method Counts

| Direction | IO.cs | Validation.cs |
|------|-------|---------------|
| Source → FinT (Map) | IO only | Generic M |
| Source → FinT\<M, B\> | IO only | Generic M + IO |
| FinT\<M, A\> → Source | IO only | Generic M + IO |

`IO` is itself a specific monad, so a generic `M` version is unnecessary. `Validation` is a data type rather than a monad, so it can be combined with any monad `M`, providing both a generic `M` version and an IO-specialized version.

### File Structure

| File | Content |
|------|------|
| `FinTLinqExtensions.cs` | Class definition and documentation |
| `FinTLinqExtensions.Fin.cs` | `Fin<A>` extensions (SelectMany, Filter) |
| `FinTLinqExtensions.IO.cs` | `IO<A>` extensions (SelectMany) |
| `FinTLinqExtensions.Validation.cs` | `Validation<Error, A>` extensions (SelectMany) |
| `FinTLinqExtensions.FinT.cs` | `FinT<M, A>` extensions (Filter, TraverseSerial) |

---

## ICacheable Caching Contract

Interface for applying caching to Query requests. When a record implementing `IQueryRequest<TSuccess>` also implements `ICacheable`, the Pipeline automatically handles caching.

### Interface Definition

```csharp
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}
```

### Properties

| Property | Type | Description |
|---------|------|------|
| `CacheKey` | `string` | Unique key for the cache entry |
| `Duration` | `TimeSpan?` | Cache validity period (`null` applies default policy) |

```csharp
// Usage example: Applying caching to a Query request
public sealed record GetProductByIdQuery(ProductId Id)
    : IQueryRequest<ProductDto>, ICacheable
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

---

## IUnitOfWork Persistence Contract

Contract for persisting changes in Command Usecases. The `UsecaseTransactionPipeline` automatically calls `SaveChanges` after Handler execution.

### Interface Definition

```csharp
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

### Methods

| Method | Return Type | Description |
|--------|----------|------|
| `SaveChanges` | `FinT<IO, Unit>` | Persists changes |
| `BeginTransactionAsync` | `Task<IUnitOfWorkTransaction>` | Starts an explicit transaction |

> `IUnitOfWork` inherits from `IObservablePort`. `IObservablePort` defines a `string RequestCategory { get; }` property, enabling the observability Pipeline to automatically identify the layer and category.

### IUnitOfWorkTransaction

```csharp
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

| Method | Return Type | Description |
|--------|----------|------|
| `CommitAsync` | `Task` | Commits the transaction |
| `DisposeAsync` | `ValueTask` | Uncommitted transactions are automatically rolled back (IAsyncDisposable) |

> **Explicit transactions** are used when you need to wrap immediately-executed SQL like `ExecuteDeleteAsync`/`ExecuteUpdateAsync` and `SaveChanges` in the same transaction.

```csharp
// Explicit transaction usage example
await using var tx = await unitOfWork.BeginTransactionAsync(ct);
// ... ExecuteDeleteAsync, SaveChanges, etc.
await tx.CommitAsync(ct);
// Auto-rollback on Dispose if not committed
```

---

## CtxIgnoreAttribute

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class CtxIgnoreAttribute : Attribute;
```

Request records, properties, or record constructor parameters with this attribute are excluded from CtxEnricher source generator auto-generation targets.

| Target | Effect |
|------|------|
| `Class` | Excludes the entire record from CtxEnricher generation |
| `Property` | Excludes only this property from CtxEnricher |
| `Parameter` | Excludes only this record constructor parameter from CtxEnricher |

---

## Related Documents

| Document | Description |
|------|------|
| [Use Case and CQRS Guide](../guides/application/11-usecases-and-cqrs) | CQRS pattern design intent and implementation guide |
| [Validation System Specification](./03-validation) | `TypedValidation`, FluentValidation integration |
| [Error System Specification](./04-error-system) | `DomainErrorType`, `ApplicationErrorType`, etc. |
| [Port and Adapter Specification](./06-port-adapter) | `IRepository`, `IQueryPort`, etc. |
| [Pipeline Specification](./07-pipeline) | Pipeline behavior, `UsecaseTransactionPipeline`, etc. |
| [Observability Specification](./08-observability) | Field/Tag specification, Meter definitions |
