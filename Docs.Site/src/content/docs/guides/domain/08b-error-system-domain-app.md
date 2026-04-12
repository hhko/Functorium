---
title: "Error System — Domain/Application Errors"
---

This document covers error definitions and test patterns for the Domain/Application/Event layers. For basic principles and naming rules of error handling, refer to [08a-error-system.md](./08a-error-system). For Adapter errors, Custom errors, testing best practices, and per-layer checklists, refer to [08c-error-system-adapter-testing.md](./08c-error-system-adapter-testing).

## Introduction

[08a-error-system.md](./08a-error-system) covered the fundamentals and naming rules of the error system. This document examines Domain and Application layer error definitions, factory method usage, and test assertion patterns in detail.

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
result.ShouldBeDomainError<Email, Email>(new DomainErrorType.Empty());
fin.ShouldBeApplicationError<GetProductQuery, Product>(new ApplicationErrorType.NotFound());
```

### Key Procedures

1. Determine which layer the error originates from (Domain / Application / Event)
2. Select a standard error type or define a Custom sealed record
3. Create the error using the layer factory (`DomainError.For`, `ApplicationError.For`, `EventError.For`)
4. Write tests using assertion methods from the `Functorium.Testing.Assertions.Errors` namespace

### Key Concepts

| Layer | Factory | Error Code Prefix | When to Use |
|--------|--------|-----------------|----------|
| Domain | `DomainError` | `DomainErrors.` | VO validation, Entity invariants, Aggregate rules |
| Application | `ApplicationError` | `ApplicationErrors.` | Usecase business logic, authorization/authentication |
| Event | `EventError` | `ApplicationErrors.` | Event publishing/handler failures |

We first examine Domain error creation and test patterns, then move on to Application errors and Event errors.

---

## Domain Errors

### Error Creation and Return

Use `DomainError.For<T>()` to create errors for Value Object validation or Entity invariant violations. The examples below show the overload differences based on the number of type parameters.

```csharp
using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;

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
// Error type definition: public sealed record InvalidRange : DomainErrorType.Custom;
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
// Error type definition: public sealed record InvalidTriangle : DomainErrorType.Custom;
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
    public sealed record InsufficientStock : DomainErrorType.Custom;

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

### DomainErrorType Category Structure and Complete List

The following table categorizes `DomainErrorType` by category and lists the files where each error type is defined.

| Category | File | Description |
|------|------|------|
| Presence | `DomainErrorType.Presence.cs` | Value existence validation |
| Length | `DomainErrorType.Length.cs` | String/collection length validation |
| Format | `DomainErrorType.Format.cs` | Format and case validation |
| DateTime | `DomainErrorType.DateTime.cs` | Date validation |
| Numeric | `DomainErrorType.Numeric.cs` | Numeric value/range validation |
| Range | `DomainErrorType.Range.cs` | min/max pair validation |
| Existence | `DomainErrorType.Existence.cs` | Existence validation |
| Custom | `DomainErrorType.Custom.cs` | Custom errors |

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
| `Custom` | Domain-specific error (abstract) | `sealed record AlreadyShipped : DomainErrorType.Custom;` -> `new AlreadyShipped()` |

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
// 기본 에러 타입 검증
[Fact]
public void ShouldBeDomainError_WhenValueIsEmpty()
{
    // Arrange
    var error = DomainError.For<Email>(
        new DomainErrorType.Empty(),
        currentValue: "",
        message: "Email cannot be empty");

    // Act & Assert
    error.ShouldBeDomainError<Email>(new DomainErrorType.Empty());
}

// 현재 값 포함 검증
[Fact]
public void ShouldBeDomainError_WithValue_WhenValueIsNegative()
{
    // Arrange
    var error = DomainError.For<Age, int>(
        new DomainErrorType.Negative(),
        currentValue: -5,
        message: "Age cannot be negative");

    // Act & Assert
    error.ShouldBeDomainError<Age, int>(
        new DomainErrorType.Negative(),
        expectedCurrentValue: -5);
}

// 두 개의 값 포함 검증
// Error type definition: public sealed record InvalidRange : DomainErrorType.Custom;
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

