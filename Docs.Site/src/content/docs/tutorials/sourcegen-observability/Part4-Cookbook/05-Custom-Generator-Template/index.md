---
title: "Custom Generator Template"
---

## 개요

Part 2에서 ObservablePortGenerator를 구현하고, 이 장에서 Entity Id, ValueConverter, Validation 생성기까지 만들어 보았다면, 이제 "다음 생성기는 어떻게 시작하지?"라는 질문이 남습니다. 매번 처음부터 프로젝트를 구성하는 것은 비효율적이고, 이전에 검증된 구조를 잊어버리기 쉽습니다. 이 절에서는 지금까지의 경험을 바탕으로, 새로운 소스 생성기를 빠르게 시작할 수 있는 프로젝트 템플릿과 코드 템플릿을 제공합니다.

## 학습 목표

### 핵심 학습 목표
1. **새로운 소스 생성기 프로젝트 구조 이해**
   - 생성기, 속성, 모델, 테스트 프로젝트의 분리 원칙
2. **재사용 가능한 템플릿 코드 습득**
   - 즉시 복사하여 사용할 수 있는 생성기 골격 코드
3. **개발 체크리스트 활용**
   - 프로젝트 설정부터 배포까지 빠짐없이 확인하는 검증 목록

---

## 프로젝트 구조 템플릿

### 권장 폴더 구조

```
MyCompany.SourceGenerator/
├── MyCompany.SourceGenerator/
│   ├── MyCompany.SourceGenerator.csproj
│   ├── MyGenerator.cs                    # 메인 생성기
│   ├── Attributes/
│   │   └── MyAttribute.cs                # 마커 속성 소스 코드
│   ├── Models/
│   │   └── MyInfo.cs                     # 메타데이터 record
│   └── Generators/
│       └── MyCodeGenerator.cs            # 코드 생성 로직
│
├── MyCompany.SourceGenerator.Tests/
│   ├── MyCompany.SourceGenerator.Tests.csproj
│   ├── MyGeneratorTests.cs               # 테스트 클래스
│   ├── TestRunner.cs                     # 테스트 유틸리티
│   └── Snapshots/
│       └── *.verified.txt                # Verify 스냅샷
│
└── MyCompany.SourceGenerator.sln
```

이 폴더 구조는 ObservablePortGenerator와 이 장의 세 가지 생성기에서 공통으로 사용한 패턴입니다. 관심사 분리를 통해 생성기 로직, 속성 정의, 메타데이터 모델, 코드 생성 로직을 각각 독립적으로 수정할 수 있도록 합니다.

---

## 프로젝트 파일 템플릿

### 소스 생성기 프로젝트 (csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- 필수: netstandard2.0 -->
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- 소스 생성기 필수 설정 -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>

    <!-- NuGet 패키지 정보 -->
    <PackageId>MyCompany.SourceGenerator</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Company>My Company</Company>
    <Description>Source generator for automating boilerplate code</Description>
    <PackageTags>source-generator;roslyn;codegen</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <!-- 빌드 설정 -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <!-- Roslyn API (버전 고정 권장) -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <!-- 소스 생성기로 패키징 -->
    <None Include="$(OutputPath)\$(AssemblyName).dll"
          Pack="true"
          PackagePath="analyzers/dotnet/cs"
          Visible="false" />
    <None Include="..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

### 테스트 프로젝트 (csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- 테스트 프레임워크 -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>

    <!-- Verify 스냅샷 테스트 -->
    <PackageReference Include="Verify.Xunit" Version="26.6.0" />

    <!-- 어설션 -->
    <PackageReference Include="Shouldly" Version="4.2.1" />

    <!-- Roslyn 테스트 유틸리티 -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyCompany.SourceGenerator\MyCompany.SourceGenerator.csproj" />
  </ItemGroup>

