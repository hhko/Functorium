---
title: "솔루션 구성 가이드"
---

이 문서는 새 솔루션을 만들 때 루트 레벨 구성 파일과 빌드 스크립트를 어떻게 생성하고 설정하는지를 다룹니다.

## Introduction

"여러 프로젝트의 패키지 버전이 제각각이어서 호환성 문제가 발생한 적은 없는가?"
"빌드 속성을 프로젝트마다 개별 설정하다가 일관성이 깨진 경험은 없는가?"
"새 팀원이 프로젝트를 클론한 후 빌드 환경을 맞추는 데 시간이 오래 걸리지 않았는가?"

일관된 빌드, 코드 품질, 패키지 관리를 위해서는 솔루션 수준의 중앙 설정이 필수적입니다. `Directory.Build.props`로 빌드 속성을 통일하고, `Directory.Packages.props`로 패키지 버전을 한 곳에서 관리하면 프로젝트 간 설정 불일치를 원천적으로 방지할 수 있습니다.

### What You Will Learn

This document covers the following topics:

1. **솔루션 파일(.slnx) 생성과 관리** - XML 기반 솔루션 파일의 구조와 프로젝트 추가 방법
2. **Directory.Build.props 구성** - 모든 프로젝트에 적용되는 공통 빌드 속성 설정
3. **Directory.Packages.props(CPM) 설정** - 중앙 집중식 패키지 버전 관리
4. **.editorconfig 코드 스타일 규칙** - 빌드 시 코드 품질 강제
5. **Build-Local.ps1 빌드 파이프라인** - 빌드, 테스트, 커버리지, 패키지 생성 자동화

> **솔루션 구성의 핵심은** 빌드 속성, 패키지 버전, 코드 스타일을 솔루션 루트에서 중앙 관리하여 모든 프로젝트의 일관성을 보장하는 것입니다.

## Summary

### Key Commands

```powershell
# 빌드 및 테스트
dotnet build Functorium.slnx
dotnet test --solution Functorium.slnx

# 전체 빌드 파이프라인 (빌드 + 테스트 + 커버리지 + 패키지)
./Build-Local.ps1

# 빌드 아티팩트 정리
./Build-Clean.ps1

# Verify 스냅샷 승인
./Build-VerifyAccept.ps1
```

### Key Procedures

**1. 새 솔루션 구성:**
1. Git 초기화 (`git init`, `.gitignore`, `.gitattributes`)
2. SDK 및 도구 (`global.json`, `dotnet new tool-manifest`)
3. 빌드 시스템 (`Directory.Build.props`, `Directory.Packages.props`)
4. 코드 품질 (`.editorconfig`, `nuget.config`)
5. 솔루션 파일 (`.slnx` 생성)

**2. 패키지 추가:**
1. `Directory.Packages.props`에 `<PackageVersion>` 추가
2. `.csproj`에 **Version 없이** `<PackageReference>` 추가

### Key Concepts

| Concept | Description |
|------|------|
| `.slnx` | XML 기반 솔루션 파일 (.NET 10+) |
| `Directory.Build.props` | 모든 프로젝트에 적용되는 공통 빌드 속성 (SDK import 전) |
| `Directory.Build.targets` | SDK 기본 항목 처리 후 적용되는 타겟 (Compile Remove 등) |
| `Directory.Packages.props` | 중앙 집중식 패키지 버전 관리 (CPM) |
| `Build-Local.ps1` | 10단계 빌드 파이프라인 (빌드 → 테스트 → 커버리지 → 패키지) |

---

## 개요

이 가이드는 새 솔루션을 만들 때 루트 레벨 구성 파일과 빌드 스크립트를 어떻게 생성하고 설정하는지를 다룹니다.
프로젝트 수준 파일(`AssemblyReference.cs`, `Using.cs`)은 [01-project-structure.md](./01-project-structure)를 참조하세요.

### 솔루션 루트에 필요한 파일

다음 테이블은 솔루션 루트에 배치해야 하는 파일의 전체 목록과 각 파일의 역할입니다.

| 파일 | 역할 | 생성 방법 |
|------|------|----------|
| `{Name}.slnx` | 솔루션 파일 | `dotnet new sln` 후 변환 또는 직접 작성 |
| `global.json` | SDK 버전 고정 + 테스트 러너 | `dotnet new globaljson` 후 수정 |
| `Directory.Build.props` | 공통 빌드 속성 | 직접 작성 |
| `Directory.Build.targets` | SDK 후처리 타겟 | 직접 작성 (필요 시) |
| `Directory.Packages.props` | 중앙 패키지 버전 관리 | 직접 작성 |
| `.editorconfig` | 코드 스타일 규칙 | `dotnet new editorconfig` 후 수정 |
| `.gitignore` | Git 제외 항목 | `dotnet new gitignore` 후 수정 |
| `.gitattributes` | 파일별 Git 속성 | 직접 작성 |
| `nuget.config` | NuGet 소스 설정 | `dotnet new nugetconfig` 후 수정 |
| `.config/dotnet-tools.json` | 로컬 .NET 도구 | `dotnet new tool-manifest` |

### 파일 로드/적용 순서

```
1. global.json           ← SDK 버전 결정 (dotnet 명령 실행 시)
2. nuget.config          ← 패키지 소스 결정 (restore 시)
3. Directory.Build.props  ← 프로젝트 공통 속성 (SDK import 전)
4. {project}.csproj       ← 개별 프로젝트 설정
5. Directory.Build.targets ← SDK 기본 항목 처리 후 타겟
6. Directory.Packages.props ← 패키지 버전 해석 (restore 시)
7. .editorconfig          ← 코드 스타일 적용 (빌드 + IDE)
```

## 솔루션 파일 (.slnx)

### .sln과 .slnx의 차이

| | `.sln` (레거시) | `.slnx` (신형) |
|---|---|---|
| 포맷 | 텍스트 기반 (독자 포맷) | XML 기반 |
| 가독성 | 낮음 (GUID 나열) | 높음 (Folder/Project 구조) |
| 수동 편집 | 어려움 | 용이 |
| 지원 SDK | 모든 버전 | .NET 10+ |

### 생성 방법

