---
title: "Unit Testing Guide"
---

This document explains the unit test writing rules and patterns for Functorium projects.

## Introduction

"How should test method names be written to read consistently?"
"Why doesn't the existing `--filter` option work in xUnit v3's MTP mode?"
"What rules are needed to ensure multiple developers follow the same AAA pattern?"

Consistency in unit tests is just as important as code quality. As project size grows, if naming conventions, variable conventions, and framework settings are not unified, the test code itself becomes a maintenance burden.

### What You Will Learn

This document covers the following topics:

1. **T1_T2_T3 naming convention** - How to structure test target, expected result, and scenario
2. **AAA pattern and standard variable names** - Consistent variable conventions like `sut`, `actual`, `expected`
3. **MTP mode configuration and CLI options** - Test execution and filtering in xUnit v3
4. **Shouldly-based Assertions** - Validation patterns that provide clear failure messages
5. **[Fact] vs [Theory] selection criteria** - Distinguishing between single-scenario and multi-input tests

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- C# asynchronous programming (`async/await`, `Task`)
- Basic concepts of the xUnit test framework (`[Fact]`, `[Theory]`)
- .NET project build and execution (`dotnet build`, `dotnet test`)

> **Core principle:** Test code requires the same level of consistency as production code. Through the T1_T2_T3 naming convention and the AAA pattern, you can ensure team-wide consistency in both test method names and bodies.

## Summary

### Key Commands (MTP Mode)

```bash
# Run all tests
dotnet test

# Test a specific project
dotnet test --project Tests/Functorium.Tests.Unit

# Run with code coverage (MTP mode)
dotnet test -- --coverage --coverage-output-format cobertura

# Run specific tests only (MTP filter)
dotnet test -- --filter-method "Handle_ReturnsSuccess"

# Class filtering
dotnet test -- --filter-class "MyNamespace.MyTestClass"
```

> **Note**: In MTP mode, test options are passed after the `--` separator.

> **Caution**: In xUnit v3 (MTP mode), VSTest's `--filter` option is not supported. Use `--filter-class`, `--filter-method`, etc. instead.

### Key Procedures

**1. Writing tests:**
```bash
# 1. Create test class (Tests/{Project}.Tests.Unit/{Feature}/)
# 2. Write test method (T1_T2_T3 naming convention)
# 3. Apply AAA pattern (Arrange-Act-Assert)
# 4. Run and verify tests
```

**2. Running tests:**
```bash
# 1. Build
dotnet build

# 2. Run tests
dotnet test

# 3. Check results
```

### Key Concepts

**1. Test Naming Convention (T1_T2_T3)**

| Component | Description | Example |
|---------|------|-----|
| **T1** | Test target method name | `Validate`, `Handle` |
| **T2** | Expected result | `ReturnsSuccess`, `ReturnsFail` |
| **T3** | Test scenario | `WhenTitleIsEmpty` |

**2. AAA Pattern**

| Phase | Variable Name | Description |
|------|--------|------|
| Arrange | `sut`, `request` | Test preparation |
| Act | `actual` | Execution |
| Assert | - | Verification |

**3. Test Packages**

| Package | Purpose |
|--------|------|
| xunit.v3 | Test framework |
| Microsoft.Testing.Extensions.CodeCoverage | Code coverage |
| Microsoft.Testing.Extensions.TrxReport | TRX report |
| Shouldly | Assertion library |
| NSubstitute | Mocking library |
| TngTech.ArchUnitNET.xUnitV3 | Architecture testing |

---

## MTP Configuration

<details>
<summary>MTP Configuration Details (click to expand)</summary>

### What is Microsoft Testing Platform?

MTP (Microsoft Testing Platform) is the new test engine replacing VSTest. xUnit v3 natively supports MTP.

### Activating MTP Mode

To use MTP, both **project settings** and **SDK version-specific settings** are required.