</Project>
```

프로젝트 설정이 준비되면 실제 코드 작성을 시작합니다. 아래 템플릿들은 Part 2의 ObservablePortGenerator와 이 장의 생성기들에서 반복적으로 사용한 구조를 추출한 것입니다.

---

## 코드 템플릿

### 메인 생성기 클래스

메인 생성기는 세 가지 책임을 순서대로 수행합니다. Post-Initialization으로 마커 속성을 등록하고, `ForAttributeWithMetadataName`으로 대상 타입을 수집한 뒤, `RegisterSourceOutput`으로 코드를 생성합니다. 이 구조는 모든 IIncrementalGenerator에 공통입니다.

```csharp
// MyGenerator.cs
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MyCompany.SourceGenerator.Attributes;
using MyCompany.SourceGenerator.Models;
using MyCompany.SourceGenerator.Generators;

namespace MyCompany.SourceGenerator;

/// <summary>
/// [MyAttribute] 속성이 붙은 타입에 대해 코드를 생성합니다.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1단계: 고정 코드 생성 (속성 정의)
        RegisterPostInitialization(context);

        // 2단계: 대상 타입 수집
        var provider = RegisterSourceProvider(context);

        // 3단계: 코드 생성
        context.RegisterSourceOutput(provider, Execute);
    }

    private static void RegisterPostInitialization(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(
                hintName: "MyAttribute.g.cs",
                sourceText: SourceText.From(MyAttribute.Source, Encoding.UTF8));
        });
    }

    private static IncrementalValuesProvider<MyInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: MyAttribute.FullyQualifiedName,
                predicate: IsTargetNode,
                transform: MapToMyInfo)
            .Where(static x => x is not null)!;
    }

    private static bool IsTargetNode(SyntaxNode node, CancellationToken _)
    {
        // TODO: 대상 노드 타입 지정
        return node is TypeDeclarationSyntax;
    }

    private static MyInfo? MapToMyInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken _)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        // TODO: 메타데이터 추출 로직
        return new MyInfo(
            TypeName: typeSymbol.Name,
            Namespace: typeSymbol.ContainingNamespace.ToDisplayString());
    }

    private static void Execute(
        SourceProductionContext context,
        MyInfo info)
    {
        var source = MyCodeGenerator.Generate(info);
        var fileName = $"{info.Namespace.Replace(".", "")}{info.TypeName}.g.cs";

        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }
}
```

### 마커 속성 정의

마커 속성은 문자열 상수로 소스 코드를 정의하고, Post-Initialization 단계에서 컴파일에 주입합니다. `global::` 접두사를 사용하여 소비자 프로젝트의 네임스페이스와 충돌하지 않도록 합니다.

```csharp
// Attributes/MyAttribute.cs
namespace MyCompany.SourceGenerator.Attributes;

/// <summary>
/// 마커 속성 소스 코드
/// </summary>
internal static class MyAttribute
{
    public const string Source = """
        // <auto-generated/>
        #nullable enable

        namespace MyCompany.SourceGenerator;

        /// <summary>
        /// 코드 생성 대상 타입에 적용합니다.
        /// </summary>
        [global::System.AttributeUsage(
            global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct,
            AllowMultiple = false,
            Inherited = false)]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
            Justification = "Generated by source generator.")]
        public sealed class MyAttribute : global::System.Attribute;
        """;

    public const string FullyQualifiedName = "MyCompany.SourceGenerator.MyAttribute";
}
```

### 메타데이터 클래스

메타데이터 클래스는 반드시 `record`로 정의합니다. Roslyn의 증분 파이프라인은 이전 실행 결과와 현재 결과를 비교하여 변경된 경우에만 코드를 재생성하는데, 이 비교에 `Equals`/`GetHashCode`가 사용되기 때문입니다.

```csharp
// Models/MyInfo.cs
namespace MyCompany.SourceGenerator.Models;

/// <summary>
/// 코드 생성에 필요한 메타데이터
/// </summary>
public sealed record MyInfo(
    string TypeName,
    string Namespace);
```

### 코드 생성기

코드 생성 로직을 별도 클래스로 분리하면, 메인 생성기의 `Execute` 메서드가 간결해지고 생성 로직을 독립적으로 테스트할 수 있습니다. `// <auto-generated/>` 헤더와 `#nullable enable`은 생성 코드의 표준 프리앰블입니다.

