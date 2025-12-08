# MinVer 사용 가이드

이 문서는 MinVer 패키지를 사용한 자동 버전 관리 방법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [설치](#설치)
- [기본 설정](#기본-설정)
- [주요 설정 옵션](#주요-설정-옵션)
- [버전 계산 방식](#버전-계산-방식)
- [버전 진행 시나리오](#버전-진행-시나리오)
- [고급 사용법](#고급-사용법)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### MinVer란?

MinVer는 Git 태그를 기반으로 .NET 프로젝트의 버전을 자동으로 계산하는 MSBuild 도구입니다.

### 주요 특징

- **태그 기반**: Git 태그만으로 버전 관리
- **제로 설정**: 기본 설정만으로 바로 사용 가능
- **빠른 속도**: 최소한의 Git 명령만 실행
- **SemVer 2.0**: 시맨틱 버전 규칙 준수

### 사용 이유

| 기존 방식 | MinVer 방식 |
|----------|------------|
| 수동으로 버전 파일 수정 | Git 태그로 자동 계산 |
| 버전 불일치 위험 | 태그와 항상 일치 |
| 릴리스 시 추가 작업 | 태그만 푸시 |

<br/>

## 요약

### 버전 구조

```
{Major}.{Minor}.{Patch}-{Identifier}.{Phase}.{Height}+{Commit}
    ↑      ↑      ↑         ↑         ↑        ↑        ↑
    │      │      │         │         │        │        └─ 커밋 해시 (short)
    │      │      │         │         │        └────────── Height (자동 증가)
    │      │      │         │         └─────────────────── Phase (수동 변경)
    │      │      │         └───────────────────────────── Identifier (수동 변경)
    │      │      └─────────────────────────────────────── Patch (자동 표시*)
    │      └────────────────────────────────────────────── Minor
    └───────────────────────────────────────────────────── Major
```

\* RTM 태그 후 MinVerAutoIncrement 설정에 따라 자동 +1 표시 (실제 변경은 태그로만 가능)

### 태그 형식

```shell
vX.X.0-alpha.0   # 처음 pre-release
vX.X.0           # 처음 stable release
vX.X.1           # 다음 patch release
vX.Y.0           # 다음 minor release
vY.0.0           # 다음 major release
```

### 주요 명령

**설치:**
```bash
# Central Package Management 사용 시
# Directory.Packages.props에 추가

# 일반 프로젝트
dotnet add package MinVer
```

**버전 확인:**
```bash
# 기본 빌드 (버전 출력 안 함)
dotnet build

# 상세 버전 정보
dotnet build -p:MinVerVerbosity=normal

# 진단 정보
dotnet build -p:MinVerVerbosity=diagnostic
```

**태그 관리:**
```bash
# 버전 태그 생성
git tag -a v1.0.0 -m "Release 1.0.0"

# 태그 푸시
git push origin v1.0.0

# 태그 목록 확인
git tag -l -n1
```

### 주요 절차

**1. MinVer 설치 및 설정:**
```bash
# 1. Directory.Packages.props에 패키지 추가
# <PackageVersion Include="MinVer" Version="6.0.0" />

# 2. Directory.Build.props에 설정 추가
# <PackageReference Include="MinVer">
#   <PrivateAssets>all</PrivateAssets>
# </PackageReference>
# <PropertyGroup>
#   <MinVerTagPrefix>v</MinVerTagPrefix>
# </PropertyGroup>

# 3. 빌드 테스트
dotnet build -p:MinVerVerbosity=normal
```

**2. 첫 릴리스:**
```bash
# 1. 모든 변경사항 커밋
git add .
git commit -m "feat: initial release"

# 2. 버전 태그 생성
git tag -a v1.0.0 -m "Release 1.0.0"

# 3. 빌드 및 버전 확인
dotnet build -p:MinVerVerbosity=normal
# MinVer: Calculated version 1.0.0

# 4. 태그 푸시
git push origin v1.0.0
```

**3. 일반 개발:**
```bash
# 1. 기능 개발
git commit -m "feat: new feature"

# 2. 빌드 (alpha 버전)
dotnet build -p:MinVerVerbosity=normal
# MinVer: Calculated version 1.0.1-alpha.0.1
```

### 주요 개념

**1. 태그 기반 버전**
- Git 태그(`v1.0.0`)가 버전의 유일한 출처
- 태그 없으면 `0.0.0-alpha.0.N` 사용
- 수동 버전 관리 불필요

**2. 버전 계산 규칙**

| Git 상태 | 계산된 버전 | 설명 |
|---------|----------|------|
| 태그 없음 | 0.0.0-alpha.0.18 | 기본 버전 + 커밋 수 |
| v1.0.0 태그 | 1.0.0 | Stable 버전 |
| v1.0.0 + 5 커밋 | 1.0.1-alpha.0.5 | 다음 patch + 커밋 수 |

**3. MSBuild 통합**
- 빌드 시 자동 실행
- 프로젝트 속성에 버전 주입
- NuGet 패키지 버전 자동 설정

<br/>

## 설치

### Central Package Management 사용 시

**1. Directory.Packages.props에 버전 정의:**

```xml
<Project>
  <ItemGroup Label="Versioning">
    <PackageVersion Include="MinVer" Version="6.0.0" />
  </ItemGroup>
</Project>
```

**2. Directory.Build.props에 참조 추가:**

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

### 일반 프로젝트

**프로젝트 파일(.csproj)에 직접 추가:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MinVer" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### PrivateAssets 설정

`<PrivateAssets>all</PrivateAssets>`의 의미:
- MinVer는 빌드 도구일 뿐
- 런타임에 필요 없음
- NuGet 패키지 의존성에 포함되지 않음

<br/>

## 기본 설정

### 최소 설정

태그 접두사만 설정하면 바로 사용 가능:

```xml
<PropertyGroup>
  <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

### 권장 설정

프로덕션 사용 시 권장하는 전체 설정 (현재 프로젝트 설정):

```xml
<PropertyGroup>
  <!-- MinVer 기본 설정 -->
  <MinVerTagPrefix>v</MinVerTagPrefix>
  <MinVerVerbosity>minimal</MinVerVerbosity>
  <MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
  <MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
  <MinVerAutoIncrement>patch</MinVerAutoIncrement>

  <!-- Git 저장소 경로 -->
  <MinVerWorkingDirectory>$(MSBuildThisFileDirectory)</MinVerWorkingDirectory>
</PropertyGroup>

<!-- AssemblyVersion은 MSBuild Target에서 설정 (MinVer 계산 후 실행) -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

**설정 설명:**
- **MinVerMinimumMajorMinor**: 태그 없을 때 최소 버전 (0.0.0 방지)
- **MinVerAutoIncrement**: RTM 태그 후 자동 증가 단위 (patch/minor/major)
- **SetAssemblyVersion Target**: MinVer 계산 후 AssemblyVersion 설정 (바이너리 호환성 관리)

### 설정 위치

| 파일 | 용도 | 적용 범위 |
|------|------|----------|
| `Directory.Build.props` | 공통 설정 | 모든 프로젝트 |
| `프로젝트.csproj` | 프로젝트별 재정의 | 해당 프로젝트만 |

<br/>

## 주요 설정 옵션

### MinVerTagPrefix

**태그 접두사 설정:**

```xml
<MinVerTagPrefix>v</MinVerTagPrefix>
```

| 설정 값 | 인식 태그 | 무시 태그 |
|--------|---------|----------|
| `v` | v1.0.0, v2.0.0 | 1.0.0, ver1.0.0 |
| `ver` | ver1.0.0 | v1.0.0, 1.0.0 |
| (빈 문자열) | 1.0.0 | v1.0.0 |

**권장**: `v` 사용 (GitHub 표준)

### MinVerVerbosity

**출력 수준 설정:**

```xml
<MinVerVerbosity>minimal</MinVerVerbosity>
```

| 값 | 출력 내용 | 사용 시기 |
|----|---------|----------|
| `minimal` | 경고/오류만 | 일반 빌드 |
| `normal` | 버전 계산 과정 | 버전 확인 |
| `diagnostic` | 상세 디버그 정보 | 문제 해결 |

**CLI로 재정의:**
```bash
dotnet build -p:MinVerVerbosity=normal
```

### MinVerDefaultPreReleaseIdentifiers

**태그 없을 때 사용할 prerelease suffix:**

```xml
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
```

#### Pre-release 단계 구조

형식: `{identifier}.{phase}`

**1. Identifier (식별자)**

Pre-release 단계의 이름입니다.

| 단계 | 의미 | 사용 시점 |
|------|------|-----------|
| `alpha` | 알파 버전 | 초기 개발, 기능 불완전, 불안정 (기본값) |
| `beta` | 베타 버전 | 기능 완성, 테스트 중, 버그 수정 중 |
| `rc` | Release Candidate | 릴리스 후보, 최종 테스트 |

**버전 비교 순서:**
```
alpha < beta < rc < (stable)
```

Semantic Versioning 규칙에 따라 알파벳 순서로 비교됩니다.

**2. Phase (단계 번호)**

동일한 identifier 내에서의 단계 번호입니다 (0부터 시작).

**중요: Phase는 자동 증가하지 않습니다.**
- Git 태그를 통해서만 변경 가능 (수동)
- 커밋해도 Phase는 그대로 유지
- Height만 자동으로 증가

예시:
```bash
# alpha.0 태그 생성
v0.1.0-alpha.0 → 0.1.0-alpha.0

# 커밋 추가 (Phase는 그대로, Height만 증가)
커밋 5개       → 0.1.0-alpha.0.5

# alpha.1 태그 생성 (Phase 수동 변경)
v0.1.0-alpha.1 → 0.1.0-alpha.1

# 커밋 추가 (Phase는 1 유지)
커밋 3개       → 0.1.0-alpha.1.3
```

#### 자동 vs 수동 증가

| 요소 | 증가 방식 | 트리거 | 비고 |
|------|----------|--------|------|
| **Height** | 자동 증가 | 커밋할 때마다 | 항상 자동 |
| **Phase** | 수동 변경 | Git 태그 생성 시에만 | 커밋으로 변경 불가 |
| **Identifier** | 수동 변경 | Git 태그 생성 시에만 | 커밋으로 변경 불가 |
| **Patch** | 자동 표시 | RTM 태그 후 자동 +1 표시 | MinVerAutoIncrement=patch |
| **Minor** | 자동 표시 | RTM 태그 후 자동 +1 표시 | MinVerAutoIncrement=minor |
| **Major** | 자동 표시 | RTM 태그 후 자동 +1 표시 | MinVerAutoIncrement=major |

**참고:** Patch/Minor/Major는 실제로 증가하는 것이 아니라 "표시만" 증가된 것처럼 보입니다. 실제 버전은 Git 태그를 만들어야만 변경됩니다.

**예시 (MinVerAutoIncrement=patch, 기본값):**
```bash
v0.1.0 태그    → 0.1.0                    # RTM 버전
다음 커밋      → 0.1.1-alpha.0.1          # Patch 자동 +1 표시
                 ↑   ↑
                 │   └─ 실제로는 0.1.0의 다음 개발 버전
                 └───── 표시상으로만 0.1.1
```

#### 버전 문자열 구조

**Git 태그가 없을 때:**
```
버전: 0.0.0-alpha.0.{height}+{commit}
      ↑     ↑     ↑  ↑
      │     │     │  └─ Height (자동 증가)
      │     │     └──── Phase (수동 변경)
      │     └────────── Identifier (수동 변경)
      └──────────────── Major.Minor.Patch (수동 증가)
```

#### 사용 예시

**결과:**
- 태그 없음: `0.0.0-alpha.0.18`
- 태그 있음: `1.0.0`
- 태그 후 커밋: `1.0.1-alpha.0.5`

**Pre-release 단계별 설정:**
```xml
<!-- Alpha 버전 (기본값) -->
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
<!-- 결과: 0.0.0-alpha.0.18 -->

<!-- Beta 버전 -->
<MinVerDefaultPreReleaseIdentifiers>beta.0</MinVerDefaultPreReleaseIdentifiers>
<!-- 결과: 0.0.0-beta.0.18 -->

<!-- Release Candidate -->
<MinVerDefaultPreReleaseIdentifiers>rc.0</MinVerDefaultPreReleaseIdentifiers>
<!-- 결과: 0.0.0-rc.0.18 -->
```

### MinVerWorkingDirectory

**Git 저장소 경로 지정:**

```xml
<MinVerWorkingDirectory>$(MSBuildThisFileDirectory)</MinVerWorkingDirectory>
```

**사용 이유:**
- 기본값: 프로젝트 폴더
- 설정 값: 솔루션 루트 (권장)
- 모노레포에서 중요

### MinVerMinimumMajorMinor

**최소 버전 설정:**

```xml
<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
```

**효과:**
- 태그 없어도 `1.0.0-alpha.0.N` 사용
- `0.0.0` 버전 방지

### MinVerBuildMetadata

**빌드 메타데이터 추가:**

```xml
<MinVerBuildMetadata>build.$(BuildNumber)</MinVerBuildMetadata>
```

**결과:**
```
1.0.0+build.12345
```

**CLI로 추가:**
```bash
dotnet build -p:MinVerBuildMetadata=ci.$(git rev-parse --short HEAD)
# 1.0.0+ci.abc1234
```

### MinVerAutoIncrement

**RTM 태그 후 자동 증가 단위 설정:**

```xml
<MinVerAutoIncrement>patch</MinVerAutoIncrement>
```

| 값 | 동작 | 예시 |
|----|------|------|
| `patch` | Patch 버전 +1 표시 (기본값) | v1.0.0 → 1.0.1-alpha.0.1 |
| `minor` | Minor 버전 +1 표시 | v1.0.0 → 1.1.0-alpha.0.1 |
| `major` | Major 버전 +1 표시 | v1.0.0 → 2.0.0-alpha.0.1 |

**중요:** 이것은 "표시만" 증가시킵니다. 실제 버전은 Git 태그로만 변경됩니다.

**동작 방식:**

```bash
# patch (기본값)
git tag v1.0.0
# → 1.0.0 (stable)

git commit
# → 1.0.1-alpha.0.1  (Patch +1 표시)

git tag v1.0.1
# → 1.0.1 (실제 버전 변경)

# minor
git tag v1.0.0
git commit
# → 1.1.0-alpha.0.1  (Minor +1 표시)

# major
git tag v1.0.0
git commit
# → 2.0.0-alpha.0.1  (Major +1 표시)
```

**사용 시나리오:**
- **patch**: 버그 수정 중심 (대부분의 프로젝트)
- **minor**: 기능 추가 중심
- **major**: 주요 변경 중심

<br/>

## 버전 계산 방식

### 계산 알고리즘

1. **Git 태그 검색**: `{MinVerTagPrefix}*` 패턴으로 검색
2. **유효한 태그 필터링**: SemVer 2.0 형식만 인식
3. **최근 태그 선택**: 현재 커밋의 조상 중 가장 가까운 태그
4. **커밋 높이 계산**: 태그 이후 커밋 수
5. **버전 생성**: 태그 + Height에 따라 버전 결정

### 태그 없을 때

```bash
# Git 히스토리
* abc123 (HEAD) commit 18
* def456 commit 17
* ...
* 123abc commit 1

# 계산 결과
0.0.0-alpha.0.18
```

**구성:**
- `0.0.0`: 기본 버전
- `alpha.0`: MinVerDefaultPreReleaseIdentifiers
- `18`: 전체 커밋 수

### 태그가 있을 때 (Height = 0)

```bash
# Git 히스토리
* abc123 (HEAD, tag: v1.0.0) Release 1.0.0

# 계산 결과
1.0.0
```

**구성:**
- `1.0.0`: 태그 버전 그대로
- prerelease suffix 없음 (stable 버전)

### 태그 후 커밋 (Height > 0)

```bash
# Git 히스토리
* xyz789 (HEAD) commit 5
* mno345 commit 4
* ...
* abc123 (tag: v1.0.0) Release 1.0.0

# 계산 결과
1.0.1-alpha.0.5
```

**구성:**
- `1.0.1`: 태그의 patch 버전 +1
- `alpha.0`: MinVerDefaultPreReleaseIdentifiers
- `5`: 태그 이후 커밋 수

### Prerelease 태그

```bash
# Git 히스토리
* abc123 (HEAD, tag: v1.0.0-rc.1) RC 1

# 계산 결과
1.0.0-rc.1
```

**추가 커밋 시:**
```bash
* xyz789 (HEAD) commit 1
* abc123 (tag: v1.0.0-rc.1) RC 1

# 계산 결과
1.0.0-rc.1.1
```

### 여러 태그가 있을 때

```bash
# Git 히스토리
* xyz789 (HEAD) commit 3
* mno345 (tag: v1.1.0) Release 1.1.0  ← 최근 태그 사용
* def456 commit 2
* abc123 (tag: v1.0.0) Release 1.0.0

# 계산 결과
1.1.1-alpha.0.3
```

**규칙:**
- 현재 커밋의 조상 중 가장 가까운 태그
- 미래 버전 태그는 무시

<br/>

## 버전 진행 시나리오

이 섹션에서는 실제 프로젝트에서 pre-release부터 stable 릴리스까지의 전체 버전 진행 과정을 보여줍니다.

### 25.13.0 릴리스 (첫 번째 버전)

```shell
# Alpha 단계
git tag v25.13.0-alpha.0
# → 25.13.0-alpha.0

# Alpha 개발 (3개 커밋)
# → 25.13.0-alpha.0.1
# → 25.13.0-alpha.0.2
# → 25.13.0-alpha.0.3

# Beta 단계
git tag v25.13.0-beta.0
# → 25.13.0-beta.0

# Beta 개발 (2개 커밋)
# → 25.13.0-beta.0.1
# → 25.13.0-beta.0.2

# Release Candidate
git tag v25.13.0-rc.0
# → 25.13.0-rc.0

# RC 개발 (1개 커밋)
# → 25.13.0-rc.0.1

# 정식 릴리스
git tag v25.13.0
# → 25.13.0 (stable)
```

### 25.13.1 릴리스 (다음 Patch 버전)

v25.13.0 릴리스 후 추가 버그 수정을 위한 patch 버전 진행 과정입니다.

```shell
# v25.13.0 릴리스 후 개발 계속
# (MinVerAutoIncrement=patch → Patch가 +1로 표시됨)
# → 25.13.1-alpha.0.1
# → 25.13.1-alpha.0.2

# Alpha 단계 (선택)
git tag v25.13.1-alpha.0
# → 25.13.1-alpha.0

# Alpha 개발
# → 25.13.1-alpha.0.1
# → 25.13.1-alpha.0.2

# Beta 단계 (선택)
git tag v25.13.1-beta.0
# → 25.13.1-beta.0

# Beta 개발
# → 25.13.1-beta.0.1

# Release Candidate (선택)
git tag v25.13.1-rc.0
# → 25.13.1-rc.0

# RC 개발
# → 25.13.1-rc.0.1

# 다음 Patch 릴리스
git tag v25.13.1
# → 25.13.1 (stable)
```

### 주요 포인트

1. **Height 자동 증가**: 커밋할 때마다 자동으로 증가
2. **Phase 수동 변경**: Git 태그로만 변경 가능
3. **Identifier 변경**: alpha → beta → rc 진행
4. **MinVerAutoIncrement**: RTM 태그 후 Patch +1 자동 표시
5. **Pre-release 선택적**: 필요에 따라 alpha, beta, rc 단계 생략 가능

<br/>

## 고급 사용법

### 프로젝트별 다른 버전

**멀티 프로젝트에서 개별 버전 관리:**

```xml
<!-- Lib 프로젝트 -->
<PropertyGroup>
  <MinVerTagPrefix>lib-v</MinVerTagPrefix>
</PropertyGroup>

<!-- App 프로젝트 -->
<PropertyGroup>
  <MinVerTagPrefix>app-v</MinVerTagPrefix>
</PropertyGroup>
```

**태그 예시:**
```bash
git tag lib-v1.0.0
git tag app-v2.0.0
```

### 수동 버전 재정의

**특정 빌드에서 버전 강제 지정:**

```bash
# 빌드 시 버전 재정의
dotnet build -p:MinVerVersion=1.2.3

# 패키지 생성 시
dotnet pack -p:MinVerVersion=1.2.3
```

**주의**: MinVer의 자동 계산을 무시하므로 필요한 경우만 사용

### 버전 출력 없이 빌드

**조용한 빌드 (경고/오류만):**

```bash
dotnet build -p:MinVerVerbosity=quiet
```

또는 프로젝트 파일에:
```xml
<PropertyGroup>
  <MinVerVerbosity>quiet</MinVerVerbosity>
</PropertyGroup>
```

### CI/CD에서 빌드 메타데이터

**GitHub Actions:**
```yaml
- name: Build
  run: dotnet build -p:MinVerBuildMetadata=${{ github.sha }}
```

**결과:**
```
1.0.0+abc123def456789
```

### 어셈블리 버전 전략

**.NET 어셈블리는 3가지 버전 속성을 가집니다:**

| 속성 | 목적 | 형식 | 값 예시 |
|------|------|------|---------|
| **AssemblyVersion** | 바이너리 호환성 | Major.Minor.0.0 | 1.0.0.0 |
| **FileVersion** | 파일 속성 표시 | Major.Minor.Patch.0 | 1.0.1.0 |
| **InformationalVersion** | 제품 버전 (사용자용) | 전체 SemVer | 1.0.1-alpha.0.5+abc123 |

#### AssemblyVersion: 왜 Patch를 포함하지 않나요?

**AssemblyVersion은 바이너리 호환성을 결정합니다.**

```xml
<!-- ❌ 나쁜 예: Patch 포함 -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).$(MinVerPatch).0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

**문제점:**
- v1.0.1 → v1.0.2 버그 수정만 했는데도 AssemblyVersion 변경
- 참조하는 모든 어셈블리 **재컴파일 필수**
- 작은 패치마다 바이너리 호환성 깨짐
- 불필요한 재배포 강제

```xml
<!-- ✅ 좋은 예: Major.Minor만 사용 (현재 프로젝트 설정) -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

**장점:**
- v1.0.1, v1.0.2, v1.0.3 모두 `1.0.0.0` 유지
- 패치 업데이트 시 **재컴파일 불필요**
- 바이너리 호환성 유지
- 배포 간소화

**실제 예시:**

```bash
# v1.0.0 릴리스
AssemblyVersion:        1.0.0.0
FileVersion:            1.0.0.0
InformationalVersion:   1.0.0

# v1.0.1 버그 수정 (참조 어셈블리 재컴파일 불필요)
AssemblyVersion:        1.0.0.0  ← 변경 없음!
FileVersion:            1.0.1.0  ← 정확한 버전
InformationalVersion:   1.0.1    ← 사용자에게 표시

# v1.0.2 버그 수정
AssemblyVersion:        1.0.0.0  ← 여전히 변경 없음
FileVersion:            1.0.2.0
InformationalVersion:   1.0.2

# v1.1.0 기능 추가 (Minor 변경 - 재컴파일 필요)
AssemblyVersion:        1.1.0.0  ← 이제 변경됨
FileVersion:            1.1.0.0
InformationalVersion:   1.1.0
```

#### 설정 방법

**MSBuild Target 사용 (권장 - 현재 프로젝트 방식):**

```xml
<!-- MinVer 계산 후 AssemblyVersion 설정 -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

MinVer는 자동으로 설정:
- **FileVersion**: `$(MinVerMajor).$(MinVerMinor).$(MinVerPatch).0`
- **InformationalVersion**: `$(MinVerVersion)` (전체 SemVer)

#### MSBuild 속성

| 속성 | 값 예시 | 설명 |
|------|---------|------|
| `$(MinVerVersion)` | 1.0.0 | 전체 SemVer 버전 |
| `$(MinVerMajor)` | 1 | Major 버전 |
| `$(MinVerMinor)` | 0 | Minor 버전 |
| `$(MinVerPatch)` | 0 | Patch 버전 |
| `$(MinVerPreRelease)` | alpha.0.5 | Prerelease 부분 |
| `$(MinVerBuildMetadata)` | abc123 | 빌드 메타데이터 |

### 조건부 설정

**Debug/Release 모드별 다른 설정:**

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <MinVerVerbosity>normal</MinVerVerbosity>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <MinVerVerbosity>minimal</MinVerVerbosity>
</PropertyGroup>
```

<br/>

## 트러블슈팅

### 버전이 0.0.0-alpha.0.N으로 표시될 때

**원인**: Git 태그가 없거나 MinVer가 태그를 찾지 못함

**해결 1**: 태그 생성
```bash
git tag -a v0.1.0 -m "Initial version"
dotnet build -p:MinVerVerbosity=normal
# MinVer: Calculated version 0.1.0
```

**해결 2**: 태그 접두사 확인
```bash
# 현재 태그 확인
git tag -l

# 접두사 없는 태그가 있다면
# Directory.Build.props 수정
<MinVerTagPrefix></MinVerTagPrefix>  <!-- 빈 문자열 -->
```

**해결 3**: 최소 버전 설정
```xml
<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
<!-- 결과: 1.0.0-alpha.0.N -->
```

### 태그를 생성했는데도 버전이 안 바뀔 때

**원인 1**: 잘못된 태그 형식

**해결**:
```bash
# 잘못된 태그
git tag 1.0.0        # ✗ 접두사 없음
git tag ver1.0.0     # ✗ 잘못된 접두사
git tag v1.0         # ✗ Patch 버전 누락

# 올바른 태그
git tag v1.0.0       # ✓
```

**원인 2**: 태그가 다른 브랜치에 있음

**해결**:
```bash
# 현재 브랜치의 태그 확인
git tag --merged

# 태그가 없다면 현재 브랜치에 태그 생성
git tag -a v1.0.0 -m "Release 1.0.0"
```

### CI/CD에서 버전이 0.0.0으로 나올 때

**원인**: Shallow clone으로 Git 히스토리 누락

**해결 (GitHub Actions)**:
```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0  # 전체 히스토리 가져오기
```

**해결 (GitLab CI)**:
```yaml
variables:
  GIT_DEPTH: 0
```

**해결 (Azure Pipelines)**:
```yaml
- checkout: self
  fetchDepth: 0
```

### MinVer가 실행되지 않을 때

**원인**: 패키지 참조 누락

**해결 1**: 패키지 참조 확인
```bash
# 복원 및 빌드
dotnet restore
dotnet build --no-incremental
```

**해결 2**: Directory.Build.props 확인
```xml
<PackageReference Include="MinVer">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**해결 3**: 캐시 정리
```bash
dotnet clean
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

### 한글 경로 문제

**원인**: Git 저장소가 한글 경로에 있으면 MinVer가 경로 인식 실패

**증상**:
```
C:\사용자\프로젝트\  ← MinVer 오류
```

**해결**: 프로젝트를 영문 경로로 이동
```bash
# 이동
C:\Dev\Projects\Functorium\  ← 정상 작동
```

### 예상과 다른 버전이 나올 때

**원인**: 예상하지 못한 태그가 더 최근에 있음

**해결**:
```bash
# 1. 현재 커밋과 조상 태그 확인
git describe --tags
# v1.0.0-5-gabc123

# 2. 모든 태그 확인
git log --oneline --decorate

# 3. 잘못된 태그 삭제
git tag -d v1.0.0-wrong
git push origin :refs/tags/v1.0.0-wrong
```

<br/>

## FAQ

### Q1. MinVer와 GitVersion의 차이점은 무엇인가요?

**A:** 주요 차이점:

| 항목 | MinVer | GitVersion |
|------|--------|-----------|
| 복잡도 | 단순 (태그만) | 복잡 (브랜치 전략) |
| 설정 | 최소 설정 | 상세 설정 파일 필요 |
| 브랜치 전략 | 지원 안 함 | GitFlow, GitHub Flow 등 |
| 속도 | 빠름 | 상대적으로 느림 |

**선택 기준:**
- **MinVer**: 단순한 태그 기반 워크플로우
- **GitVersion**: GitFlow 등 복잡한 브랜치 전략

### Q2. 태그 없이도 안정적인 버전을 사용할 수 있나요?

**A:** 네, `MinVerMinimumMajorMinor`를 설정하세요:

```xml
<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
```

**결과:**
```
태그 없음: 1.0.0-alpha.0.18
v1.0.0: 1.0.0
v1.0.0 + 커밋: 1.0.1-alpha.0.5
```

### Q3. Pre-release 단계를 변경하려면?

**A:** `MinVerDefaultPreReleaseIdentifiers`를 변경하세요:

```xml
<!-- Alpha (기본값) -->
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
<!-- 결과: 1.0.1-alpha.0.5 -->

<!-- Beta -->
<MinVerDefaultPreReleaseIdentifiers>beta.0</MinVerDefaultPreReleaseIdentifiers>
<!-- 결과: 1.0.1-beta.0.5 -->

<!-- Release Candidate -->
<MinVerDefaultPreReleaseIdentifiers>rc.0</MinVerDefaultPreReleaseIdentifiers>
<!-- 결과: 1.0.1-rc.0.5 -->
```

### Q4. 특정 프로젝트에서만 MinVer를 사용하려면?

**A:** 해당 프로젝트 파일(.csproj)에만 패키지 추가:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="MinVer" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
  </PropertyGroup>
</Project>
```

다른 프로젝트에서는 수동 버전 관리:
```xml
<PropertyGroup>
  <Version>2.0.0</Version>
</PropertyGroup>
```

### Q5. NuGet 패키지 버전은 어떻게 설정되나요?

**A:** MinVer가 자동으로 `<Version>` 속성을 설정합니다:

```bash
dotnet pack
# Functorium.1.0.0.nupkg
```

수동 재정의 가능:
```bash
dotnet pack -p:PackageVersion=1.2.3
# Functorium.1.2.3.nupkg
```

### Q6. 로컬 빌드와 CI 빌드의 버전을 구분하려면?

**A:** 빌드 메타데이터를 사용하세요:

**로컬:**
```bash
dotnet build
# 1.0.0
```

**CI (GitHub Actions):**
```yaml
- run: dotnet build -p:MinVerBuildMetadata=ci.${{ github.run_number }}
  # 1.0.0+ci.123
```

### Q7. Tag 없이 특정 버전으로 빌드하려면?

**A:** `MinVerVersion`으로 재정의:

```bash
dotnet build -p:MinVerVersion=1.2.3
dotnet pack -p:MinVerVersion=1.2.3
```

**주의**: MinVer의 자동 계산을 무시하므로 신중히 사용

### Q8. 여러 프로젝트에서 각각 다른 버전을 사용하려면?

**A:** 태그 접두사를 프로젝트별로 다르게:

```xml
<!-- Functorium.csproj -->
<MinVerTagPrefix>functorium-v</MinVerTagPrefix>

<!-- Functorium.Testing.csproj -->
<MinVerTagPrefix>testing-v</MinVerTagPrefix>
```

**태그:**
```bash
git tag functorium-v1.0.0
git tag testing-v2.0.0
```

### Q9. MinVer가 계산한 버전을 빌드 스크립트에서 사용하려면?

**A:** MSBuild 속성을 활용:

```bash
# PowerShell
$version = dotnet build -p:MinVerVerbosity=normal |
           Select-String "Calculated version (.+)\." |
           ForEach-Object { $_.Matches.Groups[1].Value }
Write-Host "Version: $version"
```

또는 `dotnet-minver` CLI 도구 사용:
```bash
dotnet tool install --global minver-cli
minver
# 1.0.0
```

### Q10. Stable 버전과 Pre-release 버전을 어떻게 구분하나요?

**A:** Prerelease identifier 유무로 구분:

```bash
# Stable
1.0.0          # Prerelease 없음
2.5.3          # Prerelease 없음

# Pre-release
1.0.1-alpha.0.5       # Alpha 버전
1.0.0-beta.0.3        # Beta 버전
2.0.0-rc.1            # Release Candidate 버전
```

**코드에서 확인:**
```csharp
var version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion;

if (version.Contains("-"))
    Console.WriteLine("Pre-release version");
else
    Console.WriteLine("Stable version");
```

## 참고 문서

- [MinVer GitHub](https://github.com/adamralph/minver)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [Git 기반 버전 관리](./Guide-Versioning-Workflow.md)
- [GitHub Actions CI/CD](./Guide-CICD-Workflow.md)
