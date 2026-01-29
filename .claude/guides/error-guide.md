# 레이어별 에러 시스템 가이드

이 문서는 Functorium 프로젝트에서 각 아키텍처 레이어(Domain, Application, Adapter)별로 에러를 정의하고 명명하는 방법을 설명합니다.

## 목차

- [1. 개요](#1-개요)
- [2. 에러 네이밍 규칙](#2-에러-네이밍-규칙)
- [3. Domain 레이어 에러](#3-domain-레이어-에러)
- [4. Application 레이어 에러](#4-application-레이어-에러)
- [5. Adapter 레이어 에러](#5-adapter-레이어-에러)
- [6. Custom 에러 가이드](#6-custom-에러-가이드)
- [7. 체크리스트](#7-체크리스트)

---

## 1. 개요

### 에러 코드 형식

모든 에러 코드는 다음 형식을 따릅니다:

```
{LayerPrefix}.{TypeName}.{ErrorName}
```

| 레이어 | 접두사 | 예시 |
|--------|--------|------|
| Domain | `DomainErrors` | `DomainErrors.Email.Empty` |
| Application | `ApplicationErrors` | `ApplicationErrors.CreateProductCommand.NotFound` |
| Adapter | `AdapterErrors` | `AdapterErrors.UsecaseValidationPipeline.PipelineValidation` |

### 레이어별 사용 시점

| 레이어 | 사용 시점 |
|--------|----------|
| **Domain** | Value Object 검증 실패, Entity 불변성 위반, Aggregate 비즈니스 규칙 위반 |
| **Application** | 유스케이스 실행 중 비즈니스 로직 오류, 권한/인증 오류, 데이터 조회 실패, 동시성 충돌 |
| **Adapter** | 파이프라인 검증/예외 처리, 외부 서비스 호출 실패, 직렬화/역직렬화 오류, 연결/타임아웃 오류 |

---

## 2. 에러 네이밍 규칙

### 빠른 참조: 네이밍 규칙 요약

| 규칙 | 적용 조건 | 패턴 | 예시 |
|------|----------|------|------|
| **R1** | 상태가 자명한 문제 | 상태 그대로 | `Empty`, `Null`, `Negative`, `Duplicate` |
| **R2** | 기준 대비 비교 | `Too-` / `Below-` / `Above-` / `OutOf-` | `TooShort`, `BelowMinimum`, `OutOfRange` |
| **R3** | 기대 조건 불충족 | `Not-` + 기대 | `NotPositive`, `NotUpperCase`, `NotFound` |
| **R4** | 이미 발생한 상태 | `Already-` + 상태 | `AlreadyExists` |
| **R5** | 형식/구조 문제 | `Invalid-` + 대상 | `InvalidFormat`, `InvalidState` |
| **R6** | 두 값 불일치 | `Mismatch` / `Wrong-` | `Mismatch`, `WrongLength` |
| **R7** | 권한/인증 문제 | 상태 그대로 | `Unauthorized`, `Forbidden` |
| **R8** | 작업/프로세스 문제 | 동사 과거분사 + 명사 | `ValidationFailed`, `OperationCancelled` |

### 규칙 상세 설명

#### R1: 자명한 상태 → 상태 그대로

**적용 조건**: 그 자체로 "문제"임이 명백한 경우

```csharp
// ✅ Correct
new Empty()      // 비어있음 → 문제
new Null()       // null임 → 문제
new Negative()   // 음수임 → 문제
new Duplicate()  // 중복됨 → 문제

// ❌ Incorrect
new NotFilled()  // Empty로 충분
new IsNull()     // Null로 충분
```

#### R2: 기준 대비 비교 → 비교 표현

**적용 조건**: 최소/최대/범위 등 기준값과 비교가 필요한 경우

```csharp
// ✅ Correct
new TooShort(MinLength: 8)        // 최소 길이 미만
new TooLong(MaxLength: 100)       // 최대 길이 초과
new BelowMinimum(Minimum: "0")    // 최소값 미만
new AboveMaximum(Maximum: "100")  // 최대값 초과
new OutOfRange(Min: "1", Max: "10") // 범위 밖

// ❌ Incorrect
new Short()      // 기준 불명확
new Long()       // 기준 불명확
```

| 접두사 | 의미 | 사용 상황 |
|--------|------|----------|
| `Too-` | 과도함/부족함 | 길이, 크기 등 상대적 비교 |
| `Below-` | 미만 | 최소 기준 미충족 |
| `Above-` | 초과 | 최대 기준 초과 |
| `OutOf-` | 범위 벗어남 | 허용 범위 외 |

#### R3: 기대 조건 불충족 → Not + 기대

**적용 조건**: "~여야 하는데 아님"을 표현해야 하는 경우

```csharp
// ✅ Correct
new NotPositive()   // 양수여야 함 (0도 에러)
new NotUpperCase()  // 대문자여야 함
new NotLowerCase()  // 소문자여야 함
new NotFound()      // 존재해야 함

// ❌ Incorrect
new Lowercase()     // 의미 모호
new Missing()       // NotFound가 더 명확
```

**R1 vs R3 구분:**

| 상황 | 적용 규칙 | 이유 |
|------|----------|------|
| `Negative` | R1 | "음수임"이 명백한 문제 |
| `NotPositive` | R3 | "양수여야 함"인데 0도 포함해야 함 |
| `Empty` | R1 | "비어있음"이 명백한 문제 |
| `NotUpperCase` | R3 | "대문자여야 함"을 명시해야 명확 |

#### R4: 이미 발생한 상태 → Already + 상태

**적용 조건**: 이미 발생하여 되돌릴 수 없는 상태

```csharp
// ✅ Correct
new AlreadyExists()  // 이미 존재함

// ❌ Incorrect
new Exists()         // "이미"가 빠지면 의미 약함
```

#### R5: 형식/구조/상태 문제 → Invalid + 대상

**적용 조건**: 값의 형식, 구조, 또는 상태가 유효하지 않은 경우

```csharp
// ✅ Correct
new InvalidFormat(Pattern: @"^\d{3}-\d{4}$")
new InvalidState()

// ❌ Incorrect
new InvalidLength()  // WrongLength 사용 (R6)
new InvalidValue()   // 너무 추상적
```

**주의**: `Invalid-` 접두사는 형식/구조/상태 문제에만 사용합니다.

#### R6: 두 값 불일치 → Mismatch 또는 Wrong

**적용 조건**: 두 값이 일치해야 하는데 불일치하는 경우

```csharp
// ✅ Correct
new Mismatch()                    // 일반적인 불일치
new WrongLength(Expected: 10)     // 정확한 길이 불일치

// ❌ Incorrect
new NotMatching()    // Mismatch가 더 간결
new LengthMismatch() // WrongLength가 더 명확
```

| 패턴 | 사용 상황 |
|------|----------|
| `Mismatch` | 두 값 비교 (비밀번호 확인 등) |
| `Wrong-` | 기대한 정확한 값과 불일치 |

#### R7: 권한/인증 문제 → 상태 그대로

**적용 조건**: 인증/권한 관련 문제

```csharp
// ✅ Correct (HTTP 상태 코드와 일치)
new Unauthorized()   // 401: 인증 필요
new Forbidden()      // 403: 접근 금지

// ❌ Incorrect
new NotAuthenticated()  // Unauthorized가 표준
new AccessDenied()      // Forbidden이 표준
```

#### R8: 작업/프로세스 문제 → 동사 과거분사 + 명사

**적용 조건**: 작업이나 프로세스 실행 중 발생한 문제

```csharp
// ✅ Correct
new ValidationFailed(PropertyName: "Email")
new OperationCancelled()
new BusinessRuleViolated(RuleName: "MaxOrderLimit")
new ConcurrencyConflict()

// ❌ Incorrect
new FailedValidation()  // 어순 불일치
new CancelledOperation() // OperationCancelled가 표준
```

### 규칙 적용 플로우차트

새로운 에러 타입을 정의할 때 다음 순서로 규칙을 적용합니다:

```
1. 상태 자체가 문제인가?
   ├─ Yes → R1 (Empty, Null, Negative, Duplicate)
   └─ No ↓

2. 기준값과 비교가 필요한가?
   ├─ Yes → R2 (TooShort, BelowMinimum, OutOfRange)
   └─ No ↓

3. "~여야 하는데 아님"인가?
   ├─ Yes → R3 (NotPositive, NotUpperCase, NotFound)
   └─ No ↓

4. 이미 발생한 상태인가?
   ├─ Yes → R4 (AlreadyExists)
   └─ No ↓

5. 형식/구조/상태 문제인가?
   ├─ Yes → R5 (InvalidFormat, InvalidState)
   └─ No ↓

6. 두 값 불일치인가?
   ├─ Yes → R6 (Mismatch, WrongLength)
   └─ No ↓

7. 권한/인증 문제인가?
   ├─ Yes → R7 (Unauthorized, Forbidden)
   └─ No ↓

8. 작업/프로세스 실패인가?
   ├─ Yes → R8 (ValidationFailed, OperationCancelled)
   └─ No → Custom 사용
```

---

## 3. Domain 레이어 에러

### 에러 생성

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

### DomainErrorType 범주 구조

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

### DomainErrorType 전체 목록

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
| `Custom` | 도메인 특화 에러 | `new Custom("AlreadyShipped")` |

### Value Object 사용 예시

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$");
    private const int MaxLength = 256;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(MaxLength);
}
```

---

## 4. Application 레이어 에러

### 에러 생성

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
| `Custom` | 애플리케이션 특화 에러 | `new Custom("PaymentDeclined")` |

### 유스케이스 사용 예시

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

## 5. Adapter 레이어 에러

### 에러 생성

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

### AdapterErrorType 전체 목록

#### 공통 에러 타입 - R1, R3, R4, R5, R7

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

#### Pipeline 관련 - R8

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `PipelineValidation` | 파이프라인 검증 실패 | `new PipelineValidation(PropertyName: "Id")` |
| `PipelineException` | 파이프라인 예외 발생 | `new PipelineException()` |

#### 외부 서비스 관련 - R1, R8

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `ExternalServiceUnavailable` | 외부 서비스 사용 불가 | `new ExternalServiceUnavailable(ServiceName: "PaymentGateway")` |
| `ConnectionFailed` | 연결 실패 | `new ConnectionFailed(Target: "database")` |
| `Timeout` | 타임아웃 | `new Timeout(Duration: TimeSpan.FromSeconds(30))` |

#### 데이터 관련 - R1, R8

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Serialization` | 직렬화 실패 | `new Serialization(Format: "JSON")` |
| `Deserialization` | 역직렬화 실패 | `new Deserialization(Format: "XML")` |
| `DataCorruption` | 데이터 손상 | `new DataCorruption()` |

#### 커스텀

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `Custom` | 어댑터 특화 에러 | `new Custom("RateLimited")` |

### 파이프라인 사용 예시

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

## 6. Custom 에러 가이드

### 언제 Custom을 사용하는가?

1. **표준 에러로 표현 불가능한 경우**: 도메인/애플리케이션/어댑터 특화 상황
2. **의미가 명확한 경우**: 에러 이름만으로 상황을 이해할 수 있을 때
3. **재사용 가능성이 낮은 경우**: 특정 상황에서만 발생하는 에러

### Custom 에러 명명 규칙

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

### 레이어별 Custom 에러 예시

| 레이어 | Custom 에러 예시 | 설명 |
|--------|-----------------|------|
| Domain | `AlreadyShipped`, `NotVerified`, `Expired` | 도메인 규칙 위반 |
| Application | `PaymentDeclined`, `QuotaExceeded`, `MaintenanceMode` | 비즈니스 프로세스 실패 |
| Adapter | `RateLimited`, `CircuitOpen`, `ServiceDegraded` | 인프라/외부 서비스 문제 |

### 표준 에러로 승격 기준

자주 사용되는 Custom 에러는 표준 에러 타입으로 승격을 고려합니다:

```csharp
// 자주 사용되는 패턴 발견 시 표준 타입으로 추가
public sealed record Expired : DomainErrorType;
public sealed record Suspended : ApplicationErrorType;
public sealed record RateLimited : AdapterErrorType;
```

---

## 7. 체크리스트

### 에러 정의 체크리스트

- [ ] 적절한 레이어(Domain/Application/Adapter)를 선택했는가?
- [ ] 표준 에러 타입으로 표현 가능한지 먼저 확인했는가?
- [ ] Custom 에러 이름이 충분히 명확한가?
- [ ] 컨텍스트 정보(파라미터)가 디버깅에 도움이 되는가?
- [ ] 에러 메시지가 사용자/개발자에게 유용한가?

### 네이밍 체크리스트

- [ ] 적절한 규칙(R1-R8)을 적용했는가?
- [ ] 대칭 쌍이 있다면 일관성을 유지했는가? (Below ↔ Above)
- [ ] 컨텍스트 정보가 필요한가? (MinLength, Pattern, PropertyName 등)
- [ ] 에러 메시지가 에러 이름과 일관성 있는가?

---

## 8. 레이어별 에러 타입 요약

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
커스텀:      Custom(Name)
```

### Application (ApplicationErrorType)

```
공통:        Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState
권한:        Unauthorized, Forbidden
검증:        ValidationFailed
비즈니스:    BusinessRuleViolated, ConcurrencyConflict, ResourceLocked,
             OperationCancelled, InsufficientPermission
커스텀:      Custom(Name)
```

### Adapter (AdapterErrorType)

```
공통:        Empty, Null, NotFound, AlreadyExists, Duplicate, InvalidState,
             Unauthorized, Forbidden
파이프라인:  PipelineValidation, PipelineException
외부서비스:  ExternalServiceUnavailable, ConnectionFailed, Timeout
데이터:      Serialization, Deserialization, DataCorruption
커스텀:      Custom(Name)
```

---

## 9. 참고 문서

- [valueobject-guide.md](./valueobject-guide.md) - 값 객체 구현 및 검증 패턴
- [error-testing-guide.md](./error-testing-guide.md) - 에러 테스트 패턴

---

## 10. 변경 이력

| 날짜 | 변경 사항 | 작성자 |
|------|----------|--------|
| 2026-01-29 | 문서 통합 - layered-error-definition-guide.md, layered-error-naming-guide.md 병합 | - |
| 2026-01-26 | 날짜 검증 에러 추가 (DefaultDate, NotInPast, NotInFuture, TooLate, TooEarly) | - |
| 2026-01-26 | 숫자 검증 에러 추가 (Zero), 범위 검증 에러 추가 (RangeInverted, RangeEmpty) | - |
| 2026-01-25 | Value Object 예시를 `ValidationRules<T>` 문법으로 업데이트 | - |
| 2026-01-23 | 최초 작성 - 레이어별 에러 정의 및 네이밍 가이드 | - |
