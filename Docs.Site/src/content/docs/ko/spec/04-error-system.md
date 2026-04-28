---
title: "에러 시스템 사양"
---

Functorium의 에러 시스템은 **레이어별 sealed record 계층**(`DomainErrorKind`, `ApplicationErrorKind`, `AdapterErrorKind`)과 **레이어별 팩토리**(`DomainError`, `ApplicationError`, `AdapterError`)로 구성됩니다. 팩토리의 공통 생성 로직은 내부 `LayerErrorCore`에 집중되고, Expected 에러의 공통 override는 `ExpectedErrorBase`에 통합됩니다. 이 사양서는 공개/내부 타입의 시그니처, 속성, 에러 코드 생성 규칙을 정의합니다.

## 요약

### 주요 타입

#### 공개 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `ErrorKind` | `Functorium.Abstractions.Errors` | 모든 레이어 에러 타입의 추상 기반 record |
| `IHasErrorCode` | `Functorium.Abstractions.Errors` | 에러 코드 접근 인터페이스 |
| `ErrorFactory` | `Functorium.Abstractions.Errors` | Expected/Exceptional 에러 생성 팩토리 |
| `DomainErrorKind` | `Functorium.Domains.Errors` | 도메인 에러 타입 sealed record 계층 (10개 카테고리) |
| `DomainError` | `Functorium.Domains.Errors` | 도메인 에러 생성 팩토리 |
| `ApplicationErrorKind` | `Functorium.Applications.Errors` | 애플리케이션 에러 타입 sealed record 계층 (14개 타입) |
| `ApplicationError` | `Functorium.Applications.Errors` | 애플리케이션 에러 생성 팩토리 |
| `AdapterErrorKind` | `Functorium.Adapters.Errors` | 어댑터 에러 타입 sealed record 계층 (20개 타입) |
| `AdapterError` | `Functorium.Adapters.Errors` | 어댑터 에러 생성 팩토리 |

#### 내부 타입 (internal)

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `ExpectedErrorBase` | `Functorium.Abstractions.Errors` | Expected 에러 4종의 공통 LanguageExt `Error` override 기반 클래스 |
| `ExpectedError` (4종) | `Functorium.Abstractions.Errors` | Expected 에러 — 값 저장만 담당하며 나머지는 base에서 상속 |
| `ExceptionalError` | `Functorium.Abstractions.Errors` | Exception을 에러 코드와 함께 래핑 |
| `LayerErrorCore` | `Functorium.Abstractions.Errors` | 3개 레이어 팩토리의 공통 에러 코드 생성 로직 |
| `ErrorAssertionCore` | `Functorium.Testing.Assertions.Errors` | 3개 레이어 Assertion의 공통 검증 로직 |

### 에러 코드 형식

모든 에러 코드는 다음 패턴을 따릅니다:

```
{LayerPrefix}.{ContextName}.{ErrorName}
```

| 레이어 | 접두사 | 예시 |
|--------|--------|------|
| Domain | `Domain` | `Domain.Email.Empty` |
| Application | `Application` | `Application.CreateProductCommand.AlreadyExists` |
| Adapter | `Adapter` | `Adapter.ProductRepository.NotFound` |

### 관계도

다음 다이어그램은 사용자 코드의 진입점(레이어 팩토리)부터 내부 에러 레코드 생성, 관측성 계층까지의 흐름을 한눈에 보여줍니다. 1.0.0-alpha.4 재설계 기준 — `ErrorKind` 추상 기반, `ErrorFactory`(internal), `ExpectedError` / `ExceptionalError`, 단축된 prefix (`"Domain"` / `"Application"` / `"Adapter"`)가 반영되어 있습니다.