```csharp
// Generators/MyCodeGenerator.cs
using System.Text;
using MyCompany.SourceGenerator.Models;

namespace MyCompany.SourceGenerator.Generators;

/// <summary>
/// 소스 코드 생성 로직
/// </summary>
internal static class MyCodeGenerator
{
    private const string Header = """
        // <auto-generated/>
        // This code was generated by MyCompany.SourceGenerator.
        // Do not modify this file directly.

        #nullable enable

        """;

    public static string Generate(MyInfo info)
    {
        var sb = new StringBuilder();

        // 헤더
        sb.Append(Header);
        sb.AppendLine();

        // using 문
        sb.AppendLine("using System;");
        sb.AppendLine();

        // 네임스페이스
        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();

        // TODO: 생성할 코드 작성
        sb.AppendLine($"// Generated code for {info.TypeName}");
        sb.AppendLine($"public partial class {info.TypeName}Generated");
        sb.AppendLine("{");
        sb.AppendLine("    // TODO: 생성할 멤버들");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
```

코드 템플릿이 준비되면 테스트를 작성합니다. 소스 생성기 테스트는 "입력 소스 코드를 컴파일하고, 생성기를 실행한 뒤, 생성된 코드를 검증"하는 일관된 패턴을 따릅니다. 아래 테스트 러너는 이 과정을 캡슐화합니다.

---

## 테스트 템플릿

### 테스트 러너

테스트 러너는 Roslyn의 `CSharpCompilation`과 `CSharpGeneratorDriver`를 사용하여 생성기를 실행합니다. `RequiredTypes` 배열에 필요한 런타임 타입을 추가하면 참조 어셈블리가 자동으로 수집됩니다.

```csharp
// TestRunner.cs
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace MyCompany.SourceGenerator.Tests;

/// <summary>
/// 소스 생성기 테스트 유틸리티
/// </summary>
public static class TestRunner
{
    private static readonly Type[] RequiredTypes =
    [
        typeof(object),       // System.Runtime
        typeof(Attribute),    // System.Runtime
    ];

    /// <summary>
    /// 소스 생성기를 실행하고 생성된 코드를 반환합니다.
    /// </summary>
    public static string? Generate<TGenerator>(
        this TGenerator generator,
        string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        // 1. 구문 트리 생성
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // 2. 참조 어셈블리 수집
        var references = RequiredTypes
            .Select(t => t.Assembly.Location)
            .Distinct()
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToImmutableArray<MetadataReference>();

        // 3. 컴파일레이션 생성
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 4. 생성기 실행
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        // 5. 진단 검증
        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        // 에러가 있으면 표시
        foreach (var error in errors)
        {
            Console.WriteLine($"Error: {error.GetMessage()}");
        }

        errors.ShouldBeEmpty("Compilation should not have errors");

        // 6. 생성된 코드 반환 (마지막 파일 - 속성 제외)
        var result = driver.GetRunResult();
        return result.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .LastOrDefault();
    }

    /// <summary>
    /// 모든 생성된 파일을 반환합니다.
    /// </summary>
    public static IReadOnlyList<(string FileName, string Content)> GenerateAll<TGenerator>(
        this TGenerator generator,
        string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = RequiredTypes
            .Select(t => t.Assembly.Location)
            .Distinct()
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToImmutableArray<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var result = driver.GetRunResult();
        return result.GeneratedTrees
            .Select(t => (
                FileName: Path.GetFileName(t.FilePath),
                Content: t.GetText().ToString()))
            .ToList();
    }
}
```

### 테스트 클래스

테스트는 세 가지 시나리오를 기본으로 작성합니다. 속성 생성 확인, 대상 타입에 대한 코드 생성, 속성이 없는 경우의 부정 테스트입니다. 이 구성은 Entity Id, ValueConverter, Validation 생성기에서 모두 동일하게 사용했습니다.

