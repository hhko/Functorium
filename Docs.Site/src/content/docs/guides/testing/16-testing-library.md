---
title: "Functorium.Testing 라이브러리 가이드"
---

테스트 코드는 프로덕션 코드와 동일한 수준의 일관성이 필요합니다. 프로젝트가 성장하면 로그 캡처, 아키텍처 규칙 검증, 소스 생성기 테스트 등 반복적인 테스트 인프라 코드가 각 프로젝트에 중복됩니다.
`Functorium.Testing`은 이러한 반복을 제거하고, 프레임워크에 특화된 테스트 유틸리티를 단일 라이브러리로 제공하여 테스트 코드의 일관성과 유지보수성을 확보합니다.

## 들어가며

"Pipeline이 출력하는 구조화된 로그 필드가 정확한지 어떻게 검증하는가?"
"ValueObject의 불변성 규칙을 모든 클래스에 일괄 적용하려면 어떻게 해야 하는가?"
"소스 생성기가 올바른 코드를 생성하는지 어떻게 테스트하는가?"

이러한 테스트 인프라를 프로젝트마다 직접 구현하면 중복 코드가 쌓이고, 프레임워크 업데이트 시 동기화가 어려워집니다. `Functorium.Testing`은 이러한 반복 패턴을 단일 라이브러리로 통합하여 일관된 테스트 기반을 제공합니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **`LogTestContext` 기반 구조화된 로그 테스트** - Serilog 인메모리 캡처와 Verify 스냅샷 연동
2. **`FinTFactory`를 활용한 Mock 반환값 설정** - Port/Adapter의 `FinT<IO, T>` 반환값 생성
3. **아키텍처 규칙 검증 Fluent API** - ArchUnitNET 기반 클래스/메서드 수준 규칙 적용
4. **`SourceGeneratorTestRunner`로 소스 생성기 테스트** - 입력 코드 → 생성 코드 검증
5. **`QuartzTestFixture`로 스케줄 Job 통합 테스트** - DI 통합 환경에서 Job 1회 실행 검증

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- [단위 테스트 가이드](./15a-unit-testing) - AAA 패턴, MTP 설정, Verify 스냅샷 테스트
- LanguageExt의 `Fin<T>`, `FinT<IO, T>` 타입 기본 개념
- Serilog 구조화된 로깅의 기본 원리

> **핵심 원칙:** `Functorium.Testing`은 구조화된 로그 캡처, 아키텍처 규칙 검증, 소스 생성기 테스트, Mock 반환값 생성 등 반복적인 테스트 인프라를 단일 라이브러리로 통합하여 프로젝트 간 일관성을 보장합니다.

## 요약

### 주요 명령

```csharp
// 구조화된 로그 테스트
using var context = new LogTestContext();
var logger = context.CreateLogger<MyPipeline>();
// ... 테스트 실행 후
await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots");

// 아키텍처 규칙 검증
ArchRuleDefinition.Classes().That()
    .ImplementInterface(typeof(IValueObject))
    .ValidateAllClasses(Architecture, @class => { ... })
    .ThrowIfAnyFailures("Rule Name");

// 소스 생성기 테스트
string? actual = _sut.Generate(input);
return Verify(actual).UseDirectory("Snapshots/EntityIdGenerator");

// Mock 반환값 설정
_repository.GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Succ(product));
```

### 주요 절차

**1. 로그 테스트:**
1. `LogTestContext` 생성
2. `CreateLogger<T>()`로 ILogger 생성
3. 테스트 대상에 로거 주입 후 실행
4. `ExtractFirstLogData()` 등으로 데이터 추출
5. `Verify()`로 스냅샷 비교 또는 직접 Assertion

**2. 아키텍처 규칙 검증:**
1. `ArchRuleDefinition.Classes()`로 대상 클래스 필터링
2. `ValidateAllClasses()`에 검증 규칙 콜백 전달
3. `ThrowIfAnyFailures()`로 실패 시 예외 발생

### 주요 개념

