---
title: "Functorium.Testing Library Guide"
---

Test code requires the same level of consistency as production code. As a project grows, repetitive test infrastructure code such as log capture, architecture rule validation, and source generator testing gets duplicated across projects.
`Functorium.Testing` eliminates this repetition by providing framework-specific test utilities in a single library, ensuring consistency and maintainability of test code.

## Introduction

"How do you verify that the structured log fields output by a Pipeline are accurate?"
"How do you apply immutability rules for ValueObjects across all classes at once?"
"How do you test whether a source generator produces correct code?"

Implementing this test infrastructure directly in each project leads to accumulated duplicate code, and synchronization becomes difficult during framework updates. `Functorium.Testing` consolidates these repetitive patterns into a single library, providing a consistent test foundation.

### What You Will Learn

This document covers the following topics:

1. **Structured log testing with `LogTestContext`** - Serilog in-memory capture with Verify snapshot integration
2. **Mock return value configuration using `FinTFactory`** - Generating `FinT<IO, T>` return values for Port/Adapter
3. **Architecture rule validation Fluent API** - ArchUnitNET-based class/method level rule enforcement
4. **Source generator testing with `SourceGeneratorTestRunner`** - Input code to generated code verification
5. **Scheduled Job integration testing with `QuartzTestFixture`** - Single Job execution verification in DI-integrated environment

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [Unit Testing Guide](../15a-unit-testing) - AAA pattern, MTP configuration, Verify snapshot testing
- Basic concepts of LanguageExt's `Fin<T>` and `FinT<IO, T>` types
- Basic principles of Serilog structured logging

> **Core principle:** `Functorium.Testing` consolidates repetitive test infrastructure -- structured log capture, architecture rule validation, source generator testing, mock return value generation -- into a single library to ensure consistency across projects.

## Summary

### Key Commands

```csharp
// Structured log testing
using var context = new LogTestContext();
var logger = context.CreateLogger<MyPipeline>();
// ... after test execution
await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots");

// Architecture rule validation
ArchRuleDefinition.Classes().That()
    .ImplementInterface(typeof(IValueObject))
    .ValidateAllClasses(Architecture, @class => { ... })
    .ThrowIfAnyFailures("Rule Name");

// Source generator testing
string? actual = _sut.Generate(input);
return Verify(actual).UseDirectory("Snapshots/EntityIdGenerator");

// Mock return value configuration
_repository.GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Succ(product));
```

### Key Procedures

**1. Log testing:**
1. Create `LogTestContext`
2. Create ILogger with `CreateLogger<T>()`
3. Inject logger into test target and execute
4. Extract data with `ExtractFirstLogData()`, etc.
5. Compare with `Verify()` snapshot or use direct Assertion

**2. Architecture rule validation:**
1. Filter target classes with `ArchRuleDefinition.Classes()`
2. Pass validation rule callback to `ValidateAllClasses()`
3. Throw exception on failure with `ThrowIfAnyFailures()`

### Key Concepts

| Concept | Description |
|------|------|
| `LogTestContext` | Serilog-based in-memory log capture context |
| `FinTFactory` | `FinT<IO, T>` mock return value generation helper |
| `ClassValidator` | Class-level architecture rule Fluent API |
| `SourceGeneratorTestRunner` | `IIncrementalGenerator` test runner |
| `QuartzTestFixture` | Quartz.NET Job integration test Fixture |

---

## Overview

`Functorium.Testing` is the test utility library for the Functorium framework.

### Namespace Structure

The following table summarizes the library's complete namespace structure and the role of each module.

