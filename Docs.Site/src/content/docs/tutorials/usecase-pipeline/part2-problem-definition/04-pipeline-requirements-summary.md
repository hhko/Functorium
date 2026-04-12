---
title: "Pipeline Requirements Summary"
---

## Overview

After three approaches, the limitations of each have been revealed. We analyzed the Pipeline structure and confirmed the role of `where` constraints (Section 1), discovered that using `Fin<T>` directly requires reflection in 3 places (Section 2), and reduced it to 1 place with a wrapper interface but still could not solve `CreateFail` (Section 3). This section organizes the **4 requirements for the response type system** based on these findings and compares how well each approach satisfies them.

---

## 1. Four Requirements for the Response Type System

The following 4 requirements must be met to safely handle the response type in Pipelines.

### R1: Pipelines Must Be Able to Read Success/Failure Status Directly (Without Reflection)

Logging, Tracing, and Metrics Pipelines need to check the success/failure status of responses. This information must be accessible through **compile-time guaranteed interface members**.

```csharp
// Requirement: Direct access without reflection
if (response.IsSucc)
    LogSuccess();
else
    LogFailure();
```

### R2: Pipelines Must Be Able to Create Failure Responses Directly (static abstract)

Validation and Exception Pipelines must **directly create** failure responses. This requires `static abstract` factory methods.

```csharp
// Requirement: Type-safe failure response creation
return TResponse.CreateFail(Error.New("Validation failed"));
```

### R3: Pipelines Must Be Able to Access Error Information

Logging and Tracing Pipelines need to access **error information** (error messages, error codes, etc.) on failure.

```csharp
// Requirement: Direct error information access
if (response is IFinResponseWithError fail)
    RecordError(fail.Error);
```

### R4: The sealed struct (Fin<T>) Success/Failure Must Be Directly Expressible Without Wrapping

Wrapping `Fin<T>` in a separate wrapper causes the dual interface problem. The response type itself must be a Discriminated Union that **directly expresses** success/failure.

```csharp
// Requirement: Direct expression without wrapper
FinResponse<string> success = FinResponse.Succ("OK");
FinResponse<string> fail = FinResponse.Fail<string>(Error.New("error"));
```

---

## 2. Capability Matrix per Pipeline

Each Pipeline requires different capabilities from the response type. The following matrix shows which capabilities (R1-R4) each Pipeline needs, and serves as the basis for the interface hierarchy separation designed in Part 3.

| Pipeline | Read (R1) | Create (R2) | Error Access (R3) | Direct Expression (R4) |
|----------|:---------:|:---------:|:--------------:|:--------------:|
| Validation | | O | | |
| Exception | | O | | |
| Logging | O | | O | |
| Tracing | O | | O | |
| Metrics | O | | O | |
| Transaction | O | | | |
| Caching | O | | | |

**Key observations:**
- **Validation/Exception**: Only need to **create** failure responses (Create-Only)
- **Logging/Tracing/Metrics**: Need to **read** success/failure + access error information
- **Transaction/Caching**: Only need to **read** success/failure

This difference is the basis for the interface hierarchy designed in Part 3.

> In actual Functorium, a Custom Pipeline slot is also provided. The required capabilities (R1-R4) for Custom Pipelines vary by implementation, and the desired constraints are applied by combining interfaces from the IFinResponse hierarchy.

---

## 3. Approach Comparison

| Approach | R1 | R2 | R3 | R4 | Reflection |
|-----------|:--:|:--:|:--:|:--:|:--------:|
| Fin\<T\> Direct (Section 2) | X | X | X | O | 3 places |
| IFinResponse Wrapper (Section 3) | △ | X | O | X | 1 place |
| IFinResponse Hierarchy (Part 3) | O | O | O | O | 0 places |

### Using Fin<T> Directly (Section 2)

- **Pros**: `Fin<T>` itself directly expresses success/failure (R4 satisfied)
- **Cons**: Cannot be used as constraint due to sealed struct, requires reflection in 3 places

### IFinResponse Wrapper (Section 3)

- **Pros**: Reduces reflection to 1 place (is casting), error access possible (R3 satisfied)
- **Cons**: `CreateFail` not possible (R2 unsatisfied), dual interface (R4 unsatisfied)
- R1 is △ because: accessible via `is` casting, but not a compile-time guarantee

### IFinResponse Hierarchy (Designed in Part 3)

- **Pros**: Satisfies all 4 requirements, 0 reflection sites
- Key idea: **Interface Segregation Principle (ISP)** + **static abstract members** + **CRTP pattern**

---

## 4. Solution Direction in Part 3

Part 3 designs the following interface hierarchy step by step:

| Interface | Requirement Satisfied | Key Member |
|-----------|:------------:|----------|
| `IFinResponse` | R1 | `IsSucc`, `IsFail` |
| `IFinResponse<out A>` | R1 + covariance | Value access |
| `IFinResponseFactory<TSelf>` | R2 | `static abstract CreateFail(Error)` |
| `IFinResponseWithError` | R3 | `Error` property |
| `FinResponse<A>` | R4 | Succ/Fail Discriminated Union |

Each interface solves **one requirement**, and Pipelines use only **the interfaces they need** as constraints.

```csharp
// Create-Only: Validation, Exception
where TResponse : IFinResponseFactory<TResponse>

// Read + Create: Logging, Tracing, Metrics, Transaction, Caching
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

## FAQ

### Q1: What problems arise if not all 4 requirements (R1-R4) are satisfied?
**A**: Without R1, reflection is needed for success/failure checking. Without R2, reflection is needed for failure response creation. Without R3, error information cannot be accessed. Without R4, the design becomes complex with dual interfaces. All 4 must be satisfied for type-safe Pipelines with 0 reflection sites.

### Q2: Can't R1 (Read) and R2 (Create) be combined into a single interface?
**A**: They can, but it **violates the Interface Segregation Principle (ISP)**. The Validation Pipeline only needs creation, and the Transaction Pipeline only needs reading. Combining them imposes unnecessary capabilities, and the Pipeline's intent is not expressed in the code.

### Q3: Why is the wrapper approach's R1 rated as △ (triangle)?
**A**: In the wrapper approach, success/failure can be accessed via `is IFinResponseWrapper` casting, but this is a **runtime type check**. Unlike `where` constraints that guarantee access at compile time, it is evaluated as partial satisfaction (△) rather than full satisfaction (O).

### Q4: How many interfaces compose the hierarchy designed in Part 3?
**A**: Five. `IFinResponse` (non-generic marker), `IFinResponse<out A>` (covariant interface), `IFinResponseFactory<TSelf>` (CRTP factory), `IFinResponseWithError` (error access), and `FinResponse<A>` (Discriminated Union), each solving one requirement.

---

As the first step of Part 3, the next section designs the non-generic marker interface `IFinResponse`, which solves requirement R1 (reading success/failure).

→ [Section 3.1: IFinResponse Non-Generic Marker](../Part3-IFinResponse-Hierarchy/01-IFinResponse-Marker/)
