---
title: "관측 가능성 사양"
---

Functorium 프레임워크의 관측 가능성(Observability) 필드/태그 사양, Meter 정의 규칙, 메시지 템플릿 패턴을 정의합니다. Pipeline 실행 순서, OpenTelemetryOptions 설정, 커스텀 확장 포인트는 [파이프라인 사양](./07-pipeline)을 참조하십시오.

## 요약

### 주요 개념

| 개념 | 설명 |
|------|------|
| Service Attributes | `service.namespace`, `service.name` 등 OpenTelemetry 표준 서비스 식별 |
| Error 분류 | `expected` (비즈니스 오류), `exceptional` (시스템 오류), `aggregate` (복합 오류) |
| 3-Pillar Field/Tag | `request.*`, `response.*`, `error.*` 필드가 Logging, Metrics, Tracing에서 동일하게 사용 |
| Meter Name | `{service.namespace}.{layer}[.{category}]` 패턴 |
| Instrument | `requests` (Counter), `responses` (Counter), `duration` (Histogram) |
| Message Template | Layer별 구조화 로그 메시지 형식과 Event ID 체계 (Application 1001-1004, Adapter 2001-2004) |
| Span Name | `{layer} {category}[.{type}] {handler}.{method}` |
| ctx.* 컨텍스트 필드 | Source Generator가 Request/Response/DomainEvent 프로퍼티를 `ctx.{snake_case}` 필드로 자동 변환. `[CtxTarget]`으로 Pillar 타겟팅 (기본: Logging + Tracing, Metrics는 opt-in) |

---

## 공통 사양

### Service Attributes

Functorium은 서비스 식별을 위해 [OpenTelemetry Service Attributes](https://opentelemetry.io/docs/specs/semconv/registry/attributes/service/)를 사용합니다.

| Attribute | 설명 | 예시 |
|-----------|------|------|
| `service.namespace` | `service.name`의 네임스페이스. 서비스 그룹을 구분하는 데 도움이 됩니다(예: 팀별 또는 환경별). | `mycompany.production` |
| `service.name` | 서비스의 논리적 이름. 수평 확장된 모든 인스턴스에서 동일해야 합니다. | `orderservice` |
| `service.version` | 서비스 API 또는 구현의 버전 문자열. | `2.0.0` |
| `service.instance.id` | 서비스 인스턴스의 고유 ID. `service.namespace,service.name` 쌍당 전역적으로 고유해야 합니다. 가능한 경우 `HOSTNAME` 환경 변수를 사용하고, 그렇지 않으면 `Environment.MachineName`으로 대체됩니다. | `my-pod-abc123` (Kubernetes), `DESKTOP-ABC123` (Windows) |
| `deployment.environment` | 배포 환경을 식별하는 속성. | `production`, `staging` |

> **권장**: `service.name`과 `service.namespace`에는 소문자 값을 사용하세요(예: `mycompany.production`, `orderservice`).
> 이렇게 하면 OpenTelemetry 규칙과의 일관성을 보장하고 다운스트림 시스템(대시보드, 쿼리, 알림)에서 대소문자 구분 문제를 방지할 수 있습니다.

### Error 분류

#### Error Type Tag 값

다음 표는 에러 원인에 따라 `error.type`과 `error.code` 태그 값이 어떻게 결정되는지 정리합니다.

| Error Case | error.type | error.code | 설명 |
|------------|------------|------------|------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | 오류 코드 | 오류 코드가 있는 예상 비즈니스 로직 오류 |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | 오류 코드 | 오류 코드가 있는 예외 시스템 오류 |
| `ManyErrors` | `"aggregate"` | Primary 오류 코드 | 여러 오류가 집계됨(Exceptional이 우선) |
| `Expected` (LanguageExt) | `"expected"` | 타입 이름 | 오류 코드가 없는 LanguageExt 기본 예상 오류 |
| `Exceptional` (LanguageExt) | `"exceptional"` | 타입 이름 | 오류 코드가 없는 LanguageExt 기본 예외 오류 |

#### Error Field 값 (Logging 전용)

> `error.type`과 `@error.ErrorType`은 서로 다른 목적을 위해 다른 값 형식을 사용합니다.

| Error Type | `error.type` (필터링용) | `@error.ErrorType` (상세용) |
|------------|------------------------|----------------------------|
| Expected Error | `"expected"` | `"ErrorCodeExpected"` |
| Exceptional Error | `"exceptional"` | `"ErrorCodeExceptional"` |
| Aggregate Error | `"aggregate"` | `"ManyErrors"` |
| LanguageExt Expected | `"expected"` | `"Expected"` |
| LanguageExt Exceptional | `"exceptional"` | `"Exceptional"` |

- **`error.type`**: 로그 필터링/쿼리를 위한 표준화된 값(Metrics/Tracing과 일관됨)
- **`@error.ErrorType`**: 상세한 오류 타입 식별을 위한 실제 클래스 이름

### 필드/태그 네이밍 규칙

#### 표기법: `snake_case + dot`

OpenTelemetry 시맨틱 규칙을 준수하여 `snake_case + dot` 표기법을 사용합니다.

```
# 올바른 예시
request.layer
request.category.type
request.handler.method
response.status
error.code

# 잘못된 예시
requestLayer          # camelCase 사용 금지
request-layer         # kebab-case 사용 금지
REQUEST_LAYER         # UPPER_SNAKE_CASE 사용 금지
```

#### 계층 구조: `{namespace}.{property}`

필드는 네임스페이스와 속성의 계층 구조로 구성합니다.

| 네임스페이스 | 설명 | 예시 |
|-------------|------|------|
| `request.*` | 요청 관련 정보 | `request.layer`, `request.handler.name` |
| `response.*` | 응답 관련 정보 | `response.status`, `response.elapsed` |
| `error.*` | 오류 관련 정보 | `error.type`, `error.code` |

#### `count` 필드 규칙

| 구분 | 규칙 | 예시 |
|------|------|------|
| 정적 필드 (이벤트 관련) | `.count` | `request.event.count` |
| 동적 필드 (파라미터 크기) | `_count` | `request.params.orders_count` |
| 형용사/명사 조합 | `_count` | `response.event.success_count`, `response.event.failure_count` |

```
# 올바른 예시
request.event.count              # 정적 .count
response.event.success_count     # 형용사 조합 _count
request.params.orders_count      # 동적 파라미터 _count

# 잘못된 예시
response.event.success.count     # 조합 count에 .count 사용 금지
request.params.orders.count      # 동적 필드에 .count 사용 금지
```

---

## Field/Tag 일관성

### Usecase (Application/Adapter)

**Application 레이어:** (단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs))

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"application"`) |
| `request.category.name` | ✅ | ✅ | ✅ | 요청 카테고리 (`"usecase"`) |
| `request.category.type` | ✅ | ✅ | ✅ | CQRS 타입 (`"command"`, `"query"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | handler 메서드 이름 (`"Handle"`) |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | 경과 시간(초) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체(상세) |

