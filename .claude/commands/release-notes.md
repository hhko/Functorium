---
title: RELEASE-NOTES
description: 릴리스 노트를 자동으로 생성합니다 (데이터 수집, 분석, 작성, 검증).
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"
---

# 릴리스 노트 자동 생성 규칙

Functorium 프로젝트의 전문적이고 정확한 릴리스 노트를 자동으로 생성합니다.

## 버전 파라미터 (`$ARGUMENTS`)

**버전이 지정된 경우:** $ARGUMENTS

버전 파라미터는 필수입니다. 생성할 릴리스 노트의 버전을 지정하십시오.

**사용 예시:**
```
/release-notes v1.2.0        # 정규 릴리스
/release-notes v1.0.0        # 첫 배포
/release-notes v1.2.0-beta.1 # 프리릴리스
```

**버전이 지정되지 않은 경우:**

오류 메시지를 출력하고 중단합니다:
```
릴리스 노트 생성 실패

오류: 버전 파라미터가 필요합니다.

사용법: /release-notes <version>
예시: /release-notes v1.2.0
```

## 적용 범위

이 명령은 다음 시나리오에서 사용됩니다:

- **정규 릴리스**: origin/release/1.0 → HEAD 간 변경사항 문서화
- **첫 배포**: 초기 커밋 → HEAD 간 전체 히스토리 문서화
- **프리릴리스**: 알파/베타 릴리스 문서화 (예: v1.0.0-alpha.1)
- **핫픽스**: 긴급 패치 릴리스 문서화

## 자동화 워크플로우

이 명령은 5단계로 구성된 완전 자동화 프로세스를 실행합니다:

### Phase 1: 환경 검증 및 준비

릴리스 노트 생성 전 필수 환경을 검증합니다.

#### 전제조건 확인

다음 조건을 모두 확인하십시오:

1. **Git 저장소 확인**
   ```bash
   git status
   ```
   - 현재 디렉터리가 Git 저장소인지 확인
   - Git이 설치되어 있는지 확인

2. **스크립트 디렉터리 확인**
   - `.release-notes/scripts` 디렉터리 존재 확인
   - `Config/component-priority.json` 파일 존재 확인

3. **.NET SDK 확인**
   ```bash
   dotnet --version
   ```
   - .NET 10.x 이상 필요
   - 설치되지 않았으면 오류 메시지 출력

4. **버전 파라미터 검증**
   - `$ARGUMENTS`가 비어있지 않은지 확인
   - 버전 형식이 유효한지 확인 (예: v1.2.0, v1.0.0-alpha.1)

#### Base Branch 결정

릴리스 간 비교를 위한 base branch를 결정합니다:

**기본 전략:**
1. `origin/release/1.0` 브랜치 존재 확인:
   ```bash
   git rev-parse --verify origin/release/1.0
   ```

2. **브랜치가 존재하는 경우:**
   - Base: `origin/release/1.0`
   - Target: `HEAD`
   - 사용자에게 안내:
     ```
     릴리스 노트 생성 시작

     비교 범위:
       Base: origin/release/1.0
       Target: HEAD
       버전: $ARGUMENTS
     ```

3. **브랜치가 없는 경우 (첫 배포):**
   - Base: 초기 커밋 (`git rev-list --max-parents=0 HEAD`)
   - Target: `HEAD`
   - 사용자에게 안내:
     ```
     첫 배포로 감지되었습니다

     초기 커밋부터 분석합니다:
       Base: <initial-commit-sha>
       Target: HEAD
       버전: $ARGUMENTS
     ```

#### 환경 검증 실패 처리

환경 검증 실패 시 명확한 오류 메시지를 출력하고 중단합니다.

**Git 저장소 아님:**
```
오류: Git 저장소가 아닙니다

현재 디렉터리에서 'git status'를 실행할 수 없습니다.
Git 저장소 루트 디렉터리에서 명령을 실행하십시오.
```

**.NET SDK 없음:**
```
오류: .NET 10 SDK가 필요합니다

'dotnet --version' 명령을 실행할 수 없습니다.

설치 방법:
  https://dotnet.microsoft.com/download/dotnet/10.0
```

**스크립트 디렉터리 없음:**
```
오류: 릴리스 노트 스크립트를 찾을 수 없습니다

'.release-notes/scripts' 디렉터리가 존재하지 않습니다.
프로젝트 루트 디렉터리에서 명령을 실행하십시오.
```

### Phase 2: 데이터 수집

C# 스크립트를 실행하여 컴포넌트 변경사항과 API 변경사항을 분석합니다.

#### 스크립트 실행 절차

