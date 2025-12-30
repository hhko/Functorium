# Trace-Analysis.md 데이터 형식 개선 사항

이 문서는 `Trace-Analysis.md`에서 발견된 **데이터 형식, 데이터 구조, 데이터 품질** 관점의 개선 사항을 정리합니다.

## 데이터 형식 일관성 문제

### 1. Activity 이름 형식의 불일치

**문제점**:
- Usecase: `Application Usecase.Command CreateProductCommand.Handle` (공백 포함)
- IAdapter: `Adapter Repository InMemoryProductRepository.ExistsByName` (공백 포함)
- Activity 이름에 공백이 포함되어 일부 도구에서 파싱 어려움 가능

**현재 상태**:
```
Usecase:   "Application Usecase.Command CreateProductCommand.Handle"
IAdapter:  "Adapter Repository InMemoryProductRepository.ExistsByName"
```

**개선 방안**:
- Activity 이름에서 공백 제거: `Application.Usecase.Command.CreateProductCommand.Handle`
- 또는 언더스코어 사용: `Application_Usecase_Command_CreateProductCommand_Handle`
- 또는 현재 형식 유지하되 파싱 규칙 명시

**영향도**: 중간 - 일부 Trace 시각화 도구에서 파싱 문제 가능

---

### 2. 태그 키 형식의 일관성

**문제점**:
- 모든 태그 키가 소문자 + 점 구분 (`request.layer`, `request.category`)
- OpenTelemetry 표준과 일치하나 일부 도구에서는 언더스코어 선호

**현재 상태**:
- 태그 키: `request.layer`, `request.category`, `request.handler.cqrs`, `request.handler`, `request.handler.method`
- 모두 소문자 + 점 구분으로 일관됨 ✅

**개선 방안**:
- 현재 상태 유지 (OpenTelemetry 표준 준수)
- 다만 도구별 변환 규칙 문서화 필요

**영향도**: 낮음 - 현재 일관성 있으나 도구 호환성 문서화 필요

---

### 3. 태그 값 타입의 불일치

**문제점**:
- 대부분의 태그 값: `string` 타입
- `response.elapsed`: `double` 타입 (숫자)
- 타입이 혼재되어 있음

**현재 상태**:
```csharp
// 문자열 태그
activity.SetTag("request.layer", "Application");  // string
activity.SetTag("request.category", "Usecase");   // string

// 숫자 태그
activity.SetTag("response.elapsed", 200.4511);    // double
```

**개선 방안**:
- 모든 태그 값을 문자열로 통일: `"200.4511"`
- 또는 숫자 태그는 별도 표준 태그 사용 (예: `duration.ms`)

**영향도**: 중간 - 타입 불일치로 인한 쿼리/필터링 복잡도 증가

---

### 4. Status 값의 이중 표현

**문제점**:
- `response.status` 태그: `"Success"` / `"Failure"` (문자열)
- `ActivityStatusCode`: `Ok` / `Error` (열거형)
- 동일한 정보가 두 가지 형식으로 표현됨

**현재 상태**:
```csharp
// 태그
activity.SetTag("response.status", "Success");  // 문자열

// 상태 코드
activity.SetStatus(ActivityStatusCode.Ok);     // 열거형
```

**개선 방안**:
- Status 정보를 태그와 상태 코드 중 하나로 통일
- 또는 두 가지 모두 유지하되 역할 명확히 구분

**영향도**: 낮음 - 기능에는 영향 없으나 데이터 중복

---

## 데이터 구조 문제

### 5. Usecase와 IAdapter의 에러 태그 불일치

**문제점**:
- Usecase: 상세한 에러 태그 (`error.type`, `error.code`, `error.message`, `error.count`)
- IAdapter: 에러 태그 없음 (StatusDescription만 설정)

