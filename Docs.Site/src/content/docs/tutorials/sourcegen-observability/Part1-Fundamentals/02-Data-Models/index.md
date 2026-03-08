---
title: "프로젝트 구조"
---

## 개요

앞 장에서 .NET SDK와 IDE를 설정했습니다. 이제 소스 생성기 프로젝트의 내부 구조를 살펴볼 차례입니다.

소스 생성기의 csproj 파일에는 일반 라이브러리와 다른 고유한 속성들이 필요합니다. `IsRoslynComponent`, `PrivateAssets="all"`, `OutputItemType="Analyzer"` 같은 설정이 각각 어떤 역할을 하는지 이해하지 못하면, 빌드는 성공하지만 생성기가 전혀 동작하지 않는 상황에 빠질 수 있습니다. 이 장에서는 이러한 설정들의 의미를 하나씩 짚어보고, 실제 ObservablePortGenerator 프로젝트의 구조와 데이터 모델을 분석합니다.

## 학습 목표

### 핵심 학습 목표
1. **소스 생성기 프로젝트의 csproj 설정 이해**
   - `IsRoslynComponent`, `EnforceExtendedAnalyzerRules` 등 필수 속성의 역할
2. **`IsRoslynComponent`와 관련 속성의 역할 파악**
   - IDE 인식, 빌드 출력, NuGet 패키징에 미치는 영향
3. **실제 Functorium 프로젝트 구조 분석**
   - ObservablePortGenerator의 파일 구조와 데이터 모델 설계

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

    <!-- 6. 암시적 using 활성화 -->
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

### 속성 상세 설명

각 속성이 왜 필요한지 이해하는 것이 중요합니다. 특히 `IsRoslynComponent`와 `EnforceExtendedAnalyzerRules`는 소스 생성기 프로젝트에서만 사용되는 고유한 설정입니다.

| 속성 | 값 | 설명 |
|------|-----|------|
| `TargetFramework` | `netstandard2.0` | 모든 .NET 환경에서 실행 가능 |
| `LangVersion` | `latest` | C# 13 문법 사용 (생성기 코드에서) |
| `IsRoslynComponent` | `true` | IDE가 소스 생성기로 인식 |
| `EnforceExtendedAnalyzerRules` | `true` | 분석기 개발 모범 사례 강제 |
| `Nullable` | `enable` | null 안전성 검사 |
| `ImplicitUsings` | `enable` | 암시적 using 활성화 |

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

## 실제 프로젝트 분석: Functorium.SourceGenerators

### csproj 전체 구조

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- NuGet 패키지 설정 -->
  <PropertyGroup>
    <PackageId>Functorium.SourceGenerators</PackageId>
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
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <!-- NuGet 패키지 참조 -->
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                      PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers"
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
Functorium.SourceGenerators/
│
├── Functorium.SourceGenerators.csproj
│
├── Abstractions/
│   ├── Constants.cs                     # 공통 상수 (헤더 등)
│   └── Selectors.cs                     # 공통 선택자
│
└── Generators/
    ├── IncrementalGeneratorBase.cs      # 템플릿 메서드 패턴 기반 클래스
    │
    └── ObservablePortGenerator/        # 생성기별 클래스
        ├── ObservablePortGenerator.cs   # 메인 소스 생성기
        ├── ObservableGeneratorConstants.cs  # 생성기 전용 상수
        ├── ObservableClassInfo.cs       # 클래스 정보 레코드
        ├── MethodInfo.cs                # 메서드 정보
        ├── ParameterInfo.cs             # 파라미터 정보
        ├── TypeExtractor.cs             # 타입 추출 유틸리티
        ├── CollectionTypeHelper.cs      # 컬렉션 타입 판별
        ├── SymbolDisplayFormats.cs      # 타입 문자열 포맷
        ├── ConstructorParameterExtractor.cs  # 생성자 분석
        └── ParameterNameResolver.cs     # 이름 충돌 해결
