---
title: "출력 파일 형식"
---

워크플로우가 실행되면 여러 파일이 생성됩니다. 이 파일들은 각각 다른 소비자(Phase)를 위해 존재합니다. Phase 3은 컴포넌트 분석 파일을 읽어 커밋을 분류하고, Phase 4는 Uber 파일에서 API 정확성을 확인하며, Phase 5는 검증 보고서를 작성합니다. 각 출력 파일이 누구를 위한 것이고, 어떤 형식인지 이해해야 워크플로우 전체가 어떻게 연결되는지 파악할 수 있습니다.

## 출력 파일 개요

전체 출력 파일의 구조는 다음과 같습니다.

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

크게 세 영역으로 나뉩니다. `.analysis-output/` 루트에는 컴포넌트별 분석 결과와 요약이, `api-changes-build-current/`에는 API 관련 파일이, `work/`에는 Phase별 작업 파일이 위치합니다. 각 파일을 하나씩 살펴보겠습니다.

---

## 컴포넌트 분석 파일 (*.md)

컴포넌트 분석 파일은 `.analysis-output/Functorium.md`, `.analysis-output/Functorium.Testing.md` 등의 이름으로 생성됩니다. component-priority.json에 정의된 각 컴포넌트마다 하나씩 만들어집니다.

이 파일의 주요 소비자는 **Phase 3입니다.** Phase 3은 이 파일을 읽어 커밋을 기능별로 분류하고, 우선순위를 매기는 기초 데이터로 사용합니다.

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

파일 변경 요약, 전체 커밋 목록, 기여자 정보, 커밋 분류가 모두 포함되어 있어 하나의 컴포넌트에서 무엇이 어떻게 바뀌었는지 한눈에 파악할 수 있습니다.

## 분석 요약 파일

분석 요약 파일은 `.analysis-output/analysis-summary.md`에 생성됩니다. 모든 컴포넌트의 분석 결과를 하나로 모아 전체 릴리스의 규모를 보여줍니다.

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

이 파일은 릴리스 노트의 통계 요약에 활용되며, 전체 분석 결과를 빠르게 확인할 때 유용합니다.

---

## Uber API 파일

Uber API 파일은 `.analysis-output/api-changes-build-current/all-api-changes.txt`에 생성됩니다. "Uber"라는 이름이 붙은 이유는 모든 어셈블리의 Public API를 **하나의 파일에** 통합했기 때문입니다.

이 파일의 소비자는 **Phase 4와 Phase 5입니다.** Phase 4에서는 릴리스 노트에 포함할 코드 예제이 실제 API와 일치하는지 확인하고, Phase 5에서는 문서화된 모든 API의 정확성을 최종 검증합니다. 단일 진실 소스(Single Source of Truth) 역할을 합니다.

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

---

## API Diff 파일

API Diff 파일은 `.analysis-output/api-changes-build-current/api-changes-diff.txt`에 생성됩니다. 이전 버전과 현재 버전의 Public API를 Git diff 형식으로 비교한 결과입니다.

이 파일의 핵심 소비자는 **Phase 3입니다.** 삭제된 API(`-` 라인)와 추가된 API(`+` 라인)를 식별하여 Breaking Changes를 자동으로 감지합니다.

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

위 예시에서 `IErrorHandler`가 삭제되고 `IErrorDestructurer`가 추가된 것을 볼 수 있습니다. 이런 패턴이 Breaking Change로 감지됩니다.

---

## Phase 작업 파일

Phase 작업 파일은 `.analysis-output/work/` 디렉터리에 생성됩니다. 각 Phase가 자신의 작업 결과를 기록하고, 다음 Phase가 이를 입력으로 사용하는 중간 산출물입니다.

```txt
.analysis-output/work/
├── phase3-commit-analysis.md
├── phase3-feature-groups.md
├── phase4-api-references.md
├── phase5-validation-report.md
└── phase5-api-validation.md
```

**phase3-commit-analysis.md는** Phase 3의 커밋 분석 결과입니다. Breaking Changes, 기능 커밋(우선순위별), 버그 수정으로 분류합니다.

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

