---
title: "FinResponse DU"
---

## Overview

Including the final **requirement R4**, this section integrates all interfaces designed from Sections 1 through 4 into **a single type**. `FinResponse<A>` is a **Discriminated Union** composed of `Succ`/`Fail` sealed records, implementing both `IFinResponse<A>` and `IFinResponseFactory<FinResponse<A>>`. It is a complete response type that includes Match, Map, Bind methods, value extraction (ThrowIfFail, IfFail), error track operations (MapFail, BiMap, BiBind, BindFail), Boolean/Choice operators, implicit conversions, and LINQ support.

```
FinResponse<A>                            Discriminated Union
├── : IFinResponse<A>                     Covariant interface implementation
├── : IFinResponseFactory<FinResponse<A>> CRTP factory implementation
│
├── sealed record Succ(A Value)           Success case
│
└── sealed record Fail(Error Error)       Failure case
    └── : IFinResponseWithError           Error access only in Fail
```

## Learning Objectives

After completing this section, you will be able to:

1. Implement a Discriminated Union with abstract record + sealed record
2. Explain how Match, Map, and Bind methods work
3. Implement Select and SelectMany for LINQ support
4. Understand how to provide concise APIs through implicit conversions
5. Explain the process of unifying all interfaces from Sections 1-4 into a single type
6. Extract values and execute side effects using ThrowIfFail, IfFail, and IfSucc
7. Manipulate the error track using MapFail, BiMap, BiBind, and BindFail
8. Write concise conditional branching using Boolean and Choice operators

## Key Concepts

### 1. Discriminated Union

`FinResponse<A>` is an `abstract record` with only two `sealed record` variants: `Succ` and `Fail`. Since new cases cannot be added, pattern matching is **exhaustive**.

```csharp
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
{
    public sealed record Succ(A Value) : FinResponse<A> { ... }
    public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError { ... }
}
```

### 2. Match Method

`Match` receives a function for each Succ/Fail case and returns a result. Since all cases must be handled, **compile-time safety** is guaranteed.

```csharp
FinResponse<int> response = FinResponse.Succ(42);

var result = response.Match(
    Succ: value => $"Value: {value}",
    Fail: error => $"Error: {error}");
// result = "Value: 42"
```

### 3. Map and Bind

`Map` transforms the Succ value, while `Bind` transforms the value and returns a new FinResponse. Both methods propagate the error when in the Fail state.

```csharp
// Map: A → B (transforms only the value)
FinResponse<string> mapped = response.Map(v => v.ToString());

// Bind: A → FinResponse<B> (chaining)
FinResponse<int> bound = response.Bind(v =>
    v > 0 ? FinResponse.Succ(v * 2) : FinResponse.Fail<int>(Error.New("negative")));
```

### 4. LINQ Support

By implementing `Select` and `SelectMany`, the LINQ `from ... select` syntax is supported.

```csharp
var result = from x in FinResponse.Succ(3)
             from y in FinResponse.Succ(4)
             select x + y;
// result = Succ(7)
```

### 5. Implicit Conversions

Values or errors can be directly assigned to `FinResponse<A>`.

```csharp
FinResponse<string> succ = "Hello";               // Implicit conversion: string → Succ
FinResponse<string> fail = Error.New("error");     // Implicit conversion: Error → Fail
```

### 6. All Interfaces Unified

The following table shows how each requirement from Sections 1-4 is unified into a single `FinResponse<A>`.

`FinResponse<A>` implements all interfaces from Sections 1-4:

| Interface | Role | Implementation |
|-----------|------|------|
| `IFinResponse` | Read success/failure | `IsSucc`, `IsFail` |
| `IFinResponse<out A>` | Covariant access | Inheritance |
| `IFinResponse<TSelf>` | Create failure | `CreateFail` |
| `IFinResponseWithError` | Error access | Implemented only in `Fail` |

The full API of `FinResponse<A>` organized by group:

| Group | Member | Role |
|------|------|------|
| **Pattern Matching** | `Match<B>(Func, Func)` | Value/error → B transformation |
| | `Match(Action, Action)` | Side effect execution |
| **Value Extraction** | `ThrowIfFail()` | Extract success value (throws on failure) |
| | `IfFail(Func<Error, A>)` | Error → fallback value |
| | `IfFail(A)` | Provide default value |
| | `IfFail(Action<Error>)` | Side effect on failure |
| | `IfSucc(Action<A>)` | Side effect on success |
| **Success Track** | `Map<B>(Func<A, B>)` | Value transformation |
| | `Bind<B>(Func<A, FinResponse<B>>)` | Monadic bind |
| **Error Track** | `MapFail(Func<Error, Error>)` | Error transformation |
| | `BindFail(Func<Error, FinResponse<A>>)` | Error recovery |
| **Bidirectional** | `BiMap<B>(Func, Func)` | Simultaneous success/error transformation |
| | `BiBind<B>(Func, Func)` | Simultaneous success/error bind |
| **LINQ** | `Select`, `SelectMany` | `from ... select` syntax |
| **Operators** | `implicit A →`, `implicit Error →` | Implicit conversions |
| | `operator true/false` | `if (response)` pattern |
| | `operator \|` | choice (`fail \| fallback`) |

