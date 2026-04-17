---
title: "ADR-0010: Domain - Error Code Sealed Record Hierarchy"
status: "accepted"
date: 2026-03-20
---

## Context and Problem

Suppose an API response returns a `"NotFound"` error. From the error string alone, it is impossible to distinguish whether this is a domain error because the order does not exist, or an adapter error because an external payment service returned 404. Even when a monitoring dashboard aggregates `"NotFound"` occurrence counts, domain issues and infrastructure issues are mixed together, making meaningful analysis impossible.

The problems with string-based error management do not stop there. If one developer writes `"NotFound"` and another writes `"Notfound"`, the same error is represented by different strings, breaking consistency. Such typos are not caught at compile time and lurk until runtime, manifesting as matching failures in `switch` branches that handle specific errors. Since business rule violations (domain), authorization failures (application), and external service outages (adapter) are errors of different natures, the error type itself must structurally include layer information and context.

## Considered Options

1. Per-layer sealed record hierarchy + automatic error code generation
2. Enum-based error types
3. String constants
4. Exception class hierarchy
5. Single ErrorType (no layer distinction)

## Decision

**Chosen option: "Per-layer sealed record hierarchy + automatic error code generation"**, to represent errors through the type system instead of strings, blocking typos and duplicates at compile time and enabling immediate identification of error origin from the code itself.

- **DomainErrorType**: Defines 27 domain error types as sealed records, including `NotFound`, `InvalidState`, `InvalidTransition`, and `DuplicateValue`. The 27 types are the result of cataloging recurring domain error patterns from actual business scenarios.
- **ApplicationErrorType**: Defines application layer errors such as `Unauthorized`, `Forbidden`, and `Conflict`.
- **AdapterErrorType**: Defines adapter layer errors such as `ExternalServiceFailure` and `DatabaseError`.
- **Error code format**: Structured codes in the format `{Layer}.{Context}.{Name}` -- such as `Domain.Order.InvalidTransition` and `Application.Auth.Unauthorized` -- are automatically generated, enabling immediate identification of the layer and originating Aggregate from just the error code in logs.
- **Factory**: The `DomainError.For<T>()` method automatically extracts Context information from the generic type `T` to generate error codes, eliminating manual string assembly.

### Consequences

- <span class="adr-good">Good</span>, because using `DomainErrorType.NotFound` means a typo like `"Notfound"` is immediately caught as a compile error, structurally preventing runtime matching failures.
- <span class="adr-good">Good</span>, because `switch` expressions on the sealed record hierarchy display unhandled error types as compile warnings, ensuring exhaustive handling of all error cases.
- <span class="adr-good">Good</span>, because when `Domain.Order.InvalidTransition` appears in logs, "state transition failure in the Order Aggregate at the domain layer" can be immediately understood from the error code alone.
- <span class="adr-good">Good</span>, because `DomainError.For<Order>()` automatically extracts the Context (`Order`) from the generic type, eliminating the need to manually compose error code strings.
- <span class="adr-bad">Bad</span>, because the initial design and classification of per-layer sealed record hierarchies (27 DomainErrorType variants + ApplicationErrorType + AdapterErrorType) requires significant investment.
- <span class="adr-bad">Bad</span>, because when new domain error patterns emerge, new types must be added to the sealed record hierarchy, and existing `switch` expressions must be updated with the corresponding cases.

### Confirmation

- Verify through architecture rule tests that all error types belong to their respective layer's sealed record hierarchy.
- Verify through unit tests that error code formats follow the `{Layer}.{Context}.{Name}` pattern.

## Pros and Cons of the Options

### Per-Layer Sealed Record Hierarchy + Automatic Error Code Generation

- <span class="adr-good">Good</span>, because representing errors as types like `DomainErrorType.NotFound` blocks typos and case-sensitivity mismatches at compile time.
- <span class="adr-good">Good</span>, because `switch` expressions on sealed records alert unhandled cases as compile warnings, preventing missed error handling.
- <span class="adr-good">Good</span>, because structured error codes in the `Domain.Order.InvalidTransition` format are used consistently across log searches, Grafana dashboard filters, and API responses.
- <span class="adr-good">Good</span>, because the `IHasErrorCode` interface unifies domain/application/adapter errors under the same format (`{Layer}.{Context}.{Name}`), enabling cross-layer error handling pipelines.
- <span class="adr-bad">Bad</span>, because there is maintenance cost in initially designing the 27 DomainErrorType variants + ApplicationErrorType + AdapterErrorType hierarchy and extending it when new error patterns emerge.

### Enum-Based Error Types

- <span class="adr-good">Good</span>, because enum members like `DomainError.NotFound` prevent typos, making them safer than strings.
- <span class="adr-bad">Bad</span>, because enum members cannot carry properties, so context information like `FromState` and `ToState` for `InvalidTransition` cannot be conveyed alongside the error, requiring additional classes.
- <span class="adr-bad">Bad</span>, because even separating `DomainError` and `ApplicationError` enums, they implicitly convert to `int` in method signatures, making layer-level type distinction practically weak.
- <span class="adr-bad">Bad</span>, because adding new error types to existing enums affects all `switch` statements referencing the enum, violating the Open-Closed Principle.

### String Constants

- <span class="adr-good">Good</span>, because `const string NotFound = "NotFound";` is the simplest definition, requiring no separate type design.
- <span class="adr-bad">Bad</span>, because when `ErrorCodes.NotFound` and `"NotFound"` literals are mixed, typos in places not using the constant are not caught at compile time.
- <span class="adr-bad">Bad</span>, because including layer/context information in strings requires relying on naming conventions like `"Domain.Order.NotFound"`, which cannot be enforced.
- <span class="adr-bad">Bad</span>, because changing an error code string means IDE "Rename" refactoring does not work, requiring manual full-text search across the entire codebase.

### Exception Class Hierarchy

- <span class="adr-good">Good</span>, because hierarchies like `OrderNotFoundException : DomainException` align with the exception handling pattern familiar to .NET developers.
- <span class="adr-bad">Bad</span>, because `try-catch`-based control flow is fundamentally incompatible with `Fin<T>`'s `Map`/`Bind` pipeline, mixing two error handling paradigms in the codebase.
- <span class="adr-bad">Bad</span>, because .NET exceptions have high stack trace capture costs, causing unnecessary performance degradation when used for "expected failures" like business rule violations that occur frequently.
- <span class="adr-bad">Bad</span>, because ADR-0002 decided to represent failures with `Fin<T>` instead of exceptions, so defining error types as exception classes directly conflicts with that existing architecture decision.

### Single ErrorType (No Layer Distinction)

- <span class="adr-good">Good</span>, because all errors belong to a single `ErrorType` hierarchy, keeping the structure simple with low learning cost.
- <span class="adr-bad">Bad</span>, because the error type alone cannot distinguish whether `NotFound` means "order not in DB" (domain) or "external API returned 404" (adapter), mixing domain and infrastructure issues in monitoring.
- <span class="adr-bad">Bad</span>, because per-layer HTTP status code mapping like "return 400 for domain errors, 502 for adapter errors" is impossible with error type branching alone.

## Related Information

- Related spec: `spec/04-error-system`
- Related API: `DomainError.For<T>()` factory
- Related ADR: [ADR-0002: Represent Failures with Fin Types Instead of Exceptions](../0002-use-fin-over-exceptions/)
