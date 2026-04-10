---
title: "ADR-0002: Foundation - Adopt LanguageExt as the Functional Base Library"
status: "accepted"
date: 2026-03-15
---

## Context and Problem

Functorium aims to compose business logic using functional programming patterns on top of C# and to control side effects. A simple `Result<T>` type alone cannot express requirements like "apply a 3-second timeout to an external payment API call, retry twice on failure, and compose with the next step on success." This inevitably leads to falling back on try-catch and manual retry loops, burying business flow under infrastructure code.

A functional library adopted at the framework level affects every layer from Domain to Adapter. Therefore, it must support not only error-handling types like Fin/Validation, but also deferred execution of side effects via IO monad, composition of compound effects through monad transformers (FinT), and type-safe handling of infrastructure concerns such as Timeout/Retry/Fork/Bracket.

## Considered Options

1. LanguageExt
2. CSharpFunctionalExtensions
3. OneOf
4. Custom implementation

## Decision

**Chosen option: "LanguageExt"**. Among the four options reviewed, it is the only one that provides all the functional abstractions Functorium requires -- error handling (Fin/Validation), side-effect deferral (IO), compound effect composition (FinT), LINQ query syntax, and Timeout/Retry/Fork/Bracket -- in a single library.

### Consequences

- <span class="adr-good">Good</span>, because error handling (Fin/Validation), side-effect control (IO), and compound effect composition (FinT) are unified in a single library without combining separate packages, maintaining API consistency.
- <span class="adr-good">Good</span>, because LINQ query syntax enables declarative "validate -> domain logic -> persist -> publish event" pipelines, making business flow visible directly in code.
- <span class="adr-good">Good</span>, because infrastructure concerns like Timeout, Retry, Fork, and Bracket compose on top of the IO type, eliminating try-catch and manual retry loops.
- <span class="adr-bad">Bad</span>, because many concepts are borrowed from Haskell -- `FinT`, `Eff`, `Aff`, etc. -- so C# developers without functional programming experience need significant learning time to grasp monad transformers.
- <span class="adr-bad">Bad</span>, because LanguageExt is a large library containing hundreds of types and extension methods, and unused types like Either and Eff are also included in the dependency.

### Confirmation

- Verify that core packages (`Functorium.Core`, `Functorium.Application`, etc.) reference LanguageExt.
- Verify through pipeline tests that side effects are deferred and composed via the IO monad.

## Pros and Cons of the Options

### LanguageExt

- <span class="adr-good">Good</span>, because Fin, Validation, Option, Either, IO, FinT, and all other types needed across every Functorium layer are provided in a single package.
- <span class="adr-good">Good</span>, because it implements LINQ `SelectMany`, enabling monadic composition via `from ... in ... select` syntax in a form familiar to C# developers.
- <span class="adr-good">Good</span>, because Timeout/Retry/Fork/Bracket compose type-safely on top of the IO monad, preventing infrastructure concerns from invading business logic.
- <span class="adr-bad">Bad</span>, because major version upgrades may introduce breaking changes, resulting in high migration costs that impact the entire framework.
- <span class="adr-bad">Bad</span>, because LanguageExt experienced developers are rare in the .NET ecosystem, limiting the hiring pool and requiring additional training for new team members.

### CSharpFunctionalExtensions

- <span class="adr-good">Good</span>, because it provides DDD-friendly types like Result, Maybe, and ValueObject, with a low learning curve enabling quick team adoption.
- <span class="adr-good">Good</span>, because high NuGet download counts indicate rich community support and references.
- <span class="adr-bad">Bad</span>, because there is no IO monad, so side effects like "retry on failure after payment API call" cannot be composed type-safely and ultimately revert to try-catch.
- <span class="adr-bad">Bad</span>, because there are no monad transformers, making it structurally impossible to compose compound effects like `Fin` + `IO` in a single pipeline.

### OneOf

- <span class="adr-good">Good</span>, because it provides a lightweight discriminated union with `Match` for pattern matching, and is simple to adopt.
- <span class="adr-bad">Bad</span>, because it does not support LINQ composition, so multi-step business pipelines cannot be constructed declaratively.
- <span class="adr-bad">Bad</span>, because there are no monadic operations like `Bind`/`Map`, requiring manual branching to connect each step's result to the next.
- <span class="adr-bad">Bad</span>, because positional type parameters in the form `OneOf<T0, T1, T2>` cannot express a structural error classification system like `DomainErrorType`.

### Custom Implementation

- <span class="adr-good">Good</span>, because only the types Functorium actually uses can be implemented as a lightweight solution, eliminating unnecessary dependencies.
- <span class="adr-good">Good</span>, because there is no risk of breaking changes from external library version upgrades.
- <span class="adr-bad">Bad</span>, because the IO monad, monad transformers (`FinT`), and LINQ `SelectMany` integration must all be implemented and verified from scratch, meaning thousands of lines of core infrastructure code must be self-maintained.
- <span class="adr-bad">Bad</span>, because bugs in edge cases (thread safety, stack overflow prevention, etc.) can arise, and it is difficult to establish reliability without community validation.

## Related Information

- Related commit: `cda0a338` feat(functorium): Add core library package references and source structure
- Related commit: `d304ab40` refactor(ecommerce-ddd): Functional refactoring of PlaceOrderCommand Handle method
- Related docs: `Docs.Site/src/content/docs/spec/`
