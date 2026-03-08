---
title: "Unit Test 설정"
---

## 개요

일반적인 단위 테스트는 메서드를 호출하고 반환값을 검증합니다. 하지만 소스 생성기는 컴파일 타임에 실행되므로, 테스트하려면 Roslyn 컴파일 파이프라인을 직접 구성해야 합니다. 입력 소스 코드를 `CSharpCompilation`으로 컴파일하고, `CSharpGeneratorDriver`로 소스 생성기를 실행한 뒤, 생성된 코드를 문자열로 추출하는 과정이 필요합니다. Functorium은 이 과정을 `SourceGeneratorTestRunner`라는 유틸리티로 추상화하여, 테스트 코드에서는 `_sut.Generate(input)` 한 줄로 생성 결과를 얻을 수 있습니다.

## 학습 목표

### 핵심 학습 목표
1. **CSharpCompilation을 이용한 테스트 환경 구축**
   - Roslyn 컴파일러 API로 소스 생성기를 실행하는 방법
2. **SourceGeneratorTestRunner 유틸리티 이해**
   - 어셈블리 참조 관리와 생성 결과 추출 과정
3. **테스트 프로젝트 구성**
   - 필요한 NuGet 패키지와 프로젝트 참조 설정

---

## 소스 생성기 테스트의 특수성

소스 생성기는 컴파일 타임에 실행되므로 일반 단위 테스트와 다른 접근이 필요합니다.

```
일반 단위 테스트
================
입력 → 메서드 호출 → 출력 검증

소스 생성기 테스트
==================
입력 소스 코드 → 컴파일 → 생성된 코드 검증
```

---

## SourceGeneratorTestRunner 구현

### 전체 구조

```csharp
// Functorium.Testing/SourceGenerators/SourceGeneratorTestRunner.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Functorium.Testing.SourceGenerators;

/// <summary>
/// 소스 생성기 테스트를 위한 유틸리티 클래스.
/// IIncrementalGenerator를 테스트 환경에서 실행하고 결과를 반환합니다.
/// </summary>
public static class SourceGeneratorTestRunner
{
    // 테스트에서 항상 참조해야 하는 필수 어셈블리 타입 목록
    private static readonly Type[] RequiredTypes =
    [
        typeof(object),                                        // System.Runtime
        typeof(LanguageExt.IO),                                // LanguageExt.Core
        typeof(LanguageExt.FinT<,>),                           // LanguageExt.Core (generic)
        typeof(Microsoft.Extensions.Logging.ILogger),          // Microsoft.Extensions.Logging
    ];

    /// <summary>
    /// 소스 생성기를 실행하고 생성된 코드를 반환합니다.
    /// </summary>
    public static string? Generate<TGenerator>(this TGenerator generator, string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        // 구현...
    }
}
```

---

## 테스트 실행 흐름

### 1. Syntax Tree 생성

```csharp
// 소스 코드에서 Syntax Tree 생성
var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
```

입력 소스 코드를 Roslyn이 이해할 수 있는 형태로 변환합니다.

### 2. 필수 어셈블리 참조

```csharp
// 필수 어셈블리를 먼저 추가 (순서 보장)
var requiredReferences = RequiredTypes
    .Select(t => t.Assembly)
    .Distinct()
    .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
    .Cast<MetadataReference>();

// 현재 로드된 어셈블리 중 동적이 아닌 것들을 참조로 변환
var otherReferences = AppDomain
    .CurrentDomain
    .GetAssemblies()
    .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
    .Where(assembly => !RequiredTypes.Any(t => t.Assembly == assembly))
    .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
    .Cast<MetadataReference>();

// 필수 참조를 먼저, 그 다음 나머지 참조
var references = requiredReferences.Concat(otherReferences);
```

### 3. Compilation 생성

```csharp
var compilation = CSharpCompilation.Create(
    "SourceGeneratorTests",     // 생성할 어셈블리 이름
    [syntaxTree],               // 소스
    references,                 // 참조
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
```

### 4. 소스 생성기 실행

```csharp
// 컴파일: IIncrementalGenerator 소스 생성기 호출
CSharpGeneratorDriver
    .Create(generator)
    .RunGeneratorsAndUpdateCompilation(
        compilation,
        out var outputCompilation,          // 소스 생성기 결과: 소스
        out var diagnostics);               // 소스 생성기 진단: 경고, 에러
```

### 5. 결과 검증

```csharp
// 소스 생성기 진단(컴파일러 에러)
diagnostics
    .Where(d => d.Severity == DiagnosticSeverity.Error)
    .ShouldBeEmpty();

// 소스 생성기 결과(컴파일러 결과)
return outputCompilation
    .SyntaxTrees
    .Skip(1)                // [0] 원본 소스 SyntaxTree 제외
    .LastOrDefault()?
    .ToString();
```

---

## 테스트 프로젝트 구성

### 프로젝트 참조

```xml
<!-- Tests/Functorium.Tests.Unit/Functorium.Tests.Unit.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- 테스트 프레임워크 -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1" />

    <!-- Assertion -->
    <PackageReference Include="Shouldly" Version="4.3.0" />

    <!-- 스냅샷 테스트 -->
    <PackageReference Include="Verify.Xunit" Version="28.9.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- 테스트 유틸리티 -->
    <ProjectReference Include="..\..\Src\Functorium.Testing\Functorium.Testing.csproj" />

    <!-- 테스트 대상 소스 생성기 -->
    <ProjectReference Include="..\..\Src\Functorium.SourceGenerators\Functorium.SourceGenerators.csproj" />
  </ItemGroup>

</Project>
```

