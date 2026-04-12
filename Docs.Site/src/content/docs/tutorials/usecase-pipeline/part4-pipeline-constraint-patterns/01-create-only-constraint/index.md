---
title: "Create-Only Constraint"
---

## Overview

Now we apply the IFinResponse hierarchy designed in Part 3 to real Pipelines. The Validation Pipeline and Exception Pipeline only need to **create failure responses** when failures occur during request processing. They don't need to read or inspect existing responses. This section covers the pattern of applying only the minimal constraint `IFinResponseFactory<TResponse>` to these "create-only" Pipelines.

```
Pipeline operation flow:

Validation Pipeline:
  isValid? ──No──→ TResponse.CreateFail(error)  ← Only creation needed
           │
           Yes──→ next() call

Exception Pipeline:
  try { next() } catch (ex) → TResponse.CreateFail(error)  ← Only creation needed
```

## Learning Objectives

After completing this section, you will be able to:

1. Identify Pipelines that use the Create-Only constraint (`IFinResponseFactory<TResponse>`)
2. Understand that `TResponse.CreateFail(error)` is a static abstract call and explain why reflection is unnecessary
3. Explain why the Validation Pipeline and Exception Pipeline don't need response reading (IFinResponse)

## Key Concepts

### 1. What Is the Create-Only Constraint?

This applies when a Pipeline does not read `IsSucc`/`IsFail` on the response object, but **only creates new responses on failure**.

```csharp
// Required capability: CreateFail only
where TResponse : IFinResponseFactory<TResponse>
```

With this single constraint, the following can be called inside the Pipeline:

```csharp
TResponse.CreateFail(Error.New("Validation failed"));
```

### 2. Validation Pipeline

The Validation Pipeline checks the validity of the request, then:
- **Valid**: Forwards the request to the next Pipeline (or Handler).
- **Invalid**: Creates a failure response with `TResponse.CreateFail(error)` and returns immediately.

```csharp
public sealed class SimpleValidationPipeline<TResponse>
    where TResponse : IFinResponseFactory<TResponse>
{
    public TResponse Validate(bool isValid, Func<TResponse> onSuccess)
    {
        if (!isValid)
            return TResponse.CreateFail(Error.New("Validation failed"));

        return onSuccess();
    }
}
```

`TResponse.CreateFail()` is a **static abstract** method of `IFinResponseFactory<TSelf>`. It is resolved at compile time without reflection.

### 3. Exception Pipeline

The Exception Pipeline catches exceptions with try-catch and converts them to failure responses:

```csharp
public sealed class SimpleExceptionPipeline<TResponse>
    where TResponse : IFinResponseFactory<TResponse>
{
    public TResponse Execute(Func<TResponse> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            return TResponse.CreateFail(Error.New(ex));
        }
    }
}
```

### 4. Why Is IFinResponse (Read) Not Needed?

The following summarizes which capabilities each Pipeline actually uses.

| Operation | Required Interface | Validation | Exception |
|------|-------------------|:----------:|:---------:|
| IsSucc/IsFail reading | IFinResponse | - | - |
| CreateFail creation | IFinResponseFactory | O | O |
| Error access | IFinResponseWithError | - | - |

The Validation/Exception Pipelines do **not inspect** existing responses. They determine failure conditions directly (validation failure, exception occurrence) and **only create** new responses on failure.

## FAQ

### Q1: Why doesn't the Validation Pipeline need to read responses?
**A**: The Validation Pipeline checks request validity **before the Handler executes**. Since no response has been created yet, there is nothing to read. If invalid, it directly creates a failure response with `TResponse.CreateFail(error)` and returns; if valid, it delegates to the next step via `next()`.

### Q2: How does `TResponse.CreateFail(error)` differ from a `new TResponse(error)` constructor call?
**A**: Constructor calls (`new TResponse()`) only support the `new()` constraint in generics, which cannot call constructors with parameters. The `static abstract` method `CreateFail` can accept an `Error` parameter and create a failure instance of the exact type.

### Q3: Why does the Exception Pipeline convert exceptions to `Error`?
**A**: By converting exceptions to `Error.New(ex)`, everything outside the Pipeline is handled consistently as `FinResponse.Fail` rather than exceptions. This allows the upper layers to handle all failures **in a uniform manner** using `IsSucc`/`IsFail` without try-catch.

## Project Structure

```
01-Create-Only-Constraint/
├── CreateOnlyConstraint/
│   ├── CreateOnlyConstraint.csproj
│   ├── SimpleValidationPipeline.cs
│   └── Program.cs
├── CreateOnlyConstraint.Tests.Unit/
│   ├── CreateOnlyConstraint.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── CreateOnlyConstraintTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project CreateOnlyConstraint

# Run tests
dotnet test --project CreateOnlyConstraint.Tests.Unit
```

---

The next section applies the Read+Create dual constraint to Logging, Tracing, and Metrics Pipelines, which need to read the response's success/failure status while also creating failure responses.

→ [Section 4.2: Read+Create Constraint](../02-Read-Create-Constraint/)
