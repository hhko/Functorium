---
title: "CI/CD 워크플로우 및 버전 관리"
---

이 문서는 GitHub Actions를 사용한 CI/CD 워크플로우 설정, NuGet 패키지 배포 방법, 그리고 MinVer를 사용한 Git 태그 기반 자동 버전 관리와 다음 버전 제안 명령을 설명합니다.

## Introduction

"태그를 푸시하면 자동으로 NuGet 패키지가 배포되도록 하려면 어떻게 구성하는가?"
"빌드 번호와 패키지 버전을 수동으로 관리하지 않으려면 어떤 도구를 사용하는가?"
"Pre-release에서 정식 릴리스까지의 버전 진행은 어떤 절차를 따르는가?"

수동 버전 관리와 배포는 오류가 발생하기 쉽고 재현성이 떨어집니다. Git 태그 기반 자동 버전 관리와 GitHub Actions CI/CD 파이프라인을 조합하면, 코드 품질 검증부터 패키지 배포까지의 전 과정을 자동화할 수 있습니다.

### What You Will Learn

This document covers the following topics:

1. **Build/Publish 워크플로우 구성** - Push/PR 시 CI, 태그 푸시 시 Release 자동화
2. **NuGet 패키지 설정** - `Directory.Build.props` 공통 메타데이터와 SourceLink/심볼 패키지
3. **MinVer 기반 자동 버전 관리** - Git 태그에서 SemVer 버전 자동 계산
4. **버전 진행 시나리오** - Alpha → Beta → RC → Stable 릴리스 흐름
5. **Conventional Commits 기반 버전 제안** - 커밋 타입으로 다음 버전 자동 결정

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [솔루션 구성 가이드](./02-solution-configuration) - `Directory.Build.props`, `Directory.Packages.props` 구조
- GitHub Actions 워크플로우의 기본 개념 (trigger, jobs, steps)
- Semantic Versioning (SemVer 2.0) 규칙

> **CI/CD와 버전 관리의 핵심은** Git 태그 하나로 버전 계산부터 패키지 배포까지 전 과정을 자동화하여, 수동 관리의 오류를 원천적으로 제거하는 것입니다.

## Summary

### CI/CD 워크플로우

#### Key Commands

```bash
# CI 자동 실행 (Push to main 또는 PR)
git push origin main

# 릴리스 배포 (태그 푸시)
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0

# 로컬 패키지 생성
./Build-Local.ps1
dotnet pack -c Release -o .nupkg
```

#### Key Procedures

**1. 일반 개발 (CI만 실행):**
1. 코드 변경 후 `git push origin main` 또는 PR 생성
2. Build 워크플로우 자동 실행 (빌드 → 테스트 → 커버리지)

**2. 릴리스 배포:**
1. `git tag -a v1.0.0 -m "Release 1.0.0"` 태그 생성
2. `git push origin v1.0.0` 태그 푸시
3. Publish 워크플로우 자동 실행 (빌드 → 테스트 → 패키지 배포 → GitHub Release)

#### Key Concepts

| Concept | Description |
|------|------|
| Build 워크플로우 | PR/Push 시 빌드, 테스트, 커버리지 수집 |
| Publish 워크플로우 | 태그 푸시(v*.*.*) 시 빌드 + 패키지 배포 + GitHub Release |
| NuGet 메타데이터 | `Directory.Build.props`에서 공통 설정, csproj에서 프로젝트별 설정 |
| SourceLink + 심볼 패키지 | 디버깅 시 라이브러리 소스 Step-into 지원 |
| Deterministic Build | CI 환경에서 재현 가능한 빌드 보장 |

### 버전 관리

#### Key Commands

```bash
# 버전 확인
dotnet build -p:MinVerVerbosity=normal

# 태그 생성 및 릴리스
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0

# 다음 버전 제안
/suggest-next-version
/suggest-next-version alpha
```

#### Key Procedures

**1. Pre-release에서 Stable까지:**
1. Alpha 태그: `git tag v1.0.0-alpha.0`
2. Beta 태그: `git tag v1.0.0-beta.0` (선택)
3. RC 태그: `git tag v1.0.0-rc.0` (선택)
4. 정식 릴리스: `git tag v1.0.0`

**2. 다음 Patch 릴리스:**
1. 정식 릴리스 후 개발 계속 (자동으로 `X.Y.Z+1-alpha.0.N` 표시)
2. 준비되면 `git tag vX.Y.Z+1`

#### Key Concepts

| Concept | Description |
|------|------|
| MinVer | Git 태그 기반 자동 버전 계산 MSBuild 도구 |
| Height | 최근 태그 이후 커밋 수 (자동 증가) |
| MinVerAutoIncrement | RTM 태그 후 자동 증가 단위 (patch/minor/major) |
| AssemblyVersion 전략 | `Major.Minor.0.0` (Patch 미포함 — 재컴파일 방지) |
| Conventional Commits | 커밋 타입(feat/fix/feat!)으로 버전 증가 결정 |

---

## 개요

### 목적

Git 태그 기반 자동 버전 관리와 CI/CD 파이프라인을 통해 안정적인 빌드와 배포를 자동화합니다.

### 워크플로우 구성

다음 테이블은 두 개의 워크플로우와 각각의 트리거 조건을 정리한 것입니다.

| 워크플로우 | 트리거 | 주요 작업 |
|-----------|--------|----------|
| CI (build.yml) | PR, Push to main | 빌드, 테스트, 커버리지 |
| Release (publish.yml) | 태그 푸시 (v*.*.*) | 빌드, 테스트, 패키지 배포 |

### 생성되는 패키지

