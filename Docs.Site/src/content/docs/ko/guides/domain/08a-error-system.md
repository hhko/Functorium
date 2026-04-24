---
title: "에러 시스템: 기초와 네이밍"
---

이 문서는 에러 처리의 기본 원칙, Fin 패턴, 에러 네이밍 규칙을 다룹니다. Domain/Application/Event 에러는 [08b-error-system-domain-app.md](../08b-error-system-domain-app), Adapter 에러와 테스트 패턴은 [08c-error-system-adapter-testing.md](../08c-error-system-adapter-testing)을 참고하세요.

## 들어가며

"Entity 메서드가 실패할 수 있을 때 반환 타입을 어떻게 설계하는가?"
"예외(Exception)를 던지는 대신 결과 타입을 사용하면 어떤 이점이 있는가?"
"에러 코드의 이름을 일관되게 짓는 규칙이 있는가?"

에러 처리는 도메인 규칙 위반부터 외부 시스템 장애까지 다양한 실패 시나리오를 다루는 핵심 관심사입니다. 이 문서는 Functorium의 `Fin<T>` 패턴, 레이어별 에러 팩토리, 에러 네이밍 규칙(R1~R8)을 다룹니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **예외 vs 결과 타입의 차이** — 왜 명시적 에러 처리를 선택하는지
2. **`Fin<T>`와 암시적 변환** — 에러를 간결하게 반환하는 패턴
3. **레이어별 에러 팩토리** — `DomainError`, `ApplicationError`, `AdapterError`의 사용법
4. **에러 네이밍 규칙 R1~R8** — 일관된 에러 코드 작성을 위한 플로우차트

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- LanguageExt의 `Fin<T>` 타입 기본 개념
- [값 객체 구현 가이드](../05a-value-objects) — Value Object에서의 `Validation<Error, T>` 사용

> Functorium은 예외 대신 `Fin<T>`와 `Validation<Error, T>`로 실패를 타입 시스템에 명시합니다. 레이어별 에러 팩토리(`DomainError`, `ApplicationError`, `AdapterError`)로 에러 출처를 구분하고, R1~R8 네이밍 규칙으로 에러 코드의 일관성을 보장합니다.

## 요약

### 주요 명령

```csharp
// 에러 반환 (암시적 변환 권장)
return DomainError.For<Email>(new Empty(), "", "이메일은 비어있을 수 없습니다");
return ApplicationError.For<CreateProductCommand>(new AlreadyExists(), code, "이미 존재합니다");
return AdapterError.For<ProductRepository>(new NotFound(), id, "찾을 수 없습니다");

// 예외 래핑
return AdapterError.FromException<MyAdapter>(new ConnectionFailed("DB"), exception);

// 성공 반환
return Fin.Succ(product);
```

### 주요 절차

1. 에러 네이밍 규칙 플로우차트(R1~R8) 순서대로 적합한 규칙 선택
2. 표준 에러 타입으로 표현 가능한지 확인, 불가능하면 `Custom` sealed record 정의
3. 레이어에 맞는 팩토리(`DomainError`, `ApplicationError`, `AdapterError`) 사용
4. `Fin.Fail<T>()` 래핑 대신 암시적 변환으로 직접 반환

### 주요 개념

| 개념 | 설명 |
|------|------|
| `Fin<T>` | 단일 에러 반환. Entity 메서드, Usecase에서 사용 |
| `Validation<Error, T>` | 여러 에러 누적. Value Object 검증에서 사용 |
| 레이어별 에러 팩토리 | `DomainError`, `ApplicationError`, `AdapterError`로 에러 출처 구분 |
| 암시적 변환 | `Error → Fin<T>` 자동 변환. `Fin.Fail<T>(error)` 래핑 불필요 |
| 네이밍 규칙 R1~R8 | 상태 자명(R1) → 기준 비교(R2) → 기대 불충족(R3) → ... → 작업 실패(R8) |

---

## 왜 명시적 에러 처리인가

### 예외(Exception) vs 결과 타입(Result Type)

전통적인 예외 기반 에러 처리는 제어 흐름이 암시적이고, 어떤 메서드가 어떤 에러를 반환하는지 시그니처에 드러나지 않습니다. 결과 타입(`Fin<T>`, `Validation<Error, T>`)은 성공과 실패를 타입 시스템으로 명시하여, 호출자가 반드시 두 경우를 모두 처리하도록 강제합니다.

### Railway Oriented Programming

