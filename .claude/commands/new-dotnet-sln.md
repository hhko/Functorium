---
title: NEW-DOTNET-SLN
description: .NET 솔루션과 팀 협업을 위한 필수 구조 파일들(global.json, .editorconfig, Directory.Build.props, Directory.Packages.props)을 자동으로 생성합니다.
---

# /new-dotnet-sln

.NET 솔루션과 프로젝트 구조 파일들을 생성합니다.

## 개요

이 커맨드는 새로운 .NET 프로젝트를 시작할 때 필요한 모든 기본 구조 파일들을 자동으로 생성합니다. 솔루션 파일뿐만 아니라 팀 협업과 일관된 개발 환경을 위한 다양한 설정 파일들을 포함합니다.

### 생성되는 파일들

| 파일 | 설명 |
|------|------|
| `*.sln` | Visual Studio 솔루션 파일 |
| `global.json` | .NET SDK 버전 고정 및 호환성 정책 |
| `.editorconfig` | 코드 스타일 및 포맷팅 규칙 |
| `Directory.Build.props` | 공통 MSBuild 속성 설정 |
| `Directory.Packages.props` | 중앙 집중식 NuGet 패키지 버전 관리 |

### 사용 방법

**대화형 모드 (인자 없이 실행):**
```bash
/new-dotnet-sln
```
커맨드를 실행하면 대화형으로 필요한 정보를 입력받습니다:
1. 솔루션 이름
2. .NET SDK 버전
3. SDK 호환성 정책 (rollForward)

**빠른 실행 모드 (인자와 함께 실행):**
```bash
/new-dotnet-sln [솔루션이름] [SDK버전] [rollForward정책]
```

예시:
```bash
# .NET 8.0.100, latestPatch 정책으로 MyApp 솔루션 생성
/new-dotnet-sln MyApp 8.0.100 latestPatch

# .NET 9.0.100, latestMinor 정책으로 MyProject 솔루션 생성
/new-dotnet-sln MyProject 9.0.100 latestMinor
```

인자 설명:
- `[솔루션이름]`: 생성할 솔루션의 이름 (필수)
- `[SDK버전]`: .NET SDK 버전 (예: 8.0.100, 9.0.100) (선택, 생략 시 질문)
- `[rollForward정책]`: latestPatch, latestMinor, latestMajor, disable 중 하나 (선택, 생략 시 질문)

### 사전 요구사항

- .NET SDK가 설치되어 있어야 합니다
- `dotnet` CLI가 명령줄에서 실행 가능해야 합니다

## 실행 단계:

### 0. 작업 계획 수립 (TodoWrite)
TodoWrite 도구를 사용하여 전체 작업 단계를 todo 리스트로 작성합니다:
1. 솔루션 파일 생성
2. global.json 생성 및 SDK 버전 확인
3. .editorconfig 생성
4. Directory.Build.props 생성
5. Directory.Packages.props 생성
6. 생성된 파일 확인

각 단계를 진행하면서 TodoWrite를 사용하여 상태를 업데이트합니다:
- 작업 시작: `status: "in_progress"`
- 작업 완료: `status: "completed"`

### 1. 인자 확인 및 파싱
먼저 사용자가 명령어와 함께 인자를 전달했는지 확인합니다:
- 인자 1개: 솔루션 이름만 제공됨 → SDK 버전과 정책은 질문
- 인자 2개: 솔루션 이름, SDK 버전 제공됨 → 정책만 질문
- 인자 3개: 모든 정보 제공됨 → 바로 실행
- 인자 없음: 모든 정보를 대화형으로 질문

### 2. 사용자 정보 수집
필요한 정보가 인자로 제공되지 않은 경우에만 AskUserQuestion 도구를 사용하여 수집합니다.
모든 질문은 `multiSelect: false`로 설정하여 단일 선택만 가능하게 합니다.

**질문 1: 솔루션 이름** (인자로 제공되지 않은 경우)
- 질문: "생성할 .NET 솔루션의 이름을 입력해주세요."
- 입력: 텍스트 입력

**질문 2: .NET SDK 버전** (인자로 제공되지 않은 경우)
- 질문: "사용할 .NET SDK 버전을 선택해주세요."
- 입력: AskUserQuestion 도구를 사용하여 선택지 제공
- 선택지:
  - .NET 9.0 (9.0.100): 최신 표준 지원(STS) 버전
  - .NET 8.0 (8.0.100): 장기 지원(LTS) 버전 (권장)
  - .NET 6.0 (6.0.100): 이전 LTS 버전
  - 기타: 사용자가 직접 버전 입력 가능

