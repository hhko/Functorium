---
title: "에러 시스템 사양"
---

Functorium의 에러 시스템은 **레이어별 sealed record 계층**(`DomainErrorType`, `ApplicationErrorType`, `AdapterErrorType`)과 **레이어별 팩토리**(`DomainError`, `ApplicationError`, `EventError`, `AdapterError`)로 구성됩니다. 팩토리의 공통 생성 로직은 내부 `LayerErrorCore`에 집중되고, Expected 에러의 공통 override는 `ErrorCodeExpectedBase`에 통합됩니다. 이 사양서는 공개/내부 타입의 시그니처, 속성, 에러 코드 생성 규칙을 정의합니다.

## 요약

### 주요 타입

#### 공개 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `ErrorType` | `Functorium.Abstractions.Errors` | 모든 레이어 에러 타입의 추상 기반 record |
| `IHasErrorCode` | `Functorium.Abstractions.Errors` | 에러 코드 접근 인터페이스 |
| `ErrorCodeFactory` | `Functorium.Abstractions.Errors` | Expected/Exceptional 에러 생성 팩토리 |
| `DomainErrorType` | `Functorium.Domains.Errors` | 도메인 에러 타입 sealed record 계층 (10개 카테고리) |
| `DomainError` | `Functorium.Domains.Errors` | 도메인 에러 생성 팩토리 |
| `ApplicationErrorType` | `Functorium.Applications.Errors` | 애플리케이션 에러 타입 sealed record 계층 (14개 타입) |
| `ApplicationError` | `Functorium.Applications.Errors` | 애플리케이션 에러 생성 팩토리 |
| `EventErrorType` | `Functorium.Applications.Errors` | 이벤트 에러 타입 sealed record 계층 (4개 타입) |
| `EventError` | `Functorium.Applications.Errors` | 이벤트 에러 생성 팩토리 |
| `AdapterErrorType` | `Functorium.Adapters.Errors` | 어댑터 에러 타입 sealed record 계층 (20개 타입) |
| `AdapterError` | `Functorium.Adapters.Errors` | 어댑터 에러 생성 팩토리 |

#### 내부 타입 (internal)

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `ErrorCodeExpectedBase` | `Functorium.Abstractions.Errors` | Expected 에러 4종의 공통 LanguageExt `Error` override 기반 클래스 |
| `ErrorCodeExpected` (4종) | `Functorium.Abstractions.Errors` | Expected 에러 — 값 저장만 담당하며 나머지는 base에서 상속 |
| `ErrorCodeExceptional` | `Functorium.Abstractions.Errors` | Exception을 에러 코드와 함께 래핑 |
| `LayerErrorCore` | `Functorium.Abstractions.Errors` | 4개 레이어 팩토리의 공통 에러 코드 생성 로직 |
| `ErrorAssertionCore` | `Functorium.Testing.Assertions.Errors` | 3개 레이어 Assertion의 공통 검증 로직 |

### 에러 코드 형식

모든 에러 코드는 다음 패턴을 따릅니다:

```
{LayerPrefix}.{ContextName}.{ErrorName}
```

| 레이어 | 접두사 | 예시 |
|--------|--------|------|
| Domain | `DomainErrors` | `DomainErrors.Email.Empty` |
| Application | `ApplicationErrors` | `ApplicationErrors.CreateProductCommand.AlreadyExists` |
| Adapter | `AdapterErrors` | `AdapterErrors.ProductRepository.NotFound` |

---

## 에러 코드 체계

### ErrorType (추상 기반)

```csharp
namespace Functorium.Abstractions.Errors;

public abstract record ErrorType
{
    public const string DomainErrorsPrefix = "DomainErrors";
    public const string ApplicationErrorsPrefix = "ApplicationErrors";
    public const string AdapterErrorsPrefix = "AdapterErrors";

    public virtual string ErrorName => GetType().Name;
}
```

**`ErrorType`은** 모든 레이어별 에러 타입(`DomainErrorType`, `ApplicationErrorType`, `AdapterErrorType`)의 공통 기반입니다.

