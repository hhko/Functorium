---
title: "통합 테스트"
---

이 문서는 Functorium 프로젝트의 통합 테스트 작성을 위한 `HostTestFixture<TProgram>` 클래스를 설명합니다.

## 들어가며

"DI 컨테이너에 등록된 서비스가 실제로 올바르게 해석되는지 어떻게 검증하는가?"
"Options 바인딩이 `appsettings.json`과 정확히 일치하는지 어떻게 확인하는가?"
"테스트 환경에서 Host 프로젝트의 전체 파이프라인을 재현하려면 무엇이 필요한가?"

단위 테스트는 개별 클래스의 동작을 검증하지만, DI 등록, 설정 바인딩, HTTP 파이프라인처럼 여러 레이어가 조합되는 영역은 통합 테스트로만 확인할 수 있습니다. `HostTestFixture<TProgram>`은 `WebApplicationFactory`를 래핑하여 이러한 통합 테스트를 간결하게 작성할 수 있도록 합니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **`HostTestFixture<TProgram>`의 구조와 생명주기** - 초기화부터 정리까지의 흐름
2. **서비스 등록 검증 패턴** - DI 컨테이너와 Options 바인딩 확인 방법
3. **환경별 설정 파일 구성** - `appsettings.{환경}.json` 로드 순서와 오버라이드
4. **HTTP API 통합 테스트** - `HttpClient`를 통한 엔드포인트 검증
5. **확장 포인트 활용** - `ConfigureHost`, `InitializeAsync` 오버라이드

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- [단위 테스트 가이드](./15a-unit-testing) - 테스트 명명 규칙, AAA 패턴
- ASP.NET Core의 DI(Dependency Injection) 개념
- `IClassFixture`와 xUnit 생명주기

> **핵심 원칙:** `HostTestFixture<TProgram>`은 실제 Host 프로젝트의 DI 컨테이너와 설정 파이프라인을 테스트 환경에서 그대로 재현합니다. 서비스 등록, Options 바인딩, HTTP 엔드포인트를 단일 Fixture로 검증할 수 있습니다.

## 요약

### 주요 코드

**기본 테스트 Fixture 정의:**
```csharp
public class MyTestFixture : HostTestFixture<Program>
{
    protected override string EnvironmentName => "Test";
}
```

**테스트 클래스 작성:**
```csharp
public class MyIntegrationTests : IClassFixture<MyTestFixture>
{
    private readonly MyTestFixture _fixture;

    public MyIntegrationTests(MyTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Service_ShouldBeRegistered()
    {
        var service = _fixture.Services.GetService<IMyService>();
        service.ShouldNotBeNull();
    }
}
```

### 주요 개념

| 개념 | 설명 |
|------|------|
| `HostTestFixture<TProgram>` | 호스트 통합 테스트용 기본 Fixture (`TProgram : class`) |
| `EnvironmentName` | 로드할 환경 이름 (기본값: `"Test"`) |
| `Services` | DI 컨테이너 (`IServiceProvider`) |
| `Client` | HTTP 요청용 `HttpClient` |
| `ConfigureHost` | Host 추가 설정 확장 포인트 (비어있는 `virtual` 메서드) |
| `GetTestProjectPath` | 테스트 프로젝트 경로 (`AppContext.BaseDirectory`에서 3단계 상위) |

### 테스트 작성 규칙

통합 테스트 작성 시 기본적인 테스트 명명 규칙, 변수 명명 규칙, AAA 패턴 등은 [단위 테스트 가이드](./15a-unit-testing)를 준수합니다.

