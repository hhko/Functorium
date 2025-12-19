# 1.2 자동화 시스템 개요

> 이 절에서는 Functorium 프로젝트의 릴리스 노트 자동화 시스템 전체 구조를 살펴봅니다.

---

## 시스템 아키텍처

릴리스 노트 자동화 시스템은 세 가지 핵심 구성요소로 이루어져 있습니다:

```
┌─────────────────────────────────────────────────────────────┐
│                    Claude Code                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  사용자 정의 Command (.claude/commands/)             │   │
│  │  ├── release-note.md (릴리스 노트 생성)             │   │
│  │  └── commit.md (커밋 규칙)                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  5-Phase 워크플로우 (.release-notes/scripts/docs/)  │   │
│  │  ├── phase1-setup.md (환경 검증)                    │   │
│  │  ├── phase2-collection.md (데이터 수집)             │   │
│  │  ├── phase3-analysis.md (커밋 분석)                 │   │
│  │  ├── phase4-writing.md (문서 작성)                  │   │
│  │  └── phase5-validation.md (검증)                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  C# 스크립트 (.release-notes/scripts/)              │   │
│  │  ├── AnalyzeAllComponents.cs (컴포넌트 분석)        │   │
│  │  ├── ExtractApiChanges.cs (API 추출)                │   │
│  │  ├── ApiGenerator.cs (Public API 생성)              │   │
│  │  └── SummarizeSlowestTests.cs (테스트 요약)         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 구성요소 1: Claude Code 사용자 정의 Command

Claude Code는 AI 기반 CLI 도구입니다. 사용자 정의 Command를 통해 복잡한 작업을 단일 명령어로 실행할 수 있습니다.

### release-note.md

이 파일은 릴리스 노트 생성의 **마스터 문서**입니다:

```markdown
# release-note.md의 역할

1. 버전 파라미터 검증 (/release-note v1.2.0)
2. 5-Phase 워크플로우 정의
3. 각 Phase별 성공 기준 명시
4. 최종 출력 형식 정의
```

**사용 예시:**
```bash
/release-note v1.2.0        # 정규 릴리스
/release-note v1.0.0        # 첫 배포
/release-note v1.2.0-beta.1 # 프리릴리스
```

### commit.md

커밋 메시지 규칙을 정의하여 일관된 커밋 히스토리를 유지합니다:

```
커밋 타입:
├── feat: 새로운 기능
├── fix: 버그 수정
├── docs: 문서 변경
├── refactor: 코드 리팩토링
├── test: 테스트 추가/수정
└── chore: 빌드, 설정 변경
```

---

## 구성요소 2: 5-Phase 워크플로우

릴리스 노트 생성은 5단계로 진행됩니다. 각 단계는 명확한 입력/출력과 성공 기준을 가집니다.

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
│ Phase 1 │───▶│ Phase 2 │───▶│ Phase 3 │───▶│ Phase 4 │───▶│ Phase 5 │
│  환경   │    │ 데이터  │    │  커밋   │    │  문서   │    │  검증   │
│  검증   │    │  수집   │    │  분석   │    │  작성   │    │         │
└─────────┘    └─────────┘    └─────────┘    └─────────┘    └─────────┘
     │              │              │              │              │
     ▼              ▼              ▼              ▼              ▼
  Base/Target   .analysis-    phase3-*.md   RELEASE-*.md   검증 보고서
   결정         output/*.md
```

### Phase 1: 환경 검증

**목표**: 릴리스 노트 생성 전 필수 환경 확인

```
검증 항목:
├── Git 저장소 확인
├── .NET 10.x SDK 설치 확인
├── 스크립트 디렉터리 존재 확인
└── Base Branch 자동 결정
```

**Base Branch 결정 로직:**
```
IF origin/release/1.0 존재
  THEN Base = origin/release/1.0
  ELSE Base = 초기 커밋 (첫 배포)
```

### Phase 2: 데이터 수집

**목표**: C# 스크립트로 변경사항 데이터 수집

```
실행되는 스크립트:
├── AnalyzeAllComponents.cs → 컴포넌트별 변경사항
├── ExtractApiChanges.cs   → Public API 추출
└── SummarizeSlowestTests.cs → 테스트 결과 요약 (선택)
```

**출력 파일:**
```
.analysis-output/
├── Functorium.md              # 핵심 라이브러리 분석
├── Functorium.Testing.md      # 테스트 라이브러리 분석
├── analysis-summary.md        # 전체 요약
└── api-changes-build-current/
    ├── all-api-changes.txt    # Uber 파일 (모든 API)
    └── api-changes-diff.txt   # API 변경사항 (Git diff)
```

### Phase 3: 커밋 분석

**목표**: 수집된 데이터에서 릴리스 노트용 기능 추출

```
분석 항목:
├── Breaking Changes 식별
│   ├── Git Diff 분석 (권장)
│   └── 커밋 메시지 패턴 (보조)
├── Feature 커밋 분류
├── Bug Fix 커밋 분류
└── 기능별 그룹화
```

**중간 결과:**
```
.analysis-output/work/
├── phase3-commit-analysis.md  # 커밋 분류 결과
└── phase3-feature-groups.md   # 기능 그룹화 결과
```

