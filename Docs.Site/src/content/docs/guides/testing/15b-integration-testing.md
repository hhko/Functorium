---
title: "Integration Testing"
---

This document explains the `HostTestFixture<TProgram>` class for writing integration tests in Functorium projects.

## Introduction

"How do you verify that services registered in the DI container are actually resolved correctly?"
"How do you confirm that Options binding matches `appsettings.json` exactly?"
"What is needed to reproduce the full pipeline of a Host project in a test environment?"

Unit tests verify the behavior of individual classes, but areas where multiple layers are combined -- such as DI registration, configuration binding, and HTTP pipelines -- can only be verified through integration tests. `HostTestFixture<TProgram>` wraps `WebApplicationFactory` to enable concise writing of such integration tests.

### What You Will Learn

This document covers the following topics:

1. **Structure and lifecycle of `HostTestFixture<TProgram>`** - Flow from initialization to cleanup
2. **Service registration verification patterns** - How to check DI container and Options binding
3. **Environment-specific configuration file setup** - `appsettings.{Environment}.json` load order and overrides
4. **HTTP API integration testing** - Endpoint verification via `HttpClient`
5. **Extension point usage** - `ConfigureHost`, `InitializeAsync` overrides

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [Unit testing guide](../15a-unit-testing) - Test naming conventions, AAA pattern
- ASP.NET Core DI (Dependency Injection) concepts
- `IClassFixture` and xUnit lifecycle

> **Core principle:** `HostTestFixture<TProgram>` reproduces the actual Host project's DI container and configuration pipeline as-is in the test environment. Service registration, Options binding, and HTTP endpoints can be verified with a single Fixture.

## Summary

### Key Code

**Basic test Fixture definition:**
```csharp
public class MyTestFixture : HostTestFixture<Program>
{
    protected override string EnvironmentName => "Test";
}
```

**Writing a test class:**
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

### Key Concepts

| Concept | Description |
|------|------|
| `HostTestFixture<TProgram>` | Base Fixture for host integration testing (`TProgram : class`) |
| `EnvironmentName` | Environment name to load (default: `"Test"`) |
| `Services` | DI container (`IServiceProvider`) |
| `Client` | `HttpClient` for HTTP requests |
| `ConfigureHost` | Host additional configuration extension point (empty `virtual` method) |
| `GetTestProjectPath` | Test project path (3 levels up from `AppContext.BaseDirectory`) |

### Test Writing Rules

When writing integration tests, basic test naming conventions, variable naming conventions, AAA pattern, etc. follow the [unit testing guide](../15a-unit-testing).