`dotnet new sln`은 기본적으로 `.sln`을 생성합니다. `.slnx`를 사용하려면 다음 두 가지 방법이 있습니다.

**방법 1: .sln 생성 후 변환**

```powershell
# 1. .sln 파일 생성
dotnet new sln -n MyApp

# 2. 프로젝트 추가
dotnet sln MyApp.sln add Src/MyApp/MyApp.csproj
dotnet sln MyApp.sln add Tests/MyApp.Tests.Unit/MyApp.Tests.Unit.csproj

# 3. .slnx로 변환 (dotnet CLI 내장)
dotnet sln MyApp.sln migrate
```

`dotnet sln migrate`는 `.sln`과 동일한 프로젝트 구성을 가진 `.slnx` 파일을 생성합니다. 변환 후 `.sln` 파일은 수동으로 삭제합니다.

**방법 2: .slnx 직접 작성**

```xml
<Solution>
  <Folder Name="/Src/">
    <Project Path="Src/MyApp/MyApp.csproj" />
  </Folder>
  <Folder Name="/Tests/">
    <Project Path="Tests/MyApp.Tests.Unit/MyApp.Tests.Unit.csproj" />
  </Folder>
</Solution>
```

직접 작성 시 `dotnet sln add` 명령은 사용할 수 없으므로, XML을 수동으로 편집합니다.

### .slnx 문법

```xml
<Solution>
  <!-- 솔루션 폴더: Name은 /로 시작/종료 -->
  <Folder Name="/Src/">
    <!-- 프로젝트: Path는 솔루션 파일 기준 상대 경로 -->
    <Project Path="Src/MyApp/MyApp.csproj" />
    <!-- Id는 선택 사항 (Visual Studio가 자동 생성) -->
    <Project Path="Src/MyApp.Domain/MyApp.Domain.csproj" Id="..." />
  </Folder>

  <!-- 중첩 폴더는 별도 Folder 요소로 선언 -->
  <Folder Name="/Tests.Hosts/" />
  <Folder Name="/Tests.Hosts/01-SingleHost/" />
  <Folder Name="/Tests.Hosts/01-SingleHost/Src/">
    <Project Path="Tests.Hosts/01-SingleHost/Src/MyHost/MyHost.csproj" />
  </Folder>
</Solution>
```

### 프로젝트 추가/제거

```powershell
# .slnx에 프로젝트 추가 (dotnet CLI 지원)
dotnet sln MyApp.slnx add Src/MyApp.Domain/MyApp.Domain.csproj

# 프로젝트 제거
dotnet sln MyApp.slnx remove Src/MyApp.Domain/MyApp.Domain.csproj

# 프로젝트 목록 확인
dotnet sln MyApp.slnx list
```

> `dotnet sln add`로 `.slnx`에 프로젝트를 추가하면 솔루션 폴더 없이 루트에 배치됩니다. 솔루션 폴더 구조가 필요하면 XML을 직접 편집하세요.

### 복수 솔루션 파일 구성

프로젝트가 많을 때 용도별로 솔루션 파일을 분리합니다.

| 솔루션 | 포함 프로젝트 | 용도 |
|--------|--------------|------|
| `{Name}.slnx` | Src/, Tests/ | 핵심 라이브러리 개발 (기본) |
| `{Name}.All.slnx` | 전체 프로젝트 | Tutorials, Books 등 포함 전체 빌드 |

### 빌드/테스트 명령

```powershell
dotnet build MyApp.slnx
dotnet test --solution MyApp.slnx
```

> `dotnet test`에 솔루션을 지정하려면 `--solution` 옵션을 사용합니다 (`--project`는 단일 프로젝트용).

솔루션 파일이 프로젝트를 묶는다면, `global.json`은 SDK 버전과 테스트 러너를 결정합니다.

## global.json

### 생성 방법

```powershell
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
```

생성된 파일에 `test` 섹션을 수동으로 추가합니다.

### 설정 내용

```json
{
  "sdk": {
    "rollForward": "latestFeature",
    "version": "10.0.100"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### 속성 설명

| 속성 | 설명 |
|------|------|
| `sdk.version` | 최소 요구 SDK 버전. `dotnet --version`으로 현재 버전 확인 |
| `sdk.rollForward` | SDK 버전 매칭 정책 |
| `test.runner` | 테스트 러너. `Directory.Build.props`의 `UseMicrosoftTestingPlatformRunner`와 함께 사용 |

### rollForward 정책 선택

| 정책 | 동작 | 사용 시기 |
|------|------|----------|
| `latestFeature` | 같은 major.minor 내 최신 feature band (권장) | CI/로컬 SDK 차이 허용 |
| `latestPatch` | 같은 feature band 내 최신 패치 | 엄격한 SDK 고정 |
| `latestMajor` | 최신 SDK 사용 | 버전 유연성 최대 |

### rollForward 정책 상세 비교

다음 테이블은 `version: 10.0.100` 기준으로 각 정책이 허용하는 SDK 버전 범위를 비교합니다.

| 정책 | 설명 | 허용 예시 | 거부 예시 |
|------|------|----------|----------|
| `patch` | 동일 major.minor 내 최신 patch | `10.0.102` | `10.1.x` |
| `feature` / `minor` | 동일 major 내 최신 minor | `10.1.x` | `11.x.x` |
| `major` | 최신 major까지 허용 | `11.x.x` | — |
| `latestPatch` | 최신 patch 버전 사용 | `10.0.x` 중 최신 | `10.1.x` |
| `latestFeature` / `latestMinor` | 최신 feature 버전 사용 | `10.x.x` 중 최신 | `11.x.x` |
| `latestMajor` | 설치된 SDK 중 최신 | 모든 버전 | — |
| `disable` | 정확한 버전만 허용 | `10.0.100`만 | 그 외 전부 |

### 환경별 권장 정책

| 환경 | 권장 정책 | 이유 |
|------|----------|------|
| 개발/테스트 | `latestFeature` | 최신 기능 활용, 보안 패치 자동 적용 |
| 프로덕션/CI | `patch` | 안정성 우선 |
| 라이브러리 | `patch` | 호환성 유지 |
| 실험 프로젝트 | `latestMajor` | 최신 버전 체험 |

### SDK 업그레이드 절차

```powershell
# 1. 설치된 SDK 확인
dotnet --list-sdks