**중요: C# 스크립트 실행 방법**

C# 스크립트는 `dotnet run --project`가 아니라 **직접 실행**합니다:

```bash
# ✓ 올바른 방법
dotnet ScriptName.cs --arguments

# ✗ 잘못된 방법 (프로젝트 파일 오류 발생)
dotnet run --project ScriptName.cs --arguments
```

**작업 디렉터리 변경:**
```bash
cd .release-notes/scripts
```

**1단계: 컴포넌트 분석**
```bash
dotnet AnalyzeAllComponents.cs --base <base-branch> --target HEAD
```

- `<base-branch>`: Phase 1에서 결정한 base branch 또는 커밋 SHA
- 출력: `.analysis-output/*.md` 파일들

**예시 - 정규 릴리스:**
```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

**예시 - 첫 배포:**
```bash
# Windows (PowerShell)
$FIRST_COMMIT = git rev-list --max-parents=0 HEAD
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD

# Linux/macOS (Bash)
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

**2단계: API 변경사항 추출**
```bash
dotnet ExtractApiChanges.cs
```

- 출력: `.analysis-output/api-changes-build-current/all-api-changes.txt` (Uber 파일)
- 출력: `Src/*/.api/*.cs` (개별 API 파일)

#### 출력 검증

스크립트 실행 후 다음 파일들이 생성되었는지 확인합니다:

**필수 파일 목록:**
- `.analysis-output/analysis-summary.md` - 전체 요약
- `.analysis-output/Functorium.md` - Functorium 컴포넌트 분석
- `.analysis-output/Functorium.Testing.md` - Functorium.Testing 컴포넌트 분석
- `.analysis-output/api-changes-build-current/all-api-changes.txt` - Uber API 파일
- `.analysis-output/api-changes-build-current/api-changes-summary.md` - API 요약

**검증 방법:**
```bash
# 컴포넌트 파일 확인 (Windows)
dir .analysis-output\*.md

# Uber 파일 확인 (Windows)
type .analysis-output\api-changes-build-current\all-api-changes.txt | more
```

**검증 성공 시 콘솔 출력:**
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 2: 데이터 수집 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

생성된 컴포넌트 분석 파일:
  ✓ analysis-summary.md
  ✓ Functorium.md (31 files, 19 commits)
  ✓ Functorium.Testing.md (18 files, 13 commits)
  ✓ Docs.md (38 files, 37 commits)

생성된 API 파일:
  ✓ all-api-changes.txt (Uber 파일)
  ✓ api-changes-summary.md
  ✓ Src/Functorium/.api/Functorium.cs
  ✓ Src/Functorium.Testing/.api/Functorium.Testing.cs

위치: .release-notes/scripts/.analysis-output/
```

#### 스크립트 실행 실패 처리

**AnalyzeAllComponents.cs 실패:**
```
스크립트 실행 실패: AnalyzeAllComponents.cs

오류: <오류 메시지>

트러블슈팅:
  1. .analysis-output 폴더 삭제 후 재시도
     rmdir /s /q .analysis-output

  2. NuGet 캐시 정리
     dotnet nuget locals all --clear

  3. dotnet 프로세스 종료 (Windows)
     taskkill /F /IM dotnet.exe

  4. 상세 가이드
     .release-notes/scripts/Docs/README.md#트러블슈팅
```

**ExtractApiChanges.cs 실패:**
```
API 추출 실패: ExtractApiChanges.cs

오류: <오류 메시지>

가능한 원인:
  1. 빌드 오류: 프로젝트가 빌드되지 않음
  2. DLL 없음: 빌드 출력이 없음
  3. API 없음: Public 타입이 없음

해결 방법:
  1. 프로젝트 빌드 확인
     dotnet build -c Release

  2. 빌드 오류 수정 후 재시도

  3. 상세 가이드
     .release-notes/scripts/Docs/README.md#api-추출-문제
```

### Phase 3: 커밋 분석 및 기능 추출

수집된 데이터를 분석하여 릴리스 노트용 기능을 추출합니다.

#### 컴포넌트 분석 파일 읽기

다음 파일들을 읽어서 커밋 히스토리를 분석합니다:

1. `.analysis-output/Functorium.md`
2. `.analysis-output/Functorium.Testing.md`
3. `.analysis-output/Docs.md`
4. 기타 컴포넌트 분석 파일 (있는 경우)

**각 파일의 구조:**
```markdown
# Analysis for Src/Functorium

## Change Summary
[변경된 파일 통계]

## All Commits
[커밋 SHA와 메시지]

## Top Contributors
[기여자 목록]

