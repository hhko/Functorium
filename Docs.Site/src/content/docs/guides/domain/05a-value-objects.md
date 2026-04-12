---
title: "Value Objects"
---

This document covers the design and implementation of Value Objects that express domain concepts as types, going beyond the limitations of primitive types. For enumeration patterns, Application validation, and FAQ, see [05b-value-objects-validation](./05b-value-objects-validation). For Union types (Discriminated Unions), see [05c-union-value-objects](./05c-union-value-objects).

## Introduction

"Why is a product with a negative price being created?"
"Even if a customer name is passed to a `string email` parameter, the compiler does not catch it."
"The same validation logic exists in three places: the controller, the service, and the repository."

These problems repeatedly occur when expressing domain concepts with primitive types (string, decimal, int). Value Objects fundamentally solve these problems by giving domain concepts a name, rules, and immutability.

### What You Will Learn

Through this document, you will learn:

1. **Why to use Value Objects instead of primitive types** - The Primitive Obsession problem and its solution
2. **Base class selection criteria** - Usage scenarios for `SimpleValueObject<T>`, `ValueObject`, `ComparableSimpleValueObject<T>`, etc.
3. **Create/Validate separation pattern** - Core design for reusing validation logic
4. **Overall validation system structure** - Sequential validation (Bind), parallel validation (Apply), and three validation approaches
5. **FluentValidation integration** - Reusing Domain Layer validation logic in the Application Layer

### Prerequisites

A basic understanding of the following concepts is required to understand this document:

- The complete building block map from the [DDD Tactical Design Overview](./04-ddd-tactical-overview)
- C# generics and the static factory method pattern
- Basic concepts of LanguageExt's `Fin<T>` and `Validation<Error, T>`

> In DDD tactical design, Value Objects are **the most fundamental building block for explicitly expressing domain concepts.** By using Value Objects instead of primitive types, you can ensure type safety at compile time and eliminate duplication by encapsulating validation logic in a single place.

## Summary

### Key Commands

```csharp
// Value Object creation (with validation)
Fin<Email> email = Email.Create("user@example.com");

// Validation only (no object creation)
Validation<Error, string> result = Email.Validate("user@example.com");

// Validation chaining
ValidationRules<Email>.NotEmpty(value).ThenMatches(pattern).ThenMaxLength(254);

// FluentValidation integration
RuleFor(x => x.Price).MustSatisfyValidation(Money.ValidateAmount);
```

### Key Procedures

1. Select a base class (`SimpleValueObject<T>`, `ComparableSimpleValueObject<T>`, `ValueObject`)
2. Implement `Validate()` method - Define validation logic with `ValidationRules<T>`, return `Validation<Error, T>`
3. Implement `Create()` method - Call `CreateFromValidation(Validate(value), factory)`, return `Fin<T>`
4. Optionally reuse validation in FluentValidation via `MustSatisfyValidation`

### Key Concepts

| Concept | Description |
|------|------|
| Create/Validate separation | `Validate` performs validation only; `Create` performs validation + object creation. Allows reuse of validation logic |
| Sequential validation (Bind) | Stops at the first error. Used for validations with dependencies |
| Parallel validation (Apply) | Collects all errors. Used for independent validations |
| Three validation approaches | Typed (`ValidationRules<T>`), Context Class (`IValidationContext`), Named Context (`ValidationRules.For()`) |
| Result types | `Fin<T>` (single error), `Validation<Error, T>` (error accumulation) |

---

## Why Value Objects

In DDD tactical design, Value Objects are **the most fundamental building block for explicitly expressing domain concepts.**

### Preventing Primitive Obsession

When only primitive types (string, int, decimal) are used, domain knowledge is not visible in the code. Value Objects give domain concepts a name and rules.

The following table shows the correspondence between primitive types and Value Objects. The key difference is that Value Objects guarantee type safety at compile time.

| Primitive Type | Value Object | Effect |
|----------|--------|------|
| `string email` | `Email email` | Compile-time type safety |
| `decimal price` | `Price price` | Negative values prohibited, maximum limit automatically enforced |
| `string currency` | `Currency currency` | Only supported currencies allowed |

### Make Illegal States Unrepresentable

Value Objects validate at creation time, ensuring that invalid values cannot exist in the system. Once created, a Value Object is always valid.

### Eliminating Side Effects Through Immutability

Value Objects cannot be changed after creation, making them thread-safe and predictable. When a value needs to change, a new Value Object is created.

### Effects of Value Objects in Practice

Comparing before and after introducing Value Objects reveals a clear difference in code safety and intent communication.

**Before**: With the `ProcessOrder(string email, decimal price, string currency)` signature, the compiler cannot catch parameter order mistakes. Negative prices go unnoticed at the call site, and validation logic is scattered across multiple layers.

**After**: With the `ProcessOrder(Email email, Money price)` signature, the types themselves guarantee validity. `Email.Create("invalid")` returns a failure, and `Money.Create(-100)` only allows positive values, so invalid values cannot enter the system.

### Decision Criteria: When to Create a Value Object

- Values with special meaning in the domain (email, price, quantity)
- Values that require validation
- Values used with the same rules in multiple places
- When two or more primitive values form a combined meaning (amount + currency -> Money)

We have covered why Value Objects are needed and the criteria for deciding when to create them. The next section covers the core characteristics of Value Objects and how to select a base class.

---

## Overview

Value Object is one of the core tactical patterns in Domain-Driven Design (DDD). It **expresses domain concepts such as "email address", "price", and "amount" as dedicated types instead of primitive types (string, decimal).**

### Why Use Value Objects?

Using only primitive types leads to the following problems:

```csharp
// Problem 1: Meaning is unclear
public void ProcessOrder(string email, decimal price, string currency);

// Problem 2: Invalid values can be passed (no compile error)
ProcessOrder(currency, price, email);  // Order mistake - only discovered at runtime

// Problem 3: Invalid values spread throughout the system
var email = "not-an-email";  // Any string can be used as an email
```

Value Objects solve these problems:

```csharp
// Solution: Express meaning through types
public void ProcessOrder(Email email, Price price, Currency currency);

// Prevent mistakes with compile errors
ProcessOrder(currency, price, email);  // Compile error!

// Validation at creation time
var email = Email.Create("not-an-email");  // Fin<Email> - returns failure result
```

### Core Characteristics

| Characteristics | Description |
|------|------|
| **Immutability** | Cannot be changed after creation. Thread-safe with no side effects |
| **Value-based equality** | Same object if property values are equal. Compared by content, not reference |
| **Self-validation** | Validates at creation time. Objects in invalid states cannot exist |
| **Domain logic encapsulation** | Includes related operations (comparison, conversion, calculation) within the type |

### Base Classes Optional

| Usage Scenario | Base Class | Features |
|--------------|------------|------|
| Composite properties | `ValueObject` | Equality determined by multiple properties |
| Single value wrapping | `SimpleValueObject<T>` | Equality determined by single value |
| Composite properties + comparison | `ComparableValueObject` | Supports sorting and comparison operations |
| Single value + comparison | `ComparableSimpleValueObject<T>` | Supports sorting and comparison operations |
| Type-safe enumeration | `SmartEnum<T, TValue>` (Ardalis.SmartEnum) | Enumeration with built-in domain logic. Requires manual IValueObject implementation |

