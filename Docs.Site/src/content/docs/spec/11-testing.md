---
title: "Testing Library Specification"
---

This is the public API specification for the test utility library provided by the Functorium framework. For design principles and usage patterns, see the [Functorium.Testing Library Guide](../guides/testing/16-testing-library).

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `FinTFactory` | `Arrangements.Effects` | `FinT<IO, T>` Mock return value generation helper |
| `HostTestFixture<TProgram>` | `Arrangements.Hosting` | Host integration test Fixture |
| `QuartzTestFixture<TProgram>` | `Arrangements.ScheduledJobs` | Quartz Job integration test Fixture |
| `LogTestContext` | `Arrangements.Logging` | Serilog-based in-memory log capture context |
| `StructuredTestLogger<T>` | `Arrangements.Logging` | Structured logging support test Logger |
| `SourceGeneratorTestRunner` | `Actions.SourceGenerators` | `IIncrementalGenerator` test runner |
| `DomainErrorAssertions` | `Assertions.Errors` | Domain error verification extension methods |
| `ApplicationErrorAssertions` | `Assertions.Errors` | Application error verification extension methods |
| `AdapterErrorAssertions` | `Assertions.Errors` | Adapter error verification extension methods |
| `ErrorCodeAssertions` | `Assertions.Errors` | General-purpose error code verification extension methods |
| `ErrorCodeExceptionalAssertions` | `Assertions.Errors` | Exception-based error verification extension methods |
| `ClassValidator` | `Assertions.ArchitectureRules` | Class-level architecture rules Fluent API |
| `MethodValidator` | `Assertions.ArchitectureRules` | Method-level architecture rules Fluent API |
| `InterfaceValidator` | `Assertions.ArchitectureRules` | Interface-level architecture rules Fluent API |
| `DomainArchitectureTestSuite` | `Assertions.ArchitectureRules.Suites` | Domain layer architecture test suite |
| `ApplicationArchitectureTestSuite` | `Assertions.ArchitectureRules.Suites` | Application layer architecture test suite |

> All namespaces have the `Functorium.Testing` prefix.

---

## FinTFactory (Mock 반환값)

A static helper for generating `FinT<IO, T>` return values of Port/Adapter in Application layer Usecase tests.

```csharp
public static class FinTFactory
{
    public static FinT<IO, T> Succ<T>(T value);
    public static FinT<IO, T> Fail<T>(Error error);
}
```

| Method | Return Type | Description |
|--------|-----------|------|
| `Succ<T>(T value)` | `FinT<IO, T>` | Returns `FinT` wrapping the success value |
| `Fail<T>(Error error)` | `FinT<IO, T>` | Returns `FinT` wrapping the failure error |

---

## Host Testing (HostTestFixture\<T\>, QuartzTestFixture\<T\>)

### HostTestFixture\<TProgram\>

A host integration test Fixture that reuses the full DI configuration using `WebApplicationFactory`. The default environment is `"Test"`, and `appsettings.Test.json` is automatically loaded.

```csharp
public class HostTestFixture<TProgram> : IAsyncLifetime where TProgram : class
{
    protected virtual string EnvironmentName => "Test";
    public IServiceProvider Services { get; }
    public HttpClient Client { get; }
    protected virtual string GetTestProjectPath();
    protected virtual void ConfigureHost(IWebHostBuilder builder);
}
```

| Member | Type | Description |
|------|------|------|
| `EnvironmentName` | `string` (virtual) | Environment name to use (default: `"Test"`) |
| `Services` | `IServiceProvider` | DI container access |
| `Client` | `HttpClient` | HTTP client for testing |
| `GetTestProjectPath()` | `string` (virtual) | Test project path (default: 3 levels up from `bin/`) |
| `ConfigureHost(builder)` | `void` (virtual) | Host additional configuration extension point |

**Configuration file load order:** TProgram project's `appsettings.json` (default) -> Test project's `appsettings.json` (overrides)

### QuartzTestFixture\<TProgram\>

A Fixture for Quartz.NET Job integration tests. Has the same environment/configuration structure as `HostTestFixture`, with additional scheduler and Job listener management.

```csharp
public class QuartzTestFixture<TProgram> : IAsyncLifetime where TProgram : class
{
    protected virtual string EnvironmentName => "Test";
    public IServiceProvider Services { get; }
    public JobCompletionListener JobListener { get; }
    public IScheduler Scheduler { get; }
    protected virtual void ConfigureWebHost(IWebHostBuilder builder);

    public Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(TimeSpan timeout)
        where TJob : IJob;
    public Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(
        string jobName, string jobGroup, TimeSpan timeout) where TJob : IJob;
}
```

