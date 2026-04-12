---
title: "Constraints vs Alternatives"
---

## Overview

Are there alternatives beyond generic constraints? This appendix compares the **interface constraint approach** chosen by Functorium with other alternatives. By analyzing the pros and cons of each approach, we understand why the interface hierarchy + generic constraints is the best choice.

---

## Approach Comparison

### 1. Interface Constraints (Functorium Approach)

```csharp
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| Item | Evaluation |
|------|------|
| **Type safety** | Guaranteed at compile time |
| **Reflection** | Not required (0 sites) |
| **Performance** | Optimal (static dispatch) |
| **Code complexity** | Requires interface hierarchy design |
| **Extensibility** | Extensible by adding new interfaces |
| **IDE support** | Full auto-completion and refactoring support |

### 2. Reflection-Based

```csharp
// Runtime type inspection inside Pipeline
var isSuccProp = typeof(TResponse).GetProperty("IsSucc");
var isSucc = (bool)isSuccProp!.GetValue(response)!;

// CreateFail also requires reflection
var createFail = typeof(TResponse).GetMethod("CreateFail", BindingFlags.Static | BindingFlags.Public);
var failResponse = (TResponse)createFail!.Invoke(null, [error])!;
```

| Item | Evaluation |
|------|------|
| **Type safety** | Validated only at runtime (no compile-time guarantee) |
| **Reflection** | Required in multiple places (3+ sites) |
| **Performance** | Reflection overhead (on every request) |
| **Code complexity** | Pipeline internals become complex |
| **Extensibility** | Reflection code must change when new properties/methods are added |
| **IDE support** | String-based, risk of missing during refactoring |

### 3. Using dynamic

```csharp
public TResponse Handle(dynamic request, Func<TResponse> next)
{
    dynamic response = next();
    if (response.IsSucc) { ... }
    return response;
}
```

| Item | Evaluation |
|------|------|
| **Type safety** | None (all checks at runtime) |
| **Reflection** | Uses reflection internally |
| **Performance** | Reflection + DLR overhead |
| **Code complexity** | Simple but unsafe |
| **Extensibility** | Cannot detect typos, runtime errors |
| **IDE support** | No auto-completion |

### 4. Source Generator-Based

```csharp
// Source Generator auto-generates Pipeline code
[GeneratePipeline]
public partial class ValidationPipeline<TResponse> { }
```

| Item | Evaluation |
|------|------|
| **Type safety** | Generated code is type-safe |
| **Reflection** | Not required |
| **Performance** | Optimal (generated at compile time) |
| **Code complexity** | Generator itself is complex |
| **Extensibility** | Requires modifying the generator (steep learning curve) |
| **IDE support** | Varies by generator |

### 5. object + Casting

```csharp
public object Handle(object request, Func<object> next)
{
    var response = next();
    if (response is IFinResponse fin && fin.IsSucc) { ... }
    return response;
}
```

| Item | Evaluation |
|------|------|
| **Type safety** | Partial (casting can fail) |
| **Reflection** | Not required but boxing occurs |
| **Performance** | Boxing/unboxing overhead |
| **Code complexity** | Casting code scattered throughout |
| **Extensibility** | Casting code must change when new types are added |
| **IDE support** | Limited |

---

## Summary Comparison Table

A comparison of all five approaches by key criteria:

| Criteria | Interface Constraints | Reflection | dynamic | Source Gen | object Casting |
|------|:--------------:|:--------:|:-------:|:----------:|:------------:|
| Compile-time safety | O | X | X | O | Partial |
| No reflection | O | X | X | O | O |
| Optimal performance | O | X | X | O | Partial |
| Design cost | Medium | Low | Low | High | Low |
| Maintainability | O | X | X | Medium | X |
| IDE support | O | X | X | Medium | Partial |

---

## Why Were Interface Constraints Chosen?

### 1. Pipelines Are on the Hot Path

Every request passes through Pipelines, so the performance overhead of reflection or dynamic accumulates.

### 2. The Compiler Should Catch Mistakes

If Pipeline constraints are wrong, runtime exceptions occur. Interface constraints prevent this at compile time.

### 3. CRTP Enables static abstract Calls

Combining C# 11's `static abstract` members with the CRTP pattern allows calling static factory methods through interfaces. This is the key to reflection-free `CreateFail` calls.

### 4. Principle of Minimal Constraints

Each Pipeline requires only the capabilities it needs as constraints, so there are no unnecessary dependencies. The Validation Pipeline only needs `CreateFail`, so it constrains only `IFinResponseFactory<TResponse>`.

---

The following appendix examines the Railway Oriented Programming pattern implemented by `FinResponse<A>`'s Map and Bind chains, and the relationship between Pipelines and ROP.

→ [Appendix C: Railway Oriented Programming Reference](C-railway-oriented-programming.md)