| 멤버 | 종류 | 설명 |
|------|------|------|
| `DomainErrorsPrefix` | `const string` | 도메인 에러 코드 접두사 `"DomainErrors"` |
| `ApplicationErrorsPrefix` | `const string` | 애플리케이션 에러 코드 접두사 `"ApplicationErrors"` |
| `AdapterErrorsPrefix` | `const string` | 어댑터 에러 코드 접두사 `"AdapterErrors"` |
| `ErrorName` | `virtual string` | 에러 코드의 마지막 세그먼트. 기본값은 `GetType().Name` |

### IHasErrorCode

```csharp
namespace Functorium.Abstractions.Errors;

public interface IHasErrorCode
{
    string ErrorCode { get; }
}
```

**`IHasErrorCode`는** 리플렉션 없이 타입 안전하게 에러 코드에 접근하기 위한 인터페이스입니다. `ErrorCodeExpected`와 `ErrorCodeExceptional`이 이 인터페이스를 구현합니다.

---

## ErrorCode 타입

### ErrorCodeExpectedBase (internal)

```csharp
internal abstract record ErrorCodeExpectedBase(
    string ErrorCode,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default) : Error, IHasErrorCode
```

**`ErrorCodeExpectedBase`는** Expected 에러 4종(`ErrorCodeExpected`, `<T>`, `<T1,T2>`, `<T1,T2,T3>`)의 공통 기반 클래스입니다. LanguageExt `Error`의 13개 override를 한 곳에 정의하여 파생 타입의 중복을 제거합니다.

| 멤버 | 종류 | 설명 |
|------|------|------|
| `ErrorCode` | `string` | `"{Prefix}.{Context}.{ErrorName}"` 형식 에러 코드 |
| `Message` | `override string` | 사람이 읽을 수 있는 에러 메시지 |
| `Code` | `override int` | 정수 에러 코드 ID (기본값 `-1000`) |
| `Inner` | `override Option<Error>` | 내부 에러 (기본값 `None`) |
| `ToString()` | `sealed override` | `Message` 반환. `sealed`로 파생 record의 자동생성 방지 |
| `ToErrorException()` | `override` | `WrappedErrorExpectedException` 반환 |
| `IsExpected` | `bool` | 항상 `true` |
| `IsExceptional` | `bool` | 항상 `false` |

> **`sealed override ToString()`**: C# record는 파생 클래스에서 `ToString()`을 자동 재생성합니다. `sealed`로 이를 차단하여 모든 파생 타입이 일관되게 `Message`를 반환하도록 보장합니다.

### ErrorCodeExpected (internal)

```csharp
internal record ErrorCodeExpected(
    string ErrorCode,
    string ErrorCurrentValue,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : ErrorCodeExpectedBase(ErrorCode, ErrorMessage, ErrorCodeId, Inner)
```

**`ErrorCodeExpected`는** 비즈니스 규칙 위반 등 예상된(Expected) 에러를 표현합니다. `ErrorCodeExpectedBase`에서 `ErrorCode`, `Message`, `Code`, `Inner`, `ToString()`, `IsExpected`, `IsExceptional` 등 공통 멤버를 상속받고, 파생 타입은 에러 발생 시점의 값(`ErrorCurrentValue`)만 추가로 정의합니다.

| 오버로드 | 추가 속성 | 설명 |
|----------|----------|------|
| `ErrorCodeExpected` | `ErrorCurrentValue: string` | 문자열 값 |
| `ErrorCodeExpected<T>` | `ErrorCurrentValue: T` | 타입 값 (1개) |
| `ErrorCodeExpected<T1, T2>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2` | 타입 값 (2개) |
| `ErrorCodeExpected<T1, T2, T3>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2`, `ErrorCurrentValue3: T3` | 타입 값 (3개) |

### ErrorCodeExceptional (internal)

```csharp
internal record ErrorCodeExceptional : Error, IHasErrorCode
{
    public ErrorCodeExceptional(string errorCode, Exception exception);
}
```

**`ErrorCodeExceptional`은** 시스템 예외(Exception)를 에러 코드와 함께 래핑합니다. `IsExpected = false`, `IsExceptional = true`입니다.

| 속성 | 타입 | 설명 |
|------|------|------|
| `ErrorCode` | `string` | `"{Prefix}.{Context}.{ErrorName}"` 형식 에러 코드 |
| `Message` | `string` | `exception.Message`에서 추출 |
| `Code` | `int` | `exception.HResult`에서 추출 |
| `Inner` | `Option<Error>` | `InnerException`이 있으면 재귀적으로 래핑 |
| `IsExpected` | `bool` | 항상 `false` |
| `IsExceptional` | `bool` | 항상 `true` |