**Adapter 레이어:** (단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservableObservableSignalgingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortTracingStructureTests.cs))

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"adapter"`) |
| `request.category.name` | ✅ | ✅ | ✅ | 카테고리 (예: `"repository"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | handler 메서드 이름 |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | 경과 시간(초) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체(상세) |

> **\* `response.elapsed`가 Metrics 태그가 아닌 이유:**
> - Metrics는 처리 시간을 캡처하기 위해 전용 `duration` **Histogram instrument를** 사용하며, 이는 지연 시간 측정에 대한 OpenTelemetry 권장 접근 방식입니다.
> - 경과 시간을 태그로 사용하면 **높은 카디널리티 폭발을** 유발합니다(각 고유한 duration 값이 새로운 시계열을 생성하여 메트릭 저장소 및 쿼리 성능이 저하됨).
> - Histogram은 개별 경과 값보다 모니터링에 더 유용한 **통계적 집계**(백분위수, 평균, 카운트)를 제공합니다.

### DomainEvent Publisher

(단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherTracingStructureTests.cs))

> DomainEvent Publisher는 Adapter 레이어로 분류되며, `request.layer`는 `"adapter"`, `request.category.name`는 `"event"`입니다.

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"adapter"`) |
| `request.category.name` | ✅ | ✅ | ✅ | 요청 카테고리 (`"event"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | Event 타입명 또는 Aggregate 타입명 |
| `request.handler.method` | ✅ | ✅ | ✅ | 메서드 이름 (`"Publish"`, `"PublishTrackedEvents"`) |
| `request.aggregate.count` | - | - | ✅ | Aggregate 유형 수 (PublishTrackedEvents 전용) |
| `request.event.count` | ✅ | - | ✅ | 배치 발행 시 이벤트 개수 (Aggregate 전용) |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | 경과 시간(초) |
| `response.event.success_count` | ✅ | - | ✅ | 부분 실패 시 성공한 이벤트 수 (Partial Failure 전용) |
| `response.event.failure_count` | ✅ | - | ✅ | 부분 실패 시 실패한 이벤트 수 (Partial Failure 전용) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체(상세) |

### DomainEvent Handler

(단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs))

> DomainEventHandler는 Application 레이어로 분류되며, `request.layer`는 `"application"`, `request.category.name`는 `"usecase"`, `request.category.type`은 `"event"`입니다.

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"application"`) |
| `request.category.name` | ✅ | ✅ | ✅ | 요청 카테고리 (`"usecase"`) |
| `request.category.type` | ✅ | ✅ | ✅ | CQRS 타입 (`"event"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | 메서드 이름 (`"Handle"`) |
| `request.event.type` | ✅ | - | ✅ | 이벤트 타입명 |
| `request.event.id` | ✅ | - | ✅ | 이벤트 고유 ID |
| `@request.message` | ✅ | - | - | 이벤트 객체 (요청 시) |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | - | 경과 시간(초) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |

> **Note:** DomainEventHandler의 `response.elapsed`는 Tracing Span 태그에 설정되지 않습니다 (Logging 전용). Span은 자체적으로 시작/종료 시간(duration)을 가지므로 별도의 elapsed 필드는 중복입니다.
> DomainEventHandler의 ErrorResponse는 Exception 객체가 직접 로깅됩니다 (`@error` 대신).
> DomainEventHandler는 `ValueTask` 반환이므로 `@response.message`를 기록하지 않습니다.

---

## ctx.* 사용자 정의 컨텍스트 필드 (3-Pillar)

### 개요

`ctx.*` 필드는 비즈니스 맥락을 Logging, Tracing, Metrics에 동시 전파하는 사용자 정의 컨텍스트 필드입니다. Source Generator가 Request/Response/DomainEvent의 공개 프로퍼티를 자동 감지하여 `IUsecaseCtxEnricher<TRequest, TResponse>` 또는 `IDomainEventCtxEnricher<TEvent>` 구현체를 생성합니다. `CtxEnricherPipeline`이 최선두 Pipeline으로 실행되어 후속 Metrics/Tracing/Logging Pipeline에서 ctx.* 데이터에 접근 가능합니다.

| 항목 | 설명 |
|------|------|
| 대상 Pillar | **Logging + Tracing** (기본값). Metrics는 `[CtxTarget]`으로 명시적 opt-in |
| 생성기 (Usecase) | `CtxEnricherGenerator` — `ICommandRequest<T>` / `IQueryRequest<T>` 구현 record 감지 |
| 생성기 (DomainEvent) | `DomainEventCtxEnricherGenerator` — `IDomainEventHandler<T>` 구현 클래스에서 `T` 감지 |
| 런타임 메커니즘 | `CtxEnricherContext.Push(name, value, pillars)` — Logging/Tracing/Metrics 동시 전파 |
| 대상 프로퍼티 | 공개 스칼라 및 컬렉션 프로퍼티 (복합 타입은 제외) |
| 파이프라인 순서 | `CtxEnricher → Metrics → Tracing → Logging → Validation → ... → Handler` |

