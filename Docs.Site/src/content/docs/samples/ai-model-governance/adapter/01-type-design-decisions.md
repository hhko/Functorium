---
title: "Adapter Type Design Decisions"
description: "Rationale for selecting LanguageExt IO advanced features (Retry, Timeout, Fork, Bracket)"
---

## Overview

This document organizes the rationale for which LanguageExt IO advanced features to apply for the 4 external service scenarios defined in the [technical requirements](../00-business-requirements/).

## External Service Requirements -> IO Pattern Mapping

| External Service | Problem Scenario | Required Guarantee | Selected IO Pattern |
|-----------|----------|-----------|--------------|
| Model health check | Intermittent slow responses (>10s) | Limit maximum wait time, fallback on timeout | **Timeout + Catch** |
| Model monitoring | Intermittent 503 errors | Automatic recovery from transient failures, retry interval control | **Retry + Schedule** |
| Parallel compliance | 5 independent checks, slow when sequential | Parallel execution, collect all results | **Fork + awaitAll** |
| Model registry | Session-based resource management | Guarantee session release even on exceptions | **Bracket** |

## Per-Pattern Design Decisions

### 1. Timeout + Catch -- Model Health Check

**Problem:** The health check service intermittently delays responses by 12 seconds or more. Waiting indefinitely slows down the entire system.

**Why Timeout?** When you cannot control the response time of an external service, you declaratively set the maximum wait time the system allows. LanguageExt's `Timeout` imposes a time limit on IO operations, raising `Errors.TimedOut`.

**Why Catch chaining?** The timeout must be converted from an "error" to a "fallback result." A health check timeout means the model is "not healthy," not that there is a system error.

| Catch Order | Condition | Result |
|-----------|------|------|
| 1st | `e.Is(Errors.TimedOut)` | TimedOut fallback result (not an error) |
| 2nd | `e.IsExceptional` | Convert to AdapterError |

### 2. Retry + Schedule -- Model Monitoring

**Problem:** The monitoring service temporarily returns 503. The first attempt fails with 60% probability, but retrying usually succeeds.

**Why Retry?** Transient network errors (503, timeout) are often resolved by retrying. LanguageExt's `Retry` automatically retries IO operations according to a Schedule.

**Schedule design:**

```
exponential(100ms) | jitter(0.3) | recurs(3) | maxDelay(5s)
```

| Component | Role | Value |
|----------|------|-----|
| `exponential` | Base delay: 100ms -> 200ms -> 400ms | Based on 100ms |
| `jitter` | Distribute concurrent retries (prevent thundering herd) | 30% variation |
| `recurs` | Maximum retry count | 3 times |
| `maxDelay` | Delay upper bound | 5 seconds |

**Why this Schedule?**
- `exponential`: Gradually reduces server load
- `jitter`: Prevents the thundering herd problem where multiple clients retry simultaneously
- `recurs(3)`: 3 retries recover most transient errors; beyond that, it is a permanent error
- `maxDelay(5s)`: Limits user wait time upper bound

### 3. Fork + awaitAll -- Parallel Compliance Check

**Problem:** Running 5 compliance criteria sequentially takes 100~500ms x 5 = up to 2.5 seconds. Each check is independent, so parallel execution is possible.

**Why Fork?** LanguageExt's `Fork` runs IO operations in separate fibers (lightweight threads) to achieve parallelism. Since each check is independent, there are no result dependencies, making it safe to Fork.

**Why awaitAll?** `awaitAll` collects results from all Forks. Even if one check is slow, the rest are already completed, so the total elapsed time converges to the slowest check's time.

**Performance comparison:**

| Execution Mode | Worst-case Time | Expected Time |
|----------|-------------|-------------|
| Sequential | 500ms x 5 = 2,500ms | ~1,500ms |
| Parallel (Fork) | max(500ms) = 500ms | ~350ms |

### 4. Bracket -- Model Registry

**Problem:** Registry lookup must acquire a session, use it, and then release it. Even if an exception occurs during lookup, the session must not leak.

**Why Bracket?** The Bracket pattern guarantees the resource lifecycle in three stages: Acquire -> Use -> Release. Release (the Fin parameter) always executes regardless of whether the Use stage succeeds or fails. It is similar to C#'s `try-finally`, but can be composed within an IO context.

```
Acquire: Session acquisition (50~150ms delay, 5% failure)
    |
    v
Use: Registry lookup (100~400ms delay, 5% failure)
    |
    v
Fin(Release): Session release (guaranteed regardless of success/failure)
```

**Why Bracket instead of try-finally?**
- Can be used naturally within IO composition chains
- Release can have IO effects (async release)
- Transparently composable in FinT LINQ chains

## Naming Conventions: `{Subject}{Role}{Variant}`

Adapter layer filenames follow a 3-dimensional naming convention:

| Dimension | Expressed By | Example |
|------|-----------|------|
| Subject (what) | Aggregate name | `AIModel`, `Deployment`, `Assessment`, `Incident` |
| Role (role) | CQRS role | `Repository`, `Query`, `DetailQuery` |
| Variant (how) | Technology suffix | `InMemory`, `EfCore`, `Dapper` |

Applied examples:

| Filename | Subject | Role | Variant |
|--------|---------|------|---------|
| `AIModelRepositoryInMemory.cs` | AIModel | Repository | InMemory |
| `AIModelRepositoryEfCore.cs` | AIModel | Repository | EfCore |
| `AIModelQueryInMemory.cs` | AIModel | Query | InMemory |
| `DeploymentDetailQueryInMemory.cs` | Deployment | DetailQuery | InMemory |
| `UnitOfWorkInMemory.cs` | (common) | UnitOfWork | InMemory |

This convention also applies to Observable wrappers: `{Subject}{Role}{Variant}Observable` (e.g., `AIModelRepositoryInMemoryObservable`).

## Observability Design

### GenerateObservablePort

All external services and Repositories apply the `[GenerateObservablePort]` Source Generator. This attribute auto-generates an Observable class that wraps the original class, adding logging, metrics, and tracing to each method call.

```
IModelHealthCheckService
    |
    [GenerateObservablePort]
    |
    v
ModelHealthCheckServiceObservable  (auto-generated by Source Generator)
    |-- Method entry/exit logging
    |-- Execution time metrics
    |-- Distributed tracing spans
    |
    v
ModelHealthCheckService  (actual implementation)
```

### DI Registration Pattern

```csharp
// Register Observable wrapper to interface
services.AddScoped<IModelHealthCheckService, ModelHealthCheckService>();
services.RegisterScopedObservablePort<IAIModelRepository, InMemoryAIModelRepositoryObservable>();
```

External services are registered directly; Repositories are registered through Observable wrappers.

In the next step, we implement this design in C# code in [Code Design](./02-code-design/).
