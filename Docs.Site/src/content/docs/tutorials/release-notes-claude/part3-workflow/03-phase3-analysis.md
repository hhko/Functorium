---
title: "Commit Analysis and Feature Extraction"
---

원시 데이터만으로는 릴리스 노트를 쓸 수 없습니다. 수십 개의 커밋과 API 변경 목록은 "무엇이 바뀌었는가"를 알려주지만, "사용자에게 어떤 의미인가"는 알려주지 않습니다. Phase 3에서는 수집된 데이터를 분석하여 릴리스 노트에 담을 기능을 추출하고, Breaking Changes를 식별합니다.

## 입력 파일

Phase 2에서 생성된 다음 파일들을 분석합니다.

```txt
.analysis-output/
├── Functorium.md                           # 핵심 라이브러리 커밋
├── Functorium.Testing.md                   # 테스트 유틸리티 커밋
└── api-changes-build-current/
    └── api-changes-diff.txt                # API 변경 Git Diff
```

## 커밋 분석 방법

분석은 네 단계로 진행됩니다.

### 1단계: 커밋 메시지 읽기

컴포넌트 분석 파일에서 커밋 목록을 확인합니다.

```markdown
# 분석 파일의 예시:
6b5ef99 Add ErrorCodeFactory for structured error creation (#123)
853c918 Rename IErrorHandler to IErrorDestructurer (#124)
c5e604f Add OpenTelemetry integration support (#125)
4ee28c2 Improve Serilog destructuring for LanguageExt errors (#126)
```

### 2단계: GitHub 이슈/PR 조회

커밋 메시지에 GitHub 참조(`#123`, `(#124)`)가 있으면 **반드시 조회합니다.** PR과 이슈에는 커밋 메시지만으로는 알 수 없는 맥락이 담겨 있습니다. 사용자가 겪은 구체적인 문제, 변경의 동기, 관련된 다른 이슈 등을 파악할 수 있어 릴리스 노트의 "Why this matters" 섹션을 작성할 때 핵심 자료가 됩니다.

```txt
PR #106 설명:
- 제목: "Error handling improvements"
- 수정 이슈: #101 및 #102
- 구현: 더 나은 오류 처리 및 검증

이슈 #101: "ErrorCodeFactory doesn't support nested errors"
- 사용자 문제: 중첩 오류 생성 시 정보 손실
- 불편점: "내부 오류 정보가 로그에 표시되지 않음"

이슈 #102: "Serilog destructuring loses error context"
- 사용자 문제: Serilog 로깅 시 LanguageExt 오류 컨텍스트 손실
```

### 3단계: 기능 유형 식별

커밋 메시지의 패턴으로 기능 유형을 식별합니다.

| 패턴 | 의미 | 우선순위 |
|------|------|:--------:|
| `Add` | 새 기능 또는 API | 높음 |
| `Rename` | Breaking Change 또는 API 업데이트 | 높음 |
| `Improve/Enhance` | 기존 기능 개선 | 중간 |
| `Fix` | 버그 수정 | 낮음 |
| `Support for` | 새 플랫폼/기술 통합 | 높음 |

### 4단계: 사용자 영향 추출

각 중요한 커밋에 대해 네 가지 질문에 답합니다. 이것이 가능하게 하는 기능은 무엇인가(새 기능), 개발자에게 무엇이 변경되는가(API 영향), 어떤 문제를 해결하는가(유스케이스), Breaking Change인가(마이그레이션 필요 여부). 이 질문들에 대한 답이 릴리스 노트의 각 섹션을 구성하는 재료가 됩니다.

## Breaking Changes 감지

Breaking Changes는 **두 가지 방법으로** 식별하며, 두 방법을 함께 사용하여 누락을 최소화합니다.

### 방법 1: 커밋 메시지 패턴 (개발자 의도)

개발자가 커밋 메시지에 명시적으로 표시한 Breaking Changes를 찾습니다. `breaking`, `BREAKING` 문자열이 포함되거나 타입 뒤에 `!`가 붙는 패턴(예: `feat!:`, `fix!:`)을 검색합니다.

```txt
feat!: Change IErrorHandler to IErrorDestructurer
fix!: Remove deprecated Create method
BREAKING: Update authentication flow
```

이 방법은 개발자의 의도를 직접 반영하지만, 표시를 누락하면 감지하지 못하는 한계가 있습니다.

### 방법 2: Git Diff 분석 (자동 감지, 권장)

