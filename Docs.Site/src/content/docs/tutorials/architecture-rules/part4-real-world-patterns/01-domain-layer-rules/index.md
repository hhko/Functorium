---
title: "Domain Layer Rules"
---

## Overview

Entity, Value Object, Domain Event, Specification, Domain Service -- each element of DDD tactical patterns has its own design rules. AggregateRoot must be sealed and have `[GenerateEntityId]`, Value Objects must be immutable with `Create` returning `Fin<T>` and `Validate` returning `Validation<Error, T>`. Domain Events must be sealed records, and Specifications must exist only in the domain layer.

In this chapter, we implement the **21 rules** verified by Functorium's `DomainArchitectureTestSuite`, organized into 6 categories.

> **"When structural rules for domain models are enforced through tests, code review comments asking 'is this also sealed?' and 'does it have a factory method?' disappear every time a new Entity is added."**

## Learning Objectives

### Core Learning Goals

1. **Separating AggregateRoot and Entity**
   - AggregateRoot: public sealed, factory methods (`Create`/`CreateFromValidated`), `[GenerateEntityId]`, private constructors
   - Entity (excluding AggregateRoot): public sealed, factory methods, private constructors

2. **Value Object Fin/Validation return type verification**
   - `Create` -> `Fin<T>` (single error, Railway-Oriented)
   - `Validate` -> `Validation<Error, T>` (multiple error accumulation)

3. **DomainEvent: sealed record + Event suffix**
   - `RequireRecord()` -- ensures value semantics and immutability
   - `RequireNameEndsWith("Event")` -- ubiquitous language consistency

4. **Specification: domain layer only**
   - `Specification<T>` inheritance verification
   - Prevents leaking outside the domain layer

5. **IDomainService marker interface-based verification**
   - `sealed class : IDomainService` pattern instead of `static class`
   - `RequireNoDependencyOn("IObservablePort")` -- architecture boundary violation detection
   - Public instance methods must return `Fin`

### What You Will Verify Through Practice
- **Order (AggregateRoot)**: sealed, `[GenerateEntityId]`, factory method, private constructors
- **Money, Address (Value Object)**: sealed, immutable, `Create -> Fin<T>`, `Validate -> Validation<Error, T>`
- **OrderCreatedEvent (Domain Event)**: sealed record, "Event" suffix
- **ActiveOrderSpecification (Specification)**: sealed, `Specification<T>` inheritance, domain layer only
- **OrderPricingService (Domain Service)**: sealed, IDomainService, stateless, Fin return

## Domain Code Structure

### Base Types

```
Domains/
├── Entity.cs                     # Entity base abstract class
├── AggregateRoot.cs              # Aggregate Root abstract class (inherits Entity<TId>)
├── IValueObject.cs               # Value object marker interface
├── DomainEvent.cs                # Domain event base abstract record
├── Specification.cs              # Specification base abstract class
├── IDomainService.cs             # Domain service marker interface
└── GenerateEntityIdAttribute.cs  # Source generator trigger attribute
```

**`AggregateRoot<TId>`** is an abstract class that inherits `Entity<TId>`. It separates Aggregate-specific responsibilities such as domain event management and invariant protection.

**`IDomainService`** is a marker interface that identifies domain services. Using this interface instead of `static class` enables DI container registration, architecture test filtering, and dependency control.

**`Specification<T>`** is an abstract class that encapsulates business rules. It expresses conditions through the `IsSatisfiedBy(T)` method.

### Implementation Types

| Type | Pattern | Core Rules |
|------|---------|------------|
| `Order` | AggregateRoot | public, sealed, `[GenerateEntityId]`, factory method, private constructors |
| `Money`, `Address` | Value Object | public, sealed, immutable, `Create -> Fin<T>`, `Validate -> Validation<Error, T>` |
| `OrderCreatedEvent` | Domain Event | sealed record, "Event" suffix |
| `ActiveOrderSpecification` | Specification | public, sealed, `Specification<T>` inheritance |
| `OrderPricingService` | Domain Service | public, sealed, IDomainService, stateless, Fin return |

## Test Code Walkthrough

### AggregateRoot vs Entity Separation

`DomainArchitectureTestSuite` verifies AggregateRoot and Entity as separate categories. AggregateRoot is a transaction boundary requiring a strongly-typed ID via `[GenerateEntityId]`, while Entity is a subordinate entity within an AggregateRoot where independent ID generation may not be needed.

```csharp
// AggregateRoot: classes inheriting AggregateRoot<> among Entity<>
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreAssignableTo(typeof(AggregateRoot<>))
    .And().AreNotAbstract()
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNotStatic(),
        verbose: true)
    .ThrowIfAnyFailures("AggregateRoot Visibility Rule");

// Entity: inherits Entity<> but excludes AggregateRoot<>
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreAssignableTo(typeof(Entity<>))
    .And().AreNotAbstract()
    .And().AreNotAssignableTo(typeof(AggregateRoot<>))  // Exclude AggregateRoot
    // ...
```

### Factory Method Return Type Verification

**`RequireReturnTypeOfDeclaringClass()`** verifies that a factory method returns its own type. If `Order.Create()` does not return `Order`, it is a violation. This rule prevents the mistake of factory methods returning the wrong type.

```csharp
.RequireMethod("Create", m => m
    .RequireVisibility(Visibility.Public)
    .RequireStatic()
    .RequireReturnTypeOfDeclaringClass())
.RequireMethod("CreateFromValidated", m => m
    .RequireVisibility(Visibility.Public)
    .RequireStatic()
    .RequireReturnTypeOfDeclaringClass())
```