```

---

## 데이터 모델 (레코드)

소스 생성기는 Roslyn API에서 추출한 정보를 코드 생성 단계까지 전달해야 합니다. 이를 위해 컴파일 타임에 수집한 클래스, 메서드, 파라미터 정보를 담는 불변 데이터 모델이 필요합니다. ObservablePortGenerator는 세 가지 핵심 레코드를 사용합니다.

### ObservableClassInfo

```csharp
using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// 파이프라인 생성에 필요한 클래스 정보
/// </summary>
public readonly record struct ObservableClassInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly List<MethodInfo> Methods;
    public readonly List<ParameterInfo> BaseConstructorParameters;
    public readonly Location? Location;                              // 진단 위치

    public static readonly ObservableClassInfo None = new(
        string.Empty, string.Empty, new List<MethodInfo>(), new List<ParameterInfo>(), null);

    public ObservableClassInfo(
        string @namespace,
        string className,
        List<MethodInfo> methods,
        List<ParameterInfo> baseConstructorParameters,
        Location? location)
    {
        Namespace = @namespace;
        ClassName = className;
        Methods = methods;
        BaseConstructorParameters = baseConstructorParameters;
        Location = location;
    }
}
```

### MethodInfo

```csharp
/// <summary>
/// 메서드 정보
/// </summary>
public class MethodInfo
{
    public string Name { get; }
    public List<ParameterInfo> Parameters { get; }
    public string ReturnType { get; }

    public MethodInfo(string name, List<ParameterInfo> parameters, string returnType)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
    }
}
```

### ParameterInfo

```csharp
/// <summary>
/// 파라미터 정보
/// </summary>
public class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public RefKind RefKind { get; }
    public bool IsCollection { get; }       // 컬렉션 타입 여부

    public ParameterInfo(string name, string type, RefKind refKind)
    {
        Name = name;
        Type = type;
        RefKind = refKind;
        IsCollection = CollectionTypeHelper.IsCollectionType(type);
    }
}
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
        Include="..\Functorium.SourceGenerators\Functorium.SourceGenerators.csproj"
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
        Include="..\Functorium.SourceGenerators\Functorium.SourceGenerators.csproj" />

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
dotnet build Functorium.SourceGenerators.csproj

# 출력 확인
ls bin/Debug/netstandard2.0/
# Functorium.SourceGenerators.dll
# Functorium.SourceGenerators.pdb
```

---

## 요약

소스 생성기 프로젝트는 csproj 설정과 프로젝트 참조 방식에서 일반 라이브러리와 근본적으로 다릅니다. `IsRoslynComponent`는 IDE 인식을, `PrivateAssets="all"`은 Roslyn 패키지의 전이적 의존성 차단을, `OutputItemType="Analyzer"`는 컴파일 타임 전용 참조를 각각 담당합니다. 데이터 모델은 `ObservableClassInfo`, `MethodInfo`, `ParameterInfo`의 불변 타입으로 설계하여 증분 빌드 파이프라인에서 안전하게 전달됩니다.

| 항목 | 설명 |
|------|------|
| `IsRoslynComponent` | IDE가 소스 생성기로 인식 |
| `PrivateAssets="all"` | Roslyn 패키지 전이 방지 |
| `OutputItemType="Analyzer"` | 프로젝트 참조 시 분석기로 처리 |
| 데이터 모델 | 불변 타입으로 정의 (ObservableClassInfo 등) |

---

## FAQ

### Q1: 데이터 모델을 왜 `record`나 `readonly record struct`로 정의해야 하나요?
**A**: Roslyn의 증분 파이프라인은 이전 실행 결과와 현재 결과를 `Equals`/`GetHashCode`로 비교하여 변경 여부를 판단합니다. `record`는 값 기반 동등성 비교를 자동 생성하므로, 데이터가 같으면 불필요한 코드 재생성을 건너뛸 수 있습니다.

### Q2: `IsRoslynComponent`와 `EnforceExtendedAnalyzerRules`는 각각 어떤 역할을 하나요?
**A**: `IsRoslynComponent`는 IDE(특히 Visual Studio)가 해당 프로젝트를 소스 생성기/분석기로 인식하여 실시간 피드백을 제공하도록 합니다. `EnforceExtendedAnalyzerRules`는 소스 생성기에서 허용되지 않는 API 사용(파일 시스템 접근 등)을 컴파일 오류로 잡아줍니다.

### Q3: `ObservableClassInfo`에 `Location?` 필드가 포함된 이유는 무엇인가요?
**A**: `Location`은 소스 생성기가 진단 메시지(경고, 오류)를 보고할 때 사용자에게 정확한 코드 위치를 알려주기 위해 필요합니다. 예를 들어 잘못된 사용 패턴을 감지했을 때, 해당 클래스 선언 위치에 경고를 표시할 수 있습니다.

---

## 다음 단계

프로젝트 구조와 데이터 모델을 이해했으니, 다음 장에서는 소스 생성기 개발에서 가장 까다로운 부분 중 하나인 디버깅 환경 설정을 다룹니다.

→ [03. 디버깅 설정](../03-Debugging-Setup/)