`.api` 폴더의 Git diff를 분석하여 실제 API 변경사항을 **객관적으로** 감지합니다. 커밋 메시지에 표시하지 않았더라도, 실제로 삭제되거나 변경된 API를 놓치지 않습니다.

**Git Diff 파일 위치:**
```txt
.analysis-output/api-changes-build-current/api-changes-diff.txt
```

다음과 같은 패턴으로 Breaking Change 여부를 판단합니다.

| Git Diff 패턴 | 의미 | Breaking? |
|--------------|------|:---------:|
| `- public class Foo` | 클래스 삭제 | Yes |
| `- public interface IFoo` | 인터페이스 삭제 | Yes |
| `- public void Method()` | 메서드 삭제 | Yes |
| `- Method(int x)` → `+ Method(string x)` | 타입 변경 | Yes |
| `+ public class Bar` | 새 클래스 추가 | No |
| `+ public void NewMethod()` | 새 메서드 추가 | No |

실제 Git Diff 예시를 살펴보겠습니다.

```diff
diff --git a/Src/Functorium/.api/Functorium.cs b/Src/Functorium/.api/Functorium.cs
@@ -34,11 +34,11 @@ namespace Functorium.Abstractions.Errors
 {
-    public class ErrorCodeExceptionalDestructurer : IErrorDestructurer
+    public class ErrorCodeExceptionalDestructurer : IErrorProcessor
     {
         public ErrorCodeExceptionalDestructurer() { }
-        public bool CanHandle(Error error) { }
+        public bool CanProcess(Error error) { }
     }
 }
```

위 예시에서는 두 가지 Breaking Changes가 감지됩니다. 인터페이스 이름이 `IErrorDestructurer`에서 `IErrorProcessor`로 변경되었고, 메서드 이름이 `CanHandle`에서 `CanProcess`로 변경되었습니다.

두 방법을 비교하면, Git Diff 분석이 더 정확하고 신뢰할 수 있습니다. 커밋 메시지 패턴은 개발자 의도에 의존하고 표시 누락이 가능한 반면, Git Diff 분석은 실제 코드 변경을 감지하여 객관적 증거를 제공하고 모든 변경을 커버합니다.

## 커밋 우선순위

모든 커밋이 릴리스 노트에 포함될 필요는 없습니다. 사용자에게 미치는 영향도에 따라 우선순위를 매깁니다.

**높은 우선순위로** 반드시 포함해야 하는 커밋은 새로운 타입(Add Type, Add Factory), 새로운 통합 지원(Add support, Support for), Breaking API 변경(Rename, Remove, Change), 주요 기능(Add method, Implement), 보안 개선(security, validation) 등입니다. 이것들은 개발자의 코드나 워크플로우에 직접적인 영향을 미칩니다.

**중간 우선순위로** 포함을 고려할 커밋은 성능 개선(Improve performance, Optimize), 향상된 구성(Add configuration, Support options), 더 나은 오류 처리(Improve error, Add validation), 개발자 경험 개선(Enhance, Better) 등입니다. 기존 기능을 개선하는 변경으로, 사용자에게 알릴 가치가 있지만 필수는 아닙니다.

**낮은 우선순위로** 보통 건너뛰는 커밋은 중요하지 않은 버그 수정, 사용자 영향 없는 내부 리팩토링, 문서 업데이트, 테스트 개선, 코드 정리 등입니다. 릴리스 노트에 포함하면 오히려 중요한 변경이 묻힐 수 있습니다.

## 기능 그룹화

### 관련 커밋 통합

여러 커밋이 하나의 기능을 구성하는 경우가 많습니다. 예를 들어 다음 세 커밋은 각각 다른 작업이지만, 함께 "향상된 오류 로깅 시스템"이라는 하나의 기능을 이룹니다.

```txt
853c918 Rename IErrorHandler to IErrorDestructurer
d4eacc6 Improve error destructuring output formatting
a1b2c3d Add structured logging for all error types
```

이렇게 통합하면, 릴리스 노트에서 개별 커밋을 나열하는 대신 사용자가 이해할 수 있는 기능 단위로 설명할 수 있습니다. 구조화된 디스트럭처링, 더 나은 오류 메시지, 향상된 추적이라는 세 가지 측면으로 하나의 기능을 설명하는 것이 훨씬 효과적입니다.

### 멀티 컴포넌트 기능

