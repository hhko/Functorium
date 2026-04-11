---
title: "Layer Dependency Rules"
---

## Overview

The moment the domain layer depends on infrastructure -- the foundation of the architecture collapses. "Domain depends on nothing", "Application depends only on Domain" -- when these simple rules are not upheld, layer separation is nothing more than a folder structure.

In this chapter, you will learn how to combine ArchUnitNET's native dependency checking with Functorium's `ClassValidator` to **simultaneously verify the dependency direction and internal structure of multiple layers**.

> **"Drawing arrows on an architecture diagram is easy. But to confirm those arrows are also respected in code, tests are needed."**

## Learning Objectives

### Core Learning Goals

1. **Enforcing dependency direction in a 3-layer architecture through tests**
   - Domain -> Application dependency prohibited
   - Domain -> Adapter dependency prohibited
   - Application -> Adapter dependency prohibited

2. **Combining ArchUnitNET native API with Functorium API**
   - Dependency direction verified with ArchUnitNET, class structure verified with Functorium
   - Using both tools together in a single test suite

3. **Excluding specific namespaces with `DoNotResideInNamespace`**
   - Exclude Ports sub-namespace from domain class rules
   - Port interfaces are verified with separate rules

### What You Will Verify Through Practice
- **Domain -> Application dependency prohibition**: `NotDependOnAnyTypesThat` verification
- **Domain -> Adapter dependency prohibition**: Ensuring the dependency inversion principle
- **Application -> Adapter dependency prohibition**: Maintaining application layer purity
- **Per-layer class structure verification**: Verify after excluding Ports with `DoNotResideInNamespace`

## Domain Code Structure

```
Domains/
├── Product.cs
└── Ports/
    └── IProductRepository.cs
Applications/
└── GetProduct.cs           # Nested Request, Response, Usecase
Adapters/
├── Persistence/
│   └── ProductRepository.cs
└── Presentation/
    └── ProductEndpoint.cs
```

### Layer Dependency Direction

```
Domain  <--  Application  <--  Adapter
  |                               |
  +-- Ports (interfaces) ---------+
```

- **Domain** does not depend on any layer
- **Application** depends only on Domain (uses Port interfaces)
- **Adapter** can depend on both Domain and Application

## Test Code Walkthrough

### ArchUnitNET Layer Dependency Verification

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

[Fact]
public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
{
    Types()
        .That()
        .ResideInNamespace(DomainNamespace)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespace(ApplicationNamespace)
        .Check(Architecture);
}

[Fact]
public void DomainLayer_ShouldNotDependOn_AdapterLayer()
{
    Types()
        .That()
        .ResideInNamespace(DomainNamespace)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespace(AdapterNamespace)
        .Check(Architecture);
}

[Fact]
public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
{
    Types()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespace(AdapterNamespace)
        .Check(Architecture);
}
```

### Combining with Functorium Class Verification

Along with layer dependency rules, verify the class structure of each layer:

```csharp
[Fact]
public void DomainClasses_ShouldBe_PublicAndSealed()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .DoNotResideInNamespace(DomainNamespace + ".Ports")
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic()
            .RequireSealed(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Rule");
}
```

`DoNotResideInNamespace(DomainNamespace + ".Ports")` **excludes the Ports sub-namespace.** Port interfaces are verified with separate rules, so they must be excluded from domain class rules.

## Summary at a Glance

The following table summarizes the dependency rules for a 3-layer architecture.

### Layer Dependency Rule Matrix

| Depending Layer | Domain | Application | Adapter |
|----------------|--------|-------------|---------|
| **Domain** | - | Prohibited | Prohibited |
| **Application** | Allowed | - | Prohibited |
| **Adapter** | Allowed | Allowed | - |

The following table organizes the tools used for each verification type.

### Role Division Between the Two Tools

| Verification Type | Tool | Usage |
|-------------------|------|-------|
| **Layer dependency** | ArchUnitNET `.Check()` | `NotDependOnAnyTypesThat().ResideInNamespace()` |
| **Class structure** | Functorium `ValidateAllClasses` | `RequirePublic().RequireSealed()` |
| **Namespace exclusion** | ArchUnitNET filter | `DoNotResideInNamespace()` |

## FAQ

### Q1: What concrete problems occur when Domain depends on Application?
**A**: When Domain depends on Application, circular dependencies arise. Domain can no longer be tested independently, and changes to Domain affect Application and vice versa. Ultimately, the two layers become a single monolith, making independent deployment and testing impossible.

### Q2: Why is Ports excluded with `DoNotResideInNamespace`?
**A**: The Ports namespace contains only interfaces. Class rules like `RequirePublic().RequireSealed()` cannot be applied to interfaces, so they are excluded. Port interfaces are verified separately with `ValidateAllInterfaces`.

### Q3: Is it allowed for an Adapter to depend on another Adapter?
**A**: This example does not separately restrict inter-Adapter dependencies. However, depending on project rules, additional rules can be defined to prevent `Adapters.Persistence` from depending on `Adapters.Presentation`.

### Q4: Can ArchUnitNET and Functorium be used in the same test class?
**A**: Yes, both share the same `Architecture` instance. You can use `Types().That()...Check(Architecture)` and `Classes().That()...ValidateAllClasses(Architecture, ...)` together in a single test class.

### Q5: What about when there are more than 3 layers (e.g., separate Infrastructure and Presentation)?
**A**: Extend the same pattern. Add `NotDependOnAnyTypesThat().ResideInNamespace()` rules for each layer pair. As the number of layers increases, it is best to define the dependency matrix first and write tests for each prohibited combination.

---

Layer dependency rules are the most fundamental safeguard of architecture. When dependency direction is verified with ArchUnitNET and internal structure with Functorium simultaneously, architecture diagrams and actual code always remain in sync.

The next chapter covers how to inherit `DomainArchitectureTestSuite` and `ApplicationArchitectureTestSuite` to instantly apply verified rules.

-> [Ch 5: Architecture Test Suites](../05-Architecture-Test-Suites/)
