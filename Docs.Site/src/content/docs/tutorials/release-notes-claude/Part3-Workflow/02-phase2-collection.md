---
title: "데이터 수집"
---

환경이 준비되었으니, 이제 릴리스 노트의 원재료가 되는 데이터를 수집할 차례입니다. Phase 2에서는 두 개의 C# 스크립트가 순서대로 실행되어, 컴포넌트별 변경사항과 전체 Public API를 추출합니다. 이 단계에서 만들어진 데이터가 이후 모든 분석과 문서 작성의 기반이 되므로, 정확하고 완전한 수집이 중요합니다.

모든 작업은 스크립트 디렉터리에서 수행합니다.

```bash
cd .release-notes/scripts
```

## 1단계: 컴포넌트 변경사항 분석

첫 번째 스크립트 `AnalyzeAllComponents.cs`는 Git 히스토리를 탐색하여 각 컴포넌트(프로젝트)별로 어떤 파일이 변경되었고, 어떤 커밋이 있었는지 분석합니다. Phase 1에서 결정한 Base/Target 범위를 인자로 전달합니다.

첫 배포일 때는 초기 커밋부터 분석합니다.

```bash
# 초기 커밋 SHA 찾기
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)

# 초기 커밋부터 현재까지 분석
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

후속 릴리스일 때는 이전 release 브랜치를 기준으로 합니다.

```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

이 스크립트가 생성하는 결과물은 두 종류입니다.

**개별 컴포넌트 분석 파일** (`.analysis-output/*.md`)은 각 컴포넌트별로 하나씩 생성됩니다. 파일에는 추가/수정/삭제된 파일 수 같은 전체 변경 통계, 해당 컴포넌트의 완전한 커밋 히스토리, 주요 기여자 정보, 그리고 기능/버그 수정/Breaking Changes로 분류된 커밋 목록이 포함됩니다. Phase 3에서 커밋을 분석하고 기능을 추출할 때 이 파일들이 주요 입력이 됩니다.

**분석 요약** (`.analysis-output/analysis-summary.md`)은 모든 컴포넌트 변경사항의 고수준 개요입니다. 각 컴포넌트의 변경 파일 수와 생성된 분석 파일 목록을 한눈에 보여줍니다.

## 2단계: API 변경사항 추출

두 번째 스크립트 `ExtractApiChanges.cs`는 프로젝트를 빌드하고 DLL에서 Public API를 추출합니다. 1단계가 "무엇이 변경되었는가"를 알려준다면, 2단계는 "현재 API가 정확히 어떤 모습인가"를 알려줍니다.

```bash
dotnet ExtractApiChanges.cs
```

이 스크립트는 세 가지 핵심 결과물을 생성합니다.

**Uber API 파일** (`all-api-changes.txt`)은 이 워크플로우의 **단일 진실 소스(Single Source of Truth)입니다.** `.analysis-output/api-changes-build-current/` 디렉터리에 생성되며, 현재 빌드의 모든 Public API 정의가 정확한 매개변수 이름과 타입과 함께 담겨 있습니다. Phase 4에서 코드 샘플을 작성할 때, 이 파일에 없는 API는 문서화하지 않습니다. 존재하지 않는 API를 릴리스 노트에 포함하는 것을 방지하는 가장 중요한 장치입니다.

**API 변경 Diff** (`api-changes-diff.txt`)는 `.api` 폴더의 Git diff로, Breaking Changes를 자동 감지하는 데 사용됩니다. 삭제되거나 시그니처가 변경된 API를 객관적으로 식별할 수 있어, 커밋 메시지만으로는 놓칠 수 있는 Breaking Changes를 잡아냅니다.

**개별 API 파일** (`Src/*/.api/*.cs`)은 각 어셈블리의 Public API를 C# 소스 코드 형식으로 정의합니다. Git으로 추적되어 API 변경 이력을 관리할 수 있습니다.

## 출력 구조

스크립트 실행 후 생성되는 전체 파일 구조입니다.

```txt
.release-notes/scripts/
└── .analysis-output/
    ├── analysis-summary.md          # 전체 요약
    ├── Functorium.md                # Src/Functorium 분석
    ├── Functorium.Testing.md        # Src/Functorium.Testing 분석
    ├── Docs.md                      # Docs 분석
    └── api-changes-build-current/
        ├── all-api-changes.txt      # Uber 파일 (전체 API)
        ├── api-changes-summary.md   # API 요약
        └── api-changes-diff.txt     # API 차이점
```

## 컴포넌트 분석 파일 구조

각 컴포넌트 파일이 어떤 형식으로 구성되는지 살펴보겠습니다.

````markdown
# Analysis for Src/Functorium

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Change Summary
[git diff --stat 출력]

## All Commits
[커밋 SHA와 메시지 목록]

## Top Contributors
[기여자별 커밋 수]

## Categorized Commits

### Feature Commits
[feat, feature, add 패턴 커밋]

### Bug Fixes
[fix, bug 패턴 커밋]

### Breaking Changes
[breaking, BREAKING, !: 패턴 커밋]
````

## 커밋 분류 패턴

`AnalyzeAllComponents.cs`는 Conventional Commits 패턴으로 커밋을 자동 분류합니다. Phase 3에서 더 정교한 분석이 이루어지지만, 이 단계에서의 1차 분류가 기초 데이터를 제공합니다.

### Feature Commits

검색 키워드: `feat`, `feature`, `add`

