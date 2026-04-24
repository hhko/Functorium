---
title: "Error System — Domain/Application Errors"
---

This document covers error definitions and test patterns for the Domain/Application/Event layers. For basic principles and naming rules of error handling, refer to [08a-error-system.md](../08a-error-system). For Adapter errors, Custom errors, testing best practices, and per-layer checklists, refer to [08c-error-system-adapter-testing.md](../08c-error-system-adapter-testing).

## Introduction

[08a-error-system.md](../08a-error-system) covered the fundamentals and naming rules of the error system. This document examines Domain and Application layer error definitions, factory method usage, and test assertion patterns in detail.

> Each layer's error factory (`DomainError.For`, `ApplicationError.For`, `EventError.For`) explicitly identifies the error source in the type system, making it immediately clear which layer the problem originated from based on the error code alone.

## Summary

### Key Commands

```csharp
// Domain error
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
DomainError.For<Age, int>(new Negative(), value, "Age cannot be negative");

// Application error
ApplicationError.For<CreateProductCommand>(new AlreadyExists(), code, "Already exists");

// Event error
EventError.For<DomainEventPublisher>(new PublishFailed(), eventType, "Failed to publish event");

// Test assertions
result.ShouldBeDomainError<Email, Email>(new DomainErrorKind.Empty());
fin.ShouldBeApplicationError<GetProductQuery, Product>(new ApplicationErrorKind.NotFound());
```

### Key Procedures

1. Determine which layer the error originates from (Domain / Application / Event)
2. Select a standard error type or define a Custom sealed record
3. Create the error using the layer factory (`DomainError.For`, `ApplicationError.For`, `EventError.For`)
4. Write tests using assertion methods from the `Functorium.Testing.Assertions.Errors` namespace

### Key Concepts

| Layer | Factory | Error Code Prefix | When to Use |
|--------|--------|-----------------|----------|
| Domain | `DomainError` | `Domain.` | VO validation, Entity invariants, Aggregate rules |
| Application | `ApplicationError` | `Application.` | Usecase business logic, authorization/authentication |
| Event | `EventError` | `Application.` | Event publishing/handler failures |

We first examine Domain error creation and test patterns, then move on to Application errors and Event errors.

---

## Domain Errors

### Error Creation and Return

Use `DomainError.For<T>()` to create errors for Value Object validation or Entity invariant violations. The examples below show the overload differences based on the number of type parameters.

```csharp
using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorKind;

// Basic usage - return directly via implicit conversion
public Fin<Email> Create(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return DomainError.For<Email>(
            new Empty(),
            currentValue: value ?? "",
            message: "Email cannot be empty");

    return new Email(value);
}

// Generic value type
public Fin<Age> Create(int value)
{
    if (value < 0)
        return DomainError.For<Age, int>(
            new Negative(),
            currentValue: value,
            message: "Age cannot be negative");

    return new Age(value);
}

// Two values included
// Error type definition: public sealed record InvalidRange : DomainErrorKind.Custom;
public Fin<DateRange> Create(DateTime start, DateTime end)
{
    if (start >= end)
        return DomainError.For<DateRange, DateTime, DateTime>(
            new InvalidRange(),
            start, end,
            message: "Start date must be before end date");

    return new DateRange(start, end);
}

// Three values included
// Error type definition: public sealed record InvalidTriangle : DomainErrorKind.Custom;
public Fin<Triangle> Create(double a, double b, double c)
{
    if (a + b <= c || b + c <= a || c + a <= b)
        return DomainError.For<Triangle, double, double, double>(
            new InvalidTriangle(),
            a, b, c,
            message: "Cannot form a valid triangle");

    return new Triangle(a, b, c);
}
```

### Returning Errors from Entity Methods

```csharp
public sealed class Product : AggregateRoot<ProductId>
{
    public sealed record InsufficientStock : DomainErrorKind.Custom;

    public Fin<Unit> DeductStock(Quantity quantity)
    {
        if ((int)quantity > (int)StockQuantity)
            return DomainError.For<Product, int>(
                new InsufficientStock(),
                currentValue: (int)StockQuantity,
                message: $"Insufficient stock. Current: {(int)StockQuantity}, Requested: {(int)quantity}");

        StockQuantity = Quantity.Create((int)StockQuantity - (int)quantity).ThrowIfFail();
        AddDomainEvent(new StockDeductedEvent(Id, quantity));
        return unit;
    }
}
```

