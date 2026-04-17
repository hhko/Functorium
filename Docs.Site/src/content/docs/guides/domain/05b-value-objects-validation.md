---
title: "Value Objects: Enumerations, Validation, and Practical Patterns"
---

This document covers enumeration patterns, practical examples, Application Layer validation merging, and FAQ for value objects. For core concepts and base classes, see [05a-value-objects](../05a-value-objects). For Union types (Discriminated Union), see [05c-union-value-objects](../05c-union-value-objects).

## Introduction

In [05a-value-objects.md](../05a-value-objects), we explored the core concepts and implementation patterns of value objects. This document covers enumeration patterns (`SmartEnum`), practical examples by base class, and the `Apply` pattern for merging multiple validations in the Application Layer.

> The key practical points for value object implementation are **understanding the Create pattern differences by base class and using Apply merging in Usecases to collect all validation errors at once.**

## Summary

### Key Commands

```csharp
// SmartEnum Create pattern
public static Fin<Currency> Create(string currencyCode) =>
    Validate(currencyCode).Map(FromValue).ToFin();

// SimpleValueObject Create pattern
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(Validate(value), v => new Email(v));

// Apply merging in Application Layer
(name, description, price, stockQuantity)
    .Apply((n, d, p, s) => Product.Create(...))
    .As().ToFin();
```

### Key Procedures

**1. Creating a Value Object:**
1. Choose a base class (`SimpleValueObject<T>`, `ValueObject`, `SmartEnum`, etc.)
2. Define validation rules with a `Validate()` method
3. Combine validation and creation with a `Create()` method

**2. Application Layer Validation Merging:**
1. Call each field's `VO.Validate()` (returns Validation<Error, T>)
2. Merge all validation results in parallel with `Apply`
3. On success, create the Entity; on failure, collect all errors

### Key Concepts

| Concept | Description |
|---------|-------------|
| `SmartEnum` | Enumeration pattern where each value needs unique properties/behavior |
| `ValidationRules<T>` | Type-based validation rule chaining in the Domain Layer |
| `ValidationRules.For()` | String-based validation for fields without a VO (Named Context) |
| `Apply` merging | Performs independent validations in parallel and collects all errors |
| `Bind`/`Then` chaining | Performs dependent validations sequentially (stops at first error) |

We will now look at enumeration patterns first, then proceed through practical examples and finally cover how to merge multiple validations in the Application Layer.

---

## Enumeration Implementation Patterns

When representing **fixed choices** (currency types, order statuses, membership tiers, etc.) in the domain, use `Ardalis.SmartEnum` instead of C#'s built-in `enum`.

**Why SmartEnum?**

C#'s built-in `enum` is merely a simple integer constant:

- Cannot attach additional properties (display name, symbol, etc.) to values
- Cannot define different behavior per value
- Invalid value casting is possible (`(Currency)999`)

SmartEnum solves these issues:
- Each value can have unique properties and behavior
- Runtime type safety guaranteed
- Can include validation logic like Value Objects

`SmartEnum` does not inherit from `SimpleValueObject`, so the Create pattern is slightly different.

### Basic Structure

Note the `Validate` -> `Map(FromValue)` -> `ToFin()` chaining that forms the SmartEnum-specific Create pattern.

```csharp
using Ardalis.SmartEnum;
using Functorium.Domains.ValueObjects;

public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
    public sealed record Unsupported : DomainErrorType.Custom;

    public static readonly Currency KRW = new(nameof(KRW), "KRW", "Korean Won", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "US Dollar", "$");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "Euro", "€");

    public string KoreanName { get; }
    public string Symbol { get; }

    private Currency(string name, string value, string koreanName, string symbol)
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }

    // SmartEnum pattern: .Map(FromValue).ToFin()
    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    public static Currency CreateFromValidated(string currencyCode) =>
        FromValue(currencyCode);

    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainError.For<Currency>(new Empty(), currencyCode ?? "",
                $"Currency code cannot be empty")
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainError.For<Currency>(new WrongLength(3), currencyCode,
                $"Currency code must be exactly 3 letters")
            : currencyCode.ToUpperInvariant();

    private static Validation<Error, string> ValidateSupported(string currencyCode)
    {
        try { FromValue(currencyCode); return currencyCode; }
        catch (SmartEnumNotFoundException)
        {
            return DomainError.For<Currency>(new Unsupported(), currencyCode,
                $"Currency code is not supported");
        }
    }

    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
    public static IEnumerable<Currency> GetAllSupportedCurrencies() => List;
}
```

