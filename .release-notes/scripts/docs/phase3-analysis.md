# Phase 3: 커밋 분석 및 기능 추출

## 목표

수집된 데이터를 분석하여 릴리스 노트용 기능을 추출하고 Breaking Changes를 식별합니다.

## 입력 파일

- `.analysis-output/Functorium.md`
- `.analysis-output/Functorium.Testing.md`
- `.analysis-output/api-changes-build-current/api-changes-diff.txt`

## 컴포넌트 분석 파일 이해하기

각 컴포넌트 분석 파일 (`.analysis-output/*.md`)에는 다음이 포함됩니다:

- **변경 요약**: 파일 수 및 통계
- **전체 커밋 목록**: 릴리스 간 해당 컴포넌트의 모든 커밋
- **주요 기여자**: 변경을 수행한 사람
- **분류된 커밋**: 기능, 버그 수정, 브레이킹 체인지

## 기능 분석을 위한 커밋 분석 방법

### 1단계: 컨텍스트를 위한 커밋 메시지 읽기

```markdown
# 분석 파일의 예시:
6b5ef99 Add ErrorCodeFactory for structured error creation (#123)
853c918 Rename IErrorHandler to IErrorDestructurer (#124)
c5e604f Add OpenTelemetry integration support (#125)
4ee28c2 Improve Serilog destructuring for LanguageExt errors (#126)
```

### 2단계: 추가 컨텍스트를 위한 GitHub 이슈 및 PR 조회

**중요**: 커밋 메시지에 GitHub 이슈 참조 (예: `#123`, `(#124)`)가 포함된 경우, **항상 이슈/PR을 조회하여 깊이 있는 이해를 얻으세요**:

- **GitHub API 도구 사용**하여 PR/이슈 세부정보 가져오기
- **PR 설명 추출**하여 해결하려는 문제와 구현 세부사항 이해
- **사용자 영향** 설명 및 유스케이스 검토
- **관련 이슈 확인** (백포트 또는 중복인 경우)
- **PR 설명의 링크된 이슈** 확인 (예: "Fixes #123", "Closes #124")
- **PR 본문의 스크린샷, 예시, 상세 설명** 검토
- **이슈 설명에서 사용자 불편점 추출**하여 변경의 "이유" 이해
- **GitHub 컨텍스트 통합**하여 개발자 이해도 향상
- **최종 문서에 GitHub 링크 포함**하여 추적성 확보

#### 실제 예시: 깊이 있는 이슈 컨텍스트 추출

**커밋:** `5e8824d Various error handling improvements (#106)`

**2a단계: PR 조회:**

```markdown
# PR #106 설명:
- 제목: "Error handling improvements"
- 수정 이슈: #101 및 #102
- 구현: 더 나은 오류 처리 및 검증
```

**2b단계: 링크된 이슈 조회:**

```markdown
# 이슈 #101: "ErrorCodeFactory doesn't support nested errors"
- 사용자 문제: 중첩 오류 생성 시 정보 손실
- 불편점: "내부 오류 정보가 로그에 표시되지 않음"
- 사용자 영향: 더 나은 디버깅, 향상된 개발자 경험

# 이슈 #102: "Serilog destructuring loses error context"
- 사용자 문제: Serilog 로깅 시 LanguageExt 오류 컨텍스트 손실
- 불편점: 오류 원인 파악이 어려움
- 사용자 영향: 더 나은 오류 메시지, 명확한 가이드
```

**2c단계: 종합적인 컨텍스트 추출:**

```markdown
# 종합 이해:
- 근본 원인: 오류 처리가 불완전하고 불명확한 메시지
- 사용자 경험: 개발자들이 느린 피드백과 혼란스러운 오류에 좌절
- 해결책: 구조화된 오류 생성 + 향상된 디스트럭처링
- 비즈니스 가치: 개발자 마찰 감소, 빠른 개발 반복

# 향상된 기능 문서:
### 향상된 오류 처리
`ErrorCodeFactory`가 이제 중첩 오류를 완전히 지원하며, Serilog 디스트럭처링이
모든 LanguageExt 오류 컨텍스트를 보존합니다 ([#106](https://github.com/org/functorium/pull/106)).

**이전:** 중첩 오류 정보가 손실되고 로그에 불완전하게 표시됨
**이후:** 모든 오류 컨텍스트가 보존되고 구조화된 로그로 출력됨

이 개선으로 개발 워크플로우에서 더 빠르고 명확한 피드백을 제공합니다.
```

