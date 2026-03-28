# Options Pattern Tutorial

.NET의 Options 패턴을 이해하기 위한 단계별 예제 프로젝트입니다. 초보자부터 고급 수준까지의 예제를 포함하며, `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>` 인터페이스의 차이와 사용법을 학습할 수 있습니다.

## 📚 학습 목표

1. **IOptions<T> 기본 사용법**: Singleton 라이프사이클, 설정 값 접근
2. **IOptionsSnapshot<T> 이해**: Scoped 라이프사이클, 요청별 설정 갱신
3. **IOptionsMonitor<T> 활용**: 설정 변경 감지, 실시간 설정 업데이트
4. **설정 바인딩**: appsettings.json에서 Options로 바인딩
5. **설정 검증**: FluentValidation을 사용한 Options 검증
6. **프로덕션 패턴**: 설정 변경 감지 및 자동 리로드

## 🎯 핵심 개념: Options 패턴의 세 가지 인터페이스

.NET의 Options 패턴은 세 가지 주요 인터페이스를 제공합니다. 각각의 특징과 사용 시나리오를 이해하는 것이 중요합니다.

### IOptions<T>

**특징:**
- **라이프사이클**: Singleton
- **변경 감지**: 없음 (애플리케이션 시작 시 한 번만 로드)
- **사용 시나리오**: 변경되지 않는 설정 값

**예시:**
```csharp
var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();
var connectionString = options.Value.ConnectionString;
```

**장점:**
- 메모리 효율적 (단일 인스턴스)
- 성능 최적화 (캐싱)
- 간단한 사용법

**단점:**
- 런타임에 설정 변경 불가
- 설정 변경 시 애플리케이션 재시작 필요

### IOptionsSnapshot<T>

**특징:**
- **라이프사이클**: Scoped (각 요청마다 새로운 스냅샷)
- **변경 감지**: 요청 시점의 최신 설정 값 캡처
- **사용 시나리오**: 웹 애플리케이션의 각 HTTP 요청

**예시:**
```csharp
public class UserService
{
    private readonly IOptionsSnapshot<ApiClientOptions> _optionsSnapshot;
    
    public UserService(IOptionsSnapshot<ApiClientOptions> optionsSnapshot)
    {
        _optionsSnapshot = optionsSnapshot;
    }
    
    public void DoSomething()
    {
        var options = _optionsSnapshot.Value; // 요청 시점의 설정 값
    }
}
```

**장점:**
- 각 요청마다 최신 설정 값 보장
- 요청별로 일관된 설정 값
- 웹 애플리케이션에 최적화

**단점:**
- 요청마다 새로운 스냅샷 생성 (메모리 사용)
- 요청 처리 중간에 변경된 설정은 반영되지 않음

### IOptionsMonitor<T>

**특징:**
- **라이프사이클**: Singleton
- **변경 감지**: OnChange() 콜백으로 실시간 감지
- **사용 시나리오**: 백그라운드 서비스, 설정 변경 감지 필요 시

**예시:**
```csharp
var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<ApiClientOptions>>();

// 변경 감지 콜백 등록
var changeToken = monitor.OnChange(options =>
{
    Console.WriteLine($"Settings changed: {options.BaseUrl}");
});

// 항상 최신 값 접근
var currentValue = monitor.CurrentValue;
```

**장점:**
- 실시간 설정 변경 감지
- CurrentValue로 항상 최신 값 접근
- 애플리케이션 재시작 없이 설정 변경 가능

**단점:**
- OnChange 콜백 관리 필요
- 복잡한 사용법

## 📊 인터페이스 비교표

| 특징 | IOptions<T> | IOptionsSnapshot<T> | IOptionsMonitor<T> |
|------|-------------|---------------------|-------------------|
| **라이프사이클** | Singleton | Scoped | Singleton |
| **변경 감지** | ❌ | ⚠️ (요청 시점) | ✅ (실시간) |
| **메모리 사용** | 낮음 | 중간 | 낮음 |
| **성능** | 최고 | 좋음 | 좋음 |
| **사용 시나리오** | 변경 없는 설정 | 웹 애플리케이션 | 백그라운드 서비스 |
| **설정 변경 시** | 재시작 필요 | 다음 요청부터 | 즉시 반영 |

## 📁 프로젝트 구조

