# Options 설정 가이드

이 문서는 Options 패턴을 사용하여 설정 클래스를 구현하고, FluentValidation으로 검증하며, 애플리케이션 시작 시 자동으로 로깅하는 방법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [Options 클래스 구현](#options-클래스-구현)
- [의존성 등록](#의존성-등록)
- [로그 출력](#로그-출력)
- [사용 예시](#사용-예시)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

- Options 패턴을 사용한 강타입 설정 클래스 구현 방법 안내
- FluentValidation 기반 설정 검증 구현 방법
- 애플리케이션 시작 시 자동 로깅 구현 방법

### 대상 독자

- 새로운 설정 클래스를 추가하려는 개발자
- 기존 Options 패턴을 이해하려는 개발자

### 관련 파일

| 파일 | 경로 | 역할 |
|------|------|------|
| `OptionsConfigurator.cs` | `Adapters/Options/` | Options 등록 확장 메서드 |
| `IStartupOptionsLogger.cs` | `Adapters/Observabilities/Logging/` | 로깅 인터페이스 |
| `OpenTelemetryOptions.cs` | `Adapters/Observabilities/` | 참조 구현체 |
| `StartupLogger.cs` | `Adapters/Observabilities/Logging/` | 시작 시 자동 로깅 |

<br/>

## 요약

### 주요 명령

```csharp
// 1. Options 클래스 정의
public sealed class MyOptions : IStartupOptionsLogger
{
    public const string SectionName = "MySection";

    public string Name { get; set; } = string.Empty;

    public void LogConfiguration(ILogger logger) { /* ... */ }

    public sealed class Validator : AbstractValidator<MyOptions> { /* ... */ }
}

// 2. 의존성 등록
services.RegisterConfigureOptions<MyOptions, MyOptions.Validator>(
    MyOptions.SectionName);
```

### 주요 절차

1. **Options 클래스 정의**: 속성 + `SectionName` 상수
2. **IStartupOptionsLogger 구현**: `LogConfiguration` 메서드
3. **중첩 Validator 클래스 정의**: FluentValidation 규칙
4. **RegisterConfigureOptions로 DI 등록**: 서비스 등록
5. **appsettings.json에 설정 섹션 추가**: 설정 값 정의

### 주요 개념

| 개념 | 설명 |
|------|------|
| Options 패턴 | `IOptions<T>` 기반 강타입 설정 |
| Validator 패턴 | FluentValidation 기반 시작 시 검증 |
| 자동 로깅 패턴 | `IStartupOptionsLogger` 인터페이스 구현 시 자동 로깅 |

<br/>

## Options 클래스 구현

### 기본 구조

Options 클래스는 다음 요소를 포함합니다:

```csharp
using FluentValidation;
using Functorium.Adapters.Observabilities.Logging;
using Microsoft.Extensions.Logging;

namespace MyProject.Options;

public sealed class DatabaseOptions : IStartupOptionsLogger
{
    /// <summary>
    /// appsettings.json의 섹션 이름
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// 데이터베이스 연결 문자열
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 연결 타임아웃 (초)
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int RetryCount { get; set; } = 3;

    // LogConfiguration 및 Validator는 아래 섹션 참조
}
```

**규칙:**
- `sealed class` 사용 권장
- `SectionName` 상수로 appsettings.json 섹션명 정의
- 모든 속성에 기본값 지정

### IStartupOptionsLogger 구현

애플리케이션 시작 시 설정 값을 자동으로 로깅합니다:

```csharp
public void LogConfiguration(ILogger logger)
{
    const int labelWidth = 20;

    // 대주제
    logger.LogInformation("Database Configuration");
    logger.LogInformation("");

    // 세부주제: Connection
    logger.LogInformation("  Connection");
    logger.LogInformation("    {Label}: {Value}",
        "Connection String".PadRight(labelWidth),
        MaskConnectionString(ConnectionString));
    logger.LogInformation("    {Label}: {Value}",
        "Timeout".PadRight(labelWidth),
        $"{ConnectionTimeout}s");
    logger.LogInformation("");

    // 세부주제: Retry
    logger.LogInformation("  Retry");
    logger.LogInformation("    {Label}: {Value}",
        "Count".PadRight(labelWidth),
        RetryCount);
}

private static string MaskConnectionString(string connectionString)
{
    // 민감 정보 마스킹
    if (string.IsNullOrEmpty(connectionString))
        return "(not configured)";

    return connectionString.Length > 20
        ? connectionString[..20] + "..."
        : connectionString;
}
```

**규칙:**
- 대주제 → 세부주제 → 항목 형식으로 구성
- `PadRight`로 레이블 정렬 (보통 20자)
- 민감 정보(비밀번호, API 키 등)는 마스킹
- 구조화된 로깅 템플릿 `{Label}: {Value}` 사용

### 중첩 Validator 클래스

FluentValidation을 사용하여 설정 값을 검증합니다:

```csharp
public sealed class Validator : AbstractValidator<DatabaseOptions>
{
    public Validator()
    {
        // 필수 값 검증
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .WithMessage($"{nameof(ConnectionString)} is required.");

        // 범위 검증
        RuleFor(x => x.ConnectionTimeout)
            .InclusiveBetween(1, 300)
            .WithMessage($"{nameof(ConnectionTimeout)} must be between 1 and 300 seconds.");

        RuleFor(x => x.RetryCount)
            .InclusiveBetween(0, 10)
            .WithMessage($"{nameof(RetryCount)} must be between 0 and 10.");
    }
}
```

**주요 검증 규칙:**

| 메서드 | 용도 | 예시 |
|--------|------|------|
| `NotEmpty()` | 필수 값 | 문자열, 컬렉션 |
| `NotNull()` | null 불가 | 참조 타입 |
| `InclusiveBetween()` | 범위 검증 | 숫자 |
| `Must()` | 커스텀 규칙 | 복잡한 조건 |
| `When()` | 조건부 검증 | 특정 상황에서만 검증 |
| `Matches()` | 정규식 검증 | 형식 검증 |

<br/>

## 의존성 등록

### RegisterConfigureOptions 사용법

`Program.cs` 또는 서비스 등록 확장 메서드에서 호출합니다:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Options 등록
builder.Services.RegisterConfigureOptions<DatabaseOptions, DatabaseOptions.Validator>(
    DatabaseOptions.SectionName);
```

**RegisterConfigureOptions가 수행하는 작업:**

1. `IValidator<TOptions>` 구현체를 Scoped로 등록
2. `IOptions<TOptions>` 등록
3. appsettings.json의 지정된 섹션과 바인딩
4. FluentValidation 연결
5. 애플리케이션 시작 시 검증 실행 (`ValidateOnStart`)
6. `IStartupOptionsLogger` 구현 시 자동 등록

### 검증 흐름

```
애플리케이션 시작
    ↓
ValidateOnStart 트리거
    ↓
FluentValidationOptions.Validate() 호출
    ↓
IValidator<TOptions>.Validate() 실행
    ↓
검증 성공 → 애플리케이션 계속 실행
검증 실패 → OptionsValidationException 발생 → 애플리케이션 종료
```

**검증 실패 시 출력 예시:**

```
Microsoft.Extensions.Options.OptionsValidationException:
    Option Validation failed for 'DatabaseOptions.ConnectionString': ConnectionString is required.
    Option Validation failed for 'DatabaseOptions.ConnectionTimeout': ConnectionTimeout must be between 1 and 300 seconds.
```

<br/>

## 로그 출력

### LogConfiguration 구현

로그 출력 형식 패턴:

```
대주제 Configuration
                          ← 빈 줄
  세부주제 1
    레이블1:              값1
    레이블2:              값2
                          ← 빈 줄
  세부주제 2
    레이블3:              값3
```

**코드 패턴:**

```csharp
public void LogConfiguration(ILogger logger)
{
    const int labelWidth = 20;

    // 대주제
    logger.LogInformation("{Title} Configuration", "Database");
    logger.LogInformation("");

    // 세부주제
    logger.LogInformation("  {Section}", "Connection");
    logger.LogInformation("    {Label}: {Value}",
        "Host".PadRight(labelWidth), Host);
    logger.LogInformation("    {Label}: {Value}",
        "Port".PadRight(labelWidth), Port);
    logger.LogInformation("");

    // 다음 세부주제...
}
```

### StartupLogger 통합

`IStartupOptionsLogger`를 구현하면 `StartupLogger`가 자동으로 수집하여 로깅합니다:

```
================================================================================
  Application Configuration Report
================================================================================

Environment & Runtime Information
  Environment           : Development
  .NET Version          : .NET 10.0.0
  Process ID            : 12345
  Start Time            : 2024-12-09 10:30:00
  Working Directory     : C:\Projects\MyApp

Host & System Information
  Host Name             : DESKTOP-ABC123
  Machine Name          : DESKTOP-ABC123
  OS                    : Microsoft Windows 10.0.22631
  OS Architecture       : X64
  Process Architecture  : X64
  Processor Cores       : 8

--------------------------------------------------------------------------------
Options Configuration Summary
  Total Options         : 2

  - OpenTelemetryOptions
  - DatabaseOptions

--------------------------------------------------------------------------------
Database Configuration

  Connection
    Host                : localhost
    Port                : 5432

  Retry
    Count               : 3

================================================================================
```

**동작 원리:**

1. `RegisterConfigureOptions`에서 `IStartupOptionsLogger` 구현 여부 확인
2. 구현 시 `IStartupOptionsLogger`로 DI 컨테이너에 등록
3. `StartupLogger`가 `IEnumerable<IStartupOptionsLogger>` 주입받음
4. 애플리케이션 시작 시 각 Options의 `LogConfiguration()` 자동 호출

<br/>

## 사용 예시

### 전체 구현 예시

**DatabaseOptions.cs:**

```csharp
using FluentValidation;
using Functorium.Adapters.Observabilities.Logging;
using Microsoft.Extensions.Logging;

namespace MyProject.Options;

public sealed class DatabaseOptions : IStartupOptionsLogger
{
    public const string SectionName = "Database";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 30;
    public int RetryCount { get; set; } = 3;

    public void LogConfiguration(ILogger logger)
    {
        const int labelWidth = 20;

        logger.LogInformation("Database Configuration");
        logger.LogInformation("");

        logger.LogInformation("  Connection");
        logger.LogInformation("    {Label}: {Value}",
            "Host".PadRight(labelWidth), Host);
        logger.LogInformation("    {Label}: {Value}",
            "Port".PadRight(labelWidth), Port);
        logger.LogInformation("    {Label}: {Value}",
            "Database".PadRight(labelWidth), Database);
        logger.LogInformation("    {Label}: {Value}",
            "Username".PadRight(labelWidth), Username);
        logger.LogInformation("    {Label}: {Value}",
            "Password".PadRight(labelWidth), "********");
        logger.LogInformation("");

        logger.LogInformation("  Settings");
        logger.LogInformation("    {Label}: {Value}",
            "Timeout".PadRight(labelWidth), $"{ConnectionTimeout}s");
        logger.LogInformation("    {Label}: {Value}",
            "Retry Count".PadRight(labelWidth), RetryCount);
    }

    public sealed class Validator : AbstractValidator<DatabaseOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Host)
                .NotEmpty()
                .WithMessage($"{nameof(Host)} is required.");

            RuleFor(x => x.Port)
                .InclusiveBetween(1, 65535)
                .WithMessage($"{nameof(Port)} must be between 1 and 65535.");

            RuleFor(x => x.Database)
                .NotEmpty()
                .WithMessage($"{nameof(Database)} is required.");

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage($"{nameof(Username)} is required.");

            RuleFor(x => x.ConnectionTimeout)
                .InclusiveBetween(1, 300)
                .WithMessage($"{nameof(ConnectionTimeout)} must be between 1 and 300.");

            RuleFor(x => x.RetryCount)
                .InclusiveBetween(0, 10)
                .WithMessage($"{nameof(RetryCount)} must be between 0 and 10.");
        }
    }
}
```

**Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Options 등록
builder.Services.RegisterConfigureOptions<DatabaseOptions, DatabaseOptions.Validator>(
    DatabaseOptions.SectionName);

var app = builder.Build();
app.Run();
```

### appsettings.json 설정

```json
{
  "Database": {
    "Host": "db.example.com",
    "Port": 5432,
    "Database": "myapp",
    "Username": "admin",
    "Password": "secret123",
    "ConnectionTimeout": 30,
    "RetryCount": 3
  }
}
```

### 서비스에서 Options 사용

```csharp
public class DatabaseService
{
    private readonly DatabaseOptions _options;

    public DatabaseService(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public string GetConnectionString()
    {
        return $"Host={_options.Host};Port={_options.Port};" +
               $"Database={_options.Database};Username={_options.Username};" +
               $"Password={_options.Password};Timeout={_options.ConnectionTimeout}";
    }
}
```

<br/>

## 트러블슈팅

### 검증 실패 시

**증상:**
```
Microsoft.Extensions.Options.OptionsValidationException:
    Option Validation failed for 'DatabaseOptions.Host': Host is required.
```

**원인:**
- appsettings.json에 필수 값이 누락됨
- 값이 검증 규칙을 만족하지 않음

**해결:**
1. appsettings.json에서 해당 섹션 확인
2. 오류 메시지의 속성명과 규칙 확인
3. 올바른 값으로 수정

```json
{
  "Database": {
    "Host": "localhost"  // 빈 문자열이 아닌 유효한 값 입력
  }
}
```

### 로깅되지 않을 때

**증상:**
- 애플리케이션 시작 시 Options 설정 정보가 출력되지 않음

**원인 및 해결:**

1. **IStartupOptionsLogger 미구현**
   ```csharp
   // IStartupOptionsLogger 인터페이스 구현 확인
   public sealed class MyOptions : IStartupOptionsLogger
   {
       public void LogConfiguration(ILogger logger)
       {
           // 구현 필요
       }
   }
   ```

2. **RegisterConfigureOptions 미호출**
   ```csharp
   // Program.cs에서 등록 확인
   services.RegisterConfigureOptions<MyOptions, MyOptions.Validator>(
       MyOptions.SectionName);
   ```

3. **StartupLogger 미등록**
   - `OpenTelemetryBuilder.Build()` 호출 확인
   - StartupLogger가 IHostedService로 등록되어야 함

### appsettings.json 섹션을 찾지 못할 때

**증상:**
- Options 값이 모두 기본값으로 설정됨

**원인:**
- `SectionName`과 appsettings.json의 섹션명 불일치

**해결:**
```csharp
// Options 클래스
public const string SectionName = "Database";  // ← 확인

// appsettings.json
{
  "Database": {  // ← SectionName과 일치해야 함
    // ...
  }
}
```

<br/>

## FAQ

### Q1. Options와 Validator를 분리해도 되나요?

**A:** 가능하지만, 중첩 클래스를 권장합니다.

```csharp
// 권장: 중첩 클래스
public sealed class MyOptions
{
    public sealed class Validator : AbstractValidator<MyOptions> { }
}

// 가능: 분리된 클래스
public sealed class MyOptions { }
public sealed class MyOptionsValidator : AbstractValidator<MyOptions> { }
```

중첩 클래스의 장점:
- Options와 Validator가 항상 함께 유지됨
- 파일 탐색이 용이함
- 네이밍 일관성 유지

### Q2. 조건부 검증은 어떻게 하나요?

**A:** `When()` 메서드를 사용합니다.

```csharp
public sealed class Validator : AbstractValidator<MyOptions>
{
    public Validator()
    {
        // UseProxy가 true일 때만 ProxyHost 검증
        RuleFor(x => x.ProxyHost)
            .NotEmpty()
            .When(x => x.UseProxy)
            .WithMessage("ProxyHost is required when UseProxy is true.");
    }
}
```

### Q3. SmartEnum을 사용한 검증은 어떻게 하나요?

**A:** `Must()` 메서드와 `TryFromName()`을 사용합니다.

```csharp
// SmartEnum 정의
public sealed class Protocol : SmartEnum<Protocol>
{
    public static readonly Protocol Http = new(nameof(Http), 1);
    public static readonly Protocol Https = new(nameof(Https), 2);

    private Protocol(string name, int value) : base(name, value) { }
}

// Validator에서 검증
RuleFor(x => x.Protocol)
    .Must(protocol => SmartEnum<Protocol>.TryFromName(protocol, out _))
    .WithMessage($"Protocol must be one of: {string.Join(", ", SmartEnum<Protocol>.List.Select(p => p.Name))}");
```

### Q4. 런타임에 Options 값을 변경할 수 있나요?

**A:** `IOptionsMonitor<T>` 또는 `IOptionsSnapshot<T>`을 사용합니다.

| 인터페이스 | 특징 | 용도 |
|-----------|------|------|
| `IOptions<T>` | Singleton, 변경 불가 | 일반적인 경우 |
| `IOptionsSnapshot<T>` | Scoped, 요청마다 갱신 | 웹 애플리케이션 |
| `IOptionsMonitor<T>` | Singleton, 변경 감지 | 설정 파일 변경 감지 |

```csharp
public class MyService
{
    private readonly IOptionsMonitor<MyOptions> _optionsMonitor;

    public MyService(IOptionsMonitor<MyOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;

        // 변경 감지 콜백
        _optionsMonitor.OnChange(options =>
        {
            Console.WriteLine($"Options changed: {options.Name}");
        });
    }

    public void DoSomething()
    {
        // 현재 값 가져오기
        var currentOptions = _optionsMonitor.CurrentValue;
    }
}
```

### Q5. 복잡한 검증 규칙은 어떻게 작성하나요?

**A:** `Must()` 메서드로 커스텀 규칙을 정의합니다.

```csharp
public sealed class Validator : AbstractValidator<MyOptions>
{
    public Validator()
    {
        // 전체 객체 기반 검증
        RuleFor(x => x)
            .Must(options =>
                !string.IsNullOrWhiteSpace(options.PrimaryHost) ||
                !string.IsNullOrWhiteSpace(options.SecondaryHost))
            .WithMessage("At least one host must be configured.");

        // 복잡한 문자열 검증
        RuleFor(x => x.Endpoint)
            .Must(endpoint => Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .When(x => !string.IsNullOrWhiteSpace(x.Endpoint))
            .WithMessage("Endpoint must be a valid HTTP or HTTPS URL.");
    }
}
```

### Q6. Options 값을 빌드 시점에 가져올 수 있나요?

**A:** `GetOptions<T>()` 확장 메서드를 사용합니다.

```csharp
// 서비스 구성 중 Options 값 접근
var myOptions = services.GetOptions<MyOptions>();

// 조건부 서비스 등록
if (myOptions.EnableFeatureX)
{
    services.AddSingleton<IFeatureX, FeatureXImpl>();
}
```

> **주의:** 이 메서드는 임시 `ServiceProvider`를 생성하므로, 빌드 시점에서만 사용하세요.

## 참고 문서

- [OpenTelemetryOptions.cs](../../Src/Functorium/Adapters/Observabilities/OpenTelemetryOptions.cs) - 참조 구현체
- [OptionsConfigurator.cs](../../Src/Functorium/Adapters/Options/OptionsConfigurator.cs) - 등록 유틸리티
