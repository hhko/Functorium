---
title: "Read+Create Constraint"
---

## Overview

In the previous section, we applied the Create-only constraint. This section examines the pattern that also requires the ability to read response status. Logging, Tracing, and Metrics Pipelines must **read** the success/failure status of the response **after** handler execution. Additionally, they may need to **create** failure responses when exceptions occur. This section covers the dual constraint pattern that requires both Read and Create.

```
Pipeline behavior flow:

Logging Pipeline:
  response = next()
  response.IsSucc? ──Yes──→ Log("Success")    ← Read needed (IFinResponse)
                   │
                   No───→ Log("Fail: ...")     ← Error access (IFinResponseWithError)

Tracing Pipeline:
  response = next()
  Tags.Add("status:" + ...)                    ← Read needed (IFinResponse)
  response is IFinResponseWithError?           ← Error access (pattern matching)
```

## Learning Objectives

After completing this section, you will be able to:

1. Identify Pipelines that require the Read+Create dual constraint
2. Explain the different roles of `IFinResponse` (read) and `IFinResponseFactory<TResponse>` (create)
3. Explain why pattern matching (`is IFinResponseWithError`) is used for Error access

## Key Concepts

### 1. Read+Create Dual Constraint

This applies when a Pipeline reads the response's status (Read) and creates failure responses when needed (Create).

```csharp
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

The capabilities granted by the dual constraint are summarized as follows.

| Capability | Interface | Usage |
|------|-----------|------|
| Read | `IFinResponse` | `response.IsSucc`, `response.IsFail` |
| Create | `IFinResponseFactory<TResponse>` | `TResponse.CreateFail(error)` |
| Error access | `IFinResponseWithError` (pattern matching) | `response is IFinResponseWithError fail` |

### 2. Logging Pipeline

The Logging Pipeline records different logs based on response status:

```csharp
public sealed class SimpleLoggingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> Logs { get; } = [];

    public TResponse LogAndReturn(TResponse response)
    {
        if (response.IsSucc)
        {
            Logs.Add("Success");
        }
        else
        {
            if (response is IFinResponseWithError fail)
                Logs.Add($"Fail: {fail.Error}");
            else
                Logs.Add("Fail: unknown");
        }
        return response;
    }
}
```

- `response.IsSucc` / `response.IsFail`: Directly accessible thanks to the `IFinResponse` constraint
- `response is IFinResponseWithError fail`: Error information accessed via pattern matching

### 3. Tracing Pipeline

The Tracing Pipeline records the response status as tags:

```csharp
public sealed class SimpleTracingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> Tags { get; } = [];

    public TResponse TraceAndReturn(TResponse response)
    {
        Tags.Add($"status:{(response.IsSucc ? "ok" : "error")}");

        if (response is IFinResponseWithError fail)
            Tags.Add($"error.message:{fail.Error}");

        return response;
    }
}
```

### 4. Error Access via Pattern Matching

`IFinResponseWithError` is not included in the constraints. Instead, it is checked at runtime via **pattern matching**:

```csharp
// IFinResponseWithError is NOT added to constraints
// because success responses must also pass through this Pipeline

if (response is IFinResponseWithError fail)
{
    // Access Error only in the Fail case
    var error = fail.Error;
}
```

The reason for this approach:
- `IFinResponseWithError` is implemented only in `FinResponse<A>.Fail`
- Success responses (`Succ`) do not implement this interface
- Adding it to constraints would prevent success responses from passing through the Pipeline

## FAQ

### Q1: Why is `IFinResponseWithError` accessed via pattern matching instead of being added to constraints?
**A**: `IFinResponseWithError` is **implemented only in the Fail case**. Adding it to constraints would prevent success responses (`Succ`), which don't implement this interface, from passing through the Pipeline. Pattern matching (`is IFinResponseWithError`) checks at runtime, allowing both success and failure responses to be processed.

### Q2: The Logging and Tracing Pipelines use the same dual constraint -- what's the difference?
**A**: The constraints are identical, but the **purpose** is different. The Logging Pipeline records text logs, while the Tracing Pipeline sets distributed tracing (OpenTelemetry) tags. Having the same constraints means the **required capabilities** from the response are identical, not that the Pipeline behavior is identical.

### Q3: When is the Create capability used in the Read+Create dual constraint?
**A**: It's rare for Logging or Tracing Pipelines themselves to directly call `CreateFail`. However, if an exception occurs during the `next()` call, a failure response must be created with `TResponse.CreateFail(Error.New(ex))` in the catch block. The Create capability is needed for this **exception handling**.

## Project Structure

```
02-Read-Create-Constraint/
├── ReadCreateConstraint/
│   ├── ReadCreateConstraint.csproj
│   ├── SimpleLoggingPipeline.cs
│   └── Program.cs
├── ReadCreateConstraint.Tests.Unit/
│   ├── ReadCreateConstraint.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── ReadCreateConstraintTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project ReadCreateConstraint

# Run tests
dotnet test --project ReadCreateConstraint.Tests.Unit
```

---

The next section covers Transaction and Caching Pipelines that use the same dual constraint while branching between Command/Query at compile time via `where` constraints.

→ [Section 4.3: Transaction/Caching Pipeline](../03-Transaction-Caching-Pipeline/)
