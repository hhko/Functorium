# Functorium Tracing 매뉴얼

Functorium 프레임워크에서 분산 추적을 활용하여
요청의 전체 여정을 시각화하고 성능 병목을 찾는 방법을 알아봅니다.

## 목차

- [들어가며](#들어가며)
- [분산 추적의 기초](#분산-추적의-기초)
- [Functorium 트레이싱 아키텍처](#functorium-트레이싱-아키텍처)
- [Span 구조 이해하기](#span-구조-이해하기)
- [태그 시스템 상세 가이드](#태그-시스템-상세-가이드)
- [Application Layer 트레이싱](#application-layer-트레이싱)
- [Adapter Layer 트레이싱](#adapter-layer-트레이싱)
- [DomainEvent 트레이싱](#domainevent-트레이싱)
- [에러 트레이싱 이해하기](#에러-트레이싱-이해하기)
- [Trace 분석하기](#trace-분석하기)
- [실습: 병목 구간 찾기](#실습-병목-구간-찾기)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

---

## 들어가며

"이 요청이 왜 3초나 걸렸을까?"
"어떤 서비스에서 지연이 발생했는가?"
"실패한 요청이 어디서 실패했는가?"

현대의 애플리케이션은 여러 서비스와 컴포넌트가 협력하여 동작합니다. 하나의 HTTP 요청이 처리되는 동안 데이터베이스 조회, 외부 API 호출, 캐시 접근 등 다양한 작업이 순차적 또는 병렬로 실행됩니다. 이러한 환경에서 "어디서 느려졌는가?"라는 질문에 답하는 것은 로그나 메트릭만으로는 매우 어렵습니다.

**분산 추적(Distributed Tracing)**은 이러한 복잡한 요청 흐름을 하나의 "여정"으로 시각화하는 기술입니다. 하나의 요청이 시스템을 통과하는 전체 경로를 추적하고, 각 단계에서 소요된 시간을 측정합니다.

Functorium은 OpenTelemetry Tracing 표준을 따르는 분산 추적 기능을 자동으로 제공합니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **분산 추적의 핵심 개념** - Trace, Span, Context의 관계
2. **Functorium이 자동으로 생성하는 Span의 구조** - 아키텍처 레이어별 Span 설계
3. **Parent-Child 관계를 통한 요청 흐름 추적** - 계층적 구조의 이해
4. **Jaeger, Grafana Tempo를 활용한 Trace 분석 방법** - 병목 구간 식별

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- Functorium 로깅 매뉴얼의 내용 (필드 네이밍, 아키텍처 레이어)
- 비동기 프로그래밍의 기본 개념
- HTTP 요청/응답 모델

---

## 분산 추적의 기초

### Trace, Span, Context 이해하기

분산 추적을 이해하려면 세 가지 핵심 개념을 알아야 합니다.

#### Trace (추적)

Trace는 하나의 요청이 시스템을 통과하는 **전체 여정**을 나타냅니다. 예를 들어, 사용자가 "주문하기" 버튼을 클릭했을 때 발생하는 모든 작업이 하나의 Trace로 묶입니다.

각 Trace는 고유한 **Trace ID**를 가집니다:

```
Trace ID: 4bf92f3577b34da6a3ce929d0e0e4736
```

이 ID는 128비트 랜덤 값으로, 전 세계적으로 고유합니다. 동일한 Trace ID를 가진 모든 Span은 하나의 요청 처리 과정에 속합니다.

#### Span (구간)

Span은 Trace 내의 **개별 작업 단위**입니다. 하나의 Trace는 여러 개의 Span으로 구성됩니다. 각 Span은 "언제 시작했고, 얼마나 걸렸는가"를 기록합니다.

**예시: 주문 처리 Trace**

```
Trace: Order Processing (Trace ID: 4bf92f...)
|
+-- Span: HTTP POST /api/orders (1.5s)
    |
    +-- Span: CreateOrderCommandHandler.Handle (1.2s)
        |
        +-- Span: OrderRepository.Save (0.3s)
        |
        +-- Span: PaymentGateway.ProcessPayment (0.8s)
        |
        +-- Span: NotificationService.SendEmail (0.1s)
```

각 Span은 다음 정보를 포함합니다:

| 속성 | 설명 | 예시 |
|------|------|------|
| **이름** | 어떤 작업인가? | "CreateOrderCommandHandler.Handle" |
| **시작 시간** | 언제 시작했는가? | 2024-01-15T10:30:45.123Z |
| **소요 시간** | 얼마나 걸렸는가? | 1.2초 |
| **태그** | 추가 메타데이터 | `response.status = "success"` |
| **부모 Span** | 어떤 Span이 이 작업을 호출했는가? | HTTP POST /api/orders |
| **상태** | 성공/실패 여부 | Ok / Error |

#### Context (컨텍스트)

Context는 Span들을 하나의 Trace로 연결하는 정보입니다. Context에는 Trace ID와 현재 Span ID가 포함됩니다.

요청이 서비스 간에 전달될 때 Context도 함께 **전파(Propagation)**됩니다. HTTP의 경우 `traceparent` 헤더를 통해 전파됩니다:

```
HTTP Header:
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
             |  |                                |                |
             |  |                                |                +-- Flags
             |  |                                +-- Parent Span ID (16 chars)
             |  +-- Trace ID (32 chars)
             +-- Version
```

이 헤더 덕분에 서로 다른 서비스에서 생성된 Span들이 하나의 Trace로 연결될 수 있습니다.

### Parent-Child 관계

Span은 **계층적 구조**를 가집니다. 부모 Span이 자식 Span을 포함합니다. 이 관계는 호출 스택과 유사합니다.

**시각화:**

```
Time -->
|                                                              |
|  CreateOrderCommandHandler.Handle (1.2s)                     |
|  [=================================================]         |
|  |                                                 |         |
|  | OrderRepository.Save (0.3s)                     |         |
|  | [============]                                  |         |
|  |              |                                  |         |
|  |              | PaymentGateway.ProcessPayment (0.8s)       |
|  |              | [================================]         |
|  |              |                                  |         |
|  |              |                                  | Email   |
|  |              |                                  | [==]    |
|  |              |                                  | (0.1s)  |
|  |              |                                  |         |
|  0s            0.3s                                1.1s      1.2s
```

이 구조를 보면:
1. 전체 요청(Handle)이 1.2초 걸렸음을 알 수 있습니다.
2. 그 중 PaymentGateway가 0.8초(66%)로 가장 오래 걸렸습니다.
3. PaymentGateway 최적화가 전체 성능 개선에 가장 효과적일 것입니다.

### Activity와 Span

.NET에서는 OpenTelemetry의 Span을 `System.Diagnostics.Activity` 클래스로 구현합니다. Functorium 코드에서 "Activity"라는 용어가 나오면 이는 OpenTelemetry의 Span과 동일합니다.

| OpenTelemetry 용어 | .NET 용어 |
|-------------------|-----------|
| Span | Activity |
| SpanContext | ActivityContext |
| Tracer | ActivitySource |
| Status.OK | ActivityStatusCode.Ok |
| Status.ERROR | ActivityStatusCode.Error |

### 로깅, 메트릭, 트레이싱 비교

세 가지 관찰 가능성 도구는 서로 다른 질문에 답합니다:

| 도구 | 질문 | 데이터 유형 |
|------|------|-------------|
| **로깅** | 무슨 일이 있었는가? | 개별 이벤트 |
| **메트릭** | 얼마나 많이/빠르게? | 집계된 숫자 |
| **트레이싱** | 어디서 시간이 소요되었는가? | 요청 경로 |

**실제 시나리오:**

1. **알림 발생**: 메트릭에서 "P99 응답 시간이 2초를 초과"
2. **원인 조사**: 트레이싱에서 "PaymentGateway에서 1.8초 지연 발견"
3. **상세 확인**: 로깅에서 "PaymentGateway 타임아웃 에러 메시지 확인"

---

## Functorium 트레이싱 아키텍처

Functorium은 두 개의 아키텍처 레이어에서 자동으로 Span을 생성합니다.

### 아키텍처 개요

```
HTTP Request Arrives
       |
       v
+------------------------------------------------------------------+
|  Application Layer                                               |
|  +------------------------------------------------------------+  |
|  |  Span: "application usecase.command                        |  |
|  |         CreateOrderCommandHandler.Handle"                  |  |
|  |  Kind: Internal                                            |  |
|  |  Tags: request.layer, request.category, etc.               |  |
|  |  Status: Ok or Error                                       |  |
|  +------------------------------------------------------------+  |
|         |                                                        |
|         v (Parent-Child Relationship)                            |
|  +------------------------------------------------------------+  |
|  |  Adapter Layer                                             |  |
|  |  +------------------------------------------------------+  |  |
|  |  |  Span: "adapter repository                           |  |  |
|  |  |         OrderRepository.Save"                        |  |  |
|  |  |  Kind: Internal                                      |  |  |
|  |  |  Tags: request.layer, request.category, etc.         |  |  |
|  |  |  Status: Ok or Error                                 |  |  |
|  |  +------------------------------------------------------+  |  |
|  +------------------------------------------------------------+  |
+------------------------------------------------------------------+
```

Application Layer의 Span은 Adapter Layer의 Span의 **부모**가 됩니다. 이 관계 덕분에 요청이 어떤 경로를 거쳤는지 명확하게 추적할 수 있습니다.

### Span Kind

OpenTelemetry는 Span의 역할을 나타내는 **Kind**를 정의합니다:

| Kind | 설명 | 예시 |
|------|------|------|
| **Server** | 외부 요청을 받아 처리 | HTTP 서버 엔드포인트 |
| **Client** | 외부 서비스 호출 | HTTP 클라이언트, DB 쿼리 |
| **Internal** | 내부 처리 | 비즈니스 로직 처리 |
| **Producer** | 비동기 메시지 발행 | 메시지 큐 발행 |
| **Consumer** | 비동기 메시지 수신 | 메시지 큐 소비 |

Functorium의 자동 생성 Span은 모두 **Internal** Kind를 사용합니다. HTTP 요청 수신이나 데이터베이스 호출 Span은 ASP.NET Core와 데이터베이스 라이브러리에서 별도로 생성합니다.

### Span 명명 규칙

Functorium은 일관된 Span 이름 패턴을 사용합니다:

**Application Layer:**
```
{layer} {category}.{cqrs} {handler}.{method}

예시:
- application usecase.command CreateOrderCommandHandler.Handle
- application usecase.query GetOrderQueryHandler.Handle
```

**Adapter Layer:**
```
{layer} {category} {handler}.{method}

예시:
- adapter repository OrderRepository.Save
- adapter repository OrderRepository.GetById
- adapter gateway PaymentGateway.ProcessPayment
```

이 명명 규칙의 이점:

1. **일관성**: 모든 Span이 동일한 패턴을 따름
2. **검색 용이성**: Span 이름으로 빠른 필터링 가능
3. **자기 설명적**: 이름만 보고 어떤 작업인지 파악 가능

---

## Span 구조 이해하기

### Span 기본 속성

각 Span은 다음 기본 속성을 가집니다:

| 속성 | 설명 | 예시 |
|------|------|------|
| **TraceId** | 소속된 Trace의 ID | 4bf92f3577b34da6a3ce929d0e0e4736 |
| **SpanId** | Span의 고유 ID | 00f067aa0ba902b7 |
| **ParentSpanId** | 부모 Span의 ID (없으면 root) | 5b8a8f6d3e7c9a1b |
| **Name** | Span 이름 | application usecase.command... |
| **Kind** | Span 종류 | Internal |
| **StartTime** | 시작 시간 | 2024-01-15T10:30:45.123Z |
| **EndTime** | 종료 시간 | 2024-01-15T10:30:46.323Z |
| **Duration** | 소요 시간 | 1.2s |
| **Status** | 상태 코드 | Ok / Error |
| **Tags** | 추가 메타데이터 | request.handler = "..." |

### Status 코드

Span의 Status는 작업의 성공/실패를 나타냅니다:

| Status | 설명 | 언제 사용 |
|--------|------|-----------|
| **Unset** | 상태 미설정 | 기본값 |
| **Ok** | 성공 | 정상 처리 완료 |
| **Error** | 실패 | 에러 발생 |

Functorium에서:
- `response.status = "success"` → `ActivityStatusCode.Ok`
- `response.status = "failure"` → `ActivityStatusCode.Error`

**중요**: Expected 에러(비즈니스 오류)도 Status는 Error입니다. 이는 "요청이 원하는 결과를 얻지 못했다"는 의미이기 때문입니다. 에러의 성격(Expected vs Exceptional)은 `error.type` 태그로 구분합니다.

### 시간 측정

Span의 시간 측정은 다음과 같이 이루어집니다:

```
StartTime                                          EndTime
    |                                                  |
    v                                                  v
    +--------------------------------------------------+
    |                 Duration (1.2s)                  |
    |                                                  |
    |  +----------+  +--------------------+  +----+    |
    |  |  0.3s    |  |       0.8s         |  |0.1s|   |
    |  +----------+  +--------------------+  +----+    |
    |  OrderRepo     PaymentGateway        Email      |
    +--------------------------------------------------+
```

**Duration 계산:**
```
Duration = EndTime - StartTime = 1.2s

Child Span Total = 0.3 + 0.8 + 0.1 = 1.2s
```

자식 Span의 합계와 부모 Span의 Duration이 같다면, 부모에서 추가 작업이 없었다는 의미입니다. 차이가 있다면 그 시간은 부모 Span에서 직접 수행한 작업(로직 실행, 데이터 변환 등)에 소요되었습니다.

---

## 태그 시스템 상세 가이드

태그는 Span에 추가적인 컨텍스트를 제공합니다. Functorium은 로깅, 메트릭과 동일한 태그 키를 사용하여 **3-Pillar 일관성**을 유지합니다.

### Application Layer 태그 구조

**태그 구조표:**

| 태그 키 | 성공 | 실패 | 설명 |
|---------|------|------|------|
| `request.layer` | "application" | "application" | 레이어 식별자 |
| `request.category` | "usecase" | "usecase" | 카테고리 식별자 |
| `request.category.type` | "command"/"query" | "command"/"query" | CQRS 타입 |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러 클래스명 |
| `request.handler.method` | "Handle" | "Handle" | 메서드명 |
| `response.elapsed` | 처리 시간 | 처리 시간 | 초 단위 |
| `response.status` | "success" | "failure" | 응답 상태 |
| `error.type` | - | "expected"/"exceptional"/"aggregate" | 에러 분류 |
| `error.code` | - | 에러 코드 | 도메인 에러 코드 |
| **총 태그 수** | **7** | **9** | |

**예시 - Command 성공:**

```json
{
  "name": "application usecase.command CreateOrderCommandHandler.Handle",
  "status": "Ok",
  "tags": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "command",
    "request.handler": "CreateOrderCommandHandler",
    "request.handler.method": "Handle",
    "response.elapsed": 0.1234,
    "response.status": "success"
  }
}
```

**예시 - Command 실패:**

```json
{
  "name": "application usecase.command CreateOrderCommandHandler.Handle",
  "status": "Error",
  "tags": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "command",
    "request.handler": "CreateOrderCommandHandler",
    "request.handler.method": "Handle",
    "response.elapsed": 0.0567,
    "response.status": "failure",
    "error.type": "expected",
    "error.code": "Order.InsufficientStock"
  }
}
```

### Adapter Layer 태그 구조

Adapter Layer에서는 CQRS 구분이 없으므로 `request.category.type` 태그가 없습니다.

**태그 구조표:**

| 태그 키 | 성공 | 실패 | 설명 |
|---------|------|------|------|
| `request.layer` | "adapter" | "adapter" | 레이어 식별자 |
| `request.category` | 카테고리명 | 카테고리명 | 카테고리 식별자 |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러 클래스명 |
| `request.handler.method` | 메서드명 | 메서드명 | 메서드명 |
| `response.elapsed` | 처리 시간 | 처리 시간 | 초 단위 |
| `response.status` | "success" | "failure" | 응답 상태 |
| `error.type` | - | "expected"/"exceptional"/"aggregate" | 에러 분류 |
| `error.code` | - | 에러 코드 | 도메인 에러 코드 |
| **총 태그 수** | **6** | **8** | |

**예시 - Repository 성공:**

```json
{
  "name": "adapter repository OrderRepository.GetById",
  "status": "Ok",
  "tags": {
    "request.layer": "adapter",
    "request.category": "repository",
    "request.handler": "OrderRepository",
    "request.handler.method": "GetById",
    "response.elapsed": 0.0456,
    "response.status": "success"
  }
}
```

### response.elapsed가 트레이싱에 포함되는 이유

메트릭에서는 `response.elapsed`가 태그가 아닌 Histogram으로 기록된다고 설명했습니다. 그런데 트레이싱에서는 태그로 포함됩니다. 이유가 무엇일까요?

**차이점:**

| 측면 | 메트릭 | 트레이싱 |
|------|--------|----------|
| **목적** | 집계 분석 | 개별 요청 추적 |
| **카디널리티** | 시계열 수 제한 필요 | Span은 개별 이벤트 |
| **저장 방식** | 태그 조합별 시계열 | Span 문서 단위 |

트레이싱에서 각 Span은 개별 문서로 저장됩니다. `response.elapsed` 값이 다르다고 별도 시계열이 생성되지 않습니다. 따라서 태그로 포함해도 카디널리티 문제가 없습니다.

또한, 개별 Span에서 정확한 처리 시간을 태그로 확인하면 특정 요청의 성능을 빠르게 파악할 수 있습니다.

---

## Application Layer 트레이싱

Application Layer의 트레이싱은 `UsecaseTracingPipeline`에 의해 자동으로 수행됩니다.

### Pipeline 동작

```csharp
public class UsecaseTracingPipeline<TRequest, TResponse>
{
    public async ValueTask<TResponse> Handle(TRequest request, ...)
    {
        // 1. Span 생성 및 시작
        using var activity = _activitySource.StartActivity(spanName);

        // 2. 요청 태그 추가
        activity?.SetTag("request.layer", "application");
        activity?.SetTag("request.category", "usecase");
        // ... 나머지 태그

        // 3. 핸들러 실행
        var response = await next(request, cancellationToken);

        // 4. 응답 태그 추가
        activity?.SetTag("response.status", response.IsSucc ? "success" : "failure");
        activity?.SetTag("response.elapsed", elapsed.TotalSeconds);

        // 5. 에러인 경우 추가 태그
        if (response.IsFail)
        {
            activity?.SetTag("error.type", GetErrorType(response));
            activity?.SetTag("error.code", GetErrorCode(response));
            activity?.SetStatus(ActivityStatusCode.Error);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        // 6. Span 종료 (using에 의해 자동)
        return response;
    }
}
```

### Span 이름 생성

Application Layer의 Span 이름은 다음 형식을 따릅니다:

```
{layer} {category}.{cqrs} {handler}.{method}
```

**생성 로직:**

```csharp
var cqrsType = GetCqrsType<TRequest>();  // "command" 또는 "query"
var handlerName = typeof(TRequest).Name.Replace("Request", "Handler");
var spanName = $"application usecase.{cqrsType} {handlerName}.Handle";
```

**예시:**

| Request Type | Span 이름 |
|--------------|-----------|
| `CreateOrderCommandRequest` | `application usecase.command CreateOrderCommandHandler.Handle` |
| `GetOrderQueryRequest` | `application usecase.query GetOrderQueryHandler.Handle` |

---

## Adapter Layer 트레이싱

Adapter Layer의 트레이싱은 Source Generator가 자동으로 생성한 코드에 의해 수행됩니다.

### Source Generated 코드

Source Generator는 `[ObservabilityPipeline]` 속성이 적용된 인터페이스에 대해 자동으로 트레이싱 코드를 생성합니다.

**원본 인터페이스:**

```csharp
[ObservabilityPipeline("repository")]
public interface IOrderRepository
{
    FinT<IO, Order> GetById(Guid id);
    FinT<IO, Unit> Save(Order order);
}
```

**생성된 코드 (간략화):**

```csharp
public partial class OrderRepositoryPipeline : IOrderRepository
{
    public FinT<IO, Order> GetById(Guid id)
    {
        return FinT<IO, Order>.LiftIO(async () =>
        {
            using var activity = _activitySource.StartActivity(
                "adapter repository OrderRepository.GetById");

            activity?.SetTag("request.layer", "adapter");
            activity?.SetTag("request.category", "repository");
            activity?.SetTag("request.handler", "OrderRepository");
            activity?.SetTag("request.handler.method", "GetById");

            var stopwatch = Stopwatch.StartNew();
            var result = await _inner.GetById(id).Run().RunAsync();
            stopwatch.Stop();

            activity?.SetTag("response.elapsed", stopwatch.Elapsed.TotalSeconds);
            activity?.SetTag("response.status",
                result.IsFail ? "failure" : "success");

            if (result.IsFail)
            {
                activity?.SetTag("error.type", GetErrorType(result));
                activity?.SetTag("error.code", GetErrorCode(result));
                activity?.SetStatus(ActivityStatusCode.Error);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            return result;
        });
    }
}
```

### Span 이름 생성

Adapter Layer의 Span 이름은 다음 형식을 따릅니다:

```
{layer} {category} {handler}.{method}
```

**예시:**

| Handler | Method | Span 이름 |
|---------|--------|-----------|
| `OrderRepository` | `GetById` | `adapter repository OrderRepository.GetById` |
| `OrderRepository` | `Save` | `adapter repository OrderRepository.Save` |
| `PaymentGateway` | `ProcessPayment` | `adapter gateway PaymentGateway.ProcessPayment` |

---

## DomainEvent 트레이싱

DomainEvent 트레이싱은 이벤트 발행과 처리 과정을 Span으로 기록합니다. Usecase Span → Publisher Span → Handler Span(s)의 Parent-Child 관계를 형성합니다.

### Parent-Child 관계

```
application usecase.command CreateProductCommandHandler.Handle [Parent]
  ├─ adapter repository InMemoryProductRepository.ExistsByName [Child]
  ├─ adapter repository InMemoryProductRepository.Create [Child]
  └─ adapter event Product.PublishEvents [Child - Publisher]
       └─ application usecase.event OnProductCreated.Handle [Grandchild - Handler]
```

Publisher Span은 Adapter 레이어에 속하고, Handler Span은 Application 레이어에 속합니다. 하나의 Publisher가 여러 Handler를 호출하면 Handler Span이 여러 개 생성됩니다.

### Publisher Span 구조

**Span Name:**

| 메서드 | Span Name 패턴 | 예시 |
|--------|---------------|------|
| Publish | `adapter event {EventType}.Publish` | `adapter event CreatedEvent.Publish` |
| PublishEvents | `adapter event {AggregateType}.PublishEvents` | `adapter event Product.PublishEvents` |
| PublishEventsWithResult | `adapter event {AggregateType}.PublishEventsWithResult` | `adapter event Product.PublishEventsWithResult` |

Kind: `Internal`

### Publisher 태그 구조 (Publish)

단일 이벤트 발행 시의 태그 구조:

| 태그 키 | Request | Success | Failure |
|---------|---------|---------|---------|
| `request.layer` | "adapter" | "adapter" | "adapter" |
| `request.category` | "event" | "event" | "event" |
| `request.handler` | event type name | event type name | event type name |
| `request.handler.method` | "Publish" | "Publish" | "Publish" |
| `response.elapsed` | - | 처리 시간(초) | 처리 시간(초) |
| `response.status` | - | "success" | "failure" |
| `error.type` | - | - | "expected"/"exceptional" |
| `error.code` | - | - | 에러 코드 |
| **총 태그 수** | **4** | **6** | **8** |

### Publisher 태그 구조 (PublishEvents/PublishEventsWithResult)

Aggregate 다중 이벤트 발행 시의 태그 구조:

| 태그 키 | Request | Success | Partial Failure | Total Failure |
|---------|---------|---------|-----------------|---------------|
| `request.layer` | "adapter" | "adapter" | "adapter" | "adapter" |
| `request.category` | "event" | "event" | "event" | "event" |
| `request.handler` | aggregate type | aggregate type | aggregate type | aggregate type |
| `request.handler.method` | method name | method name | method name | method name |
| `request.event.count` | event count | event count | event count | event count |
| `response.elapsed` | - | 처리 시간(초) | 처리 시간(초) | 처리 시간(초) |
| `response.status` | - | "success" | "failure" | "failure" |
| `response.event.success_count` | - | - | success count | - |
| `response.event.failure_count` | - | - | failure count | - |
| `error.type` | - | - | - | "expected"/"exceptional" |
| `error.code` | - | - | - | 에러 코드 |
| **총 태그 수** | **5** | **7** | **9** | **9** |

### Handler Span 구조

**Span Name:**

```
application usecase.event {HandlerName}.Handle
```

예시: `application usecase.event OnProductCreated.Handle`

Kind: `Internal`

### Handler 태그 구조

| 태그 키 | Success | Failure |
|---------|---------|---------|
| `request.layer` | "application" | "application" |
| `request.category` | "usecase" | "usecase" |
| `request.category.type` | "event" | "event" |
| `request.handler` | handler name | handler name |
| `request.handler.method` | "Handle" | "Handle" |
| `event.type` | event type name | event type name |
| `event.id` | event id | event id |
| `response.status` | "success" | "failure" |
| `error.type` | - | "expected"/"exceptional" |
| `error.code` | - | 에러 코드 |
| **총 태그 수** | **8** | **10** |

> **Note:** Handler의 `response.elapsed`는 Activity 태그에 설정되지 않습니다 (Logging 전용).

### event.type과 event.id 필드

Handler Span에는 `event.type`과 `event.id`라는 고유 태그가 있습니다:

- **`event.type`**: 이벤트 타입명. `request.handler`(핸들러명)와 **다른 값**입니다.
  - 예: `request.handler = "OnProductCreated"`, `event.type = "CreatedEvent"`
  - 하나의 이벤트 타입에 여러 핸들러가 등록될 수 있으므로 구분이 필요합니다.

- **`event.id`**: 이벤트 인스턴스별 GUID. 동일 이벤트를 처리하는 여러 핸들러 간의 상관관계를 추적합니다.
  - 예: `event.id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"`

### LayeredArch Trace 시각화

**상품 생성 성공 (`POST /api/products`):**

```
application usecase.command CreateProductCommand.Handle [Ok]
  ├─ adapter repository InMemoryProductRepository.ExistsByName [Ok]
  ├─ adapter repository InMemoryProductRepository.Create [Ok]
  └─ adapter event Product.PublishEvents [Ok]
       └─ application usecase.event OnProductCreated.Handle [Ok]
            ├─ event.type = "CreatedEvent"
            └─ event.id = "515711cd-..."
```

**핸들러 예외 (`POST /api/products` with `[handler-error]`):**

```
application usecase.command CreateProductCommand.Handle [Error]
  ├─ adapter repository InMemoryProductRepository.ExistsByName [Ok]
  ├─ adapter repository InMemoryProductRepository.Create [Ok]
  └─ adapter event Product.PublishEvents [Error]
       └─ application usecase.event OnProductCreated.Handle [Error]
            ├─ event.type = "CreatedEvent"
            ├─ event.id = "f385a945-..."
            ├─ error.type = "exceptional"
            └─ error.code = "InvalidOperationException"
```

> **Note:** Handler의 `error.code`는 예외 타입명(`InvalidOperationException`), Publisher의 `error.code`는 래핑된 에러 코드(`ApplicationErrors.DomainEventPublisher.PublishFailed`)가 기록됩니다.

**어댑터 예외 (`POST /api/products` with `[adapter-error]`):**

어댑터 예외는 Repository에서 발생하므로 이벤트 발행까지 도달하지 않습니다:

```
application usecase.command CreateProductCommand.Handle [Error]
  ├─ adapter repository InMemoryProductRepository.ExistsByName [Ok]
  └─ adapter repository InMemoryProductRepository.Create [Error]
       ├─ error.type = "exceptional"
       └─ error.code = "Exceptional"
```

### Trace 검색 쿼리

**DomainEvent Publisher Span 검색:**

```traceql
{span.request.category="event" && span.request.layer="adapter"}
```

**DomainEvent Handler Span 검색:**

```traceql
{span.request.category.type="event" && span.request.layer="application"}
```

**에러가 발생한 Handler Span:**

```traceql
{span.request.category.type="event" && span.error.type="exceptional"}
```

---

## 에러 트레이싱 이해하기

### Status vs error.type

Span의 Status와 `error.type` 태그는 서로 다른 정보를 전달합니다:

| 속성 | 의미 | 값 |
|------|------|-----|
| **Status** | 작업 성공 여부 | Ok / Error |
| **error.type** | 에러의 성격 | expected / exceptional / aggregate |

**예시:**

| 시나리오 | Status | error.type | 설명 |
|----------|--------|------------|------|
| 주문 성공 | Ok | - | 정상 처리 |
| 재고 부족 | Error | expected | 비즈니스 규칙에 따른 거부 |
| DB 연결 실패 | Error | exceptional | 시스템 문제 |

### Trace UI에서 에러 표시

대부분의 Trace UI(Jaeger, Tempo)는 `Status = Error`인 Span을 빨간색으로 표시합니다. 이를 통해 어느 단계에서 문제가 발생했는지 빠르게 파악할 수 있습니다.

```
CreateOrderCommandHandler.Handle [Error] (1.2s)
+-- OrderRepository.GetById [Ok] (0.1s)
+-- InventoryService.CheckStock [Error] (0.05s)  <-- Failed here
+-- PaymentGateway.Process [Not Started]         <-- Not executed
```

### 에러 전파

자식 Span에서 에러가 발생하면, 부모 Span도 일반적으로 Error 상태가 됩니다. 이는 "자식의 실패가 부모의 실패를 유발"하기 때문입니다.

```
Application Layer: CreateOrderCommand -> Error (due to child failure)
    |
    +-- Adapter Layer: InventoryRepository.CheckStock -> Error (root cause)
```

단, 부모가 자식의 에러를 처리(fallback, retry 등)하면 부모는 Ok 상태일 수 있습니다.

---

## Trace 분석하기

### Jaeger 쿼리 예시

**서비스별 Trace 검색:**

```
service=orderservice
```

**느린 Trace 검색:**

```
service=orderservice minDuration=1s
```

**에러 Trace 검색:**

```
service=orderservice tags={"response.status":"failure"}
```

**특정 핸들러의 Trace:**

```
service=orderservice tags={"request.handler":"CreateOrderCommandHandler"}
```

### Grafana Tempo 쿼리 예시

**TraceQL 기본 검색:**

```traceql
{resource.service.name="orderservice"}
```

**특정 Span 검색:**

```traceql
{span.request.handler="CreateOrderCommandHandler" && span.response.status="failure"}
```

**느린 Span 검색:**

```traceql
{span.response.elapsed > 1.0}
```

**에러 타입별 검색:**

```traceql
{span.error.type="exceptional"}
```

### Trace 분석 워크플로우

1. **문제 식별**: 메트릭에서 "P99 응답 시간 > 2초" 감지
2. **샘플 Trace 조회**: 해당 시간대의 느린 Trace 검색
3. **병목 구간 식별**: Span별 Duration 비교
4. **근본 원인 파악**: 가장 오래 걸린 Span 확인
5. **상세 조사**: 해당 Span의 태그와 로그 확인

---

## 실습: 병목 구간 찾기

### 시나리오: 주문 생성이 느림

**상황:** "주문 생성" API의 P99 응답 시간이 3초를 초과합니다.

**단계 1: 느린 Trace 샘플 조회**

Jaeger에서 다음 조건으로 검색:
```
service=orderservice
operation=application usecase.command CreateOrderCommandHandler.Handle
minDuration=2s
```

**단계 2: Trace 상세 분석**

조회된 Trace를 펼쳐 각 Span의 Duration을 확인합니다:

```
CreateOrderCommandHandler.Handle (2.8s)
+-- OrderRepository.GetCustomer (0.1s)
+-- InventoryService.CheckStock (0.2s)
+-- PaymentGateway.ProcessPayment (2.3s)  <-- Bottleneck!
+-- NotificationService.SendEmail (0.2s)
```

**단계 3: 병목 Span 분석**

`PaymentGateway.ProcessPayment` Span의 태그 확인:
```json
{
  "request.handler": "PaymentGateway",
  "request.handler.method": "ProcessPayment",
  "response.elapsed": 2.3,
  "response.status": "success"
}
```

**단계 4: 추가 조사**

PaymentGateway의 외부 호출 Span(Client Kind)이 있다면 확인:
```
PaymentGateway.ProcessPayment (2.3s)
+-- HTTP POST payment-provider.com/api/charge (2.2s)  <-- External service delay
```

**결론:** 외부 결제 서비스(payment-provider.com)의 응답 지연이 근본 원인입니다.

**대응 방안:**
1. 결제 서비스 타임아웃 설정 확인
2. 비동기 처리 고려 (결제 완료 대기 없이 주문 생성)
3. 결제 서비스 제공자에게 지연 문의

### 시나리오: 간헐적 에러 발생

**상황:** 특정 시간대에 "주문 생성" 에러가 급증합니다.

**단계 1: 에러 Trace 조회**

```
service=orderservice
tags={"response.status":"failure","error.type":"exceptional"}
```

**단계 2: 에러 패턴 분석**

여러 Trace를 비교하여 공통점 확인:
- 모두 `DatabaseRepository.Save`에서 실패
- `error.code = "Database.ConnectionFailed"`

**단계 3: 시간대 상관관계**

에러 발생 시간대와 다른 이벤트(배포, 트래픽 급증, 인프라 변경) 비교

**결론:** 데이터베이스 커넥션 풀 고갈이 원인으로 추정

---

## 트러블슈팅

### Span이 생성되지 않는 경우

**증상:** Trace에 특정 Span이 보이지 않습니다.

**확인 사항:**

1. **Pipeline 등록 확인:**
   ```csharp
   services.AddMediator(options =>
   {
       options.AddOpenBehavior(typeof(UsecaseTracingPipeline<,>));
   });
   ```

2. **ActivitySource 등록 확인:**
   ```csharp
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing
           .AddSource("Functorium.*"));
   ```

3. **Sampling 설정 확인:**
   ```csharp
   .SetSampler(new AlwaysOnSampler())  // 모든 Trace 수집
   ```

### Parent-Child 관계가 끊어진 경우

**증상:** 자식 Span이 별도 Trace로 표시됩니다.

**원인:** Context가 전파되지 않았습니다.

**확인 사항:**

1. **비동기 호출에서 Context 전파:**
   ```csharp
   // 잘못된 예: Context가 전파되지 않음
   Task.Run(() => adapter.DoSomething());

   // 올바른 예: Context 전파
   await adapter.DoSomething();
   ```

2. **외부 서비스 호출 시 헤더 전파:**
   ```csharp
   httpClient.DefaultRequestHeaders.Add("traceparent", activity?.Id);
   ```

### Duration이 예상보다 긴 경우

**증상:** Span의 Duration이 자식 Span 합계보다 훨씬 큽니다.

**가능한 원인:**

1. **Span 외부에서 시간 소요:**
   ```csharp
   Thread.Sleep(1000);  // Span 생성 전에 대기
   using var activity = source.StartActivity("...");
   // 실제 작업
   ```

2. **비동기 대기:**
   ```csharp
   using var activity = source.StartActivity("...");
   await Task.Delay(1000);  // Span 내에서 대기
   // 자식 Span 없이 대기만 함
   ```

---

## FAQ

### Q: 모든 요청을 추적해야 하나요?

A: 대부분의 운영 환경에서는 **샘플링**을 적용합니다. 모든 요청을 추적하면 저장 비용과 성능 오버헤드가 큽니다.

**일반적인 샘플링 전략:**
- 에러 요청: 100% 수집
- 성공 요청: 1-10% 수집
- 특정 조건: 100% 수집 (예: 특정 사용자, 특정 API)

```csharp
.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)))  // 10% 샘플링
```

### Q: Trace 보존 기간은 어떻게 설정하나요?

A: 저장 백엔드에 따라 다릅니다:

- **Jaeger**: `--es.max-span-age` 플래그
- **Tempo**: `compactor.compaction.block_retention`

일반적으로 7-30일 보존을 권장합니다. 중요한 Trace는 별도 저장할 수 있습니다.

### Q: 로깅과 트레이싱을 어떻게 연결하나요?

A: Trace ID를 로그에 포함하면 연결할 수 있습니다:

```csharp
Log.ForContext("TraceId", Activity.Current?.TraceId.ToString())
   .Information("Order created");
```

Grafana에서 Trace → Log 연동을 설정하면 클릭 한 번으로 관련 로그를 조회할 수 있습니다.

### Q: 외부 서비스 호출도 추적되나요?

A: HttpClient, 데이터베이스 드라이버 등의 계측(Instrumentation)을 추가하면 됩니다:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddNpgsql());
```

이 설정으로 HTTP 호출과 DB 쿼리가 자동으로 Span으로 기록됩니다.

### Q: 성능 오버헤드는 얼마나 되나요?

A: OpenTelemetry의 오버헤드는 일반적으로 매우 낮습니다:

- CPU: 1-5% 추가
- 메모리: 수 MB 추가
- 지연 시간: < 1ms 추가

단, 모든 Span을 export하면 네트워크 대역폭 비용이 증가합니다. 샘플링을 적용하면 오버헤드를 최소화할 수 있습니다.

### Q: Activity.Current가 null인 경우?

A: Span이 시작되지 않았거나 Context가 전파되지 않은 경우입니다.

**확인 사항:**
1. ActivitySource가 등록되었는지 확인
2. ActivityListener가 해당 소스를 수신하고 있는지 확인
3. Sampler가 해당 Activity를 제외하지 않는지 확인

```csharp
// 디버깅용 코드
Console.WriteLine($"Current Activity: {Activity.Current?.DisplayName ?? "null"}");
Console.WriteLine($"TraceId: {Activity.Current?.TraceId}");
```

---

## 참조

- [OpenTelemetry Tracing Specification](https://opentelemetry.io/docs/specs/otel/trace/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Grafana Tempo Documentation](https://grafana.com/docs/tempo/latest/)
- [.NET Activity and DiagnosticSource](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing)
- [Observability Specification](./observability-spec.md)
