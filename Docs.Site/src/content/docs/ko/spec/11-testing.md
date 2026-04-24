---
title: "테스트 라이브러리 사양"
---

Functorium 프레임워크가 제공하는 테스트 유틸리티 라이브러리의 공개 API 사양입니다. 설계 원칙과 사용 패턴은 [Functorium.Testing 라이브러리 가이드](../guides/testing/16-testing-library)를 참조하십시오.

## 요약

### 주요 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `FinTFactory` | `Arrangements.Effects` | `FinT<IO, T>` Mock 반환값 생성 헬퍼 |
| `HostTestFixture<TProgram>` | `Arrangements.Hosting` | 호스트 통합 테스트 Fixture |
| `QuartzTestFixture<TProgram>` | `Arrangements.ScheduledJobs` | Quartz Job 통합 테스트 Fixture |
| `LogTestContext` | `Arrangements.Logging` | Serilog 기반 인메모리 로그 캡처 컨텍스트 |
| `StructuredTestLogger<T>` | `Arrangements.Logging` | 구조화된 로깅 지원 테스트 로거 |
| `SourceGeneratorTestRunner` | `Actions.SourceGenerators` | `IIncrementalGenerator` 테스트 실행기 |
| `DomainErrorAssertions` | `Assertions.Errors` | 도메인 에러 검증 확장 메서드 |
| `ApplicationErrorAssertions` | `Assertions.Errors` | 애플리케이션 에러 검증 확장 메서드 |
| `AdapterErrorAssertions` | `Assertions.Errors` | 어댑터 에러 검증 확장 메서드 |
| `ErrorCodeAssertions` | `Assertions.Errors` | 범용 에러 코드 검증 확장 메서드 |
| `ExceptionalErrorAssertions` | `Assertions.Errors` | 예외 기반 에러 검증 확장 메서드 |
| `ClassValidator` | `Assertions.ArchitectureRules` | 클래스 수준 아키텍처 규칙 Fluent API |
| `MethodValidator` | `Assertions.ArchitectureRules` | 메서드 수준 아키텍처 규칙 Fluent API |
| `InterfaceValidator` | `Assertions.ArchitectureRules` | 인터페이스 수준 아키텍처 규칙 Fluent API |
| `DomainArchitectureTestSuite` | `Assertions.ArchitectureRules.Suites` | 도메인 레이어 아키텍처 테스트 스위트 |
| `ApplicationArchitectureTestSuite` | `Assertions.ArchitectureRules.Suites` | 애플리케이션 레이어 아키텍처 테스트 스위트 |

> 모든 네임스페이스는 `Functorium.Testing` 접두사를 가집니다.

---

## FinTFactory (Mock 반환값)

Application 레이어 Usecase 테스트에서 Port/Adapter의 `FinT<IO, T>` 반환값을 생성하는 정적 헬퍼입니다.

```csharp
public static class FinTFactory
{
    public static FinT<IO, T> Succ<T>(T value);
    public static FinT<IO, T> Fail<T>(Error error);
}
```

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `Succ<T>(T value)` | `FinT<IO, T>` | 성공 값을 감싼 `FinT` 반환 |
| `Fail<T>(Error error)` | `FinT<IO, T>` | 실패 에러를 감싼 `FinT` 반환 |

---

## 호스트 테스트 (HostTestFixture\<T\>, QuartzTestFixture\<T\>)

### HostTestFixture\<TProgram\>

`WebApplicationFactory`를 사용하여 전체 DI 설정을 재사용하는 호스트 통합 테스트 Fixture입니다. 기본 환경은 `"Test"`이며, `appsettings.Test.json`을 자동으로 로드합니다.

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

| 멤버 | 타입 | 설명 |
|------|------|------|
| `EnvironmentName` | `string` (virtual) | 사용할 환경 이름 (기본값: `"Test"`) |
| `Services` | `IServiceProvider` | DI 컨테이너 접근 |
| `Client` | `HttpClient` | 테스트용 HTTP 클라이언트 |
| `GetTestProjectPath()` | `string` (virtual) | 테스트 프로젝트 경로 (기본: `bin/` 기준 3단계 상위) |
| `ConfigureHost(builder)` | `void` (virtual) | Host 추가 설정 확장 포인트 |

**설정 파일 로드 순서:** TProgram 프로젝트의 `appsettings.json` (기본) -> 테스트 프로젝트의 `appsettings.json` (덮어씀)

### QuartzTestFixture\<TProgram\>

