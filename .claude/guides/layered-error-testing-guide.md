# 레이어별 에러 코드 테스트 가이드

## 1. 개요

이 문서는 Functorium 프로젝트에서 각 아키텍처 레이어(Domain, Application, Adapter)별로 에러 코드를 테스트하는 방법을 설명합니다.

### 1.1 테스트 어설션 네임스페이스

```csharp
using Functorium.Testing.Assertions.Errors;
```

### 1.2 어설션 메서드 요약

| 레이어 | Error 검증 | Fin<T> 검증 | Validation<Error, T> 검증 |
|--------|-----------|-------------|--------------------------|
| Domain | `ShouldBeDomainError` | `ShouldBeDomainError` | `ShouldHaveDomainError`, `ShouldHaveOnlyDomainError`, `ShouldHaveDomainErrors` |
| Application | `ShouldBeApplicationError` | `ShouldBeApplicationError` | `ShouldHaveApplicationError`, `ShouldHaveOnlyApplicationError`, `ShouldHaveApplicationErrors` |
| Adapter | `ShouldBeAdapterError`, `ShouldBeAdapterExceptionalError` | `ShouldBeAdapterError`, `ShouldBeAdapterExceptionalError` | `ShouldHaveAdapterError`, `ShouldHaveOnlyAdapterError`, `ShouldHaveAdapterErrors` |

---

## 2. Domain 에러 테스트

### 2.1 Error 검증

#### 기본 에러 타입 검증

```csharp
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
```

#### 현재 값 포함 검증

```csharp
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
```

#### 두 개의 값 포함 검증

```csharp
[Fact]
public void ShouldBeDomainError_WithTwoValues_WhenRangeIsInvalid()
{
    // Arrange
    var startDate = new DateTime(2024, 12, 31);
    var endDate = new DateTime(2024, 1, 1);
    var error = DomainError.For<DateRange, DateTime, DateTime>(
        new DomainErrorType.Custom("InvalidRange"),
        startDate,
        endDate,
        message: "Start date must be before end date");

    // Act & Assert
    error.ShouldBeDomainError<DateRange, DateTime, DateTime>(
        new DomainErrorType.Custom("InvalidRange"),
        expectedValue1: startDate,
        expectedValue2: endDate);
}
```

#### 세 개의 값 포함 검증

```csharp
[Fact]
public void ShouldBeDomainError_WithThreeValues()
{
    // Arrange
    var error = DomainError.For<Triangle, double, double, double>(
        new DomainErrorType.Custom("InvalidTriangle"),
        1.0, 2.0, 10.0,
        message: "Invalid triangle sides");

    // Act & Assert
    error.ShouldBeDomainError<Triangle, double, double, double>(
        new DomainErrorType.Custom("InvalidTriangle"),
        expectedValue1: 1.0,
        expectedValue2: 2.0,
        expectedValue3: 10.0);
}
```

### 2.2 Fin<T> 검증

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

### 2.3 Validation<Error, T> 검증

#### 특정 에러 포함 여부 검증

```csharp
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
```

#### 정확히 하나의 에러만 포함 검증

```csharp
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
```

#### 여러 에러 모두 포함 검증

```csharp
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
```

#### 현재 값 포함 검증

```csharp
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

## 3. Application 에러 테스트

### 3.1 Error 검증

#### 기본 에러 타입 검증

```csharp
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
```

#### 현재 값 포함 검증

```csharp
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
```

#### 두 개의 값 포함 검증

```csharp
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

### 3.2 Fin<T> 검증

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

### 3.3 Validation<Error, T> 검증

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

## 4. Adapter 에러 테스트

### 4.1 Error 검증

#### 기본 에러 타입 검증

```csharp
[Fact]
public void ShouldBeAdapterError_WhenValidationFails()
{
    // Arrange
    var error = AdapterError.For<UsecaseValidationPipeline>(
        new AdapterErrorType.PipelineValidation("ProductName"),
        currentValue: "",
        message: "ProductName is required");

    // Act & Assert
    error.ShouldBeAdapterError<UsecaseValidationPipeline>(
        new AdapterErrorType.PipelineValidation("ProductName"));
}
```

