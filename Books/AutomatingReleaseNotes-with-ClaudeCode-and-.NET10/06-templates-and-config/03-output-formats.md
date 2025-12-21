# 6.3 출력 파일 형식

> 이 절에서는 릴리스 노트 자동화 시스템이 생성하는 출력 파일들의 형식을 알아봅니다.

---

## 출력 파일 개요

```txt
.release-notes/
├── RELEASE-v1.2.0.md                    # 최종 릴리스 노트
└── scripts/
    └── .analysis-output/
        ├── Functorium.md                # 컴포넌트 분석
        ├── Functorium.Testing.md
        ├── analysis-summary.md          # 분석 요약
        ├── api-changes-build-current/
        │   ├── all-api-changes.txt      # Uber 파일
        │   ├── api-changes-summary.md
        │   └── api-changes-diff.txt     # Git Diff
        └── work/
            ├── phase3-commit-analysis.md
            ├── phase3-feature-groups.md
            ├── phase4-api-references.md
            └── phase5-validation-report.md
```

---

## 컴포넌트 분석 파일 (*.md)

### 위치

```txt
.analysis-output/Functorium.md
.analysis-output/Functorium.Testing.md
```

### 형식

```markdown
# Analysis for Src/Functorium

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Change Summary

 Src/Functorium/Abstractions/Errors/ErrorCodeFactory.cs | 45 +++++
 Src/Functorium/Applications/ElapsedTimeCalculator.cs   | 32 +++
 37 files changed, 1542 insertions(+), 89 deletions(-)

## All Commits

6b5ef99 feat(errors): Add ErrorCodeFactory
853c918 feat(logging): Add Serilog integration
c5e604f fix(build): Fix NuGet package icon path
d4eacc6 docs: Update README

## Top Contributors

1. developer@example.com (15 commits)
2. contributor@example.com (4 commits)

## Categorized Commits

### Feature Commits

6b5ef99 feat(errors): Add ErrorCodeFactory
853c918 feat(logging): Add Serilog integration

### Bug Fixes

c5e604f fix(build): Fix NuGet package icon path

### Breaking Changes

(none)
```

### 용도

- **Phase 3**에서 커밋 분석에 사용
- 기능별 분류의 기초 데이터

---

## 분석 요약 파일

### 위치

```txt
.analysis-output/analysis-summary.md
```

### 형식

```markdown
# Analysis Summary

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Components Analyzed

| Component | Files | Commits | Output |
|-----------|-------|---------|--------|
| Functorium | 37 | 19 | Functorium.md |
| Functorium.Testing | 18 | 13 | Functorium.Testing.md |
| Docs | 38 | 37 | Docs.md |

## Total Statistics

- **Total Components**: 3
- **Total Files Changed**: 93
- **Total Commits**: 69

## Output Files

- `.analysis-output/Functorium.md`
- `.analysis-output/Functorium.Testing.md`
- `.analysis-output/Docs.md`
```

### 용도

- 전체 분석 결과 한눈에 확인
- 릴리스 노트 통계 요약에 사용

---

## Uber API 파일

### 위치

```txt
.analysis-output/api-changes-build-current/all-api-changes.txt
```

### 형식

```csharp
// All API Changes - Uber File
// Generated: 2025-12-19 10:30:00

// ═══════════════════════════════════════════
// Assembly: Functorium
// ═══════════════════════════════════════════

namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static LanguageExt.Common.Error Create(
            string errorCode,
            string errorCurrentValue,
            string errorMessage) { }
        public static LanguageExt.Common.Error CreateFromException(
            string errorCode,
            System.Exception exception) { }
    }
}

namespace Functorium.Abstractions.Registrations
{
    public static class OpenTelemetryRegistration
    {
        public static OpenTelemetryBuilder RegisterObservability(
            this IServiceCollection services,
            IConfiguration configuration) { }
    }
}

// ═══════════════════════════════════════════
// Assembly: Functorium.Testing
// ═══════════════════════════════════════════

namespace Functorium.Testing.Arrangements.Hosting
{
    public class HostTestFixture<TProgram> where TProgram : class
    {
        public HttpClient Client { get; }
        public IServiceProvider Services { get; }
    }
}
```

### 용도

- **단일 진실 소스 (Single Source of Truth)**
- Phase 4에서 코드 샘플 검증
- Phase 5에서 API 정확성 검증

---

## API Diff 파일

### 위치

```txt
.analysis-output/api-changes-build-current/api-changes-diff.txt
```

### 형식

```diff
diff --git a/Src/Functorium/.api/Functorium.cs b/Src/Functorium/.api/Functorium.cs
index abc1234..def5678 100644
--- a/Src/Functorium/.api/Functorium.cs
+++ b/Src/Functorium/.api/Functorium.cs
@@ -10,6 +10,10 @@ namespace Functorium.Abstractions.Errors
     public static class ErrorCodeFactory
     {
         public static Error Create(string errorCode, string errorCurrentValue, string errorMessage) { }
+        public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
+            where T : notnull { }
+        public static Error CreateFromException(string errorCode, Exception exception) { }
     }
 }

-namespace Functorium.Abstractions.Handlers
-{
-    public interface IErrorHandler
-    {
-        void Handle(Error error);
-    }
-}
+namespace Functorium.Abstractions.DestructuringPolicies
+{
+    public interface IErrorDestructurer
+    {
+        LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory);
+    }
+}
```