```txt
예시:
- feat: 사용자 인증 추가
- add: 로깅 기능
- feature(api): 새 엔드포인트
```

### Bug Fixes

검색 키워드: `fix`, `bug`

```txt
예시:
- fix: null 참조 예외 처리
- bug: 메모리 누수 수정
```

### Breaking Changes

검색 조건 (OR):
1. `breaking` 또는 `BREAKING` 문자열 포함
2. 타입 뒤 `!` 패턴 (예: `feat!:`, `fix!:`)

```txt
예시:
- feat!: API 응답 형식 변경
- feat!: BREAKING CHANGE: API 형식 변경
- fix: breaking: 호환성 변경
```

### 기타 Conventional Commits 타입

| 타입 | 설명 | 릴리스 노트 포함 |
|------|------|:---------------:|
| `docs` | 문서 변경 | 보통 생략 |
| `refactor` | 리팩터링 | 보통 생략 |
| `perf` | 성능 개선 | 포함 |
| `test` | 테스트 추가/수정 | 생략 |
| `build` | 빌드 시스템 변경 | 보통 생략 |
| `chore` | 기타 변경 | 생략 |
| `ci` | CI 설정 변경 | 생략 |

## 데이터 수집 검증

스크립트 실행이 끝나면, 결과물이 제대로 생성되었는지 확인합니다.

### 컴포넌트 분석 검증

```bash
# 컴포넌트 파일 수 확인
ls -1 .analysis-output/*.md | wc -l

# 주요 컴포넌트 존재 확인
ls .analysis-output/Functorium*.md

# 분석 요약 확인
cat .analysis-output/analysis-summary.md
```

### API 변경사항 검증

```bash
# Uber 파일 존재 및 크기 확인
wc -l .analysis-output/api-changes-build-current/all-api-changes.txt

# 주요 API 확인 (예시)
grep -c "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt

# API 파일 확인
ls Src/*/.api/*.cs
```

## 콘솔 출력 형식

### 데이터 수집 성공

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 2: 데이터 수집 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

생성된 컴포넌트 분석 파일:
  analysis-summary.md
  Functorium.md (31 files, 19 commits)
  Functorium.Testing.md (18 files, 13 commits)
  Docs.md (38 files, 37 commits)

생성된 API 파일:
  all-api-changes.txt (Uber 파일)
  api-changes-summary.md
  api-changes-diff.txt
  Src/Functorium/.api/Functorium.cs
  Src/Functorium.Testing/.api/Functorium.Testing.cs

위치: .release-notes/scripts/.analysis-output/
```

## 오류 처리

### AnalyzeAllComponents.cs 실패

```txt
스크립트 실행 실패: AnalyzeAllComponents.cs

오류: <오류 메시지>

트러블슈팅:
  1. .analysis-output 폴더 삭제 후 재시도
     rmdir /s /q .analysis-output  (Windows)
     rm -rf .analysis-output       (Linux/Mac)

  2. NuGet 캐시 정리
     dotnet nuget locals all --clear

  3. dotnet 프로세스 종료 (Windows)
     taskkill /F /IM dotnet.exe
```

### ExtractApiChanges.cs 실패

```txt
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
```

## 주의할 점

데이터 수집은 워크플로우 시작 시 **한 번만** 수행합니다. Phase 4에서 문서를 작성하는 도중에 스크립트를 다시 실행하면 분석 결과가 덮어씌워져 일관성이 깨질 수 있습니다. Uber 파일이 모든 API 검증의 단일 진실 소스이므로, 문서화된 모든 API는 반드시 이 파일에 존재해야 합니다. 커밋 분석 결과는 기능 기반으로 제공되어, 릴리스 노트의 섹션 구성에 직접 활용됩니다.

## FAQ

### Q1: `AnalyzeAllComponents.cs`와 `ExtractApiChanges.cs`의 실행 순서가 중요한가요?
**A**: 중요합니다. `AnalyzeAllComponents.cs`를 먼저 실행하여 컴포넌트별 커밋 히스토리를 수집하고, 그 다음 `ExtractApiChanges.cs`를 실행하여 현재 빌드의 Public API를 추출합니다. 두 스크립트가 생성하는 데이터는 서로 다른 용도로 사용되지만, **Phase 3에서 두 결과를 함께 분석해야** 완전한 릴리스 노트를 작성할 수 있습니다.

### Q2: 데이터 수집을 워크플로우 도중에 다시 실행하면 어떤 문제가 생기나요?
**A**: `.analysis-output/` 폴더의 파일이 덮어씌워져 **데이터 일관성이 깨집니다.** Phase 3에서 이미 분석한 커밋 목록과 Phase 4에서 참조하는 Uber 파일이 달라질 수 있으므로, 데이터 수집은 워크플로우 시작 시 **한 번만** 수행해야 합니다.

### Q3: Uber 파일에 없는 API가 릴리스 노트에 포함되면 어떤 일이 발생하나요?
**A**: Phase 5 검증에서 "API 정확성 오류"로 감지됩니다. Uber 파일은 컴파일된 DLL에서 추출한 것이므로, 여기에 없는 API는 실제로 존재하지 않거나 internal 접근 수준입니다. 이를 문서화하면 사용자가 존재하지 않는 API를 사용하려다 컴파일 오류를 경험하게 됩니다.

데이터 수집이 완료되면, 원시 데이터를 의미 있는 기능으로 변환하는 [Phase 3: 커밋 분석](03-phase3-analysis.md)으로 진행합니다.
