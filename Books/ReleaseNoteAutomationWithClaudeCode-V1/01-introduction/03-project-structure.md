# 1.3 프로젝트 구조 소개

> 이 절에서는 릴리스 노트 자동화 시스템의 전체 폴더 구조와 각 파일의 역할을 살펴봅니다.

---

## 전체 폴더 구조

```
Functorium/
├── .claude/
│   └── commands/
│       ├── release-note.md          # 릴리스 노트 생성 Command
│       └── commit.md                # 커밋 규칙 Command
│
├── .release-notes/
│   ├── TEMPLATE.md                  # 릴리스 노트 템플릿
│   ├── RELEASE-v1.0.0.md           # 생성된 릴리스 노트
│   ├── RELEASE-v1.0.0-alpha.1.md   # 프리릴리스 노트
│   │
│   └── scripts/
│       ├── AnalyzeAllComponents.cs  # 컴포넌트 분석 스크립트
│       ├── ExtractApiChanges.cs     # API 추출 스크립트
│       ├── ApiGenerator.cs          # Public API 생성기
│       ├── AnalyzeFolder.cs         # 폴더 분석 (보조)
│       ├── SummarizeSlowestTests.cs # 테스트 요약
│       │
│       ├── config/
│       │   └── component-priority.json  # 분석 대상 설정
│       │
│       ├── docs/
│       │   ├── README.md            # 전체 프로세스 개요
│       │   ├── phase1-setup.md      # Phase 1 상세 가이드
│       │   ├── phase2-collection.md # Phase 2 상세 가이드
│       │   ├── phase3-analysis.md   # Phase 3 상세 가이드
│       │   ├── phase4-writing.md    # Phase 4 상세 가이드
│       │   └── phase5-validation.md # Phase 5 상세 가이드
│       │
│       └── .analysis-output/        # 분석 결과 (자동 생성)
│           ├── Functorium.md
│           ├── Functorium.Testing.md
│           ├── analysis-summary.md
│           ├── api-changes-build-current/
│           │   ├── all-api-changes.txt
│           │   └── api-changes-diff.txt
│           └── work/
│               ├── phase3-*.md
│               ├── phase4-*.md
│               └── phase5-*.md
│
└── Src/
    ├── Functorium/                  # 핵심 라이브러리
    │   ├── .api/
    │   │   └── Functorium.cs        # Public API 정의
    │   └── *.cs
    │
    └── Functorium.Testing/          # 테스트 라이브러리
        ├── .api/
        │   └── Functorium.Testing.cs
        └── *.cs
```

---

## .claude/commands/ 폴더

Claude Code의 사용자 정의 Command가 저장되는 폴더입니다.

### release-note.md

**역할**: 릴리스 노트 생성의 마스터 문서

**구조:**
```yaml
---
title: RELEASE-NOTES
description: 릴리스 노트를 자동으로 생성합니다
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"
---
```

**핵심 내용:**
- 버전 파라미터 검증 규칙
- 5-Phase 워크플로우 정의
- 각 Phase별 성공 기준
- 최종 출력 형식

### commit.md

**역할**: 커밋 메시지 규칙 정의

**구조:**
```yaml
---
title: COMMIT
description: Conventional Commits 규격에 따라 변경사항을 커밋합니다
argument-hint: "[topic]을 전달하면 해당 topic 관련 파일만 선별하여 커밋합니다"
---
```

**핵심 내용:**
- Conventional Commits 형식
- 커밋 타입 (feat, fix, docs 등)
- Topic 파라미터 활용법

---

## .release-notes/ 폴더

릴리스 노트 관련 모든 파일이 저장되는 폴더입니다.

### TEMPLATE.md

**역할**: 릴리스 노트의 기본 구조 제공

**구조:**
```markdown
---
title: Functorium {VERSION} 새로운 기능
description: Functorium {VERSION}의 새로운 기능을 알아봅니다.
date: {DATE}
---

# Functorium Release {VERSION}

## 개요
## Breaking Changes
## 새로운 기능
## 버그 수정
## API 변경사항
## 설치
```

**Placeholder:**
| Placeholder | 설명 | 예시 |
|-------------|------|------|
| `{VERSION}` | 버전 번호 | v1.2.0 |
| `{DATE}` | 작성 날짜 | 2025-12-20 |

### RELEASE-*.md

생성된 릴리스 노트 파일입니다. 버전별로 별도 파일로 관리됩니다:
- `RELEASE-v1.0.0.md` - 정규 릴리스
- `RELEASE-v1.0.0-alpha.1.md` - 알파 릴리스
- `RELEASE-v1.0.0-beta.1.md` - 베타 릴리스

---

## .release-notes/scripts/ 폴더

C# 스크립트와 설정 파일이 저장되는 폴더입니다.

### C# 스크립트 파일

| 파일 | 역할 | 사용 Phase |
|------|------|-----------|
| AnalyzeAllComponents.cs | 모든 컴포넌트 변경사항 분석 | Phase 2 |
| ExtractApiChanges.cs | Public API 추출 및 Uber 파일 생성 | Phase 2 |
| ApiGenerator.cs | DLL에서 Public API 추출 | Phase 2 (보조) |
| AnalyzeFolder.cs | 단일 폴더 분석 | 독립 실행 |
| SummarizeSlowestTests.cs | TRX 테스트 결과 요약 | Phase 2 (선택) |