### DomainErrorKind Category Structure and Complete List

The following table categorizes `DomainErrorKind` by category and lists the files where each error type is defined.

| Category | File | Description |
|------|------|------|
| Presence | `DomainErrorKind.Presence.cs` | Value existence validation |
| Length | `DomainErrorKind.Length.cs` | String/collection length validation |
| Format | `DomainErrorKind.Format.cs` | Format and case validation |
| DateTime | `DomainErrorKind.DateTime.cs` | Date validation |
| Numeric | `DomainErrorKind.Numeric.cs` | Numeric value/range validation |
| Range | `DomainErrorKind.Range.cs` | min/max pair validation |
| Existence | `DomainErrorKind.Existence.cs` | Existence validation |
| Custom | `DomainErrorKind.Custom.cs` | Custom errors |

#### Presence (Value Existence Validation) - R1

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Empty` | Is empty (null, empty string, empty collection) | `new Empty()` |
| `Null` | Is null | `new Null()` |

#### Length (String/Collection Length Validation) - R2, R6

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `TooShort` | Below minimum length | `new TooShort(MinLength: 8)` |
| `TooLong` | Exceeds maximum length | `new TooLong(MaxLength: 100)` |
| `WrongLength` | Exact length mismatch | `new WrongLength(Expected: 10)` |

#### Format (Format Validation) - R3, R5

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `InvalidFormat` | Format mismatch | `new InvalidFormat(Pattern: @"^\d{3}-\d{4}$")` |
| `NotUpperCase` | Not uppercase | `new NotUpperCase()` |
| `NotLowerCase` | Not lowercase | `new NotLowerCase()` |

#### DateTime (Date Validation) - R1, R2, R3

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `DefaultDate` | Date is default value (DateTime.MinValue) | `new DefaultDate()` |
| `NotInPast` | Date should be in past but is in future | `new NotInPast()` |
| `NotInFuture` | Date should be in future but is in past | `new NotInFuture()` |
| `TooLate` | Date is later than boundary (should be before) | `new TooLate(Boundary: "2025-12-31")` |
| `TooEarly` | Date is earlier than boundary (should be after) | `new TooEarly(Boundary: "2020-01-01")` |

#### Numeric (Numeric Validation) - R1, R2, R3

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Zero` | Is zero | `new Zero()` |
| `Negative` | Is negative | `new Negative()` |
| `NotPositive` | Not positive (includes 0) | `new NotPositive()` |
| `OutOfRange` | Out of range | `new OutOfRange(Min: "1", Max: "100")` |
| `BelowMinimum` | Below minimum | `new BelowMinimum(Minimum: "0")` |
| `AboveMaximum` | Exceeds maximum | `new AboveMaximum(Maximum: "1000")` |

#### Range (Range Pair Validation) - R1

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `RangeInverted` | Range is inverted (min is greater than max) | `new RangeInverted(Min: "10", Max: "1")` |
| `RangeEmpty` | Range is empty (min == max, strict range) | `new RangeEmpty(Value: "5")` |

#### Existence (Existence Validation) - R1, R3, R4

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `NotFound` | Not found | `new NotFound()` |
| `AlreadyExists` | Already exists | `new AlreadyExists()` |
| `Duplicate` | Duplicated | `new Duplicate()` |
| `Mismatch` | Value mismatch | `new Mismatch()` |

#### Custom (Custom Errors)

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Custom` | Domain-specific error (abstract) | `sealed record AlreadyShipped : DomainErrorKind.Custom;` -> `new AlreadyShipped()` |

### Value Object Usage Example

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$");
    private const int MaxLength = 254;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(MaxLength);
}
```

### Domain Error Testing

Test assertion namespace:

```csharp
using Functorium.Testing.Assertions.Errors;
```

#### Error Validation

How `ShouldBeDomainError` assertion's type parameter specifies the error source type.