### 용도

- **Breaking Changes 자동 감지**
- Phase 3에서 API 변경 분석
- 삭제(-) 및 추가(+) API 식별

---

## Phase 작업 파일

### 위치

```txt
.analysis-output/work/
├── phase3-commit-analysis.md
├── phase3-feature-groups.md
├── phase4-api-references.md
├── phase5-validation-report.md
└── phase5-api-validation.md
```

### phase3-commit-analysis.md

```markdown
# Phase 3: 커밋 분석 결과

## Breaking Changes
- IErrorHandler → IErrorDestructurer 이름 변경

## Feature Commits (높은 우선순위)
- [6b5ef99] feat(errors): Add ErrorCodeFactory
- [853c918] feat(logging): Add Serilog integration

## Feature Commits (중간 우선순위)
- [d4eacc6] feat(config): Add configuration options

## Bug Fixes
- [c5e604f] fix(build): Fix NuGet package icon path
```

### phase3-feature-groups.md

```markdown
# Phase 3: 기능 그룹화 결과

## 그룹 1: 함수형 오류 처리
**관련 커밋:**
- ErrorCodeFactory.Create 추가
- ErrorCodeFactory.CreateFromException 추가

**사용자 가치:**
구조화된 오류 생성 및 Serilog 통합

## 그룹 2: OpenTelemetry 통합
**관련 커밋:**
- OpenTelemetryRegistration 추가
- OpenTelemetryBuilder 추가

**사용자 가치:**
분산 추적, 메트릭, 로깅 통합
```

### phase5-validation-report.md

```markdown
# Phase 5: 검증 결과 보고서

## 검증 일시
2025-12-19T10:30:00

## 검증 대상
.release-notes/RELEASE-v1.0.0.md

## 검증 결과 요약
- API 정확성: 통과 (30개 타입 검증)
- Breaking Changes: 통과 (1개 문서화됨)
- Markdown 포맷: 통과
- 체크리스트: 100%

## 상세 결과

### API 검증
| API | 상태 | Uber 파일 라인 |
|-----|------|---------------|
| ErrorCodeFactory.Create | 검증됨 | 75-77 |
| ErrorCodeFactory.CreateFromException | 검증됨 | 78-79 |
| OpenTelemetryRegistration.RegisterObservability | 검증됨 | 93-95 |
```

---

## 최종 릴리스 노트

### 위치

```txt
.release-notes/RELEASE-v1.0.0.md
```

### 형식

템플릿(TEMPLATE.md)을 기반으로 생성된 최종 문서입니다.
자세한 구조는 [6.1 TEMPLATE.md 구조](01-template-md.md)를 참조하세요.

---

## 파일 생성 흐름

```txt
Phase 1: 환경 검증
    └─▶ (파일 생성 없음)

Phase 2: 데이터 수집
    ├─▶ .analysis-output/*.md (컴포넌트 분석)
    ├─▶ .analysis-output/analysis-summary.md
    ├─▶ .analysis-output/api-changes-build-current/all-api-changes.txt
    └─▶ .analysis-output/api-changes-build-current/api-changes-diff.txt

Phase 3: 커밋 분석
    ├─▶ .analysis-output/work/phase3-commit-analysis.md
    └─▶ .analysis-output/work/phase3-feature-groups.md

Phase 4: 문서 작성
    ├─▶ .release-notes/RELEASE-v1.0.0.md
    └─▶ .analysis-output/work/phase4-api-references.md

Phase 5: 검증
    ├─▶ .analysis-output/work/phase5-validation-report.md
    └─▶ .analysis-output/work/phase5-api-validation.md
```

---

## 요약

| 파일 | 생성 Phase | 용도 |
|------|-----------|------|
| `*.md` (컴포넌트) | Phase 2 | 커밋 분석 기초 |
| `all-api-changes.txt` | Phase 2 | API 검증 (단일 진실 소스) |
| `api-changes-diff.txt` | Phase 2 | Breaking Changes 감지 |
| `phase3-*.md` | Phase 3 | 기능 분류 결과 |
| `RELEASE-*.md` | Phase 4 | 최종 릴리스 노트 |
| `phase5-*.md` | Phase 5 | 검증 보고서 |

---

## 6장 완료

6장에서 다룬 템플릿 및 설정 파일 요약:

| 파일 | 역할 |
|------|------|
| TEMPLATE.md | 릴리스 노트 표준 형식 |
| component-priority.json | 분석 대상 및 우선순위 |
| 출력 파일들 | 각 Phase의 결과물 |

---

## 다음 단계

- [7.1 첫 번째 릴리스 노트 생성](../07-hands-on-tutorial/01-first-release-note.md)
