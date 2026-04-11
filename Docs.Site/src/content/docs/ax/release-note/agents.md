---
title: "Expert Agents"
description: "The release-engineer agent of the release-note plugin"
---

## release-engineer -- 릴리스 노트 생성 전문가

C# 스크립트를 실행하여 컴포넌트/API 변경을 분석하고, 릴리스 노트를 자동 생성합니다.

**전문 영역:**
- .NET C# 스크립트 실행 (`AnalyzeAllComponents.cs`, `ExtractApiChanges.cs`)
- Conventional Commits 분석 및 기능 추출
- Breaking Changes 감지 (Git Diff 분석 우선, 커밋 메시지 패턴 보조)
- API 변경사항 추적 (Uber 파일 기반 검증)
- 릴리스 노트 작성 (TEMPLATE.md 기반)
- GitHub Issue/PR 참조 추적

**핵심 원칙:**

| 원칙 | 설명 |
|------|------|
| 정확성 우선 | Uber 파일에 없는 API는 절대 문서화하지 않음 |
| 가치 전달 필수 | 모든 주요 기능에 "Why this matters" 섹션 포함 |
| Breaking Changes 자동 감지 | Git Diff 분석이 커밋 메시지 패턴보다 우선 |
| 추적성 | 모든 기능을 커밋 SHA로 추적 |

### 예시 1: 첫 릴리스 노트 생성

```text
사용자: v1.0.0 릴리스 노트를 생성해줘.
에이전트: 5단계로 진행합니다:
         Phase 1: 환경 검증 -- .NET 10.x, Git, Base Branch 결정
         Phase 2: 데이터 수집 -- AnalyzeAllComponents.cs + ExtractApiChanges.cs
         Phase 3: 커밋 분석 -- Breaking Changes, Feature/Fix 분류
         Phase 4: 릴리스 노트 작성 -- TEMPLATE.md 기반, API 검증
         Phase 5: 검증 -- 프론트매터, Why this matters, 크기 제한 확인
```

### 예시 2: Breaking Changes 감지

```text
사용자: 이번 릴리스에 Breaking Changes가 있을까?
에이전트: api-changes-diff.txt를 분석합니다:
         - 삭제된 public API: 2개
           1. IRepository.GetAll() -> 제거 (Specification 패턴으로 대체)
           2. FinResponse.IsSuccess -> IsSucc로 이름 변경
         각 Breaking Change에 마이그레이션 가이드를 포함합니다.
```