Quartz.NET Job 통합 테스트를 위한 Fixture입니다. `HostTestFixture`와 동일한 환경/설정 구조이며, 스케줄러와 Job 리스너를 추가로 관리합니다.

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

| 멤버 | 설명 |
|------|------|
| `JobListener` | Job 완료 감지 리스너 (`JobCompletionListener`) |
| `Scheduler` | Quartz 스케줄러 인스턴스 |
| `ExecuteJobOnceAsync<TJob>(timeout)` | Job을 즉시 1회 실행하고 완료 대기 (이름/그룹 자동 추출) |
| `ExecuteJobOnceAsync<TJob>(name, group, timeout)` | Job을 즉시 1회 실행하고 완료 대기 (이름/그룹 명시) |

### JobExecutionResult

```csharp
public sealed record JobExecutionResult(
    string JobName, bool Success, object? Result,
    JobExecutionException? Exception, TimeSpan ExecutionTime);
```

`Success`는 예외 없이 완료되면 `true`입니다.

---

## 로그 테스트 (LogTestContext, StructuredTestLogger)

### LogTestContext

Serilog 기반 인메모리 로그 캡처와 검증을 위한 컨텍스트 클래스입니다. `IDisposable`을 구현합니다.

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

**생성자:**

| 생성자 | 설명 |
|--------|------|
| `LogTestContext()` | 기본 최소 레벨(`Debug`)로 초기화 |
| `LogTestContext(minimumLevel)` | 지정된 최소 로그 레벨로 초기화 |
| `LogTestContext(minimumLevel, enrichFromLogContext)` | 최소 레벨 + LogContext enrichment 옵션. `ctx.*` 필드를 캡처하려면 `true` |

**주요 메서드:**

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `CreateLogger<T>()` | `ILogger<T>` | 구조화된 테스트 로거 생성 |
| `GetFirstLog()` / `GetSecondLog()` | `LogEvent` | 첫 번째/두 번째 로그 이벤트 |
| `GetLogAt(index)` | `LogEvent` | 지정 인덱스의 로그 이벤트 |
| `GetLogsByLevel(level)` | `IReadOnlyList<LogEvent>` | 지정 레벨의 모든 로그 이벤트 |
| `ExtractFirstLogData()` / `ExtractSecondLogData()` | `object` | Verify 스냅샷용 익명 객체 추출 |
| `ExtractAllLogData()` | `IEnumerable<object>` | 모든 로그를 익명 객체 목록으로 추출 |

### StructuredTestLogger\<T\>

`LoggerMessage` 어트리뷰트로 생성된 메서드의 구조화된 로깅을 올바르게 처리하는 `ILogger<T>` 구현체입니다. `state`가 `IReadOnlyList<KeyValuePair<string, object?>>` 형태인 경우 `OriginalFormat`과 속성명을 파싱하여 Serilog `LogEvent`를 직접 생성합니다.

```csharp
public class StructuredTestLogger<T> : ILogger<T>
{
    public StructuredTestLogger(Serilog.ILogger serilogLogger);
    public bool IsEnabled(LogLevel logLevel); // 항상 true
}
```

---

## 소스 생성기 테스트 (SourceGeneratorTestRunner)

`IIncrementalGenerator`를 테스트 환경에서 실행하고 결과를 반환하는 정적 유틸리티입니다.

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

| 메서드 | 설명 |
|--------|------|
| `Generate` | 생성된 코드 반환. 컴파일러 에러 시 Shouldly로 실패 |
| `GenerateWithDiagnostics` | 생성된 코드 + 진단 결과 반환. `DiagnosticDescriptor` 테스트용 |

**필수 참조 어셈블리:** `System.Runtime`, `LanguageExt.Core`, `Microsoft.Extensions.Logging` + 현재 로드된 모든 비동적 어셈블리

---

## 에러 어설션 (5종)

레이어별 에러 타입에 대한 타입 안전 검증 확장 메서드입니다. 모든 어설션은 `Error`, `Fin<T>`, `Validation<Error, T>`에 대해 동작합니다.

### 레이어별 어설션 (3종)

`DomainErrorAssertions`, `ApplicationErrorAssertions`, `AdapterErrorAssertions`는 동일한 패턴으로 레이어별 에러를 검증합니다. 각 클래스의 `TContext` 타입 매개변수와 에러 코드 접두사만 다릅니다.

