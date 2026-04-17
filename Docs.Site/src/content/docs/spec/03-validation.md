---
title: "Validation System Specification"
---

Functorium's validation system provides a functional API for Value Object and DTO validation. In the domain layer, `TypedValidation` and `ContextualValidation` compose type-safe validation chains. In the application layer, `FluentValidationExtensions` integrates Value Object validation logic into FluentValidation rules.

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `TypedValidation<TVO, T>` | `Domains.ValueObjects.Validations.Typed` | Wrapper that carries Value Object type information through chaining |
| `ValidationRules<TVO>` | `Domains.ValueObjects.Validations.Typed` | Validation entry point that specifies the type parameter once |
| `TypedValidationExtensions` | `Domains.ValueObjects.Validations.Typed` | `Then*` chaining extension methods |
| `ContextualValidation<T>` | `Domains.ValueObjects.Validations.Contextual` | Wrapper that carries context name through chaining |
| `ValidationRules` | `Domains.ValueObjects.Validations.Contextual` | `For(contextName)` Named Context entry point |
| `ValidationContext` | `Domains.ValueObjects.Validations.Contextual` | Named Context validation rule instance methods |
| `ContextualValidationExtensions` | `Domains.ValueObjects.Validations.Contextual` | `Then*` chaining extension methods (Contextual) |
| `IValidationContext` | `Domains.ValueObjects.Validations` | Reusable validation context marker for Application Layer |
| `ValidationApplyExtensions` | `Domains.ValueObjects.Validations` | `Validation<Error, T>` Tuple Apply (2~5-tuple) |
| `FinApplyExtensions` | `Domains.ValueObjects.Validations` | `Fin<T>` Tuple Apply (2~5-tuple) |
| `FluentValidationExtensions` | `Applications.Validations` | FluentValidation + Value Object Validate integration |

### Key Concepts

| Concept | Description |
|------|------|
| Typed Validation | `ValidationRules<TVO>.Rule(value)` form automatically includes Value Object type in error messages |
| Contextual Validation | `ValidationRules.For("Name").Rule(value)` form includes string context in error messages |
| `Then*` chaining | Sequential validation chain (stops at first error, `Bind`-based) |
| `Apply` merging | Performs independent validations in parallel, accumulating all errors |
| FluentValidation integration | Converts Value Object `Validate()` results into `IRuleBuilder` rules |

---

## TypedValidation vs ContextualValidation Comparison

Both validation approaches provide the same rule catalog but differ in how they identify the error source.

| Aspect | TypedValidation | ContextualValidation |
|------|----------------|---------------------|
| **Entry point** | `ValidationRules<TVO>.Rule(value)` | `ValidationRules.For("ctx").Rule(value)` |
| **Error source** | `typeof(TVO).Name` (compile-time type) | `contextName` (runtime string) |
| **Wrapper** | `TypedValidation<TVO, T>` | `ContextualValidation<T>` |
| **Chaining** | `.ThenRule()` | `.ThenRule()` |
| **Apply** | 2~4-tuple Apply supported | 2~4-tuple Apply supported |
| **Recommended layer** | Domain Layer (inside Value Objects) | Presentation Layer, rapid prototyping |
| **DomainError factory** | `DomainError.For<TVO>(...)` | `DomainError.ForContext(...)` |

```csharp
// TypedValidation: inside Value Object
public static Validation<Error, ProductName> Validate(string value) =>
    ValidationRules<ProductName>.NotEmpty(value)
        .ThenMinLength(3)
        .ThenMaxLength(100)
        .Value;

// ContextualValidation: Named Context
ValidationRules.For("ProductName")
    .NotEmpty(name)
    .ThenMinLength(3)
    .ThenMaxLength(100);
```

Implementing the `IValidationContext` marker interface allows defining reusable validation context classes in the Application Layer.

```csharp
// Application Layer validation context class
public sealed class ProductValidation : IValidationContext;

// Usage: same API as TypedValidation
ValidationRules<ProductValidation>.Positive(amount);
// Error: DomainErrors.ProductValidation.NotPositive
```

---

## TypedValidation\<TVO, T\>