| 패키지 | Description |
|--------|------|
| `Functorium` | .NET용 함수형 도메인 프레임워크 |
| `Functorium.Testing` | Functorium 테스트 유틸리티 |

### 파일 위치

```
프로젝트루트/
├── .github/
│   └── workflows/
│       ├── build.yml        # Build 워크플로우 (CI)
│       └── publish.yml      # Publish 워크플로우 (Release)
├── Directory.Build.props    # 공통 NuGet 설정
├── Directory.Packages.props # 중앙 패키지 버전 관리
├── Functorium.png              # 패키지 아이콘 (128x128 PNG)
├── .nupkg/                  # 생성된 패키지 출력 디렉토리
└── Src/
    ├── Functorium/
    │   └── Functorium.csproj
    └── Functorium.Testing/
        └── Functorium.Testing.csproj
```

---

## 워크플로우 구성

### Build 워크플로우 (build.yml)

**트리거:**
- Pull Request to main
- Push to main 브랜치
- 문서/스크립트 파일은 제외 (*.md, Docs/**, .claude/**, *.ps1)
- 수동 실행 (workflow_dispatch)

**작업:**
1. 코드 체크아웃
2. .NET 10 설정
3. NuGet 패키지 캐시
4. 의존성 복원
5. 취약점 패키지 검사
6. Release 모드 빌드
7. 테스트 실행 및 커버리지 수집
8. 테스트 결과 업로드
9. ReportGenerator로 커버리지 리포트 생성
10. 커버리지 요약 표시 (GITHUB_STEP_SUMMARY)
11. 커버리지 리포트 업로드

### Publish 워크플로우 (publish.yml)

**트리거:**
- 태그 푸시: v*.*.* (예: v1.0.0, v1.2.3)

**작업:**
1. Build 워크플로우의 모든 작업 수행
2. NuGet 패키지 생성
3. NuGet.org에 배포
4. GitHub Release 생성

---

## Build 워크플로우

### 워크플로우 파일

`.github/workflows/build.yml`:

```yaml
name: Build

on:
  push:
    branches: [ main ]
    paths-ignore:
      - '**.md'
      - 'Docs/**'
      - '.claude/**'
      - '**.ps1'
  pull_request:
    branches: [ main ]
    paths-ignore:
      - '**.md'
      - 'Docs/**'
      - '.claude/**'
      - '**.ps1'
  workflow_dispatch:

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  CONFIGURATION: Release

jobs:
  build:
    name: Build and Test
    runs-on: ${{ matrix.os }}

    env:
      SOLUTION_FILE: ${{ github.workspace }}/Functorium.slnx
      COVERAGE_REPORT_DIR: ${{ github.workspace }}/coverage

    strategy:
      matrix:
        os: [ubuntu-24.04]
        dotnet: ['10.0.x']

    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet }}

    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-

    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}

    - name: Check for vulnerable packages
      run: |
        dotnet list ${{ env.SOLUTION_FILE }} package --vulnerable --include-transitive 2>&1 | tee ${{ github.workspace }}/vulnerability-report.txt
        if grep -q "has the following vulnerable packages" ${{ github.workspace }}/vulnerability-report.txt; then
          echo "::warning::Vulnerable packages detected. Review vulnerability-report.txt for details."
        fi

    - name: Build
      run: |
        dotnet build ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-restore \
          -p:MinVerVerbosity=normal

    - name: Test with coverage (MTP)
      run: |
        dotnet test \
          --solution ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-build \
          -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx

    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: ${{ github.workspace }}/**/TestResults/**/*.trx
        retention-days: 30

    - name: Generate coverage report
      if: success()
      uses: danielpalme/ReportGenerator-GitHub-Action@v5.4.4
      with:
        reports: '${{ github.workspace }}/**/TestResults/**/coverage.cobertura.xml'
        targetdir: '${{ env.COVERAGE_REPORT_DIR }}'
        reporttypes: 'Html;Cobertura;MarkdownSummaryGithub'
        assemblyfilters: '-*.Tests.*'
        filefilters: '-*.g.cs'
        verbosity: 'Warning'

    - name: Display coverage summary
      if: success()
      run: |
        cat ${{ env.COVERAGE_REPORT_DIR }}/SummaryGithub.md >> $GITHUB_STEP_SUMMARY

    - name: Upload coverage report
      if: success()
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: '${{ env.COVERAGE_REPORT_DIR }}'
        retention-days: 30
```

### 실행 방법

```bash
# Push to main - Build 워크플로우 자동 실행
git push origin main

# Pull Request - Build 워크플로우 자동 실행
gh pr create --base main --head feature/new-feature

# 수동 실행 - GitHub Actions 탭 > Build > Run workflow
```

---

## Publish 워크플로우

### 워크플로우 파일

`.github/workflows/publish.yml`:

```yaml
name: Publish

on:
  push:
    tags:
      - 'v*.*.*'

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  CONFIGURATION: Release

permissions:
  contents: write

jobs:
  release:
    name: Build and Publish Release
    runs-on: ${{ matrix.os }}

    env:
      SOLUTION_FILE: ${{ github.workspace }}/Functorium.slnx
      COVERAGE_REPORT_DIR: ${{ github.workspace }}/coverage

    strategy:
      matrix:
        os: [ubuntu-24.04]
        dotnet: ['10.0.x']

    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet }}

    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-

    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}

    - name: Check for vulnerable packages
      run: |
        dotnet list ${{ env.SOLUTION_FILE }} package --vulnerable --include-transitive 2>&1 | tee ${{ github.workspace }}/vulnerability-report.txt
        if grep -q "has the following vulnerable packages" ${{ github.workspace }}/vulnerability-report.txt; then
          echo "::warning::Vulnerable packages detected. Review vulnerability-report.txt for details."
        fi

    - name: Build
      run: |
        dotnet build ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-restore \
          -p:MinVerVerbosity=normal

    - name: Test with coverage (MTP)
      run: |
        dotnet test \
          --solution ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-build \
          -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx

    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: ${{ github.workspace }}/**/TestResults/**/*.trx
        retention-days: 30

    - name: Generate coverage report
      if: success()
      uses: danielpalme/ReportGenerator-GitHub-Action@v5.4.4
      with:
        reports: '${{ github.workspace }}/**/TestResults/**/coverage.cobertura.xml'
        targetdir: '${{ env.COVERAGE_REPORT_DIR }}'
        reporttypes: 'Html;Cobertura;MarkdownSummaryGithub'
        assemblyfilters: '-*.Tests.*'
        filefilters: '-*.g.cs'
        verbosity: 'Warning'

    - name: Display coverage summary
      if: success()
      run: |
        cat ${{ env.COVERAGE_REPORT_DIR }}/SummaryGithub.md >> $GITHUB_STEP_SUMMARY

    - name: Upload coverage report
      if: success()
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: '${{ env.COVERAGE_REPORT_DIR }}'
        retention-days: 30

    # - name: Pack NuGet packages
    #   run: |
    #     # Pack only Src projects (exclude Tests)
    #     for project in $(find ./Src -name "*.csproj" ! -name "*Tests*"); do
    #       echo "Packing: $project"
    #       dotnet pack "$project" \
    #         --configuration ${{ env.CONFIGURATION }} \
    #         --no-build \
    #         --output ./packages
    #     done

    # - name: List packages
    #   run: |
    #     echo "=== NuGet Packages ==="
    #     ls -la ./packages/*.nupkg 2>/dev/null || echo "No .nupkg files"
    #     echo ""
    #     echo "=== Symbol Packages ==="
    #     ls -la ./packages/*.snupkg 2>/dev/null || echo "No .snupkg files"

    # - name: Publish to NuGet.org
    #   if: startsWith(github.ref, 'refs/tags/v')
    #   run: |
    #     # Push NuGet packages (.nupkg)
    #     # Symbol packages (.snupkg) are automatically pushed when pushing to NuGet.org
    #     dotnet nuget push ./packages/*.nupkg \
    #       --api-key ${{ secrets.NUGET_API_KEY }} \
    #       --source https://api.nuget.org/v3/index.json \
    #       --skip-duplicate
    #   continue-on-error: true

    # - name: Publish to GitHub Packages
    #   if: startsWith(github.ref, 'refs/tags/v')
    #   run: |
    #     dotnet nuget push ./packages/*.nupkg \
    #       --api-key ${{ secrets.GITHUB_TOKEN }} \
    #       --source https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json \
    #       --skip-duplicate
    #   continue-on-error: true

    # Custom Release Notes Integration
    # Checks for .release-notes/RELEASE-{version}.md file generated by /release-note command
    # Falls back to auto-generated notes if file not found
    # Generate custom notes with: /release-note [version] [topic]

    - name: Extract version from tag
      id: version
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        # Extract version (e.g., refs/tags/v1.0.0 → v1.0.0)
        VERSION=${GITHUB_REF#refs/tags/}
        echo "version=$VERSION" >> $GITHUB_OUTPUT
        echo "Extracted version: $VERSION"

    - name: Check for custom release notes
      id: release_notes
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        VERSION=${{ steps.version.outputs.version }}
        RELEASE_NOTE_FILE=".release-notes/RELEASE-${VERSION}.md"

        if [ -f "$RELEASE_NOTE_FILE" ]; then
          echo "exists=true" >> $GITHUB_OUTPUT
          echo "file=$RELEASE_NOTE_FILE" >> $GITHUB_OUTPUT
          echo "✓ Found custom release notes: $RELEASE_NOTE_FILE"
        else
          echo "exists=false" >> $GITHUB_OUTPUT
          echo "⚠ No custom release notes found, will use auto-generated notes"
          echo "::notice::Release note file not found: $RELEASE_NOTE_FILE"
        fi

    # Delete existing release assets before creating/updating release
    # This prevents stale files from previous releases when re-releasing with same tag
    - name: Delete existing release assets
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        VERSION=${{ steps.version.outputs.version }}

        # Check if release exists
        if gh release view "$VERSION" &>/dev/null; then
          echo "Found existing release: $VERSION"

          # Get and delete all assets
          ASSETS=$(gh release view "$VERSION" --json assets -q '.assets[].name')
          if [ -n "$ASSETS" ]; then
            echo "Deleting existing assets..."
            echo "$ASSETS" | while read -r asset; do
              if [ -n "$asset" ]; then
                echo "  Deleting: $asset"
                gh release delete-asset "$VERSION" "$asset" --yes
              fi
            done
            echo "✓ All existing assets deleted"
          else
            echo "No existing assets to delete"
          fi
        else
          echo "No existing release found for $VERSION"
        fi
      env:
        GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}

    - name: Create GitHub Release
      uses: softprops/action-gh-release@v2
      if: startsWith(github.ref, 'refs/tags/v')
      with:
        # files: |
        #   ./packages/*.nupkg
        #   ./packages/*.snupkg
        body_path: ${{ steps.release_notes.outputs.exists == 'true' && steps.release_notes.outputs.file || '' }}
        generate_release_notes: ${{ steps.release_notes.outputs.exists != 'true' }}
        draft: false
        prerelease: ${{ contains(github.ref, '-') }}
        fail_on_unmatched_files: false
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### 릴리스 프로세스

