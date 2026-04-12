---
title: "Error System — Adapter Errors and Testing"
---

This document covers Adapter errors, custom error definitions, testing best practices, and layer-specific checklists. For basic error handling principles and naming conventions, see [08a-error-system.md](./08a-error-system). For Domain/Application/Event errors, see [08b-error-system-domain-app.md](./08b-error-system-domain-app).

## Introduction

Domain/Application errors were covered in [08b-error-system-domain-app.md](./08b-error-system-domain-app). This document covers Adapter errors, custom error definition patterns, testing best practices, and layer-specific checklists.

> Adapter errors express failures in pipelines, external services, and data processing. Exceptions are wrapped with `AdapterError.FromException` to maintain error traceability, and assertions from `Functorium.Testing.Assertions.Errors` precisely verify error types and codes.

## Summary

### Key Commands

```csharp
// Adapter 에러
AdapterError.For<ProductRepository>(new NotFound(), id, "찾을 수 없습니다");
AdapterError.FromException<MyAdapter>(new ConnectionFailed("DB"), exception);

// 테스트 어설션
error.ShouldBeAdapterError<ProductRepository>(new AdapterErrorType.NotFound());
error.ShouldBeAdapterExceptionalError<UsecaseExceptionPipeline>(new AdapterErrorType.PipelineException());

// 범용 어설션
result.ShouldFailWithErrorCode("AdapterErrors.ProductRepository.NotFound");
error.ShouldBeErrorCodeExceptional<InvalidOperationException>("AdapterErrors.DatabaseAdapter.ConnectionFailed");
```

### Key Procedures

1. Adapter 에러: 표준 에러 타입 선택 또는 Custom sealed record 정의
2. `AdapterError.For` 또는 `AdapterError.FromException`으로 에러 생성
3. Custom 에러가 필요하면 `AdapterErrorType.Custom`을 상속한 sealed record 정의
4. 테스트 작성 - 레이어별 어설션 또는 범용 어설션 사용

### Key Concepts

| 레이어 | 팩토리 | 에러 코드 접두사 | 사용 시점 |
|--------|--------|-----------------|----------|
| Adapter | `AdapterError` | `AdapterErrors.` | 파이프라인, 외부 서비스, 데이터 |
| Custom | 각 레이어별 | 레이어에 따름 | 표준 에러로 표현 불가능한 경우 |

먼저 Adapter 에러의 생성 패턴을 살펴본 뒤, Custom 에러 정의, 테스트 모범 사례, 레이어별 체크리스트 순서로 진행합니다.

---

## Adapter 에러

### 에러 생성 및 반환

파이프라인, 외부 서비스, 데이터 처리 과정에서 발생하는 에러를 `AdapterError.For`로 생성합니다. 예외를 래핑할 때는 `AdapterError.FromException`을 사용합니다.

```csharp
using Functorium.Adapters.Errors;
using static Functorium.Adapters.Errors.AdapterErrorType;

// Basic usage - direct return via implicit conversion
return AdapterError.For<ProductRepository>(
    new NotFound(),
    id.ToString(),
    "상품을 찾을 수 없습니다");

// Generic value type
return AdapterError.For<HttpClientAdapter, string>(
    new Timeout(Duration: TimeSpan.FromSeconds(30)),
    url,
    "요청 타임아웃");

// Exception wrapping
return AdapterError.FromException<ExternalApiService>(
    new ConnectionFailed("ExternalApi"),
    exception);
```

### AdapterErrorType 전체 목록

아래 표는 Adapter 에러 타입을 범주별로 정리한 것입니다.

#### 공통 에러 타입 - R1, R3, R4, R5, R7

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |

#### Pipeline 관련 - R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `PipelineValidation` | 파이프라인 검증 실패 | `new PipelineValidation(PropertyName: "Id")` |
| `PipelineException` | 파이프라인 예외 발생 | `new PipelineException()` |

#### 외부 서비스 관련 - R1, R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `ExternalServiceUnavailable` | 외부 서비스 사용 불가 | `new ExternalServiceUnavailable(ServiceName: "PaymentGateway")` |
| `ConnectionFailed` | 연결 실패 | `new ConnectionFailed(Target: "database")` |
| `Timeout` | 타임아웃 | `new Timeout(Duration: TimeSpan.FromSeconds(30))` |