| 클래스 | `TContext` 의미 | 에러 코드 형식 |
|--------|----------------|---------------|
| `DomainErrorAssertions` | `TDomain` (도메인 타입) | `Domain.{Name}.{ErrorName}` |
| `ApplicationErrorAssertions` | `TUsecase` (유스케이스 타입) | `Application.{Name}.{ErrorName}` |
| `AdapterErrorAssertions` | `TAdapter` (어댑터 타입) | `Adapter.{Name}.{ErrorName}` |

**공통 메서드 패턴 (Domain 기준):**

```csharp
// Error 검증 (0~3개 현재 값 오버로드)
error.ShouldBeDomainError<TDomain>(errorType);
error.ShouldBeDomainError<TDomain, TValue>(errorType, currentValue);

// Fin<T> 검증
fin.ShouldBeDomainError<TDomain, T>(errorType);
fin.ShouldBeDomainError<TDomain, T, TValue>(errorType, currentValue);

// Validation<Error, T> 검증
validation.ShouldHaveDomainError<TDomain, T>(errorType);
validation.ShouldHaveOnlyDomainError<TDomain, T>(errorType);
validation.ShouldHaveDomainErrors<TDomain, T>(errorType1, errorType2, ...);
validation.ShouldHaveDomainError<TDomain, T, TValue>(errorType, currentValue);
```

**`AdapterErrorAssertions`의 추가 메서드** (예외 래핑 에러 검증):

```csharp
error.ShouldBeAdapterExceptionalError<TAdapter>(errorType);
error.ShouldBeAdapterExceptionalError<TAdapter, TException>(errorType);
fin.ShouldBeAdapterExceptionalError<TAdapter, T>(errorType);
```

### ErrorCodeAssertions (범용)

`DomainErrorKind` 등에 의존하지 않는 범용 에러 코드 검증입니다.

```csharp
public static class ErrorCodeAssertions
{
    // Error 상태 검증
    public static IHasErrorCode ShouldHaveErrorCode(this Error error);
    public static void ShouldBeExpected(this Error error);
    public static void ShouldBeExceptional(this Error error);

    // ErrorCode 매칭
    public static void ShouldHaveErrorCode(this Error error, string expectedErrorCode);
    public static void ShouldHaveErrorCodeStartingWith(this Error error, string prefix);
    public static void ShouldHaveErrorCode(this Error error, Func<string, bool> predicate, ...);

    // ExpectedError 변형 (0~3개 값 오버로드 + predicate)
    public static void ShouldBeExpectedError(this Error error, string code, string value);
    public static void ShouldBeExpectedError<T>(this Error error, string code, T value);

    // Fin<T> 검증
    public static T ShouldSucceed<T>(this Fin<T> fin);
    public static void ShouldSucceedWith<T>(this Fin<T> fin, T expectedValue);
    public static void ShouldFail<T>(this Fin<T> fin);
    public static void ShouldFail<T>(this Fin<T> fin, Action<Error> errorAssertion);
    public static void ShouldFailWithErrorCode<T>(this Fin<T> fin, string expectedErrorCode);

    // Validation<Error, T> 검증
    public static T ShouldBeValid<T>(this Validation<Error, T> validation);
    public static void ShouldBeInvalid<T>(..., Action<Seq<Error>> errorsAssertion);
    public static void ShouldContainErrorCode<T>(..., string expectedErrorCode);
    public static void ShouldContainOnlyErrorCode<T>(..., string expectedErrorCode);
    public static void ShouldContainErrorCodes<T>(..., params string[] expectedErrorCodes);
}
```

### ExceptionalErrorAssertions

예외 기반 에러(`ExceptionalError`) 검증을 위한 특화된 확장 메서드입니다.

```csharp
public static class ExceptionalErrorAssertions
{
    // Error 검증
    public static void ShouldBeExceptionalError(this Error error, string code);
    public static void ShouldBeExceptionalError<TException>(this Error error, string code);
    public static void ShouldWrapException<TException>(
        this Error error, string code, string? message = null);

    // Fin<T> 검증
    public static void ShouldFailWithException<T>(this Fin<T> fin, string code);
    public static void ShouldFailWithException<T, TException>(this Fin<T> fin, string code);

    // Validation<Error, T> 검증
    public static void ShouldContainException<T>(..., string code);
    public static void ShouldContainException<T, TException>(..., string code);
    public static void ShouldContainOnlyException<T>(..., string code);
}
```

### ErrorAssertionHelpers

`Error`와 `Validation<Error, T>`에 대한 공통 확장 속성입니다. C# 14 Extension Members 문법을 사용합니다.

