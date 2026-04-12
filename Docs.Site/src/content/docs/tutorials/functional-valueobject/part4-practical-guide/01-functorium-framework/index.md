---
title: "Functorium Framework Integration"
---
## Overview

In Parts 1-3, we covered value object concepts, validation patterns, and framework types. Now it is time to integrate these value objects into a real application.

Manually implementing `Equals()`, `GetHashCode()`, and comparison operators every time leads to duplicated code and subtle bugs. The Functorium framework provides a base class hierarchy that combines DDD value object patterns with functional programming principles, allowing developers to focus solely on business logic.

## Learning Objectives

- Understand the relationships within the Functorium type hierarchy (IValueObject, AbstractValueObject, ValueObject, SimpleValueObject, ComparableValueObject, etc.).
- Implement domain value objects by inheriting from framework base classes.
- Generate structured error codes using the `DomainError.For<T>()` pattern.
- Implement value object creation patterns by integrating `Fin<T>` with `ValidationRules<T>`.

## Why Is This Needed?

In Parts 1-3, we learned about value object concepts, validation patterns, and various value object types. However, manually implementing these features in every project is inefficient and error-prone.

Implementing common features like value equality, hash code calculation, and comparison operators from scratch every time creates code duplication. By leveraging the framework's base classes, you can eliminate this repetitive work and use consistent patterns across the entire project, improving code predictability and maintainability. Additionally, since Functorium's base classes are battle-tested implementations combining DDD principles with functional programming paradigms, they prevent design-level mistakes.

## Core Concepts

### Framework Type Hierarchy

Functorium provides the following hierarchy of value object base classes.

```
IValueObject (interface - naming convention constants)
    └── AbstractValueObject (base class - equality, hash code, ORM proxy)
        ├── ValueObject (CreateFromValidation<TVO, TValue> helper)
        │   └── SimpleValueObject<T> (single value wrapper, protected T Value)
        └── ComparableValueObject (IComparable, comparison operators)
            └── ComparableSimpleValueObject<T> (single comparable value wrapper, protected T Value)
```

Choose the appropriate base class based on the required functionality. Use `SimpleValueObject<T>` for wrapping a single value, `ComparableSimpleValueObject<T>` when comparison is needed, and `ValueObject` for composite objects with multiple properties.

### SimpleValueObject\<T\>

`SimpleValueObject<T>` is the most basic value object type for wrapping a single value. Since the `Value` property is `protected`, accessing the value externally requires using an explicit conversion operator (`explicit operator T`) or defining a separate public property.

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

    public static explicit operator T(SimpleValueObject<T>? valueObject) => ...;
}
```

Value-based equality comparison is automatically implemented through `GetEqualityComponents()`, allowing developers to focus solely on business logic.

### ComparableSimpleValueObject\<T\>

Value objects that require comparison operations inherit from `ComparableSimpleValueObject<T>`. Since it inherits from `ComparableValueObject`, it is in a separate hierarchy from `SimpleValueObject<T>`.

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

    public static explicit operator T(ComparableSimpleValueObject<T>? valueObject) => ...;
}
```

The generic constraint `where T : notnull, IComparable` ensures only comparable types are allowed, enabling use in sorting and range checking.

### DomainError.For\<T\>() Pattern

Functorium concisely creates structured errors through the `DomainError.For<T>()` helper.

```csharp
using static Functorium.Domains.Errors.DomainErrorType;

DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
// Custom error type definition
public sealed record Unsupported : DomainErrorType.Custom;
DomainError.For<Currency>(new Unsupported(), value, "Currency not supported");
```

Error codes are automatically generated in the `DomainErrors.{TypeName}.{ErrorName}` format, enabling consistent use across logging, internationalization, and API responses.

### ValidationRules\<T\> Chaining System

`ValidationRules<T>` specifies the type parameter once and chains validation rules together.

```csharp
public const int MaxLength = 320;

public static Validation<Error, string> Validate(string? value) =>
    ValidationRules<Email>
        .NotNull(value)
        .ThenNotEmpty()
        .ThenNormalize(v => v.Trim().ToLowerInvariant())
        .ThenMaxLength(MaxLength)
        .ThenMatches(EmailRegex(), "Invalid email format");
```

The `Then*` methods execute sequentially and short-circuit immediately on failure. This allows validation logic to be expressed declaratively.

## Practical Guidelines

### Expected Output
```
=== Functorium Framework Integration ===

1. SimpleValueObject<T> Usage Example
────────────────────────────────────────
   Valid email: user@example.com
   Error: Email.InvalidFormat

2. ComparableSimpleValueObject<T> Usage Example
────────────────────────────────────────
   Before sorting: 30, 25, 35
   After sorting: 25, 30, 35

3. ValueObject (Composite) Usage Example
────────────────────────────────────────
   Address: Seoul Gangnam-gu Teheran-ro 123 (06234)

4. Framework Type Hierarchy
────────────────────────────────────────

   IValueObject (interface - naming convention constants)
       └── AbstractValueObject (base class - equality, hash code, ORM proxy)
           ├── ValueObject (CreateFromValidation<TVO, TValue> helper)
           │   └── SimpleValueObject<T> (single value wrapper, protected T Value)
           └── ComparableValueObject (IComparable, comparison operators)
               └── ComparableSimpleValueObject<T> (single comparable value wrapper, protected T Value)
```

### value object Implementation Pattern

The following is the complete pattern for implementing an Email value object by inheriting from `SimpleValueObject<T>`.

