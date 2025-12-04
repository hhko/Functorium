# Git 기반 버전 관리 가이드

이 문서는 MinVer를 사용한 Git 태그 기반 자동 버전 관리 시스템을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [버전 관리 시스템](#버전-관리-시스템)
- [시맨틱 버전 규칙](#시맨틱-버전-규칙)
- [릴리스 워크플로우](#릴리스-워크플로우)
- [MinVer 설정](#minver-설정)
- [검증 방법](#검증-방법)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

Git 태그를 기반으로 빌드 버전을 자동 생성하여 수동 버전 관리의 오류를 방지하고 일관성을 유지합니다.

### 주요 도구

| 도구 | 역할 | 버전 |
|------|------|------|
| MinVer | Git 태그 기반 버전 자동 생성 | 6.0.0 |
| Git Tags | 버전 정보 저장 | - |

### 설정 파일

```
프로젝트루트/
├── Directory.Build.props          # MinVer 설정
├── Directory.Packages.props       # MinVer 버전 정의
└── .git/
    └── refs/tags/                # Git 태그
```

<br/>

## 요약

### 주요 명령

**버전 확인:**
```bash
dotnet build -p:MinVerVerbosity=normal
```

**릴리스 태그 생성:**
```bash
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
```

**개발 빌드:**
```bash
dotnet build
# 버전: 1.0.1-preview.0.5 (다음 버전-preview.0.커밋높이)
```

### 주요 절차

**1. 개발 중:**
```bash
# 일반 개발 작업
git commit -m "feat: new feature"
dotnet build
# 버전: 1.0.1-preview.0.5
```

**2. 릴리스:**
```bash
# 릴리스 태그 생성
git tag -a v1.0.1 -m "Release 1.0.1"
git push origin v1.0.1

# 빌드
dotnet build -c Release
# 버전: 1.0.1
```

### 주요 개념

**1. Git 태그 기반 버전**
- Git 태그(v1.2.3)가 버전의 유일한 출처
- 태그 이후 커밋은 자동으로 preview 버전 생성
- 수동 버전 관리 불필요

**2. 자동 버전 생성 규칙**

| 상태 | Git 상태 | 생성 버전 | 설명 |
|------|---------|----------|------|
| 릴리스 | v1.0.0 태그 | 1.0.0 | Stable 버전 |
| 개발 중 | v1.0.0 + 5 커밋 | 1.0.1-preview.0.5 | Preview 버전 |
| 태그 없음 | 초기 상태 | 0.0.0-preview.0.N | 기본 버전 |

**3. 시맨틱 버전 (SemVer 2.0)**
- **Major**: 호환되지 않는 API 변경
- **Minor**: 호환되는 기능 추가
- **Patch**: 호환되는 버그 수정

<br/>

## 버전 관리 시스템

### 버전 자동 생성 원리

MinVer는 Git 히스토리를 분석하여 현재 커밋의 버전을 자동 계산합니다.

```
v1.0.0 ─┬─> commit1 ─> commit2 ─> commit3 (HEAD)
        │    1.0.1     1.0.1      1.0.1
        │    -preview  -preview   -preview
        │    .0.1      .0.2       .0.3
```

### 버전 계산 프로세스

```
MinVer 실행
    ↓
Git 태그 검색 (v* 패턴)
    ↓
최근 태그 찾기
    ↓
커밋 높이 계산 (Height)
    ↓
버전 생성
    - 태그 있음: {태그버전}-preview.0.{Height}
    - 태그 없음: 0.0.0-preview.0.{Height}
```

### 버전 형식

**Stable 버전 (릴리스):**
```
v1.2.3 태그 → 1.2.3
```

**Preview 버전 (개발):**
```
v1.2.3 + 5 커밋 → 1.2.4-preview.0.5
```

**초기 버전 (태그 없음):**
```
18 커밋 → 0.0.0-preview.0.18
```

### 커밋 높이 (Height)

커밋 높이는 최근 태그 이후의 커밋 수입니다.

```bash
# 예시
git log --oneline
# abc123 (HEAD) feat: add feature C
# def456 feat: add feature B
# ghi789 feat: add feature A
# jkl012 (tag: v1.0.0) Release 1.0.0

# Height = 3
# 생성 버전: 1.0.1-preview.0.3
```

<br/>

## 시맨틱 버전 규칙

### SemVer 2.0 형식

```
Major.Minor.Patch[-Prerelease][+BuildMetadata]
  1  .  2  .  3  [-preview.0.5] [+abc123]
```

### 버전 증가 규칙

| 변경 유형 | 버전 증가 | 예시 | 설명 |
|----------|----------|------|------|
| Breaking Change | Major | 1.0.0 → 2.0.0 | API 호환성 깨짐 |
| New Feature | Minor | 1.0.0 → 1.1.0 | 호환되는 기능 추가 |
| Bug Fix | Patch | 1.0.0 → 1.0.1 | 호환되는 버그 수정 |

### Conventional Commits와 버전

| Commit Type | 버전 영향 | 예시 |
|-------------|----------|------|
| `feat:` | Minor | feat: add user authentication |
| `fix:` | Patch | fix: resolve login timeout |
| `feat!:` | Major | feat!: change API response format |
| `docs:` | 영향 없음 | docs: update README |
| `chore:` | 영향 없음 | chore: update dependencies |

<br/>

## 릴리스 워크플로우

### 1. 개발 단계

일반적인 개발 작업:

```bash
# 기능 개발
git checkout -b feature/new-feature
git commit -m "feat: implement new feature"
git push origin feature/new-feature

# 빌드 확인
dotnet build
# 버전: 1.0.1-preview.0.5
```

### 2. 릴리스 준비

릴리스 버전을 결정하고 태그를 생성합니다.

**Patch 릴리스 (버그 수정):**
```bash
# 현재: v1.0.0
git tag -a v1.0.1 -m "Release 1.0.1"
```

**Minor 릴리스 (기능 추가):**
```bash
# 현재: v1.0.0
git tag -a v1.1.0 -m "Release 1.1.0"
```

**Major 릴리스 (Breaking Change):**
```bash
# 현재: v1.0.0
git tag -a v2.0.0 -m "Release 2.0.0"
```

### 3. 릴리스 배포

```bash
# 1. 태그 원격 푸시
git push origin v1.0.1

# 2. Release 빌드
dotnet build -c Release

# 3. 버전 확인
dotnet build -c Release -p:MinVerVerbosity=normal
# MinVer: Calculated version 1.0.1

# 4. 배포
dotnet pack -c Release
dotnet nuget push ./bin/Release/*.nupkg
```

### 4. 릴리스 이후 개발

```bash
# 릴리스 이후 첫 커밋
git commit -m "chore: prepare next development iteration"
dotnet build
# 버전: 1.0.2-preview.0.1
```

### 릴리스 체크리스트

- [ ] 모든 테스트 통과
- [ ] CHANGELOG 업데이트
- [ ] 버전 번호 결정 (SemVer 규칙)
- [ ] Git 태그 생성 및 푸시
- [ ] Release 빌드 성공 확인
- [ ] 패키지 배포
- [ ] GitHub Release 생성 (선택)

<br/>

## MinVer 설정

### Directory.Build.props

프로젝트 루트의 `Directory.Build.props`에 MinVer를 설정합니다.

```xml
<Project>
  <!-- Versioning with MinVer -->
  <ItemGroup>
    <PackageReference Include="MinVer">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <PropertyGroup>
    <!-- MinVer Configuration -->
    <MinVerTagPrefix>v</MinVerTagPrefix>
    <MinVerVerbosity>minimal</MinVerVerbosity>
    <MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
    <MinVerWorkingDirectory>$(MSBuildThisFileDirectory)</MinVerWorkingDirectory>

    <!-- Assembly Version Strategy -->
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
    <FileVersion>$(MinVerMajor).$(MinVerMinor).$(MinVerPatch).0</FileVersion>
  </PropertyGroup>
</Project>
```

### 설정 항목 설명

| 설정 | 값 | 설명 |
|------|----|----- |
| `MinVerTagPrefix` | `v` | 태그 접두사 (v1.0.0) |
| `MinVerVerbosity` | `minimal` | 출력 수준 (minimal/normal/diagnostic) |
| `MinVerDefaultPreReleaseIdentifiers` | `preview.0` | 태그 없을 때 사용할 prerelease suffix |
| `MinVerWorkingDirectory` | `$(MSBuildThisFileDirectory)` | Git 저장소 루트 경로 |
| `AssemblyVersion` | `Major.Minor.0.0` | 어셈블리 버전 (바이너리 호환성) |
| `FileVersion` | `Major.Minor.Patch.0` | 파일 버전 (빌드 정보) |

### Directory.Packages.props

Central Package Management를 사용하는 경우:

```xml
<Project>
  <ItemGroup Label="Versioning">
    <PackageVersion Include="MinVer" Version="6.0.0" />
  </ItemGroup>
</Project>
```

### MinVer 버전 출력 수준

| Verbosity | 출력 내용 | 사용 시기 |
|-----------|----------|----------|
| `minimal` | 경고/오류만 | 일반 빌드 |
| `normal` | 버전 계산 과정 | 버전 확인 |
| `diagnostic` | 상세 디버그 정보 | 문제 해결 |

<br/>

## 검증 방법

### 1. 버전 확인

**기본 빌드:**
```bash
dotnet build
# 출력에서 버전 정보는 보이지 않음
```

**상세 버전 정보:**
```bash
dotnet build -p:MinVerVerbosity=normal
```

**출력 예시:**
```
MinVer: Using { Commit: 5a7ebd6, Tag: 'v0.1.0', Version: 0.1.0, Height: 0 }.
MinVer: Calculated version 0.1.0.
```

### 2. 태그별 버전 테스트

**시나리오 1: 태그 있음 (Stable)**
```bash
git tag -a v1.0.0 -m "Release 1.0.0"
dotnet build -p:MinVerVerbosity=normal
# MinVer: Calculated version 1.0.0.
```

**시나리오 2: 태그 후 커밋 (Preview)**
```bash
git commit -m "feat: new feature"
dotnet build -p:MinVerVerbosity=normal
# MinVer: Calculated version 1.0.1-preview.0.1.
```

**시나리오 3: 태그 없음 (Default)**
```bash
# 태그가 하나도 없는 상태
dotnet build -p:MinVerVerbosity=normal
# MinVer: No commit found with a valid SemVer 2.0 version prefixed with 'v'.
# MinVer: Calculated version 0.0.0-preview.0.18.
```

### 3. 어셈블리 버전 확인

빌드된 DLL의 버전 정보를 확인:

```bash
# Windows
(Get-Item ./bin/Release/net10.0/Functorium.dll).VersionInfo

# Linux/Mac
dotnet build -c Release -v:n | grep "AssemblyVersion\|FileVersion\|InformationalVersion"
```

### 4. 패키지 버전 확인

NuGet 패키지 생성 시:

```bash
dotnet pack -c Release
# Functorium.1.0.0.nupkg
```

<br/>

## 트러블슈팅

### 버전이 0.0.0-preview.0.N으로 표시될 때

**원인**: Git 태그가 없거나 MinVer가 태그를 찾지 못함

**해결**:

```bash
# 1. 태그 확인
git tag -l

# 2. 태그 없으면 생성
git tag -a v0.1.0 -m "Initial version"

# 3. 빌드 확인
dotnet build -p:MinVerVerbosity=normal
```

### 태그를 생성했는데도 버전이 안 바뀔 때

**원인 1**: 태그 접두사 불일치

**해결**:

```bash
# 잘못된 태그
git tag 1.0.0  # ✗

# 올바른 태그
git tag v1.0.0  # ✓ (MinVerTagPrefix=v)
```

**원인 2**: 잘못된 Git 디렉토리

**해결**:

`Directory.Build.props`의 `MinVerWorkingDirectory` 확인:

```xml
<!-- 올바른 설정 -->
<MinVerWorkingDirectory>$(MSBuildThisFileDirectory)</MinVerWorkingDirectory>
```

### 한글 경로 문제

**원인**: Git 저장소가 한글 경로에 있으면 MinVer가 경로를 인식하지 못함

**증상**:
```
C:\Users\사용자\2025년\프로젝트\  ← MinVer 오류
```

**해결**:

프로젝트를 영문 경로로 이동:
```bash
# 이동 전
C:\Users\사용자\2025년\프로젝트\

# 이동 후
C:\Dev\Projects\Functorium\
```

### MinVer가 빌드 시 실행되지 않을 때

**원인**: MinVer 패키지 참조 누락

**해결**:

1. `Directory.Packages.props` 확인:
```xml
<PackageVersion Include="MinVer" Version="6.0.0" />
```

2. `Directory.Build.props` 확인:
```xml
<PackageReference Include="MinVer">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

3. 복원 및 빌드:
```bash
dotnet restore
dotnet build --no-incremental
```

### 예상과 다른 버전이 생성될 때

**원인**: 다른 태그가 더 최근 커밋에 있음

**해결**:

```bash
# 1. 전체 태그 목록 확인
git tag -l --sort=-creatordate

# 2. 현재 커밋의 태그 확인
git describe --tags

# 3. 태그와 커밋 히스토리 확인
git log --oneline --decorate

# 4. 불필요한 태그 삭제
git tag -d v1.0.0-wrong
git push origin :refs/tags/v1.0.0-wrong
```

### CI/CD에서 버전이 다르게 나올 때

**원인**: Shallow clone으로 태그 정보 누락

**해결**:

GitHub Actions 예시:
```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0  # 전체 히스토리 가져오기
```

GitLab CI 예시:
```yaml
variables:
  GIT_DEPTH: 0  # 전체 히스토리
```

<br/>

## FAQ

### Q1. 수동으로 버전을 설정할 수 있나요?

**A:** MinVer 사용 시 수동 버전 설정은 권장하지 않지만, 필요한 경우 재정의 가능합니다:

```bash
# 빌드 시 재정의
dotnet build -p:MinVerVersion=1.2.3

# 프로젝트 파일에서 재정의
<PropertyGroup>
  <Version>1.2.3</Version>
</PropertyGroup>
```

**권장**: Git 태그를 사용하여 버전을 관리하세요.

### Q2. 브랜치별로 다른 버전 규칙을 적용할 수 있나요?

**A:** MinVer는 브랜치를 구분하지 않고 Git 태그만 사용합니다. 브랜치별 버전이 필요하면 GitVersion을 고려하세요.

**현재 시스템:**
- main 브랜치: v1.0.0 태그 → 1.0.0
- feature 브랜치: v1.0.0 + 커밋 → 1.0.1-preview.0.N

### Q3. Preview 버전의 suffix를 변경할 수 있나요?

**A:** 네, `MinVerDefaultPreReleaseIdentifiers`를 변경하세요:

```xml
<!-- preview 대신 alpha 사용 -->
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
```

**결과:**
```
v1.0.0 + 5 커밋 → 1.0.1-alpha.0.5
```

### Q4. AssemblyVersion과 FileVersion의 차이는?

**A:** .NET 어셈블리는 여러 버전 속성을 가집니다:

| 속성 | 용도 | 형식 | 예시 |
|------|------|------|------|
| `AssemblyVersion` | 바이너리 호환성 | Major.Minor.0.0 | 1.2.0.0 |
| `FileVersion` | 파일 정보 | Major.Minor.Patch.0 | 1.2.3.0 |
| `InformationalVersion` | 표시 버전 | SemVer 전체 | 1.2.3-preview.0.5 |

**권장 설정:**
```xml
<AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
<FileVersion>$(MinVerMajor).$(MinVerMinor).$(MinVerPatch).0</FileVersion>
```

### Q5. 태그를 잘못 만들었을 때 수정하려면?

**A:** 로컬과 원격 태그를 모두 삭제하고 재생성하세요:

```bash
# 1. 로컬 태그 삭제
git tag -d v1.0.0

# 2. 원격 태그 삭제
git push origin :refs/tags/v1.0.0

# 3. 새 태그 생성
git tag -a v1.0.0 -m "Release 1.0.0"

# 4. 원격 푸시
git push origin v1.0.0
```

**주의**: 이미 배포된 버전의 태그는 삭제하지 마세요.

### Q6. 여러 프로젝트가 있을 때 각각 다른 버전을 사용할 수 있나요?

**A:** MinVer는 Git 저장소 단위로 작동하므로 모든 프로젝트가 같은 버전을 공유합니다.

**대안:**
1. 각 프로젝트를 별도 Git 저장소로 분리
2. 프로젝트별 태그 접두사 사용 (예: `lib-v1.0.0`, `app-v2.0.0`)
3. 각 프로젝트에 MinVerTagPrefix 재정의:

```xml
<!-- Lib 프로젝트 -->
<MinVerTagPrefix>lib-v</MinVerTagPrefix>

<!-- App 프로젝트 -->
<MinVerTagPrefix>app-v</MinVerTagPrefix>
```

### Q7. RC(Release Candidate) 버전을 만들려면?

**A:** 태그에 prerelease identifier를 포함하세요:

```bash
# RC 버전 태그
git tag -a v1.0.0-rc.1 -m "Release Candidate 1"
dotnet build
# 버전: 1.0.0-rc.1

# RC 이후 커밋
git commit -m "fix: minor fix"
dotnet build
# 버전: 1.0.0-rc.1.1
```

### Q8. 로컬 빌드와 CI 빌드의 버전을 다르게 하려면?

**A:** 환경에 따라 MinVer 설정을 재정의할 수 있습니다:

```yaml
# CI에서만 빌드 메타데이터 추가
- name: Build
  run: dotnet build -p:MinVerBuildMetadata=${{ github.sha }}
  # 결과: 1.0.0+abc1234
```

### Q9. Hotfix 릴리스를 어떻게 관리하나요?

**A:** 이전 릴리스 태그에서 브랜치를 생성하고 새 태그를 만듭니다:

```bash
# 1. 이전 릴리스에서 브랜치 생성
git checkout v1.0.0
git checkout -b hotfix/1.0.1

# 2. 수정 작업
git commit -m "fix: critical bug"

# 3. Hotfix 태그 생성
git tag -a v1.0.1 -m "Hotfix 1.0.1"
git push origin v1.0.1

# 4. main에 머지
git checkout main
git merge hotfix/1.0.1
```

### Q10. MinVer 대신 다른 도구를 사용할 수 있나요?

**A:** 네, 여러 대안이 있습니다:

| 도구 | 특징 | 사용 시기 |
|------|------|----------|
| MinVer | 단순, 태그 기반 | 간단한 버전 관리 |
| GitVersion | 브랜치 전략 지원 | 복잡한 브랜치 워크플로우 |
| Nerdbank.GitVersioning | 세밀한 제어 | 높은 커스터마이징 필요 |

**선택 기준:**
- **MinVer**: GitFlow 없이 태그만 사용
- **GitVersion**: GitFlow, GitHub Flow 등 브랜치 전략 사용
- **Nerdbank.GitVersioning**: 버전 파일 기반 관리 선호

## 참고 문서

- [MinVer GitHub](https://github.com/adamralph/minver)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [.NET Versioning](https://learn.microsoft.com/dotnet/standard/library-guidance/versioning)