#### Required Project Settings (common across all .NET versions)

The following settings are **required** in the `.csproj` file of every test project:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
</PropertyGroup>
```

| attribute | Description |
|------|------|
| `OutputType` | `Exe` is **required** since MTP mode operates as a standalone executable (required in MTP mode, not needed in VSTest mode) |
| `UseMicrosoftTestingPlatformRunner` | Enables MTP runner in xUnit v3 (xUnit-specific) |

> **Note**: The reason for setting `OutputType` to `Exe` is a [Microsoft official recommendation](https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/) to prevent bugs during MSBuild/NuGet restore.

> **Tip**: Adding common settings to a `Directory.Build.props` file will automatically apply them to all test projects:
> ```xml
> <Project>
>   <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
>     <OutputType>Exe</OutputType>
>     <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
>   </PropertyGroup>
> </Project>
> ```

#### `dotnet test` Configuration by SDK Version

**.NET 10 SDK and above**: Configure in `global.json`

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

> **Location**: `global.json` is located at the solution root.
> ```
> Solution root/
> ├── global.json          ← MTP configuration location
> ├── Directory.Packages.props
> └── Functorium.slnx
> ```

> **Note**: With .NET 10 SDK and above, you can use MTP options directly without the `--` separator.

**.NET 8-9 SDK**: Additional settings required in the project file

```xml
<PropertyGroup>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

> **Caution**: In .NET 8-9, the `--` separator is required when using the `dotnet test` command.

### xUnit v3 MTP Package Selection

xUnit v3 allows you to select the MTP version:

| Package | Description |
|--------|------|
| `xunit.v3` | Default (includes MTP v1) |
| `xunit.v3.mtp-v1` | Explicitly specify MTP v1 |
| `xunit.v3.mtp-v2` | Use MTP v2 |
| `xunit.v3.mtp-off` | Disable MTP (VSTest only) |

### MTP CLI Options (xUnit v3)

| Feature | xUnit Native | MTP Command Line |
|------|---------------|-----------|
| Class filtering | `-class "name"` | `--filter-class "name"` |
| Method filtering | `-method "name"` | `--filter-method "name"` |
| Namespace filtering | `-namespace "name"` | `--filter-namespace "name"` |
| Trait filtering | `-trait "name=value"` | `--filter-trait "name=value"` |
| Parallel processing | `-parallel <option>` | `--parallel <option>` |
| HTML report | `-html <file>` | `--report-xunit-html --report-xunit-html-filename <file>` ¹ |
| JUnit report | `-junit <file>` | `--report-junit --report-junit-filename <file>` ¹ |
| Live output | `-showLiveOutput` | `--show-live-output on` |

> ¹ HTML/JUnit reports require separate package installation: `xunit.v3.reports.html`, `xunit.v3.reports.junit`

### VSTest vs MTP Filter Comparison

| Mode | Filter Option | Example |
|------|----------|------|
| VSTest | `--filter` | `dotnet test --filter "FullyQualifiedName~MyTest"` |
| MTP | `-- --filter-method` | `dotnet test -- --filter-method "MyTest"` |

> **Note**: In VSTest mode, the `--filter` option is used without the `--` separator.

> **Important**: When using the `--filter` option in xUnit v3 (MTP mode), the following error occurs:
> ```
> Unknown option '--filter'
> ```
> In this case, use the `--filter-class` or `--filter-method` options instead.

### Code Coverage Options (MTP)

Available after installing the `Microsoft.Testing.Extensions.CodeCoverage` package:

| Option | Description |
|------|------|
| `--coverage` | Enable code coverage (required) |
| `--coverage-output <file>` | Specify output file name |
| `--coverage-output-format <format>` | Format (coverage, xml, cobertura) |
| `--coverage-settings <file>` | XML settings file path |

**Usage examples:**

```bash
# Collect coverage via dotnet test
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.xml

# Run directly via dotnet run
dotnet run --project Tests -- --coverage --coverage-output-format cobertura
```

