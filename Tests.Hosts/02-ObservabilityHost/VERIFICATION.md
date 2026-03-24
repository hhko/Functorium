# 02-ObservabilityHost — 관찰 가능성 로그 검증 결과

> **검증 일시:** 2026-03-21
> **실행 명령:** `dotnet run --project Tests.Hosts/02-ObservabilityHost/Src/ObservabilityHost`
> **Serilog OutputTemplate:** `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}`

---

## 시나리오 개요

| # | 시나리오 | Usecase 유형 | 로그 레벨 | 핵심 관찰 가능성 |
|---|----------|-------------|-----------|-----------------|
| 1 | PlaceOrderCommand | Command | INF | Usecase Log Enricher (Request + Response) |
| 2 | OrderPlacedEvent | Domain Event | INF | Domain Event Log Enricher + Root 필드 |
| 3 | GetOrderSummaryQuery | Query | INF | 기준선 (커스텀 Enricher 없음) |
| 4 | FailExpectedCommand | Command | WRN | Expected 비즈니스 에러 |
| 5 | FailExceptionalCommand | Command | ERR | Exceptional 시스템 에러 |

---

## 1. PlaceOrderCommand — Command 성공 + Log Enricher

### Console 로그 (Raw)

```
[20:36:05 INF] application usecase.command PlaceOrderCommand.Handle requesting with {"CustomerId": "CUST-001", "Lines": [{"ProductId": "PROD-A", "Quantity": 2, "UnitPrice": 100.00, "$type": "OrderLine"}, {"ProductId": "PROD-B", "Quantity": 1, "UnitPrice": 250.00, "$type": "OrderLine"}], "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.place_order_command.request.order_total_amount": 450.00, "ctx.place_order_command.request.lines_count": 2, "ctx.customer_id": "CUST-001"}
[20:36:05 INF] application usecase.command PlaceOrderCommand.Handle responded success in 0.0092 s with {"Value": {"OrderId": "4d467449-b85a-43e3-9fde-fc89ab72eb81", "LineCount": 2, "TotalAmount": 450.00, "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.place_order_command.response.average_line_amount": 225.00, "ctx.place_order_command.response.total_amount": 450.00, "ctx.place_order_command.response.line_count": 2, "ctx.place_order_command.response.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.3009604Z",
  "log.level": "Information",
  "message": "application usecase.command PlaceOrderCommand.Handle requesting with ...",
  "message.template": "{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "PlaceOrderCommand",
  "request.handler.method": "Handle",
  "ctx.place_order_command.request.order_total_amount": 450.00,
  "ctx.place_order_command.request.lines_count": 2,
  "ctx.customer_id": "CUST-001",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.426725Z",
  "log.level": "Information",
  "message": "application usecase.command PlaceOrderCommand.Handle responded success in 0.0092 s with ...",
  "message.template": "{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "PlaceOrderCommand",
  "request.handler.method": "Handle",
  "response.status": "success",
  "response.elapsed": 0.0091929,
  "ctx.place_order_command.response.average_line_amount": 225.00,
  "ctx.place_order_command.response.total_amount": 450.00,
  "ctx.place_order_command.response.line_count": 2,
  "ctx.place_order_command.response.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

### Enricher 필드 요약

| ctx 필드 | 값 | Enricher 위치 |
|----------|-----|--------------|
| `ctx.customer_id` | `CUST-001` | **Root** (Request) |
| `ctx.place_order_command.request.order_total_amount` | `450.00` | Request Enricher |
| `ctx.place_order_command.request.lines_count` | `2` | Request Enricher |
| `ctx.place_order_command.response.order_id` | `4d467449-...` | Response Enricher |
| `ctx.place_order_command.response.total_amount` | `450.00` | Response Enricher |
| `ctx.place_order_command.response.line_count` | `2` | Response Enricher |
| `ctx.place_order_command.response.average_line_amount` | `225.00` | Response Enricher |

---

## 2. OrderPlacedEvent — Domain Event Enricher

### Console 로그 (Raw)

```
[20:36:05 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM82WNHP7TTEPZSSX7DYFACT requesting with {"CustomerId": "CUST-001", "OrderId": "4d467449-b85a-43e3-9fde-fc89ab72eb81", "LineCount": 2, "TotalAmount": 450.00, "OccurredAt": "2026-03-21T11:36:05.4307454+00:00", "EventId": {"Random": "3EB4EB7F39E9DBE7A99A", "Time": "2026-03-21T11:36:05.4300000+00:00", "$type": "Ulid"}, "CorrelationId": null, "CausationId": null, "$type": "OrderPlacedEvent"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler", "ctx.order_placed_event.total_amount": 450.00, "ctx.order_placed_event.line_count": 2, "ctx.order_placed_event.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81", "ctx.customer_id": "CUST-001"}
[20:36:05 INF] [DomainEvent] Order placed: 4d467449-b85a-43e3-9fde-fc89ab72eb81, Customer: CUST-001, Lines: 2, Total: 450.00 {"SourceContext": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler", "ctx.order_placed_event.total_amount": 450.00, "ctx.order_placed_event.line_count": 2, "ctx.order_placed_event.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81", "ctx.customer_id": "CUST-001"}
[20:36:05 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM82WNHP7TTEPZSSX7DYFACT responded success in 0.0049 s {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler", "ctx.order_placed_event.total_amount": 450.00, "ctx.order_placed_event.line_count": 2, "ctx.order_placed_event.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81", "ctx.customer_id": "CUST-001"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.4541422Z",
  "log.level": "Information",
  "message": "application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM82WNHP7TTEPZSSX7DYFACT requesting with ...",
  "message.template": "{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "event",
  "request.handler.name": "OrderPlacedEventHandler",
  "request.handler.method": "Handle",
  "request.event.type": "OrderPlacedEvent",
  "request.event.id": "01KM82WNHP7TTEPZSSX7DYFACT",
  "ctx.order_placed_event.total_amount": 450.00,
  "ctx.order_placed_event.line_count": 2,
  "ctx.order_placed_event.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81",
  "ctx.customer_id": "CUST-001",
  "log.logger": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler"
}
```

**Handler 내부:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.4565411Z",
  "log.level": "Information",
  "message": "[DomainEvent] Order placed: 4d467449-b85a-43e3-9fde-fc89ab72eb81, Customer: CUST-001, Lines: 2, Total: 450.00",
  "message.template": "[DomainEvent] Order placed: {OrderId}, Customer: {CustomerId}, Lines: {LineCount}, Total: {TotalAmount}",
  "ctx.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81",
  "ctx.customer_id": "CUST-001",
  "ctx.line_count": 2,
  "ctx.total_amount": 450.00,
  "ctx.order_placed_event.total_amount": 450.00,
  "ctx.order_placed_event.line_count": 2,
  "ctx.order_placed_event.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81",
  "log.logger": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler"
}
```

**Response:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.4615482Z",
  "log.level": "Information",
  "message": "application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM82WNHP7TTEPZSSX7DYFACT responded success in 0.0049 s",
  "message.template": "{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "event",
  "request.handler.name": "OrderPlacedEventHandler",
  "request.handler.method": "Handle",
  "request.event.type": "OrderPlacedEvent",
  "request.event.id": "01KM82WNHP7TTEPZSSX7DYFACT",
  "response.status": "success",
  "response.elapsed": 0.0049272,
  "ctx.order_placed_event.total_amount": 450.00,
  "ctx.order_placed_event.line_count": 2,
  "ctx.order_placed_event.order_id": "4d467449-b85a-43e3-9fde-fc89ab72eb81",
  "ctx.customer_id": "CUST-001",
  "log.logger": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler"
}
```

### Enricher 필드 요약

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer_id` | `CUST-001` | **Root 필드** — CustomerId |
| `ctx.order_placed_event.order_id` | `4d467449-...` | 주문 ID |
| `ctx.order_placed_event.line_count` | `2` | 주문 라인 수 |
| `ctx.order_placed_event.total_amount` | `450.00` | 총 주문 금액 |

### Enricher 스코프 검증

| 로그 위치 | ctx.customer_id | ctx.order_placed_event.* | 결과 |
|-----------|----------------|--------------------------|------|
| Request 로그 | CUST-001 | order_id, line_count, total_amount | **포함** |
| Handler 내부 | CUST-001 | order_id, line_count, total_amount | **포함** |
| Response 로그 | CUST-001 | order_id, line_count, total_amount | **포함** |

---

## 3. GetOrderSummaryQuery — Query 기준선

### Console 로그 (Raw)

```
[20:36:05 INF] application usecase.query GetOrderSummaryQuery.Handle requesting with {"OrderId": "ORD-123", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[20:36:05 INF] application usecase.query GetOrderSummaryQuery.Handle responded success in 0.0007 s with {"Value": {"OrderId": "ORD-123", "Status": "Completed", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.4685706Z",
  "log.level": "Information",
  "message": "application usecase.query GetOrderSummaryQuery.Handle requesting with ...",
  "message.template": "{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "query",
  "request.handler.name": "GetOrderSummaryQuery",
  "request.handler.method": "Handle",
  "request.message": "{\"OrderId\":\"ORD-123\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.4706802Z",
  "log.level": "Information",
  "message": "application usecase.query GetOrderSummaryQuery.Handle responded success in 0.0007 s with ...",
  "message.template": "{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "query",
  "request.handler.name": "GetOrderSummaryQuery",
  "request.handler.method": "Handle",
  "response.status": "success",
  "response.elapsed": 0.0006544,
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

> 기준선 시나리오: 커스텀 Enricher가 없으므로 `ctx.*` 필드 없음

---

## 4. FailExpectedCommand — Expected 비즈니스 에러

### Console 로그 (Raw)

```
[20:36:05 INF] application usecase.command FailExpectedCommand.Handle requesting with {"OrderId": "ORD-NOT-EXIST", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[20:36:05 WRN] application usecase.command FailExpectedCommand.Handle responded failure in 0.0031 s with expected:Order.NotFound {"ErrorType": "ErrorCodeExpected", "ErrorCode": "Order.NotFound", "ErrorCodeId": -1000, "ErrorCurrentValue": "ORD-NOT-EXIST", "Message": "주문을 찾을 수 없습니다"} {"EventId": {"Id": 1003, "Name": "application.response.warning"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.4765911Z",
  "log.level": "Information",
  "message": "application usecase.command FailExpectedCommand.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "FailExpectedCommand",
  "request.handler.method": "Handle",
  "request.message": "{\"OrderId\":\"ORD-NOT-EXIST\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response (Warning):**
```json
{
  "@timestamp": "2026-03-21T11:36:05.4841045Z",
  "log.level": "Warning",
  "message": "application usecase.command FailExpectedCommand.Handle responded failure in 0.0031 s with expected:Order.NotFound ...",
  "event.id": 1003,
  "event.name": "application.response.warning",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "FailExpectedCommand",
  "request.handler.method": "Handle",
  "response.status": "failure",
  "response.elapsed": 0.0030533,
  "error.type": "expected",
  "error.code": "Order.NotFound",
  "error.detail": "{\"ErrorType\":\"ErrorCodeExpected\",\"ErrorCode\":\"Order.NotFound\",\"ErrorCodeId\":-1000,\"ErrorCurrentValue\":\"ORD-NOT-EXIST\",\"Message\":\"주문을 찾을 수 없습니다\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

> Expected 에러는 `log.level: Warning` + `event.id: 1003` 으로 출력

---

## 5. FailExceptionalCommand — Exceptional 시스템 에러

### Console 로그 (Raw)

```
[20:36:05 INF] application usecase.command FailExceptionalCommand.Handle requesting with {"OrderId": "ORD-DB-FAIL", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[20:36:05 ERR] application usecase.command FailExceptionalCommand.Handle responded failure in 0.0292 s with exceptional:Database.ConnectionFailed {"ErrorType": "ErrorCodeExceptional", "ErrorCode": "Database.ConnectionFailed", "ErrorCodeId": -2146233079, "Message": "데이터베이스 연결이 끊어졌습니다", "ExceptionDetails": {"TargetSite": null, "Message": "데이터베이스 연결이 끊어졌습니다", "Data": [], "InnerException": null, "HelpLink": null, "Source": null, "HResult": -2146233079, "StackTrace": null, "$type": "InvalidOperationException"}} {"EventId": {"Id": 1004, "Name": "application.response.error"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:36:05.486642Z",
  "log.level": "Information",
  "message": "application usecase.command FailExceptionalCommand.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "FailExceptionalCommand",
  "request.handler.method": "Handle",
  "request.message": "{\"OrderId\":\"ORD-DB-FAIL\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response (Error):**
```json
{
  "@timestamp": "2026-03-21T11:36:05.5208343Z",
  "log.level": "Error",
  "message": "application usecase.command FailExceptionalCommand.Handle responded failure in 0.0292 s with exceptional:Database.ConnectionFailed ...",
  "event.id": 1004,
  "event.name": "application.response.error",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "FailExceptionalCommand",
  "request.handler.method": "Handle",
  "response.status": "failure",
  "response.elapsed": 0.0291804,
  "error.type": "exceptional",
  "error.code": "Database.ConnectionFailed",
  "error.detail": "{\"ErrorType\":\"ErrorCodeExceptional\",\"ErrorCode\":\"Database.ConnectionFailed\",\"ErrorCodeId\":-2146233079,\"Message\":\"데이터베이스 연결이 끊어졌습니다\",\"ExceptionDetails\":{\"TargetSite\":null,\"Message\":\"데이터베이스 연결이 끊어졌습니다\",\"Data\":[],\"InnerException\":null,\"HelpLink\":null,\"Source\":null,\"HResult\":-2146233079,\"StackTrace\":null}}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

> Exceptional 에러는 `log.level: Error` + `event.id: 1004` + `ExceptionDetails` 포함

---

## Usecase 인터페이스 스코프 ctx 필드 검증

### 변경 사항

`PlaceOrderCommand.Request`에 비-root 인터페이스 `IOperatorContext`를 추가하여 세 가지 ctx 필드 스코프를 검증합니다.

```csharp
public interface IOperatorContext { string OperatorId { get; } }              // 비-root
[CtxEnricherRoot] public interface ICustomerRequest { string CustomerId { get; } }  // root

public sealed record Request(string CustomerId, List<OrderLine> Lines, string OperatorId)
    : ICommandRequest<Response>, ICustomerRequest, IOperatorContext;
```

### 기대 ctx 필드

| 프로퍼티 | 소속 인터페이스 | 기대 ctx 필드 | 스코프 |
|---------|---------------|-------------|--------|
| `CustomerId` | `[CtxEnricherRoot] ICustomerRequest` | `ctx.customer_id` | Root |
| `OperatorId` | `IOperatorContext` | `ctx.operator_context.operator_id` | Interface |
| `Lines` | 없음 (직접 프로퍼티) | `ctx.place_order_command.request.lines_count` | Usecase |

---

## DomainEvent 인터페이스 스코프 ctx 필드 검증

### 변경 사항

`OrderPlacedEvent`에 비-root 인터페이스 `IOperatorContext`를 추가하여 DomainEvent에서도 인터페이스 스코프 ctx 필드를 검증합니다.

```csharp
public sealed record OrderPlacedEvent(
    [CtxEnricherRoot] string CustomerId,
    string OrderId,
    int LineCount,
    decimal TotalAmount,
    string OperatorId) : DomainEvent, IOperatorContext;
```

### 기대 ctx 필드

| 프로퍼티 | 스코프 | ctx 필드 |
|---------|--------|---------|
| `CustomerId` | Root | `ctx.customer_id` |
| `OperatorId` | **Interface** (`IOperatorContext`) | `ctx.operator_context.operator_id` |
| `OrderId` | Event | `ctx.order_placed_event.order_id` |
| `LineCount` | Event | `ctx.order_placed_event.line_count` |
| `TotalAmount` | Event | `ctx.order_placed_event.total_amount` |

---

## 검증 결과 요약

| # | 검증 항목 | 결과 |
|---|----------|------|
| 1 | PlaceOrderCommand Request Enricher (`ctx.place_order_command.request.*`) | **Pass** |
| 2 | PlaceOrderCommand Response Enricher (`ctx.place_order_command.response.*`) | **Pass** |
| 3 | PlaceOrderCommand Root 필드 (`ctx.customer_id`) | **Pass** |
| 4 | PlaceOrderCommand Interface 스코프 (`ctx.operator_context.operator_id`) | **TODO** |
| 5 | OrderPlacedEvent Domain Event Enricher (`ctx.order_placed_event.*`) | **Pass** |
| 6 | OrderPlacedEvent Root 필드 (`ctx.customer_id`) Handler 전체 범위 포함 | **Pass** |
| 6a | OrderPlacedEvent Interface 스코프 (`ctx.operator_context.operator_id`) | **TODO** |
| 7 | GetOrderSummaryQuery 기준선 — `ctx.*` 필드 없음 | **Pass** |
| 8 | FailExpectedCommand → `log.level: Warning` + `event.id: 1003` | **Pass** |
| 9 | FailExceptionalCommand → `log.level: Error` + `event.id: 1004` + `ExceptionDetails` | **Pass** |
| 10 | OpenSearch JSON 포맷 정상 출력 (모든 시나리오) | **Pass** |
| 11 | Console Raw 출력과 JSON 필드 일치 | **Pass** |
