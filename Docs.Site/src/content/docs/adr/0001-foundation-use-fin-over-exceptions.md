---
title: "ADR-0001: Foundation - Represent Failures with Fin Types Instead of Exceptions"
status: "accepted"
date: 2026-03-26
---

## Context and Problem

Suppose you are writing an order creation handler. The `PlaceOrder(command)` signature alone does not reveal which failures can occur -- insufficient stock, price changes, or payment limit exceeded. The caller must open the source code to trace every `throw` statement one by one, or dig through documentation. When try-catch blocks are nested, the business flow of "check stock -> validate price -> request payment -> confirm order" gets buried under exception-handling branches. Even when a new failure type is added, the compiler gives no warning, so unhandled exceptions are only discovered at runtime in production.

Functorium must solve this problem at the type level. A type system is needed that can address each scenario: single-value failures (order rejected due to insufficient stock), parallel validation failures (validate product name, price, and category all at once and collect all errors), and failures involving side effects (timeout after calling an external payment API).

## Considered Options

1. Fin/Validation/FinT type system (LanguageExt)
2. Custom Result type implementation
3. FluentResults library
4. ErrorOr library
5. Keep exceptions (status quo)

## Decision

**Chosen option: "Fin/Validation/FinT type system (LanguageExt)"**. Paths that fail from a single cause, like insufficient stock, are represented with `Fin<T>`. Paths that must validate multiple fields simultaneously and return all errors at once, like product creation, use `Validation<Error, T>`. Failures involving side effects, like external payment API calls, are expressed with `FinT<IO, T>`. Rather than lumping all three scenarios into a single type, each scenario gets a type optimized for its specific needs.

### Consequences

- <span class="adr-good">Good</span>, because just by seeing the return type `Fin<Order>`, you know the handler can fail, and missing failure handling surfaces as a compile error.
- <span class="adr-good">Good</span>, because `Bind`/`Map` chains and LINQ query syntax compose the "check stock -> validate price -> payment -> confirm order" flow declaratively without try-catch.
- <span class="adr-good">Good</span>, because `ApplyT` composes product name/price/category validation results in parallel, returning all errors to the user at once.
- <span class="adr-bad">Bad</span>, because C# developers unfamiliar with functional concepts like `Bind`, `Map`, and `FinT` face a learning curve during code reviews and debugging.
- <span class="adr-bad">Bad</span>, because System.Text.Json cannot deserialize LanguageExt collection types such as `Seq<T>`, requiring adapter code to convert to `List<T>` in API response DTOs.

### Confirmation

- Verify that handlers returning `Fin<T>` handle failures via `ThrowIfFail()` or pattern matching.
- Verify that validation logic uses `Validation<Error, T>` for parallel error collection.
- Verify through architecture tests that no business logic throws exceptions.

## Pros and Cons of the Options

### Fin/Validation/FinT Type System (LanguageExt)

- <span class="adr-good">Good</span>, because single failures (`Fin<T>`), parallel validation (`Validation<Error, T>`), and side effects (`FinT<IO, T>`) are distinguished by dedicated types per scenario, making intent explicit in code.
- <span class="adr-good">Good</span>, because LINQ query syntax and `Bind`/`Map`/`ApplyT` compose business steps, resulting in highly readable pipelines.
- <span class="adr-good">Good</span>, because escape hatches like `Unwrap()` and `ThrowIfFail()` allow gradual migration from exception-based code.
- <span class="adr-bad">Bad</span>, because `Fin<T>` propagates across all layers -- Domain, Application, Adapter -- creating a structural dependency on LanguageExt.

### Custom Result Type Implementation

- <span class="adr-good">Good</span>, because `Result<T, E>` level success/failure can be expressed without external dependencies.
- <span class="adr-bad">Bad</span>, because `SelectMany` (LINQ composition), `ApplyT` (parallel validation), and IO monad transformers all must be implemented and tested from scratch, essentially rewriting parts of LanguageExt.
- <span class="adr-bad">Bad</span>, because every new composition scenario requires adding more extension methods, accumulating maintenance burden over time.

### FluentResults Library

- <span class="adr-good">Good</span>, because it is widely used in the .NET ecosystem, reducing onboarding effort for new team members.
- <span class="adr-bad">Bad</span>, because it does not support `Bind`/`Map`/LINQ composition, so connecting multiple steps ultimately regresses to if-else branching.
- <span class="adr-bad">Bad</span>, because errors are string-based, making type-safe classification like `DomainErrorKind.InsufficientStock` and pattern matching impossible.

### ErrorOr Library

- <span class="adr-good">Good</span>, because the API is intuitive and lightweight, adoptable within 30 minutes.
- <span class="adr-bad">Bad</span>, because it does not support LINQ query syntax, so business pipelines of 3 or more steps cannot be composed declaratively.
- <span class="adr-bad">Bad</span>, because there is no Applicative composition (parallel validation), so the pattern of validating multiple fields simultaneously and collecting all errors, as in product creation, cannot be implemented.

### Keep Exceptions (Status Quo)

- <span class="adr-good">Good</span>, because the existing C# code style is preserved, requiring no additional learning.
- <span class="adr-bad">Bad</span>, because compilation succeeds even when a caller does not catch a newly added exception type, and unhandled failures surface as 500 errors in production.
- <span class="adr-bad">Bad</span>, because try-catch nesting obscures business flow, and composing validate -> process -> save steps with `Bind`/`Map` is structurally impossible.

## Related Information

- Related commit: `b967b91c` refactor(validation): Introduce Fin<T>.Unwrap() and migrate handlers to ThrowIfFail
- Related commit: `47d88180` feat(validation): Add FinApplyExtensions.ApplyT and CreateProductCommand reference implementation
- Related commit: `3cb5c29b` feat(domain): Add Fin tuple Apply extension methods
- Related docs: `Docs.Site/src/content/docs/guides/domain/`