// 세 개의 값 포함 검증
// Error type definition: public sealed record InvalidTriangle : DomainErrorType.Custom;
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

#### Fin<T> 검증

```csharp
[Fact]
public void Fin_ShouldBeDomainError_WhenCreationFails()
{
    // Arrange
    Fin<Email> fin = DomainError.For<Email>(
        new DomainErrorType.InvalidFormat(),
        currentValue: "invalid-email",
        message: "Invalid email format");

    // Act & Assert
    // ShouldBeDomainError<TErrorSource, TFin>: TErrorSource = 에러 소스 타입, TFin = Fin<T>의 T
    fin.ShouldBeDomainError<Email, Email>(new DomainErrorType.InvalidFormat());
}

[Fact]
public void Fin_ShouldBeDomainError_WithValue()
{
    // Arrange
    Fin<Age> fin = DomainError.For<Age, int>(
        new DomainErrorType.Negative(),
        currentValue: -5,
        message: "Age cannot be negative");

    // Act & Assert
    fin.ShouldBeDomainError<Age, Age, int>(
        new DomainErrorType.Negative(),
        expectedCurrentValue: -5);
}
```

#### Validation<Error, T> 검증

```csharp
// 특정 에러 포함 여부 검증
[Fact]
public void Validation_ShouldHaveDomainError()
{
    // Arrange
    Validation<Error, Address> validation = Fail<Error, Address>(
        DomainError.For<Street>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Street cannot be empty"));

    // Act & Assert
    validation.ShouldHaveDomainError<Street, Address>(new DomainErrorType.Empty());
}

// 정확히 하나의 에러만 포함 검증
[Fact]
public void Validation_ShouldHaveOnlyDomainError()
{
    // Arrange
    Validation<Error, PostalCode> validation = Fail<Error, PostalCode>(
        DomainError.For<PostalCode>(
            new DomainErrorType.InvalidFormat(),
            currentValue: "invalid",
            message: "Invalid postal code format"));

    // Act & Assert
    validation.ShouldHaveOnlyDomainError<PostalCode, PostalCode>(
        new DomainErrorType.InvalidFormat());
}

// 여러 에러 모두 포함 검증
[Fact]
public void Validation_ShouldHaveDomainErrors_WhenMultipleErrorsExist()
{
    // Arrange
    var error1 = DomainError.For<Password>(
        new DomainErrorType.TooShort(MinLength: 8),
        currentValue: "abc",
        message: "Password too short");

    var error2 = DomainError.For<Password>(
        new DomainErrorType.NotUpperCase(),
        currentValue: "abc",
        message: "Password must contain uppercase");

    Validation<Error, Password> validation = Fail<Error, Password>(Error.Many(error1, error2));

    // Act & Assert
    validation.ShouldHaveDomainErrors<Password, Password>(
        new DomainErrorType.TooShort(MinLength: 8),
        new DomainErrorType.NotUpperCase());
}

// 현재 값 포함 검증
[Fact]
public void Validation_ShouldHaveDomainError_WithValue()
{
    // Arrange
    Validation<Error, Quantity> validation = Fail<Error, Quantity>(
        DomainError.For<Quantity, int>(
            new DomainErrorType.Negative(),
            currentValue: -10,
            message: "Quantity cannot be negative"));

    // Act & Assert
    validation.ShouldHaveDomainError<Quantity, Quantity, int>(
        new DomainErrorType.Negative(),
        expectedCurrentValue: -10);
}
```

Domain 에러의 생성과 테스트 패턴을 확인했으니, 이제 Usecase 수준에서 사용하는 Application 에러로 넘어갑니다.

---

## Application 에러

### 에러 생성 및 반환

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;

// 기본 사용법 - 암시적 변환으로 직접 반환
if (await _repository.ExistsAsync(command.ProductCode))
{
    return ApplicationError.For<CreateProductCommand>(
        new AlreadyExists(),
        command.ProductCode,
        "이미 존재하는 상품 코드입니다");
}

// 제네릭 값 타입
return ApplicationError.For<UpdateOrderCommand, Guid>(
    new NotFound(),
    orderId,
    "주문을 찾을 수 없습니다");