```
[Public API -- User Code Path]

  DomainError.For<Email>(new DomainErrorKind.Empty(), value, msg)
      |                        |
      | (static factory)       | (classification = Kind)
      |                        +--> ErrorKind (abstract base)
      |                                |
      |                                v
      |                             *ErrorKind (per-layer derivation)
      |                                |
      |                                v
      |                             Empty/Null/Custom/... (nested)
      v
  IHasErrorCode { string ErrorCode }   <-- implemented by all errors

                   | (internal delegation)
                   v

[Internal Implementation -- InternalsVisibleTo: Adapters, Testing]

  LayerErrorCore.Create<Email>(ErrorCodePrefixes.Domain, kind, ...)
      |                                 |
      |                                 | (3 internal constants)
      |                                 +--> "Domain" / "Application" / "Adapter"
      v
  ErrorFactory.CreateExpected(prefix, typeName, kind.Name, msg)
      | (exception path)
      +--> ErrorFactory.CreateExceptional(exception, ...)
      v
  ExpectedError / ExceptionalError : LanguageExt.Error
      * ErrorCode : string     <-- "Domain.Email.Empty"
      * NumericCode : int      <-- -1000 (default)
      v
  Serilog Destructurer
      --> ErrorLogFieldNames.{Kind, NumericCode, Message, Inner, ...}
```

> **참고**: 1.0.0-alpha.4에서 적용된 전면 재설계(`ErrorType` → `ErrorKind`, `ErrorCodeFactory` → `ErrorFactory` internal, prefix 값 단축 등)가 본문·다이어그램·표에 모두 반영되어 있습니다. 이전 버전과의 차이는 릴리스 노트를 참조하세요.

---

## 에러 코드 체계

### ErrorKind (추상 기반)

```csharp
namespace Functorium.Abstractions.Errors;

public abstract record ErrorKind
{
    public virtual string Name => GetType().Name;
}
```

**`ErrorKind`은** 모든 레이어별 에러 타입(`DomainErrorKind`, `ApplicationErrorKind`, `AdapterErrorKind`)의 공통 기반입니다.

| 멤버 | 종류 | 설명 |
|------|------|------|
| `Name` | `virtual string` | 에러 코드의 마지막 세그먼트. 기본값은 `GetType().Name` |

> **레이어 접두사 상수**: `"Domain"`·`"Application"`·`"Adapter"` prefix 상수는 내부 `ErrorCodePrefixes` 클래스(`Functorium.Abstractions.Errors` 네임스페이스, `internal`)로 분리되어 있으며, 외부 공개 API에 노출되지 않습니다. 소비자는 레이어 팩토리(`DomainError.For<T>(...)` 등)를 통해 간접적으로만 사용합니다.

### IHasErrorCode

```csharp
namespace Functorium.Abstractions.Errors;

public interface IHasErrorCode
{
    string ErrorCode { get; }
}
```

**`IHasErrorCode`는** 리플렉션 없이 타입 안전하게 에러 코드에 접근하기 위한 인터페이스입니다. `ExpectedError`와 `ExceptionalError`이 이 인터페이스를 구현합니다.

---

## ErrorCode 타입

### ExpectedErrorBase (internal)

```csharp
internal abstract record ExpectedErrorBase(
    string ErrorCode,
    string ErrorMessage,
    int NumericCode = -1000,
    Option<Error> Inner = default) : Error, IHasErrorCode
```

**`ExpectedErrorBase`는** Expected 에러 4종(`ExpectedError`, `<T>`, `<T1,T2>`, `<T1,T2,T3>`)의 공통 기반 클래스입니다. LanguageExt `Error`의 13개 override를 한 곳에 정의하여 파생 타입의 중복을 제거합니다.

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

### ExpectedError (internal)

```csharp
internal record ExpectedError(
    string ErrorCode,
    string ErrorCurrentValue,
    string ErrorMessage,
    int NumericCode = -1000,
    Option<Error> Inner = default)
    : ExpectedErrorBase(ErrorCode, ErrorMessage, NumericCode, Inner)
```

**`ExpectedError`는** 비즈니스 규칙 위반 등 예상된(Expected) 에러를 표현합니다. `ExpectedErrorBase`에서 `ErrorCode`, `Message`, `Code`, `Inner`, `ToString()`, `IsExpected`, `IsExceptional` 등 공통 멤버를 상속받고, 파생 타입은 에러 발생 시점의 값(`ErrorCurrentValue`)만 추가로 정의합니다.