### Struct Definition

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    // Implicit conversion to Validation<Error, T>
    public static implicit operator Validation<Error, T>(
        TypedValidation<TValueObject, T> typed) => typed.Value;
}
```

- `TValueObject`: Value Object type (type name included in error messages)
- `T`: Type of the value being validated
- Extract `Validation<Error, T>` via the `Value` property or implicit conversion

### ValidationRules\<TVO\> Entry Point Methods

The `ValidationRules<TValueObject>` static class provides entry points for validation chains. All methods return `TypedValidation<TValueObject, T>`.

---

## Rule Catalog

### Presence

Entry points (`ValidationRules<TVO>`):

| Method | Signature | Description |
|--------|---------|------|
| `NotNull` | `NotNull<T>(T? value) where T : class` | Reference type null check |
| `NotNull` | `NotNull<T>(T? value) where T : struct` | Nullable value type null check |

Chaining (`TypedValidationExtensions`):

| Method | Signature | Description |
|--------|---------|------|
| `ThenNotNull` | `ThenNotNull<TVO, T>(this TypedValidation<TVO, T?>) where T : class` | Reference type null check |
| `ThenNotNull` | `ThenNotNull<TVO, T>(this TypedValidation<TVO, T?>) where T : struct` | Nullable value type null check |

**DomainErrorType:** `Null()`

### Length (String Length)

Entry points:

| Method | Signature | Description |
|--------|---------|------|
| `NotEmpty` | `NotEmpty(string value)` | Whitespace string check (`IsNullOrWhiteSpace`) |
| `MinLength` | `MinLength(string value, int minLength)` | Minimum length |
| `MaxLength` | `MaxLength(string value, int maxLength)` | Maximum length |
| `ExactLength` | `ExactLength(string value, int length)` | Exact length |

Chaining:

| Method | Signature | Description |
|--------|---------|------|
| `ThenNotEmpty` | `ThenNotEmpty<TVO>(this TypedValidation<TVO, string>)` | Whitespace string check |
| `ThenMinLength` | `ThenMinLength<TVO>(this TypedValidation<TVO, string>, int)` | Minimum length |
| `ThenMaxLength` | `ThenMaxLength<TVO>(this TypedValidation<TVO, string>, int)` | Maximum length |
| `ThenExactLength` | `ThenExactLength<TVO>(this TypedValidation<TVO, string>, int)` | Exact length |
| `ThenNormalize` | `ThenNormalize<TVO>(this TypedValidation<TVO, string>, Func<string, string>)` | String transformation (normalization) |

**DomainErrorType:** `Empty()`, `TooShort(minLength)`, `TooLong(maxLength)`, `WrongLength(length)`

### Numeric

Entry points (`where T : notnull, INumber<T>`):

| Method | Signature | Description |
|--------|---------|------|
| `NotZero` | `NotZero<T>(T value)` | Not zero check |
| `NonNegative` | `NonNegative<T>(T value)` | Not negative check (>= 0) |
| `Positive` | `Positive<T>(T value)` | Positive check (> 0) |
| `Between` | `Between<T>(T value, T min, T max)` | Range check |
| `AtMost` | `AtMost<T>(T value, T max)` | Maximum value check |
| `AtLeast` | `AtLeast<T>(T value, T min)` | Minimum value check |

Chaining (`where T : notnull, INumber<T>`):

| Method | Signature | Description |
|--------|---------|------|
| `ThenNotZero` | `ThenNotZero<TVO, T>(this TypedValidation<TVO, T>)` | Not zero check |
| `ThenNonNegative` | `ThenNonNegative<TVO, T>(this TypedValidation<TVO, T>)` | Not negative check |
| `ThenPositive` | `ThenPositive<TVO, T>(this TypedValidation<TVO, T>)` | Positive check |
| `ThenBetween` | `ThenBetween<TVO, T>(this TypedValidation<TVO, T>, T min, T max)` | Range check |
| `ThenAtMost` | `ThenAtMost<TVO, T>(this TypedValidation<TVO, T>, T max)` | Maximum value check |
| `ThenAtLeast` | `ThenAtLeast<TVO, T>(this TypedValidation<TVO, T>, T min)` | Minimum value check |

**DomainErrorType:** `Zero()`, `Negative()`, `NotPositive()`, `OutOfRange(min, max)`, `AboveMaximum(max)`, `BelowMinimum(min)`

### Format

Entry points:

| Method | Signature | Description |
|--------|---------|------|
| `Matches` | `Matches(string value, Regex pattern, string? message = null)` | Regex pattern match |
| `IsUpperCase` | `IsUpperCase(string value)` | Uppercase check |
| `IsLowerCase` | `IsLowerCase(string value)` | Lowercase check |

Chaining:

| Method | Signature | Description |
|--------|---------|------|
| `ThenMatches` | `ThenMatches<TVO>(this TypedValidation<TVO, string>, Regex, string?)` | Regex pattern match |
| `ThenIsUpperCase` | `ThenIsUpperCase<TVO>(this TypedValidation<TVO, string>)` | Uppercase check |
| `ThenIsLowerCase` | `ThenIsLowerCase<TVO>(this TypedValidation<TVO, string>)` | Lowercase check |

**DomainErrorType:** `InvalidFormat(pattern)`, `NotUpperCase()`, `NotLowerCase()`

> The `pattern` parameter of `ThenMatches` is of type `Regex`. Using `[GeneratedRegex]` patterns is recommended for performance.

### DateTime

Entry points:

| Method | Signature | Description |
|--------|---------|------|
| `NotDefault` | `NotDefault(DateTime value)` | Not `DateTime.MinValue` check |
| `InPast` | `InPast(DateTime value)` | Past date check |
| `InFuture` | `InFuture(DateTime value)` | Future date check |
| `Before` | `Before(DateTime value, DateTime boundary)` | Before boundary date check |
| `After` | `After(DateTime value, DateTime boundary)` | After boundary date check |
| `DateBetween` | `DateBetween(DateTime value, DateTime min, DateTime max)` | Date range check |

Chaining:

| Method | Signature | Description |
|--------|---------|------|
| `ThenNotDefault` | `ThenNotDefault<TVO>(this TypedValidation<TVO, DateTime>)` | Default value check |
| `ThenInPast` | `ThenInPast<TVO>(this TypedValidation<TVO, DateTime>)` | Past date check |
| `ThenInFuture` | `ThenInFuture<TVO>(this TypedValidation<TVO, DateTime>)` | Future date check |
| `ThenBefore` | `ThenBefore<TVO>(this TypedValidation<TVO, DateTime>, DateTime)` | Before boundary check |
| `ThenAfter` | `ThenAfter<TVO>(this TypedValidation<TVO, DateTime>, DateTime)` | After boundary check |
| `ThenDateBetween` | `ThenDateBetween<TVO>(this TypedValidation<TVO, DateTime>, DateTime, DateTime)` | Date range check |

**DomainErrorType:** `DefaultDate()`, `NotInPast()`, `NotInFuture()`, `TooLate(boundary)`, `TooEarly(boundary)`, `OutOfRange(min, max)`

### Range (Range Pair)

Entry points (`where TValue : notnull, IComparable<TValue>`):

| Method | Signature | Description |
|--------|---------|------|
| `ValidRange` | `ValidRange<TValue>(TValue min, TValue max)` | min <= max check |
| `ValidStrictRange` | `ValidStrictRange<TValue>(TValue min, TValue max)` | min < max check (empty range not allowed) |

Chaining:

| Method | Signature | Description |
|--------|---------|------|
| `ThenValidRange` | `ThenValidRange<TVO, TValue>(this TypedValidation<TVO, (TValue, TValue)>)` | min <= max check |
| `ThenValidStrictRange` | `ThenValidStrictRange<TVO, TValue>(this TypedValidation<TVO, (TValue, TValue)>)` | min < max check |

Return type is `TypedValidation<TVO, (TValue Min, TValue Max)>`.

**DomainErrorType:** `RangeInverted(min, max)`, `RangeEmpty(value)` (StrictRange only)

### Collection

Entry points:

| Method | Signature | Description |
|--------|---------|------|
| `NotEmptyArray` | `NotEmptyArray<TElement>(TElement[]? value)` | Array is not null and not empty check |

Chaining:

| Method | Signature | Description |
|--------|---------|------|
| `ThenNotEmptyArray` | `ThenNotEmptyArray<TVO, TElement>(this TypedValidation<TVO, TElement[]>)` | Array not empty check |

**DomainErrorType:** `Empty()`

### Generic (User-Defined)

Entry points:

| Method | Signature | Description |
|--------|---------|------|
| `Must` | `Must<T>(T value, Func<T, bool> predicate, DomainErrorType errorType, string message) where T : notnull` | User-defined condition validation |

Chaining:

| Method | Signature | Description |
|--------|---------|------|
| `ThenMust` | `ThenMust<TVO, T>(this TypedValidation<TVO, T>, Func<T, bool>, DomainErrorType, string)` | User-defined condition |
| `ThenMust` | `ThenMust<TVO, T>(this TypedValidation<TVO, T>, Func<T, bool>, DomainErrorType, Func<T, string>)` | Message factory overload |

```csharp
ValidationRules<Discount>.Must(
    rate,
    r => r <= 100m,
    new DomainErrorType.BusinessRule("MaxDiscount"),
    $"Discount rate must not exceed 100%. Current: {rate}%");