// 두 개의 값 포함
return ApplicationError.For<TransferCommand, decimal, decimal>(
    new BusinessRuleViolated("InsufficientBalance"),
    balance, amount,
    "잔액이 부족합니다");
```

### ApplicationErrorType 전체 목록

아래 표는 Application 에러 타입을 범주별로 정리한 것입니다.

#### 공통 에러 타입 - R1, R3, R4, R5

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |

#### 권한/인증 - R7

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |

#### 검증/비즈니스 규칙 - R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `ValidationFailed` | 검증 실패 | `new ValidationFailed(PropertyName: "Quantity")` |
| `BusinessRuleViolated` | 비즈니스 규칙 위반 | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `ConcurrencyConflict` | 동시성 충돌 | `new ConcurrencyConflict()` |
| `ResourceLocked` | 리소스 잠금 | `new ResourceLocked(ResourceName: "Order")` |
| `OperationCancelled` | 작업 취소됨 | `new OperationCancelled()` |
| `InsufficientPermission` | 권한 부족 | `new InsufficientPermission(Permission: "Admin")` |

#### 커스텀

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Custom` | 애플리케이션 특화 에러 (abstract) | `sealed record PaymentDeclined : ApplicationErrorType.Custom;` → `new PaymentDeclined()` |

### Usecase 에러 사용 패턴

LINQ 쿼리의 `guard` 구문에서 `ApplicationError.For`를 사용하는 패턴과, 직접 반환하는 패턴을 모두 보여줍니다.

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;

public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);

    public sealed class Usecase(IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, ...)
        {
            // LINQ 쿼리에서 guard와 함께 사용
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"이미 존재하는 상품명: '{request.Name}'"))
                from product in _productRepository.Create(...)
                select new Response(...);

            // 직접 반환 (암시적 변환)
            return ApplicationError.For<CreateProductCommand>(
                new NotFound(),
                productId.ToString(),
                $"상품을 찾을 수 없습니다. ID: {productId}");
        }
    }
}
```

에러 코드 형식:

```
ApplicationErrors.{UsecaseName}.{ErrorTypeName}
```

예시:
- `ApplicationErrors.CreateProductCommand.AlreadyExists`
- `ApplicationErrors.UpdateProductCommand.NotFound`
- `ApplicationErrors.DeleteOrderCommand.BusinessRuleViolated`

유스케이스 사용 예시:

```csharp
public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, FinResponse<ProductId>>
{
    public async ValueTask<FinResponse<ProductId>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // 중복 체크 - 암시적 변환으로 직접 반환
        if (await _repository.ExistsAsync(command.ProductCode))
        {
            return ApplicationError.For<CreateProductCommand>(
                new AlreadyExists(),
                command.ProductCode,
                "이미 존재하는 상품 코드입니다");
        }

        // 비즈니스 규칙 검증
        if (command.Price <= 0)
        {
            return ApplicationError.For<CreateProductCommand, decimal>(
                new BusinessRuleViolated("PositivePrice"),
                command.Price,
                "가격은 양수여야 합니다");
        }

        // 성공 처리
        var product = Product.Create(command.ProductCode, command.Name, command.Price);
        await _repository.AddAsync(product);
        return product.Id;
    }
}
```

### Application 에러 테스트

테스트 어설션 네임스페이스:

```csharp
using Functorium.Testing.Assertions.Errors;
```

#### Error 검증

```csharp
// 기본 에러 타입 검증
[Fact]
public void ShouldBeApplicationError_WhenProductNotFound()
{
    // Arrange
    var error = ApplicationError.For<GetProductQuery>(
        new ApplicationErrorType.NotFound(),
        currentValue: "PROD-001",
        message: "Product not found");

    // Act & Assert
    error.ShouldBeApplicationError<GetProductQuery>(new ApplicationErrorType.NotFound());
}

// 현재 값 포함 검증
[Fact]
public void ShouldBeApplicationError_WithValue_WhenDuplicate()
{
    // Arrange
    var productId = Guid.NewGuid();
    var error = ApplicationError.For<CreateProductCommand, Guid>(
        new ApplicationErrorType.AlreadyExists(),
        currentValue: productId,
        message: "Product already exists");

    // Act & Assert
    error.ShouldBeApplicationError<CreateProductCommand, Guid>(
        new ApplicationErrorType.AlreadyExists(),
        expectedCurrentValue: productId);
}