| Namespace | Role |
|---|---|
| `Functorium.Testing.Arrangements.Logging` | Structured log capture (LogTestContext, StructuredTestLogger) |
| `Functorium.Testing.Arrangements.Loggers` | In-memory Serilog Sink (TestSink) |
| `Functorium.Testing.Arrangements.Effects` | `FinT<IO, T>` return value generation helper (FinTFactory) |
| `Functorium.Testing.Arrangements.Hosting` | HTTP integration test Fixture (HostTestFixture) |
| `Functorium.Testing.Arrangements.ScheduledJobs` | Scheduled Job test Fixture (QuartzTestFixture) |
| `Functorium.Testing.Actions.SourceGenerators` | Source generator test Runner |
| `Functorium.Testing.Assertions.ArchitectureRules` | Architecture rule validation (ClassValidator, MethodValidator, InterfaceValidator) |
| `Functorium.Testing.Assertions.ArchitectureRules.Rules` | Reusable rules (ImmutabilityRule, etc.) |
| `Functorium.Testing.Assertions.ArchitectureRules.Suites` | Domain/Application architecture test suites (DomainArchitectureTestSuite, ApplicationArchitectureTestSuite) |
| `Functorium.Testing.Assertions.Logging` | Log data extraction/conversion utilities (including SerilogTestPropertyValueFactory) |
| `Functorium.Testing.Assertions.Errors` | Error type Assertions (per Domain/Application/Adapter + generic ErrorCode/Exceptional) |

### Features Documented in Other Guides

| Feature | Reference Guide |
|---|---|
| `HostTestFixture<TProgram>` -- HTTP endpoint integration testing | [15b-integration-testing.md](../15b-integration-testing), [01-project-structure.md](../architecture/01-project-structure) |
| `ShouldBeDomainError`, `ShouldBeApplicationError`, etc. error Assertions | [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app), [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) |

---

## Project Reference Setup

### Unit Test csproj Package Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Test framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />

    <!-- Assertion / Mocking -->
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Verify.XunitV3" />

    <!-- Log testing -->
    <PackageReference Include="Serilog" />

    <!-- Source generator testing -->
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

### Source Generator Dual Reference Pattern

When testing source generator projects, **both types of references** are required.

```xml
<!-- 1. Regular reference: for using generator types (classes) in code -->
<ItemGroup>
  <ProjectReference Include="..\..\Src\MyProject.SourceGenerator\MyProject.SourceGenerator.csproj" />
</ItemGroup>

<!-- 2. Analyzer reference: enables the source generator to perform actual code generation -->
<ItemGroup>
  <ProjectReference Include="..\..\Src\MyProject.SourceGenerator\MyProject.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

| Reference Type | Purpose |
|---|---|
| Regular `ProjectReference` | Instantiate generator types like `new EntityIdGenerator()` |
| `OutputItemType="Analyzer"` | Enable code generation via attributes like `[GenerateEntityId]` at build time |

> **Note**: When referencing a Host project in integration tests, add `ExcludeAssets="analyzers"` to prevent Mediator SourceGenerator duplication. For details, see the FAQ in [01-project-structure.md](../architecture/01-project-structure).

### Recommended Using.cs Pattern

```csharp
global using Functorium.Testing.Arrangements.Logging;
global using Functorium.Testing.Assertions.Logging;
global using Functorium.Testing.Actions.SourceGenerators;
global using Functorium.Testing.Assertions.ArchitectureRules;
global using Xunit;
global using Shouldly;
```

---

Once project references are configured, let's examine the core features the library provides one by one.

## FinTFactory (Mock Return Value Helper)

`FinTFactory` is a static helper that conveniently generates `FinT<IO, T>` return values. It is used when setting up mock return values for Port/Adapter.

```csharp
// Namespace
using Functorium.Testing.Arrangements.Effects;
```

### API

| Method | Return Type | Description |
|--------|----------|------|
| `FinTFactory.Succ<T>(T value)` | `FinT<IO, T>` | Creates a `FinT` wrapping a success value |
| `FinTFactory.Fail<T>(Error error)` | `FinT<IO, T>` | Creates a `FinT` wrapping a failure error |

### NSubstitute Usage Example

```csharp
// Port Mock setup — success return
_productRepository
    .GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Succ(product));

// Port Mock setup — failure return
_productRepository
    .GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Fail<Product>(
        AdapterError.For<InMemoryProductRepository>(
            new NotFound(), id.ToString(), "Product not found")));
```

---

## Structured Log Testing

Structured log testing verifies that `LoggerMessage` attribute-based logging outputs the correct field structure.

### Components

```
LogTestContext (test entry point)
├── StructuredTestLogger<T>  ← ILogger<T> implementation (Serilog bridge)
├── TestSink                 ← In-memory Serilog Sink
└── LogEventPropertyExtractor / LogEventPropertyValueConverter  ← Data extraction
```

### LogTestContext

The core context for log testing. Upon creation, it internally configures a Serilog Logger + TestSink, and creates `ILogger<T>` instances via `CreateLogger<T>()`.

```csharp
// Namespace
using Functorium.Testing.Arrangements.Logging;
```

#### Construction

```csharp
// Default (minimum level: Debug)
using var context = new LogTestContext();