```bash
# 1. 버전 태그 생성
git tag -a v1.0.0 -m "Release 1.0.0"

# 2. 태그 푸시 (Release 워크플로우 자동 실행)
git push origin v1.0.0

# 3. GitHub에서 확인
# - Actions 탭: 워크플로우 실행 상태
# - Releases 탭: 생성된 릴리스
```

---

워크플로우가 빌드와 배포를 자동화한다면, NuGet 패키지 설정은 배포되는 패키지의 메타데이터와 디버깅 지원을 결정합니다.

## NuGet 패키지 설정

### Directory.Build.props 공통 설정

모든 프로젝트에 적용되는 NuGet 메타데이터 설정입니다.

```xml
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
</PropertyGroup>
```

### 심볼 패키지 설정

디버깅 지원을 위한 심볼 패키지 생성 설정입니다.

```xml
<!-- Symbol Package for Debugging -->
<PropertyGroup>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

- `.snupkg` 파일에 PDB 포함
- NuGet.org 심볼 서버에 자동 업로드
- Visual Studio에서 Step-into 디버깅 가능

### SourceLink 설정

GitHub 소스와 연결하여 디버깅 시 원본 코드 접근을 지원합니다.

```xml
<!-- Source Link for Debugging -->
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
</ItemGroup>
```

### Deterministic Build 설정

CI 환경에서 재현 가능한 빌드를 위한 설정입니다.

```xml
<!-- Deterministic Build -->
<PropertyGroup>
  <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