```csharp
using static Functorium.Domains.Errors.DomainErrorType;

// 1. Inherit SimpleValueObject<T>
public sealed class Email : SimpleValueObject<string>
{
    // 2. Declare domain constraints as constants
    public const int MaxLength = 320;

    // 3. Private constructor
    private Email(string value) : base(value) { }

    // 4. Create method returning Fin<T>
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // 5. Validation via ValidationRules<T> chaining
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format");

    // 5. Implicit type conversion (optional)
    public static implicit operator string(Email email) => email.Value;
}
```

## Project Description

### Project Structure
```
01-Functorium-Framework/
├── FunctoriumFramework/
│   ├── Program.cs                  # Main executable
│   └── FunctoriumFramework.csproj  # Project file
└── README.md                       # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### Core Code

> **Note**: The examples below inherit from base classes in the `Functorium.Domains.ValueObjects` namespace.
> Since the `Value` property is declared as `protected`, use `implicit operator` or define a separate public property when external access is needed.

**Email value object (SimpleValueObject)**
```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public string Address => Value;  // Public accessor for protected Value

    public static Fin<Email> Create(string value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant());

    public static implicit operator string(Email email) => email.Value;
}
```

**Age value object (ComparableSimpleValueObject)**
```csharp
public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    public int Id => Value;  // Public accessor for protected Value

    public static Fin<Age> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Age(v));

    public static Age CreateFromValidated(int value) => new(value);

    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotTooOld(value))
            .Map(_ => value);

    public static implicit operator int(Age age) => age.Value;
}
```

**Address value object (ValueObject)**
```csharp
public sealed class Address : ValueObject
{
    public sealed record CityEmpty : DomainErrorType.Custom;
    public sealed record StreetEmpty : DomainErrorType.Custom;
    public sealed record PostalCodeEmpty : DomainErrorType.Custom;

    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private Address(string city, string street, string postalCode)
    {
        City = city; Street = street; PostalCode = postalCode;
    }

    public static Fin<Address> Create(string city, string street, string postalCode) =>
        CreateFromValidation(
            Validate(city, street, postalCode),
            v => new Address(v.City, v.Street, v.PostalCode));

    public static Validation<Error, (string City, string Street, string PostalCode)> Validate(
        string city, string street, string postalCode) =>
        (ValidateCityNotEmpty(city), ValidateStreetNotEmpty(street), ValidatePostalCodeNotEmpty(postalCode))
            .Apply((c, s, p) => (c, s, p));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return PostalCode;
    }

    public override string ToString() => $"{City} {Street} ({PostalCode})";
}
```

## Summary at a Glance

### Base Class Selection Guide

The following table guides which base class to inherit based on value object requirements.

| Base Class | Purpose | Features |
|------------|---------|----------|
| `SimpleValueObject<T>` | Single value wrapping | value equality, automatic hash code |
| `ComparableSimpleValueObject<T>` | Comparable single value | Sorting, range checking support |
| `ValueObject` | Composite value object | Multiple properties, requires `GetEqualityComponents()` implementation |
| `ComparableValueObject` | Comparable composite value | Multiple properties + sorting support |

### Implementation Checklist

Verify the following items in order when implementing a value object.

| Item | Description |
|------|-------------|
| Private constructor | Prevents direct external instantiation |
| `Create()` method | Integrates validation and creation with `Fin<T>` return |
| `Validate()` method | Independent validation with `Validation<Error, T>` return |
| `CreateFromValidated()` method | Creation without validation (for ORM, testing) |
| `DomainError.For<T>()` | Automatic structured error code generation |
| `ValidationRules<T>` | Chaining validation rules |
| Implicit/explicit type conversion | Optionally provides primitive type conversion |

### Benefits of Using the Framework

Compares the differences between manual implementation and framework usage.

| Manual Implementation | Framework Usage |
|----------------------|-----------------|
| Write equality logic every time | Automatically provided via inheritance |
| Possible hash code calculation mistakes | Reuse battle-tested implementations |
| Repeated comparison operator implementation | Leverage generic base classes |
| Different patterns per project | Consistent implementation patterns |

## FAQ

### Q1: When should I use SimpleValueObject\<T\> vs ValueObject?
**A**: Use `SimpleValueObject<T>` for wrapping a single value (Email, UserId, ProductCode, etc.), `ComparableSimpleValueObject<T>` for comparable single values (Age, Money, etc.), and `ValueObject` for cases with multiple properties (Address, ExchangeRate, etc.).

### Q2: Why is the CreateFromValidated() method needed?
**A**: It is used when creating an object from an already validated value. It is useful when an ORM loads data from the database or when quickly creating objects in test code. For user input or external API responses, always use the `Create()` method.

### Q3: When should implicit type conversion (implicit operator) be used?
**A**: It is useful when value objects need to be used naturally like primitive types during string interpolation or API serialization. However, since implicit conversion partially sacrifices type safety, use explicit conversion (`explicit operator`) by default and only use implicit conversion when truly necessary.

---

## Tests

This project includes unit tests.

### Running Tests
```bash
cd FunctoriumFramework.Tests.Unit
dotnet test
```

### Test Structure
```
FunctoriumFramework.Tests.Unit/
├── EmailTests.cs      # SimpleValueObject pattern tests
├── AgeTests.cs        # ComparableSimpleValueObject pattern tests
└── AddressTests.cs    # AbstractValueObject pattern tests
```

### Key Test Cases

| Test Class | Test Content |
|------------|-------------|
| EmailTests | Creation validation, format validation, normalization, equality |
| AgeTests | Range validation, comparison operations, sorting |
| AddressTests | Multi-field validation, composite equality |

The next chapter covers patterns for persisting the value objects implemented here to a database by integrating with Entity Framework Core.

---

→ [Chapter 2: ORM Integration Patterns](../02-ORM-Integration/)