// Specify minimum level
using var context = new LogTestContext(LogEventLevel.Information);
```

#### CreateLogger\<T\>()

Creates an `ILogger<T>` instance. All logs recorded with this logger are captured in the context.

```csharp
var logger = context.CreateLogger<MyPipeline>();
```

#### Log Query API

| Method | Description |
|---|---|
| `LogEvents` | Full list of captured LogEvents (IReadOnlyList) |
| `LogCount` | Number of captured logs |
| `GetFirstLog()` | First log (typically the Request log) |
| `GetSecondLog()` | Second log (typically the Response log) |
| `GetLogAt(int index)` | Query log by index |
| `GetLogsByLevel(LogEventLevel level)` | List of logs at a specific level |
| `Clear()` | Delete all captured logs |

#### Data Extraction API

Converts LogEvents to anonymous objects for Verify snapshot testing.

| Method | Description |
|---|---|
| `ExtractFirstLogData()` | Extract first log data as anonymous object |
| `ExtractSecondLogData()` | Extract second log data as anonymous object |
| `ExtractLogDataAt(int index)` | Extract log data at specified index |
| `ExtractAllLogData()` | Extract all log data as a list of anonymous objects |

### StructuredTestLogger\<T\>

Serves as an `ILogger<T>` → Serilog bridge. Correctly handles structured logging generated by `LoggerMessage` attributes.

- Separates `{OriginalFormat}` and attributes from state in `IReadOnlyList<KeyValuePair<string, object?>>` form
- Processes explicit attribute names in `{@Error:Error}` format
- Directly creates `LogEvent` to maintain accurate attribute names

> **Caution**: Create via `LogTestContext.CreateLogger<T>()`. Direct instantiation is not necessary.

### TestSink

An in-memory Serilog `ILogEventSink` implementation. Used internally by `LogTestContext`, and rarely needs to be used directly.

```csharp
// Namespace
using Functorium.Testing.Arrangements.Loggers;
```

### LogEventPropertyExtractor

A utility that recursively extracts attribute values from `LogEvent`.

```csharp
// Namespace
using Functorium.Testing.Assertions.Logging;
```

| Method | Description |
|---|---|
| `ExtractValue(LogEventPropertyValue)` | Recursively extracts ScalarValue, SequenceValue, StructureValue, DictionaryValue |
| `ExtractLogData(LogEvent)` | Single LogEvent → `{ Information, Properties }` anonymous object |
| `ExtractLogData(IEnumerable<LogEvent>)` | Multiple LogEvents → list of anonymous objects |

### SerilogTestPropertyValueFactory

A factory that converts property values to Serilog `LogEventPropertyValue` when manually creating `LogEvent` in test environments. An `ILogEventPropertyValueFactory` implementation that supports major types including string, int, long, double, bool, Exception, and ValueTuple.

```csharp
using Functorium.Testing.Assertions.Logging;

