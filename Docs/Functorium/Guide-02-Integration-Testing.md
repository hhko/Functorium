# 통합 테스트 가이드

이 문서는 Functorium 프로젝트의 통합 테스트 작성을 위한 `HostTestFixture<TProgram>` 클래스을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [HostTestFixture 구조](#hosttestfixture-구조)
- [테스트 작성](#테스트-작성)
- [환경별 설정](#환경별-설정)
- [확장 포인트](#확장-포인트)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

`HostTestFixture<TProgram>`은 WebApplicationFactory를 래핑하여 호스트 통합 테스트를 간편하게 작성할 수 있도록 합니다. DI 컨테이너, Options, 서비스 등록 등을 실제 호스트 환경에서 테스트할 수 있습니다.

### 주요 특징

- WebApplicationFactory 기반 호스트 실행
- 환경별 appsettings 파일 자동 로드
- IServiceProvider를 통한 서비스 접근
- HttpClient를 통한 API 호출
- 확장 가능한 구조

### 위치

```
Src/Functorium.Testing/Arrangements/Hosting/HostTestFixture.cs
```

### 테스트 작성 규칙

통합 테스트 작성 시 기본적인 테스트 명명 규칙, 변수 명명 규칙, AAA 패턴 등은 [단위 테스트 가이드](001-Unit-Testing.md)를 준수합니다.

| 규칙 | 설명 | 참조 |
|------|------|------|
| 테스트 명명 | T1_T2_T3 형식 | [테스트 명명 규칙](001-Unit-Testing.md#테스트-명명-규칙) |
| 변수 명명 | `sut`, `actual`, `expected` 등 | [변수 명명 규칙](001-Unit-Testing.md#변수-명명-규칙) |
| AAA 패턴 | Arrange-Act-Assert | [AAA 패턴](001-Unit-Testing.md#aaa-패턴) |

<br/>

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

### 주요 절차

**1. 테스트 환경 설정:**
```bash
# 1. appsettings.json에 기본 설정 추가 (모든 Options에 유효한 값)

# 2. appsettings.{환경}.json에 테스트별 오버라이드 설정 추가

# 3. csproj에 설정 파일 복사 설정
```

**2. Fixture 클래스 작성:**
```csharp
// 테스트 클래스 내부에 중첩 클래스로 정의
public class MyTestFixture : HostTestFixture<Program>
{
    protected override string EnvironmentName => "MyTest";
}
```

**3. 테스트 작성:**
```csharp
[Trait(nameof(IntegrationTest), IntegrationTest.Category)]
public class MyTests : IClassFixture<MyTests.MyTestFixture>
{
    // 테스트 메서드 작성
}
```

### 주요 개념

| 개념 | 설명 |
|------|------|
| `HostTestFixture<TProgram>` | 호스트 통합 테스트용 기본 Fixture |
| `EnvironmentName` | 로드할 환경 이름 (기본값: "Test") |
| `Services` | DI 컨테이너 (IServiceProvider) |
| `Client` | HTTP 요청용 HttpClient |
| `ConfigureHost` | Host 추가 설정 확장 포인트 |

<br/>

## HostTestFixture 구조

### 클래스 정의

```csharp
public class HostTestFixture<TProgram> : IAsyncLifetime where TProgram : class
{
    // WebApplicationFactory 인스턴스
    private WebApplicationFactory<TProgram>? _factory;

    // 환경 이름 (파생 클래스에서 override)
    protected virtual string EnvironmentName => "Test";

    // DI 컨테이너 접근
    public IServiceProvider Services => _factory?.Services
        ?? throw new InvalidOperationException("Fixture not initialized");

    // HTTP 클라이언트
    public HttpClient Client { get; private set; } = null!;
}
```

### 생명주기

```
IClassFixture<T> 적용
    ↓
InitializeAsync() 호출
    ↓
WebApplicationFactory 생성
    ↓
UseEnvironment(EnvironmentName)
    ↓
UseContentRoot(테스트 프로젝트 경로)
    ↓
ConfigureHost(builder) 호출
    ↓
CreateClient() - 앱 시작
    ↓
테스트 실행
    ↓
DisposeAsync() - 정리
```

### 설정 파일 로드 순서

```
1. TProgram 프로젝트의 appsettings.json (기본 설정)
2. 테스트 프로젝트의 appsettings.json (덮어씀)
3. 테스트 프로젝트의 appsettings.{EnvironmentName}.json (병합)
```

<br/>

## 테스트 작성

### 기본 구조

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

    // Fixture 정의
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

**인터페이스 구현 확인:**
```csharp
[Fact]
public void Service_ShouldImplementInterface()
{
    var services = _fixture.Services.GetServices<IStartupLogger>();
    var myLogger = services.OfType<MyOptions>().FirstOrDefault();
    myLogger.ShouldNotBeNull();
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

<br/>

## 환경별 설정

### 설정 파일 구조

```
Tests/MyProject.Tests.Integration/
├── appsettings.json                 # 기본 설정 (모든 Options에 유효한 값)
├── appsettings.Development.json     # Development 환경
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

  "Ftp": {
    "Host": "ftp.default.local",
    "Port": 21,
    "Username": "defaultuser",
    "Password": "defaultpass",
    "UsePassive": true,
    "UseTls": false,
    "ConnectionTimeout": 30,
    "RootDirectory": "/default"
  },

  "AllowedHosts": "*"
}
```

### appsettings.{환경}.json (오버라이드)

테스트에 필요한 설정만 오버라이드합니다:

```json
{
  "Ftp": {
    "Host": "ftp.test.local",
    "Port": 2121,
    "UseTls": true,
    "ConnectionTimeout": 60
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

public class OpenTelemetryTestFixture : HostTestFixture<Program>
{
    // appsettings.OpenTelemetryTest.json 로드
    protected override string EnvironmentName => "OpenTelemetryTest";
}
```

<br/>

## 확장 포인트

### ConfigureHost 오버라이드

추가적인 Host 설정이 필요한 경우:

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
        // 기본: bin/Debug/net10.0에서 3단계 상위
        var baseDirectory = AppContext.BaseDirectory;

        // 커스텀 경로 계산
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

<br/>

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

**원인:** InitializeAsync가 완료되기 전에 Services에 접근했습니다.

**해결:** IClassFixture를 올바르게 구현하고 있는지 확인하세요:
```csharp
public class MyTests : IClassFixture<MyTestFixture>  // ✓ 올바름
{
    private readonly MyTestFixture _fixture;

    public MyTests(MyTestFixture fixture)  // 생성자 주입
    {
        _fixture = fixture;
    }
}
```

### 설정 파일이 로드되지 않음

**증상:** Options 값이 기본값으로 설정됩니다.

**원인:**
1. csproj에 CopyToOutputDirectory가 설정되지 않음
2. EnvironmentName이 파일명과 일치하지 않음

**해결:**
```xml
<!-- csproj -->
<Content Include="appsettings.MyTest.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

```csharp
// Fixture
protected override string EnvironmentName => "MyTest";  // appsettings.MyTest.json
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

<br/>

## FAQ

### Q1. HostTestFixture와 WebApplicationFactory의 차이점은 무엇인가요?

`HostTestFixture`는 `WebApplicationFactory`를 래핑하여 다음을 제공합니다:

| 기능 | WebApplicationFactory | HostTestFixture |
|------|----------------------|-----------------|
| 환경 설정 | 수동 설정 | EnvironmentName 프로퍼티 |
| ContentRoot | 수동 설정 | 자동 계산 |
| 확장 포인트 | WithWebHostBuilder | ConfigureHost 메서드 |
| 생명주기 | 수동 관리 | IAsyncLifetime 구현 |

### Q2. 테스트마다 다른 환경을 사용하려면 어떻게 하나요?

각 테스트 클래스에 별도의 Fixture를 정의하세요:

```csharp
// FTP 테스트
public class FtpTests : IClassFixture<FtpTests.FtpTestFixture>
{
    public class FtpTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "FtpTest";
    }
}

// OpenTelemetry 테스트
public class OtelTests : IClassFixture<OtelTests.OtelTestFixture>
{
    public class OtelTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "OpenTelemetryTest";
    }
}
```

### Q3. Mock 서비스를 주입하려면 어떻게 하나요?

`ConfigureWebHost`를 오버라이드하여 서비스를 교체하세요:

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureServices(services =>
    {
        // 기존 서비스 제거
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IExternalService));
        if (descriptor != null)
            services.Remove(descriptor);

        // Mock 서비스 등록
        services.AddSingleton<IExternalService, MockExternalService>();
    });
}
```

### Q4. appsettings.json은 어느 것이 우선하나요?

설정 파일은 다음 순서로 병합됩니다 (나중 것이 우선):

1. TProgram 프로젝트의 appsettings.json
2. 테스트 프로젝트의 appsettings.json
3. 테스트 프로젝트의 appsettings.{EnvironmentName}.json

### Q5. HttpClient로 API 테스트 시 BaseAddress는 무엇인가요?

`HostTestFixture.Client`의 BaseAddress는 테스트 서버의 주소입니다. 상대 경로로 요청하면 됩니다:

```csharp
// 올바른 사용
var response = await _fixture.Client.GetAsync("/api/health");

// 절대 URL 불필요
// var response = await _fixture.Client.GetAsync("http://localhost/api/health");
```

### Q6. 여러 테스트 클래스에서 같은 Fixture를 공유할 수 있나요?

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

**주의:** 같은 Fixture를 공유하면 xUnit이 테스트 컬렉션 내에서 Fixture 인스턴스를 재사용합니다.

### Q7. 테스트 실행 중 로그를 확인하려면 어떻게 하나요?

Serilog Console Sink를 appsettings.json에 설정하세요:

```json
{
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
```

또는 `ConfigureWebHost`에서 테스트용 로거를 추가하세요.

### Q8. Fixture 초기화에 시간이 오래 걸리면 어떻게 하나요?

xUnit의 Collection Fixture를 사용하여 여러 테스트 클래스에서 하나의 호스트를 공유하세요:

```csharp
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<SharedTestFixture> { }

[Collection("IntegrationTests")]
public class TestA { }

[Collection("IntegrationTests")]
public class TestB { }
```

<br/>

## 참고 문서

- [단위 테스트 가이드](001-Unit-Testing.md) - 테스트 명명 규칙, AAA 패턴 등 기본 테스트 작성 규칙
- [Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Class Fixtures](https://xunit.net/docs/shared-context#class-fixture)
