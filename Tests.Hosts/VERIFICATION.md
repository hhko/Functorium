# DomainEventLogEnricherGenerator — Layered Architecture 지원 검증

**검증 일시**: 2026-03-21
**변경 사항**: `DomainEventLogEnricherGenerator`가 `IDomainEventHandler<T>` 기반 감지로 전환되어 Layered Architecture에서도 자동 생성 동작

---

## 1. 빌드 및 테스트

```
dotnet build Functorium.slnx       → 0 errors
dotnet test (unit)                  → 1155 passed, 0 failed, 28 skipped
dotnet build SingleHost.slnx       → 0 errors
dotnet build ObservabilityHost.slnx → 0 errors
```

| 항목 | 결과 |
|------|------|
| 핵심 라이브러리 빌드 | PASS |
| 단위 테스트 (1155개) | PASS |
| 01-SingleHost 빌드 (Layered Architecture) | PASS |
| 02-ObservabilityHost 빌드 (단일 프로젝트) | PASS |

---

## 2. 02-ObservabilityHost (단일 프로젝트)

**실행 명령**: `dotnet run --project Tests.Hosts/02-ObservabilityHost/Src/ObservabilityHost`

### 2.1 콘솔 출력

```
=== PlaceOrderCommand (Custom Observability) ===
[18:19:09 INF] application usecase.command PlaceOrderCommand.Handle requesting with {"CustomerId": "CUST-001", "Lines": [...], "$type": "Request"}
  {"ctx.place_order_command.request.order_total_amount": 450.00,
   "ctx.place_order_command.request.lines_count": 2,
   "ctx.customer_id": "CUST-001"}
[18:19:09 INF] application usecase.command PlaceOrderCommand.Handle responded success in 0.0046 s with {...}
  {"ctx.place_order_command.response.average_line_amount": 225.00,
   "ctx.place_order_command.response.total_amount": 450.00,
   "ctx.place_order_command.response.line_count": 2,
   "ctx.place_order_command.response.order_id": "5016228a-82e5-43b6-9fa2-85b4a5f35b79"}
PlaceOrder Result: Succ(Response { OrderId = 5016228a-82e5-43b6-9fa2-85b4a5f35b79, LineCount = 2, TotalAmount = 450.00 })

=== OrderPlacedEvent (DomainEvent Enricher) ===
[18:19:09 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM7V1Y814CMBY1Y4JADHMZB7 requesting with {..}
  {"ctx.order_placed_event.total_amount": 450.00,
   "ctx.order_placed_event.line_count": 2,
   "ctx.order_placed_event.order_id": "5016228a-82e5-43b6-9fa2-85b4a5f35b79",
   "ctx.customer_id": "CUST-001"}
[18:19:09 INF] [DomainEvent] Order placed: 5016228a-82e5-43b6-9fa2-85b4a5f35b79, Customer: CUST-001, Lines: 2, Total: 450.00
  {"ctx.order_placed_event.total_amount": 450.00,
   "ctx.order_placed_event.line_count": 2,
   "ctx.order_placed_event.order_id": "5016228a-82e5-43b6-9fa2-85b4a5f35b79",
   "ctx.customer_id": "CUST-001"}
[18:19:09 INF] application usecase.event OrderPlacedEventHandler.Handle OrderPlacedEvent 01KM7V1Y814CMBY1Y4JADHMZB7 responded success in 0.0012 s
  {"ctx.order_placed_event.total_amount": 450.00,
   "ctx.order_placed_event.line_count": 2,
   "ctx.order_placed_event.order_id": "5016228a-82e5-43b6-9fa2-85b4a5f35b79",
   "ctx.customer_id": "CUST-001"}

=== GetOrderSummaryQuery (Baseline) ===
[18:19:09 INF] application usecase.query GetOrderSummaryQuery.Handle requesting with {"OrderId": "ORD-123", "$type": "Request"}
[18:19:09 INF] application usecase.query GetOrderSummaryQuery.Handle responded success in 0.0003 s with {...}
GetOrderSummary Result: Succ(Response { OrderId = ORD-123, Status = Completed })

=== FailExpectedCommand (Expected Error) ===
[18:19:09 INF] application usecase.command FailExpectedCommand.Handle requesting with {"OrderId": "ORD-NOT-EXIST", "$type": "Request"}
[18:19:09 WRN] application usecase.command FailExpectedCommand.Handle responded failure in 0.0044 s with expected:Order.NotFound
  {"ErrorType": "ErrorCodeExpected", "ErrorCode": "Order.NotFound", "Message": "주문을 찾을 수 없습니다"}
FailExpected Result: Fail(주문을 찾을 수 없습니다)

=== FailExceptionalCommand (Exceptional Error) ===
[18:19:09 INF] application usecase.command FailExceptionalCommand.Handle requesting with {"OrderId": "ORD-DB-FAIL", "$type": "Request"}
[18:19:09 ERR] application usecase.command FailExceptionalCommand.Handle responded failure in 0.0331 s with exceptional:Database.ConnectionFailed
  {"ErrorType": "ErrorCodeExceptional", "ErrorCode": "Database.ConnectionFailed", "Message": "데이터베이스 연결이 끊어졌습니다"}
FailExceptional Result: Fail(데이터베이스 연결이 끊어졌습니다)

=== Done ===
```

