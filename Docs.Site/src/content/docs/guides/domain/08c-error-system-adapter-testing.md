---
title: "Error System — Adapter Errors and Testing"
---

This document covers Adapter errors, custom error definitions, testing best practices, and layer-specific checklists. For basic error handling principles and naming conventions, see [08a-error-system.md](../08a-error-system). For Domain/Application/Event errors, see [08b-error-system-domain-app.md](../08b-error-system-domain-app).

## Introduction

Domain/Application errors were covered in [08b-error-system-domain-app.md](../08b-error-system-domain-app). This document covers Adapter errors, custom error definition patterns, testing best practices, and layer-specific checklists.

> Adapter errors express failures in pipelines, external services, and data processing. Exceptions are wrapped with `AdapterError.FromException` to maintain error traceability, and assertions from `Functorium.Testing.Assertions.Errors` precisely verify error types and codes.

## Summary

### Key Commands

```csharp
// Adapter error
AdapterError.For<ProductRepository>(new NotFound(), id, "Not found");
AdapterError.FromException<MyAdapter>(new ConnectionFailed("DB"), exception);

// Test assertions
error.ShouldBeAdapterError<ProductRepository>(new AdapterErrorKind.NotFound());
error.ShouldBeAdapterExceptionalError<UsecaseExceptionPipeline>(new AdapterErrorKind.PipelineException());

// Generic assertions
result.ShouldFailWithErrorCode("Adapter.ProductRepository.NotFound");
error.ShouldBeExceptionalError<InvalidOperationException>("Adapter.DatabaseAdapter.ConnectionFailed");
```

### Key Procedures

1. Adapter error: Select a standard error type or define a Custom sealed record
2. Create errors with `AdapterError.For` or `AdapterError.FromException`
3. If Custom error is needed, define a sealed record inheriting from `AdapterErrorKind.Custom`
4. Write tests - Use layer-specific assertions or generic assertions

### Key Concepts

| Layer | Factory | Error Code Prefix | When to Use |
|--------|--------|-----------------|----------|
| Adapter | `AdapterError` | `Adapter.` | Pipeline, external services, data |
| Custom | Per layer | Depends on layer | When standard errors cannot express the situation |

First we examine Adapter error creation patterns, then Custom error definitions, testing best practices, and layer-specific checklists.

---

## Adapter Errors

### Error Creation and Return

Errors occurring in pipelines, external services, and data processing are created with `AdapterError.For`. When wrapping exceptions, use `AdapterError.FromException`.

```csharp
using Functorium.Adapters.Errors;
using static Functorium.Adapters.Errors.AdapterErrorKind;

// Basic usage - direct return via implicit conversion
return AdapterError.For<ProductRepository>(
    new NotFound(),
    id.ToString(),
    "Product not found");

// Generic value type
return AdapterError.For<HttpClientAdapter, string>(
    new Timeout(Duration: TimeSpan.FromSeconds(30)),
    url,
    "Request timeout");

// Exception wrapping
return AdapterError.FromException<ExternalApiService>(
    new ConnectionFailed("ExternalApi"),
    exception);
```

### Complete AdapterErrorKind List

The following table organizes Adapter error types by category.

#### Common Error Types - R1, R3, R4, R5, R7

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Empty` | Empty | `new Empty()` |
| `Null` | Null | `new Null()` |
| `NotFound` | Not found | `new NotFound()` |
| `AlreadyExists` | Already exists | `new AlreadyExists()` |
| `Duplicate` | Duplicate | `new Duplicate()` |
| `InvalidState` | Invalid state | `new InvalidState()` |
| `Unauthorized` | Not authenticated | `new Unauthorized()` |
| `Forbidden` | Access forbidden | `new Forbidden()` |

#### Pipeline Related - R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `PipelineValidation` | Pipeline validation failure | `new PipelineValidation(PropertyName: "Id")` |
| `PipelineException` | Pipeline exception occurred | `new PipelineException()` |

#### External Service Related - R1, R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `ExternalServiceUnavailable` | External service unavailable | `new ExternalServiceUnavailable(ServiceName: "PaymentGateway")` |
| `ConnectionFailed` | Connection failed | `new ConnectionFailed(Target: "database")` |
| `Timeout` | Timeout | `new Timeout(Duration: TimeSpan.FromSeconds(30))` |

#### Data Related - R1, R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Serialization` | Serialization failed | `new Serialization(Format: "JSON")` |
| `Deserialization` | Deserialization failed | `new Deserialization(Format: "XML")` |
| `DataCorruption` | Data corruption | `new DataCorruption()` |

