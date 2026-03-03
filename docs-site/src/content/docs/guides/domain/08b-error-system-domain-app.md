---
title: "에러 시스템 — Domain/Application 에러"
---

이 문서는 Domain/Application/Event 레이어별 에러 정의와 테스트 패턴을 다룹니다. 에러 처리의 기본 원칙과 네이밍 규칙은 [08a-error-system.md](./08a-error-system)을 참고하세요. Adapter 에러, Custom 에러, 테스트 모범 사례, 레이어별 체크리스트는 [08c-error-system-adapter-testing.md](./08c-error-system-adapter-testing)을 참고하세요.

## 목차

- [요약](#요약)
- [Domain 에러](#domain-에러)
- [Application 에러](#application-에러)
- [Event 에러](#event-에러)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// Domain 에러
DomainError.For<Email>(new Empty(), value, "이메일은 비어있을 수 없습니다");
DomainError.For<Age, int>(new Negative(), value, "나이는 음수일 수 없습니다");

// Application 에러
ApplicationError.For<CreateProductCommand>(new AlreadyExists(), code, "이미 존재합니다");

// Event 에러
EventError.For<DomainEventPublisher>(new PublishFailed(), eventType, "Failed to publish event");

// 테스트 어설션
result.ShouldBeDomainError<Email, Email>(new DomainErrorType.Empty());
fin.ShouldBeApplicationError<GetProductQuery, Product>(new ApplicationErrorType.NotFound());
```

### 주요 절차

1. 에러가 발생하는 레이어 결정 (Domain / Application / Event)
2. 표준 에러 타입 선택 또는 Custom sealed record 정의
3. 레이어 팩토리로 에러 생성 (`DomainError.For`, `ApplicationError.For`, `EventError.For`)
4. 테스트 작성 - `Functorium.Testing.Assertions.Errors` 네임스페이스의 어설션 메서드 사용

### 주요 개념

| 레이어 | 팩토리 | 에러 코드 접두사 | 사용 시점 |
|--------|--------|-----------------|----------|
| Domain | `DomainError` | `DomainErrors.` | VO 검증, Entity 불변식, Aggregate 규칙 |
| Application | `ApplicationError` | `ApplicationErrors.` | Usecase 비즈니스 로직, 권한/인증 |
| Event | `EventError` | `ApplicationErrors.` | 이벤트 발행/핸들러 실패 |

---

## Domain 에러

### 에러 생성 및 반환

```csharp
using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;

// 기본 사용법 - 암시적 변환으로 직접 반환
public Fin<Email> Create(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return DomainError.For<Email>(
            new Empty(),
            currentValue: value ?? "",
            message: "이메일은 비어있을 수 없습니다");

    return new Email(value);
}

// 제네릭 값 타입
public Fin<Age> Create(int value)
{
    if (value < 0)
        return DomainError.For<Age, int>(
            new Negative(),
            currentValue: value,
            message: "나이는 음수일 수 없습니다");

    return new Age(value);
}

// 두 개의 값 포함
// Error type definition: public sealed record InvalidRange : DomainErrorType.Custom;
public Fin<DateRange> Create(DateTime start, DateTime end)
{
    if (start >= end)
        return DomainError.For<DateRange, DateTime, DateTime>(
            new InvalidRange(),
            start, end,
            message: "시작 날짜는 종료 날짜보다 이전이어야 합니다");

    return new DateRange(start, end);
}

// 세 개의 값 포함
// Error type definition: public sealed record InvalidTriangle : DomainErrorType.Custom;
public Fin<Triangle> Create(double a, double b, double c)
{
    if (a + b <= c || b + c <= a || c + a <= b)
        return DomainError.For<Triangle, double, double, double>(
            new InvalidTriangle(),
            a, b, c,
            message: "유효한 삼각형을 만들 수 없습니다");

    return new Triangle(a, b, c);
}
```

### Entity 메서드에서 에러 반환

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
                message: $"재고 부족. 현재: {(int)StockQuantity}, 요청: {(int)quantity}");

        StockQuantity = Quantity.Create((int)StockQuantity - (int)quantity).ThrowIfFail();
        AddDomainEvent(new StockDeductedEvent(Id, quantity));
        return unit;
    }
}
```

### DomainErrorType 범주 구조 및 전체 목록

| 범주 | 파일 | 설명 |
|------|------|------|
| Presence | `DomainErrorType.Presence.cs` | 값 존재 검증 |
| Length | `DomainErrorType.Length.cs` | 문자열/컬렉션 길이 검증 |
| Format | `DomainErrorType.Format.cs` | 형식 및 대소문자 검증 |
| DateTime | `DomainErrorType.DateTime.cs` | 날짜 검증 |
| Numeric | `DomainErrorType.Numeric.cs` | 숫자 값/범위 검증 |
| Range | `DomainErrorType.Range.cs` | min/max 쌍 검증 |
| Existence | `DomainErrorType.Existence.cs` | 존재 여부 검증 |
| Custom | `DomainErrorType.Custom.cs` | 커스텀 에러 |

#### Presence (값 존재 검증) - R1

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Empty` | 비어있음 (null, empty string, empty collection) | `new Empty()` |
| `Null` | null임 | `new Null()` |

#### Length (문자열/컬렉션 길이 검증) - R2, R6

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `TooShort` | 최소 길이 미만 | `new TooShort(MinLength: 8)` |
| `TooLong` | 최대 길이 초과 | `new TooLong(MaxLength: 100)` |
| `WrongLength` | 정확한 길이 불일치 | `new WrongLength(Expected: 10)` |

#### Format (형식 검증) - R3, R5

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `InvalidFormat` | 형식 불일치 | `new InvalidFormat(Pattern: @"^\d{3}-\d{4}$")` |
| `NotUpperCase` | 대문자가 아님 | `new NotUpperCase()` |
| `NotLowerCase` | 소문자가 아님 | `new NotLowerCase()` |

#### DateTime (날짜 검증) - R1, R2, R3

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `DefaultDate` | 날짜가 기본값(DateTime.MinValue)임 | `new DefaultDate()` |
| `NotInPast` | 날짜가 과거여야 하는데 미래임 | `new NotInPast()` |
| `NotInFuture` | 날짜가 미래여야 하는데 과거임 | `new NotInFuture()` |
| `TooLate` | 날짜가 기준보다 늦음 (이전이어야 함) | `new TooLate(Boundary: "2025-12-31")` |
| `TooEarly` | 날짜가 기준보다 이름 (이후여야 함) | `new TooEarly(Boundary: "2020-01-01")` |

#### Numeric (숫자 검증) - R1, R2, R3

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Zero` | 0임 | `new Zero()` |
| `Negative` | 음수임 | `new Negative()` |
| `NotPositive` | 양수가 아님 (0 포함) | `new NotPositive()` |
| `OutOfRange` | 범위 밖 | `new OutOfRange(Min: "1", Max: "100")` |
| `BelowMinimum` | 최소값 미만 | `new BelowMinimum(Minimum: "0")` |
| `AboveMaximum` | 최대값 초과 | `new AboveMaximum(Maximum: "1000")` |

#### Range (범위 쌍 검증) - R1

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `RangeInverted` | 범위가 역전됨 (최소값이 최대값보다 큼) | `new RangeInverted(Min: "10", Max: "1")` |
| `RangeEmpty` | 범위가 비어있음 (min == max, 엄격한 범위) | `new RangeEmpty(Value: "5")` |

#### Existence (존재 여부 검증) - R1, R3, R4

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `Mismatch` | 값 불일치 | `new Mismatch()` |

#### Custom (커스텀 에러)

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Custom` | 도메인 특화 에러 (abstract) | `sealed record AlreadyShipped : DomainErrorType.Custom;` → `new AlreadyShipped()` |

### Value Object 사용 예시

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

### Domain 에러 테스트

테스트 어설션 네임스페이스:

```csharp
using Functorium.Testing.Assertions.Errors;
```

#### Error 검증

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

#### 공통 에러 타입 - R1, R3, R4, R5

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |

#### 권한/인증 - R7

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |

#### 검증/비즈니스 규칙 - R8

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `ValidationFailed` | 검증 실패 | `new ValidationFailed(PropertyName: "Quantity")` |
| `BusinessRuleViolated` | 비즈니스 규칙 위반 | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `ConcurrencyConflict` | 동시성 충돌 | `new ConcurrencyConflict()` |
| `ResourceLocked` | 리소스 잠금 | `new ResourceLocked(ResourceName: "Order")` |
| `OperationCancelled` | 작업 취소됨 | `new OperationCancelled()` |
| `InsufficientPermission` | 권한 부족 | `new InsufficientPermission(Permission: "Admin")` |

#### 커스텀

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Custom` | 애플리케이션 특화 에러 (abstract) | `sealed record PaymentDeclined : ApplicationErrorType.Custom;` → `new PaymentDeclined()` |

### Usecase 에러 사용 패턴

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

| 에러 타입 | 설명 | 사용 예시 |
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

## 트러블슈팅

### 테스트에서 `ShouldBeDomainError` 어설션이 실패함
**원인:** 에러 타입의 파라미터가 일치하지 않는 경우입니다. 예를 들어 `TooShort(MinLength: 8)`로 생성했는데 `new TooShort(MinLength: 3)`로 검증하면 실패합니다.
**해결:** 에러 타입의 파라미터까지 정확히 일치시켜야 합니다. sealed record 기반이므로 모든 필드가 동등성 비교에 포함됩니다.

### Custom 에러가 `ShouldBeDomainError`에서 인식되지 않음
**원인:** Custom 에러의 정의 위치가 잘못되었거나, `DomainErrorType.Custom`을 상속하지 않았을 수 있습니다.
**해결:** Custom 에러는 반드시 해당 레이어의 `Custom` abstract record를 상속해야 합니다. 예: `public sealed record InsufficientStock : DomainErrorType.Custom;`

---

## FAQ

### Q1. Domain 에러와 Application 에러의 구분 기준은?
Domain 에러는 도메인 모델 내부에서 발생하는 불변식 위반(VO 검증 실패, Entity 상태 규칙 위반)에 사용합니다. Application 에러는 Usecase 수준의 비즈니스 로직(중복 검사, 권한 확인, 리소스 조회 실패)에 사용합니다. 에러가 발생하는 코드의 위치(레이어)가 기준입니다.

### Q2. EventError는 언제 사용하나요?
도메인 이벤트 발행(`PublishFailed`, `PublishCancelled`)이나 이벤트 핸들러 실행 실패(`HandlerFailed`) 시 사용합니다. 이벤트 시스템 내부의 오류를 표현하기 위한 전용 에러 타입입니다. 에러 코드 접두사는 `ApplicationErrors.`를 사용합니다.

### Q3. 에러에 포함하는 현재 값(currentValue)은 어떤 정보를 넣어야 하나요?
디버깅에 도움이 되는 정보를 넣습니다. 주로 검증 실패한 입력값(`id.ToString()`, `request.Name`), 현재 상태값(`Status.ToString()`, `(int)StockQuantity`) 등입니다. 민감 정보(비밀번호, 토큰)는 포함하지 마세요.

---

## 참고 문서

- [05a-value-objects.md](./05a-value-objects) - 값 객체 구현 패턴, [05b-value-objects-validation.md](./05b-value-objects-validation) - 열거형·검증·FAQ
- [08a-error-system.md](./08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [08c-error-system-adapter-testing.md](./08c-error-system-adapter-testing) - Adapter 에러, Custom 에러, 테스트 모범 사례, 체크리스트
- [09-domain-services.md](./09-domain-services) - 도메인 서비스
- [15a-unit-testing.md](../testing/15a-unit-testing) - 단위 테스트 가이드
