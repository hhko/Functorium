# Phase 2: 데이터 수집

## 목표

C# 스크립트로 컴포넌트/API 변경사항을 분석하여 릴리스 노트 생성에 필요한 모든 데이터를 수집합니다.

## 작업 디렉터리

```bash
cd .release-notes/scripts
```

## 데이터 수집 단계

### 1단계: 컴포넌트 변경사항 분석

```bash
dotnet AnalyzeAllComponents.cs --base <base_branch> --target <target_branch>
```

**예시 - 첫 배포 (태그 없는 경우):**

```bash
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target origin/main
```

**예시 - 릴리스 간 비교:**

```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target origin/main
```

#### 생성되는 결과물

**개별 컴포넌트 분석 파일** (`.analysis-output/*.md`)

- 각 Functorium 컴포넌트별 파일 생성
- 각 파일 포함 내용:
  - **전체 변경 통계**: 추가/수정/삭제된 파일 수
  - **완전한 커밋 히스토리**: 해당 컴포넌트의 릴리스 간 모든 커밋
  - **주요 기여자**: 가장 많은 변경을 한 사람
  - **분류된 커밋**: 기능, 버그 수정, 브레이킹 체인지

**분석 요약** (`.analysis-output/analysis-summary.md`)

- 모든 컴포넌트 변경사항의 고수준 개요
- 각 컴포넌트의 변경 파일 수
- 생성된 분석 파일 목록

#### 실제 출력 예시

```
.analysis-output/
├── analysis-summary.md          # 전체 요약
├── Functorium.md                # Src/Functorium 분석 (30 files)
├── Functorium.Testing.md        # Src/Functorium.Testing 분석 (17 files)
└── Docs.md                      # Docs 분석 (38 files)
```

### 2단계: API 변경사항 추출

```bash
dotnet ExtractApiChanges.cs
```

#### 생성되는 결과물

**Uber API 파일** (`.analysis-output/api-changes-build-current/all-api-changes.txt`)

- 모든 API 참조의 **단일 진실 소스**
- 현재 빌드의 완전한 API 정의
- 정확한 매개변수 이름과 타입을 포함한 메서드 시그니처
- **코드 샘플 검증에 중요** - 이 파일에 없으면 문서화하지 않습니다

**API 변경 요약** (`.analysis-output/api-changes-build-current/api-changes-summary.md`)

- 생성된 API 파일 목록
- 사용된 도구 버전 정보
- 각 어셈블리별 API 파일 경로

**API 변경 Diff** (`.analysis-output/api-changes-build-current/api-changes-diff.txt`)

- `.api` 폴더의 Git diff
- Breaking Changes 자동 감지에 사용
- 삭제/변경된 API 식별

**개별 API 파일** (`Src/*/.api/*.cs`)

- 각 어셈블리의 Public API 정의
- C# 소스 코드 형식
- Git으로 추적되어 API 변경 이력 관리 가능

#### 실제 출력 예시

```
.analysis-output/api-changes-build-current/
├── all-api-changes.txt          # UBER 파일 - 전체 API
├── api-changes-summary.md       # API 요약
└── api-changes-diff.txt         # API 차이점

Src/
├── Functorium/.api/
│   └── Functorium.cs            # Functorium Public API
└── Functorium.Testing/.api/
    └── Functorium.Testing.cs    # Functorium.Testing Public API
```

### 3단계: 데이터 수집 결과 검증

두 스크립트 실행 후 다음을 확인합니다:

#### 컴포넌트 분석 검증

```bash
# 컴포넌트 파일 수 확인
ls -1 .analysis-output/*.md | wc -l

# 주요 컴포넌트 존재 확인
ls .analysis-output/Functorium*.md

# 분석 요약 확인
cat .analysis-output/analysis-summary.md
```

#### API 변경사항 검증

```bash
# Uber 파일 존재 및 크기 확인
wc -l .analysis-output/api-changes-build-current/all-api-changes.txt

# 주요 API 확인 (예시)
grep -c "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt

# API 파일 확인
ls Src/*/.api/*.cs
```

## 출력 이해하기

### 컴포넌트 분석 파일 구조

각 컴포넌트 파일 (`*.md`)은 다음 구조를 따릅니다:

```markdown
# Analysis for Src/Functorium

Generated: <timestamp>
Comparing: <base_branch> -> <target_branch>

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
[breaking, BREAKING 패턴 커밋]
```

### 주요 커밋 패턴 (Conventional Commits)

