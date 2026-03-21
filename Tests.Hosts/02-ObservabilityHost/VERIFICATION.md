# 02-ObservabilityHost 검증 결과

**검증 일시**: 2026-03-21
**실행 명령**: `dotnet run --project Tests.Hosts/02-ObservabilityHost/Src/ObservabilityHost`

---

## 1. 빌드 검증

```
dotnet build Functorium.slnx
```

| 항목 | 결과 |
|------|------|
| 빌드 | PASS (0 errors) |
| 기존 테스트 | PASS (1480 passed, 0 failed, 28 skipped) |

---

## 2. 콘솔 출력 전문

### 2.1 원본 출력 (Raw)

```
=== PlaceOrderCommand (Custom Observability) ===
[23:51:16 INF] application usecase.command PlaceOrderCommand.Handle requesting with {"CustomerId": "CUST-001", "Lines": [{"ProductId": "PROD-A", "Quantity": 2, "UnitPrice": 100.00, "$type": "OrderLine"}, {"ProductId": "PROD-B", "Quantity": 1, "UnitPrice": 250.00, "$type": "OrderLine"}], "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.place_order_command.request.order_total_amount": 450.00, "ctx.place_order_command.request.lines_count": 2, "ctx.customer_id": "CUST-001"}
[23:51:16 INF] application usecase.command PlaceOrderCommand.Handle responded success in 0.0069 s with {"Value": {"OrderId": "c8e0e473-e0ea-4bb2-bda3-25e86eaa6625", "LineCount": 2, "TotalAmount": 450.00, "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.place_order_command.response.average_line_amount": 225.00, "ctx.place_order_command.response.total_amount": 450.00, "ctx.place_order_command.response.line_count": 2, "ctx.place_order_command.response.order_id": "c8e0e473-e0ea-4bb2-bda3-25e86eaa6625"}
PlaceOrder Result: Succ(Response { OrderId = c8e0e473-e0ea-4bb2-bda3-25e86eaa6625, LineCount = 2, TotalAmount = 450.00 })

=== OrderPlacedEvent (DomainEvent Enricher) ===
[23:51:16 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM5VNB39GRJZACVBQ3X6K2PT requesting with {"CustomerId": "CUST-001", "OrderId": "c8e0e473-e0ea-4bb2-bda3-25e86eaa6625", "LineCount": 2, "TotalAmount": 450.00, "OccurredAt": "2026-03-20T14:51:16.4575054+00:00", "EventId": {...}, "CorrelationId": null, "CausationId": null, "$type": "OrderPlacedEvent"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler", "ctx.order_placed_event.total_amount": 450.00, "ctx.order_placed_event.line_count": 2, "ctx.order_placed_event.order_id": "c8e0e473-e0ea-4bb2-bda3-25e86eaa6625", "ctx.customer_id": "CUST-001"}
[23:51:16 INF] [DomainEvent] Order placed: c8e0e473-e0ea-4bb2-bda3-25e86eaa6625, Customer: CUST-001, Lines: 2, Total: 450.00 {"SourceContext": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler", "ctx.order_placed_event.total_amount": 450.00, "ctx.order_placed_event.line_count": 2, "ctx.order_placed_event.order_id": "c8e0e473-e0ea-4bb2-bda3-25e86eaa6625", "ctx.customer_id": "CUST-001"}
[23:51:16 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM5VNB39GRJZACVBQ3X6K2PT responded success in 0.0021 s {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "ObservabilityHost.DomainEvents.OrderPlacedEventHandler", "ctx.order_placed_event.total_amount": 450.00, "ctx.order_placed_event.line_count": 2, "ctx.order_placed_event.order_id": "c8e0e473-e0ea-4bb2-bda3-25e86eaa6625", "ctx.customer_id": "CUST-001"}

=== GetOrderSummaryQuery (Baseline) ===
[23:51:16 INF] application usecase.query GetOrderSummaryQuery.Handle requesting with {"OrderId": "ORD-123", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[23:51:16 INF] application usecase.query GetOrderSummaryQuery.Handle responded success in 0.0055 s with {"Value": {"OrderId": "ORD-123", "Status": "Completed", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
GetOrderSummary Result: Succ(Response { OrderId = ORD-123, Status = Completed })

=== FailExpectedCommand (Expected Error) ===
[23:51:16 INF] application usecase.command FailExpectedCommand.Handle requesting with {"OrderId": "ORD-NOT-EXIST", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[23:51:16 WRN] application usecase.command FailExpectedCommand.Handle responded failure in 0.0019 s with expected:Order.NotFound {"ErrorType": "ErrorCodeExpected", "ErrorCode": "Order.NotFound", "ErrorCodeId": -1000, "ErrorCurrentValue": "ORD-NOT-EXIST", "Message": "주문을 찾을 수 없습니다"} {"EventId": {"Id": 1003, "Name": "application.response.warning"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
FailExpected Result: Fail(주문을 찾을 수 없습니다)

=== FailExceptionalCommand (Exceptional Error) ===
[23:51:16 INF] application usecase.command FailExceptionalCommand.Handle requesting with {"OrderId": "ORD-DB-FAIL", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[23:51:16 ERR] application usecase.command FailExceptionalCommand.Handle responded failure in 0.0161 s with exceptional:Database.ConnectionFailed {"ErrorType": "ErrorCodeExceptional", "ErrorCode": "Database.ConnectionFailed", "ErrorCodeId": -2146233079, "Message": "데이터베이스 연결이 끊어졌습니다", "ExceptionDetails": {...}} {"EventId": {"Id": 1004, "Name": "application.response.error"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
FailExceptional Result: Fail(데이터베이스 연결이 끊어졌습니다)

=== Done ===
```