### Create Pattern Differences

The composition approach of the Create method differs by base class. The table below provides an at-a-glance comparison.

| Base Class | Create Pattern |
|------------|----------------|
| `SimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` |
| `ComparableSimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` |
| `ValueObject` | `CreateFromValidation(Validate(...), factory)` |
| `SmartEnum<T, TValue>` | `Validate(value).Map(FromValue).ToFin()` |

Now that we understand enumeration and Create pattern differences, let's look at practical examples for various base classes.

---

## Practical Examples

These are complete examples applying the patterns described above. Each example demonstrates production-ready implementations.

### Email (SimpleValueObject)

A complete example of the most common pattern, `SimpleValueObject<string>`. It includes regex validation, normalization (lowercase conversion), and derived properties (LocalPart, Domain).

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using System.Text.RegularExpressions;

public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);
    private const int MaxLength = 254;

    private Email(string value) : base(value)
    {
        var atIndex = value.IndexOf('@');
        LocalPart = value[..atIndex];
        Domain = value[(atIndex + 1)..];
    }

    public string LocalPart { get; }
    public string Domain { get; }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenNormalize(v => v.ToLowerInvariant())
            .ThenMatches(EmailPattern)
            .ThenMaxLength(MaxLength);

    public static implicit operator string(Email email) => email.Value;
}
```

### Quantity (ComparableSimpleValueObject)

An example of a comparable single-value object. Quantity comparison (`q1 > q2`) and sorting are needed, and it includes domain operations (Add, Subtract) and convenience properties (IsZero, IsPositive).

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

public sealed class Quantity : ComparableSimpleValueObject<int>
{
    public const int MaxValue = 10000;

    private Quantity(int value) : base(value) { }

    public static Quantity Zero => new(0);
    public static Quantity One => new(1);

    public bool IsZero => Value == 0;
    public bool IsPositive => Value > 0;

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Quantity(v));

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<Quantity>.NonNegative(value)
            .ThenAtMost(MaxValue);

    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));

    public static implicit operator int(Quantity q) => q.Value;
}
```

### Money (ValueObject with Apply)

An example of a value object composed of multiple properties (Amount + Currency). It uses the Apply pattern to **validate both properties in parallel,** and handles **business rule violations** (adding different currencies) with `DomainError.For<T>()` in domain operations (Add).

Note the tuple + `Apply` parallel validation in the `Validate` method and the currency mismatch handling with `DomainError` in the `Add` method.

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;
using static Functorium.Domains.Errors.DomainErrorType;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // Create: uses CreateFromValidation helper
    public static Fin<Money> Create(decimal amount, string currency) =>
        CreateFromValidation(Validate(amount, currency), v => new Money(v.Amount, v.Currency));

    // Validate: returns validated primitive tuple (ValueObject creation happens in Create)
    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => (Amount: a, Currency: c));

    private static Validation<Error, decimal> ValidateAmount(decimal amount) =>
        ValidationRules<Money>.NonNegative(amount);

    private static Validation<Error, string> ValidateCurrency(string currency) =>
        ValidationRules<Money>.NotEmpty(currency)
            .ThenNormalize(v => v.ToUpperInvariant())
            .ThenExactLength(3);

    public Fin<Money> Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : DomainError.For<Money, string, string>(
                new Mismatch(), Currency, other.Currency,
                $"Cannot add different currencies: {Currency} vs {other.Currency}");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

Now that we have looked at individual value object implementations, let's learn how to merge multiple value object validation results in a Usecase.

---

## Merging VO Validations in the Application Layer

When simultaneously validating multiple ValueObjects and creating an Entity in a Usecase, use the Apply pattern.

### Apply Merging Pattern (Inside Usecase)