```csharp
public static class ErrorAssertionHelpers
{
    extension(Error error)
    {
        public string? ErrorCode { get; }   // IHasErrorCode 구현 시 코드 반환
        public bool HasErrorCode { get; }   // IHasErrorCode 구현 여부
    }
    extension<T>(Validation<Error, T> validation)
    {
        public IReadOnlyList<Error> Errors { get; }  // 에러 목록 추출
    }
}
```

---

## 아키텍처 규칙 (ClassValidator, MethodValidator, InterfaceValidator)

ArchUnitNET 기반 Fluent API로, 클래스/메서드/인터페이스 수준의 아키텍처 규칙을 검증합니다.

### 검증 진입점

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

`TypeValidator<Class, ClassValidator>`를 상속하며, Fluent API로 체이닝합니다.

| 카테고리 | 메서드 |
|----------|--------|
| 가시성 | `RequirePublic()`, `RequireInternal()` |
| 한정자 | `RequireSealed()`, `RequireNotSealed()`, `RequireStatic()`, `RequireNotStatic()`, `RequireAbstract()`, `RequireNotAbstract()` |
| 타입 | `RequireRecord()`, `RequireNotRecord()`, `RequireAttribute(name)` |
| 상속 | `RequireInherits(baseType)` |
| 생성자 | `RequirePrivateAnyParameterlessConstructor()`, `RequireAllPrivateConstructors()` |
| 프로퍼티 | `RequireNoPublicSetters()`, `RequireOnlyPrimitiveProperties(...)` |
| 필드 | `RequireNoInstanceFields(...)` |
| 중첩 클래스 | `RequireNestedClass(name, validation?)`, `RequireNestedClassIfExists(name, validation?)` |
| 불변성 | `RequireImmutable()` |

### TypeValidator\<TType, TSelf\> (공통 기반)

`ClassValidator`와 `InterfaceValidator`가 공유하는 CRTP 기반 추상 기반 클래스입니다.

| 카테고리 | 메서드 |
|----------|--------|
| 네이밍 | `RequireNameStartsWith(prefix)`, `RequireNameEndsWith(suffix)`, `RequireNameMatching(regex)` |
| 인터페이스 | `RequireImplements(type)`, `RequireImplementsGenericInterface(name)` |
| 의존성 | `RequireNoDependencyOn(typeNameContains)` |
| 메서드 | `RequireMethod(name, validation)`, `RequireAllMethods(validation)`, `RequireAllMethods(filter, validation)`, `RequireMethodIfExists(name, validation)` |
| 프로퍼티 | `RequireProperty(name)` |
| 합성 | `Apply(IArchRule<TType> rule)` |

### MethodValidator

| 카테고리 | 메서드 |
|----------|--------|
| 가시성/한정자 | `RequireVisibility(v)`, `RequireStatic()`, `RequireNotStatic()`, `RequireVirtual()`, `RequireNotVirtual()`, `RequireExtensionMethod()` |
| 반환 타입 | `RequireReturnType(type)`, `RequireReturnTypeOfDeclaringClass()`, `RequireReturnTypeOfDeclaringTopLevelClass()`, `RequireReturnTypeContaining(fragment)` |
| 매개변수 | `RequireParameterCount(n)`, `RequireParameterCountAtLeast(n)`, `RequireFirstParameterTypeContaining(fragment)`, `RequireAnyParameterTypeContaining(fragment)` |

### InterfaceValidator

`TypeValidator<Interface, InterfaceValidator>`를 상속합니다. `TypeValidator`의 공통 메서드만 사용합니다.

### IArchRule\<TType\>과 ImmutabilityRule

```csharp
public interface IArchRule<in TType> where TType : IType
{
    string Description { get; }
    IReadOnlyList<RuleViolation> Validate(TType target, Architecture architecture);
}
```

**`ImmutabilityRule`은** `IArchRule<Class>` 구현체로, 6가지 차원에서 불변성을 검증합니다: Writability, Constructors (all private), PropertySetters (no public), PublicFields (none), MutableCollections (`List<>` 등 금지), StateChangingMethods (팩토리/동등성/getter 제외).

### 보조 타입

| 타입 | 설명 |
|------|------|
| `RuleViolation(TargetName, RuleName, Description)` | 규칙 위반 sealed record |
| `ValidationResultSummary` | 결과 집계, `ThrowIfAnyFailures(ruleName)` 호출 시 `ArchitectureViolationException` 발생 |
| `ArchitectureViolationException` | `RuleName`, `Violations` 속성 보유 |
| `CompositeArchRule<TType>` | 여러 규칙을 AND 합성 |
| `DelegateArchRule<TType>` | 람다 기반 커스텀 규칙 |