### TRX Report Options (MTP)

Available after installing the `Microsoft.Testing.Extensions.TrxReport` package:

| Option | Description |
|------|------|
| `--report-trx` | Generate TRX report |
| `--report-trx-filename <file>` | Specify output file name |

**Usage examples:**

```bash
# Generate TRX report
dotnet test -- --report-trx

# Specify file name
dotnet test -- --report-trx --report-trx-filename results.trx

# Generate both coverage and TRX report (Build-Local.ps1 approach)
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.xml --report-trx
```

</details>




Once MTP settings and packages are ready, let's look at the package configuration needed for test projects.

## Test Packages

| Package | Purpose | Note |
|--------|------|------|
| xunit.v3 | Test framework | xUnit v3 (MTP-based) |
| xunit.runner.visualstudio | VS/IDE test explorer support | Required |
| Microsoft.NET.Test.Sdk | .NET test SDK | Required |
| Microsoft.Testing.Extensions.CodeCoverage | Code coverage collection | MTP extension |
| Microsoft.Testing.Extensions.TrxReport | TRX report generation | MTP extension |
| Shouldly | Fluent Assertion | Recommended |
| Verify.XunitV3 | Snapshot testing | For xUnit v3 |
| NSubstitute | Mocking | Recommended |
| TngTech.ArchUnitNET.xUnitV3 | Architecture testing | For xUnit v3 |

### Package Installation

```bash
# xUnit v3 (test framework) - required packages
dotnet add package xunit.v3
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk

# MTP extensions (code coverage, TRX report)
dotnet add package Microsoft.Testing.Extensions.CodeCoverage
dotnet add package Microsoft.Testing.Extensions.TrxReport

# Shouldly (Assertion)
dotnet add package Shouldly

# NSubstitute (Mocking)
dotnet add package NSubstitute
```

> **Caution**: Without the `Microsoft.Testing.Extensions.TrxReport` package, tests will not run when executing `Build-Local.ps1` due to the `--report-trx` option.




Once packages are ready, configure the test project's csproj file.

## Test Project Setup

### Basic csproj Configuration

Review the required MTP settings (`OutputType`, `UseMicrosoftTestingPlatformRunner`) and package reference configuration in the following csproj example.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <!-- MTP required settings (details: see MTP Configuration section) -->
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <!-- Required packages -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />

    <!-- MTP extensions (coverage, TRX report) -->
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />

    <!-- Assertion library -->
    <PackageReference Include="Shouldly" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyProject\MyProject.csproj" />
  </ItemGroup>

</Project>
```

> **Important**: `OutputType` and `UseMicrosoftTestingPlatformRunner` are required settings for MTP operation. For the role of each attribute and additional SDK version-specific settings, see the [MTP Configuration](#mtp-configuration) section.

### xunit.runner.json Configuration

Create an `xunit.runner.json` file at the test project root:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true
}
```

### xUnit v3 Namespace Changes

When migrating from xUnit v2 to v3, the following namespace changes are required:

| v2 | v3 |
|----|-----|
| `Xunit.Abstractions` | `Xunit` |
| `ITestOutputHelper` (Xunit.Abstractions) | `ITestOutputHelper` (Xunit) |

```csharp
// xUnit v2
using Xunit.Abstractions;

// xUnit v3
using Xunit;
```

### Using xUnit Types in Non-Test Libraries

When you need to use xUnit types like `ITestOutputHelper` in a test utility library (e.g., `Functorium.Testing`):

```xml
<!-- Use xunit.v3.extensibility.core instead of xunit.v3 -->
<PackageReference Include="xunit.v3.extensibility.core" />
```

> **Caution**: The `xunit.v3` package should only be used in test projects (`<IsTestProject>true</IsTestProject>`). Using it in non-test libraries will cause a "test projects must be executable" error.