#### Custom

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Custom` | Adapter-specific error (abstract) | `sealed record RateLimited : AdapterErrorKind.Custom;` -> `new RateLimited()` |

### Repository Implementation Example

Implicit conversion pattern for directly returning Not Found with `AdapterError.For` in `GetById`.

```csharp
[GenerateObservablePort]
public class InMemoryProductRepository : IProductRepository
{
    private static readonly ConcurrentDictionary<ProductId, Product> _products = new();

    public string RequestCategory => "Repository";

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (_products.TryGetValue(id, out Product? product))
                return Fin.Succ(product);

            // Direct return via implicit conversion
            return AdapterError.For<InMemoryProductRepository>(
                new NotFound(),
                id.ToString(),
                $"Product ID '{id}' not found");
        });
    }

    public virtual FinT<IO, Product> Update(Product product)
    {
        return IO.lift(() =>
        {
            if (!_products.ContainsKey(product.Id))
            {
                return AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    product.Id.ToString(),
                    $"Product ID '{product.Id}' not found");
            }

            _products[product.Id] = product;
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, int> Delete(ProductId id)
    {
        return IO.lift(() =>
        {
            if (!_products.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"Product ID '{id}' not found");
            }

            return Fin.Succ(unit);
        });
    }
}
```

### External API Service Implementation Example

The `HandleHttpError` pattern that returns different error types based on HTTP status codes, and `FromException` usage by exception type.

```csharp
[GenerateObservablePort]
public class ExternalPricingApiService : IExternalPricingService
{
    public sealed record OperationCancelled : AdapterErrorKind.Custom;
    public sealed record UnexpectedException : AdapterErrorKind.Custom;
    public sealed record RateLimited : AdapterErrorKind.Custom;
    public sealed record HttpError : AdapterErrorKind.Custom;

    private readonly HttpClient _httpClient;

    public string RequestCategory => "ExternalApi";

