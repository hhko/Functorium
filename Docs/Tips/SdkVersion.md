# .NET SDK 버전 관리

이 문서는 `global.json` 파일을 사용하여 .NET 프로젝트의 SDK 버전을 지정하고 관리하는 방법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [global.json 기본 구조](#globaljson-기본-구조)
- [SDK 버전 지정](#sdk-버전-지정)
- [rollForward 정책](#rollforward-정책)
- [사용 예시](#사용-예시)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### global.json이란?

`global.json`은 .NET 프로젝트에서 사용할 SDK 버전을 명시적으로 지정하는 구성 파일입니다.

### 주요 목적

- **버전 일관성**: 팀 전체가 동일한 SDK 버전 사용
- **재현 가능한 빌드**: CI/CD 환경에서 예측 가능한 빌드
- **버전 업그레이드 제어**: SDK 업데이트를 선택적으로 적용

### 파일 위치

```
프로젝트-루트/
├── global.json          ← 여기에 위치
├── Functorium.slnx
├── Src/
└── Docs/
```

### 작동 방식

1. .NET CLI가 명령어를 실행할 때 `global.json` 파일을 검색
2. 현재 디렉토리부터 상위 디렉토리로 올라가며 파일 탐색
3. 발견된 `global.json`의 설정에 따라 SDK 버전 결정
4. 지정된 버전이 설치되지 않은 경우 `rollForward` 정책에 따라 처리

<br/>

## 요약

### 주요 명령

**파일 생성:**
```bash
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
dotnet new globaljson --sdk-version 10.0.100
```

**버전 확인:**
```bash
dotnet --list-sdks
dotnet --version
```

### 주요 절차

**1. 새 프로젝트 설정:**
```bash
# 1. 설치된 SDK 확인
dotnet --list-sdks

# 2. global.json 생성
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature

# 3. 적용된 버전 확인
dotnet --version
```

**2. 기존 프로젝트에 추가:**
```bash
# 1. 프로젝트 루트로 이동
cd <프로젝트-경로>

# 2. global.json 생성
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature

# 3. Git에 커밋
git add global.json
git commit -m "build: SDK 버전 10.0.100으로 고정"
```

### 주요 개념

**1. SDK 버전 고정**
- `global.json`으로 프로젝트에서 사용할 .NET SDK 버전 지정
- 팀 전체가 동일한 빌드 환경 유지
- 파일 위치: 프로젝트 루트 디렉토리

**2. rollForward 정책**
- 지정된 버전이 없을 때 대체 버전 선택 규칙
- 권장: `latestFeature` (동일 major 내 최신 기능)
- 엄격: `disable` (정확한 버전만 허용)

**3. 핵심 필드**

| 필드 | 필수 | 설명 | 예시 |
|------|------|------|------|
| `sdk.version` | ✓ | 사용할 SDK 버전 | `"10.0.100"` |
| `sdk.rollForward` | ✗ | 버전 선택 정책 | `"latestFeature"` |
| `sdk.allowPrerelease` | ✗ | 프리릴리스 허용 여부 | `false` |

<br/>

## global.json 기본 구조

### 최소 구성

```json
{
  "sdk": {
    "version": "10.0.100"
  }
}
```

### 전체 구성

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  },
  "msbuild-sdks": {
    "Custom.Sdk": "1.0.0"
  }
}
```

### 필드 설명

| 섹션 | 필드 | 설명 |
|------|------|------|
| `sdk` | `version` | 사용할 .NET SDK 버전 (필수) |
| `sdk` | `rollForward` | SDK 버전 선택 정책 |
| `sdk` | `allowPrerelease` | 프리릴리스 버전 허용 여부 |
| `msbuild-sdks` | - | MSBuild SDK 추가 참조 |

<br/>

## SDK 버전 지정

### 설치된 SDK 확인

```bash
# 설치된 모든 SDK 목록
dotnet --list-sdks

# 예시 출력:
# 9.0.100 [C:\Program Files\dotnet\sdk]
# 10.0.100 [C:\Program Files\dotnet\sdk]
```

### 버전 형식

.NET SDK는 `{major}.{minor}.{patch}` 형식을 따릅니다:

| 버전 | 설명 | 예시 |
|------|------|------|
| Major | 주요 버전 | `10.x.x` (C# 13, .NET 10) |
| Minor | 기능 업데이트 | `10.0.x` |
| Patch | 패치/수정 | `10.0.100` |

### 버전 지정 예시

```json
{
  "sdk": {
    "version": "10.0.100"
  }
}
```

**동작:**
- 정확히 `10.0.100` SDK를 사용
- 없으면 `rollForward` 정책에 따라 다른 버전 선택

### 현재 사용 중인 SDK 확인

```bash
# global.json이 적용된 SDK 버전
dotnet --version

# 출력 예시:
# 10.0.100
```

<br/>

## rollForward 정책

### 개요

`rollForward`는 지정된 버전이 없을 때 어떤 버전을 선택할지 결정하는 정책입니다.

### 정책 종류

| 정책 | 설명 | 예시 (`version: 10.0.100` 기준) |
|------|------|----------------------------------|
| `patch` | 동일한 major.minor 내에서 최신 patch | `10.0.100` → `10.0.102` (O), `10.1.x` (X) |
| `feature` | 동일한 major 내에서 최신 minor | `10.0.100` → `10.1.x` (O), `11.x.x` (X) |
| `minor` | 동일한 major 내에서 최신 minor | `feature`와 동일 |
| `major` | 최신 major 버전까지 허용 | `10.0.100` → `11.x.x` (O) |
| `latestPatch` | 최신 patch 버전 사용 | `10.0.x` 중 최신 |
| `latestFeature` | 최신 feature 버전 사용 | `10.x.x` 중 최신 |
| `latestMinor` | 최신 minor 버전 사용 | `latestFeature`와 동일 |
| `latestMajor` | 최신 major 버전 사용 | 설치된 SDK 중 최신 |
| `disable` | 정확한 버전만 허용 | 없으면 오류 |

### 권장 설정

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

**이유:**
- ✓ 동일 major 버전 내에서 최신 기능 사용
- ✓ 호환성 문제 최소화
- ✓ 보안 패치 자동 적용
- ✗ 예기치 않은 major 업그레이드 방지

### 정책별 비교

**개발 환경:**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"  // 최신 기능 사용
  }
}
```

**프로덕션/CI:**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "patch"  // 안정성 우선
  }
}
```

**엄격한 버전 제어:**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "disable"  // 정확한 버전만
  }
}
```

