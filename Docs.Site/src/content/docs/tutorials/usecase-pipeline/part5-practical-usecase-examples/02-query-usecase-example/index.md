---
title: "Query Usecase Example"
---

## Overview

In the previous section, we implemented a Command Usecase. This section covers Query Usecases. Using Functorium's `IQueryRequest<TSuccess>` interface, we build a **complete Query Usecase implementation example**. Unlike Commands, Queries **only read data**, so the Transaction Pipeline is not applied, and caching optimization can be applied by implementing `ICacheable`.

```
Query Usecase structure:

GetProductQuery (top-level class)
├── Request   : IQueryRequest<Response>, ICacheable   ← Read-only request + caching
├── Response                                          ← Query result
└── Handler   : IQueryUsecase<Request, Response>      ← Query logic
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain the roles of `IQueryRequest<TSuccess>` and `IQueryUsecase<TQuery, TSuccess>` interfaces and the differences from Command
2. Apply caching optimization to Queries by implementing the `ICacheable` interface
3. Understand the pattern where Query Handlers operate in read-only fashion
4. Explain how Pipelines distinguish Command/Query at the type level

## Key Concepts

### 1. IQueryRequest Interface

`IQueryRequest<TSuccess>` inherits from Mediator's `IQuery<FinResponse<TSuccess>>`. Pipelines recognize the request as a Query through this interface.

```csharp
// Functorium definition
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>> { }
```

The Handler implements `IQueryUsecase<TQuery, TSuccess>`. This interface inherits from `IQueryHandler<TQuery, FinResponse<TSuccess>>`, so when a Handler implements it, Mediator automatically registers it in the Pipeline chain:

```csharp
// Functorium definition
public interface IQueryUsecase<in TQuery, TSuccess>
    : IQueryHandler<TQuery, FinResponse<TSuccess>>
    where TQuery : IQueryRequest<TSuccess> { }
```

Distinguishing Command and Query through interfaces determines the application target **at compile time** via Pipeline `where` constraints:
- `ICommandRequest` → inherits `ICommand<TResponse>` → Transaction Pipeline (`where TRequest : ICommand<TResponse>`) applied
- `IQueryRequest` → inherits `IQuery<TResponse>` → Caching Pipeline (`where TRequest : IQuery<TResponse>`) applicable

### 2. Command vs Query Differences

The key differences between the two patterns are summarized as follows.

| Item | Command | Query |
|------|---------|-------|
| Interface | `ICommandRequest<T>` | `IQueryRequest<T>` |
| Data modification | O (create/update/delete) | X (read only) |
| Transaction | Applied | Not applied |
| Caching | Generally not applied | Applied when `ICacheable` is implemented |
| Return type | `FinResponse<TSuccess>` | `FinResponse<TSuccess>` |

### 3. Caching Optimization via ICacheable

When a Query Request implements `ICacheable`, the Caching Pipeline automatically applies caching.

```csharp
// ICacheable interface (Functorium definition)
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}
```

The `GetProductQuery.Request` in this example actually implements `ICacheable`:

```csharp
public sealed record Request(string ProductId)
    : IQueryRequest<Response>, ICacheable
{
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

The Caching Pipeline performs conditional caching with `request is ICacheable`, so Queries not implementing ICacheable skip caching.

### 4. Read-Only Handler Pattern

Since the Query Handler does not modify state, its dependencies are limited to read-only stores (the Query portion of the Repository).

```csharp
public sealed class Handler : IQueryUsecase<Request, Response>
{
    private readonly Dictionary<string, Response> _products = new() { ... };

    public ValueTask<FinResponse<Response>> Handle(Request query, CancellationToken cancellationToken)
    {
        FinResponse<Response> result = _products.TryGetValue(query.ProductId, out var product)
            ? product
            : Error.New($"Product not found: {query.ProductId}");

        return new ValueTask<FinResponse<Response>>(result);
    }
}
```

## FAQ

### Q1: Why is there no Validator in the Query Usecase?
**A**: In this example, the Validator is omitted for brevity. In real projects, Validators can be added to Queries as well. For example, checking whether `ProductId` is an empty string is a valid validation. Whether to add a Validator is determined by business requirements.

### Q2: What benefit does separating `IQueryRequest` and `ICommandRequest` provide for Pipelines?
**A**: The application target is determined **at compile time** through Pipeline `where` constraints. The Transaction Pipeline is registered only for Commands via the `where TRequest : ICommand<TResponse>` constraint, and the Caching Pipeline is registered only for Queries via the `where TRequest : IQuery<TResponse>` constraint. The Mediator source generator checks these constraints and applies Pipelines only to matching types, so branching is determined **by interface constraints alone** without runtime type checks.

### Q3: What happens if `ICacheable`'s `Duration` is `null`?
**A**: When `Duration` is `null`, the Caching Pipeline applies a **default cache expiration time**. This allows most Queries to use the default while setting custom expiration times for specific Queries.

### Q4: Is the use of `Dictionary` in the Query Handler the same in production?
**A**: No. In the example, `Dictionary` is used as an in-memory store for learning purposes. In production, a Repository interface is injected via DI to query from a database, and the `Fin<T>` returned by the Repository is converted to `FinResponse<T>` using `ToFinResponse()`.

## Project Structure

```
02-Query-Usecase-Example/
├── QueryUsecaseExample/
│   ├── QueryUsecaseExample.csproj
│   ├── GetProductQuery.cs
│   └── Program.cs
├── QueryUsecaseExample.Tests.Unit/
│   ├── QueryUsecaseExample.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── GetProductQueryTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project QueryUsecaseExample

# Run tests
dotnet test --project QueryUsecaseExample.Tests.Unit
```

---

The next section connects all 7 built-in Pipelines and the Custom Pipeline slot (8 total) to simulate the complete request processing flow for Command/Query.

→ [Section 5.3: Full Pipeline Integration](../03-Full-Pipeline-Integration/)