### ErrorCodeFactory

```csharp
namespace Functorium.Abstractions.Errors;

public static class ErrorCodeFactory
{
    // Expected 에러 생성
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage);
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull;
    public static Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull;
    public static Error Create<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
        where T1 : notnull where T2 : notnull where T3 : notnull;

    // Exceptional 에러 생성
    public static Error CreateFromException(string errorCode, Exception exception);

    // 에러 코드 조합
    public static string Format(params string[] parts);
}
```

**`ErrorCodeFactory`는** `ErrorCodeExpected`와 `ErrorCodeExceptional` 인스턴스를 생성하는 정적 팩토리입니다. 레이어별 팩토리는 다음 흐름으로 에러를 생성합니다:

```
DomainError.For<T>(DomainErrorType, ...)        ← 레이어 타입 안전성 (공개 API)
  → LayerErrorCore.Create<T>(prefix, ErrorType, ...)  ← 공통 에러 코드 조립 (internal)
    → ErrorCodeFactory.Create(errorCode, ...)          ← ErrorCodeExpected 인스턴스 생성 (internal)
```

`LayerErrorCore`가 에러 코드 문자열(`{Prefix}.{Context}.{ErrorName}`)을 조립하고, `ErrorCodeFactory`가 최종 `Error` 인스턴스를 생성합니다. 모든 메서드에 `[AggressiveInlining]`이 적용되어 JIT이 위임 호출을 제거하므로 성능 차이는 없습니다.

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `Create(...)` | `Error` | Expected 에러 생성 (4개 오버로드) |
| `CreateFromException(...)` | `Error` | Exception을 Exceptional 에러로 래핑 |
| `Format(...)` | `string` | 문자열 배열을 `'.'`으로 결합하여 에러 코드 생성 |

---

## Domain ErrorType 카탈로그

```csharp
namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType : ErrorType;
```

**`DomainErrorType`은** 도메인 레이어 에러의 sealed record 계층 기반입니다. 10개 카테고리로 분류됩니다.

### 존재(Existence)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `NotFound` | (없음) | 값을 찾을 수 없음 |
| `AlreadyExists` | (없음) | 값이 이미 존재함 |
| `Duplicate` | (없음) | 중복된 값 |
| `Mismatch` | (없음) | 값이 일치하지 않음 (예: 비밀번호 확인) |

```csharp
public sealed record NotFound : DomainErrorType;
public sealed record AlreadyExists : DomainErrorType;
public sealed record Duplicate : DomainErrorType;
public sealed record Mismatch : DomainErrorType;
```

### 존재 여부(Presence)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `Empty` | (없음) | 값이 비어있음 (null, empty string, empty collection 등) |
| `Null` | (없음) | 값이 null임 |

```csharp
public sealed record Empty : DomainErrorType;
public sealed record Null : DomainErrorType;
```

### 형식(Format)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `InvalidFormat` | `string? Pattern` | 값의 형식이 유효하지 않음. `Pattern`은 기대되는 형식 패턴 |
| `NotUpperCase` | (없음) | 값이 대문자가 아님 |
| `NotLowerCase` | (없음) | 값이 소문자가 아님 |

```csharp
public sealed record InvalidFormat(string? Pattern = null) : DomainErrorType;
public sealed record NotUpperCase : DomainErrorType;
public sealed record NotLowerCase : DomainErrorType;
```

### 길이(Length)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `TooShort` | `int MinLength` | 값이 최소 길이보다 짧음. 기본값 `0` (미지정) |
| `TooLong` | `int MaxLength` | 값이 최대 길이를 초과함. 기본값 `int.MaxValue` (미지정) |
| `WrongLength` | `int Expected` | 값의 길이가 기대와 불일치. 기본값 `0` (미지정) |

```csharp
public sealed record TooShort(int MinLength = 0) : DomainErrorType;
public sealed record TooLong(int MaxLength = int.MaxValue) : DomainErrorType;
public sealed record WrongLength(int Expected = 0) : DomainErrorType;
```