### 심볼 패키지와 SourceLink의 역할 차이

다음 테이블은 심볼 패키지와 SourceLink가 디버깅에서 각각 어떤 정보를 제공하는지 비교합니다.

| Feature | 역할 | 제공 정보 |
|------|------|----------|
| **심볼 패키지 (.snupkg)** | "어디서" 실행 중인지 | 메서드 이름, 라인 번호, 변수 이름 |
| **SourceLink** | "무엇을" 실행 중인지 | 실제 소스 코드 내용 |

둘 다 있어야 디버깅 시 라이브러리 소스에 Step-into 가능합니다.

---

## 프로젝트별 패키지 설정

### Functorium.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Functorium</PackageId>
    <Description>Functorium - A functional domain framework for .NET</Description>
    <PackageTags>$(PackageTags);functional-programming;domain-driven-design;ddd</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Functorium.png" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

### Functorium.Testing.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Functorium.Testing</PackageId>
    <Description>Functorium.Testing - Testing utilities for a functional domain framework for .NET</Description>
    <PackageTags>$(PackageTags);testing;functional-programming;domain-driven-design;ddd</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Functorium.png" Pack="true" PackagePath="\" />
  </ItemGroup>

  <!-- Not a test project (produces NuGet package) -->
  <PropertyGroup>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>
</Project>
```

> **Note**: `IsTestProject`를 `false`로 설정해야 NuGet 패키지 생성이 가능합니다.

### 새 패키지 프로젝트 추가

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Functorium.NewPackage</PackageId>
    <Description>Functorium.NewPackage - Description here</Description>
    <PackageTags>$(PackageTags);additional;tags</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Functorium.png" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

필수 설정:
1. `PackageId`: 고유한 패키지 이름
2. `Description`: 패키지 설명
3. `PackageTags`: 공통 태그 + 추가 태그
4. Package Files: README.md, Functorium.png 포함

---

## 패키지 생성

### Build-Local.ps1 사용 (권장)

```bash
# 전체 빌드 및 패키지 생성
./Build-Local.ps1

# 패키지 생성 건너뛰기
./Build-Local.ps1 -SkipPack
```

### dotnet CLI 직접 사용

```bash
# 솔루션 전체 패키지 생성
dotnet pack -c Release -o .nupkg

# 특정 프로젝트만
dotnet pack Src/Functorium/Functorium.csproj -c Release -o .nupkg

# 버전 지정
dotnet pack -c Release -o .nupkg -p:Version=1.0.0
```

### 출력 파일

| File | Description |
|------|------|
| `*.nupkg` | NuGet 패키지 (배포용) |
| `*.snupkg` | 심볼 패키지 (디버깅용) |

---

## 패키지 검증

### 패키지 내용 확인

```bash
# 패키지 내용 확인
unzip -l .nupkg/Functorium.1.0.0.nupkg

# dotnet CLI 검증
dotnet nuget verify .nupkg/Functorium.1.0.0.nupkg
```

### 로컬 테스트

```bash
# 1. 로컬 NuGet 소스 추가
dotnet nuget add source .nupkg/ --name local

# 2. 패키지 설치 테스트
dotnet add package Functorium --source local