**현재 상태**:
```
Usecase 에러 태그:
  - error.type: "ErrorCodeExpected"
  - error.code: "ApplicationErrors.UsecaseValidationPipeline.Validator"
  - error.message: "Name: 상품명은 필수입니다"
  - error.count: 3 (ManyErrors인 경우)

IAdapter 에러 태그:
  - 없음 ❌
  - StatusDescription만: error.Message
```

**개선 방안**:
- IAdapter에도 동일한 에러 태그 구조 적용
- 또는 IAdapter는 간소화된 에러 정보만 기록하는 것을 명시

**영향도**: 중간 - 에러 분석 시 Usecase/IAdapter 간 불일치

---

### 6. Request 태그의 불일치

**문제점**:
- Usecase: `request.handler.cqrs` 태그 포함
- IAdapter: `request.handler.cqrs` 태그 없음
- 태그 구조가 다름

**현재 상태**:
```
Usecase Request 태그:
  - request.layer: "Application"
  - request.category: "Usecase"
  - request.handler.cqrs: "Command" / "Query" ✅
  - request.handler: "CreateProductCommand"

IAdapter Request 태그:
  - request.layer: "Adapter"
  - request.category: "Repository"
  - request.handler: "InMemoryProductRepository"
  - request.handler.cqrs: 없음 ❌
```

**개선 방안**:
- 공통 태그는 동일하게 유지
- 또는 태그 차이를 명시적으로 문서화

**영향도**: 중간 - 태그 구조 차이로 인한 통합 쿼리 작성 어려움

---

### 7. Response 태그의 불일치

**문제점**:
- Usecase: `response.status` 태그 포함
- IAdapter: `response.status` 태그 없음 (ActivityStatusCode만 사용)

**현재 상태**:
```
Usecase Response 태그:
  - response.elapsed: 200.4511
  - response.status: "Success" / "Failure" ✅

IAdapter Response 태그:
  - response.elapsed: 96.0635
  - response.status: 없음 ❌
  - ActivityStatusCode만 사용
```

**개선 방안**:
- IAdapter에도 `response.status` 태그 추가
- 또는 Usecase에서 태그 제거하고 ActivityStatusCode만 사용

**영향도**: 중간 - 태그 불일치로 인한 쿼리 복잡도 증가

---

## 데이터 품질 문제

### 8. Activity 생성 실패 시 데이터 손실

**문제점**:
- Activity 생성 실패 시 (`StartActivity()`가 `null` 반환) 추적 없이 진행
- 샘플링 또는 리소스 제약으로 인한 데이터 손실 가능

**현재 상태**:
```csharp
using Activity? activity = _activitySource.StartActivity(...);
if (activity == null)
{
    // Activity 생성 실패 시 추적 없이 다음 Pipeline으로
    return await next(request, cancellationToken);
}
```

**개선 방안**:
- Activity 생성 실패 원인 로깅
- 또는 샘플링 전략 조정

**영향도**: 낮음 - 의도된 동작이지만 모니터링 필요

---

### 9. Parent Context 결정 로직의 복잡성

**문제점**:
- IAdapter의 Parent Context 결정이 복잡함 (Traverse Activity 우선, 없으면 Usecase Activity)
- 결정 로직이 명확하지 않으면 Trace 계층 구조가 예상과 다를 수 있음

**현재 상태**:
```csharp
Activity? traverseActivity = TraceParentActivityHolder.GetCurrent();
ActivityContext actualParentContext;

if (traverseActivity != null)
{
    actualParentContext = traverseActivity.Context;  // 우선순위 1
}
else
{
    actualParentContext = parentContext;  // 우선순위 2 (Usecase)
}
```

**개선 방안**:
- Parent Context 결정 로직을 명시적으로 문서화
- 또는 결정 로직 단순화 검토

**영향도**: 낮음 - 현재 동작하나 복잡도 높음

---

### 10. StartTime 정확성

**문제점**:
- IAdapter는 `DateTimeOffset.UtcNow` 사용
- Usecase는 `ElapsedTimeCalculator.GetCurrentTimestamp()` 사용
- 시간 측정 방식이 다름