### 숫자(Numeric)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `Zero` | (없음) | 값이 0임 |
| `Negative` | (없음) | 값이 음수임 |
| `NotPositive` | (없음) | 값이 양수가 아님 (0 또는 음수) |
| `OutOfRange` | `string? Min`, `string? Max` | 값이 허용 범위를 벗어남 |
| `BelowMinimum` | `string? Minimum` | 값이 최소값보다 작음 |
| `AboveMaximum` | `string? Maximum` | 값이 최대값을 초과함 |

```csharp
public sealed record Zero : DomainErrorType;
public sealed record Negative : DomainErrorType;
public sealed record NotPositive : DomainErrorType;
public sealed record OutOfRange(string? Min = null, string? Max = null) : DomainErrorType;
public sealed record BelowMinimum(string? Minimum = null) : DomainErrorType;
public sealed record AboveMaximum(string? Maximum = null) : DomainErrorType;
```

### 날짜/시간(DateTime)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `DefaultDate` | (없음) | 날짜가 기본값(`DateTime.MinValue`)임 |
| `NotInPast` | (없음) | 날짜가 과거여야 하는데 미래임 |
| `NotInFuture` | (없음) | 날짜가 미래여야 하는데 과거임 |
| `TooLate` | `string? Boundary` | 날짜가 기준 날짜보다 이후임 (이전이어야 함) |
| `TooEarly` | `string? Boundary` | 날짜가 기준 날짜보다 이전임 (이후여야 함) |

```csharp
public sealed record DefaultDate : DomainErrorType;
public sealed record NotInPast : DomainErrorType;
public sealed record NotInFuture : DomainErrorType;
public sealed record TooLate(string? Boundary = null) : DomainErrorType;
public sealed record TooEarly(string? Boundary = null) : DomainErrorType;
```

### 범위(Range)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `RangeInverted` | `string? Min`, `string? Max` | 범위가 역전됨 (최소값이 최대값보다 큼) |
| `RangeEmpty` | `string? Value` | 범위가 비어있음 (최소값과 최대값이 같음) |

```csharp
public sealed record RangeInverted(string? Min = null, string? Max = null) : DomainErrorType;
public sealed record RangeEmpty(string? Value = null) : DomainErrorType;
```

### 상태 전이(Transition)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `InvalidTransition` | `string? FromState`, `string? ToState` | 무효한 상태 전이 (예: `Paid` -> `Active`) |

```csharp
public sealed record InvalidTransition(string? FromState = null, string? ToState = null) : DomainErrorType;
```

`FromState`와 `ToState`로 전이 전후 상태를 기록합니다. 에러 메시지와 별개로 구조화된 데이터로 전이 정보를 보존하여, 로깅/모니터링에서 활용할 수 있습니다.

### 커스텀(Custom)

```csharp
public abstract record Custom : DomainErrorType;
```

**`Custom`은** 표준 에러 타입으로 표현할 수 없는 도메인 특화 에러의 기반 클래스입니다. 파생 sealed record로 정의하여 사용합니다.

```csharp
// 엔티티 내부에 nested record로 정의
public sealed record InsufficientStock : DomainErrorType.Custom;

DomainError.For<Inventory>(new InsufficientStock(), currentStock, "재고가 부족합니다");
// 에러 코드: DomainErrors.Inventory.InsufficientStock
```

---

## Application ErrorType 카탈로그

```csharp
namespace Functorium.Applications.Errors;

public abstract record ApplicationErrorType : ErrorType;
```

**`ApplicationErrorType`은** 애플리케이션 레이어 에러의 sealed record 계층 기반입니다.

### 공통

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `Empty` | (없음) | 값이 비어있음 |
| `Null` | (없음) | 값이 null임 |
| `NotFound` | (없음) | 값을 찾을 수 없음 |
| `AlreadyExists` | (없음) | 값이 이미 존재함 |
| `Duplicate` | (없음) | 중복된 값 |
| `InvalidState` | (없음) | 유효하지 않은 상태 |
| `Unauthorized` | (없음) | 인증되지 않음 |
| `Forbidden` | (없음) | 접근 금지 |

```csharp
public sealed record Empty : ApplicationErrorType;
public sealed record Null : ApplicationErrorType;
public sealed record NotFound : ApplicationErrorType;
public sealed record AlreadyExists : ApplicationErrorType;
public sealed record Duplicate : ApplicationErrorType;
public sealed record InvalidState : ApplicationErrorType;
public sealed record Unauthorized : ApplicationErrorType;
public sealed record Forbidden : ApplicationErrorType;
```