| Member | Description |
|------|------|
| `JobListener` | Job completion detection listener (`JobCompletionListener`) |
| `Scheduler` | Quartz scheduler instance |
| `ExecuteJobOnceAsync<TJob>(timeout)` | Execute Job once immediately and wait for completion (name/group auto-extracted) |
| `ExecuteJobOnceAsync<TJob>(name, group, timeout)` | Execute Job once immediately and wait for completion (name/group explicit) |

### JobExecutionResult

```csharp
public sealed record JobExecutionResult(
    string JobName, bool Success, object? Result,
    JobExecutionException? Exception, TimeSpan ExecutionTime);
```

`Success` is `true` when completed without exceptions.

---

## Log Testing (LogTestContext, StructuredTestLogger)

### LogTestContext

A context class for Serilog-based in-memory log capture and verification. Implements `IDisposable`.

```csharp
public sealed class LogTestContext : IDisposable
{
    public LogTestContext();
    public LogTestContext(LogEventLevel minimumLevel);
    public LogTestContext(LogEventLevel minimumLevel, bool enrichFromLogContext);

    public IReadOnlyList<LogEvent> LogEvents { get; }
    public int LogCount { get; }
    public ILogger<T> CreateLogger<T>();

    public LogEvent GetFirstLog();
    public LogEvent GetSecondLog();
    public LogEvent GetLogAt(int index);
    public IReadOnlyList<LogEvent> GetLogsByLevel(LogEventLevel level);

    public object ExtractFirstLogData();
    public object ExtractSecondLogData();
    public object ExtractLogDataAt(int index);
    public IEnumerable<object> ExtractAllLogData();
    public void Clear();
}
```

**Constructors:**

| Constructor | Description |
|--------|------|
| `LogTestContext()` | Initialize with default minimum level (`Debug`) |
| `LogTestContext(minimumLevel)` | Initialize with specified minimum log level |
| `LogTestContext(minimumLevel, enrichFromLogContext)` | Minimum level + LogContext enrichment option. Set to `true` to capture `ctx.*` fields |

**Key methods:**

| Method | Return Type | Description |
|--------|-----------|------|
| `CreateLogger<T>()` | `ILogger<T>` | Create structured test Logger |
| `GetFirstLog()` / `GetSecondLog()` | `LogEvent` | First/second log event |
| `GetLogAt(index)` | `LogEvent` | Log event at specified index |
| `GetLogsByLevel(level)` | `IReadOnlyList<LogEvent>` | All log events at specified level |
| `ExtractFirstLogData()` / `ExtractSecondLogData()` | `object` | Extract anonymous object for Verify snapshot |
| `ExtractAllLogData()` | `IEnumerable<object>` | Extract all logs as anonymous object list |

### StructuredTestLogger\<T\>

An `ILogger<T>` implementation that correctly handles structured logging of methods generated by `LoggerMessage` attributes. When `state` is in `IReadOnlyList<KeyValuePair<string, object?>>` form, it parses `OriginalFormat` and property names to directly create Serilog `LogEvent`.

```csharp
public class StructuredTestLogger<T> : ILogger<T>
{
    public StructuredTestLogger(Serilog.ILogger serilogLogger);
    public bool IsEnabled(LogLevel logLevel); // Always true
}
```

---

## Source Generator Testing (SourceGeneratorTestRunner)

A static utility that runs `IIncrementalGenerator` in a test environment and returns the results.

```csharp
public static class SourceGeneratorTestRunner
{
    public static string? Generate<TGenerator>(
        this TGenerator generator, string sourceCode)
        where TGenerator : IIncrementalGenerator, new();

    public static (string? GeneratedCode, ImmutableArray<Diagnostic> Diagnostics)
        GenerateWithDiagnostics<TGenerator>(
            this TGenerator generator, string sourceCode)
        where TGenerator : IIncrementalGenerator, new();
}
```

| Method | Description |
|--------|------|
| `Generate` | Returns generated code. Fails with Shouldly on compiler error |
| `GenerateWithDiagnostics` | Returns generated code + diagnostic results. For `DiagnosticDescriptor` testing |

**Required reference assemblies:** `System.Runtime`, `LanguageExt.Core`, `Microsoft.Extensions.Logging` + all currently loaded non-dynamic assemblies

---

## Error Assertions (5 types)