```

### LINQ Support

`TypedValidation` supports LINQ query expressions.

| Method | Description |
|--------|------|
| `Select` | Value transformation (`Map`) |
| `SelectMany` (TypedValidation -> Validation) | Chaining via `from...in` syntax |
| `SelectMany` (TypedValidation -> TypedValidation) | Chaining within the same TVO type |
| `ToValidation` | Explicit `Validation<Error, T>` conversion |

```csharp
// LINQ query expression
from validStart in ValidationRules<DateRange>.NotDefault(startDate)
from validEnd in ValidationRules<DateRange>.NotDefault(endDate)
select (validStart, validEnd);
```

---

## ContextualValidation\<T\>

### Struct Definition

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

### ValidationRules.For() Entry Point

```csharp
public static class ValidationRules
{
    public static ValidationContext For(string contextName) => new(contextName);
}
```

`ValidationContext` provides the same rule catalog as `ValidationRules<TVO>` as instance methods. All rule error messages use `ContextName` instead of `typeof(TVO).Name`.

### ValidationContext Instance Methods

Entry point methods provided by `ValidationContext`. Rules per category are identical to TypedValidation.

| Category | Methods |
|---------|--------|
| Presence | `NotNull<T>` |
| Length | `NotEmpty`, `MinLength`, `MaxLength`, `ExactLength` |
| Numeric | `NotZero<T>`, `NonNegative<T>`, `Positive<T>`, `Between<T>`, `AtMost<T>`, `AtLeast<T>` |
| Format | `Matches`, `IsUpperCase`, `IsLowerCase` |
| DateTime | `NotDefault`, `InPast`, `InFuture`, `Before`, `After`, `DateBetween` |
| Generic | `Must<T>` |

### ContextualValidationExtensions Chaining

`Then*` chaining methods provided by `ContextualValidationExtensions`. The context name is automatically propagated.

| Category | Methods |
|---------|--------|
| Presence | `ThenNotNull<T>` |
| Length | `ThenNotEmpty`, `ThenMinLength`, `ThenMaxLength`, `ThenExactLength`, `ThenNormalize` |
| Numeric | `ThenNotZero<T>`, `ThenNonNegative<T>`, `ThenPositive<T>`, `ThenBetween<T>`, `ThenAtMost<T>`, `ThenAtLeast<T>` |

```csharp
// Named Context chaining example
ValidationRules.For("Price")
    .Positive(amount)
    .ThenAtMost(1_000_000m);

