---
title: "Full Pipeline Integration"
---

## Overview

The ultimate goal of this tutorial was for Pipelines to operate in a type-safe manner without reflection. This section connects **all 7 built-in Pipelines and the Custom Pipeline slot (8 slots total)** to simulate the complete request processing flow. While actual Mediator Pipelines are automatically registered via DI (dependency injection), here we manually invoke them for learning purposes to clearly understand each Pipeline's role and execution order.

```
Pipeline execution order (Command success):

1. Metrics Pipeline        ─ Increment request count
2. Tracing Pipeline        ─ Start Activity
3. Logging Pipeline        ─ Log request
4. Validation Pipeline     ─ Validate input
5. Exception Pipeline      ─ try { ... } catch
6. Transaction Pipeline    ─ BEGIN (Command only)
7. Custom Pipeline         ─ User-defined logic (optional)
8. Handler                 ─ Execute business logic
7. Custom Pipeline         ─ Post-processing (optional)
6. Transaction Pipeline    ─ COMMIT/ROLLBACK
5. Exception Pipeline      ─ Catch on exception
3. Logging Pipeline        ─ Log result
2. Tracing Pipeline        ─ End Activity
1. Metrics Pipeline        ─ Increment response count
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain the execution order and nested structure of the 8 Pipeline slots (7 built-in + Custom)
2. Understand Pipeline branching based on Command vs Query
3. Explain what capabilities each Pipeline's constraint provides
4. Explain how the Pipeline flow short-circuits on failure

## Key Concepts

### 1. Pipeline Execution Order

Mediator Pipelines nest like **Matryoshka (Russian nesting dolls)**. The outermost Pipeline executes first, processes inward, then returns outward for post-processing.

```
Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler
                                                                                 ↓
Metrics ← Tracing ← Logging ← Validation ← Exception ← Transaction ← Custom ← Result
```

### 2. Pipeline Constraint Summary

The following provides an at-a-glance summary of the constraints studied individually in Part 4.

| Pipeline | Constraint | Request Constraint | Required Capability |
|----------|-----------|-----------|-----------|
| Metrics | `IFinResponse, IFinResponseFactory<TResponse>` | `IMessage` | Read + Create |
| Tracing | `IFinResponse, IFinResponseFactory<TResponse>` | `IMessage` | Read + Create |
| Logging | `IFinResponse, IFinResponseFactory<TResponse>` | `IMessage` | Read + Create |
| Validation | `IFinResponseFactory<TResponse>` | `IMessage` | CreateFail |
| Caching | `IFinResponse, IFinResponseFactory<TResponse>` | `IQuery<TResponse>` | Read + Create |
| Exception | `IFinResponseFactory<TResponse>` | `IMessage` | CreateFail |
| Transaction | `IFinResponse, IFinResponseFactory<TResponse>` | `ICommand<TResponse>` | Read + Create |
| Custom | (User-defined) | `IMessage` | Varies |

### 3. Command vs Query Branching

In actual Functorium, `where TRequest : ICommand<TResponse>` / `where TRequest : IQuery<TResponse>` constraints ensure Transaction applies only to Commands and Caching only to Queries **at compile time**. In this educational code, branching is simulated with an `isCommand` boolean for learning purposes:

```csharp
// Educational code: branching simulated with isCommand boolean
// Actual Functorium: compile-time filtering via where TRequest : ICommand<TResponse> constraint
if (isCommand)
    ExecutionLog.Add("Transaction: BEGIN");

// After Handler execution
if (isCommand)
{
    if (response.IsSucc)
        ExecutionLog.Add("Transaction: COMMIT");
    else
        ExecutionLog.Add("Transaction: ROLLBACK");
}
```

### 4. Error Propagation Pattern

Each Pipeline **returns immediately** on failure. If Validation fails, the Handler is never reached.

```
On Validation failure:
  Metrics → Tracing → Logging → Validation → FAIL → immediate return
                                                       ↓
  Metrics ← Tracing ← Logging ← failure response
```

### 5. Flow Tracking via ExecutionLog

The `PipelineOrchestrator` records each step's execution in the `ExecutionLog`. This enables tests to verify execution order and conditional branching.

```csharp
sut.ExecutionLog.ShouldContain("Validation: PASS");
sut.ExecutionLog.ShouldContain("Transaction: COMMIT");
```

## Project Structure

```
03-Full-Pipeline-Integration/
├── FullPipelineIntegration/
│   ├── FullPipelineIntegration.csproj
│   ├── PipelineOrchestrator.cs
│   └── Program.cs
├── FullPipelineIntegration.Tests.Unit/
│   ├── FullPipelineIntegration.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── PipelineOrchestratorTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project FullPipelineIntegration

# Run tests
dotnet test --project FullPipelineIntegration.Tests.Unit
```

## FAQ

### Q1: Does changing the Pipeline execution order affect behavior?
**A**: Yes. Execution order is important. Metrics, Tracing, and Logging Pipelines must be outermost to ensure observability for all requests. Validation must be before Exception/Transaction so that invalid requests don't start unnecessary transactions. The Exception Pipeline wraps Transaction and Handler to capture business logic exceptions, and Transaction is positioned closest to the Handler to minimize the commit/rollback scope.

### Q2: All default Pipelines operate through `FinResponse<T>`, so why use different constraints per Pipeline?
**A**: Following the Interface Segregation Principle (ISP), each Pipeline constrains only the **minimum capabilities it needs**. Validation constrains only `IFinResponseFactory`, while Logging constrains `IFinResponse` + `IFinResponseFactory`. Since `FinResponse<T>` implements all interfaces, it can pass through all Pipelines, but **the code's intent is clearly expressed through the constraints**.

### Q3: What happens to the Transaction Pipeline on Validation failure?
**A**: When the Validation Pipeline fails, it returns a failure response immediately without calling `next()` (**short-circuit**). The subsequent Transaction Pipeline and Handler are not executed, **preventing unnecessary transaction starts**. The response passes through outer Pipelines (Logging, Tracing, etc.) where results are recorded.

### Q4: In production, how are Pipelines registered instead of this manual orchestration?
**A**: **DI (dependency injection) registration** from Mediator frameworks (Mediator, MediatR, etc.) is used. When `services.AddMediator()` is called, Pipelines are connected to the chain in registration order. The manual invocation in this section is for learning purposes to clearly understand each Pipeline's role and execution order.

### Q5: Why is the Custom Pipeline slot positioned right before the Handler?
**A**: Custom Pipelines handle **domain-specific concerns**. After observability (Metrics, Tracing, Logging) and infrastructure concerns (Validation, Exception, Transaction) are processed, performing domain-specific pre/post-processing at the position closest to the business logic is natural. Examples include custom metric collection or tracing attribute additions specific to certain Usecases.

---

From interface hierarchy design to Pipeline constraints, practical Usecase application, and full integration -- the journey of how `FinResponse<T>` enables type-safe Pipelines without reflection is complete.

---

The appendices provide a complete reference of the IFinResponse interface hierarchy, Pipeline constraint matrix, and detailed implementations of each interface at a glance.

→ [Appendix A: IFinResponse Interface Hierarchy Complete Reference](../../Appendix/A-interface-hierarchy-reference.md)