## Categorized Commits

### Feature Commits
[feat, add 커밋]

### Bug Fixes
[fix 커밋]

### Breaking Changes
[breaking, BREAKING, !: 커밋]
```

#### 커밋 분류 및 우선순위 결정

**Breaking Changes 식별:**
- 패턴: `breaking`, `BREAKING`, `!:` (예: `feat!:`)
- 모든 Breaking Changes는 최우선 문서화 필요
- 마이그레이션 가이드 필수

**Feature 커밋 추출:**
- 키워드: `feat`, `feature`, `add`
- 우선순위 결정:
  - **높음**: 새 Public 타입, 통합 지원, 주요 기능
  - **중간**: 성능 개선, 구성 옵션, 오류 처리
  - **낮음**: 내부 리팩토링, 문서 업데이트

**Bug Fix 커밋 추출:**
- 키워드: `fix`, `bug`
- 중요한 버그 수정만 포함 (사용자 영향 큰 것)

**커밋 우선순위 표:**

| 우선순위 | 커밋 패턴 | 예시 |
|---------|----------|------|
| 필수 | Breaking Changes | `feat!: API 형식 변경` |
| 높음 | 새 타입/클래스 | `Add ErrorCodeFactory class` |
| 높음 | 통합 지원 | `Add OpenTelemetry integration` |
| 높음 | 주요 기능 | `Implement user authentication` |
| 중간 | 성능 개선 | `Improve query performance` |
| 중간 | 구성 옵션 | `Add configuration validation` |
| 낮음 | 내부 리팩토링 | `Refactor error handling` |
| 낮음 | 문서 업데이트 | `Update README` |

#### 기능 그룹화

**관련 커밋 통합:**

여러 커밋이 하나의 사용자 대면 기능을 구성하는 경우, 논리적으로 그룹화합니다.

**예시:**
```
개별 커밋:
  - Add ErrorCodeFactory.Create method
  - Add ErrorCodeFactory.CreateFromException method
  - Add ErrorsDestructuringPolicy

통합 기능:
  ### 함수형 오류 처리 (Error Handling)
  ErrorCodeFactory를 통한 구조화된 오류 생성 기능을 제공합니다.
  [3개 커밋을 하나의 기능으로 통합]
```

**멀티 컴포넌트 기능 식별:**

여러 컴포넌트에 걸친 변경사항을 하나의 기능으로 통합합니다.

**예시:**
```
Functorium.md:
  - Add OpenTelemetryRegistration

Functorium.Testing.md:
  - Add StructuredTestLogger for testing

통합:
  ### OpenTelemetry 통합 (Observability)
  OpenTelemetry 및 Serilog를 통합하며, 테스트 지원도 포함합니다.
```

#### API 변경사항 확인

**Uber 파일 읽기:**
```
.analysis-output/api-changes-build-current/all-api-changes.txt
```

**Uber 파일 구조:**
```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     Assembly: Functorium
//     Generated at: 2025-12-15
// </auto-generated>
//------------------------------------------------------------------------------

namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static LanguageExt.Common.Error Create(string errorCode, string errorCurrentValue, string errorMessage) { }
        public static LanguageExt.Common.Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
            where T : notnull { }
        public static LanguageExt.Common.Error CreateFromException(string errorCode, System.Exception exception) { }
    }
}
```

**API 추출 작업:**
1. 새로운 Public 타입 식별
2. 메서드 시그니처 추출 (매개변수 이름 및 타입 포함)
3. 네임스페이스 정보 추출
4. 제네릭 제약 조건 확인

**중요:** Uber 파일에 없는 API는 절대 문서화하지 않습니다.

#### 사용자 가치 추출

각 기능에 대해 다음 질문에 답하십시오:

1. **이것이 가능하게 하는 기능은?** (새 기능)
2. **개발자에게 무엇이 변경되나?** (API 영향)
3. **어떤 문제를 해결하나?** (유스케이스)
4. **브레이킹 체인지인가?** (마이그레이션 필요)

**예시:**

커밋: `Add ErrorCodeFactory.CreateFromException method`

사용자 가치:
- **기능**: 예외에서 구조화된 오류 생성
- **변경**: `ErrorCodeFactory.CreateFromException(string, Exception)` 메서드 추가
- **문제 해결**: 예외 처리를 Functorium 오류 시스템과 통합
- **브레이킹**: 아니오

#### 중간 결과 저장 (.release-notes/.work 폴더)

Phase 3의 분석 결과를 `.release-notes/.work/` 폴더에 저장하여 추적 가능하게 만듭니다:

**저장할 파일:**
```bash
.release-notes/.work/
  ├── phase3-commit-analysis.md     # 커밋 분류 및 우선순위
  ├── phase3-feature-groups.md      # 기능 그룹화 결과
  └── phase3-api-mapping.md         # API와 커밋 매핑