**phase3-feature-groups.md는** 개별 커밋을 사용자 관점의 기능 그룹으로 묶은 결과입니다. Phase 4에서 릴리스 노트의 "새로운 기능" 섹션을 작성할 때 이 그룹화를 기반으로 합니다.

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

**phase5-validation-report.md는** Phase 5의 최종 검증 보고서입니다. API 정확성, Breaking Changes 문서화 여부, Markdown 포맷, 체크리스트 완료율을 검증합니다.

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

## 최종 릴리스 노트

최종 결과물은 `.release-notes/RELEASE-v1.0.0.md`에 생성됩니다. 템플릿(TEMPLATE.md)을 기반으로 Phase 4에서 작성한 문서입니다. 자세한 구조는 [TEMPLATE.md 구조](07-template-structure.md)를 참조하세요.

---

## 파일 생성 흐름

각 파일이 어느 Phase에서 생성되는지 정리하면 다음과 같습니다.

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

Phase 1은 환경만 검증하므로 파일을 생성하지 않습니다. Phase 2에서 원시 데이터를 수집하고, Phase 3에서 분석하고, Phase 4에서 문서를 작성하고, Phase 5에서 검증합니다. 각 Phase는 이전 Phase의 출력 파일을 입력으로 사용하기 때문에, 파일 간의 의존 관계가 곧 워크플로우의 실행 순서가 됩니다.

---

이상으로 6장에서 다룬 템플릿 및 설정 파일을 모두 살펴보았습니다. TEMPLATE.md가 릴리스 노트의 표준 형식을 정의하고, component-priority.json이 분석 대상과 우선순위를 결정하며, 각 Phase의 출력 파일들이 워크플로우를 연결하는 매개체 역할을 합니다. 이 세 가지를 이해하면 자동화 시스템이 어떻게 일관된 릴리스 노트를 만들어내는지 전체 그림이 보일 것입니다.

## FAQ

### Q1: `.analysis-output/work/` 디렉터리의 중간 산출물을 Git에 커밋해야 하나요?
**A**: 선택사항입니다. 중간 산출물은 **디버깅과 감사(Audit) 목적으로** 유용하므로 커밋해두면 나중에 릴리스 노트가 어떤 데이터를 기반으로 작성되었는지 추적할 수 있습니다. 다만 매번 재생성 가능한 파일이므로, `.gitignore`에 추가하고 필요할 때만 커밋하는 것도 합리적인 선택입니다.

### Q2: Phase 간 출력 파일이 누락되면 어떻게 감지할 수 있나요?
**A**: 각 Phase는 이전 Phase의 출력 파일을 입력으로 사용하므로, **파일이 없으면 해당 Phase에서 즉시 오류가 발생합니다.** 예를 들어 Phase 3은 `.analysis-output/*.md` 파일이 없으면 "분석 파일을 찾을 수 없습니다" 오류를, Phase 4는 Uber 파일이 없으면 "all-api-changes.txt가 없습니다" 오류를 출력합니다.

### Q3: Uber 파일과 API Diff 파일의 차이는 무엇인가요?
**A**: **Uber 파일(`all-api-changes.txt`)은** 현재 브랜치의 전체 Public API를 담고 있는 스냅샷이고, **API Diff 파일(`api-changes-diff.txt`)은** 이전 버전과 현재 버전 사이에 추가/삭제/변경된 API만 보여주는 차이 정보입니다. Uber 파일은 API 정확성 검증에, Diff 파일은 Breaking Change 감지에 사용됩니다.

### Q4: `phase3-feature-groups.md`에서 기능 그룹화는 어떤 기준으로 이루어지나요?
**A**: Phase 3에서 Claude가 커밋 메시지의 스코프(`feat(errors)`, `feat(logging)`)와 변경된 파일 경로를 분석하여, **동일한 사용자 가치를 제공하는 커밋들을** 하나의 기능 그룹으로 묶습니다. 예를 들어 `ErrorCodeFactory.Create`과 `ErrorCodeFactory.CreateFromException` 관련 커밋은 "함수형 오류 처리" 그룹으로 합쳐집니다.

## 다음 단계

- [첫 번째 릴리스 노트 생성](../Part5-Hands-On/01-first-release-note.md)
