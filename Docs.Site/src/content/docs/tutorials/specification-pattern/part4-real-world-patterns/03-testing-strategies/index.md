---
title: "Testing Strategies"
---

## Overview

How can we ensure that Specifications express the correct business rules? Is a single unit test enough, or do we need to verify compositions and Repository integration as well? This chapter systematically covers three testing levels for Specifications -- individual Spec, composition, and Usecase integration.

## Learning Objectives

1. **Level 1: Spec self-testing** - Boundary value testing of individual Specification's `IsSatisfiedBy()`
2. **Level 2: Composition testing** - Verifying correct behavior of `And`, `Or`, `Not` compositions
3. **Level 3: Usecase testing** - Verifying Specification integration through Mock Repository

## Core Concepts

### 3-Level Test Pyramid

```
         /  Level 3  \       Usecase tests (integration)
        / ----------- \      Verify Specs are correctly used via Mock Repository
       /   Level 2     \     Composition tests (And/Or/Not)
      / --------------- \    Verify correct behavior of composite conditions
     /     Level 1       \   Spec self-tests (boundary values)
    / ------------------- \  Verify satisfaction/non-satisfaction boundaries of IsSatisfiedBy()
```

### Level 1: Spec Self-Testing

Verify boundary values using `Theory` + `InlineData`.

```csharp
[Theory]
[InlineData(0, false)]     // Boundary: stock 0
[InlineData(1, true)]      // Boundary: stock 1
[InlineData(100, true)]    // Normal case
public void ProductInStockSpec_ShouldReturnExpected_WhenStockIs(int stock, bool expected)
{
    var product = new Product("Test", 1000, stock, "Test");
    var spec = new ProductInStockSpec();
    spec.IsSatisfiedBy(product).ShouldBe(expected);
}
```

### Level 2: Composition Testing

Verify `And`, `Or`, `Not` compositions with real data.

```csharp
var spec = new ProductCategorySpec("Electronics") & new ProductInStockSpec();
spec.IsSatisfiedBy(inStockElectronics).ShouldBeTrue();
spec.IsSatisfiedBy(outOfStockElectronics).ShouldBeFalse();
```

### Level 3: Usecase Testing

Use a Mock Repository to verify that Specifications are correctly passed within Usecases.

```csharp
public class MockProductRepository : IProductRepository
{
    public Specification<Product>? LastSpec { get; private set; }
    private readonly List<Product> _products;
    // ...
}
```

## Project Description

### Project Structure

```
TestingStrategies/
├── Product.cs
├── IProductRepository.cs
├── Specifications/
│   ├── ProductInStockSpec.cs
│   ├── ProductPriceRangeSpec.cs
│   ├── ProductCategorySpec.cs
│   └── ProductNameUniqueSpec.cs
└── Program.cs

TestingStrategies.Tests.Unit/
├── Level1_SpecSelfTests.cs         # Spec boundary value tests
├── Level2_CompositionTests.cs      # And/Or/Not composition tests
└── Level3_UsecaseTests.cs          # Mock Repository integration tests
```

## At a Glance

A summary of what each test level verifies and the techniques used.

| Level | Target | What it verifies | Technique |
|-------|--------|-----------------|-----------|
| **Level 1** | Individual Spec | `IsSatisfiedBy()` boundary values | `Theory` + `InlineData` |
| **Level 2** | Spec composition | `And`, `Or`, `Not` behavior | Real data + operators |
| **Level 3** | Usecase | Spec correctly passed to Repository | Mock Repository |

## FAQ

### Q1: Do I need to write all 3 levels?
**A**: Level 1 is mandatory. Boundary value tests should be written for all Specifications. Level 2 is needed when there are complex compositions, and Level 3 when Usecases use Specifications.

### Q2: Can I use a mocking framework like NSubstitute for Level 3?
**A**: Yes, if you're already using a mocking framework in your project. In this example, we implement Mock classes directly without external dependencies to clearly demonstrate the pattern.

### Q3: What is the most common mistake in Specification testing?
**A**: Missing boundary values. For example, if you don't test the exact values 1000 and 10000 for `ProductPriceRangeSpec(1000, 10000)`, you may miss bugs caused by the difference between `>=` and `>`.

---

We've ensured Specification correctness through testing. The next chapter covers architecture rules that use ArchUnitNET to automatically verify Specification class naming, folder placement, and inheritance rules.

→ [Chapter 4: Architecture Rules](../04-Architecture-Rules/)