    public virtual FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/pricing/{productCode}",
                    cancellationToken);

                // HTTP error response handling - using implicit conversion
                if (!response.IsSuccessStatusCode)
                    return HandleHttpError<Money>(response, productCode);

                var priceResponse = await response.Content
                    .ReadFromJsonAsync<ExternalPriceResponse>(cancellationToken: cancellationToken);

                // null response handling
                if (priceResponse is null)
                {
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(),
                        productCode,
                        $"External API response is null. ProductCode: {productCode}");
                }

                return Money.Create(priceResponse.Price);
            }
            catch (HttpRequestException ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new ConnectionFailed("ExternalPricingApi"),
                    ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                return AdapterError.For<ExternalPricingApiService>(
                    new OperationCancelled(),
                    productCode,
                    "Request was cancelled");
            }
            catch (TaskCanceledException ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new Timeout(TimeSpan.FromSeconds(30)),
                    ex);
            }
            catch (Exception ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new UnexpectedException(),
                    ex);
            }
        });
    }

    /// <summary>
    /// Converts HTTP error responses to AdapterError.
    /// Implicit conversion is automatically applied in switch expressions.
    /// </summary>
    private static Fin<T> HandleHttpError<T>(HttpResponseMessage response, string context) =>
        response.StatusCode switch
        {
            HttpStatusCode.NotFound => AdapterError.For<ExternalPricingApiService>(
                new NotFound(),
                context,
                $"Resource not found in external API. Context: {context}"),

            HttpStatusCode.Unauthorized => AdapterError.For<ExternalPricingApiService>(
                new Unauthorized(),
                context,
                "External API authentication failed"),

            HttpStatusCode.Forbidden => AdapterError.For<ExternalPricingApiService>(
                new Forbidden(),
                context,
                "External API access forbidden"),

            HttpStatusCode.TooManyRequests => AdapterError.For<ExternalPricingApiService>(
                new RateLimited(),
                context,
                "External API rate limit reached"),

            HttpStatusCode.ServiceUnavailable => AdapterError.For<ExternalPricingApiService>(
                new ExternalServiceUnavailable("ExternalPricingApi"),
                context,
                "External pricing service unavailable"),

            _ => AdapterError.For<ExternalPricingApiService, HttpStatusCode>(
                new HttpError(),
                response.StatusCode,
                $"External API call failed. Status: {response.StatusCode}")
        };
}
```

### Adapter Error Testing

Test assertion namespace:

```csharp
using Functorium.Testing.Assertions.Errors;
```

The following table summarizes assertion methods provided per layer.

| Layer | Error Verification | Fin<T> Verification | Validation<Error, T> Verification |
|--------|-----------|-------------|--------------------------|
| Domain | `ShouldBeDomainError` | `ShouldBeDomainError` | `ShouldHaveDomainError`, `ShouldHaveOnlyDomainError`, `ShouldHaveDomainErrors` |
| Application | `ShouldBeApplicationError` | `ShouldBeApplicationError` | `ShouldHaveApplicationError`, `ShouldHaveOnlyApplicationError`, `ShouldHaveApplicationErrors` |
| Adapter | `ShouldBeAdapterError`, `ShouldBeAdapterExceptionalError` | `ShouldBeAdapterError`, `ShouldBeAdapterExceptionalError` | `ShouldHaveAdapterError`, `ShouldHaveOnlyAdapterError`, `ShouldHaveAdapterErrors` |

#### Error Verification

```csharp
// Basic error type verification
[Fact]
public void ShouldBeAdapterError_WhenValidationFails()
{
    // Arrange
    var error = AdapterError.For<UsecaseValidationPipeline>(
        new AdapterErrorKind.PipelineValidation("ProductName"),
        currentValue: "",
        message: "ProductName is required");

    // Act & Assert
    error.ShouldBeAdapterError<UsecaseValidationPipeline>(
        new AdapterErrorKind.PipelineValidation("ProductName"));
}

// Verification including current value
[Fact]
public void ShouldBeAdapterError_WithValue_WhenTimeout()
{
    // Arrange
    var url = "https://api.example.com/data";
    var error = AdapterError.For<HttpClientAdapter, string>(
        new AdapterErrorKind.Timeout(Duration: TimeSpan.FromSeconds(30)),
        currentValue: url,
        message: "Request timed out");

    // Act & Assert
    error.ShouldBeAdapterError<HttpClientAdapter, string>(
        new AdapterErrorKind.Timeout(Duration: TimeSpan.FromSeconds(30)),
        expectedCurrentValue: url);
}

// Exception wrapping error verification
[Fact]
public void ShouldBeAdapterExceptionalError_WhenExceptionOccurs()
{
    // Arrange
    var exception = new InvalidOperationException("Something went wrong");
    var error = AdapterError.FromException<UsecaseExceptionPipeline>(
        new AdapterErrorKind.PipelineException(),
        exception);

    // Act & Assert
    error.ShouldBeAdapterExceptionalError<UsecaseExceptionPipeline>(
        new AdapterErrorKind.PipelineException());
}

