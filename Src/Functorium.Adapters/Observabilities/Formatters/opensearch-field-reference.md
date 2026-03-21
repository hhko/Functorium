# OpenSearch 로그 필드 레퍼런스

`OpenSearchJsonFormatter`가 출력하는 JSON 문서의 필드 정의입니다.

## 필드 출처 분류

| 분류 | 출처 | 설명 |
|------|------|------|
| ECS | [Elastic Common Schema](https://www.elastic.co/guide/en/ecs/current/index.html) | 로그 플랫폼 표준 필드 |
| OTel | [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/) | 관찰가능성 표준 속성 |
| Custom | `ObservabilityNaming.CustomAttributes` | Functorium 3-Pillar 공통 속성 |
| Formatter | `OpenSearchJsonFormatter` | 포맷터 변환 규칙에 의한 파생 필드 |
| Dynamic | `IUsecaseLogEnricher` | 사용자 정의 컨텍스트 필드 |

## ECS 표준 필드

| 필드 | 타입 | 인덱스 | 출처 | 설명 | 예시 |
|------|------|--------|------|------|------|
| `@timestamp` | `date` | O | ECS | UTC ISO 8601 타임스탬프 | `2026-03-20T09:15:30.123Z` |
| `log.level` | `keyword` | O | ECS | 로그 레벨 | `Information`, `Warning`, `Error` |
| `log.logger` | `keyword` | O | ECS | 로거 이름 (SourceContext) | `ObservabilityHost.PlaceOrderCommand` |
| `message` | `text` | O | ECS | 렌더링된 메시지 (전문 검색 가능) | `application usecase.command PlaceOrder.Handle requesting with {...}` |
| `message.template` | `keyword` | O | ECS | 원본 메시지 템플릿 | `{request.layer} {request.category}.{request.category_type} ...` |
| `event.id` | `integer` | O | ECS | .NET EventId (Serilog EventId 구조체) | `1001`, `2001` |
| `event.name` | `keyword` | O | ECS | .NET 이벤트 이름 | `ApplicationRequest`, `AdapterResponseSuccess` |
| `error.stack_trace` | `text` | X | ECS | 예외 스택 트레이스 (저장 전용) | `System.InvalidOperationException: ...` |

## Request 필드

| 필드 | 타입 | 인덱스 | 출처 | 설명 | 예시 |
|------|------|--------|------|------|------|
| `request.layer` | `keyword` | O | Custom | 레이어 | `application`, `adapter` |
| `request.category.name` | `keyword` | O | Custom | 카테고리 | `usecase`, `event`, `repository` |
| `request.category.type` | `keyword` | O | Custom | 카테고리 세부 타입 | `command`, `query` |
| `request.handler.name` | `keyword` | O | Custom | 핸들러 이름 | `PlaceOrderCommand`, `OrderRepository` |
| `request.handler.method` | `keyword` | O | Custom | 핸들러 메서드 | `Handle`, `GetById` |
| `request.message` | `text` | X | Custom | 요청 객체 (JSON 문자열, 저장 전용) | `{"Name":"TestName","Quantity":5}` |
| `request.params` | `text` | X | Custom | 타입 필터링된 파라미터 (JSON 문자열, 저장 전용) | `{"customer_id":"01KM834T..."}` |

## Response 필드

| 필드 | 타입 | 인덱스 | 출처 | 설명 | 예시 |
|------|------|--------|------|------|------|
| `response.status` | `keyword` | O | Custom | 응답 상태 | `success`, `failure` |
| `response.elapsed` | `double` | O | Custom | 처리 시간 (초) | `0.0023` |
| `response.message` | `text` | X | Custom | 응답 객체 (JSON 문자열, 저장 전용) | `{"OrderId":"01H5...","Total":150}` |

## Error 필드

| 필드 | 타입 | 인덱스 | 출처 | 설명 | 예시 |
|------|------|--------|------|------|------|
| `error.type` | `keyword` | O | OTel | 에러 타입 분류 | `ExpectedError`, `ExceptionalError` |
| `error.code` | `keyword` | O | Custom | 에러 코드 | `ORDER_NOT_FOUND`, `DB_CONNECTION_FAILED` |
| `error.detail` | `text` | X | Formatter | 구조화 에러 객체 (JSON 문자열, 저장 전용) | `{"ErrorType":"ExpectedError","Code":"..."}` |
| `error.stack_trace` | `text` | X | ECS | 예외 스택 트레이스 (저장 전용) | `System.InvalidOperationException: ...` |

## Event 필드 (Domain Event 전용)

| 필드 | 타입 | 인덱스 | 출처 | 설명 | 예시 |
|------|------|--------|------|------|------|
| `request.event.type` | `keyword` | O | Custom | 도메인 이벤트 타입 | `OrderPlacedEvent` |
| `request.event.id` | `keyword` | O | Custom | 도메인 이벤트 ID (Ulid) | `01H5KXYZ...` |
| `request.event.count` | `integer` | O | Custom | 배치 이벤트 수 | `3` |
| `response.event.success_count` | `integer` | O | Custom | 배치 성공 수 | `2` |
| `response.event.failure_count` | `integer` | O | Custom | 배치 실패 수 | `1` |

## Context 필드 (ctx.*)

`IUsecaseLogEnricher`를 통해 주입되는 사용자 정의 컨텍스트 필드입니다.

| 필드 | 타입 | 인덱스 | 출처 | 설명 |
|------|------|--------|------|------|
| `ctx.*` | `keyword` (문자열) | O | Dynamic | Enricher가 `ctx.` 접두사로 푸시한 필드 |
| `ctx.*` | `long`/`double`/`boolean` | O | Dynamic | 비문자열 타입은 OpenSearch 기본 매핑 적용 |

### ctx 필드 예시 (PlaceOrderCommand)

```json
{
  "ctx.place_order_command.request.customer_id": "CUST-001",
  "ctx.place_order_command.request.lines_count": 2,
  "ctx.place_order_command.request.order_total_amount": 150.00
}
```

### ctx 안전망

`ctx.` 접두사 없이 푸시된 PascalCase 프로퍼티는 포맷터가 자동 변환합니다.

```
CustomerId      → ctx.customer_id
OrderLineCount  → ctx.order_line_count
```

## 필드 변환 규칙

```
Serilog LogEvent
├── Timestamp           → @timestamp (UTC)
├── Level               → log.level
├── MessageTemplate     → message.template
├── (렌더링)              → message
├── Exception           → error.stack_trace
└── Properties
    ├── EventId (구조체)
    │   ├── Id          → event.id
    │   └── Name        → event.name
    ├── SourceContext   → log.logger
    ├── request.*       → request.* (그대로)
    ├── response.*      → response.* (그대로)
    ├── error.type      → error.type (그대로)
    ├── error.code      → error.code (그대로)
    ├── error (객체)     → error.detail (JSON 문자열화)
    ├── ctx.*           → ctx.* (그대로)
    ├── PascalCase      → ctx.snake_case (안전망)
    ├── _typeTag        → (제거)
    └── $type           → (제거)
```

## 매핑 정책

| 정책 | 설정 | 근거 |
|------|------|------|
| 루트 dynamic | `false` | 미식별 필드는 저장만, 인덱싱 안 함 |
| ctx dynamic | `true` | Enricher 필드는 자동 매핑 |
| ctx 문자열 | `keyword` | 필터/집계 최적화 (dynamic_template) |
| `request.message`, `response.message` | `index: false` | JSON 문자열화된 구조체 — 매핑 폭발 방지 |
| `error.stack_trace`, `error.detail` | `index: false` | 대용량 텍스트 — 저장 전용 |
| `message` | `text` | 렌더링된 메시지 전문 검색 |
| `message.template` | `keyword` | 템플릿별 집계/필터링 |

## 인덱스 템플릿 적용

```bash
# opensearch-index-template.json을 인덱스 템플릿으로 등록
curl -X PUT "https://localhost:9200/_index_template/functorium-logs" \
  -H "Content-Type: application/json" \
  -d @opensearch-index-template.json
```
