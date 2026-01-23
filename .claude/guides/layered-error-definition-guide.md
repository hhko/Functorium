# 레이어별 에러 코드 정의 가이드

## 1. 개요

이 문서는 Functorium 프로젝트에서 각 아키텍처 레이어(Domain, Application, Adapter)별로 에러 코드를 정의하는 방법을 설명합니다.

### 1.1 에러 코드 형식

모든 에러 코드는 다음 형식을 따릅니다:

```
{LayerPrefix}.{TypeName}.{ErrorName}
```

| 레이어 | 접두사 | 예시 |
|--------|--------|------|
| Domain | `DomainErrors` | `DomainErrors.Email.Empty` |
| Application | `ApplicationErrors` | `ApplicationErrors.CreateProductCommand.NotFound` |
| Adapter | `AdapterErrors` | `AdapterErrors.UsecaseValidationPipeline.PipelineValidation` |

---

## 2. Domain 레이어 에러

### 2.1 사용 시점

- Value Object 검증 실패
- Entity 불변성 위반
- Aggregate 비즈니스 규칙 위반

### 2.2 에러 생성

```csharp
using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;

// 기본 사용법
var error = DomainError.For<Email>(
    new Empty(),
    currentValue: "",
    message: "Email cannot be empty");

// 제네릭 값 타입
var error = DomainError.For<Age, int>(
    new Negative(),
    currentValue: -5,
    message: "Age cannot be negative");

// 두 개의 값 포함
var error = DomainError.For<DateRange, DateTime, DateTime>(
    new Custom("InvalidRange"),
    startDate,
    endDate,
    message: "Start date must be before end date");

// 세 개의 값 포함
var error = DomainError.For<Triangle, double, double, double>(
    new Custom("InvalidTriangle"),
    sideA, sideB, sideC,
    message: "Invalid triangle sides");
```

### 2.3 DomainErrorType 목록

#### 값 존재 검증

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Empty` | 비어있음 (null, empty string, empty collection) | `new Empty()` |
| `Null` | null임 | `new Null()` |

#### 문자열/컬렉션 길이 검증

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `TooShort` | 최소 길이 미만 | `new TooShort(MinLength: 8)` |
| `TooLong` | 최대 길이 초과 | `new TooLong(MaxLength: 100)` |
| `WrongLength` | 정확한 길이 불일치 | `new WrongLength(Expected: 10)` |

#### 형식 검증

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `InvalidFormat` | 형식 불일치 | `new InvalidFormat(Pattern: @"^\d{3}-\d{4}$")` |

#### 대소문자 검증

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `NotUpperCase` | 대문자가 아님 | `new NotUpperCase()` |
| `NotLowerCase` | 소문자가 아님 | `new NotLowerCase()` |

#### 숫자 범위 검증

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Negative` | 음수임 | `new Negative()` |
| `NotPositive` | 양수가 아님 (0 포함) | `new NotPositive()` |
| `OutOfRange` | 범위 밖 | `new OutOfRange(Min: "1", Max: "100")` |
| `BelowMinimum` | 최소값 미만 | `new BelowMinimum(Minimum: "0")` |
| `AboveMaximum` | 최대값 초과 | `new AboveMaximum(Maximum: "1000")` |

#### 존재 여부 검증

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |

#### 비즈니스 규칙 검증

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Mismatch` | 값 불일치 | `new Mismatch()` |

#### 커스텀 에러

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Custom` | 도메인 특화 에러 | `new Custom("AlreadyShipped")` |

### 2.4 Value Object에서 사용 예시

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        Validate(value).ToFin();

    public static Validation<Error, Email> Validate(string? value) =>
        ValidationRules.NotEmpty<Email>(value)
            .ThenMatches<Email>(EmailPattern)
            .ThenMaxLength<Email>(MaxLength)
            .Map(v => new Email(v));

    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$");
    private const int MaxLength = 256;
}
```

---

## 3. Application 레이어 에러

### 3.1 사용 시점

- 유스케이스(Command/Query) 실행 중 비즈니스 로직 오류
- 권한/인증 오류
- 데이터 조회 실패
- 동시성 충돌

### 3.2 에러 생성

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;

// 기본 사용법
var error = ApplicationError.For<CreateProductCommand>(
    new AlreadyExists(),
    currentValue: productId,
    message: "Product already exists");

// 제네릭 값 타입
var error = ApplicationError.For<UpdateOrderCommand, Guid>(
    new NotFound(),
    currentValue: orderId,
    message: "Order not found");

// 두 개의 값 포함
var error = ApplicationError.For<TransferCommand, decimal, decimal>(
    new BusinessRuleViolated("InsufficientBalance"),
    balance,
    amount,
    message: "Insufficient balance for transfer");
```

### 3.3 ApplicationErrorType 목록

#### 공통 에러 타입

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |

#### 검증 관련

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `ValidationFailed` | 검증 실패 | `new ValidationFailed(PropertyName: "Quantity")` |

#### 비즈니스 규칙

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `BusinessRuleViolated` | 비즈니스 규칙 위반 | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `ConcurrencyConflict` | 동시성 충돌 | `new ConcurrencyConflict()` |
| `ResourceLocked` | 리소스 잠금 | `new ResourceLocked(ResourceName: "Order")` |
| `OperationCancelled` | 작업 취소됨 | `new OperationCancelled()` |
| `InsufficientPermission` | 권한 부족 | `new InsufficientPermission(Permission: "Admin")` |