# 3. 빌드 확인
dotnet build
```

---

## 초기 설정

### GitHub Secrets

| Secret | Purpose | 필수 여부 | 획득 방법 |
|--------|------|----------|----------|
| `NUGET_API_KEY` | NuGet.org 배포 | Release 시 필수 | NuGet.org > Account > API Keys |
| `CODECOV_TOKEN` | Codecov 업로드 | 선택 (현재 비활성화) | Codecov.io |

`GITHUB_TOKEN`은 자동 제공됩니다 (GitHub Packages, Release 생성).

### GitHub Secrets 추가 방법

1. GitHub 저장소 > **Settings** > **Secrets and variables** > **Actions**
2. **New repository secret** 클릭
3. Name: `NUGET_API_KEY`, Secret: [NuGet API Key 값]
4. **Add secret** 클릭

### NuGet.org API Key 생성

1. NuGet.org 로그인 > 계정 > **API Keys**
2. **Create** 클릭
3. 설정: Key Name, Package Owner, Glob Pattern: `Functorium.*`, Scopes: **Push**
4. **Create** 클릭 후 API Key 복사

### 워크플로우 권한 설정

Settings > Actions > General:
- Workflow permissions: **Read and write permissions**
- **Allow GitHub Actions to create and approve pull requests** 체크

---

CI/CD 파이프라인과 패키지 설정이 갖춰졌으면, 이제 패키지 버전을 자동으로 결정하는 MinVer 기반 버전 관리를 살펴봅니다.

## 버전 관리 개요

### MinVer란?

MinVer는 Git 태그를 기반으로 .NET 프로젝트의 버전을 자동으로 계산하는 MSBuild 도구입니다.

### 주요 특징

- **태그 기반**: Git 태그만으로 버전 관리
- **제로 설정**: 기본 설정만으로 바로 사용 가능
- **빠른 속도**: 최소한의 Git 명령만 실행
- **SemVer 2.0**: 시맨틱 버전 규칙 준수

### 기존 방식과 비교

다음 테이블은 수동 버전 관리와 MinVer 자동 버전 관리의 차이를 비교합니다.

| 기존 방식 | MinVer 방식 |
|----------|------------|
| 수동으로 버전 파일 수정 | Git 태그로 자동 계산 |
| 버전 불일치 위험 | 태그와 항상 일치 |
| 릴리스 시 추가 작업 | 태그만 푸시 |

---

## 버전 구조

### 전체 구조

```
{Major}.{Minor}.{Patch}-{Identifier}.{Phase}.{Height}+{Commit}
    |      |      |         |         |        |        |
    |      |      |         |         |        |        +-- 커밋 해시 (short)
    |      |      |         |         |        +----------- Height (자동 증가: 커밋 건수)
    |      |      |         |         +-------------------- Phase (수동 변경)
    |      |      |         +------------------------------ Identifier (수동 변경)
    |      |      +---------------------------------------- Patch (자동 증가*: RTM 태그 후)
    |      +----------------------------------------------- Minor
    +------------------------------------------------------ Major
```

### 요소별 설명

| 요소 | Description | 변경 방식 |
|------|------|----------|
| **Major** | 호환성을 깨는 변경 시 증가 | 수동 (태그) |
| **Minor** | 새로운 기능 추가 시 증가 | 수동 (태그) |
| **Patch** | 버그 수정 시 증가 | 수동 (태그) / 자동 표시* |
| **Identifier** | Pre-release 단계 (alpha, beta, rc) | 수동 (태그) |
| **Phase** | 동일 Identifier 내 단계 번호 (0부터 시작) | 수동 (태그) |
| **Height** | 최근 태그 이후 커밋 수 | 자동 증가 |
| **Commit** | 현재 커밋의 short 해시 (빌드 메타데이터) | 자동 |

\* RTM 태그 후 `MinVerAutoIncrement` 설정에 따라 자동 +1 표시

### 태그 형식

```bash
vX.X.0-alpha.0   # 처음 pre-release
vX.X.0           # 처음 stable release
vX.X.1           # 다음 patch release
vX.Y.0           # 다음 minor release
vY.0.0           # 다음 major release
```

### 자동 vs 수동 증가

| 요소 | 증가 방식 | 트리거 |
|------|----------|--------|
| **Height** | 자동 증가 | 커밋할 때마다 |
| **Phase** | 수동 변경 | Git 태그 생성 시에만 |
| **Identifier** | 수동 변경 | Git 태그 생성 시에만 |
| **Patch/Minor/Major** | 자동 표시 | RTM 태그 후 (실제 변경은 태그만) |

---

## 설치 및 설정

### Central Package Management 사용 시

**Directory.Packages.props:**

```xml
<Project>
  <ItemGroup Label="Versioning">
    <PackageVersion Include="MinVer" Version="6.0.0" />
  </ItemGroup>
</Project>
```

**Directory.Build.props:**

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="MinVer">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### 권장 설정 (현재 프로젝트)

```xml
<PropertyGroup>
  <MinVerTagPrefix>v</MinVerTagPrefix>
  <MinVerVerbosity>minimal</MinVerVerbosity>
  <MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
  <MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
  <MinVerAutoIncrement>patch</MinVerAutoIncrement>
  <MinVerWorkingDirectory>$(MSBuildThisFileDirectory)</MinVerWorkingDirectory>
</PropertyGroup>

<!-- AssemblyVersion은 MSBuild Target에서 설정 (MinVer 계산 후 실행) -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

### 버전 확인 명령

```bash
# 기본 빌드
dotnet build

# 상세 버전 정보
dotnet build -p:MinVerVerbosity=normal

# 진단 정보
dotnet build -p:MinVerVerbosity=diagnostic
```

---

## 주요 설정 옵션

### MinVerTagPrefix

```xml
<MinVerTagPrefix>v</MinVerTagPrefix>
```

| 설정 값 | 인식 태그 | 무시 태그 |
|--------|---------|----------|
| `v` | v1.0.0, v2.0.0 | 1.0.0, ver1.0.0 |
| `ver` | ver1.0.0 | v1.0.0, 1.0.0 |
| (빈 문자열) | 1.0.0 | v1.0.0 |

권장: `v` (GitHub 표준)

### MinVerVerbosity

| Value | 출력 내용 | 사용 시기 |
|----|---------|----------|
| `minimal` | 경고/오류만 | 일반 빌드 |
| `normal` | 버전 계산 과정 | 버전 확인 |
| `diagnostic` | 상세 디버그 정보 | 문제 해결 |

### MinVerDefaultPreReleaseIdentifiers

