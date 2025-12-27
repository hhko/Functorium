# 프로젝트 구조

## 학습 목표

- 소스 생성기 프로젝트의 csproj 설정 이해
- IsRoslynComponent와 관련 속성의 역할 파악
- 실제 Functorium 프로젝트 구조 분석

---

## 소스 생성기 프로젝트 설정

### 필수 PropertyGroup 설정

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- 1. 타겟 프레임워크: 반드시 netstandard2.0 -->
    <TargetFramework>netstandard2.0</TargetFramework>

    <!-- 2. 최신 C# 언어 버전 사용 -->
    <LangVersion>latest</LangVersion>

    <!-- 3. Roslyn 컴포넌트 표시 (핵심!) -->
    <IsRoslynComponent>true</IsRoslynComponent>

    <!-- 4. 분석기 규칙 강화 -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>

    <!-- 5. Nullable 참조 타입 -->
    <Nullable>enable</Nullable>

    <!-- 6. 암시적 using 비활성화 (netstandard2.0에서 권장) -->
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

### 속성 상세 설명

| 속성 | 값 | 설명 |
|------|-----|------|
| `TargetFramework` | `netstandard2.0` | 모든 .NET 환경에서 실행 가능 |
| `LangVersion` | `latest` | C# 13 문법 사용 (생성기 코드에서) |
| `IsRoslynComponent` | `true` | IDE가 소스 생성기로 인식 |
| `EnforceExtendedAnalyzerRules` | `true` | 분석기 개발 모범 사례 강제 |
| `Nullable` | `enable` | null 안전성 검사 |
| `ImplicitUsings` | `disable` | netstandard2.0 호환성 |

---

## IsRoslynComponent의 역할

`IsRoslynComponent`를 `true`로 설정하면:

```
1. IDE 인식
===========
Visual Studio와 VS Code가 이 프로젝트를
소스 생성기/분석기로 인식합니다.

2. 빌드 출력
===========
analyzers 폴더에 DLL이 배치됩니다:
MyGenerator/
├── bin/
│   └── Debug/
│       └── netstandard2.0/
│           └── MyGenerator.dll
│
└── obj/
    └── Debug/
        └── netstandard2.0/
            └── analyzer/  ← 분석기 출력 폴더

3. NuGet 패키징
==============
NuGet 패키지로 배포 시 올바른 위치에 배치:
analyzers/dotnet/cs/MyGenerator.dll
```

---

## 실제 프로젝트 분석: Functorium.Adapters.SourceGenerator

### csproj 전체 구조

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- NuGet 패키지 설정 -->
  <PropertyGroup>
    <PackageId>Functorium.Adapters.SourceGenerator</PackageId>
    <Description>Source generator for Functorium adapter pipeline</Description>
    <PackageTags>source-generator;roslyn;observability</PackageTags>
  </PropertyGroup>

  <!-- 소스 생성기 핵심 설정 -->
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>

  <!-- NuGet 패키지 참조 -->
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                      Version="4.12.0"
                      PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers"
                      Version="3.11.0"
                      PrivateAssets="all" />
  </ItemGroup>

  <!-- 분석기로 패키징 -->
  <ItemGroup>
    <None Include="$(OutputPath)\$(AssemblyName).dll"
          Pack="true"
          PackagePath="analyzers/dotnet/cs"
          Visible="false" />
  </ItemGroup>

</Project>
```

### PrivateAssets="all"의 의미

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                  Version="4.12.0"
                  PrivateAssets="all" />
```

```
PrivateAssets="all" 효과
========================

1. 전이적 의존성 차단
   - 이 패키지를 참조하는 프로젝트에
   - Microsoft.CodeAnalysis.CSharp가 전달되지 않음

2. NuGet 패키지에서 제외
   - 소스 생성기 NuGet 패키지에
   - Roslyn 패키지가 포함되지 않음

3. 런타임 의존성 제거
   - 컴파일 타임에만 사용
   - 애플리케이션 런타임에 필요 없음
```

---

## 프로젝트 파일 구조

