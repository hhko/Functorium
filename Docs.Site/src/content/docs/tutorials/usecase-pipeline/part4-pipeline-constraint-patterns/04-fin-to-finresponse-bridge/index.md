---
title: "Fin to FinResponse Bridge"
---

## Overview

How do you connect existing `Fin<T>`-based code with the new `FinResponse<T>`? The Repository layer returns `Fin<T>`, while the Usecase layer returns `FinResponse<T>`. The **bridge** connecting these two layers is the `ToFinResponse()` extension method. This section covers various conversion overloads and usage scenarios.

```
Type flow between layers:

Repository Layer          Usecase Layer
─────────────────        ─────────────────
Fin<Product>       ──→   FinResponse<Product>        Direct conversion
Fin<Product>       ──→   FinResponse<ProductDto>     Mapper conversion
Fin<Unit>          ──→   FinResponse<string>         Factory conversion
Fin<T> (Fail)      ──→   FinResponse<T> (Fail)       Failure propagation
Fin<int>           ──→   FinResponse<string>         Custom conversion
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain why conversion between the Repository (`Fin<T>`) and Usecase (`FinResponse<T>`) layers is needed
2. Select the appropriate `ToFinResponse()` overload for the situation
3. Understand the mechanism by which failure state is automatically propagated during conversion

## Key Concepts

### 1. Why Is a Bridge Needed?

- **`Fin<T>`**: LanguageExt's Result type. Cannot be used as a constraint because it is a sealed struct.
- **`FinResponse<T>`**: A Discriminated Union implementing the IFinResponse interface hierarchy. Can be used in Pipeline constraints.

The `Fin<T>` returned by Repositories must be converted to the Usecase's `FinResponse<T>` to be handled in a type-safe manner within the Pipeline chain.

### 2. Direct Conversion: Fin\<A\> -> FinResponse\<A\>

The simplest conversion. Used when the success value type is identical.

```csharp
Fin<string> fin = Fin<string>.Succ("Hello");
FinResponse<string> response = fin.ToFinResponse();
```

### 3. Mapper Conversion: Fin\<A\> -> FinResponse\<B\>

Used when the success value type needs to be converted. Example: Entity -> DTO

```csharp
Fin<string> fin = Fin<string>.Succ("Hello");
FinResponse<int> response = fin.ToFinResponse(s => s.Length);
```

### 4. Factory Conversion: Fin\<A\> -> FinResponse\<B\>

Used when ignoring the original success value and generating a new one. Example: `Fin<Unit>` -> `FinResponse<string>`

```csharp
Fin<Unit> fin = Fin<Unit>.Succ(Unit.Default);
FinResponse<string> response = fin.ToFinResponse(() => "Deleted successfully");
```

### 5. Failure Propagation

When `Fin` is in a failure state, the Error is propagated directly to `FinResponse`'s Fail regardless of the conversion method.

```csharp
Fin<string> fin = Fin<string>.Fail(Error.New("not found"));
FinResponse<string> response = fin.ToFinResponse();
// response.IsFail == true
```

### 6. Custom Conversion: Fin\<A\> -> FinResponse\<B\> (onSucc/onFail)

Used when custom handling is needed for both success and failure. Example: converting the success value to a different type while also applying separate error handling on failure.

```csharp
Fin<int> fin = Fin.Succ(42);
FinResponse<string> response = fin.ToFinResponse(
    onSucc: value => FinResponse.Succ($"Value is {value}"),
    onFail: error => FinResponse.Fail<string>(error));
```

This overload has the same structure as `Fin`'s `Match` and is the most flexible, but in most cases the direct/mapper/factory conversions are sufficient.

### 7. Conversion Overload Summary

The following summarizes the conversion overloads provided by `ToFinResponse()`.

| Overload | Signature | Use Case |
|----------|---------|------|
| Direct conversion | `Fin<A>.ToFinResponse()` | Same type conversion |
| Mapper conversion | `Fin<A>.ToFinResponse(Func<A, B>)` | Entity -> DTO |
| Factory conversion | `Fin<A>.ToFinResponse(Func<B>)` | Unit -> Response |
| Custom conversion | `Fin<A>.ToFinResponse(Func<A, FinResponse<B>>, Func<Error, FinResponse<B>>)` | Full control |

## FAQ

### Q1: Why does the Repository return `Fin<T>` while the Usecase returns `FinResponse<T>`?
**A**: The Repository uses LanguageExt's pure functional type `Fin<T>` to express success/failure **without external library dependencies**. The Usecase must return `FinResponse<T>` that can be used in Pipeline constraints. `ToFinResponse()` connects these two layers.

### Q2: When should mapper conversion vs factory conversion be used?
**A**: **Mapper conversion** (`Func<A, B>`) is used when converting an Entity to a DTO. Example: `fin.ToFinResponse(product => new ProductDto(product))`. **Factory conversion** (`Func<B>`) is used when ignoring the original value and generating a new one. Example: converting a `Fin<Unit>` return to a "deletion successful" message in `FinResponse<string>`.

### Q3: How does failure auto-propagation work in `ToFinResponse()`?
**A**: `ToFinResponse()` internally calls `Fin<T>`'s `Match`. On Succ, the conversion function is applied; **on Fail, the conversion function is not called** and the `Error` is passed directly to `FinResponse.Fail`. This works identically regardless of which overload is used.

## Project Structure

```
04-Fin-To-FinResponse-Bridge/
├── FinToFinResponseBridge/
│   ├── FinToFinResponseBridge.csproj
│   ├── BridgeExamples.cs
│   └── Program.cs
├── FinToFinResponseBridge.Tests.Unit/
│   ├── FinToFinResponseBridge.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinToFinResponseBridgeTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project FinToFinResponseBridge

# Run tests
dotnet test --project FinToFinResponseBridge.Tests.Unit
```

---

Pipeline constraint patterns are complete. The next section builds a complete Command Usecase implementation example, composing Request/Response/Validator/Handler using the Nested class pattern.

→ [Section 5.1: Command Usecase Complete Example](../../Part5-Practical-Usecase-Examples/01-Command-Usecase-Example/)
