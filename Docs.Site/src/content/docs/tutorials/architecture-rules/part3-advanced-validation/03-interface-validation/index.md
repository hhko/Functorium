---
title: "Interface Validation"
---

## Overview

If an interface name is missing the `I` prefix, or a Repository interface does not end with `Repository`, or an async method does not return `Task` -- the compiler does not catch these rule violations. Team naming conventions gradually break down when they rely solely on code reviews.

In this chapter, you will learn how to verify interface naming rules and method signatures through **automated tests** using `ValidateAllInterfaces()` and `InterfaceValidator`.

> **"Naming conventions should not be written in documents -- they should be enforced through tests. The code review comment 'please add the I prefix' is now handled by tests."**

## Learning Objectives

### Core Learning Goals

1. **Starting interface verification with `ValidateAllInterfaces()`**
   - Verify interfaces using the same pattern as `ValidateAllClasses()`
   - Provides naming and method verification through `InterfaceValidator`

2. **Naming rule verification with `InterfaceValidator`**
   - `RequireNameStartsWith("I")` -- prefix rule
   - `RequireNameEndsWith("Repository")` -- suffix rule

3. **Return type verification for interface methods**
   - Verify async method signatures with `RequireMethod()` + `RequireReturnTypeContaining()`
   - Understanding how ArchUnitNET represents generic interface names

### What You Will Verify Through Practice
- **IRepository\<T\>**: Generic-based Repository interface
- **IOrderRepository / IProductRepository**: Naming verification of specialized Repository interfaces
- **GetByIdAsync**: `Task` return type verification for async methods

## Domain Code

### IRepository - Base Repository Interface

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task SaveAsync(T entity);
}
```

### IOrderRepository / IProductRepository - Specialized Repositories

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerName);
}

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category);
}
```

All Repository interfaces start with the `I` prefix, end with `Repository`, and async methods return `Task`.

## Test Code

### Interface Naming Rule Verification

`ValidateAllInterfaces()` follows the same pattern as `ValidateAllClasses()`, verifying interfaces through `InterfaceValidator`.

```csharp
[Fact]
public void AllInterfaces_ShouldHave_NameStartingWithI()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllInterfaces(Architecture, iface => iface
            .RequireNameStartsWith("I"),
            verbose: true)
        .ThrowIfAnyFailures("Interface Naming Convention Rule");
}
```

### Repository Interface Name Verification

`HaveNameEndingWith("Repository")` filters only concrete Repository interfaces.
Generic interfaces (`IRepository<T>`) are represented as `` IRepository`1 `` in ArchUnitNET, so they must be handled separately.

```csharp
[Fact]
public void ConcreteRepositoryInterfaces_ShouldHave_NameEndingWithRepository()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameEndingWith("Repository")
        .ValidateAllInterfaces(Architecture, iface => iface
            .RequireNameEndsWith("Repository"),
            verbose: true)
        .ThrowIfAnyFailures("Repository Interface Naming Rule");
}
```

### Base Interface Method Return Type Verification

`RequireMethod()` and `RequireReturnTypeContaining()` can also be used with interface methods.
Since inherited interfaces only have directly declared members, the base interface must be targeted directly for verification.

```csharp
[Fact]
public void BaseRepositoryInterface_ShouldHave_GetByIdAsyncReturningTask()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameStartingWith("IRepository")
        .ValidateAllInterfaces(Architecture, iface => iface
            .RequireMethod("GetByIdAsync", m => m
                .RequireReturnTypeContaining("Task")),
            verbose: true)
        .ThrowIfAnyFailures("Repository GetByIdAsync Rule");
}
```

## Summary at a Glance

The following table organizes the key APIs used for interface verification.

### Interface Verification API Summary

| API | Role | Target |
|-----|------|--------|
| **`ValidateAllInterfaces()`** | Entry point for interface verification | All interfaces |
| **`InterfaceValidator`** | Inherits `TypeValidator` to provide naming and method verification | Inside verification callback |
| **`RequireNameStartsWith("I")`** | Interface `I` prefix rule | Naming convention |
| **`RequireNameEndsWith("Repository")`** | Repository interface suffix rule | Role-specific interfaces |
| **`RequireMethod()` + `RequireReturnTypeContaining()`** | Method signature verification | Async method patterns |

The following table shows how ArchUnitNET represents generic type names.

### ArchUnitNET Generic Name Representation

| C# Code | ArchUnitNET Name | Notes |
|----------|------------------|-------|
| `IRepository<T>` | `` IRepository`1 `` | Generic parameter count shown after backtick |
| `IOrderRepository` | `IOrderRepository` | Non-generic remains as-is |

## FAQ

### Q1: What is the difference between `ValidateAllInterfaces()` and `ValidateAllClasses()`?
**A**: Only the entry point differs. `ValidateAllInterfaces()` starts with `ArchRuleDefinition.Interfaces()` and provides `InterfaceValidator` in the callback. `ValidateAllClasses()` starts with `ArchRuleDefinition.Classes()` and provides `ClassValidator`. The usage pattern is identical.

### Q2: Does `HaveNameEndingWith("Repository")` also include `IRepository<T>` when filtering generic interfaces?
**A**: No. In ArchUnitNET, `IRepository<T>` is represented as `` IRepository`1 ``, so it does not match the `HaveNameEndingWith("Repository")` filter. Only non-generic interfaces like `IOrderRepository` and `IProductRepository` are filtered.

### Q3: Can parent methods be verified on inherited interfaces?
**A**: No. In ArchUnitNET, interfaces only have directly declared members. To verify `GetByIdAsync` on `IOrderRepository`, you must target `IRepository<T>` directly.

### Q4: Can `RequireImmutable()` be applied to interfaces?
**A**: `RequireImmutable()` is a rule that verifies structural immutability of classes, so it does not apply to interfaces. Since interfaces only define method signatures, they are verified with `RequireMethod()` and `RequireReturnTypeContaining()`.

---

By automatically verifying interface naming rules and method signatures, team conventions are maintained consistently even without code reviews. The next chapter examines how to create team-specific rules that the framework does not provide out of the box.

-> [Ch 4: Custom Rules](../04-Custom-Rules/)
