# Metrics-Analysis.md 데이터 형식 개선 사항

이 문서는 `Metrics-Analysis.md`에서 발견된 **데이터 형식, 데이터 구조, 데이터 품질** 관점의 개선 사항을 정리합니다.

## 데이터 형식 일관성 문제

### 1. 메트릭 이름 형식의 불일치

**문제점**:
- Usecase Metrics: `application.usecase.{cqrs}.requests` (점 구분, 소문자)
- IAdapter Metrics: `adapter.{category}.op.request` (점 구분, 소문자, `.op` 포함)

**현재 상태**:
```
Usecase:   application.usecase.command.requests
IAdapter:  adapter.repository.op.request
```

**개선 방안**:
- 메트릭 이름 형식을 통일: `application.usecase.{cqrs}.requests` vs `application.adapter.{category}.requests`
- 또는 IAdapter의 `.op` 접미사 제거 또는 Usecase에도 추가

**영향도**: 높음 - 메트릭 이름 패턴 불일치로 인한 쿼리 복잡도 증가

---

### 2. 태그 키 형식의 일관성

**문제점**:
- 모든 태그 키가 소문자 + 점 구분 (`request.layer`, `request.category`)
- Prometheus 변환 시 언더스코어로 변환되지만 원본 형식이 일관적

**현재 상태**:
- 태그 키: `request.layer`, `request.category`, `request.handler.cqrs`, `request.handler`, `request.handler.method`
- 모두 소문자 + 점 구분으로 일관됨 ✅

**개선 방안**:
- 현재 상태 유지 (일관성 있음)
- 다만 문서에 Prometheus 변환 규칙 명시 필요

**영향도**: 낮음 - 현재 일관성 있으나 문서화 필요

---

### 3. 태그 값의 대소문자 불일치

**문제점**:
- `response.status` 태그 값: `Success` / `Failure` (대문자 시작)
- 다른 태그 값들도 대소문자 규칙이 일관되지 않을 수 있음

**현재 상태**:
- `request.layer`: `Application` / `Adapter` (대문자 시작)
- `request.category`: `Usecase` / `Repository` (대문자 시작)
- `response.status`: `Success` / `Failure` (대문자 시작)
- `request.handler.cqrs`: `Command` / `Query` (대문자 시작)

**개선 방안**:
- 모든 태그 값을 소문자로 통일 (Prometheus 권장사항)
- 또는 대문자 시작으로 통일 (현재 상태 유지)

**영향도**: 중간 - 태그 값 쿼리 시 대소문자 구분 필요

---

### 4. 단위(Unit) 표준화 문제

**문제점**:
- Counter 단위: `{request}`, `{response}` (커스텀 단위)
- Histogram 단위: `s` (초, 표준 단위)
- OpenTelemetry 표준 단위와의 일치 여부 불명확

**현재 상태**:
```csharp
// Counter
unit: "{request}"  // 커스텀
unit: "{response}" // 커스텀

// Histogram
unit: "s"  // 표준 (초)
```

**개선 방안**:
- OpenTelemetry 표준 단위 사용 검토
- 또는 커스텀 단위 사용 이유 명시

**영향도**: 낮음 - 기능에는 영향 없으나 표준 준수 측면에서 검토 필요

---

## 데이터 구조 문제

### 5. RequestCategory 변환 로직의 불일치

**문제점**:
- Metrics 이름: 소문자 변환 (`"Repository"` → `"repository"`)
- Meter 이름: PascalCase 유지 (`"Repository"` → `"Repository"`)
- 태그 값: 원본 값 그대로 (`"Repository"`)

**현재 상태**:
```csharp
// Metrics 이름 생성
string categoryLower = requestCategory.ToLower();  // "repository"
name: $"adapter.{categoryLower}.op.request"

// Meter 이름 생성
Meter meter = new($"{_serviceNamespace}.Adapter.{requestCategory}");  // "Repository"

// 태그 값
requestCategory  // "Repository" (원본)
```

**개선 방안**:
- 모든 곳에서 동일한 변환 규칙 적용
- 또는 변환 규칙을 명시적으로 문서화

**영향도**: 중간 - 변환 로직 불일치로 인한 혼란 가능성

---

### 6. 태그 구성의 불일치

**문제점**:
- Request Counter: `request.handler.method` 태그 포함
- Response Success/Failure Counter: `request.handler.method` 태그 제외
- Duration Histogram: `request.handler.method` 태그 포함

**현재 상태**:
```
Request Counter 태그:
  - request.layer
  - request.category
  - request.handler.cqrs (Usecase만)
  - request.handler
  - request.handler.method ✅

Response Success Counter 태그:
  - request.layer
  - request.category
  - request.handler.cqrs (Usecase만)
  - request.handler
  - response.status
  - request.handler.method ❌

Duration Histogram 태그:
  - request.layer
  - request.category
  - request.handler.cqrs (Usecase만)
  - request.handler
  - request.handler.method ✅
```

**개선 방안**:
- 모든 메트릭에 동일한 태그 세트 적용
- 또는 태그 제외 이유 명시

**영향도**: 중간 - 태그 불일치로 인한 쿼리 복잡도 증가

---

### 7. Usecase와 IAdapter의 태그 차이

**문제점**:
- Usecase: `request.handler.cqrs` 태그 포함
- IAdapter: `request.handler.cqrs` 태그 없음
- 태그 구조가 완전히 다름

**현재 상태**:
```
Usecase 태그:
  - request.layer: "Application"
  - request.category: "Usecase"
  - request.handler.cqrs: "Command" / "Query" ✅
  - request.handler: "CreateProductCommand"
  - request.handler.method: "Handle"

IAdapter 태그:
  - request.layer: "Adapter"
  - request.category: "Repository"
  - request.handler: "InMemoryProductRepository"
  - request.handler.method: "ExistsByName"
  - request.handler.cqrs: 없음 ❌
```