**현재 상태**:
```csharp
// IAdapter
DateTimeOffset startTime = DateTimeOffset.UtcNow;

// Usecase
long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();
```

**개선 방안**:
- 시간 측정 방식을 통일
- 또는 각각의 사용 이유 명시

**영향도**: 낮음 - 기능에는 영향 없으나 일관성 측면에서 검토 필요

---

## 데이터 수집 효율성 문제

### 11. 태그 설정 시점의 차이

**문제점**:
- Usecase: Activity 생성 후 개별 `SetTag()` 호출
- IAdapter: Activity 생성 시 `ActivityTagsCollection`으로 한 번에 전달
- 성능 차이 발생 가능

**현재 상태**:
```csharp
// Usecase
using Activity? activity = _activitySource.StartActivity(...);
SetRequestTags(activity, ...);  // 개별 SetTag() 호출

// IAdapter
ActivityTagsCollection tags = new ActivityTagsCollection { ... };
Activity? activity = _activitySource.StartActivity(..., tags, ...);  // 한 번에 전달
```

**개선 방안**:
- Usecase도 `ActivityTagsCollection` 사용으로 통일
- 또는 성능 차이 측정 및 문서화

**영향도**: 낮음 - 성능 최적화 측면에서 검토 필요

---

### 12. 에러 태그의 선택적 설정

**문제점**:
- Usecase는 에러 타입에 따라 다른 태그 설정
- 에러 타입이 많을수록 태그 구조가 복잡해짐

**현재 상태**:
```csharp
switch (error)
{
    case ErrorCodeExpected errorCodeExpected:
        SetErrorCodeExpectedTags(activity, errorCodeExpected);
        break;
    case ErrorCodeExceptional errorCodeExceptional:
        SetErrorCodeExceptionalTags(activity, errorCodeExceptional);
        break;
    case ManyErrors manyErrors:
        SetManyErrorsTags(activity, manyErrors);
        break;
    default:
        SetUnknownErrorTags(activity, error);
        break;
}
```

**개선 방안**:
- 공통 에러 태그 구조 정의
- 또는 에러 타입별 태그 구조 명시

**영향도**: 낮음 - 현재 동작하나 구조 복잡도 높음

---

## 우선순위별 개선 사항 요약

| 우선순위 | 개선 사항 | 데이터 관점 | 예상 영향 |
|---------|----------|------------|----------|
| 높음 | Activity 이름 형식 표준화 | 형식 일관성 | 파싱 호환성 향상 |
| 중간 | 태그 값 타입 통일 | 타입 일관성 | 쿼리/필터링 단순화 |
| 중간 | 에러 태그 구조 통일 | 구조 일관성 | 에러 분석 쿼리 통일 |
| 중간 | Request/Response 태그 통일 | 구조 일관성 | 통합 쿼리 작성 용이 |
| 중간 | Status 표현 방식 통일 | 구조 일관성 | 데이터 중복 제거 |
| 낮음 | Parent Context 결정 로직 단순화 | 구조 명확성 | Trace 계층 구조 예측 가능 |
| 낮음 | 시간 측정 방식 통일 | 타입 일관성 | 시간 데이터 일관성 |
| 낮음 | 태그 설정 방식 통일 | 성능 최적화 | 성능 향상 가능 |

## 권장 개선 방향

1. **Activity 이름 표준화**: 공백 제거 또는 표준 구분자 사용
2. **태그 값 타입 통일**: 모든 태그 값을 문자열로 통일 또는 숫자 태그 표준화
3. **태그 구조 통일**: Usecase/IAdapter 간 공통 태그 구조 통일
4. **에러 태그 통일**: Usecase/IAdapter 간 에러 태그 구조 통일
5. **Status 표현 통일**: 태그와 상태 코드 중 하나로 통일 또는 역할 명확히 구분