Once project setup is complete, let's look at the naming conventions that are central to writing test code.

## Test Naming Convention

Test method names are written in the **T1_T2_T3** format.

### Format

```
{T1}_{T2}_{T3}
```

| Component | Description | Example |
|---------|------|-----|
| **T1** | Test target method name | `Validate`, `Handle`, `Execute` |
| **T2** | Expected result | `ReturnsSuccess`, `ReturnsFail`, `ThrowsException` |
| **T3** | Test scenario/condition | `WhenTitleIsEmpty`, `WhenInputIsValid` |

### Validator Test Naming Examples

```csharp
// Return validation errors
Validate_ReturnsValidationError_WhenTitleIsEmpty
Validate_ReturnsValidationError_WhenTitleExceedsMaxLength
Validate_ReturnsValidationError_WhenTemperatureCIsBelowMinimum
Validate_ReturnsValidationError_WhenTemperatureCIsAboveMaximum

// Validation passes
Validate_ReturnsNoError_WhenRequestIsValid
Validate_ReturnsNoError_WhenTemperatureCIsWithinRange
Validate_ReturnsNoError_WhenTitleIsAtMaxLength
```

### Usecase Test Naming Examples

```csharp
// Success scenarios
Handle_ReturnsSuccess_WhenTemperatureCIsPositive
Handle_ReturnsSuccess_WhenTemperatureCIsZero
Handle_ReturnsSuccess_WhenRequestIsValid

// Failure scenarios
Handle_ReturnsFail_WhenTemperatureCIsNegative
Handle_ReturnsFail_WhenEntityNotFound

// Return value verification
Handle_ReturnsTemperatureCBasedOnTitleLength_WhenSuccessful
Handle_ReturnsTemperatureCEqualToTitleLength_WhenSuccessful
```

### [Fact] vs [Theory] Selection Criteria

| Scenario | Attribute | Data Source | Example |
|----------|-----------|------------|------|
| Single scenario verification | `[Fact]` | None | Creation success, specific business rule |
| Same logic, multiple inputs | `[Theory]` + `[InlineData]` | Inline values | Boundary values, various valid/invalid inputs |
| Complex object inputs | `[Theory]` + `[MemberData]` | Static method/attribute | VO combinations, Entity state combinations |

### T2 (Expected Result) Standard Terms

| Term | When to Use |
|------|----------|
| `ReturnsSuccess` | Returns a success result |
| `ReturnsFail` | Returns a failure result |
| `ReturnsValidationError` | Validation error |
| `ReturnsNoError` | No error |
| `ThrowsException` | Exception thrown |
| `Returns{Value}` | Returns a specific value |

### T3 (Scenario) Standard Prefixes

| Prefix | When to Use | Example |
|--------|----------|------|
| `When` | Condition/situation | `WhenInputIsNull` |
| `Given` | Precondition | `GivenUserIsAuthenticated` |
| `With` | Specific value | `WithValidInput` |




If the naming convention determines the "name" of a test method, then variable naming conventions determine the consistency of the test method "body."

## Variable Naming Convention

### Standard Variable Names

| Variable Name | Purpose | AAA Phase |
|--------|------|----------|
| `sut` | System Under Test | Arrange |
| `request` | Request object | Arrange |
| `actual` | Execution result | Act |
| `expected` | Expected result (for comparison) | Assert |