Railway Oriented Programming(ROP)은 성공 트랙과 실패 트랙을 두 개의 레일로 비유합니다. 각 단계는 성공 시 다음 단계로, 실패 시 에러 트랙으로 자동 전환됩니다. `Fin<T>`의 `Bind`/`Map`과 LINQ 쿼리 문법이 이 패턴을 자연스럽게 지원합니다.

### DDD에서 에러의 역할

도메인 주도 설계에서 에러는 단순한 예외가 아니라 **도메인 규칙 위반의 명시적 표현입니다.** Value Object의 불변식 위반, Entity의 상태 전이 제약, Aggregate의 비즈니스 규칙 등이 모두 타입화된 에러로 표현됩니다.

### Functorium의 접근

Functorium은 LanguageExt의 `Fin<T>`와 `Validation<Error, T>`를 활용합니다:

- **`Fin<T>`**: 단일 에러를 반환하는 연산 (Entity 메서드, Usecase 등)
- **`Validation<Error, T>`**: 여러 에러를 누적하는 검증 (Value Object 생성 등)
- **레이어별 에러 팩토리**: `DomainError`, `ApplicationError`, `AdapterError`로 에러 출처를 명확히 구분
- **타입 안전 에러 코드**: 문자열 대신 `DomainErrorKind`, `ApplicationErrorKind`, `AdapterErrorKind` 사용

명시적 에러 처리의 필요성을 이해했으니, 이제 Functorium에서 에러를 반환하는 구체적인 패턴을 살펴보겠습니다.

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
// Error type definition: public sealed record InsufficientStock : DomainErrorKind.Custom;
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
// Error type definition: public sealed record HttpError : AdapterErrorKind.Custom;
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
// public sealed record OperationCancelled : AdapterErrorKind.Custom;
// public sealed record UnexpectedException : AdapterErrorKind.Custom;
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

에러 반환 패턴을 익혔다면, 다음으로 중요한 것은 에러 코드의 이름을 일관되게 짓는 것입니다.

---

## 에러 네이밍 규칙

### 빠른 참조: 네이밍 규칙 요약

새 에러 타입을 정의할 때 가장 먼저 이 표에서 적합한 규칙을 찾으세요. R1부터 순서대로 적용합니다.

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

> 1. **3개 이상의 서로 다른 위치에서** 동일 Custom 에러 사용
> 2. **재사용 의미가 명확** (도메인 개념으로 자리잡음)
> 3. 기존 네이밍 규칙(R1-R8)에 **자연스럽게 매핑** 가능
> 4. **안정성 확인** (더 이상 의미가 변하지 않음)

4가지 조건을 모두 충족하면 표준 `DomainErrorKind` / `ApplicationErrorKind` / `AdapterErrorKind`에 추가합니다.

---

## 트러블슈팅

### `unit` 반환 시 `Fin<Unit>` 타입 추론 실패
**원인:** `return unit;`은 `Unit` 타입이지 `Error`가 아니므로 `Fin<T>`로의 암시적 변환이 되지 않습니다. 암시적 변환은 `Error → Fin<T>` 방향만 지원합니다.
**해결:** 성공 반환 시 항상 `return Fin.Succ(unit);`을 명시적으로 사용하세요. 값 타입(`Product` 등)은 암시적 변환이 가능하지만, `Unit`은 예외입니다.

### 에러 네이밍에서 R1과 R3 구분이 모호함
**원인:** `Negative`(R1)와 `NotPositive`(R3)처럼 비슷해 보이는 경우가 있습니다.
**해결:** R1은 "그 자체로 문제가 명백한 상태"(예: `Empty`, `Null`, `Negative`), R3은 "기대 조건이 있어야 의미가 통하는 부정"(예: `NotPositive`는 0도 포함, `NotUpperCase`). 플로우차트를 위에서 아래로 순서대로 따르면 가장 먼저 일치하는 규칙이 정답입니다.

### `FinT<IO, T>` 내부에서 에러 반환 시 `Fin.Fail` 필요
**원인:** `IO.lift(() => { ... })` 블록 내부에서는 반환 타입이 `Fin<T>`이므로 에러 반환 시 암시적 변환이 정상 동작합니다. 다만 성공 반환 시에는 `Fin.Succ(value)`가 필요합니다.
**해결:** `IO.lift` 블록 내부에서는 에러는 암시적 변환, 성공은 `Fin.Succ(value)`를 사용하세요.

---

## FAQ

**Q: `Fin<T>`과 Exception의 차이점은?**

`Fin<T>`은 예상 가능한 실패(비즈니스 규칙 위반, 검증 실패 등)를 타입으로 표현하여 호출자가 반드시 처리하도록 강제합니다. Exception은 네트워크 장애, 메모리 부족 등 예외적이고 복구 불가능한 상황에만 사용합니다.