// 두 개의 값 포함 검증
[Fact]
public void ShouldBeApplicationError_WithTwoValues_WhenBusinessRuleViolated()
{
    // Arrange
    var error = ApplicationError.For<TransferCommand, decimal, decimal>(
        new ApplicationErrorType.BusinessRuleViolated("InsufficientBalance"),
        100m,
        500m,
        message: "Insufficient balance for transfer");

    // Act & Assert
    error.ShouldBeApplicationError<TransferCommand, decimal, decimal>(
        new ApplicationErrorType.BusinessRuleViolated("InsufficientBalance"),
        expectedValue1: 100m,
        expectedValue2: 500m);
}
```

#### Fin<T> 검증

```csharp
[Fact]
public void Fin_ShouldBeApplicationError_WhenQueryFails()
{
    // Arrange
    Fin<Product> fin = ApplicationError.For<GetProductQuery>(
        new ApplicationErrorType.NotFound(),
        currentValue: "PROD-001",
        message: "Product not found");

    // Act & Assert
    fin.ShouldBeApplicationError<GetProductQuery, Product>(
        new ApplicationErrorType.NotFound());
}

[Fact]
public void Fin_ShouldBeApplicationError_WithValue()
{
    // Arrange
    var orderId = Guid.NewGuid();
    Fin<Order> fin = ApplicationError.For<CancelOrderCommand, Guid>(
        new ApplicationErrorType.InvalidState(),
        currentValue: orderId,
        message: "Cannot cancel shipped order");

    // Act & Assert
    fin.ShouldBeApplicationError<CancelOrderCommand, Order, Guid>(
        new ApplicationErrorType.InvalidState(),
        expectedCurrentValue: orderId);
}
```

#### Validation<Error, T> 검증

```csharp
[Fact]
public void Validation_ShouldHaveApplicationError()
{
    // Arrange
    Validation<Error, ProductId> validation = Fail<Error, ProductId>(
        ApplicationError.For<CreateProductCommand>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "PROD-001",
            message: "Product already exists"));

    // Act & Assert
    validation.ShouldHaveApplicationError<CreateProductCommand, ProductId>(
        new ApplicationErrorType.AlreadyExists());
}

[Fact]
public void Validation_ShouldHaveOnlyApplicationError()
{
    // Arrange
    Validation<Error, Unit> validation = Fail<Error, Unit>(
        ApplicationError.For<DeleteOrderCommand>(
            new ApplicationErrorType.Forbidden(),
            currentValue: "ORDER-001",
            message: "Cannot delete this order"));

    // Act & Assert
    validation.ShouldHaveOnlyApplicationError<DeleteOrderCommand, Unit>(
        new ApplicationErrorType.Forbidden());
}

[Fact]
public void Validation_ShouldHaveApplicationErrors()
{
    // Arrange
    var error1 = ApplicationError.For<UpdateUserCommand>(
        new ApplicationErrorType.ValidationFailed("Email"),
        currentValue: "",
        message: "Email is required");

    var error2 = ApplicationError.For<UpdateUserCommand>(
        new ApplicationErrorType.ValidationFailed("Name"),
        currentValue: "",
        message: "Name is required");

    Validation<Error, Unit> validation = Fail<Error, Unit>(Error.Many(error1, error2));

    // Act & Assert
    validation.ShouldHaveApplicationErrors<UpdateUserCommand, Unit>(
        new ApplicationErrorType.ValidationFailed("Email"),
        new ApplicationErrorType.ValidationFailed("Name"));
}
```

Application 에러의 정의와 테스트를 살펴보았으니, 마지막으로 이벤트 시스템 내부 오류를 표현하는 Event 에러를 확인합니다.

---

## Event 에러

### 에러 생성 및 반환

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.EventErrorType;

// 기본 사용법 - 이벤트 발행 실패
EventError.For<DomainEventPublisher>(
    new PublishFailed(),
    eventType,
    "Failed to publish event");

// 제네릭 값 타입
EventError.For<ObservableDomainEventPublisher, Guid>(
    new HandlerFailed(),
    eventId,
    "Event handler threw exception");

// 예외 래핑 (기본 PublishFailed 타입)
EventError.FromException<DomainEventPublisher>(exception);

// 예외 래핑 (특정 에러 타입 지정)
EventError.FromException<DomainEventPublisher>(
    new HandlerFailed(),
    exception);
```