<br/>

## 사용 예시

### 시나리오 1: 새 프로젝트 시작

```bash
# 1. 현재 설치된 SDK 확인
dotnet --list-sdks

# 2. global.json 생성
dotnet new globaljson --sdk-version 10.0.100

# 3. rollForward 정책 추가 (수동 편집)
# global.json 파일을 열어서 rollForward 추가

# 4. 적용된 버전 확인
dotnet --version
```

**생성된 global.json:**
```json
{
  "sdk": {
    "version": "10.0.100"
  }
}
```

**rollForward 추가 후:**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### 시나리오 2: 기존 프로젝트에 추가

```bash
# 1. 프로젝트 루트로 이동
cd E:\프로젝트\Functorium

# 2. 현재 사용 중인 SDK 확인
dotnet --version

# 3. global.json 파일 생성
dotnet new globaljson --sdk-version 10.0.100

# 4. Git에 추가
git add global.json
git commit -m "build: SDK 버전 10.0.100으로 고정"
```

### 시나리오 3: SDK 버전 업그레이드

```bash
# 1. 새 SDK 설치 확인
dotnet --list-sdks
# 출력:
# 10.0.100 [C:\Program Files\dotnet\sdk]
# 10.0.200 [C:\Program Files\dotnet\sdk]

# 2. global.json 업데이트
# version을 10.0.200으로 변경

# 3. 변경 사항 검증
dotnet --version
# 출력: 10.0.200

# 4. 빌드 테스트
dotnet build

# 5. 커밋
git add global.json
git commit -m "build: SDK 버전을 10.0.200으로 업그레이드"
```

### 시나리오 4: 팀 환경 설정

**프로젝트 리더:**
```bash
# 1. global.json 생성 및 설정
dotnet new globaljson --sdk-version 10.0.100

# 2. rollForward 정책 추가

# 3. Git에 푸시
git add global.json
git commit -m "build: 팀 SDK 버전 설정"
git push
```