### 3단계: 커밋 메시지 패턴으로 기능 유형 식별

- **"Add" / "추가"** 커밋 → 새 기능 또는 API
- **"Implement" / "구현"** 커밋 → 새 기능 또는 API
- **"Rename" / "이름 변경"** 커밋 → 브레이킹 체인지 또는 API 업데이트
- **"Improve/Enhance" / "개선" / "향상"** 커밋 → 기존 기능 개선
- **"Fix" / "수정"** 커밋 → 버그 수정 (중요하지 않으면 보통 포함하지 않음)
- **"Support for" / "지원"** 커밋 → 새 플랫폼/기술 통합
- **"Remove" / "제거"** 커밋 → 기능 제거 (브레이킹 체인지 가능)

### 4단계: 사용자 대면 영향 추출

각 중요한 커밋에 대해 다음을 결정합니다:

- **이것이 가능하게 하는 기능은?** (새 기능)
- **개발자에게 무엇이 변경되나?** (API 영향)
- **어떤 문제를 해결하나?** (유스케이스)
- **브레이킹 체인지인가?** (마이그레이션 필요)

## 커밋-기능 변환 프로세스

### 예시: 오류 처리 커밋 분석

`Functorium.md`에서:

```markdown
c5e604f Add ErrorCodeFactory.CreateFromException method (#97)
4ee28c2 Add ManyErrorsDestructurer for composite errors (#98)
```

**변환 프로세스:**

1. **커밋 c5e604f**: "Add ErrorCodeFactory.CreateFromException method"
   - **기능**: 예외에서 구조화된 오류 생성
   - **사용자 영향**: 개발자가 예외를 구조화된 오류 코드로 쉽게 변환 가능
   - **API 변경**: `ErrorCodeFactory.CreateFromException(string, Exception)` 메서드 추가

2. **커밋 4ee28c2**: "Add ManyErrorsDestructurer for composite errors"
   - **기능**: 복합 오류의 향상된 로깅
   - **사용자 영향**: 여러 오류가 함께 발생할 때 더 나은 로깅
   - **동작 변경**: ManyErrors 타입이 개별 오류로 분해되어 로깅

## 멀티 컴포넌트 기능 식별

**컴포넌트 간 관련 커밋 찾기:**

예시: 오류 처리 개선이 여러 파일에 나타남:

- `Functorium.md`: 핵심 오류 팩토리 변경
- `Functorium.Testing.md`: 테스트 유틸리티 업데이트

**통합 프로세스:**

1. **패턴 식별**: 여러 컴포넌트에서 유사한 "오류 처리" 커밋
2. **테마 찾기**: 오류 처리 통합 및 표준화
3. **통합 기능 생성**: "오류 처리 시스템 개선" 섹션
4. **마이그레이션 경로 표시**: 영향받는 모든 컴포넌트의 이전/이후 예시

## 릴리스 노트용 커밋 우선순위

### 높은 우선순위 커밋 (반드시 포함)

- 새 타입 (`Add.*Type`, `Add.*Factory`, `.*타입 추가`, `.*팩토리 추가`)
- 새 통합 지원 (`Add.*support`, `Support for.*`, `.*지원 추가`, `.*지원`)
- 브레이킹 API 변경 (`Rename.*`, `Remove.*`, `Change.*`, `.*이름 변경`, `.*제거`, `.*변경`)
- 주요 기능 (`Add.*method`, `Implement.*`, `.*메서드 추가`, `.*구현`, `.*추가`)
- 보안 개선 (`security`, `validation`, `보안`, `검증`)

### 중간 우선순위 커밋 (포함 고려)

- 성능 개선 (`Improve.*performance`, `Optimize.*`, `성능.*개선`, `.*최적화`)
- 향상된 구성 (`Add.*configuration`, `Support.*options`, `.*설정 추가`, `.*옵션 지원`)
- 더 나은 오류 처리 (`Improve.*error`, `Add.*validation`, `오류.*개선`, `.*검증 추가`)
- 개발자 경험 (`Enhance.*`, `Better.*`, `.*향상`, `.*개선`)

### 낮은 우선순위 커밋 (보통 건너뜀)

- 버그 수정 (`Fix.*`) - 중요하거나 사용자에게 보이지 않으면
- 사용자 영향 없는 내부 리팩토링
- 문서 업데이트
- 테스트 개선
- 코드 정리

## Breaking Changes 감지