하나의 기능이 여러 컴포넌트에 걸쳐 구현되는 경우도 있습니다. Functorium에서 핵심 오류 팩토리가 변경되고, Functorium.Testing에서 테스트 유틸리티가 업데이트되었다면, 이를 "오류 처리 시스템 개선"이라는 하나의 기능으로 통합합니다.

```txt
Functorium.md:
  - 핵심 오류 팩토리 변경

Functorium.Testing.md:
  - 테스트 유틸리티 업데이트

→ 통합: "오류 처리 시스템 개선"
```

## 중간 결과 저장

Phase 3의 분석 결과는 다음 파일들에 저장됩니다.

```txt
.release-notes/scripts/.analysis-output/work/
├── phase3-commit-analysis.md     # 커밋 분류 및 우선순위
├── phase3-feature-groups.md      # 기능 그룹화 결과
└── phase3-api-mapping.md         # API와 커밋 매핑
```

### phase3-commit-analysis.md 형식

````markdown
# Phase 3: 커밋 분석 결과

## Breaking Changes
- 없음 (또는 목록)

## Feature Commits (높은 우선순위)
- [cda0a33] feat(functorium): 핵심 라이브러리 패키지 참조 추가
- [1790c73] feat(observability): OpenTelemetry 및 Serilog 통합

## Feature Commits (중간 우선순위)
- [4727bf9] feat(api): Public API 파일 추가

## Bug Fixes
- [a8ec763] fix(build): NuGet 패키지 아이콘 경로 수정
````

### phase3-feature-groups.md 형식

````markdown
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

**사용자 가치:**
분산 추적, 메트릭, 로깅 통합 지원
````

## 콘솔 출력 형식

### 분석 완료

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 3: 커밋 분석 및 기능 추출 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

분석 결과:
  Breaking Changes: 0개
  Feature Commits: 6개 (높음: 4개, 중간: 2개)
  Bug Fixes: 1개
  기능 그룹: 8개

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
  .analysis-output/work/phase3-commit-analysis.md
  .analysis-output/work/phase3-feature-groups.md
  .analysis-output/work/phase3-api-mapping.md
```

## 검증 단계

분석이 완료되면 결과의 품질을 확인합니다.

먼저, 커밋 우선순위가 적절한지 검토합니다. 새 타입, 통합 지원, Breaking Changes, 주요 기능은 높은 우선순위에, 성능이나 구성 관련 변경은 중간 우선순위에, 문서나 리팩토링은 낮은 우선순위에 분류되어야 합니다.

다음으로, Uber 파일로 모든 API 참조를 확인합니다.

```bash
grep "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

마지막으로, Breaking Changes가 빠짐없이 포함되었는지 확인합니다. `api-changes-diff.txt`의 모든 삭제/변경 API가 문서화되어야 하고, 커밋 메시지 패턴(`!:`, `breaking`)으로 표시된 커밋도 포함되어야 합니다.

## FAQ

### Q1: 여러 커밋이 하나의 기능으로 그룹화되는 기준은 무엇인가요?
**A**: 관련된 API나 모듈을 다루는 커밋, 동일한 GitHub 이슈/PR에 연결된 커밋, 유사한 주제(예: 오류 처리, 로깅)를 다루는 커밋이 하나의 기능 그룹으로 통합됩니다. 멀티 컴포넌트 기능(예: Functorium에서 핵심 변경, Functorium.Testing에서 테스트 추가)도 하나의 그룹으로 묶입니다.

### Q2: GitHub 이슈/PR 조회가 "반드시" 필요한 이유는 무엇인가요?
**A**: 커밋 메시지만으로는 **변경의 동기와 맥락을** 파악하기 어렵습니다. PR과 이슈에는 사용자가 겪은 구체적인 문제, 대안 검토 내용, 관련 이슈 등이 담겨 있어 "Why this matters" 섹션을 작성할 때 핵심 자료가 됩니다. 이 정보 없이는 단순한 기능 나열에 그치게 됩니다.

### Q3: 중간 결과를 파일로 저장하는 이유는 무엇인가요?
**A**: **추적성과 디버깅을** 위한 설계입니다. `phase3-commit-analysis.md`와 `phase3-feature-groups.md`를 파일로 저장하면, Phase 4에서 이 파일들을 입력으로 사용하고, 문제가 발생했을 때 Phase 3의 분석 결과를 직접 확인하여 원인을 파악할 수 있습니다.

분석이 완료되면, 추출된 기능 그룹을 바탕으로 실제 문서를 작성하는 [Phase 4: 릴리스 노트 작성](04-phase4-writing.md)으로 진행합니다.