| 오버로드 | 추가 속성 | 설명 |
|----------|----------|------|
| `ExpectedError` | `ErrorCurrentValue: string` | 문자열 값 |
| `ExpectedError<T>` | `ErrorCurrentValue: T` | 타입 값 (1개) |
| `ExpectedError<T1, T2>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2` | 타입 값 (2개) |
| `ExpectedError<T1, T2, T3>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2`, `ErrorCurrentValue3: T3` | 타입 값 (3개) |

### ExceptionalError (internal)

```csharp
internal record ExceptionalError : Error, IHasErrorCode
{
    public ExceptionalError(string errorCode, Exception exception);
}
```

**`ExceptionalError`은** 시스템 예외(Exception)를 에러 코드와 함께 래핑합니다. `IsExpected = false`, `IsExceptional = true`입니다.

| 속성 | 타입 | 설명 |
|------|------|------|
| `ErrorCode` | `string` | `"{Prefix}.{Context}.{ErrorName}"` 형식 에러 코드 |
| `Message` | `string` | `exception.Message`에서 추출 |
| `Code` | `int` | `exception.HResult`에서 추출 |
| `Inner` | `Option<Error>` | `InnerException`이 있으면 재귀적으로 래핑 |
| `IsExpected` | `bool` | 항상 `false` |
| `IsExceptional` | `bool` | 항상 `true` |

### ErrorFactory (internal)

```csharp
namespace Functorium.Abstractions.Errors;

internal static class ErrorFactory
{
    // Expected 에러 생성
    public static Error CreateExpected(string errorCode, string errorCurrentValue, string errorMessage);
    public static Error CreateExpected<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull;
    public static Error CreateExpected<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull;
    public static Error CreateExpected<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
        where T1 : notnull where T2 : notnull where T3 : notnull;

    // Exceptional 에러 생성
    public static Error CreateExceptional(string errorCode, Exception exception);
}
```

**`ErrorFactory`는** `ExpectedError`와 `ExceptionalError` 인스턴스를 생성하는 internal 정적 팩토리입니다. 외부 소비자가 직접 호출하지 않으며, 공개 레이어 팩토리가 다음 흐름으로 위임합니다:

```
DomainError.For<T>(DomainErrorKind, ...)                          ← 레이어 타입 안전성 (공개 API)
  → LayerErrorCore.Create<T>(ErrorCodePrefixes.Domain, kind, ...)  ← 공통 에러 코드 조립 (internal)
    → ErrorFactory.CreateExpected(errorCode, ...)                  ← ExpectedError 인스턴스 생성 (internal)
```

`LayerErrorCore`가 에러 코드 문자열(`{Prefix}.{Context}.{Kind.Name}`)을 조립하고, `ErrorFactory`가 최종 `Error` 인스턴스를 생성합니다. 모든 메서드에 `[AggressiveInlining]`이 적용되어 JIT이 위임 호출을 제거하므로 성능 차이는 없습니다.

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `CreateExpected(...)` | `Error` | Expected 에러 생성 (4개 오버로드) |
| `CreateExceptional(...)` | `Error` | Exception을 Exceptional 에러로 래핑 |

---

## Domain ErrorKind 카탈로그

```csharp
namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorKind : ErrorKind;
```

**`DomainErrorKind`은** 도메인 레이어 에러의 sealed record 계층 기반입니다. 10개 카테고리로 분류됩니다.

### 존재(Existence)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `NotFound` | (없음) | 값을 찾을 수 없음 |
| `AlreadyExists` | (없음) | 값이 이미 존재함 |
| `Duplicate` | (없음) | 중복된 값 |
| `Mismatch` | (없음) | 값이 일치하지 않음 (예: 비밀번호 확인) |

```csharp
public sealed record NotFound : DomainErrorKind;
public sealed record AlreadyExists : DomainErrorKind;
public sealed record Duplicate : DomainErrorKind;
public sealed record Mismatch : DomainErrorKind;
```

### 존재 여부(Presence)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `Empty` | (없음) | 값이 비어있음 (null, empty string, empty collection 등) |
| `Null` | (없음) | 값이 null임 |

```csharp
public sealed record Empty : DomainErrorKind;
public sealed record Null : DomainErrorKind;
```

