---
title: "Immutability Rules"
---

## Overview

Have you ever left a code review comment saying "why does this Value Object have a public setter?" If even one domain object has a `public set`, immutability breaks, leading to concurrency bugs and unpredictable state changes. But there are too many classes to check by eye every time.

In this chapter, you will learn how to comprehensively verify class immutability across **6 dimensions** using the `RequireImmutable()` method. With a single line of test code, you can automatically ensure immutability across your entire domain.

> **"Blocking setters is just the beginning. Constructors, fields, collection types, state-mutating methods -- true immutability must pass all 6 dimensions."**

## Learning Objectives

### Core Learning Goals

1. **Understanding the 6 verification dimensions of `RequireImmutable()`**
   - Basic writability, constructors, properties, fields, mutable collections, state-mutating methods
   - Why each dimension is needed and what violations it catches

2. **Learning correct immutable class design patterns**
   - Private constructor + factory method pattern
   - Getter-only properties and transformation method patterns

3. **Implementing immutable classes with read-only collections**
   - The difference between `IReadOnlyList<T>` and `List<T>`
   - Why mutable collections violate immutability verification

### What You Will Verify Through Practice
- **Temperature**: Basic immutable class with private constructor, getter-only properties, and factory method
- **Palette**: Immutable class containing collections using `IReadOnlyList<string>`
- **Entire domain verification**: Verify all domain classes at once based on namespace

## Domain Code

### Temperature - Basic Immutable Class

An immutable class using private constructor, getter-only properties, and factory method pattern.

```csharp
public sealed class Temperature
{
    public double Value { get; }
    public string Unit { get; }

    private Temperature(double value, string unit)
    {
        Value = value;
        Unit = unit;
    }

    public static Temperature Create(double value, string unit)
        => new(value, unit);

    public Temperature ToCelsius()
        => Unit == "F" ? Create((Value - 32) * 5 / 9, "C") : this;

    public override string ToString() => $"{Value}°{Unit}";
}
```

The `ToCelsius()` method returns a new `Temperature` instance without modifying the existing object.
This is the core pattern of immutable objects -- instead of changing state, you create an object with new state.

### Palette - Immutable Class with Read-Only Collections

```csharp
public sealed class Palette
{
    public string Name { get; }
    public IReadOnlyList<string> Colors { get; }

    private Palette(string name, IReadOnlyList<string> colors)
    {
        Name = name;
        Colors = colors;
    }

    public static Palette Create(string name, params string[] colors)
        => new(name, colors.ToList().AsReadOnly());
}
```

Using `IReadOnlyList<string>` guarantees collection immutability.
Directly exposing `List<string>` would violate `ImmutabilityRule`'s mutable collection verification.

## Test Code

### The 6 Verification Dimensions of RequireImmutable()

`RequireImmutable()` internally applies `ImmutabilityRule` and verifies class immutability across the following 6 dimensions:

1. **Basic writability verification** - Checks that members are immutable
2. **Constructor verification** - Checks that all constructors are private
3. **Property verification** - Checks that no public setters exist
4. **Field verification** - Checks that no public fields exist
5. **Mutable collection type verification** - Prohibits use of `List<>`, `Dictionary<>`, etc.
6. **State-mutating method verification** - Prohibits methods other than allowed ones (factory, getter, `ToString`, etc.)

### Full Domain Class Immutability Verification

```csharp
[Fact]
public void DomainClasses_ShouldBe_Immutable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireImmutable(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Immutability Rule");
}
```

### Individual Class Verification (Sealed + Immutable)

```csharp
[Fact]
public void Temperature_ShouldBe_SealedAndImmutable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveName("Temperature")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireSealed()
            .RequireImmutable(),
            verbose: true)
        .ThrowIfAnyFailures("Temperature Sealed Immutability Rule");
}
```

Chaining `RequireSealed()` and `RequireImmutable()` verifies a class that is both sealed and immutable.

## Summary at a Glance

The following table summarizes the 6 dimensions verified by `RequireImmutable()`.

### RequireImmutable() Verification Dimension Summary

| Verification Dimension | What It Checks | Violation Example |
|----------------------|----------------|-------------------|
| **Basic Writability** | Checks members are immutable | Writable member exists |
| **Constructors** | Checks all constructors are private | `public Temperature(...)` |
| **Properties** | Checks no public setters exist | `public double Value { get; set; }` |
| **Fields** | Checks no public fields exist | `public double value;` |
| **Mutable Collections** | Prohibits `List<>`, `Dictionary<>`, etc. | `public List<string> Colors { get; }` |
| **State-Mutating Methods** | Prohibits methods outside the allowed list | void methods that modify internal state |

The following table organizes correct immutable class design patterns.

### Immutable Class Design Patterns

| Pattern | Description | Example |
|---------|-------------|---------|
| **Private constructor** | Prevents direct external instantiation | `private Temperature(...)` |
| **Getter-only properties** | Prevents property value modification | `public double Value { get; }` |
| **Factory method** | Creates instances via static `Create` method | `Temperature.Create(36.5, "C")` |
| **`IReadOnlyList<T>`** | Uses read-only instead of mutable collections | `IReadOnlyList<string> Colors` |
| **Transformation methods** | Returns new instances without modifying existing objects | `ToCelsius()` -> new Temperature |

## FAQ

### Q1: How does `RequireImmutable()` differ from `RequireNoPublicSetters()`?
**A**: `RequireNoPublicSetters()` only checks for public setters on properties. `RequireImmutable()` is much more comprehensive, verifying all 6 dimensions including constructor accessibility, fields, mutable collection types, and state-mutating methods. It guarantees "true immutability" rather than simply blocking setters.

### Q2: Do record types pass `RequireImmutable()` verification?
**A**: `record` types generate `init`-only properties by default, so they pass at the property dimension. However, they have public constructors, so they may violate constructor verification. When using records, combining `RequireRecord()` and `RequireSealed()` is more appropriate.

### Q3: Is it a violation to use `List<T>` only as a private field without exposing it externally?
**A**: `RequireImmutable()` checks for the existence of mutable collections at the type level. Even if it is a private field, having a `List<T>` type is reported as a violation. Using `IReadOnlyList<T>` or immutable collections for internal storage is recommended.

### Q4: Why are transformation methods like `ToCelsius()` allowed?
**A**: `RequireImmutable()`'s state-mutating method verification works on an allow list basis (factory methods, getters, `ToString`, `Equals`, `GetHashCode`, etc.). Methods whose return type is themselves (`Temperature`) are considered transformation methods that return new instances and are allowed.

---

Immutability is the most fundamental safeguard for domain objects. The next chapter goes a step further and examines how to verify the existence and structure of nested classes in Command/Query patterns.

-> [Ch 2: Nested Class Verification](../02-Nested-Class-Validation/)
