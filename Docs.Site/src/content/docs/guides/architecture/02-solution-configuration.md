---
title: "Solution Configuration Guide"
---

This document covers how to create and configure root-level configuration files and build scripts when creating a new solution.

## Introduction

"Have you ever encountered compatibility issues because package versions differ across projects?"
"Have you experienced broken consistency from setting build properties individually per project?"
"Has it taken new team members a long time to set up the build environment after cloning the project?"

Central configuration at the solution level is essential for consistent builds, code quality, and package management. Unifying build properties with `Directory.Build.props` and managing package versions in one place with `Directory.Packages.props` fundamentally prevents configuration inconsistencies between projects.

### What You Will Learn

This document covers the following topics:

1. **Solution file (.slnx) creation and management** - XML-based solution file structure and how to add projects
2. **Directory.Build.props configuration** - Common build property settings applied to all projects
3. **Directory.Packages.props (CPM) setup** - Centralized package version management
4. **.editorconfig code style rules** - Enforcing code quality at build time
5. **Build-Local.ps1 build pipeline** - Automating build, test, coverage, and package creation

> **솔루션 구성의 핵심은** Build 속성, 패키지 버전, 코드 스타일을 at the solution root서 중앙 관리하여 모든 프로젝트의 일관성을 보장하는 것.

## Summary

### Key Commands

```powershell
# Build and test
dotnet build Functorium.slnx
dotnet test --solution Functorium.slnx

# full Build 파이프라인 (Build + Test + coverage + 패키지)
./Build-Local.ps1

# Clean build artifacts
./Build-Clean.ps1

# Verify snapshot approval
./Build-VerifyAccept.ps1
```

### Key Procedures

**1. 새 솔루션 구성:**
1. Git 초기화 (`git init`, `.gitignore`, `.gitattributes`)
2. SDK 및 도구 (`global.json`, `dotnet new tool-manifest`)
3. Build 시스템 (`Directory.Build.props`, `Directory.Packages.props`)
4. 코드 품질 (`.editorconfig`, `nuget.config`)
5. Solution file (`.slnx` 생성)

**2. 패키지 Add:**
1. `Directory.Packages.props`에 `<PackageVersion>` Add
2. `.csproj`에 **Version 없이** `<PackageReference>` Add

### Key Concepts

| Concept | Description |
|------|------|
| `.slnx` | XML 기반 Solution file (.NET 10+) |
| `Directory.Build.props` | 모든 프로젝트에 적용되는 Common build properties (SDK import 전) |
| `Directory.Build.targets` | SDK 기본 항목 처리 후 적용되는 타겟 (Compile Remove 등) |
| `Directory.Packages.props` | 중앙 집중식 패키지 Versioning (CPM) |
| `Build-Local.ps1` | 10단계 Build 파이프라인 (Build → 테스트 → 커버리지 → 패키지) |

---

## Overview

This guide covers how to create and configure root-level configuration files and build scripts when creating a new solution.
프로젝트 수준 파일(`AssemblyReference.cs`, `Using.cs`)은 [01-project-structure.md](./01-project-structure)를 참조하세요.

### Files Required at the Solution Root

The following table at the solution root 배치해야 하는 파일의 full 목록과 각 파일의 Role.

| 파일 | Role | Creation method |
|------|------|----------|
| `{Name}.slnx` | Solution file | `dotnet new sln` 후 변환 또는 write directly |
| `global.json` | SDK version pinning + test runner | `dotnet new globaljson` then modify |
| `Directory.Build.props` | Common build properties | write directly |
| `Directory.Build.targets` | SDK post-processing targets | write directly (필요 시) |
| `Directory.Packages.props` | Central package version management | write directly |
| `.editorconfig` | Code style rules | `dotnet new editorconfig` then modify |
| `.gitignore` | Git exclusion items | `dotnet new gitignore` then modify |
| `.gitattributes` | Per-file Git attributes | write directly |
| `nuget.config` | NuGet source configuration | `dotnet new nugetconfig` then modify |
| `.config/dotnet-tools.json` | Local .NET tools | `dotnet new tool-manifest` |

### File Load/Application Order

