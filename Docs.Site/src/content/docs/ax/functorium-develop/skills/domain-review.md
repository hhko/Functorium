---
title: "Domain Review"
description: "Code review and improvement suggestions from Eric Evans DDD perspective"
---

> A review skill that can be executed at any point in the workflow.

## Prerequisites

- Source code to be reviewed must exist.
- A specific Aggregate, layer, or the entire codebase can be specified.
- Review is performed directly from existing code even without prerequisite documents.

## Background

Consistently applying DDD tactical design principles is difficult. Problems such as ambiguous Aggregate boundaries, blurred domain event ownership, primitive types used instead of Value Objects, and business logic leaking into the Application layer are repeatedly found in code reviews.

The `/domain-review` skill systematically reviews existing code from Eric Evans' DDD perspective. It examines Aggregate boundaries, event ownership, Value Object usage, layer violations, and Functorium pattern compliance through a checklist-based approach, and provides concrete improvement directions.

## Skill Overview

### Review Checklist

| Category | Review Item | Description |
|----------|-------------|-------------|
| Aggregate Boundary | Transactional consistency | Is only one Aggregate modified per transaction |
| Aggregate Boundary | ID reference | Are inter-Aggregate references made only by ID (no direct references) |
| Aggregate Boundary | Invariant scope | Are the invariants protected by the Aggregate clear |
| Event Ownership | Publishing location | Are domain events published from Aggregate command methods |
| Event Ownership | Naming | Are events named in past tense (~Event) |
| Value Object | Primitive Obsession | Are VOs used instead of primitive types (string, int, decimal) |
| Value Object | Validation location | Is validation logic encapsulated within the VO |
| Layer Violation | Domain -> Adapter | Does the domain not depend on infrastructure technology |
| Layer Violation | Application -> Domain | Is there no business logic in the Application layer |
| Functorium Pattern | `Fin<T>` return | Do command methods return `Fin<Unit>` or `Fin<T>` |
| Functorium Pattern | sealed class | Are Aggregate, VO, and Event `sealed` |
| Functorium Pattern | private constructor | Are VO and Entity constructors `private` |

## Usage

### Basic Invocation

```text
/domain-review Review the Product Aggregate from a DDD perspective.
```

### Interactive Mode

Invoking `/domain-review` without arguments starts the skill in interactive mode to confirm the review scope.

### Output Format

Review results are output in checklist table format:

```text
| Item | Status | Violation | Improvement Suggestion |
|------|--------|-----------|----------------------|
| Aggregate Boundary | PASS | -- | -- |
| Event Ownership | WARN | ... | ... |
```

## Example 1: Beginner -- Single Aggregate Review

The most basic review. Inspects invariants, command methods, event publishing, and Value Object usage for a single Aggregate Root.

### Prompt

```text
/domain-review Review the Product Aggregate from a DDD perspective.
```

### Output Example

The skill outputs a checklist table like the following:

```text
## Product Aggregate Review Results

| Item | Status | Violation | Improvement Suggestion |
|------|--------|-----------|----------------------|
| sealed class | PASS | Product is sealed | -- |
| private constructor | PASS | Uses private constructor | -- |
| Fin<Unit> return | PASS | Both UpdateName and UpdatePrice return Fin<Unit> | -- |
| Event publishing | PASS | AddDomainEvent called in all command methods | -- |
| Value Object usage | WARN | Description is string type | Recommend introducing ProductDescription VO |
| Invariant scope | PASS | "Price must be positive" rule encoded in Money VO | -- |
| ID reference | PASS | Category referenced only by CategoryId | -- |

### Summary
- 6 PASS, 1 WARN out of 7 total items
- Key improvement: Recommend promoting Description to VO if it has domain rules
```

### Review Perspective

The skill examines the Aggregate based on the following questions:

- What **invariants** does the Aggregate protect?
- Are all state changes made through **command methods**?
- Do command methods publish **domain events**?
- Are **Value Objects** used instead of **primitive types**?
- Are failures **explicitly expressed** with `Fin<Unit>` or `Fin<T>` returns?