[Fact]
public void ShouldBeAdapterExceptionalError_WithExceptionType()
{
    // Arrange
    var exception = new TimeoutException("Connection timed out");
    var error = AdapterError.FromException<DatabaseAdapter>(
        new AdapterErrorKind.ConnectionFailed("database"),
        exception);

    // Act & Assert
    error.ShouldBeAdapterExceptionalError<DatabaseAdapter, TimeoutException>(
        new AdapterErrorKind.ConnectionFailed("database"));
}
```

#### Fin<T> Verification

```csharp
[Fact]
public void Fin_ShouldBeAdapterError_WhenServiceUnavailable()
{
    // Arrange
    Fin<PaymentResult> fin = AdapterError.For<PaymentGatewayAdapter>(
        new AdapterErrorKind.ExternalServiceUnavailable("PaymentGateway"),
        currentValue: "https://payment.example.com",
        message: "Payment service unavailable");

    // Act & Assert
    fin.ShouldBeAdapterError<PaymentGatewayAdapter, PaymentResult>(
        new AdapterErrorKind.ExternalServiceUnavailable("PaymentGateway"));
}

[Fact]
public void Fin_ShouldBeAdapterExceptionalError()
{
    // Arrange
    Fin<Unit> fin = AdapterError.FromException<UsecaseExceptionPipeline>(
        new AdapterErrorKind.PipelineException(),
        new Exception("Unexpected error"));

    // Act & Assert
    fin.ShouldBeAdapterExceptionalError<UsecaseExceptionPipeline, Unit>(
        new AdapterErrorKind.PipelineException());
}
```

#### Validation<Error, T> Verification

```csharp
[Fact]
public void Validation_ShouldHaveAdapterError()
{
    // Arrange
    Validation<Error, Unit> validation = Fail<Error, Unit>(
        AdapterError.For<CacheAdapter>(
            new AdapterErrorKind.ConnectionFailed("Redis"),
            currentValue: "localhost:6379",
            message: "Cannot connect to Redis"));

    // Act & Assert
    validation.ShouldHaveAdapterError<CacheAdapter, Unit>(
        new AdapterErrorKind.ConnectionFailed("Redis"));
}

[Fact]
public void Validation_ShouldHaveOnlyAdapterError()
{
    // Arrange
    Validation<Error, byte[]> validation = Fail<Error, byte[]>(
        AdapterError.For<MessageSerializer>(
            new AdapterErrorKind.Serialization("JSON"),
            currentValue: "invalid-object",
            message: "Failed to serialize object to JSON"));

    // Act & Assert
    validation.ShouldHaveOnlyAdapterError<MessageSerializer, byte[]>(
        new AdapterErrorKind.Serialization("JSON"));
}

[Fact]
public void Validation_ShouldHaveAdapterErrors()
{
    // Arrange
    var error1 = AdapterError.For<UsecaseValidationPipeline>(
        new AdapterErrorKind.PipelineValidation("Name"),
        currentValue: "",
        message: "Name is required");

    var error2 = AdapterError.For<UsecaseValidationPipeline>(
        new AdapterErrorKind.PipelineValidation("Price"),
        currentValue: "-1",
        message: "Price must be positive");

    Validation<Error, Unit> validation = Fail<Error, Unit>(Error.Many(error1, error2));

    // Act & Assert
    validation.ShouldHaveAdapterErrors<UsecaseValidationPipeline, Unit>(
        new AdapterErrorKind.PipelineValidation("Name"),
        new AdapterErrorKind.PipelineValidation("Price"));
}
```

Now that Adapter error creation and test patterns have been confirmed, let us learn how to define Custom errors for situations that cannot be expressed with standard errors.

---

## Custom Errors

### When to Use Custom Errors?

1. **When standard errors cannot express the situation**: Domain/application/adapter-specific scenarios
2. **When the meaning is clear**: When the error name alone conveys the situation
3. **When reuse potential is low**: Errors that occur only in specific situations

### Custom Error Naming Rules

```csharp
// ✅ Good - clear and specific
// public sealed record AlreadyShipped : DomainErrorKind.Custom;
// public sealed record PaymentDeclined : ApplicationErrorKind.Custom;
// public sealed record StockDepleted : DomainErrorKind.Custom;
new AlreadyShipped()     // Already shipped
new PaymentDeclined()    // Payment declined
new StockDepleted()      // Stock depleted

