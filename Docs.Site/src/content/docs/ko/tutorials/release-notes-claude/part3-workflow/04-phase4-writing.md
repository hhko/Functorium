---
title: "릴리스 노트 작성"
---

분석은 끝났습니다. 이제 데이터를 개발자가 읽고 싶어하는 문서로 만들 차례입니다. Phase 4는 전체 워크플로우에서 가장 시간이 많이 걸리는 단계이지만, 그만큼 가장 중요한 단계이기도 합니다. 아무리 정확한 분석이라도 읽기 어려운 문서가 되면 가치가 없기 때문입니다.

## 파일 정보

| 항목 | 경로 |
|------|------|
| 템플릿 | `.release-notes/TEMPLATE.md` |
| 출력 | `.release-notes/RELEASE-$VERSION.md` |

## 작성 절차

### 1. 템플릿 복사

```bash
cp .release-notes/TEMPLATE.md .release-notes/RELEASE-v1.2.0.md
```

### 2. Placeholder 교체

| Placeholder | 변환 예시 |
|-------------|----------|
| `{VERSION}` | v1.2.0 |
| `{DATE}` | 2025-12-19 |

### 3. 섹션 채우기

Phase 3 분석 결과를 참조하여 각 섹션을 작성합니다. `phase3-commit-analysis.md`에서 커밋 분류를, `phase3-feature-groups.md`에서 기능 그룹을 가져옵니다.

### 4. API 검증

모든 코드 예제을 Uber 파일에서 검증합니다.

### 5. 주석 정리

템플릿 가이드 주석(`<!-- -->`)을 삭제합니다.

## 템플릿 구조

릴리스 노트에 포함되어야 하는 섹션입니다.

| 섹션 | 필수 | 설명 |
|------|:----:|------|
| 프론트매터 | O | YAML 헤더 (title, description, date) |
| 개요 | O | 버전 소개, 주요 변경 요약 |
| Breaking Changes | O | API 호환성 깨는 변경 (없으면 명시) |
| 새로운 기능 | O | feat 커밋 기반 |
| 버그 수정 | | fix 커밋 기반 (없으면 생략) |
| API 변경사항 | O | 새로 추가된 주요 API 요약 |
| 설치 | O | 설치 방법 |

## 핵심 문서화 규칙

### 1. 정확성 우선

Uber 파일에 없는 API는 절대 문서화하지 않습니다. 이것이 가장 중요한 규칙입니다.

```txt
검증 방법:
grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. 코드 예제 필수

모든 주요 기능에 실행 가능한 코드 예제을 포함합니다.

### 3. 추적성

커밋 SHA를 주석으로 포함하여, 릴리스 노트의 모든 내용이 실제 변경으로 추적 가능하도록 합니다.

```markdown
<!-- 관련 커밋: abc1234 -->
```

### 4. 가치 전달 필수

**"Why this matters" 섹션이 없는 기능 문서화는 불완전한** 것으로 간주됩니다. 이 규칙이 존재하는 이유는, 기능의 존재 자체보다 그 기능이 개발자의 일상에 어떤 변화를 가져오는지가 더 중요하기 때문입니다. 단순히 "ErrorFactory를 추가했습니다"라고 쓰면 개발자는 "그래서 나한테 왜 중요하지?"라는 질문에 답을 찾지 못합니다.

## 가치 전달 방법

### 필수 구조

각 기능은 다음 네 가지 요소를 **모두** 포함해야 합니다.

1. **기능 설명 (What)** - 무엇을 하는가?
2. **코드 예제 (How)** - 어떻게 사용하는가?
3. **Why this matters (Why)** - 왜 중요한가?
4. **API 참조 (Reference)** - 정확한 API 시그니처

### 나쁜 예시 (단순 사실 나열)

```markdown
### 함수형 오류 처리

ErrorFactory를 통한 구조화된 오류 생성 기능을 제공합니다.

[코드 예제]
```

이 문서의 문제는 개발자가 왜 이 기능을 사용해야 하는지 알 수 없다는 점입니다. 기능이 존재한다는 사실만 전달할 뿐, 어떤 문제를 해결하는지, 기존 방식보다 나은 점이 무엇인지 설명하지 않습니다.

### 좋은 예시 (가치 중심)

```markdown
### 함수형 오류 처리

ErrorFactory를 통한 구조화된 오류 생성 기능을 제공합니다.

[코드 예제]