### Core Pattern

The key point in the following code is that `Create()` and `Validate()` are separated. Since `Validate()` returns primitive types, it can be reused in FluentValidation, and `Create()` takes the result of `Validate()` to create the object.

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using static Functorium.Domains.Errors.DomainErrorType;

public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$");

    private Email(string value) : base(value) { }

    // Create: Uses CreateFromValidation helper
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // Validate: Returns primitive type, type parameter specified only once
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenNormalize(v => v.ToLowerInvariant())
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254);

    public static implicit operator string(Email email) => email.Value;
}
```

In the overview, we confirmed the core characteristics of Value Objects and the base class selection guide. The next section examines the class hierarchy provided by Functorium in detail.

---

## Class Hierarchy

Functorium provides a base class hierarchy for various Value Object types. Each class is optimized for specific scenarios and inherits the functionality of its parent class.

```
IValueObject (interface)
|
AbstractValueObject (abstract class)
+-- GetEqualityComponents() - equality components
+-- Equals() / GetHashCode() - value-based equality
+-- == / != operators
`-- proxy type handling (ORM support)
    |
    `-- ValueObject
        +-- CreateFromValidation<TValueObject, TValue>() helper
        |
        +-- SimpleValueObject<T>
        |   +-- protected T Value
        |   +-- CreateFromValidation<TValueObject>() helper
        |   `-- explicit operator T
        |
        `-- ComparableValueObject
            +-- GetComparableEqualityComponents()
            +-- IComparable<ComparableValueObject>
            +-- < / <= / > / >= operators
            |
            `-- ComparableSimpleValueObject<T>
                +-- protected T Value
                +-- CreateFromValidation<TValueObject>() helper
                `-- explicit operator T
```

**Understanding the Hierarchy:**

- **IValueObject**: Marker interface implemented by all Value Objects. SmartEnum does not automatically implement IValueObject, so SmartEnum-based Value Objects must explicitly implement IValueObject.
- **AbstractValueObject**: Automatically implements equality comparison (`Equals`, `GetHashCode`, `==`, `!=`). Also handles ORM proxy types.
- **ValueObject**: Base for composite properties Value Objects. Provides the `CreateFromValidation` helper method.
- **SimpleValueObject\<T\>**: For single value wrapping. `GetEqualityComponents()` is automatically implemented.
- **ComparableValueObject / ComparableSimpleValueObject\<T\>**: Supports comparison operators (`<`, `>`, `<=`, `>=`) and sorting.

---

## Base Classes

The first thing to do when implementing a Value Object is to decide **which base class to inherit from.** You can easily decide with the following two questions:

**Question 1: How many values is it composed of?**
- Single value -> `SimpleValueObject<T>` family (email, price, ID, etc.)
- Multiple properties -> `ValueObject` family (amount + currency, address, coordinates, etc.)

**Question 2: Is size comparison/sorting needed?**
- Not needed -> Base classes (`SimpleValueObject<T>`, `ValueObject`)
- Needed -> Comparable classes (`ComparableSimpleValueObject<T>`, `ComparableValueObject`)

```
Is it a single value?
    |
    +-- Yes --> Is comparison/sorting needed?
    |              |
    |              +-- Yes --> ComparableSimpleValueObject<T>
    |              |
    |              `-- No --> SimpleValueObject<T>
    |
    `-- No --> Is comparison/sorting needed?
                       |
                       +-- Yes --> ComparableValueObject
                       |
                       `-- No --> ValueObject
```

### ValueObject

Base class for Value Objects composed of composite properties. Used when a combination of multiple properties represents a single concept (e.g., amount + currency = Money).

**Location**: `Functorium.Domains.ValueObjects.ValueObject`

```csharp
public abstract class ValueObject : AbstractValueObject
{
    // Factory helper method
    public static Fin<TValueObject> CreateFromValidation<TValueObject, TValue>(
        Validation<Error, TValue> validation,
        Func<TValue, TValueObject> factory)
        where TValueObject : ValueObject;
}
```

**Required Implementation:**

| Item | Description |
|------|------|
| `GetEqualityComponents()` | Returns equality comparison components |
| Private constructor | Prevents external construction |
| `Create()` / `Validate()` | Factory and validation methods |

**Example:**

The key point in the following code is that `GetEqualityComponents()` returns both `Amount` and `Currency`, so equality is determined by the combination of both properties.

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    // Create: Uses CreateFromValidation helper
    public static Fin<Money> Create(decimal amount, string currency) =>
        CreateFromValidation(Validate(amount, currency), v => new Money(v.Amount, v.Currency));

    // Validate: Returns validated primitive value tuple (ValueObject creation happens in Create)
    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => (Amount: a, Currency: c));
}
```

### SimpleValueObject\<T\>

Base class for Value Objects that wrap a single value. **This is the most commonly used base class,** used when giving domain meaning to a single primitive type. `GetEqualityComponents()` is automatically implemented to return `Value`.

**Location**: `Functorium.Domains.ValueObjects.SimpleValueObject<T>`

```csharp
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
{
    protected T Value { get; }

    protected SimpleValueObject(T value);

    // Factory helper method
    public static Fin<TValueObject> CreateFromValidation<TValueObject>(
        Validation<Error, T> validation,
        Func<T, TValueObject> factory)
        where TValueObject : SimpleValueObject<T>;

    // Explicit conversion
    public static explicit operator T(SimpleValueObject<T>? valueObject);
}
```

**Characteristics:**
- The `Value` property is `protected` - not directly accessible from outside
- `GetEqualityComponents()` is automatically implemented (returns `Value`)
- Provides an explicit conversion operator

**Example:**

```csharp
public sealed class ProductName : SimpleValueObject<string>
{
    private ProductName(string value) : base(value) { }

    public static Fin<ProductName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ProductName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ProductName>.NotEmpty(value ?? "")
            .ThenMaxLength(100);

    // Implicit conversion (optional)
    public static implicit operator string(ProductName name) => name.Value;
}
```

### ComparableValueObject

Base class for comparable composite Value Objects. Used when composite properties need sorting or size comparison (e.g., comparing the duration of a DateRange composed of start and end dates).

**Location**: `Functorium.Domains.ValueObjects.ComparableValueObject`

```csharp
public abstract class ComparableValueObject : ValueObject, IComparable<ComparableValueObject>
{
    protected abstract IEnumerable<IComparable> GetComparableEqualityComponents();

    public virtual int CompareTo(ComparableValueObject? other);

