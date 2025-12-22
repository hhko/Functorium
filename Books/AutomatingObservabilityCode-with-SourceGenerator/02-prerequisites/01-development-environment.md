# 개발 환경 설정

## 학습 목표

- .NET 10 SDK 설치 및 확인
- IDE 설정 (Visual Studio 2022 / VS Code)
- 필수 NuGet 패키지 이해

---

## .NET 10 SDK 설치

### Windows

```powershell
# winget을 사용한 설치
winget install Microsoft.DotNet.SDK.10

# 또는 공식 사이트에서 다운로드
# https://dotnet.microsoft.com/download/dotnet/10.0
```

### macOS

```bash
# Homebrew를 사용한 설치
brew install --cask dotnet-sdk

# 또는 공식 사이트에서 다운로드
```

### 설치 확인

```bash
dotnet --version
# 출력 예: 10.0.100

dotnet --list-sdks
# 출력 예: 10.0.100 [C:\Program Files\dotnet\sdk]
```

---

## IDE 설정

### Visual Studio 2022 (권장)

**최소 버전**: 17.12 이상

```
필수 워크로드
============

1. .NET 데스크톱 개발
2. ASP.NET 및 웹 개발 (선택)

설치 확인
=========
Visual Studio Installer → 수정 → 워크로드 확인
```

**소스 생성기 디버깅 설정:**

```
도구 → 옵션 → 텍스트 편집기 → C# → 고급

☑ "소스 생성기에서 생성된 파일 표시" 활성화
```

### VS Code

**필수 확장:**

```
확장 프로그램 설치
=================

1. C# Dev Kit (ms-dotnettools.csdevkit)
   - C# 언어 지원, IntelliSense, 디버깅

2. .NET Install Tool (ms-dotnettools.vscode-dotnet-runtime)
   - .NET SDK 버전 관리

3. EditorConfig for VS Code (editorconfig.editorconfig)
   - 코드 스타일 일관성
```

**settings.json 권장 설정:**

```json
{
  "dotnet.defaultSolution": "Functorium.sln",
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "csharp.semanticHighlighting.enabled": true
}
```

---

## 소스 생성기 프로젝트 생성

### 1. 클래스 라이브러리 프로젝트 생성

```bash
# 소스 생성기 프로젝트 생성
dotnet new classlib -n MySourceGenerator -f netstandard2.0

# 테스트 프로젝트 생성
dotnet new xunit -n MySourceGenerator.Tests -f net10.0
```

### 2. 필수 NuGet 패키지 설치

소스 생성기 프로젝트에 필요한 핵심 패키지:

```xml
<!-- MySourceGenerator.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- 소스 생성기는 반드시 netstandard2.0 타겟 -->
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>

    <!-- 소스 생성기 컴포넌트 표시 -->
    <IsRoslynComponent>true</IsRoslynComponent>

    <!-- 분석기로 패키지될 때 필요 -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>

    <!-- Nullable 참조 타입 활성화 -->
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Roslyn 코드 분석 API -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

### 3. 패키지 설명

| 패키지 | 용도 |
|--------|------|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# 컴파일러 API (Syntax, Semantic) |
| `Microsoft.CodeAnalysis.Analyzers` | 분석기 개발 규칙 검증 |

---

## netstandard2.0을 사용하는 이유

소스 생성기는 **컴파일러 확장**으로 동작하므로, 다양한 .NET 환경에서 실행되어야 합니다:

```
소스 생성기 실행 환경
====================

1. Visual Studio (Windows)
   - .NET Framework 4.7.2 기반

2. VS Code + OmniSharp
   - .NET Core 기반

3. dotnet CLI
   - .NET 8/10 기반

4. JetBrains Rider
   - 자체 런타임

→ 모든 환경에서 동작하려면 netstandard2.0 필수
```

**주의**: 소스 생성기 프로젝트 자체는 `netstandard2.0`이지만, **생성하는 코드**는 `net10.0` 문법을 사용할 수 있습니다.

```csharp
// 소스 생성기 (netstandard2.0에서 실행)
[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(...)
    {
        // 생성하는 코드는 C# 13 문법 사용 가능
        var code = """
            public class Generated
            {
                // C# 13 collection expressions
                public int[] Numbers => [1, 2, 3];
            }
            """;
    }
}
```

---

## 프로젝트 참조 설정

소스 생성기를 다른 프로젝트에서 사용하려면 특별한 참조 설정이 필요합니다:

```xml
<!-- 소스 생성기를 사용하는 프로젝트 -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 소스 생성기 프로젝트 참조 -->
    <ProjectReference Include="..\MySourceGenerator\MySourceGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>

</Project>
```

| 속성 | 설명 |
|------|------|
| `OutputItemType="Analyzer"` | 분석기/생성기로 인식 |
| `ReferenceOutputAssembly="false"` | 런타임 참조 제외 (컴파일 타임만 사용) |

---

## 프로젝트 구조 예시

```
MySolution/
├── MySolution.sln
├── src/
│   ├── MySourceGenerator/           # 소스 생성기 프로젝트
│   │   ├── MySourceGenerator.csproj # netstandard2.0
│   │   └── MyGenerator.cs
│   │
│   └── MyApp/                       # 소스 생성기 사용 프로젝트
│       ├── MyApp.csproj             # net10.0
│       └── Program.cs
│
└── tests/
    └── MySourceGenerator.Tests/     # 테스트 프로젝트
        ├── MySourceGenerator.Tests.csproj
        └── GeneratorTests.cs
```

---

## 요약

| 항목 | 값 |
|------|-----|
| .NET SDK | 10.0 이상 |
| IDE | Visual Studio 2022 (17.12+) 또는 VS Code |
| 생성기 타겟 프레임워크 | netstandard2.0 (필수) |
| 핵심 NuGet | Microsoft.CodeAnalysis.CSharp 4.12.0+ |
| 프로젝트 참조 | OutputItemType="Analyzer" |

---

## 다음 단계

다음 섹션에서는 소스 생성기 프로젝트의 상세 구조를 살펴봅니다.

➡️ [02. 프로젝트 구조](02-project-structure.md)