| 규칙 | 참조 |
|------|------|
| 테스트 명명 (T1_T2_T3) | [테스트 명명 규칙](./15a-unit-testing#테스트-명명-규칙) |
| 변수 명명 (`sut`, `actual` 등) | [변수 명명 규칙](./15a-unit-testing#변수-명명-규칙) |
| AAA 패턴 | [AAA 패턴](./15a-unit-testing#aaa-패턴) |

---

## HostTestFixture 구조

### 클래스 정의

소스 위치: `Src/Functorium.Testing/Arrangements/Hosting/HostTestFixture.cs`

```csharp
public class HostTestFixture<TProgram> : IAsyncLifetime where TProgram : class
{
    private WebApplicationFactory<TProgram>? _factory;

    protected virtual string EnvironmentName => "Test";

    public IServiceProvider Services => _factory?.Services
        ?? throw new InvalidOperationException("Fixture not initialized");

    public HttpClient Client { get; private set; } = null!;
}
```

### 생명주기

```
IClassFixture<T> 적용
    ↓
InitializeAsync() 호출 (ValueTask)
    ↓
WebApplicationFactory 생성
    ↓
UseEnvironment(EnvironmentName)
    ↓
UseContentRoot(GetTestProjectPath())
    ↓
ConfigureHost(builder) 호출
    ↓
CreateClient() - 앱 시작
    ↓
테스트 실행
    ↓
DisposeAsync() - HttpClient, WebApplicationFactory 정리
```

### 설정 파일 로드 순서

```
1. TProgram 프로젝트의 appsettings.json (기본 설정)
2. 테스트 프로젝트의 appsettings.json (덮어씀)
3. 테스트 프로젝트의 appsettings.{EnvironmentName}.json (병합)
```




Fixture의 구조와 생명주기를 이해했으면, 이제 실제 테스트 코드를 작성해봅니다.

## 테스트 작성

### 기본 구조

`IClassFixture<T>`를 구현하고 Fixture를 생성자 주입으로 받는 패턴에 주목하세요.

```csharp
using Functorium.Testing.Arrangements.Hosting;

namespace MyProject.Tests.Integration;

[Trait(nameof(IntegrationTest), IntegrationTest.Category)]
public class MyServiceIntegrationTests : IClassFixture<MyServiceIntegrationTests.MyTestFixture>
{
    private readonly MyTestFixture _fixture;

    public MyServiceIntegrationTests(MyTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Host_ShouldStartSuccessfully()
    {
        _fixture.Services.ShouldNotBeNull();
    }

    [Fact]
    public void MyService_ShouldBeRegistered()
    {
        var service = _fixture.Services.GetService<IMyService>();
        service.ShouldNotBeNull();
    }

    [Fact]
    public void MyOptions_ShouldBeValidatedAndBound()
    {
        var options = _fixture.Services
            .GetRequiredService<IOptionsMonitor<MyOptions>>()
            .CurrentValue;

        options.ShouldNotBeNull();
        options.PropertyName.ShouldBe("ExpectedValue");
    }

    // Fixture 정의 (중첩 클래스)
    public class MyTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "MyTest";
    }
}
```

### 서비스 검증 패턴

**DI 등록 확인:**
```csharp
[Fact]
public void Service_ShouldBeRegistered()
{
    var service = _fixture.Services.GetService<IMyService>();
    service.ShouldNotBeNull();
}
```

**Options 바인딩 확인:**
```csharp
[Fact]
public void Options_ShouldBeBound()
{
    var options = _fixture.Services
        .GetRequiredService<IOptionsMonitor<MyOptions>>()
        .CurrentValue;

    options.PropertyA.ShouldBe("ExpectedA");
    options.PropertyB.ShouldBe(123);
}
```

### HTTP API 테스트

```csharp
[Fact]
public async Task GetEndpoint_ShouldReturnSuccess()
{
    var response = await _fixture.Client.GetAsync("/api/health");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
}

[Fact]
public async Task PostEndpoint_ShouldCreateResource()
{
    var content = new StringContent(
        JsonSerializer.Serialize(new { Name = "Test" }),
        Encoding.UTF8,
        "application/json");

    var response = await _fixture.Client.PostAsync("/api/items", content);
    response.StatusCode.ShouldBe(HttpStatusCode.Created);
}
```




테스트 작성 패턴을 익혔으면, 다음으로 테스트 환경마다 다른 설정을 적용하는 방법을 살펴봅니다.

## 환경별 설정

### 설정 파일 구조

```
Tests/MyProject.Tests.Integration/
├── appsettings.json                 # 기본 설정 (모든 Options에 유효한 값)
├── appsettings.MyTest.json          # MyTest 환경 (테스트별 오버라이드)
└── appsettings.AnotherTest.json     # AnotherTest 환경
```

### appsettings.json (기본)

모든 Options에 유효한 기본값을 설정합니다:

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyProject.Tests.Integration",
    "CollectorEndpoint": "http://127.0.0.1:18889",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": false,
    "TracingCollectorEndpoint": "",
    "MetricsCollectorEndpoint": "",
    "LoggingCollectorEndpoint": ""
  },
  "AllowedHosts": "*"
}
```

> **참고**: `OpenTelemetry` 설정의 `ServiceName`, `ServiceNamespace`, `CollectorEndpoint`는 필수 항목입니다.

### appsettings.{환경}.json (오버라이드)

테스트에 필요한 설정만 오버라이드합니다:

```json
{
  "Ftp": {
    "Host": "ftp.test.local",
    "Port": 2121,
    "UseTls": true
  }
}
```

### csproj 설정

```xml
<ItemGroup>
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="appsettings.MyTest.json">
    <DependentUpon>appsettings.json</DependentUpon>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Fixture에서 환경 지정

```csharp
public class FtpTestFixture : HostTestFixture<Program>
{
    // appsettings.FtpTest.json 로드
    protected override string EnvironmentName => "FtpTest";
}
```

### 호스트 프로젝트 참조 시 주의사항

통합 테스트 프로젝트에서 호스트 프로젝트를 참조할 때, SourceGenerator 중복 실행을 방지하려면 `ExcludeAssets=analyzers`를 추가합니다:

```xml
<ProjectReference Include="..\..\Src\MyHost\MyHost.csproj"
                  ExcludeAssets="analyzers" />
```




기본 설정으로 부족한 경우, Fixture의 확장 포인트를 통해 Host 동작을 커스터마이즈할 수 있습니다.

## 확장 포인트

### ConfigureHost 오버라이드

추가적인 Host 설정이 필요한 경우, `ConfigureHost`를 오버라이드하여 서비스를 교체하거나 설정을 추가합니다.

```csharp
public class CustomTestFixture : HostTestFixture<Program>
{
    protected override string EnvironmentName => "CustomTest";

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 테스트용 서비스 교체
            services.AddSingleton<IExternalService, MockExternalService>();
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // 추가 설정 소스
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Custom:Setting"] = "TestValue"
            });
        });
    }
}
```

### GetTestProjectPath 오버라이드

테스트 프로젝트 경로가 다른 경우:

```csharp
public class CustomPathFixture : HostTestFixture<Program>
{
    protected override string GetTestProjectPath()
    {
        // 기본: AppContext.BaseDirectory에서 3단계 상위 (bin/Debug/net10.0)
        var baseDirectory = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
    }
}
```

### InitializeAsync 오버라이드

초기화 로직 추가:

```csharp
public class ExtendedTestFixture : HostTestFixture<Program>
{
    public ILogger TestLogger { get; private set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        // 추가 초기화
        TestLogger = Services.GetRequiredService<ILogger<ExtendedTestFixture>>();
    }
}
```




## 트러블슈팅

### Options 유효성 검사 실패

**증상:**
```
OptionsValidationException: Option Validation failed for 'MyOptions.Property': Property is required.
```

**원인:** 기본 appsettings.json에 해당 Options의 유효한 값이 없습니다.

**해결:**
```json
// appsettings.json에 유효한 기본값 추가
{
  "MyOptions": {
    "Property": "ValidDefaultValue"
  }
}
```

### Fixture 초기화 실패

**증상:**
```
InvalidOperationException: Fixture not initialized
```

**원인:** `InitializeAsync`가 완료되기 전에 `Services`에 접근했습니다.

**해결:** `IClassFixture`를 올바르게 구현하고 있는지 확인:
```csharp
public class MyTests : IClassFixture<MyTestFixture>
{
    private readonly MyTestFixture _fixture;

    public MyTests(MyTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

### 설정 파일이 로드되지 않음

**증상:** Options 값이 기본값으로 설정됩니다.

**원인:**
1. csproj에 `CopyToOutputDirectory`가 설정되지 않음
2. `EnvironmentName`이 파일명과 일치하지 않음

**해결:**
```xml
<!-- csproj -->
<Content Include="appsettings.MyTest.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

```csharp
// Fixture - EnvironmentName과 파일명 일치 확인
protected override string EnvironmentName => "MyTest";  // → appsettings.MyTest.json
```

### OTLP 연결 실패

**증상:**
```
Grpc.Core.RpcException: Error starting gRPC call
```

**원인:** 테스트 환경에서 실제 OTLP 엔드포인트에 연결을 시도합니다.

**해결:** 개별 엔드포인트를 빈 문자열로 설정하여 비활성화:
```json
{
  "OpenTelemetry": {
    "TracingCollectorEndpoint": "",
    "MetricsCollectorEndpoint": "",
    "LoggingCollectorEndpoint": ""
  }
}
```

### Seq 직렬화 오류

**증상:** `System.Text.Json`으로 응답을 역직렬화할 때 `Seq<T>` 타입이 실패합니다.

**해결:** 테스트 DTO에서 `Seq<T>` 대신 `List<T>`를 사용합니다.

### SourceGenerator 중복 오류

**증상:** Mediator SourceGenerator 등이 호스트 프로젝트와 테스트 프로젝트에서 중복 실행됩니다.

**해결:** 호스트 프로젝트 참조에 `ExcludeAssets=analyzers` 추가:
```xml
<ProjectReference Include="..\..\Src\MyHost\MyHost.csproj"
                  ExcludeAssets="analyzers" />
```




## FAQ

### Q1. HostTestFixture와 WebApplicationFactory의 차이점은 무엇인가요?

`HostTestFixture`는 `WebApplicationFactory`를 래핑하여 다음을 제공합니다:

| 기능 | WebApplicationFactory | HostTestFixture |
|------|----------------------|-----------------|
| 환경 설정 | 수동 설정 | `EnvironmentName` 프로퍼티 |
| ContentRoot | 수동 설정 | 자동 계산 (`GetTestProjectPath`) |
| 확장 포인트 | `WithWebHostBuilder` | `ConfigureHost` 메서드 |
| 생명주기 | 수동 관리 | `IAsyncLifetime` 구현 |

### Q2. 테스트마다 다른 환경을 사용하려면 어떻게 하나요?

각 테스트 클래스에 별도의 Fixture를 정의하세요:

```csharp
public class FtpTests : IClassFixture<FtpTests.FtpTestFixture>
{
    public class FtpTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "FtpTest";
    }
}

public class OtelTests : IClassFixture<OtelTests.OtelTestFixture>
{
    public class OtelTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "OpenTelemetryTest";
    }
}
```

### Q3. Mock 서비스를 주입하려면 어떻게 하나요?

`ConfigureHost`를 오버라이드하여 서비스를 교체하세요:

```csharp
public class MockTestFixture : HostTestFixture<Program>
{
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IExternalService));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddSingleton<IExternalService, MockExternalService>();
        });
    }
}
```

### Q4. 여러 테스트 클래스에서 같은 Fixture를 공유할 수 있나요?

네, Fixture 클래스를 별도 파일로 분리하면 됩니다:

```csharp
// Fixtures/SharedTestFixture.cs
public class SharedTestFixture : HostTestFixture<Program>
{
    protected override string EnvironmentName => "Test";
}

// TestA.cs
public class TestA : IClassFixture<SharedTestFixture> { }

// TestB.cs
public class TestB : IClassFixture<SharedTestFixture> { }
```

xUnit Collection Fixture를 사용하면 여러 테스트 클래스에서 하나의 호스트 인스턴스를 공유할 수 있습니다:

```csharp
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<SharedTestFixture> { }

[Collection("IntegrationTests")]
public class TestA { }

[Collection("IntegrationTests")]
public class TestB { }
```

### Q5. HttpClient로 API 테스트 시 BaseAddress는 무엇인가요?

`HostTestFixture.Client`의 BaseAddress는 테스트 서버의 주소입니다. 상대 경로로 요청하면 됩니다:

```csharp
// 올바른 사용
var response = await _fixture.Client.GetAsync("/api/health");

// 절대 URL 불필요
// var response = await _fixture.Client.GetAsync("http://localhost/api/health");
```

## 참고 문서

- [단위 테스트 가이드](./15a-unit-testing) - 테스트 명명 규칙, AAA 패턴 등 기본 테스트 작성 규칙
- [테스트 라이브러리](./16-testing-library) - Functorium.Testing 라이브러리 가이드
- [Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Class Fixtures](https://xunit.net/docs/shared-context#class-fixture)