    // Comparison operators
    public static bool operator <(ComparableValueObject? left, ComparableValueObject? right);
    public static bool operator <=(ComparableValueObject? left, ComparableValueObject? right);
    public static bool operator >(ComparableValueObject? left, ComparableValueObject? right);
    public static bool operator >=(ComparableValueObject? left, ComparableValueObject? right);
}
```

**Required Implementation:**
- `GetComparableEqualityComponents()` - Must only return types implementing `IComparable`

### ComparableSimpleValueObject\<T\>

Base class for comparable single-value Value Objects. Used when a single value has meaningful "greater than/less than" comparisons (e.g., price comparison, quantity sorting, age range validation).

**Location**: `Functorium.Domains.ValueObjects.ComparableSimpleValueObject<T>`

```csharp
public abstract class ComparableSimpleValueObject<T> : ComparableValueObject
    where T : notnull, IComparable
{
    protected T Value { get; }

    protected ComparableSimpleValueObject(T value);

    public static Fin<TValueObject> CreateFromValidation<TValueObject>(
        Validation<Error, T> validation,
        Func<T, TValueObject> factory)
        where TValueObject : ComparableSimpleValueObject<T>;
}
```

**Example:**

```csharp
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    private Price(decimal value) : base(value) { }

    public static Fin<Price> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Price(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Price>.Positive(value)
            .ThenAtMost(1_000_000);

    public static implicit operator decimal(Price price) => price.Value;
}
```

We have covered base class selection and implementation methods. The next section covers the Self-Validation system, which is the core of Value Objects, in detail.

---

## Validation System

This system implements the **Self-Validation** principle of Value Objects. All Value Objects validate at creation time, ensuring that **objects in an invalid state cannot exist.**

### Core Concepts of Validation

The Functorium validation system follows the **Railway Oriented Programming** pattern. Validation proceeds along two tracks (success/failure):

```
Input ──┬── [Validate 1] ──┬── [Validate 2] ──┬── [Validate 3] ──┬── Success (Valid Value)
        │                  │                  │                  │
        └── Fail ──────────┴── Fail ──────────┴── Fail ──────────┴── Fail (Error)
```

**Sequential Validation (Bind/Then)**: The next validation runs only if the previous one passes. Suitable for validations with dependencies.
```csharp
ValidationRules<Email>.NotEmpty(value)    // 1. First check if empty
    .ThenMatches(EmailPattern)            // 2. Pattern validation requires non-empty
    .ThenMaxLength(254);                  // 3. Length validation is meaningful only with correct format
```

**Parallel Validation (Apply)**: Runs all validations independently and collects all errors. Suitable for independent field validations.
```csharp
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => (a, c));  // Both validations run, all errors collected
```

### Namespace Structure

The validation system is organized into three namespaces:

| Namespace | Purpose | Key Classes |
|-------------|------|------------|
| `Functorium.Domains.ValueObjects.Validations` | Common infrastructure | `ValidationApplyExtensions`, `IValidationContext` |
| `Functorium.Domains.ValueObjects.Validations.Typed` | Value Object / Context Class validation | `ValidationRules<T>`, `TypedValidation<T,V>`, `TypedValidationExtensions` |
| `Functorium.Domains.ValueObjects.Validations.Contextual` | Named Context validation | `ValidationRules.For()`, `ValidationContext`, `ContextualValidation<T>` |

**using statement guide:**
- Sequential validation only: Only `Validations.Typed` namespace needed
- Apply pattern: Both `Validations` + `Validations.Typed` needed
- Context Class: Both `Validations` (IValidationContext) + `Validations.Typed` needed
- Named Context validation: `Validations.Contextual` namespace used

```csharp
// When using sequential validation only (Value Object)
using Functorium.Domains.ValueObjects.Validations.Typed;

// When using Apply pattern (parallel validation)
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;