// ❌ Bad - ambiguous or too generic
// sealed record Error : XxxErrorType.Custom;       // Meaningless
// sealed record Failed : XxxErrorType.Custom;      // Too generic
// sealed record Invalid : XxxErrorType.Custom;     // Not specific enough
```

### Custom Error Examples by Layer

The following table shows commonly defined Custom errors in each layer.

| Layer | Custom Error Examples | Description |
|--------|-----------------|------|
| Domain | `AlreadyShipped`, `NotVerified`, `Expired` | Domain rule violation |
| Application | `PaymentDeclined`, `QuotaExceeded`, `MaintenanceMode` | Business process failure |
| Adapter | `RateLimited`, `CircuitOpen`, `ServiceDegraded` | Infrastructure/external service issues |

### Criteria for Promoting to Standard Error

Frequently used Custom errors should be considered for promotion to standard error types (see [08a promotion criteria](../08a-error-system#custom-to-standard-error-promotion-criteria)):

> 1. Used in **3 or more different locations** with the same Custom error
> 2. **Reuse meaning is clear** (established as a domain concept)
> 3. Can be **naturally mapped** to existing naming conventions (R1-R8)
> 4. **Stability confirmed** (meaning no longer changes)

```csharp
// Add as standard type when frequently used patterns are discovered
public sealed record Expired : DomainErrorKind;
public sealed record Suspended : ApplicationErrorKind;
public sealed record RateLimited : AdapterErrorKind;
```

Now that Custom error definitions and promotion criteria are understood, let us examine best practices for writing error tests effectively.

---

## Testing Best Practices

### Failure Case Testing

Success cases where no error should occur must also be tested:

```csharp
[Fact]
public void Create_ShouldSucceed_WhenValidValue()
{
    // Arrange
    var validEmail = "user@example.com";

    // Act
    var result = Email.Create(validEmail);

    // Assert
    result.IsSucc.ShouldBeTrue();
    result.IfSucc(email => email.Value.ShouldBe(validEmail));
}

[Fact]
public void Validate_ShouldSucceed_WhenValidValue()
{
    // Arrange
    var validPassword = "SecureP@ss123";

    // Act
    var result = Password.Validate(validPassword);

    // Assert
    result.IsSuccess.ShouldBeTrue();
}
```

### Test Naming Conventions

```csharp
// Pattern: [Method]_Should[Behavior]_When[Condition]

// Error verification
ShouldBeDomainError_WhenValueIsEmpty
ShouldBeApplicationError_WhenProductNotFound
ShouldBeAdapterError_WhenValidationFails

// Fin verification
Create_ShouldFail_WhenEmailIsInvalid
Execute_ShouldFail_WhenProductNotFound

// Validation verification
Validate_ShouldHaveError_WhenPasswordTooShort
Validate_ShouldHaveMultipleErrors_WhenMultipleValidationsFail
```

### Arrange-Act-Assert Pattern

```csharp
[Fact]
public void Create_ShouldFail_WhenEmailIsEmpty()
{
    // Arrange
    var emptyEmail = "";

    // Act
    var result = Email.Create(emptyEmail);

    // Assert
    result.ShouldBeDomainError<Email, Email>(new DomainErrorKind.Empty());
}
```

### Parameterized Tests with Theory

```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void Create_ShouldFail_WhenEmailIsEmptyOrWhitespace(string? email)
{
    // Act
    var result = Email.Create(email);

    // Assert
    result.ShouldBeDomainError<Email, Email>(new DomainErrorKind.Empty());
}

