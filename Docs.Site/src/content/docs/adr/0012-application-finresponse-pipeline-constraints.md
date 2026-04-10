---
title: "ADR-0012: Application - FinResponse Pipeline Type Constraint Hierarchy"
status: "accepted"
date: 2026-03-18
---

## Context and Problem

In the validation pipeline, when validation fails, `FinResponse<T>.Fail(error)` must be created, but the pipeline's `TResponse` was only constrained to `object`, requiring double casting like `(TResponse)(object)FinResponse<T>.Fail(error)`. This casting is not verified by the compiler, so when registered on a pipeline whose response type is not `FinResponse<T>`, an `InvalidCastException` would occur at runtime. The logging pipeline only needs to read `IsSucc`/`IsFail`, the validation pipeline must create failure responses, and the observability pipeline must access error details -- yet these different levels of access requirements were all mixed together inside a single `object` cast.

A constraint system was needed where each pipeline declares only the minimum contract it needs through its type signature, blocking incorrect combinations at compile time and making the pipeline's intent immediately readable from code.

## Considered Options

- **Option 1**: `object` casting
- **Option 2**: Single interface unifying all capabilities
- **Option 3**: IFinResponse 4-level interface hierarchy + `where` constraints
- **Option 4**: Reflection-based dynamic dispatch

## Decision

**Option 3: Adopt the IFinResponse 4-level interface hierarchy + `where` constraints.**

Each pipeline requires a different level of access. Logging only needs to read success/failure status. Validation must create failure responses. Observability must access error details. Instead of conflating these differences with `object` casting, they are separated into 4 interface levels so each pipeline declares only its minimum contract via `where TResponse :` constraints.

1. **IFinResponse (marker)**: Marks the response as being of the FinResponse family. The loosest constraint, serving as the default entry point for all FinResponse pipelines.
2. **IFinResponseCovariant (covariant)**: `IsSucc`/`IsFail` success/failure determination. Used where read-only access is sufficient, like logging pipelines.
3. **IFinResponseFactory (CRTP factory)**: Creates new response instances with `Fail<T>(error)`, etc. Used when validation pipelines need to directly create failure responses.
4. **IFinResponseErrorAccessor (error access)**: Accesses error detail information on failure. Used in error classification and observability pipelines.

Just by looking at a pipeline's `where TResponse :` signature, one can immediately tell which aspect of the response the pipeline accesses.

### Consequences

- **Positive**: `object` casting is completely eliminated, structurally preventing runtime `InvalidCastException`. The logging pipeline constrained to only `IFinResponseCovariant` does not expose factory or error access methods, making unintended usage impossible. When adding new pipelines, which of the 4 interfaces to choose is clear, speeding up design decisions. 85 tests verify correct operation and compile constraints across all pipeline-interface combinations.
- **Negative**: The 4-level interface hierarchy and CRTP pattern raise the initial learning curve. Four interfaces starting with `IFinResponse` may lengthen IDE autocomplete lists.

### Confirmation

- 85 unit tests verify all pipeline-interface combinations.
- Verify that using incorrect `where` constraints produces compile errors.
- Provide guidance on selecting the appropriate interface level when adding new pipelines.

## Pros and Cons of the Options

### Option 1: object Casting

- **Pros**: No interface design needed. Works with a single line of `(FinResponse<T>)(object)response`.
- **Cons**: Registering on a pipeline whose response type is not `FinResponse<T>` triggers runtime `InvalidCastException`. Pipeline signatures alone do not reveal which response type is expected, requiring digging through existing code when writing new pipelines. The compiler cannot detect incorrect casts during refactoring.

### Option 2: Single Interface Unifying All Capabilities

- **Pros**: One interface keeps the learning curve low. All pipelines use the same constraint, ensuring consistency.
- **Cons**: The logging pipeline only needs `IsSucc` but the `Fail()` factory and error access methods are also exposed. This violates the Interface Segregation Principle (ISP). Adding new methods to the interface requires modifying all implementations, resulting in high extension cost.

### Option 3: IFinResponse 4-Level Interface Hierarchy

- **Pros**: The logging pipeline constrains only `IFinResponseCovariant`, validation constrains `IFinResponseFactory`, each declaring only the minimum contract needed. Covariant interfaces allow flexible handling of generic type parameters in read-only pipelines. CRTP factory makes `Fail<T>(error)` calls type-safe. Incorrect `where` constraints are immediately caught as compile errors.
- **Cons**: Onboarding cost of explaining the 4-level interface hierarchy and CRTP pattern design intent to new team members. Four interfaces starting with `IFinResponse` increase IDE autocomplete and documentation burden.

### Option 4: Reflection-Based Dynamic Dispatch

- **Pros**: Response types can be dynamically inspected and processed at runtime without interface design. No modification of existing types required.
- **Cons**: Reflection calls on every request degrade hot path performance. No compile-time verification at all, so type mismatches are discovered only in production. Stack traces passing through reflection internals make debugging difficult.

## Related Information

- Commits: ace89d39 (85 tests), 91b57254, 33821633