Type-safe verification extension methods for per-layer error types. All assertions work with `Error`, `Fin<T>`, and `Validation<Error, T>`.

### Per-Layer Assertions (3 types)

`DomainErrorAssertions`, `ApplicationErrorAssertions`, `AdapterErrorAssertions` verify per-layer errors using the same pattern. Only the `TContext` type parameter and error code prefix differ between each class.

| Class | `TContext` Meaning | Error Code Format |
|--------|----------------|---------------|
| `DomainErrorAssertions` | `TDomain` (Domain type) | `DomainErrors.{Name}.{ErrorName}` |
| `ApplicationErrorAssertions` | `TUsecase` (Usecase type) | `ApplicationErrors.{Name}.{ErrorName}` |
| `AdapterErrorAssertions` | `TAdapter` (Adapter type) | `AdapterErrors.{Name}.{ErrorName}` |

**Common method pattern (based on Domain):**

```csharp
// Error verification (0-3 current value overloads)
error.ShouldBeDomainError<TDomain>(errorType);
error.ShouldBeDomainError<TDomain, TValue>(errorType, currentValue);

// Fin<T> verification
fin.ShouldBeDomainError<TDomain, T>(errorType);
fin.ShouldBeDomainError<TDomain, T, TValue>(errorType, currentValue);

// Validation<Error, T> verification
validation.ShouldHaveDomainError<TDomain, T>(errorType);
validation.ShouldHaveOnlyDomainError<TDomain, T>(errorType);
validation.ShouldHaveDomainErrors<TDomain, T>(errorType1, errorType2, ...);
validation.ShouldHaveDomainError<TDomain, T, TValue>(errorType, currentValue);
```

**Additional methods in `AdapterErrorAssertions`** (exception-wrapping error verification):

```csharp
error.ShouldBeAdapterExceptionalError<TAdapter>(errorType);
error.ShouldBeAdapterExceptionalError<TAdapter, TException>(errorType);
fin.ShouldBeAdapterExceptionalError<TAdapter, T>(errorType);
```

### ErrorCodeAssertions (General-purpose)

General-purpose error code verification independent of `DomainErrorType`, etc.

```csharp
public static class ErrorCodeAssertions
{
    // Error state verification
    public static IHasErrorCode ShouldHaveErrorCode(this Error error);
    public static void ShouldBeExpected(this Error error);
    public static void ShouldBeExceptional(this Error error);

    // ErrorCode matching
    public static void ShouldHaveErrorCode(this Error error, string expectedErrorCode);
    public static void ShouldHaveErrorCodeStartingWith(this Error error, string prefix);
    public static void ShouldHaveErrorCode(this Error error, Func<string, bool> predicate, ...);

    // ErrorCodeExpected variants (0-3 value overloads + predicate)
    public static void ShouldBeErrorCodeExpected(this Error error, string code, string value);
    public static void ShouldBeErrorCodeExpected<T>(this Error error, string code, T value);

    // Fin<T> verification
    public static T ShouldSucceed<T>(this Fin<T> fin);
    public static void ShouldSucceedWith<T>(this Fin<T> fin, T expectedValue);
    public static void ShouldFail<T>(this Fin<T> fin);
    public static void ShouldFail<T>(this Fin<T> fin, Action<Error> errorAssertion);
    public static void ShouldFailWithErrorCode<T>(this Fin<T> fin, string expectedErrorCode);

    // Validation<Error, T> verification
    public static T ShouldBeValid<T>(this Validation<Error, T> validation);
    public static void ShouldBeInvalid<T>(..., Action<Seq<Error>> errorsAssertion);
    public static void ShouldContainErrorCode<T>(..., string expectedErrorCode);
    public static void ShouldContainOnlyErrorCode<T>(..., string expectedErrorCode);
    public static void ShouldContainErrorCodes<T>(..., params string[] expectedErrorCodes);
}
```

### ErrorCodeExceptionalAssertions

Specialized extension methods for exception-based error (`ErrorCodeExceptional`) verification.

```csharp
public static class ErrorCodeExceptionalAssertions
{
    // Error verification
    public static void ShouldBeErrorCodeExceptional(this Error error, string code);
    public static void ShouldBeErrorCodeExceptional<TException>(this Error error, string code);
    public static void ShouldWrapException<TException>(
        this Error error, string code, string? message = null);

    // Fin<T> verification
    public static void ShouldFailWithException<T>(this Fin<T> fin, string code);
    public static void ShouldFailWithException<T, TException>(this Fin<T> fin, string code);

    // Validation<Error, T> verification
    public static void ShouldContainException<T>(..., string code);
    public static void ShouldContainException<T, TException>(..., string code);
    public static void ShouldContainOnlyException<T>(..., string code);
}
```