태그 없을 때 사용할 prerelease suffix:

```xml
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
```

**Pre-release 단계:**

| 단계 | 의미 | 사용 시점 |
|------|------|-----------|
| `alpha` | 알파 버전 | 초기 개발, 기능 불완전, 불안정 (기본값) |
| `beta` | 베타 버전 | 기능 완성, 테스트 중, 버그 수정 중 |
| `rc` | Release Candidate | 릴리스 후보, 최종 테스트 |

버전 비교 순서: `alpha < beta < rc < (stable)`

### MinVerAutoIncrement

RTM 태그 후 자동 증가 단위:

| Value | 동작 | Example |
|----|------|------|
| `patch` (기본값) | Patch 버전 +1 표시 | v1.0.0 → 1.0.1-alpha.0.1 |
| `minor` | Minor 버전 +1 표시 | v1.0.0 → 1.1.0-alpha.0.1 |
| `major` | Major 버전 +1 표시 | v1.0.0 → 2.0.0-alpha.0.1 |

> **중요:** 이것은 "표시만" 증가시킵니다. 실제 버전은 Git 태그로만 변경됩니다.

### MinVerMinimumMajorMinor

태그 없을 때 최소 버전 설정:

```xml
<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
```

`0.0.0` 버전 방지 효과: 태그 없어도 `1.0.0-alpha.0.N` 사용

---

## 버전 계산 방식

### 태그 없을 때

```bash
# Git 히스토리: 18개 커밋, 태그 없음
# 계산 결과: 0.0.0-alpha.0.18
```

### 태그가 있을 때 (Height = 0)

```bash
# HEAD에 v1.0.0 태그
# 계산 결과: 1.0.0
```

### 태그 후 커밋 (Height > 0)

```bash
# v1.0.0 태그 후 5개 커밋
# 계산 결과: 1.0.1-alpha.0.5
```

### Prerelease 태그

```bash
# v1.0.0-rc.1 태그
# 계산 결과: 1.0.0-rc.1

# 이후 1개 커밋
# 계산 결과: 1.0.0-rc.1.1
```

### 여러 태그가 있을 때

현재 커밋의 조상 중 가장 가까운 태그를 사용합니다:

```bash
# v1.1.0 태그 후 3개 커밋
# 계산 결과: 1.1.1-alpha.0.3
```

---

## 버전 진행 시나리오

### Pre-release에서 Stable까지

```bash
# Alpha 단계
git tag v25.13.0-alpha.0     # → 25.13.0-alpha.0
# 3개 커밋                    # → 25.13.0-alpha.0.1 ~ .3

# Beta 단계
git tag v25.13.0-beta.0      # → 25.13.0-beta.0
# 2개 커밋                    # → 25.13.0-beta.0.1 ~ .2

# Release Candidate
git tag v25.13.0-rc.0        # → 25.13.0-rc.0
# 1개 커밋                    # → 25.13.0-rc.0.1

# 정식 릴리스
git tag v25.13.0             # → 25.13.0 (stable)
```

### 다음 Patch 버전

```bash
# v25.13.0 릴리스 후 개발 계속
# (MinVerAutoIncrement=patch → Patch +1 자동 표시)
# 2개 커밋                    # → 25.13.1-alpha.0.1 ~ .2

# 다음 Patch 릴리스
git tag v25.13.1             # → 25.13.1 (stable)
```

### 주요 포인트

1. Height는 커밋할 때마다 자동 증가
2. Phase는 Git 태그로만 변경 가능
3. alpha -> beta -> rc 진행은 선택적 (단계 생략 가능)

---

## Height의 실질적 활용

### 빌드 고유성 보장

태그 없이도 모든 빌드가 고유한 버전 번호를 가집니다:

```bash
v1.0.0-alpha.0.3  # 3번째 커밋
v1.0.0-alpha.0.4  # 4번째 커밋
v1.0.0-alpha.0.5  # 5번째 커밋
```

NuGet 패키지 관리자에서 각 버전을 명확히 구분하며, 버전 충돌 없이 CI/CD 자동 배포가 가능합니다.

### 추적 가능성

```bash
# 버전에서 커밋 위치 파악
1.0.0-alpha.0.47
# → "alpha.0 태그로부터 47 커밋 후" 즉시 확인
```

### SemVer 2.0 정렬 규칙

```bash
1.0.0-alpha.0.5   <
1.0.0-alpha.0.6   <
1.0.0-alpha.1     <  (태그 생성 시 자동 "승격")
1.0.0-alpha.1.1   <
1.0.0-alpha.2
```

### 철학

MinVer의 철학은 **"태그는 의미 있는 마일스톤에만, 나머지는 자동"**입니다.

---

## 어셈블리 버전 전략

.NET 어셈블리는 3가지 버전 속성을 가집니다:

| 속성 | 목적 | 형식 | 값 예시 |
|------|------|------|---------|
| **AssemblyVersion** | 바이너리 호환성 | Major.Minor.0.0 | 1.0.0.0 |
| **FileVersion** | 파일 속성 표시 | Major.Minor.Patch.0 | 1.0.1.0 |
| **InformationalVersion** | 제품 버전 (사용자용) | 전체 SemVer | 1.0.1-alpha.0.5+abc123 |

### AssemblyVersion에 Patch를 포함하지 않는 이유

AssemblyVersion은 바이너리 호환성을 결정합니다. Patch를 포함하면 버그 수정마다 참조하는 모든 어셈블리를 재컴파일해야 합니다.