### 7. Value Extraction Patterns -- ThrowIfFail, IfFail, IfSucc

`Match` always requires handling both branches. What if you just want to extract the success value?

**ThrowIfFail** -- the most commonly used pattern in test code:

```csharp
var value = response.ThrowIfFail();  // Throws ErrorException on failure
```

**IfFail** -- safe fallback:

```csharp
var value = response.IfFail(-1);              // Provide default value
var value = response.IfFail(err => 0);        // Error-based fallback
response.IfFail(err => logger.Error(err));    // Side effect
```

**IfSucc** -- side effect on success:

```csharp
response.IfSucc(value => logger.Info($"Got: {value}"));
```

> **FAQ: Is `ThrowIfFail()` safe for production?**
> Use it only in tests and top-level API boundaries (Controllers, etc.). Within business logic, propagating errors via `Match` or `Bind` is safer.

### 8. Error Track Operations -- MapFail, BiMap, BiBind, BindFail

Used when transformations are needed on the Railway's error track.

**MapFail** -- transform domain errors to application errors:

```csharp
var result = response.MapFail(e => Error.New($"Application error: {e.Message}"));
```

**BindFail** -- error recovery attempt (fallback lookup):

```csharp
var result = response.BindFail(err => TryFallback());
// If TryFallback() returns Succ, recovery succeeds; if Fail, new error propagates
```

**BiMap, BiBind** -- bidirectional transformation:

```csharp
// BiMap: transform both success value and error simultaneously
var result = response.BiMap(
    value => value.ToString(),
    error => Error.New($"Wrapped: {error.Message}"));

// BiBind: return new FinResponse for both success/error
var result = response.BiBind(
    value => FinResponse.Succ(value.ToString()),
    error => FinResponse.Succ("recovered"));
```

> Appendix C "Railway Oriented Programming" covers practical usage patterns for error track operations in more detail.

### 9. Boolean and Choice Operators

`if/else` branching can be expressed more concisely.

**`operator true/false`** -- `if (response)` pattern:

```csharp
if (response)
    Console.WriteLine("Success!");
else
    Console.WriteLine("Failure!");
```

**`operator |`** -- choice operator:

```csharp
// Use alternative on failure
var result = primaryLookup | fallbackLookup;
```

## FAQ

### Q1: Does implementing a Discriminated Union with `abstract record` guarantee exhaustiveness of `switch` pattern matching?
**A**: The C# compiler currently supports exhaustiveness checking for sealed hierarchies at the **warning level**. Since both `Succ` and `Fail` are `sealed record`, no new cases can be added, and the `Match` method **enforces handling of both cases at compile time**.

### Q2: Can't implicit conversions (`implicit operator`) harm code readability?
**A**: When types are clear, implicit conversions **reduce boilerplate** and actually improve readability. Instead of `return new Response(...)`, you can write `return response`. However, in ambiguous type situations, it's better to use explicit factory methods like `FinResponse.Succ(value)`.

### Q3: What is the difference between `Map` and `Bind`?
**A**: `Map` transforms the value where the result is always a success (`A → B`). `Bind` transforms the value while returning a new `FinResponse` (`A → FinResponse<B>`), so failure can occur during the transformation. In Railway-Oriented Programming, `Map` corresponds to a straight path, while `Bind` corresponds to a path with possible branching.

### Q4: Is the LINQ `from ... select` syntax frequently used in practice?
**A**: It's useful when **sequentially composing** multiple `FinResponse` values. LINQ syntax is often more readable than nested `Bind` calls. However, for single transformations, using `Map` or `Bind` directly is more concise.

## Project Structure

```
05-FinResponse-Discriminated-Union/
├── FinResponseDiscriminatedUnion/
│   ├── FinResponseDiscriminatedUnion.csproj
│   ├── IFinResponse.cs
│   ├── FinResponse.cs
│   └── Program.cs
├── FinResponseDiscriminatedUnion.Tests.Unit/
│   ├── FinResponseDiscriminatedUnion.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseDiscriminatedUnionTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project FinResponseDiscriminatedUnion

# Run tests
dotnet test --project FinResponseDiscriminatedUnion.Tests.Unit
```

---

The IFinResponse hierarchy is complete. The next section covers the Create-Only constraint pattern, applying only `IFinResponseFactory<TResponse>` to the Validation and Exception Pipelines.

→ [Section 4.1: Create-Only Constraint](../../Part4-Pipeline-Constraint-Patterns/01-Create-Only-Constraint/)