// Named Context validation (DTO, API input, etc.)
using Functorium.Domains.ValueObjects.Validations.Contextual;
```

**Recommended usage by DDD layer:**

| Layer | Recommended Approach | Example |
|--------|----------|------|
| Domain Layer | Value Object (Typed) | `ValidationRules<Price>.Positive(amount)` |
| Application Layer | Context Class (IValidationContext) | `ValidationRules<ProductValidation>.NotEmpty(name)` |
| Presentation Layer | Named Context (Contextual) | `ValidationRules.For("ProductName").NotEmpty(name)` |

> **Context Class** is an empty class that implements `IValidationContext`. It is used when reusing validation contexts in the Application Layer. See the [IValidationContext-based Validation](#ivalidationcontext를-이용한-검증-context-class) section for details.

### Validation Category Summary

The validation classes (DomainErrorType, ValidationRules, TypedValidationExtensions) follow a consistent category structure:

| DomainErrorType | ValidationRules | TypedValidationExtensions |
|-----------------|-----------------|---------------------------|
| Presence | Presence | Presence |
| Length | Length | Length |
| Format | Format | Format |
| DateTime | DateTime | DateTime |
| Numeric | Numeric | Numeric |
| Range | Range | Range |
| Existence | (use Must) | (use ThenMust) |
| Custom | Custom | Generic |
| - | Collection | Collection |

#### Methods and ErrorType by Category

| Category | Method | ErrorType | Description |
|------|--------|-----------|------|
| **Presence** | `NotNull` | `Null` | Null validation |
| **Length** | `NotEmpty`, `MinLength`, `MaxLength`, `ExactLength` | `Empty`, `TooShort`, `TooLong`, `WrongLength` | String/collection length validation |
| **Format** | `Matches`, `IsUpperCase`, `IsLowerCase` | `InvalidFormat`, `NotUpperCase`, `NotLowerCase` | Format and case validation |
| **DateTime** | `NotDefault`, `InPast`, `InFuture`, `Before`, `After`, `DateBetween` | `DefaultDate`, `NotInPast`, `NotInFuture`, `TooLate`, `TooEarly`, `OutOfRange` | Date validation |
| **Numeric** | `Positive`, `NonNegative`, `NotZero`, `Between`, `AtMost`, `AtLeast` | `NotPositive`, `Negative`, `Zero`, `OutOfRange`, `AboveMaximum`, `BelowMinimum` | Numeric value/range validation |
| **Range** | `ValidRange`, `ValidStrictRange` | `RangeInverted`, `RangeEmpty` | Min/max pair validation |
| **Collection** | `NotEmptyArray` | `Empty` | Array validation |
| **Custom** | `Must`, `ThenMust` | `Custom` (abstract record, user-defined derived) | User-defined validation |

### ValidationRules\<T\> Entry Point

**Location**: `Functorium.Domains.ValueObjects.Validations.Typed.ValidationRules<TValueObject>`

Once the type parameter is specified, it does not need to be repeated during chaining.

#### Presence Validation Methods

```csharp
ValidationRules<User>.NotNull(value)                // Not null (reference type)
ValidationRules<User>.NotNull(nullableValue)        // Not null (nullable value type)
```

| Method | ErrorType | Error Message |
|--------|-----------|------------|
| `NotNull` | `Null` | `{Type} cannot be null.` |

#### Length Validation Methods

```csharp
ValidationRules<Email>.NotEmpty(value)              // Not empty
ValidationRules<Email>.MinLength(value, 8)          // Minimum length
ValidationRules<Email>.MaxLength(value, 100)        // Maximum length
ValidationRules<Email>.ExactLength(value, 10)       // Exact length
```

| Method | ErrorType | Error Message |
|--------|-----------|------------|
| `NotEmpty` | `Empty` | `{Type} cannot be empty. Current value: '{v}'` |
| `MinLength` | `TooShort(n)` | `{Type} must be at least {n} characters. Current length: {len}` |
| `MaxLength` | `TooLong(n)` | `{Type} must not exceed {n} characters. Current length: {len}` |
| `ExactLength` | `WrongLength(n)` | `{Type} must be exactly {n} characters. Current length: {len}` |

#### Format Validation Methods

```csharp
ValidationRules<Email>.Matches(value, regex)        // Regex pattern
ValidationRules<Email>.Matches(value, regex, msg)   // Regex + custom message
ValidationRules<Code>.IsUpperCase(value)            // Uppercase validation
ValidationRules<Code>.IsLowerCase(value)            // Lowercase validation
```

| Method | ErrorType | Error Message |
|--------|-----------|------------|
| `Matches` | `InvalidFormat(pattern)` | `Invalid {Type} format. Current value: '{v}'` |
| `IsUpperCase` | `NotUpperCase` | `{Type} must be uppercase. Current value: '{v}'` |
| `IsLowerCase` | `NotLowerCase` | `{Type} must be lowercase. Current value: '{v}'` |

#### Numeric Validation Methods

Works with all numeric types (int, decimal, double, etc.) through the `INumber<T>` constraint:

```csharp
ValidationRules<Price>.Positive(value)              // > 0
ValidationRules<Age>.NonNegative(value)             // >= 0
ValidationRules<Denominator>.NotZero(value)         // != 0
ValidationRules<Age>.Between(value, 0, 150)         // min <= value <= max
ValidationRules<Age>.AtMost(value, 150)             // <= max
ValidationRules<Age>.AtLeast(value, 0)              // >= min
```

| Method | ErrorType | Error Message |
|--------|-----------|------------|
| `Positive` | `NotPositive` | `{Type} must be positive. Current value: '{v}'` |
| `NonNegative` | `Negative` | `{Type} cannot be negative. Current value: '{v}'` |
| `NotZero` | `Zero` | `{Type} cannot be zero. Current value: '{v}'` |
| `Between` | `OutOfRange(min, max)` | `{Type} must be between {min} and {max}. Current value: '{v}'` |
| `AtMost` | `AboveMaximum(max)` | `{Type} cannot exceed {max}. Current value: '{v}'` |
| `AtLeast` | `BelowMinimum(min)` | `{Type} must be at least {min}. Current value: '{v}'` |

#### Collection Validation Methods

```csharp
ValidationRules<BinaryData>.NotEmptyArray(value)    // Array is not null and length > 0
```

| Method | ErrorType | Error Message |
|--------|-----------|------------|
| `NotEmptyArray` | `Empty` | `{Type} array cannot be empty or null. Current length: '{len}'` |

#### Range Validation Methods

```csharp
ValidationRules<PriceRange>.ValidRange(minValue, maxValue)        // Validates min <= max, returns (min, max) tuple
ValidationRules<DateRange>.ValidStrictRange(minValue, maxValue)   // Validates min < max, returns (min, max) tuple
```

| Method | ErrorType | Error Message |
|--------|-----------|------------|
| `ValidRange` | `RangeInverted(min, max)` | `{Type} range is invalid. Minimum ({min}) cannot exceed maximum ({max}).` |
| `ValidStrictRange` | `RangeInverted(min, max)` | `{Type} range is invalid. Minimum ({min}) cannot exceed maximum ({max}).` |
| `ValidStrictRange` | `RangeEmpty(value)` | `{Type} range is empty. Start ({value}) equals end ({value}).` |

#### DateTime Validation Methods

```csharp
ValidationRules<Birthday>.NotDefault(value)         // != DateTime.MinValue
ValidationRules<Birthday>.InPast(value)             // < DateTime.Now
ValidationRules<ExpiryDate>.InFuture(value)         // > DateTime.Now
ValidationRules<EndDate>.Before(value, boundary)    // < boundary
ValidationRules<StartDate>.After(value, boundary)   // > boundary
ValidationRules<EventDate>.DateBetween(value, min, max)  // min <= value <= max
```

| Method | ErrorType | Error Message |
|--------|-----------|------------|
| `NotDefault` | `DefaultDate` | `{Type} date cannot be default. Current value: '{v}'` |
| `InPast` | `NotInPast` | `{Type} must be in the past. Current value: '{v}'` |
| `InFuture` | `NotInFuture` | `{Type} must be in the future. Current value: '{v}'` |
| `Before` | `TooLate(boundary)` | `{Type} must be before {boundary}. Current value: '{v}'` |
| `After` | `TooEarly(boundary)` | `{Type} must be after {boundary}. Current value: '{v}'` |
| `DateBetween` | `OutOfRange(min, max)` | `{Type} must be between {min} and {max}. Current value: '{v}'` |

#### Custom Validation Methods

```csharp
// Error type definition: public sealed record Unsupported : DomainErrorType.Custom;
ValidationRules<Currency>.Must(
    value,
    v => SupportedCurrencies.Contains(v),
    new Unsupported(),
    $"Currency '{value}' is not supported")
```

### TypedValidation Chaining

**Location**: `Functorium.Domains.ValueObjects.Validations.Typed.TypedValidationExtensions`

Chaining methods for `TypedValidation<TValueObject, T>` returned by `ValidationRules<T>`.

#### Presence Chaining

| Method | Description |
|--------|------|
| `ThenNotNull()` | Validates not null |

#### Length Chaining

| Method | Description |
|--------|------|
| `ThenNotEmpty()` | Validates not empty |
| `ThenMinLength(n)` | Minimum length validation |
| `ThenMaxLength(n)` | Maximum length validation |
| `ThenExactLength(n)` | Exact length validation |
| `ThenNormalize(func)` | Value transformation (Map) |

#### Format Chaining

| Method | Description |
|--------|------|
| `ThenMatches(regex)` | Regex pattern validation |
| `ThenMatches(regex, message)` | Regex + custom message |
| `ThenIsUpperCase()` | Uppercase validation |
| `ThenIsLowerCase()` | Lowercase validation |

#### Numeric Chaining

| Method | Description |
|--------|------|
| `ThenPositive()` | Positive number validation |
| `ThenNonNegative()` | Non-negative (>= 0) validation |
| `ThenNotZero()` | Non-zero validation |
| `ThenBetween(min, max)` | Range validation |
| `ThenAtMost(max)` | Maximum value validation |
| `ThenAtLeast(min)` | Minimum value validation |

#### DateTime Chaining

| Method | Description |
|--------|------|
| `ThenNotDefault()` | Validates not default (DateTime.MinValue) |
| `ThenInPast()` | Validates date is in the past |
| `ThenInFuture()` | Validates date is in the future |
| `ThenBefore(boundary)` | Validates date is before boundary |
| `ThenAfter(boundary)` | Validates date is after boundary |
| `ThenDateBetween(min, max)` | Validates date is within range |

#### Range Chaining

| Method | Description |
|--------|------|
| `ThenValidRange()` | Validates range is valid (min <= max) |
| `ThenValidStrictRange()` | Strict range validation (min < max) |

#### Collection Chaining

| Method | Description |
|--------|------|
| `ThenNotEmptyArray()` | Validates array is not empty |

#### Generic/Custom Chaining

| Method | Description |
|--------|------|
| `ThenMust(predicate, errorType, message)` | Custom condition (fixed message) |
| `ThenMust(predicate, errorType, messageFactory)` | Custom condition (message factory function) |

```csharp
// Error type definition: public sealed record Unsupported : DomainErrorType.Custom;
// Using message factory function
.ThenMust(
    v => SupportedCurrencies.Contains(v),
    new Unsupported(),
    v => $"Currency '{v}' is not supported")  // Dynamic message including the value
