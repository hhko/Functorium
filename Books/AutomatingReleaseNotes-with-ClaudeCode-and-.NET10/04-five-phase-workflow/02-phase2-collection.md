# 4.2 Phase 2: 데이터 수집

> Phase 2에서는 C# 스크립트를 실행하여 컴포넌트별 변경사항과 API 변경사항을 분석합니다.

---

## 목표

C# 스크립트로 컴포넌트/API 변경사항을 분석하여 릴리스 노트 생성에 필요한 모든 데이터를 수집합니다.

---

## 작업 디렉터리

Phase 2 작업은 스크립트 디렉터리에서 수행합니다:

```bash
cd .release-notes/scripts
```

---

## 데이터 수집 단계

### 1단계: 컴포넌트 변경사항 분석

```bash
dotnet AnalyzeAllComponents.cs --base <base_branch> --target <target_branch>
```

#### 첫 배포 예시

태그나 release 브랜치가 없는 경우:

```bash
# 초기 커밋 SHA 찾기
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)

# 초기 커밋부터 현재까지 분석
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

#### 후속 릴리스 예시

이전 release 브랜치가 있는 경우:

```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

#### 생성되는 결과물

**개별 컴포넌트 분석 파일** (`.analysis-output/*.md`)

각 컴포넌트별로 다음 내용이 포함된 파일이 생성됩니다:

| 항목 | 설명 |
|------|------|
| 전체 변경 통계 | 추가/수정/삭제된 파일 수 |
| 완전한 커밋 히스토리 | 해당 컴포넌트의 모든 커밋 |
| 주요 기여자 | 가장 많은 변경을 한 사람 |
| 분류된 커밋 | 기능, 버그 수정, Breaking Changes |

**분석 요약** (`.analysis-output/analysis-summary.md`)

- 모든 컴포넌트 변경사항의 고수준 개요
- 각 컴포넌트의 변경 파일 수
- 생성된 분석 파일 목록

---

### 2단계: API 변경사항 추출

```bash
dotnet ExtractApiChanges.cs
```

이 스크립트는 프로젝트를 빌드하고 DLL에서 Public API를 추출합니다.

#### 생성되는 결과물

**Uber API 파일** (`all-api-changes.txt`)

```txt
위치: .analysis-output/api-changes-build-current/all-api-changes.txt
```

이 파일은 **단일 진실 소스(Single Source of Truth)**입니다:
- 현재 빌드의 **모든 Public API** 정의
- 정확한 매개변수 이름과 타입
- **코드 샘플 검증에 필수** - 이 파일에 없으면 문서화하지 않음

**API 변경 Diff** (`api-changes-diff.txt`)

```txt
위치: .analysis-output/api-changes-build-current/api-changes-diff.txt
```

- `.api` 폴더의 Git diff
- **Breaking Changes 자동 감지**에 사용
- 삭제/변경된 API 식별

**개별 API 파일** (`Src/*/.api/*.cs`)

```txt
Src/
├── Functorium/.api/
│   └── Functorium.cs
└── Functorium.Testing/.api/
    └── Functorium.Testing.cs
```

- 각 어셈블리의 Public API 정의
- C# 소스 코드 형식
- Git으로 추적되어 API 변경 이력 관리

---

### 3단계: 테스트 결과 요약 (선택적)

```bash
dotnet SummarizeSlowestTests.cs
```

TRX 파일이 있는 경우 테스트 결과를 요약합니다.

---

## 출력 구조

스크립트 실행 후 생성되는 파일 구조:

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

---

## 컴포넌트 분석 파일 구조

각 컴포넌트 파일 (`*.md`)의 구조:

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

---

## 커밋 분류 패턴

AnalyzeAllComponents.cs는 Conventional Commits 패턴으로 커밋을 분류합니다:

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

---

## 데이터 수집 검증

스크립트 실행 후 다음을 확인합니다:

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

---

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

---

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

---

## 성공 기준 체크리스트

Phase 2 완료를 위해 다음을 모두 확인하세요:

- [ ] 컴포넌트 분석 완료 (`dotnet AnalyzeAllComponents.cs`)
- [ ] API 변경사항 추출 완료 (`dotnet ExtractApiChanges.cs`)
- [ ] 컴포넌트 파일 생성됨 (`.analysis-output/*.md`)
- [ ] 분석 요약 생성됨 (`.analysis-output/analysis-summary.md`)
- [ ] Uber API 파일 생성됨 (`all-api-changes.txt`)
- [ ] API Diff 파일 생성됨 (`api-changes-diff.txt`)
- [ ] 개별 API 파일 생성됨 (`Src/*/.api/*.cs`)

---

## 중요 사항

```txt
문서 작성 중 스크립트 실행 금지
└── 데이터 수집은 사전에 한 번만 수행

Uber 파일이 단일 진실 소스
└── 모든 API 검증에 사용

모든 문서화된 API는 반드시 존재해야 함
└── Uber 파일에서 확인

커밋 분석이 기능 기반 제공
└── 릴리스 노트 섹션 구성에 활용
```

---

## 요약

| 항목 | 설명 |
|------|------|
| 목표 | 컴포넌트/API 변경사항 수집 |
| 스크립트 | AnalyzeAllComponents.cs, ExtractApiChanges.cs |
| 주요 출력 | 컴포넌트 MD 파일, Uber API 파일 |
| 검증 | 파일 존재 및 내용 확인 |

---

## 다음 단계

데이터 수집 완료 후 [4.3 Phase 3: 커밋 분석](03-phase3-analysis.md)으로 진행합니다.