| 개념 | 설명 |
|------|------|
| `LogTestContext` | Serilog 기반 인메모리 로그 캡처 컨텍스트 |
| `FinTFactory` | `FinT<IO, T>` Mock 반환값 생성 헬퍼 |
| `ClassValidator` | 클래스 수준 아키텍처 규칙 Fluent API |
| `SourceGeneratorTestRunner` | `IIncrementalGenerator` 테스트 실행기 |
| `QuartzTestFixture` | Quartz.NET Job 통합 테스트 Fixture |

---

## 개요

`Functorium.Testing`은 Functorium 프레임워크의 테스트 유틸리티 라이브러리입니다.

### 네임스페이스 구조

다음 테이블은 라이브러리의 전체 네임스페이스 구조와 각 모듈의 역할을 정리한 것입니다.

| 네임스페이스 | 역할 |
|---|---|
| `Functorium.Testing.Arrangements.Logging` | 구조화된 로그 캡처 (LogTestContext, StructuredTestLogger) |
| `Functorium.Testing.Arrangements.Loggers` | 인메모리 Serilog Sink (TestSink) |
| `Functorium.Testing.Arrangements.Effects` | `FinT<IO, T>` 반환값 생성 헬퍼 (FinTFactory) |
| `Functorium.Testing.Arrangements.Hosting` | HTTP 통합 테스트 Fixture (HostTestFixture) |
| `Functorium.Testing.Arrangements.ScheduledJobs` | 스케줄 Job 테스트 Fixture (QuartzTestFixture) |
| `Functorium.Testing.Actions.SourceGenerators` | 소스 생성기 테스트 Runner |
| `Functorium.Testing.Assertions.ArchitectureRules` | 아키텍처 규칙 검증 |
| `Functorium.Testing.Assertions.Logging` | 로그 데이터 추출/변환 유틸리티 |
| `Functorium.Testing.Assertions.Errors` | 에러 타입 Assertion (Domain/Application/Adapter별 + 범용 ErrorCode/Exceptional) |

### 다른 가이드에 문서화된 기능

| 기능 | 참조 가이드 |
|---|---|
| `HostTestFixture<TProgram>` — HTTP 엔드포인트 통합 테스트 | [01-project-structure.md](../architecture/01-project-structure) |
| `ShouldBeDomainError`, `ShouldBeApplicationError` 등 에러 Assertion | [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app), [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) |

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

> **참고**: 통합 테스트에서 Host 프로젝트를 참조할 때 Mediator SourceGenerator 중복을 방지하려면 `ExcludeAssets="analyzers"`를 추가합니다. 자세한 내용은 [01-project-structure.md](../architecture/01-project-structure)의 FAQ를 참조하세요.

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

프로젝트 참조가 구성되었으면, 이제 라이브러리가 제공하는 핵심 기능을 하나씩 살펴봅니다.

## FinTFactory (Mock 반환값 헬퍼)

`FinTFactory`는 `FinT<IO, T>` 반환값을 간편하게 생성하는 정적 헬퍼입니다. Port/Adapter의 Mock 반환값을 설정할 때 사용합니다.

```csharp
// 네임스페이스
using Functorium.Testing.Arrangements.Effects;
```

### API

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `FinTFactory.Succ<T>(T value)` | `FinT<IO, T>` | 성공 값을 래핑한 `FinT` 생성 |
| `FinTFactory.Fail<T>(Error error)` | `FinT<IO, T>` | 실패 에러를 래핑한 `FinT` 생성 |

### NSubstitute 사용 예시