```

**phase3-commit-analysis.md 형식:**
```markdown
# Phase 3: 커밋 분석 결과

## Breaking Changes
- 없음

## Feature Commits (높은 우선순위)
- [cda0a33] feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가
- [1790c73] feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가

## Feature Commits (중간 우선순위)
- [4727bf9] feat(api): PublicApiGenerator로 생성한 Public API 파일 추가

## Bug Fixes
- [a8ec763] fix(build): NuGet 패키지 아이콘 경로 수정
```

**phase3-feature-groups.md 형식:**
```markdown
# Phase 3: 기능 그룹화 결과

## 그룹 1: 함수형 오류 처리
**관련 커밋:**
- ErrorCodeFactory.Create 추가
- ErrorCodeFactory.CreateFromException 추가
- ErrorsDestructuringPolicy 추가

**사용자 가치:**
구조화된 오류 생성 및 Serilog 통합

## 그룹 2: OpenTelemetry 통합
**관련 커밋:**
- OpenTelemetryRegistration 추가
- OpenTelemetryBuilder 추가
- Configurators 추가

**사용자 가치:**
분산 추적, 메트릭, 로깅 통합 지원
```

**콘솔 출력:**
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 3: 커밋 분석 및 기능 추출 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

분석 결과:
  ✓ Breaking Changes: 0개
  ✓ Feature Commits: 6개 (높은 우선순위: 4개, 중간: 2개)
  ✓ Bug Fixes: 1개
  ✓ 기능 그룹: 8개

식별된 주요 기능:
  1. 함수형 오류 처리 (ErrorCodeFactory)
  2. OpenTelemetry 통합 (Observability)
  3. 아키텍처 검증 (ArchUnitNET)
  4. 테스트 픽스처 (Host, Quartz)
  5. Serilog 테스트 유틸리티
  6. FinT 유틸리티 (LINQ 확장)
  7. Options 패턴 (FluentValidation)
  8. 유틸리티 확장 메서드

중간 결과 저장:
  ✓ .release-notes/.work/phase3-commit-analysis.md
  ✓ .release-notes/.work/phase3-feature-groups.md
  ✓ .release-notes/.work/phase3-api-mapping.md
```

### Phase 4: 릴리스 노트 작성

분석 결과를 바탕으로 전문적인 릴리스 노트를 작성합니다.

#### 문서 구조 (템플릿)

다음 구조를 따르는 `RELEASE-$ARGUMENTS.md` 파일을 `.release-notes/` 디렉터리에 생성합니다:

```markdown
# Functorium Release $ARGUMENTS

**릴리스 날짜:** YYYY-MM-DD

## 개요

[1-2 문단으로 이번 릴리스의 주요 변경사항 요약]

주요 기능:
- 기능 1
- 기능 2
- 기능 3

## Breaking Changes

[Breaking Changes가 있으면 나열, 없으면 "없음"]

### Breaking Change 제목

**이전:**
```csharp
// 이전 API (Uber 파일에서 검증)
```

**이후:**
```csharp
// 새 API (Uber 파일에서 검증)
```

**마이그레이션 가이드:**
1. 단계별 마이그레이션 절차
2. 코드 예시 포함
3. ...

## 새로운 기능

### 1. 기능 이름

[기능 설명 및 사용자 가치]

```csharp
// Uber 파일에서 검증된 코드 샘플
```

**장점:**
- 장점 1
- 장점 2

**API:**
```csharp
// Uber 파일에서 추출한 정확한 API 시그니처
namespace Functorium.Abstractions
{
    public static class ClassName
    {
        public static ReturnType MethodName(ParamType paramName);
    }
}
```

### 2. 다음 기능

[반복]

## 버그 수정

[중요한 버그 수정만 포함]

### 버그 제목

[설명 및 영향]

## API 변경사항

[API 요약 - Functorium 및 Functorium.Testing]

## 문서화

[추가/업데이트된 문서 나열]

## 알려진 제한사항

[제한사항 나열]

## 감사의 말

[사용된 오픈소스 라이브러리 크레딧]

## 설치

```bash
dotnet add package Functorium --version $ARGUMENTS
dotnet add package Functorium.Testing --version $ARGUMENTS
```
```

#### 작성 원칙 (필수 준수)