### 형식(Format)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `InvalidFormat` | `string? Pattern` | 값의 형식이 유효하지 않음. `Pattern`은 기대되는 형식 패턴 |
| `NotUpperCase` | (없음) | 값이 대문자가 아님 |
| `NotLowerCase` | (없음) | 값이 소문자가 아님 |

```csharp
public sealed record InvalidFormat(string? Pattern = null) : DomainErrorKind;
public sealed record NotUpperCase : DomainErrorKind;
public sealed record NotLowerCase : DomainErrorKind;
```

### 길이(Length)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `TooShort` | `int MinLength` | 값이 최소 길이보다 짧음. 기본값 `0` (미지정) |
| `TooLong` | `int MaxLength` | 값이 최대 길이를 초과함. 기본값 `int.MaxValue` (미지정) |
| `WrongLength` | `int Expected` | 값의 길이가 기대와 불일치. 기본값 `0` (미지정) |

```csharp
public sealed record TooShort(int MinLength = 0) : DomainErrorKind;
public sealed record TooLong(int MaxLength = int.MaxValue) : DomainErrorKind;
public sealed record WrongLength(int Expected = 0) : DomainErrorKind;
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
public sealed record Zero : DomainErrorKind;
public sealed record Negative : DomainErrorKind;
public sealed record NotPositive : DomainErrorKind;
public sealed record OutOfRange(string? Min = null, string? Max = null) : DomainErrorKind;
public sealed record BelowMinimum(string? Minimum = null) : DomainErrorKind;
public sealed record AboveMaximum(string? Maximum = null) : DomainErrorKind;
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
public sealed record DefaultDate : DomainErrorKind;
public sealed record NotInPast : DomainErrorKind;
public sealed record NotInFuture : DomainErrorKind;
public sealed record TooLate(string? Boundary = null) : DomainErrorKind;
public sealed record TooEarly(string? Boundary = null) : DomainErrorKind;
```

### 범위(Range)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `RangeInverted` | `string? Min`, `string? Max` | 범위가 역전됨 (최소값이 최대값보다 큼) |
| `RangeEmpty` | `string? Value` | 범위가 비어있음 (최소값과 최대값이 같음) |

```csharp
public sealed record RangeInverted(string? Min = null, string? Max = null) : DomainErrorKind;
public sealed record RangeEmpty(string? Value = null) : DomainErrorKind;
```

### 상태 전이(Transition)

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `InvalidTransition` | `string? FromState`, `string? ToState` | 무효한 상태 전이 (예: `Paid` -> `Active`) |

```csharp
public sealed record InvalidTransition(string? FromState = null, string? ToState = null) : DomainErrorKind;
```

`FromState`와 `ToState`로 전이 전후 상태를 기록합니다. 에러 메시지와 별개로 구조화된 데이터로 전이 정보를 보존하여, 로깅/모니터링에서 활용할 수 있습니다.

### 커스텀(Custom)

```csharp
public abstract record Custom : DomainErrorKind;
```

**`Custom`은** 표준 에러 타입으로 표현할 수 없는 도메인 특화 에러의 기반 클래스입니다. 파생 sealed record로 정의하여 사용합니다.

```csharp
// 엔티티 내부에 nested record로 정의
public sealed record InsufficientStock : DomainErrorKind.Custom;

DomainError.For<Inventory>(new InsufficientStock(), currentStock, "재고가 부족합니다");
// 에러 코드: Domain.Inventory.InsufficientStock
```

---

## Application ErrorKind 카탈로그

```csharp
namespace Functorium.Applications.Errors;

public abstract record ApplicationErrorKind : ErrorKind;
```

