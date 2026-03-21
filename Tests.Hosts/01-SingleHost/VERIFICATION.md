# 01-SingleHost — 관찰 가능성 로그 검증 결과

> **검증 일시:** 2026-03-21
> **검증 방법:** 웹 API 실행 후 curl 호출 (`LayeredArch.http` 시나리오 기반)
> **Serilog OutputTemplate:** `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}`

---

## 시나리오 개요

| # | 호출 | Usecase 유형 | 로그 레벨 | 핵심 관찰 가능성 |
|---|------|-------------|-----------|-----------------|
| 1 | `POST /api/products` (성공) | Command | INF | Command Request/Response |
| 2 | `POST /api/customers` (성공) | Command + Event | INF | Customer.CreatedEvent Enricher |
| 3 | `POST /api/orders` (성공) | Command + Event | INF | Order.CreatedEvent Enricher + Root ctx + Command Enricher |
| 4 | `GET /api/products` | Query | INF | Query Request/Response |
| 5 | `POST /api/products` (이름 빈값) | Command | WRN | Validation 실패 |
| 6 | `POST /api/test-error` (SingleExpected) | Command | WRN | Expected 비즈니스 에러 |
| 7 | `POST /api/test-error` (SingleExceptional) | Command | ERR | Exceptional 시스템 에러 |

---

## 1. CreateProductCommand — Command 성공

### Console 로그 (Raw)