```
OptionsPattern/
├── Src/
│   └── OptionsPattern.Demo/
│       ├── Program.cs                    # 메인 진입점 (대화형 메뉴)
│       ├── OptionsPattern.Demo.csproj
│       ├── Basic/                        # 초보자 레벨
│       │   ├── Basic01_SimpleOptions.cs
│       │   ├── Basic02_OptionsRegistration.cs
│       │   ├── Basic03_AppSettingsBinding.cs
│       │   └── Basic04_OptionsValidation.cs
│       ├── Intermediate/                 # 중급 레벨
│       │   ├── Intermediate01_OptionsSnapshot.cs
│       │   ├── Intermediate02_ScopedOptions.cs
│       │   └── Intermediate03_WebAppScenario.cs
│       ├── Advanced/                      # 고급 레벨
│       │   ├── Advanced01_OptionsMonitor.cs
│       │   ├── Advanced02_ChangeDetection.cs
│       │   └── Advanced03_ReloadOnChange.cs
│       ├── Production/                    # 프로덕션 레벨
│       │   └── Production01_ConfigReload.cs
│       ├── Shared/
│       │   ├── ExampleOptions.cs         # 예제용 Options 클래스들
│       │   └── OptionsViewer.cs          # Options 값 출력 헬퍼
│       └── appsettings.json              # 설정 파일
└── Tests/
    └── OptionsPattern.Demo.Tests.Unit/
        ├── Basic/
        │   ├── Basic01_SimpleOptionsTests.cs
        │   └── Basic04_OptionsValidationTests.cs
        ├── Intermediate/
        │   └── Intermediate01_OptionsSnapshotTests.cs
        ├── Advanced/
        │   ├── Advanced01_OptionsMonitorTests.cs
        │   └── Advanced02_ChangeDetectionTests.cs
        └── Production/
            └── Production01_ConfigReloadTests.cs
```

## 🚀 실행 방법

### 1. 프로젝트 빌드

```bash
cd Tutorials/OptionsPattern/Src/OptionsPattern.Demo
dotnet build
```

### 2. 예제 실행

**대화형 메뉴:**
```bash
dotnet run
```

**특정 예제 직접 실행:**
```bash
dotnet run -- 1   # Basic01 실행
dotnet run -- 5   # Intermediate01 실행
dotnet run -- 8   # Advanced01 실행
dotnet run -- 11  # Production01 실행
```

### 3. 테스트 실행

```bash
cd Tutorials/OptionsPattern/Tests/OptionsPattern.Demo.Tests.Unit
dotnet test
```

## 📖 예제 설명

### Basic Level (IOptions<T>)

#### Basic01: Simple Options
- IOptions<T> 기본 사용법
- Options 클래스를 DI 컨테이너에 등록
- Value 속성으로 설정 값 접근
- Configure<T>()로 코드에서 직접 설정

#### Basic02: Options Registration Methods
- AddOptions<T>() 사용법
- Configure<T>() 패턴의 다양한 방법
- 여러 등록 방법 비교
- PostConfigure<T>() 사용법

#### Basic03: AppSettings Binding
- BindConfiguration() 사용법
- appsettings.json에서 설정 읽기
- 중첩 설정 구조 바인딩
- IConfiguration과 Options 패턴 통합

#### Basic04: Options Validation ⭐
- ValidateOnStart() 사용법
- FluentValidation을 사용한 검증 규칙 작성
- 검증 실패 시 동작 이해
- Validator 클래스 패턴

### Intermediate Level (IOptionsSnapshot<T>)

#### Intermediate01: Options Snapshot
- IOptionsSnapshot<T> vs IOptions<T> 차이 이해
- Scoped 라이프사이클 이해
- 요청별 설정 갱신 동작
- IOptionsSnapshot<T>의 Value 속성 사용

#### Intermediate02: Scoped Options
- HTTP 요청 시나리오 시뮬레이션
- 요청 중간에 설정 변경 시나리오
- IOptionsSnapshot<T>의 실시간 반영
- Scoped 서비스와 함께 사용

#### Intermediate03: Web Application Scenario
- 컨트롤러/서비스에서 IOptionsSnapshot<T> 사용
- 요청별 다른 설정 값 처리
- 실제 웹 애플리케이션 패턴
- 여러 서비스에서 Options 공유

### Advanced Level (IOptionsMonitor<T>)

#### Advanced01: Options Monitor
- IOptionsMonitor<T> vs IOptionsSnapshot<T> 차이 이해
- CurrentValue 속성 사용
- Singleton 라이프사이클 이해
- 실시간 설정 값 접근