## Example 2: Intermediate -- Layer Dependency Review

Reviews layer-level dependencies beyond the Aggregate. Identifies code where the domain layer depends on infrastructure, business logic that has leaked into the Application layer, and incorrect `using` declarations.

### Prompt

```text
/domain-review Check if there's any code where the domain layer depends on infrastructure.
```

### Output Example

```text
## Layer Dependency Review Results

### Domain -> Adapter Dependency Check
| File | Violating using | Improvement Suggestion |
|------|----------------|----------------------|
| No violations | -- | -- |

### Application -> Domain Logic Leakage Check
| File | Violating Code | Improvement Suggestion |
|------|---------------|----------------------|
| CreateOrderCommand.cs:45 | `if (order.TotalAmount > customer.CreditLimit)` | Extract to Domain Service: OrderCreditCheckService |

### Unnecessary usings for Cleanup
| File | Unnecessary using | Reason |
|------|-------------------|--------|
| No violations | -- | -- |

### Summary
- Domain -> Adapter: No violations
- Application -> Domain logic leakage: 1 issue (credit limit validation -> Recommend extracting to Domain Service)
```

### Review Perspective

The skill verifies the following dependency rules:

- **Domain** -> Application, Adapter dependency is forbidden
- **Application** -> Adapter dependency is forbidden
- **Application** must not contain business logic (conditional branching, calculations) -> Move to Domain Service or Aggregate
- **Adapter** inter-dependency is forbidden (Presentation <-> Persistence <-> Infrastructure)

## Example 3: Advanced -- Full Bounded Context Architecture Review

Performs a comprehensive architecture review across the entire codebase. Comprehensively analyzes Aggregate boundary redesign, Domain Service extraction candidates, Value Object promotion targets, and event flow consistency.

### Prompt

```text
/domain-review Do a full architecture review from Eric Evans' DDD perspective.
```

### Output Example

```text
## Full Architecture Review Results

### 1. Per-Layer Violation Summary

| Layer | PASS | WARN | FAIL | Key Issues |
|-------|------|------|------|-----------|
| Domain | 12 | 2 | 0 | 2 Description primitive type issues |
| Application | 8 | 1 | 0 | Credit validation logic leaked into Application |
| Adapter | 6 | 0 | 0 | -- |

### 2. Aggregate Boundary Analysis

| Aggregate | Invariants | Events | Status | Improvement Suggestion |
|-----------|-----------|--------|--------|----------------------|
| Product | 3 (name, price, deletion) | 4 | PASS | -- |
| Order | 4 (line, status, amount, address) | 2 | PASS | -- |
| Customer | 2 (name, email) | 1 | WARN | Recommend promoting CreditLimit to VO |
| Inventory | 2 (quantity, threshold) | 2 | PASS | -- |

### 3. Domain Service Extraction Candidates

| Current Location | Logic | Extraction Target | Reason |
|-----------------|-------|-------------------|--------|
| CreateOrderCommand | Credit limit validation | OrderCreditCheckService | Cross-Aggregate logic |

### 4. Value Object Promotion Candidates

| Current Type | Usage Location | Domain Rule | Suggested VO |
|-------------|---------------|-------------|--------------|
| string Description | Product, Order | Max 500 chars, must not be empty | Description VO |
| decimal CreditLimit | Customer | Positive, comparison operations used | CreditLimit VO |

### 5. Event Flow Consistency

| Aggregate | Command Method | Event Published | Status |
|-----------|---------------|-----------------|--------|
| Product.Create | CreatedEvent | PASS | -- |
| Product.UpdateName | NameUpdatedEvent | PASS | -- |
| Product.Delete | DeletedEvent | PASS | -- |
| Order.Create | CreatedEvent | PASS | -- |
| Order.Confirm | ConfirmedEvent | PASS | -- |

### 6. Overall Assessment
- DDD tactical design principles are well adhered to overall
- Primitive Obsession in 2 cases -- Recommend VO promotion
- Application layer logic leakage in 1 case -- Recommend Domain Service extraction
- Aggregate boundaries and event ownership are consistent
```