```csharp
// Basic error type verification
[Fact]
public void ShouldBeDomainError_WhenValueIsEmpty()
{
    // Arrange
    var error = DomainError.For<Email>(
        new DomainErrorKind.Empty(),
        currentValue: "",
        message: "Email cannot be empty");

    // Act & Assert
    error.ShouldBeDomainError<Email>(new DomainErrorKind.Empty());
}

// Verification including current value
[Fact]
public void ShouldBeDomainError_WithValue_WhenValueIsNegative()
{
    // Arrange
    var error = DomainError.For<Age, int>(
        new DomainErrorKind.Negative(),
        currentValue: -5,
        message: "Age cannot be negative");

    // Act & Assert
    error.ShouldBeDomainError<Age, int>(
        new DomainErrorKind.Negative(),
        expectedCurrentValue: -5);
}

// Verification including two values
// Error type definition: public sealed record InvalidRange : DomainErrorKind.Custom;
[Fact]
public void ShouldBeDomainError_WithTwoValues_WhenRangeIsInvalid()
{
    // Arrange
    var startDate = new DateTime(2024, 12, 31);
    var endDate = new DateTime(2024, 1, 1);
    var error = DomainError.For<DateRange, DateTime, DateTime>(
        new InvalidRange(),
        startDate,
        endDate,
        message: "Start date must be before end date");

    // Act & Assert
    error.ShouldBeDomainError<DateRange, DateTime, DateTime>(
        new InvalidRange(),
        expectedValue1: startDate,
        expectedValue2: endDate);
}

// Verification including three values
// Error type definition: public sealed record InvalidTriangle : DomainErrorKind.Custom;
[Fact]
public void ShouldBeDomainError_WithThreeValues()
{
    // Arrange
    var error = DomainError.For<Triangle, double, double, double>(
        new InvalidTriangle(),
        1.0, 2.0, 10.0,
        message: "Invalid triangle sides");

    // Act & Assert
    error.ShouldBeDomainError<Triangle, double, double, double>(
        new InvalidTriangle(),
        expectedValue1: 1.0,
        expectedValue2: 2.0,
        expectedValue3: 10.0);
}
```

#### Fin<T> Verification

```csharp
[Fact]
public void Fin_ShouldBeDomainError_WhenCreationFails()
{
    // Arrange
    Fin<Email> fin = DomainError.For<Email>(
        new DomainErrorKind.InvalidFormat(),
        currentValue: "invalid-email",
        message: "Invalid email format");

    // Act & Assert
    // ShouldBeDomainError<TErrorSource, TFin>: TErrorSource = error source type, TFin = T of Fin<T>
    fin.ShouldBeDomainError<Email, Email>(new DomainErrorKind.InvalidFormat());
}

[Fact]
public void Fin_ShouldBeDomainError_WithValue()
{
    // Arrange
    Fin<Age> fin = DomainError.For<Age, int>(
        new DomainErrorKind.Negative(),
        currentValue: -5,
        message: "Age cannot be negative");

    // Act & Assert
    fin.ShouldBeDomainError<Age, Age, int>(
        new DomainErrorKind.Negative(),
        expectedCurrentValue: -5);
}
```

#### Validation<Error, T> Verification

```csharp
// Verify whether a specific error is included
[Fact]
public void Validation_ShouldHaveDomainError()
{
    // Arrange
    Validation<Error, Address> validation = Fail<Error, Address>(
        DomainError.For<Street>(
            new DomainErrorKind.Empty(),
            currentValue: "",
            message: "Street cannot be empty"));

    // Act & Assert
    validation.ShouldHaveDomainError<Street, Address>(new DomainErrorKind.Empty());
}

// Verify exactly one error is included
[Fact]
public void Validation_ShouldHaveOnlyDomainError()
{
    // Arrange
    Validation<Error, PostalCode> validation = Fail<Error, PostalCode>(
        DomainError.For<PostalCode>(
            new DomainErrorKind.InvalidFormat(),
            currentValue: "invalid",
            message: "Invalid postal code format"));

    // Act & Assert
    validation.ShouldHaveOnlyDomainError<PostalCode, PostalCode>(
        new DomainErrorKind.InvalidFormat());
}

// Verify all multiple errors are included
[Fact]
public void Validation_ShouldHaveDomainErrors_WhenMultipleErrorsExist()
{
    // Arrange
    var error1 = DomainError.For<Password>(
        new DomainErrorKind.TooShort(MinLength: 8),
        currentValue: "abc",
        message: "Password too short");

    var error2 = DomainError.For<Password>(
        new DomainErrorKind.NotUpperCase(),
        currentValue: "abc",
        message: "Password must contain uppercase");

    Validation<Error, Password> validation = Fail<Error, Password>(Error.Many(error1, error2));

    // Act & Assert
    validation.ShouldHaveDomainErrors<Password, Password>(
        new DomainErrorKind.TooShort(MinLength: 8),
        new DomainErrorKind.NotUpperCase());
}

// Verification including current value
[Fact]
public void Validation_ShouldHaveDomainError_WithValue()
{
    // Arrange
    Validation<Error, Quantity> validation = Fail<Error, Quantity>(
        DomainError.For<Quantity, int>(
            new DomainErrorKind.Negative(),
            currentValue: -10,
            message: "Quantity cannot be negative"));

    // Act & Assert
    validation.ShouldHaveDomainError<Quantity, Quantity, int>(
        new DomainErrorKind.Negative(),
        expectedCurrentValue: -10);
}
```