```
[20:40:31 INF] application usecase.command CreateProductCommand.Handle requesting with {"Name": "Notebook", "Description": "High-performance development notebook", "Price": 1500000, "StockQuantity": 10, "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRI:00000001", "RequestPath": "/api/products", "ConnectionId": "0HNK77Q79FSRI"}
[20:40:31 INF] application usecase.command CreateProductCommand.Handle responded success in 0.0865 s with {"Value": {"ProductId": "01KM834S9N3D1ZS5CJ45JB7QY1", "Name": "Notebook", "Description": "High-performance development notebook", "Price": 1500000, "StockQuantity": 10, "CreatedAt": "2026-03-21T11:40:31.4148221Z", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRI:00000001", "RequestPath": "/api/products", "ConnectionId": "0HNK77Q79FSRI"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:40:31.3865216Z",
  "log.level": "Information",
  "message": "application usecase.command CreateProductCommand.Handle requesting with ...",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} requesting with {@request.message}",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "CreateProductCommand",
  "request.handler_method": "Handle",
  "request.message": "{\"Name\":\"Notebook\",\"Description\":\"High-performance development notebook\",\"Price\":1500000,\"StockQuantity\":10}",
  "ctx.request_id": "0HNK77Q79FSRI:00000001",
  "ctx.request_path": "/api/products",
  "ctx.connection_id": "0HNK77Q79FSRI",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response:**
```json
{
  "@timestamp": "2026-03-21T11:40:31.4743421Z",
  "log.level": "Information",
  "message": "application usecase.command CreateProductCommand.Handle responded success in 0.0865 s with ...",
  "message.template": "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "CreateProductCommand",
  "request.handler_method": "Handle",
  "response.status": "success",
  "response.elapsed": 0.0865307,
  "response.message": "{\"Value\":{\"ProductId\":\"01KM834S9N3D1ZS5CJ45JB7QY1\",\"Name\":\"Notebook\",\"Description\":\"High-performance development notebook\",\"Price\":1500000,\"StockQuantity\":10,\"CreatedAt\":\"2026-03-21T11:40:31.4148221Z\"},\"IsSucc\":true,\"IsFail\":false}",
  "ctx.request_id": "0HNK77Q79FSRI:00000001",
  "ctx.request_path": "/api/products",
  "ctx.connection_id": "0HNK77Q79FSRI",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

---

## 2. CreateCustomerCommand — Command 성공 + Domain Event

### Console 로그 (Raw)

**Command Request/Response:**
```
[20:40:32 INF] application usecase.command CreateCustomerCommand.Handle requesting with {"Name": "Kim Cheolsu", "Email": "kim2@example.com", "CreditLimit": 5000000, "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRJ:00000001", "RequestPath": "/api/customers", "ConnectionId": "0HNK77Q79FSRJ"}
[20:40:32 INF] application usecase.command CreateCustomerCommand.Handle responded success in 0.0154 s with {"Value": {"CustomerId": "01KM834TN24Q1GV3KNSWPFQ19D", "Name": "Kim Cheolsu", "Email": "kim2@example.com", "CreditLimit": 5000000, "CreatedAt": "2026-03-21T11:40:32.8024009Z", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRJ:00000001", "RequestPath": "/api/customers", "ConnectionId": "0HNK77Q79FSRJ"}
```

**Customer.CreatedEvent (Enricher):**
```
[20:40:32 INF] application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM834TN2S58H79EA3RH1SMV8 requesting with {..} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent", "ctx.customer.created_event.email": "kim2@example.com", "ctx.customer.created_event.name": "Kim Cheolsu", "ctx.customer.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D"}
[20:40:32 INF] [DomainEvent] Customer created: 01KM834TN24Q1GV3KNSWPFQ19D, Name: Kim Cheolsu, Email: kim2@example.com {"SourceContext": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent", "ctx.customer.created_event.email": "kim2@example.com", "ctx.customer.created_event.name": "Kim Cheolsu", "ctx.customer.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D"}
[20:40:32 INF] application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM834TN2S58H79EA3RH1SMV8 responded success in 0.0007 s {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent", "ctx.customer.created_event.email": "kim2@example.com", "ctx.customer.created_event.name": "Kim Cheolsu", "ctx.customer.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D"}
```

### OpenSearch JSON 로그

**Command Request:**
```json
{
  "@timestamp": "2026-03-21T11:40:32.8017247Z",
  "log.level": "Information",
  "message": "application usecase.command CreateCustomerCommand.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "CreateCustomerCommand",
  "request.handler_method": "Handle",
  "request.message": "{\"Name\":\"Kim Cheolsu\",\"Email\":\"kim2@example.com\",\"CreditLimit\":5000000}",
  "ctx.request_id": "0HNK77Q79FSRJ:00000001",
  "ctx.request_path": "/api/customers",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**CustomerCreatedEvent Request (Enricher):**
```json
{
  "@timestamp": "2026-03-21T11:40:32.8138755Z",
  "log.level": "Information",
  "message": "application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM834TN2S58H79EA3RH1SMV8 requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "event",
  "request.handler": "CustomerCreatedEvent",
  "request.handler_method": "Handle",
  "request.event.type": "CreatedEvent",
  "request.event.id": "01KM834TN2S58H79EA3RH1SMV8",
  "ctx.customer.created_event.email": "kim2@example.com",
  "ctx.customer.created_event.name": "Kim Cheolsu",
  "ctx.customer.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "log.logger": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent"
}
```

**CustomerCreatedEvent Handler 내부:**
```json
{
  "@timestamp": "2026-03-21T11:40:32.8145419Z",
  "log.level": "Information",
  "message": "[DomainEvent] Customer created: 01KM834TN24Q1GV3KNSWPFQ19D, Name: Kim Cheolsu, Email: kim2@example.com",
  "message.template": "[DomainEvent] Customer created: {CustomerId}, Name: {Name}, Email: {Email}",
  "ctx.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "ctx.name": "Kim Cheolsu",
  "ctx.email": "kim2@example.com",
  "ctx.customer.created_event.email": "kim2@example.com",
  "ctx.customer.created_event.name": "Kim Cheolsu",
  "ctx.customer.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "log.logger": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent"
}
```

**CustomerCreatedEvent Response:**
```json
{
  "@timestamp": "2026-03-21T11:40:32.8152364Z",
  "log.level": "Information",
  "message": "application usecase.event CustomerCreatedEvent.Handle CreatedEvent 01KM834TN2S58H79EA3RH1SMV8 responded success in 0.0007 s",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.handler": "CustomerCreatedEvent",
  "request.event.type": "CreatedEvent",
  "request.event.id": "01KM834TN2S58H79EA3RH1SMV8",
  "response.status": "success",
  "response.elapsed": 0.0007256,
  "ctx.customer.created_event.email": "kim2@example.com",
  "ctx.customer.created_event.name": "Kim Cheolsu",
  "ctx.customer.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "log.logger": "LayeredArch.Application.Usecases.Customers.Events.CustomerCreatedEvent"
}
```

**Command Response:**
```json
{
  "@timestamp": "2026-03-21T11:40:32.8177706Z",
  "log.level": "Information",
  "message": "application usecase.command CreateCustomerCommand.Handle responded success in 0.0154 s with ...",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.handler": "CreateCustomerCommand",
  "response.status": "success",
  "response.elapsed": 0.0154468,
  "response.message": "{\"Value\":{\"CustomerId\":\"01KM834TN24Q1GV3KNSWPFQ19D\",\"Name\":\"Kim Cheolsu\",\"Email\":\"kim2@example.com\",\"CreditLimit\":5000000,\"CreatedAt\":\"2026-03-21T11:40:32.8024009Z\"},\"IsSucc\":true,\"IsFail\":false}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

### Enricher 필드

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer.created_event.customer_id` | `01KM834TN24Q1GV3KNSWPFQ19D` | 고객 ID |
| `ctx.customer.created_event.name` | `Kim Cheolsu` | 고객명 |
| `ctx.customer.created_event.email` | `kim2@example.com` | 이메일 |

---

## 3. CreateOrderCommand — Command 성공 + Domain Event + Enricher

### Console 로그 (Raw)

**Command Request/Response:**
```
[20:40:56 INF] application usecase.command CreateOrderCommand.Handle requesting with {"CustomerId": "01KM834TN24Q1GV3KNSWPFQ19D", "OrderLines": [{"ProductId": "01KM834S9N3D1ZS5CJ45JB7QY1", "Quantity": 2}], "ShippingAddress": "Seoul Gangnam", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.create_order_command.request.order_total_quantity": 2, "ctx.create_order_command.request.shipping_address": "Seoul Gangnam", "ctx.create_order_command.request.order_lines_count": 1, "ctx.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D"}
[20:40:56 INF] application usecase.command CreateOrderCommand.Handle responded success in 0.4831 s with {"Value": {"OrderId": "01KM835HSNW6CNAHHAQBB745BK", ...}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "ctx.create_order_command.response.order_id": "01KM835HSNW6CNAHHAQBB745BK", "ctx.create_order_command.response.total_amount": 3000000, "ctx.create_order_command.response.order_lines_count": 1, "ctx.create_order_command.response.shipping_address": "Seoul Gangnam"}
```

**Order.CreatedEvent (Enricher):**
```
[20:40:56 INF] application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM835J4HCFM8Y3583JTDFFRH requesting with {..} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent", "ctx.order.created_event.total_amount": "3000000", "ctx.order.created_event.order_lines_count": 1, "ctx.order.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D", "ctx.order.created_event.order_id": "01KM835HSNW6CNAHHAQBB745BK"}
[20:40:56 INF] [DomainEvent] Order created: 01KM835HSNW6CNAHHAQBB745BK, OrderLines: 1, TotalAmount: 3000000 {"SourceContext": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent", "ctx.order.created_event.total_amount": "3000000", "ctx.order.created_event.order_lines_count": 1, "ctx.order.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D", "ctx.order.created_event.order_id": "01KM835HSNW6CNAHHAQBB745BK"}
[20:40:56 INF] application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM835J4HCFM8Y3583JTDFFRH responded success in 0.0009 s {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent", "ctx.order.created_event.total_amount": "3000000", "ctx.order.created_event.order_lines_count": 1, "ctx.order.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D", "ctx.order.created_event.order_id": "01KM835HSNW6CNAHHAQBB745BK"}
```

### OpenSearch JSON 로그

**Command Request (Enricher):**
```json
{
  "@timestamp": "2026-03-21T11:40:56.4062696Z",
  "log.level": "Information",
  "message": "application usecase.command CreateOrderCommand.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "command",
  "request.handler": "CreateOrderCommand",
  "request.handler_method": "Handle",
  "ctx.create_order_command.request.order_total_quantity": 2,
  "ctx.create_order_command.request.shipping_address": "Seoul Gangnam",
  "ctx.create_order_command.request.order_lines_count": 1,
  "ctx.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**OrderCreatedEvent Request (Enricher):**
```json
{
  "@timestamp": "2026-03-21T11:40:56.8876453Z",
  "log.level": "Information",
  "message": "application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM835J4HCFM8Y3583JTDFFRH requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "event",
  "request.handler": "OrderCreatedEvent",
  "request.handler_method": "Handle",
  "request.event.type": "CreatedEvent",
  "request.event.id": "01KM835J4HCFM8Y3583JTDFFRH",
  "ctx.order.created_event.total_amount": "3000000",
  "ctx.order.created_event.order_lines_count": 1,
  "ctx.order.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "ctx.order.created_event.order_id": "01KM835HSNW6CNAHHAQBB745BK",
  "log.logger": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent"
}
```

**OrderCreatedEvent Handler 내부:**
```json
{
  "@timestamp": "2026-03-21T11:40:56.8887912Z",
  "log.level": "Information",
  "message": "[DomainEvent] Order created: 01KM835HSNW6CNAHHAQBB745BK, OrderLines: 1, TotalAmount: 3000000",
  "message.template": "[DomainEvent] Order created: {OrderId}, OrderLines: {OrderLineCount}, TotalAmount: {TotalAmount}",
  "ctx.order_id": "01KM835HSNW6CNAHHAQBB745BK",
  "ctx.order_line_count": 1,
  "ctx.total_amount": "3000000",
  "ctx.order.created_event.total_amount": "3000000",
  "ctx.order.created_event.order_lines_count": 1,
  "ctx.order.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "ctx.order.created_event.order_id": "01KM835HSNW6CNAHHAQBB745BK",
  "log.logger": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent"
}
```

**OrderCreatedEvent Response:**
```json
{
  "@timestamp": "2026-03-21T11:40:56.8890537Z",
  "log.level": "Information",
  "message": "application usecase.event OrderCreatedEvent.Handle CreatedEvent 01KM835J4HCFM8Y3583JTDFFRH responded success in 0.0009 s",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.handler": "OrderCreatedEvent",
  "request.event.type": "CreatedEvent",
  "request.event.id": "01KM835J4HCFM8Y3583JTDFFRH",
  "response.status": "success",
  "response.elapsed": 0.0009405,
  "ctx.order.created_event.total_amount": "3000000",
  "ctx.order.created_event.order_lines_count": 1,
  "ctx.order.created_event.customer_id": "01KM834TN24Q1GV3KNSWPFQ19D",
  "ctx.order.created_event.order_id": "01KM835HSNW6CNAHHAQBB745BK",
  "log.logger": "LayeredArch.Application.Usecases.Orders.Events.OrderCreatedEvent"
}
```

**Command Response (Enricher):**
```json
{
  "@timestamp": "2026-03-21T11:40:56.8931892Z",
  "log.level": "Information",
  "message": "application usecase.command CreateOrderCommand.Handle responded success in 0.4831 s with ...",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.handler": "CreateOrderCommand",
  "response.status": "success",
  "response.elapsed": 0.4831437,
  "ctx.create_order_command.response.created_at": "2026-03-21T11:40:56.8488155Z",
  "ctx.create_order_command.response.shipping_address": "Seoul Gangnam",
  "ctx.create_order_command.response.total_amount": 3000000,
  "ctx.create_order_command.response.order_lines_count": 1,
  "ctx.create_order_command.response.order_id": "01KM835HSNW6CNAHHAQBB745BK",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

### Enricher 필드

**Command Enricher (Request):**

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.customer_id` | `01KM834TN24Q1GV3KNSWPFQ19D` | **Root 필드** — CustomerId |
| `ctx.create_order_command.request.order_lines_count` | `1` | 주문 라인 수 |
| `ctx.create_order_command.request.order_total_quantity` | `2` | 총 수량 |
| `ctx.create_order_command.request.shipping_address` | `Seoul Gangnam` | 배송 주소 |

**Command Enricher (Response):**

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.create_order_command.response.order_id` | `01KM835HSNW6CNAHHAQBB745BK` | 주문 ID |
| `ctx.create_order_command.response.total_amount` | `3000000` | 총 금액 |
| `ctx.create_order_command.response.order_lines_count` | `1` | 주문 라인 수 |
| `ctx.create_order_command.response.shipping_address` | `Seoul Gangnam` | 배송 주소 |

**Domain Event Enricher:**

| ctx 필드 | 값 | 설명 |
|----------|-----|------|
| `ctx.order.created_event.order_id` | `01KM835HSNW6CNAHHAQBB745BK` | 주문 ID |
| `ctx.order.created_event.customer_id` | `01KM834TN24Q1GV3KNSWPFQ19D` | 고객 ID |
| `ctx.order.created_event.order_lines_count` | `1` | 주문 라인 수 |
| `ctx.order.created_event.total_amount` | `3000000` | 총 주문 금액 |

---

## 4. GetAllProductsQuery — Query 성공

### Console 로그 (Raw)

```
[20:40:35 INF] application usecase.query GetAllProductsQuery.Handle requesting with {"$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRL:00000001", "RequestPath": "/api/products", "ConnectionId": "0HNK77Q79FSRL"}
[20:40:35 INF] application usecase.query GetAllProductsQuery.Handle responded success in 0.0027 s with {"Value": {"Products": [{"ProductId": "01KM834S9N3D1ZS5CJ45JB7QY1", "Name": "Notebook", "Price": 1500000}], "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRL:00000001", "RequestPath": "/api/products", "ConnectionId": "0HNK77Q79FSRL"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:40:35.4883492Z",
  "log.level": "Information",
  "message": "application usecase.query GetAllProductsQuery.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "query",
  "request.handler": "GetAllProductsQuery",
  "request.handler_method": "Handle",
  "request.message": "{}",
  "ctx.request_id": "0HNK77Q79FSRL:00000001",
  "ctx.request_path": "/api/products",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response:**
```json
{
  "@timestamp": "2026-03-21T11:40:35.4929939Z",
  "log.level": "Information",
  "message": "application usecase.query GetAllProductsQuery.Handle responded success in 0.0027 s with ...",
  "event.id": 1002,
  "event.name": "application.response.success",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category_type": "query",
  "request.handler": "GetAllProductsQuery",
  "request.handler_method": "Handle",
  "response.status": "success",
  "response.elapsed": 0.00273,
  "response.message": "{\"Value\":{\"Products\":[{\"ProductId\":\"01KM834S9N3D1ZS5CJ45JB7QY1\",\"Name\":\"Notebook\",\"Price\":1500000}]},\"IsSucc\":true,\"IsFail\":false}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

---

## 5. CreateProductCommand — Validation 실패

### Console 로그 (Raw)

```
[20:40:36 INF] application usecase.command CreateProductCommand.Handle requesting with {"Name": "", "Description": "Empty name test", "Price": 1000, "StockQuantity": 5, "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRM:00000001", "RequestPath": "/api/products", "ConnectionId": "0HNK77Q79FSRM"}
[20:40:36 WRN] application usecase.command CreateProductCommand.Handle responded failure in 0.0035 s with expected:AdapterErrors.UsecaseValidationPipeline`2.PipelineValidation {"ErrorType": "ErrorCodeExpected`1", "ErrorCodeId": -1000, "ErrorCode": "AdapterErrors.UsecaseValidationPipeline`2.PipelineValidation", "Message": "Name: [DomainErrors.ProductName.Empty] ProductName cannot be empty. Current value: ''"} {"EventId": {"Id": 1003, "Name": "application.response.warning"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRM:00000001", "RequestPath": "/api/products", "ConnectionId": "0HNK77Q79FSRM"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:40:36.6514868Z",
  "log.level": "Information",
  "message": "application usecase.command CreateProductCommand.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.handler": "CreateProductCommand",
  "request.handler_method": "Handle",
  "request.message": "{\"Name\":\"\",\"Description\":\"Empty name test\",\"Price\":1000,\"StockQuantity\":5}",
  "ctx.request_id": "0HNK77Q79FSRM:00000001",
  "ctx.request_path": "/api/products",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response (Warning):**
```json
{
  "@timestamp": "2026-03-21T11:40:36.6554309Z",
  "log.level": "Warning",
  "message": "application usecase.command CreateProductCommand.Handle responded failure in 0.0035 s with expected:AdapterErrors.UsecaseValidationPipeline`2.PipelineValidation ...",
  "event.id": 1003,
  "event.name": "application.response.warning",
  "request.handler": "CreateProductCommand",
  "request.handler_method": "Handle",
  "response.status": "failure",
  "response.elapsed": 0.0035165,
  "error.type": "expected",
  "error.code": "AdapterErrors.UsecaseValidationPipeline`2.PipelineValidation",
  "error.detail": "{\"ErrorType\":\"ErrorCodeExpected`1\",\"ErrorCodeId\":-1000,\"ErrorCode\":\"AdapterErrors.UsecaseValidationPipeline`2.PipelineValidation\",\"Message\":\"Name: [DomainErrors.ProductName.Empty] ProductName cannot be empty. Current value: ''\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

> Validation 실패는 `log.level: Warning` + `error.type: expected` — 비즈니스 규칙 위반으로 분류

---

## 6. TestErrorCommand — Expected 에러

### Console 로그 (Raw)

```
[20:40:37 INF] application usecase.command TestErrorCommand.Handle requesting with {"Scenario": "SingleExpected", "TestMessage": "Business rule violation test", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRN:00000001", "RequestPath": "/api/test-error", "ConnectionId": "0HNK77Q79FSRN"}
[20:40:37 WRN] application usecase.command TestErrorCommand.Handle responded failure in 0.0050 s with expected:TestErrors.TestErrorCommand.BusinessRuleViolation {"ErrorType": "ErrorCodeExpected", "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation", "ErrorCodeId": -1000, "ErrorCurrentValue": "Business rule violation test", "Message": "Business rule violated: Business rule violation test"} {"EventId": {"Id": 1003, "Name": "application.response.warning"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRN:00000001", "RequestPath": "/api/test-error", "ConnectionId": "0HNK77Q79FSRN"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:40:37.8736371Z",
  "log.level": "Information",
  "message": "application usecase.command TestErrorCommand.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.handler": "TestErrorCommand",
  "request.handler_method": "Handle",
  "request.message": "{\"Scenario\":\"SingleExpected\",\"TestMessage\":\"Business rule violation test\"}",
  "ctx.request_id": "0HNK77Q79FSRN:00000001",
  "ctx.request_path": "/api/test-error",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response (Warning):**
```json
{
  "@timestamp": "2026-03-21T11:40:37.8798662Z",
  "log.level": "Warning",
  "message": "application usecase.command TestErrorCommand.Handle responded failure in 0.0050 s with expected:TestErrors.TestErrorCommand.BusinessRuleViolation ...",
  "event.id": 1003,
  "event.name": "application.response.warning",
  "request.handler": "TestErrorCommand",
  "request.handler_method": "Handle",
  "response.status": "failure",
  "response.elapsed": 0.0049922,
  "error.type": "expected",
  "error.code": "TestErrors.TestErrorCommand.BusinessRuleViolation",
  "error.detail": "{\"ErrorType\":\"ErrorCodeExpected\",\"ErrorCode\":\"TestErrors.TestErrorCommand.BusinessRuleViolation\",\"ErrorCodeId\":-1000,\"ErrorCurrentValue\":\"Business rule violation test\",\"Message\":\"Business rule violated: Business rule violation test\"}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

---

## 7. TestErrorCommand — Exceptional 에러

### Console 로그 (Raw)

```
[20:40:39 INF] application usecase.command TestErrorCommand.Handle requesting with {"Scenario": "SingleExceptional", "TestMessage": "System error test", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRO:00000001", "RequestPath": "/api/test-error", "ConnectionId": "0HNK77Q79FSRO"}
[20:40:39 ERR] application usecase.command TestErrorCommand.Handle responded failure in 0.0198 s with exceptional:TestErrors.TestErrorCommand.SystemFailure {"ErrorType": "ErrorCodeExceptional", "ErrorCode": "TestErrors.TestErrorCommand.SystemFailure", "ErrorCodeId": -2146233079, "Message": "System failure: System error test", "ExceptionDetails": {"TargetSite": null, "Message": "System failure: System error test", "Data": [], "InnerException": null, "HelpLink": null, "Source": null, "HResult": -2146233079, "StackTrace": null, "$type": "InvalidOperationException"}} {"EventId": {"Id": 1004, "Name": "application.response.error"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "RequestId": "0HNK77Q79FSRO:00000001", "RequestPath": "/api/test-error", "ConnectionId": "0HNK77Q79FSRO"}
```

### OpenSearch JSON 로그

**Request:**
```json
{
  "@timestamp": "2026-03-21T11:40:39.0504505Z",
  "log.level": "Information",
  "message": "application usecase.command TestErrorCommand.Handle requesting with ...",
  "event.id": 1001,
  "event.name": "application.request",
  "request.handler": "TestErrorCommand",
  "request.handler_method": "Handle",
  "request.message": "{\"Scenario\":\"SingleExceptional\",\"TestMessage\":\"System error test\"}",
  "ctx.request_id": "0HNK77Q79FSRO:00000001",
  "ctx.request_path": "/api/test-error",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

**Response (Error):**
```json
{
  "@timestamp": "2026-03-21T11:40:39.0739254Z",
  "log.level": "Error",
  "message": "application usecase.command TestErrorCommand.Handle responded failure in 0.0198 s with exceptional:TestErrors.TestErrorCommand.SystemFailure ...",
  "event.id": 1004,
  "event.name": "application.response.error",
  "request.handler": "TestErrorCommand",
  "request.handler_method": "Handle",
  "response.status": "failure",
  "response.elapsed": 0.0198294,
  "error.type": "exceptional",
  "error.code": "TestErrors.TestErrorCommand.SystemFailure",
  "error.detail": "{\"ErrorType\":\"ErrorCodeExceptional\",\"ErrorCode\":\"TestErrors.TestErrorCommand.SystemFailure\",\"ErrorCodeId\":-2146233079,\"Message\":\"System failure: System error test\",\"ExceptionDetails\":{\"TargetSite\":null,\"Message\":\"System failure: System error test\",\"Data\":[],\"InnerException\":null,\"HelpLink\":null,\"Source\":null,\"HResult\":-2146233079,\"StackTrace\":null}}",
  "log.logger": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

> Exceptional 에러는 `log.level: Error` + `event.id: 1004` + `ExceptionDetails` 포함

---

## 인터페이스 스코프 ctx 필드 검증

### 변경 사항

`CreateOrderCommand.Request`에 비-root 인터페이스 `IAuditable`을 추가하여 세 가지 ctx 필드 스코프를 검증합니다.

```csharp
public interface IAuditable { string OperatorId { get; } }              // 비-root
[LogEnricherRoot] public interface ICustomerRequest { string CustomerId { get; } }  // root

public sealed record Request(
    string CustomerId, Seq<OrderLineRequest> OrderLines,
    string ShippingAddress, string OperatorId)
    : ICommandRequest<Response>, ICustomerRequest, IAuditable;
```

### 기대 ctx 필드

| 프로퍼티 | 소속 인터페이스 | 기대 ctx 필드 | 스코프 |
|---------|---------------|-------------|--------|
| `CustomerId` | `[LogEnricherRoot] ICustomerRequest` | `ctx.customer_id` | Root |
| `OperatorId` | `IAuditable` | `ctx.auditable.operator_id` | Interface |
| `OrderLines` | 없음 (직접 프로퍼티) | `ctx.create_order_command.request.order_lines_count` | Usecase |
| `ShippingAddress` | 없음 (직접 프로퍼티) | `ctx.create_order_command.request.shipping_address` | Usecase |

---

## 검증 결과 요약

| # | 검증 항목 | 결과 |
|---|----------|------|
| 1 | CreateProductCommand Request/Response 로그 정상 출력 | **Pass** |
| 2 | CreateCustomerCommand + Customer.CreatedEvent Enricher 정상 출력 | **Pass** |
| 3 | CreateOrderCommand Request Enricher (`ctx.create_order_command.request.*`) | **Pass** |
| 4 | CreateOrderCommand Response Enricher (`ctx.create_order_command.response.*`) | **Pass** |
| 5 | CreateOrderCommand Root 필드 (`ctx.customer_id`) | **Pass** |
| 6 | CreateOrderCommand Interface 스코프 (`ctx.auditable.operator_id`) | **TODO** |
| 7 | Order.CreatedEvent Domain Event Enricher (`ctx.order.created_event.*`) | **Pass** |
| 8 | Order.CreatedEvent Handler 내부 로그에 `ctx.*` 포함 | **Pass** |
| 9 | GetAllProductsQuery Request/Response 로그 정상 출력 | **Pass** |
| 10 | Validation 실패 → `log.level: Warning` + `event.id: 1003` | **Pass** |
| 11 | Expected 에러 → `log.level: Warning` + `error.type: expected` | **Pass** |
| 12 | Exceptional 에러 → `log.level: Error` + `error.type: exceptional` + `ExceptionDetails` | **Pass** |
| 13 | OpenSearch JSON 포맷 정상 출력 (모든 시나리오) | **Pass** |
| 14 | Console Raw 출력과 JSON 필드 일치 | **Pass** |
| 15 | ASP.NET Core HTTP Context 필드 (`ctx.request_id`, `ctx.request_path`) 포함 | **Pass** |