#### 현재 값 포함 검증

```csharp
[Fact]
public void ShouldBeAdapterError_WithValue_WhenTimeout()
{
    // Arrange
    var url = "https://api.example.com/data";
    var error = AdapterError.For<HttpClientAdapter, string>(
        new AdapterErrorType.Timeout(Duration: TimeSpan.FromSeconds(30)),
        currentValue: url,
        message: "Request timed out");

    // Act & Assert
    error.ShouldBeAdapterError<HttpClientAdapter, string>(
        new AdapterErrorType.Timeout(Duration: TimeSpan.FromSeconds(30)),
        expectedCurrentValue: url);
}
```

#### 예외 래핑 에러 검증

```csharp
[Fact]
public void ShouldBeAdapterExceptionalError_WhenExceptionOccurs()
{
    // Arrange
    var exception = new InvalidOperationException("Something went wrong");
    var error = AdapterError.FromException<UsecaseExceptionPipeline>(
        new AdapterErrorType.PipelineException(),
        exception);

    // Act & Assert
    error.ShouldBeAdapterExceptionalError<UsecaseExceptionPipeline>(
        new AdapterErrorType.PipelineException());
}

[Fact]
public void ShouldBeAdapterExceptionalError_WithExceptionType()
{
    // Arrange
    var exception = new TimeoutException("Connection timed out");
    var error = AdapterError.FromException<DatabaseAdapter>(
        new AdapterErrorType.ConnectionFailed("database"),
        exception);

    // Act & Assert
    error.ShouldBeAdapterExceptionalError<DatabaseAdapter, TimeoutException>(
        new AdapterErrorType.ConnectionFailed("database"));
}
```

### 4.2 Fin<T> 검증

```csharp
[Fact]
public void Fin_ShouldBeAdapterError_WhenServiceUnavailable()
{
    // Arrange
    Fin<PaymentResult> fin = AdapterError.For<PaymentGatewayAdapter>(
        new AdapterErrorType.ExternalServiceUnavailable("PaymentGateway"),
        currentValue: "https://payment.example.com",
        message: "Payment service unavailable");

    // Act & Assert
    fin.ShouldBeAdapterError<PaymentGatewayAdapter, PaymentResult>(
        new AdapterErrorType.ExternalServiceUnavailable("PaymentGateway"));
}

[Fact]
public void Fin_ShouldBeAdapterExceptionalError()
{
    // Arrange
    Fin<Unit> fin = AdapterError.FromException<UsecaseExceptionPipeline>(
        new AdapterErrorType.PipelineException(),
        new Exception("Unexpected error"));

    // Act & Assert
    fin.ShouldBeAdapterExceptionalError<UsecaseExceptionPipeline, Unit>(
        new AdapterErrorType.PipelineException());
}
```

### 4.3 Validation<Error, T> 검증

```csharp
[Fact]
public void Validation_ShouldHaveAdapterError()
{
    // Arrange
    Validation<Error, Unit> validation = Fail<Error, Unit>(
        AdapterError.For<CacheAdapter>(
            new AdapterErrorType.ConnectionFailed("Redis"),
            currentValue: "localhost:6379",
            message: "Cannot connect to Redis"));

    // Act & Assert
    validation.ShouldHaveAdapterError<CacheAdapter, Unit>(
        new AdapterErrorType.ConnectionFailed("Redis"));
}

[Fact]
public void Validation_ShouldHaveOnlyAdapterError()
{
    // Arrange
    Validation<Error, byte[]> validation = Fail<Error, byte[]>(
        AdapterError.For<MessageSerializer>(
            new AdapterErrorType.Serialization("JSON"),
            currentValue: "invalid-object",
            message: "Failed to serialize object to JSON"));

    // Act & Assert
    validation.ShouldHaveOnlyAdapterError<MessageSerializer, byte[]>(
        new AdapterErrorType.Serialization("JSON"));
}

[Fact]
public void Validation_ShouldHaveAdapterErrors()
{
    // Arrange
    var error1 = AdapterError.For<UsecaseValidationPipeline>(
        new AdapterErrorType.PipelineValidation("Name"),
        currentValue: "",
        message: "Name is required");

    var error2 = AdapterError.For<UsecaseValidationPipeline>(
        new AdapterErrorType.PipelineValidation("Price"),
        currentValue: "-1",
        message: "Price must be positive");

    Validation<Error, Unit> validation = Fail<Error, Unit>(Error.Many(error1, error2));

    // Act & Assert
    validation.ShouldHaveAdapterErrors<UsecaseValidationPipeline, Unit>(
        new AdapterErrorType.PipelineValidation("Name"),
        new AdapterErrorType.PipelineValidation("Price"));
}
```