Now that we have confirmed Domain error creation and test patterns, let's move on to Application errors used at the Usecase level.

---

## Application Errors

### Error Creation and Return

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorKind;

// Basic usage - return directly via implicit conversion
if (await _repository.ExistsAsync(command.ProductCode))
{
    return ApplicationError.For<CreateProductCommand>(
        new AlreadyExists(),
        command.ProductCode,
        "Product code already exists");
}

// Generic value type
return ApplicationError.For<UpdateOrderCommand, Guid>(
    new NotFound(),
    orderId,
    "Order not found");

// Including two values
return ApplicationError.For<TransferCommand, decimal, decimal>(
    new BusinessRuleViolated("InsufficientBalance"),
    balance, amount,
    "Insufficient balance");
```

### Complete ApplicationErrorKind List

The following table categorizes Application error types by category.

#### Common Error Types - R1, R3, R4, R5

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Empty` | Is empty | `new Empty()` |
| `Null` | Is null | `new Null()` |
| `NotFound` | Not found | `new NotFound()` |
| `AlreadyExists` | Already exists | `new AlreadyExists()` |
| `Duplicate` | Duplicated | `new Duplicate()` |
| `InvalidState` | Invalid state | `new InvalidState()` |

#### Authorization/Authentication - R7

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Unauthorized` | Not authenticated | `new Unauthorized()` |
| `Forbidden` | Access forbidden | `new Forbidden()` |

#### Validation/Business Rules - R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `ValidationFailed` | Validation failed | `new ValidationFailed(PropertyName: "Quantity")` |
| `BusinessRuleViolated` | Business rule violated | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `ConcurrencyConflict` | Concurrency conflict | `new ConcurrencyConflict()` |
| `ResourceLocked` | Resource locked | `new ResourceLocked(ResourceName: "Order")` |
| `OperationCancelled` | Operation cancelled | `new OperationCancelled()` |
| `InsufficientPermission` | Insufficient permission | `new InsufficientPermission(Permission: "Admin")` |

#### Custom

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Custom` | Application-specific error (abstract) | `sealed record PaymentDeclined : ApplicationErrorKind.Custom;` → `new PaymentDeclined()` |

### Usecase Error Usage Pattern

This shows both the pattern of using `ApplicationError.For` in LINQ query `guard` clauses and the pattern of returning directly.

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorKind;

public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);

    public sealed class Usecase(IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, ...)
        {
            // Used with guard in LINQ query
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from product in _productRepository.Create(...)
                select new Response(...);

            // Direct return (implicit conversion)
            return ApplicationError.For<CreateProductCommand>(
                new NotFound(),
                productId.ToString(),
                $"Product not found. ID: {productId}");
        }
    }
}
```

Error code format:

```
ApplicationErrors.{UsecaseName}.{ErrorTypeName}
```

Examples:
- `Application.CreateProductCommand.AlreadyExists`
- `Application.UpdateProductCommand.NotFound`
- `Application.DeleteOrderCommand.BusinessRuleViolated`

Usecase usage example:

```csharp
public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, FinResponse<ProductId>>
{
    public async ValueTask<FinResponse<ProductId>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // Duplicate check - return directly via implicit conversion
        if (await _repository.ExistsAsync(command.ProductCode))
        {
            return ApplicationError.For<CreateProductCommand>(
                new AlreadyExists(),
                command.ProductCode,
                "Product code already exists");
        }

        // Business rule validation
        if (command.Price <= 0)
        {
            return ApplicationError.For<CreateProductCommand, decimal>(
                new BusinessRuleViolated("PositivePrice"),
                command.Price,
                "Price must be positive");
        }

        // Success handling
        var product = Product.Create(command.ProductCode, command.Name, command.Price);
        await _repository.AddAsync(product);
        return product.Id;
    }
}
```

### Application Error Testing

Test assertion namespace:

```csharp
using Functorium.Testing.Assertions.Errors;
```

#### Error Verification

```csharp
// Basic error type verification
[Fact]
public void ShouldBeApplicationError_WhenProductNotFound()
{
    // Arrange
    var error = ApplicationError.For<GetProductQuery>(
        new ApplicationErrorKind.NotFound(),
        currentValue: "PROD-001",
        message: "Product not found");

    // Act & Assert
    error.ShouldBeApplicationError<GetProductQuery>(new ApplicationErrorKind.NotFound());
}