**팀원:**
```bash
# 1. 프로젝트 클론
git clone <repository-url>
cd <project>

# 2. 필요한 SDK 확인
cat global.json

# 3. SDK 설치 (필요한 경우)
# 10.0.100 다운로드 및 설치

# 4. 버전 확인
dotnet --version
# 출력: 10.0.100

# 5. 빌드
dotnet build
```

<br/>

## 트러블슈팅

### 지정된 SDK 버전을 찾을 수 없을 때

**오류 메시지:**
```
error: The specified SDK version '10.0.100' was not found.
```

**원인**: 지정된 SDK가 설치되지 않음

**해결 방법:**

**방법 1: SDK 설치**
```bash
# 1. 필요한 버전 확인
cat global.json

# 2. .NET SDK 다운로드 페이지 방문
# https://dotnet.microsoft.com/download/dotnet

# 3. 해당 버전 설치

# 4. 설치 확인
dotnet --list-sdks
```

**방법 2: rollForward 정책 조정**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"  // 추가
  }
}
```

### 예상과 다른 SDK 버전이 사용될 때

**증상**: `dotnet --version` 출력이 `global.json`과 다름

**원인**: 여러 `global.json` 파일이 존재하거나 `rollForward` 정책 때문

**해결 방법:**

```bash
# 1. 상위 디렉토리의 global.json 확인
cd ..
ls global.json

# 2. 현재 디렉토리의 global.json 우선순위 확인
cd <프로젝트-루트>

# 3. rollForward 정책 확인
cat global.json

# 4. 필요시 rollForward를 'disable'로 설정
```

**엄격한 버전 제어:**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "disable"
  }
}
```

### CI/CD 빌드 실패

**오류**: CI 환경에서 SDK 버전 불일치

**해결 방법:**

**GitHub Actions:**
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    global-json-file: global.json  # global.json 자동 인식
```

또는:

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '10.0.100'  # 명시적 버전 지정
```

**Azure Pipelines:**
```yaml
- task: UseDotNet@2
  inputs:
    version: '10.0.100'
    includePreviewVersions: false
```

### 프리릴리스 버전 문제

**증상**: 프리릴리스 SDK가 선택되거나 무시됨

**해결 방법:**

**프리릴리스 허용:**
```json
{
  "sdk": {
    "version": "10.0.100-preview.1",
    "rollForward": "disable",
    "allowPrerelease": true
  }
}
```

**프리릴리스 금지:**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false  // 기본값
  }
}
```

<br/>

## FAQ

### Q1. global.json은 필수인가요?

**A:** 아니요, 선택사항입니다.

| 상황 | global.json 없을 때 | global.json 있을 때 |
|------|-------------------|-------------------|
| SDK 선택 | 설치된 최신 버전 사용 | 지정된 버전 사용 |
| 팀 협업 | 버전 불일치 가능 | 버전 일관성 보장 |
| CI/CD | 예측 불가능 | 예측 가능 |

**권장**: 팀 프로젝트나 프로덕션 환경에서는 사용 권장

### Q2. global.json과 .csproj의 TargetFramework의 차이는?

**A:** 서로 다른 개념입니다.

| 항목 | global.json | .csproj |
|------|-------------|---------|
| 대상 | SDK 버전 (빌드 도구) | 런타임 버전 (실행 환경) |
| 예시 | `10.0.100` (SDK) | `net10.0` (런타임) |
| 역할 | 빌드 시 사용할 도구 버전 | 앱 실행 시 필요한 런타임 |

**예시:**
```json
// global.json
{
  "sdk": {
    "version": "10.0.100"  // SDK 10.0.100으로 빌드
  }
}
```

```xml
<!-- .csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>  <!-- .NET 10 런타임 대상 -->
  </PropertyGroup>
</Project>
```

### Q3. 여러 프로젝트가 있을 때 global.json 위치는?

**A:** 솔루션 루트에 하나만 두는 것을 권장합니다.

```
프로젝트-루트/
├── global.json          ← 여기에 하나만
├── Functorium.slnx
├── Src/
│   ├── ProjectA/
│   └── ProjectB/
└── Tests/
    └── ProjectA.Tests/