### Review Perspective

The full review analyzes across 6 axes:

1. **Layer dependency** -- Is each layer's dependency direction correct
2. **Aggregate boundaries** -- Transactional consistency, ID references, invariant scope
3. **Domain Service necessity** -- Is cross-Aggregate logic in the right place
4. **Value Object utilization** -- Where Primitive Obsession remains
5. **Event flow** -- Are events published for all state changes
6. **Functorium pattern compliance** -- sealed, private constructors, `Fin<T>` returns

## ApplyT Pattern Review

Reviews correct usage of the ApplyT pattern in the Application layer.

### Checklist

| Item | Review Content | Correct Example |
|------|---------------|-----------------|
| Error accumulation | Are errors accumulated with Apply when validating multiple VOs | `(v1, v2).Apply((a, b) => ...)` |
| CreateFromValidated | Is the validation-bypassing factory used inside Apply | `ProductName.Create(n).ThrowIfFail()` |
| Single field | Is unnecessary Apply not used when there's only 1 VO | Simple `Create` + `if (IsFail)` |
| ToFin conversion | Is the `.As().ToFin()` chain correct | `Validation -> Fin` conversion |

### Common Violations

```text
| Item | Status | Violation | Improvement Suggestion |
|------|--------|-----------|----------------------|
| ApplyT error accumulation | WARN | 3 VOs validated sequentially | Apply pattern for parallel validation + error accumulation |
| CreateFromValidated | FAIL | Create re-called inside Apply | Use Create -> ThrowIfFail to bypass validation |
```

## Observability Review

Reviews correct usage of CtxEnricher and Observable Port.

### Checklist

| Item | Review Content |
|------|---------------|
| CtxPillar appropriateness | Are high-cardinality fields not propagated to MetricsTag |
| CtxRoot promotion | Are key identifiers promoted to ctx.{field} root |
| ObservableSignal | Is ObservableSignal used for internal adapter logging |
| Observable Port | Is `[GenerateObservablePort]` applied to all Ports |
| RequestCategory | Are correct categories (`"repository"`, `"query"`, `"external_api"`) used |
| error.type classification | Is expected/exceptional/aggregate classification correct |

### Output Example

```text
## Observability Review Results

| Item | Status | Violation | Improvement Suggestion |
|------|--------|-----------|----------------------|
| CtxPillar | FAIL | customer_id set to CtxPillar.All | Change to CtxPillar.Default (high cardinality) |
| Observable Port | PASS | [GenerateObservablePort] applied to all Repositories | -- |
| RequestCategory | WARN | ExternalApiAdapter RequestCategory missing | Set to "external_api" |
```

## References

### Workflow

- [Workflow](../workflow/) -- 7-step overall flow
- [Test Develop Skill](../test-develop/) -- Automate review items with architecture rule tests

### Framework Guides

- [DDD Tactical Design Overview](../guides/domain/04-ddd-tactical-overview/)
- [Value Objects](../guides/domain/05a-value-objects/)
- [Aggregate Design](../guides/domain/06a-aggregate-design/)
- [Entity/Aggregate Core](../guides/domain/06b-entity-aggregate-core/)
- [Domain Events](../guides/domain/07-domain-events/)
- [Error System](../guides/domain/08a-error-system/)
- [Specification](../guides/domain/10-specifications/)
- [Domain Services](../guides/domain/09-domain-services/)

### Related Skills

- [Domain Develop Skill](../domain-develop/) -- Implement improvements found in review as code
- [Application Layer Develop Skill](../application-develop/) -- Restructure Usecases after Domain Service extraction
- [Test Develop Skill](../test-develop/) -- Automatically detect violations with architecture rule tests
