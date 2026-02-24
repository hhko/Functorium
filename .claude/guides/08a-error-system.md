# 에러 시스템: 기초와 네이밍

이 문서는 에러 처리의 기본 원칙, Fin 패턴, 에러 네이밍 규칙을 다룹니다. 레이어별 에러 정의와 테스트 패턴은 [08b-error-system-layers.md](./08b-error-system-layers.md)을 참고하세요.

## 목차

- [왜 명시적 에러 처리인가](#왜-명시적-에러-처리인가)
- [Fin과 에러 반환 패턴](#fin과-에러-반환-패턴)
- [에러 네이밍 규칙](#에러-네이밍-규칙)
- [참고 문서](#참고-문서)

---

## 왜 명시적 에러 처리인가

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

## Fin과 에러 반환 패턴

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

## 에러 네이밍 규칙

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

### Custom → 표준 에러 승격 기준

플로우차트 끝의 `Custom` 에러가 프로젝트 전반에서 반복 사용되면, 표준 에러 타입(R1-R8)으로 승격을 검토합니다:

> 1. **3개 이상의 서로 다른 위치**에서 동일 Custom 에러 사용
> 2. **재사용 의미가 명확** (도메인 개념으로 자리잡음)
> 3. 기존 네이밍 규칙(R1-R8)에 **자연스럽게 매핑** 가능
> 4. **안정성 확인** (더 이상 의미가 변하지 않음)

4가지 조건을 모두 충족하면 표준 `DomainErrorType` / `ApplicationErrorType` / `AdapterErrorType`에 추가합니다.

---

## FAQ

**Q: `Fin<T>`과 Exception의 차이점은?**

`Fin<T>`은 예상 가능한 실패(비즈니스 규칙 위반, 검증 실패 등)를 타입으로 표현하여 호출자가 반드시 처리하도록 강제합니다. Exception은 네트워크 장애, 메모리 부족 등 예외적이고 복구 불가능한 상황에만 사용합니다.

**Q: 에러 코드 네이밍에서 R1-R8 중 어떤 규칙을 적용할지 모르겠을 때는?**

[규칙 적용 플로우차트](#규칙-적용-플로우차트)를 위에서 아래로 순서대로 따르세요. 가장 먼저 일치하는 규칙이 가장 구체적인 규칙이므로 그것을 적용합니다.

**Q: Custom 에러는 언제 만드는가?**

R1-R8의 표준 에러 타입으로 표현할 수 없는 도메인 특화 에러일 때 Custom을 사용합니다. 예: `InsufficientStock`, `HttpError` 등. 이후 3개 이상 위치에서 반복 사용되면 [표준 에러 승격 기준](#custom--표준-에러-승격-기준)에 따라 승격을 검토합니다.

---

## 참고 문서

- [08b-error-system-layers.md](./08b-error-system-layers.md) - 레이어별 구현과 테스트