Note the flow of calling each field's `Validate()` individually, then merging all results at once with tuple + `Apply`.

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // 1. All fields: call VO Validate() (returns Validation<Error, T>)
    var name = ProductName.Validate(request.Name);
    var description = ProductDescription.Validate(request.Description);
    var price = Money.Validate(request.Price);
    var stockQuantity = Quantity.Validate(request.StockQuantity);

    // 2. Merge validations in parallel with Apply, then create Entity
    return (name, description, price, stockQuantity)
        .Apply((n, d, p, s) => Product.Create(
            ProductName.Create(n).ThrowIfFail(),
            ProductDescription.Create(d).ThrowIfFail(),
            Money.Create(p).ThrowIfFail(),
            Quantity.Create(s).ThrowIfFail()))
        .As()
        .ToFin();
}
```

### Pattern Explanation

The table below summarizes the role of each step in the code above.

| Step | Description |
|------|-------------|
| Validate() calls | Collect all field validations as Validation<Error, T> |
| Apply merge | Entity creation proceeds only if all validations succeed |
| ThrowIfFail() | Safe VO conversion since values are already validated |

### Validation for Fields Without a VO (Named Context)

When not all fields are defined as Value Objects, use Named Context validation:

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // Fields with VOs
    var name = ProductName.Validate(request.Name);
    var price = Money.Validate(request.Price);

    // Fields without VOs: use Named Context
    var note = ValidationRules.For("Note")
        .NotEmpty(request.Note)
        .ThenMaxLength(500);

    // Merge all into tuple - parallel validation with Apply
    return (name, price, note.Value)
        .Apply((n, p, noteValue) =>
            Product.Create(
                ProductName.Create(n).ThrowIfFail(),
                noteValue,
                Money.Create(p).ThrowIfFail()))
        .As()
        .ToFin();
}
```

> **Note**: For frequently used fields, defining a separate ValueObject is recommended over using Named Context.

---

## Fin\<T\> Composition in the Application Layer (FinApplyExtensions)

The `Validation<Error, T>` Apply pattern above is used **inside VOs** to compose multiple validation rules in parallel. In contrast, in the **Application Layer**, you need to compose the `Create()` results (`Fin<T>`) of multiple already-created VOs. This is where `FinApplyExtensions` is used.

### Motivation

- `VO.Create()` returns `Fin<T>` (success or failure)
- When composing multiple `Fin<T>` results in the Application Layer, individual `ThrowIfFail()` stops at the first error
- `FinApplyExtensions` internally converts all `Fin<T>` to `Validation<Error, T>` to **accumulate all errors**

### Usage Example

```csharp
// Application Layer: compose multiple VO Create results applicatively
var contact = (
    PersonalName.Create(cmd.FirstName, cmd.LastName),
    EmailAddress.Create(cmd.Email)
).Apply((name, email) => Contact.Create(name, email, now));
// -> Fin<Contact>, with all VO validation errors accumulated
```

### Validation Apply vs Fin Apply Comparison

| Property | Validation Apply | Fin Apply |
|----------|-----------------|-----------|
| Input type | `Validation<Error, T>` tuple | `Fin<T>` tuple |
| Usage location | VO internal `Validate` composition | Application Layer VO `Create` composition |
| Error accumulation | Collects all errors | Collects all errors (internally converts to Validation) |
| Overloads | 2-5 tuples | 2-5 tuples |

---

## Layer-by-Layer Validation Composition Responsibilities

The validation responsibility for converting raw inputs (strings, etc.) to VOs is clearly separated by layer:

| Layer | Validation Boundary | `Validate` | `Create` | `CreateFromValidated` |
|-------|---------------------|-----------|----------|----------------------|
| Simple VO | raw -> VO | `ValidationRules` chain | `string?` -> `Fin<T>` | `string` -> T |
| Composite VO | raw -> VO | Child `Validate` applicative composition | `string?` -> `Fin<T>` | Child VO -> T |
| Entity/Aggregate | VO -> Entity | -- | VO -> Entity | VO + ID -> Entity (ORM restoration) |
| Application Layer | -- | -- | `FinApply` for N `Fin<T>` applicative composition | -- |

Entities/Aggregates have no `Validate` and only receive already-validated VOs. When composing multiple VO `Create` results (`Fin<T>`) in the Application Layer, use `FinApplyExtensions`' tuple `.Apply()`.