### NuGet 패키지

| 패키지 | 용도 |
|--------|------|
| `xunit` | 테스트 프레임워크 |
| `Shouldly` | Fluent Assertion |
| `Verify.Xunit` | 스냅샷 테스트 |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn 컴파일러 |

---

## 기본 테스트 작성

### 테스트 클래스 구조

```csharp
// ObservablePortGeneratorTests.cs
using Functorium.Adapters.SourceGenerators;
using Functorium.Testing.SourceGenerators;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

[Trait(nameof(UnitTest), UnitTest.Functorium_SourceGenerator)]
public sealed class ObservablePortGeneratorTests
{
    private readonly ObservablePortGenerator _sut;

    public ObservablePortGeneratorTests()
    {
        _sut = new ObservablePortGenerator();
    }

    [Fact]
    public Task Should_Generate_PipelineClass()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerators;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITestAdapter : IObservablePort
            {
                FinT<IO, int> GetValue();
            }

            [GenerateObservablePort]
            public class TestAdapter : ITestAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }
}
```

### 입력 소스 코드 패턴

```csharp
string input = """
    // 1. 필요한 using 문
    using Functorium.Adapters.SourceGenerators;
    using Functorium.Applications.Observabilities;
    using LanguageExt;

    // 2. 네임스페이스
    namespace TestNamespace;

    // 3. 인터페이스 정의 (IObservablePort 상속)
    public interface ITestAdapter : IObservablePort
    {
        FinT<IO, int> GetValue();
    }

    // 4. [GenerateObservablePort] 속성 적용
    [GenerateObservablePort]
    public class TestAdapter : ITestAdapter
    {
        public string RequestCategory => "Test";
        public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
    }
    """;
```

---

## 확장 메서드 활용

### Generate 메서드 사용

```csharp
// SourceGeneratorTestRunner의 확장 메서드
string? actual = _sut.Generate(input);

// 내부적으로:
// 1. CSharpSyntaxTree.ParseText(input)
// 2. CSharpCompilation.Create(...)
// 3. CSharpGeneratorDriver.Create(_sut).RunGeneratorsAndUpdateCompilation(...)
// 4. 생성된 SyntaxTree의 ToString() 반환
```

### Null 결과 처리

```csharp
[Fact]
public void Should_Return_Null_When_NoAttributeApplied()
{
    string input = """
        public class RegularClass { }
        """;

    string? actual = _sut.Generate(input);

    // [GenerateObservablePort] 속성이 없으면 생성 안 됨
    actual.ShouldBeNull();
}
```

---

## 테스트 실행

### Visual Studio

```
Test Explorer → Run All Tests
```

### 명령줄

```bash
dotnet test Tests/Functorium.Tests.Unit/Functorium.Tests.Unit.csproj
```

### 특정 테스트만 실행

```bash
dotnet test --filter "FullyQualifiedName~ObservablePortGeneratorTests"
```

---

## 요약

소스 생성기 테스트의 핵심은 Roslyn 컴파일 파이프라인을 테스트 환경에서 재현하는 것입니다. `SourceGeneratorTestRunner`가 Syntax Tree 생성, 어셈블리 참조 수집, Compilation 생성, Generator 실행의 전 과정을 캡슐화하므로, 테스트 코드는 입력과 출력에만 집중할 수 있습니다. 생성된 코드의 검증에는 `Verify` 스냅샷 테스트와 `Shouldly` assertion을 함께 사용합니다.

---

## FAQ

### Q1: `SourceGeneratorTestRunner`에서 `RequiredTypes`에 타입을 추가해야 하는 기준은 무엇인가요?
**A**: 입력 소스 코드에서 사용하는 외부 타입의 어셈블리가 컴파일에 참조되어야 합니다. `LanguageExt.IO`, `FinT<,>`, `ILogger` 등 ObservablePortGenerator가 분석하는 코드에 등장하는 타입들의 어셈블리를 `RequiredTypes`에 등록하면, `MetadataReference.CreateFromFile()`로 자동 수집됩니다. 테스트 입력에 새로운 외부 타입이 추가되면 이 배열도 업데이트해야 합니다.

### Q2: `outputCompilation.SyntaxTrees.Skip(1)`에서 첫 번째 트리를 건너뛰는 이유는 무엇인가요?
**A**: `SyntaxTrees`의 첫 번째 항목은 테스트에서 입력한 원본 소스 코드입니다. 소스 생성기가 추가한 코드는 그 이후에 위치하므로, `Skip(1).LastOrDefault()`로 마지막 생성 파일(일반적으로 Observable 클래스 코드)을 가져옵니다. 마커 Attribute도 생성 파일에 포함되므로, 마지막 파일이 실제 생성 코드가 됩니다.

### Q3: 소스 생성기 테스트에서 컴파일 오류가 발생하면 어떻게 디버깅하나요?
**A**: `diagnostics`에서 `DiagnosticSeverity.Error`를 필터링하면 오류 메시지를 확인할 수 있습니다. 흔한 원인은 입력 소스 코드에서 사용하는 타입의 어셈블리가 `RequiredTypes`에 누락된 경우, 또는 입력 코드 자체에 구문 오류가 있는 경우입니다. `outputCompilation.GetDiagnostics()`로 전체 진단 목록을 출력하면 원인을 특정할 수 있습니다.

---

## 다음 단계

테스트 환경이 갖추어졌으니, 생성된 코드 전체를 파일로 저장하고 비교하는 Verify 스냅샷 테스트 방식을 알아봅니다.

→ [06. Verify 스냅샷 테스트](../06-Verify-Snapshot-Testing/)