#### 데이터 관련 - R1, R8

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Serialization` | 직렬화 실패 | `new Serialization(Format: "JSON")` |
| `Deserialization` | 역직렬화 실패 | `new Deserialization(Format: "XML")` |
| `DataCorruption` | 데이터 손상 | `new DataCorruption()` |

#### 커스텀

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Custom` | 어댑터 특화 에러 (abstract) | `sealed record RateLimited : AdapterErrorType.Custom;` → `new RateLimited()` |

### Repository 구현 예시

`GetById`에서 `AdapterError.For`로 Not Found를 직접 반환하는 암시적 변환 패턴.

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

            // 암시적 변환으로 직접 반환
            return AdapterError.For<InMemoryProductRepository>(
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
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
                    $"상품 ID '{product.Id}'을(를) 찾을 수 없습니다");
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
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);
        });
    }
}
```

### 외부 API 서비스 구현 예시

HTTP 상태 코드별로 다른 에러 타입을 반환하는 `HandleHttpError` 패턴과, 예외 종류별 `FromException` 사용.

```csharp
[GenerateObservablePort]
public class ExternalPricingApiService : IExternalPricingService
{
    public sealed record OperationCancelled : AdapterErrorType.Custom;
    public sealed record UnexpectedException : AdapterErrorType.Custom;
    public sealed record RateLimited : AdapterErrorType.Custom;
    public sealed record HttpError : AdapterErrorType.Custom;

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

                // HTTP 오류 응답 처리 - 암시적 변환 활용
                if (!response.IsSuccessStatusCode)
                    return HandleHttpError<Money>(response, productCode);

                var priceResponse = await response.Content
                    .ReadFromJsonAsync<ExternalPriceResponse>(cancellationToken: cancellationToken);

                // null 응답 처리
                if (priceResponse is null)
                {
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(),
                        productCode,
                        $"외부 API 응답이 null입니다. ProductCode: {productCode}");
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
                    "요청이 취소되었습니다");
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
    /// HTTP 오류 응답을 AdapterError로 변환합니다.
    /// switch 표현식에서 암시적 변환이 자동 적용됩니다.
    /// </summary>
    private static Fin<T> HandleHttpError<T>(HttpResponseMessage response, string context) =>
        response.StatusCode switch
        {
            HttpStatusCode.NotFound => AdapterError.For<ExternalPricingApiService>(
                new NotFound(),
                context,
                $"외부 API에서 리소스를 찾을 수 없습니다. Context: {context}"),

            HttpStatusCode.Unauthorized => AdapterError.For<ExternalPricingApiService>(
                new Unauthorized(),
                context,
                "외부 API 인증에 실패했습니다"),

            HttpStatusCode.Forbidden => AdapterError.For<ExternalPricingApiService>(
                new Forbidden(),
                context,
                "외부 API 접근이 금지되었습니다"),

            HttpStatusCode.TooManyRequests => AdapterError.For<ExternalPricingApiService>(
                new RateLimited(),
                context,
                "외부 API 요청 제한에 도달했습니다"),

            HttpStatusCode.ServiceUnavailable => AdapterError.For<ExternalPricingApiService>(
                new ExternalServiceUnavailable("ExternalPricingApi"),
                context,
                "외부 가격 서비스를 사용할 수 없습니다"),

            _ => AdapterError.For<ExternalPricingApiService, HttpStatusCode>(
                new HttpError(),
                response.StatusCode,
                $"외부 API 호출 실패. Status: {response.StatusCode}")
        };
}
```

### Adapter 에러 테스트

테스트 어설션 네임스페이스:

```csharp
using Functorium.Testing.Assertions.Errors;
```

아래 표는 레이어별로 제공되는 어설션 메서드를 정리한 것입니다.

| 레이어 | Error 검증 | Fin<T> 검증 | Validation<Error, T> 검증 |
|--------|-----------|-------------|--------------------------|
| Domain | `ShouldBeDomainError` | `ShouldBeDomainError` | `ShouldHaveDomainError`, `ShouldHaveOnlyDomainError`, `ShouldHaveDomainErrors` |
| Application | `ShouldBeApplicationError` | `ShouldBeApplicationError` | `ShouldHaveApplicationError`, `ShouldHaveOnlyApplicationError`, `ShouldHaveApplicationErrors` |
| Adapter | `ShouldBeAdapterError`, `ShouldBeAdapterExceptionalError` | `ShouldBeAdapterError`, `ShouldBeAdapterExceptionalError` | `ShouldHaveAdapterError`, `ShouldHaveOnlyAdapterError`, `ShouldHaveAdapterErrors` |

#### Error 검증

```csharp
// 기본 에러 타입 검증
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