---

## Troubleshooting

### SmartEnumNotFoundException from SmartEnum's Create

**Cause:** An unregistered value was passed to `FromValue()`. SmartEnum only accepts values registered as `static readonly` fields.

**Resolution:** Validate support first through the `Validate()` method. The `ValidateSupported` method uses `try-catch` to catch `SmartEnumNotFoundException` and convert it to a `DomainError`.

```csharp
private static Validation<Error, string> ValidateSupported(string currencyCode)
{
    try { FromValue(currencyCode); return currencyCode; }
    catch (SmartEnumNotFoundException)
    {
        return DomainError.For<Currency>(new Unsupported(), currencyCode,
            $"Currency code is not supported");
    }
}
```

### Only Some Validation Errors Returned During Apply Merging

**Cause:** `Bind` was used instead of `Apply`, or validation chaining within a field used `Bind` (`Then*`) which executes sequentially and stops at the first error.

**Resolution:** Always use `Apply` for independent inter-field validations. Sequential validation within a single field (`NotEmpty -> Matches -> MaxLength`) can use `Then*`, but inter-field merging must use tuple + `Apply`.

```csharp
// Inter-field validation uses Apply (parallel)
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => new Money(a, c));
```

### Exception from ThrowIfFail()

**Cause:** When `ThrowIfFail()` is called in the factory function after Apply merging, the factory function should only execute when Apply succeeds, so this normally should not occur.

**Resolution:** Use `ThrowIfFail()` only inside the factory function of Apply. Calling `ThrowIfFail()` directly on individual `Fin<T>` outside Apply will throw an exception on validation failure.

---

## FAQ

These are frequently asked questions about value object implementation. If you are still confused after reading the content above, refer to this section.

### Q1. What is the base class selection criteria?

When creating a value object, you need to decide which base class to inherit. Two key questions make this easy to determine.

**First question: Is it a single value or multiple values?**
- **Wrapping a single value** -> `SimpleValueObject<T>` family
  - Examples: email address (single string), price (single decimal), user ID (single int)
- **Composed of multiple properties** -> `ValueObject` family
  - Examples: money (amount + currency), address (city + street + postalCode), coordinates (x + y)

**Second question: Is comparison needed?**
- **No comparison needed** -> `SimpleValueObject<T>` or `ValueObject`
  - Example: "which email is bigger" is meaningless
- **Comparison/sorting needed** -> `ComparableSimpleValueObject<T>` or `ComparableValueObject`
  - Example: prices need "more expensive/cheaper" comparisons, date ranges need sorting

| Condition | Choice |
|-----------|--------|
| Single value wrapper | `SimpleValueObject<T>` |
| Single value + comparison/sorting needed | `ComparableSimpleValueObject<T>` |
| Composite properties | `ValueObject` |
| Composite properties + comparison/sorting needed | `ComparableValueObject` |
| Enumeration + domain logic | `SmartEnum<T, TValue>` |

### Q2. When to use ValidationRules\<T\> vs DomainError.For\<T\>()?

Both generate validation errors, but they serve different purposes.

**ValidationRules\<T\> is for "common validation rules."**

Common validations like "must not be empty", "must be positive", "max 100 characters" are already implemented and can be used via simple chaining.

```csharp
// Good: use ValidationRules for common validations
ValidationRules<Email>.NotEmpty(value)
    .ThenMaxLength(254)
    .ThenMatches(EmailPattern);
```

**DomainError.For\<T\>() is for "specialized business rules."**

Domain-specific errors like "cannot add different currencies" or "insufficient stock" must be created directly.

```csharp
// Good: use DomainError.For for business rule violations
return Currency == other.Currency
    ? new Money(Amount + other.Amount, Currency)
    : DomainError.For<Money, string, string>(
        new Mismatch(), Currency, other.Currency,
        $"Cannot add different currencies: {Currency} vs {other.Currency}");
```

| Scenario | Recommendation |
|----------|----------------|
| Common validation | `ValidationRules<T>` + chaining |
| Custom business rules | `ThenMust` or `DomainError.For<T>()` |
| Errors during domain operations | `DomainError.For<T>()` |

### Q3. When to use Bind(Then) vs Apply?

