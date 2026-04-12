---
title: "Architecture Rules"
---

## Overview

As a team grows, Specification naming becomes inconsistent. Someone names it `ActiveProductSpec`, while another creates `IsActiveSpecification`. Folder locations are all over the place. This chapter covers naming conventions, folder placement, and automated verification with ArchUnitNET to maintain consistency across the entire team.

## Learning Objectives

1. **Naming conventions** - Consistent naming in the `{Aggregate}{Condition}Spec` format
2. **Folder placement rules** - Placing Specifications in a `Specifications/` folder under the Aggregate
3. **ArchUnitNET verification** - Automated rule verification through architecture tests

## Core Concepts

### Naming Convention: `{Aggregate}{Condition}Spec`

Specification names are composed by combining the target Aggregate and the condition.

| Specification | Aggregate | Condition | Description |
|--------------|-----------|-----------|-------------|
| `ProductInStockSpec` | Product | InStock | Products in stock |
| `ProductPriceRangeSpec` | Product | PriceRange | Products within a price range |
| `ProductLowStockSpec` | Product | LowStock | Products with low stock |

### Folder Placement

```
Domain/AggregateRoots/Products/
├── Product.cs
└── Specifications/
    ├── ProductInStockSpec.cs
    ├── ProductPriceRangeSpec.cs
    └── ProductLowStockSpec.cs
```

### ArchUnitNET Automated Verification

ArchUnitNET is a library that verifies code architecture rules as tests.

```csharp
// Classes in the Specifications namespace must end with Spec
var rule = Classes()
    .That()
    .ResideInNamespace("Specifications", useRegularExpressions: false)
    .Should()
    .HaveNameEndingWith("Spec");

rule.Check(Architecture);
```

## Project Description

### Project Structure

```
ArchitectureRules/
├── Domain/
│   └── AggregateRoots/
│       └── Products/
│           ├── Product.cs
│           └── Specifications/
│               ├── ProductInStockSpec.cs
│               ├── ProductPriceRangeSpec.cs
│               └── ProductLowStockSpec.cs
└── Program.cs

ArchitectureRules.Tests.Unit/
├── SpecificationNamingTests.cs     # ArchUnitNET architecture tests
└── ProductSpecTests.cs             # Spec self-tests
```

## At a Glance

| Rule | Description | Verification Method |
|------|-------------|---------------------|
| **Naming** | `{Aggregate}{Condition}Spec` | ArchUnitNET: `HaveNameEndingWith("Spec")` |
| **Placement** | `Specifications/` namespace | ArchUnitNET: `ResideInNamespace("Specifications")` |
| **Access restriction** | `sealed class` | Code review |
| **Single responsibility** | One Spec = one condition | Code review |

### Importance of Bidirectional Rules

| Rule Direction | Meaning | Problem it prevents |
|----------------|---------|---------------------|
| Specifications -> Spec | Classes in the namespace end with Spec | Unrelated classes placed in the namespace |
| Spec -> Specifications | Classes ending with Spec are in the namespace | Specs placed in wrong locations |

## FAQ

### Q1: What is ArchUnitNET?
**A**: ArchUnitNET is a library that verifies architecture rules of .NET projects as unit tests. Inspired by Java's ArchUnit, it defines rules with a Fluent API and verifies them with `Check()`.

### Q2: Why are architecture tests needed?
**A**: Code review alone cannot guarantee 100% compliance with naming conventions or folder placement rules. Including architecture tests in the CI/CD pipeline automatically detects rule violations, maintaining consistency.

### Q3: Can the same rules be applied to Specifications of other Aggregates?
**A**: Yes, ArchUnitNET rules are applied to the entire assembly. Even if you add `CustomerEmailUniqueSpec` to the `Customer` Aggregate, the same rules are automatically verified.

### Q4: Does Functorium provide built-in architecture rules?
**A**: Yes. By inheriting `DomainArchitectureTestSuite` from `Functorium.Testing`, you get 3 Specification-specific rules out of the box:

| Rule | What it verifies |
|------|-----------------|
| `Specification_ShouldBe_PublicSealed()` | Enforces public sealed class |
| `Specification_ShouldInherit_SpecificationBase()` | Enforces `Specification<T>` inheritance |
| `Specification_ShouldResideIn_DomainLayer()` | Enforces placement in domain namespace |

In real projects, you simply inherit this suite and define only additional rules. The rules written by hand in this chapter are for understanding the principles of ArchUnitNET.

---

In Part 4, we explored real-world usage of the Specification pattern -- CQRS integration, dynamic filter builders, testing strategies, and architecture rules. In Part 5, we'll synthesize everything and apply it to actual domain scenarios.

→ [Part 5, Chapter 1: E-Commerce Product Filtering](../../Part5-Domain-Examples/01-Ecommerce-Product-Filtering/)