# 2. global.json의 version 필드 업데이트
# 예: "version": "10.0.200"

# 3. 적용된 버전 확인
dotnet --version

# 4. 빌드 및 테스트 검증
dotnet build
dotnet test --solution Functorium.slnx

# 5. 커밋
git add global.json
git commit -m "build: SDK 버전을 10.0.200으로 업그레이드"
```

### CI/CD에서 global.json 활용

```yaml
# GitHub Actions 예시
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    global-json-file: global.json  # global.json 자동 인식
```

SDK 버전이 결정되었으면, 다음으로 모든 프로젝트에 적용되는 공통 빌드 속성을 설정합니다.

## Directory.Build.props

### 생성 방법

솔루션 루트에 `Directory.Build.props` 파일을 직접 생성합니다. MSBuild가 프로젝트 파일 평가 전 디렉토리 트리를 상향 탐색하여 자동으로 찾습니다.

### 기본 템플릿

필수 속성만 포함한 최소 구성입니다.

```xml
<Project>
  <PropertyGroup>
    <!-- Target Framework -->
    <TargetFramework>net10.0</TargetFramework>

    <!-- Language Features -->
    <LangVersion>14</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Code Quality -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### NuGet 패키지 메타데이터 추가

NuGet 패키지를 배포하는 프로젝트가 있으면 공통 메타데이터를 추가합니다.

```xml
  <!-- NuGet Package Common Settings -->
  <PropertyGroup>
    <Authors>{이름}</Authors>
    <Company>{회사}</Company>
    <Copyright>Copyright (c) {회사} Contributors. All rights reserved.</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/{owner}/{repo}</PackageProjectUrl>
    <RepositoryUrl>https://github.com/{owner}/{repo}.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>{icon}.png</PackageIcon>
    <PackageTags>{tag1};{tag2}</PackageTags>

    <!-- Symbol Package for Debugging -->
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>

    <!-- Source Link for Debugging -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>

    <!-- Deterministic Build (CI 환경에서만 활성화) -->
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
```

### Microsoft Testing Platform (MTP) 설정 추가

테스트 프로젝트에서 MTP를 사용하려면 다음 섹션을 추가합니다.

```xml
  <!-- Microsoft Testing Platform -->
  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>
```

> `IsTestProject`는 테스트 SDK 참조가 있는 프로젝트에서 자동으로 `true`가 됩니다.

### Source Link 패키지 추가

GitHub 호스팅 프로젝트에서 Source Link를 활성화하려면 모든 프로젝트에 패키지를 추가합니다.

```xml
  <!-- Source Link Package -->
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
  </ItemGroup>
```

### 섹션별 요약

| 섹션 | 주요 속성 | 필수 여부 |
|------|----------|----------|
| Target Framework / Language | `TargetFramework`, `LangVersion`, `Nullable` | 필수 |
| Code Quality | `EnforceCodeStyleInBuild` | 권장 |
| NuGet Metadata | `Authors`, `License`, `RepositoryUrl` 등 | NuGet 배포 시 |
| Symbol/Source Link | `IncludeSymbols`, `PublishRepositoryUrl` | NuGet 배포 시 |
| Deterministic Build | `ContinuousIntegrationBuild` | CI 환경 시 |
| Testing (MTP) | `OutputType Exe`, `UseMicrosoftTestingPlatformRunner` | MTP 사용 시 |
| Source Link Package | `Microsoft.SourceLink.GitHub` | Source Link 사용 시 |

<details>
<summary>현재 Functorium의 Directory.Build.props 전체</summary>

```xml
<Project>
  <!-- See https://aka.ms/dotnet/msbuild/customize for more details on customizing your build -->
  <PropertyGroup>
    <!-- Target Framework -->
    <TargetFramework>net10.0</TargetFramework>

    <!-- Language Features -->
    <LangVersion>14</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Code Quality -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <!-- NuGet Package Common Settings -->
  <PropertyGroup>
    <Authors>고형호</Authors>
    <Company>Functorium</Company>
    <Copyright>Copyright (c) Functorium Contributors. All rights reserved.</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/hhko/Functorium</PackageProjectUrl>
    <RepositoryUrl>https://github.com/hhko/Functorium.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>Functorium.png</PackageIcon>
    <PackageTags>functorium;functional;dotnet;csharp</PackageTags>

    <!-- Symbol Package for Debugging -->
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>

    <!-- Source Link for Debugging -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>

    <!-- Deterministic Build -->
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>

  <!-- Microsoft Testing Platform -->
  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <!-- Source Link Package -->
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
  </ItemGroup>

  <!-- Versioning with MinVer -->
  <ItemGroup>
    <PackageReference Include="MinVer">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
    <MinVerVerbosity>minimal</MinVerVerbosity>
    <MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
    <MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
    <MinVerAutoIncrement>patch</MinVerAutoIncrement>
    <MinVerWorkingDirectory>$(MSBuildThisFileDirectory)</MinVerWorkingDirectory>
  </PropertyGroup>

  <Target Name="SetAssemblyVersion" AfterTargets="MinVer">
    <PropertyGroup>
      <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
    </PropertyGroup>
  </Target>
</Project>
```

</details>

## Directory.Build.targets

### 역할

SDK 기본 항목 처리 **후에** 적용되는 타겟 파일입니다. `Compile` 항목에서 특정 파일을 제외할 때 사용합니다.

### props와 targets의 차이

| | `Directory.Build.props` | `Directory.Build.targets` |
|---|---|---|
| 적용 시점 | SDK import **전** | SDK import **후** |
| 용도 | 속성(Property) 설정 | 기본 항목(Item) 수정 |
| 예시 | `TargetFramework`, `Nullable` | `Compile Remove`, 조건부 항목 제거 |

### 왜 targets에서 제거해야 하는가

SDK는 props 처리 후 `**/*.cs`를 자동으로 `Compile` 항목에 추가합니다. props에서 `<Compile Remove="...">`를 해도 SDK가 다시 추가하므로, targets에서 제거해야 효과가 있습니다.

### 생성 방법

솔루션 루트에 `Directory.Build.targets` 파일을 직접 생성합니다. 필요할 때만 만들면 됩니다.