```
1. global.json           ← SDK version determination (when running dotnet commands)
2. nuget.config          ← Package source determination (during restore)
3. Directory.Build.props  ← Project common properties (before SDK import)
4. {project}.csproj       ← Individual project settings
5. Directory.Build.targets ← Targets after SDK default item processing
6. Directory.Packages.props ← Package version resolution (during restore)
7. .editorconfig          ← Code style application (build + IDE)
```

## Solution File (.slnx)

### Differences Between .sln and .slnx

| | `.sln` (legacy) | `.slnx` (new) |
|---|---|---|
| Format | Text-based (proprietary format) | XML-based |
| Readability | Low (GUID listings) | High (Folder/Project structure) |
| Manual editing | Difficult | Easy |
| Supported SDK | All versions | .NET 10+ |

### Creation Method

`dotnet new sln` generates `.sln` by default. To use `.slnx`, there are two methods.

**Method 1: Create .sln then convert**

```powershell
# 1. Create .sln file
dotnet new sln -n MyApp

# 2. Add projects
dotnet sln MyApp.sln add Src/MyApp/MyApp.csproj
dotnet sln MyApp.sln add Tests/MyApp.Tests.Unit/MyApp.Tests.Unit.csproj

# 3. Convert to .slnx (built-in dotnet CLI)
dotnet sln MyApp.sln migrate
```

`dotnet sln migrate` generates a `.slnx` file with the same project configuration as the `.sln`. After conversion, delete the `.sln` file manually.

**방법 2: .slnx write directly**

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

write directly 시 `dotnet sln add` 명령은 사용할 수 없으므로, XML을 수동으로 편집합니다.

### .slnx Syntax

```xml
<Solution>
  <!-- Solution folder: Name starts/ends with / -->
  <Folder Name="/Src/">
    <!-- 프로젝트: Path는 Solution file 기준 상대 경로 -->
    <Project Path="Src/MyApp/MyApp.csproj" />
    <!-- Id is optional (auto-generated by Visual Studio) -->
    <Project Path="Src/MyApp.Domain/MyApp.Domain.csproj" Id="..." />
  </Folder>

  <!-- Nested folders are declared as separate Folder elements -->
  <Folder Name="/Tests.Hosts/" />
  <Folder Name="/Tests.Hosts/01-SingleHost/" />
  <Folder Name="/Tests.Hosts/01-SingleHost/Src/">
    <Project Path="Tests.Hosts/01-SingleHost/Src/MyHost/MyHost.csproj" />
  </Folder>
</Solution>
```

### Adding/Removing Projects

```powershell
# Add project to .slnx (dotnet CLI supported)
dotnet sln MyApp.slnx add Src/MyApp.Domain/MyApp.Domain.csproj

# Remove project
dotnet sln MyApp.slnx remove Src/MyApp.Domain/MyApp.Domain.csproj

# Check project list
dotnet sln MyApp.slnx list
```

> When adding a project to `.slnx` with `dotnet sln add`, it is placed at the root without a solution folder. If solution folder structure is needed, edit the XML directly.

### Multiple Solution File Configuration

프로젝트가 많을 때 용도별로 Solution file을 분리합니다.

| Solution | Included Projects | Purpose |
|--------|--------------|------|
| `{Name}.slnx` | Src/, Tests/ | Core library development (default) |
| `{Name}.All.slnx` | full 프로젝트 | Tutorials, Books 등 포함 full Build |

### Build/Test Commands

```powershell
dotnet build MyApp.slnx
dotnet test --solution MyApp.slnx
```

> To specify a solution for `dotnet test`, use the `--solution` option (`--project` is for single projects).

Solution file이 프로젝트를 묶는다면, `global.json`은 SDK 버전과 테스트 러너를 결정합니다.

## global.json

### Creation Method

```powershell
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
```

Manually add the `test` section to the generated file.

### Configuration Content

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

### Property Descriptions

| Property | Description |
|------|------|
| `sdk.version` | Minimum required SDK version. Check current version with `dotnet --version` |
| `sdk.rollForward` | SDK version matching policy |
| `test.runner` | Test runner. Used together with `UseMicrosoftTestingPlatformRunner` in `Directory.Build.props` |

### rollForward Policy Selection

| Policy | Behavior | When to Use |
|------|------|----------|
| `latestFeature` | Latest feature band within same major.minor (recommended) | Allow CI/local SDK differences |
| `latestPatch` | Latest patch within same feature band | Strict SDK pinning |
| `latestMajor` | Use latest SDK | Maximum version flexibility |

