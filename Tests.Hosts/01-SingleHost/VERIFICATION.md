# 01-SingleHost — Domain Event Log Enricher 검증 결과

> **검증 일시:** 2026-03-21
> **검증 방법:** 웹 API 실행 후 `POST /api/customers`, `POST /api/orders` 호출
> **Serilog OutputTemplate:** `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}`

---

## 1. Customer.CreatedEvent — Enricher 로그 출력

### Enricher 필드

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer.created_event.customer_id` | `01KM5X39ADT0GV9AQ74Z10W6AA` | 고객 ID |
| `ctx.customer.created_event.name` | `Kim Cheolsu` | 고객명 |
| `ctx.customer.created_event.email` | `kim@example.com` | 이메일 |

### Console 로그 (Text)

**Request 로그:**
```
[00:16:21 INF] application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM5X39ADSKMWPPC4NH8MA88R requesting with {..}
  {"EventId":{"Id":1001,"Name":"application.request"},
   "SourceContext":"LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent",
   "ctx.customer.created_event.email":"kim@example.com",
   "ctx.customer.created_event.name":"Kim Cheolsu",
   "ctx.customer.created_event.customer_id":"01KM5X39ADT0GV9AQ74Z10W6AA"}
```

**Handler 내부 로그:**
```
[00:16:21 INF] [DomainEvent] Customer created: 01KM5X39ADT0GV9AQ74Z10W6AA, Name: Kim Cheolsu, Email: kim@example.com
  {"SourceContext":"LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent",
   "ctx.customer.created_event.email":"kim@example.com",
   "ctx.customer.created_event.name":"Kim Cheolsu",
   "ctx.customer.created_event.customer_id":"01KM5X39ADT0GV9AQ74Z10W6AA"}
```

**Response 로그:**
```
[00:16:21 INF] application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM5X39ADSKMWPPC4NH8MA88R responded success in 0.0003 s
  {"EventId":{"Id":1002,"Name":"application.response.success"},
   "SourceContext":"LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent",
   "ctx.customer.created_event.email":"kim@example.com",
   "ctx.customer.created_event.name":"Kim Cheolsu",
   "ctx.customer.created_event.customer_id":"01KM5X39ADT0GV9AQ74Z10W6AA"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-20T15:16:21.9780302Z",
  "log.level": "Information",
  "message": "application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM5X39ADSKMWPPC4NH8MA88R requesting with ...",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} {request.event.type} {request.event.id} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "event",
  "request.handler": "CustomerCreatedEvent",
  "request.handler_method": "Handle",
  "request.event.type": "CreatedEvent",
  "request.event.id": "01KM5X39ADSKMWPPC4NH8MA88R",
  "ctx.customer.created_event.email": "kim@example.com",
  "ctx.customer.created_event.name": "Kim Cheolsu",
  "ctx.customer.created_event.customer_id": "01KM5X39ADT0GV9AQ74Z10W6AA",
  "log.logger": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent"
}
```

**Handler 내부:**
```json
{
  "@timestamp": "2026-03-20T15:16:21.9785619Z",
  "log.level": "Information",
  "message": "[DomainEvent] Customer created: 01KM5X39ADT0GV9AQ74Z10W6AA, Name: Kim Cheolsu, Email: kim@example.com",
  "message.template": "[DomainEvent] Customer created: {CustomerId}, Name: {Name}, Email: {Email}",
  "ctx.customer_id": "01KM5X39ADT0GV9AQ74Z10W6AA",
  "ctx.name": "Kim Cheolsu",
  "ctx.email": "kim@example.com",
  "ctx.customer.created_event.email": "kim@example.com",
  "ctx.customer.created_event.name": "Kim Cheolsu",
  "ctx.customer.created_event.customer_id": "01KM5X39ADT0GV9AQ74Z10W6AA",
  "log.logger": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent"
}
```

**Response:**
```json
{
  "@timestamp": "2026-03-20T15:16:21.9788168Z",
  "log.level": "Information",
  "message": "application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM5X39ADSKMWPPC4NH8MA88R responded success in 0.0003 s",
  "event.id": 1002,
  "event.name": "application.response.success",
  "response.status": "success",
  "response.elapsed": 0.0003217,
  "ctx.customer.created_event.email": "kim@example.com",
  "ctx.customer.created_event.name": "Kim Cheolsu",
  "ctx.customer.created_event.customer_id": "01KM5X39ADT0GV9AQ74Z10W6AA",
  "log.logger": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent"
}
```

---

## 2. Order.CreatedEvent — Enricher 로그 출력

### Enricher 필드

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer_id` | `01KM5X39ADT0GV9AQ74Z10W6AA` | **Root 필드** — CustomerId |
| `ctx.order.created_event.order_id` | `01KM5X3AYW5RSBRBET3KN0S2EX` | 주문 ID |
| `ctx.order.created_event.order_lines_count` | `1` | 주문 라인 수 |
| `ctx.order.created_event.total_amount` | `1500.00` | 총 주문 금액 |