**PublicApiGenerator 사용 시:**

```xml
<Project>
  <!-- Exclude Public API files from compilation (generated by PublicApiGenerator) -->
  <!-- This must be in targets (not props) because SDK adds default items after props are processed -->
  <ItemGroup>
    <Compile Remove=".api\**\*.cs" />
    <None Include=".api\**\*.cs" />
  </ItemGroup>
</Project>
```

> PublicApiGenerator를 사용하지 않으면 이 파일은 불필요합니다.

빌드 속성이 통일되었으면, 다음으로 패키지 버전을 한 곳에서 관리하는 CPM을 설정합니다.

## Directory.Packages.props

### 역할

중앙 집중식 패키지 버전 관리(Central Package Management, CPM)를 활성화합니다. 모든 프로젝트의 패키지 버전을 한 곳에서 관리합니다.

### 생성 방법

솔루션 루트에 `Directory.Packages.props` 파일을 직접 생성합니다.

### 기본 템플릿

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <!-- Label로 카테고리를 구분하여 관리합니다 -->
  <ItemGroup Label="Source Link">
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup Label="Basic">
    <!-- 프로젝트에서 사용할 패키지 버전을 여기에 추가 -->
  </ItemGroup>

  <ItemGroup Label="Testing">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageVersion Include="xunit.v3" Version="3.2.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <!-- 추가 테스트 패키지 -->
  </ItemGroup>
</Project>
```

### 패키지 추가 절차

1. `Directory.Packages.props`에 적절한 Label 그룹에 `<PackageVersion>` 추가:
   ```xml
   <ItemGroup Label="Basic">
     <PackageVersion Include="NewPackage" Version="1.0.0" />
   </ItemGroup>
   ```

2. 사용할 프로젝트 `.csproj`에 **Version 없이** `<PackageReference>` 추가:
   ```xml
   <ItemGroup>
     <PackageReference Include="NewPackage" />
   </ItemGroup>
   ```

> CPM 활성화 시 csproj에서 `Version`을 지정하면 빌드 오류가 발생합니다. 버전은 반드시 `Directory.Packages.props`에서만 관리합니다.

### 버전 업데이트

`Directory.Packages.props`의 `Version` 속성만 수정하면 해당 패키지를 참조하는 모든 프로젝트에 일괄 적용됩니다.

### Label 카테고리 구성 예시

다음 테이블은 `Label` 속성으로 패키지를 분류하는 구성 예시입니다.

| Label | 용도 | 대표 패키지 |
|-------|------|------------|
| Source Link | 소스 링크 디버깅 | `Microsoft.SourceLink.GitHub` |
| API Generation | Public API 표면 생성 | `PublicApiGenerator` |
| Source Generator | 소스 생성기 개발 | `Microsoft.CodeAnalysis.CSharp` |
| Basic | 핵심 라이브러리 | `LanguageExt.Core`, `Mediator.*`, `FluentValidation` |
| Observability | 로깅/메트릭/트레이싱 | `Serilog.*`, `OpenTelemetry.*` |
| WebApi | HTTP API | `FastEndpoints`, `Swashbuckle.AspNetCore` |
| Versioning | 버전 관리 | `MinVer` |
| ORM | 데이터 액세스 | `Dapper`, `Microsoft.EntityFrameworkCore.*` |
| Scheduling | 작업 스케줄링 | `Quartz` |
| Testing | 테스트 프레임워크 | `xunit.v3`, `Shouldly`, `NSubstitute` |

<details>
<summary>현재 Functorium의 Directory.Packages.props 전체</summary>

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Label="Versioning">
    <PackageVersion Include="MinVer" Version="6.0.0" />
  </ItemGroup>
  <ItemGroup Label="Source Link">
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup Label="API Generation">
    <PackageVersion Include="PublicApiGenerator" Version="11.5.0" />
    <PackageVersion Include="System.Reflection.MetadataLoadContext" Version="9.0.1" />
  </ItemGroup>
  <ItemGroup Label="Source Generator">
    <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
    <PackageVersion Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0" />
  </ItemGroup>
  <ItemGroup Label="Basic">
    <PackageVersion Include="LanguageExt.Core" Version="5.0.0-beta-77" />
    <PackageVersion Include="Ulid" Version="1.3.4" />
    <PackageVersion Include="Mediator.Abstractions" Version="3.0.1" />
    <PackageVersion Include="Mediator.SourceGenerator" Version="3.0.1" />
    <PackageVersion Include="FluentValidation" Version="12.1.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.0" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageVersion Include="WolverineFx" Version="5.9.2" />
    <PackageVersion Include="WolverineFx.RabbitMQ" Version="5.9.2" />
    <PackageVersion Include="Scrutor" Version="7.0.0" />
  </ItemGroup>
  <ItemGroup Label="Observability">
    <PackageVersion Include="System.Diagnostics.DiagnosticSource" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Diagnostics" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Console" Version="10.0.0" />
    <PackageVersion Include="Serilog" Version="4.3.0" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="9.0.0" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="10.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.0.1" />
    <PackageVersion Include="Serilog.Enrichers.Process" Version="3.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Thread" Version="4.0.0" />
    <PackageVersion Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
    <PackageVersion Include="OpenTelemetry" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Exporter.Console" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.11.0-beta.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.11.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Runtime" Version="1.11.1" />
    <PackageVersion Include="Ardalis.SmartEnum" Version="8.2.0" />
  </ItemGroup>
  <ItemGroup Label="WebApi">
    <PackageVersion Include="FastEndpoints" Version="7.1.1" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.2" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="10.1.0" />
  </ItemGroup>
  <ItemGroup Label="ORM">
    <PackageVersion Include="Dapper" Version="2.1.66" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="Scheduling">
    <PackageVersion Include="Quartz" Version="3.15.1" />
  </ItemGroup>
  <ItemGroup Label="Testing">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageVersion Include="Microsoft.Testing.Extensions.CodeCoverage" Version="18.0.4" />
    <PackageVersion Include="Microsoft.Testing.Extensions.TrxReport" Version="1.8.4" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="Verify.XunitV3" Version="31.8.0" />
    <PackageVersion Include="xunit.v3" Version="3.2.1" />
    <PackageVersion Include="xunit.v3.extensibility.core" Version="3.2.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageVersion Include="TngTech.ArchUnitNET.xUnitV3" Version="0.13.1" />
    <PackageVersion Include="TngTech.ArchUnitNET" Version="0.13.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageVersion Include="BenchmarkDotNet" Version="0.15.8" />
  </ItemGroup>
</Project>
```