// Named Context Apply example
(ValidationRules.For("Amount").Positive(amount),
 ValidationRules.For("Currency").NotEmpty(currency))
    .Apply((a, c) => new Money(a, c));
```

---

## Apply Composition

### Validation\<Error, T\> Tuple Apply

`ValidationApplyExtensions` provides `Apply` extension methods for `Validation<Error, T>` tuples. It solves the issue where LanguageExt's generic Apply returns `K<Validation<Error>, T>`, returning a concrete `Validation<Error, R>` without needing `.As()` calls.

```csharp
// Signature (2-tuple example)
public static Validation<Error, R> Apply<T1, T2, R>(
    this (Validation<Error, T1> v1, Validation<Error, T2> v2) tuple,
    Func<T1, T2, R> f)
```

| Tuple Size | Support |
|-----------|------|
| 2-tuple | `(v1, v2).Apply((a, b) => ...)` |
| 3-tuple | `(v1, v2, v3).Apply((a, b, c) => ...)` |
| 4-tuple | `(v1, v2, v3, v4).Apply((a, b, c, d) => ...)` |
| 5-tuple | `(v1, v2, v3, v4, v5).Apply((a, b, c, d, e) => ...)` |

### TypedValidation Tuple Apply

`TypedValidationExtensions.Apply` provides overloads that freely mix `TypedValidation` and `Validation`.

| Tuple Size | Combination Patterns |
|-----------|---------|
| 2-tuple | TT, TV, VT |
| 3-tuple | TTT, VVT, TVV, VTV, TTV, TVT, VTT |
| 4-tuple | TTTT, TVVV, VTVV, VVTV, VVVT |

> T = TypedValidation, V = Validation

```csharp
// TypedValidation + Validation mix
(ValidationRules<Money>.NonNegative(amount),
 ValidationRules<Money>.NotEmpty(currency))
    .Apply((a, c) => new Money(a, c));