```csharp
// MyGeneratorTests.cs
using Xunit;

namespace MyCompany.SourceGenerator.Tests;

public sealed class MyGeneratorTests
{
    private readonly MyGenerator _sut = new();

    [Fact]
    public Task MyGenerator_ShouldGenerate_Attribute()
    {
        // Arrange
        string input = """
            namespace TestNamespace;

            public class TestClass { }
            """;

        // Act
        var files = _sut.GenerateAll(input);

        // Assert
        var attributeFile = files.FirstOrDefault(f => f.FileName.Contains("MyAttribute"));
        return Verify(attributeFile.Content);
    }

    [Fact]
    public Task MyGenerator_ShouldGenerate_ForTargetType()
    {
        // Arrange
        string input = """
            using MyCompany.SourceGenerator;

            namespace TestNamespace;

            [My]
            public class TestClass { }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    [Fact]
    public void MyGenerator_ShouldNotGenerate_WhenNoAttribute()
    {
        // Arrange
        string input = """
            namespace TestNamespace;

            public class TestClass { }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldBeNull();
    }
}
```

코드 템플릿과 테스트 템플릿이 준비되었으니, 마지막으로 개발 과정에서 빠뜨리기 쉬운 항목들을 체크리스트로 정리합니다. 이 목록은 01절의 7단계 워크플로우와 대응합니다.

---

## 개발 체크리스트

```markdown
# 소스 생성기 개발 체크리스트

## 프로젝트 설정
- [ ] TargetFramework: netstandard2.0
- [ ] EnforceExtendedAnalyzerRules: true
- [ ] IsRoslynComponent: true
- [ ] Microsoft.CodeAnalysis.CSharp 참조 (버전 고정)
- [ ] Microsoft.CodeAnalysis.Analyzers 참조

## 구현
- [ ] IIncrementalGenerator 구현
- [ ] [Generator(LanguageNames.CSharp)] 속성 적용
- [ ] RegisterPostInitializationOutput으로 마커 속성 생성
- [ ] ForAttributeWithMetadataName으로 대상 필터링
- [ ] predicate에서 대상 노드 타입 검증
- [ ] transform에서 메타데이터 추출
- [ ] RegisterSourceOutput으로 코드 생성 연결

## 생성 코드 품질
- [ ] // <auto-generated/> 헤더
- [ ] #nullable enable
- [ ] ExcludeFromCodeCoverage 속성
- [ ] global:: 접두사로 네임스페이스 충돌 방지
- [ ] XML 문서 주석

## 테스트
- [ ] Verify 스냅샷 테스트
- [ ] 기본 케이스 테스트
- [ ] 경계 케이스 테스트
- [ ] 부정 케이스 테스트 (속성 없는 경우)
- [ ] 네임스페이스 변형 테스트

## 패키징
- [ ] PackageId, Version 설정
- [ ] 패키지 설명 작성
- [ ] analyzers/dotnet/cs 경로에 DLL 포함
- [ ] dotnet pack -c Release 테스트

## 문서화
- [ ] README.md 작성
- [ ] 사용 예제 포함
- [ ] 제한 사항 명시
```

생성기가 기대대로 동작하지 않을 때 사용할 수 있는 디버깅 기법을 소개합니다.

---

## 디버깅 팁

### Visual Studio에서 디버깅

```csharp
// 생성기 코드에 추가
public void Initialize(IncrementalGeneratorInitializationContext context)
{
#if DEBUG
    // 디버거 연결 대기
    if (!System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Launch();
    }
#endif

    // ... 나머지 코드
}
```

### 진단 출력

```csharp
// 진단 메시지 출력
private static void Execute(
    SourceProductionContext context,
    MyInfo info)
{
    // 정보성 진단
    context.ReportDiagnostic(Diagnostic.Create(
        new DiagnosticDescriptor(
            id: "MYGEN001",
            title: "Code Generated",
            messageFormat: "Generated code for {0}",
            category: "MyGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true),
        Location.None,
        info.TypeName));

    // ... 코드 생성
}
```

### 로깅 (개발 중)

```csharp
// 파일로 로그 출력 (개발 중에만 사용)
private static void Log(string message)
{
#if DEBUG
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "generator-log.txt");
    File.AppendAllText(logPath, $"{DateTime.Now}: {message}\n");
#endif
}
```