---

## 테스트 스위트 (DomainArchitectureTestSuite, ApplicationArchitectureTestSuite)

### DomainArchitectureTestSuite

도메인 레이어의 아키텍처 규칙을 일괄 검증하는 추상 테스트 스위트입니다. 총 21개의 `[Fact]` 테스트를 제공합니다.

```csharp
public abstract class DomainArchitectureTestSuite
{
    protected abstract Architecture Architecture { get; }
    protected abstract string DomainNamespace { get; }
    protected virtual IReadOnlyList<Type> ValueObjectExcludeFromFactoryMethods => [];
    protected virtual string[] DomainServiceAllowedFieldTypes => [];
}
```

**포함된 테스트 (21개):**

| 카테고리 | 테스트 | 설명 |
|----------|--------|------|
| Entity (7) | `AggregateRoot_ShouldBe_PublicSealedClass` | public sealed 클래스 |
| | `AggregateRoot_ShouldHave_CreateAndCreateFromValidated` | 정적 팩토리 메서드 필수 |
| | `AggregateRoot_ShouldHave_GenerateEntityIdAttribute` | `[GenerateEntityId]` 필수 |
| | `AggregateRoot_ShouldHave_AllPrivateConstructors` | 모든 생성자 private |
| | `Entity_ShouldBe_PublicSealedClass` | 비 AggregateRoot Entity도 public sealed |
| | `Entity_ShouldHave_CreateAndCreateFromValidated` | Entity 팩토리 메서드 필수 |
| | `Entity_ShouldHave_AllPrivateConstructors` | Entity 생성자 private |
| ValueObject (4) | `ValueObject_ShouldBe_PublicSealedWithPrivateConstructors` | public sealed + private 생성자 |
| | `ValueObject_ShouldBe_Immutable` | `ImmutabilityRule` 적용 |
| | `ValueObject_ShouldHave_CreateFactoryMethod` | `Create`가 `Fin<T>` 반환 |
| | `ValueObject_ShouldHave_ValidateMethod` | `Validate`가 `Validation<Error, T>` 반환 |
| DomainEvent (2) | `DomainEvent_ShouldBe_SealedRecord` | sealed record 필수 |
| | `DomainEvent_ShouldHave_EventSuffix` | `"Event"` 접미사 필수 |
| Specification (3) | `Specification_ShouldBe_PublicSealed` | public sealed |
| | `Specification_ShouldInherit_SpecificationBase` | `Specification<T>` 상속 |
| | `Specification_ShouldResideIn_DomainLayer` | 도메인 레이어에만 위치 |
| DomainService (5) | `DomainService_ShouldBe_PublicSealed` | public sealed |
| | `DomainService_ShouldBe_Stateless` | 인스턴스 필드 없음 |
| | `DomainService_ShouldNotDependOn_IObservablePort` | 관측 의존 금지 |
| | `DomainService_PublicMethods_ShouldReturn_Fin` | public 메서드 `Fin` 반환 |
| | `DomainService_ShouldNotBe_Record` | record 금지 |

### ApplicationArchitectureTestSuite

애플리케이션 레이어의 CQRS 구조를 검증하는 추상 테스트 스위트입니다. 총 4개의 `[Fact]` 테스트를 제공합니다.

```csharp
public abstract class ApplicationArchitectureTestSuite
{
    protected abstract Architecture Architecture { get; }
    protected abstract string ApplicationNamespace { get; }
}
```

| 테스트 | 설명 |
|--------|------|
| `Command_ShouldHave_ValidatorNestedClass` | Validator 있으면 sealed + `AbstractValidator` 구현 |
| `Command_ShouldHave_UsecaseNestedClass` | Usecase 필수, sealed + `ICommandUsecase` 구현 |
| `Query_ShouldHave_ValidatorNestedClass` | Validator 있으면 sealed + `AbstractValidator` 구현 |
| `Query_ShouldHave_UsecaseNestedClass` | Usecase 필수, sealed + `IQueryUsecase` 구현 |

---

## 관련 문서

- [Functorium.Testing 라이브러리 가이드](../guides/testing/16-testing-library) - 설계 원칙과 사용 패턴
- [단위 테스트 가이드](../guides/testing/15a-unit-testing) - AAA 패턴, MTP 설정, Verify 스냅샷 테스트