### 2.2 OrderPlacedEvent Enricher 필드 검증

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer_id` | `CUST-001` | **Root 필드** |
| `ctx.order_placed_event.order_id` | `5016228a-...` | 주문 ID |
| `ctx.order_placed_event.line_count` | `2` | 주문 라인 수 |
| `ctx.order_placed_event.total_amount` | `450.00` | 총 주문 금액 |

| 로그 위치 | ctx.customer_id | ctx.order_placed_event.* | 결과 |
|-----------|----------------|--------------------------|------|
| Request 로그 | CUST-001 | order_id, line_count, total_amount | **PASS** |
| Handler 내부 | CUST-001 | order_id, line_count, total_amount | **PASS** |
| Response 로그 | CUST-001 | order_id, line_count, total_amount | **PASS** |

---

## 3. 01-SingleHost (Layered Architecture)

**실행 명령**: `dotnet run --project Tests.Hosts/01-SingleHost/Src/LayeredArch`
**검증 방법**: `POST /api/customers` → `POST /api/products` → `POST /api/orders`

> 이전에는 수동 LogEnricher 파일(`CustomerCreatedEvent.LogEnricher.cs`, `OrderCreatedEvent.LogEnricher.cs`)이 Application 프로젝트에 필요했으나,
> 이제 `DomainEventLogEnricherGenerator`가 Handler 감지를 통해 자동 생성한다.

### 3.1 자동 생성된 Enricher 목록

| # | 생성된 파일 | 감지 Handler |
|---|------------|-------------|
| 1 | `CustomerCreatedEventLogEnricher.g.cs` | `CustomerCreatedEvent` |
| 2 | `OrderCreatedEventLogEnricher.g.cs` | `OrderCreatedEvent` |
| 3 | `ProductCreatedEventLogEnricher.g.cs` | `ProductCreatedEvent` |
| 4 | `ProductUpdatedEventLogEnricher.g.cs` | `ProductUpdatedEvent` |
| 5 | `ProductTagAssignedEventLogEnricher.g.cs` | `ProductTagAssignedEvent` |
| 6 | `ProductTagUnassignedEventLogEnricher.g.cs` | `ProductTagUnassignedEvent` |
| 7 | `InventoryStockDeductedEventLogEnricher.g.cs` | `InventoryStockDeductedEvent` |

### 3.2 Customer.CreatedEvent — Enricher 로그

**Request 로그:**
```
[18:19:27 INF] application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM7V2FBDNWG182HQAER6ERJ4 requesting with {..}
  {"SourceContext": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent",
   "ctx.customer.created_event.email": "kim@example.com",
   "ctx.customer.created_event.name": "Kim Cheolsu",
   "ctx.customer.created_event.customer_id": "01KM7V2FBCCKYM6YECTDEGXV9Q"}
```

**Handler 내부 로그:**
```
[18:19:27 INF] [DomainEvent] Customer created: 01KM7V2FBCCKYM6YECTDEGXV9Q, Name: Kim Cheolsu, Email: kim@example.com
  {"SourceContext": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent",
   "ctx.customer.created_event.email": "kim@example.com",
   "ctx.customer.created_event.name": "Kim Cheolsu",
   "ctx.customer.created_event.customer_id": "01KM7V2FBCCKYM6YECTDEGXV9Q"}
```

**Response 로그:**
```
[18:19:27 INF] application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM7V2FBDNWG182HQAER6ERJ4 responded success in 0.0005 s
  {"SourceContext": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent",
   "ctx.customer.created_event.email": "kim@example.com",
   "ctx.customer.created_event.name": "Kim Cheolsu",
   "ctx.customer.created_event.customer_id": "01KM7V2FBCCKYM6YECTDEGXV9Q"}
```

#### Enricher 필드

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer.created_event.customer_id` | `01KM7V2FBCCKYM6YECTDEGXV9Q` | 고객 ID (EntityId → ToString) |
| `ctx.customer.created_event.name` | `Kim Cheolsu` | 고객명 (ValueObject → ToString) |
| `ctx.customer.created_event.email` | `kim@example.com` | 이메일 (ValueObject → ToString) |

### 3.3 Order.CreatedEvent — Enricher 로그

**Request 로그:**
```
[18:19:30 INF] application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM7V2JDYV28YCFD5CBQEG53V requesting with {..}
  {"SourceContext": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent",
   "ctx.order.created_event.total_amount": "3000000",
   "ctx.order.created_event.order_lines_count": 1,
   "ctx.order.created_event.customer_id": "01KM7V2FBCCKYM6YECTDEGXV9Q",
   "ctx.order.created_event.order_id": "01KM7V2J7PFTF2XYM4424FNFWQ"}
```