### ErrorAssertionHelpers

Common extension properties for `Error` and `Validation<Error, T>`. Uses C# 14 Extension Members syntax.

```csharp
public static class ErrorAssertionHelpers
{
    extension(Error error)
    {
        public string? ErrorCode { get; }   // Returns code when IHasErrorCode is implemented
        public bool HasErrorCode { get; }   // Whether IHasErrorCode is implemented
    }
    extension<T>(Validation<Error, T> validation)
    {
        public IReadOnlyList<Error> Errors { get; }  // Extract error list
    }
}
```

---

## Architecture Rules (ClassValidator, MethodValidator, InterfaceValidator)

ArchUnitNET-based Fluent API that verifies architecture rules at the class/method/interface level.

### Validation Entry Point

```csharp
public static class ArchitectureValidationEntryPoint
{
    public static ValidationResultSummary ValidateAllClasses(
        this IObjectProvider<Class> classes, Architecture architecture,
        Action<ClassValidator> validationRule, bool verbose = false);
    public static ValidationResultSummary ValidateAllInterfaces(
        this IObjectProvider<Interface> interfaces, Architecture architecture,
        Action<InterfaceValidator> validationRule, bool verbose = false);
}
```

### ClassValidator

Inherits `TypeValidator<Class, ClassValidator>` and chains via Fluent API.

| Category | Methods |
|----------|--------|
| Visibility | `RequirePublic()`, `RequireInternal()` |
| Modifiers | `RequireSealed()`, `RequireNotSealed()`, `RequireStatic()`, `RequireNotStatic()`, `RequireAbstract()`, `RequireNotAbstract()` |
| Type | `RequireRecord()`, `RequireNotRecord()`, `RequireAttribute(name)` |
| Inheritance | `RequireInherits(baseType)` |
| Constructors | `RequirePrivateAnyParameterlessConstructor()`, `RequireAllPrivateConstructors()` |
| Properties | `RequireNoPublicSetters()`, `RequireOnlyPrimitiveProperties(...)` |
| Fields | `RequireNoInstanceFields(...)` |
| Nested classes | `RequireNestedClass(name, validation?)`, `RequireNestedClassIfExists(name, validation?)` |
| Immutability | `RequireImmutable()` |

### TypeValidator\<TType, TSelf\> (Common Base)

A CRTP-based abstract base class shared by `ClassValidator` and `InterfaceValidator`.

| Category | Methods |
|----------|--------|
| Naming | `RequireNameStartsWith(prefix)`, `RequireNameEndsWith(suffix)`, `RequireNameMatching(regex)` |
| Interface | `RequireImplements(type)`, `RequireImplementsGenericInterface(name)` |
| Dependencies | `RequireNoDependencyOn(typeNameContains)` |
| Methods | `RequireMethod(name, validation)`, `RequireAllMethods(validation)`, `RequireAllMethods(filter, validation)`, `RequireMethodIfExists(name, validation)` |
| Properties | `RequireProperty(name)` |
| Composition | `Apply(IArchRule<TType> rule)` |

### MethodValidator

| Category | Methods |
|----------|--------|
| Visibility/Modifiers | `RequireVisibility(v)`, `RequireStatic()`, `RequireNotStatic()`, `RequireVirtual()`, `RequireNotVirtual()`, `RequireExtensionMethod()` |
| Return Type | `RequireReturnType(type)`, `RequireReturnTypeOfDeclaringClass()`, `RequireReturnTypeOfDeclaringTopLevelClass()`, `RequireReturnTypeContaining(fragment)` |
| Parameters | `RequireParameterCount(n)`, `RequireParameterCountAtLeast(n)`, `RequireFirstParameterTypeContaining(fragment)`, `RequireAnyParameterTypeContaining(fragment)` |

### InterfaceValidator

Inherits `TypeValidator<Interface, InterfaceValidator>`. Uses only the common methods from `TypeValidator`.

### IArchRule\<TType\>과 ImmutabilityRule

```csharp
public interface IArchRule<in TType> where TType : IType
{
    string Description { get; }
    IReadOnlyList<RuleViolation> Validate(TType target, Architecture architecture);
}
```

**`ImmutabilityRule`** is an `IArchRule<Class>` implementation that verifies immutability across 6 dimensions: Writability, Constructors (all private), PropertySetters (no public), PublicFields (none), MutableCollections (no `List<>`, etc.), StateChangingMethods (excluding factory/equality/getter).