| Rule | Reference |
|------|------|
| Test naming (T1_T2_T3) | [Test naming conventions](../15a-unit-testing#test-naming-conventions) |
| Variable naming (`sut`, `actual`, etc.) | [Variable naming conventions](../15a-unit-testing#variable-naming-conventions) |
| AAA pattern | [AAA pattern](../15a-unit-testing#aaa-pattern) |

---

## HostTestFixture Structure

### Class Definition

Source location: `Src/Functorium.Testing/Arrangements/Hosting/HostTestFixture.cs`

```csharp
public class HostTestFixture<TProgram> : IAsyncDisposable, IAsyncLifetime where TProgram : class
{
    private WebApplicationFactory<TProgram>? _factory;

    protected virtual string EnvironmentName => "Test";

    public IServiceProvider Services => _factory?.Services
        ?? throw new InvalidOperationException("Fixture not initialized");

    public HttpClient Client { get; private set; } = null!;
}
```

### Lifecycle

```
IClassFixture<T> applied
    ↓
InitializeAsync() called (ValueTask)
    ↓
WebApplicationFactory created
    ↓
UseEnvironment(EnvironmentName)
    ↓
UseContentRoot(GetTestProjectPath())
    ↓
ConfigureHost(builder) called
    ↓
CreateClient() - app starts
    ↓
Tests execute
    ↓
DisposeAsync() - HttpClient, WebApplicationFactory cleanup
```

### Configuration File Load Order

```
1. TProgram project's appsettings.json (default settings)
2. Test project's appsettings.json (overrides)
3. Test project's appsettings.{EnvironmentName}.json (merged)
```




Now that we understand the Fixture's structure and lifecycle, let us write actual test code.

## Writing Tests

### Basic Structure

Note the pattern of implementing `IClassFixture<T>` and receiving the Fixture via constructor injection.

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

    // Fixture definition (nested class)
    public class MyTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "MyTest";
    }
}
```

### Service Verification Patterns

**DI registration check:**
```csharp
[Fact]
public void Service_ShouldBeRegistered()
{
    var service = _fixture.Services.GetService<IMyService>();
    service.ShouldNotBeNull();
}
```

**Options binding check:**
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

### HTTP API Testing

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




Now that we have learned the test writing patterns, let us examine how to apply different configurations per test environment.

## Environment-Specific Configuration

### Configuration File Structure

```
Tests/MyProject.Tests.Integration/
├── appsettings.json                 # Default settings (valid values for all Options)
├── appsettings.MyTest.json          # MyTest environment (per-test overrides)
└── appsettings.AnotherTest.json     # AnotherTest environment
```

### appsettings.json (Default)

Set valid default values for all Options:

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyProject.Tests.Integration",
    "ServiceNamespace": "MyProject",
    "CollectorEndpoint": "http://127.0.0.1:18889",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": false,
    "TracingEndpoint": "",
    "MetricsEndpoint": "",
    "LoggingEndpoint": ""
  },
  "AllowedHosts": "*"
}
```

> **Note**: `ServiceName`, `ServiceNamespace`, and `CollectorEndpoint` in the `OpenTelemetry` settings are required fields.

### appsettings.{Environment}.json (Override)

Override only the settings needed for the test:

```json
{
  "Ftp": {
    "Host": "ftp.test.local",
    "Port": 2121,
    "UseTls": true
  }
}
```

### csproj Configuration

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

### Specifying Environment in Fixture

```csharp
public class FtpTestFixture : HostTestFixture<Program>
{
    // Loads appsettings.FtpTest.json
    protected override string EnvironmentName => "FtpTest";
}
```

### Precautions When Referencing Host Projects

When referencing host projects from integration test projects, add `ExcludeAssets=analyzers` to prevent duplicate SourceGenerator execution:

```xml
<ProjectReference Include="..\..\Src\MyHost\MyHost.csproj"
                  ExcludeAssets="analyzers" />
```




When the default configuration is insufficient, you can customize Host behavior through the Fixture's extension points.

## Extension Points

### ConfigureHost Override

When additional Host configuration is needed, override `ConfigureHost` to replace services or add configuration.

```csharp
public class CustomTestFixture : HostTestFixture<Program>
{
    protected override string EnvironmentName => "CustomTest";

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace with test service
            services.AddSingleton<IExternalService, MockExternalService>();
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Additional configuration source
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Custom:Setting"] = "TestValue"
            });
        });
    }
}
```

### GetTestProjectPath Override

When the test project path differs:

```csharp
public class CustomPathFixture : HostTestFixture<Program>
{
    protected override string GetTestProjectPath()
    {
        // Default: 3 levels up from AppContext.BaseDirectory (bin/Debug/net10.0)
        var baseDirectory = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
    }
}
```

### InitializeAsync Override

Adding initialization logic:

```csharp
public class ExtendedTestFixture : HostTestFixture<Program>
{
    public ILogger TestLogger { get; private set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        // Additional initialization
        TestLogger = Services.GetRequiredService<ILogger<ExtendedTestFixture>>();
    }
}
```




## Troubleshooting

### Options Validation Failure

**Symptom:**
```
OptionsValidationException: Option Validation failed for 'MyOptions.Property': Property is required.
```

**Cause:** The default appsettings.json does not contain valid values for the corresponding Options.

**Resolution:**
```json
// Add valid default values to appsettings.json
{
  "MyOptions": {
    "Property": "ValidDefaultValue"
  }
}
```

### Fixture Initialization Failure

**Symptom:**
```
InvalidOperationException: Fixture not initialized
```

**Cause:** `Services` was accessed before `InitializeAsync` completed.

**Resolution:** Verify that `IClassFixture` is correctly implemented:
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

### Configuration File Not Loaded

**Symptom:** Options values are set to defaults.

**Cause:**
1. `CopyToOutputDirectory` not set in csproj
2. `EnvironmentName` does not match the filename

**Resolution:**
```xml
<!-- csproj -->
<Content Include="appsettings.MyTest.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

```csharp
// Fixture - Verify EnvironmentName matches filename
protected override string EnvironmentName => "MyTest";  // → appsettings.MyTest.json
```

### OTLP Connection Failure

**Symptom:**
```
Grpc.Core.RpcException: Error starting gRPC call
```

**Cause:** The test environment attempts to connect to the actual OTLP endpoint.

**Resolution:** Disable by setting individual endpoints to empty strings:
```json
{
  "OpenTelemetry": {
    "TracingEndpoint": "",
    "MetricsEndpoint": "",
    "LoggingEndpoint": ""
  }
}
```

### Seq Serialization Error

**Symptom:** Deserialization of responses with `System.Text.Json` fails for `Seq<T>` types.

**Resolution:** Use `List<T>` instead of `Seq<T>` in test DTOs.

### SourceGenerator Duplicate Error

**Symptom:** Mediator SourceGenerator and others run in duplicate across host and test projects.

**Resolution:** Add `ExcludeAssets=analyzers` to the host project reference:
```xml
<ProjectReference Include="..\..\Src\MyHost\MyHost.csproj"
                  ExcludeAssets="analyzers" />
```




## FAQ

### Q1. What is the difference between HostTestFixture and WebApplicationFactory?

`HostTestFixture` wraps `WebApplicationFactory` and provides the following:

| Feature | WebApplicationFactory | HostTestFixture |
|------|----------------------|-----------------|
| Environment configuration | Manual setup | `EnvironmentName` property |
| ContentRoot | Manual setup | Auto-calculated (`GetTestProjectPath`) |
| Extension points | `WithWebHostBuilder` | `ConfigureHost` method |
| Lifecycle | Manual management | `IAsyncLifetime` implementation |

### Q2. How do I use different environments per test?

Define a separate Fixture for each test class:

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

### Q3. How do I inject mock services?

Override `ConfigureHost` to replace services:

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

### Q4. Can the same Fixture be shared across multiple test classes?

Yes, separate the Fixture class into its own file:

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

Using xUnit Collection Fixture, multiple test classes can share a single host instance:

```csharp
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<SharedTestFixture> { }

[Collection("IntegrationTests")]
public class TestA { }

[Collection("IntegrationTests")]
public class TestB { }
```

### Q5. What is the BaseAddress when testing APIs with HttpClient?

The BaseAddress of `HostTestFixture.Client` is the test server's address. Use relative paths for requests:

```csharp
// Correct usage
var response = await _fixture.Client.GetAsync("/api/health");

// Absolute URL unnecessary
// var response = await _fixture.Client.GetAsync("http://localhost/api/health");
```

## References

- [Unit testing guide](../15a-unit-testing) - Test naming conventions, AAA pattern, and other basic test writing rules
- [Testing library](../16-testing-library) - Functorium.Testing library guide
- [Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Class Fixtures](https://xunit.net/docs/shared-context#class-fixture)
