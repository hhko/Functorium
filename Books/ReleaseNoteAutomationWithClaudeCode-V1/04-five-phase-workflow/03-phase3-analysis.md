# 4.3 Phase 3: 커밋 분석 및 기능 추출

> Phase 3에서는 수집된 데이터를 분석하여 릴리스 노트용 기능을 추출하고 Breaking Changes를 식별합니다.

---

## 목표

수집된 데이터를 분석하여 릴리스 노트용 기능을 추출하고 Breaking Changes를 식별합니다.

---

## 입력 파일

Phase 2에서 생성된 파일들을 분석합니다:

```txt
.analysis-output/
├── Functorium.md                           # 핵심 라이브러리 커밋
├── Functorium.Testing.md                   # 테스트 유틸리티 커밋
└── api-changes-build-current/
    └── api-changes-diff.txt                # API 변경 Git Diff
```

---

## 커밋 분석 방법

### 1단계: 커밋 메시지 읽기

컴포넌트 분석 파일에서 커밋 목록을 확인합니다:

```markdown
# 분석 파일의 예시:
6b5ef99 Add ErrorCodeFactory for structured error creation (#123)
853c918 Rename IErrorHandler to IErrorDestructurer (#124)
c5e604f Add OpenTelemetry integration support (#125)
4ee28c2 Improve Serilog destructuring for LanguageExt errors (#126)
```

### 2단계: GitHub 이슈/PR 조회

커밋 메시지에 GitHub 참조 (`#123`, `(#124)`)가 있으면 **반드시 조회**합니다:

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

커밋 메시지 패턴으로 기능 유형을 식별합니다:

| 패턴 | 의미 | 우선순위 |
|------|------|:--------:|
| `Add` | 새 기능 또는 API | 높음 |
| `Rename` | Breaking Change 또는 API 업데이트 | 높음 |
| `Improve/Enhance` | 기존 기능 개선 | 중간 |
| `Fix` | 버그 수정 | 낮음 |
| `Support for` | 새 플랫폼/기술 통합 | 높음 |

### 4단계: 사용자 영향 추출

각 중요한 커밋에 대해 다음을 결정합니다:

- **이것이 가능하게 하는 기능은?** (새 기능)
- **개발자에게 무엇이 변경되나?** (API 영향)
- **어떤 문제를 해결하나?** (유스케이스)
- **Breaking Change인가?** (마이그레이션 필요)

---

## Breaking Changes 감지

Breaking Changes는 **두 가지 방법**으로 식별합니다.

### 방법 1: 커밋 메시지 패턴 (개발자 의도)

커밋 메시지에서 다음 패턴을 찾습니다:

```txt
검색 패턴:
- breaking
- BREAKING
- !: (예: feat!:, fix!:)
```

**예시:**
```txt
feat!: Change IErrorHandler to IErrorDestructurer
fix!: Remove deprecated Create method
BREAKING: Update authentication flow
```

### 방법 2: Git Diff 분석 (자동 감지, 권장)

`.api` 폴더의 Git diff를 분석하여 실제 API 변경사항을 **객관적으로** 감지합니다.

**Git Diff 파일 위치:**
```txt
.analysis-output/api-changes-build-current/api-changes-diff.txt
```

#### 자동 감지 패턴

| Git Diff 패턴 | 의미 | Breaking? |
|--------------|------|:---------:|
| `- public class Foo` | 클래스 삭제 | Yes |
| `- public interface IFoo` | 인터페이스 삭제 | Yes |
| `- public void Method()` | 메서드 삭제 | Yes |
| `- Method(int x)` → `+ Method(string x)` | 타입 변경 | Yes |
| `+ public class Bar` | 새 클래스 추가 | No |
| `+ public void NewMethod()` | 새 메서드 추가 | No |

#### Git Diff 예시

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

위 예시에서 감지되는 Breaking Changes:
- 인터페이스 이름 변경: `IErrorDestructurer` → `IErrorProcessor`
- 메서드 이름 변경: `CanHandle` → `CanProcess`

