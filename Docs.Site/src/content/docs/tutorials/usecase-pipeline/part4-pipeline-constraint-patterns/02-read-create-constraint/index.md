---
title: "Read+Create Constraint"
---

## Overview

The previous section applied the Create-only constraint. Now we look at the pattern that also requires the ability to read response status. Logging, Tracing, and Metrics Pipelines need to **read** the success/failure status of responses **after** the handler executes. They must also be able to **create** failure responses when exceptions occur. This section covers the dual constraint pattern that requires both Read and Create.

```
Pipeline operation flow:

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
2. Explain the role difference between `IFinResponse` (Read) and `IFinResponseFactory<TResponse>` (Create)
3. Explain why pattern matching (`is IFinResponseWithError`) is used for Error access

## Key Concepts

### 1. Read+Create Dual Constraint

This applies when a Pipeline reads the response status (Read) and, when necessary, creates failure responses (Create).

```csharp
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

The capabilities provided by the dual constraint:

| Capability | Interface | Usage |
|------|-----------|------|
| Read | `IFinResponse` | `response.IsSucc`, `response.IsFail` |
| Create | `IFinResponseFactory<TResponse>` | `TResponse.CreateFail(error)` |
| Error access | `IFinResponseWithError` (pattern matching) | `response is IFinResponseWithError fail` |

### 2. Logging Pipeline

The Logging Pipeline records different logs based on the response status:

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

- `response.IsSucc` / `response.IsFail`: Direct access thanks to the `IFinResponse` constraint
- `response is IFinResponseWithError fail`: Error information access via pattern matching

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

`IFinResponseWithError` is not included in the constraints. Instead, it is checked at **runtime via pattern matching**:

```csharp
// IFinResponseWithError is NOT added to constraints
// Because success responses must also pass through this Pipeline

if (response is IFinResponseWithError fail)
{
    // Error access only in Fail case
    var error = fail.Error;
}
```

The reason:
- `IFinResponseWithError` is only implemented in `FinResponse<A>.Fail`
- Success responses (`Succ`) do not implement this interface
- Adding it to constraints would prevent success responses from passing through the Pipeline

## FAQ

### Q1: Why is `IFinResponseWithError` accessed via pattern matching rather than added to constraints?
**A**: `IFinResponseWithError` is implemented **only in the Fail case**. Adding it to constraints would prevent success responses (`Succ`) from passing through the Pipeline since they don't implement this interface. Using pattern matching (`is IFinResponseWithError`) for runtime checking allows both success and failure responses to be processed.

### Q2: The Logging Pipeline and Tracing Pipeline use the same dual constraint -- what's the difference?
**A**: The constraints are identical, but the **purpose** differs. The Logging Pipeline records text logs, while the Tracing Pipeline sets distributed tracing (OpenTelemetry) tags. Having the same constraints means the **required capabilities** from the response are identical, not that the Pipeline behavior is the same.

### Q3: When is the Create capability used in the Read+Create dual constraint?
**A**: It's rare for Logging or Tracing Pipelines to directly call `CreateFail` themselves. However, when an exception occurs during `next()` invocation, a failure response must be created in the catch block with `TResponse.CreateFail(Error.New(ex))`. The Create capability is needed for this **exception handling**.

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

The next section examines the Transaction and Caching Pipelines, which use the same dual constraint while filtering Command/Query at compile time via `where` constraints.

→ [Section 4.3: Transaction/Caching Pipeline](../03-Transaction-Caching-Pipeline/)
