# CI/CD 워크플로우

이 문서는 GitHub Actions를 사용한 CI/CD 워크플로우 설정과 NuGet 패키지 배포 방법을 설명합니다.

## 목차

- [요약](#요약)
- [개요](#개요)
- [워크플로우 구성](#워크플로우-구성)
- [Build 워크플로우](#build-워크플로우)
- [Publish 워크플로우](#publish-워크플로우)
- [NuGet 패키지 설정](#nuget-패키지-설정)
- [프로젝트별 패키지 설정](#프로젝트별-패키지-설정)
- [패키지 생성](#패키지-생성)
- [패키지 검증](#패키지-검증)
- [초기 설정](#초기-설정)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

---

## 요약

### 주요 명령

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

### 주요 절차

**1. 일반 개발 (CI만 실행):**
1. 코드 변경 후 `git push origin main` 또는 PR 생성
2. Build 워크플로우 자동 실행 (빌드 → 테스트 → 커버리지)

**2. 릴리스 배포:**
1. `git tag -a v1.0.0 -m "Release 1.0.0"` 태그 생성
2. `git push origin v1.0.0` 태그 푸시
3. Publish 워크플로우 자동 실행 (빌드 → 테스트 → 패키지 배포 → GitHub Release)

### 주요 개념

| 개념 | 설명 |
|------|------|
| Build 워크플로우 | PR/Push 시 빌드, 테스트, 커버리지 수집 |
| Publish 워크플로우 | 태그 푸시(v*.*.*) 시 빌드 + 패키지 배포 + GitHub Release |
| NuGet 메타데이터 | `Directory.Build.props`에서 공통 설정, csproj에서 프로젝트별 설정 |
| SourceLink + 심볼 패키지 | 디버깅 시 라이브러리 소스 Step-into 지원 |
| Deterministic Build | CI 환경에서 재현 가능한 빌드 보장 |

---

## 개요

### 목적

Git 태그 기반 자동 버전 관리와 CI/CD 파이프라인을 통해 안정적인 빌드와 배포를 자동화합니다.

### 워크플로우 구성

| 워크플로우 | 트리거 | 주요 작업 |
|-----------|--------|----------|
| CI (build.yml) | PR, Push to main | 빌드, 테스트, 커버리지 |
| Release (publish.yml) | 태그 푸시 (v*.*.*) | 빌드, 테스트, 패키지 배포 |

### 생성되는 패키지

| 패키지 | 설명 |
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

| 기능 | 역할 | 제공 정보 |
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

> **참고**: `IsTestProject`를 `false`로 설정해야 NuGet 패키지 생성이 가능합니다.

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

| 파일 | 설명 |
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

| Secret | 용도 | 필수 여부 | 획득 방법 |
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

## 트러블슈팅

### 워크플로우가 실행되지 않을 때

**원인**: 워크플로우 파일 구문 오류 또는 권한 부족

**해결:**
1. YAML 파일 검증 (VS Code YAML extension 또는 yamllint.com)
2. Settings > Actions > General에서 권한 확인

### 버전이 0.0.0으로 표시될 때

```bash
# 프로젝트 Version 속성 확인
dotnet build -v detailed | grep Version
```

### NuGet 배포 실패

| 원인 | 해결 |
|------|------|
| API Key 없음 | GitHub Secrets에 `NUGET_API_KEY` 확인 |
| 패키지 이름 중복 | 프로젝트 이름 변경 또는 패키지 소유자에게 권한 요청 |
| API Key 권한 부족 | NuGet.org에서 Push 권한과 Glob Pattern 확인 |

### CI에서 버전이 0.0.0으로 나올 때

Shallow clone으로 Git 히스토리가 누락된 경우:

```yaml
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

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [02c-versioning.md](./02c-versioning.md) | 버전 관리 (MinVer) |
| [GitHub Actions 문서](https://docs.github.com/actions) | GitHub Actions 공식 문서 |
| [NuGet 배포 가이드](https://learn.microsoft.com/nuget/nuget-org/publish-a-package) | NuGet.org 배포 공식 가이드 |
| [SourceLink 문서](https://github.com/dotnet/sourcelink) | SourceLink 디버깅 설정 |