// 현재 값 포함 검증
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

// Exception wrapping 에러 검증
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

#### Fin<T> 검증

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

#### Validation<Error, T> 검증

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

Adapter 에러의 생성과 테스트 패턴을 확인했으니, 이제 표준 에러로 표현할 수 없는 상황을 위한 Custom 에러 정의 방법을 알아봅니다.

---

## Custom 에러

### 언제 Custom을 사용하는가?

1. **표준 에러로 표현 불가능한 경우**: 도메인/애플리케이션/어댑터 특화 상황
2. **의미가 명확한 경우**: 에러 이름만으로 상황을 이해할 수 있을 때
3. **재사용 가능성이 낮은 경우**: 특정 상황에서만 발생하는 에러

### Custom 에러 명명 규칙

```csharp
// ✅ Good - 명확하고 구체적
// public sealed record AlreadyShipped : DomainErrorType.Custom;
// public sealed record PaymentDeclined : ApplicationErrorType.Custom;
// public sealed record StockDepleted : DomainErrorType.Custom;
new AlreadyShipped()     // 이미 배송됨
new PaymentDeclined()    // 결제 거부됨
new StockDepleted()      // 재고 소진

// ❌ Bad - 모호하거나 너무 일반적
// sealed record Error : XxxErrorType.Custom;       // 의미 없음
// sealed record Failed : XxxErrorType.Custom;      // 너무 일반적
// sealed record Invalid : XxxErrorType.Custom;     // 구체적이지 않음
```

### 레이어별 Custom 에러 예시

다음 표는 각 레이어에서 흔히 정의되는 Custom 에러의 예시입니다.

| 레이어 | Custom 에러 예시 | Description |
|--------|-----------------|------|
| Domain | `AlreadyShipped`, `NotVerified`, `Expired` | 도메인 규칙 위반 |
| Application | `PaymentDeclined`, `QuotaExceeded`, `MaintenanceMode` | 비즈니스 프로세스 실패 |
| Adapter | `RateLimited`, `CircuitOpen`, `ServiceDegraded` | 인프라/외부 서비스 문제 |

### 표준 에러로 승격 기준

