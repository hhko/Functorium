---
title: "IFinResponse Non-Generic Marker"
---

## Overview

**Requirement R1** defined in Part 2, Section 4: Pipelines must be able to read success/failure status without reflection. The first interface that solves this requirement is the non-generic marker `IFinResponse`. By defining `IsSucc` and `IsFail` properties, Pipelines can check success/failure without knowing the generic type `T` of the response.

```
IFinResponse          ← Non-generic marker
├── IsSucc: bool
└── IsFail: bool
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain the role of a non-generic marker interface
2. Understand the principle by which a marker interface eliminates reflection
3. Explain the meaning of `where TResponse : IFinResponse` constraints in Pipelines
4. Understand that success/failure reading is the first requirement of the IFinResponse hierarchy

## Key Concepts

### 1. Non-Generic Marker Interface

`IFinResponse` is a **non-generic** interface without generic type parameters. Since it only exposes success/failure status, Pipelines can check the response state without needing to know what `T` is.

```csharp
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}
```

### 2. Read Access Without Reflection

Without a marker interface, **reflection** would be needed to check success/failure in a Pipeline:

```csharp
// Reflection needed (without marker)
var isSuccProp = response.GetType().GetProperty("IsSucc");
var isSucc = (bool)isSuccProp!.GetValue(response)!;
```

The key thing to note is that a single `where TResponse : IFinResponse` constraint eliminates reflection. With a marker interface, **direct access** is possible:

```csharp
// Direct access (with marker)
public static string LogResponse<TResponse>(TResponse response)
    where TResponse : IFinResponse
{
    return response.IsSucc ? "Success" : "Fail";
}
```

### 3. Role in Pipeline Constraints

The `where TResponse : IFinResponse` constraint on a Pipeline guarantees `IsSucc`/`IsFail` access at compile time. This is the first step toward **type-safe Pipelines**.

```csharp
public class LoggingPipeline<TRequest, TResponse>
    where TResponse : IFinResponse    // IsSucc/IsFail access guaranteed
{
    public TResponse Handle(TRequest request, Func<TResponse> next)
    {
        var response = next();
        Console.WriteLine(response.IsSucc ? "Success" : "Fail");
        return response;
    }
}
```

## FAQ

### Q1: Why is the non-generic marker interface needed before the generic interface?
**A**: When checking the success/failure of a response in a Pipeline, **the type `T` of the value does not need to be known**. `IsSucc`/`IsFail` is information independent of type `T`, so a non-generic interface is sufficient. Without a generic parameter, the Pipeline's `where` constraint becomes simpler and applies consistently to all response types.

### Q2: Which Pipelines can be implemented with just the `IFinResponse` marker interface?
**A**: It can be used for Pipelines that only need to check the success/failure status of the response. For example, the Transaction Pipeline uses `response.IsSucc` to decide commit/rollback, and the Metrics Pipeline collects success/failure counts. However, for the Validation Pipeline, which must **create** failure responses, `IFinResponseFactory` is additionally needed.

### Q3: Does adding a `where TResponse : IFinResponse` constraint affect existing response types?
**A**: Existing response types that don't implement `IFinResponse` cannot pass through Pipelines with this constraint. However, this is intentional behavior. Making all response types implement `IFinResponse`, or ultimately using the `FinResponse<A>` Discriminated Union, automatically resolves this.

## Project Structure

```
01-IFinResponse-Marker/
├── FinResponseMarker/
│   ├── FinResponseMarker.csproj
│   ├── IFinResponse.cs
│   ├── SimpleResponse.cs
│   ├── PipelineExample.cs
│   └── Program.cs
├── FinResponseMarker.Tests.Unit/
│   ├── FinResponseMarker.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseMarkerTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project FinResponseMarker

# Run tests
dotnet test --project FinResponseMarker.Tests.Unit
```

---

We can now read success/failure, but there is no type information for the value yet. The next section adds type-safe value access using a covariant interface with the `out A` keyword.

→ [Section 3.2: IFinResponse\<out A\> Covariant Interface](../02-IFinResponse-Covariant/)
