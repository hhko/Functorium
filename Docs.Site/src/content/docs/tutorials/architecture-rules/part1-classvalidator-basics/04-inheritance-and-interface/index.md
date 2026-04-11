---
title: "Inheritance and Interfaces"
---
## Overview

There are domain entities that must inherit from an `Entity<TId>` base class. But what if a new team member creates an independent `Product` class without inheriting `Entity<TId>`? There is no `Id` property, equality comparisons break, and problems arise later in the repository. In this chapter, you will learn how to verify **inheritance relationships and interface implementations** with architecture tests to ensure domain model consistency.

> **"Domain model consistency starts with correct inheritance and interface implementation. Architecture tests guarantee that these contracts are upheld across the entire codebase."**

## Learning Objectives

### Core Learning Goals
1. **Inheritance verification**
   - `RequireInherits(typeof(Entity<>))`: Enforce entity base class inheritance
2. **Interface implementation verification**
   - `RequireImplements(typeof(IAggregate))`: Enforce specific interface implementation
   - `RequireImplements(typeof(IAuditable))`: Enforce audit interface implementation
3. **Generic interface verification**
   - `RequireImplementsGenericInterface("IRepository")`: Verify generic interface implementation

### What You Will Verify Through Practice
- Verify `Entity<TId>` inheritance with open generic types
- Enforce `IAggregate` and `IAuditable` interface implementation
- Match generic interfaces by name only

## Project Structure

```
04-Inheritance-And-Interface/
├── InheritanceAndInterface/                  # Main project
│   ├── Domains/
│   │   ├── Entity.cs                         # Abstract base entity class
│   │   ├── IAggregate.cs                     # Aggregate root marker interface
│   │   ├── IAuditable.cs                     # Audit interface
│   │   ├── Product.cs                        # Entity + IAggregate + IAuditable
│   │   └── Category.cs                       # Entity + IAuditable
│   ├── Services/
│   │   ├── IRepository.cs                    # Generic repository interface
│   │   └── ProductRepository.cs              # IRepository<Product> implementation
│   ├── Program.cs
│   └── InheritanceAndInterface.csproj
├── InheritanceAndInterface.Tests.Unit/       # Test project
│   ├── ArchitectureTests.cs
│   ├── InheritanceAndInterface.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## Code Under Verification

### Domain Hierarchy

```
Entity<TId> (abstract)
├── Product : Entity<Guid>, IAggregate, IAuditable
└── Category : Entity<Guid>, IAuditable
```

**Product** is an aggregate root and therefore implements `IAggregate`, while **Category** is a regular entity and implements only `IAuditable`.

### Repository Layer

```
IRepository<T> (interface)
└── ProductRepository : IRepository<Product>
```

## Test Code Walkthrough

### Inheritance Verification (RequireInherits)

```csharp
[Fact]
public void Entities_ShouldInherit_EntityBase()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireInherits(typeof(Entity<>)),
            verbose: true)
        .ThrowIfAnyFailures("Entity Inheritance Rule");
}
```

`RequireInherits(typeof(Entity<>))` specifies the open generic type `Entity<>` as the base class. Internally, it uses `FullName.StartsWith()` to match IL-level type names like `Entity`1[System.Guid]`.

The `.AreNotAbstract()` filter excludes `Entity<TId>` itself (which is abstract) from the verification targets.

### Interface Implementation Verification (RequireImplements)

```csharp
[Fact]
public void AuditableEntities_ShouldImplement_IAuditable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireImplements(typeof(IAuditable)),
            verbose: true)
        .ThrowIfAnyFailures("Auditable Entity Rule");
}
```

`RequireImplements` verifies whether a specified interface is implemented. In this example, the rule enforces that all concrete entities must implement `IAuditable`.

### Generic Interface Verification (RequireImplementsGenericInterface)

```csharp
[Fact]
public void Repositories_ShouldImplement_GenericIRepository()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ServiceNamespace)
        .And()
        .HaveNameEndingWith("Repository")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireImplementsGenericInterface("IRepository"),
            verbose: true)
        .ThrowIfAnyFailures("Repository Interface Rule");
}
```

`RequireImplementsGenericInterface("IRepository")` verifies implementation by the generic interface's name alone. Without needing to specify type parameters, it matches all concrete types such as `IRepository<Product>`, `IRepository<Category>`, etc.

## Summary at a Glance

The following table organizes the inheritance and interface verification methods.

### Inheritance/Interface Verification Methods

| Method | Verifies | Use Scenario |
|--------|----------|-------------|
| `RequireInherits(Type)` | Specific base class inheritance | Entity base class, DDD patterns |
| `RequireImplements(Type)` | Specific interface implementation | Marker interfaces, contract enforcement |
| `RequireImplementsGenericInterface(string)` | Generic interface implementation (name-based) | Repository, handler patterns |

### Open Generic Type Handling

When passing an open generic type with empty type parameters like `typeof(Entity<>)`, `RequireInherits` internally uses `FullName.StartsWith()`. This approach matches **all concrete types** including `Entity<Guid>`, `Entity<int>`, `Entity<ProductId>`, etc. In contrast, passing a closed generic type like `typeof(Entity<Guid>)` matches only exactly `Entity<Guid>`.

### Combining Filtering and Rules

By combining ArchUnitNET's filtering methods (`.AreNotAbstract()`, `.HaveNameEndingWith()`, etc.) with ClassValidator rules, you can apply rules only to the subset of classes that satisfy specific conditions.

## FAQ

### Q1: What is the difference between `RequireInherits` and `RequireImplements`?
**A**: `RequireInherits` verifies **the class hierarchy**. It verifies the relationship with `Entity<Guid>` in `class Product : Entity<Guid>`. `RequireImplements` verifies **interface implementation**. It verifies the relationship with `IAggregate` in `class Product : IAggregate`.

### Q2: When should `RequireImplements` vs `RequireImplementsGenericInterface` be used?
**A**: Use `RequireImplements(typeof(IAggregate))` for non-generic interfaces (`IAggregate`, `IAuditable`). For generic interfaces (`IRepository<Product>`), `RequireImplementsGenericInterface("IRepository")` is convenient as it matches by name only, ignoring type parameters.

### Q3: Can multiple interface implementations be verified at once?
**A**: Yes, through Validator chaining. You can verify multiple interface implementations in a single chain like `@class.RequireImplements(typeof(IAggregate)).RequireImplements(typeof(IAuditable))`.

### Q4: What happens if the `AreNotAbstract()` filter is removed?
**A**: The abstract class `Entity<TId>` itself is also included in the verification targets. Since `Entity<TId>` does not inherit from itself, it will fail the `RequireInherits(typeof(Entity<>))` verification. Excluding abstract base classes from the filter is a common pattern.

---

The next Part expands the scope to method signature verification through MethodValidator and property/field immutability verification.

-> [Part 2: Method and Property Verification](../../Part2-Method-And-Property-Validation/)
