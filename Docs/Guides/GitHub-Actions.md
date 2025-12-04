# GitHub Actions CI/CD 가이드

이 문서는 MinVer 기반 버전 관리와 자동 빌드/배포를 위한 GitHub Actions 워크플로우 설정을 설명합니다.

## 목차
- [개요](#개요)
- [워크플로우 구성](#워크플로우-구성)
- [초기 설정](#초기-설정)
- [CI 워크플로우](#ci-워크플로우)
- [Release 워크플로우](#release-워크플로우)
- [배포 설정](#배포-설정)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

Git 태그 기반 자동 버전 관리와 CI/CD 파이프라인을 통해 안정적인 빌드와 배포를 자동화합니다.

### 워크플로우 구성

| 워크플로우 | 트리거 | 주요 작업 |
|-----------|--------|----------|
| CI | PR, Push to main | 빌드, 테스트, 커버리지 |
| Release | 태그 푸시 (v*.*.*) | 빌드, 테스트, 패키지 배포 |

### 파일 위치

```
프로젝트루트/
├── .github/
│   └── workflows/
│       ├── build.yml        # Build 워크플로우 (CI)
│       └── release.yml      # Release 워크플로우
└── Docs/
    └── Guides/
        └── GitHub-Actions.md  # 이 문서
```

<br/>

## 워크플로우 구성

### Build 워크플로우 (build.yml)

**트리거:**
- Pull Request to main
- Push to main 브랜치
- 문서/스크립트 파일은 제외 (*.md, Docs/**, .claude/**, *.ps1)
- 수동 실행 (workflow_dispatch)

**작업:**
1. 코드 체크아웃 (전체 Git 히스토리)
2. .NET 10 설정
3. NuGet 패키지 캐시
4. 의존성 복원
5. 취약점 패키지 검사
6. Release 모드 빌드 (MinVer 버전 출력 포함)
7. 테스트 실행 및 커버리지 수집
8. 테스트 결과 업로드
9. ReportGenerator로 커버리지 리포트 생성
10. 커버리지 요약 표시 (GITHUB_STEP_SUMMARY)
11. 커버리지 리포트 업로드

### Release 워크플로우 (release.yml)

**트리거:**
- 태그 푸시: v*.*.* (예: v1.0.0, v1.2.3)

**작업:**
1. 코드 체크아웃 (전체 Git 히스토리)
2. .NET 10 설정
3. 의존성 복원
4. Release 모드 빌드
5. MinVer 버전 확인
6. 테스트 실행
7. NuGet 패키지 생성
8. NuGet.org에 배포 (선택)
9. GitHub Packages에 배포 (선택)
10. GitHub Release 생성

<br/>

## 초기 설정

### 1. GitHub Secrets 설정

**필수 Secrets:**

| Secret | 용도 | 필수 여부 | 획득 방법 |
|--------|------|----------|----------|
| `NUGET_API_KEY` | NuGet.org 배포 | Release 시 필수 | [NuGet.org API Keys](https://www.nuget.org/account/apikeys) |
| `CODECOV_TOKEN` | Codecov 업로드 | 선택 (현재 비활성화) | [Codecov](https://codecov.io/) |

**GITHUB_TOKEN은 자동 제공됨** (GitHub Packages, Release 생성)

> **참고:** 현재 build.yml에서 Codecov 업로드는 주석 처리되어 있습니다. 대신 ReportGenerator로 커버리지 리포트를 생성하고 Artifacts로 업로드합니다.

### 2. GitHub Secrets 추가 방법

1. GitHub 저장소 페이지 접속
2. **Settings** → **Secrets and variables** → **Actions** 클릭
3. **New repository secret** 클릭
4. Secret 이름과 값 입력:
   - Name: `NUGET_API_KEY`
   - Secret: [NuGet API Key 값]
5. **Add secret** 클릭

### 3. NuGet.org API Key 생성

1. [NuGet.org](https://www.nuget.org/) 로그인
2. 계정 메뉴 → **API Keys** 클릭
3. **Create** 클릭
4. 설정:
   - Key Name: `GitHub Actions - Functorium`
   - Package Owner: 본인 계정
   - Glob Pattern: `Functorium.*`
   - Select Scopes: **Push**
5. **Create** 클릭 후 API Key 복사 (한 번만 표시됨)

### 4. Codecov 설정 (선택)

1. [Codecov.io](https://codecov.io/) 접속
2. GitHub 계정으로 로그인
3. 저장소 추가
4. Upload Token 복사
5. GitHub Secrets에 `CODECOV_TOKEN` 추가

<br/>

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
        fetch-depth: 0  # MinVer requires full Git history

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
        dotnet list ${{ env.SOLUTION_FILE }} package --vulnerable --include-transitive 2>&1 | tee vulnerability-report.txt
        if grep -q "has the following vulnerable packages" vulnerability-report.txt; then
          echo "::warning::Vulnerable packages detected. Review vulnerability-report.txt for details."
        fi

    - name: Build
      run: |
        dotnet build ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-restore \
          -p:MinVerVerbosity=normal

    - name: Test with coverage
      run: |
        dotnet test ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-build \
          --collect:"XPlat Code Coverage" \
          --logger "trx" \
          --logger "console;verbosity=minimal" \
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

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
        reporttypes: 'Html;Cobertura;TextSummary;MarkdownSummaryGithub'
        assemblyfilters: '-*.Tests.*'
        verbosity: 'Info'

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

### 실행 시점

**Push to main:**
```bash
git push origin main
# Build 워크플로우 자동 실행
```

**Pull Request:**
```bash
gh pr create --base main --head feature/new-feature
# Build 워크플로우 자동 실행
```

**수동 실행:**
```bash
# GitHub Actions 탭 → Build → Run workflow
```

### 로그 확인

1. GitHub 저장소 → **Actions** 탭
2. 워크플로우 실행 목록에서 선택
3. Job 클릭하여 상세 로그 확인

<br/>

## Release 워크플로우

### 워크플로우 파일

`.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'

env:
  DOTNET_VERSION: '10.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  release:
    name: Build and Publish Release
    runs-on: ubuntu-latest

    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore Functorium.slnx

    - name: Build
      run: dotnet build Functorium.slnx -c Release --no-restore

    - name: Display version
      run: dotnet build Functorium.slnx -c Release --no-restore -p:MinVerVerbosity=normal

    - name: Run tests
      run: dotnet test Functorium.slnx -c Release --no-build --verbosity normal

    - name: Pack NuGet packages
      run: dotnet pack Functorium.slnx -c Release --no-build --output ./packages

    - name: Publish to NuGet.org
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        dotnet nuget push ./packages/*.nupkg \
          --api-key ${{ secrets.NUGET_API_KEY }} \
          --source https://api.nuget.org/v3/index.json \
          --skip-duplicate
      continue-on-error: true

    - name: Publish to GitHub Packages
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        dotnet nuget push ./packages/*.nupkg \
          --api-key ${{ secrets.GITHUB_TOKEN }} \
          --source https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json \
          --skip-duplicate
      continue-on-error: true

    - name: Create GitHub Release
      uses: softprops/action-gh-release@v2
      if: startsWith(github.ref, 'refs/tags/v')
      with:
        files: ./packages/*.nupkg
        generate_release_notes: true
        draft: false
        prerelease: ${{ contains(github.ref, '-') }}
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### 릴리스 프로세스

**1. 로컬에서 태그 생성:**
```bash
# 버전 결정 (SemVer)
git tag -a v1.0.0 -m "Release 1.0.0"
```

**2. 태그 푸시:**
```bash
git push origin v1.0.0
# Release 워크플로우 자동 실행
```

**3. GitHub에서 확인:**
- Actions 탭: 워크플로우 실행 상태
- Releases 탭: 생성된 릴리스
- Packages 탭: 배포된 패키지 (GitHub Packages)

<br/>

## 배포 설정

### NuGet.org 배포

**프로젝트 메타데이터 설정:**

`Directory.Build.props`에 추가:

```xml
<PropertyGroup>
  <!-- Package Metadata -->
  <Authors>Your Name</Authors>
  <Company>Your Company</Company>
  <Description>Functorium library description</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/yourusername/functorium</PackageProjectUrl>
  <RepositoryUrl>https://github.com/yourusername/functorium</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageTags>ddd;functional;csharp</PackageTags>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>

<ItemGroup>
  <None Include="$(MSBuildThisFileDirectory)README.md" Pack="true" PackagePath="/" />
</ItemGroup>
```

### GitHub Packages 배포

**패키지 소스 추가:**

로컬에서 패키지 설치 시:

```bash
# NuGet 소스 추가
dotnet nuget add source \
  --username USERNAME \
  --password GITHUB_PAT \
  --store-password-in-clear-text \
  --name github \
  "https://nuget.pkg.github.com/OWNER/index.json"

# 패키지 설치
dotnet add package Functorium --version 1.0.0 --source github
```

### 배포 대상 선택

**NuGet.org만:**
- `release.yml`에서 GitHub Packages 단계 제거

**GitHub Packages만:**
- `release.yml`에서 NuGet.org 단계 제거
- `NUGET_API_KEY` Secret 불필요

**둘 다:**
- 기본 설정 유지
- 두 Secret 모두 설정

<br/>

## 트러블슈팅

### 워크플로우가 실행되지 않을 때

**원인 1**: 워크플로우 파일 구문 오류

**해결:**
```bash
# YAML 파일 검증
# VS Code: YAML extension 설치
# Online: https://www.yamllint.com/
```

**원인 2**: 권한 부족

**해결:**
1. Settings → Actions → General
2. Workflow permissions: **Read and write permissions** 선택
3. **Allow GitHub Actions to create and approve pull requests** 체크

### MinVer 버전이 0.0.0으로 표시될 때

**원인**: Shallow clone으로 태그 정보 누락

**해결:**

`fetch-depth: 0` 설정 확인:
```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0  # 필수!
```

### NuGet 배포 실패

**원인 1**: API Key 없음

**해결:**
```bash
# GitHub Secrets 확인
# Settings → Secrets and variables → Actions
# NUGET_API_KEY가 있는지 확인
```

**원인 2**: 패키지 이름 중복

**해결:**

첫 배포 시 패키지 이름이 이미 존재:
- 프로젝트 이름 변경
- 또는 패키지 소유자에게 권한 요청

**원인 3**: API Key 권한 부족

**해결:**

NuGet.org에서 API Key 재생성:
- Scopes: **Push** 권한 확인
- Glob Pattern: 패키지 이름 패턴 확인

### GitHub Release 생성 실패

**원인**: GITHUB_TOKEN 권한 부족

**해결:**

Settings → Actions → General:
- **Allow GitHub Actions to create and approve pull requests**: 체크
- Workflow permissions: **Read and write permissions**

### 테스트 실패 시 배포 방지

**원인**: 테스트 실패해도 배포 진행

**해결:**

각 단계에 의존성 추가는 이미 설정됨:
```yaml
# 테스트 실패 시 자동으로 워크플로우 중단
- name: Run tests
  run: dotnet test --no-build
```

### .NET 10 SDK를 찾을 수 없을 때

**원인**: .NET 10이 아직 정식 출시되지 않음

**해결:**

`build.yml`, `release.yml`의 버전 수정:
```yaml
env:
  DOTNET_VERSION: '9.0.x'  # 또는 현재 설치 가능한 버전
```

<br/>

## FAQ

### Q1. 매번 태그를 푸시해야 하나요?

**A:** 릴리스를 배포할 때만 태그를 푸시합니다.

**일반 개발:**
```bash
git commit -m "feat: new feature"
git push origin main
# CI만 실행 (빌드, 테스트)
```

**릴리스:**
```bash
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
# Release 워크플로우 실행 (빌드, 테스트, 배포)
```

### Q2. Preview 버전도 배포할 수 있나요?

**A:** 네, prerelease 태그를 사용하세요:

```bash
# RC 버전
git tag -a v1.0.0-rc.1 -m "Release Candidate 1"
git push origin v1.0.0-rc.1
# GitHub Release에 prerelease로 표시됨
```

워크플로우가 자동 감지:
```yaml
prerelease: ${{ contains(github.ref, '-') }}
```

### Q3. 특정 프로젝트만 배포하려면?

**A:** `dotnet pack`에서 프로젝트 지정:

```yaml
- name: Pack NuGet packages
  run: |
    dotnet pack Src/Functorium/Functorium.csproj -c Release --no-build --output ./packages
    dotnet pack Src/Functorium.Testing/Functorium.Testing.csproj -c Release --no-build --output ./packages
```

### Q4. 로컬에서 Release 워크플로우를 테스트하려면?

**A:** [act](https://github.com/nektos/act) 도구 사용:

```bash
# act 설치
# macOS: brew install act
# Windows: choco install act-cli

# 워크플로우 테스트
act push -e .github/workflows/release.yml
```

또는 수동으로:
```bash
dotnet build -c Release
dotnet test -c Release --no-build
dotnet pack -c Release --no-build --output ./packages
```

### Q5. 배포된 패키지를 어떻게 사용하나요?

**A:** NuGet.org에서 설치:

```bash
dotnet add package Functorium --version 1.0.0
```

GitHub Packages에서 설치:
```bash
# 소스 추가 (한 번만)
dotnet nuget add source \
  --username YOUR_USERNAME \
  --password YOUR_PAT \
  --store-password-in-clear-text \
  --name github \
  "https://nuget.pkg.github.com/OWNER/index.json"

# 설치
dotnet add package Functorium --version 1.0.0 --source github
```

### Q6. 워크플로우 실행 시간을 줄이려면?

**A:** 캐시를 활용하세요 (build.yml에 이미 적용됨):

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
    restore-keys: |
      ${{ runner.os }}-nuget-

- name: Restore dependencies
  run: dotnet restore ${{ env.SOLUTION_FILE }}
```

### Q7. 여러 .NET 버전에서 테스트하려면?

**A:** Matrix 빌드 사용:

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: ['8.0.x', '9.0.x', '10.0.x']

    steps:
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet-version }}
```

### Q8. 배포 전에 수동 승인이 필요하다면?

**A:** Environment protection rules 사용:

1. Settings → Environments → **New environment**
2. Environment name: `production`
3. **Required reviewers** 추가

`release.yml` 수정:
```yaml
jobs:
  release:
    runs-on: ubuntu-latest
    environment: production  # 수동 승인 필요
```

### Q9. Codecov 없이 커버리지를 보려면?

**A:** build.yml에 이미 ReportGenerator와 Artifact 업로드가 설정되어 있습니다:

```yaml
- name: Generate coverage report
  uses: danielpalme/ReportGenerator-GitHub-Action@v5.4.4
  with:
    reports: '${{ github.workspace }}/**/TestResults/**/coverage.cobertura.xml'
    targetdir: '${{ env.COVERAGE_REPORT_DIR }}'
    reporttypes: 'Html;Cobertura;TextSummary;MarkdownSummaryGithub'

- name: Display coverage summary
  run: cat ${{ env.COVERAGE_REPORT_DIR }}/SummaryGithub.md >> $GITHUB_STEP_SUMMARY

- name: Upload coverage report
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
    path: '${{ env.COVERAGE_REPORT_DIR }}'
```

- Actions 탭 → Workflow 실행 → **Summary**에서 커버리지 요약 확인
- **Artifacts**에서 상세 HTML 리포트 다운로드

### Q10. 워크플로우를 로컬 브랜치에서만 테스트하려면?

**A:** 워크플로우 파일 이름 변경:

```bash
# 비활성화
mv .github/workflows/build.yml .github/workflows/build.yml.disabled

# 다시 활성화
mv .github/workflows/build.yml.disabled .github/workflows/build.yml
```

또는 수동 실행 사용 (build.yml에 이미 workflow_dispatch 설정됨):
```yaml
on:
  workflow_dispatch:  # 수동 실행 허용
```

GitHub Actions 탭에서 **Run workflow** 버튼으로 수동 실행 가능

## 참고 문서

- [GitHub Actions 문서](https://docs.github.com/actions)
- [.NET GitHub Actions](https://github.com/actions/setup-dotnet)
- [MinVer](https://github.com/adamralph/minver)
- [NuGet 배포 가이드](https://learn.microsoft.com/nuget/nuget-org/publish-a-package)
- [GitHub Packages](https://docs.github.com/packages)
