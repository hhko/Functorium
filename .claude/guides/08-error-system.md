# 에러 시스템

이 문서는 Functorium 프로젝트에서 에러를 정의, 반환, 테스트하는 방법을 통합적으로 설명합니다.

## 목차

- [1. 왜 명시적 에러 처리인가](#1-왜-명시적-에러-처리인가)
- [2. Fin과 에러 반환 패턴](#2-fin과-에러-반환-패턴)
- [3. 에러 네이밍 규칙](#3-에러-네이밍-규칙)
- [4. Domain 에러](#4-domain-에러)
- [5. Application 에러](#5-application-에러)
- [6. Adapter 에러](#6-adapter-에러)
- [7. Custom 에러](#7-custom-에러)
- [8. 테스트 모범 사례](#8-테스트-모범-사례)
- [9. 레이어별 요약 + 체크리스트](#9-레이어별-요약--체크리스트)
- [참고 문서](#참고-문서)

---

## 1. 왜 명시적 에러 처리인가

### 예외(Exception) vs 결과 타입(Result Type)

전통적인 예외 기반 에러 처리는 제어 흐름이 암시적이고, 어떤 메서드가 어떤 에러를 반환하는지 시그니처에 드러나지 않습니다. 결과 타입(`Fin<T>`, `Validation<Error, T>`)은 성공과 실패를 타입 시스템으로 명시하여, 호출자가 반드시 두 경우를 모두 처리하도록 강제합니다.

### Railway Oriented Programming

Railway Oriented Programming(ROP)은 성공 트랙과 실패 트랙을 두 개의 레일로 비유합니다. 각 단계는 성공 시 다음 단계로, 실패 시 에러 트랙으로 자동 전환됩니다. `Fin<T>`의 `Bind`/`Map`과 LINQ 쿼리 문법이 이 패턴을 자연스럽게 지원합니다.

### DDD에서 에러의 역할

도메인 주도 설계에서 에러는 단순한 예외가 아니라 **도메인 규칙 위반의 명시적 표현**입니다. Value Object의 불변식 위반, Entity의 상태 전이 제약, Aggregate의 비즈니스 규칙 등이 모두 타입화된 에러로 표현됩니다.

### Functorium의 접근

Functorium은 LanguageExt의 `Fin<T>`와 `Validation<Error, T>`를 활용합니다:

- **`Fin<T>`**: 단일 에러를 반환하는 연산 (Entity 메서드, Usecase 등)
- **`Validation<Error, T>`**: 여러 에러를 누적하는 검증 (Value Object 생성 등)
- **레이어별 에러 팩토리**: `DomainError`, `ApplicationError`, `AdapterError`로 에러 출처를 명확히 구분
- **타입 안전 에러 코드**: 문자열 대신 `DomainErrorType`, `ApplicationErrorType`, `AdapterErrorType` 사용

---

## 2. Fin과 에러 반환 패턴

### Fin<T> 개요

`Fin<T>`는 LanguageExt에서 제공하는 성공/실패를 표현하는 타입입니다:

```csharp
// 성공
Fin<Product> success = product;           // 암시적 변환
Fin<Product> success = Fin.Succ(product); // 명시적

// 실패
Fin<Product> failure = error;             // 암시적 변환 (권장)
Fin<Product> failure = Fin.Fail<Product>(error); // 명시적 (불필요)
```

### 암시적 변환 활용 (권장)

LanguageExt는 `Error → Fin<T>` 암시적 변환을 제공합니다. **`Fin.Fail<T>(error)` 래핑은 불필요합니다.**

```csharp
// ❌ 기존 방식 (verbose)
return Fin.Fail<Money>(AdapterError.For<MyAdapter>(
    new NotFound(), context, "리소스를 찾을 수 없습니다"));

// ✅ 권장 방식 (implicit conversion)
return AdapterError.For<MyAdapter>(
    new NotFound(), context, "리소스를 찾을 수 없습니다");
```

### 레이어별 에러 반환 패턴

```csharp
// Domain Layer - Entity 메서드
// Error type definition: public sealed record InsufficientStock : DomainErrorType.Custom;
public Fin<Unit> DeductStock(Quantity quantity)
{
    if ((int)quantity > (int)StockQuantity)
        return DomainError.For<Product, int>(
            new InsufficientStock(),
            currentValue: (int)StockQuantity,
            message: $"재고 부족. 현재: {(int)StockQuantity}, 요청: {(int)quantity}");

    StockQuantity = Quantity.Create((int)StockQuantity - (int)quantity).ThrowIfFail();
    return unit;
}

// Application Layer - Usecase
public async ValueTask<FinResponse<Response>> Handle(Request request, ...)
{
    if (await _repository.ExistsAsync(request.ProductCode))
        return ApplicationError.For<CreateProductCommand>(
            new AlreadyExists(),
            request.ProductCode,
            "이미 존재하는 상품 코드입니다");

    // 성공 처리...
}

// Adapter Layer - Repository
public virtual FinT<IO, Product> GetById(ProductId id)
{
    return IO.lift(() =>
    {
        if (_products.TryGetValue(id, out Product? product))
            return Fin.Succ(product);

        return AdapterError.For<InMemoryProductRepository>(
            new NotFound(),
            id.ToString(),
            $"상품 ID '{id}'을(를) 찾을 수 없습니다");
    });
}
```

### FinT<IO, T> 패턴

비동기 IO 작업과 함께 사용하는 `FinT<IO, T>` 패턴입니다:

```csharp
// 동기 작업
return IO.lift(() =>
{
    if (condition)
        return Fin.Succ(result);
    return AdapterError.For<MyAdapter>(new NotFound(), context, "메시지");
});

// 비동기 작업
// Error type definition: public sealed record HttpError : AdapterErrorType.Custom;
return IO.liftAsync(async () =>
{
    try
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return AdapterError.For<MyAdapter>(
                new HttpError(),
                response.StatusCode.ToString(),
                "API 호출 실패");

        var result = await response.Content.ReadFromJsonAsync<T>();
        return Fin.Succ(result!);
    }
    catch (HttpRequestException ex)
    {
        return AdapterError.FromException<MyAdapter>(
            new ConnectionFailed("ExternalApi"),
            ex);
    }
});
```

### 성공 반환 시 주의사항

성공 값을 반환할 때는 여전히 `Fin.Succ(value)`를 사용합니다:

```csharp
// ✅ 성공 반환
return Fin.Succ(product);
return Fin.Succ(unit);  // Unit 타입

// ❌ Unit은 암시적 변환 안됨 (Unit은 Error가 아님)
return unit;  // 컴파일 에러 또는 타입 추론 실패
```

### 예외 처리 패턴

예외를 `Error`로 변환할 때는 `FromException` 메서드를 사용합니다:

```csharp
catch (HttpRequestException ex)
{
    return AdapterError.FromException<ExternalPricingApiService>(
        new ConnectionFailed("ExternalPricingApi"),
        ex);
}
// Error type definitions:
// public sealed record OperationCancelled : AdapterErrorType.Custom;
// public sealed record UnexpectedException : AdapterErrorType.Custom;
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
```

---

## 3. 에러 네이밍 규칙

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

## 4. Domain 에러

### 4.1 에러 생성 및 반환

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

### 4.2 Entity 메서드에서 에러 반환

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

### 4.3 DomainErrorType 범주 구조 및 전체 목록

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

### 4.4 Value Object 사용 예시

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

### 4.5 Domain 에러 테스트

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

## 5. Application 에러

### 5.1 에러 생성 및 반환

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

### 5.2 ApplicationErrorType 전체 목록

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

### 5.3 Usecase 에러 사용 패턴

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

### 5.4 Application 에러 테스트

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

## 6. Adapter 에러

### 6.1 에러 생성 및 반환

```csharp
using Functorium.Adapters.Errors;
using static Functorium.Adapters.Errors.AdapterErrorType;

// 기본 사용법 - 암시적 변환으로 직접 반환
return AdapterError.For<ProductRepository>(
    new NotFound(),
    id.ToString(),
    "상품을 찾을 수 없습니다");

// 제네릭 값 타입
return AdapterError.For<HttpClientAdapter, string>(
    new Timeout(Duration: TimeSpan.FromSeconds(30)),
    url,
    "요청 타임아웃");

// 예외 래핑
return AdapterError.FromException<ExternalApiService>(
    new ConnectionFailed("ExternalApi"),
    exception);
```

### 6.2 AdapterErrorType 전체 목록

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
| `Custom` | 어댑터 특화 에러 (abstract) | `sealed record RateLimited : AdapterErrorType.Custom;` → `new RateLimited()` |

### 6.3 Repository 구현 예시

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

    public virtual FinT<IO, Unit> Delete(ProductId id)
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

### 6.4 외부 API 서비스 구현 예시

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

### 6.5 Adapter 에러 테스트

테스트 어설션 네임스페이스:

```csharp
using Functorium.Testing.Assertions.Errors;
```

어설션 메서드 요약:

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

// 예외 래핑 에러 검증
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

---

## 7. Custom 에러

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

## 8. 테스트 모범 사례

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

---

## 9. 레이어별 요약 + 체크리스트

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

| 레이어 | 사용 시점 |
|--------|----------|
| **Domain** | Value Object 검증 실패, Entity 불변성 위반, Aggregate 비즈니스 규칙 위반 |
| **Application** | 유스케이스 실행 중 비즈니스 로직 오류, 권한/인증 오류, 데이터 조회 실패, 동시성 충돌 |
| **Adapter** | 파이프라인 검증/예외 처리, 외부 서비스 호출 실패, 직렬화/역직렬화 오류, 연결/타임아웃 오류 |

### 에러 코드 형식

모든 에러 코드는 다음 형식을 따릅니다:

```
{LayerPrefix}.{TypeName}.{ErrorName}
```

| 레이어 | 접두사 | 예시 |
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

## 참고 문서

- [05-value-objects.md](./05-value-objects.md) - 값 객체 구현 및 검증 패턴
- [13-adapters.md](./13-adapters.md) - Adapter 구현 가이드
- [15-unit-testing.md](./15-unit-testing.md) - 단위 테스트 가이드
- [16-testing-library.md](./16-testing-library.md) - 에러 외 테스트 유틸리티 (로그/아키텍처/소스생성기/Job 테스트)