// Verification including current value
[Fact]
public void ShouldBeApplicationError_WithValue_WhenDuplicate()
{
    // Arrange
    var productId = Guid.NewGuid();
    var error = ApplicationError.For<CreateProductCommand, Guid>(
        new ApplicationErrorKind.AlreadyExists(),
        currentValue: productId,
        message: "Product already exists");

    // Act & Assert
    error.ShouldBeApplicationError<CreateProductCommand, Guid>(
        new ApplicationErrorKind.AlreadyExists(),
        expectedCurrentValue: productId);
}

// Verification including two values
[Fact]
public void ShouldBeApplicationError_WithTwoValues_WhenBusinessRuleViolated()
{
    // Arrange
    var error = ApplicationError.For<TransferCommand, decimal, decimal>(
        new ApplicationErrorKind.BusinessRuleViolated("InsufficientBalance"),
        100m,
        500m,
        message: "Insufficient balance for transfer");

    // Act & Assert
    error.ShouldBeApplicationError<TransferCommand, decimal, decimal>(
        new ApplicationErrorKind.BusinessRuleViolated("InsufficientBalance"),
        expectedValue1: 100m,
        expectedValue2: 500m);
}
```

#### Fin<T> Verification

```csharp
[Fact]
public void Fin_ShouldBeApplicationError_WhenQueryFails()
{
    // Arrange
    Fin<Product> fin = ApplicationError.For<GetProductQuery>(
        new ApplicationErrorKind.NotFound(),
        currentValue: "PROD-001",
        message: "Product not found");

    // Act & Assert
    fin.ShouldBeApplicationError<GetProductQuery, Product>(
        new ApplicationErrorKind.NotFound());
}

[Fact]
public void Fin_ShouldBeApplicationError_WithValue()
{
    // Arrange
    var orderId = Guid.NewGuid();
    Fin<Order> fin = ApplicationError.For<CancelOrderCommand, Guid>(
        new ApplicationErrorKind.InvalidState(),
        currentValue: orderId,
        message: "Cannot cancel shipped order");

    // Act & Assert
    fin.ShouldBeApplicationError<CancelOrderCommand, Order, Guid>(
        new ApplicationErrorKind.InvalidState(),
        expectedCurrentValue: orderId);
}
```

#### Validation<Error, T> Verification

```csharp
[Fact]
public void Validation_ShouldHaveApplicationError()
{
    // Arrange
    Validation<Error, ProductId> validation = Fail<Error, ProductId>(
        ApplicationError.For<CreateProductCommand>(
            new ApplicationErrorKind.AlreadyExists(),
            currentValue: "PROD-001",
            message: "Product already exists"));

    // Act & Assert
    validation.ShouldHaveApplicationError<CreateProductCommand, ProductId>(
        new ApplicationErrorKind.AlreadyExists());
}

[Fact]
public void Validation_ShouldHaveOnlyApplicationError()
{
    // Arrange
    Validation<Error, Unit> validation = Fail<Error, Unit>(
        ApplicationError.For<DeleteOrderCommand>(
            new ApplicationErrorKind.Forbidden(),
            currentValue: "ORDER-001",
            message: "Cannot delete this order"));

    // Act & Assert
    validation.ShouldHaveOnlyApplicationError<DeleteOrderCommand, Unit>(
        new ApplicationErrorKind.Forbidden());
}