**Why this matters (왜 중요한가):**
- 예외를 함수형 오류로 변환하여 타입 안전성 향상
- Serilog와 자동 통합으로 구조화된 로깅 지원
- 보일러플레이트 코드 제거 (try-catch 반복 감소)
- 일관된 오류 코드 체계로 디버깅 시간 단축
- 비즈니스 로직과 오류 처리 분리로 코드 가독성 개선
```

여기서는 각 항목이 개발자가 직면하는 구체적인 문제(타입 안전성, 보일러플레이트, 디버깅 시간)와 연결됩니다.

### Why this matters 작성 체크리스트

- [ ] 개발자가 직면하는 **구체적인 문제** 명시
- [ ] 이 기능이 그 문제를 **어떻게 해결하는지** 설명
- [ ] **정량적 이점** 포함 (가능한 경우: 시간 절약, 코드 감소)
- [ ] **정성적 이점** 포함 (가독성, 유지보수성, 타입 안전성)
- [ ] **실제 유스케이스** 제시

### 피해야 할 표현 vs 권장 표현

| 피해야 할 표현 | 권장 표현 |
|---------------|----------|
| "기능을 제공합니다" | "~하여 [구체적 이점]을 제공합니다" |
| "지원합니다" | "[문제]를 해결하여 [결과]를 달성합니다" |
| "추가되었습니다" | "[복잡한 작업]을 [간단한 방법]으로 간소화합니다" |
| "~할 수 있습니다" | "[시간/코드] 절감으로 생산성을 향상시킵니다" |

## API 문서화 규칙

### API 소스

**Uber 파일이 단일 진실 소스입니다.**

```txt
.analysis-output/api-changes-build-current/all-api-changes.txt
```

### 정확한 문서 작성 프로세스

API를 문서화할 때는 반드시 다음 순서를 따릅니다. 먼저 Uber 파일에서 API를 검색하고, 개별 API 파일에서 상세 내용을 확인한 뒤, 완전한 API 시그니처를 추출하고, 올바른 시그니처로 코드 예제을 작성합니다. 이 순서를 지키지 않으면 존재하지 않는 API를 문서화하는 실수가 발생합니다.

```txt
1단계: Uber 파일에서 API 검색
grep -A 10 "ErrorFactory" all-api-changes.txt

2단계: 개별 API 파일에서 상세 확인
cat Src/Functorium/.api/Functorium.cs | grep -A 5 "ErrorFactory"

3단계: 완전한 API 시그니처 추출

4단계: 올바른 시그니처로 코드 예제 작성
```

### API 시그니처 정확히 일치

```csharp
// Uber 파일에서 확인한 정확한 시그니처:
public static Error Create(string errorCode, string errorCurrentValue, string errorMessage)
public static Error CreateFromException(string errorCode, Exception exception)
```

```csharp
// 올바른 사용 예시:
var error = ErrorFactory.CreateExpected("VALIDATION_001", invalidValue, "값이 유효하지 않습니다");
var errorFromEx = ErrorFactory.CreateExceptional("SYSTEM_001", exception);
```

### API를 발명하지 마세요

실제로 존재하지 않는 메서드나 플루언트 체인을 만들어내는 것은 가장 흔하고 위험한 실수입니다. Uber 파일에 없는 API는 코드 예제에 절대 사용해서는 안 됩니다.

```csharp
// 잘못됨: 이 메서드들은 Uber 파일에 없음
ErrorFactory.CreateExpected("error")
    .WithDetails(details: "추가 정보")      // 발명됨
    .WithInnerError(inner: innerError)     // 발명됨
    .Build();                               // 발명됨
```

## 완전한 기능 문서 예시

### 예시 1: 기본 기능

````markdown
### 예외에서 구조화된 오류 생성

LanguageExt Error 타입을 기반으로 예외를 구조화된 오류 코드로 변환합니다.

```csharp
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

try
{
    await HttpClient.GetAsync(url);
}
catch (HttpRequestException ex)
{
    // 예외에서 구조화된 오류 생성
    var error = ErrorFactory.CreateExceptional("HTTP_001", ex);

    // Serilog에 구조화된 형태로 자동 로깅
    Log.Error("API 요청 실패: {@Error}", error);

    return Fin<Response>.Fail(error);
}
```

**Why this matters (왜 중요한가):**
- 예외를 함수형 오류로 변환하여 타입 안전성 향상
- Serilog와 자동 통합으로 구조화된 로깅 지원
- try-catch 보일러플레이트 코드 제거
- 일관된 오류 코드 체계로 디버깅 시간 단축

**API:**
```csharp
namespace Functorium.Abstractions.Errors
{
    public static class ErrorFactory
    {
        public static Error CreateFromException(string errorCode, Exception exception);
    }
}
```
````

### 예시 2: Breaking Change

````markdown
### 오류 핸들러 인터페이스 이름 변경

오류 처리 API가 더 나은 일관성을 위해 통합되었습니다.

**이전 (v1.0):**
```csharp
public class MyHandler : IErrorHandler
{
    public void Handle(Error error) { }
}
```

**이후 (v1.1):**
```csharp
public class MyDestructurer : IErrorDestructurer
{
    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory) { }
}
```

**마이그레이션 가이드:**
1. 모든 `IErrorHandler` 참조를 `IErrorDestructurer`로 업데이트
2. `Handle` 메서드를 `Destructure` 메서드로 교체
3. 반환 타입을 `LogEventPropertyValue`로 변경
````

## 작성 스타일 가이드

### 언어 및 톤

- **능동태를 사용합니다:** "이제 오류를 생성할 수 있습니다" (O) / "오류가 생성될 수 있습니다" (X)
- **개발자 중심으로 작성합니다:** 실용적인 예시로 개발자가 바로 활용할 수 있게
- **명확하고 간결하게 씁니다:** 불필요한 전문 용어나 장황한 설명은 피합니다

### 코드 포맷팅

- **언어를 지정합니다:** 항상 ```csharp, ```bash, ```json
- **완전한 예시를 보여줍니다:** 전체가 작동하는 코드 예제
- **새 기능을 강조합니다:** `// 새 기능` 주석으로 변경 부분 표시