### Console 로그 (Text)

**Request 로그:**
```
[00:16:24 INF] application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM5X3B90RMAKW222ENRASC4R requesting with {..}
  {"EventId":{"Id":1001,"Name":"application.request"},
   "SourceContext":"LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent",
   "ctx.order.created_event.total_amount":"1500.00",
   "ctx.order.created_event.order_lines_count":1,
   "ctx.order.created_event.order_id":"01KM5X3AYW5RSBRBET3KN0S2EX",
   "ctx.customer_id":"01KM5X39ADT0GV9AQ74Z10W6AA"}
```

**Handler 내부 로그:**
```
[00:16:24 INF] [DomainEvent] Order created: 01KM5X3AYW5RSBRBET3KN0S2EX, OrderLines: 1, TotalAmount: 1500.00
  {"SourceContext":"LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent",
   "ctx.order.created_event.total_amount":"1500.00",
   "ctx.order.created_event.order_lines_count":1,
   "ctx.order.created_event.order_id":"01KM5X3AYW5RSBRBET3KN0S2EX",
   "ctx.customer_id":"01KM5X39ADT0GV9AQ74Z10W6AA"}
```

**Response 로그:**
```
[00:16:24 INF] application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM5X3B90RMAKW222ENRASC4R responded success in 0.0007 s
  {"EventId":{"Id":1002,"Name":"application.response.success"},
   "SourceContext":"LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent",
   "ctx.order.created_event.total_amount":"1500.00",
   "ctx.order.created_event.order_lines_count":1,
   "ctx.order.created_event.order_id":"01KM5X3AYW5RSBRBET3KN0S2EX",
   "ctx.customer_id":"01KM5X39ADT0GV9AQ74Z10W6AA"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-20T15:16:24.0054508Z",
  "log.level": "Information",
  "message": "application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM5X3B90RMAKW222ENRASC4R requesting with ...",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} {request.event.type} {request.event.id} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "event",
  "request.handler": "OrderCreatedEvent",
  "request.handler_method": "Handle",
  "request.event.type": "CreatedEvent",
  "request.event.id": "01KM5X3B90RMAKW222ENRASC4R",
  "ctx.order.created_event.total_amount": "1500.00",
  "ctx.order.created_event.order_lines_count": 1,
  "ctx.order.created_event.order_id": "01KM5X3AYW5RSBRBET3KN0S2EX",
  "ctx.customer_id": "01KM5X39ADT0GV9AQ74Z10W6AA",
  "log.logger": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent"
}
```

**Handler 내부:**
```json
{
  "@timestamp": "2026-03-20T15:16:24.0065548Z",
  "log.level": "Information",
  "message": "[DomainEvent] Order created: 01KM5X3AYW5RSBRBET3KN0S2EX, OrderLines: 1, TotalAmount: 1500.00",
  "message.template": "[DomainEvent] Order created: {OrderId}, OrderLines: {OrderLineCount}, TotalAmount: {TotalAmount}",
  "ctx.order_id": "01KM5X3AYW5RSBRBET3KN0S2EX",
  "ctx.order_line_count": 1,
  "ctx.total_amount": "1500.00",
  "ctx.order.created_event.total_amount": "1500.00",
  "ctx.order.created_event.order_lines_count": 1,
  "ctx.order.created_event.order_id": "01KM5X3AYW5RSBRBET3KN0S2EX",
  "ctx.customer_id": "01KM5X39ADT0GV9AQ74Z10W6AA",
  "log.logger": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent"
}
```

**Response:**
```json
{
  "@timestamp": "2026-03-20T15:16:24.0067418Z",
  "log.level": "Information",
  "message": "application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM5X3B90RMAKW222ENRASC4R responded success in 0.0007 s",
  "event.id": 1002,
  "event.name": "application.response.success",
  "response.status": "success",
  "response.elapsed": 0.0006791,
  "ctx.order.created_event.total_amount": "1500.00",
  "ctx.order.created_event.order_lines_count": 1,
  "ctx.order.created_event.order_id": "01KM5X3AYW5RSBRBET3KN0S2EX",
  "ctx.customer_id": "01KM5X39ADT0GV9AQ74Z10W6AA",
  "log.logger": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent"
}
```

---

## 3. 검증 결과 요약

| 검증 항목 | 결과 |
|-----------|------|
| `ctx.*` 필드가 Request 로그에 포함 | **Pass** |
| `ctx.*` 필드가 Handler 내부 로그에 포함 | **Pass** |
| `ctx.*` 필드가 Response 로그에 포함 | **Pass** |
| Root 필드 `ctx.customer_id`가 Order 이벤트에서 출력 | **Pass** |
| OpenSearch JSON 포맷에 `ctx.*` 필드 정상 출력 | **Pass** |
| 기존 테스트 회귀 없음 (1480 passed, 0 failed) | **Pass** |
