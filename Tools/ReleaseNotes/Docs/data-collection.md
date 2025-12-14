# 릴리스 노트를 위한 데이터 수집

이 가이드는 릴리스 노트 생성 전 필요한 전체 데이터 수집 프로세스를 다룹니다. 모든 스크립트는 `Tools/ReleaseNotes` 디렉터리에서 실행해야 합니다.

## 목표: 포괄적인 데이터 기반 구축

릴리스 버전 간의 컴포넌트 변경사항과 API 수정사항을 분석하여 정확하고 포괄적인 릴리스 노트 생성에 필요한 모든 데이터를 수집합니다.

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

#### 생성되는 결과물:

**개별 컴포넌트 분석 파일** (`analysis-output/*.md`)
- 각 Functorium 컴포넌트별 파일 생성
- 각 파일 포함 내용:
  - **전체 변경 통계**: 추가/수정/삭제된 파일 수
  - **완전한 커밋 히스토리**: 해당 컴포넌트의 릴리스 간 모든 커밋
  - **주요 기여자**: 가장 많은 변경을 한 사람
  - **분류된 커밋**: 기능, 버그 수정, 브레이킹 체인지

**분석 요약** (`analysis-output/analysis-summary.md`)
- 모든 컴포넌트 변경사항의 고수준 개요
- 각 컴포넌트의 변경 파일 수
- 생성된 분석 파일 목록

#### 실제 출력 예시:
```
analysis-output/
├── analysis-summary.md          # 전체 요약
├── Functorium.md                # Src/Functorium 분석 (30 files)
├── Functorium.Testing.md        # Src/Functorium.Testing 분석 (17 files)
└── Docs.md                      # Docs 분석 (38 files)
```

### 2단계: API 변경사항 추출

```bash
dotnet ExtractApiChanges.cs
```

#### 생성되는 결과물:

**Uber API 파일** (`analysis-output/api-changes-build-current/all-api-changes.txt`)
- 모든 API 참조의 **단일 진실 소스**
- 현재 빌드의 완전한 API 정의
- 정확한 매개변수 이름과 타입을 포함한 메서드 시그니처
- **코드 샘플 검증에 중요** - 이 파일에 없으면 문서화하지 않습니다

**API 변경 요약** (`analysis-output/api-changes-build-current/api-changes-summary.md`)
- 생성된 API 파일 목록
- 사용된 도구 버전 정보
- 각 어셈블리별 API 파일 경로

**개별 API 파일** (`Src/*/.api/*.cs`)
- 각 어셈블리의 Public API 정의
- C# 소스 코드 형식
- Git으로 추적되어 API 변경 이력 관리 가능

#### 실제 출력 예시:
```
analysis-output/api-changes-build-current/
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

#### 컴포넌트 분석 검증:
```bash
# 컴포넌트 파일 수 확인
ls -1 analysis-output/*.md | wc -l

# 주요 컴포넌트 존재 확인
ls analysis-output/Functorium*.md

# 분석 요약 확인
cat analysis-output/analysis-summary.md
```

#### API 변경사항 검증:
```bash
# Uber 파일 존재 및 크기 확인
wc -l analysis-output/api-changes-build-current/all-api-changes.txt

# 주요 API 확인 (예시)
grep -c "ErrorCodeFactory" analysis-output/api-changes-build-current/all-api-changes.txt

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

### 주요 커밋 패턴 (Conventional Commits):

| 타입 | 설명 |
|------|------|
| `feat` | 새로운 기능 추가 |
| `fix` | 버그 수정 |
| `docs` | 문서 변경 |
| `refactor` | 리팩터링 (기능/버그 수정 아님) |
| `perf` | 성능 개선 |
| `test` | 테스트 추가/수정 |
| `build` | 빌드 시스템/의존성 변경 |
| `chore` | 기타 변경 |
| `!` 또는 `BREAKING CHANGE:` | 브레이킹 체인지 |

- **GitHub 참조** (`#12345`) → 추가 컨텍스트 확인

## 다음 단계

데이터 수집 완료 후:

1. **[commit-analysis.md](commit-analysis.md) 검토** - 기능 분석 방법 학습
2. **[api-documentation.md](api-documentation.md) 검토** - API 검증 프로세스 이해
3. **기능 추출 시작** - 수집된 데이터 사용
4. **릴리스 노트 생성** - [writing-guidelines.md](writing-guidelines.md) 따르기

## 중요 사항

- **문서 작성 중 스크립트 실행 금지** - 데이터 수집은 사전에 한 번만 수행
- **Uber 파일이 단일 진실 소스** - API 검증용
- **모든 문서화된 API는 반드시 존재해야 함** - Uber 파일에서 확인
- **커밋 분석이 기능 기반 제공** - 릴리스 노트용
- **GitHub 이슈 조회로 커밋 이해 향상** - 추가 컨텍스트 제공

## 데이터 수집 체크리스트

- [ ] 컴포넌트 분석 완료 (`dotnet AnalyzeAllComponents.cs`)
- [ ] API 변경사항 추출 완료 (`dotnet ExtractApiChanges.cs`)
- [ ] 컴포넌트 파일 생성됨 (`analysis-output/*.md`)
- [ ] 분석 요약 생성됨 (`analysis-output/analysis-summary.md`)
- [ ] Uber API 파일 생성됨 (`all-api-changes.txt`)
- [ ] API 요약 생성됨 (`api-changes-summary.md`)
- [ ] 개별 API 파일 생성됨 (`Src/*/.api/*.cs`)
- [ ] 기능 분석 및 문서화 진행 준비 완료