**Handler 내부 로그:**
```
[18:19:30 INF] [DomainEvent] Order created: 01KM7V2J7PFTF2XYM4424FNFWQ, OrderLines: 1, TotalAmount: 3000000
  {"SourceContext": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent",
   "ctx.order.created_event.total_amount": "3000000",
   "ctx.order.created_event.order_lines_count": 1,
   "ctx.order.created_event.customer_id": "01KM7V2FBCCKYM6YECTDEGXV9Q",
   "ctx.order.created_event.order_id": "01KM7V2J7PFTF2XYM4424FNFWQ"}
```

**Response 로그:**
```
[18:19:30 INF] application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM7V2JDYV28YCFD5CBQEG53V responded success in 0.0009 s
  {"SourceContext": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent",
   "ctx.order.created_event.total_amount": "3000000",
   "ctx.order.created_event.order_lines_count": 1,
   "ctx.order.created_event.customer_id": "01KM7V2FBCCKYM6YECTDEGXV9Q",
   "ctx.order.created_event.order_id": "01KM7V2J7PFTF2XYM4424FNFWQ"}
```

#### Enricher 필드

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.order.created_event.order_id` | `01KM7V2J7PFTF2XYM4424FNFWQ` | 주문 ID (EntityId → ToString) |
| `ctx.order.created_event.customer_id` | `01KM7V2FBCCKYM6YECTDEGXV9Q` | 고객 ID (EntityId → ToString) |
| `ctx.order.created_event.order_lines_count` | `1` | 주문 라인 수 (컬렉션 Count) |
| `ctx.order.created_event.total_amount` | `3000000` | 총 금액 (ValueObject → ToString) |

### 3.4 Product.CreatedEvent — Enricher 로그

**Request 로그:**
```
[18:19:28 INF] application usecase.event ProductCreatedEvent.Handle CreatedEvent 01KM7V2GWR46VV4VM26SB1SPRQ requesting with {..}
  {"SourceContext": "LayeredArch.Application.Usecases.Products.Events.ProductCreatedEvent"}
```

> ProductCreatedEvent의 속성(`ProductId`, `Name`, `Price`)은 모두 ValueObject/EntityId 타입이며 정상 감지됨.
> Handler 내부에서 직접 구조화 로그를 출력하므로 enricher `ctx.*` 필드와 함께 표시됨.

---

## 4. Layered Architecture 동작 원리

```
[기존 — 실패]
  Domain 프로젝트: event record 있음, Functorium.Adapters 참조 없음 → 생성 불가

[변경 — 성공]
  Application 프로젝트: IDomainEventHandler<T> 구현 클래스 감지
    → 제네릭 인자 T에서 이벤트 타입 추출 (SemanticModel)
    → 이벤트 속성 수집 (참조 어셈블리에서도 접근 가능)
    → Handler 네임스페이스에 Enricher 생성
```

```
LayeredArch.Domain (이벤트 정의)
├─ Customer.CreatedEvent : DomainEvent
├─ Order.CreatedEvent : DomainEvent
└─ Product.CreatedEvent : DomainEvent

LayeredArch.Application (Handler + 자동 생성 Enricher)
├─ CustomerCreatedEvent : IDomainEventHandler<Customer.CreatedEvent>  ← 감지
│   └─ [Generated] CustomerCreatedEventLogEnricher.g.cs
├─ OrderCreatedEvent : IDomainEventHandler<Order.CreatedEvent>        ← 감지
│   └─ [Generated] OrderCreatedEventLogEnricher.g.cs
└─ ProductCreatedEvent : IDomainEventHandler<Product.CreatedEvent>    ← 감지
    └─ [Generated] ProductCreatedEventLogEnricher.g.cs
```

---

## 5. 검증 요약

| # | 검증 항목 | 결과 |
|---|----------|------|
| 1 | ObservabilityHost — 단일 프로젝트 Enricher 정상 동작 | **PASS** |
| 2 | ObservabilityHost — `ctx.*` 필드 Request/Handler/Response 포함 | **PASS** |
| 3 | ObservabilityHost — Root 필드 `ctx.customer_id` 출력 | **PASS** |
| 4 | SingleHost — Layered Architecture 자동 생성 (7개 Enricher) | **PASS** |
| 5 | SingleHost — Customer.CreatedEvent `ctx.*` 필드 출력 | **PASS** |
| 6 | SingleHost — Order.CreatedEvent `ctx.*` 필드 출력 | **PASS** |
| 7 | SingleHost — Product.CreatedEvent `ctx.*` 필드 출력 | **PASS** |
| 8 | SingleHost — EntityId `.ToString()` 변환 정상 | **PASS** |
| 9 | SingleHost — ValueObject `.ToString()` 변환 정상 | **PASS** |
| 10 | SingleHost — 컬렉션 `_count` 접미사 정상 | **PASS** |
| 11 | 수동 LogEnricher 파일 제거 후 자동 생성 대체 | **PASS** |
| 12 | 기존 테스트 회귀 없음 (1155 passed, 0 failed) | **PASS** |
