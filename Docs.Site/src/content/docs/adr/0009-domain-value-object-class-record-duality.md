---
title: "ADR-0009: Domain - Value Object Class/Record Dual Hierarchy"
status: "accepted"
date: 2026-03-20
---

## Context and Problem

Suppose you are implementing an `Email` Value Object. The key concern is wrapping a string value with format validation (`@` included, max length, etc.) and determining that two `Email` instances with the same string are equal -- value equality. The `OrderStatus` Value Object, on the other hand, has an entirely different nature. It represents a finite set of states like `Pending`, `Confirmed`, `Shipped`, and `Delivered`, where the key concerns are transition rules that allow `Pending -> Confirmed` but block `Shipped -> Pending`, and exhaustive pattern matching via `switch` expressions that ensure every state is handled.

`Email` needs `Equals`/`GetHashCode` overrides and constructor validation, while `OrderStatus` needs a sealed hierarchy and C# pattern matching. Attempting to satisfy both requirements with a single base type would either force an unnecessary sealed hierarchy on `Email` or require `OrderStatus` to use if-else branches instead of pattern matching, sacrificing expressiveness on one side.

## Considered Options

1. Class hierarchy + Record hierarchy in parallel
2. Single Class hierarchy
3. Single Record hierarchy
4. Interface only (no implementation)

## Decision

**Chosen option: "Class hierarchy + Record hierarchy in parallel"**, because value-wrapping VOs like `Email` and state-set VOs like `OrderStatus` require fundamentally different language features, so providing a dedicated hierarchy that maximizes each one's strengths is necessary.

**Class Hierarchy** (traditional Value Object):
- `AbstractValueObject` -> `ValueObject` -> `SimpleValueObject<T>` / `ComparableSimpleValueObject<T>`
- Equality comparison (`Equals`, `GetHashCode`) is handled in the base class.
- `SimpleValueObject<T>` is for single-value wrapping; `ComparableSimpleValueObject<T>` for comparable value wrapping.

**Record Hierarchy** (Discriminated Union):
- `UnionValueObject<TSelf>`
- Leverages C# record's structural equality and `with` expressions.
- Uses sealed record inheritance to represent finite state sets.

### Consequences

- <span class="adr-good">Good</span>, because value-wrapping VOs like `Email` and `Money` optimally leverage automatic `Equals`/`GetHashCode` handling from the Class hierarchy, while state VOs like `OrderStatus` optimally leverage pattern matching from the Record hierarchy.
- <span class="adr-good">Good</span>, because `SimpleValueObject<T>` handles `Equals`, `GetHashCode`, `ToString`, and comparison operators in the base class, eliminating repetitive equality comparison code across implementations.
- <span class="adr-good">Good</span>, because in the `UnionValueObject<TSelf>`-based sealed record hierarchy, C# `switch` expressions display unhandled cases as compile warnings when a new state is added.
- <span class="adr-bad">Bad</span>, because the team must decide "is this VO a Class hierarchy or Record hierarchy?" requiring documentation and sharing of selection criteria ("does it wrap a value, or represent a set of states?").

### Confirmation

- Verify through architecture rule tests that Value Objects must inherit from one of the two hierarchies.
- Verify during code reviews that Union Value Object sealed record hierarchies represent complete state sets.

## Pros and Cons of the Options

### Class Hierarchy + Record Hierarchy in Parallel

- <span class="adr-good">Good</span>, because `Email` inherits `SimpleValueObject<string>` for equality and validation, while `OrderStatus` inherits `UnionValueObject<OrderStatus>` for pattern matching, each leveraging optimal C# features.
- <span class="adr-good">Good</span>, because the Class hierarchy provides only equality/comparison and the Record hierarchy provides only sealed inheritance/pattern matching, keeping each hierarchy's responsibilities clear and simple.
- <span class="adr-good">Good</span>, because C# `switch` expression exhaustiveness checks in the Record hierarchy flag unhandled cases at compile time when a new state is added.
- <span class="adr-bad">Bad</span>, because the selection criterion "Class for value wrapping, Record for state sets" must be documented and consistently applied during code reviews.

### Single Class Hierarchy

- <span class="adr-good">Good</span>, because all VOs inherit `AbstractValueObject`, eliminating the need for base type selection decisions.
- <span class="adr-bad">Bad</span>, because implementing `OrderStatus` as a Class loses `switch` expression exhaustiveness checks, and the compiler cannot catch unhandled branches when a new state is added.
- <span class="adr-bad">Bad</span>, because representing finite state sets via Class inheritance requires manually restricting inheritance without the sealed keyword, and `if-else` or `is` checks must be used instead of pattern matching.

### Single Record Hierarchy

- <span class="adr-good">Good</span>, because C# record structural equality (auto-generated `Equals`, `GetHashCode`) can be leveraged across all VOs without separate implementation.
- <span class="adr-bad">Bad</span>, because custom equality logic like rounding `Amount` decimal places before comparison in complex value types like `Money(Amount, Currency)` cannot be expressed with record's auto-generated `Equals`, requiring separate overrides.
- <span class="adr-bad">Bad</span>, because record `with` expressions (`email with { Value = "new@test.com" }`) allow value changes that bypass validation, circumventing the immutability contract.

### Interface Only (No Implementation)

- <span class="adr-good">Good</span>, because only an `IValueObject` interface is defined, and implementations can freely choose class or record.
- <span class="adr-bad">Bad</span>, because common logic needed by all VOs -- `Equals`, `GetHashCode`, `ToString`, validation -- must be repeatedly written in each implementation, and implementation omissions lead to runtime bugs.
- <span class="adr-bad">Bad</span>, because an interface alone cannot enforce the contract that "all VOs are immutable and guarantee value equality," potentially resulting in some VOs with missing equality or exposed mutable state.

## Related Information

- Related commit: `5c347e54`
- Related spec: `spec/02-value-object`
- Related tutorial: `Docs.Site/src/content/docs/tutorials/functional-valueobject/`