```
Functorium.Adapters.SourceGenerator/
│
├── Functorium.Adapters.SourceGenerator.csproj
│
├── AdapterPipelineGenerator.cs          # 메인 소스 생성기
│
├── Abstractions/
│   ├── Constants.cs                     # 공통 상수 (헤더 등)
│   └── Selectors.cs                     # 공통 선택자
│
└── Generators/
    ├── IncrementalGeneratorBase.cs      # 템플릿 메서드 패턴 기반 클래스
    │
    └── AdapterPipelineGenerator/        # 생성기별 헬퍼 클래스
        ├── PipelineClassInfo.cs         # 클래스 정보 레코드
        ├── MethodInfo.cs                # 메서드 정보 레코드
        ├── ParameterInfo.cs             # 파라미터 정보 레코드
        ├── TypeExtractor.cs             # 타입 추출 유틸리티
        ├── CollectionTypeHelper.cs      # 컬렉션 타입 판별
        ├── SymbolDisplayFormats.cs      # 타입 문자열 포맷
        ├── ConstructorParameterExtractor.cs  # 생성자 분석
        └── ParameterNameResolver.cs     # 이름 충돌 해결
```

---

## 데이터 모델 (레코드)

### PipelineClassInfo

```csharp
namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 파이프라인 생성에 필요한 클래스 정보
/// </summary>
public sealed record PipelineClassInfo(
    string Namespace,                               // 네임스페이스
    string ClassName,                               // 클래스 이름
    List<MethodInfo> Methods,                       // 메서드 목록
    List<ParameterInfo> BaseConstructorParameters)  // 생성자 파라미터
{
    public static readonly PipelineClassInfo None = new(
        string.Empty, string.Empty, [], []);
}
```

### MethodInfo

```csharp
/// <summary>
/// 메서드 정보
/// </summary>
public sealed record MethodInfo(
    string Name,                    // 메서드 이름
    List<ParameterInfo> Parameters, // 파라미터 목록
    string ReturnType);             // 반환 타입 (global:: 접두사 포함)
```

### ParameterInfo

```csharp
/// <summary>
/// 파라미터 정보
/// </summary>
public sealed record ParameterInfo(
    string Name,            // 파라미터 이름
    string Type,            // 타입 (global:: 접두사 포함)
    RefKind RefKind);       // ref, out, in 키워드
```

---

## 프로젝트 참조 구성

### 소스 생성기를 사용하는 프로젝트

```xml
<!-- Functorium.csproj (핵심 라이브러리) -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 소스 생성기 참조 (컴파일 타임만) -->
    <ProjectReference
        Include="..\Functorium.Adapters.SourceGenerator\Functorium.Adapters.SourceGenerator.csproj"
        OutputItemType="Analyzer"
        ReferenceOutputAssembly="false" />
  </ItemGroup>

</Project>
```

```
참조 속성 설명
=============

OutputItemType="Analyzer"
  → MSBuild가 이 참조를 분석기로 처리

ReferenceOutputAssembly="false"
  → 런타임 어셈블리 참조 제외
  → 컴파일 타임에만 소스 생성기 실행
```

### 테스트 프로젝트

```xml
<!-- Functorium.Tests.Unit.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- 소스 생성기 직접 참조 (테스트용) -->
    <ProjectReference
        Include="..\Functorium.Adapters.SourceGenerator\Functorium.Adapters.SourceGenerator.csproj" />

    <!-- 테스트 유틸리티 -->
    <ProjectReference
        Include="..\Functorium.Testing\Functorium.Testing.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Verify.Xunit" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>

</Project>
```

---

## 빌드 출력 확인

빌드 후 생성된 파일 확인:

```bash
# 빌드
dotnet build Functorium.Adapters.SourceGenerator.csproj

# 출력 확인
ls bin/Debug/netstandard2.0/
# Functorium.Adapters.SourceGenerator.dll
# Functorium.Adapters.SourceGenerator.pdb
```

---

## 요약

| 항목 | 설명 |
|------|------|
| `IsRoslynComponent` | IDE가 소스 생성기로 인식 |
| `PrivateAssets="all"` | Roslyn 패키지 전이 방지 |
| `OutputItemType="Analyzer"` | 프로젝트 참조 시 분석기로 처리 |
| 데이터 모델 | 불변 레코드로 정의 (PipelineClassInfo 등) |

---

## 다음 단계

다음 섹션에서는 소스 생성기 디버깅 환경을 설정합니다.

➡️ [03. 디버깅 설정](03-debugging-setup.md)