[Theory]
[InlineData("invalid")]
[InlineData("missing@domain")]
[InlineData("@nodomain.com")]
public void Create_ShouldFail_WhenEmailFormatIsInvalid(string email)
{
    // Act
    var result = Email.Create(email);

    // Assert
    result.ShouldBeDomainError<Email, Email>(new DomainErrorKind.InvalidFormat());
}
```

### Custom Error Testing

```csharp
// Error type definition (nested in Order class):
// public sealed record AlreadyShipped : DomainErrorKind.Custom;

[Fact]
public void Cancel_ShouldFail_WhenOrderAlreadyShipped()
{
    // Arrange
    var error = DomainError.For<Order>(
        new Order.AlreadyShipped(),
        currentValue: "ORDER-001",
        message: "Cannot cancel shipped order");

    // Act & Assert
    error.ShouldBeDomainError<Order>(new Order.AlreadyShipped());
}
```

### Generic Error Assertion Utilities

In addition to layer-specific Assertions (`ShouldBeDomainError`, `ShouldBeApplicationError`, `ShouldBeAdapterError`), **layer-independent generic error verification** utilities are provided.

```csharp
using Functorium.Testing.Assertions.Errors;
```

#### ExpectedErrorAssertions -- Generic Error Code Verification

| Method | Description |
|--------|------|
| `error.ShouldHaveErrorCode()` | Verify `IHasErrorCode` implementation, return interface |
| `error.ShouldHaveErrorCode("code")` | Verify specific error code match |
| `error.ShouldHaveErrorCodeStartingWith("prefix")` | Verify error code prefix |
| `error.ShouldHaveErrorCode(predicate)` | Predicate-based error code verification |
| `error.ShouldBeExpected()` | Expected type verification |
| `error.ShouldBeExceptional()` | Exceptional type verification |
| `error.ShouldBeExpectedError("code", "value")` | `ExpectedError` type + code + value verification |
| `error.ShouldBeExpectedError<T>("code", value)` | `ExpectedError<T>` type + code + value verification |
| `error.ShouldBeExpectedError<T1, T2>("code", v1, v2)` | `ExpectedError<T1, T2>` verification |
| `error.ShouldBeExpectedError<T1, T2, T3>("code", v1, v2, v3)` | `ExpectedError<T1, T2, T3>` verification |
| `fin.ShouldSucceed()` | Success verification, return success value |
| `fin.ShouldSucceedWith(value)` | Success + specific value verification |
| `fin.ShouldFail()` | Failure verification |
| `fin.ShouldFail(errorAssertion)` | Failure + execute error assertion |
| `fin.ShouldFailWithErrorCode("code")` | Failure + specific error code verification |
| `validation.ShouldBeValid()` | Success verification, return success value |
| `validation.ShouldBeInvalid(errorsAssertion)` | Failure + error list assertion |
| `validation.ShouldContainErrorCode("code")` | Failure + verify specific error code inclusion |
| `validation.ShouldContainOnlyErrorCode("code")` | Failure + verify exactly 1 error with that code |
| `validation.ShouldContainErrorCodes("code1", "code2")` | Failure + verify multiple error code inclusion |

```csharp
// Generic error code verification examples
[Fact]
public void Create_ShouldFail_WithExpectedErrorCode()
{
    // Arrange & Act
    var result = Email.Create("");

    // Assert -- verify error code regardless of layer
    result.ShouldFailWithErrorCode("Domain.Email.Empty");
}