var factory = new SerilogTestPropertyValueFactory();
var value = factory.CreatePropertyValue("test-value");
```

### LogEventPropertyValueConverter

Converts `LogEventPropertyValue` to anonymous objects for Verify snapshots.

| Method | Description |
|---|---|
| `ToAnonymousObject(LogEventPropertyValue)` | StructureValue → Dictionary, SequenceValue → Array, ScalarValue → primitive value |

### LogEventPropertyExtractor Type-Specific Processing Details

`LogEventPropertyExtractor` is a `static class` that handles all major Serilog `LogEventPropertyValue` subtypes via a switch expression in the `ExtractValue(LogEventPropertyValue)` method.

**Processing logic by type:**

| Type | Processing Method | Result |
|------|----------|------|
| `ScalarValue` | `.Value` (`"null"` string if null) | Primitive value (`string`, `int`, `bool`, etc.) |
| `SequenceValue` | `.Elements.Select(ExtractValue).ToList()` | `List<object>` |
| `StructureValue` | `.Properties.ToDictionary(p => p.Name, p => ExtractValue(p.Value))` | `Dictionary<string, object>` |
| `DictionaryValue` | `.Elements.ToDictionary(kvp => kvp.Key.Value?.ToString() ?? "null", kvp => ExtractValue(kvp.Value))` | `Dictionary<string, object>` |
| Other | `HandleUnhandledType()` — Debug.WriteLine then return `.ToString()` | `string` |

**`ExtractLogData(LogEvent)`** — Creates an anonymous object from a single LogEvent:

```csharp
new
{
    Information = logEvent.MessageTemplate.Text,
    Properties = logEvent.Properties.ToDictionary(
        static p => p.Key,
        static p => ExtractValue(p.Value)
    )
}
```

**`ExtractLogData(IEnumerable<LogEvent>)`** — Converts multiple LogEvents using `.Select()`.

> **Note**: Uses static lambdas (`static p =>`) to prevent unnecessary closure allocations.

### LogEventPropertyExtractor Usage Example

Pattern for verifying log fields using direct Assertion instead of snapshot testing:

```csharp
[Fact]
public async Task Pipeline_Should_Log_RequestLayer_And_Handler()
{
    // Arrange
    using var context = new LogTestContext();
    var logger = context.CreateLogger<UsecaseLoggingPipeline<TestRequest, TestResponse>>();
    var pipeline = new UsecaseLoggingPipeline<TestRequest, TestResponse>(logger);

    // Act
    await pipeline.Handle(new TestRequest("Test"), next, CancellationToken.None);

    // Assert - Directly verify attributes of the first log
    var firstLog = context.GetFirstLog();
    var data = LogEventPropertyExtractor.ExtractLogData(firstLog);

    // Verify specific fields in Properties
    var properties = (IDictionary<string, object?>)data.Properties;
    properties["request.layer"].ShouldBe("application");
    properties["request.category.name"].ShouldBe("usecase");
    properties["request.handler.name"].ShouldNotBeNull();
}
```

### Verify Snapshot Integration Pattern

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

    // Assert - Verify the field structure of the first log (Request) via snapshot
    await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots");
}
```

**Core flow:**
1. Create `LogTestContext`
2. Create logger with `CreateLogger<T>()`
3. Inject logger into code under test and execute
4. Extract data with `ExtractFirstLogData()` / `ExtractAllLogData()`, etc.
5. Compare with `Verify()` snapshot

---

Now that we've learned how to set up mock return values, let's learn how to automatically validate architecture rules.

## Architecture Rule Validation

Validates class/method level architecture rules with a Fluent API based on ArchUnitNET.

```csharp
// Namespace
using Functorium.Testing.Assertions.ArchitectureRules;
```

### ArchitectureValidationEntryPoint.ValidateAllClasses()

An extension method for ArchUnitNET's `IObjectProvider<Class>`. Applies validation rules in bulk to a filtered set of classes.

```csharp
public static ValidationResultSummary ValidateAllClasses(
    this IObjectProvider<Class> classes,
    Architecture architecture,
    Action<ClassValidator> validationRule,
    bool verbose = false);
```

### ClassValidator Fluent API

**Visibility:**

| Method | Description |
|---|---|
| `RequirePublic()` | Must be a public class |
| `RequireInternal()` | Must be an internal class |

**Modifiers:**

| Method | Description |
|---|---|
| `RequireSealed()` / `RequireNotSealed()` | sealed requirement |
| `RequireStatic()` / `RequireNotStatic()` | static requirement |
| `RequireAbstract()` / `RequireNotAbstract()` | abstract requirement |

**Naming (inherited from TypeValidator):**

| Method | Description |
|---|---|
| `RequireNameStartsWith(string)` | Name must start with a specific prefix |
| `RequireNameEndsWith(string)` | Name must end with a specific suffix |
| `RequireNameMatching(string)` | Name must match a regex pattern |

**Type/Inheritance:**

| Method | Description |
|---|---|
| `RequireRecord()` / `RequireNotRecord()` | record type requirement |
| `RequireAttribute(string)` | Requires a specific attribute |
| `RequireInherits(Type)` | Requires inheriting a specific base class |
| `RequireImplements(Type)` | Requires implementing a specific interface |
| `RequireImplementsGenericInterface(string)` | Requires implementing a generic interface |
| `RequireNoDependencyOn(string)` | Prohibits dependency on a specific type |