```

**이유:**
- ✓ 모든 프로젝트가 동일한 SDK 사용
- ✓ 관리 포인트 단일화
- ✓ 혼란 방지

### Q4. rollForward 정책 중 어떤 것을 선택해야 하나요?

**A:** 프로젝트 성격에 따라 선택하세요:

| 프로젝트 유형 | 권장 정책 | 이유 |
|------------|----------|------|
| 개발/테스트 | `latestFeature` | 최신 기능 활용 |
| 프로덕션 | `patch` | 안정성 우선 |
| 라이브러리 | `patch` | 호환성 유지 |
| 실험 프로젝트 | `latestMajor` | 최신 버전 체험 |

### Q5. SDK 버전을 업데이트하는 주기는?

**A:** 상황에 따라 다릅니다:

**패치 업데이트 (10.0.100 → 10.0.101):**
- 빠르게 적용 (보안 수정 포함)
- `rollForward: patch` 사용 시 자동

**기능 업데이트 (10.0.x → 10.1.x):**
- 릴리스 노트 확인 후 적용
- 테스트 후 업데이트

**메이저 업데이트 (10.x.x → 11.x.x):**
- 계획적으로 진행
- 호환성 검토 필수
- 마이그레이션 가이드 참고

### Q6. 프리릴리스 버전을 사용해도 되나요?

**A:** 프로덕션 환경에서는 권장하지 않습니다.

| 환경 | 프리릴리스 사용 | 이유 |
|------|--------------|------|
| 개발 | ✓ 가능 | 새 기능 테스트 |
| 테스트 | △ 신중히 | 안정성 검증 |
| 프로덕션 | ✗ 금지 | 안정성 필수 |

**개발 환경 예시:**
```json
{
  "sdk": {
    "version": "11.0.100-preview.1",
    "rollForward": "disable",
    "allowPrerelease": true
  }
}
```

### Q7. global.json이 Git에 커밋되어야 하나요?

**A:** 네, 반드시 커밋해야 합니다.

**이유:**
- ✓ 팀원들과 동일한 SDK 사용
- ✓ CI/CD 환경에서 일관성 보장
- ✓ 재현 가능한 빌드

**`.gitignore`에 추가하지 마세요:**
```gitignore
# ✗ 잘못된 예시
global.json

# ✓ global.json은 Git에 포함
```

### Q8. global.json 없이 특정 SDK 사용하는 방법은?

**A:** 환경 변수를 사용할 수 있습니다:

```bash
# Windows (PowerShell)
$env:DOTNET_ROOT = "C:\Program Files\dotnet"
$env:DOTNET_MULTILEVEL_LOOKUP = 0
dotnet --version

# Linux/macOS
export DOTNET_ROOT=/usr/share/dotnet
export DOTNET_MULTILEVEL_LOOKUP=0
dotnet --version
```

**하지만 권장하지 않습니다:**
- ✗ 환경마다 다르게 설정 필요
- ✗ 재현성 떨어짐
- ✓ global.json 사용 권장

### Q9. 하나의 머신에 여러 SDK가 설치되어 있을 때 어떻게 되나요?

**A:** `global.json`이 있으면 지정된 버전을 사용하고, 없으면 최신 버전을 사용합니다.

```bash
# 설치된 SDK
dotnet --list-sdks
# 9.0.100 [C:\Program Files\dotnet\sdk]
# 10.0.100 [C:\Program Files\dotnet\sdk]
# 10.0.200 [C:\Program Files\dotnet\sdk]

# global.json 없을 때
dotnet --version
# 10.0.200 (최신)

# global.json 있을 때 (version: 10.0.100)
dotnet --version
# 10.0.100 (지정된 버전)
```

### Q10. global.json 파일 생성 명령어는?

**A:** .NET CLI 명령어를 사용하세요:

```bash
# 최신 SDK 버전으로 생성
dotnet new globaljson

# 특정 버전 지정
dotnet new globaljson --sdk-version 10.0.100

# 파일 내용 확인
cat global.json
```

**생성된 파일:**
```json
{
  "sdk": {
    "version": "10.0.100"
  }
}
```

**이후 rollForward 등 추가 설정은 수동으로 편집하세요.**

<br/>

## 참고 문서

- [Microsoft 공식 문서 - global.json 개요](https://docs.microsoft.com/dotnet/core/tools/global-json)
- [.NET SDK 다운로드](https://dotnet.microsoft.com/download/dotnet)
- [Git 커밋 가이드](./Guide-Commit-Conventions.md)