### 섹션 구성

- **가장 영향력 있는 변경부터 시작합니다:** Breaking Changes를 먼저, 그다음 주요 기능 순
- **관련 기능을 그룹화합니다:** 유사한 기능을 통합 섹션으로 결합
- **설명적인 제목을 사용합니다:** 스캔하기 쉽도록

## 중간 결과 저장

Phase 4의 작성 결과를 저장합니다.

```txt
.release-notes/scripts/.analysis-output/work/
├── phase4-draft.md              # 릴리스 노트 초안
├── phase4-api-references.md     # 사용된 API 목록
└── phase4-code-samples.md       # 모든 코드 예제
```

## 콘솔 출력 형식

### 작성 완료

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 4: 릴리스 노트 작성 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

작성 통계:
  전체 길이: 15,380 줄
  섹션 수: 8개
  코드 예제: 24개
  API 참조: 30개 타입

주요 섹션:
  1. 개요 (버전: v1.0.0-alpha.1)
  2. Breaking Changes (0개)
  3. 새로운 기능 (8개)
  4. 버그 수정 (1개)
  5. API 변경사항 (요약)
  6. 설치

출력 파일:
  .release-notes/RELEASE-v1.0.0-alpha.1.md
```

## 품질 체크리스트

문서 완료 전 확인해야 할 항목입니다.

### 정확성 검증

- [ ] 모든 코드 예제 Uber 파일에서 검증됨
- [ ] API 시그니처 매개변수 이름 및 타입 정확히 일치
- [ ] 발명된 API나 명령 없음
- [ ] Breaking Changes에 대한 완전한 마이그레이션 가이드

### 가치 전달 검증

- [ ] 모든 주요 기능에 "Why this matters" 섹션 포함됨
- [ ] 구체적인 문제 해결 명시
- [ ] 개발자 생산성 이점 설명
- [ ] 실제 유스케이스 제시

### 구조 및 포맷팅

- [ ] 일관된 포맷팅 - 기존 템플릿과 일치
- [ ] 명확하고 개발자 중심 언어
- [ ] 적절한 문서 링크

## FAQ

### Q1: "Why this matters" 섹션이 없는 기능 문서화가 왜 불완전한 것으로 간주되나요?
**A**: "ErrorFactory를 추가했습니다"만으로는 개발자가 그 기능을 **써야 할 이유를** 알 수 없기 때문입니다. 어떤 문제를 해결하는지, 기존 방식보다 나은 점이 무엇인지, 코드가 얼마나 줄어드는지를 함께 설명해야 릴리스 노트가 업데이트 결정에 실질적인 도움을 줍니다.

### Q2: API를 문서화할 때 Uber 파일에서 검색하는 구체적인 방법은 무엇인가요?
**A**: `grep -A 10 "ErrorFactory" all-api-changes.txt` 명령으로 API 존재 여부와 정확한 시그니처를 확인합니다. 매개변수 이름, 타입, 반환값이 Uber 파일과 **정확히 일치해야** 합니다. 비슷한 이름의 메서드가 여러 개 있을 때 순서가 뒤바뀌지 않도록 주의해야 합니다.

### Q3: 피해야 할 표현("기능을 제공합니다")과 권장 표현의 차이는 무엇인가요?
**A**: 피해야 할 표현은 기능의 존재만 알리고, 권장 표현은 **구체적인 이점을** 전달합니다. "지원합니다" 대신 "[문제]를 해결하여 [결과]를 달성합니다", "추가되었습니다" 대신 "[복잡한 작업]을 [간단한 방법]으로 간소화합니다"처럼 개발자가 체감할 수 있는 가치를 명시합니다.

릴리스 노트 작성이 완료되면, 최종 품질을 확인하는 [Phase 5: 검증](05-phase5-validation.md)으로 진행합니다.