**`ApplicationErrorKind`은** 애플리케이션 레이어 에러의 sealed record 계층 기반입니다.

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
public sealed record Empty : ApplicationErrorKind;
public sealed record Null : ApplicationErrorKind;
public sealed record NotFound : ApplicationErrorKind;
public sealed record AlreadyExists : ApplicationErrorKind;
public sealed record Duplicate : ApplicationErrorKind;
public sealed record InvalidState : ApplicationErrorKind;
public sealed record Unauthorized : ApplicationErrorKind;
public sealed record Forbidden : ApplicationErrorKind;
```

### 검증

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `ValidationFailed` | `string? PropertyName` | 검증 실패. `PropertyName`은 실패한 속성 이름 |

```csharp
public sealed record ValidationFailed(string? PropertyName = null) : ApplicationErrorKind;
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
public sealed record BusinessRuleViolated(string? RuleName = null) : ApplicationErrorKind;
public sealed record ConcurrencyConflict : ApplicationErrorKind;
public sealed record ResourceLocked(string? ResourceName = null) : ApplicationErrorKind;
public sealed record OperationCancelled : ApplicationErrorKind;
public sealed record InsufficientPermission(string? Permission = null) : ApplicationErrorKind;
```

### 커스텀

```csharp
public abstract record Custom : ApplicationErrorKind;
```

```csharp
// 사용 예시
public sealed record CannotProcess : ApplicationErrorKind.Custom;
```

---

## Adapter ErrorKind 카탈로그

```csharp
namespace Functorium.Adapters.Errors;

public abstract record AdapterErrorKind : ErrorKind;
```

**`AdapterErrorKind`은** 어댑터 레이어 에러의 sealed record 계층 기반입니다.

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
public sealed record Empty : AdapterErrorKind;
public sealed record Null : AdapterErrorKind;
public sealed record NotFound : AdapterErrorKind;
public sealed record PartialNotFound : AdapterErrorKind;
public sealed record AlreadyExists : AdapterErrorKind;
public sealed record Duplicate : AdapterErrorKind;
public sealed record InvalidState : AdapterErrorKind;
public sealed record NotConfigured : AdapterErrorKind;
public sealed record NotSupported : AdapterErrorKind;
public sealed record Unauthorized : AdapterErrorKind;
public sealed record Forbidden : AdapterErrorKind;
```

### Pipeline

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `PipelineValidation` | `string? PropertyName` | 파이프라인 검증 실패. `PropertyName`은 실패한 속성 이름 |
| `PipelineException` | (없음) | 파이프라인 예외 발생 |

```csharp
public sealed record PipelineValidation(string? PropertyName = null) : AdapterErrorKind;
public sealed record PipelineException : AdapterErrorKind;
```

### 외부 서비스

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `ExternalServiceUnavailable` | `string? ServiceName` | 외부 서비스 사용 불가. `ServiceName`은 서비스 이름 |
| `ConnectionFailed` | `string? Target` | 연결 실패. `Target`은 연결 대상 |
| `Timeout` | `TimeSpan? Duration` | 타임아웃. `Duration`은 타임아웃 시간 |

```csharp
public sealed record ExternalServiceUnavailable(string? ServiceName = null) : AdapterErrorKind;
public sealed record ConnectionFailed(string? Target = null) : AdapterErrorKind;
public sealed record Timeout(TimeSpan? Duration = null) : AdapterErrorKind;
```

### 데이터

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `Serialization` | `string? Format` | 직렬화 실패. `Format`은 직렬화 형식 |
| `Deserialization` | `string? Format` | 역직렬화 실패. `Format`은 역직렬화 형식 |
| `DataCorruption` | (없음) | 데이터 손상 |

```csharp
public sealed record Serialization(string? Format = null) : AdapterErrorKind;
public sealed record Deserialization(string? Format = null) : AdapterErrorKind;
public sealed record DataCorruption : AdapterErrorKind;
```

### 동시성

| sealed record | 속성 | 설명 |
|---------------|------|------|
| `ConcurrencyConflict` | (없음) | `Update`/`UpdateRange` 시 로드 이후 Aggregate가 다른 주체에 의해 변경됐을 때 반환되는 낙관적 동시성 충돌. `NotFound`와 구분됩니다. 재시도 가능성 판단은 호출자 책임입니다. |

```csharp
public sealed record ConcurrencyConflict : AdapterErrorKind;
```

> `ApplicationErrorKind.ConcurrencyConflict`와의 관계:
> Application 계층 버전은 유스케이스 로직이 판단하는 **비즈니스 수준 충돌**을, Adapter 계층 버전은 영속성 계층이 **직접 감지한 충돌**(EF Core `RowVersion`·`DbUpdateConcurrencyException` 또는 `affected == 0` + ID 존재)을 의미합니다.

