---
title: "TEMPLATE.md 구조"
---

릴리스 노트를 매번 빈 문서에서 시작하면, 어떤 섹션을 넣어야 할지부터 고민해야 합니다. Breaking Changes는 빠뜨리기 쉽고, 설치 방법은 잊기 마련이며, 작성자에 따라 문서 구조가 제각각이 됩니다. TEMPLATE.md는 이 문제를 해결합니다. 릴리스 노트의 **표준 형식을** 미리 정의해 두고, 매번 같은 구조로 문서를 작성할 수 있게 하는 템플릿 파일입니다.

이 파일은 `.release-notes/TEMPLATE.md`에 위치합니다. 릴리스 노트의 기본 구조를 정의하고, Placeholder로 동적 내용이 들어갈 위치를 표시하며, 작성 가이드라인과 체크리스트를 포함하고 있어 누가 작성하더라도 일관된 품질의 문서를 만들 수 있습니다.

## 파일 구조

템플릿의 전체 골격은 다음과 같습니다.

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

<!-- 템플릿 가이드 (삭제 필요) -->
```

프론트매터부터 설치까지 총 6개 섹션으로 구성되어 있습니다. 각 섹션이 왜 필요하고 어떻게 작성하는지 하나씩 살펴보겠습니다.

## 프론트매터

문서 상단의 YAML 프론트매터는 메타데이터를 정의합니다.

```yaml
---
title: Functorium {VERSION} 새로운 기능
description: Functorium {VERSION}의 새로운 기능을 알아봅니다.
date: {DATE}
---
```

여기서 `{VERSION}`과 `{DATE}`는 실제 값으로 교체해야 하는 Placeholder입니다. `{VERSION}`은 `v1.2.0` 같은 릴리스 버전으로, `{DATE}`는 `2025-12-19` 같은 릴리스 날짜로 바꿉니다.

교체 결과는 다음과 같습니다.

```yaml
---
title: Functorium v1.2.0 새로운 기능
description: Functorium v1.2.0의 새로운 기능을 알아봅니다.
date: 2025-12-19
---
```

## 개요 섹션

개요는 릴리스의 첫인상입니다. 이번 버전이 어떤 목표를 가지고 있는지, 주요 기능이 무엇인지를 한눈에 보여줍니다.

```markdown
## 개요

{버전 소개 - 이 릴리스의 주요 목표와 테마}

**주요 기능**:

- **{기능1 카테고리}**: {한 줄 설명}
- **{기능2 카테고리}**: {한 줄 설명}
- **{기능3 카테고리}**: {한 줄 설명}
```

실제로 작성하면 이런 모습이 됩니다.

```markdown
## 개요

Functorium v1.0.0은 .NET 애플리케이션을 위한 함수형 프로그래밍 도구 모음의 첫 번째 정식 릴리스입니다.
이 릴리스에서는 오류 처리, 관측성(Observability), 테스트 지원에 중점을 두었습니다.

**주요 기능**:

- **함수형 오류 처리**: ErrorFactory를 통한 구조화된 오류 생성
- **OpenTelemetry 통합**: 분산 추적, 메트릭, 로깅 통합 설정
- **테스트 픽스처**: ASP.NET Core 및 Quartz 통합 테스트 지원
```

---

## Breaking Changes 섹션

Breaking Changes는 업그레이드하는 사용자에게 가장 중요한 정보입니다. 기존 코드가 깨질 수 있는 변경이므로, 없더라도 "없다"고 명시해야 합니다. 변경이 있는 경우에는 이전/이후 코드와 마이그레이션 가이드를 반드시 포함합니다.

````markdown
## Breaking Changes

{Breaking Changes가 없는 경우}
이번 릴리스에는 Breaking Changes가 없습니다.

{Breaking Changes가 있는 경우}
### {변경된 API/기능명}

{변경 내용 설명}

**이전 ({이전 버전})**:
```csharp
{이전 코드}
```

**이후 ({현재 버전})**:
```csharp
{새 코드}
```

**마이그레이션 가이드**:
1. {단계 1}
2. {단계 2}
3. {단계 3}
````

Breaking Change가 있는 경우의 작성 예시입니다.

````markdown
## Breaking Changes

### IErrorHandler 인터페이스 이름 변경

오류 처리 인터페이스가 더 명확한 이름으로 변경되었습니다.

**이전 (v0.9.0)**:
```csharp
public class MyHandler : IErrorHandler
{
    public void Handle(Error error) { }
}
```

**이후 (v1.0.0)**:
```csharp
public class MyDestructurer : IErrorDestructurer
{
    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory) { }
}
```

**마이그레이션 가이드**:
1. 모든 `IErrorHandler` 참조를 `IErrorDestructurer`로 변경
2. `Handle` 메서드를 `Destructure`로 이름 변경
3. 반환 타입을 `LogEventPropertyValue`로 수정
````

---

## 새로운 기능 섹션

새로운 기능은 릴리스 노트의 핵심입니다. 각 기능은 "무엇을 하는가(What)", "어떻게 사용하는가(How)", "왜 중요한가(Why)"의 세 가지 관점에서 문서화해야 합니다. 코드 예제과 함께 관련 커밋 SHA를 주석으로 남겨 추적성을 확보합니다.

````markdown
## 새로운 기능

### {컴포넌트명} 라이브러리

#### 1. {기능명}

{기능 설명 - What: 무엇을 하는가?}

```csharp
{코드 예제 - How: 어떻게 사용하는가?}
```

**Why this matters (왜 중요한가):**
- {해결하는 문제}
- {개발자 생산성}
- {코드 품질 향상}
- {정량적 이점}

<!-- 관련 커밋: {SHA} {커밋 메시지} -->
````

모든 기능에는 기능 설명, 코드 예제, **Why this matters** 섹션, 커밋 주석이 반드시 포함되어야 합니다. 이 네 가지 요소가 갖춰져야 사용자가 기능을 이해하고 바로 사용할 수 있습니다.

실제 작성 예시를 보겠습니다.

````markdown
### Functorium 라이브러리

#### 1. 예외에서 구조화된 오류 생성

LanguageExt Error 타입을 기반으로 예외를 구조화된 오류 코드로 변환합니다.

```csharp
using Functorium.Abstractions.Errors;