### 검증

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `ValidationFailed` | `string? PropertyName` | 검증 실패. `PropertyName`은 실패한 속성 이름 |

```csharp
public sealed record ValidationFailed(string? PropertyName = null) : ApplicationErrorType;
```

### 비즈니스 규칙

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `BusinessRuleViolated` | `string? RuleName` | 비즈니스 규칙 위반. `RuleName`은 위반된 규칙 이름 |
| `ConcurrencyConflict` | (없음) | 동시성 충돌 |
| `ResourceLocked` | `string? ResourceName` | 리소스 잠금. `ResourceName`은 잠긴 리소스 이름 |
| `OperationCancelled` | (없음) | 작업 취소됨 |
| `InsufficientPermission` | `string? Permission` | 권한 부족. `Permission`은 필요한 권한 |

```csharp
public sealed record BusinessRuleViolated(string? RuleName = null) : ApplicationErrorType;
public sealed record ConcurrencyConflict : ApplicationErrorType;
public sealed record ResourceLocked(string? ResourceName = null) : ApplicationErrorType;
public sealed record OperationCancelled : ApplicationErrorType;
public sealed record InsufficientPermission(string? Permission = null) : ApplicationErrorType;
```

### 커스텀

```csharp
public abstract record Custom : ApplicationErrorType;
```

```csharp
// 사용 예시
public sealed record CannotProcess : ApplicationErrorType.Custom;
```

### EventErrorType

```csharp
namespace Functorium.Applications.Errors;

public abstract record EventErrorType : ErrorType;
```

**`EventErrorType`은** 도메인 이벤트 발행/처리 과정의 에러 타입입니다.

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `PublishFailed` | (없음) | 이벤트 발행 실패 |
| `HandlerFailed` | (없음) | 이벤트 핸들러 실행 실패 |
| `InvalidEventType` | (없음) | 이벤트 타입이 유효하지 않음 |
| `PublishCancelled` | (없음) | 이벤트 발행 취소됨 |

```csharp
public sealed record PublishFailed : EventErrorType;
public sealed record HandlerFailed : EventErrorType;
public sealed record InvalidEventType : EventErrorType;
public sealed record PublishCancelled : EventErrorType;
```

**커스텀 확장:**

```csharp
public abstract record Custom : EventErrorType;

// 사용 예시
public sealed record RetryExhausted : EventErrorType.Custom;
```

---

## Adapter ErrorType 카탈로그

```csharp
namespace Functorium.Adapters.Errors;

public abstract record AdapterErrorType : ErrorType;
```

**`AdapterErrorType`은** 어댑터 레이어 에러의 sealed record 계층 기반입니다.

### 공통

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `Empty` | (없음) | 값이 비어있음 |
| `Null` | (없음) | 값이 null임 |
| `NotFound` | (없음) | 값을 찾을 수 없음 |
| `PartialNotFound` | (없음) | 요청한 ID 중 일부를 찾을 수 없음 |
| `AlreadyExists` | (없음) | 값이 이미 존재함 |
| `Duplicate` | (없음) | 중복된 값 |
| `InvalidState` | (없음) | 유효하지 않은 상태 |
| `NotConfigured` | (없음) | 필수 설정이 누락됨 |
| `NotSupported` | (없음) | 지원되지 않는 연산 |
| `Unauthorized` | (없음) | 인증되지 않음 |
| `Forbidden` | (없음) | 접근 금지 |

```csharp
public sealed record Empty : AdapterErrorType;
public sealed record Null : AdapterErrorType;
public sealed record NotFound : AdapterErrorType;
public sealed record PartialNotFound : AdapterErrorType;
public sealed record AlreadyExists : AdapterErrorType;
public sealed record Duplicate : AdapterErrorType;
public sealed record InvalidState : AdapterErrorType;
public sealed record NotConfigured : AdapterErrorType;
public sealed record NotSupported : AdapterErrorType;
public sealed record Unauthorized : AdapterErrorType;
public sealed record Forbidden : AdapterErrorType;
```

### Pipeline

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `PipelineValidation` | `string? PropertyName` | 파이프라인 검증 실패. `PropertyName`은 실패한 속성 이름 |
| `PipelineException` | (없음) | 파이프라인 예외 발생 |

```csharp
public sealed record PipelineValidation(string? PropertyName = null) : AdapterErrorType;
public sealed record PipelineException : AdapterErrorType;
```

