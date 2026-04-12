---
title: "Functorium Framework Integration"
---
## Overview

Covers learning and practical application of the Functorium framework's value object type hierarchy.

---

## Learning Objectives

- Understand the framework type hierarchy
- How to use `SimpleValueObject<T>`
- How to use `ComparableSimpleValueObject<T>`
- Implementing composite `ValueObject`

---

## Framework Type Hierarchy

```
IValueObject (interface — naming convention constants)
    └── AbstractValueObject (base class — equality, hash code, ORM proxy)
        ├── ValueObject (CreateFromValidation<TVO, TValue> helper)
        │   └── SimpleValueObject<T> (single value wrapper, CreateFromValidation<TVO> helper, protected T Value)
        └── ComparableValueObject (IComparable, comparison operators)
            └── ComparableSimpleValueObject<T> (single comparable value wrapper, protected T Value)
```

---

## How to Run

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/01-Functorium-Framework/FunctoriumFramework
dotnet run
```

---

## Expected Output

```
=== Functorium Framework Integration ===

1. SimpleValueObject<T> Usage Example
────────────────────────────────────────
   Valid email: user@example.com
   Error: Not a valid email format.

2. ComparableSimpleValueObject<T> Usage Example
────────────────────────────────────────
   Before sorting: 30, 25, 35
   After sorting: 25, 30, 35

3. ValueObject (composite) Usage Example
────────────────────────────────────────
   Address: Seoul Gangnam-gu Teheran-ro 123 (06234)

4. Framework Type Hierarchy
────────────────────────────────────────
   ...
```

---

## Core Code Explanation

### SimpleValueObject\<T\>

> The `Value` property is `protected`, so to access the value from outside, use the explicit conversion operator (`explicit operator T`) or define a separate public property.

```csharp
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
{
    protected T Value { get; }

    protected SimpleValueObject(T value) { Value = value; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    // Explicit conversion operator (access value from outside)
    public static explicit operator T(SimpleValueObject<T>? valueObject) => ...;

    // CreateFromValidation helper
    public static Fin<TVO> CreateFromValidation<TVO>(
        Validation<Error, T> validation, Func<T, TVO> factory)
        where TVO : SimpleValueObject<T>;
}
```

### ComparableSimpleValueObject\<T\>

> Inherits from `ComparableValueObject` and is in a separate hierarchy from `SimpleValueObject<T>`.

```csharp
public abstract class ComparableSimpleValueObject<T> : ComparableValueObject
    where T : notnull, IComparable
{
    protected T Value { get; }

    protected ComparableSimpleValueObject(T value) { Value = value; }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }

    // Explicit conversion operator (access value from outside)
    public static explicit operator T(ComparableSimpleValueObject<T>? valueObject) => ...;
}
```

### ValidationRules\<T\> System

```csharp
// Validation starting point where type parameter is specified only once
ValidationRules<Email>.NotNull(value)
    .ThenNotEmpty()
    .ThenNormalize(v => v.Trim().ToLowerInvariant())
    .ThenMaxLength(MaxLength)    // public const int MaxLength = 320;
    .ThenMatches(EmailRegex(), "Invalid email format");

// DomainError.For<T>() pattern
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
```

## FAQ

### Q1: What criteria determine the choice between `SimpleValueObject<T>` and `ComparableSimpleValueObject<T>`?
**A**: Use `ComparableSimpleValueObject<T>` when size comparison or sorting of values is needed, and `SimpleValueObject<T>` when only equality comparison is needed. For example, `Email` does not need sorting so it inherits `SimpleValueObject<string>`, while `Age` needs comparison so it inherits `ComparableSimpleValueObject<int>`.

### Q2: Why is the `Value` property `protected`?
**A**: Because direct access to the internal value from outside can break the encapsulation of the value object. When external access to the value is needed, use the `explicit operator T` conversion operator or define a separate public property appropriate to the domain.

### Q3: Is the `ValidationRules<T>` system mandatory?
**A**: No. You can validate directly with `if` statements and the `DomainError.For<T>()` pattern. `ValidationRules<T>` is a convenience system for when you want to express common validations like `NotNull`, `ThenNotEmpty`, `ThenMaxLength` concisely through chaining.

---

## Next Steps

Learn ORM integration patterns.

-> [4.2 ORM Integration Patterns](../../02-ORM-Integration/OrmIntegration/)