**개선 방안**:
- 공통 태그는 동일하게 유지하고, 계층별 고유 태그만 추가
- 또는 태그 차이를 명시적으로 문서화

**영향도**: 중간 - 태그 구조 차이로 인한 통합 쿼리 작성 어려움

---

## 데이터 품질 문제

### 8. 메트릭 카디널리티 관리 부재

**문제점**:
- `request.handler` 태그 값이 동적으로 생성됨
- Handler 클래스가 많아질수록 메트릭 카디널리티 폭발 가능
- 카디널리티 제한 또는 관리 전략 없음

**현재 상태**:
- 각 Usecase Handler마다 별도의 메트릭 시계열 생성
- Handler 개수에 비례하여 메트릭 수 증가

**개선 방안**:
- 카디널리티 제한 설정 (예: 최대 Handler 수)
- 또는 Handler별 메트릭을 선택적으로 수집하는 옵션 제공

**영향도**: 높음 - 대규모 시스템에서 메트릭 폭발 가능성

---

### 9. Duration 단위의 불일치 가능성

**문제점**:
- Duration Histogram의 단위가 `s` (초)로 명시되어 있음
- 실제 기록 시 밀리초를 초로 변환 (`elapsed / 1000.0`)
- 변환 로직이 일관되게 적용되는지 확인 필요

**현재 상태**:
```csharp
// Usecase
durationHistogram.Record(elapsed / 1000.0, tags);  // 밀리초 → 초

// IAdapter
DurationHistogram.Record(elapsed / 1000.0, tags);  // 밀리초 → 초
```

**개선 방안**:
- 변환 로직 일관성 검증
- 또는 밀리초 단위로 통일하여 변환 제거

**영향도**: 중간 - 단위 불일치 시 데이터 해석 오류

---

### 10. Counter와 Histogram의 태그 불일치

**문제점**:
- Request Counter와 Duration Histogram의 태그가 동일해야 하는데 차이가 있을 수 있음
- Response Success/Failure Counter는 `request.handler.method` 태그가 없음

**개선 방안**:
- 모든 관련 메트릭에 동일한 태그 세트 적용
- 또는 태그 차이를 의도적으로 설계한 경우 명시

**영향도**: 중간 - 메트릭 간 조인/집계 시 태그 불일치 문제

---

## 데이터 수집 효율성 문제

### 11. Meter 생성 전략의 차이

**문제점**:
- Usecase: 단일 Meter (`{ServiceNamespace}.Application`)
- IAdapter: RequestCategory별 Meter 분리 (`{ServiceNamespace}.Adapter.{RequestCategory}`)

**현재 상태**:
```
Usecase Meter:   Cqrs03Functional.Demo.Application (1개)
IAdapter Meter:  Cqrs03Functional.Demo.Adapter.Repository (N개)
                 Cqrs03Functional.Demo.Adapter.Db (N개)
                 ...
```

**개선 방안**:
- Meter 생성 전략 통일 검토
- 또는 전략 차이의 이유 명시

**영향도**: 낮음 - 기능에는 영향 없으나 일관성 측면에서 검토 필요

---

### 12. 지연 초기화(Lazy Initialization)의 데이터 손실 가능성

**문제점**:
- IAdapter Metrics는 첫 요청 시점에 초기화됨
- 초기화 전 요청은 메트릭 수집되지 않을 수 있음

**현재 상태**:
```csharp
private void EnsureMetricsForCategory(string requestCategory)
{
    if (!_metrics.ContainsKey(requestCategory))
    {
        InitializeMetricsForCategory(requestCategory);  // 첫 요청 시 초기화
    }
}
```

**개선 방안**:
- 초기화 전 요청 처리 방법 명시
- 또는 사전 초기화 옵션 제공

**영향도**: 낮음 - 일반적으로 첫 요청은 매우 빠르게 발생

---

## 우선순위별 개선 사항 요약

| 우선순위 | 개선 사항 | 데이터 관점 | 예상 영향 |
|---------|----------|------------|----------|
| 높음 | 메트릭 이름 형식 통일 | 형식 일관성 | 쿼리 패턴 통일 |
| 높음 | 카디널리티 관리 전략 | 데이터 품질 | 메트릭 폭발 방지 |
| 중간 | 태그 값 대소문자 통일 | 값 일관성 | 쿼리 시 대소문자 구분 불필요 |
| 중간 | RequestCategory 변환 로직 통일 | 구조 일관성 | 변환 로직 혼란 방지 |
| 중간 | 태그 구성 일관성 | 구조 일관성 | 메트릭 간 조인 용이 |
| 중간 | Duration 단위 변환 검증 | 타입 명확성 | 데이터 해석 오류 방지 |
| 낮음 | 단위 표준화 검토 | 형식 표준화 | OpenTelemetry 표준 준수 |
| 낮음 | Meter 생성 전략 통일 | 구조 일관성 | 일관된 아키텍처 |

## 권장 개선 방향

1. **메트릭 이름 통일**: Usecase/IAdapter 간 메트릭 이름 패턴 통일
2. **태그 값 표준화**: 모든 태그 값을 소문자로 통일 (Prometheus 권장)
3. **태그 구성 통일**: 관련 메트릭 간 태그 세트 통일
4. **카디널리티 관리**: 동적 태그 값에 대한 카디널리티 제한 설정
5. **변환 로직 명시**: RequestCategory 등 변환 로직의 일관성 확보 및 문서화