### 외부 서비스

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `ExternalServiceUnavailable` | `string? ServiceName` | 외부 서비스 사용 불가. `ServiceName`은 서비스 이름 |
| `ConnectionFailed` | `string? Target` | 연결 실패. `Target`은 연결 대상 |
| `Timeout` | `TimeSpan? Duration` | 타임아웃. `Duration`은 타임아웃 시간 |

```csharp
public sealed record ExternalServiceUnavailable(string? ServiceName = null) : AdapterErrorType;
public sealed record ConnectionFailed(string? Target = null) : AdapterErrorType;
public sealed record Timeout(TimeSpan? Duration = null) : AdapterErrorType;
```

### 데이터

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `Serialization` | `string? Format` | 직렬화 실패. `Format`은 직렬화 형식 |
| `Deserialization` | `string? Format` | 역직렬화 실패. `Format`은 역직렬화 형식 |
| `DataCorruption` | (없음) | 데이터 손상 |

```csharp
public sealed record Serialization(string? Format = null) : AdapterErrorType;
public sealed record Deserialization(string? Format = null) : AdapterErrorType;
public sealed record DataCorruption : AdapterErrorType;
```

### 커스텀

```csharp
public abstract record Custom : AdapterErrorType;
```

```csharp
// 사용 예시
public sealed record RateLimited : AdapterErrorType.Custom;
```

---

## 내부 아키텍처

### LayerErrorCore (internal)

```csharp
namespace Functorium.Abstractions.Errors;

internal static class LayerErrorCore
{
    internal static Error Create<TContext>(string prefix, ErrorType errorType, string currentValue, string message);
    internal static Error Create<TContext, TValue>(string prefix, ErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error Create<TContext, T1, T2>(...) where T1 : notnull where T2 : notnull;
    internal static Error Create<TContext, T1, T2, T3>(...) where T1 : notnull where T2 : notnull where T3 : notnull;
    internal static Error Create(string prefix, Type contextType, ErrorType errorType, string currentValue, string message);
    internal static Error ForContext(string prefix, string contextName, ErrorType errorType, string currentValue, string message);
    internal static Error ForContext<TValue>(string prefix, string contextName, ErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error FromException<TContext>(string prefix, ErrorType errorType, Exception exception);
}
```

**`LayerErrorCore`는** 4개 레이어 팩토리(`DomainError`, `ApplicationError`, `EventError`, `AdapterError`)의 공통 구현입니다. 에러 코드 문자열 `{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}`을 조립하고 `ErrorCodeFactory`에 위임합니다.

**설계 원리**: 공개 팩토리는 레이어별 타입 파라미터(`DomainErrorType`, `ApplicationErrorType` 등)를 유지하여 **컴파일 타임 안전성을** 보장합니다. `LayerErrorCore`는 기반 타입 `ErrorType`으로 수신하여 **구현 중복을 제거합니다.** 모든 메서드에 `[AggressiveInlining]`이 적용되어 JIT이 위임 호출을 인라인 처리합니다.

```csharp
// 컴파일 타임 안전성 보장 예시
DomainError.For<Email>(new DomainErrorType.Empty(), ...)       // ✅ 컴파일 OK
DomainError.For<Email>(new AdapterErrorType.Timeout(), ...)    // ❌ CS1503
```

### ErrorAssertionCore (internal)

```csharp
namespace Functorium.Testing.Assertions.Errors;

internal static class ErrorAssertionCore
{
    // Error — ErrorCode 검증, 값 검증 (1~3개), Exceptional 검증
    internal static void ShouldBeError<TContext>(Error error, string prefix, string errorName);
    internal static void ShouldBeError<TContext, TValue>(Error error, string prefix, string errorName, TValue expectedValue);
    internal static void ShouldBeExceptionalError<TContext>(Error error, string prefix, string errorName);

    // Fin<T> — 실패 상태 + ErrorCode 검증
    internal static void ShouldBeFinError<TContext, T>(Fin<T> fin, string prefix, string errorName);

    // Validation<Error, T> — 에러 포함/유일/복수 검증
    internal static void ShouldHaveError<TContext, T>(Validation<Error, T> validation, string prefix, string errorName);
    internal static void ShouldHaveOnlyError<TContext, T>(Validation<Error, T> validation, string prefix, string errorName);
    internal static void ShouldHaveErrors<TContext, T>(Validation<Error, T> validation, string prefix, params string[] errorNames);
}
```