[Fact]
public void Validate_ShouldContain_MultipleErrorCodes()
{
    // Arrange & Act
    var result = Password.Validate("");

    // Assert
    result.ShouldContainErrorCodes(
        "Domain.Password.Empty",
        "Domain.Password.TooShort");
}
```

#### ExceptionalErrorAssertions -- Exception-Based Error Verification

| Method | Description |
|--------|------|
| `error.ShouldBeExceptionalError("code")` | `ExceptionalError` type + error code verification |
| `error.ShouldBeExceptionalError<TException>("code")` | Specific exception type wrapping verification |
| `error.ShouldWrapException<TException>("code", message?)` | Exception type + optional message verification |
| `error.ShouldBeExceptionalError("code", exceptionAssertion)` | Execute exception assertion |
| `fin.ShouldFailWithException("code")` | `Fin` failure + `ExceptionalError` verification |
| `fin.ShouldFailWithException<T, TException>("code")` | `Fin` failure + specific exception type verification |
| `validation.ShouldContainException("code")` | `Validation` failure + `ExceptionalError` inclusion verification |
| `validation.ShouldContainException<T, TException>("code")` | `Validation` failure + specific exception type inclusion verification |

```csharp
// Exception wrapping error verification example
[Fact]
public void ShouldWrapException_WhenDatabaseFails()
{
    // Arrange
    var exception = new InvalidOperationException("DB connection lost");
    var error = AdapterError.FromException<DatabaseAdapter>(
        new AdapterErrorKind.ConnectionFailed("database"),
        exception);

    // Assert
    error.ShouldBeExceptionalError<InvalidOperationException>(
        "Adapter.DatabaseAdapter.ConnectionFailed");
}
```

#### ErrorAssertionHelpers -- Extension Properties (C# 14 Extension Members)

| Extension Property | Target Type | Description |
|-----------|----------|------|
| `error.ErrorCode` | `Error` | Extract error code (`null` if `IHasErrorCode` not implemented) |
| `error.HasErrorCode` | `Error` | Whether error code exists |
| `validation.Errors` | `Validation<Error, T>` | Extract error list (`IReadOnlyList<Error>`) |

```csharp
// Extension property usage examples
[Fact]
public void Error_ShouldHave_ErrorCode_Property()
{
    // Arrange
    var error = DomainError.For<Email>(new Empty(), "", "Email cannot be empty");

    // Assert -- concise access via extension properties
    error.HasErrorCode.ShouldBeTrue();
    error.ErrorCode.ShouldBe("Domain.Email.Empty");
}
```

Now that test writing patterns are familiar, let us summarize the entire error system by layer and conclude with checklists.

---

## Summary by Layer + Checklist

### Domain (DomainErrorKind)

```
Presence:    Empty, Null
Length:      TooShort, TooLong, WrongLength
Format:      InvalidFormat
Case:        NotUpperCase, NotLowerCase
DateTime:    DefaultDate, NotInPast, NotInFuture, TooLate, TooEarly
Range:       RangeInverted, RangeEmpty
Numeric:     Zero, Negative, NotPositive, OutOfRange, BelowMinimum, AboveMaximum
Existence:   NotFound, AlreadyExists, Duplicate
Comparison:  Mismatch
Custom:      Custom (abstract -> sealed record MyError : DomainErrorKind.Custom)
```

### Application (ApplicationErrorKind)

```
Common:      Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState
Auth:        Unauthorized, Forbidden
Validation:  ValidationFailed
Business:    BusinessRuleViolated, ConcurrencyConflict, ResourceLocked,
             OperationCancelled, InsufficientPermission
Custom:      Custom (abstract -> sealed record MyError : ApplicationErrorKind.Custom)
```

### Adapter (AdapterErrorKind)

```
Common:      Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState,
             Unauthorized, Forbidden