**Constructor/Property/Field:**

| Method | Description |
|---|---|
| `RequireAllPrivateConstructors()` | All constructors must be private |
| `RequirePrivateAnyParameterlessConstructor()` | Requires a parameterless private constructor |
| `RequireNoPublicSetters()` | Prohibits public setters (only get-only allowed) |
| `RequireOnlyPrimitiveProperties(params string[])` | Only primitive type properties allowed (additional allowed types can be specified) |
| `RequireNoInstanceFields(params string[])` | Prohibits instance fields (field types to exclude can be specified) |
| `RequireImmutable()` | Comprehensive immutability validation (6 dimensions) |

**Method/Nested Class:**

| Method | Description |
|---|---|
| `RequireMethod(string, Action<MethodValidator>)` | Validate a method with a specific name |
| `RequireMethodIfExists(string, Action<MethodValidator>)` | Validate if method exists |
| `RequireAllMethods(Action<MethodValidator>)` | Validate all methods |
| `RequireProperty(string)` | Requires a property with a specific name |
| `RequireNestedClass(string, Action<ClassValidator>?)` | Requires nested class + validation |
| `RequireNestedClassIfExists(string, Action<ClassValidator>?)` | Validate if nested class exists |
| `ValidateAndThrow()` | Validate a single class and throw immediately |

#### RequireImmutable() Validation Items

`RequireImmutable()` comprehensively validates ValueObject immutability across 6 dimensions:

1. **Writability validation** -- All non-static members satisfy `IsImmutable()`
2. **Constructor validation** -- All constructors are private (public constructors prohibited)
3. **Property validation** -- Public setters prohibited (get-only allowed)
4. **Field validation** -- Public fields prohibited (all fields must be private)
5. **Mutable collection validation** -- `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, etc. prohibited
6. **State-changing method validation** -- `Set*`, `Update*`, `Add*`, `Remove*`, etc. prohibited

### MethodValidator Fluent API

**Visibility/Modifiers:**

| Method | Description |
|---|---|
| `RequireVisibility(Visibility)` | Requires specific visibility |
| `RequireStatic()` / `RequireNotStatic()` | static requirement |
| `RequireVirtual()` / `RequireNotVirtual()` | virtual requirement |
| `RequireExtensionMethod()` | Must be an extension method |

**Return type:**

| Method | Description |
|---|---|
| `RequireReturnType(Type)` | Return type validation (supports generic type matching) |
| `RequireReturnTypeOfDeclaringClass()` | Must return the declaring class |
| `RequireReturnTypeOfDeclaringTopLevelClass()` | Must return the top-level declaring class |
| `RequireReturnTypeContaining(string)` | Return type name must contain a specific string |

**Parameters:**

| Method | Description |
|---|---|
| `RequireParameterCount(int)` | Exact parameter count |
| `RequireParameterCountAtLeast(int)` | Minimum parameter count |
| `RequireFirstParameterTypeContaining(string)` | First parameter type must contain a specific string |
| `RequireAnyParameterTypeContaining(string)` | Any parameter type must contain a specific string |

### InterfaceValidator

`InterfaceValidator` inherits from `TypeValidator<Interface, InterfaceValidator>` and applies the same Fluent API pattern as `ClassValidator` to interfaces.

### IArchRule\<T\> Interface

An interface that defines reusable architecture rules.

| Type | Description |
|---|---|
| `IArchRule<TType>` | Rule interface. Provides `Description` and `Validate()` method |
| `DelegateArchRule<TType>` | Lambda-based rule implementation |
| `CompositeArchRule<TType>` | Composes multiple rules with AND |
| `ImmutabilityRule` | Class immutability validation rule (detects 14 mutable collection types) |

### Architecture Test Suite

`DomainArchitectureTestSuite` and `ApplicationArchitectureTestSuite` provide pre-defined test sets for Domain/Application layer architecture rules. Simply inherit and specify `Architecture` and namespace to automatically apply 21+4 architecture tests.

#### ArchitectureTestBase Setup

Load target assemblies with `ArchLoader` and define namespaces as shared constants:

```csharp
// Reference: samples/ecommerce-ddd/.../ArchitectureTestBase.cs
using ArchUnitNET.Loader;