try
{
    await HttpClient.GetAsync(url);
}
catch (HttpRequestException ex)
{
    var error = ErrorFactory.CreateExceptional("HTTP_001", ex);
    Log.Error("API 요청 실패: {@Error}", error);
    return Fin<Response>.Fail(error);
}
```

**Why this matters (왜 중요한가):**
- 예외를 함수형 오류로 변환하여 타입 안전성 향상
- Serilog와 자동 통합으로 구조화된 로깅 지원
- try-catch 보일러플레이트 코드 제거
- 일관된 오류 코드 체계로 디버깅 시간 단축

<!-- 관련 커밋: abc1234 feat(errors): Add ErrorFactory.CreateExceptional -->
````

---

## 버그 수정 섹션

버그 수정은 간결하게 작성합니다. 수정 내용과 커밋 SHA만 나열하면 충분합니다. 수정할 버그가 없으면 이 섹션 자체를 삭제합니다.

```markdown
## 버그 수정

{버그 수정이 없는 경우 이 섹션 삭제}

- {버그 설명} ({SHA})
- {버그 설명} ({SHA})
```

작성 예시입니다.

```markdown
## 버그 수정

- NuGet 패키지 아이콘 경로가 잘못 설정된 문제 수정 (a8ec763)
- 특정 조건에서 발생하는 null 참조 예외 처리 (b9fd874)
```

## API 변경사항 섹션

API 변경사항은 네임스페이스 구조를 트리 형태로 보여줍니다. 라이브러리 사용자가 어떤 타입이 어디에 위치하는지 한눈에 파악할 수 있게 하는 것이 목적입니다.

````markdown
## API 변경사항

### {컴포넌트명} 네임스페이스 구조

```
{Namespace.Root}
├── {SubNamespace1}/
│   ├── {Class1}
│   └── {Class2}
└── {SubNamespace2}/
    └── {Class3}
```
````

작성 예시입니다.

````markdown
## API 변경사항

### Functorium 네임스페이스 구조

```
Functorium
├── Abstractions/
│   ├── Errors/
│   │   ├── ErrorFactory
│   │   └── ErrorsDestructuringPolicy
│   └── Registrations/
│       └── OpenTelemetryRegistration
├── Adapters/
│   └── Observabilities/
│       └── Builders/
│           └── OpenTelemetryBuilder
└── Applications/
    └── Linq/
        └── FinTUtilities
```
````

## 설치 섹션

설치 섹션은 사용자가 바로 따라 할 수 있는 NuGet 설치 명령과 필수 의존성 정보를 제공합니다.

````markdown
## 설치

### NuGet 패키지 설치

```bash
# {패키지명} 핵심 라이브러리
dotnet add package {PackageName} --version {VERSION}

# {패키지명} 테스트 라이브러리 (선택적)
dotnet add package {PackageName}.Testing --version {VERSION}
```

### 필수 의존성

- .NET {버전} 이상
- {의존성 1}
- {의존성 2}
````

작성 예시입니다.

````markdown
## 설치

### NuGet 패키지 설치

```bash
# Functorium 핵심 라이브러리
dotnet add package Functorium --version 1.0.0

