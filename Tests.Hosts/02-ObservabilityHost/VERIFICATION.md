# 02-ObservabilityHost 검증 결과

**검증 일시**: 2026-03-19 23:22 KST
**실행 명령**: `dotnet run --project Tests.Hosts/02-ObservabilityHost/Src/ObservabilityHost`

---

## 1. 빌드 검증

```
dotnet build Functorium.slnx
```

| 항목 | 결과 |
|------|------|
| 빌드 | PASS (0 errors, 0 warnings) |
| 기존 테스트 | PASS (1446 passed, 0 failed, 25 skipped) |

---

## 2. 콘솔 출력 전문

### 2.1 원본 출력 (Raw)

```
=== PlaceOrderCommand (Custom Observability) ===
[23:22:06 INF] application usecase.command PlaceOrderCommand.Handle requesting with {"CustomerId": "CUST-001", "Lines": [{"ProductId": "PROD-A", "Quantity": 2, "UnitPrice": 100.00, "$type": "OrderLine"}, {"ProductId": "PROD-B", "Quantity": 1, "UnitPrice": 250.00, "$type": "OrderLine"}], "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.order_line_count": 2, "ctx.customer_id": "CUST-001"}
[23:22:06 INF] application usecase.command PlaceOrderCommand.Handle responded success in 0.0127 s with {"Value": {"OrderId": "d7f7b93f-...", "LineCount": 2, "TotalAmount": 450.00, "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.order_total_amount": 450.00, "ctx.customer_id": "CUST-001"}
PlaceOrder Result: Succ(Response { OrderId = d7f7b93f-..., LineCount = 2, TotalAmount = 450.00 })

=== GetOrderSummaryQuery (Baseline) ===
[23:22:06 INF] application usecase.query GetOrderSummaryQuery.Handle requesting with {"OrderId": "ORD-123", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[23:22:06 INF] application usecase.query GetOrderSummaryQuery.Handle responded success in 0.0004 s with {"Value": {"OrderId": "ORD-123", "Status": "Completed", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
GetOrderSummary Result: Succ(Response { OrderId = ORD-123, Status = Completed })

=== FailExpectedCommand (Expected Error) ===
[23:22:06 INF] application usecase.command FailExpectedCommand.Handle requesting with {"OrderId": "ORD-NOT-EXIST", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[23:22:06 WRN] application usecase.command FailExpectedCommand.Handle responded failure in 0.0019 s with expected:Order.NotFound {"ErrorType": "ErrorCodeExpected", ...} {"EventId": {"Id": 1003, "Name": "application.response.warning"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
FailExpected Result: Fail(주문을 찾을 수 없습니다)

=== FailExceptionalCommand (Exceptional Error) ===
[23:22:06 INF] application usecase.command FailExceptionalCommand.Handle requesting with {"OrderId": "ORD-DB-FAIL", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[23:22:06 ERR] application usecase.command FailExceptionalCommand.Handle responded failure in 0.0212 s with exceptional:Database.ConnectionFailed {"ErrorType": "ErrorCodeExceptional", ...} {"EventId": {"Id": 1004, "Name": "application.response.error"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
FailExceptional Result: Fail(데이터베이스 연결이 끊어졌습니다)

=== Done ===
```

---

## 3. 파일 로그 출력 검증

### 3.1 파일 생성 확인

| 파일 | 경로 | 생성 | 결과 |
|------|------|------|------|
| Plain text | `logs/log-20260319.txt` | 생성됨 | PASS |
| JSON | `logs/log-20260319.json` | 생성됨 | PASS |

### 3.2 JSON 로그 파일 전문 (`logs/log-20260319.json`)

**Formatter**: `OpenSearchJsonFormatter` (ECS 호환 + `ctx.*` 네임스페이스)

#### PlaceOrderCommand Request