**질문 3: SDK 호환성 정책 (rollForward)** (인자로 제공되지 않은 경우)
- 질문: "SDK 버전 호환성 정책(rollForward)을 선택해주세요."
- 입력: AskUserQuestion 도구를 사용하여 선택지 제공
- 선택지:
  - `latestPatch`: 동일 minor 버전의 최신 패치만 허용 (권장, 가장 안전)
  - `latestMinor`: 동일 major 버전의 최신 minor 허용 (유연함)
  - `latestMajor`: 모든 최신 버전 허용 (가장 유연함)
  - `disable`: 정확히 지정된 버전만 사용 (가장 엄격함)

**중요:**
- 인자로 제공된 값이 있으면 해당 질문은 건너뜁니다
- 인자의 개수에 따라 필요한 질문만 선택적으로 진행합니다
- 모든 인자가 제공된 경우 바로 파일 생성 단계로 진행합니다

### 3. 파일 생성
TodoWrite로 각 파일 생성 작업의 진행 상태를 업데이트하면서 다음 명령어들을 순차적으로 실행합니다:

```bash
# 1. 솔루션 파일 생성
dotnet new sln -n [솔루션이름]

# 2. global.json 생성 (SDK 버전 고정)
dotnet new global.json --sdk-version [SDK버전] --roll-forward [정책]

# 2-1. SDK 버전 확인 및 자동 조정
# global.json 생성 후 SDK 버전을 검증합니다
dotnet --version

# SDK 버전 오류가 발생하는 경우 (Exit code 145):
# 1. 오류 메시지에서 설치된 SDK 버전 목록을 확인
#    예: "Installed SDKs: 5.0.301, 6.0.100, 7.0.100, 8.0.303, 9.0.100"
# 2. Read 도구로 global.json 파일 읽기
# 3. 요청된 버전과 동일한 major.minor 버전 중 가장 가까운 버전 선택
#    예: 8.0.100 요청 → 8.0.303 선택
# 4. Edit 도구로 global.json의 version 필드 수정
#
# 주의: 동일한 major.minor 버전이 없으면 오류를 사용자에게 알리고 중단

# 3. .editorconfig 생성 (코드 스타일 설정)
# Read 도구를 사용하여 .claude/commands/templates/editorconfig.template 파일을 읽음
# Write 도구를 사용하여 읽은 내용을 .editorconfig 파일로 작성

# 4. Directory.Build.props 생성 (공통 MSBuild 속성)
dotnet new buildprops

# 5. Directory.Packages.props 생성 (중앙 패키지 관리)
# 주의: packagesprops 템플릿이 존재하지 않으므로 수동으로 생성
# Write 도구를 사용하여 다음 내용으로 생성:
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Add your centrally managed package versions here -->
    <!-- Example:
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    -->
  </ItemGroup>
</Project>
```

### 4. 생성 확인
다음 명령어들로 생성된 파일들을 확인합니다:

```bash
# 일반 파일 목록 확인
ls

# .editorconfig 등 숨김 파일 확인 (.으로 시작하는 파일)
ls -la .editorconfig
```

생성된 파일들을 사용자에게 요약하여 보고합니다:
- `[솔루션이름].sln` - 솔루션 파일
- `global.json` - SDK 버전 및 호환성 정책 (버전 자동 조정된 경우 명시)
- `.editorconfig` - 코드 스타일 규칙
- `Directory.Build.props` - 공통 MSBuild 속성
- `Directory.Packages.props` - NuGet 패키지 버전 관리

## 각 파일의 역할:

### global.json
- 프로젝트에서 사용할 .NET SDK 버전을 명시적으로 지정
- 팀 전체가 동일한 SDK 버전을 사용하도록 보장
- CI/CD 환경에서 일관된 빌드 환경 제공

### .editorconfig
- 코드 스타일 및 포맷팅 규칙 정의
- IDE 간 일관된 코딩 스타일 유지
- 린트 규칙 및 코드 품질 설정
- 사전 정의된 템플릿 파일(.claude/commands/templates/editorconfig.template)을 사용하여 생성
- 템플릿 파일을 수정하여 팀의 코딩 스타일에 맞게 커스터마이징 가능