</details>

패키지 버전 관리가 구성되었으면, 다음으로 코드 스타일과 포맷팅 규칙을 설정합니다.

## .editorconfig

### 생성 방법

```powershell
dotnet new editorconfig
```

이 명령은 .NET SDK 기본 규칙이 모두 포함된 `.editorconfig`를 생성합니다. 생성 후 프로젝트에 맞게 수정합니다.

### 필수 설정

```ini
root = true

# All files
[*]
indent_style = space

# Document files (XML, JSON, props, slnx, csproj, Markdown, config, PowerShell)
[*.{xml,json,props,sln,slnx,csproj,md,config,ps1}]
indent_size = 2

# C# files
[*.cs]
indent_size = 4
tab_width = 4
insert_final_newline = false
```

### Verify 스냅샷 설정 추가 (Verify.Xunit 사용 시)

```ini
# Verify settings
[*.{received,verified}.{json,txt,xml}]
charset = utf-8-bom
end_of_line = lf
indent_size = unset
indent_style = unset
insert_final_newline = false
tab_width = unset
trim_trailing_whitespace = false
```

### file-scoped namespace 강제 (권장)

```ini
[*.{cs,vb}]
# 네임스페이스 선언 기본 설정 (IDE0161)
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0161.severity = warning
```

### 기본값 사용 전략

대부분의 .NET 코딩 규칙(using 정렬, 명명 규칙, 포맷팅 등)은 주석 처리하여 SDK 기본값을 사용합니다. 명시적으로 활성화할 규칙만 주석을 해제하세요. `dotnet new editorconfig`로 생성하면 모든 규칙이 주석 포함으로 나오므로, 원하는 규칙만 주석을 해제하는 방식이 편리합니다.

### 코드 스타일 규칙과 진단 규칙

`csharp_style_namespace_declarations`와 `dotnet_diagnostic.IDE0161.severity`는 모두 네임스페이스 스타일을 제어하지만 역할이 다릅니다.

| 항목 | `csharp_style_namespace_declarations` | `dotnet_diagnostic.IDE0161.severity` |
|------|--------------------------------------|--------------------------------------|
| **유형** | 코드 스타일 규칙 | 진단 규칙 |
| **역할** | 선호 스타일 + 심각도 정의 | 심각도만 정의 |
| **형식** | `값:심각도` (예: `file_scoped:warning`) | `심각도` (예: `warning`) |
| **우선순위** | 낮음 | 높음 (재정의 가능) |

둘 다 함께 사용하면 스타일 정의와 빌드 심각도를 명시적으로 강제할 수 있습니다.

### 심각도 수준

| 수준 | IDE 표시 | 빌드 영향 |
|------|----------|----------|
| `none` | 표시 안 함 | 영향 없음 |
| `silent` | 흐리게 표시 | 영향 없음 |
| `suggestion` | 점선 표시 | 영향 없음 |
| `warning` | 물결선 표시 | 경고 발생 |
| `error` | 빨간 표시 | 빌드 실패 |

### 빌드 시 코드 분석 활성화

`Directory.Build.props`의 `EnforceCodeStyleInBuild`와 `.editorconfig` 규칙이 함께 동작합니다.

| 설정 | IDE | 빌드 |
|------|-----|------|
| `EnforceCodeStyleInBuild = false` (기본) | 실시간 경고 | 무시 |
| `EnforceCodeStyleInBuild = true` | 실시간 경고 | 빌드 경고 |

### 코드 품질 검증 워크플로우

```powershell
# 기본 빌드 (증분)
dotnet build

# 설정 변경 후 (캐시 무시)
dotnet build --no-incremental

# 완전히 새로 빌드
dotnet clean && dotnet build --no-incremental

# 경고를 오류로 처리 (CI 환경)
dotnet build /p:TreatWarningsAsErrors=true
```

> `.editorconfig`나 `Directory.Build.props`를 변경한 후에는 반드시 `--no-incremental` 옵션을 사용하세요. 증분 빌드는 설정 변경을 감지하지 못합니다.

### 카테고리별 규칙 일괄 활성화

```ini
# 모든 스타일 규칙
dotnet_analyzer_diagnostic.category-Style.severity = warning

# 모든 성능 규칙
dotnet_analyzer_diagnostic.category-Performance.severity = warning

# 모든 보안 규칙
dotnet_analyzer_diagnostic.category-Security.severity = error
```

> 처음부터 모두 활성화하면 경고가 많을 수 있습니다. 점진적으로 적용하세요.

## .gitignore / .gitattributes

### .gitignore 생성

```powershell
dotnet new gitignore
```

이 명령은 Visual Studio/dotnet 표준 `.gitignore`를 생성합니다. 생성 후 프로젝트에 맞는 항목을 추가합니다.

**추가할 항목:**

```gitignore
# Verify
*.received.*

# Local NuGet output directory
.nupkg/

# Coverage
.coverage/reports/

# Environment files
*.env
```

### .gitignore 주요 카테고리

| 카테고리 | 패턴 | 설명 |
|---------|------|------|
| 빌드 결과 | `[Dd]ebug/`, `[Rr]elease/`, `[Oo]bj/`, `**/[Bb]in/*` | 빌드 아티팩트 |
| NuGet | `*.nupkg`, `*.snupkg`, `**/[Pp]ackages/*` | 패키지 파일 |
| 테스트 결과 | `[Tt]est[Rr]esult*/`, `*.trx` | 테스트 리포트 |
| 커버리지 | `coverage*.json`, `coverage*.xml`, `.coverage/reports/` | 코드 커버리지 |
| Verify | `*.received.*` | Verify 스냅샷 중간 파일 |
| IDE | `.vs/`, `.vscode/*` | Visual Studio/VS Code 설정 |

### .gitattributes 생성 (Verify.Xunit 사용 시)