Breaking Changes는 **두 가지 방법**으로 식별합니다.

### 방법 1: 커밋 메시지 패턴 (개발자 의도 표시)

커밋 메시지에서 다음 패턴을 찾습니다:

- `breaking`, `BREAKING`, `!:` (예: `feat!:`)

**예시:**

```
feat!: Change IErrorHandler to IErrorDestructurer
fix!: Remove deprecated Create method
BREAKING: Update authentication flow
```

### 방법 2: Git Diff 분석 (자동 감지, 권장)

`.api` 폴더의 Git diff를 분석하여 실제 API 변경사항을 객관적으로 감지합니다.

**Git Diff 파일 위치:**

```
.analysis-output/api-changes-build-current/api-changes-diff.txt
```

이 파일은 `ExtractApiChanges.cs` 스크립트가 다음 명령을 실행하여 생성합니다:

```bash
git diff HEAD -- 'Src/*/.api/*.cs'
```

#### 자동 감지 패턴

| Git Diff 패턴 | 의미 | Breaking? | 우선순위 |
|--------------|------|-----------|---------:|
| `- public class Foo` | 클래스 삭제 | Yes | 필수 |
| `- public interface IFoo` | 인터페이스 삭제 | Yes | 필수 |
| `- public void Method()` | 메서드 삭제 | Yes | 필수 |
| `- Method(int x)`<br>`+ Method(string x)` | 매개변수 타입 변경 | Yes | 필수 |
| `- Method(int x, int y)`<br>`+ Method(int x)` | 매개변수 개수 변경 | Yes | 필수 |
| `- interface IErrorHandler`<br>`+ interface IErrorDestructurer` | 타입 이름 변경 | Yes | 필수 |
| `+ public class Bar` | 새 클래스 추가 | No | 높음 |
| `+ public void NewMethod()` | 새 메서드 추가 | No | 높음 |
| 매개변수 이름만 변경 | 이름 변경 (바이너리 호환) | No | 낮음 |

#### Git Diff 예시

```diff
diff --git a/Src/Functorium/.api/Functorium.cs b/Src/Functorium/.api/Functorium.cs
@@ -34,11 +34,11 @@ namespace Functorium.Abstractions.Errors.DestructuringPolicies
 {
-    public class ErrorCodeExceptionalDestructurer : IErrorDestructurer
+    public class ErrorCodeExceptionalDestructurer : IErrorProcessor
     {
         public ErrorCodeExceptionalDestructurer() { }
-        public bool CanHandle(Error error) { }
+        public bool CanProcess(Error error) { }
-        public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory) { }
+        public LogEventPropertyValue Process(Error error, ILogEventPropertyValueFactory factory) { }
     }
 }
```

위 예시에서 감지되는 Breaking Changes:

- 인터페이스 이름 변경: `IErrorDestructurer` → `IErrorProcessor`
- 메서드 이름 변경: `CanHandle` → `CanProcess`
- 메서드 이름 변경: `Destructure` → `Process`

#### 왜 Git Diff 방법을 권장하나요?

| 비교 | 커밋 메시지 패턴 | Git Diff 분석 |
|-----|----------------|--------------|
| 정확도 | 개발자 의도에 의존 | 실제 코드 변경 감지 |
| 신뢰성 | 표시 누락 가능 | 객관적 증거 |
| 커버리지 | 명시적 표시만 | 모든 변경 감지 |

**중요:**

- Git Diff 분석은 커밋 메시지 패턴보다 **더 정확하고 객관적**입니다
- 개발자가 실수로 Breaking Change를 표시하지 않아도 **자동으로 감지**됩니다
- 첫 배포의 경우 diff가 비어있을 수 있습니다 (모든 API가 새로 생성됨)

## 커밋 분석에서 기능 작성하기

### 커밋에서 문서 섹션으로

**단계별 프로세스:**

1. **커밋 패턴 식별**
   - 키워드 스캔: "Add", "Support", "Improve", "Rename"
   - 기술 이름 찾기: "Serilog", "OpenTelemetry", "LanguageExt"
   - 기능 단어 찾기: "logging", "validation", "error handling"

2. **사용자 가치 추출**
   - **어떤 문제를 해결하나?** ("왜")
   - **개발자가 이제 무엇을 할 수 있나?** (기능)
   - **어떻게 사용하나?** (API)