### Value Object Create/Validate Return Types

`Create` must return `Fin<T>`, and `Validate` must return `Validation<Error, T>`. The two methods serve different roles:
- **`Create`** -- "fail immediately on any single error" (Railway-Oriented)
- **`Validate`** -- "accumulate and collect all errors" (Applicative)

```csharp
// Create -> Fin<T>
.RequireMethod("Create", m => m
    .RequireStatic()
    .RequireReturnType(typeof(Fin<>)))

// Validate -> Validation<Error, T>
.RequireMethod("Validate", m => m
    .RequireStatic()
    .RequireReturnType(typeof(Validation<,>)))
```

### DomainEvent Sealed Record Verification

Domain Events require value semantics. Two `OrderCreatedEvent` instances generated with the same order ID are the same event. `record` automatically ensures this equality, and `sealed` prevents changes to the event contract.

```csharp
.ValidateAllClasses(Architecture, @class => @class
    .RequireSealed()
    .RequireRecord(),      // Enforce record type
    verbose: true)
```

### Specification Domain Layer Restriction

Since Specifications encapsulate business rules, they must exist only in the domain layer. If a Specification appears in the Application or Infrastructure layer, business rules have leaked.

```csharp
ArchRuleDefinition.Classes()
    .That()
    .AreAssignableTo(typeof(Specification<>))
    .And().AreNotAbstract()
    .And().ResideInNamespace(DomainNamespace)
    .Should().ResideInNamespace(DomainNamespace)
    .Check(Architecture);
```

### IDomainService Architecture Boundary Verification

`RequireNoDependencyOn("IObservablePort")` enforces that domain services do not depend on observability concerns. Logging, metrics, and tracing should be handled as cross-cutting concerns in the Application layer's Usecase Pipeline.

```csharp
// Architecture boundary violation detection
.ValidateAllClasses(Architecture, @class => @class
    .RequireNoDependencyOn("IObservablePort"),
    verbose: true)

// Public instance methods must return Fin
.RequireAllMethods(
    m => m.Visibility == Visibility.Public
         && m.IsStatic != true
         && m.MethodForm == MethodForm.Normal,
    method => method.RequireReturnTypeContaining("Fin"))
```

## Summary at a Glance

### 6 Categories x 21 Rules

| Category | Tests | Core Rules |
|----------|-------|------------|
| **AggregateRoot (4)** | PublicSealed, Create/CreateFromValidated, GenerateEntityId, PrivateCtors | Transaction boundary, source generator integration |
| **Entity (3)** | PublicSealed, Create/CreateFromValidated, PrivateCtors | AggregateRoot exclusion filter |
| **ValueObject (4)** | PublicSealed+PrivateCtors, Immutable, Create->`Fin<>`, Validate->`Validation<,>` | Dual return type verification |
| **DomainEvent (2)** | SealedRecord, NameEndsWith("Event") | record + naming rule |
| **Specification (3)** | PublicSealed, InheritsBase, ResideInDomain | Domain restriction |
| **DomainService (5)** | PublicSealed, Stateless, NoDependencyOn, ReturnFin, NotRecord | Marker interface based |

### Abstract Class Exclusion Patterns

| Scenario | Filter Combination | Reason |
|----------|-------------------|--------|
| AggregateRoot verification | `AreAssignableTo(typeof(AggregateRoot<>))` + `AreNotAbstract()` | Exclude `AggregateRoot<>` itself |
| Entity verification | `AreAssignableTo(typeof(Entity<>))` + `AreNotAbstract()` + `AreNotAssignableTo(typeof(AggregateRoot<>))` | Separate Entity from AggregateRoot |
| Value Object verification | `ImplementInterface(typeof(IValueObject))` + `AreNotAbstract()` | Marker interface filtering |
| DomainService verification | `ImplementInterface(typeof(IDomainService))` + `AreNotAbstract()` | Marker interface filtering |

## FAQ

### Q1: Why are AggregateRoot and Entity distinguished?
**A**: AggregateRoot is a transaction boundary and must have a strongly-typed ID via `[GenerateEntityId]`. Entity is a subordinate entity within an AggregateRoot (e.g., `OrderItem`) that may not need independent ID generation. The Suite separates the two categories and applies different rules to each.

### Q2: Why use `IDomainService` instead of `static class` for Domain Services?
**A**: `static class` cannot be registered in a DI container and cannot be selected with the `ImplementInterface` filter. Using the `IDomainService` marker interface enables: (1) filtering only domain services precisely in architecture tests, (2) verifying architecture boundaries with `RequireNoDependencyOn`, and (3) extension for DI registration when needed.

### Q3: What problem does `RequireNoDependencyOn("IObservablePort")` prevent?
**A**: When a domain service depends on logging/metrics/tracing interfaces, pure domain logic becomes contaminated with infrastructure concerns. Observability should be handled as a Cross-Cutting Concern in the Application layer's Usecase Pipeline.

### Q4: Why require both `Create -> Fin<T>` and `Validate -> Validation<Error, T>`?
**A**: `Create` is a Railway-Oriented pattern that fails immediately on a single error, and `Validate` is an Applicative pattern that accumulates all errors. In a Command Usecase, `Create` fails fast, while in Application layer DTO validation, `Validate` shows all errors to the user at once.

---

By enforcing the 6-category, 21-rule set for the domain layer through tests, rule compliance is automatically verified every time a new domain object is added. The next chapter examines application layer rules based on the Command/Query pattern.

-> [Ch 2: Application Layer Rules](../02-Application-Layer-Rules/)