### config/ 폴더

**component-priority.json**

분석 대상 컴포넌트를 정의합니다:

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs",
    ".release-notes/scripts"
  ]
}
```

- 배열 순서대로 분석 우선순위 결정
- Glob 패턴 지원 (`"Src/*/"`)

### docs/ 폴더

각 Phase의 상세 가이드 문서입니다:

| 파일 | 내용 |
|------|------|
| README.md | 전체 프로세스 개요 |
| phase1-setup.md | 환경 검증 및 준비 |
| phase2-collection.md | 데이터 수집 |
| phase3-analysis.md | 커밋 분석 및 기능 추출 |
| phase4-writing.md | 릴리스 노트 작성 |
| phase5-validation.md | 검증 |

### .analysis-output/ 폴더 (자동 생성)

스크립트 실행 결과가 저장됩니다:

```
.analysis-output/
├── Functorium.md              # Src/Functorium 분석 결과
├── Functorium.Testing.md      # Src/Functorium.Testing 분석 결과
├── Docs.md                    # Docs 폴더 분석 결과
├── analysis-summary.md        # 전체 요약
│
├── api-changes-build-current/
│   ├── all-api-changes.txt    # Uber 파일 (모든 Public API)
│   ├── api-changes-diff.txt   # API 변경사항 (Git diff)
│   ├── api-changes-summary.md # API 요약
│   └── projects.txt           # 분석된 프로젝트 목록
│
└── work/                      # 중간 결과물
    ├── phase3-commit-analysis.md
    ├── phase3-feature-groups.md
    ├── phase4-draft.md
    ├── phase4-api-references.md
    ├── phase5-validation-report.md
    └── phase5-api-validation.md
```

---

## Src/ 폴더의 .api 서브폴더

각 프로젝트 폴더 내에 `.api` 서브폴더가 있습니다. 이 폴더에는 PublicApiGenerator로 생성된 Public API 정의 파일이 저장됩니다.

```
Src/Functorium/
├── .api/
│   └── Functorium.cs          # Public API 정의
├── Abstractions/
│   └── Errors/
│       └── ErrorCodeFactory.cs
└── Functorium.csproj
```

**Functorium.cs (Public API 정의):**
```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by PublicApiGenerator.
//     Assembly: Functorium
//     Generated at: 2025-12-20
// </auto-generated>
//------------------------------------------------------------------------------

namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static Error Create(string errorCode, ...) { }
        public static Error CreateFromException(string errorCode, ...) { }
    }
}
```

**용도:**
- Git으로 추적하여 API 변경 이력 관리
- Breaking Changes 자동 감지 (Git diff)
- 릴리스 노트 작성 시 API 검증

---

## 파일 간 연관성

```
사용자 입력
    │
    │  /release-note v1.2.0
    ▼
┌─────────────────────────┐
│ .claude/commands/       │
│ release-note.md         │──── 워크플로우 정의
└───────────┬─────────────┘
            │
            │  Phase 문서 참조
            ▼
┌─────────────────────────┐
│ .release-notes/scripts/ │
│ docs/*.md               │──── 각 Phase 상세 가이드
└───────────┬─────────────┘
            │
            │  C# 스크립트 실행
            ▼
┌─────────────────────────┐
│ .release-notes/scripts/ │
│ *.cs                    │──── 데이터 수집/분석
└───────────┬─────────────┘
            │
            │  분석 결과 저장
            ▼
┌─────────────────────────┐
│ .analysis-output/       │──── 분석 결과
│ ├── *.md                │
│ └── work/*.md           │
└───────────┬─────────────┘
            │
            │  템플릿 사용
            ▼
┌─────────────────────────┐
│ .release-notes/         │
│ TEMPLATE.md             │──── 릴리스 노트 템플릿
└───────────┬─────────────┘
            │
            │  최종 문서 생성
            ▼
┌─────────────────────────┐
│ .release-notes/         │
│ RELEASE-v1.2.0.md       │──── 최종 릴리스 노트
└─────────────────────────┘
```

---

## 정리

| 폴더/파일 | 역할 | 수정 빈도 |
|----------|------|----------|
| .claude/commands/ | Command 정의 | 낮음 (설정) |
| .release-notes/scripts/docs/ | Phase 가이드 | 낮음 (설정) |
| .release-notes/scripts/*.cs | C# 스크립트 | 중간 (개선) |
| .release-notes/TEMPLATE.md | 템플릿 | 낮음 (설정) |
| .release-notes/RELEASE-*.md | 릴리스 노트 | 높음 (매 릴리스) |
| .analysis-output/ | 분석 결과 | 높음 (매 실행) |
| Src/*/.api/ | Public API | 중간 (코드 변경 시) |

---

## 다음 단계

- [2.1 .NET 10 설치 및 환경 설정](../02-prerequisites/01-dotnet10-setup.md)
