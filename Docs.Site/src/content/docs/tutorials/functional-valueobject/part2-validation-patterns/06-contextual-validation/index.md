---
title: "Contextual Validation"
---

## Overview

In Part 2, chapters 1-5, we learned **type-based** validation like `ValidationRules<Price>.NotEmpty(value)`. This pattern is ideal for validating Value Objects in the domain layer -- the error message automatically includes the type information `Price`.

But the situation is different when validating DTOs in the Application layer. If the `Price` field and `ShippingCost` field of `CreateOrderCommand` are both `decimal`, type information alone cannot distinguish which field produced the error.

`ContextualValidation` resolves this problem by **including the field name (context)** in the error.

> **"In the Domain Layer, the type is the context; in the Application Layer, the field name is the context."**

## Learning Objectives

1. **`ValidationRules.For("fieldName")`** -- Named Context validation entry point
2. **`ContextualValidation<T>`** -- wrapper that propagates context during chaining
3. **TypedValidation vs ContextualValidation** comparison
4. **Apply composition** for multi-field parallel validation

### What you will verify through practice
- **PhoneNumber**: Single field validation -- NotNull -> NotEmpty -> MinLength -> MaxLength
- **Address**: Multi-field parallel validation -- City + Street + PostalCode combined with Apply

## TypedValidation vs ContextualValidation

| Feature | TypedValidation | ContextualValidation |
|------|-----------------|---------------------|
| **Entry point** | `ValidationRules<Price>.NotEmpty(value)` | `ValidationRules.For("price").NotEmpty(value)` |
| **Context** | Type name (`Price`) | Field name (`"price"`) |
| **Error identification** | `DomainError.For<Price>(...)` | `DomainError.ForContext("price", ...)` |
| **Primary usage** | Domain Layer -- Value Object validation | Application Layer -- DTO/Command validation |
| **Advantage** | Compile time type safety | Dynamic field name specification |

They share the same validation rule categories (Presence, Length, Numeric, Format, DateTime).

## Code Structure

### ValidationRules.For -- Entry Point

```csharp
// Start named context validation
ValidationContext context = ValidationRules.For("PhoneNumber");

// Chaining -- context is automatically propagated
ContextualValidation<string> result = context
    .NotNull(phoneNumber)   // Returns ContextualValidation<string>
    .ThenNotEmpty()         // Chain with Then* methods
    .ThenMinLength(10)
    .ThenMaxLength(15);

// Implicit conversion to Validation<Error, string>
Validation<Error, string> validation = result;
```

### ContextualValidation<T> -- Context Propagation Wrapper

```csharp
public readonly struct ContextualValidation<T>
{
    public Validation<Error, T> Value { get; }
    public string ContextName { get; }

    // Implicit conversion to Validation<Error, T>
    public static implicit operator Validation<Error, T>(
        ContextualValidation<T> contextual) => contextual.Value;
}
```

`ContextualValidation<T>` wraps `Validation<Error, T>` while passing `ContextName` to chaining methods. The chaining methods like `ThenNotEmpty()`, `ThenMinLength()`, etc. use this context to generate error messages.

### Single Field Validation

```csharp
public static Validation<Error, string> Validate(string? phoneNumber)
    => ValidationRules.For("PhoneNumber")
        .NotNull(phoneNumber)
        .ThenNotEmpty()
        .ThenMinLength(10)
        .ThenMaxLength(15);
// On failure, error: "PhoneNumber cannot be null."
```

### Multi-Field Apply Composition

```csharp
public static Validation<Error, AddressDto> Validate(
    string? city, string? street, string? postalCode)
    => (ValidateCity(city), ValidateStreet(street), ValidatePostalCode(postalCode))
        .Apply((c, s, p) => new AddressDto(c, s, p));

private static Validation<Error, string> ValidateCity(string? value)
    => ValidationRules.For("City")
        .NotNull(value)
        .ThenNotEmpty()
        .ThenMaxLength(100);
```

Each field is validated independently, and **all errors are collected at once**. If City is null and PostalCode is too short, both errors are returned.

## Validation Rule Categories

`ValidationContext` provides the same rule categories as TypedValidation:

| Category | Method examples | Target |
|---------|-----------|------|
| **Presence** | `NotNull()` | All types |
| **Length** | `NotEmpty()`, `MinLength()`, `MaxLength()`, `ExactLength()` | string |
| **Numeric** | `NotZero()`, `Positive()`, `NonNegative()`, `Between()` | INumber\<T\> |
| **Format** | `Matches(Regex)`, `IsUpperCase()`, `IsLowerCase()` | string |
| **DateTime** | `NotDefault()`, `InPast()`, `InFuture()`, `Before()` | DateTime |
| **Custom** | `Must(predicate, errorType, message)` | All types |

## Summary at a Glance

| Component | Role |
|-----------|------|
| `ValidationRules.For(name)` | Starts named context validation, returns `ValidationContext` |
| `ValidationContext` | Holds context name, provides validation methods |
| `ContextualValidation<T>` | Propagates validation result + context name during chaining |
| `Then*` chaining | `ThenNotEmpty()`, `ThenMinLength()` etc. -- Bind-based sequential validation |
| `Apply` composition | Combines parallel validation results of 2-4 fields |

## FAQ

### Q1: How does this differ from FluentValidation?
**A**: FluentValidation defines Validators at the class level and injects via DI. ContextualValidation is functional -- based on the `Validation<Error, T>` monad, it can be composed with other validation results using Apply/Bind. The two approaches can coexist.

### Q2: Can ContextualValidation be used in the Domain Layer?
**A**: It is possible but not recommended. In the Domain Layer, using type-based validation with `ValidationRules<Price>.NotEmpty(value)` aligns with DDD principles. ContextualValidation is suitable for Application/Presentation Layers where Value Objects are not present.

### Q3: Can error messages be customized?
**A**: You can specify custom messages with `Must(predicate, errorType, message)` custom validation. Built-in rules (NotNull, NotEmpty, etc.) generate standardized messages that automatically include the context name.

---

ContextualValidation provides **the same functional composition as TypedValidation** on a field-name basis when validating DTO fields in the Application Layer.

In Part 2, we have learned all the validation patterns. In Part 3, we assemble these concepts into framework base classes from the Functorium framework to complete value object patterns ready for practical use.

-> [Part 3, Chapter 1: SimpleValueObject](../../Part3-ValueObject-Patterns/01-SimpleValueObject/)