Verify 스냅샷 파일의 줄바꿈과 인코딩을 강제하는 `.gitattributes`를 직접 생성합니다.

```
*.verified.txt text eol=lf working-tree-encoding=UTF-8
*.verified.xml text eol=lf working-tree-encoding=UTF-8
*.verified.json text eol=lf working-tree-encoding=UTF-8
*.verified.bin binary
```

**필요 이유:** Verify 스냅샷은 OS에 관계없이 동일한 내용이어야 합니다. Windows의 CRLF가 섞이면 불필요한 diff가 발생하므로 LF를 강제합니다.

> Verify.Xunit을 사용하지 않으면 `.gitattributes`는 불필요합니다.

## nuget.config

### 생성 방법

```powershell
dotnet new nugetconfig
```

생성된 파일을 다음과 같이 수정합니다.

### 설정 내용

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### `<clear />`를 추가하는 이유

`<clear />`는 시스템/사용자 레벨에서 설정된 모든 NuGet 소스를 제거하고, 이 파일에 명시된 소스만 사용합니다.

- 예측 가능한 패키지 해석 (어떤 환경에서든 동일한 소스)
- 의도치 않은 사설 피드에서 패키지가 해석되는 것을 방지

> `dotnet new nugetconfig`으로 생성하면 `<clear />`가 포함되지 않으므로 수동으로 추가해야 합니다.

### 사설 피드 추가

```xml
<packageSources>
  <clear />
  <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  <add key="private" value="https://pkgs.example.com/nuget/v3/index.json" />
</packageSources>
```

## .config/dotnet-tools.json

### 생성 방법

```powershell
dotnet new tool-manifest
```

이 명령은 `.config/dotnet-tools.json` 매니페스트 파일을 생성합니다.

### 도구 설치

```powershell
# 코드 커버리지 리포트 생성 도구
dotnet tool install dotnet-reportgenerator-globaltool

# Verify 스냅샷 관리 도구
dotnet tool install verify.tool
```

설치하면 매니페스트에 자동으로 등록됩니다.

### 도구 복원 (클론 후)

```powershell
dotnet tool restore
```

> `Build-Local.ps1`은 실행 시 자동으로 `dotnet tool restore`를 수행합니다.

### 설치된 도구 목록

| 도구 | 명령 | 용도 |
|------|------|------|
| `dotnet-reportgenerator-globaltool` | `reportgenerator` | 코드 커버리지 HTML 리포트 생성 |
| `verify.tool` | `dotnet-verify` | Verify 스냅샷 관리 (accept/reject) |
| `gman.siren` | `siren-gen` | EF Core DbContext → Mermaid ER 다이어그램 생성 |

> 각 도구의 상세 사용법(파라미터, 실행 예시)은 [03-dotnet-tools.md](./03-dotnet-tools)를 참조하세요.

### 새 도구 추가/업데이트

```powershell
# 새 도구 추가 (매니페스트에 자동 등록)
dotnet tool install <package-name>

# 도구 업데이트
dotnet tool update <package-name>

# 도구 제거
dotnet tool uninstall <package-name>
```

구성 파일 설정이 완료되었으면, 마지막으로 빌드 파이프라인을 자동화하는 스크립트를 살펴봅니다.

## 빌드 스크립트

### 스크립트 목록

다음 테이블은 프로젝트에서 제공하는 빌드 스크립트의 전체 목록입니다.

| 스크립트 | 역할 | 주요 파라미터 |
|----------|------|-------------|
| `Build-Local.ps1` | 빌드, 테스트, 커버리지, NuGet 패키지 | `-Solution`, `-SkipPack`, `-SlowTestThreshold` |
| `Build-Clean.ps1` | bin/obj 폴더 삭제 | `-Help` |
| `Build-VerifyAccept.ps1` | Verify 스냅샷 일괄 승인 | `-Help` |
| `Build-CleanRunFileCache.ps1` | .NET 10 runfile 캐시 정리 | `-Pattern`, `-WhatIf` |
| `Build-SetAsSetupProject.ps1` | Tests.Hosts 프로젝트 Setup 설정 | — |
| `Build-ERDiagram.ps1` | EF Core DbContext → Mermaid ER 다이어그램 생성 | — |

### Build-Local.ps1

전체 빌드 파이프라인을 10단계로 실행합니다.

| 단계 | 작업 | 설명 |
|------|------|------|
| 1 | 도구 복원 | `dotnet tool restore` |
| 2 | 솔루션 검색 | `-Solution` 파라미터 또는 자동 검색 |
| 3 | 빌드 | `dotnet build -c Release` |
| 4 | 버전 정보 | 빌드된 DLL의 ProductVer, FileVer, Assembly 출력 |
| 5 | 테스트 + 커버리지 | `dotnet test` + MTP 코드 커버리지 수집 |
| 6 | 커버리지 병합 | 여러 테스트 프로젝트의 커버리지 파일 수집 |
| 7 | HTML 리포트 | ReportGenerator로 HTML + Cobertura + Markdown 리포트 생성 |
| 8 | 커버리지 출력 | Project 커버리지 + Full 커버리지 콘솔 출력 |
| 9 | 느린 테스트 분석 | 지정 임계값 초과 테스트 리포트 생성 |
| 10 | NuGet 패키지 | `dotnet pack` (Src/ 내 프로젝트) |

**주요 파라미터:**

| 파라미터 | 별칭 | 기본값 | 설명 |
|---------|------|--------|------|
| `-Solution` | `-s` | `Functorium.slnx` | 솔루션 파일 경로 |
| `-ProjectPrefix` | `-p` | `Functorium` | 커버리지 필터링 접두사 |
| `-SkipPack` | — | `$false` | NuGet 패키지 생성 건너뛰기 |
| `-SlowTestThreshold` | `-t` | `30` | 느린 테스트 판단 기준 (초) |

**출력 디렉토리:**

```
{SolutionDir}/
├── .coverage/reports/              ← HTML 리포트, 병합 커버리지 (Cobertura.xml)
├── .nupkg/                         ← NuGet 패키지 (.nupkg, .snupkg)
└── Tests/
    └── {TestProject}/
        └── TestResults/
            ├── {GUID}/
            │   └── coverage.cobertura.xml  ← 원본 커버리지
            └── *.trx                       ← 테스트 결과
```