#### Git Diff vs 커밋 메시지 비교

| 비교 | 커밋 메시지 패턴 | Git Diff 분석 |
|-----|----------------|--------------|
| 정확도 | 개발자 의도에 의존 | 실제 코드 변경 감지 |
| 신뢰성 | 표시 누락 가능 | 객관적 증거 |
| 커버리지 | 명시적 표시만 | 모든 변경 감지 |

**결론:** Git Diff 분석이 더 정확하고 신뢰할 수 있습니다.

---

## 커밋 우선순위

### 높은 우선순위 (반드시 포함)

```txt
- 새 타입 (Add.*Type, Add.*Factory)
- 새 통합 지원 (Add.*support, Support for.*)
- Breaking API 변경 (Rename.*, Remove.*, Change.*)
- 주요 기능 (Add.*method, Implement.*)
- 보안 개선 (security, validation)
```

### 중간 우선순위 (포함 고려)

```txt
- 성능 개선 (Improve.*performance, Optimize.*)
- 향상된 구성 (Add.*configuration, Support.*options)
- 더 나은 오류 처리 (Improve.*error, Add.*validation)
- 개발자 경험 (Enhance.*, Better.*)
```

### 낮은 우선순위 (보통 건너뜀)

```txt
- 버그 수정 (Fix.*) - 중요하지 않으면
- 사용자 영향 없는 내부 리팩토링
- 문서 업데이트
- 테스트 개선
- 코드 정리
```

---

## 기능 그룹화

### 관련 커밋 통합

여러 커밋이 단일 기능에 기여할 때 통합합니다:

**여러 관련 커밋:**
```txt
853c918 Rename IErrorHandler to IErrorDestructurer
d4eacc6 Improve error destructuring output formatting
a1b2c3d Add structured logging for all error types
```

**통합 기능:**
```txt
향상된 오류 로깅 시스템
├── 구조화된 디스트럭처링
├── 더 나은 오류 메시지
└── 향상된 추적
```

### 멀티 컴포넌트 기능

컴포넌트 간 관련 커밋을 찾습니다:

```txt
Functorium.md:
  - 핵심 오류 팩토리 변경

Functorium.Testing.md:
  - 테스트 유틸리티 업데이트

→ 통합: "오류 처리 시스템 개선"
```

---

## 중간 결과 저장

Phase 3의 분석 결과를 저장합니다:

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

---

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

---

## 성공 기준 체크리스트

Phase 3 완료를 위해 다음을 모두 확인하세요:

- [ ] Breaking Changes 식별됨
- [ ] Feature Commits 분류됨 (높음/중간/낮음)
- [ ] 기능 그룹화 완료됨
- [ ] 중간 결과 파일 저장됨

---

## 검증 단계

분석 완료 후 다음을 확인합니다:

### 1. 포함할 커밋 우선순위 확인

- 높음: 새 타입, 통합 지원, Breaking Changes, 주요 기능
- 중간: 성능, 구성, 오류 처리, 개발자 경험
- 낮음: 버그 수정 (중요하지 않으면), 리팩토링, 문서

### 2. API 검증

Uber 파일로 모든 API 참조 확인:
```bash
grep "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 3. Breaking Changes 검증

- `api-changes-diff.txt`의 모든 삭제/변경 API 문서화 확인
- 커밋 메시지 패턴으로 표시된 커밋도 포함 확인

---

## 요약

| 항목 | 설명 |
|------|------|
| 목표 | 기능 추출 및 Breaking Changes 식별 |
| 입력 | 컴포넌트 MD 파일, API Diff |
| 분석 | 커밋 분류, 기능 그룹화 |
| 출력 | phase3-*.md 파일 |

---

## 다음 단계

커밋 분석 완료 후 [4.4 Phase 4: 릴리스 노트 작성](04-phase4-writing.md)으로 진행합니다.