3. **기능 섹션 구조 생성**

   ```markdown
   ### 기능 이름

   [새 기능과 가치에 대한 간략한 설명]

   ```csharp
   // Uber 파일에서 검증된 API를 사용한 코드 예시
   ```

   [예시 설명, 주요 이점, 추가 정보 링크]
   ```

### 관련 커밋을 통합 기능으로 그룹화

여러 커밋이 단일 사용자 대면 기능에 기여할 때:

**여러 관련 커밋:**

- `853c918 Rename IErrorHandler to IErrorDestructurer`
- `d4eacc6 Improve error destructuring output formatting`
- `a1b2c3d Add structured logging for all error types`

**통합 기능 섹션:**

```markdown
### 향상된 오류 로깅 시스템

오류 로깅 시스템이 더 나은 디스트럭처링, 명확한 출력 포맷, 향상된 구조화 로깅으로 크게 개선되었습니다.

```csharp
// 구조화된 오류 로깅 자동 적용
Log.Error("Operation failed: {@Error}", error);
// 출력: ErrorCode=APP001, Message="...", InnerErrors=[...]
```

주요 개선사항:

- **구조화된 디스트럭처링**: 모든 오류 타입에 대한 명확한 로그 출력
- **더 나은 오류 메시지**: 문제 해결에 더 도움이 되는 정보
- **향상된 추적**: 중첩 및 복합 오류 완전 지원
```

## 중간 결과 저장

Phase 3의 분석 결과를 `.analysis-output/work/` 폴더에 저장하여 추적 가능하게 만듭니다.

### 저장할 파일

```
.release-notes/scripts/.analysis-output/work/
├── phase3-commit-analysis.md     # 커밋 분류 및 우선순위
├── phase3-feature-groups.md      # 기능 그룹화 결과
└── phase3-api-mapping.md         # API와 커밋 매핑
```

### phase3-commit-analysis.md 형식

```markdown
# Phase 3: 커밋 분석 결과

## Breaking Changes
- 없음

## Feature Commits (높은 우선순위)
- [cda0a33] feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가
- [1790c73] feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가

## Feature Commits (중간 우선순위)
- [4727bf9] feat(api): PublicApiGenerator로 생성한 Public API 파일 추가

## Bug Fixes
- [a8ec763] fix(build): NuGet 패키지 아이콘 경로 수정
```

### phase3-feature-groups.md 형식

```markdown
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
- Configurators 추가

**사용자 가치:**
분산 추적, 메트릭, 로깅 통합 지원
```

## 콘솔 출력 형식

### 분석 완료

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 3: 커밋 분석 및 기능 추출 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

분석 결과:
  ✓ Breaking Changes: 0개
  ✓ Feature Commits: 6개 (높은 우선순위: 4개, 중간: 2개)
  ✓ Bug Fixes: 1개
  ✓ 기능 그룹: 8개

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
  ✓ .release-notes/scripts/.analysis-output/work/phase3-commit-analysis.md
  ✓ .release-notes/scripts/.analysis-output/work/phase3-feature-groups.md
  ✓ .release-notes/scripts/.analysis-output/work/phase3-api-mapping.md
```

## 성공 기준

- [ ] Breaking Changes 식별됨
- [ ] Feature Commits 분류됨
- [ ] 기능 그룹화 완료됨
- [ ] 중간 결과 저장됨

## 최종 단계: 검증

상세 분석 후 다음을 확인합니다:

1. **포함할 커밋 우선순위 지정:**
   - 높음: 새 타입, 통합 지원, 브레이킹 API 변경, 주요 기능, 보안 개선
   - 중간: 성능, 구성, 오류 처리, 개발자 경험
   - 낮음: 버그 수정 (중요하지 않으면), 리팩토링, 문서, 테스트, 정리

2. **API 검증:**
   - `.analysis-output/api-changes-build-current/all-api-changes.txt` (Uber 파일)를 사용하여 모든 API 참조 및 코드 샘플 확인

3. **Breaking Changes 검증:**
   - `api-changes-diff.txt`의 모든 삭제/변경 API가 문서화되었는지 확인
   - 커밋 메시지 패턴(`!:`, `breaking`)으로 표시된 커밋도 포함되었는지 확인

4. **정확하게 문서화:**
   - API를 발명하지 않습니다; 분석과 Uber 파일에서 확인된 것만 문서화
   - 브레이킹 체인지에 대한 마이그레이션 가이드 제공

## 다음 단계

커밋 분석 완료 후 [Phase 4: 릴리스 노트 작성](phase4-writing.md)으로 진행합니다.