**커버리지 분류 (콘솔 출력):**

| 분류 | 포함 패턴 | 설명 |
|------|-----------|------|
| Project Coverage | `{Prefix}.*` | 지정된 접두사로 시작하는 프로젝트 |
| Full Coverage | 전체 (테스트 제외) | 모든 프로덕션 코드 |

**사용 예시:**

```powershell
# 기본 실행 (빌드 + 테스트 + 패키지)
./Build-Local.ps1

# 전체 솔루션 빌드
./Build-Local.ps1 -s Functorium.All.slnx

# 패키지 생성 건너뛰기
./Build-Local.ps1 -SkipPack

# 느린 테스트 임계값 변경
./Build-Local.ps1 -t 60
```

### Build-Clean.ps1

모든 프로젝트의 `bin/` 및 `obj/` 폴더를 일괄 삭제합니다.

```powershell
./Build-Clean.ps1
```

**사용 시기:**
- 빌드 아티팩트를 완전히 초기화하고 싶을 때
- 빌드 오류가 캐시된 바이너리로 인해 발생할 때
- 브랜치 전환 후 이전 빌드 결과물을 정리할 때

### Build-VerifyAccept.ps1

Verify.Xunit 스냅샷 테스트 결과를 일괄 승인합니다.

```powershell
./Build-VerifyAccept.ps1
```

**사용 시기:**
- 테스트 실행 후 `*.received.*` 파일이 생성되어 pending 상태의 스냅샷이 있을 때
- 의도적으로 출력이 변경되어 새 스냅샷을 승인해야 할 때

**동작 과정:**
1. `dotnet tool restore`로 `verify.tool` 복원
2. `dotnet verify accept -y`로 모든 pending 스냅샷 승인

### Build-CleanRunFileCache.ps1

.NET 10 파일 기반 프로그램(`.cs` 직접 실행)의 캐시를 정리합니다.

```powershell
# SummarizeSlowestTests 캐시만 정리 (기본)
./Build-CleanRunFileCache.ps1

# 모든 runfile 캐시 정리
./Build-CleanRunFileCache.ps1 -Pattern "All"

# 삭제 대상만 확인 (실제 삭제 안 함)
./Build-CleanRunFileCache.ps1 -WhatIf
```

**사용 시기:** `System.CommandLine` 등 패키지 로딩 오류가 발생할 때. 캐시 위치는 `%TEMP%\dotnet\runfile\`입니다.

| 파라미터 | 기본값 | 설명 |
|---------|--------|------|
| `-Pattern` | `SummarizeSlowestTests` | 삭제할 캐시 패턴 (`All`이면 전체) |
| `-WhatIf` | — | 삭제 대상만 표시 |

## 중첩 구성 파일

### 부모 import 차단이 필요한 경우

.NET 10 파일 기반 프로그램(runfile)처럼 독립 실행되는 `.cs` 파일이 포함된 하위 폴더에서는 루트 `Directory.Build.props`의 설정(Source Link 패키지 등)이 불필요하거나 오류를 유발할 수 있습니다.

### 차단 방법

해당 폴더에 자체 `Directory.Build.props`를 배치합니다. MSBuild는 가장 가까운 `Directory.Build.props`만 적용하므로, 상위 파일을 자동으로 import하지 않습니다.

```xml
<Project>
  <!-- DO NOT import parent Directory.Build.props to avoid SourceLink dependencies -->
  <!-- This folder contains file-based programs that should be self-contained -->

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Note: ManagePackageVersionsCentrally is set in Directory.Packages.props -->
</Project>
```

> 현재 `.coverage/scripts/`와 `.release-notes/scripts/`에 이 패턴이 적용되어 있습니다.

### 반대로 부모를 상속하고 싶을 때

하위 `Directory.Build.props`에서 상위 파일도 함께 적용하려면 명시적으로 import합니다.

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

  <!-- 추가 설정 -->
</Project>
```

## 새 솔루션 구성 체크리스트

새 솔루션을 만들 때 다음 순서로 파일을 생성합니다.

1. **Git 초기화**
   - [ ] `git init`
   - [ ] `dotnet new gitignore` → 프로젝트별 항목 추가
   - [ ] `.gitattributes` 생성 (Verify 사용 시)

2. **SDK 및 도구 설정**
   - [ ] `dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature` → `test` 섹션 추가
   - [ ] `dotnet new tool-manifest` → 필요한 도구 설치

3. **빌드 시스템 구성**
   - [ ] `Directory.Build.props` 생성 (기본 템플릿 + 필요한 섹션)
   - [ ] `Directory.Build.targets` 생성 (필요 시)
   - [ ] `Directory.Packages.props` 생성 (CPM 활성화 + 패키지 추가)

4. **코드 품질 설정**
   - [ ] `dotnet new editorconfig` → 필요한 규칙만 활성화
   - [ ] `dotnet new nugetconfig` → `<clear />` 추가

5. **솔루션 파일 생성**
   - [ ] `dotnet new sln -n {Name}` → `dotnet sln migrate`로 `.slnx` 변환
   - [ ] 또는 `.slnx` 직접 작성
   - [ ] 프로젝트 추가 (`dotnet sln add` 또는 XML 편집)

## PowerShell 스크립트 개발 표준

### Requirements 및 구조

- PowerShell 7.0 이상 (`#Requires -Version 7.0`)
- 각 스크립트는 자체 완결형으로, 필요한 헬퍼 함수를 `#region Helpers` 블록에 직접 포함

### 파일 명명 규칙

| 유형 | 패턴 | 예시 |
|------|------|------|
| 빌드 스크립트 | `Build-*.ps1` | `Build-Local.ps1` |
| 배포 스크립트 | `Deploy-*.ps1` | `Deploy-Production.ps1` |
| 유틸리티 스크립트 | `Invoke-*.ps1` | `Invoke-Migration.ps1` |

### 필수 설정