**1. 정확성 우선**
- **Uber 파일에 없는 API는 절대 문서화하지 않습니다**
- 모든 API는 정확한 매개변수 이름과 타입 포함
- 추측 금지, 검증된 정보만 사용

**2. 코드 샘플 필수**
- 모든 주요 기능에 실행 가능한 코드 샘플 포함
- 코드 샘플은 Uber 파일에서 검증된 API만 사용
- C# 구문 강조 적용 (```csharp)

**3. 추적성**
- 가능한 경우 GitHub 이슈/PR 링크 포함
- 커밋 메시지에서 `#123` 패턴 추출하여 링크
- 예: `([#123](https://github.com/org/functorium/pull/123))`

**4. 개발자 중심 언어**
- 능동태 사용 ("추가합니다" → "추가")
- 명확하고 실용적인 언어
- 전문 용어 사용 (개발자 대상)

**5. 일관된 포맷**
- Markdown 문법 준수
- 일관된 제목 계층 (H1 → H2 → H3)
- 코드 블록에 언어 지정

#### Breaking Changes 작성 가이드

**필수 요소:**
1. **제목**: 변경사항 요약
2. **이전/이후 비교**: 코드 예시로 명확하게 표시
3. **마이그레이션 가이드**: 단계별 절차
4. **영향 범위**: 어떤 코드가 영향받는지

**예시:**
```markdown
### IErrorHandler → IErrorDestructurer 이름 변경

인터페이스 이름이 더 명확한 의미를 전달하도록 변경되었습니다.

**이전:**
```csharp
public interface IErrorHandler
{
    LogEventPropertyValue Handle(Error error);
}
```

**이후:**
```csharp
public interface IErrorDestructurer
{
    LogEventPropertyValue Destructure(Error error);
}
```

**마이그레이션 가이드:**

1. 인터페이스 이름 변경:
   ```csharp
   // 이전
   public class MyHandler : IErrorHandler

   // 이후
   public class MyHandler : IErrorDestructurer
   ```

2. 메서드 이름 변경:
   ```csharp
   // 이전
   public LogEventPropertyValue Handle(Error error) { }

   // 이후
   public LogEventPropertyValue Destructure(Error error) { }
   ```

**영향 범위:**
- 커스텀 오류 핸들러를 구현한 경우
- Serilog 디스트럭처링을 확장한 경우
```

#### API 검증 프로세스

**모든 코드 샘플 검증:**

1. 문서에서 사용된 모든 API 추출
2. 각 API를 Uber 파일에서 검색
3. 존재하지 않으면 오류 보고 및 제거

**검증 방법 (Windows):**
```powershell
# Uber 파일에서 API 검색
Select-String -Path .analysis-output\api-changes-build-current\all-api-changes.txt -Pattern "MethodName"
```

**검증 실패 예시:**
```
API 검증 실패

다음 API가 Uber 파일에서 발견되지 않았습니다:
  - ErrorCodeFactory.FromException (line 123)
    올바른 이름: ErrorCodeFactory.CreateFromException

조치: 코드 샘플을 수정하거나 제거하십시오.
```

#### 중간 결과 저장 (.release-notes/.work 폴더)

Phase 4의 초안을 `.release-notes/.work/` 폴더에 저장합니다:

**저장할 파일:**
```bash
.release-notes/.work/
  ├── phase4-draft.md              # 릴리스 노트 초안
  ├── phase4-api-references.md     # 사용된 API 목록
  └── phase4-code-samples.md       # 모든 코드 샘플
```

**phase4-api-references.md 형식:**
```markdown
# Phase 4: 사용된 API 참조

## Functorium 어셈블리

### ErrorCodeFactory
- Location: Functorium.Abstractions.Errors.ErrorCodeFactory
- Methods:
  - Create(string, string, string)
  - Create<T>(string, T, string)
  - CreateFromException(string, Exception)
- Uber File: Line 75-89
- Status: ✓ 검증됨

### OpenTelemetryRegistration
- Location: Functorium.Abstractions.Registrations.OpenTelemetryRegistration
- Methods:
  - RegisterObservability(IServiceCollection, IConfiguration)
- Uber File: Line 93-96
- Status: ✓ 검증됨
```

**콘솔 출력:**
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 4: 릴리스 노트 작성 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

작성 통계:
  ✓ 전체 길이: 15,380 줄
  ✓ 섹션 수: 8개
  ✓ 코드 샘플: 24개
  ✓ API 참조: 30개 타입

주요 섹션:
  1. 개요 (버전: v1.0.0-alpha.1)
  2. Breaking Changes (0개)
  3. 새로운 기능 (8개)
  4. 버그 수정 (1개)
  5. API 변경사항 (요약)
  6. 문서화 (38개 문서)
  7. 알려진 제한사항
  8. 감사의 말

