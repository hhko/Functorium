---
title: "IFinResponseWithError"
---

## Overview

**Requirement R3**: Pipelines must be able to access error information on failure. So far, `IFinResponse` enables checking success/failure and `IFinResponseFactory` enables creating failure responses, but how do Pipelines **access error information**? This section introduces the `IFinResponseWithError` interface to design a type-safe pattern where errors are accessible **only from the Fail case**.

```
IFinResponseWithError           ← Error access interface (this section)
├── Error: Error                  Implemented only in Fail
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain why `IFinResponseWithError` is implemented only in Fail
2. Write code that safely accesses errors through pattern matching
3. Understand the principle by which error access from Succ is prevented by the type system
4. Explain how interface segregation strengthens type safety

## Key Concepts

### 1. IFinResponseWithError Interface

`IFinResponseWithError` is a separate interface providing the `Error` property. By implementing this interface **only in the Fail case**, access to errors from the Succ case is prevented at the source.

```csharp
public interface IFinResponseWithError
{
    Error Error { get; }
}
```

### 2. Implemented Only in Fail

Only the `Fail` record of the Discriminated Union implements `IFinResponseWithError`. `Succ` does not implement this interface, so error access is blocked by the type system.

```csharp
public abstract record ErrorAccessResponse<A> : IFinResponse
{
    public sealed record Succ(A Value) : ErrorAccessResponse<A>
    {
        // Does NOT implement IFinResponseWithError!
    }

    public sealed record Fail(Error Error) : ErrorAccessResponse<A>, IFinResponseWithError
    {
        // Only Fail implements IFinResponseWithError
    }
}
```

### 3. Error Access via Pattern Matching

In Pipelines, `is IFinResponseWithError` pattern matching is used to safely access errors. When the response is Succ, the pattern match fails, naturally preventing error access.

```csharp
public static string LogResponse<TResponse>(TResponse response)
    where TResponse : IFinResponse
{
    if (response.IsSucc)
        return "Success";

    // Pattern matching for error access - only Fail implements IFinResponseWithError
    if (response is IFinResponseWithError failResponse)
        return $"Fail: {failResponse.Error}";

    return "Fail: unknown error";
}
```

### 4. Why a Separate Interface?

If the `Error` property were added directly to `IFinResponse`, it would be accessible even from the Succ case, creating **runtime exception** risks. Separating into a distinct interface ensures **compile-time safety**.

## FAQ

### Q1: Why can't `Error` be added directly to `IFinResponse`?
**A**: Adding `Error` to `IFinResponse` would make it accessible **from the Success (Succ) case** as well. Since Succ has no error, this creates runtime risks like `null` returns or exceptions. Separating into a distinct interface and implementing only in Fail ensures **the type system guarantees safety**.

### Q2: Is `is IFinResponseWithError` pattern matching different from reflection?
**A**: Completely different. `is` pattern matching is a **native type check** performed by the CLR's type system, and is tens of times faster than reflection (`GetType().GetProperty()`). Additionally, `fail.Error` access is verified at compile time, eliminating the risk of property name typos.

### Q3: Is the pattern of implementing `IFinResponseWithError` only in Fail used elsewhere?
**A**: Yes. This is a general design technique called **per-case interface implementation**. For example, in HTTP responses, error bodies only exist in 4xx/5xx responses, so an error body interface would be implemented only in the error response case -- the same principle.

## Project Structure

```
04-IFinResponseWithError/
├── FinResponseWithError/
│   ├── FinResponseWithError.csproj
│   ├── Interfaces.cs
│   ├── ErrorAccessResponse.cs
│   ├── LoggingPipelineExample.cs
│   └── Program.cs
├── FinResponseWithError.Tests.Unit/
│   ├── FinResponseWithError.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseWithErrorTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project FinResponseWithError

# Run tests
dotnet test --project FinResponseWithError.Tests.Unit
```

---

With error access solved, it's time to unify all requirements R1-R4 into a single type. The `FinResponse<A>` Discriminated Union implements Match, Map, Bind, implicit conversions, and more.

→ [Section 3.5: FinResponse\<A\> Discriminated Union](../05-FinResponse-Discriminated-Union/)