---

## 5. 실패 케이스 테스트

에러가 발생하지 않아야 하는 성공 케이스도 테스트해야 합니다:

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

---

## 6. 테스트 패턴 모범 사례

### 6.1 테스트 명명 규칙

```csharp
// 패턴: [Method]_Should[Behavior]_When[Condition]

// Error 검증
ShouldBeDomainError_WhenValueIsEmpty
ShouldBeApplicationError_WhenProductNotFound
ShouldBeAdapterError_WhenValidationFails

// Fin 검증
Create_ShouldFail_WhenEmailIsInvalid
Execute_ShouldFail_WhenProductNotFound

// Validation 검증
Validate_ShouldHaveError_WhenPasswordTooShort
Validate_ShouldHaveMultipleErrors_WhenMultipleValidationsFail
```

### 6.2 Arrange-Act-Assert 패턴

```csharp
[Fact]
public void Create_ShouldFail_WhenEmailIsEmpty()
{
    // Arrange
    var emptyEmail = "";

    // Act
    var result = Email.Create(emptyEmail);

    // Assert
    result.ShouldBeDomainError<Email, Email>(new DomainErrorType.Empty());
}
```

### 6.3 Theory를 사용한 파라미터화 테스트

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
    result.ShouldBeDomainError<Email, Email>(new DomainErrorType.Empty());
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
    result.ShouldBeDomainError<Email, Email>(new DomainErrorType.InvalidFormat());
}
```

### 6.4 Custom 에러 테스트

```csharp
[Fact]
public void Cancel_ShouldFail_WhenOrderAlreadyShipped()
{
    // Arrange
    var error = DomainError.For<Order>(
        new DomainErrorType.Custom("AlreadyShipped"),
        currentValue: "ORDER-001",
        message: "Cannot cancel shipped order");

    // Act & Assert
    error.ShouldBeDomainError<Order>(new DomainErrorType.Custom("AlreadyShipped"));
}
```

---

## 7. 테스트 체크리스트

### 7.1 에러 테스트 체크리스트

- [ ] 모든 에러 케이스에 대한 테스트가 있는가?
- [ ] 에러 타입이 정확히 일치하는지 검증하는가?
- [ ] 필요한 경우 현재 값도 검증하는가?
- [ ] Custom 에러의 이름이 정확히 일치하는지 검증하는가?

### 7.2 성공 케이스 체크리스트

- [ ] 유효한 입력에 대한 성공 테스트가 있는가?
- [ ] 경계값(boundary values)에 대한 테스트가 있는가?
- [ ] 반환값이 예상과 일치하는지 검증하는가?

---

## 8. 참고 문서

- [layered-error-definition-guide.md](./layered-error-definition-guide.md) - 레이어별 에러 정의 방법
- [layered-error-naming-guide.md](./layered-error-naming-guide.md) - 레이어별 에러 이름 규칙
- [unit-testing-guide.md](./unit-testing-guide.md) - 단위 테스트 가이드

---

## 9. 변경 이력

| 날짜 | 변경 사항 | 작성자 |
|------|----------|--------|
| 2026-01-23 | 최초 작성 - 레이어별 에러 테스트 가이드 | - |