### Phase 4: 문서 작성

**목표**: 분석 결과를 바탕으로 릴리스 노트 작성

```
작성 절차:
1. TEMPLATE.md 복사
2. Placeholder 교체 ({VERSION}, {DATE})
3. 각 섹션 채우기
4. API를 Uber 파일에서 검증
5. 주석 정리
```

**핵심 규칙:**
- 모든 기능에 **"장점:" 섹션 필수**
- API는 반드시 Uber 파일에서 검증
- 커밋 SHA를 주석으로 포함

### Phase 5: 검증

**목표**: 생성된 릴리스 노트의 품질 검증

```
검증 항목:
├── 프론트매터 존재
├── 필수 섹션 포함
├── "장점:" 섹션 포함
├── API 정확성 (Uber 파일 대조)
└── Breaking Changes 완전성
```

---

## 구성요소 3: C# 스크립트

.NET 10의 file-based app 기능을 활용하여 작성된 스크립트입니다. 프로젝트 파일(.csproj) 없이 단일 .cs 파일로 실행됩니다.

### AnalyzeAllComponents.cs

**역할**: 모든 컴포넌트의 변경사항 분석

```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

**출력 예시:**
```markdown
# Analysis for Src/Functorium

## Change Summary
37 files changed, 3167 insertions(+)

## All Commits
51533b1 refactor(observability): Observability 추상화 및 구조 개선
4683281 feat(linq): TraverseSerial 메서드 추가
...

## Categorized Commits
### Feature Commits
- 4683281 feat(linq): TraverseSerial 메서드 추가
### Breaking Changes
None found
```

### ExtractApiChanges.cs

**역할**: Public API 추출 및 Uber 파일 생성

```bash
dotnet ExtractApiChanges.cs
```

**출력 예시 (all-api-changes.txt):**
```csharp
namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static Error Create(string errorCode, string errorCurrentValue, string errorMessage) { }
        public static Error CreateFromException(string errorCode, Exception exception) { }
    }
}
```

### SummarizeSlowestTests.cs

**역할**: TRX 테스트 결과 파일에서 성능 병목 식별

```bash
dotnet SummarizeSlowestTests.cs --threshold 30
```

---

## 데이터 흐름

전체 시스템의 데이터 흐름을 시각화하면 다음과 같습니다:

```
Git Repository
     │
     ├──── Src/Functorium/*.cs (소스 코드)
     │
     └──── Git History (커밋 로그)
              │
              ▼
     ┌────────────────────┐
     │ Phase 2: 데이터 수집 │
     │                    │
     │ AnalyzeAllComponents│──▶ .analysis-output/*.md
     │ ExtractApiChanges  │──▶ all-api-changes.txt
     └────────────────────┘    api-changes-diff.txt
              │
              ▼
     ┌────────────────────┐
     │ Phase 3: 커밋 분석  │
     │                    │
     │ Breaking Changes   │──▶ phase3-commit-analysis.md
     │ Feature 그룹화     │──▶ phase3-feature-groups.md
     └────────────────────┘
              │
              ▼
     ┌────────────────────┐
     │ Phase 4: 문서 작성  │
     │                    │
     │ TEMPLATE.md 사용   │──▶ RELEASE-v1.2.0.md
     │ API 검증           │
     └────────────────────┘
              │
              ▼
     ┌────────────────────┐
     │ Phase 5: 검증      │
     │                    │
     │ 품질 체크          │──▶ phase5-validation-report.md
     └────────────────────┘
```

---

## 핵심 원칙

이 자동화 시스템은 네 가지 핵심 원칙을 따릅니다:

### 1. 정확성 우선

> **Uber 파일에 없는 API는 절대 문서화하지 않습니다.**

모든 API는 실제 코드에서 추출된 `all-api-changes.txt` 파일에서 검증해야 합니다.

### 2. 가치 전달 필수

> **모든 주요 기능에 "장점:" 섹션을 포함합니다.**

단순히 "~를 추가했습니다"가 아닌, 그 기능이 왜 유용한지 설명합니다:
- 해결하는 문제
- 개발자 생산성 향상
- 코드 품질 개선

### 3. Breaking Changes 자동 감지

> **Git Diff 분석이 커밋 메시지 패턴보다 우선합니다.**

- `.api` 폴더의 Git diff 분석 (객관적)
- 커밋 메시지 패턴은 보조 수단 (주관적)

### 4. 추적성

> **모든 기능을 실제 커밋으로 추적합니다.**

- 커밋 SHA 주석 포함
- GitHub 이슈/PR 링크 (가능한 경우)

---

## 정리

| 구성요소 | 역할 | 위치 |
|---------|------|------|
| release-note.md | 워크플로우 정의 | .claude/commands/ |
| Phase 문서 | 각 단계 상세 가이드 | .release-notes/scripts/docs/ |
| C# 스크립트 | 데이터 수집/분석 | .release-notes/scripts/ |
| TEMPLATE.md | 릴리스 노트 템플릿 | .release-notes/ |

---

## 다음 단계

- [1.3 프로젝트 구조 소개](03-project-structure.md)