출력 파일:
  ✓ .release-notes/RELEASE-v1.0.0-alpha.1.md

중간 결과 저장:
  ✓ .release-notes/.work/phase4-draft.md
  ✓ .release-notes/.work/phase4-api-references.md
  ✓ .release-notes/.work/phase4-code-samples.md
```

### Phase 5: 검증

생성된 릴리스 노트의 품질 및 정확성을 검증합니다.

#### 검증 항목

**1. API 정확성 검증**

문서의 모든 코드 샘플에서 API를 추출하고 Uber 파일과 대조합니다.

**검증 절차:**
- [ ] 모든 `public class`, `public static class` 이름 확인
- [ ] 모든 `public` 메서드 이름 및 시그니처 확인
- [ ] 매개변수 이름 및 타입 정확히 일치 확인
- [ ] 네임스페이스 정확히 일치 확인

**통과 기준:**
- Uber 파일에 없는 API 사용: 0개
- 매개변수 불일치: 0개

**2. Breaking Changes 검증**

Breaking Changes 섹션의 완전성을 확인합니다.

**검증 절차:**
- [ ] Breaking Changes 섹션 존재 확인
- [ ] 각 Breaking Change에 마이그레이션 가이드 존재
- [ ] 이전/이후 코드 비교 포함
- [ ] 영향 범위 명시

**통과 기준:**
- 모든 Breaking Change 커밋이 문서화됨
- 각 Breaking Change에 완전한 마이그레이션 가이드 포함

**3. Markdown 포맷 검증**

Markdown 문법 및 포맷팅을 검증합니다.

**검증 항목:**
- [ ] YAML frontmatter 없음 (필요 없음)
- [ ] H1 제목 하나만 존재
- [ ] 일관된 제목 계층 구조
- [ ] 모든 코드 블록에 언어 지정
- [ ] 링크 형식 올바름

**선택적: Markdownlint 실행**
```bash
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-$ARGUMENTS.md --disable MD013
```

**4. 체크리스트 검증**

`.release-notes/scripts/Docs/validation-checklist.md` 기준을 적용합니다.

**포괄적인 분석:**
- [ ] 모든 중요한 커밋이 분석됨
- [ ] 높은 우선순위 커밋이 모두 포함됨
- [ ] 멀티 컴포넌트 기능이 통합됨

**API 정확성:**
- [ ] 모든 API가 Uber 파일에서 검증됨
- [ ] 발명된 API 없음
- [ ] 매개변수 이름/타입 정확히 일치

**Breaking Changes 완전성:**
- [ ] Breaking Changes가 실제 API diff 반영
- [ ] 모든 Breaking Changes에 마이그레이션 가이드
- [ ] API 변경에 대한 이전/이후 예시

**구조 및 품질:**
- [ ] 템플릿 구조를 따름
- [ ] 일관된 포맷팅
- [ ] 개발자 중심 언어
- [ ] 추적성 참조 포함

#### 검증 결과 보고

**검증 통과 - 콘솔 출력:**
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: 검증 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

검증 항목 통과:
  ✓ API 정확성 (0 오류)
    - ErrorCodeFactory ✓
    - OpenTelemetryRegistration ✓
    - ArchitectureValidationEntryPoint ✓
    - HostTestFixture ✓
    - QuartzTestFixture ✓
    - LogEventPropertyExtractor ✓
    - FinTUtilites ✓

  ✓ Breaking Changes 완전성
    - 첫 릴리스, Breaking Changes 없음

  ✓ Markdown 포맷
    - H1 제목: 1개
    - 일관된 제목 계층
    - 코드 블록 언어 지정: 100%

  ✓ 체크리스트 (100%)
    - 포괄적인 분석 ✓
    - API 정확성 ✓
    - 구조 및 품질 ✓

검증 결과 저장:
  ✓ .release-notes/.work/phase5-validation-report.md
  ✓ .release-notes/.work/phase5-api-validation.md

상태: 게시 가능 ✓
```

**검증 실패 - 콘솔 출력:**
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: 검증 실패 ✗
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

발견된 문제:

API 정확성 (2 오류):
  ✗ ErrorCodeFactory.FromException (line 123)
    위치: RELEASE-v1.0.0-alpha.1.md:123
    문제: Uber 파일에 없는 API
    제안: ErrorCodeFactory.CreateFromException 사용

  ✗ OpenTelemetryBuilder.Register (line 456)
    위치: RELEASE-v1.0.0-alpha.1.md:456
    문제: 매개변수 불일치
    Uber: RegisterObservability(IServiceCollection, IConfiguration)
    문서: Register(IServiceCollection)