```xml
<!-- 권장 (현재 프로젝트 설정) -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

**예시:**

```bash
v1.0.0: AssemblyVersion=1.0.0.0, FileVersion=1.0.0.0
v1.0.1: AssemblyVersion=1.0.0.0, FileVersion=1.0.1.0  # 재컴파일 불필요
v1.0.2: AssemblyVersion=1.0.0.0, FileVersion=1.0.2.0  # 재컴파일 불필요
v1.1.0: AssemblyVersion=1.1.0.0, FileVersion=1.1.0.0  # Minor 변경 - 재컴파일 필요
```

### MSBuild 속성

| 속성 | 값 예시 | Description |
|------|---------|------|
| `$(MinVerVersion)` | 1.0.0 | 전체 SemVer 버전 |
| `$(MinVerMajor)` | 1 | Major 버전 |
| `$(MinVerMinor)` | 0 | Minor 버전 |
| `$(MinVerPatch)` | 0 | Patch 버전 |
| `$(MinVerPreRelease)` | alpha.0.5 | Prerelease 부분 |
| `$(MinVerBuildMetadata)` | abc123 | 빌드 메타데이터 |

---

버전 계산 방식을 이해했으면, 마지막으로 커밋 히스토리에서 다음 버전을 자동으로 제안하는 명령을 살펴봅니다.

## 다음 버전 제안 명령

### 개요

`/suggest-next-version` 명령은 Conventional Commits 히스토리를 분석하여 Semantic Versioning에 따른 다음 릴리스 버전 태그를 제안합니다.

### 사용법

```bash
/suggest-next-version          # 정식 버전 제안
/suggest-next-version alpha    # 알파 버전 제안
/suggest-next-version beta     # 베타 버전 제안
/suggest-next-version rc       # RC 버전 제안
```

### 버전 증가 규칙 (Conventional Commits)

| 커밋 타입 | 버전 증가 | Example |
|-----------|-----------|------|
| `feat!`, `BREAKING CHANGE` | Major | v1.0.0 → v2.0.0 |
| `feat` | Minor | v1.0.0 → v1.1.0 |
| `fix`, `perf` | Patch | v1.0.0 → v1.0.1 |
| `docs`, `style`, `refactor`, `test`, `build`, `ci`, `chore` | 없음 | 버전 증가 불필요 |

우선순위: `Major > Minor > Patch`

### 실행 절차

1. **현재 버전 확인**: `git describe --tags --abbrev=0`
2. **커밋 히스토리 분석**: 마지막 태그 이후 커밋 분류
3. **버전 증가 결정**: 가장 높은 수준의 변경 기준
4. **결과 출력**: 제안 버전과 git 명령어 표시

### 출력 예시

```
태그 제안 결과

현재 버전: v1.2.3
제안 버전: v1.3.0

버전 증가 이유:
  - feat 커밋 3개 발견 (Minor 증가)
  - fix 커밋 5개 발견

태그 생성 명령어:
  git tag v1.3.0
  git push origin v1.3.0
```

### 프리릴리스 지원

| Type | Description | 버전 예시 |
|------|------|----------|
| `alpha` | 알파 버전 (초기 개발 단계) | v1.3.0-alpha.0 |
| `beta` | 베타 버전 (기능 완료, 테스트 단계) | v1.3.0-beta.0 |
| `rc` | Release Candidate (출시 후보) | v1.3.0-rc.0 |

> **Note**: `/suggest-next-version` 명령은 제안만 합니다. 실제 태그 생성은 사용자가 명령어를 직접 실행해야 합니다.

---

## Troubleshooting

### 워크플로우가 실행되지 않을 때

**원인**: 워크플로우 파일 구문 오류 또는 권한 부족

**Resolution:**
1. YAML 파일 검증 (VS Code YAML extension 또는 yamllint.com)
2. Settings > Actions > General에서 권한 확인

### 버전이 0.0.0으로 표시될 때

```bash
# 프로젝트 Version 속성 확인
dotnet build -v detailed | grep Version
```

### NuGet 배포 실패

| Cause | Solution |
|------|------|
| API Key 없음 | GitHub Secrets에 `NUGET_API_KEY` 확인 |
| 패키지 이름 중복 | 프로젝트 이름 변경 또는 패키지 소유자에게 권한 요청 |
| API Key 권한 부족 | NuGet.org에서 Push 권한과 Glob Pattern 확인 |

### CI에서 버전이 0.0.0으로 나올 때

Shallow clone으로 Git 히스토리가 누락된 경우:

```yaml
# GitHub Actions
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0  # 전체 히스토리 가져오기
```

### README.md가 패키지에 포함되지 않음

csproj에 Pack 설정 추가:

```xml
<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### 버전이 0.0.0-alpha.0.N으로 표시될 때

| Cause | Solution |
|------|------|
| Git 태그 없음 | `git tag -a v0.1.0 -m "Initial version"` |
| 태그 접두사 불일치 | `<MinVerTagPrefix>` 설정 확인 |
| 최소 버전 미설정 | `<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>` |

### 태그를 생성했는데도 버전이 안 바뀔 때

```bash
# 태그 형식 확인
git tag v1.0.0       # O - 올바른 형식
git tag 1.0.0        # X - 접두사 없음
git tag v1.0         # X - Patch 버전 누락

# 현재 브랜치의 태그 확인
git tag --merged
```

### MinVer가 실행되지 않을 때

```bash
# 캐시 정리 후 재빌드
dotnet clean
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

### 한글 경로 문제

Git 저장소가 한글 경로에 있으면 MinVer가 경로 인식에 실패할 수 있습니다. 프로젝트를 영문 경로로 이동하세요.

---

## FAQ

### Q1. 매번 태그를 푸시해야 하나요?

**A:** 릴리스를 배포할 때만 태그를 푸시합니다.

```bash
# 일반 개발 - CI만 실행 (빌드, 테스트)
git commit -m "feat: new feature"
git push origin main