### Auxiliary Types

| Type | Description |
|------|------|
| `RuleViolation(TargetName, RuleName, Description)` | Rule violation sealed record |
| `ValidationResultSummary` | Result aggregation, `ThrowIfAnyFailures(ruleName)` Throws `ArchitectureViolationException` on call |
| `ArchitectureViolationException` | `RuleName`, `Violations` Has properties |
| `CompositeArchRule<TType>` | AND composition of multiple rules |
| `DelegateArchRule<TType>` | Lambda-based custom rules |

---

## Test Suites (DomainArchitectureTestSuite, ApplicationArchitectureTestSuite)

### DomainArchitectureTestSuite

An abstract test suite that batch-verifies architecture rules for the domain layer. Provides a total of 21 `[Fact]` tests.

```csharp
public abstract class DomainArchitectureTestSuite
{
    protected abstract Architecture Architecture { get; }
    protected abstract string DomainNamespace { get; }
    protected virtual IReadOnlyList<Type> ValueObjectExcludeFromFactoryMethods => [];
    protected virtual string[] DomainServiceAllowedFieldTypes => [];
}
```

**Included tests (21):

| Category | Test | Description |
|----------|--------|------|
| Entity (7) | `AggregateRoot_ShouldBe_PublicSealedClass` | public sealed class |
| | `AggregateRoot_ShouldHave_CreateAndCreateFromValidated` | Static factory methods required |
| | `AggregateRoot_ShouldHave_GenerateEntityIdAttribute` | `[GenerateEntityId]` required |
| | `AggregateRoot_ShouldHave_AllPrivateConstructors` | All constructors private |
| | `Entity_ShouldBe_PublicSealedClass` | Non-AggregateRoot Entity also public sealed |
| | `Entity_ShouldHave_CreateAndCreateFromValidated` | Entity factory methods required |
| | `Entity_ShouldHave_AllPrivateConstructors` | Entity constructors private |
| ValueObject (4) | `ValueObject_ShouldBe_PublicSealedWithPrivateConstructors` | public sealed + private Constructors |
| | `ValueObject_ShouldBe_Immutable` | `ImmutabilityRule` applied |
| | `ValueObject_ShouldHave_CreateFactoryMethod` | `Create` returns `Fin<T>` |
| | `ValueObject_ShouldHave_ValidateMethod` | `Validate` returns `Validation<Error, T>` |
| DomainEvent (2) | `DomainEvent_ShouldBe_SealedRecord` | sealed record required |
| | `DomainEvent_ShouldHave_EventSuffix` | `"Event"` suffix required |
| Specification (3) | `Specification_ShouldBe_PublicSealed` | public sealed |
| | `Specification_ShouldInherit_SpecificationBase` | `Specification<T>` Inheritance |
| | `Specification_ShouldResideIn_DomainLayer` | Located only in domain layer |
| DomainService (5) | `DomainService_ShouldBe_PublicSealed` | public sealed |
| | `DomainService_ShouldBe_Stateless` | No instance fields |
| | `DomainService_ShouldNotDependOn_IObservablePort` | Observation dependency prohibited |
| | `DomainService_PublicMethods_ShouldReturn_Fin` | Public methods return `Fin` |
| | `DomainService_ShouldNotBe_Record` | record prohibited |

### ApplicationArchitectureTestSuite

An abstract test suite that verifies the CQRS structure of the application layer. Provides a total of 4 `[Fact]` tests.

```csharp
public abstract class ApplicationArchitectureTestSuite
{
    protected abstract Architecture Architecture { get; }
    protected abstract string ApplicationNamespace { get; }
}
```

| Test | Description |
|--------|------|
| `Command_ShouldHave_ValidatorNestedClass` | If Validator exists, sealed + `AbstractValidator` implementation |
| `Command_ShouldHave_UsecaseNestedClass` | Usecase required, sealed + `ICommandUsecase` implementation |
| `Query_ShouldHave_ValidatorNestedClass` | If Validator exists, sealed + `AbstractValidator` implementation |
| `Query_ShouldHave_UsecaseNestedClass` | Usecase required, sealed + `IQueryUsecase` implementation |

---

## Related Documents

- [Functorium.Testing Library Guide](../guides/testing/16-testing-library) - Design principles and usage patterns
- [Unit Testing Guide](../guides/testing/15a-unit-testing) - AAA pattern, MTP configuration, Verify snapshot testing