Breaking Changes (1 오류):
  ✗ IErrorHandler → IErrorDestructurer 이름 변경
    문제: 마이그레이션 가이드 누락
    필요: 이전/이후 코드 예시 및 단계별 가이드

Markdown 포맷 (경고):
  ⚠ 코드 블록 언어 미지정: 2개
    - Line 234: ```
    - Line 567: ```

검증 결과 저장:
  ✓ .release-notes/.work/phase5-validation-report.md
  ✓ .release-notes/.work/phase5-errors.md

조치 필요:
  1. 문서 수정
  2. 검증 재실행
```

#### 자동 수정 시도

가능한 경우 자동으로 문제를 수정합니다:

**수정 가능한 문제:**
- 잘못된 API 이름 (Uber 파일에서 유사한 이름 찾기)
- 누락된 매개변수 타입 (Uber 파일에서 완전한 시그니처 가져오기)
- 포맷팅 문제 (Markdown 문법 수정)

**수정 불가능한 문제:**
- 누락된 마이그레이션 가이드 (수동 작성 필요)
- 불완전한 설명 (수동 보완 필요)
- 컨텍스트 부족 (추가 조사 필요)

## 완료 메시지

릴리스 노트 생성 완료 시 다음 형식으로 표시합니다:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
릴리스 노트 생성 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

버전: v1.0.0-alpha.1
파일: .release-notes/RELEASE-v1.0.0-alpha.1.md

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 통계 요약

컴포넌트 분석:
  • Functorium: 31 files, 19 commits
  • Functorium.Testing: 18 files, 13 commits
  • Docs: 38 files, 37 commits

릴리스 노트:
  • Breaking Changes: 0개
  • 새로운 기능: 8개
  • 버그 수정: 1개
  • 문서화: 38개 문서
  • 코드 샘플: 24개
  • API 참조: 30개 타입

검증 상태: ✓ 통과

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📁 생성된 파일

릴리스 노트:
  ✓ .release-notes/RELEASE-v1.0.0-alpha.1.md

분석 데이터:
  ✓ .release-notes/scripts/.analysis-output/analysis-summary.md
  ✓ .release-notes/scripts/.analysis-output/Functorium.md
  ✓ .release-notes/scripts/.analysis-output/Functorium.Testing.md
  ✓ .release-notes/scripts/.analysis-output/Docs.md
  ✓ .release-notes/scripts/.analysis-output/api-changes-build-current/all-api-changes.txt

중간 결과 (.release-notes/.work 폴더):
  ✓ .release-notes/.work/phase3-commit-analysis.md
  ✓ .release-notes/.work/phase3-feature-groups.md
  ✓ .release-notes/.work/phase3-api-mapping.md
  ✓ .release-notes/.work/phase4-draft.md
  ✓ .release-notes/.work/phase4-api-references.md
  ✓ .release-notes/.work/phase4-code-samples.md
  ✓ .release-notes/.work/phase5-validation-report.md
  ✓ .release-notes/.work/phase5-api-validation.md

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 다음 단계

1. 생성된 릴리스 노트 검토
   cat .release-notes/RELEASE-v1.0.0-alpha.1.md

2. 중간 결과 확인 (선택적)
   ls -la .release-notes/.work/

3. 필요시 수동 수정
   • 복잡한 마이그레이션 가이드 보완
   • 추가 설명 및 예시 추가
   • GitHub 이슈/PR 링크 추가

4. Git에 커밋
   git add .release-notes/RELEASE-v1.0.0-alpha.1.md
   git commit -m "docs: 릴리스 노트 v1.0.0-alpha.1"
   git push

5. GitHub Release 생성 (선택적)
   https://github.com/<org>/<repo>/releases/new

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

## 참고 문서

릴리스 노트 생성 프로세스에 대한 상세 가이드:

- [data-collection.md](.release-notes/scripts/Docs/data-collection.md) - 데이터 수집 프로세스
- [commit-analysis.md](.release-notes/scripts/Docs/commit-analysis.md) - 커밋 분석 방법론
- [api-documentation.md](.release-notes/scripts/Docs/api-documentation.md) - API 검증 프로세스
- [writing-guidelines.md](.release-notes/scripts/Docs/writing-guidelines.md) - 문서 작성 스타일
- [validation-checklist.md](.release-notes/scripts/Docs/validation-checklist.md) - 검증 기준
- [README.md](.release-notes/scripts/Docs/README.md) - 전체 프로세스 개요

## 트러블슈팅

### 일반적인 문제 해결

**1. Base Branch 없음**

**증상:**
```
Base branch 'origin/release/1.0' does not exist.
```

**해결:**
첫 배포로 자동 감지되며, 초기 커밋부터 분석합니다.

**수동 해결 (필요시):**
```bash
# 릴리스 브랜치 생성
git checkout -b release/1.0
git push -u origin release/1.0