internal static class ArchitectureTestBase
{
    internal static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
                ECommerce.Domain.AssemblyReference.Assembly,
                ECommerce.Application.AssemblyReference.Assembly)
            .Build();

    internal static readonly string DomainNamespace =
        typeof(ECommerce.Domain.AssemblyReference).Namespace!;
    internal static readonly string ApplicationNamespace =
        typeof(ECommerce.Application.AssemblyReference).Namespace!;
}
```

#### DomainArchitectureRuleTests

```csharp
// Reference: samples/ecommerce-ddd/.../DomainArchitectureRuleTests.cs
using Functorium.Testing.Assertions.ArchitectureRules.Suites;

public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override ArchUnitNET.Domain.Architecture Architecture => ArchitectureTestBase.Architecture;
    protected override string DomainNamespace => ArchitectureTestBase.DomainNamespace;
}
```

#### ApplicationArchitectureRuleTests

```csharp
// Reference: samples/ecommerce-ddd/.../ApplicationArchitectureRuleTests.cs
using Functorium.Testing.Assertions.ArchitectureRules.Suites;

public sealed class ApplicationArchitectureRuleTests : ApplicationArchitectureTestSuite
{
    protected override ArchUnitNET.Domain.Architecture Architecture => ArchitectureTestBase.Architecture;
    protected override string ApplicationNamespace => ArchitectureTestBase.ApplicationNamespace;
}
```

#### Customization: Overridable Properties

You can adjust the test suite to fit specific domain structures:

```csharp
// Reference: samples/designing-with-types/.../DomainArchitectureRuleTests.cs
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    private static readonly ArchUnitNET.Domain.Architecture s_architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
            DesigningWithTypes.AssemblyReference.Assembly)
        .Build();

    protected override ArchUnitNET.Domain.Architecture Architecture => s_architecture;
    protected override string DomainNamespace =>
        typeof(DesigningWithTypes.AssemblyReference).Namespace!;

    // Union VO does not need Create/Validate factory pattern → exclude from checks
    protected override IReadOnlyList<Type> ValueObjectExcludeFromFactoryMethods =>
        [typeof(UnionValueObject)];

    // Allow DomainService to have Repository fields
    protected override string[] DomainServiceAllowedFieldTypes => ["Repository"];
}
```

| override attribute | Default | Purpose |
|---|---|---|
| `ValueObjectExcludeFromFactoryMethods` | `[]` | VO types to exclude from Create/Validate factory checks. Used for types like Union VO that are created directly without factories |
| `DomainServiceAllowedFieldTypes` | `[]` | Allowed field type list for DomainService. Used for DomainServices that inject Repositories |

**DomainArchitectureTestSuite (21 tests):**
Automatically validates architecture rules for AggregateRoot, Entity, ValueObject, DomainEvent, Specification, and DomainService.

**ApplicationArchitectureTestSuite (4 tests):**
Automatically validates the existence of Command/Query Validator and Usecase nested classes.

### ValidationResultSummary.ThrowIfAnyFailures()

Aggregates validation results from multiple classes and throws `XunitException` if there are any failures.

```csharp
summary.ThrowIfAnyFailures("ValueObject Immutability Rule");
```

Exception message format:
```
'ValueObject Immutability Rule' rule violation:

MyProject.ValueObjects.Email:
  - Class 'Email' must be sealed.
  - Found public constructors: .ctor

MyProject.ValueObjects.PhoneNumber:
  - Method 'Create' in class 'PhoneNumber' must be static.