# 릴리스 - Release 워크플로우 실행 (빌드, 테스트, 배포)
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
```

### Q2. Preview 버전도 배포할 수 있나요?

**A:** 네, prerelease 태그를 사용합니다. 워크플로우가 자동으로 prerelease 감지합니다:

```bash
git tag -a v1.0.0-rc.1 -m "Release Candidate 1"
git push origin v1.0.0-rc.1
```

```yaml
prerelease: ${{ contains(github.ref, '-') }}
```

### Q3. 특정 프로젝트만 배포하려면?

**A:** `dotnet pack`에서 프로젝트를 지정합니다:

```yaml
- name: Pack NuGet packages
  run: |
    dotnet pack Src/Functorium/Functorium.csproj -c Release --no-build --output ./packages
    dotnet pack Src/Functorium.Testing/Functorium.Testing.csproj -c Release --no-build --output ./packages
```

### Q4. 배포 전 수동 승인이 필요하다면?

**A:** GitHub Environment protection rules를 사용합니다:

1. Settings > Environments > **New environment** (`production`)
2. **Required reviewers** 추가
3. publish.yml에 `environment: production` 추가

### Q5. 여러 .NET 버전에서 테스트하려면?

**A:** Matrix 빌드를 사용합니다:

```yaml
strategy:
  matrix:
    dotnet-version: ['8.0.x', '9.0.x', '10.0.x']
```

### Q6. 배포된 패키지를 어떻게 사용하나요?

**A:**

```bash
# NuGet.org에서 설치
dotnet add package Functorium --version 1.0.0

# Pre-release 설치
dotnet add package Functorium --prerelease
```

### Q7. PackageId는 어떻게 정하나요?

**A:** 네임스페이스와 일치시키고, 소문자와 점(.)을 사용합니다:

```xml
<!-- 권장 -->
<PackageId>Functorium</PackageId>
<PackageId>Functorium.Testing</PackageId>
<PackageId>Functorium.Extensions.Http</PackageId>
```

### Q8. Directory.Build.props와 csproj 설정이 충돌하면?

**A:** csproj 설정이 우선합니다:

```xml
<!-- Directory.Build.props -->
<PackageTags>functorium;functional</PackageTags>

<!-- Functorium.csproj - 태그 추가 -->
<PackageTags>$(PackageTags);ddd</PackageTags>
<!-- 결과: functorium;functional;ddd -->
```

### Q9. MinVer와 GitVersion의 차이점은?

**A:**

| Item | MinVer | GitVersion |
|------|--------|-----------|
| 복잡도 | 단순 (태그만) | 복잡 (브랜치 전략) |
| 설정 | 최소 설정 | 상세 설정 파일 필요 |
| 브랜치 전략 | 지원 안 함 | GitFlow, GitHub Flow 등 |
| 속도 | 빠름 | 상대적으로 느림 |

### Q10. Pre-release 단계를 변경하려면?

**A:** `MinVerDefaultPreReleaseIdentifiers`를 변경합니다:

```xml
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
<MinVerDefaultPreReleaseIdentifiers>beta.0</MinVerDefaultPreReleaseIdentifiers>
<MinVerDefaultPreReleaseIdentifiers>rc.0</MinVerDefaultPreReleaseIdentifiers>
```

### Q11. 태그 없이 특정 버전으로 빌드하려면?

**A:** `MinVerVersion`으로 재정의합니다:

```bash
dotnet build -p:MinVerVersion=1.2.3
dotnet pack -p:MinVerVersion=1.2.3
```

### Q12. Hotfix 릴리스를 어떻게 관리하나요?

**A:** 이전 릴리스 태그에서 브랜치를 생성하고 새 태그를 만듭니다:

```bash
git checkout v1.0.0
git checkout -b hotfix/1.0.1
git commit -m "fix: critical bug"
git tag -a v1.0.1 -m "Hotfix 1.0.1"
git push origin v1.0.1
git checkout main
git merge hotfix/1.0.1
```

### Q13. NuGet 패키지 버전은 어떻게 설정되나요?

**A:** MinVer가 자동으로 `<Version>` 속성을 설정합니다:

```bash
dotnet pack
# Functorium.1.0.0.nupkg
```

### Q14. 로컬 빌드와 CI 빌드 버전을 구분하려면?

**A:** 빌드 메타데이터를 사용합니다:

```yaml
# CI (GitHub Actions)
- run: dotnet build -p:MinVerBuildMetadata=ci.${{ github.run_number }}
  # 결과: 1.0.0+ci.123
```

### Q15. Breaking Change는 어떻게 감지하나요?

**A:** 두 가지 방법으로 감지합니다:

1. 타입 뒤 느낌표: `feat!`, `fix!`
2. 푸터의 `BREAKING CHANGE:` 포함

```
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답이 배열에서 객체로 변경됨
```

---

## References

| 문서 | Description |
|------|------|
| [GitHub Actions 문서](https://docs.github.com/actions) | GitHub Actions 공식 문서 |
| [NuGet 배포 가이드](https://learn.microsoft.com/nuget/nuget-org/publish-a-package) | NuGet.org 배포 공식 가이드 |
| [SourceLink 문서](https://github.com/dotnet/sourcelink) | SourceLink 디버깅 설정 |
| [MinVer GitHub](https://github.com/adamralph/minver) | MinVer 공식 저장소 |
| [Semantic Versioning 2.0.0](https://semver.org/) | SemVer 공식 문서 |
| [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) | Conventional Commits 공식 문서 |