### 커스텀

```csharp
public abstract record Custom : AdapterErrorKind;
```

```csharp
// 사용 예시
public sealed record RateLimited : AdapterErrorKind.Custom;
```

---

## 내부 아키텍처

### LayerErrorCore (internal)

```csharp
namespace Functorium.Abstractions.Errors;

internal static class LayerErrorCore
{
    internal static Error Create<TContext>(string prefix, ErrorKind errorType, string currentValue, string message);
    internal static Error Create<TContext, TValue>(string prefix, ErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error Create<TContext, T1, T2>(...) where T1 : notnull where T2 : notnull;
    internal static Error Create<TContext, T1, T2, T3>(...) where T1 : notnull where T2 : notnull where T3 : notnull;
    internal static Error Create(string prefix, Type contextType, ErrorKind errorType, string currentValue, string message);
    internal static Error ForContext(string prefix, string contextName, ErrorKind errorType, string currentValue, string message);
    internal static Error ForContext<TValue>(string prefix, string contextName, ErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error FromException<TContext>(string prefix, ErrorKind errorType, Exception exception);
}
```

**`LayerErrorCore`는** 3개 레이어 팩토리(`DomainError`, `ApplicationError`, `AdapterError`)의 공통 구현입니다. 에러 코드 문자열 `{prefix}.{typeof(TContext).Name}.{kind.Name}`을 조립하고 `ErrorFactory`에 위임합니다.

**설계 원리**: 공개 팩토리는 레이어별 타입 파라미터(`DomainErrorKind`, `ApplicationErrorKind` 등)를 유지하여 **컴파일 타임 안전성을** 보장합니다. `LayerErrorCore`는 기반 타입 `ErrorKind`으로 수신하여 **구현 중복을 제거합니다.** 모든 메서드에 `[AggressiveInlining]`이 적용되어 JIT이 위임 호출을 인라인 처리합니다.

```csharp
// 컴파일 타임 안전성 보장 예시
DomainError.For<Email>(new DomainErrorKind.Empty(), ...)       // ✅ 컴파일 OK
DomainError.For<Email>(new AdapterErrorKind.Timeout(), ...)    // ❌ CS1503
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

**`ErrorAssertionCore`는** 3개 레이어 Assertion(`DomainErrorAssertions`, `ApplicationErrorAssertions`, `AdapterErrorAssertions`)의 공통 검증 로직입니다. 에러 코드 조립(`{prefix}.{typeof(TContext).Name}.{errorName}`)과 `ExpectedError<T>` 타입 캐스팅, 값 비교를 제공합니다. 레이어별 Assertion은 prefix와 에러 타입만 바인딩하는 thin wrapper입니다.

---

## 팩토리 API

### DomainError

```csharp
namespace Functorium.Domains.Errors;