```

### SingleHost Architecture Test Inventory

The following table is the complete list of architecture tests implemented in the SingleHost reference project.

| Test Class | Test Count | Validation Target |
|--------------|----------|----------|
| `LayerDependencyArchitectureRuleTests` | 6 | Dependency direction between layers (Domain !-> Application, no cross-references between Adapters, etc.) |
| `EntityArchitectureRuleTests` | 5 | AggregateRoot/Entity: public sealed, inheritance, Create/CreateFromValidated factory |
| `ValueObjectArchitectureRuleTests` | 4 | ValueObject: public sealed, immutability, Create/Validate factory |
| `DtoArchitectureRuleTests` | 5 | DTO/Model/Mapper: Persistence Mapper internal static, Usecase nested Request/Response |
| `CqrsArchitectureRuleTests` | 1 | CQRS pattern compliance: Enforces that Query Usecase does not depend on IRepository |
| `UsecaseArchitectureRuleTests` | 4 | Command/Query: Internal Validator/Usecase nested class existence |
| `SpecificationArchitectureRuleTests` | 3 | Specification: public sealed, inheritance, resides in Domain layer |
| `PortAndAdapterArchitectureRuleTests` | 3 | Adapter: GenerateObservablePort attribute, RequestCategory, DomainService sealed |

### Usage Pattern: ValueObject Immutability Validation

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
            // Class-level validation
            @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors()
                .RequireImmutable()
                .RequireImplements(typeof(IEquatable<>));

            // Create method validation
            @class.RequireMethod("Create", method => method
                .RequireVisibility(Visibility.Public)
                .RequireStatic()
                .RequireReturnType(typeof(Fin<>)));

            // Validate method validation
            @class.RequireMethod("Validate", method => method
                .RequireVisibility(Visibility.Public)
                .RequireStatic()
                .RequireReturnType(typeof(Validation<,>)));

            // DomainErrors nested class validation (only if exists)
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

While architecture rules validate class structure, source generator tests validate code generation results.

## Source Generator Testing

`SourceGeneratorTestRunner` runs `IIncrementalGenerator` in a test environment and returns the generated code. `EntityIdGenerator`, `ObservablePortGenerator`, and `UnionTypeGenerator` can all be tested with the same pattern.

```csharp
// Namespace
using Functorium.Testing.Actions.SourceGenerators;
```

### SourceGeneratorTestRunner.Generate\<TGenerator\>()

Takes source code as input, runs the source generator, and returns the generated code string.

```csharp
public static string? Generate<TGenerator>(this TGenerator generator, string sourceCode)
    where TGenerator : IIncrementalGenerator, new();
```

Internally performs the following:
1. Parses input source code into `CSharpSyntaxTree`
2. Automatically adds required assembly references (System.Runtime, LanguageExt.Core, Microsoft.Extensions.Logging)
3. Runs the source generator with `CSharpGeneratorDriver`
4. Fails with Shouldly assertion if there are compiler errors
5. Returns the generated code (`null` if nothing was generated)

### GenerateWithDiagnostics\<TGenerator\>()

Returns diagnostic results along with the generated code. Used for `DiagnosticDescriptor` testing.

```csharp
public static (string? GeneratedCode, ImmutableArray<Diagnostic> Diagnostics)
    GenerateWithDiagnostics<TGenerator>(this TGenerator generator, string sourceCode)
    where TGenerator : IIncrementalGenerator, new();