```

### TypedValidation\<TValueObject, T\>

**Location**: `Functorium.Domains.ValueObjects.Validations.Typed.TypedValidation<TValueObject, T>`

A wrapper struct that carries type information during chaining.

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    // Implicit conversion to Validation<Error, T>
    public static implicit operator Validation<Error, T>(TypedValidation<TValueObject, T> typed);
}
```

**Performance Characteristics:**
- 8-byte readonly struct (stack allocated)
- `AggressiveInlining` applied to all methods
- `TValueObject` is a phantom type parameter (not used at runtime)

#### LINQ Support (SelectMany, Select)

TypedValidation supports LINQ query expressions. You can use `from...in` syntax without explicit casting.

```csharp
// Using LINQ query expression without casting
public static Validation<Error, (DateTime Min, DateTime Max)> Validate(DateTime startDate, DateTime endDate) =>
    from validStartDate in ValidationRules<DateRange>.NotDefault(startDate)
    from validEndDate in ValidationRules<DateRange>.NotDefault(endDate)
    from validRange in ValidationRules<DateRange>.ValidStrictRange(validStartDate, validEndDate)
    select validRange;
```

| Method | Description |
|--------|------|
| `SelectMany` | TypedValidation -> Validation or TypedValidation -> TypedValidation chaining |
| `Select` | Value transformation (Map) |
| `ToValidation()` | Explicit conversion from TypedValidation to Validation |

#### Tuple Apply Support

**Location**: `Functorium.Domains.ValueObjects.Validations.ValidationApplyExtensions`

Provides Apply overloads for `Validation<Error, T>` or `TypedValidation<TValueObject, T>` tuples. Can be used without `.As()`.

> **Note**: Apply extension methods are in the root `Validations` namespace, so you need to add that namespace when using the Apply pattern.

```csharp
// Validation tuple - .As() not needed
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => new Money(a, c));  // Directly returns Validation<Error, Money>

// Tuple including TypedValidation - .As() not needed
(ValidateCurrency(baseCurrency),
 ValidateCurrency(quoteCurrency),
 ValidationRules<ExchangeRate>.Positive(rate))  // TypedValidation
    .Apply((b, q, r) => (b, q, r));  // Directly returns Validation<Error, T>
```

### Contextual Validation (Named Context)

**Location**: `Functorium.Domains.ValueObjects.Validations.Contextual`

Used for validating primitive types without a Value Object. Suitable for DTO validation, API input validation, and rapid prototyping.

#### ValidationRules.For() Entry Point

```csharp
using Functorium.Domains.ValueObjects.Validations.Contextual;

// Named Context validation start
ValidationRules.For("ProductName").NotEmpty(name);
// Error: DomainErrors.ProductName.Empty

// Chaining
ValidationRules.For("OrderValidation")
    .NotEmpty(name)
    .ThenMinLength(3)
    .ThenMaxLength(100);
```

#### ValidationContext Validation Methods

The `ValidationContext` returned by `ValidationRules.For()` provides the same validation methods as `ValidationRules<T>`:

| Category | Method |
|------|--------|
| Presence | `NotNull()` |
| Length | `NotEmpty()`, `MinLength()`, `MaxLength()`, `ExactLength()` |
| Format | `Matches()` |
| Numeric | `Positive()`, `NonNegative()`, `NotZero()`, `Between()`, `AtMost()`, `AtLeast()` |
| DateTime | `NotDefault()`, `InPast()`, `InFuture()`, `Before()`, `After()`, `DateBetween()` |
| Custom | `Must()` |

#### ContextualValidation\<T\> Chaining

Chaining methods for `ContextualValidation<T>` returned by `ValidationContext` methods. Provides the same methods as `TypedValidationExtensions`:

| Category | Method |
|------|--------|
| Presence | `ThenNotNull()` |
| Length | `ThenNotEmpty()`, `ThenMinLength()`, `ThenMaxLength()`, `ThenExactLength()`, `ThenNormalize()` |
| Numeric | `ThenPositive()`, `ThenNonNegative()`, `ThenNotZero()`, `ThenBetween()`, `ThenAtMost()`, `ThenAtLeast()` |
| Apply | `Apply()` - Apply support for ContextualValidation tuples |

#### Usage Examples

```csharp
using Functorium.Domains.ValueObjects.Validations.Contextual;

// DTO validation example
public Validation<Error, CreateProductRequest> ValidateRequest(CreateProductRequest request) =>
    (ValidationRules.For("ProductName").NotEmpty(request.Name).ThenMaxLength(100),
     ValidationRules.For("Price").Positive(request.Price),
     ValidationRules.For("Category").NotEmpty(request.Category))
        .Apply((name, price, category) => request);

// API input validation example
public Validation<Error, decimal> ValidateAmount(decimal amount) =>
    ValidationRules.For("Amount")
        .Positive(amount)
        .ThenAtMost(1_000_000m);
```

#### Validation Using IValidationContext (Context Class)

**Location**: `Functorium.Domains.ValueObjects.Validations.IValidationContext`

When you want to use the `ValidationRules<T>` pattern without a Value Object, you can create a class that implements the `IValidationContext` marker interface. This approach is a middle ground between Named Context (`ValidationRules.For()`) and Typed (`ValidationRules<T>`).

**When to use?**

- When you need a **reusable validation context** in the Application Layer
- When you want to avoid the **string typo risk** of Named Context
- But creating a Value Object would be **overkill**

```csharp
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;

// 1. Define empty classes implementing IValidationContext
public sealed class ProductValidation : IValidationContext;
public sealed class OrderValidation : IValidationContext;

// 2. Use in ValidationRules<T> instead of a Value Object
public Validation<Error, decimal> ValidatePrice(decimal price) =>
    ValidationRules<ProductValidation>.Positive(price);
// Error Code: DomainErrors.ProductValidation.NotPositive

public Validation<Error, string> ValidateOrderId(string orderId) =>
    ValidationRules<OrderValidation>.NotEmpty(orderId)
        .ThenMinLength(10);
// Error Code: DomainErrors.OrderValidation.Empty or TooShort
```

**Advantages:**
- Compile-time type safety (prevents typos)
- Validation context can be reused in multiple places
- IDE autocompletion support

#### Comparison of Three Validation Approaches

The following table compares the three validation approaches. The key difference is the level of type safety and the recommended layer for use.

| Characteristics | Typed | Context Class | Named Context |
|------|-------|---------------|---------------|
| **Usage** | `ValidationRules<Price>` | `ValidationRules<ProductValidation>` | `ValidationRules.For("Price")` |
| **Type Source** | Value Object | IValidationContext implementing class | String |
| **Type Safety** | Compile-time | Compile-time | Runtime |
| **Namespace** | `Validations.Typed` | `Validations.Typed` | `Validations.Contextual` |
| **Recommended Layer** | Domain | Application | Presentation |
| **Recommended For** | Value Object | Reusable validation | One-off validation, prototyping |
| **Error Code** | `DomainErrors.Price.NotPositive` | `DomainErrors.ProductValidation.NotPositive` | `DomainErrors.Price.NotPositive` |