```

### ContextualValidation Tuple Apply

`ContextualValidationExtensions.Apply` provides overloads with the same pattern for mixing `ContextualValidation` and `Validation`. Tuple sizes and combination patterns are identical to TypedValidation Apply (2~4-tuple).

### Fin\<T\> Tuple Apply

`FinApplyExtensions` internally converts `Fin<T>` tuples to `Validation<Error, T>`, performs Apply, then converts the result back to `Fin<R>`.

```csharp
// Signature (2-tuple example)
public static Fin<R> Apply<T1, T2, R>(
    this (Fin<T1> v1, Fin<T2> v2) tuple,
    Func<T1, T2, R> f)
```

| Tuple Size | Support |
|-----------|------|
| 2-tuple | `(fin1, fin2).Apply((a, b) => ...)` |
| 3-tuple | `(fin1, fin2, fin3).Apply((a, b, c) => ...)` |
| 4-tuple | `(fin1, fin2, fin3, fin4).Apply((a, b, c, d) => ...)` |
| 5-tuple | `(fin1, fin2, fin3, fin4, fin5).Apply((a, b, c, d, e) => ...)` |

```csharp
// Fin Apply example
(PersonalName.Create("HyungHo", "Ko"),
 EmailAddress.Create("user@example.com"))
    .Apply((name, email) => Contact.Create(name, email, now));
```

---

## FluentValidation Integration

`FluentValidationExtensions` provides extension methods that integrate Value Object `Validate()` methods into FluentValidation rules. When validation fails, errors implementing the `IHasErrorCode` interface generate error messages in `[ErrorCode] Message` format.

### MustSatisfyValidation

Used when the input type and validation result type are the same. C# 14 extension members syntax enables automatic type inference.

```csharp
public IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValidation(
    Func<TProperty, Validation<Error, TProperty>> validationMethod)
```

```csharp
RuleFor(x => x.Price)
    .MustSatisfyValidation(Money.ValidateAmount);

RuleFor(x => x.Currency)
    .MustSatisfyValidation(Money.ValidateCurrency);
```

### MustSatisfyValidationOf\<TValueObject\>

Used when the input type and validation result type differ. Only the `TValueObject` type needs to be specified.

```csharp
public IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValidationOf<TValueObject>(
    Func<TProperty, Validation<Error, TValueObject>> validationMethod)
```

```csharp
// string -> Validation<Error, ProductName>
RuleFor(x => x.Name)
    .MustSatisfyValidationOf<ProductName>(ProductName.Validate);
```

> When calling methods with additional generic parameters from `IRuleBuilderInitial`, C# 14 extension members type inference limitations may occur. In this case, use the traditional extension method overload (`MustSatisfyValidationOf<TRequest, TProperty, TValueObject>`).

### MustBeEntityId\<TEntityId\>

String validation for EntityId types implementing `IEntityId<TEntityId>`. Combines `NotEmpty` + `TryParse` into a single rule.

```csharp
public static IRuleBuilderOptions<TRequest, string> MustBeEntityId<TRequest, TEntityId>(
    this IRuleBuilder<TRequest, string> ruleBuilder)
    where TEntityId : struct, IEntityId<TEntityId>