### Usage Example

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenRequestIsValid()
{
    // Arrange
    var sut = new MyUsecase();
    var request = new MyRequest(Title: "Valid");

    // Act
    var actual = await sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```




## AAA Pattern

All tests follow the **Arrange-Act-Assert** pattern.

### Structure

Note that each phase is clearly separated by `// Arrange`, `// Act`, `// Assert` comments.

```csharp
[Fact]
public async Task T1_T2_T3()
{
    // Arrange - Test preparation
    var sut = new TestTarget();
    var request = new Request(...);

    // Act - Execution
    var actual = await sut.Method(request);

    // Assert - Verification
    actual.ShouldBe(expected);
}
```

### Complete Example

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenTemperatureCIsPositive()
{
    // Arrange
    var sut = new UpdateWeatherForecastCommand.Usecase();
    var request = new UpdateWeatherForecastCommand.Request(
        Title: "Valid Title",
        Description: "Valid description",
        TemperatureC: 25);

    // Act
    var actual = await sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

### Shouldly Assertion Examples

```csharp
// Value comparison
actual.ShouldBe(expected);
actual.ShouldNotBe(unexpected);

// Boolean
actual.IsSucc.ShouldBeTrue();
actual.IsFail.ShouldBeFalse();

// Null checks
actual.ShouldBeNull();
actual.ShouldNotBeNull();

// Collections
list.ShouldBeEmpty();
list.ShouldContain(item);
list.Count.ShouldBe(3);

// Exceptions
Should.Throw<ArgumentException>(() => sut.Method());
```




## Troubleshooting

### When Tests Are Not Discovered

**Cause**: xUnit package version mismatch or Test SDK missing

**Resolution:**
```bash
# Check packages
dotnet list package

# Install required packages
dotnet add package xunit.v3
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.runner.visualstudio
```

### When Some Tests Are Not Executed in Build-Local.ps1

**Cause**: Missing `Microsoft.Testing.Extensions.TrxReport` package

**Symptom:**
- All tests pass when running directly with `dotnet test`
- Only some tests run with "Error: N" message when executing `Build-Local.ps1`

**Resolution:**
```bash
# Add TrxReport package
dotnet add package Microsoft.Testing.Extensions.TrxReport
```

Or add directly to the csproj file:
```xml
<PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
```

### "test projects must be executable" Error

**Cause**: Using the `xunit.v3` package in a non-test library

**Resolution:** Use `xunit.v3.extensibility.core` instead of `xunit.v3`:
```xml
<!-- Incorrect setting (non-test library) -->
<PackageReference Include="xunit.v3" />

<!-- Correct setting (non-test library) -->
<PackageReference Include="xunit.v3.extensibility.core" />
```

### ITestOutputHelper Namespace Error (xUnit v3)

**Cause**: Namespace change when migrating from xUnit v2 to v3

**Resolution:**
```csharp
// Before (v2)
using Xunit.Abstractions;

// After (v3)
using Xunit;
```

### "Unknown option '--filter'" Error

**Cause**: VSTest's `--filter` option is not supported in xUnit v3 (MTP mode)

**Symptom:**
```
Unknown option '--filter'
```

**Resolution:**

Use the following filter options in MTP mode:

| VSTest Option | MTP Alternative | Example |
|-------------|--------------|------|
| `--filter "FullyQualifiedName~MyTest"` | `--filter-method "*MyTest*"` | Method name filter |
| `--filter "ClassName~MyClass"` | `--filter-class "*MyClass*"` | Class name filter |
| `--filter "Namespace~MyNamespace"` | `--filter-namespace "*MyNamespace*"` | Namespace filter |

```bash
# Incorrect usage (not supported in MTP)
dotnet test --filter "FullyQualifiedName~DomainEventPublisherTests"

# Correct usage (MTP)
dotnet test --filter-class "*DomainEventPublisherTests"
dotnet test --filter-method "*ReturnsSuccess*"
```

> **Note**: The `--` separator is optional in .NET 10 SDK and above. In .NET 8-9, you must use the format `dotnet test -- --filter-class "..."`.

### When Async Tests Fail

**Cause**: Using `async void` or missing `await`

**Resolution:**
```csharp
// Incorrect example
[Fact]
public async void Handle_ReturnsSuccess_WhenValid()  // Using async void
{
    var actual = sut.Handle(request);  // Missing await
}

// Correct example
[Fact]
public async Task Handle_ReturnsSuccess_WhenValid()  // Using async Task
{
    var actual = await sut.Handle(request);  // Using await
}
```

### When Shouldly Assertion Messages Are Unclear

**Cause**: Using default Assert

**Resolution:**
```csharp
// Unclear message
Assert.True(actual.IsSucc);  // "Expected: True, Actual: False"

// Clear message (Shouldly)
actual.IsSucc.ShouldBeTrue();  // "actual.IsSucc should be True but was False"
```

### When Mock Objects Don't Behave as Expected

**Cause**: Missing NSubstitute setup

**Resolution:**
```csharp
// Mock setup
var repository = Substitute.For<IRepository>();
repository.GetById(Arg.Any<int>()).Returns(expectedEntity);

// Call verification
repository.Received(1).GetById(42);
```




## FAQ

### Q1. What if the test method name becomes too long?

**A:** Maintain the T1_T2_T3 format, but write each part concisely:

```csharp
// Too long
Handle_ReturnsValidationErrorWithDetailedMessage_WhenUserInputTemperatureCelsiusValueIsNegativeNumber

// Appropriate name
Handle_ReturnsValidationError_WhenTemperatureCIsNegative
```

### Q2. How do you test multiple conditions?

**A:** Use `[Theory]` and `[InlineData]`:

```csharp
[Theory]
[InlineData(-10)]
[InlineData(-1)]
[InlineData(int.MinValue)]
public void Validate_ReturnsFail_WhenTemperatureCIsNegative(int temperature)
{
    var request = new Request(TemperatureC: temperature);
    var actual = sut.Validate(request);
    actual.IsFail.ShouldBeTrue();
}
```

### Q2-1. How do you use complex objects as inputs?

**A:** Use `[Theory]` and `[MemberData]`. Since `[InlineData]` only supports primitive types, VO combinations and complex objects are provided from static methods via `[MemberData]`:

```csharp
[Theory]
[MemberData(nameof(InvalidRequests))]
public void Validate_ReturnsValidationError_WhenRequestIsInvalid(
    CreateProductCommand.Request request,
    string expectedErrorCode)
{
    // Arrange
    var sut = new CreateProductCommand.Validator();

    // Act
    var actual = sut.Validate(request);

    // Assert
    actual.IsFail.ShouldBeTrue();
    actual.FailToSeq().Head.Code.Value.ShouldBe(expectedErrorCode);
}

public static IEnumerable<object[]> InvalidRequests()
{
    yield return new object[]
    {
        new CreateProductCommand.Request(Name: "", Price: 100m),
        "Validation.NameRequired"
    };
    yield return new object[]
    {
        new CreateProductCommand.Request(Name: "Valid", Price: -1m),
        "Validation.PriceNegative"
    };
}
```

> **Tip**: The data source method for `[MemberData]` must be `public static` and return `IEnumerable<object[]>`.

### Q3. How should test classes be organized?

**A:** Create one test class per class under test:

```
Tests/Functorium.Tests.Unit/
├── Features/
│   └── WeatherForecast/
│       ├── UpdateWeatherForecastCommandTests.cs
│       └── GetWeatherForecastQueryTests.cs
└── Common/
    └── ValidationTests.cs
```

### Q4. How do you test private methods?

**A:** Do not test private methods directly. Test them indirectly through public methods. If you need to test a private method directly, reconsider the design.

### Q5. How do you test code with external dependencies (DB, API)?

**A:** Create mock objects using NSubstitute:

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenEntityExists()
{
    // Arrange
    var repository = Substitute.For<IRepository>();
    repository.GetByIdAsync(42).Returns(new Entity { Id = 42 });

    var sut = new MyUsecase(repository);

    // Act
    var actual = await sut.Handle(new Request(Id: 42));

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

### Q6. How do you handle tests that depend on execution order?

**A:** Tests should be independent. Set up the required state directly in each test so they don't depend on execution order:

```csharp
// Bad Example - depends on other tests
[Fact]
public void Test2_DependsOnTest1()
{
    // Depends on state set by Test1
}

// Good Example - independent
[Fact]
public void Test2_IsIndependent()
{
    // Arrange - Set up all required state directly
    var sut = CreateSut();
    SetupRequiredState();

    // Act & Assert
}
```

### Q7. How do you check code coverage?

**A:** Use the MTP code coverage extension:

```bash
# MTP coverage collection (recommended)
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.xml

# Generate report (requires ReportGenerator)
reportgenerator -reports:"**/coverage.xml" -targetdir:"coverage-report"
```

Or run the `Build-Local.ps1` script, which automatically generates a coverage report.

> **Note**: The VSTest approach (`--collect:"XPlat Code Coverage"`) still works, but the MTP approach is recommended.

### Coverage Exclusion Settings (coverlet.runsettings)

If there are targets to exclude from coverage, such as generated code or migrations, place a `coverlet.runsettings` file at the solution root:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <ExcludeByAttribute>
            GeneratedCodeAttribute,CompilerGeneratedAttribute
          </ExcludeByAttribute>
          <ExcludeByFile>
            **/Migrations/*.cs,**/*.g.cs,**/*.Designer.cs
          </ExcludeByFile>
          <Exclude>
            [*.Tests.*]*,[*]*.Migrations.*
          </Exclude>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

```bash
# Collect coverage with specified runsettings file
dotnet test -- --coverage --coverage-settings coverlet.runsettings
```

## Appendix: xUnit v3

### Theory Data Patterns

#### InlineData

Passes constant values directly. Only supports primitive types (int, string, bool, etc.).

```csharp
[Theory]
[InlineData(1, 2, 3)]
[InlineData(-5, 5, 0)]
public void Add_ReturnsSum(int a, int b, int expected)
{
    Assert.Equal(expected, new Calculator().Add(a, b));
}
```

#### MemberData

Retrieves data from static methods or attributes. Allows passing complex objects.

```csharp
public static IEnumerable<object[]> AddTestData =>
[
    [1, 2, 3],
    [10, 20, 30],
];

[Theory]
[MemberData(nameof(AddTestData))]
public void Add_WithMemberData_ReturnsExpected(int a, int b, int expected)
{
    Assert.Equal(expected, new Calculator().Add(a, b));
}
```

> **Note**: The data source for `[MemberData]` must be `public static` and return `IEnumerable<object[]>`.

#### ClassData

Provides data from a separate class.

```csharp
public class AddTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [1, 2, 3];
        yield return [-5, 5, 0];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(AddTestData))]
public void Add_WithClassData_ReturnsExpected(int a, int b, int expected) { }
```

#### TheoryData (Strongly-Typed)

A strongly-typed data source that provides type safety.

```csharp
public static TheoryData<int, int, int> AddTestData => new()
{
    { 1, 2, 3 },
    { -5, 5, 0 }
};

[Theory]
[MemberData(nameof(AddTestData))]
public void Add_ReturnsExpected(int a, int b, int expected) { }
```

#### TheoryDataRow (v3 Row Metadata)

In v3, you can specify metadata such as Skip, Timeout, and DisplayName on individual rows.

```csharp
public static IEnumerable<ITheoryDataRow> GetTestData()
{
    yield return new TheoryDataRow<int, int, int>(1, 2, 3);
    yield return new TheoryDataRow<int, int, int>(10, 20, 30)
    {
        Skip = "Not yet implemented"
    };
    yield return new TheoryDataRow<int, int, int>(-5, 5, 0)
    {
        Timeout = 1000,
        DisplayName = "Negative number test"
    };
}
```

#### MatrixTheoryData (v3 Combinations)

Automatically generates combinations of multiple data sets.

```csharp
public static MatrixTheoryData<int, string> MatrixData => new(
    [1, 2, 3],
    ["A", "B", "C"]
);
// Result: (1,A), (1,B), (1,C), (2,A), (2,B), (2,C), (3,A), (3,B), (3,C)

[Theory]
[MemberData(nameof(MatrixData))]
public void Matrix_AllCombinations(int number, string letter) { }
```

### v3 New Features

#### Dynamic Test Skipping

```csharp
[Fact]
public void Test_SkipOnCondition()
{
    Assert.SkipWhen(!OperatingSystem.IsWindows(), "Only runs on Windows");
    Assert.SkipUnless(OperatingSystem.IsWindows(), "Only runs on Windows");
    Assert.Skip("Not yet implemented");
}
```

#### Explicit Tests

Only runs when explicitly requested by the user.

```csharp
[Fact(Explicit = true)]
public void ManualIntegrationTest() { }
```

#### TestContext

Accesses context information during test execution.

```csharp
[Fact]
public async Task Test_WithContext()
{
    var context = TestContext.Current;
    var cancellationToken = context.CancellationToken;
    context.SendDiagnosticMessage("Test started");
}
```

#### ITestContextAccessor (Dependency Injection)

```csharp
public class MyTests(ITestContextAccessor contextAccessor)
{
    [Fact]
    public void Test_WithInjectedContext()
    {
        var context = contextAccessor.Current;
        context.SendDiagnosticMessage("Dependency-injected context");
    }
}
```

#### Assembly Fixture

Manages shared resources at the assembly level.

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = "";
    public async ValueTask InitializeAsync() { /* Initialize */ }
    public async ValueTask DisposeAsync() { /* Cleanup */ }
}

[assembly: AssemblyFixture(typeof(DatabaseFixture))]

public class DatabaseTests(DatabaseFixture fixture)
{
    [Fact]
    public void Test() => Assert.NotEmpty(fixture.ConnectionString);
}
```

#### Console/Trace Output Capture

```csharp
[assembly: CaptureConsole]
[assembly: CaptureTrace]
```

### xunit.runner.json Key Settings

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "diagnosticMessages": true,
  "longRunningTestSeconds": 60,
  "methodDisplay": "classAndMethod",
  "methodDisplayOptions": "replaceUnderscoreWithSpace"
}
```

| Option | Default | Description |
|------|--------|------|
| `parallelizeAssembly` | false | Parallel execution across assemblies |
| `parallelizeTestCollections` | true | Parallel execution across collections |
| `maxParallelThreads` | CPU count | Maximum parallel thread count |
| `longRunningTestSeconds` | 0 | Long-running test detection (0=disabled) |
| `methodDisplayOptions` | none | `replaceUnderscoreWithSpace`, `useOperatorMonikers`, `all` |
| `preEnumerateTheories` | true | Pre-enumerate Theory data |
| `failSkips` | false | Treat skipped tests as failures |

---

## References

### Functorium.Testing Library
- Structured log testing (LogTestContext), architecture rule validation, source generator testing, scheduled Job testing:
  [16-testing-library.md](./16-testing-library)
- Error type Assertions (ShouldBeDomainError, etc.):
  [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app), [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing)

### xUnit v3
- [xUnit.net v3 What's New](https://xunit.net/docs/getting-started/v3/whats-new)
- [xUnit.net v3 Migration Guide](https://xunit.net/docs/getting-started/v3/migration)
- [xUnit.net v3 Microsoft Testing Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [xUnit.net v3 Code Coverage with MTP](https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp)

### Microsoft Testing Platform
- [Microsoft Testing Platform Overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro)
- [Testing with dotnet test](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
- [dotnet test Command Reference](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)

### Other Libraries
- [Shouldly Documentation](https://docs.shouldly.org/)
- [NSubstitute Documentation](https://nsubstitute.github.io/help/getting-started/)
- [ArchUnitNET Documentation](https://archunitnet.readthedocs.io/)
