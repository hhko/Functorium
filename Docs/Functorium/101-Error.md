# 에러 타입 가이드

이 문서는 Functorium 프로젝트의 에러 타입과 구조화된 로깅을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [에러 타입](#에러-타입)
- [에러 팩토리](#에러-팩토리)
- [Serilog 구조화](#serilog-구조화)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

LanguageExt의 `Error` 타입을 확장하여 에러 코드, 현재 값, 구조화된 로깅을 지원합니다.

### 에러 타입 계층

```
Error (LanguageExt.Common)
├── ErrorCodeExpected               - 도메인/비즈니스 에러 (문자열 값)
├── ErrorCodeExpected<T>            - 도메인/비즈니스 에러 (타입 값)
├── ErrorCodeExpected<T1, T2>       - 도메인/비즈니스 에러 (2개 타입 값)
├── ErrorCodeExpected<T1, T2, T3>   - 도메인/비즈니스 에러 (3개 타입 값)
├── ErrorCodeExceptional            - 예외 래퍼
└── ManyErrors                      - 복수 에러 컬렉션
```

### 파일 구조

```
Src/Functorium/Abstractions/Errors/
├── ErrorCodeExpected.cs              # Expected 에러 타입 (4가지 변형)
├── ErrorCodeExceptional.cs           # Exceptional 에러 타입
├── ErrorCodeFactory.cs               # 팩토리 메서드
├── ErrorCodeFieldNames.cs            # 필드명 상수
└── DestructuringPolicies/            # Serilog 구조화 정책
    ├── IErrorDestructurer.cs
    ├── ErrorsDestructuringPolicy.cs
    └── ErrorTypes/
        ├── ErrorCodeExpectedDestructurer.cs
        ├── ErrorCodeExpectedTDestructurer.cs
        ├── ErrorCodeExceptionalDestructurer.cs
        └── ManyErrorsDestructurer.cs
```

<br/>

## 요약

### 주요 명령

**에러 생성:**
```csharp
// Expected 에러 (문자열 값)
Error error = ErrorCodeFactory.Create("DomainErrors.User.NotFound", "user123", "사용자를 찾을 수 없습니다");

// Expected 에러 (타입 값)
Error error = ErrorCodeFactory.Create("DomainErrors.Sensor.TemperatureOutOfRange", 150, "온도 범위 초과");

// Exceptional 에러
Error error = ErrorCodeFactory.CreateFromException("ApplicationErrors.Database.ConnectionFailed", exception);

// 에러 코드 포맷
string code = ErrorCodeFactory.Format("DomainErrors", "User", "NotFound"); // "DomainErrors.User.NotFound"
```

### 주요 절차

**1. 에러 정의:**
```csharp
// 1. 에러 코드 정의 (도메인.컨텍스트.상세)
// 2. ErrorCodeFactory로 에러 생성
// 3. Fin<T> 또는 Either<Error, T>로 반환
```

**2. 에러 처리:**
```csharp
Fin<User> result = GetUser(userId);

result.Match(
    Succ: user => HandleSuccess(user),
    Fail: error => HandleError(error)
);
```

### 주요 개념

**1. Expected vs Exceptional**

| 구분 | Expected | Exceptional |
|------|----------|-------------|
| 용도 | 예상된 비즈니스 에러 | 예외 상황 |
| 예시 | 유효성 검증 실패, 리소스 없음 | DB 연결 실패, 네트워크 오류 |
| `IsExpected` | `true` | `false` |
| `IsExceptional` | `false` | `true` |

**2. 필드 매핑**

| Field             | Expected  | Expected&lt;T&gt;     | Exceptional | ManyErrors  |
|------             |---------- |-------------------    |-------------|------------ |
| ErrorType         | O         | O                     | O           | O           |
| ErrorCode         | O         | O                     | O           | X           |
| ErrorCodeId       | O         | O                     | O           | O           |
| ErrorCurrentValue | O         | O                     | X           | X           |
| Message           | O         | O                     | O           | X           |
| Count             | X         | X                     | X           | O           |
| Errors            | X         | X                     | X           | O           |
| ExceptionDetails  | X         | X                     | O           | X           |

<br/>

## 에러 타입

### ErrorCodeExpected

예상된 도메인/비즈니스 에러를 나타냅니다.

```csharp
internal record ErrorCodeExpected(
    string ErrorCode,          // 에러 코드 (예: "DomainErrors.User.NotFound")
    string ErrorCurrentValue,  // 현재 값 (예: "user123")
    string ErrorMessage,       // 에러 메시지
    int ErrorCodeId = -1000,   // 에러 코드 ID
    Option<Error> Inner = default) : Error
```

**속성:**

| 속성 | 타입 | 설명 |
|------|------|------|
| `ErrorCode` | `string` | 에러 식별 코드 |
| `ErrorCurrentValue` | `string` | 에러 발생 시 현재 값 |
| `Message` | `string` | 사용자 메시지 |
| `Code` | `int` | 에러 코드 ID (기본값: -1000) |
| `IsExpected` | `bool` | 항상 `true` |
| `IsExceptional` | `bool` | 항상 `false` |

**사용 예시:**
```csharp
Error error = ErrorCodeFactory.Create(
    errorCode: "DomainErrors.User.NotFound",
    errorCurrentValue: "user123",
    errorMessage: "사용자를 찾을 수 없습니다");

// 속성 확인
error.Message;        // "사용자를 찾을 수 없습니다"
error.IsExpected;     // true
error.IsExceptional;  // false
```

### ErrorCodeExpected&lt;T&gt;

타입이 있는 현재 값을 포함하는 Expected 에러입니다.

```csharp
internal record ErrorCodeExpected<T>(
    string ErrorCode,
    T ErrorCurrentValue,       // 타입 값
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default) : Error where T : notnull
```

**사용 예시:**
```csharp
// 단일 타입 값
Error error = ErrorCodeFactory.Create(
    errorCode: "DomainErrors.Sensor.TemperatureOutOfRange",
    errorCurrentValue: 150,
    errorMessage: "온도가 범위를 초과했습니다");

// 2개 타입 값
Error error = ErrorCodeFactory.Create(
    errorCode: "DomainErrors.Range.InvalidBounds",
    errorCurrentValue1: 100,
    errorCurrentValue2: 50,
    errorMessage: "최소값이 최대값보다 큽니다");

// 3개 타입 값
Error error = ErrorCodeFactory.Create(
    errorCode: "DomainErrors.Schedule.InvalidDate",
    errorCurrentValue1: 2025,
    errorCurrentValue2: 13,
    errorCurrentValue3: 32,
    errorMessage: "유효하지 않은 날짜입니다");
```

### ErrorCodeExceptional

예외(Exception)를 래핑하는 에러 타입입니다.

```csharp
internal record ErrorCodeExceptional : Error
{
    public string ErrorCode { get; init; }
    public override string Message { get; }
    public override int Code { get; }  // Exception.HResult
}
```

**속성:**

| 속성 | 타입 | 설명 |
|------|------|------|
| `ErrorCode` | `string` | 에러 식별 코드 |
| `Message` | `string` | 예외 메시지 |
| `Code` | `int` | 예외의 HResult |
| `IsExpected` | `bool` | 항상 `false` |
| `IsExceptional` | `bool` | 항상 `true` |

**주요 메서드:**

| 메서드 | 설명 |
|--------|------|
| `HasException<E>()` | 특정 예외 타입인지 확인 |
| `ToException()` | 원본 예외 반환 |
| `Is(Error)` | 같은 예외 타입인지 비교 |

**사용 예시:**
```csharp
try
{
    // 위험한 작업
}
catch (Exception ex)
{
    Error error = ErrorCodeFactory.CreateFromException(
        errorCode: "ApplicationErrors.Database.ConnectionFailed",
        exception: ex);

    // 예외 타입 확인
    if (error.HasException<SqlException>())
    {
        // SQL 예외 처리
    }
}
```

### ManyErrors

여러 에러를 하나로 묶는 컬렉션 타입입니다.

**특징:**
- LanguageExt에서 제공하는 기본 타입
- `ErrorCodeId`는 고정값 (`-2000000006`)
- 개별 에러의 메시지는 각 에러에서 제공

**사용 예시:**
```csharp
// 여러 에러 결합
Error combined = Error.Many(error1, error2, error3);

// ManyErrors 확인
if (error is ManyErrors many)
{
    foreach (Error inner in many.Errors)
    {
        // 개별 에러 처리
    }
}
```

<br/>

## 에러 팩토리

### ErrorCodeFactory

에러 인스턴스를 생성하는 정적 팩토리 클래스입니다.

```csharp
public static class ErrorCodeFactory
{
    // Expected 에러 (문자열 값)
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage);

    // Expected 에러 (타입 값)
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage);
    public static Error Create<T1, T2>(string errorCode, T1 v1, T2 v2, string errorMessage);
    public static Error Create<T1, T2, T3>(string errorCode, T1 v1, T2 v2, T3 v3, string errorMessage);

    // Exceptional 에러
    public static Error CreateFromException(string errorCode, Exception exception);

    // 에러 코드 포맷
    public static string Format(params string[] parts);
}
```

### 에러 코드 명명 규칙

```
{Layer}Errors.{ClassName}.{Reason}
```

| 부분 | 설명 | 예시 |
|------|------|------|
| `{Layer}Errors` | 레이어명 + Errors 접미사 | `DomainErrors`, `ApplicationErrors` |
| `{ClassName}` | 에러가 발생한 클래스명 | `Session`, `Subscription`, `User` |
| `{Reason}` | 에러 발생 이유 | `ReservationInPast`, `AlreadyExists` |

**예시:**
```csharp
// 좋은 예시
"DomainErrors.Session.ReservationInPast"
"DomainErrors.Subscription.GymAlreadyExists"
"ApplicationErrors.User.NotFound"
"ApplicationErrors.Payment.InsufficientFunds"

// 나쁜 예시
"Error1"
"NotFound"
"invalid_input"
```

**Format 메서드 사용:**
```csharp
// 동적 에러 코드 생성
string code = ErrorCodeFactory.Format(
    nameof(DomainErrors),
    nameof(Session),
    nameof(ReservationInPast));
// 결과: "DomainErrors.Session.ReservationInPast"
```

<br/>

## Serilog 구조화

### 설정

Serilog 구성에 `ErrorsDestructuringPolicy`를 추가합니다:

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.With<ErrorsDestructuringPolicy>()
    // ... 기타 설정
    .CreateLogger();
```

### JSON 출력 형식

#### ErrorCodeExpected

```json
{
  "Properties": {
    "Error": {
      "ErrorType": "ErrorCodeExpected",
      "ErrorCode": "DomainErrors.User.NotFound",
      "ErrorCodeId": -1000,
      "ErrorCurrentValue": "user123",
      "Message": "사용자를 찾을 수 없습니다"
    }
  }
}
```

#### ErrorCodeExpected&lt;T&gt;

```json
{
  "Properties": {
    "Error": {
      "ErrorType": "ErrorCodeExpected`1",
      "ErrorCode": "DomainErrors.Sensor.TemperatureOutOfRange",
      "ErrorCodeId": -1000,
      "ErrorCurrentValue": {
        "X": 2025,
        "Y": 2026,
        "_typeTag": "Point"
      },
      "Message": "온도 범위 초과"
    }
  }
}
```

#### ErrorCodeExpected&lt;T1, T2&gt;

```json
{
  "Properties": {
    "Error": {
      "ErrorType": "ErrorCodeExpected`2",
      "ErrorCode": "DomainErrors.Range.InvalidBounds",
      "ErrorCodeId": -1000,
      "ErrorCurrentValue1": { "X": 100, "_typeTag": "Min" },
      "ErrorCurrentValue2": { "X": 50, "_typeTag": "Max" },
      "Message": "범위가 유효하지 않습니다"
    }
  }
}
```

#### ErrorCodeExceptional

```json
{
  "Properties": {
    "Error": {
      "ErrorType": "ErrorCodeExceptional",
      "ErrorCode": "ApplicationErrors.Database.ConnectionFailed",
      "ErrorCodeId": -2147352558,
      "Message": "Attempted to divide by zero.",
      "ExceptionDetails": {
        "TargetSite": "Int32 Divide(Int32, Int32)",
        "Message": "Attempted to divide by zero.",
        "Data": [],
        "InnerException": null,
        "HelpLink": null,
        "Source": "MyApplication",
        "HResult": -2147352558,
        "StackTrace": "...",
        "_typeTag": "DivideByZeroException"
      }
    }
  }
}
```

#### ManyErrors

```json
{
  "Properties": {
    "Error": {
      "ErrorType": "ManyErrors",
      "ErrorCodeId": -2000000006,
      "Count": 2,
      "Errors": [
        {
          "ErrorType": "ErrorCodeExpected",
          "ErrorCode": "ApplicationErrors.User.NameRequired",
          "ErrorCodeId": -1000,
          "ErrorCurrentValue": "name",
          "Message": "이름은 필수입니다"
        },
        {
          "ErrorType": "ErrorCodeExpected",
          "ErrorCode": "ApplicationErrors.User.DescriptionTooLong",
          "ErrorCodeId": -1000,
          "ErrorCurrentValue": "description",
          "Message": "설명이 너무 깁니다"
        }
      ]
    }
  }
}
```

<br/>

## 트러블슈팅

### 에러가 Serilog에서 구조화되지 않을 때

**원인**: `ErrorsDestructuringPolicy`가 등록되지 않았습니다.

**해결:**
```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.With<ErrorsDestructuringPolicy>()  // 추가
    .CreateLogger();
```

### Internal 클래스 접근 불가

**원인**: `ErrorCodeExpected`, `ErrorCodeExceptional`은 `internal` 클래스입니다.

**해결:**

테스트 프로젝트에서 접근하려면 `InternalsVisibleTo`를 추가합니다:

```xml
<!-- Functorium.csproj -->
<ItemGroup>
  <InternalsVisibleTo Include="Functorium.Tests.Unit" />
</ItemGroup>
```

### HasException 타입 불일치

**원인**: 잘못된 예외 타입으로 확인하고 있습니다.

**해결:**
```csharp
// 올바른 사용
if (error.HasException<InvalidOperationException>())
{
    // 처리
}

// 기본 타입으로 확인
if (error.HasException<Exception>())
{
    // 모든 예외 처리
}
```

<br/>

## FAQ

### Q1. Expected와 Exceptional의 차이점은 무엇인가요?

**A:**

| 구분 | Expected | Exceptional |
|------|----------|-------------|
| 의미 | 예상된 비즈니스 에러 | 예상치 못한 시스템 에러 |
| 예시 | 유효성 검증 실패 | 데이터베이스 연결 실패 |
| 복구 | 클라이언트가 처리 가능 | 시스템 관리자 개입 필요 |

### Q2. 에러 코드 명명 규칙은 무엇인가요?

**A:** `{Layer}Errors.{ClassName}.{Reason}` 형식을 사용합니다:

```csharp
// 좋은 예시
"DomainErrors.Session.ReservationInPast"
"ApplicationErrors.User.NotFound"

// 나쁜 예시
"ERR001"
"error"
```

### Q3. ManyErrors는 언제 사용하나요?

**A:** 여러 유효성 검증 에러를 한 번에 반환할 때 사용합니다:

```csharp
var errors = new List<Error>();

if (string.IsNullOrEmpty(request.Name))
    errors.Add(ErrorCodeFactory.Create("ApplicationErrors.User.NameRequired", "name", "이름은 필수입니다"));

if (request.Age < 0)
    errors.Add(ErrorCodeFactory.Create("ApplicationErrors.User.InvalidAge", request.Age, "나이는 0 이상이어야 합니다"));

if (errors.Count > 0)
    return Error.Many(errors.ToSeq());
```

### Q4. Fin&lt;T&gt;와 Either&lt;Error, T&gt;의 차이점은?

**A:**

| 타입 | 실패 타입 | 용도 |
|------|----------|------|
| `Fin<T>` | `Error` (고정) | 에러 처리 전용 |
| `Either<L, R>` | 제네릭 | 범용 |

`Fin<T>`는 `Either<Error, T>`의 특수화된 버전입니다.

### Q5. ExceptionDetails에 스택 트레이스가 포함되나요?

**A:** 네, `ErrorCodeExceptional`의 Serilog 출력에는 전체 `ExceptionDetails`가 포함됩니다:
- TargetSite
- Message
- StackTrace
- InnerException
- HResult

### Q6. 커스텀 Destructurer를 추가하려면?

**A:** `IErrorDestructurer` 인터페이스를 구현합니다:

```csharp
public class MyErrorDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) => error is MyCustomError;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        var e = (MyCustomError)error;
        return new StructureValue([
            new LogEventProperty("ErrorType", new ScalarValue(e.GetType().Name)),
            // ... 추가 속성
        ]);
    }
}
```

그리고 `ErrorsDestructuringPolicy`의 `Destructurers` 리스트에 추가합니다.

## 참고 문서

- [LanguageExt Documentation](https://github.com/louthy/language-ext)
- [Serilog Destructuring](https://github.com/serilog/serilog/wiki/Structured-Data)
- [단위 테스트 가이드](./UnitTesting.md)