### Pillar별 전파 메커니즘

| Pillar | 메커니즘 | 설명 |
|--------|----------|------|
| Logging | Serilog `LogContext.PushProperty` | 구조화 로그 필드로 출력 |
| Tracing | `Activity.Current?.SetTag` | Span Attribute로 출력 |
| MetricsTag | `MetricsTagContext` (AsyncLocal) → `TagList` 병합 | 기존 Counter/Histogram의 차원으로 추가 |
| MetricsValue | 별도 Histogram instrument에 값 기록 | 수치 필드를 통계적 집계 대상으로 기록 |

### Pillar 타겟팅: `CtxPillar` Enum

```csharp
[Flags]
public enum CtxPillar
{
    Logging      = 1,   // Serilog LogContext
    Tracing      = 2,   // Activity.SetTag
    MetricsTag   = 4,   // TagList 차원 (저카디널리티 전용)
    MetricsValue = 8,   // Histogram 값 기록 (수치 전용)

    Default = Logging | Tracing,        // 기본값
    All     = Logging | Tracing | MetricsTag,
}
```

### 필드 네이밍 규칙

| 범위 | 패턴 | 예시 (C# → ctx 필드) |
|------|------|----------------------|
| Usecase Request | `ctx.{containing_type}.request.{property}` | `PlaceOrderCommand.Request.CustomerId` → `ctx.place_order_command.request.customer_id` |
| Usecase Response | `ctx.{containing_type}.response.{property}` | `PlaceOrderCommand.Response.OrderId` → `ctx.place_order_command.response.order_id` |
| DomainEvent (최상위) | `ctx.{event}.{property}` | `OrderPlacedEvent.CustomerId` → `ctx.order_placed_event.customer_id` |
| DomainEvent (중첩) | `ctx.{containing_type}.{event}.{property}` | `Order.CreatedEvent.OrderId` → `ctx.order.created_event.order_id` |
| 인터페이스 범위 | `ctx.{interface (I 제거)}.{property}` | `IRegional.RegionCode` → `ctx.regional.region_code` |
| `[CtxRoot]` 승격 | `ctx.{property}` | `[CtxRoot] CustomerId` → `ctx.customer_id` |
| 컬렉션 | `{위 규칙}_count` 접미사 | `List<OrderLine> Lines` → `ctx.place_order_command.request.lines_count` |

> 모든 이름은 PascalCase → `snake_case`로 변환됩니다. 인터페이스 이름의 `I` 접두사는 제거됩니다 (`ICustomerRequest` → `customer_request`, `IX` → `x`).
> **필드 이름은 모든 Pillar에서 동일**합니다 — Logging, Tracing, Metrics에서 같은 `ctx.*` 이름을 사용합니다.

### 코드 예시

```csharp
[CtxRoot]
[CtxTarget(CtxPillar.All)]                          // 모든 프로퍼티 → 3-Pillar Tag
public interface IRegional
{
    string RegionCode { get; }                       // → ctx.region_code (Root + MetricsTag)
}

public sealed class PlaceOrderCommand
{
    public sealed record Request(
        string CustomerId,                           // 기본(L+T). 고카디널리티
        [CtxTarget(CtxPillar.All)] bool IsExpress,   // 3-Pillar Tag. boolean 안전
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        int ItemCount,                               // L+T + Histogram 기록
        List<OrderLine> Lines,                       // 기본(L+T). count 전파
        [CtxTarget(CtxPillar.Logging)] string InternalNote,  // Logging 전용
        [CtxIgnore] string DebugInfo                 // 완전 제외
    ) : ICommandRequest<Response>, IRegional;

    public sealed record Response(
        string OrderId,
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        decimal TotalAmount                          // L+T + Histogram 기록
    );
}
```

### Field/Tag 일관성

✅ = 지원, - = 미지원/해당없음

| ctx 필드 | 타입 | Logging | Tracing | Metrics Tag | Metrics Value |
|----------|------|---------|---------|-------------|---------------|
| `ctx.region_code` | keyword | ✅ | ✅ | **✅** | - |
| `ctx.place_order_command.request.customer_id` | keyword | ✅ | ✅ | - | - |
| `ctx.place_order_command.request.is_express` | boolean | ✅ | ✅ | **✅** | - |
| `ctx.place_order_command.request.item_count` | long | ✅ | ✅ | - | **✅** |
| `ctx.place_order_command.request.lines_count` | long | ✅ | ✅ | - | - |
| `ctx.place_order_command.request.internal_note` | keyword | ✅ | - | - | - |
| `ctx.place_order_command.response.order_id` | keyword | ✅ | ✅ | - | - |
| `ctx.place_order_command.response.total_amount` | double | ✅ | ✅ | - | **✅** |

### 타입 매핑 (C# → OpenSearch)

| C# 타입 | OpenSearch 타입 그룹 | 비고 |
|---------|---------------------|------|
| `bool` | `boolean` | |
| `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong` | `long` | |
| `float`, `double`, `decimal` | `double` | |
| `string` | `keyword` | |
| `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Uri` | `keyword` | |
| `enum` | `keyword` | |
| `Option<T>` (LanguageExt) | `keyword` | |
| `IValueObject`, `IEntityId<T>` 구현체 | `keyword` | `.ToString()` 호출 (DomainEvent 전용) |
| `Nullable<T>` | 내부 `T`에 위임 | |
| 컬렉션 (`List<T>`, `IReadOnlyList<T>` 등) | `long` | 요소 count 값 |
| 기타 복합 타입 (class, record, struct) | — (제외) | 스칼라/컬렉션만 대상 |

> 동일한 ctx 필드명에 서로 다른 타입 그룹이 할당되면 OpenSearch 동적 매핑 충돌이 발생합니다 (컴파일 타임 진단 `FUNCTORIUM002`).

### 제어 어트리뷰트

| 어트리뷰트 | 적용 대상 | 효과 |
|-----------|----------|------|
| `[CtxRoot]` | `interface`, `property`, `parameter` | 필드를 `ctx.{field}` 루트 레벨로 승격. Pillar 타겟팅과 독립 (네이밍에만 영향) |
| `[CtxIgnore]` | `class`, `property`, `parameter` | 모든 Pillar에서 제외. `[CtxTarget]`보다 우선 |
| `[CtxTarget(CtxPillar)]` | `interface`, `property`, `parameter` | Pillar 타겟 지정. 미지정 시 `Default` (Logging + Tracing) |

#### 결정 흐름

```
[CtxIgnore]? → YES → 모든 Pillar 제외
    ↓ NO
[CtxTarget] 지정됨? → YES → 지정된 Pillar
    ↓ NO
기본값 → CtxPillar.Default (Logging + Tracing)
```

프로퍼티/파라미터 수준 설정이 인터페이스 수준 설정보다 항상 우선합니다.

### 카디널리티 분류 규칙

| 카디널리티 수준 | 해당 타입 | MetricsTag | MetricsValue |
|---------------|----------|------------|--------------|
| `Fixed` | `bool` | 안전 | 불가 |
| `BoundedLow` | `enum` | 조건부 | 불가 |
| `Unbounded` | `string`, `Guid`, `DateTime`, `IValueObject`, `IEntityId<T>`, `Option<T>` | **경고** (FUNCTORIUM005) | 불가 |
| `Numeric` | `int`, `long`, `decimal`, `double` | **경고** (FUNCTORIUM005) | 허용 |
| `NumericCount` | 컬렉션 count | **경고** (FUNCTORIUM005) | 허용 |

### IDomainEvent 기본 속성 제외

DomainEvent Enricher는 `IDomainEvent` 인터페이스의 기본 속성을 자동으로 제외합니다. 이 속성들은 이미 표준 필드(`request.event.id` 등)로 출력됩니다.

| 제외 속성 | 이유 |
|----------|------|
| `OccurredAt` | 타임스탬프는 `@timestamp`로 별도 출력 |
| `EventId` | `request.event.id`로 별도 출력 |
| `CorrelationId` | 표준 상관 ID 필드로 별도 관리 |
| `CausationId` | 표준 인과 ID 필드로 별도 관리 |

### 안전망 (Safety Net)

`ctx.` 접두사 없이 `LogContext.PushProperty`로 푸시된 PascalCase 프로퍼티는 `OpenSearchJsonFormatter`가 자동으로 `ctx.snake_case`로 변환합니다.

```
CustomerId      → ctx.customer_id
OrderLineCount  → ctx.order_line_count
```

> Source Generator가 생성한 코드는 이미 `ctx.` 접두사를 포함하므로 안전망 변환 대상이 아닙니다. 이 변환은 수동으로 `LogContext.PushProperty`를 호출한 경우에만 적용됩니다.

### 확장 포인트 (Partial Methods)

생성된 Enricher 클래스는 `partial class`이며, 다음 확장 포인트를 제공합니다.

#### Partial Methods

| Enricher 유형 | Partial Method | 호출 시점 |
|--------------|---------------|----------|
| Usecase | `OnEnrichRequest(request, disposables)` | Request 처리 시작 시 |
| Usecase | `OnEnrichResponse(request, response, disposables)` | Response 처리 완료 시 |
| DomainEvent | `OnEnrich(domainEvent, disposables)` | Handler 실행 전 |

#### Helper Methods

| Enricher 유형 | Helper Method | 생성되는 필드 패턴 |
|--------------|---------------|------------------|
| Usecase | `PushRequestCtx(disposables, fieldName, value, pillars)` | `ctx.{type}.request.{fieldName}` |
| Usecase | `PushResponseCtx(disposables, fieldName, value, pillars)` | `ctx.{type}.response.{fieldName}` |
| DomainEvent | `PushEventCtx(disposables, fieldName, value, pillars)` | `ctx.{event}.{fieldName}` |
| 공통 | `PushRootCtx(disposables, fieldName, value, pillars)` | `ctx.{fieldName}` — `[CtxRoot]` 속성 존재 시에만 생성 |

### 컴파일 타임 진단

| 진단 코드 | 심각도 | 조건 | 설명 |
|----------|-------|------|------|
| `FUNCTORIUM002` | Warning | 동일 ctx 필드명에 서로 다른 OpenSearch 타입 그룹 할당 | 예: `ctx.customer_id`가 enricher A에서는 `keyword`, enricher B에서는 `long` |
| `FUNCTORIUM003` | Warning | Request 타입이 `private`/`protected` 접근 제한 | `[CtxIgnore]`를 Request record에 적용하여 경고 억제 |
| `FUNCTORIUM004` | Warning | Event 타입이 `private`/`protected` 접근 제한 | `[CtxIgnore]`를 Event record에 적용하여 경고 억제 |
| `FUNCTORIUM005` | Warning | 고카디널리티 타입 + `MetricsTag` | `string`/`Guid`/수치를 `MetricsTag`로 지정 시 카디널리티 폭발 경고 |
| `FUNCTORIUM006` | Error | 비수치 타입 + `MetricsValue` | `boolean`/`keyword`를 `MetricsValue`로 지정 시 에러 |
| `FUNCTORIUM007` | Warning | `MetricsTag` + `MetricsValue` 동시 지정 | 동일 프로퍼티에 Tag와 Value 동시 지정 시 경고 |

### 통합

| 통합 지점 | 호출 위치 | 설명 |
|----------|----------|------|
| `CtxEnricherPipeline` | Application 레이어 (최선두) | `IUsecaseCtxEnricher<TRequest, TResponse>` DI 주입. `EnrichRequest`/`EnrichResponse` 호출로 3-Pillar 동시 전파 |
| `ObservableDomainEventNotificationPublisher` | DomainEvent Handler | `IDomainEventCtxEnricher<TEvent>`를 런타임 해석 후 `Enrich` 호출 |
| `UsecaseMetricsPipeline` | Application 레이어 | `MetricsTagContext`에서 ctx.* MetricsTag를 읽어 기존 TagList에 병합 |
| `LogTestContext` | 테스트 | `enrichFromLogContext: true` 옵션으로 ctx.* 필드 캡처 및 검증 |

---

## Logging

### Usecase Logging

#### Field 구조

| Field Name | Application 레이어 | Adapter 레이어 | 설명 |
|------------|-------------------|---------------|------|
| **Static Fields** | | | |
| `request.layer` | `"application"` | `"adapter"` | 요청 레이어 식별자 |
| `request.category.name` | `"usecase"` | 카테고리 이름 | 요청 카테고리 식별자 |
| `request.category.type` | `"command"` / `"query"` | - | CQRS 타입 |
| `request.handler.name` | handler 이름 | handler 이름 | handler 클래스 이름 |
| `request.handler.method` | `"Handle"` | 메서드 이름 | handler 메서드 이름 |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | 응답 상태 |
| `response.elapsed` | 경과 시간(초) | 경과 시간(초) | 경과 시간(초) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | 오류 분류 |
| `error.code` | 오류 코드 | 오류 코드 | 도메인 특화 오류 코드 |
| `@error` | 오류 객체(구조화) | 오류 객체(구조화) | 오류 데이터(상세) |
| **Dynamic Fields** | | | |
| `@request.message` | 전체 Command/Query 객체 | 전체 파라미터 객체 (Debug) | 요청 메시지 |
| `@response.message` | 전체 응답 객체 | 메서드 반환 값 (Debug) | 응답 메시지 |
| `@request.params` | - | type-filtered 파라미터 복합 객체 (Info/Debug) | 요청 파라미터 |

#### 이벤트별 로그 레벨

| Event | Log 수준 | Application 레이어 | Adapter 레이어 | 설명 |
|-------|-----------|-------------------|---------------|------|
| Request | Information | 1001 `application.request` | 2001 `adapter.request` | 요청 수신 |
| Request (Debug) | Debug | - | 2001 `adapter.request` | 파라미터 값이 포함된 요청 |
| Response Success | Information | 1002 `application.response.success` | 2002 `adapter.response.success` | 성공 응답 |
| Response Success (Debug) | Debug | - | 2002 `adapter.response.success` | 결과 값이 포함된 응답 |
| Response Warning | Warning | 1003 `application.response.warning` | 2003 `adapter.response.warning` | 예상 오류(비즈니스 로직) |
| Response Error | Error | 1004 `application.response.error` | 2004 `adapter.response.error` | 예외 오류(시스템 장애) |

#### Message Templates (Application)

```
# Request
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} requesting with {@request.message}

# Response - Success
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}

# Response - Warning/Error
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### Message Templates (Adapter)

```
# Request (Information) - 5 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting

# Request (Debug) - 6 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting with {@request.message}

# Response (Information) - 6 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response (Debug) - 7 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}

# Response Warning/Error
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| Application | 직접 `ILogger.LogXxx()` 호출 | [UsecaseLoggingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs) | 7개 이상의 파라미터가 `LoggerMessage.Define`의 6개 제한을 초과 |
| Adapter | `LoggerMessage.Define` 델리게이트 | [ObservableObservableSignalgingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservableObservableSignalgingStructureTests.cs) | 제로 할당, 고성능 |

### DomainEvent Logging

#### Field 비교

**Application Usecase vs DomainEvent Publisher vs DomainEventHandler 필드 비교:**

✅ = 지원, ✅ (조건) = 조건부 지원, - = 미지원/해당없음

| Field | Application Usecase | DomainEvent Publisher | DomainEventHandler |
|-------|---------------------|----------------------|-------------------|
| `request.layer` | `"application"` | `"adapter"` | `"application"` |
| `request.category.name` | `"usecase"` | `"event"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | - | `"event"` |
| `request.handler.name` | handler 클래스명 | Event/Aggregate 타입명 | handler 클래스명 |
| `request.handler.method` | `"Handle"` | `"Publish"` / `"PublishTrackedEvents"` | `"Handle"` |
| `@request.message` | Command/Query 객체 | 이벤트 객체 | 이벤트 객체 |
| `@response.message` | 응답 객체 | - | - |
| `request.event.count` | - | ✅ (Aggregate만) | - |
| `response.event.success_count` | - | ✅ (Partial Failure만) | - |
| `response.event.failure_count` | - | ✅ (Partial Failure만) | - |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | `"success"` / `"failure"` |
| `response.elapsed` | 경과 시간(초) | 경과 시간(초) | 경과 시간(초) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` | `"expected"` / `"exceptional"` |
| `error.code` | 오류 코드 | 오류 코드 | 오류 코드 |
| `@error` | 오류 객체 | 오류 객체 | 오류 객체 (Exception) |

> Error 분류 상세는 [Error 분류](#error-분류) 섹션 참조.

#### Message Templates (Publisher)

```
# Request - 단일 이벤트
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {@request.message}

# Request - Aggregate 다중 이벤트
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {request.event.count} events

# Response - Success
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response - Success (Aggregate)
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events

# Response - Warning/Error
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}

# Response - Warning/Error (Aggregate)
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}

# Response - Partial Failure (Aggregate)
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events partial failure: {response.event.success_count} succeeded, {response.event.failure_count} failed
```

#### Publisher Event IDs

> DomainEvent Publisher는 Adapter 레이어로 분류되므로, Adapter 레이어와 동일한 Event ID를 사용합니다.

| Event | ID | Name |
|-------|-----|------|
| Request | 2001 | `adapter.request` |
| Success | 2002 | `adapter.response.success` |
| Warning | 2003 | `adapter.response.warning` |
| Error | 2004 | `adapter.response.error` |

#### Message Templates (Handler)

> DomainEventHandler는 Publisher가 발행한 이벤트를 처리하는 Handler 관점의 로깅입니다. `request.layer`는 `"application"`, `request.category.name`는 `"usecase"`, `request.category.type`은 `"event"`입니다.

```
# Request
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} requesting with {@request.message}

# Response - Success
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s

# Response - Warning/Error
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### Handler Event IDs

> DomainEventHandler는 Application 레이어의 usecase로 분류되므로, Application 레이어와 동일한 Event ID를 사용합니다.

| Event | ID | Name |
|-------|-----|------|
| Request | 1001 | `application.request` |
| Success | 1002 | `application.response.success` |
| Warning | 1003 | `application.response.warning` |
| Error | 1004 | `application.response.error` |

#### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| DomainEvent Publisher | Decorator | [DomainEventPublisherLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs) | Adapter 레이어 패턴 |
| DomainEvent Handler | `INotificationPublisher` | [DomainEventHandlerLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs) | Application 레이어 패턴 |
| DomainEvent Handler Enricher | `IDomainEventCtxEnricher<TEvent>` | [DomainEventHandlerEnricherLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerEnricherLoggingStructureTests.cs) | CtxEnricherContext 기반 3-Pillar Enrichment |

---

## Metrics

### Meter Name

| 레이어 | Meter Name 패턴 | 예시 (`ServiceNamespace = "mycompany.production"`) |
|-------|-----------------|---------------------------------------------------|
| Application | `{service.namespace}.application` | `mycompany.production.application` |
| Adapter | `{service.namespace}.adapter.{category}` | `mycompany.production.adapter.repository` |
| DomainEvent Publisher | `{service.namespace}.adapter.event` | `mycompany.production.adapter.event` |
| DomainEvent Handler | `{service.namespace}.application` | `mycompany.production.application` |

### Instrument 구조

| Instrument | Application 레이어 | Adapter 레이어 | DomainEvent Publisher | DomainEvent Handler | Type | Unit |
|------------|-------------------|---------------|----------------------|--------------------|------|------|
| requests | `application.usecase.{type}.requests` | `adapter.{category}.requests` | `adapter.event.requests` | `application.usecase.event.requests` | Counter | `{request}` |
| responses | `application.usecase.{type}.responses` | `adapter.{category}.responses` | `adapter.event.responses` | `application.usecase.event.responses` | Counter | `{response}` |
| duration | `application.usecase.{type}.duration` | `adapter.{category}.duration` | `adapter.event.duration` | `application.usecase.event.duration` | Histogram | `s` |

### Usecase Metrics

#### Tag 구조 (Application)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` | `"application"` |
| `request.category.name` | `"usecase"` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` |
| `request.handler.name` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | Primary 오류 코드 |
| **Total Tags** | **5** | **5** | **6** | **8** |

#### Tag 구조 (Adapter)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | 카테고리 이름 | 카테고리 이름 | 카테고리 이름 | 카테고리 이름 |
| `request.handler.name` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | 메서드 이름 | 메서드 이름 | 메서드 이름 | 메서드 이름 |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **4** | **4** | **5** | **7** |

> Error 분류 상세는 [Error 분류](#error-분류) 섹션 참조.

### DomainEvent Metrics

#### Tag 구조 (Publisher)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | 메서드 이름 | 메서드 이름 | 메서드 이름 | 메서드 이름 |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **4** | **4** | **5** | **7** |

> **DomainEvent Metrics에서 제외되는 태그:**
> `request.event.count`, `response.event.success_count`, `response.event.failure_count`는 Metrics 태그로 사용하지 않습니다.
> 이 값들은 각각 고유한 수치를 가지므로 태그로 사용하면 **높은 카디널리티 폭발을** 유발합니다.
> 이는 `response.elapsed`를 Metrics 태그로 사용하지 않는 것과 동일한 원칙입니다.

#### Tag 구조 (Handler)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` | `"application"` |
| `request.category.name` | `"usecase"` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **5** | **5** | **6** | **8** |

### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| Application | `IPipelineBehavior` + `IMeterFactory` | [UsecaseMetricsPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs) | Mediator pipeline |
| Adapter | Source Generator | [ObservablePortMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortMetricsStructureTests.cs) | 자동 생성된 metrics instruments |
| DomainEvent Publisher | Decorator + `IMeterFactory` | [DomainEventPublisherMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherMetricsStructureTests.cs) | Adapter 레이어 패턴 |
| DomainEvent Handler | `INotificationPublisher` + `IMeterFactory` | [DomainEventHandlerMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs) | Application 레이어 패턴 |

---

## Tracing

### Usecase Tracing

#### Span 구조

| Property | Application 레이어 | Adapter 레이어 |
|----------|-------------------|---------------|
| Span Name | `{layer} {category}.{type} {handler}.{method}` | `{layer} {category} {handler}.{method}` |
| Example | `application usecase.command CreateOrderCommandHandler.Handle` | `adapter repository OrderRepository.GetById` |
| Kind | `Internal` | `Internal` |

> **Span Name 형식 차이:** Application 레이어는 `.{type}` 세그먼트(command/query/event)를 포함하지만, Adapter 레이어는 CQRS 타입 구분이 없으므로 `.{type}` 세그먼트가 생략됩니다.

#### Tag 구조

| Tag Key | Application 레이어 | Adapter 레이어 | 설명 |
|---------|-------------------|---------------|------|
| **Request Tags** | | | |
| `request.layer` | `"application"` | `"adapter"` | 레이어 식별자 |
| `request.category.name` | `"usecase"` | 카테고리 이름 | 카테고리 식별자 |
| `request.category.type` | `"command"` / `"query"` | - | CQRS 타입 |
| `request.handler.name` | handler 이름 | handler 이름 | handler 클래스 이름 |
| `request.handler.method` | `"Handle"` | 메서드 이름 | 메서드 이름 |
| **Response Tags** | | | |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | 응답 상태 |
| `response.elapsed` | 경과 시간(초) | 경과 시간(초) | 경과 시간(초) |
| **Error Tags** | | | |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | 오류 분류 |
| `error.code` | 오류 코드 | 오류 코드 | 오류 코드 |
| **ActivityStatus** | `Ok` / `Error` | `Ok` / `Error` | OpenTelemetry 상태 |

> Error 분류 상세는 [Error 분류](#error-분류) 섹션 참조.

### DomainEvent Tracing

#### Publisher Span 구조

| Property | Publish | PublishTrackedEvents |
|----------|---------|---------------------|
| Span Name | `adapter event {EventType}.Publish` | `adapter event PublishTrackedEvents.PublishTrackedEvents` |
| Kind | `Internal` | `Internal` |

#### Publisher Tag 구조 (Publish)

| Tag Key | Request | Success Response | Failure Response |
|---------|---------|-----------------|-----------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | event type name | event type name | event type name |
| `request.handler.method` | `"Publish"` | `"Publish"` | `"Publish"` |
| `response.elapsed` | - | 경과 시간(초) | 경과 시간(초) |
| `response.status` | - | `"success"` | `"failure"` |
| `error.type` | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | 오류 코드 |
| **Total Tags** | **4** | **6** | **8** |

#### Publisher Tag 구조 (PublishTrackedEvents)

| Tag Key | Request | Success | Partial Failure | Total Failure |
|---------|---------|---------|-----------------|---------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` |
| `request.handler.method` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` |
| `request.aggregate.count` | aggregate count | aggregate count | aggregate count | aggregate count |
| `request.event.count` | event count | event count | event count | event count |
| `response.elapsed` | - | 경과 시간(초) | 경과 시간(초) | 경과 시간(초) |
| `response.status` | - | `"success"` | `"failure"` | `"failure"` |
| `response.event.success_count` | - | - | success count | - |
| `response.event.failure_count` | - | - | failure count | - |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **6** | **8** | **10** | **10** |

#### Handler Span 구조

| Property | 설명 |
|----------|------|
| Span Name | `application usecase.event {HandlerName}.Handle` |
| Kind | `Internal` |

#### Handler Tag 구조

| Tag Key | Success | Failure |
|---------|---------|---------|
| `request.layer` | `"application"` | `"application"` |
| `request.category.name` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"event"` | `"event"` |
| `request.handler.name` | handler name | handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` |
| `request.event.type` | event type name | event type name |
| `request.event.id` | event id | event id |
| `response.status` | `"success"` | `"failure"` |
| `error.type` | - | `"expected"` / `"exceptional"` |
| `error.code` | - | 오류 코드 |
| **Total Tags** | **8** | **10** |

> **Note:** Handler의 `response.elapsed`는 Activity 태그에 설정되지 않습니다 (Logging 전용).

### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| Application | `IPipelineBehavior` + `ActivitySource.StartActivity()` | [UsecaseTracingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs) | Mediator pipeline |
| Adapter | Source Generator | [ObservablePortTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortTracingStructureTests.cs) | 자동 생성된 Activity spans |
| DomainEvent Publisher | Decorator + `ActivitySource.StartActivity()` | [DomainEventPublisherTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherTracingStructureTests.cs) | Adapter 레이어 패턴 |
| DomainEvent Handler | `INotificationPublisher` + `ActivitySource.StartActivity()` | [DomainEventHandlerTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs) | Application 레이어 패턴 |

---

## 관련 코드

### 코드 위치

| 구성 요소 | 파일 경로 |
|----------|----------|
| 필드 이름 생성 헬퍼 | `Src/Functorium.SourceGenerators/Generators/ObservablePortGenerator/CollectionTypeHelper.cs` |
| Application Logging | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseLoggingPipeline.cs` |
| Adapter Logging | Source Generator 생성 코드 |
| Application Metrics | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseMetricsPipeline.cs` |
| Application Tracing | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseTracingPipeline.cs` |
| DomainEvent Publisher | `Src/Functorium.Adapters/Observabilities/Events/ObservableDomainEventPublisher.cs` |
| Custom Pipeline 마커 | `Src/Functorium.Adapters/Observabilities/Pipelines/ICustomUsecasePipeline.cs` |
| Ctx Enricher Pipeline | `Src/Functorium.Adapters/Observabilities/Pipelines/CtxEnricherPipeline.cs` |
| Ctx Enricher 인터페이스 | `Src/Functorium/Applications/Observabilities/IUsecaseCtxEnricher.cs` |
| DomainEvent Ctx Enricher 인터페이스 | `Src/Functorium/Applications/Observabilities/IDomainEventCtxEnricher.cs` |
| CtxEnricher Source Generator | `Src/Functorium.SourceGenerators/Generators/CtxEnricherGenerator/CtxEnricherGenerator.cs` |
| DomainEvent CtxEnricher Source Generator | `Src/Functorium.SourceGenerators/Generators/DomainEventCtxEnricherGenerator/DomainEventCtxEnricherGenerator.cs` |
| CtxEnricherContext | `Src/Functorium/Applications/Observabilities/CtxEnricherContext.cs` |
| MetricsTagContext | `Src/Functorium.Adapters/Observabilities/Contexts/MetricsTagContext.cs` |
| CtxPillar enum | `Src/Functorium/Applications/Observabilities/CtxPillar.cs` |
| CtxRoot 어트리뷰트 | `Src/Functorium/Applications/Observabilities/CtxRootAttribute.cs` |
| CtxIgnore 어트리뷰트 | `Src/Functorium/Applications/Observabilities/CtxIgnoreAttribute.cs` |
| CtxTarget 어트리뷰트 | `Src/Functorium/Applications/Observabilities/CtxTargetAttribute.cs` |
| Tracing Custom Base | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseTracingCustomPipelineBase.cs` |
| Metric Custom Base | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseMetricCustomPipelineBase.cs` |
| Pipeline 설정 | `Src/Functorium.Adapters/Observabilities/Builders/Configurators/PipelineConfigurator.cs` |

### 테스트

| 테스트 | 파일 경로 |
|--------|----------|
| Application Logging 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs` |
| Adapter Logging 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservableObservableSignalgingStructureTests.cs` |
| Application Metrics 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs` |
| Adapter Metrics 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortMetricsStructureTests.cs` |
| Application Tracing 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs` |
| Adapter Tracing 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortTracingStructureTests.cs` |
| DomainEvent Publisher Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs` |
| DomainEvent Handler Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs` |
| CtxEnricher Source Generator | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/CtxEnricherGeneratorTests.cs` |
| DomainEvent CtxEnricher Source Generator | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/DomainEventCtxEnricherGeneratorTests.cs` |
| Ctx Enricher 통합 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineEnricherTests.cs` |
| DomainEvent Handler Enricher Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerEnricherLoggingStructureTests.cs` |
| DomainEvent Handler Enricher Metrics | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs` |
| DomainEvent Handler Enricher Tracing | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs` |
| Tracing Custom Base | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingCustomPipelineBaseTests.cs` |
| Pipeline 설정 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Configurators/PipelineConfiguratorTests.cs` |

---

## ObservableSignal — Adapter 구현 내부 개발자 로깅 API

### 개요

`ObservableSignal`은 Adapter 구현 코드 내부에서 개발자가 직접 운영 목적의 로그를 출력하는 정적 API입니다. Observable 래퍼가 설정한 공통 컨텍스트(`request.layer`, `request.category.name`, `request.handler.name`, `request.handler.method`)를 자동으로 포함합니다.

### Pillar 범위

✅ = 지원, X = 미지원

| Level | Logging | Tracing (Activity Event) | Metrics |
|-------|---------|--------------------------|---------|
| Debug | ✅ | X (고빈도 → 노이즈) | X |
| Warning | ✅ | ✅ (span 내 열화 원인 추적) | X |
| Error | ✅ | ✅ (span 내 실패 원인 추적) | X |

- **Metrics 제외**: Observable 래퍼가 request/response/duration 메트릭을 이미 자동 생성. 커스텀 메트릭은 `IMeterFactory` 직접 사용.
- **Debug에서 Tracing 제외**: 캐시 미스 등 고빈도 이벤트를 span에 넣으면 trace 노이즈.

### EventId

| EventId | Name | Level | 설명 |
|---------|------|-------|------|
| 2021 | `adapter.signal.debug` | Debug | 정상 흐름 상세 (캐시 미스, 쿼리 상세) |
| 2022 | `adapter.signal.warning` | Warning | 자동 복구 열화 (재시도, 폴백, rate limit) |
| 2023 | `adapter.signal.error` | Error | 복구 불가 실패 (재시도 소진, 서킷 오픈) |

### 메시지 템플릿

```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} — {adapter.log.message} {@adapter.log.context}
```

### 부가 필드 네이밍: `adapter.*`

| 프리픽스 | 용도 | 예시 |
|----------|------|------|
| `adapter.retry.*` | 재시도 관련 | `adapter.retry.attempt`, `adapter.retry.delay_ms` |
| `adapter.http.*` | HTTP 관련 | `adapter.http.status_code`, `adapter.http.retry_after_seconds` |
| `adapter.message.*` | 메시지 브로커 관련 | `adapter.message.id`, `adapter.message.queue` |
| `adapter.db.*` | 데이터베이스 관련 | `adapter.db.elapsed_ms`, `adapter.db.operation` |
| `adapter.cache.*` | 캐시 관련 | `adapter.cache.key`, `adapter.cache.provider` |

### 사용 예시

```csharp
// Polly 재시도 시 Warning
ObservableSignal.Warning("Retry attempt {Attempt}/{MaxRetry} after {Delay}s delay",
    ("adapter.retry.attempt", attempt),
    ("adapter.retry.delay_ms", delay.TotalMilliseconds));

// 캐시 미스 시 Debug (고빈도)
ObservableSignal.Debug("Cache miss", ("adapter.cache.key", cacheKey));

// 재시도 소진 시 Error
ObservableSignal.Error(ex, "Database operation failed after exhausting retries",
    ("adapter.db.retry.attempt", maxRetries));
```

### 동작 원리

1. `[GenerateObservablePort]` Source Generator가 `ExecuteWithSpan` 내에서 `ObservableSignalScope.Begin()`을 호출
2. `ObservableSignalScope`가 `AsyncLocal`로 현재 컨텍스트(logger, layer, category, handler, method)를 설정
3. Adapter 코드에서 `ObservableSignal.Debug/Warning/Error` 호출 시 `ObservableSignalScope.Current`에서 공통 필드 획득
4. `ObservableSignalFactory`가 ILogger + Activity Event로 출력

### 테스트 참조

| 테스트 | 파일 |
|--------|------|
| ObservableSignal API + Scope | `Tests/Functorium.Tests.Unit/DomainsTests/Observabilities/ObservableSignalTests.cs` |

---

## 관련 문서

- [Logging 가이드](../guides/observability/19-observability-logging) — 구조화된 로깅 상세 가이드
- [Metrics 가이드](../guides/observability/20-observability-metrics) — 메트릭 수집 및 분석 가이드
- [Tracing 가이드](../guides/observability/21-observability-tracing) — 분산 추적 상세 가이드
- [파이프라인 사양](./07-pipeline) — Pipeline 실행 순서, OpenTelemetryOptions, 커스텀 확장 포인트
- [코드 명명 규칙](../guides/observability/18b-observability-naming) — Observability 코드 명명 규칙