AnalyzeAllComponents.cs는 다음 패턴으로 커밋을 분류합니다:

#### Feature Commits

검색 키워드: `feat`, `feature`, `add`

- 새로운 기능 추가
- 예시: `feat: 사용자 인증 추가`, `add: 로깅 기능`

#### Bug Fixes

검색 키워드: `fix`, `bug`

- 버그 수정
- 예시: `fix: null 참조 예외 처리`, `bug: 메모리 누수 수정`

#### Breaking Changes

검색 조건 (OR 조건):

1. `breaking` 또는 `BREAKING` 문자열 포함
2. 타입 뒤 `!` 패턴 (예: `feat!:`, `fix!:`)

호환성을 깨는 변경사항을 찾습니다:

- `feat!: API 응답 형식 변경` - 잡힘 (`!` 패턴)
- `feat!: BREAKING CHANGE: API 형식 변경` - 잡힘 (`!` + BREAKING)
- `fix: breaking: 호환성 변경` - 잡힘 (breaking 키워드)
- 푸터에 `BREAKING CHANGE:` 명시 - 잡힘 (BREAKING 키워드)

**검색 로직:**
Regex 패턴 `\b\w+!:` 으로 타입!: 형식을 감지하고, "breaking" 또는 "BREAKING" 키워드도 검색합니다.

**권장 커밋 형식:**

- `feat!: API 응답 형식 변경` (타입 뒤 느낌표)
- `feat!: BREAKING CHANGE: 상세 설명` (느낌표 + 푸터)
- `refactor: breaking: 레거시 메서드 제거` (키워드 사용)

#### 기타 Conventional Commits 타입

분석 도구가 직접 분류하지는 않지만 릴리스 노트 작성 시 참고:

| 타입       | 설명                             |
| ---------- | -------------------------------- |
| `docs`     | 문서 변경                        |
| `refactor` | 리팩터링 (기능/버그 수정 아님)   |
| `perf`     | 성능 개선                        |
| `test`     | 테스트 추가/수정                 |
| `build`    | 빌드 시스템/의존성 변경          |
| `chore`    | 기타 변경                        |
| `style`    | 코드 포맷팅 (동작 변경 없음)     |
| `ci`       | CI 설정 변경                     |

#### GitHub 참조

- **이슈/PR 번호** (`#12345`) → 추가 컨텍스트 확인
- 커밋 메시지에 포함된 GitHub 참조를 조회하여 상세 정보 파악

## 콘솔 출력 형식

### 데이터 수집 성공

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
  ✓ api-changes-diff.txt
  ✓ Src/Functorium/.api/Functorium.cs
  ✓ Src/Functorium.Testing/.api/Functorium.Testing.cs

위치: .release-notes/scripts/.analysis-output/
```

### 스크립트 실행 실패 처리

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
     .release-notes/scripts/docs/README.md#트러블슈팅
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
     .release-notes/scripts/docs/README.md#api-추출-문제
```

## 데이터 수집 체크리스트

- [ ] 컴포넌트 분석 완료 (`dotnet AnalyzeAllComponents.cs`)
- [ ] API 변경사항 추출 완료 (`dotnet ExtractApiChanges.cs`)
- [ ] 컴포넌트 파일 생성됨 (`.analysis-output/*.md`)
- [ ] 분석 요약 생성됨 (`.analysis-output/analysis-summary.md`)
- [ ] Uber API 파일 생성됨 (`all-api-changes.txt`)
- [ ] API 요약 생성됨 (`api-changes-summary.md`)
- [ ] API Diff 파일 생성됨 (`api-changes-diff.txt`)
- [ ] 개별 API 파일 생성됨 (`Src/*/.api/*.cs`)

## 성공 기준

- [ ] `.analysis-output/*.md` 파일 생성됨
- [ ] `all-api-changes.txt` Uber 파일 생성됨
- [ ] `api-changes-diff.txt` Git Diff 파일 생성됨

## 중요 사항

- **문서 작성 중 스크립트 실행 금지** - 데이터 수집은 사전에 한 번만 수행
- **Uber 파일이 단일 진실 소스** - API 검증용
- **모든 문서화된 API는 반드시 존재해야 함** - Uber 파일에서 확인
- **커밋 분석이 기능 기반 제공** - 릴리스 노트용
- **GitHub 이슈 조회로 커밋 이해 향상** - 추가 컨텍스트 제공

## 다음 단계

데이터 수집 완료 후 [Phase 3: 커밋 분석](phase3-analysis.md)으로 진행합니다.
