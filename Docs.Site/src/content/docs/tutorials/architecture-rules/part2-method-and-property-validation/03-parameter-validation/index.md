---
title: "Parameter Validation"
---

## Overview

`Address.Create(city, street, zipCode)` -- a factory method that takes 3 parameters. What happens if someone removes the `zipCode` parameter during refactoring? Compilation succeeds after fixing the call sites, and existing tests pass after modification. But the object is now created with essential address information missing -- code that deviates from the design intent. In this chapter, you will learn how to verify method parameter counts and types with architecture tests to keep factory method signatures consistent.

> **"A parameter signature is an API contract. Detecting contract changes through tests prevents unintended signature changes from slipping through code review."**

## Learning Objectives

### Core Learning Goals
1. **Exact parameter count verification**
   - Fix factory method signatures with `RequireParameterCount(n)`
   - Tests fail immediately when parameters are added or removed

2. **Minimum parameter count verification**
   - Guarantee a lower bound with `RequireParameterCountAtLeast(n)`
   - Useful when applying a common rule across multiple classes

3. **Parameter type verification**
   - Check the first parameter's type with `RequireFirstParameterTypeContaining`
   - Check whether a parameter of a specific type exists with `RequireAnyParameterTypeContaining`

### What You Will Verify Through Practice
- **Address.Create**: Enforce exactly 3 `string` parameters
- **Coordinate.Create**: Verify existence of `double` type parameters
- Guarantee at least 1 parameter for all factory methods

## Domain Code

### Address Class

Has a factory method that takes 3 string parameters.

```csharp
public sealed class Address
{
    public string City { get; }
    public string Street { get; }
    public string ZipCode { get; }

    private Address(string city, string street, string zipCode)
    {
        City = city;
        Street = street;
        ZipCode = zipCode;
    }

    public static Address Create(string city, string street, string zipCode)
        => new(city, street, zipCode);
}
```

### Coordinate Class

Has a factory method that takes 2 `double` parameters.

```csharp
public sealed class Coordinate
{
    public double Latitude { get; }
    public double Longitude { get; }

    private Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Coordinate Create(double latitude, double longitude)
        => new(latitude, longitude);
}
```

## Test Code

### Exact Parameter Count Verification

```csharp
[Fact]
public void AddressCreate_ShouldHave_ThreeParameters()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Address")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireParameterCount(3)),
            verbose: true)
        .ThrowIfAnyFailures("Address Parameter Count Rule");
}
```

### Minimum Parameter Count Verification

```csharp
[Fact]
public void FactoryMethods_ShouldHave_AtLeastOneParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireParameterCountAtLeast(1)),
            verbose: true)
        .ThrowIfAnyFailures("Factory Method Minimum Parameter Rule");
}
```

### First Parameter Type Verification

```csharp
[Fact]
public void AddressCreate_ShouldHave_StringFirstParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Address")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireFirstParameterTypeContaining("String")),
            verbose: true)
        .ThrowIfAnyFailures("Address First Parameter Type Rule");
}
```

### Specific Type Parameter Existence Verification

```csharp
[Fact]
public void CoordinateCreate_ShouldHave_DoubleParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Coordinate")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireAnyParameterTypeContaining("Double")),
            verbose: true)
        .ThrowIfAnyFailures("Coordinate Double Parameter Rule");
}
```

## Summary at a Glance

The following table summarizes the parameter verification APIs and their verification approaches.

### Parameter Verification API Summary
| API | Verification Target | Use Scenario |
|-----|---------------------|--------------|
| `RequireParameterCount(n)` | Exactly n | Factory methods with fixed signatures |
| `RequireParameterCountAtLeast(n)` | At least n | Applying a common lower bound across multiple classes |
| `RequireFirstParameterTypeContaining(fragment)` | First parameter's type name | When enforcing parameter order |
| `RequireAnyParameterTypeContaining(fragment)` | Any parameter's type name | When only checking if a specific type parameter exists |

### Count Verification vs Type Verification
| Aspect | Count Verification | Type Verification |
|--------|-------------------|-------------------|
| **Strength** | Detects parameter addition/removal | Detects type changes |
| **Flexibility** | Exact count or minimum | String-based matching |
| **Combination** | Can be used standalone | Recommended to use with count verification |

## FAQ

### Q1: How should RequireParameterCount and RequireParameterCountAtLeast be distinguished?
**A**: `RequireParameterCount(3)` passes only when there are exactly 3. It is suitable for methods with confirmed signatures like `Address.Create`. `RequireParameterCountAtLeast(1)` passes as long as there is 1 or more, making it useful for applying a common rule like "parameterless factory methods are not allowed" across multiple classes.

### Q2: What is the difference between RequireFirstParameterTypeContaining and RequireAnyParameterTypeContaining?
**A**: `RequireFirstParameterTypeContaining` inspects only the first parameter, enforcing parameter order. `RequireAnyParameterTypeContaining` passes as long as a parameter of that type exists regardless of position. For example, with `Coordinate.Create(double, double)`, `RequireAnyParameterTypeContaining("Double")` succeeds as long as a `double` parameter exists at any position.

### Q3: Are "String" and "string" different in type name matching?
**A**: Matching is performed against the CLR type's full name (`System.String`, `System.Double`, etc.). You must use part of the CLR type name, not C# keywords (`string`, `double`). Since it is case-sensitive, `"String"` matches but `"string"` does not.

### Q4: Can parameter count verification and type verification be used together?
**A**: Yes, they can be combined through chaining. Writing `m.RequireParameterCount(3).RequireFirstParameterTypeContaining("String")` applies the compound rule "there must be exactly 3 parameters, and the first must be of `String` type".

---

You can now verify parameter signatures. The next chapter covers verifying class **properties and fields** to enforce rules like prohibiting `public setters` and instance fields, ensuring that domain class immutability is not broken.

-> [Ch 4: Property and Field Verification](../04-Property-And-Field-Validation/)
