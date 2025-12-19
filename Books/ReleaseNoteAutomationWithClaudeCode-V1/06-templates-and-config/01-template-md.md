# 6.1 TEMPLATE.md 구조

> 이 절에서는 릴리스 노트 생성에 사용되는 TEMPLATE.md 파일의 구조와 작성 방법을 알아봅니다.

---

## 개요

TEMPLATE.md는 릴리스 노트의 **표준 형식**을 정의하는 템플릿 파일입니다.

```txt
위치: .release-notes/TEMPLATE.md

역할:
├── 릴리스 노트 기본 구조 정의
├── Placeholder로 동적 내용 표시
├── 작성 가이드라인 포함
└── 체크리스트 제공
```

---

## 파일 구조

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

---

## 프론트매터

YAML 형식의 메타데이터:

```yaml
---
title: Functorium {VERSION} 새로운 기능
description: Functorium {VERSION}의 새로운 기능을 알아봅니다.
date: {DATE}
---
```

### Placeholder

| Placeholder | 교체 예시 | 설명 |
|-------------|----------|------|
| `{VERSION}` | v1.2.0 | 릴리스 버전 |
| `{DATE}` | 2025-12-19 | 릴리스 날짜 |

### 교체 결과

```yaml
---
title: Functorium v1.2.0 새로운 기능
description: Functorium v1.2.0의 새로운 기능을 알아봅니다.
date: 2025-12-19
---
```

---

## 개요 섹션

```markdown
## 개요

{버전 소개 - 이 릴리스의 주요 목표와 테마}

**주요 기능**:

- **{기능1 카테고리}**: {한 줄 설명}
- **{기능2 카테고리}**: {한 줄 설명}
- **{기능3 카테고리}**: {한 줄 설명}
```

### 작성 예시

```markdown
## 개요

Functorium v1.0.0은 .NET 애플리케이션을 위한 함수형 프로그래밍 도구 모음의 첫 번째 정식 릴리스입니다.
이 릴리스에서는 오류 처리, 관측성(Observability), 테스트 지원에 중점을 두었습니다.

**주요 기능**:

- **함수형 오류 처리**: ErrorCodeFactory를 통한 구조화된 오류 생성
- **OpenTelemetry 통합**: 분산 추적, 메트릭, 로깅 통합 설정
- **테스트 픽스처**: ASP.NET Core 및 Quartz 통합 테스트 지원
```

---

## Breaking Changes 섹션

```markdown
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
```

### 작성 예시 (Breaking Change 있는 경우)

```markdown
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
```

---

## 새로운 기능 섹션

```markdown
## 새로운 기능

### {컴포넌트명} 라이브러리

#### 1. {기능명}

{기능 설명 - What: 무엇을 하는가?}

```csharp
{코드 샘플 - How: 어떻게 사용하는가?}
```

**장점:**
- {해결하는 문제}
- {개발자 생산성}
- {코드 품질 향상}
- {정량적 이점}

<!-- 관련 커밋: {SHA} {커밋 메시지} -->
```

### 필수 요소

모든 기능에 반드시 포함해야 하는 요소:

| 요소 | 설명 | 필수 |
|------|------|:----:|
| 기능 설명 | 무엇을 하는가? | O |
| 코드 샘플 | 어떻게 사용하는가? | O |
| **장점:** 섹션 | 왜 중요한가? | O |
| 커밋 주석 | 추적성 | O |

### 작성 예시

```markdown
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
    var error = ErrorCodeFactory.CreateFromException("HTTP_001", ex);
    Log.Error("API 요청 실패: {@Error}", error);
    return Fin<Response>.Fail(error);
}
```

**장점:**
- 예외를 함수형 오류로 변환하여 타입 안전성 향상
- Serilog와 자동 통합으로 구조화된 로깅 지원
- try-catch 보일러플레이트 코드 제거
- 일관된 오류 코드 체계로 디버깅 시간 단축

<!-- 관련 커밋: abc1234 feat(errors): Add ErrorCodeFactory.CreateFromException -->
```

---

## 버그 수정 섹션

```markdown
## 버그 수정

{버그 수정이 없는 경우 이 섹션 삭제}

- {버그 설명} ({SHA})
- {버그 설명} ({SHA})
```

### 작성 예시

```markdown
## 버그 수정

- NuGet 패키지 아이콘 경로가 잘못 설정된 문제 수정 (a8ec763)
- 특정 조건에서 발생하는 null 참조 예외 처리 (b9fd874)
```

---

## API 변경사항 섹션

```markdown
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
```

### 작성 예시

```markdown
## API 변경사항

### Functorium 네임스페이스 구조

```
Functorium
├── Abstractions/
│   ├── Errors/
│   │   ├── ErrorCodeFactory
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
```

---

## 설치 섹션

```markdown
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
```

### 작성 예시

```markdown
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
```

---

## 템플릿 가이드 (주석)

템플릿 하단의 주석은 작성 시 참고용이며, 최종 문서에서 삭제해야 합니다:

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
- [ ] 모든 기능에 "장점:" 섹션 포함
- [ ] 모든 코드 샘플이 Uber 파일에서 검증됨
- [ ] 커밋 SHA 주석 추가

참조 문서:
- 작성 규칙: .release-notes/scripts/docs/phase4-writing.md
- 검증 기준: .release-notes/scripts/docs/phase5-validation.md
- Uber 파일: .analysis-output/api-changes-build-current/all-api-changes.txt
-->
```

---

## 요약

| 섹션 | 필수 | 설명 |
|------|:----:|------|
| 프론트매터 | O | 메타데이터 (title, description, date) |
| 개요 | O | 버전 소개 및 주요 기능 |
| Breaking Changes | O | 호환성 깨는 변경 (없으면 명시) |
| 새로운 기능 | O | feat 커밋 기반 문서화 |
| 버그 수정 | | fix 커밋 기반 (없으면 생략) |
| API 변경사항 | O | 네임스페이스 구조 |
| 설치 | O | NuGet 설치 방법 |

---

## 다음 단계

- [6.2 component-priority.json 설정](02-component-config.md)
