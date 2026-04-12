---
title: "Why Type-Safe Pipelines"
---

## The Mediator Pipeline Dilemma

In the Mediator pattern, a Pipeline is a **cross-cutting concern** handler that intercepts all requests/responses. It enables consistently applying Logging, Validation, Exception Handling, Transaction management, etc., separately from Usecase code.

```csharp
// Pipeline intercepts all requests/responses
Request → [Validation] → [Logging] → [Transaction] → Handler → [Logging] → Response
```

However, a Pipeline must handle **all types of responses**. The Logging Pipeline needs to know whether a response is success or failure, and the Validation Pipeline must **directly create** a response object when validation fails.

## Core Problem: sealed struct Cannot Be Used as a Constraint

LanguageExt's `Fin<T>` is a monad that represents success/failure. However, because `Fin<T>` is a **sealed struct**:

```csharp
// This is not possible
where TResponse : Fin<T>  // Compile error! sealed struct cannot be a constraint
```

Due to this single constraint limitation, `Fin<T>` cannot be directly handled in Pipelines. This ultimately leads to **reliance on reflection**.

## The 3 Costs of Reflection

When handling `Fin<T>` through reflection:

1. **Runtime performance degradation**: Dynamically inspecting type information on every request
2. **Loss of compile-time safety**: Typos and type mismatches are only discovered at runtime
3. **Maintenance complexity**: Reflection code must be manually synchronized when types change

## The Solution This Tutorial Presents

This tutorial covers the process of completely eliminating reflection through **interface hierarchy design** and **C# 11 static abstract members**.

```
3 reflection sites → Interface hierarchy → 0 reflection sites
```

Specifically:

| Problem | Solution |
|------|------|
| Reading success/failure | `IFinResponse` non-generic marker interface |
| Covariant access | `IFinResponse<out A>` covariant interface |
| Creating failure responses | `IFinResponseFactory<TSelf>` CRTP factory |
| Accessing error information | `IFinResponseWithError` error access interface |
| Complete integration | `FinResponse<A>` Discriminated Union record |

## The Journey of This Tutorial

1. **Part 1**: Establishes the foundations of C# generic variance
2. **Part 2**: Precisely defines the conflict between `Fin<T>` and Mediator Pipelines
3. **Part 3**: Designs the IFinResponse interface hierarchy step by step
4. **Part 4**: Applies minimal constraints to each Pipeline
5. **Part 5**: Integrates the complete flow in practical Command/Query Usecases

## FAQ

### Q1: Why is it a problem that `Fin<T>` is a sealed struct and cannot be used as a constraint?
**A**: Pipelines access members of the response type through generic constraints in the form `where TResponse : SomeType`. If `Fin<T>` is a sealed struct, it cannot be used as this constraint, which means there is no way to access members like `IsSucc` or `Error` at compile time. This ultimately leads to reliance on reflection, degrading performance, safety, and maintainability.

### Q2: Can't caching reflection solve the performance problem?
**A**: Reflection caching can improve performance, but it does not solve the **loss of compile-time safety** or **maintenance complexity**. Property name typos or LanguageExt version changes will only be caught as errors at runtime. The interface hierarchy design presented in this tutorial solves all three problems.

### Q3: Is the solution in this tutorial only applicable to LanguageExt?
**A**: No. The pattern of wrapping a sealed struct with an interface hierarchy is applicable to **all sealed types**. For example, types from other libraries like `Result<T>` or `Option<T>` can also be leveraged for Pipeline constraints using the same strategy.

### Q4: Do I need to study Parts 1-5 in order?
**A**: It is recommended to study Parts 1-3 in order. Generic variance fundamentals (Part 1) build toward the problem definition (Part 2) and interface design (Part 3). Parts 4-5 can be studied selectively after completing Part 3.

---

Before starting the tutorial, verify the required tools, prerequisite knowledge, and project build methods.

→ [Section 0.2: Prerequisites and Setup](02-prerequisites-and-setup.md)