```

### Verify Snapshot Comparison Pattern

```csharp
[Fact]
public Task EntityIdGenerator_ShouldGenerate_EntityId_ForSimpleEntity()
{
    // Arrange
    string input = """
        using Functorium.Domains.Entities;

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

### Validating Attribute Generation with Empty Input

When a source generator auto-generates marker Attributes, verify with an empty string input:

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

While source generator tests verify static code generation, scheduled Job tests verify actual Job execution at runtime.

## Scheduled Job Integration Testing

A Fixture for integration testing of Quartz.NET Jobs.

```csharp
// Namespace
using Functorium.Testing.Arrangements.ScheduledJobs;
```

### QuartzTestFixture\<TProgram\>

A generic Fixture that reuses the full DI setup using `WebApplicationFactory`.

#### Key Properties

| attribute | Type | Description |
|---|---|---|
| `Services` | `IServiceProvider` | DI container |
| `Scheduler` | `IScheduler` | Quartz scheduler |
| `JobListener` | `JobCompletionListener` | Job completion tracking listener |

#### Environment Configuration

The default environment is `"Test"`. It can be overridden in derived classes.

```csharp
// appsettings.Test.json is loaded automatically
protected virtual string EnvironmentName => "Test";
```

> **Note**: The `appsettings.Test.json` file must be located in the Host project root, and `CopyToOutputDirectory` must be set in the `.csproj`:
> ```xml
> <ItemGroup>
>   <Content Include="appsettings.Test.json" CopyToOutputDirectory="PreserveNewest" />
> </ItemGroup>
> ```
> Since `WebApplicationFactory` loads configuration files based on the Host project's `ContentRootPath`, the file must be in the **Host project**, not the test project.

#### DI Extension Point

Override `ConfigureWebHost` to apply additional settings.

```csharp
public class MyJobTestFixture : QuartzTestFixture<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace with test service
        });
    }
}
```

### ExecuteJobOnceAsync\<TJob\>()

Immediately executes the specified Job once and waits for completion.

```csharp
// Auto-extract name/group from Job type
Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(TimeSpan timeout)
    where TJob : IJob;

// Explicit name/group specification
Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(
    string jobName, string jobGroup, TimeSpan timeout)
    where TJob : IJob;
```

Internal behavior:
1. Call `JobListener.Reset()`
2. Create a test Job with a unique name (`{JobName}-Test-{Guid}`)
3. Schedule immediate one-time execution with `SimpleTrigger`
4. Wait for completion with `JobListener.WaitForJobCompletionAsync()`

### JobCompletionListener

An `IJobListener` implementation that asynchronously tracks Job completion.

| Method | Description |
|---|---|
| `WaitForJobCompletionAsync(jobName, timeout)` | Wait for Job completion (`TimeoutException` on timeout) |
| `Reset()` | Initialize tracking state (called before each test) |

Internally uses `ConcurrentDictionary<string, TaskCompletionSource<JobExecutionResult>>` to track completion in a thread-safe manner.

### JobExecutionResult

A record representing the Job execution result.

| attribute | Type | Description |
|---|---|---|
| `JobName` | `string` | Job name |
| `Success` | `bool` | Whether it succeeded |
| `Result` | `object?` | Job execution result |
| `Exception` | `JobExecutionException?` | Exception that occurred |
| `ExecutionTime` | `TimeSpan` | Execution time |

### Usage Example

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

### Timeout Handling Pattern

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

## Troubleshooting

### Compilation Error in Source Generator Tests

**Cause:** `SourceGeneratorTestRunner.Generate()` internally auto-references only required assemblies (System.Runtime, LanguageExt.Core, Microsoft.Extensions.Logging). A compilation error occurs if the test input code uses types from other assemblies.

**Resolution:** Write the input code for source generator tests within the scope of auto-referenced assemblies. It is sufficient to include only the marker Attribute and target class that the source generator processes.

### Logs Not Captured in `LogTestContext`

**Cause:** The default minimum level of `LogTestContext` is `Debug`. Logs will not be captured if the test target logs at `Verbose` level. Or the type parameter of `CreateLogger<T>()` differs from the actual logging class.

**Resolution:** Specify the minimum level explicitly: `new LogTestContext(LogEventLevel.Verbose)`. Verify that the logger's type parameter matches the `ILogger<T>` of the class under test.

### Unexpected Classes Included in Architecture Rule Validation

**Cause:** The `ArchRuleDefinition.Classes().That()` filter condition is too broad, including unintended classes (abstract classes, test classes, etc.).

**Resolution:** Apply additional filter conditions such as `.And().AreNotAbstract()`, `.And().DoNotHaveNameContaining("Test")` to narrow the target scope. Use the `verbose: true` option to view the list of classes being validated.

---

## FAQ

**Q: What is the difference between `LogTestContext` and `ITestOutputHelper`?**

`LogTestContext` is Serilog-based and captures structured log fields (attribute names, value types, nested structures) enabling snapshot testing. `ITestOutputHelper` only supports simple text output, making it unsuitable for field structure verification.

**Q: Can `ArchitectureRules` be customized?**

Yes. In addition to the built-in rules (`RequireImmutable`, `RequireSealed`, etc.), you can combine project-specific rules in the `Action<ClassValidator>` callback of `ValidateAllClasses`.

**Q: Are actual Jobs executed in `QuartzTestFixture`?**

Jobs are actually executed in an in-memory scheduler. Since all services from the DI container are injected, integration-level verification is possible by replacing only external dependencies (DB, API, etc.) with mocks.

---

## References

- [15a-unit-testing.md](../15a-unit-testing) — Unit test rules (naming, AAA pattern, MTP configuration)
- [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app) — Domain/Application error Assertion patterns
- [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) — Adapter error Assertion and generic error Assertion
- [01-project-structure.md](../architecture/01-project-structure) — Project structure (HostTestFixture, integration testing)
- [08-observability.md](../../spec/08-observability) — Observability specification (log field definitions)
