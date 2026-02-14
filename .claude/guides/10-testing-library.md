# Functorium.Testing 라이브러리 가이드

## 목차
- [개요](#개요)
- [프로젝트 참조 설정](#프로젝트-참조-설정)
- [구조화된 로그 테스트](#구조화된-로그-테스트)
- [아키텍처 규칙 검증](#아키텍처-규칙-검증)
- [소스 생성기 테스트](#소스-생성기-테스트)
- [스케줄 Job 통합 테스트](#스케줄-job-통합-테스트)
- [참고 문서](#참고-문서)

## 개요

`Functorium.Testing`은 Functorium 프레임워크의 테스트 유틸리티 라이브러리입니다.

### 네임스페이스 구조

| 네임스페이스 | 역할 |
|---|---|
| `Functorium.Testing.Arrangements.Logging` | 구조화된 로그 캡처 (LogTestContext, StructuredTestLogger) |
| `Functorium.Testing.Arrangements.Loggers` | 인메모리 Serilog Sink (TestSink) |
| `Functorium.Testing.Arrangements.Hosting` | HTTP 통합 테스트 Fixture (HostTestFixture) |
| `Functorium.Testing.Arrangements.ScheduledJobs` | 스케줄 Job 테스트 Fixture (QuartzTestFixture) |
| `Functorium.Testing.Actions.SourceGenerators` | 소스 생성기 테스트 Runner |
| `Functorium.Testing.Assertions.ArchitectureRules` | 아키텍처 규칙 검증 |
| `Functorium.Testing.Assertions.Logging` | 로그 데이터 추출/변환 유틸리티 |
| `Functorium.Testing.Assertions.Errors` | 에러 타입 Assertion |

### 다른 가이드에 문서화된 기능

| 기능 | 참조 가이드 |
|---|---|
| `HostTestFixture<TProgram>` — HTTP 엔드포인트 통합 테스트 | [11-project-structure.md](./11-project-structure.md) |
| `ShouldBeDomainError`, `ShouldBeApplicationError` 등 에러 Assertion | [05-error-system.md](./05-error-system.md) |

---

## 프로젝트 참조 설정

### 단위 테스트 csproj 패키지 구성

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- 테스트 프레임워크 -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />

    <!-- Assertion / Mocking -->
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Verify.XunitV3" />

    <!-- 로그 테스트 -->
    <PackageReference Include="Serilog" />

    <!-- 소스 생성기 테스트 -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Src\MyProject\MyProject.csproj" />
    <ProjectReference Include="..\..\Src\Functorium.Testing\Functorium.Testing.csproj" />
  </ItemGroup>

</Project>
```

### 소스 생성기 이중 참조 패턴

소스 생성기 프로젝트를 테스트할 때는 **두 가지 참조**가 모두 필요합니다.

```xml
<!-- 1. 일반 참조: 생성기 타입(클래스)을 코드에서 사용하기 위한 참조 -->
<ItemGroup>
  <ProjectReference Include="..\..\Src\MyProject.SourceGenerator\MyProject.SourceGenerator.csproj" />
</ItemGroup>

<!-- 2. Analyzer 참조: 소스 생성기가 실제 코드 생성을 수행하도록 활성화 -->
<ItemGroup>
  <ProjectReference Include="..\..\Src\MyProject.SourceGenerator\MyProject.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

| 참조 방식 | 용도 |
|---|---|
| 일반 `ProjectReference` | 생성기 타입을 `new EntityIdGenerator()`처럼 인스턴스화 |
| `OutputItemType="Analyzer"` | 빌드 시 `[GenerateEntityId]` 등의 어트리뷰트로 코드 생성 활성화 |

> **참고**: 통합 테스트에서 Host 프로젝트를 참조할 때 Mediator SourceGenerator 중복을 방지하려면 `ExcludeAssets="analyzers"`를 추가합니다. 자세한 내용은 [11-project-structure.md](./11-project-structure.md)의 FAQ를 참조하세요.

### Using.cs 권장 패턴

```csharp
global using Functorium.Testing.Arrangements.Logging;
global using Functorium.Testing.Assertions.Logging;
global using Functorium.Testing.Actions.SourceGenerators;
global using Functorium.Testing.Assertions.ArchitectureRules;
global using Xunit;
global using Shouldly;
```

---

## 구조화된 로그 테스트

구조화된 로그 테스트는 `LoggerMessage` 어트리뷰트 기반의 로깅이 올바른 필드 구조를 출력하는지 검증합니다.

### 구성 요소

```
LogTestContext (테스트 진입점)
├── StructuredTestLogger<T>  ← ILogger<T> 구현 (Serilog 브릿지)
├── TestSink                 ← 인메모리 Serilog Sink
└── LogEventPropertyExtractor / LogEventPropertyValueConverter  ← 데이터 추출
```

### LogTestContext

로그 테스트의 핵심 컨텍스트입니다. 생성 시 내부적으로 Serilog Logger + TestSink을 구성하고, `CreateLogger<T>()`로 `ILogger<T>`를 생성합니다.

```csharp
// 네임스페이스
using Functorium.Testing.Arrangements.Logging;
```

#### 생성

```csharp
// 기본 (최소 레벨: Debug)
using var context = new LogTestContext();

// 최소 레벨 지정
using var context = new LogTestContext(LogEventLevel.Information);
```

#### CreateLogger\<T\>()

`ILogger<T>` 인스턴스를 생성합니다. 이 로거로 기록된 로그는 모두 컨텍스트에 캡처됩니다.

```csharp
var logger = context.CreateLogger<MyPipeline>();
```

#### 로그 조회 API

| 메서드 | 설명 |
|---|---|
| `LogEvents` | 캡처된 전체 LogEvent 목록 (IReadOnlyList) |
| `LogCount` | 캡처된 로그 수 |
| `GetFirstLog()` | 첫 번째 로그 (일반적으로 Request 로그) |
| `GetSecondLog()` | 두 번째 로그 (일반적으로 Response 로그) |
| `GetLogAt(int index)` | 인덱스로 로그 조회 |
| `GetLogsByLevel(LogEventLevel level)` | 특정 레벨의 로그 목록 |
| `Clear()` | 캡처된 로그 전체 삭제 |

#### 데이터 추출 API

Verify 스냅샷 테스트용으로 LogEvent를 익명 객체로 변환합니다.

| 메서드 | 설명 |
|---|---|
| `ExtractFirstLogData()` | 첫 번째 로그 데이터를 익명 객체로 추출 |
| `ExtractSecondLogData()` | 두 번째 로그 데이터를 익명 객체로 추출 |
| `ExtractLogDataAt(int index)` | 인덱스 지정 로그 데이터 추출 |
| `ExtractAllLogData()` | 전체 로그 데이터를 익명 객체 목록으로 추출 |

### StructuredTestLogger\<T\>

`ILogger<T>` → Serilog 브릿지 역할을 합니다. `LoggerMessage` 어트리뷰트로 생성된 구조화된 로깅을 올바르게 처리합니다.

- `IReadOnlyList<KeyValuePair<string, object?>>` 형태의 state에서 `{OriginalFormat}`과 속성들을 분리
- `{@Error:Error}` 형태의 명시적 속성명을 처리
- `LogEvent`를 직접 생성하여 속성명을 정확하게 유지

> **주의**: `LogTestContext.CreateLogger<T>()`를 통해 생성하세요. 직접 인스턴스화할 필요는 없습니다.

### TestSink

인메모리 Serilog `ILogEventSink` 구현입니다. `LogTestContext`가 내부적으로 사용하며, 직접 사용할 일은 거의 없습니다.

```csharp
// 네임스페이스
using Functorium.Testing.Arrangements.Loggers;
```

### LogEventPropertyExtractor

`LogEvent`에서 속성 값을 재귀적으로 추출하는 유틸리티입니다.

```csharp
// 네임스페이스
using Functorium.Testing.Assertions.Logging;
```

| 메서드 | 설명 |
|---|---|
| `ExtractValue(LogEventPropertyValue)` | ScalarValue, SequenceValue, StructureValue, DictionaryValue를 재귀적으로 추출 |
| `ExtractLogData(LogEvent)` | 단일 LogEvent → `{ Information, Properties }` 익명 객체 |
| `ExtractLogData(IEnumerable<LogEvent>)` | 여러 LogEvent → 익명 객체 목록 |

### LogEventPropertyValueConverter

`LogEventPropertyValue`를 Verify 스냅샷용 익명 객체로 변환합니다.

| 메서드 | 설명 |
|---|---|
| `ToAnonymousObject(LogEventPropertyValue)` | StructureValue → Dictionary, SequenceValue → Array, ScalarValue → 원시값 |

### Verify 스냅샷 연동 패턴

```csharp
[Fact]
public async Task Command_Request_Should_Log_Expected_Fields()
{
    // Arrange
    using var context = new LogTestContext();
    var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
    var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
    var request = new TestCommandRequest("TestName");
    var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

    MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
        (_, _) => ValueTask.FromResult(expectedResponse);

    // Act
    await pipeline.Handle(request, next, CancellationToken.None);

    // Assert - 첫 번째 로그(Request)의 필드 구조를 스냅샷으로 검증
    await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots");
}
```

**핵심 흐름:**
1. `LogTestContext` 생성
2. `CreateLogger<T>()`로 로거 생성
3. 테스트 대상 코드에 로거 주입 후 실행
4. `ExtractFirstLogData()` / `ExtractAllLogData()` 등으로 데이터 추출
5. `Verify()`로 스냅샷 비교

---

## 아키텍처 규칙 검증

ArchUnitNET 기반으로 클래스/메서드 수준의 아키텍처 규칙을 Fluent API로 검증합니다.

```csharp
// 네임스페이스
using Functorium.Testing.Assertions.ArchitectureRules;
```

### ArchitectureValidationEntryPoint.ValidateAllClasses()

ArchUnitNET의 `IObjectProvider<Class>`에 대한 확장 메서드입니다. 필터링된 클래스 집합에 대해 검증 규칙을 일괄 적용합니다.

```csharp
public static ValidationResultSummary ValidateAllClasses(
    this IObjectProvider<Class> classes,
    Architecture architecture,
    Action<ClassValidator> validationRule,
    bool verbose = false);
```

### ClassValidator Fluent API

| 메서드 | 설명 |
|---|---|
| `RequirePublic()` | public 클래스여야 함 |
| `RequireInternal()` | internal 클래스여야 함 |
| `RequireSealed()` | sealed 클래스여야 함 |
| `RequireImmutable()` | 불변성 종합 검증 (6가지 차원) |
| `RequireImplements(Type)` | 특정 인터페이스 구현 필수 |
| `RequireImplementsGenericInterface(string)` | 제네릭 인터페이스 구현 필수 |
| `RequireInherits(Type)` | 특정 기본 클래스 상속 필수 |
| `RequireAllPrivateConstructors()` | 모든 생성자가 private이어야 함 |
| `RequirePrivateAnyParameterlessConstructor()` | 매개변수 없는 private 생성자 필수 |
| `RequireMethod(string, Action<MethodValidator>)` | 특정 이름의 메서드 검증 |
| `RequireAllMethods(Action<MethodValidator>)` | 모든 메서드에 대해 검증 |
| `RequireNestedClass(string, Action<ClassValidator>?)` | 중첩 클래스 필수 + 검증 |
| `RequireNestedClassIfExists(string, Action<ClassValidator>?)` | 중첩 클래스가 있으면 검증 |
| `ValidateAndThrow()` | 단일 클래스 검증 후 즉시 예외 |

#### RequireImmutable() 검증 항목

`RequireImmutable()`은 ValueObject의 불변성을 6가지 차원에서 종합 검증합니다:

1. **Writability 검증** — 모든 non-static 멤버가 `IsImmutable()`을 만족
2. **생성자 검증** — 모든 생성자가 private (public 생성자 금지)
3. **프로퍼티 검증** — public setter 금지 (get-only만 허용)
4. **필드 검증** — public 필드 금지 (모든 필드는 private)
5. **가변 컬렉션 검증** — `List<T>`, `Dictionary<K,V>`, `HashSet<T>` 등 금지
6. **상태 변경 메서드 검증** — `Set*`, `Update*`, `Add*`, `Remove*` 등 금지

### MethodValidator Fluent API

| 메서드 | 설명 |
|---|---|
| `RequireVisibility(Visibility)` | 특정 가시성 필수 |
| `RequireStatic()` | static 메서드여야 함 |
| `RequireReturnType(Type)` | 반환 타입 검증 (제네릭 타입 매칭 지원) |
| `RequireReturnTypeOfDeclaringClass()` | 선언 클래스를 반환해야 함 |

### ValidationResultSummary.ThrowIfAnyFailures()

여러 클래스의 검증 결과를 집계한 후 실패가 있으면 `XunitException`을 발생시킵니다.

```csharp
summary.ThrowIfAnyFailures("ValueObject Immutability Rule");
```

예외 메시지 형식:
```
'ValueObject Immutability Rule' rule violation:

MyProject.ValueObjects.Email:
  - Class 'Email' must be sealed.
  - Found public constructors: .ctor

MyProject.ValueObjects.PhoneNumber:
  - Method 'Create' in class 'PhoneNumber' must be static.
```

### 사용 패턴: ValueObject 불변성 검증

```csharp
[Fact]
public void ValueObject_ShouldSatisfy_ImmutabilityRules()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ImplementInterface(typeof(IValueObject))
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class =>
        {
            // 클래스 수준 검증
            @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors()
                .RequireImmutable()
                .RequireImplements(typeof(IEquatable<>));

            // Create 메서드 검증
            @class.RequireMethod("Create", method => method
                .RequireVisibility(Visibility.Public)
                .RequireStatic()
                .RequireReturnType(typeof(Fin<>)));

            // Validate 메서드 검증
            @class.RequireMethod("Validate", method => method
                .RequireVisibility(Visibility.Public)
                .RequireStatic()
                .RequireReturnType(typeof(Validation<,>)));

            // DomainErrors 중첩 클래스 검증 (존재하는 경우만)
            @class.RequireNestedClassIfExists("DomainErrors", domainErrors =>
            {
                domainErrors
                    .RequireInternal()
                    .RequireSealed()
                    .RequireAllMethods(method => method
                        .RequireVisibility(Visibility.Public)
                        .RequireStatic()
                        .RequireReturnType(typeof(Error)));
            });
        })
        .ThrowIfAnyFailures("ValueObject Rule");
}
```

---

## 소스 생성기 테스트

`SourceGeneratorTestRunner`는 `IIncrementalGenerator`를 테스트 환경에서 실행하고 생성된 코드를 반환합니다.

```csharp
// 네임스페이스
using Functorium.Testing.Actions.SourceGenerators;
```

### SourceGeneratorTestRunner.Generate\<TGenerator\>()

소스 코드를 입력받아 소스 생성기를 실행하고 생성된 코드 문자열을 반환합니다.

```csharp
public static string? Generate<TGenerator>(this TGenerator generator, string sourceCode)
    where TGenerator : IIncrementalGenerator, new();
```

내부적으로 다음을 수행합니다:
1. 입력 소스 코드를 `CSharpSyntaxTree`로 파싱
2. 필수 어셈블리 참조 자동 추가 (System.Runtime, LanguageExt.Core, Microsoft.Extensions.Logging)
3. `CSharpGeneratorDriver`로 소스 생성기 실행
4. 컴파일러 에러가 있으면 Shouldly assertion으로 실패
5. 생성된 코드 반환 (생성되지 않은 경우 `null`)

### GenerateWithDiagnostics\<TGenerator\>()

진단 결과(Diagnostic)를 함께 반환합니다. `DiagnosticDescriptor` 테스트에 사용합니다.

```csharp
public static (string? GeneratedCode, ImmutableArray<Diagnostic> Diagnostics)
    GenerateWithDiagnostics<TGenerator>(this TGenerator generator, string sourceCode)
    where TGenerator : IIncrementalGenerator, new();
```

### Verify 스냅샷 비교 패턴

```csharp
[Fact]
public Task EntityIdGenerator_ShouldGenerate_EntityId_ForSimpleEntity()
{
    // Arrange
    string input = """
        using Functorium.Domains.SourceGenerators;

        namespace MyApp.Domain.Entities;

        [GenerateEntityId]
        public class Product
        {
            public string Name { get; set; } = string.Empty;
        }
        """;

    // Act
    string? actual = _sut.Generate(input);

    // Assert
    return Verify(actual).UseDirectory("Snapshots/EntityIdGenerator");
}
```

### 빈 입력으로 Attribute 생성 검증

소스 생성기가 마커 Attribute를 자동 생성하는 경우, 빈 문자열 입력으로 검증합니다:

```csharp
[Fact]
public Task EntityIdGenerator_ShouldGenerate_GenerateEntityIdAttribute()
{
    // Arrange
    string input = string.Empty;

    // Act
    string? actual = _sut.Generate(input);

    // Assert
    return Verify(actual).UseDirectory("Snapshots/EntityIdGenerator");
}
```

---

## 스케줄 Job 통합 테스트

Quartz.NET Job을 통합 테스트하기 위한 Fixture입니다.

```csharp
// 네임스페이스
using Functorium.Testing.Arrangements.ScheduledJobs;
```

### QuartzTestFixture\<TProgram\>

`WebApplicationFactory`를 사용하여 전체 DI 설정을 재사용하는 제네릭 Fixture입니다.

#### 주요 속성

| 속성 | 타입 | 설명 |
|---|---|---|
| `Services` | `IServiceProvider` | DI 컨테이너 |
| `Scheduler` | `IScheduler` | Quartz 스케줄러 |
| `JobListener` | `JobCompletionListener` | Job 완료 추적 리스너 |

#### 환경 설정

기본 환경은 `"Test"`입니다. 파생 클래스에서 오버라이드할 수 있습니다.

```csharp
// appsettings.Test.json이 자동으로 로드됩니다
protected virtual string EnvironmentName => "Test";
```

#### DI 확장점

`ConfigureWebHost`를 오버라이드하여 추가 설정을 적용할 수 있습니다.

```csharp
public class MyJobTestFixture : QuartzTestFixture<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 테스트용 서비스 교체
        });
    }
}
```

### ExecuteJobOnceAsync\<TJob\>()

지정된 Job을 즉시 1회 실행하고 완료를 대기합니다.

```csharp
// Job 타입에서 이름/그룹 자동 추출
Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(TimeSpan timeout)
    where TJob : IJob;

// 이름/그룹 명시적 지정
Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(
    string jobName, string jobGroup, TimeSpan timeout)
    where TJob : IJob;
```

내부 동작:
1. `JobListener.Reset()` 호출
2. 고유 이름의 테스트용 Job 생성 (`{JobName}-Test-{Guid}`)
3. `SimpleTrigger`로 즉시 1회 실행 스케줄링
4. `JobListener.WaitForJobCompletionAsync()`로 완료 대기

### JobCompletionListener

`IJobListener` 구현체로, Job 완료를 비동기적으로 추적합니다.

| 메서드 | 설명 |
|---|---|
| `WaitForJobCompletionAsync(jobName, timeout)` | Job 완료 대기 (타임아웃 시 `TimeoutException`) |
| `Reset()` | 추적 상태 초기화 (각 테스트 전 호출) |

내부적으로 `ConcurrentDictionary<string, TaskCompletionSource<JobExecutionResult>>`를 사용하여 스레드 안전하게 완료를 추적합니다.

### JobExecutionResult

Job 실행 결과를 나타내는 record입니다.

| 속성 | 타입 | 설명 |
|---|---|---|
| `JobName` | `string` | Job 이름 |
| `Success` | `bool` | 성공 여부 |
| `Result` | `object?` | Job 실행 결과 |
| `Exception` | `JobExecutionException?` | 발생한 예외 |
| `ExecutionTime` | `TimeSpan` | 실행 시간 |

### 사용 예시

```csharp
public sealed class MyJobTests : IAsyncLifetime
{
    private readonly QuartzTestFixture<Program> _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task MyJob_ShouldComplete_Successfully()
    {
        // Act
        var result = await _fixture.ExecuteJobOnceAsync<MyJob>(
            timeout: TimeSpan.FromSeconds(10));

        // Assert
        result.Success.ShouldBeTrue();
        result.Exception.ShouldBeNull();
    }
}
```

### 타임아웃 처리 패턴

```csharp
[Fact]
public async Task SlowJob_ShouldComplete_WithinTimeout()
{
    // Act & Assert
    var result = await _fixture.ExecuteJobOnceAsync<SlowJob>(
        timeout: TimeSpan.FromSeconds(30));

    result.Success.ShouldBeTrue();
    result.ExecutionTime.ShouldBeLessThan(TimeSpan.FromSeconds(30));
}

[Fact]
public async Task Job_ShouldThrow_WhenTimeout()
{
    // Act & Assert
    await Should.ThrowAsync<TimeoutException>(async () =>
        await _fixture.ExecuteJobOnceAsync<VerySlowJob>(
            timeout: TimeSpan.FromSeconds(1)));
}
```

---

## 참고 문서

- [09-unit-testing.md](./09-unit-testing.md) — 단위 테스트 규칙 (명명, AAA 패턴, MTP 설정)
- [05-error-system.md](./05-error-system.md) — 에러 타입 Assertion 패턴
- [11-project-structure.md](./11-project-structure.md) — 프로젝트 구성 (HostTestFixture, 통합 테스트)
- [observability-spec.md](./observability-spec.md) — Observability 사양 (로그 필드 정의)