Pipeline:    PipelineValidation, PipelineException
External:    ExternalServiceUnavailable, ConnectionFailed, Timeout
Data:        Serialization, Deserialization, DataCorruption
Custom:      Custom (abstract -> sealed record MyError : AdapterErrorKind.Custom)
```

### When to Use Each Layer

| Layer | When to Use |
|--------|----------|
| **Domain** | Value Object validation failure, Entity invariant violation, Aggregate business rule violation |
| **Application** | Business logic errors during Usecase execution, auth/permission errors, data retrieval failure, concurrency conflicts |
| **Adapter** | Pipeline validation/exception handling, external service call failures, serialization/deserialization errors, connection/timeout errors |

### Error Code Format

All error codes follow this format:

```
{LayerPrefix}.{TypeName}.{ErrorName}
```

| Layer | Prefix | Example |
|--------|--------|------|
| Domain | `Domain` | `Domain.Email.Empty` |
| Application | `Application` | `Application.CreateProductCommand.NotFound` |
| Adapter | `Adapter` | `Adapter.ProductRepository.NotFound` |

### Error Definition Checklist

- [ ] Was the appropriate layer (Domain/Application/Adapter) selected?
- [ ] Was it first verified whether a standard error type can express it?
- [ ] Is the Custom error name sufficiently clear?
- [ ] Does the context information (parameters) help with debugging?
- [ ] Is the error message useful to users/developers?

### Error Return Checklist

- [ ] Was implicit conversion used instead of `Fin.Fail<T>(error)`?
- [ ] Was `Fin.Succ(value)` used for success returns?
- [ ] Was the `FromException` method used for exception handling?
- [ ] Was the appropriate error factory for the layer (`DomainError`, `ApplicationError`, `AdapterError`) used?

### Naming Checklist

- [ ] Were the appropriate rules (R1-R8) applied?
- [ ] If symmetric pairs exist, was consistency maintained? (Below <-> Above)
- [ ] Is context information needed? (MinLength, Pattern, PropertyName, etc.)
- [ ] Is the error message consistent with the error name?

### Test Checklist

- [ ] Are there tests for all error cases?
- [ ] Is the error type verified to match exactly?
- [ ] Is the current value also verified when needed?
- [ ] Is the Custom error name verified to match exactly?
- [ ] Are there success tests for valid input?
- [ ] Are there tests for boundary values?
- [ ] Is the return value verified to match expectations?

---

## Troubleshooting

### Error code differs from expectation when using `FromException`
**Cause:** `FromException` creates an `ExceptionalError` type, so `ShouldBeAdapterExceptionalError` must be used instead of `ShouldBeAdapterError`.
**Resolution:** Verify exception-wrapping errors with `ShouldBeAdapterExceptionalError<TAdapter>(errorType)` or `ShouldBeAdapterExceptionalError<TAdapter, TException>(errorType)`.

### Custom error not recognized in layer-specific assertions
**Cause:** The Custom error may be defined in the wrong location or may not inherit from the `Custom` of the corresponding layer.
**Resolution:** Custom errors must inherit from the corresponding layer's `Custom` abstract record. Example: `public sealed record RateLimited : AdapterErrorKind.Custom;`

---

## FAQ

### Q1. Should I use generic assertions or layer-specific assertions?
Layer-specific assertions (`ShouldBeDomainError`, `ShouldBeApplicationError`, `ShouldBeAdapterError`) are stricter because they also verify the error's origin. Generic assertions (`ShouldFailWithErrorCode`, `ShouldContainErrorCode`) only verify error codes and are suitable for layer-independent tests. Generally, layer-specific assertions are recommended.

### Q2. When should Custom errors be promoted to standard errors?
When all 4 conditions are met: (1) Used in 3 or more different locations, (2) Reuse meaning is clear, (3) Can be naturally mapped to R1-R8 naming conventions, (4) Meaning is stable (no longer changes).

### Q3. What information should be included in the currentValue of an error?
Include information that helps with debugging. Mainly validation-failed input values (`id.ToString()`, `request.Name`), current state values (`Status.ToString()`, `(int)StockQuantity`), etc. Do not include sensitive information (passwords, tokens).

---

## References

- [08a-error-system.md](../08a-error-system) - Error handling basic principles and naming conventions
- [08b-error-system-domain-app.md](../08b-error-system-domain-app) - Domain/Application/Event error definition and testing
- [13-adapters.md](../adapter/13-adapters) - Adapter implementation guide
- [15a-unit-testing.md](../testing/15a-unit-testing) - Unit testing guide
- [16-testing-library.md](../testing/16-testing-library) - Non-error test utilities (log/architecture/source generator/job testing)