---

## 3. Scenario 5: OrderPlacedEvent — Domain Event Log Enricher 검증

### 3.1 Enricher 필드

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer_id` | `CUST-001` | **Root 필드** — CustomerId |
| `ctx.order_placed_event.order_id` | `c8e0e473-e0ea-4bb2-bda3-25e86eaa6625` | 주문 ID |
| `ctx.order_placed_event.line_count` | `2` | 주문 라인 수 |
| `ctx.order_placed_event.total_amount` | `450.00` | 총 주문 금액 |

### 3.2 시나리오 흐름

```
PlaceOrderCommand 성공 → FinResponse<Response>.IsSucc = true
  → OrderPlacedEvent 생성 (CustomerId, OrderId, LineCount, TotalAmount)
  → mediator.Publish(orderPlacedEvent)
  → ObservableDomainEventNotificationPublisher
    → IDomainEventLogEnricher<OrderPlacedEvent>.EnrichLog() → LogContext Push
    → OrderPlacedEventHandler.Handle() 실행
    → Enrichment Dispose
```

### 3.3 Request 로그

```
[23:51:16 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM5VNB39GRJZACVBQ3X6K2PT requesting with {..}
  {"EventId":{"Id":1001,"Name":"application.request"},
   "SourceContext":"ObservabilityHost.DomainEvents.OrderPlacedEventHandler",
   "ctx.order_placed_event.total_amount":450.00,
   "ctx.order_placed_event.line_count":2,
   "ctx.order_placed_event.order_id":"c8e0e473-e0ea-4bb2-bda3-25e86eaa6625",
   "ctx.customer_id":"CUST-001"}
```

### 3.4 Handler 내부 로그

```
[23:51:16 INF] [DomainEvent] Order placed: c8e0e473-e0ea-4bb2-bda3-25e86eaa6625, Customer: CUST-001, Lines: 2, Total: 450.00
  {"SourceContext":"ObservabilityHost.DomainEvents.OrderPlacedEventHandler",
   "ctx.order_placed_event.total_amount":450.00,
   "ctx.order_placed_event.line_count":2,
   "ctx.order_placed_event.order_id":"c8e0e473-e0ea-4bb2-bda3-25e86eaa6625",
   "ctx.customer_id":"CUST-001"}
```

### 3.5 Response 로그

```
[23:51:16 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM5VNB39GRJZACVBQ3X6K2PT responded success in 0.0021 s
  {"EventId":{"Id":1002,"Name":"application.response.success"},
   "SourceContext":"ObservabilityHost.DomainEvents.OrderPlacedEventHandler",
   "ctx.order_placed_event.total_amount":450.00,
   "ctx.order_placed_event.line_count":2,
   "ctx.order_placed_event.order_id":"c8e0e473-e0ea-4bb2-bda3-25e86eaa6625",
   "ctx.customer_id":"CUST-001"}
```

### 3.6 Enricher 스코프 검증

`using var enrichment = ResolveEnrichment(domainEvent)` 패턴으로 인해 `ctx.*` 필드는 핸들러 전체 실행 범위에 적용됩니다.

```
ObservableDomainEventNotificationPublisher.HandleWithObservability()
│
├─ using var enrichment = ResolveEnrichment(domainEvent)  ← LogContext.Push 시작
│   ├─ ctx.customer_id = "CUST-001"
│   ├─ ctx.order_placed_event.order_id = "c8e0e473-..."
│   ├─ ctx.order_placed_event.line_count = 2
│   └─ ctx.order_placed_event.total_amount = 450.00
│
├─ logger.LogDomainEventHandlerRequest(...)          ← Request 로그 (ctx.* 포함)
├─ handler.Handle(notification, ct)                  ← Handler 내부 로그 (ctx.* 포함)
├─ logger.LogDomainEventHandlerResponseSuccess(...)  ← Response 로그 (ctx.* 포함)
│
└─ enrichment.Dispose()                              ← LogContext.Pop 복원
```

| 로그 위치 | ctx.customer_id | ctx.order_placed_event.* | 결과 |
|-----------|----------------|--------------------------|------|
| Request 로그 | CUST-001 | order_id, line_count, total_amount | **포함** |
| Handler 내부 | CUST-001 | order_id, line_count, total_amount | **포함** |
| Response 로그 | CUST-001 | order_id, line_count, total_amount | **포함** |

---

## 4. Usecase Enricher vs Domain Event Enricher 비교

| 구분 | Usecase Log Enricher | Domain Event Log Enricher |
|------|---------------------|--------------------------|
| 인터페이스 | `IUsecaseLogEnricher<TReq, TResp>` | `IDomainEventLogEnricher<TEvent>` |
| 적용 위치 | `UsecaseLoggingPipeline` | `ObservableDomainEventNotificationPublisher` |
| 소스 생성 | Source Generator 자동 생성 | **수동 구현** |
| Root 필드 | `[LogEnricherRoot]` 어트리뷰트 | 수동 `ctx.customer_id` Push |
| 스코프 | Request/Response 파이프라인 | Handler 전체 (Request → Handler → Response) |

---

## 5. 전체 시나리오 목록

| # | 시나리오 | 관찰 가능성 | 로그 레벨 |
|---|----------|------------|-----------|
| 1 | PlaceOrderCommand | Usecase Metrics + Tracing + **Log Enricher** | INF |
| 2 | GetOrderSummaryQuery | 기준선 (커스텀 없음) | INF |
| 3 | FailExpectedCommand | Expected 비즈니스 에러 | WRN |
| 4 | FailExceptionalCommand | Exceptional 시스템 에러 | ERR |
| **5** | **OrderPlacedEvent** | **Domain Event Log Enricher** | **INF** |

---

## 6. 검증 요약

| # | 검증 항목 | 결과 |
|---|----------|------|
| 1 | `dotnet build Functorium.slnx` 빌드 성공 | PASS |
| 2 | `NotificationPublisherType` 설정 → `ObservableDomainEventNotificationPublisher` | PASS |
| 3 | `OrderPlacedEvent` → `DomainEvent` 파생 확인 | PASS |
| 4 | `OrderPlacedEventHandler` → `IDomainEventHandler<OrderPlacedEvent>` 정상 호출 | PASS |
| 5 | `OrderPlacedEventLogEnricher` DI 등록 및 자동 해석 | PASS |
| 6 | `ctx.*` 필드가 Request 로그에 포함 | PASS |
| 7 | `ctx.*` 필드가 Handler 내부 로그에 포함 | PASS |
| 8 | `ctx.*` 필드가 Response 로그에 포함 | PASS |
| 9 | Root 필드 `ctx.customer_id` 출력 | PASS |
| 10 | 기존 Scenario 1~4 회귀 없음 | PASS |
| 11 | PlaceOrderCommand Usecase Enricher 정상 유지 | PASS |
| 12 | 전체 테스트 1480 passed, 0 failed | PASS |
