# 버전 관리

이 문서는 MinVer를 사용한 Git 태그 기반 자동 버전 관리와 다음 버전 제안 명령을 설명합니다.

## 목차

- [요약](#요약)
- [개요](#개요)
- [버전 구조](#버전-구조)
- [설치 및 설정](#설치-및-설정)
- [주요 설정 옵션](#주요-설정-옵션)
- [버전 계산 방식](#버전-계산-방식)
- [버전 진행 시나리오](#버전-진행-시나리오)
- [Height의 실질적 활용](#height의-실질적-활용)
- [어셈블리 버전 전략](#어셈블리-버전-전략)
- [다음 버전 제안 명령](#다음-버전-제안-명령)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

---

## 요약

### 주요 명령

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

### 주요 절차

**1. Pre-release에서 Stable까지:**
1. Alpha 태그: `git tag v1.0.0-alpha.0`
2. Beta 태그: `git tag v1.0.0-beta.0` (선택)
3. RC 태그: `git tag v1.0.0-rc.0` (선택)
4. 정식 릴리스: `git tag v1.0.0`

**2. 다음 Patch 릴리스:**
1. 정식 릴리스 후 개발 계속 (자동으로 `X.Y.Z+1-alpha.0.N` 표시)
2. 준비되면 `git tag vX.Y.Z+1`

### 주요 개념

| 개념 | 설명 |
|------|------|
| MinVer | Git 태그 기반 자동 버전 계산 MSBuild 도구 |
| Height | 최근 태그 이후 커밋 수 (자동 증가) |
| MinVerAutoIncrement | RTM 태그 후 자동 증가 단위 (patch/minor/major) |
| AssemblyVersion 전략 | `Major.Minor.0.0` (Patch 미포함 — 재컴파일 방지) |
| Conventional Commits | 커밋 타입(feat/fix/feat!)으로 버전 증가 결정 |

---

## 개요

### MinVer란?

MinVer는 Git 태그를 기반으로 .NET 프로젝트의 버전을 자동으로 계산하는 MSBuild 도구입니다.

### 주요 특징

- **태그 기반**: Git 태그만으로 버전 관리
- **제로 설정**: 기본 설정만으로 바로 사용 가능
- **빠른 속도**: 최소한의 Git 명령만 실행
- **SemVer 2.0**: 시맨틱 버전 규칙 준수

### 기존 방식과 비교

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

| 요소 | 설명 | 변경 방식 |
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

| 값 | 출력 내용 | 사용 시기 |
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

| 값 | 동작 | 예시 |
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

| 속성 | 값 예시 | 설명 |
|------|---------|------|
| `$(MinVerVersion)` | 1.0.0 | 전체 SemVer 버전 |
| `$(MinVerMajor)` | 1 | Major 버전 |
| `$(MinVerMinor)` | 0 | Minor 버전 |
| `$(MinVerPatch)` | 0 | Patch 버전 |
| `$(MinVerPreRelease)` | alpha.0.5 | Prerelease 부분 |
| `$(MinVerBuildMetadata)` | abc123 | 빌드 메타데이터 |

---

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

| 커밋 타입 | 버전 증가 | 예시 |
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

| 타입 | 설명 | 버전 예시 |
|------|------|----------|
| `alpha` | 알파 버전 (초기 개발 단계) | v1.3.0-alpha.0 |
| `beta` | 베타 버전 (기능 완료, 테스트 단계) | v1.3.0-beta.0 |
| `rc` | Release Candidate (출시 후보) | v1.3.0-rc.0 |

> **참고**: `/suggest-next-version` 명령은 제안만 합니다. 실제 태그 생성은 사용자가 명령어를 직접 실행해야 합니다.

---

## 트러블슈팅

### 버전이 0.0.0-alpha.0.N으로 표시될 때

| 원인 | 해결 |
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

### CI/CD에서 버전이 0.0.0으로 나올 때

Shallow clone으로 Git 히스토리 누락:

```yaml
# GitHub Actions
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0  # 전체 히스토리 가져오기
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

### Q1. MinVer와 GitVersion의 차이점은?

**A:**

| 항목 | MinVer | GitVersion |
|------|--------|-----------|
| 복잡도 | 단순 (태그만) | 복잡 (브랜치 전략) |
| 설정 | 최소 설정 | 상세 설정 파일 필요 |
| 브랜치 전략 | 지원 안 함 | GitFlow, GitHub Flow 등 |
| 속도 | 빠름 | 상대적으로 느림 |

### Q2. Pre-release 단계를 변경하려면?

**A:** `MinVerDefaultPreReleaseIdentifiers`를 변경합니다:

```xml
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
<MinVerDefaultPreReleaseIdentifiers>beta.0</MinVerDefaultPreReleaseIdentifiers>
<MinVerDefaultPreReleaseIdentifiers>rc.0</MinVerDefaultPreReleaseIdentifiers>
```

### Q3. 태그 없이 특정 버전으로 빌드하려면?

**A:** `MinVerVersion`으로 재정의합니다:

```bash
dotnet build -p:MinVerVersion=1.2.3
dotnet pack -p:MinVerVersion=1.2.3
```

### Q4. Hotfix 릴리스를 어떻게 관리하나요?

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

### Q5. NuGet 패키지 버전은 어떻게 설정되나요?

**A:** MinVer가 자동으로 `<Version>` 속성을 설정합니다:

```bash
dotnet pack
# Functorium.1.0.0.nupkg
```

### Q6. 로컬 빌드와 CI 빌드 버전을 구분하려면?

**A:** 빌드 메타데이터를 사용합니다:

```yaml
# CI (GitHub Actions)
- run: dotnet build -p:MinVerBuildMetadata=ci.${{ github.run_number }}
  # 결과: 1.0.0+ci.123
```

### Q7. Breaking Change는 어떻게 감지하나요?

**A:** 두 가지 방법으로 감지합니다:

1. 타입 뒤 느낌표: `feat!`, `fix!`
2. 푸터의 `BREAKING CHANGE:` 포함

```
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답이 배열에서 객체로 변경됨
```

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [02b-ci-cd.md](./02b-ci-cd.md) | CI/CD 워크플로우 |
| [MinVer GitHub](https://github.com/adamralph/minver) | MinVer 공식 저장소 |
| [Semantic Versioning 2.0.0](https://semver.org/) | SemVer 공식 문서 |
| [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) | Conventional Commits 공식 문서 |