**`ErrorAssertionCore`는** 3개 레이어 Assertion(`DomainErrorAssertions`, `ApplicationErrorAssertions`, `AdapterErrorAssertions`)의 공통 검증 로직입니다. 에러 코드 조립(`{prefix}.{typeof(TContext).Name}.{errorName}`)과 `ErrorCodeExpected<T>` 타입 캐스팅, 값 비교를 제공합니다. 레이어별 Assertion은 prefix와 에러 타입만 바인딩하는 thin wrapper입니다.

---

## 팩토리 API

### DomainError

```csharp
namespace Functorium.Domains.Errors;

public static class DomainError
{
    public static Error For<TDomain>(DomainErrorType errorType, string currentValue, string message);
    public static Error For<TDomain, TValue>(DomainErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TDomain, T1, T2>(DomainErrorType errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TDomain, T1, T2, T3>(DomainErrorType errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**에러 코드 형식:** `DomainErrors.{typeof(TDomain).Name}.{errorType.ErrorName}`

| 오버로드 | 값 파라미터 | 설명 |
|----------|-----------|------|
| `For<TDomain>(...)` | `string currentValue` | 기본 문자열 값 |
| `For<TDomain, TValue>(...)` | `TValue currentValue` | 제네릭 단일 값 |
| `For<TDomain, T1, T2>(...)` | `T1 value1, T2 value2` | 제네릭 2개 값 |
| `For<TDomain, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | 제네릭 3개 값 |

**사용 예시:**

```csharp
using static Functorium.Domains.Errors.DomainErrorType;

// 기본 사용
DomainError.For<Email>(new Empty(), "", "이메일은 비어있을 수 없습니다");
// 에러 코드: DomainErrors.Email.Empty

// 속성이 있는 에러 타입
DomainError.For<Password>(new TooShort(MinLength: 8), value, "비밀번호가 너무 짧습니다");
// 에러 코드: DomainErrors.Password.TooShort

// 상태 전이 에러
DomainError.For<Order>(new InvalidTransition(FromState: "Paid", ToState: "Active"), orderId, "유효하지 않은 상태 전이");
// 에러 코드: DomainErrors.Order.InvalidTransition

// 커스텀 에러
DomainError.For<Currency>(new Unsupported(), value, "지원되지 않는 통화입니다");
// 에러 코드: DomainErrors.Currency.Unsupported
```

### ApplicationError

```csharp
namespace Functorium.Applications.Errors;

public static class ApplicationError
{
    public static Error For<TUsecase>(ApplicationErrorType errorType, string currentValue, string message);
    public static Error For<TUsecase, TValue>(ApplicationErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TUsecase, T1, T2>(ApplicationErrorType errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TUsecase, T1, T2, T3>(ApplicationErrorType errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**에러 코드 형식:** `ApplicationErrors.{typeof(TUsecase).Name}.{errorType.ErrorName}`

| 오버로드 | 값 파라미터 | 설명 |
|----------|-----------|------|
| `For<TUsecase>(...)` | `string currentValue` | 기본 문자열 값 |
| `For<TUsecase, TValue>(...)` | `TValue currentValue` | 제네릭 단일 값 |
| `For<TUsecase, T1, T2>(...)` | `T1 value1, T2 value2` | 제네릭 2개 값 |
| `For<TUsecase, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | 제네릭 3개 값 |

**사용 예시:**

```csharp
using static Functorium.Applications.Errors.ApplicationErrorType;

ApplicationError.For<CreateProductCommand>(new AlreadyExists(), productId, "이미 존재합니다");
// 에러 코드: ApplicationErrors.CreateProductCommand.AlreadyExists

ApplicationError.For<UpdateOrderCommand>(new ValidationFailed("Quantity"), value, "수량은 양수여야 합니다");
// 에러 코드: ApplicationErrors.UpdateOrderCommand.ValidationFailed
```

### EventError

```csharp
namespace Functorium.Applications.Errors;

public static class EventError
{
    public static Error For<TPublisher>(EventErrorType errorType, string currentValue, string message);
    public static Error For<TPublisher, TValue>(EventErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error FromException<TPublisher>(Exception exception);
    public static Error FromException<TPublisher>(EventErrorType errorType, Exception exception);
}
```