# Functorium 테스트 라이브러리 (선택적)
dotnet add package Functorium.Testing --version 1.0.0
```

### 필수 의존성

- .NET 10.0 이상
- LanguageExt.Core 5.0.0 이상
- Serilog 4.0.0 이상 (로깅 기능 사용 시)
````

---

## 템플릿 가이드 (주석)

템플릿 하단의 주석은 작성 시 참고용이며, 최종 문서에서 삭제해야 합니다.

```markdown
<!--
============================================================
템플릿 사용 가이드
============================================================

1. {VERSION}을 실제 버전으로 교체 (예: v1.0.0)
2. {DATE}를 오늘 날짜로 교체 (예: 2025-12-19)
3. 각 섹션의 {placeholder}를 실제 내용으로 교체
4. 주석 (`<!-- -->` 형식)은 최종 문서에서 삭제
5. 해당 없는 섹션은 삭제

필수 체크리스트:
- [ ] 프론트매터 완성
- [ ] 개요 섹션 작성
- [ ] Breaking Changes 확인 (api-changes-diff.txt)
- [ ] 모든 feat 커밋에 대한 기능 문서화
- [ ] 모든 fix 커밋에 대한 버그 수정 문서화
- [ ] 모든 기능에 "Why this matters" 섹션 포함
- [ ] 모든 코드 예제이 Uber 파일에서 검증됨
- [ ] 커밋 SHA 주석 추가

참조 문서:
- 작성 규칙: .release-notes/scripts/docs/phase4-writing.md
- 검증 기준: .release-notes/scripts/docs/phase5-validation.md
- Uber 파일: .analysis-output/api-changes-build-current/all-api-changes.txt
-->
```

이 주석에는 Placeholder 교체 순서, 필수 체크리스트, 참조 문서 링크가 포함되어 있어 작성 과정에서 빠뜨리기 쉬운 항목을 점검할 수 있습니다.

---

TEMPLATE.md는 프론트매터, 개요, Breaking Changes, 새로운 기능, 버그 수정, API 변경사항, 설치의 7개 섹션으로 구성됩니다. 이 중 버그 수정만 해당 사항이 없으면 생략할 수 있고, 나머지는 모두 필수입니다. 특히 새로운 기능 섹션의 모든 항목에는 기능 설명, 코드 예제, "Why this matters", 커밋 주석이 반드시 포함되어야 합니다. 이 구조를 따르면 작성자가 달라도 일관된 품질의 릴리스 노트를 만들 수 있습니다.

## FAQ

### Q1: 템플릿의 섹션 순서를 변경해도 되나요?
**A**: 권장하지 않습니다. 현재 순서(개요 → Breaking Changes → 새로운 기능 → 버그 수정 → API 변경사항 → 설치)는 **사용자가 가장 먼저 확인해야 할 정보부터** 배치한 것입니다. 특히 Breaking Changes가 새로운 기능보다 앞에 오는 것은, 업그레이드 시 코드 수정 여부를 먼저 파악해야 하기 때문입니다.

### Q2: "Why this matters" 섹션이 모든 기능에 필수인 이유는 무엇인가요?
**A**: API 시그니처와 코드 예제만으로는 **해당 기능이 어떤 문제를 해결하는지** 사용자가 파악하기 어렵습니다. "Why this matters" 섹션은 기능의 실질적 가치를 전달하여, 사용자가 업데이트 여부를 판단하고 도입 우선순위를 결정하는 데 도움을 줍니다.

### Q3: 버그 수정 섹션만 삭제 가능하고 나머지는 필수인 이유는 무엇인가요?
**A**: 버그 수정이 없는 릴리스는 실제로 존재하지만, **개요, Breaking Changes(없더라도 "없음" 명시), 새로운 기능, API 변경사항, 설치 가이드는** 어떤 릴리스에서든 사용자가 반드시 확인해야 할 정보입니다. 빈 Breaking Changes 섹션이라도 명시적으로 "없다"고 알려주는 것이 사용자 신뢰에 중요합니다.

### Q4: 하단의 HTML 주석 가이드는 최종 문서에서 반드시 삭제해야 하나요?
**A**: 네. `<!-- -->` 형식의 주석은 Markdown 렌더러에 따라 숨겨지기도 하지만, **GitHub의 Raw 보기나 일부 문서 사이트에서는** 노출될 수 있습니다. Phase 5 검증에서 주석 삭제 여부를 체크리스트로 확인하므로, 최종 제출 전 반드시 제거해야 합니다.

## 다음 단계

- [component-priority.json 설정](08-component-config.md)