```

```csharp
RuleFor(x => x.ProductId)
    .MustBeEntityId<CreateProductRequest, ProductId>();
```

### MustBeEnum (SmartEnum)

Validation for `Ardalis.SmartEnum` types. Provides three overloads for Value, Name, and string Value.

| Method | Signature | Description |
|--------|---------|------|
| `MustBeEnum<TSmartEnum, TValue>` | `IRuleBuilder<TReq, TValue>` | Validate by Value |
| `MustBeEnum<TSmartEnum>` (int) | `IRuleBuilder<TReq, int>` | Simplified int Value overload |
| `MustBeEnumName<TSmartEnum, TValue>` | `IRuleBuilder<TReq, string>` | Validate by Name |
| `MustBeEnumValue<TSmartEnum>` (string) | `IRuleBuilder<TReq, string>` | Validate by string Value (case-insensitive) |

```csharp
RuleFor(x => x.CurrencyCode)
    .MustBeEnumValue<CreateMoneyRequest, Currency>();

RuleFor(x => x.Status)
    .MustBeEnum<UpdateOrderRequest, OrderStatus>();
```

### MustBeOneOf

Validates that a value is one of the allowed string values. Case-insensitive, and null or empty strings skip validation.

```csharp
public static IRuleBuilderOptions<TRequest, string> MustBeOneOf<TRequest>(
    this IRuleBuilder<TRequest, string> ruleBuilder,
    string[] allowedValues)
```

```csharp
RuleFor(x => x.SortBy)
    .MustBeOneOf(["name", "price", "date"]);
```

### Option\<T\> Validation

Validation for `Option<TProperty>` properties. Skips validation if `None`, extracts and validates the inner value if `Some`.

| Method | Description |
|--------|------|
| `MustSatisfyValidation` | Input type == result type |
| `MustSatisfyValidationOf<TValueObject>` | Input type != result type |

```csharp
// Option<decimal> -> skip if None, validate if Some(100m)
RuleFor(x => x.MinPrice)
    .MustSatisfyValidation(Money.Validate);

// Option<string> -> skip if None, validate if Some("name")
RuleFor(x => x.Name)
    .MustSatisfyValidationOf<ProductName>(ProductName.Validate);
```

### MustBePairedRange

Validates paired range filters where two `Option` fields must be provided together, in a single call.

```csharp
public static void MustBePairedRange<TRequest, T>(
    this AbstractValidator<TRequest> validator,
    Expression<Func<TRequest, Option<T>>> minExpr,
    Expression<Func<TRequest, Option<T>>> maxExpr,
    Func<T, Validation<Error, T>> validate,
    bool inclusive = false)
    where T : IComparable<T>
```

**Validation logic:**

1. Both `None` -- pass (filter not applied)
2. Only one `Some` -- fail ("MaxPrice is required when MinPrice is specified")
3. Both `Some` -- run `validate` on each + range comparison

```csharp
// Default: max > min (exclusive)
this.MustBePairedRange(
    x => x.MinPrice,
    x => x.MaxPrice,
    Money.Validate);

// Custom: max >= min (inclusive)
this.MustBePairedRange(
    x => x.MinPrice,
    x => x.MaxPrice,
    Money.Validate,
    inclusive: true);
```

---

## Related Documents

| Document | Description |
|------|------|
| [Value Object: Enumeration/Validation/Practical Patterns](../guides/domain/05b-value-objects-validation) | Apply merging, chaining patterns, SmartEnum Create guide |
| [Value Object Base Classes](../guides/domain/05a-value-objects) | `SimpleValueObject<T>`, `ValueObject`, Create patterns |
| [Error System Specification](../04-error-system) | `DomainErrorType`, `DomainError.For<T>()`, `DomainError.ForContext()` |
| [Value Object Specification](../02-value-object) | `ValueObject`, `SimpleValueObject<T>`, Union types |