```csharp
// Port Mock 설정 — 성공 반환
_productRepository
    .GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Succ(product));

// Port Mock 설정 — 실패 반환
_productRepository
    .GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Fail<Product>(
        AdapterError.For<InMemoryProductRepository>(
            new NotFound(), id.ToString(), "상품을 찾을 수 없습니다")));
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

### LogEventPropertyExtractor 타입별 처리 상세

`LogEventPropertyExtractor`는 `static class`이며, `ExtractValue(LogEventPropertyValue)` 메서드에서 switch 식으로 Serilog의 모든 주요 `LogEventPropertyValue` 하위 타입을 처리합니다.

**타입별 처리 로직:**

| 타입 | 처리 방식 | 결과 |
|------|----------|------|
| `ScalarValue` | `.Value` (null이면 `"null"` 문자열) | 원시 값 (`string`, `int`, `bool` 등) |
| `SequenceValue` | `.Elements.Select(ExtractValue).ToList()` | `List<object>` |
| `StructureValue` | `.Properties.ToDictionary(p => p.Name, p => ExtractValue(p.Value))` | `Dictionary<string, object>` |
| `DictionaryValue` | `.Elements.ToDictionary(kvp => kvp.Key.Value?.ToString() ?? "null", kvp => ExtractValue(kvp.Value))` | `Dictionary<string, object>` |
| 기타 | `HandleUnhandledType()` — Debug.WriteLine 후 `.ToString()` 반환 | `string` |

**`ExtractLogData(LogEvent)`** — 단일 LogEvent에서 익명 객체를 생성합니다:

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

**`ExtractLogData(IEnumerable<LogEvent>)`** — 여러 LogEvent를 `.Select()`로 변환합니다.

> **참고**: 정적 람다(`static p =>`)를 사용하여 불필요한 클로저 할당을 방지합니다.

### LogEventPropertyExtractor 사용 예시

스냅샷 테스트가 아닌 직접 Assertion으로 로그 필드를 검증하는 패턴:

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

    // Assert - 첫 번째 로그의 속성을 직접 검증
    var firstLog = context.GetFirstLog();
    var data = LogEventPropertyExtractor.ExtractLogData(firstLog);

    // Properties에서 특정 필드 검증
    var properties = (IDictionary<string, object?>)data.Properties;
    properties["request.layer"].ShouldBe("application");
    properties["request.category"].ShouldBe("usecase");
    properties["request.handler"].ShouldNotBeNull();
}
```

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

Mock 반환값 설정 방법을 익혔으면, 다음으로 아키텍처 규칙을 자동으로 검증하는 방법을 알아봅니다.

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

### SingleHost 아키텍처 테스트 인벤토리

다음 테이블은 SingleHost 레퍼런스 프로젝트에 구현된 아키텍처 테스트의 전체 목록입니다.

| 테스트 클래스 | 테스트 수 | 검증 대상 |
|--------------|----------|----------|
| `LayerDependencyArchitectureRuleTests` | 6 | 레이어 간 의존성 방향 (Domain !→ Application, Adapter 간 교차 참조 금지 등) |
| `EntityArchitectureRuleTests` | 5 | AggregateRoot/Entity: public sealed, 상속, Create/CreateFromValidated 팩토리 |
| `ValueObjectArchitectureRuleTests` | 4 | ValueObject: public sealed, 불변성, Create/Validate 팩토리 |
| `DtoArchitectureRuleTests` | 5 | DTO/Model/Mapper: Persistence Mapper internal static, Usecase nested Request/Response |
| `CqrsArchitectureRuleTests` | 1 | CQRS 패턴 준수: Query Usecase가 IRepository에 의존하지 않도록 강제 |
| `UsecaseArchitectureRuleTests` | 4 | Command/Query: 내부 Validator/Usecase nested class 존재 |
| `SpecificationArchitectureRuleTests` | 3 | Specification: public sealed, 상속, Domain 레이어 거주 |
| `PortAndAdapterArchitectureRuleTests` | 3 | Adapter: GenerateObservablePort 어트리뷰트, RequestCategory, DomainService sealed |

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

아키텍처 규칙은 클래스 구조를 검증한다면, 소스 생성기 테스트는 코드 생성 결과를 검증합니다.

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

소스 생성기가 정적 코드 생성을 검증한다면, 스케줄 Job 테스트는 런타임에서 실제 Job 실행을 검증합니다.

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