**Selection Guide:**

```
Do you have a Value Object?
    |
    +-- Yes --> ValidationRules<Price> (Typed)
    |
    `-- No --> Is the validation reused in multiple places?
                      |
                      +-- Yes --> ValidationRules<ProductValidation> (Context Class)
                      |
                      `-- No --> ValidationRules.For("Price") (Named Context)
```

---

## Usecase Pipeline Validation System

FluentValidation is commonly used to validate Request DTOs in the Application Layer. By **reusing validation logic already defined in Value Objects,** you can avoid duplication and maintain consistency.

Functorium provides the `MustSatisfyValidation` extension method for FluentValidation integration. This allows you to **define validation logic once in the Domain Layer (Value Object)** and reuse it directly in the Application Layer.

### Why Reuse Validation?

In a typical layered architecture, validation occurs in two places:

1. **Application Layer (use case entry point)**: When a Request DTO arrives, it is validated with FluentValidation
2. **Domain Layer (Value Object creation)**: Validated when calling `Price.Create(value)`

Writing validation logic separately in each place causes **duplication,** and when rules change, **both places must be updated.**

```
+-------------------------------------------------------------------+
|  Application Layer                                                |
|  +-------------------------------------------------------------+  |
|  |  UsecaseValidationPipeline (FluentValidation)               |  |
|  |  - RuleFor(x => x.Price).MustSatisfyValidation(...)    <----+--+-- Value Object's
|  +-------------------------------------------------------------+  |   Validate reuse
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|  Domain Layer                                                     |
|  +-------------------------------------------------------------+  |
|  |  Value Object (Price)                                       |  |
|  |  - Validate(): Single source of validation logic       <----+--+-- Define validation
|  |  - Create(): Create object after Validate call              |  |
|  +-------------------------------------------------------------+  |
+-------------------------------------------------------------------+
```

**Solution**: Reuse by directly calling the Value Object's `Validate` method from FluentValidation.

**Location**: `Functorium.Applications.Validations.FluentValidationExtensions`

### MustSatisfyValidation (Input Type == Output Type)

Used when the input type and output type of the validation method are the same. In most cases, this method is used.

```csharp
// decimal → Validation<Error, decimal>
public static Validation<Error, decimal> ValidateAmount(decimal amount) =>
    ValidationRules<Money>.NonNegative(amount);

// Application Layer UsecaseValidationPipeline
// Used in FluentValidation (type inference works)
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Price)
            .MustSatisfyValidation(Money.ValidateAmount);

        RuleFor(x => x.Currency)
            .MustSatisfyValidation(Money.ValidateCurrency);

        RuleFor(x => x.ProductId)
            .MustSatisfyValidation(ProductId.Validate);
    }
}
```

### MustSatisfyValidationOf (Input Type != Output Type)

Rarely, a validation method may return a different type than its input. For example, when receiving a string, parsing it to an integer, and then validating.

```csharp
// string → Validation<Error, int> (input: string, output: int)
public sealed class Age : ComparableSimpleValueObject<int>
{
    public sealed record InvalidFormat : DomainErrorType.Custom;

    // Receives a string, converts to integer, then validates
    public static Validation<Error, int> Validate(string value) =>
        int.TryParse(value, out var parsed)
            ? ValidationRules<Age>.Between(parsed, 0, 150)
            : DomainError.For<Age>(new InvalidFormat(), value,
                $"'{value}' is not a valid number");
}

// Application Layer UsecaseValidationPipeline
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // Use MustSatisfyValidationOf because types differ
        // Type parameters: <RequestType, InputType(string), OutputType(int)>
        RuleFor(x => x.Age)
            .MustSatisfyValidationOf<Request, string, int>(Age.Validate);
    }
}
```

### Which Method Should You Use?

In most cases, use **MustSatisfyValidation.** This is because Value Object `Validate` methods typically take and return the same type.

```csharp
// Most cases: decimal → Validation<Error, decimal>
public static Validation<Error, decimal> Validate(decimal value) => ...

// Rare cases: string → Validation<Error, int> (includes parsing)
public static Validation<Error, int> Validate(string value) => ...
```

| Validation Method Signature | Method to Use | Type Specification |
|---------------------|-------------|----------|
| `Func<T, Validation<Error, T>>` | `MustSatisfyValidation` | Not needed (type inference) |
| `Func<TIn, Validation<Error, TOut>>` | `MustSatisfyValidationOf` | Required (`<TRequest, TIn, TOut>`) |

> **Note**: The reason types must be specified in `MustSatisfyValidationOf` is that C# 14 extension members do not support type inference when there are additional generic type parameters.

---

## Error System

Functorium uses **result types instead of exceptions** to handle errors. Validation failures are expressed as `Validation<Error, T>` or `Fin<T>` without throwing exceptions. This approach follows the functional programming philosophy that "failure is a normal result, not an exception."

**Advantages of result types:**
- Callers must **explicitly** handle failure possibilities (enforced by the compiler)
- Error handling via **function chaining** without try-catch
- Multiple errors can be **collected** and returned at once

> **For details, see the [Error System Guide](./08a-error-system).**

### DomainErrorType Overview

**Location**: `Functorium.Domains.Errors.DomainErrorType`

Provides type-safe error definitions through a sealed record hierarchy.

```csharp
using static Functorium.Domains.Errors.DomainErrorType;
```

#### Category Structure

| Category | Description | Representative ErrorType |
|------|------|---------------|
| Presence | Value existence validation | `Empty`, `Null` |
| Length | Length validation | `TooShort`, `TooLong`, `WrongLength` |
| Format | Format validation | `InvalidFormat`, `NotUpperCase`, `NotLowerCase` |
| DateTime | Date validation | `DefaultDate`, `NotInPast`, `NotInFuture`, `TooLate`, `TooEarly` |
| Numeric | Numeric validation | `Zero`, `Negative`, `NotPositive`, `OutOfRange`, `BelowMinimum`, `AboveMaximum` |
| Range | Range pair validation | `RangeInverted`, `RangeEmpty` |
| Existence | Existence validation | `NotFound`, `AlreadyExists`, `Duplicate`, `Mismatch` |
| Custom | Custom errors | `Custom` (abstract record, user-defined derived) |

### DomainError.For\<T\>() Helper

**Location**: `Functorium.Domains.Errors.DomainError`

Creates errors for custom business rule validation failures not covered by `ValidationRules<T>`. Automatically generates error codes in the format `DomainErrors.{TypeName}.{ErrorName}`.

#### Method Signatures

```csharp
// Single value (string)
public static Error For<TContext>(
    DomainErrorType errorType,
    string currentValue,
    string message);

// Single value (generic)
public static Error For<TContext, TValue>(
    DomainErrorType errorType,
    TValue currentValue,
    string message);

// Two values
public static Error For<TContext, TValue1, TValue2>(
    DomainErrorType errorType,
    TValue1 value1,
    TValue2 value2,
    string message);

// Three values
public static Error For<TContext, TValue1, TValue2, TValue3>(
    DomainErrorType errorType,
    TValue1 value1,
    TValue2 value2,
    TValue3 value3,
    string message);
```

