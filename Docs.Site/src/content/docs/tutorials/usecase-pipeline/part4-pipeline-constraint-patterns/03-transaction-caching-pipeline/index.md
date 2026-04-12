---
title: "Transaction/Caching"
---

## Overview

In the previous section, we applied the Read+Create dual constraint. This section covers Pipelines that use the same dual constraint while filtering their application targets **at compile time** via `where` constraints. The Transaction Pipeline uses the `where TRequest : ICommand<TResponse>` constraint to apply **only to Commands**, while the Caching Pipeline uses the `where TRequest : IQuery<TResponse>` constraint to apply **only to Queries**. The Mediator source generator checks the `where` constraints and registers Pipelines only for matching request types, so Command/Query separation happens without runtime type checks.

```
Transaction Pipeline:
  isCommand? ──No──→ Skip (Query doesn't need transaction)
              │
              Yes──→ Begin → handler() → IsSucc? → Commit / Rollback

Caching Pipeline:
  isCacheable? ──No──→ handler() executed directly
                 │
                 Yes──→ cache hit? → return cached
                                 │
                                 No → handler() → IsSucc? → save to cache
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain why the Transaction Pipeline applies only to Commands
2. Explain why the Caching Pipeline caches only successful responses
3. Understand why both Pipelines need the Read+Create constraint
4. Understand how Command/Query branching works through `where` constraints

## Key Concepts

### 1. Transaction Pipeline

The Transaction Pipeline applies transactions only to Command requests:

```csharp
public sealed class SimpleTransactionPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public TResponse Execute(bool isCommand, Func<TResponse> handler)
    {
        if (!isCommand)
        {
            // Query doesn't need a transaction
            return handler();
        }

        // Command: Begin → Execute → Commit/Rollback
        var response = handler();

        if (response.IsSucc)    // Read: IFinResponse
            Commit();
        else
            Rollback();

        return response;
    }
}
```

In the actual Functorium `UsecaseTransactionPipeline`, the `where TRequest : ICommand<TResponse>` constraint is used. The Mediator source generator checks this constraint and applies the Pipeline only to Command requests, filtering at compile time without runtime branching.

### 2. Caching Pipeline

The Caching Pipeline applies caching only to Query requests that implement `ICacheable`:

```csharp
public sealed class SimpleCachingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public TResponse GetOrExecute(string cacheKey, bool isCacheable, Func<TResponse> handler)
    {
        if (!isCacheable)
            return handler();

        if (TryGetFromCache(cacheKey, out var cached))
            return cached;

        var response = handler();

        if (response.IsSucc)    // Read: cache only successful responses
            SetCache(cacheKey, response);

        return response;
    }
}
```

### 3. Why Read+Create Constraint?

The following summarizes how the two Pipelines use the Read and Create capabilities respectively.

| Pipeline | Read (IsSucc/IsFail) | Create (CreateFail) |
|----------|:--------------------:|:-------------------:|
| Transaction | Determines Commit/Rollback | Creates failure response on exception |
| Caching | Caches only successful responses | Creates failure response on exception |

Both Pipelines need `IFinResponse` because they must **read** the response status, and `IFinResponseFactory<TResponse>` for exception handling.

### 4. Command/Query Branching

In actual Functorium Pipelines, `where` constraints determine the application target at compile time:

```csharp
// Transaction Pipeline: applies only to Commands via where constraint
internal sealed class UsecaseTransactionPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>          // ← Command only
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{ ... }

// Caching Pipeline: applies only to Queries via where constraint
internal sealed class UsecaseCachingPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>            // ← Query only
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{ ... }
```

The Mediator source generator checks the `where` constraints and does not register `UsecaseTransactionPipeline` for requests that don't implement `ICommand<TResponse>`, and does not register `UsecaseCachingPipeline` for requests that don't implement `IQuery<TResponse>`. No runtime type checks like `request is ICommandRequest` are needed.

## FAQ

### Q1: How is the Transaction Pipeline's skipping of Queries implemented?
**A**: `UsecaseTransactionPipeline` uses the `where TRequest : ICommand<TResponse>` constraint. The Mediator source generator checks this constraint and registers the Pipeline **only for Command requests**, so the Transaction Pipeline itself is never executed for Query requests. Filtering happens **at compile time** without runtime type checks.

### Q2: Why doesn't the Caching Pipeline cache failure responses?
**A**: Failure responses are often transient errors (network timeouts, temporary DB failures, etc.). Caching failures would return the cached failure on retry, creating an **unrecoverable state**. Therefore, only successful responses are cached using `response.IsSucc`.

### Q3: Why do Transaction and Caching use the same dual constraint but apply to different targets?
**A**: Both Pipelines need the ability to read the response's success/failure (Read) and create failure responses on exception (Create), so **the constraints are identical**. However, Transaction applying only to data-changing **Commands** and Caching applying only to read-only **Queries** is a business requirement.

### Q4: What happens to a Query that doesn't implement `ICacheable`?
**A**: The Caching Pipeline checks `request is ICacheable` for cacheability. Queries not implementing `ICacheable` skip caching and execute the Handler every time. This enables **selective optimization** without forcing caching on all Queries.

## Project Structure

```
03-Transaction-Caching-Pipeline/
├── TransactionCachingPipeline/
│   ├── TransactionCachingPipeline.csproj
│   ├── SimpleTransactionPipeline.cs
│   └── Program.cs
├── TransactionCachingPipeline.Tests.Unit/
│   ├── TransactionCachingPipeline.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── TransactionCachingPipelineTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project TransactionCachingPipeline

# Run tests
dotnet test --project TransactionCachingPipeline.Tests.Unit
```

---

The next section covers the bridge pattern connecting the Repository layer's `Fin<T>` and the Usecase layer's `FinResponse<T>` via the `ToFinResponse()` extension method.

→ [Section 4.4: Fin → FinResponse Bridge](../04-Fin-To-FinResponse-Bridge/)
