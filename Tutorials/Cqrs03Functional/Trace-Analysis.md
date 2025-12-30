# Cqrs03Functional Trace 형식 분석

이 문서는 `Cqrs03Functional.Demo` 애플리케이션에서 수집되는 Trace(분산 추적)를 분석하여, **Usecase**와 **IAdapter** 인터페이스 중심의 두 가지 Trace 범주로 구분하고 각각의 Activity 이름, 태그, 상태를 정리합니다.

## 목차

1. [Trace 범주 개요](#trace-범주-개요)
2. [Usecase Trace 형식](#usecase-trace-형식)
3. [IAdapter Trace 형식](#iadapter-trace-형식)
4. [Trace 태그 매핑](#trace-태그-매핑)

---

## Trace 범주 개요

Cqrs03Functional 애플리케이션에서 수집되는 Trace는 크게 두 가지 범주로 구분됩니다:

### 1. Usecase Trace
- **생성 위치**: `UsecaseTracePipeline<TRequest, TResponse>`
- **대상**: CQRS Command/Query Handler (Usecase)
- **ActivitySource 이름**: `{ServiceName}` (예: `Cqrs03Functional.Demo`)
- **ActivityKind**: `Internal`
- **특징**: CQRS 타입(Command/Query)별로 Activity 이름이 구분됨

### 2. IAdapter Trace
- **생성 위치**: 소스 생성기로 자동 생성된 `*Pipeline` 클래스에서 `IAdapterTrace` 호출
- **대상**: `IAdapter` 인터페이스를 구현한 Repository 등의 Adapter
- **ActivitySource 이름**: `{ServiceName}` (예: `Cqrs03Functional.Demo`) - Usecase와 동일한 ActivitySource 사용
- **ActivityKind**: `Internal`
- **특징**: RequestCategory별로 Activity 이름이 구분됨

---

## Usecase Trace 형식

Usecase Trace는 `UsecaseTracePipeline<TRequest, TResponse>`에서 자동으로 수집됩니다. CQRS 타입(Command/Query)별로 Activity 이름이 달라집니다.

### 1. Activity 생성

#### Activity 이름 (DisplayName)

```
{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod}
```

**예시**:
- `Application Usecase.Command CreateProductCommand.Handle`
- `Application Usecase.Query GetProductByIdQuery.Handle`

#### ActivitySource 정보

| 항목 | 값 |
|------|-----|
| **ActivitySource 이름** | `{ServiceName}` (예: `Cqrs03Functional.Demo`) |
| **ActivitySource 버전** | `{ServiceVersion}` (appsettings.json에서 설정) |
| **ActivityKind** | `Internal` |
| **Parent Context** | `Activity.Current?.Context` (부모 Activity가 있으면 사용) |

#### 생성 시점
- 요청이 들어올 때 `_activitySource.StartActivity()` 호출
- Activity 생성 실패 시 추적 없이 다음 Pipeline으로 진행

---

### 2. Request 태그 (요청 시점)

#### 태그 (Attributes/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Application` | 요청 계층 (고정값) | "Application" |
| `request.category` | `Usecase` | 요청 카테고리 (고정값) | "Usecase" |
| `request.handler.cqrs` | `Command` / `Query` | CQRS 타입 | "Command", "Query" |
| `request.handler` | Handler 클래스 이름 | Handler 클래스 이름 | "CreateProductCommand" |

#### 설정 시점
- Activity 생성 직후 `SetRequestTags()` 메서드에서 설정

---

### 3. Response 태그 (응답 시점)

#### 공통 태그

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `response.elapsed` | double | 경과 시간 (밀리초) | 200.4511 |
| `response.status` | `Success` / `Failure` | 응답 상태 | "Success", "Failure" |

#### 성공 응답 (Success)

**Activity 상태**:
- `ActivityStatusCode`: `Ok`

**태그**:
- `response.status`: `Success`
- `response.elapsed`: 경과 시간 (밀리초)

#### 실패 응답 (Failure)

**Activity 상태**:
- `ActivityStatusCode`: `Error`
- `StatusDescription`: 에러 메시지 (예: "Unknown error")

**태그**:
- `response.status`: `Failure`
- `response.elapsed`: 경과 시간 (밀리초)
- 에러 타입에 따라 추가 태그 설정 (아래 참조)

#### 에러 태그 (Error Tags)

에러 타입에 따라 추가 태그가 설정됩니다:

##### ErrorCodeExpected

| 태그 키 | 태그 값 | 설명 |
|---------|---------|------|
| `error.type` | `ErrorCodeExpected` | 에러 타입 |
| `error.code` | ErrorCode 문자열 | 에러 코드 (예: "ApplicationErrors.UsecaseValidationPipeline.Validator") |
| `error.message` | string | 에러 메시지 |

##### ErrorCodeExceptional

| 태그 키 | 태그 값 | 설명 |
|---------|---------|------|
| `error.type` | `ErrorCodeExceptional` | 에러 타입 |
| `error.code` | ErrorCode 문자열 | 에러 코드 |
| `error.message` | string | 에러 메시지 |

##### ManyErrors

| 태그 키 | 태그 값 | 설명 |
|---------|---------|------|
| `error.type` | `ManyErrors` | 에러 타입 |
| `error.count` | int | 에러 개수 |

##### 기타 에러 타입

| 태그 키 | 태그 값 | 설명 |
|---------|---------|------|
| `error.type` | 에러 타입 이름 | `error.GetType().Name` |
| `error.message` | string | 에러 메시지 |

#### 설정 시점
- 응답 처리 완료 후 `SetResponseTags()` 메서드에서 설정
- `response.IsSucc` / `response.IsFail` 패턴을 사용하여 성공/실패 구분

---

## IAdapter Trace 형식

IAdapter Trace는 소스 생성기(`AdapterPipelineGenerator`)에 의해 자동 생성된 Pipeline 클래스에서 `IAdapterTrace` 인터페이스를 통해 수집됩니다. `[GeneratePipeline]` 애트리뷰트가 적용된 클래스에 대해 자동으로 Trace 수집이 활성화됩니다.

### 1. Activity 생성

#### Activity 이름 (DisplayName)

```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod}
```

**예시**:
- `Adapter Repository InMemoryProductRepository.ExistsByName`
- `Adapter Repository InMemoryProductRepository.Create`
- `Adapter Repository InMemoryProductRepository.GetAll`

#### ActivitySource 정보

| 항목 | 값 |
|------|-----|
| **ActivitySource 이름** | `{ServiceName}` (예: `Cqrs03Functional.Demo`) - Usecase와 동일한 ActivitySource 사용 |
| **ActivityKind** | `Internal` |
| **Parent Context** | 우선순위: 1) `TraceParentActivityHolder.GetCurrent()` (Traverse Activity), 2) `parentContext` (Usecase Activity) |
| **StartTime** | `DateTimeOffset.UtcNow` (요청 시작 시점) |

**참고**: Parent Context는 다음 우선순위로 결정됩니다:
1. AsyncLocal에 저장된 Traverse Activity (FinT의 AsyncLocal 복원 문제를 우회하기 위함)
2. 캡처된 Usecase Activity의 Context

#### 생성 시점
- 요청이 들어올 때 `_adapterTrace.Request(parentContext, requestCategory, requestHandler, requestHandlerMethod, startTime)` 호출
- Activity 생성 시 태그들을 `ActivityTagsCollection`으로 한 번에 전달하여 성능 최적화

---

### 2. Request 태그 (요청 시점)

#### 태그 (Attributes/Tags)

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `request.layer` | `Adapter` | 요청 계층 (고정값) | "Adapter" |
| `request.category` | RequestCategory 값 | 요청 카테고리 (IAdapter.RequestCategory 속성 값) | "Repository", "Db" |
| `request.handler` | Handler 클래스 이름 | Handler 클래스 이름 | "InMemoryProductRepository" |
| `request.handler.method` | 호출된 메서드 이름 | 호출된 메서드 이름 | "ExistsByName", "Create", "GetAll" |

#### 설정 시점
- Activity 생성 시 `ActivityTagsCollection`으로 한 번에 전달하여 설정

---

### 3. Response 태그 (응답 시점)

#### 공통 태그

| 태그 키 | 태그 값 | 설명 | 예시 |
|---------|---------|------|------|
| `response.elapsed` | double | 경과 시간 (밀리초) | 96.0635 |

#### 성공 응답 (Success)

**Activity 상태**:
- `ActivityStatusCode`: `Ok`

**태그**:
- `response.elapsed`: 경과 시간 (밀리초)

#### 실패 응답 (Failure)

**Activity 상태**:
- `ActivityStatusCode`: `Error`
- `StatusDescription`: 에러 메시지 (`error.Message`)

**태그**:
- `response.elapsed`: 경과 시간 (밀리초)

**참고**: IAdapter Trace는 Usecase와 달리 에러 상세 정보를 태그로 기록하지 않습니다. 에러 정보는 Log를 통해 확인할 수 있습니다.

#### 설정 시점
- 응답 처리 완료 후 `_adapterTrace.ResponseSuccess()` 또는 `_adapterTrace.ResponseFailure()` 호출

---

## Trace 태그 매핑

### Usecase Trace 태그 매핑

| 태그 타입 | 공통 태그 | 성공 시 추가 | 실패 시 추가 |
|----------|----------|------------|------------|
| **Request** | `request.layer`, `request.category`, `request.handler.cqrs`, `request.handler` | - | - |
| **Response** | `response.elapsed`, `response.status` | `response.status: Success`, `ActivityStatusCode: Ok` | `response.status: Failure`, `ActivityStatusCode: Error`, `StatusDescription`, 에러 타입별 태그 |

### IAdapter Trace 태그 매핑

| 태그 타입 | 공통 태그 | 성공 시 추가 | 실패 시 추가 |
|----------|----------|------------|------------|
| **Request** | `request.layer`, `request.category`, `request.handler`, `request.handler.method` | - | - |
| **Response** | `response.elapsed` | `ActivityStatusCode: Ok` | `ActivityStatusCode: Error`, `StatusDescription` |

---

## 참고사항

### Usecase Trace 특징

1. **CQRS 타입별 구분**: Activity 이름에 Command/Query가 포함됨
2. **부모 Activity 지원**: `Activity.Current?.Context`를 사용하여 부모 Activity와 연결
3. **에러 상세 정보**: 에러 타입에 따라 상세한 에러 태그 설정
4. **IsSucc/IsFail 패턴**: `FinResponse<T>` 타입의 `IsSucc`/`IsFail` 패턴을 사용하여 안전하게 상태 설정
5. **Activity 생성 실패 처리**: Activity 생성 실패 시 추적 없이 다음 Pipeline으로 진행

### IAdapter Trace 특징

1. **RequestCategory별 구분**: Activity 이름에 RequestCategory가 포함됨
2. **부모 Context 우선순위**: Traverse Activity를 우선 사용하여 FinT의 AsyncLocal 복원 문제 우회
3. **성능 최적화**: Activity 생성 시 태그를 `ActivityTagsCollection`으로 한 번에 전달
4. **에러 정보 간소화**: 에러 상세 정보는 태그로 기록하지 않고 StatusDescription만 설정
5. **StartTime 정확성**: `DateTimeOffset.UtcNow`를 사용하여 정확한 시작 시점 기록

### Activity 이름 규칙

#### Usecase Trace
- **형식**: `{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod}`
- **예시**: `Application Usecase.Command CreateProductCommand.Handle`

#### IAdapter Trace
- **형식**: `{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod}`
- **예시**: `Adapter Repository InMemoryProductRepository.ExistsByName`

### Trace 계층 구조

```
Usecase Activity (Application Usecase.Command CreateProductCommand.Handle)
  └── IAdapter Activity (Adapter Repository InMemoryProductRepository.Create)
        └── IAdapter Activity (Adapter Repository InMemoryProductRepository.GetById)
```

**참고**: IAdapter Activity는 Usecase Activity의 자식으로 생성되며, 여러 IAdapter 호출이 중첩될 수 있습니다.

### OpenTelemetry 표준 태그

다음 태그들은 OpenTelemetry 표준을 따릅니다:

- `request.layer`: 요청 계층 구분
- `request.category`: 요청 카테고리 구분
- `request.handler`: 핸들러 식별
- `request.handler.method`: 메서드 식별
- `response.status`: 응답 상태
- `response.elapsed`: 응답 시간
- `error.type`: 에러 타입
- `error.code`: 에러 코드
- `error.message`: 에러 메시지

### ActivitySource 등록

ActivitySource는 OpenTelemetry 설정에서 자동으로 등록됩니다:

- **Usecase**: `{ServiceName}*` 패턴으로 등록 (예: `Cqrs03Functional.Demo*`)
- **IAdapter**: `Functorium.*` 패턴으로 등록 (Usecase와 동일한 ActivitySource 사용)

**참고**: 
- Usecase와 IAdapter 모두 동일한 ActivitySource(`{ServiceName}`)를 사용합니다.
- ActivitySource가 등록되지 않으면 Activity가 생성되지 않습니다 (`StartActivity()`가 `null` 반환).

---

## 관련 코드 위치

- **Usecase Trace**: `Src/Functorium/Applications/Pipelines/UsecaseTracePipeline.cs`
- **IAdapter Trace 구현**: `Src/Functorium/Adapters/Observabilities/Tracing/AdapterTrace.cs`
- **IAdapter Pipeline 생성기**: `Src/Functorium.Adapters.SourceGenerator/AdapterPipelineGenerator.cs`
- **Observability 필드 정의**: `Src/Functorium/Adapters/Observabilities/ObservabilityFields.cs`
- **Trace Parent Context Holder**: `Src/Functorium/Applications/Observabilities/TraceParentContextHolder.cs`
- **Trace Parent Activity Holder**: `Src/Functorium/Applications/Observabilities/TraceParentActivityHolder.cs`