When there are multiple validations, the choice depends on **how you want to present errors.**

**Bind/Then is "sequential validation." It stops at the first error.**

Subsequent validations do not execute if a preceding one fails. Use when there are dependencies between validations.

```csharp
// "not empty" must pass for "email format check" to be meaningful
ValidationRules<Email>.NotEmpty(value)    // 1. Stops here if empty
    .ThenMatches(EmailPattern)            // 2. Executes only if 1 passes
    .ThenMaxLength(254);                  // 3. Executes only if 2 passes
```

**Apply is "parallel validation." It collects all errors.**

Use when each validation is independent. This provides a better UX by showing users all issues at once.

```csharp
// amount and currency validations are independent
// if both are wrong, both errors are returned
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => new Money(a, c));
```

**Comparison with real example:**

| Input | Bind Result | Apply Result |
|-------|------------|--------------|
| amount=-100, currency="" | "Amount must be positive" (1) | "Amount must be positive", "Currency code cannot be empty" (2) |

| Strategy | When to Use | Characteristic |
|----------|-------------|----------------|
| `Bind` / `Then*` | Dependencies between validations | Stops at first error |
| `Apply` | Independent validations | Collects all errors |

### Q4. How to access the Value property?

The `Value` property of `SimpleValueObject<T>` is declared `protected`, so it cannot be accessed directly from outside. This is intentional design -- it prevents using value objects "as if they were primitive values" and maintains type safety.

There are three ways to access the internal value externally:

```csharp
// Method 1: Define an implicit conversion operator (recommended)
// Email can be passed directly where a string is needed
public static implicit operator string(Email email) => email.Value;

string emailString = email;  // Implicit conversion
SendEmail(email);            // Passed directly to string parameter

// Method 2: Provide meaningful derived properties
// Properties with domain meaning are better than simply exposing Value
public string LocalPart { get; }   // "user" part of user@example.com
public string Domain { get; }      // "example.com" part of user@example.com

// Method 3: Override ToString()
// Useful for debugging or logging
public override string ToString() => Value;
```

**Note**: The implicit conversion in Method 1 is convenient, but overuse can weaken the type safety of value objects. Use only when truly necessary.

### Q5. When should SmartEnum be used?

C#'s basic `enum` is merely a simple integer constant. **Use `SmartEnum` when each value needs different properties or behavior.**

**When basic enum is sufficient:**

```csharp
// Simple status distinction only needed
public enum OrderStatus { Pending, Confirmed, Shipped, Delivered }
```

**When SmartEnum is needed:**

```csharp
// Each currency needs unique properties (symbol, name) and behavior (formatting)
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency KRW = new("KRW", "KRW", "₩", "Korean Won");
    public static readonly Currency USD = new("USD", "USD", "$", "US Dollar");

    public string Symbol { get; }
    public string DisplayName { get; }

    // Different behavior per value
    public string Format(decimal amount) => $"{Symbol}{amount:N2}";
}
```

| Scenario | Choice |
|----------|--------|
| Simple status/flag | Existing C# enum |
| Unique properties per value needed | SmartEnum |
| Different behavior per value needed | SmartEnum |
| Runtime type safety is important | SmartEnum |

### Q6. What is the difference between ValidationRules\<T\> and ValidationRules.For()?

Both provide the same validation methods (`NotEmpty`, `Positive`, etc.), but **where they get type information from** differs.

**ValidationRules\<T\> gets context from the "type."**

Used inside a Value Object class, the type is determined at compile time.

```csharp
// Inside the Price class
public static Validation<Error, decimal> Validate(decimal value) =>
    ValidationRules<Price>.Positive(value);
// Error code: DomainErrors.Price.NotPositive
```

**ValidationRules.For() gets context from a "string."**

Used when there is no Value Object (DTO validation, API input validation).

```csharp
// In DTO validation
var result = ValidationRules.For("ProductPrice").Positive(request.Price);
// Error code: DomainErrors.ProductPrice.NotPositive
```

**When to use which?**

```csharp
// Domain Layer: always use ValidationRules<T>
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Price>.Positive(value);  // Type safe
}

// Application/Presentation Layer: can use ValidationRules.For()
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        // DTO validation - validate directly without a Value Object
        RuleFor(x => x.Price)
            .Must(v => ValidationRules.For("Price").Positive(v).IsSuccess);
    }
}
```