개발과 테스트가 완료되면 NuGet 패키지로 배포합니다. csproj에서 이미 설정한 `analyzers/dotnet/cs` 경로 설정이 여기서 효과를 발휘합니다.

---

## 패키징 및 배포

### NuGet 패키지 생성

```bash
# Release 빌드 (중요!)
dotnet build -c Release

# 패키지 생성
dotnet pack -c Release -o ./packages

# 패키지 확인
dotnet nuget locals all --list
```

### 로컬 테스트

```xml
<!-- 소비자 프로젝트의 nuget.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local" value="C:\path\to\packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- 소비자 프로젝트 csproj -->
<ItemGroup>
  <PackageReference Include="MyCompany.SourceGenerator"
                    Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### NuGet.org 게시

```bash
# API 키 설정
dotnet nuget setapikey YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# 게시
dotnet nuget push ./packages/MyCompany.SourceGenerator.1.0.0.nupkg \
    --source https://api.nuget.org/v3/index.json
```

---

## 요약

| 항목 | 권장 사항 |
|------|----------|
| **TargetFramework** | netstandard2.0 |
| **Roslyn 버전** | 4.8.0 (버전 고정) |
| **테스트 프레임워크** | xUnit + Verify |
| **코드 구조** | 생성기 / 속성 / 모델 / 생성 로직 분리 |
| **빌드** | dotnet pack -c Release |

이 템플릿은 ObservablePortGenerator, Entity Id 생성기, ValueConverter 생성기, Validation 생성기를 구현하며 반복적으로 검증된 구조입니다. 새로운 생성기를 시작할 때 이 템플릿을 복사하고, `My`를 실제 이름으로 바꾸고, TODO 주석을 채워 나가면 됩니다.

---

## 추가 학습 자료

- [Roslyn Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [Microsoft Learn: Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Andrew Lock: Source Generators](https://andrewlock.net/series/creating-a-source-generator/)

---

## FAQ

### Q1: 템플릿의 `MyGenerator`에서 `IsTargetNode`의 `TypeDeclarationSyntax` 필터링을 더 구체적으로 바꿔야 하나요?
**A**: 네. `TypeDeclarationSyntax`는 `class`, `struct`, `record`, `interface`를 모두 포함하므로, 대상 노드를 정확히 필터링하지 않으면 불필요한 심볼 분석이 발생합니다. Entity Id 생성기처럼 `RecordDeclarationSyntax`와 `StructKeyword`를 조합하거나, 필요에 따라 `ClassDeclarationSyntax`만 허용하도록 구체화하면 증분 캐싱 효율이 높아집니다.

### Q2: 메타데이터 클래스를 `record`가 아닌 `class`로 정의하면 어떤 문제가 생기나요?
**A**: Roslyn의 증분 파이프라인은 이전 실행 결과와 현재 결과를 `Equals()`로 비교하여 변경 여부를 판단합니다. `class`는 기본적으로 참조 동등성을 사용하므로, 내용이 동일해도 매번 다른 객체로 인식되어 코드가 매 빌드마다 재생성됩니다. `record`는 값 동등성을 자동 제공하므로 증분 캐싱이 올바르게 작동합니다.

### Q3: `GenerateAll()` 메서드와 `Generate()` 메서드는 언제 각각 사용하나요?
**A**: `Generate()`는 마지막 생성 파일(일반적으로 메인 생성 코드)만 반환하므로 대부분의 스냅샷 테스트에 적합합니다. `GenerateAll()`은 마커 속성, 인터페이스, 메인 코드 등 모든 생성 파일을 파일명과 함께 반환하므로, 속성 코드가 올바르게 생성되는지 검증하거나 생성 파일 목록을 확인할 때 사용합니다.

---

## 다음 단계

Part 4 Cookbook의 모든 내용을 다루었습니다. 개발 워크플로우부터 세 가지 실전 생성기, 그리고 재사용 가능한 템플릿까지 소스 생성기를 독립적으로 만들기 위한 도구가 갖추어졌습니다. 다음 장에서는 이 튜토리얼 전체를 돌아보며 핵심 내용을 정리합니다.

→ [11장: 결론으로 돌아가기](../../Part5-Conclusion/01-summary.md)
