# Cqrs03Functional Metrics 형식 분석

이 문서는 `Cqrs03Functional.Demo` 애플리케이션에서 수집되는 Metrics를 분석하여, **Usecase**와 **IAdapter** 인터페이스 중심의 두 가지 Metrics 범주로 구분하고 각각의 메트릭 이름, 타입, 단위, 태그를 정리합니다.

## 목차

1. [Metrics 범주 개요](#metrics-범주-개요)
2. [Usecase Metrics 형식](#usecase-metrics-형식)
3. [IAdapter Metrics 형식](#iadapter-metrics-형식)
4. [Metrics 태그 매핑](#metrics-태그-매핑)

---

## Metrics 범주 개요

Cqrs03Functional 애플리케이션에서 수집되는 Metrics는 크게 두 가지 범주로 구분됩니다:

### 1. Usecase Metrics
- **생성 위치**: `UsecaseMetricPipeline<TRequest, TResponse>`
- **대상**: CQRS Command/Query Handler (Usecase)
- **Meter 이름**: `{ServiceNamespace}.Application`
- **특징**: CQRS 타입(Command/Query)별로 메트릭을 구분하여 수집

### 2. IAdapter Metrics
- **생성 위치**: 소스 생성기로 자동 생성된 `*Pipeline` 클래스에서 `IAdapterMetric` 호출
- **대상**: `IAdapter` 인터페이스를 구현한 Repository 등의 Adapter
- **Meter 이름**: `{ServiceNamespace}.Adapter.{RequestCategory}`
- **특징**: RequestCategory별로 Meter와 Metrics를 분리하여 관리

---

## Usecase Metrics 형식

Usecase Metrics는 `UsecaseMetricPipeline<TRequest, TResponse>`에서 자동으로 수집됩니다. CQRS 타입(Command/Query)별로 메트릭 이름이 달라집니다.

### 1. Request Counter (요청 수)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `application.usecase.{cqrs}.requests` |
| **메트릭 타입** | Counter<long> |
| **단위** | `{request}` |
| **설명** | Total number of {requestHandler} requests |
| **예시** | `application.usecase.command.requests`, `application.usecase.query.requests` |

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Application` | 요청 계층 (고정값) | "Application" |
| `request.category` | `Usecase` | 요청 카테고리 (고정값) | "Usecase" |
| `request.handler.cqrs` | `Command` / `Query` | CQRS 타입 | "Command", "Query" |
| `request.handler` | Handler 클래스 이름 | Handler 클래스 이름 | "CreateProductCommand" |
| `request.handler.method` | `Handle` | Handler 메서드 이름 (고정값) | "Handle" |

#### 수집 시점
- 요청이 들어올 때마다 `requestCounter.Add(1, tags)` 호출

---

### 2. Response Success Counter (성공 응답 수)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `application.usecase.{cqrs}.responses.success` |
| **메트릭 타입** | Counter<long> |
| **단위** | `{response}` |
| **설명** | Total number of successful {requestHandler} responses |
| **예시** | `application.usecase.command.responses.success`, `application.usecase.query.responses.success` |

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Application` | 요청 계층 (고정값) | "Application" |
| `request.category` | `Usecase` | 요청 카테고리 (고정값) | "Usecase" |
| `request.handler.cqrs` | `Command` / `Query` | CQRS 타입 | "Command", "Query" |
| `request.handler` | Handler 클래스 이름 | Handler 클래스 이름 | "CreateProductCommand" |
| `response.status` | `Success` | 응답 상태 (고정값) | "Success" |

#### 수집 시점
- 응답이 성공(`response.IsSucc == true`)일 때 `responseSuccessCounter.Add(1, tags)` 호출

---

### 3. Response Failure Counter (실패 응답 수)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `application.usecase.{cqrs}.responses.failure` |
| **메트릭 타입** | Counter<long> |
| **단위** | `{response}` |
| **설명** | Total number of failed {requestHandler} responses |
| **예시** | `application.usecase.command.responses.failure`, `application.usecase.query.responses.failure` |

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Application` | 요청 계층 (고정값) | "Application" |
| `request.category` | `Usecase` | 요청 카테고리 (고정값) | "Usecase" |
| `request.handler.cqrs` | `Command` / `Query` | CQRS 타입 | "Command", "Query" |
| `request.handler` | Handler 클래스 이름 | Handler 클래스 이름 | "CreateProductCommand" |
| `response.status` | `Failure` | 응답 상태 (고정값) | "Failure" |

#### 수집 시점
- 응답이 실패(`response.IsFail == true`)일 때 `responseFailureCounter.Add(1, tags)` 호출

---

### 4. Duration Histogram (처리 시간)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `application.usecase.{cqrs}.duration` |
| **메트릭 타입** | Histogram<double> |
| **단위** | `s` (초) |
| **설명** | Duration of {requestHandler} request processing in seconds |
| **예시** | `application.usecase.command.duration`, `application.usecase.query.duration` |

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Application` | 요청 계층 (고정값) | "Application" |
| `request.category` | `Usecase` | 요청 카테고리 (고정값) | "Usecase" |
| `request.handler.cqrs` | `Command` / `Query` | CQRS 타입 | "Command", "Query" |
| `request.handler` | Handler 클래스 이름 | Handler 클래스 이름 | "CreateProductCommand" |
| `request.handler.method` | `Handle` | Handler 메서드 이름 (고정값) | "Handle" |

#### 수집 시점
- 요청 처리 완료 후 `durationHistogram.Record(elapsed / 1000.0, tags)` 호출
- `elapsed`는 밀리초 단위이므로 초 단위로 변환하여 기록

---

## IAdapter Metrics 형식

IAdapter Metrics는 소스 생성기(`AdapterPipelineGenerator`)에 의해 자동 생성된 Pipeline 클래스에서 `IAdapterMetric` 인터페이스를 통해 수집됩니다. `[GeneratePipeline]` 애트리뷰트가 적용된 클래스에 대해 자동으로 Metrics 수집이 활성화됩니다.

**중요**: IAdapter Metrics는 **RequestCategory별로 Meter와 Metrics가 분리**되어 관리됩니다.

### 1. Request Counter (요청 수)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `adapter.{category}.op.request` |
| **메트릭 타입** | Counter<long> |
| **단위** | `{request}` |
| **설명** | Total number of {requestCategory} op requests |
| **예시** | `adapter.repository.op.request`, `adapter.db.op.request` |
| **참고** | `{category}`는 RequestCategory를 소문자로 변환한 값 (예: "Repository" → "repository")

#### Meter 정보

| 항목 | 값 |
|------|-----|
| **Meter 이름** | `{ServiceNamespace}.Adapter.{RequestCategory}` |
| **예시** | `Cqrs03Functional.Demo.Adapter.Repository` |

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Adapter` | 요청 계층 (고정값) | "Adapter" |
| `request.category` | RequestCategory 값 | 요청 카테고리 (IAdapter.RequestCategory 속성 값) | "Repository", "Db" |
| `request.handler` | Handler 클래스 이름 | Handler 클래스 이름 | "InMemoryProductRepository" |
| `request.handler.method` | 호출된 메서드 이름 | 호출된 메서드 이름 | "ExistsByName", "Create", "GetAll" |

#### 수집 시점
- 요청이 들어올 때마다 `_adapterMetric.Request(activity, requestCategory, requestHandler, requestHandlerMethod, startTime)` 호출

---

### 2. Response Success Counter (성공 응답 수)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `adapter.{category}.op.response.success` |
| **메트릭 타입** | Counter<long> |
| **단위** | `{response}` |
| **설명** | Total number of {requestCategory} op response success |
| **예시** | `adapter.repository.op.response.success`, `adapter.db.op.response.success` |
| **참고** | `{category}`는 RequestCategory를 소문자로 변환한 값 (예: "Repository" → "repository") |

#### Meter 정보

| 항목 | 값 |
|------|-----|
| **Meter 이름** | `{ServiceNamespace}.Adapter.{RequestCategory}` |
| **예시** | `Cqrs03Functional.Demo.Adapter.Repository` |
| **참고** | RequestCategory는 `GetRequestCategoryPascalCase()`를 통해 PascalCase로 변환됨 (예: "Repository" → "Repository")

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Adapter` | 요청 계층 (고정값) | "Adapter" |
| `request.category` | RequestCategory 값 | 요청 카테고리 (IAdapter.RequestCategory 속성 값) | "Repository", "Db" |
| `request.handler` | Handler 클래스 이름 | Activity에서 추출한 Handler 클래스 이름 | "InMemoryProductRepository" |
| `request.handler.method` | 호출된 메서드 이름 | Activity에서 추출한 메서드 이름 | "ExistsByName", "Create" |

**참고**: 태그 값은 Activity에서 `GetTagItem()`을 통해 추출됩니다.

#### 수집 시점
- 응답이 성공일 때 `_adapterMetric.ResponseSuccess(activity, requestCategory, elapsed)` 호출

---

### 3. Response Failure Counter (실패 응답 수)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `adapter.{category}.op.response.failure` |
| **메트릭 타입** | Counter<long> |
| **단위** | `{response}` |
| **설명** | Total number of {requestCategory} op response failure |
| **예시** | `adapter.repository.op.response.failure`, `adapter.db.op.response.failure` |
| **참고** | `{category}`는 RequestCategory를 소문자로 변환한 값 (예: "Repository" → "repository") |

#### Meter 정보

| 항목 | 값 |
|------|-----|
| **Meter 이름** | `{ServiceNamespace}.Adapter.{RequestCategory}` |
| **예시** | `Cqrs03Functional.Demo.Adapter.Repository` |
| **참고** | RequestCategory는 `GetRequestCategoryPascalCase()`를 통해 PascalCase로 변환됨 (예: "Repository" → "Repository")

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Adapter` | 요청 계층 (고정값) | "Adapter" |
| `request.category` | RequestCategory 값 | 요청 카테고리 (IAdapter.RequestCategory 속성 값) | "Repository", "Db" |
| `request.handler` | Handler 클래스 이름 | Activity에서 추출한 Handler 클래스 이름 | "InMemoryProductRepository" |
| `request.handler.method` | 호출된 메서드 이름 | Activity에서 추출한 메서드 이름 | "GetById" |

**참고**: 태그 값은 Activity에서 `GetTagItem()`을 통해 추출됩니다.

#### 수집 시점
- 응답이 실패일 때 `_adapterMetric.ResponseFailure(activity, requestCategory, elapsed, error)` 호출

---

### 4. Duration Histogram (처리 시간)

#### 메트릭 정보

| 항목 | 값 |
|------|-----|
| **메트릭 이름** | `adapter.{category}.op.duration` |
| **메트릭 타입** | Histogram<double> |
| **단위** | `s` (초) |
| **설명** | Duration of {requestCategory} op execution in seconds |
| **예시** | `adapter.repository.op.duration`, `adapter.db.op.duration` |
| **참고** | `{category}`는 RequestCategory를 소문자로 변환한 값 (예: "Repository" → "repository") |

#### Meter 정보

| 항목 | 값 |
|------|-----|
| **Meter 이름** | `{ServiceNamespace}.Adapter.{RequestCategory}` |
| **예시** | `Cqrs03Functional.Demo.Adapter.Repository` |
| **참고** | RequestCategory는 `GetRequestCategoryPascalCase()`를 통해 PascalCase로 변환됨 (예: "Repository" → "Repository")

#### 태그 (Labels/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Adapter` | 요청 계층 (고정값) | "Adapter" |
| `request.category` | RequestCategory 값 | 요청 카테고리 (IAdapter.RequestCategory 속성 값) | "Repository", "Db" |
| `request.handler` | Handler 클래스 이름 | Activity에서 추출한 Handler 클래스 이름 | "InMemoryProductRepository" |
| `request.handler.method` | 호출된 메서드 이름 | Activity에서 추출한 메서드 이름 | "ExistsByName", "Create" |

**참고**: 태그 값은 Activity에서 `GetTagItem()`을 통해 추출됩니다.

#### 수집 시점
- 성공/실패 응답 모두에서 `DurationHistogram.Record(elapsed / 1000.0, tags)` 호출
- `elapsed`는 밀리초 단위이므로 초 단위로 변환하여 기록

---

## Metrics 태그 매핑

### Usecase Metrics 태그 매핑

| 메트릭 타입 | 공통 태그 | 추가 태그 |
|------------|----------|----------|
| **Request Counter** | `request.layer`, `request.category`, `request.handler.cqrs`, `request.handler`, `request.handler.method` | - |
| **Response Success Counter** | `request.layer`, `request.category`, `request.handler.cqrs`, `request.handler` | `response.status: Success` |
| **Response Failure Counter** | `request.layer`, `request.category`, `request.handler.cqrs`, `request.handler` | `response.status: Failure` |
| **Duration Histogram** | `request.layer`, `request.category`, `request.handler.cqrs`, `request.handler`, `request.handler.method` | - |

### IAdapter Metrics 태그 매핑

| 메트릭 타입 | 공통 태그 | 추가 태그 |
|------------|----------|----------|
| **Request Counter** | `request.layer`, `request.category`, `request.handler`, `request.handler.method` | - |
| **Response Success Counter** | `request.layer`, `request.category`, `request.handler`, `request.handler.method` | - |
| **Response Failure Counter** | `request.layer`, `request.category`, `request.handler`, `request.handler.method` | - |
| **Duration Histogram** | `request.layer`, `request.category`, `request.handler`, `request.handler.method` | - |

---

## 참고사항

### Usecase Metrics 특징

1. **CQRS 타입별 분리**: Command와 Query에 대해 별도의 메트릭 이름 사용
2. **Meter 단일화**: 모든 Usecase가 동일한 Meter(`{ServiceNamespace}.Application`) 사용
3. **성공/실패 분리**: Response Success와 Failure를 별도의 Counter로 분리하여 성능과 쿼리 효율성 향상
4. **IsSucc/IsFail 패턴**: `FinResponse<T>` 타입의 `IsSucc`/`IsFail` 패턴을 사용하여 안전하게 메트릭 기록
5. **시간 단위 변환**: Duration은 밀리초를 초로 변환하여 기록

### IAdapter Metrics 특징

1. **RequestCategory별 Meter 분리**: 각 RequestCategory마다 별도의 Meter 생성
   - 예: `{ServiceNamespace}.Adapter.Repository`, `{ServiceNamespace}.Adapter.Db`
2. **지연 초기화**: RequestCategory별 Metrics는 첫 요청 시점에 초기화 (Lazy Initialization)
3. **Activity 기반 태그 추출**: Response Success/Failure/Duration의 태그는 Activity에서 추출
4. **성공/실패 분리**: Response Success와 Failure를 별도의 Counter로 분리
5. **시간 단위 변환**: Duration은 밀리초를 초로 변환하여 기록

### 메트릭 이름 규칙

#### Usecase Metrics
- **Request**: `application.usecase.{cqrs}.requests`
- **Response Success**: `application.usecase.{cqrs}.responses.success`
- **Response Failure**: `application.usecase.{cqrs}.responses.failure`
- **Duration**: `application.usecase.{cqrs}.duration`

#### IAdapter Metrics
- **Request**: `adapter.{category}.op.request`
- **Response Success**: `adapter.{category}.op.response.success`
- **Response Failure**: `adapter.{category}.op.response.failure`
- **Duration**: `adapter.{category}.op.duration`

**참고**: Prometheus는 Counter 메트릭에 자동으로 `_total` 접미사를 추가합니다.
- `application.usecase.command.requests` → `application_usecase_command_requests_total`
- `adapter.repository.op.request` → `adapter_repository_op_request_total`

### Prometheus 쿼리 예시

#### Usecase Metrics 쿼리

**Command 요청 수 (1분당)**
```
rate(application_usecase_command_requests_total[1m])
```

**Query 성공률**
```
rate(application_usecase_query_responses_success_total[5m]) / rate(application_usecase_query_requests_total[5m])
```

**Command 처리 시간 (P95)**
```
histogram_quantile(0.95, rate(application_usecase_command_duration_bucket[5m]))
```

#### IAdapter Metrics 쿼리

**Repository 요청 수 (1분당)**
```
rate(adapter_repository_op_request_total[1m])
```

**Repository 성공률**
```
rate(adapter_repository_op_response_success_total[5m]) / rate(adapter_repository_op_request_total[5m])
```

**Repository 처리 시간 (P99)**
```
histogram_quantile(0.99, rate(adapter_repository_op_duration_bucket[5m]))
```

---

## 관련 코드 위치

- **Usecase Metrics**: `Src/Functorium/Applications/Pipelines/UsecaseMetricPipeline.cs`
- **Usecase Metrics 필드 정의**: `Src/Functorium/Applications/Observabilities/UsecaseFields.cs`
- **IAdapter Metrics 구현**: `Src/Functorium/Adapters/Observabilities/Metrics/AdapterMetric.cs`
- **IAdapter Pipeline 생성기**: `Src/Functorium.Adapters.SourceGenerator/AdapterPipelineGenerator.cs`
- **Observability 필드 정의**: `Src/Functorium/Adapters/Observabilities/ObservabilityFields.cs`