```json
{
  "@timestamp": "2026-03-19T14:22:06.6958763Z",
  "log.level": "Information",
  "message": "application usecase.command PlaceOrderCommand.Handle requesting with Request { CustomerId: \"CUST-001\", Lines: [...] }",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "PlaceOrderCommand",
  "request.handler_method": "Handle",
  "request.message": "{\"CustomerId\":\"CUST-001\",\"Lines\":[{\"ProductId\":\"PROD-A\",\"Quantity\":2,\"UnitPrice\":100.00},{\"ProductId\":\"PROD-B\",\"Quantity\":1,\"UnitPrice\":250.00}]}",
  "ctx.order_line_count": 2,
  "ctx.customer_id": "CUST-001",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

#### PlaceOrderCommand Response (성공)

```json
{
  "@timestamp": "2026-03-19T14:22:06.8346266Z",
  "log.level": "Information",
  "message": "application usecase.command PlaceOrderCommand.Handle responded success in 0.0127 s with Succ { Value: Response { ... }, IsSucc: True, IsFail: False }",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "PlaceOrderCommand",
  "request.handler_method": "Handle",
  "response.status": "success",
  "response.elapsed": 0.0127208,
  "response.message": "{\"Value\":{\"OrderId\":\"d7f7b93f-...\",\"LineCount\":2,\"TotalAmount\":450.00},\"IsSucc\":true,\"IsFail\":false}",
  "ctx.order_total_amount": 450.00,
  "ctx.customer_id": "CUST-001",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

#### GetOrderSummaryQuery Request (Enricher 미적용)

```json
{
  "@timestamp": "2026-03-19T14:22:06.8483074Z",
  "log.level": "Information",
  "message": "application usecase.query GetOrderSummaryQuery.Handle requesting with Request { OrderId: \"ORD-123\" }",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "query",
  "request.handler": "GetOrderSummaryQuery",
  "request.handler_method": "Handle",
  "request.message": "{\"OrderId\":\"ORD-123\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

#### FailExpectedCommand Response (Expected 에러 — Warning)

```json
{
  "@timestamp": "2026-03-19T14:22:06.8566836Z",
  "log.level": "Warning",
  "message": "application usecase.command FailExpectedCommand.Handle responded failure in 0.0019 s with expected:Order.NotFound { ErrorType: \"ErrorCodeExpected\", ErrorCode: \"Order.NotFound\", ... }",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
  "event.id": 1003,
  "event.name": "application.response.warning",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "FailExpectedCommand",
  "request.handler_method": "Handle",
  "response.status": "failure",
  "response.elapsed": 0.001863,
  "error.type": "expected",
  "error.code": "Order.NotFound",
  "error.detail": "{\"ErrorType\":\"ErrorCodeExpected\",\"ErrorCode\":\"Order.NotFound\",\"ErrorCodeId\":-1000,\"ErrorCurrentValue\":\"ORD-NOT-EXIST\",\"Message\":\"주문을 찾을 수 없습니다\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

#### FailExceptionalCommand Response (Exceptional 에러 — Error)

```json
{
  "@timestamp": "2026-03-19T14:22:06.8869311Z",
  "log.level": "Error",
  "message": "application usecase.command FailExceptionalCommand.Handle responded failure in 0.0212 s with exceptional:Database.ConnectionFailed { ErrorType: \"ErrorCodeExceptional\", ... }",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
  "event.id": 1004,
  "event.name": "application.response.error",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "FailExceptionalCommand",
  "request.handler_method": "Handle",
  "response.status": "failure",
  "response.elapsed": 0.0211916,
  "error.type": "exceptional",
  "error.code": "Database.ConnectionFailed",
  "error.detail": "{\"ErrorType\":\"ErrorCodeExceptional\",\"ErrorCode\":\"Database.ConnectionFailed\",\"ErrorCodeId\":-2146233079,\"Message\":\"데이터베이스 연결이 끊어졌습니다\",\"ExceptionDetails\":{\"TargetSite\":null,\"Message\":\"데이터베이스 연결이 끊어졌습니다\",\"Data\":[],\"InnerException\":null,\"HelpLink\":null,\"Source\":null,\"HResult\":-2146233079,\"StackTrace\":null}}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

### 3.3 JSON 구조 분석

#### OpenSearchJsonFormatter vs 기존 JsonFormatter 차이점

| 항목 | 기존 `JsonFormatter` | `OpenSearchJsonFormatter` |
|------|---------------------|--------------------------|
| 포맷 | `Properties` 래퍼로 중첩 | 플랫 JSON (래퍼 제거) |
| Timestamp | ISO 8601 (`+09:00` 포함) | `@timestamp` UTC ISO 8601 |
| Level | `"Level": "Information"` | `"log.level": "Information"` (ECS 호환) |
| 렌더링 메시지 | 없음 | `"message"` (literal + JSON 렌더링) |
| MessageTemplate | `"MessageTemplate"` | `"message.template"` |
| EventId | 중첩 객체 | `event.id` + `event.name` (플랫) |
| SourceContext | PascalCase 키 | `log.logger` (ECS 호환) |
| request/response.message | 구조화 객체 (매핑 폭발) | JSON 문자열 (매핑 안전) |
| error 객체 | 구조화 객체 (매핑 폭발) | `error.detail` JSON 문자열 |
| `_typeTag`, `$type` | 포함 (노이즈) | 제거 |
| `Renderings` | 포함 (노이즈) | 제거 |
| Enricher 필드 | PascalCase (`CustomerId`) | `ctx.*` snake_case (`ctx.customer_id`) |

#### error 필드 구조 검증

| 필드 | OpenSearch 타입 | FailExpectedCommand | FailExceptionalCommand |
|------|----------------|---------------------|------------------------|
| `error.type` | `keyword` | `"expected"` | `"exceptional"` |
| `error.code` | `keyword` | `"Order.NotFound"` | `"Database.ConnectionFailed"` |
| `error.detail` | `text` (JSON 문자열) | `"{\"ErrorType\":\"ErrorCodeExpected\",...}"` | `"{\"ErrorType\":\"ErrorCodeExceptional\",...}"` |

> `error` 객체가 `error.detail`로 이름 변경 + JSON 문자열화되어 `error.type`, `error.code`와 동일한 네임스페이스 하위 keyword/text 필드로 정리됩니다.

#### Enricher 커스텀 필드 포함 검증

| 필드 | PlaceOrderCommand Request | PlaceOrderCommand Response | GetOrderSummaryQuery |
|------|--------------------------|---------------------------|---------------------|
| `ctx.customer_id` | `"CUST-001"` | `"CUST-001"` | - |
| `ctx.order_line_count` | `2` | - | - |
| `ctx.order_total_amount` | - | `450.00` | - |

> Enricher 커스텀 필드가 `ctx.*` 네임스페이스로 플랫 JSON에 정상 포함됩니다.

---

## 4. IUsecaseLogEnricher 검증 (콘솔 기준)

### 4.1 PlaceOrderCommand — Enricher 적용

`PlaceOrderLogEnricher`가 `LogContext.PushProperty`로 Push한 커스텀 필드가 `{Properties:j}`에 표시되는지 검증합니다.

#### Request 로그

| Enricher 필드 | 예상값 | 실제값 | 결과 |
|---------------|--------|--------|------|
| `ctx.customer_id` | `"CUST-001"` | `"CUST-001"` | PASS |
| `ctx.order_line_count` | `2` | `2` | PASS |

`EnrichRequestLog`에서 Push한 두 필드 모두 `ctx.*` 네임스페이스로 정상 표시됩니다.

#### Response 로그

| Enricher 필드 | 예상값 | 실제값 | 결과 |
|---------------|--------|--------|------|
| `ctx.order_total_amount` | `450.00` | `450.00` | PASS |

`EnrichResponseLog`에서 Push한 `ctx.order_total_amount` 필드가 정상 표시됩니다.

### 4.2 GetOrderSummaryQuery — Enricher 미적용 (기준선)

Enricher가 등록되지 않은 Query에서는 커스텀 필드가 없어야 합니다.

| Properties 필드 | Request | Response | 비고 |
|-----------------|---------|----------|------|
| `EventId` | 1001 | 1002 | 프레임워크 표준 |
| `SourceContext` | `"...UsecaseLoggingPipeline"` | `"...UsecaseLoggingPipeline"` | 프레임워크 표준 |
| 커스텀 필드 | **(없음)** | **(없음)** | PASS |

### 4.3 Enricher 대비 분석

| 구분 | PlaceOrderCommand | GetOrderSummaryQuery |
|------|-------------------|---------------------|
| `ctx.customer_id` | CUST-001 | - |
| `ctx.order_line_count` | 2 | - |
| `ctx.order_total_amount` | 450.00 | - |

Enricher가 등록된 Command에만 커스텀 필드가 표시되고, 등록되지 않은 Query에는 표시되지 않습니다.

---

## 5. 에러 응답 검증

### 5.1 FailExpectedCommand — Expected 에러 (Warning 레벨)

| 항목 | 예상 | 실제 | 결과 |
|------|------|------|------|
| `log.level` | `Warning` | `Warning` | PASS |
| `event.id` | `1003` | `1003` | PASS |
| `event.name` | `application.response.warning` | `application.response.warning` | PASS |
| `response.status` | `failure` | `failure` | PASS |
| `error.type` | `expected` | `expected` | PASS |
| `error.code` | `Order.NotFound` | `Order.NotFound` | PASS |
| `error.detail` | JSON 문자열 | `"{\"ErrorType\":\"ErrorCodeExpected\",...}"` | PASS |

> `error.detail`에 `ErrorType`, `ErrorCode`, `ErrorCodeId`, `ErrorCurrentValue`, `Message` 필드가 JSON 문자열로 포함됩니다.

### 5.2 FailExceptionalCommand — Exceptional 에러 (Error 레벨)

| 항목 | 예상 | 실제 | 결과 |
|------|------|------|------|
| `log.level` | `Error` | `Error` | PASS |
| `event.id` | `1004` | `1004` | PASS |
| `event.name` | `application.response.error` | `application.response.error` | PASS |
| `response.status` | `failure` | `failure` | PASS |
| `error.type` | `exceptional` | `exceptional` | PASS |
| `error.code` | `Database.ConnectionFailed` | `Database.ConnectionFailed` | PASS |
| `error.detail` | JSON 문자열 | `"{\"ErrorType\":\"ErrorCodeExceptional\",...}"` | PASS |

> `error.detail`에 `ErrorType`, `ErrorCode`, `ErrorCodeId`, `Message`, `ExceptionDetails` 필드가 JSON 문자열로 포함됩니다.
> `ExceptionDetails`에는 Exception의 `TargetSite`, `Message`, `HResult`, `StackTrace` 등이 중첩됩니다.

### 5.3 에러 레벨 분류

| 에러 유형 | Log Level | EventId | error.type | 비고 |
|-----------|-----------|---------|------------|------|
| Expected (비즈니스) | `Warning` | 1003 | `expected` | `ErrorCodeExpected` |
| Exceptional (시스템) | `Error` | 1004 | `exceptional` | `ErrorCodeExceptional` |

---

## 6. 표준 로깅 파이프라인 검증

### 6.1 로그 메시지 포맷

| 항목 | PlaceOrderCommand | GetOrderSummaryQuery | FailExpectedCommand | FailExceptionalCommand |
|------|-------------------|---------------------|---------------------|------------------------|
| Layer | `application` | `application` | `application` | `application` |
| Category | `usecase.command` | `usecase.query` | `usecase.command` | `usecase.command` |
| Handler | `PlaceOrderCommand` | `GetOrderSummaryQuery` | `FailExpectedCommand` | `FailExceptionalCommand` |
| Method | `Handle` | `Handle` | `Handle` | `Handle` |
| Status | `success` | `success` | `failure` | `failure` |

CQRS 타입이 `ICommandRequest<>` / `IQueryRequest<>` 인터페이스 기반으로 자동 식별됩니다.

---

## 7. Serilog 설정 검증

| 설정 항목 | 예상 | 실제 | 결과 |
|-----------|------|------|------|
| `Enrich: FromLogContext` | Enricher 속성이 Properties에 병합 | 병합됨 | PASS |
| `{Properties:j}` in outputTemplate | 커스텀 필드가 JSON으로 표시 | JSON 표시 | PASS |
| `OpenSearchJsonFormatter` | JSON 파일 싱크에 적용 | 적용됨 | PASS |
| `AnsiConsoleTheme::Code` | 컬러 콘솔 출력 | 적용됨 | PASS |

---

## 8. OpenTelemetry Console Exporter 검증

### 8.1 Metrics/Tracing Console Export

| 항목 | 결과 | 비고 |
|------|------|------|
| 콘솔 출력 | 미출력 | `ServiceCollection` 기반 단기 실행 앱 제약 |

**미출력 원인**: `PeriodicExportingMetricReader`의 기본 Export 주기가 60초이며, 콘솔 앱이 그 전에 종료됩니다. `IHost` 기반 전환 시 검증 가능합니다.

---

## 9. 검증 요약

| # | 검증 항목 | 결과 |
|---|----------|------|
| 1 | `dotnet build Functorium.slnx` 빌드 성공 | PASS |
| 2 | `dotnet run` 실행 성공 (4개 시나리오) | PASS |
| 3 | `logs/log-YYYYMMDD.txt` Plain text 로그 파일 생성 | PASS |
| 4 | `logs/log-YYYYMMDD.json` OpenSearch 최적화 JSON 로그 파일 생성 | PASS |
| 5 | JSON 로그에 `ctx.*` Enricher 커스텀 필드 포함 | PASS |
| 6 | PlaceOrderCommand Request 로그에 `ctx.customer_id`, `ctx.order_line_count` 표시 | PASS |
| 7 | PlaceOrderCommand Response 로그에 `ctx.order_total_amount` 표시 | PASS |
| 8 | GetOrderSummaryQuery 로그에 커스텀 필드 없음 (기준선) | PASS |
| 9 | CQRS 타입 자동 식별 (`command` / `query`) | PASS |
| 10 | 기존 테스트 회귀 없음 (1446 passed, 0 failed) | PASS |
| 11 | `request.message`가 JSON 문자열(객체 아님)인지 확인 | PASS |
| 12 | `_typeTag`, `Renderings` 제거 확인 | PASS |
| 13 | dot 충돌 해소 (`request.category_type`, `request.handler_method`) | PASS |
| 14 | Expected 에러 → Warning 레벨, `error.type: "expected"` | PASS |
| 15 | Exceptional 에러 → Error 레벨, `error.type: "exceptional"` | PASS |
| 16 | `error` 객체 → `error.detail` JSON 문자열 변환 | PASS |
| 17 | `error.type`, `error.code` 스칼라 필드 보존 | PASS |
| 18 | `message` 렌더링 (literal string + JSON structure) | PASS |
| 19 | Metrics Console Export | NOT TESTED (*) |
| 20 | Tracing Console Export | NOT TESTED (*) |

(*) `ServiceCollection` 기반 단기 실행 콘솔 앱에서는 OTel SDK의 Graceful Shutdown Flush가 동작하지 않아 미출력. `IHost` 기반 전환 시 검증 가능.