> **참고**: `appsettings.Test.json` 파일은 Host 프로젝트 루트에 위치해야 하며, `.csproj`에서 `CopyToOutputDirectory`를 설정해야 합니다:
> ```xml
> <ItemGroup>
>   <Content Include="appsettings.Test.json" CopyToOutputDirectory="PreserveNewest" />
> </ItemGroup>
> ```
> `WebApplicationFactory`가 Host 프로젝트의 `ContentRootPath`를 기준으로 설정 파일을 로드하므로, 테스트 프로젝트가 아닌 **Host 프로젝트**에 파일이 있어야 합니다.

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

## 트러블슈팅

### 소스 생성기 테스트에서 컴파일 에러 발생

**원인:** `SourceGeneratorTestRunner.Generate()`는 내부적으로 필수 어셈블리(System.Runtime, LanguageExt.Core, Microsoft.Extensions.Logging)만 자동 참조합니다. 테스트 입력 코드가 다른 어셈블리의 타입을 사용하면 컴파일 에러가 발생합니다.

**해결:** 소스 생성기 테스트의 입력 코드는 자동 참조되는 어셈블리 범위 내에서 작성하세요. 소스 생성기가 처리하는 마커 Attribute와 대상 클래스만 포함하면 충분합니다.

### `LogTestContext`에서 로그가 캡처되지 않음

**원인:** `LogTestContext`의 기본 최소 레벨은 `Debug`입니다. 테스트 대상이 `Verbose` 레벨로 로깅하는 경우 캡처되지 않습니다. 또는 `CreateLogger<T>()`의 타입 파라미터가 실제 로깅 클래스와 다른 경우입니다.

**해결:** 최소 레벨을 명시적으로 지정하세요: `new LogTestContext(LogEventLevel.Verbose)`. 로거의 타입 파라미터가 테스트 대상 클래스의 `ILogger<T>`와 일치하는지 확인하세요.

### 아키텍처 규칙 검증에서 예상치 못한 클래스가 포함됨

**원인:** `ArchRuleDefinition.Classes().That()` 필터 조건이 너무 넓어 의도하지 않은 클래스(추상 클래스, 테스트용 클래스 등)가 포함된 경우입니다.

**해결:** `.And().AreNotAbstract()`, `.And().DoNotHaveNameContaining("Test")` 등 추가 필터 조건을 적용하여 대상 범위를 좁히세요. `verbose: true` 옵션으로 검증 대상 클래스 목록을 확인할 수 있습니다.

---

## FAQ

**Q: `LogTestContext`와 `ITestOutputHelper` 차이는?**

`LogTestContext`는 Serilog 기반으로 구조화된 로그 필드(속성명, 값 타입, 중첩 구조)까지 캡처하여 스냅샷 테스트가 가능합니다. `ITestOutputHelper`는 단순 텍스트 출력만 지원하므로 필드 구조 검증에는 적합하지 않습니다.

**Q: `ArchitectureRules`를 커스텀할 수 있는가?**

가능합니다. 기본 제공 규칙(`RequireImmutable`, `RequireSealed` 등) 외에 `ValidateAllClasses`의 `Action<ClassValidator>` 콜백에서 프로젝트별 규칙을 조합하여 추가할 수 있습니다.

**Q: `QuartzTestFixture`에서 실제 Job이 실행되는가?**

인메모리 스케줄러에서 Job이 실제로 실행됩니다. DI 컨테이너의 모든 서비스가 주입되므로, 외부 의존성(DB, API 등)만 Mock으로 교체하면 통합 수준의 검증이 가능합니다.

---

## 참고 문서

- [15a-unit-testing.md](./15a-unit-testing) — 단위 테스트 규칙 (명명, AAA 패턴, MTP 설정)
- [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app) — Domain/Application 에러 Assertion 패턴
- [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) — Adapter 에러 Assertion 및 범용 에러 Assertion
- [01-project-structure.md](../architecture/01-project-structure) — 프로젝트 구성 (HostTestFixture, 통합 테스트)
- [18a-observability-spec.md](../observability/18a-observability-spec) — Observability 사양 (로그 필드 정의)