### EventErrorType 전체 목록

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `PublishFailed` | 이벤트 발행 실패 | `new PublishFailed()` |
| `HandlerFailed` | 이벤트 핸들러 실행 실패 | `new HandlerFailed()` |
| `InvalidEventType` | 이벤트 타입이 유효하지 않음 | `new InvalidEventType()` |
| `PublishCancelled` | 이벤트 발행 취소됨 | `new PublishCancelled()` |
| `Custom` | 이벤트 특화 커스텀 에러 (abstract) | `sealed record RetryExhausted : EventErrorType.Custom;` → `new RetryExhausted()` |

### 에러 코드 형식

EventError는 Application 레이어 접두사를 사용합니다:

```
ApplicationErrors.{PublisherName}.{ErrorTypeName}
```

예시:
- `ApplicationErrors.DomainEventPublisher.PublishFailed`
- `ApplicationErrors.ObservableDomainEventPublisher.HandlerFailed`
- `ApplicationErrors.DomainEventPublisher.InvalidEventType`

---

## Troubleshooting

### 테스트에서 `ShouldBeDomainError` 어설션이 실패함
**Cause:** 에러 타입의 파라미터가 일치하지 않는 경우입니다. 예를 들어 `TooShort(MinLength: 8)`로 생성했는데 `new TooShort(MinLength: 3)`로 검증하면 실패합니다.
**Resolution:** 에러 타입의 파라미터까지 정확히 일치시켜야 합니다. sealed record 기반이므로 모든 필드가 동등성 비교에 포함됩니다.

### Custom 에러가 `ShouldBeDomainError`에서 인식되지 않음
**Cause:** Custom 에러의 정의 위치가 잘못되었거나, `DomainErrorType.Custom`을 상속하지 않았을 수 있습니다.
**Resolution:** Custom 에러는 반드시 해당 레이어의 `Custom` abstract record를 상속해야 합니다. 예: `public sealed record InsufficientStock : DomainErrorType.Custom;`

---

## FAQ

### Q1. Domain 에러와 Application 에러의 구분 기준은?
Domain 에러는 도메인 모델 내부에서 발생하는 불변식 위반(VO 검증 실패, Entity 상태 규칙 위반)에 사용합니다. Application 에러는 Usecase 수준의 비즈니스 로직(중복 검사, 권한 확인, 리소스 조회 실패)에 사용합니다. 에러가 발생하는 코드의 위치(레이어)가 기준입니다.

### Q2. EventError는 언제 사용하나요?
도메인 이벤트 발행(`PublishFailed`, `PublishCancelled`)이나 이벤트 핸들러 실행 실패(`HandlerFailed`) 시 사용합니다. 이벤트 시스템 내부의 오류를 표현하기 위한 전용 에러 타입입니다. 에러 코드 접두사는 `ApplicationErrors.`를 사용합니다.

### Q3. 에러에 포함하는 현재 값(currentValue)은 어떤 정보를 넣어야 하나요?
디버깅에 도움이 되는 정보를 넣습니다. 주로 검증 실패한 입력값(`id.ToString()`, `request.Name`), 현재 상태값(`Status.ToString()`, `(int)StockQuantity`) 등입니다. 민감 정보(비밀번호, 토큰)는 포함하지 마세요.

---

## References

- [05a-value-objects.md](./05a-value-objects) - 값 객체 구현 패턴, [05b-value-objects-validation.md](./05b-value-objects-validation) - 열거형·검증·FAQ
- [08a-error-system.md](./08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [08c-error-system-adapter-testing.md](./08c-error-system-adapter-testing) - Adapter 에러, Custom 에러, 테스트 모범 사례, 체크리스트
- [09-domain-services.md](./09-domain-services) - 도메인 서비스
- [15a-unit-testing.md](../testing/15a-unit-testing) - 단위 테스트 가이드