public static class DomainError
{
    public static Error For<TDomain>(DomainErrorKind errorType, string currentValue, string message);
    public static Error For<TDomain, TValue>(DomainErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TDomain, T1, T2>(DomainErrorKind errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TDomain, T1, T2, T3>(DomainErrorKind errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**에러 코드 형식:** `Domain.{typeof(TDomain).Name}.{kind.Name}`

| 오버로드 | 값 파라미터 | 설명 |
|----------|-----------|------|
| `For<TDomain>(...)` | `string currentValue` | 기본 문자열 값 |
| `For<TDomain, TValue>(...)` | `TValue currentValue` | 제네릭 단일 값 |
| `For<TDomain, T1, T2>(...)` | `T1 value1, T2 value2` | 제네릭 2개 값 |
| `For<TDomain, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | 제네릭 3개 값 |

**사용 예시:**

```csharp
using static Functorium.Domains.Errors.DomainErrorKind;

// 기본 사용
DomainError.For<Email>(new Empty(), "", "이메일은 비어있을 수 없습니다");
// 에러 코드: Domain.Email.Empty

// 속성이 있는 에러 타입
DomainError.For<Password>(new TooShort(MinLength: 8), value, "비밀번호가 너무 짧습니다");
// 에러 코드: Domain.Password.TooShort

// 상태 전이 에러
DomainError.For<Order>(new InvalidTransition(FromState: "Paid", ToState: "Active"), orderId, "유효하지 않은 상태 전이");
// 에러 코드: Domain.Order.InvalidTransition

// 커스텀 에러
DomainError.For<Currency>(new Unsupported(), value, "지원되지 않는 통화입니다");
// 에러 코드: Domain.Currency.Unsupported
```

### ApplicationError

```csharp
namespace Functorium.Applications.Errors;

public static class ApplicationError
{
    public static Error For<TUsecase>(ApplicationErrorKind errorType, string currentValue, string message);
    public static Error For<TUsecase, TValue>(ApplicationErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TUsecase, T1, T2>(ApplicationErrorKind errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TUsecase, T1, T2, T3>(ApplicationErrorKind errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**에러 코드 형식:** `Application.{typeof(TUsecase).Name}.{kind.Name}`

| 오버로드 | 값 파라미터 | 설명 |
|----------|-----------|------|
| `For<TUsecase>(...)` | `string currentValue` | 기본 문자열 값 |
| `For<TUsecase, TValue>(...)` | `TValue currentValue` | 제네릭 단일 값 |
| `For<TUsecase, T1, T2>(...)` | `T1 value1, T2 value2` | 제네릭 2개 값 |
| `For<TUsecase, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | 제네릭 3개 값 |

**사용 예시:**

```csharp
using static Functorium.Applications.Errors.ApplicationErrorKind;

ApplicationError.For<CreateProductCommand>(new AlreadyExists(), productId, "이미 존재합니다");
// 에러 코드: Application.CreateProductCommand.AlreadyExists

ApplicationError.For<UpdateOrderCommand>(new ValidationFailed("Quantity"), value, "수량은 양수여야 합니다");
// 에러 코드: Application.UpdateOrderCommand.ValidationFailed
```

### AdapterError

```csharp
namespace Functorium.Adapters.Errors;

public static class AdapterError
{
    public static Error For<TAdapter>(AdapterErrorKind errorType, string currentValue, string message);
    public static Error For(Type adapterType, AdapterErrorKind errorType, string currentValue, string message);
    public static Error For<TAdapter, TValue>(AdapterErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TAdapter, T1, T2>(AdapterErrorKind errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TAdapter, T1, T2, T3>(AdapterErrorKind errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
    public static Error FromException<TAdapter>(AdapterErrorKind errorType, Exception exception);
}
```

**에러 코드 형식:** `Adapter.{typeof(TAdapter).Name}.{kind.Name}`

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
using static Functorium.Adapters.Errors.AdapterErrorKind;

// Expected 에러
AdapterError.For<ProductRepository>(new NotFound(), id, "제품을 찾을 수 없습니다");
// 에러 코드: Adapter.ProductRepository.NotFound

// Pipeline 에러
AdapterError.For<UsecaseValidationPipeline>(new PipelineValidation("PropertyName"), value, "검증에 실패했습니다");
// 에러 코드: Adapter.UsecaseValidationPipeline.PipelineValidation

// Exception 래핑
AdapterError.FromException<UsecaseExceptionPipeline>(new PipelineException(), exception);
// 에러 코드: Adapter.UsecaseExceptionPipeline.PipelineException (Exceptional)

// 런타임 Type 사용
AdapterError.For(GetType(), new ConnectionFailed("DB"), connectionString, "연결에 실패했습니다");
// 에러 코드: Adapter.{실제타입이름}.ConnectionFailed
```

---

## 관련 문서

| 문서 | 설명 |
|------|------|
| [에러 시스템: 기초와 네이밍](../guides/domain/08a-error-system) | 에러 처리 원칙, Fin 패턴, 네이밍 규칙 R1~R8 |
| [에러 시스템: Domain/Application/Event](../guides/domain/08b-error-system-domain-app) | Domain, Application, Event 에러 상세 가이드 |
| [에러 시스템: Adapter와 테스트](../guides/domain/08c-error-system-adapter-testing) | Adapter 에러와 테스트 패턴 가이드 |
| [검증 시스템 사양](../03-validation) | TypedValidation, ContextualValidation 사양 |