모든 스크립트는 다음 설정으로 시작합니다.

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```

### 코딩 규칙

**함수 명명:** `Get-`, `Set-`, `New-`, `Remove-`, `Invoke-`, `Test-`, `Show-`, `Write-` 접두사 사용

**변수 명명:** 스크립트 전역은 `$script:TOTAL_STEPS` (대문자), 함수 로컬은 `$result`

**코드 구조:** `#region`으로 Constants, Helper Functions, Step N, Main, Entry Point 순서로 구분

**에러 처리:** Entry Point에서 `try-catch`로 감싸고 `exit 0`/`exit 1` 반환

### 콘솔 출력 헬퍼 함수

| 함수 | 용도 | 색상 |
|------|------|------|
| `Write-StepProgress` | `[1/5] Building...` 형식 진행 상황 | Gray |
| `Write-Detail` | 상세 정보 (들여쓰기) | DarkGray |
| `Write-Success` | 성공 메시지 | Green |
| `Write-WarningMessage` | 경고 메시지 | Yellow |
| `Write-StartMessage` | `[START] Title` 시작 메시지 | Blue |
| `Write-DoneMessage` | `[DONE] Title` 완료 메시지 | Green |
| `Write-ErrorMessage` | 에러 메시지 + 스택 트레이스 | Red |

### 스크립트 템플릿

새 스크립트 작성 시 기본 구조입니다.

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, HelpMessage = "도움말 표시")]
  [Alias("h", "?")]
  [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

#region Helpers
function Write-StepProgress {
  param([int]$Step, [int]$TotalSteps, [string]$Message)
  Write-Host "[$Step/$TotalSteps] $Message" -ForegroundColor Gray
}
function Write-Success { param([string]$Message) Write-Host "  $Message" -ForegroundColor Green }
function Write-StartMessage { param([string]$Title) Write-Host ""; Write-Host "[START] $Title" -ForegroundColor Blue; Write-Host "" }
function Write-DoneMessage { param([string]$Title) Write-Host ""; Write-Host "[DONE] $Title" -ForegroundColor Green; Write-Host "" }
function Write-ErrorMessage {
  param([System.Management.Automation.ErrorRecord]$ErrorRecord)
  Write-Host "`n[ERROR] $($ErrorRecord.Exception.Message)" -ForegroundColor Red
  Write-Host $ErrorRecord.ScriptStackTrace -ForegroundColor DarkGray
}
#endregion

$script:TOTAL_STEPS = 3

#region Main
function Main {
  Write-StartMessage -Title "Script Title"
  # Steps...
  Write-DoneMessage -Title "Script completed"
}
#endregion

if ($Help) { Show-Help; exit 0 }

try { Main; exit 0 }
catch { Write-ErrorMessage -ErrorRecord $_; exit 1 }
```

## Troubleshooting

### .editorconfig 변경 후 경고가 반영되지 않을 때

**Cause:** 증분 빌드는 `.editorconfig` 변경을 감지하지 못합니다.

**Resolution:**
```powershell
dotnet build --no-incremental
# 또는 완전히 새로 빌드
dotnet clean && dotnet build --no-incremental
```

### csproj에서 패키지 Version을 지정했을 때 빌드 오류

**Cause:** CPM(Central Package Management) 활성화 시 csproj에서 `Version`을 지정하면 빌드 오류가 발생합니다.

**Resolution:** csproj에서 `Version` 속성을 제거하고, `Directory.Packages.props`에서만 버전을 관리합니다.
```xml
<!-- 잘못된 예 -->
<PackageReference Include="NewPackage" Version="1.0.0" />

<!-- 올바른 예 -->
<PackageReference Include="NewPackage" />
```

### 하위 폴더의 .cs 파일이 Source Link 오류를 발생시킬 때

**Cause:** 루트 `Directory.Build.props`의 Source Link 패키지가 파일 기반 프로그램 등 독립 실행 스크립트에 적용됩니다.

**Resolution:** 해당 폴더에 자체 `Directory.Build.props`를 배치하여 부모 import을 차단합니다.
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

---

## FAQ

### Q1. .sln과 .slnx의 차이점은 무엇인가요?

`.sln`은 레거시 텍스트 기반 포맷으로 GUID가 나열되어 가독성이 낮습니다. `.slnx`는 .NET 10+에서 지원하는 XML 기반 포맷으로, 수동 편집이 용이하고 구조가 명확합니다.

### Q2. Directory.Build.props와 Directory.Build.targets는 언제 각각 사용하나요?

`Directory.Build.props`는 SDK import 전에 적용되므로 속성(Property) 설정에 사용합니다. `Directory.Build.targets`는 SDK import 후에 적용되므로 기본 항목(Item) 수정에 사용합니다. 예를 들어 `Compile Remove`는 targets에서 해야 SDK가 다시 추가하는 것을 방지할 수 있습니다.

### Q3. Build-Local.ps1의 주요 파라미터는 무엇인가요?

| 파라미터 | 별칭 | 기본값 | 설명 |
|---------|------|--------|------|
| `-Solution` | `-s` | `Functorium.slnx` | 솔루션 파일 |
| `-SkipPack` | — | `$false` | NuGet 패키지 생성 건너뛰기 |
| `-SlowTestThreshold` | `-t` | `30` | 느린 테스트 판단 기준 (초) |

### Q4. nuget.config에서 `<clear />`를 추가하는 이유는 무엇인가요?

시스템/사용자 레벨에서 설정된 NuGet 소스를 제거하고, 파일에 명시된 소스만 사용하도록 합니다. 이를 통해 어떤 환경에서든 동일한 패키지 소스를 사용하고, 의도치 않은 사설 피드에서 패키지가 해석되는 것을 방지합니다.

### Q5. 복수 솔루션 파일은 어떤 경우에 사용하나요?

프로젝트가 많을 때 용도별로 분리합니다. `{Name}.slnx`는 핵심 라이브러리(Src/, Tests/) 개발용이고, `{Name}.All.slnx`는 Tutorials, Books 등을 포함한 전체 빌드용입니다.

---

## References

- [01-project-structure.md](./01-project-structure) — 프로젝트 수준 구성 (폴더, 네이밍, 의존성)
- [15a-unit-testing.md](../testing/15a-unit-testing) — 테스트 작성 방법론 (MTP 설정 포함)
- [16-testing-library.md](../testing/16-testing-library) — Functorium.Testing 라이브러리
