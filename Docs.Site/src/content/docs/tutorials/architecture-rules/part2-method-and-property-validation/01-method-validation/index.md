---
title: "Method Validation"
---

## Overview

The rule that a factory method must be `public static` is easily missed in code reviews. Even if a new team member changes `Money.Create` to an instance method or declares it `internal`, compilation succeeds and existing tests pass. The problem only surfaces when calling from another assembly. In this chapter, you will learn how to enforce method-level architecture rules through tests, automatically verifying the visibility and static status of **factory methods, instance methods, and extension methods.**

> **"The rule that a factory method must be public static can be enforced through compile-time tests instead of code reviews."**

## Learning Objectives

### Core Learning Goals
1. **Verifying required methods with `RequireMethod`**
   - Confirm that a method with the specified name must exist
   - Simultaneously verify visibility (`Visibility.Public`) and static status

2. **Conditional method verification with `RequireMethodIfExists`**
   - A flexible approach that applies rules only when the method exists
   - Useful when not all classes have the same method

3. **Applying common rules in bulk with `RequireAllMethods`**
   - Select target methods with filter conditions
   - Verify extension method status with `RequireExtensionMethod()`

### What You Will Verify Through Practice
- **Money.Create**: Verify `public static` factory method rule
- **Money.Add**: Verify non-static rule for instance methods
- **MoneyExtensions**: Verify that all regular methods are extension methods

## Domain Code

### Money Class

`Money` is a value object that uses the factory method pattern. `Create` is a static factory method, and `Add` is an instance method.

```csharp
public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
        => new(amount, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new InvalidOperationException("Currency mismatch");
        return new Money(Amount + other.Amount, Currency);
    }
}
```

### MoneyExtensions Extension Methods

```csharp
public static class MoneyExtensions
{
    public static string FormatKrw(this Money money)
        => $"₩{money.Amount:N0}";

    public static Money ApplyDiscount(this Money money, Discount discount)
        => Money.Create(money.Amount * (1 - discount.Percentage / 100), money.Currency);
}
```

## Test Code

### Factory Method Verification

Verify that the `Create` method is `public static` using `RequireMethod`.

```csharp
[Fact]
public void FactoryMethods_ShouldBe_PublicAndStatic()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("MethodValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireVisibility(Visibility.Public)
                .RequireStatic()),
            verbose: true)
        .ThrowIfAnyFailures("Factory Method Rule");
}
```

### Instance Method Verification

Verify that the `Add` method is not static when it exists, using `RequireMethodIfExists`.

```csharp
[Fact]
public void InstanceMethods_ShouldNotBe_Static()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("MethodValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethodIfExists("Add", m => m
                .RequireNotStatic()),
            verbose: true)
        .ThrowIfAnyFailures("Instance Method Rule");
}
```

### Extension Method Verification

Apply a filter to `RequireAllMethods` to select only regular methods, then verify with `RequireExtensionMethod()`.

```csharp
[Fact]
public void ExtensionMethods_ShouldBe_ExtensionMethods()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("MethodValidation.Extensions")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireAllMethods(
                m => !m.Name.StartsWith(".") && m.MethodForm == MethodForm.Normal,
                m => m
                    .RequireStatic()
                    .RequireExtensionMethod()),
            verbose: true)
        .ThrowIfAnyFailures("Extension Method Rule");
}
```

## Summary at a Glance

The following table summarizes the method verification APIs and their purposes.

### Method Verification API Summary
| API | Description |
|-----|-------------|
| `RequireMethod(name, validation)` | The method with the specified name must exist, and validation rules are applied |
| `RequireMethodIfExists(name, validation)` | Validation rules are applied only when the method exists |
| `RequireAllMethods(validation)` | Applies validation rules to all methods |
| `RequireAllMethods(filter, validation)` | Applies validation rules only to methods matching the filter condition |
| `RequireStatic()` / `RequireNotStatic()` | Verifies static/non-static method status |
| `RequireExtensionMethod()` | Verifies extension method status |

### RequireMethod vs RequireMethodIfExists
| Aspect | `RequireMethod` | `RequireMethodIfExists` |
|--------|-----------------|-------------------------|
| **When method is absent** | Fails (method required) | Passes (verification skipped) |
| **When method exists** | Rule verification | Rule verification |
| **Use scenario** | Methods that must exist, like factory methods | Optional methods that exist only in some classes |

## FAQ

### Q1: When should RequireMethod vs RequireMethodIfExists be used?
**A**: `RequireMethod` causes the test to fail if the method does not exist. Use it for methods that must exist in all target classes, like the factory method `Create`. `RequireMethodIfExists` simply passes if the method is absent, making it suitable for verifying rules on methods like `Add` that may exist only in some classes.

### Q2: Why is a filter needed for RequireAllMethods?
**A**: Classes contain methods that are not verification targets, such as constructors (`.ctor`) and compiler-generated methods. Filters like `m => !m.Name.StartsWith(".") && m.MethodForm == MethodForm.Normal` exclude these methods and select only regular methods that developers wrote directly.

### Q3: Can RequireVisibility and RequireStatic be applied simultaneously?
**A**: Yes, method verification works through chaining. Connecting multiple conditions like `m.RequireVisibility(Visibility.Public).RequireStatic()` combines all conditions with AND. If any condition is not met, verification fails.

### Q4: Why use both RequireStatic() and RequireExtensionMethod() in extension method verification?
**A**: In C#, extension methods must be `static`, so `RequireExtensionMethod()` alone implicitly guarantees static status. However, explicitly using `RequireStatic()` together communicates the test's intent more clearly and provides a specific error message about which condition was violated when verification fails.

### Q5: Can methods be verified by signature instead of name?
**A**: `RequireMethod` finds methods by name. When overloads exist, all methods with the same name become verification targets. For fine-grained signature-level verification, you can combine it with return type verification and parameter verification covered in the next chapters.

---

You can now verify method existence and static status. The next chapter covers verifying method **return types** to enforce that factory methods must use functional result types like `Fin<T>`.

-> [Ch 2: Return Type Verification](../02-Return-Type-Validation/)