### rollForward Policy Detailed Comparison

The following table `version: 10.0.100` 기준으로 각 정책이 허용하는 SDK 버전 범위를 비교합니다.

| 정책 | Description | 허용 예시 | 거부 예시 |
|------|------|----------|----------|
| `patch` | 동일 major.minor 내 최신 patch | `10.0.102` | `10.1.x` |
| `feature` / `minor` | 동일 major 내 최신 minor | `10.1.x` | `11.x.x` |
| `major` | 최신 major까지 허용 | `11.x.x` | — |
| `latestPatch` | 최신 patch 버전 사용 | `10.0.x` 중 최신 | `10.1.x` |
| `latestFeature` / `latestMinor` | 최신 feature 버전 사용 | `10.x.x` 중 최신 | `11.x.x` |
| `latestMajor` | 설치된 SDK 중 최신 | 모든 버전 | — |
| `disable` | 정확한 버전만 허용 | `10.0.100`만 | 그 외 전부 |

### Recommended Policy by Environment

| 환경 | 권장 정책 | 이유 |
|------|----------|------|
| 개발/테스트 | `latestFeature` | 최신 기능 활용, 보안 패치 자동 적용 |
| 프로덕션/CI | `patch` | 안정성 우선 |
| 라이브러리 | `patch` | 호환성 유지 |
| 실험 프로젝트 | `latestMajor` | 최신 버전 체험 |

### SDK Upgrade Procedure

```powershell
# 1. 설치된 SDK 확인
dotnet --list-sdks

# 2. global.json의 version 필드 업데이트
# 예: "version": "10.0.200"

# 3. 적용된 버전 확인
dotnet --version

# 4. Build 및 테스트 검증
dotnet build
dotnet test --solution Functorium.slnx

# 5. 커밋
git add global.json
git commit -m "build: SDK 버전을 10.0.200으로 업그레이드"
```

### Using global.json in CI/CD

```yaml
# GitHub Actions 예시
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    global-json-file: global.json  # global.json 자동 인식
```

SDK 버전이 결정되었으면, 다음으로 모든 프로젝트에 적용되는 Common build properties을 설정합니다.

## Directory.Build.props

### Creation Method

at the solution root `Directory.Build.props` 파일을 create directly. MSBuild가 프로젝트 파일 평가 전 디렉토리 트리를 상향 탐색하여 자동으로 찾습니다.

### Default Template

This is a minimal configuration containing only required properties.

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

### Adding NuGet Package Metadata

If there are projects that deploy NuGet packages, add common metadata.

```xml
  <!-- NuGet Package Common Settings -->
  <PropertyGroup>
    <Authors>{Name}</Authors>
    <Company>{Company}</Company>
    <Copyright>Copyright (c) {Company} Contributors. All rights reserved.</Copyright>
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

### Adding Microsoft Testing Platform (MTP) Settings

To use MTP in test projects, add the following section.

```xml
  <!-- Microsoft Testing Platform -->
  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>
```

> `IsTestProject`는 테스트 SDK 참조가 있는 프로젝트에서 자동으로 `true`가 됩니다.

### Adding Source Link Package

To enable Source Link in GitHub-hosted projects, add the package to all projects.

```xml
  <!-- Source Link Package -->
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
  </ItemGroup>
