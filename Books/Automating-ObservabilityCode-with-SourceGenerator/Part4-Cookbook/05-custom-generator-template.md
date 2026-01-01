# 커스텀 생성기 템플릿

## 학습 목표

- 새로운 소스 생성기 프로젝트 구조 이해
- 재사용 가능한 템플릿 코드 습득
- 개발 체크리스트 활용

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

---

## 코드 템플릿

### 메인 생성기 클래스

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

---

## 테스트 템플릿

### 테스트 러너

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

---

## 추가 학습 자료

- [Roslyn Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [Microsoft Learn: Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Andrew Lock: Source Generators](https://andrewlock.net/series/creating-a-source-generator/)

---

## 다음 단계

이 장에서 배운 내용을 바탕으로 자신만의 소스 생성기를 만들어 보세요!

➡️ [11장: 결론으로 돌아가기](../11-conclusion/01-summary.md)