### Directory.Build.props
- 솔루션 전체에 적용되는 공통 MSBuild 속성
- 언어 버전, nullable 설정, 경고 레벨 등 공통 설정

### Directory.Packages.props
- 중앙 집중식 패키지 버전 관리 (Central Package Management)
- 모든 프로젝트에서 동일한 패키지 버전 사용 보장

## 주의사항:

1. **SDK 버전 선택**:
   - LTS 버전 사용을 권장 (현재: .NET 8.0)
   - 프로덕션 환경에서는 안정성이 검증된 버전 사용
   - 시스템에 설치된 SDK 버전과 일치하지 않으면 자동으로 조정됨

2. **rollForward 정책**:
   - `latestPatch` 권장: 보안 패치는 적용하되 호환성 유지
   - CI/CD 환경과 개발 환경의 일관성 고려

3. **.editorconfig 템플릿 커스터마이징**:
   - `.claude/commands/templates/editorconfig.template` 파일을 직접 수정하여 팀의 코딩 스타일에 맞게 조정 가능
   - 템플릿 파일 변경 후 모든 팀원이 동일한 템플릿을 사용하도록 Git에 커밋

4. **Directory.Packages.props 생성**:
   - `dotnet new packagesprops` 템플릿이 존재하지 않음
   - 대신 Write 도구를 사용하여 수동으로 생성해야 함
   - 생성 후 필요한 패키지 버전을 수동으로 추가

5. **팀 협업**:
   - 생성된 모든 파일을 Git에 커밋하여 팀원들과 공유
   - global.json 변경 시 팀원들에게 SDK 업데이트 안내

6. **크로스 플랫폼 고려사항**:
   - 파일 확인 시 `ls` 명령어 사용 (Windows Git Bash, Linux, macOS 모두 호환)
   - Windows CMD의 `dir` 명령어는 사용하지 않음
   - 모든 경로는 슬래시(/)를 사용하여 크로스 플랫폼 호환성 보장

## 실행 예시:

### 예시 1: 솔루션 이름만 제공
```bash
/new-dotnet-sln Hello2
```

실행 결과:
1. SDK 버전 선택 대화창 표시 (.NET 9.0, 8.0, 6.0 중 선택)
2. rollForward 정책 선택 대화창 표시 (latestPatch, latestMinor 등)
3. 선택한 옵션으로 모든 파일 생성
4. SDK 버전이 시스템에 없으면 자동으로 가장 가까운 버전으로 조정
   - 예: 8.0.100 선택 → 8.0.303으로 자동 조정 (시스템에 8.0.303 설치됨)

### 예시 2: 모든 인자 제공
```bash
/new-dotnet-sln MyApp 9.0.100 latestPatch
```

실행 결과:
1. 질문 없이 바로 파일 생성 시작
2. Hello2.sln, global.json, .editorconfig, Directory.Build.props, Directory.Packages.props 생성
3. 생성된 파일 목록과 요약 출력

## 문제 해결 (Troubleshooting):

### SDK 버전을 찾을 수 없음
**증상**: `dotnet --version` 실행 시 "A compatible .NET SDK was not found" 오류 발생

**원인**: 요청한 SDK 버전이 시스템에 설치되지 않음

**해결**:
- 자동 조정 기능이 동일한 major.minor 버전을 찾아 자동으로 수정
- 예: 8.0.100 요청 시 8.0.303이 설치되어 있으면 자동으로 8.0.303으로 변경
- 동일한 major.minor 버전이 없으면 사용자에게 알리고 중단

### .editorconfig 템플릿을 찾을 수 없음
**증상**: `.claude/commands/templates/editorconfig.template` 파일을 찾을 수 없음

**원인**: 템플릿 파일이 존재하지 않음

**해결**:
- `.claude/commands/templates/` 디렉토리가 없으면 생성
- 기본 .editorconfig 템플릿 파일 생성
- 사용자에게 템플릿 파일 생성 필요성 알림

### 이미 파일이 존재함
**증상**: 솔루션 파일이나 설정 파일이 이미 존재

**원인**: 이전에 실행했거나 수동으로 파일을 생성함

**해결**:
- 사용자에게 기존 파일 덮어쓰기 여부 확인
- 백업 후 진행하거나 다른 디렉토리에서 실행 권장