#### Advanced02: Change Detection ⭐
- OnChange() 이벤트 사용법
- 변경 감지 시나리오
- 콜백에서 주의사항
- 설정 변경 시 자동 처리

#### Advanced03: Reload on Change
- AddOptions().BindConfiguration() 패턴
- IConfiguration.GetReloadToken() 사용
- 파일 변경 감지 및 자동 리로드
- reloadOnChange 옵션 이해

### Production Level

#### Production01: Configuration Reload ⭐
- appsettings.json 파일 변경 감지
- IOptionsMonitor<T>.OnChange() 콜백 구현
- 실시간 설정 업데이트 시뮬레이션
- 파일 감시(FileSystemWatcher) 통합
- 변경 사항 로깅 및 알림

## 💡 주요 학습 포인트

### 1. 라이프사이클 이해

**Singleton (IOptions<T>, IOptionsMonitor<T>):**
- 애플리케이션 전체에서 단일 인스턴스
- 메모리 효율적
- 설정 변경 시 주의 필요

**Scoped (IOptionsSnapshot<T>):**
- 각 요청(스코프)마다 새로운 인스턴스
- 웹 애플리케이션에 최적화
- 요청별로 일관된 설정 값 보장

### 2. 설정 변경 감지

**IOptions<T>:**
- 변경 감지 없음
- 애플리케이션 시작 시 한 번만 로드
- 변경 시 재시작 필요

**IOptionsSnapshot<T>:**
- 요청 시점의 최신 설정 값 캡처
- 요청 처리 중간 변경은 반영되지 않음
- 다음 요청부터 새로운 값 적용

**IOptionsMonitor<T>:**
- OnChange() 콜백으로 실시간 감지
- CurrentValue로 항상 최신 값 접근
- 애플리케이션 재시작 없이 변경 가능

### 3. 사용 시나리오 선택 가이드

**IOptions<T>를 사용할 때:**
- 설정이 변경되지 않는 경우
- 성능이 중요한 경우
- 메모리 사용을 최소화해야 하는 경우

**IOptionsSnapshot<T>를 사용할 때:**
- 웹 애플리케이션의 각 HTTP 요청
- 요청별로 일관된 설정 값이 필요한 경우
- 요청 처리 중간에 설정이 변경될 수 있는 경우

**IOptionsMonitor<T>를 사용할 때:**
- 백그라운드 서비스나 Singleton 서비스
- 설정 변경을 실시간으로 감지해야 하는 경우
- 애플리케이션 재시작 없이 설정 변경이 필요한 경우

### 4. 설정 검증 패턴

```csharp
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string ConnectionString { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 30;

    public sealed class Validator : AbstractValidator<DatabaseOptions>
    {
        public Validator()
        {
            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .WithMessage("ConnectionString is required.");

            RuleFor(x => x.ConnectionTimeout)
                .InclusiveBetween(1, 300)
                .WithMessage("ConnectionTimeout must be between 1 and 300 seconds.");
        }
    }
}
```

**검증 등록:**
```csharp
services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateFluentValidation()
    .ValidateOnStart();
```

### 5. 설정 변경 감지 패턴

```csharp
var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<ApiClientOptions>>();

// 변경 감지 콜백 등록
var changeToken = monitor.OnChange(options =>
{
    logger.LogInformation("Settings changed: BaseUrl={BaseUrl}", options.BaseUrl);
    // 설정 변경 시 처리 로직
});

// 정리 (애플리케이션 종료 시)
changeToken?.Dispose();
```

## 🔗 참고 자료

### Microsoft Learn 문서
- [Options pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [Options validation](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-validation)
- [IOptions, IOptionsSnapshot, IOptionsMonitor](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)

### Functorium 코드베이스
- `Src/Functorium/Adapters/Options/OptionsConfigurator.cs`: Options 등록 확장 메서드
- `Docs/guides/14a-adapter-pipeline-di.md`: Options 패턴 가이드
- `Src/Functorium/Adapters/Observabilities/OpenTelemetryOptions.cs`: 참조 구현체

## 🛠️ 요구사항

- .NET 10.0 SDK
- Microsoft.Extensions.Options 패키지
- Microsoft.Extensions.Configuration 패키지
- FluentValidation 패키지
- Functorium 프로젝트 참조

## 📝 라이선스

이 튜토리얼은 Functorium 프로젝트의 일부입니다.