#### 커스텀 에러

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Custom` | 애플리케이션 특화 에러 | `new Custom("PaymentDeclined")` |

### 3.4 유스케이스에서 사용 예시

```csharp
public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, FinResponse<ProductId>>
{
    public async ValueTask<FinResponse<ProductId>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // 중복 체크
        if (await _repository.ExistsAsync(command.ProductCode))
        {
            return ApplicationError.For<CreateProductCommand>(
                new AlreadyExists(),
                command.ProductCode,
                "Product with this code already exists");
        }

        // 비즈니스 규칙 검증
        if (command.Price <= 0)
        {
            return ApplicationError.For<CreateProductCommand, decimal>(
                new BusinessRuleViolated("PositivePrice"),
                command.Price,
                "Price must be positive");
        }

        // 성공 처리
        var product = Product.Create(command.ProductCode, command.Name, command.Price);
        await _repository.AddAsync(product);
        return product.Id;
    }
}
```

---

## 4. Adapter 레이어 에러

### 4.1 사용 시점

- 파이프라인 검증/예외 처리
- 외부 서비스 호출 실패
- 데이터 직렬화/역직렬화 오류
- 연결/타임아웃 오류

### 4.2 에러 생성

```csharp
using Functorium.Adapters.Errors;
using static Functorium.Adapters.Errors.AdapterErrorType;

// 기본 사용법
var error = AdapterError.For<UsecaseValidationPipeline>(
    new PipelineValidation(PropertyName: "Name"),
    currentValue: "",
    message: "Name is required");

// 제네릭 값 타입
var error = AdapterError.For<HttpClientAdapter, string>(
    new Timeout(Duration: TimeSpan.FromSeconds(30)),
    currentValue: url,
    message: "Request timed out");

// 예외 래핑
var error = AdapterError.FromException<UsecaseExceptionPipeline>(
    new PipelineException(),
    exception);
```

### 4.3 AdapterErrorType 목록

#### 공통 에러 타입

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Empty` | 비어있음 | `new Empty()` |
| `Null` | null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |

#### Pipeline 관련

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `PipelineValidation` | 파이프라인 검증 실패 | `new PipelineValidation(PropertyName: "Id")` |
| `PipelineException` | 파이프라인 예외 발생 | `new PipelineException()` |

#### 외부 서비스 관련

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `ExternalServiceUnavailable` | 외부 서비스 사용 불가 | `new ExternalServiceUnavailable(ServiceName: "PaymentGateway")` |
| `ConnectionFailed` | 연결 실패 | `new ConnectionFailed(Target: "database")` |
| `Timeout` | 타임아웃 | `new Timeout(Duration: TimeSpan.FromSeconds(30))` |

#### 데이터 관련

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Serialization` | 직렬화 실패 | `new Serialization(Format: "JSON")` |
| `Deserialization` | 역직렬화 실패 | `new Deserialization(Format: "XML")` |
| `DataCorruption` | 데이터 손상 | `new DataCorruption()` |

#### 커스텀 에러

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Custom` | 어댑터 특화 에러 | `new Custom("RateLimited")` |

### 4.4 파이프라인에서 사용 예시

```csharp
internal sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.IsEmpty)
            return await next(request, cancellationToken);

        Error[] errors = _validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(failure => AdapterError.For<UsecaseValidationPipeline<TRequest, TResponse>, Dictionary<string, object>>(
                new PipelineValidation(failure.PropertyName),
                failure.FormattedMessagePlaceholderValues,
                $"{failure.PropertyName}: {failure.ErrorMessage}"))
            .Distinct()
            .ToArray();

        if (errors.Length is not 0)
        {
            var error = errors.Length == 1 ? errors[0] : Error.Many(errors);
            return TResponse.CreateFail(error);
        }

        return await next(request, cancellationToken);
    }
}
```

---

## 5. 커스텀 에러 정의 지침

### 5.1 언제 Custom을 사용하는가?

1. **표준 에러로 표현 불가능한 경우**: 도메인 특화 비즈니스 규칙
2. **의미가 명확한 경우**: 에러 이름만으로 상황을 이해할 수 있을 때
3. **재사용 가능성이 낮은 경우**: 특정 상황에서만 발생하는 에러

### 5.2 Custom 에러 명명 규칙

```csharp
// ✅ Good - 명확하고 구체적
new Custom("AlreadyShipped")     // 이미 배송됨
new Custom("PaymentDeclined")    // 결제 거부됨
new Custom("StockDepleted")      // 재고 소진

// ❌ Bad - 모호하거나 너무 일반적
new Custom("Error")              // 의미 없음
new Custom("Failed")             // 너무 일반적
new Custom("Invalid")            // 구체적이지 않음
```

### 5.3 새로운 표준 에러 타입 추가

자주 사용되는 Custom 에러는 표준 에러 타입으로 승격을 고려합니다:

```csharp
// 자주 사용되는 패턴 발견 시 표준 타입으로 추가
public sealed record Expired : DomainErrorType;
public sealed record Suspended : ApplicationErrorType;
public sealed record RateLimited : AdapterErrorType;
```

---

## 6. 에러 코드 네이밍 체크리스트

- [ ] 적절한 레이어(Domain/Application/Adapter)를 선택했는가?
- [ ] 표준 에러 타입으로 표현 가능한지 먼저 확인했는가?
- [ ] Custom 에러 이름이 충분히 명확한가?
- [ ] 컨텍스트 정보(파라미터)가 디버깅에 도움이 되는가?
- [ ] 에러 메시지가 사용자/개발자에게 유용한가?

---

## 7. 참고 문서

- [layered-error-naming-guide.md](./layered-error-naming-guide.md) - 레이어별 에러 이름 규칙
- [layered-error-testing-guide.md](./layered-error-testing-guide.md) - 레이어별 에러 테스트 방법

---

## 8. 변경 이력

| 날짜 | 변경 사항 | 작성자 |
|------|----------|--------|
| 2026-01-23 | 최초 작성 - 레이어별 에러 정의 가이드 | - |