자주 사용되는 Custom 에러는 표준 에러 타입으로 승격을 고려합니다 ([08a 승격 기준](./08a-error-system#custom--표준-에러-승격-기준) 참조):

> 1. **3개 이상의 서로 다른 위치에서** 동일 Custom 에러 사용
> 2. **재사용 의미가 명확** (도메인 개념으로 자리잡음)
> 3. 기존 네이밍 규칙(R1-R8)에 **자연스럽게 매핑** 가능
> 4. **안정성 확인** (더 이상 의미가 변하지 않음)

```csharp
// 자주 사용되는 패턴 발견 시 표준 타입으로 추가
public sealed record Expired : DomainErrorType;
public sealed record Suspended : ApplicationErrorType;
public sealed record RateLimited : AdapterErrorType;
```

Custom 에러의 정의와 승격 기준을 이해했다면, 이제 에러 테스트를 효과적으로 작성하는 모범 사례를 살펴봅니다.

---

## 테스트 모범 사례

### 실패 케이스 테스트

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

### 테스트 명명 규칙

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

### Arrange-Act-Assert 패턴

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

### Theory를 사용한 파라미터화 테스트

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

### Custom 에러 테스트

```csharp
// Error type definition (nested in Order class):
// public sealed record AlreadyShipped : DomainErrorType.Custom;

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

### 범용 에러 Assertion 유틸리티

레이어별 Assertion(`ShouldBeDomainError`, `ShouldBeApplicationError`, `ShouldBeAdapterError`)과 별도로, **레이어에 의존하지 않는 범용 에러 검증** 유틸리티가 제공됩니다.

```csharp
using Functorium.Testing.Assertions.Errors;
```

#### ErrorCodeAssertions — 범용 에러 코드 검증

| Method | Description |
|--------|------|
| `error.ShouldHaveErrorCode()` | `IHasErrorCode` 구현 여부 검증, 인터페이스 반환 |
| `error.ShouldHaveErrorCode("code")` | 특정 에러 코드 일치 검증 |
| `error.ShouldHaveErrorCodeStartingWith("prefix")` | 에러 코드 접두사 검증 |
| `error.ShouldHaveErrorCode(predicate)` | predicate 기반 에러 코드 검증 |
| `error.ShouldBeExpected()` | Expected 타입 검증 |
| `error.ShouldBeExceptional()` | Exceptional 타입 검증 |
| `error.ShouldBeErrorCodeExpected("code", "value")` | `ErrorCodeExpected` 타입 + 코드 + 값 검증 |
| `error.ShouldBeErrorCodeExpected<T>("code", value)` | `ErrorCodeExpected<T>` 타입 + 코드 + 값 검증 |
| `error.ShouldBeErrorCodeExpected<T1, T2>("code", v1, v2)` | `ErrorCodeExpected<T1, T2>` 검증 |
| `error.ShouldBeErrorCodeExpected<T1, T2, T3>("code", v1, v2, v3)` | `ErrorCodeExpected<T1, T2, T3>` 검증 |
| `fin.ShouldSucceed()` | 성공 검증, 성공 값 반환 |
| `fin.ShouldSucceedWith(value)` | 성공 + 특정 값 검증 |
| `fin.ShouldFail()` | 실패 검증 |
| `fin.ShouldFail(errorAssertion)` | 실패 + 에러 assertion 실행 |
| `fin.ShouldFailWithErrorCode("code")` | 실패 + 특정 에러 코드 검증 |
| `validation.ShouldBeValid()` | 성공 검증, 성공 값 반환 |
| `validation.ShouldBeInvalid(errorsAssertion)` | 실패 + 에러 목록 assertion |
| `validation.ShouldContainErrorCode("code")` | 실패 + 특정 에러 코드 포함 검증 |
| `validation.ShouldContainOnlyErrorCode("code")` | 실패 + 에러가 정확히 1개이고 해당 코드 검증 |
| `validation.ShouldContainErrorCodes("code1", "code2")` | 실패 + 여러 에러 코드 포함 검증 |

```csharp
// 범용 에러 코드 검증 예시
[Fact]
public void Create_ShouldFail_WithExpectedErrorCode()
{
    // Arrange & Act
    var result = Email.Create("");

    // Assert — 레이어 무관하게 에러 코드만 검증
    result.ShouldFailWithErrorCode("DomainErrors.Email.Empty");
}

[Fact]
public void Validate_ShouldContain_MultipleErrorCodes()
{
    // Arrange & Act
    var result = Password.Validate("");

    // Assert
    result.ShouldContainErrorCodes(
        "DomainErrors.Password.Empty",
        "DomainErrors.Password.TooShort");
}
```

#### ErrorCodeExceptionalAssertions — 예외 기반 에러 검증

| Method | Description |
|--------|------|
| `error.ShouldBeErrorCodeExceptional("code")` | `ErrorCodeExceptional` 타입 + 에러 코드 검증 |
| `error.ShouldBeErrorCodeExceptional<TException>("code")` | 특정 예외 타입 래핑 검증 |
| `error.ShouldWrapException<TException>("code", message?)` | 예외 타입 + 선택적 메시지 검증 |
| `error.ShouldBeErrorCodeExceptional("code", exceptionAssertion)` | 예외 assertion 실행 |
| `fin.ShouldFailWithException("code")` | `Fin` 실패 + `ErrorCodeExceptional` 검증 |
| `fin.ShouldFailWithException<T, TException>("code")` | `Fin` 실패 + 특정 예외 타입 검증 |
| `validation.ShouldContainException("code")` | `Validation` 실패 + `ErrorCodeExceptional` 포함 검증 |
| `validation.ShouldContainException<T, TException>("code")` | `Validation` 실패 + 특정 예외 타입 포함 검증 |

```csharp
// Exception wrapping 에러 검증 예시
[Fact]
public void ShouldWrapException_WhenDatabaseFails()
{
    // Arrange
    var exception = new InvalidOperationException("DB connection lost");
    var error = AdapterError.FromException<DatabaseAdapter>(
        new AdapterErrorType.ConnectionFailed("database"),
        exception);

    // Assert
    error.ShouldBeErrorCodeExceptional<InvalidOperationException>(
        "AdapterErrors.DatabaseAdapter.ConnectionFailed");
}
```

#### ErrorAssertionHelpers — 확장 속성 (C# 14 Extension Members)

| Extension Property | Target Type | Description |
|-----------|----------|------|
| `error.ErrorCode` | `Error` | 에러 코드 추출 (`IHasErrorCode` 미구현 시 `null`) |
| `error.HasErrorCode` | `Error` | 에러 코드 존재 여부 |
| `validation.Errors` | `Validation<Error, T>` | 에러 목록 추출 (`IReadOnlyList<Error>`) |

```csharp
// 확장 속성 사용 예시
[Fact]
public void Error_ShouldHave_ErrorCode_Property()
{
    // Arrange
    var error = DomainError.For<Email>(new Empty(), "", "이메일은 비어있을 수 없습니다");

    // Assert — 확장 속성으로 간결하게 접근
    error.HasErrorCode.ShouldBeTrue();
    error.ErrorCode.ShouldBe("DomainErrors.Email.Empty");
}
```

테스트 작성 패턴을 익혔다면, 마지막으로 전체 에러 시스템을 레이어별로 정리하고 체크리스트로 마무리합니다.

---

## 레이어별 요약 + 체크리스트

### Domain (DomainErrorType)

```
값 존재:     Empty, Null
길이:        TooShort, TooLong, WrongLength
형식:        InvalidFormat
대소문자:    NotUpperCase, NotLowerCase
날짜:        DefaultDate, NotInPast, NotInFuture, TooLate, TooEarly
범위:        RangeInverted, RangeEmpty
숫자 범위:   Zero, Negative, NotPositive, OutOfRange, BelowMinimum, AboveMaximum
존재 여부:   NotFound, AlreadyExists, Duplicate
비교:        Mismatch
커스텀:      Custom (abstract → sealed record MyError : DomainErrorType.Custom)
```

### Application (ApplicationErrorType)

```
공통:        Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState
권한:        Unauthorized, Forbidden
검증:        ValidationFailed
비즈니스:    BusinessRuleViolated, ConcurrencyConflict, ResourceLocked,
             OperationCancelled, InsufficientPermission
커스텀:      Custom (abstract → sealed record MyError : ApplicationErrorType.Custom)
```

### Event (EventErrorType)

```
발행:        PublishFailed, PublishCancelled
핸들러:      HandlerFailed
검증:        InvalidEventType
커스텀:      Custom (abstract → sealed record MyError : EventErrorType.Custom)
```

### Adapter (AdapterErrorType)

```
공통:        Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState,
             Unauthorized, Forbidden
파이프라인:  PipelineValidation, PipelineException
외부서비스:  ExternalServiceUnavailable, ConnectionFailed, Timeout
데이터:      Serialization, Deserialization, DataCorruption
커스텀:      Custom (abstract → sealed record MyError : AdapterErrorType.Custom)
```

### 레이어별 사용 시점

| Layer | When to Use |
|--------|----------|
| **Domain** | Value Object 검증 실패, Entity 불변성 위반, Aggregate 비즈니스 규칙 위반 |
| **Application** | 유스케이스 실행 중 비즈니스 로직 오류, 권한/인증 오류, 데이터 조회 실패, 동시성 충돌 |
| **Adapter** | 파이프라인 검증/예외 처리, 외부 서비스 호출 실패, 직렬화/역직렬화 오류, 연결/타임아웃 오류 |

### 에러 코드 형식

모든 에러 코드는 다음 형식을 따릅니다:

```
{LayerPrefix}.{TypeName}.{ErrorName}
```

| Layer | Prefix | Example |
|--------|--------|------|
| Domain | `DomainErrors` | `DomainErrors.Email.Empty` |
| Application | `ApplicationErrors` | `ApplicationErrors.CreateProductCommand.NotFound` |
| Adapter | `AdapterErrors` | `AdapterErrors.ProductRepository.NotFound` |

### 에러 정의 체크리스트

- [ ] 적절한 레이어(Domain/Application/Adapter)를 선택했는가?
- [ ] 표준 에러 타입으로 표현 가능한지 먼저 확인했는가?
- [ ] Custom 에러 이름이 충분히 명확한가?
- [ ] 컨텍스트 정보(파라미터)가 디버깅에 도움이 되는가?
- [ ] 에러 메시지가 사용자/개발자에게 유용한가?

### 에러 반환 체크리스트

- [ ] `Fin.Fail<T>(error)` 대신 암시적 변환을 사용했는가?
- [ ] 성공 반환 시 `Fin.Succ(value)`를 사용했는가?
- [ ] 예외 처리 시 `FromException` 메서드를 사용했는가?
- [ ] 레이어에 맞는 에러 팩토리(`DomainError`, `ApplicationError`, `AdapterError`)를 사용했는가?

### 네이밍 체크리스트

- [ ] 적절한 규칙(R1-R8)을 적용했는가?
- [ ] 대칭 쌍이 있다면 일관성을 유지했는가? (Below ↔ Above)
- [ ] 컨텍스트 정보가 필요한가? (MinLength, Pattern, PropertyName 등)
- [ ] 에러 메시지가 에러 이름과 일관성 있는가?

### 테스트 체크리스트

- [ ] 모든 에러 케이스에 대한 테스트가 있는가?
- [ ] 에러 타입이 정확히 일치하는지 검증하는가?
- [ ] 필요한 경우 현재 값도 검증하는가?
- [ ] Custom 에러의 이름이 정확히 일치하는지 검증하는가?
- [ ] 유효한 입력에 대한 성공 테스트가 있는가?
- [ ] 경계값(boundary values)에 대한 테스트가 있는가?
- [ ] 반환값이 예상과 일치하는지 검증하는가?

---

## Troubleshooting

### `FromException` 사용 시 에러 코드가 기대와 다름
**Cause:** `FromException`은 `ErrorCodeExceptional` 타입을 생성하므로 `ShouldBeAdapterError` 대신 `ShouldBeAdapterExceptionalError`를 사용해야 합니다.
**Resolution:** 예외 래핑 에러는 `ShouldBeAdapterExceptionalError<TAdapter>(errorType)` 또는 `ShouldBeAdapterExceptionalError<TAdapter, TException>(errorType)`으로 검증하세요.

### Custom 에러가 레이어별 어설션에서 인식되지 않음
**Cause:** Custom 에러의 정의 위치가 잘못되었거나, 해당 레이어의 `Custom`을 상속하지 않았을 수 있습니다.
**Resolution:** Custom 에러는 반드시 해당 레이어의 `Custom` abstract record를 상속해야 합니다. 예: `public sealed record RateLimited : AdapterErrorType.Custom;`

---

## FAQ

### Q1. 범용 어설션과 레이어별 어설션 중 어느 것을 사용해야 하나요?
레이어별 어설션(`ShouldBeDomainError`, `ShouldBeApplicationError`, `ShouldBeAdapterError`)은 에러의 출처까지 검증하므로 더 엄격합니다. 범용 어설션(`ShouldFailWithErrorCode`, `ShouldContainErrorCode`)은 에러 코드만 검증하므로 레이어에 독립적인 테스트에 적합합니다. 일반적으로 레이어별 어설션을 권장합니다.

### Q2. Custom 에러를 표준 에러로 승격해야 하는 시점은?
4가지 조건을 모두 충족할 때입니다: (1) 3개 이상의 서로 다른 위치에서 사용, (2) 재사용 의미가 명확, (3) R1-R8 네이밍 규칙에 자연스럽게 매핑 가능, (4) 의미가 안정적(더 이상 변하지 않음).

### Q3. 에러에 포함하는 현재 값(currentValue)은 어떤 정보를 넣어야 하나요?
디버깅에 도움이 되는 정보를 넣습니다. 주로 검증 실패한 입력값(`id.ToString()`, `request.Name`), 현재 상태값(`Status.ToString()`, `(int)StockQuantity`) 등입니다. 민감 정보(비밀번호, 토큰)는 포함하지 마세요.

---

## References

- [08a-error-system.md](./08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [08b-error-system-domain-app.md](./08b-error-system-domain-app) - Domain/Application/Event 에러 정의와 테스트
- [13-adapters.md](../adapter/13-adapters) - Adapter 구현 가이드
- [15a-unit-testing.md](../testing/15a-unit-testing) - 단위 테스트 가이드
- [16-testing-library.md](../testing/16-testing-library) - 에러 외 테스트 유틸리티 (로그/아키텍처/소스생성기/Job 테스트)