#### Parameter Description

| Parameter | Description |
|----------|------|
| `TContext` | Error context type (Value Object or IValidationContext). The `{TypeName}` part of the error code |
| `errorType` | `DomainErrorType` instance. The `{ErrorName}` part of the error code |
| `currentValue` | The current value that failed validation. Included in debugging and error messages |
| `message` | Error message to display to users/developers |

#### Usage Examples and Output

Each overload internally creates a different Error type:

| Overload | Internal Type | Value Fields |
|----------|----------|---------|
| `For<TContext>` | `ErrorCodeExpected` | `ErrorCurrentValue: string` |
| `For<TContext, TValue>` | `ErrorCodeExpected<TValue>` | `ErrorCurrentValue: TValue` |
| `For<TContext, T1, T2>` | `ErrorCodeExpected<T1, T2>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2` |
| `For<TContext, T1, T2, T3>` | `ErrorCodeExpected<T1, T2, T3>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2`, `ErrorCurrentValue3: T3` |

**Single value (string) -> `ErrorCodeExpected`**

```csharp
var error = DomainError.For<Email>(new Empty(), "", "Email cannot be empty");

// Type verification
error.ShouldBeOfType<ErrorCodeExpected>();
var typed = (ErrorCodeExpected)error;
typed.ErrorCode.ShouldBe("DomainErrors.Email.Empty");
typed.ErrorCurrentValue.ShouldBe("");
typed.Message.ShouldBe("Email cannot be empty");
```

```json
{
  "ErrorCode": "DomainErrors.Email.Empty",
  "ErrorCurrentValue": "",
  "Message": "Email cannot be empty"
}
```

**Single value (generic) -> `ErrorCodeExpected<TValue>`**

```csharp
var error = DomainError.For<Age, int>(new Negative(), -5, "Age cannot be negative");

// Type verification
error.ShouldBeOfType<ErrorCodeExpected<int>>();
var typed = (ErrorCodeExpected<int>)error;
typed.ErrorCode.ShouldBe("DomainErrors.Age.Negative");
typed.ErrorCurrentValue.ShouldBe(-5);  // int type preserved
typed.Message.ShouldBe("Age cannot be negative");
```

```json
{
  "ErrorCode": "DomainErrors.Age.Negative",
  "ErrorCurrentValue": -5,
  "Message": "Age cannot be negative"
}
```

**Two values -> `ErrorCodeExpected<T1, T2>`**

```csharp
// Error type definition: public sealed record InvalidRange : DomainErrorType.Custom;
var startDate = new DateTime(2024, 12, 31);
var endDate = new DateTime(2024, 1, 1);
var error = DomainError.For<DateRange, DateTime, DateTime>(
    new InvalidRange(), startDate, endDate, "Start must be before end");

// Type verification
error.ShouldBeOfType<ErrorCodeExpected<DateTime, DateTime>>();
var typed = (ErrorCodeExpected<DateTime, DateTime>)error;
typed.ErrorCode.ShouldBe("DomainErrors.DateRange.InvalidRange");
typed.ErrorCurrentValue1.ShouldBe(startDate);  // DateTime type preserved
typed.ErrorCurrentValue2.ShouldBe(endDate);    // DateTime type preserved
typed.Message.ShouldBe("Start must be before end");
```

```json
{
  "ErrorCode": "DomainErrors.DateRange.InvalidRange",
  "ErrorCurrentValue1": "2024-12-31T00:00:00",
  "ErrorCurrentValue2": "2024-01-01T00:00:00",
  "Message": "Start must be before end"
}
```

**Three values -> `ErrorCodeExpected<T1, T2, T3>`**

```csharp
// Error type definition: public sealed record InvalidTriangle : DomainErrorType.Custom;
var error = DomainError.For<Triangle, double, double, double>(
    new InvalidTriangle(), 1.0, 2.0, 10.0, "Invalid triangle sides");

// Type verification
error.ShouldBeOfType<ErrorCodeExpected<double, double, double>>();
var typed = (ErrorCodeExpected<double, double, double>)error;
typed.ErrorCode.ShouldBe("DomainErrors.Triangle.InvalidTriangle");
typed.ErrorCurrentValue1.ShouldBe(1.0);   // double type preserved
typed.ErrorCurrentValue2.ShouldBe(2.0);   // double type preserved
typed.ErrorCurrentValue3.ShouldBe(10.0);  // double type preserved
typed.Message.ShouldBe("Invalid triangle sides");
```

```json
{
  "ErrorCode": "DomainErrors.Triangle.InvalidTriangle",
  "ErrorCurrentValue1": 1.0,
  "ErrorCurrentValue2": 2.0,
  "ErrorCurrentValue3": 10.0,
  "Message": "Invalid triangle sides"
}
```

**Tuple value example -> `ErrorCodeExpected<(T1, T2)>`**

```csharp
var range = (Min: 100m, Max: 50m);
var error = DomainError.For<PriceRange, (decimal Min, decimal Max)>(
    new RangeInverted(Min: "100", Max: "50"),
    range,
    "Price range is invalid. Minimum cannot exceed maximum.");

// Type verification
error.ShouldBeOfType<ErrorCodeExpected<(decimal Min, decimal Max)>>();
var typed = (ErrorCodeExpected<(decimal Min, decimal Max)>)error;
typed.ErrorCode.ShouldBe("DomainErrors.PriceRange.RangeInverted");
typed.ErrorCurrentValue.ShouldBe((100m, 50m));  // Tuple type preserved
typed.Message.ShouldBe("Price range is invalid. Minimum cannot exceed maximum.");
```

```json
{
  "ErrorCode": "DomainErrors.PriceRange.RangeInverted",
  "ErrorCurrentValue": {
    "Item1": 100.0,
    "Item2": 50.0
  },
  "Message": "Price range is invalid. Minimum cannot exceed maximum."
}
```

#### ValidationRules\<T\> vs DomainError.For\<T\>()

| Situation | Recommendation |
|------|------|
| Common validation (empty values, length, range, etc.) | `ValidationRules<T>` + chaining |
| Custom condition validation | `ValidationRules<T>.Must()` or `.ThenMust()` |
| Business rule violation during domain operation | `DomainError.For<T>()` |
| Two-value comparison failure (currency mismatch, etc.) | `DomainError.For<T, V1, V2>()` |

```csharp
// ValidationRules<T>: Common validation
public static Validation<Error, decimal> ValidateAmount(decimal amount) =>
    ValidationRules<Money>.NonNegative(amount);

// DomainError.For<T>(): Business rule violation during domain operation
public Fin<Money> Add(Money other) =>
    Currency == other.Currency
        ? new Money(Amount + other.Amount, Currency)
        : DomainError.For<Money, string, string>(
            new Mismatch(), Currency, other.Currency,
            $"Cannot add different currencies: {Currency} vs {other.Currency}");
```

---

## Implementation Patterns

The core of Value Object implementation is the **Create/Validate separation pattern.** This pattern separates validation logic from object creation to improve reusability and testability.