[Fact]
public void Validation_ShouldHaveApplicationErrors()
{
    // Arrange
    var error1 = ApplicationError.For<UpdateUserCommand>(
        new ApplicationErrorKind.ValidationFailed("Email"),
        currentValue: "",
        message: "Email is required");

    var error2 = ApplicationError.For<UpdateUserCommand>(
        new ApplicationErrorKind.ValidationFailed("Name"),
        currentValue: "",
        message: "Name is required");

    Validation<Error, Unit> validation = Fail<Error, Unit>(Error.Many(error1, error2));

    // Act & Assert
    validation.ShouldHaveApplicationErrors<UpdateUserCommand, Unit>(
        new ApplicationErrorKind.ValidationFailed("Email"),
        new ApplicationErrorKind.ValidationFailed("Name"));
}
```

Having examined the definition and testing of Application errors, let's now look at Event errors that represent internal failures in the event system.

---

## Event Errors

### Error Creation and Return

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.EventErrorType;

// Basic usage - event publishing failure
EventError.For<DomainEventPublisher>(
    new PublishFailed(),
    eventType,
    "Failed to publish event");

// Generic value type
EventError.For<ObservableDomainEventPublisher, Guid>(
    new HandlerFailed(),
    eventId,
    "Event handler threw exception");

// Exception wrapping (default PublishFailed type)
EventError.FromException<DomainEventPublisher>(exception);

// Exception wrapping (specifying a specific error type)
EventError.FromException<DomainEventPublisher>(
    new HandlerFailed(),
    exception);
```

### Complete EventErrorType List

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `PublishFailed` | Event publishing failure | `new PublishFailed()` |
| `HandlerFailed` | Event handler execution failure | `new HandlerFailed()` |
| `InvalidEventType` | Invalid event type | `new InvalidEventType()` |
| `PublishCancelled` | Event publishing cancelled | `new PublishCancelled()` |
| `Custom` | Event-specific custom error (abstract) | `sealed record RetryExhausted : EventErrorType.Custom;` → `new RetryExhausted()` |

### Error Code Format

EventError uses the Application layer prefix:

```
ApplicationErrors.{PublisherName}.{ErrorTypeName}
```

Examples:
- `Application.DomainEventPublisher.PublishFailed`
- `Application.ObservableDomainEventPublisher.HandlerFailed`
- `Application.DomainEventPublisher.InvalidEventType`

---

## Troubleshooting

### `ShouldBeDomainError` Assertion Fails in Tests
**Cause:** The error type parameters do not match. For example, if you create with `TooShort(MinLength: 8)` but verify with `new TooShort(MinLength: 3)`, the assertion fails.
**Resolution:** Error type parameters must match exactly. Since they are sealed record-based, all fields are included in equality comparison.

### Custom Error Not Recognized by `ShouldBeDomainError`
**Cause:** The Custom error may be defined in the wrong location, or it may not inherit from `DomainErrorKind.Custom`.
**Resolution:** Custom errors must inherit from the corresponding layer's `Custom` abstract record. Example: `public sealed record InsufficientStock : DomainErrorKind.Custom;`

---

## FAQ

### Q1. What is the criterion for distinguishing Domain errors from Application errors?
Domain errors are used for invariant violations within the domain model (VO validation failures, Entity state rule violations). Application errors are used for Usecase-level business logic (duplicate checks, authorization checks, resource lookup failures). The criterion is the location (layer) of the code where the error occurs.

### Q2. When should EventError be used?
Use it for domain event publishing failures (`PublishFailed`, `PublishCancelled`) or event handler execution failures (`HandlerFailed`). It is a dedicated error type for expressing internal failures of the event system. The error code prefix uses `Application.`.

### Q3. What information should be included as the current value (currentValue) in errors?
Include information that helps with debugging. Typically this includes the failed validation input value (`id.ToString()`, `request.Name`), current state values (`Status.ToString()`, `(int)StockQuantity`), etc. Do not include sensitive information (passwords, tokens).

---

## References

- [05a-value-objects.md](../05a-value-objects) - Value Object implementation patterns, [05b-value-objects-validation.md](../05b-value-objects-validation) - Enumerations, validation, and FAQ
- [08a-error-system.md](../08a-error-system) - Error handling basic principles and naming rules
- [08c-error-system-adapter-testing.md](../08c-error-system-adapter-testing) - Adapter errors, Custom errors, testing best practices, and checklists
- [09-domain-services.md](../09-domain-services) - Domain services
- [15a-unit-testing.md](../testing/15a-unit-testing) - Unit testing guide