# 다시 시도
/release-notes $ARGUMENTS
```

**2. .NET SDK 버전 오류**

**증상:**
```
error CS8652: The feature 'top-level statements' is not available in C# 9.0
```

**해결:**
```bash
# .NET 버전 확인
dotnet --version

# .NET 10 SDK 설치
# https://dotnet.microsoft.com/download/dotnet/10.0
```

**3. 파일 잠금 문제**

**증상:**
```
The process cannot access the file because it is being used by another process
```

**해결 (Windows):**
```powershell
# dotnet 프로세스 종료
Stop-Process -Name "dotnet" -Force

# 출력 디렉터리 삭제
Remove-Item -Recurse -Force .release-notes\scripts\.analysis-output

# 다시 시도
/release-notes $ARGUMENTS
```

**4. API 검증 실패**

**증상:**
```
API 검증 실패: MethodName not found in Uber file
```

**해결:**
```bash
# 1. Uber 파일에서 API 검색 (Windows)
Select-String -Path .release-notes\scripts\.analysis-output\api-changes-build-current\all-api-changes.txt -Pattern "MethodName"

# 2. API가 없으면 코드 샘플 수정
#    - API 이름 수정
#    - 또는 코드 샘플 제거

# 3. Uber 파일에 있는 API만 문서화
```

**5. NuGet 캐시 문제**

**증상:**
```
error NU1301: Unable to load the service index
```

**해결:**
```bash
# NuGet 캐시 정리
dotnet nuget locals all --clear

# 다시 시도
/release-notes $ARGUMENTS
```

### 전체 초기화 (Windows)

모든 캐시와 출력을 삭제하고 처음부터 다시 시작합니다:

```powershell
# dotnet 프로세스 종료
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue

# 출력 디렉터리 삭제
Remove-Item -Recurse -Force .release-notes\scripts\.analysis-output -ErrorAction SilentlyContinue

# NuGet 캐시 정리
dotnet nuget locals all --clear

# 다시 시도
/release-notes $ARGUMENTS
```

### 상세 가이드

더 많은 트러블슈팅 정보는 다음 문서를 참조하십시오:

- `.release-notes/scripts/Docs/README.md` - 10가지 일반적인 문제 및 해결 방법
- `.release-notes/scripts/Docs/data-collection.md` - 데이터 수집 문제 해결

## 핵심 원칙

릴리스 노트 생성 시 다음 원칙을 준수하십시오:

### 1. 정확성 우선

> **Uber 파일에 없는 API는 절대 문서화하지 않습니다.**

- 모든 API를 Uber 파일에서 검증
- 매개변수 이름 및 타입 정확히 일치
- 추측 금지, 검증된 정보만 사용

### 2. 완전 자동화

- 사용자 개입 최소화
- 오류 발생 시 명확한 메시지와 복구 방법 제공
- 가능한 경우 자동 복구 시도

### 3. 추적성

- 모든 기능을 실제 커밋으로 추적
- GitHub 이슈/PR 링크 포함 (가능한 경우)
- 커밋 SHA 참조

### 4. 개발자 경험

- 명확하고 실행 가능한 코드 샘플
- 능동태 및 개발자 중심 언어
- 실용적인 예시와 가이드

## 제한사항

현재 버전의 제한사항:

1. **완전 자동화**: 사용자 개입 없이 진행되므로 복잡한 마이그레이션 가이드는 사후 검토 필요
2. **GitHub API 통합 없음**: PR/이슈 조회 제한적 (커밋 메시지에서만 추출)
3. **한국어 전용**: 영어 릴리스 노트는 수동 작성 필요
4. **단일 버전**: 한 번에 하나의 버전만 처리 (여러 버전 동시 생성 불가)

## 향후 개선 가능성

다음 기능들이 향후 추가될 수 있습니다:

1. **대화형 모드**: 각 단계 완료 후 사용자 확인
2. **부분 실행**: 특정 Phase만 실행 (예: `--phase analyze`)
3. **템플릿 커스터마이징**: 사용자 정의 템플릿 지원
4. **다국어 지원**: 영어/한국어 선택 가능
5. **GitHub 통합**: 자동으로 GitHub Release 생성