**Q: 에러 코드 네이밍에서 R1-R8 중 어떤 규칙을 적용할지 모르겠을 때는?**

[규칙 적용 플로우차트](#규칙-적용-플로우차트)를 위에서 아래로 순서대로 따르세요. 가장 먼저 일치하는 규칙이 가장 구체적인 규칙이므로 그것을 적용합니다.

**Q: Custom 에러는 언제 만드는가?**

R1-R8의 표준 에러 타입으로 표현할 수 없는 도메인 특화 에러일 때 Custom을 사용합니다. 예: `InsufficientStock`, `HttpError` 등. 이후 3개 이상 위치에서 반복 사용되면 [표준 에러 승격 기준](#custom--표준-에러-승격-기준)에 따라 승격을 검토합니다.

---

## 부록: ErrorFactory API

### 파일 구조

```
Src/Functorium/Abstractions/Errors/
├── ExpectedError.cs              # Expected 에러 타입 (4가지 변형)
├── ExpectedErrorBase.cs          # Expected 에러 공통 기반 클래스 (13개 override 통합)
├── ExceptionalError.cs           # Exceptional 에러 타입
├── ErrorFactory.cs               # ExpectedError/Exceptional 인스턴스 생성
├── ErrorLogFieldNames.cs            # Serilog 구조화 필드명 상수
├── ErrorCodePrefixes.cs               # 내부 접두사 상수 (Domain, Application, Adapter)
├── ErrorKind.cs                       # 추상 기반 record (virtual Name 프로퍼티)
├── IHasErrorCode.cs                  # 에러 코드 접근 인터페이스
└── LayerErrorCore.cs                 # 레이어별 팩토리 공통 에러 코드 생성 로직

Src/Functorium.Testing/Assertions/Errors/
├── DomainErrorAssertions.cs          # 도메인 에러 검증 (thin wrapper)
├── ApplicationErrorAssertions.cs     # 애플리케이션 에러 검증 (thin wrapper)
├── AdapterErrorAssertions.cs         # 어댑터 에러 검증 (thin wrapper)
├── ErrorAssertionCore.cs             # 레이어별 Assertion 공통 검증 로직
├── ErrorAssertionHelpers.cs          # 공유 유틸리티
├── ErrorCodeAssertions.cs            # 범용 에러 코드 검증
└── ExceptionalErrorAssertions.cs # Exceptional 에러 검증

Src/Functorium.Adapters/Abstractions/Errors/
└── DestructuringPolicies/            # Serilog 구조화 정책
    ├── IErrorDestructurer.cs
    ├── ErrorsDestructuringPolicy.cs
    └── ErrorTypes/
        ├── ExpectedErrorDestructurer.cs
        ├── ExpectedErrorTDestructurer.cs
        ├── ExceptionalErrorDestructurer.cs
        ├── ExceptionalDestructurer.cs    # LanguageExt Exceptional 구조화
        ├── ExpectedDestructurer.cs       # LanguageExt Expected 구조화
        └── ManyErrorsDestructurer.cs
```

### 에러 타입 계층

Functorium의 에러 타입은 LanguageExt의 `Error`를 확장하여 다음과 같은 계층을 형성합니다.

```
Error (LanguageExt.Common)
├── ExpectedErrorBase             - Expected 에러 공통 기반 (13개 override 통합)
│   ├── ExpectedError             - 도메인/비즈니스 에러 (문자열 값)
│   ├── ExpectedError<T>          - 도메인/비즈니스 에러 (타입 값 1개)
│   ├── ExpectedError<T1, T2>     - 도메인/비즈니스 에러 (타입 값 2개)
│   └── ExpectedError<T1, T2, T3> - 도메인/비즈니스 에러 (타입 값 3개)
├── ExceptionalError              - 예외 래퍼
└── ManyErrors                        - 복수 에러 컬렉션
```

`ExpectedErrorBase`는 `ErrorCode`, `Message`, `Code`, `Inner` 속성과 `sealed override ToString() => Message`, `IsExpected = true`, `IsExceptional = false` 등 공통 멤버를 정의합니다. 파생 4종은 `ErrorCurrentValue` 관련 속성만 추가합니다. `sealed override ToString()`은 C# record가 파생 클래스에서 `ToString()`을 자동 재생성하는 것을 차단하여, 모든 Expected 에러가 일관되게 `Message`를 반환하도록 보장합니다.

모든 `ExpectedError` 변형과 `ExceptionalError`은 **internal record이며** `IHasErrorCode` 인터페이스를 구현합니다.

| 타입 | `IsExpected` | `IsExceptional` | 접근 제한자 |
|------|:---:|:---:|:---:|
| `ExpectedErrorBase` | `true` | `false` | internal (abstract) |
| `ExpectedError` | `true` | `false` | internal |
| `ExpectedError<T>` | `true` | `false` | internal |
| `ExpectedError<T1, T2>` | `true` | `false` | internal |
| `ExpectedError<T1, T2, T3>` | `true` | `false` | internal |
| `ExceptionalError` | `false` | `true` | internal |

> **참고**: `DomainError.For<TDomain, T1, T2, T3>()` 3-값 오버로드도 지원됩니다. 상세 시그니처와 사용 예제는 [에러 시스템: Domain/Application 에러](../08b-error-system-domain-app)를 참조하세요.

### 에러 생성 흐름

레이어별 팩토리(`DomainError`, `ApplicationError`, `EventError`, `AdapterError`)는 2단계 내부 위임으로 에러를 생성합니다.

```
DomainError.For<Email>(new Empty(), value, msg)     ← 공개 API (DomainErrorKind 강제)
  → LayerErrorCore.Create<Email>(prefix, errorType, value, msg)
      ← ErrorKind(base)로 수신 → 에러 코드 조립: "Domain.Email.Empty"
    → ErrorFactory.CreateExpected(errorCode, value, msg)
        ← ExpectedError 인스턴스 생성
```

`LayerErrorCore`는 4개 팩토리의 공통 구현으로, 에러 코드 문자열 `{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}`을 조립합니다. 공개 팩토리는 레이어별 타입 파라미터(`DomainErrorKind`, `ApplicationErrorKind` 등)를 유지하여 **컴파일 타임에 잘못된 레이어 에러 사용을 차단합니다.** 모든 메서드에 `[AggressiveInlining]`이 적용되어 JIT이 위임 호출을 인라인 처리하므로, 직접 호출과 성능이 동일합니다.

### ErrorFactory API

`ErrorFactory`는 `Abstractions/Errors/ErrorFactory.cs`에 위치한 **정적 클래스로,** `ExpectedError`와 `ExceptionalError` 인스턴스를 직접 생성합니다.

```csharp
public static class ErrorFactory
{
    // Expected 에러 (문자열 값) → ExpectedError
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage);

    // Expected 에러 (타입 값) → ExpectedError<T>
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
        where T : notnull;

    // Expected 에러 (2개 타입 값) → ExpectedError<T1, T2>
    public static Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull;

    // Expected 에러 (3개 타입 값) → ExpectedError<T1, T2, T3>
    public static Error Create<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
        where T1 : notnull where T2 : notnull where T3 : notnull;

    // Exceptional 에러 → ExceptionalError
    public static Error CreateFromException(string errorCode, Exception exception);

    // 에러 코드 포맷 → string.Join('.', parts)
    public static string Format(params string[] parts);
}
```

### 사용 예시

```csharp
// Expected 에러 (문자열 값)
Error error = ErrorFactory.CreateExpected(
    "Domain.User.NotFound", "user123", "사용자를 찾을 수 없습니다");

// Expected 에러 (타입 값)
Error error = ErrorFactory.CreateExpected(
    "Domain.Sensor.TemperatureOutOfRange", 150, "온도 범위 초과");

// Expected 에러 (2개 타입 값)
Error error = ErrorFactory.CreateExpected(
    "Domain.Range.InvalidBounds", 100, 50, "최소값이 최대값보다 큽니다");

// Exceptional 에러
Error error = ErrorFactory.CreateExceptional(
    "Application.Database.ConnectionFailed", exception);
```

### Serilog 구조화

`ErrorsDestructuringPolicy`를 등록하면 에러 객체가 구조화된 JSON으로 로깅됩니다.

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.With<ErrorsDestructuringPolicy>()
    .CreateLogger();
```

**필드 매핑:**

| Field | Expected | Expected&lt;T&gt; | Exceptional | ManyErrors |
|-------|:---:|:---:|:---:|:---:|
| ErrorKind | O | O | O | O |
| ErrorCode | O | O | O | X |
| NumericCode | O | O | O | O |
| ErrorCurrentValue | O | O | X | X |
| Message | O | O | O | X |
| Count | X | X | X | O |
| Errors | X | X | X | O |
| ExceptionDetails | X | X | O | X |

---

## 참고 문서

- [08b-error-system-domain-app.md](../08b-error-system-domain-app) - Domain/Application 에러 정의와 테스트
- [08c-error-system-adapter-testing.md](../08c-error-system-adapter-testing) - Adapter 에러, Custom 에러, 테스트 모범 사례