```

### Summary by Section

| 섹션 | key properties | required |
|------|----------|----------|
| Target Framework / Language | `TargetFramework`, `LangVersion`, `Nullable` | 필수 |
| Code Quality | `EnforceCodeStyleInBuild` | 권장 |
| NuGet Metadata | `Authors`, `License`, `RepositoryUrl` 등 | NuGet 배포 시 |
| Symbol/Source Link | `IncludeSymbols`, `PublishRepositoryUrl` | NuGet 배포 시 |
| Deterministic Build | `ContinuousIntegrationBuild` | CI 환경 시 |
| Testing (MTP) | `OutputType Exe`, `UseMicrosoftTestingPlatformRunner` | MTP 사용 시 |
| Source Link Package | `Microsoft.SourceLink.GitHub` | Source Link 사용 시 |

<details>
<summary>Current Functorium Directory.Build.props full</summary>

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

### Role

A target file applied **after** SDK default item processing. Used to exclude specific files from `Compile` items.

### Differences Between props and targets

| | `Directory.Build.props` | `Directory.Build.targets` |
|---|---|---|
| Application timing | Before SDK import | After SDK import |
| Purpose | Property settings | Default item modification |
| Example | `TargetFramework`, `Nullable` | `Compile Remove`, conditional item removal |

### Why Removal Must Be Done in targets

The SDK automatically adds `**/*.cs` to `Compile` items after props processing. Even if you do `<Compile Remove="...">` in props, the SDK adds them back, so removal must be done in targets to be effective.

### Creation Method

at the solution root `Directory.Build.targets` 파일을 create directly. 필요할 때만 만들면 됩니다.

**When using PublicApiGenerator:**

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

> This file is unnecessary if you are not using PublicApiGenerator.

Once build properties are unified, next set up CPM to manage package versions in one place.

## Directory.Packages.props

### Role

Enables Central Package Management (CPM). Manages all project package versions in one place.

### Creation Method

at the solution root `Directory.Packages.props` 파일을 create directly.

### Default Template

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <!-- Categories are organized and managed by Label -->
  <ItemGroup Label="Source Link">
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup Label="Basic">
    <!-- Add package versions to use in projects here -->
  </ItemGroup>

  <ItemGroup Label="Testing">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageVersion Include="xunit.v3" Version="3.2.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <!-- Additional test packages -->
  </ItemGroup>
</Project>
```

### Package Addition Procedure

1. Add `<PackageVersion>` to the appropriate Label group in `Directory.Packages.props`:
   ```xml
   <ItemGroup Label="Basic">
     <PackageVersion Include="NewPackage" Version="1.0.0" />
   </ItemGroup>
   ```

2. Add `<PackageReference>` **without Version** to the project `.csproj`:
   ```xml
   <ItemGroup>
     <PackageReference Include="NewPackage" />
   </ItemGroup>
   ```

> When CPM is enabled, specifying `Version` in csproj causes build errors. Versions must be managed only in `Directory.Packages.props`.

### Version Update

Simply modifying the `Version` property in `Directory.Packages.props` applies it to all projects referencing that package.

### Label Category Configuration Example

The following table `Label` 속성으로 패키지를 분류하는 구성 예시.

| Label | Purpose | Representative Package |
|-------|------|------------|
| Source Link | Source Link debugging | `Microsoft.SourceLink.GitHub` |
| API Generation | Public API surface generation | `PublicApiGenerator` |
| Source Generator | Source generator development | `Microsoft.CodeAnalysis.CSharp` |
| Basic | Core libraries | `LanguageExt.Core`, `Mediator.*`, `FluentValidation` |
| Observability | Logging/metrics/tracing | `Serilog.*`, `OpenTelemetry.*` |
| WebApi | HTTP API | `FastEndpoints`, `Swashbuckle.AspNetCore` |
| Versioning | Versioning | `MinVer` |
| ORM | Data access | `Dapper`, `Microsoft.EntityFrameworkCore.*` |
| Scheduling | Job scheduling | `Quartz` |
| Testing | Testing framework | `xunit.v3`, `Shouldly`, `NSubstitute` |

<details>
<summary>Current Functorium Directory.Packages.props full</summary>

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

패키지 Versioning가 구성되었으면, 다음으로 코드 스타일과 포맷팅 규칙을 설정합니다.

## .editorconfig

### Creation Method

```powershell
dotnet new editorconfig
```

This command generates an `.editorconfig` containing all .NET SDK default rules. Modify it for your project after generation.

### Required Settings

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

### Adding Verify Snapshot Settings (When Using Verify.Xunit)

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

### Enforcing file-scoped namespace (Recommended)

```ini
[*.{cs,vb}]
# Default namespace declaration setting (IDE0161)
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0161.severity = warning
```

### Default Value Strategy

Most .NET coding rules (using sorting, naming conventions, formatting, etc.) are commented out to use SDK defaults. Uncomment only the rules you want to explicitly enable. Since `dotnet new editorconfig` generates all rules with comments, it is convenient to uncomment only the desired rules.

### Code Style Rules and Diagnostic Rules

`csharp_style_namespace_declarations`와 `dotnet_diagnostic.IDE0161.severity`는 모두 네임스페이스 스타일을 제어하지만 Role이 다릅니다.

| Item | `csharp_style_namespace_declarations` | `dotnet_diagnostic.IDE0161.severity` |
|------|--------------------------------------|--------------------------------------|
| **유형** | Code style rules | 진단 규칙 |
| **Role** | 선호 스타일 + 심각도 정의 | 심각도만 정의 |
| **Format** | `value:severity` (e.g., `file_scoped:warning`) | `severity` (e.g., `warning`) |
| **Priority** | Low | High (can override) |

Using both together allows explicitly enforcing style definitions and build severity.

### Severity Levels

| Level | IDE Display | Build Impact |
|------|----------|----------|
| `none` | Not displayed | No impact |
| `silent` | Dimmed display | No impact |
| `suggestion` | Dotted line | No impact |
| `warning` | Wavy line | Warning generated |
| `error` | Red display | Build failure |

### Enabling Code Analysis at Build Time

`Directory.Build.props`'s `EnforceCodeStyleInBuild` and `.editorconfig` rules work together.

| Setting | IDE | Build |
|------|-----|------|
| `EnforceCodeStyleInBuild = false` (default) | Real-time warnings | Ignored |
| `EnforceCodeStyleInBuild = true` | Real-time warnings | Build warnings |

### Code Quality Verification Workflow

```powershell
# Default build (incremental)
dotnet build

# After setting changes (ignore cache)
dotnet build --no-incremental

# Complete fresh build
dotnet clean && dotnet build --no-incremental

# Treat warnings as errors (CI environment)
dotnet build /p:TreatWarningsAsErrors=true
```

> After changing `.editorconfig` or `Directory.Build.props`, you must use the `--no-incremental` option. Incremental builds do not detect configuration changes.

### Enabling Rules by Category

```ini
# All style rules
dotnet_analyzer_diagnostic.category-Style.severity = warning

# All performance rules
dotnet_analyzer_diagnostic.category-Performance.severity = warning

# All security rules
dotnet_analyzer_diagnostic.category-Security.severity = error
```

> Enabling all rules from the start may generate many warnings. Apply gradually.

## .gitignore / .gitattributes

### Creating .gitignore

```powershell
dotnet new gitignore
```

This command generates a standard Visual Studio/dotnet `.gitignore`. After generation, add items appropriate for your project.

**Items to add:**

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

### Main .gitignore Categories

| Category | Pattern | Description |
|---------|------|------|
| Build output | `[Dd]ebug/`, `[Rr]elease/`, `[Oo]bj/`, `**/[Bb]in/*` | Build artifacts |
| NuGet | `*.nupkg`, `*.snupkg`, `**/[Pp]ackages/*` | Package files |
| Test results | `[Tt]est[Rr]esult*/`, `*.trx` | Test reports |
| 커버리지 | `coverage*.json`, `coverage*.xml`, `.coverage/reports/` | Code coverage |
| Verify | `*.received.*` | Verify snapshot intermediate files |
| IDE | `.vs/`, `.vscode/*` | Visual Studio/VS Code settings |

### Creating .gitattributes (When Using Verify.Xunit)

Verify 스냅샷 파일의 줄바꿈과 인코딩을 강제하는 `.gitattributes`를 create directly.

```
*.verified.txt text eol=lf working-tree-encoding=UTF-8
*.verified.xml text eol=lf working-tree-encoding=UTF-8
*.verified.json text eol=lf working-tree-encoding=UTF-8
*.verified.bin binary
```

**Reason:** Verify snapshots must have identical content regardless of OS. Since mixing Windows CRLF causes unnecessary diffs, LF is enforced.

> `.gitattributes` is unnecessary if you are not using Verify.Xunit.

## nuget.config

### Creation Method

```powershell
dotnet new nugetconfig
```

Modify the generated file as follows.

### Configuration Content

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### `<clear />`를 Add하는 이유

`<clear />` removes all NuGet sources set at the system/user level and uses only the sources specified in this file.

- Predictable package resolution (same sources in any environment)
- Prevents packages from being resolved from unintended private feeds

> `<clear />` is not included when generated with `dotnet new nugetconfig`, so it must be added manually.

### Adding Private Feeds

```xml
<packageSources>
  <clear />
  <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  <add key="private" value="https://pkgs.example.com/nuget/v3/index.json" />
</packageSources>
```

## .config/dotnet-tools.json

### Creation Method

```powershell
dotnet new tool-manifest
```

This command generates the `.config/dotnet-tools.json` manifest file.

### Tool Installation

```powershell
# Code coverage 리포트 생성 도구
dotnet tool install dotnet-reportgenerator-globaltool

# Verify snapshot management tool
dotnet tool install verify.tool
```

Upon installation, it is automatically registered in the manifest.

### Tool Restoration (After Cloning)

```powershell
dotnet tool restore
```

> `Build-Local.ps1` automatically performs `dotnet tool restore` when executed.

### Installed Tool List

| Tool | Command | Purpose |
|------|------|------|
| `dotnet-reportgenerator-globaltool` | `reportgenerator` | Code coverage HTML report 생성 |
| `verify.tool` | `dotnet-verify` | Verify snapshot management (accept/reject) |
| `gman.siren` | `siren-gen` | EF Core DbContext -> Mermaid ER diagram generation |

> For detailed usage of each tool (parameters, execution examples), see [03-dotnet-tools.md](./03-dotnet-tools)를 참조하세요.

### Adding/Updating New Tools

```powershell
# Add new tool (auto-registered in manifest)
dotnet tool install <package-name>

# Update tool
dotnet tool update <package-name>

# Remove tool
dotnet tool uninstall <package-name>
```

Once configuration file setup is complete, let us finally look at the scripts that automate the build pipeline.

## Build Scripts

### Script List

The following table 프로젝트에서 제공하는 Build 스크립트의 full 목록.

| 스크립트 | Role | 주요 파라미터 |
|----------|------|-------------|
| `Build-Local.ps1` | Build, test, coverage, NuGet packages | `-Solution`, `-SkipPack`, `-SlowTestThreshold` |
| `Build-Clean.ps1` | Delete bin/obj folders | `-Help` |
| `Build-VerifyAccept.ps1` | Batch approve Verify snapshots | `-Help` |
| `Build-CleanRunFileCache.ps1` | .NET 10 runfile cache cleanup | `-Pattern`, `-WhatIf` |
| `Build-SetAsSetupProject.ps1` | Tests.Hosts project setup configuration | — |
| `Build-ERDiagram.ps1` | EF Core DbContext -> Mermaid ER diagram generation | — |

### Build-Local.ps1

full Build 파이프라인을 10단계로 실행합니다.

| Step | Task | Description |
|------|------|------|
| 1 | Tool restore | `dotnet tool restore` |
| 2 | Solution search | `-Solution` parameter or auto-search |
| 3 | Build | `dotnet build -c Release` |
| 4 | Version info | Build된 DLL의 ProductVer, FileVer, Assembly 출력 |
| 5 | Test + coverage | `dotnet test` + MTP Code coverage 수집 |
| 6 | Coverage merge | Collect coverage files from multiple test projects |
| 7 | HTML report | Generate HTML + Cobertura + Markdown reports with ReportGenerator |
| 8 | Coverage output | Project coverage + Full coverage console output |
| 9 | Slow test analysis | 지정 임계값 초과 Test reports 생성 |
| 10 | NuGet packages | `dotnet pack` (projects in Src/) |

**Key parameters:**

| Parameter | Alias | Default | Description |
|---------|------|--------|------|
| `-Solution` | `-s` | `Functorium.slnx` | Solution file 경로 |
| `-ProjectPrefix` | `-p` | `Functorium` | Coverage filtering prefix |
| `-SkipPack` | — | `$false` | NuGet packages 생성 건너뛰기 |
| `-SlowTestThreshold` | `-t` | `30` | Slow test threshold (seconds) |

**Output directories:**

```
{SolutionDir}/
├── .coverage/reports/              ← HTML report, 병합 커버리지 (Cobertura.xml)
├── .nupkg/                         ← NuGet packages (.nupkg, .snupkg)
└── Tests/
    └── {TestProject}/
        └── TestResults/
            ├── {GUID}/
            │   └── coverage.cobertura.xml  <- Original coverage
            └── *.trx                       ← Test results
```

**Coverage classification (console output):**

| Classification | Include Pattern | Description |
|------|-----------|------|
| Project Coverage | `{Prefix}.*` | Projects starting with specified prefix |
| Full Coverage | full (테스트 제외) | All production code |

**Usage examples:**

```powershell
# 기본 실행 (Build + 테스트 + 패키지)
./Build-Local.ps1

# full 솔루션 Build
./Build-Local.ps1 -s Functorium.All.slnx

# Skip package creation
./Build-Local.ps1 -SkipPack

# Change slow test threshold
./Build-Local.ps1 -t 60
```

### Build-Clean.ps1

Batch deletes all `bin/` and `obj/` folders from all projects.

```powershell
./Build-Clean.ps1
```

**When to use:**
- Build artifacts를 완전히 초기화하고 싶을 때
- Build 오류가 캐시된 바이너리로 인해 발생할 때
- 브랜치 전환 후 이전 Build output물을 정리할 때

### Build-VerifyAccept.ps1

Verify.Xunit 스냅샷 Test results를 일괄 승인합니다.

```powershell
./Build-VerifyAccept.ps1
```

**When to use:**
- When `*.received.*` files are generated after test execution and there are pending snapshots
- When output has intentionally changed and new snapshots need to be approved

**Operation process:**
1. `dotnet tool restore`로 `verify.tool` restore
2. `dotnet verify accept -y`to approve all pending snapshots

### Build-CleanRunFileCache.ps1

Cleans the cache for .NET 10 file-based programs (`.cs` direct execution).

```powershell
# Clean only SummarizeSlowestTests cache (default)
./Build-CleanRunFileCache.ps1

# Clean all runfile caches
./Build-CleanRunFileCache.ps1 -Pattern "All"

# Check deletion targets only (no actual deletion)
./Build-CleanRunFileCache.ps1 -WhatIf
```

**When to use:** `System.CommandLine` and other package loading errors occur. Cache location is `%TEMP%\dotnet\runfile\`.

| 파라미터 | Default | Description |
|---------|--------|------|
| `-Pattern` | `SummarizeSlowestTests` | Cache pattern to delete (`All`이면 full) |
| `-WhatIf` | — | Display deletion targets only |

## Nested Configuration Files

### When Parent Import Blocking Is Needed

In subfolders containing independently-executed `.cs` files like .NET 10 file-based programs (runfile), root `Directory.Build.props` settings (Source Link packages, etc.) may be unnecessary or cause errors.

### Blocking Method

Place a self-contained `Directory.Build.props` in that folder. MSBuild applies only the nearest `Directory.Build.props`, so it does not automatically import the parent file.

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

> This pattern is currently applied in `.coverage/scripts/` and `.release-notes/scripts/`.

### When You Want to Inherit from Parent Instead

To also apply the parent file from a child `Directory.Build.props`, explicitly import it.

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

  <!-- Additional settings -->
</Project>
```

## New Solution Configuration Checklist

When creating a new solution, generate files in the following order.

1. **Git initialization**
   - [ ] `git init`
   - [ ] `dotnet new gitignore` → Add project-specific items
   - [ ] `.gitattributes` Create (when using Verify)

2. **SDK and tools setup**
   - [ ] `dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature` → `test` Add section
   - [ ] `dotnet new tool-manifest` → Install required tools

3. **Build 시스템 구성**
   - [ ] `Directory.Build.props` Create (default template + required sections)
   - [ ] `Directory.Build.targets` Create (if needed)
   - [ ] `Directory.Packages.props` Create (CPM activation + add packages)

4. **Code quality setup**
   - [ ] `dotnet new editorconfig` → Enable only required rules
   - [ ] `dotnet new nugetconfig` → `<clear />` Add

5. **Solution file 생성**
   - [ ] `dotnet new sln -n {Name}` → `dotnet sln migrate`to convert to `.slnx`
   - [ ] 또는 `.slnx` write directly
   - [ ] 프로젝트 Add (`dotnet sln add` or XML editing)

## PowerShell Script Development Standards

### Requirements and Structure

- PowerShell 7.0 or higher (`#Requires -Version 7.0`)
- Each script is self-contained, directly including required helper functions in the `#region Helpers` block

### File Naming Rules

| Type | Pattern | Example |
|------|------|------|
| Build 스크립트 | `Build-*.ps1` | `Build-Local.ps1` |
| Deployment scripts | `Deploy-*.ps1` | `Deploy-Production.ps1` |
| Utility scripts | `Invoke-*.ps1` | `Invoke-Migration.ps1` |

### Required Settings

All scripts start with the following settings.

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```

### Coding Rules

**Function naming:** `Get-`, `Set-`, `New-`, `Remove-`, `Invoke-`, `Test-`, `Show-`, `Write-` prefix

**Variable naming:** Script global is `$script:TOTAL_STEPS` (uppercase), function local is `$result`

**Code structure:** `#region`으to organize in order: Constants, Helper Functions, Step N, Main, Entry Point

**Error handling:** Wrap in `try-catch`at Entry Point and return `exit 0`/`exit 1` 

### Console Output Helper Functions

| Function | Purpose | Color |
|------|------|------|
| `Write-StepProgress` | `[1/5] Building...` format progress | Gray |
| `Write-Detail` | Detail info (indented) | DarkGray |
| `Write-Success` | Success message | Green |
| `Write-WarningMessage` | Warning message | Yellow |
| `Write-StartMessage` | `[START] Title` Start message | Blue |
| `Write-DoneMessage` | `[DONE] Title` Done message | Green |
| `Write-ErrorMessage` | Error message + stack trace | Red |

### Script Template

새 스크립트 작성 시 기본 구조.

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, HelpMessage = "Display help")]
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

### When Warnings Are Not Reflected After .editorconfig Changes

**Cause:** 증분 Build는 `.editorconfig` 변경을 감지하지 못합니다.

**Resolution:**
```powershell
dotnet build --no-incremental
# 또는 완전히 새로 Build
dotnet clean && dotnet build --no-incremental
```

### csproj에서 패키지 Version을 지정했을 때 Build 오류

**Cause:** CPM(Central Package Management) 활성화 시 csproj에서 `Version`을 지정하면 Build 오류가 발생합니다.

**Resolution:** Remove the `Version` property from csproj and manage versions only in `Directory.Packages.props`.
```xml
<!-- Incorrect example -->
<PackageReference Include="NewPackage" Version="1.0.0" />

<!-- Correct example -->
<PackageReference Include="NewPackage" />
```

### When .cs Files in Subfolders Cause Source Link Errors

**Cause:** The root `Directory.Build.props`'s Source Link package is applied to independently-executed scripts like file-based programs.

**Resolution:** Place a self-contained `Directory.Build.props` in that folder to block parent import.
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

### Q1. What is the difference between .sln and .slnx?

`.sln` is a legacy text-based format with listed GUIDs and low readability. `.slnx` is an XML-based format supported in .NET 10+ with easy manual editing and clear structure.

### Q2. When should Directory.Build.props and Directory.Build.targets be used respectively?

`Directory.Build.props`는 SDK import 전에 적용되므로 속성(Property) 설정에 사용합니다. `Directory.Build.targets`는 SDK import 후에 적용되므로 기본 항목(Item) 수정에 사용합니다. 예를 들어 `Compile Remove`는 targets에서 해야 SDK가 다시 Add하는 것을 방지할 수 있습니다.

### Q3. What are the key parameters of Build-Local.ps1?

| Parameter | Alias | Default | Description |
|---------|------|--------|------|
| `-Solution` | `-s` | `Functorium.slnx` | Solution file |
| `-SkipPack` | — | `$false` | NuGet packages 생성 건너뛰기 |
| `-SlowTestThreshold` | `-t` | `30` | Slow test threshold (seconds) |

### Q4. nuget.config에서 `<clear />`를 Add하는 이유는 무엇인가요?

Removes NuGet sources set at the system/user level and ensures only sources specified in the file are used. This ensures the same package sources are used in any environment and prevents packages from being resolved from unintended private feeds.

### Q5. 복수 Solution file은 어떤 경우에 사용하나요?

프로젝트가 많을 때 용도별로 분리합니다. `{Name}.slnx`는 Core libraries(Src/, Tests/) 개발용이고, `{Name}.All.slnx`는 Tutorials, Books 등을 포함한 full Build용.

---

## References

- [01-project-structure.md](./01-project-structure) -- Project-level configuration (folders, naming, dependencies)
- [15a-unit-testing.md](../testing/15a-unit-testing) -- Test writing methodology (including MTP settings)
- [16-testing-library.md](../testing/16-testing-library) -- Functorium.Testing library