| Property | `ValidationRules<T>` (Typed) | `ValidationRules.For()` (Contextual) |
|----------|------------------------------|--------------------------------------|
| Namespace | `Validations.Typed` | `Validations.Contextual` |
| Type source | Compile time (generic) | Runtime (string) |
| Recommended layer | Domain Layer | Presentation/Application Layer |
| Target | Value Object | DTO, API input, prototyping |
| Example | `ValidationRules<Price>.Positive(v)` | `ValidationRules.For("Price").Positive(v)` |

**Recommendations:**
- Always use `ValidationRules<T>` in the Domain Layer (type safety)
- Can use `ValidationRules.For()` for DTO or API input validation

---

## Validation Pipeline Roles and Responsibilities

Value object validation is divided among 4 roles. Each role is independent and ensures the DDD Always-Valid principle at different levels.

### Role 1: `Validate()` -- Domain Knowledge Container

- **Responsibility**: **Encapsulates domain knowledge** about "what constitutes a valid value"
- **Contents**: Normalization (Trim, ToLower) + structural validation (MaxLength, Matches)
- **Normalization placement rule**: After existence checks (NotNull, NotEmpty), before structural checks
- **Returns**: `Validation<Error, T>` (normalized primitive value)
- **Usage**: Inside `Create()`, Presentation Validator's `MustSatisfyValidation`

```csharp
public static Validation<Error, string> Validate(string? value) =>
    ValidationRules<ProductName>
        .NotNull(value)
        .ThenNotEmpty()              // Existence check
        .ThenNormalize(v => v.Trim()) // Normalization (right after existence check)
        .ThenMaxLength(MaxLength);    // Structural check (based on normalized value)
```

### Role 2: `Create()` -- Authoritative Factory (Always-Valid Guarantee)

- **Responsibility**: The **sole entry point** for creating valid, normalized value objects
- **Internal**: Calls `Validate()` -> constructs value object on success
- **Returns**: `Fin<VO>` -- a valid value object or error
- **Rationale**: "An always-valid domain model is the most fundamental principle" (Vladimir Khorikov)

### Role 3: Handler + ApplyT -- Domain Validation + Usecase Orchestration

- **Responsibility**: Value object creation (= domain validation) + business logic execution
- **ApplyT**: Applicatively composes multiple `Create()` results -> starts FinT LINQ chain
- **Key point**: This is not "re-validation" -- the handler creating value objects **is** the domain validation
- **Rationale**: "Commands carry primitive values, and handlers create value objects" (Vladimir Khorikov)

```csharp
FinT<IO, Response> usecase =
    from vos in (
        ProductName.Create(request.Name),
        Money.Create(request.Price)
    ).ApplyT((name, price) => (Name: name, Price: price))
    let product = Product.Create(vos.Name, vos.Price)
    from created in productRepository.Create(product)
    select new Response(...);
```

### Role 4: Presentation Validator -- Optional UX Convenience

- **Responsibility**: Provides fast validation feedback to API users (FluentValidation format)
- **Limitation**: Discards normalized results (only checks pass/fail)
- **Principle**: Removing it has no impact on domain correctness
- **Rationale**: "UI validation is for UX, domain validation is for correctness" (Microsoft .NET Architecture)

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // Reuse Validate() to check pass/fail only -- normalized results are discarded
        RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
        RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);
    }
}
```

### Flow Summary

```
Request(primitives) -> [Presentation Validator: UX feedback] -> Handler
                                                                  |
                                                       Create() via ApplyT
                                                       (domain validation + normalization + VO creation)
                                                                  |
                                                       Business logic + persistence
```

---

## References

- [Value Objects: Union Types](../05c-union-value-objects) - Discriminated Union patterns and state transitions
- [Error System: Basics and Naming](../08a-error-system) - Error handling principles and naming conventions
- [Error System: Domain/Application Errors](../08b-error-system-domain-app) - Domain/Application error definitions and test patterns
- [Unit Testing Guide](../testing/15a-unit-testing)
- [LanguageExt](https://github.com/louthy/language-ext)
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum)