- **Validate**: Receives primitive values and returns a validation result (`Validation<Error, T>`). Does not create an object.
- **Create**: Calls Validate and on success creates the object, returning `Fin<T>`.

This separation allows **the Validate method to be reused elsewhere (such as FluentValidation pipelines).**

> **Note**: Entities follow the same Create/Validate separation pattern. For details, see the [Entity Implementation Guide - Creation Patterns](./06b-entity-aggregate-core#생성-패턴).

### CreateFromValidated: Factory for ORM/Repository Restoration

`Create` and `Validate` validate **external input** to create Value Objects. In contrast, `CreateFromValidated` directly receives **already validated and normalized data** to restore Value Objects. It performs neither validation nor normalization.

**Contract:** `CreateFromValidated` only accepts already valid and normalized data -- direct pass-through, no validation/normalization.

**Purpose:**
- **ORM/Repository restoration** -- Values read from the DB have already passed validation/normalization at save time, so they do not need to be validated again.
- **Direct use in handlers after pipeline validation** -- After FluentValidation completes validation with `Validate()`, handlers create VOs with `CreateFromValidated(request.Name)`.

**Create vs CreateFromValidated Comparison:**

| Category | `Create(string? value)` | `CreateFromValidated(string value)` |
|------|------------------------|-------------------------------------|
| Input | External primitive value (untrusted) | Already validated/normalized value (trusted) |
| Return | `Fin<T>` (can fail) | `T` (direct return) |
| Validation | Calls Validate() | None |
| Normalization | ThenNormalize within Validate() | None |
| Use cases | External API, user input | ORM restoration, handlers |

**Code Example:**

```csharp
public sealed partial class Email : SimpleValueObject<string>
{
    public const int MaxLength = 320;
    private Email(string value) : base(value) { }

    // Create: External input validation + normalization + object creation
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // Validate: Validation + normalization (returns primitive type)
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex());

    // CreateFromValidated: Directly pass-through already normalized values
    public static Email CreateFromValidated(string value) => new(value);

    public static implicit operator string(Email email) => email.Value;
}
```

Do not put normalization logic like `.Trim()`, `.ToLowerInvariant()` in `CreateFromValidated`. Values read from the DB were already normalized at save time, and values received in handlers were already normalized during pipeline validation.

### Create/Validate Pattern

| Base Class | Create Pattern | Validate Return |
|------------|-------------|---------------|
| `SimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` | `Validation<Error, T>` |
| `ComparableSimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` | `Validation<Error, T>` |
| `ValueObject` (Apply) | `CreateFromValidation(Validate(...), factory)` | `Validation<Error, (T1, T2, ...)>` |

### Sequential Validation (Bind/Then)

Stops at the first error. Suitable for validations with dependencies:

```csharp
public static Validation<Error, string> Validate(string? value) =>
    ValidationRules<Email>.NotEmpty(value ?? "")  // 1. Empty value validation
        .ThenMatches(EmailPattern)          // 2. Format validation (if 1 passes)
        .ThenMaxLength(254);                // 3. Length validation (if 2 passes)
```

### Parallel Validation (Apply)

Collects all errors. Suitable for independent validations:

```csharp
public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
    (ValidateAmount(amount), ValidateCurrency(currency))
        .Apply((a, c) => (Amount: a, Currency: c));
```

> **Note**: When using the Apply pattern, the `Functorium.Domains.ValueObjects.Validations` namespace is required.
> (`ValidationApplyExtensions` is located in that namespace)

### Mixed Pattern (Apply + Bind)

Dependent validation after parallel validation:

```csharp
// .As() not needed when tuple includes TypedValidation
public static Validation<Error, (string BaseCurrency, string QuoteCurrency, decimal Rate)> Validate(
    string baseCurrency, string quoteCurrency, decimal rate) =>
    (ValidateCurrency(baseCurrency),
     ValidateCurrency(quoteCurrency),
     ValidationRules<ExchangeRate>.Positive(rate))  // TypedValidation
        .Apply((b, q, r) => (BaseCurrency: b, QuoteCurrency: q, Rate: r))
        .Bind(v => ValidateDifferentCurrencies(v.BaseCurrency, v.QuoteCurrency)
            .Map(_ => (v.BaseCurrency, v.QuoteCurrency, v.Rate)));
```

---

## Troubleshooting

### Compile Error from Missing `.As()` Call in Apply Pattern
**Cause:** When using Apply with `Validation<Error, T>` tuples, LanguageExt's type inference can sometimes fail.
**Solution:** If the tuple includes `TypedValidation`, `.As()` is not needed. Otherwise, add the `ValidationApplyExtensions` namespace (`Functorium.Domains.ValueObjects.Validations`). The Apply overloads in this namespace work directly without `.As()`.

### Type Inference Failure in MustSatisfyValidation
**Cause:** `MustSatisfyValidationOf` is used when input and output types differ, and due to C#14 extension members' type inference limitations, generic parameters must be specified explicitly.
**Solution:** If input/output types are the same, use `MustSatisfyValidation`. If they differ, explicitly specify types as `MustSatisfyValidationOf<TRequest, TIn, TOut>(Validate)`.

### Cannot Find ValidationRules Namespace
**Cause:** The namespace differs depending on the validation approach (Typed, Contextual, Context Class).
**Solution:**
- Typed: `using Functorium.Domains.ValueObjects.Validations.Typed;`
- Contextual: `using Functorium.Domains.ValueObjects.Validations.Contextual;`
- Apply extensions: `using Functorium.Domains.ValueObjects.Validations;`

---

## FAQ

### Q1. Should I use SimpleValueObject or ValueObject?
Use `SimpleValueObject<T>` (or `ComparableSimpleValueObject<T>` if comparison is needed) when wrapping a single primitive value. Use `ValueObject` when multiple properties together form a meaning (e.g., Money = Amount + Currency).

### Q2. Why separate Create and Validate?
`Validate` returns `Validation<Error, T>` so it can be reused in FluentValidation pipelines. `Create` returns `Fin<T>` to create the actual object. Separation allows defining validation logic in one place and reusing it in multiple places.

### Q3. When should I use sequential vs parallel validation?
**Sequential validation (Bind/Then)**: When the next validation is meaningful only if the previous one passes. Example: empty check -> format check -> length check. **Parallel validation (Apply)**: When each validation is independent and you want to collect all errors at once. Example: Amount validation + Currency validation.

### Q4. Should I use Named Context or IValidationContext?
For one-off validation or prototyping, use `ValidationRules.For("Name")` (Named Context). To reuse in multiple places or prevent typos, create a class implementing `IValidationContext` and use it as `ValidationRules<MyContext>`.

### Q5. What is the difference between DomainError.For and ValidationRules.Must?
`ValidationRules<T>.Must()` is used in validation chaining during Value Object creation and returns `Validation<Error, T>`. `DomainError.For<T>()` is used for business rule violations during Entity domain operations, directly creating an error and returning it as `Fin<T>`.

---

## Reference Documents

- [Value Objects: Enumerations, Validation, and Practical Patterns](./05b-value-objects-validation)
- [Value Objects: Union Types](./05c-union-value-objects) - Discriminated Union patterns and state transitions
