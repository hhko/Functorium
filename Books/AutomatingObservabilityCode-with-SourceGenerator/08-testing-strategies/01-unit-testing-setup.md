# 단위 테스트 설정

## 학습 목표

- CSharpCompilation을 이용한 테스트 환경 구축
- SourceGeneratorTestRunner 유틸리티 이해
- 테스트 프로젝트 구성

---

## 소스 생성기 테스트의 특수성

소스 생성기는 **컴파일 타임**에 실행되므로 일반 단위 테스트와 다른 접근이 필요합니다.

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
    <ProjectReference Include="..\..\Src\Functorium.Adapters.SourceGenerator\Functorium.Adapters.SourceGenerator.csproj" />
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
// AdapterPipelineGeneratorTests.cs
using Functorium.Adapters.SourceGenerator;
using Functorium.Testing.SourceGenerators;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters_SourceGenerator)]
public sealed class AdapterPipelineGeneratorTests
{
    private readonly AdapterPipelineGenerator _sut;

    public AdapterPipelineGeneratorTests()
    {
        _sut = new AdapterPipelineGenerator();
    }

    [Fact]
    public Task Should_Generate_PipelineClass()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITestAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
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
    using Functorium.Adapters.SourceGenerator;
    using Functorium.Applications.Observabilities;
    using LanguageExt;

    // 2. 네임스페이스
    namespace TestNamespace;

    // 3. 인터페이스 정의 (IAdapter 상속)
    public interface ITestAdapter : IAdapter
    {
        FinT<IO, int> GetValue();
    }

    // 4. [GeneratePipeline] 속성 적용
    [GeneratePipeline]
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

    // [GeneratePipeline] 속성이 없으면 생성 안 됨
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
dotnet test --filter "FullyQualifiedName~AdapterPipelineGeneratorTests"
```

---

## 요약

| 구성 요소 | 역할 |
|----------|------|
| `SourceGeneratorTestRunner` | 테스트 실행 유틸리티 |
| `CSharpCompilation` | 컴파일 컨텍스트 생성 |
| `CSharpGeneratorDriver` | 소스 생성기 실행 |
| `Verify` | 스냅샷 테스트 |

| 단계 | 설명 |
|------|------|
| 1 | 입력 소스 코드 준비 |
| 2 | Syntax Tree 생성 |
| 3 | 어셈블리 참조 추가 |
| 4 | Compilation 생성 |
| 5 | 소스 생성기 실행 |
| 6 | 결과 검증 |

---

## 다음 단계

다음 섹션에서는 Verify 스냅샷 테스트를 학습합니다.

➡️ [02. Verify 스냅샷 테스트](02-verify-snapshot-testing.md)