**에러 코드 형식:** `ApplicationErrors.{typeof(TPublisher).Name}.{errorType.ErrorName}`

**`EventError`는** `ApplicationErrors` 접두사를 공유하며, 이벤트 발행/처리 실패를 표현합니다.

| 메서드 | 설명 |
|--------|------|
| `For<TPublisher>(...)` | Expected 에러 생성 |
| `For<TPublisher, TValue>(...)` | 제네릭 값의 Expected 에러 생성 |
| `FromException<TPublisher>(exception)` | 예외를 `PublishFailed` 타입의 Exceptional 에러로 래핑 |
| `FromException<TPublisher>(errorType, exception)` | 예외를 지정한 타입의 Exceptional 에러로 래핑 |

**사용 예시:**

```csharp
using static Functorium.Applications.Errors.EventErrorType;

EventError.For<DomainEventPublisher>(new PublishFailed(), eventType, "이벤트 발행에 실패했습니다");
// 에러 코드: ApplicationErrors.DomainEventPublisher.PublishFailed

EventError.FromException<DomainEventPublisher>(exception);
// 에러 코드: ApplicationErrors.DomainEventPublisher.PublishFailed (Exceptional)
```

### AdapterError

```csharp
namespace Functorium.Adapters.Errors;

public static class AdapterError
{
    public static Error For<TAdapter>(AdapterErrorType errorType, string currentValue, string message);
    public static Error For(Type adapterType, AdapterErrorType errorType, string currentValue, string message);
    public static Error For<TAdapter, TValue>(AdapterErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TAdapter, T1, T2>(AdapterErrorType errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TAdapter, T1, T2, T3>(AdapterErrorType errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
    public static Error FromException<TAdapter>(AdapterErrorType errorType, Exception exception);
}
```

**에러 코드 형식:** `AdapterErrors.{typeof(TAdapter).Name}.{errorType.ErrorName}`

| 오버로드 | 값 파라미터 | 설명 |
|----------|-----------|------|
| `For<TAdapter>(...)` | `string currentValue` | 기본 문자열 값 |
| `For(Type, ...)` | `string currentValue` | 런타임 Type으로 어댑터 지정 (베이스 클래스에서 `GetType()` 사용 시) |
| `For<TAdapter, TValue>(...)` | `TValue currentValue` | 제네릭 단일 값 |
| `For<TAdapter, T1, T2>(...)` | `T1 value1, T2 value2` | 제네릭 2개 값 |
| `For<TAdapter, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | 제네릭 3개 값 |
| `FromException<TAdapter>(...)` | `Exception exception` | Exception을 Exceptional 에러로 래핑 |

**사용 예시:**

```csharp
using static Functorium.Adapters.Errors.AdapterErrorType;

// Expected 에러
AdapterError.For<ProductRepository>(new NotFound(), id, "제품을 찾을 수 없습니다");
// 에러 코드: AdapterErrors.ProductRepository.NotFound

// Pipeline 에러
AdapterError.For<UsecaseValidationPipeline>(new PipelineValidation("PropertyName"), value, "검증에 실패했습니다");
// 에러 코드: AdapterErrors.UsecaseValidationPipeline.PipelineValidation

// Exception 래핑
AdapterError.FromException<UsecaseExceptionPipeline>(new PipelineException(), exception);
// 에러 코드: AdapterErrors.UsecaseExceptionPipeline.PipelineException (Exceptional)

// 런타임 Type 사용
AdapterError.For(GetType(), new ConnectionFailed("DB"), connectionString, "연결에 실패했습니다");
// 에러 코드: AdapterErrors.{실제타입이름}.ConnectionFailed
```

---

## 관련 문서

| 문서 | 설명 |
|------|------|
| [에러 시스템: 기초와 네이밍](../guides/domain/08a-error-system) | 에러 처리 원칙, Fin 패턴, 네이밍 규칙 R1~R8 |
| [에러 시스템: Domain/Application/Event](../guides/domain/08b-error-system-domain-app) | Domain, Application, Event 에러 상세 가이드 |
| [에러 시스템: Adapter와 테스트](../guides/domain/08c-error-system-adapter-testing) | Adapter 에러와 테스트 패턴 가이드 |
| [검증 시스템 사양](./03-validation) | TypedValidation, ContextualValidation 사양 |
