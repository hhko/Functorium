# LayeredArch HTTP API 로그 분석

이 문서는 LayeredArch 프로젝트의 HTTP API 호출 시 출력되는 **콘솔 로그**와 **JSON 로그**(구조화된 로그)를 분석한 결과입니다.

## 목차

1. [로그 포맷](#로그-포맷)
2. [API별 로그 출력](#api별-로그-출력)
3. [로그 구조 요약](#로그-구조-요약)
4. [주요 발견사항](#주요-발견사항)

---

## 로그 포맷

### 콘솔 로그 템플릿

```
[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}
```

**실제 형식**:
```
[HH:mm:ss LEVEL] {layer} {category}.{CQRS} {HandlerName}.{Method} {data} {status}
```

| 필드 | 설명 | 예시 |
|------|------|------|
| LEVEL | INF, WRN, ERR | `INF` |
| layer | 레이어 구분 | `application`, `adapter` |
| category | 카테고리 | `usecase`, `repository` |
| CQRS | Command/Query 구분 | `command`, `query` |
| data | 요청/응답 JSON | `{"Name": "...", "$type": "Request"}` |
| status | 상태 | `requesting`, `responded success` |

### JSON 로그 스키마

Serilog `JsonFormatter`로 출력되는 실제 JSON 형식:

```json
{
  "Timestamp": "2026-02-03T11:55:00.7285957+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {@request.message} requesting",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "command",
    "request.handler": "CreateProductCommand",
    "request.handler.method": "Handle",
    "request.message": { "Name": "TestProduct", ... },
    "EventId": { "Id": 1001, "Name": "application.request" },
    "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline",
    "RequestId": "0HNJ2PUFC1KBF:00000001",
    "RequestPath": "/api/products",
    "ConnectionId": "0HNJ2PUFC1KBF"
  }
}
```

| 필드 | 위치 | 설명 |
|------|------|------|
| `Timestamp` | 최상위 | ISO 8601 형식 타임스탬프 |
| `Level` | 최상위 | Information, Warning, Error |
| `MessageTemplate` | 최상위 | Serilog 메시지 템플릿 |
| `TraceId` | 최상위 | OpenTelemetry 32자리 Trace ID |
| `SpanId` | 최상위 | OpenTelemetry 16자리 Span ID |
| `Properties.*` | 중첩 | 구조화된 로그 속성들 |

---

## API별 로그 출력

### API 1: POST /api/products (상품 생성)

**요청**:
```json
{
  "Name": "Laptop",
  "Description": "High-performance laptop",
  "Price": 1500000,
  "StockQuantity": 10
}
```

**응답** (201 Created):
```json
{
  "ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS",
  "Name": "Laptop",
  "Description": "High-performance laptop",
  "Price": 1500000,
  "StockQuantity": 10,
  "CreatedAt": "2026-02-03T01:26:22.9953226Z"
}
```

#### 콘솔 로그

```
[10:56:16 INF] application usecase.command CreateProductCommand.Handle {"Name": "TestProduct", "Description": "A test product", "Price": 100000, "StockQuantity": 10, "$type": "Request"} requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName responded success in 0.0730 s
[10:56:16 INF] adapter repository InMemoryProductRepository.Create requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.Create responded success in 0.0174 s
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events requesting
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle {"ProductId": {...}, "Name": {...}, "Price": {...}, "$type": "CreatedEvent"} requesting
[10:56:16 INF] [DomainEvent] Product created: 01KGGKDXN48CHR54KF22P4AM9G, Name: TestProduct, Price: 100000
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle responded success in 0.0019 s
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events responded success in 0.0157 s
[10:56:16 INF] application usecase.command CreateProductCommand.Handle {"Value": {...}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded success in 0.2487 s
```

**로그 계층 구조**:
1. **Usecase (시작)**: `application usecase.command CreateProductCommand.Handle ... requesting`
2. **Repository**: `adapter repository InMemoryProductRepository.ExistsByName/Create`
3. **DomainEvent Publisher (시작)**: `application domain_event.publisher Product.PublishEvents ... requesting`
4. **DomainEvent Handler (시작)**: `application domain_event.handler OnProductCreated.Handle ... requesting`
5. **Handler 로직**: `[DomainEvent] Product created: ...`
6. **DomainEvent Handler (종료)**: `application domain_event.handler OnProductCreated.Handle responded success`
7. **DomainEvent Publisher (종료)**: `application domain_event.publisher Product.PublishEvents ... responded success`
8. **Usecase (종료)**: `application usecase.command CreateProductCommand.Handle ... responded success`

#### JSON 로그

**Request 로그** (Usecase 시작):
```json
{
  "Timestamp": "2026-02-03T11:55:00.7285957+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {@request.message} requesting",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "command",
    "request.handler": "CreateProductCommand",
    "request.handler.method": "Handle",
    "request.message": {
      "Name": "TestProduct2",
      "Description": "Test for JSON log",
      "Price": 20000,
      "StockQuantity": 5,
      "_typeTag": "Request"
    },
    "EventId": { "Id": 1001, "Name": "application.request" },
    "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline",
    "RequestId": "0HNJ2PUFC1KBF:00000001",
    "RequestPath": "/api/products",
    "ConnectionId": "0HNJ2PUFC1KBF"
  }
}
```

**Response 로그** (Usecase 완료):
```json
{
  "Timestamp": "2026-02-03T11:55:01.1070832+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {@response.message} responded {response.status} in {response.elapsed:0.0000} s",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "command",
    "request.handler": "CreateProductCommand",
    "request.handler.method": "Handle",
    "response.message": {
      "Value": {
        "ProductId": "01KGGPSFPKCW9FF3GN282N9B3M",
        "Name": "TestProduct2",
        "Description": "Test for JSON log",
        "Price": 20000,
        "StockQuantity": 5,
        "CreatedAt": "2026-02-03T02:55:00.8211948Z",
        "_typeTag": "Response"
      },
      "IsSucc": true,
      "IsFail": false,
      "_typeTag": "Succ"
    },
    "response.status": "success",
    "response.elapsed": 0.373971,
    "EventId": { "Id": 1002, "Name": "application.response.success" },
    "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline",
    "RequestId": "0HNJ2PUFC1KBF:00000001",
    "RequestPath": "/api/products"
  },
  "Renderings": {
    "response.elapsed": [{ "Format": "0.0000", "Rendering": "0.3740" }]
  }
}
```

**Repository 로그**:
```json
{
  "Timestamp": "2026-02-03T11:55:00.9682808+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "7415a2b141fc05b7",
  "Properties": {
    "request.layer": "adapter",
    "request.category": "repository",
    "request.handler": "InMemoryProductRepository",
    "request.handler.method": "ExistsByName",
    "response.status": "success",
    "response.elapsed": 0.1138871,
    "EventId": { "Id": 2002, "Name": "adapter.response.success" },
    "SourceContext": "LayeredArch.Adapters.Persistence.Repositories.InMemoryProductRepositoryPipeline"
  }
}
```

**DomainEvent Publisher 로그**:
```json
{
  "Timestamp": "2026-02-03T11:55:01.0769134+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category} {request.handler}.{request.handler.method} {event.count} events requesting",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "request.layer": "application",
    "request.category": "domain_event.publisher",
    "request.handler": "Product",
    "request.handler.method": "PublishEvents",
    "event.count": 1,
    "EventId": { "Id": 3001, "Name": "domain_event.request" },
    "SourceContext": "Functorium.Adapters.Events.ObservableDomainEventPublisher"
  }
}
```

**DomainEvent Handler Pipeline 로그**:
```json
{
  "Timestamp": "2026-02-03T11:55:01.0978280+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category} {request.handler}.{request.handler.method} {@request.message} requesting",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "request.layer": "application",
    "request.category": "domain_event.handler",
    "request.handler": "OnProductCreated",
    "request.handler.method": "Handle",
    "request.message": { "ProductId": "...", "Name": "...", "_typeTag": "CreatedEvent" },
    "EventId": { "Id": 3101, "Name": "domain_event_handler.request" },
    "SourceContext": "LayeredArch.Application.Usecases.Products.OnProductCreated"
  }
}
```

**DomainEvent Handler 커스텀 로그** (개발자 비즈니스 로직):
```json
{
  "Timestamp": "2026-02-03T11:55:01.1008054+09:00",
  "Level": "Information",
  "MessageTemplate": "[DomainEvent] Product created: {ProductId}, Name: {Name}, Price: {Price}",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "ProductId": "01KGGPSFPKCW9FF3GN282N9B3M",
    "Name": "TestProduct2",
    "Price": "20000",
    "SourceContext": "LayeredArch.Application.Usecases.Products.OnProductCreated",
    "RequestId": "0HNJ2PUFC1KBF:00000001",
    "RequestPath": "/api/products"
  }
}
```

#### OpenTelemetry Activity (Trace)

```
Activity.TraceId:            bd3c5f055fad22be6307d3c9b18894bd
Activity.SpanId:             16965e766de661fc
Activity.TraceFlags:         Recorded
Activity.ParentSpanId:       e77db4fbf2c8f7d8
Activity.DisplayName:        application usecase.command CreateProductCommand.Handle
Activity.Kind:               Internal
Activity.StartTime:          2026-02-03T01:26:22.9313217Z
Activity.Duration:           00:00:00.1818000
Activity.Tags:
    request.layer: application
    request.category: usecase
    request.category.type: command
    request.handler: CreateProductCommand
    request.handler.method: Handle
    response.elapsed: 0.1818
    response.status: success
StatusCode: Ok
```

**로그 흐름 분석**:
- Application Layer → Adapter Layer → Domain Event 순서로 로그 출력
- DomainEvent 로그가 **1회만 출력**됨 (수정 완료)
- Request/Response 구조화된 JSON 포함

---

### API 2: GET /api/products (전체 상품 조회)

**응답** (200 OK):
```json
{
  "Products": [
    {
      "ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS",
      "Name": "Laptop",
      "Price": 1500000,
      "StockQuantity": 10
    }
  ]
}
```

#### 콘솔 로그

```
[10:26:24 INF] application usecase.query GetAllProductsQuery.Handle {"$type": "Request"} requesting
[10:26:24 INF] adapter repository InMemoryProductRepository.GetAll requesting
[10:26:24 INF] adapter repository InMemoryProductRepository.GetAll responded success in 0.0239 s
[10:26:24 INF] application usecase.query GetAllProductsQuery.Handle {"Value": {"Products": [{"ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS", "Name": "Laptop", "Price": 1500000, "StockQuantity": 10, "$type": "ProductDto"}], "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded success in 0.0670 s
```

#### JSON 로그

**Request 로그**:
```json
{
  "@t": "2026-02-03T01:26:24.123Z",
  "@l": "Information",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "query",
  "HandlerName": "GetAllProductsQuery",
  "Method": "Handle",
  "Data": { "$type": "Request" },
  "Status": "requesting"
}
```

**Response 로그**:
```json
{
  "@t": "2026-02-03T01:26:24.190Z",
  "@l": "Information",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "query",
  "HandlerName": "GetAllProductsQuery",
  "Method": "Handle",
  "Data": {
    "Value": {
      "Products": [
        { "ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS", "Name": "Laptop", "Price": 1500000 }
      ],
      "$type": "Response"
    },
    "IsSucc": true,
    "$type": "Succ"
  },
  "Elapsed": 0.0670,
  "Status": "responded success"
}
```

#### OpenTelemetry Activity (Trace)

```
Activity.TraceId:            ...
Activity.SpanId:             ...
Activity.DisplayName:        application usecase.query GetAllProductsQuery.Handle
Activity.Kind:               Internal
Activity.Tags:
    request.layer: application
    request.category: usecase
    request.category.type: query
    request.handler: GetAllProductsQuery
    response.status: success
```

**로그 분석**:
- Query는 `usecase.query`로 구분
- DomainEvent 없음 (읽기 전용)

---

### API 3: GET /api/products/{productId} (ID로 상품 조회)

**응답** (200 OK):
```json
{
  "ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS",
  "Name": "Laptop",
  "Description": "High-performance laptop",
  "Price": 1500000,
  "StockQuantity": 10,
  "CreatedAt": "2026-02-03T01:26:22.9953226Z",
  "UpdatedAt": null
}
```

#### 콘솔 로그

```
[10:26:35 INF] application usecase.query GetProductByIdQuery.Handle {"ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS", "$type": "Request"} requesting
[10:26:35 INF] adapter repository InMemoryProductRepository.GetById requesting
[10:26:35 INF] adapter repository InMemoryProductRepository.GetById responded success in 0.0028 s
[10:26:35 INF] application usecase.query GetProductByIdQuery.Handle {"Value": {"ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS", "Name": "Laptop", "Description": "High-performance laptop", "Price": 1500000, "StockQuantity": 10, "CreatedAt": "2026-02-03T01:26:22.9953226Z", "UpdatedAt": null, "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded success in 0.0115 s
```

#### JSON 로그

**Request 로그**:
```json
{
  "@t": "2026-02-03T01:26:35.000Z",
  "@l": "Information",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "query",
  "HandlerName": "GetProductByIdQuery",
  "Method": "Handle",
  "Data": {
    "ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS",
    "$type": "Request"
  },
  "Status": "requesting"
}
```

---

### API 4: PUT /api/products/{productId} (상품 업데이트)

**요청**:
```json
{
  "Name": "Laptop Pro",
  "Description": "Upgraded laptop",
  "Price": 2000000,
  "StockQuantity": 5
}
```

**응답** (200 OK):
```json
{
  "ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS",
  "Name": "Laptop Pro",
  "Description": "Upgraded laptop",
  "Price": 2000000,
  "StockQuantity": 5,
  "UpdatedAt": "2026-02-03T01:26:36.4814895Z"
}
```

#### 콘솔 로그

```
[10:57:24 INF] application usecase.command UpdateProductCommand.Handle {"ProductId": "01KGGKDXN48CHR54KF22P4AM9G", "Name": "Updated Product", "Description": "Updated description", "Price": 150000, "StockQuantity": 20, "SimulateException": false, "$type": "Request"} requesting
[10:57:24 INF] adapter repository InMemoryProductRepository.GetById requesting
[10:57:24 INF] adapter repository InMemoryProductRepository.GetById responded success in 0.0018 s
[10:57:24 INF] adapter repository InMemoryProductRepository.ExistsByName requesting
[10:57:24 INF] adapter repository InMemoryProductRepository.ExistsByName responded success in 0.0013 s
[10:57:24 INF] adapter repository InMemoryProductRepository.Update requesting
[10:57:24 INF] adapter repository InMemoryProductRepository.Update responded success in 0.0013 s
[10:57:24 INF] application domain_event.publisher Product.PublishEvents 1 events requesting
[10:57:24 INF] application domain_event.handler OnProductUpdated.Handle {"ProductId": {...}, "Name": {...}, "OldPrice": {...}, "NewPrice": {...}, "$type": "UpdatedEvent"} requesting
[10:57:24 INF] [DomainEvent] Product updated: 01KGGKDXN48CHR54KF22P4AM9G, Name: Updated Product, OldPrice: 100000, NewPrice: 150000
[10:57:24 INF] application domain_event.handler OnProductUpdated.Handle responded success in 0.0004 s
[10:57:24 INF] application domain_event.publisher Product.PublishEvents 1 events responded success in 0.0012 s
[10:57:24 INF] application usecase.command UpdateProductCommand.Handle {"Value": {...}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded success in 0.0532 s
```

**도메인 이벤트 관찰 가능성**:
- **Publisher 관점**: `domain_event.publisher` - 이벤트 발행 시작/종료 추적
- **Handler 관점**: `domain_event.handler` - 핸들러 실행 시작/종료 및 소요 시간 추적

#### JSON 로그

**DomainEvent 로그**:
```json
{
  "@t": "2026-02-03T01:26:36.480Z",
  "@l": "Information",
  "@mt": "[DomainEvent] Product updated: {ProductId}, Name: {Name}, OldPrice: {OldPrice}, NewPrice: {NewPrice}",
  "ProductId": "01KGGHQ6GJZBM31CMXQ7SKPEYS",
  "Name": "Laptop Pro",
  "OldPrice": 1500000,
  "NewPrice": 2000000,
  "SourceContext": "LayeredArch.Application.Usecases.Products.OnProductUpdated"
}
```

#### OpenTelemetry Activity (Trace)

```
Activity.TraceId:            ...
Activity.SpanId:             ...
Activity.DisplayName:        application usecase.command UpdateProductCommand.Handle
Activity.Duration:           00:00:00.0337000
Activity.Tags:
    request.layer: application
    request.category: usecase
    request.category.type: command
    request.handler: UpdateProductCommand
    response.status: success
```

**로그 분석**:
- 3개의 Repository 호출: GetById → ExistsByName → Update
- DomainEvent 로그가 **1회만 출력**됨 (수정 완료)

---

### API 5: POST /api/test-error (Success)

**요청**:
```json
{
  "Scenario": "Success",
  "TestMessage": "Test message"
}
```

**응답** (200 OK):
```json
{
  "Scenario": 0,
  "Message": "Success: Test message",
  "ExecutedAt": "2026-02-03T01:26:56.9231184Z"
}
```

#### 콘솔 로그

```
[10:26:56 INF] application usecase.command TestErrorCommand.Handle {"Scenario": "Success", "TestMessage": "Test message", "$type": "Request"} requesting
[10:26:56 INF] Testing error scenario: Success
[10:26:56 INF] application usecase.command TestErrorCommand.Handle {"Value": {"Scenario": "Success", "Message": "Success: Test message", "ExecutedAt": "2026-02-03T01:26:56.9231184Z", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded success in 0.0033 s
```

#### JSON 로그

```json
{
  "@t": "2026-02-03T01:26:56.923Z",
  "@l": "Information",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "command",
  "HandlerName": "TestErrorCommand",
  "Method": "Handle",
  "Data": {
    "Value": {
      "Scenario": "Success",
      "Message": "Success: Test message",
      "ExecutedAt": "2026-02-03T01:26:56.9231184Z"
    },
    "IsSucc": true,
    "$type": "Succ"
  },
  "Elapsed": 0.0033,
  "Status": "responded success"
}
```

---

### API 6: POST /api/test-error (SingleExpected - 비즈니스 에러)

**요청**:
```json
{
  "Scenario": "SingleExpected",
  "TestMessage": "Business rule violation"
}
```

**응답** (400 Bad Request):
```json
{
  "StatusCode": 400,
  "Message": "Business rule violated: Business rule violation",
  "Errors": ["Business rule violated: Business rule violation"]
}
```

#### 콘솔 로그

```
[10:27:16 INF] application usecase.command TestErrorCommand.Handle {"Scenario": "SingleExpected", "TestMessage": "Business rule violation", "$type": "Request"} requesting
[10:27:16 INF] Testing error scenario: SingleExpected
[10:27:16 WRN] application usecase.command TestErrorCommand.Handle responded failure in 0.0021 s with expected:TestErrors.TestErrorCommand.BusinessRuleViolation {"ErrorType": "ErrorCodeExpected", "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation", "ErrorCodeId": -1000, "ErrorCurrentValue": "Business rule violation", "Message": "Business rule violated: Business rule violation"}
```

#### JSON 로그

```json
{
  "@t": "2026-02-03T01:27:16.000Z",
  "@l": "Warning",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "command",
  "HandlerName": "TestErrorCommand",
  "Method": "Handle",
  "Elapsed": 0.0021,
  "Status": "responded failure",
  "ErrorPrefix": "expected",
  "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation",
  "ErrorInfo": {
    "ErrorType": "ErrorCodeExpected",
    "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation",
    "ErrorCodeId": -1000,
    "ErrorCurrentValue": "Business rule violation",
    "Message": "Business rule violated: Business rule violation"
  }
}
```

**로그 분석**:
- **WRN (Warning)** 레벨: Expected 에러는 경고로 처리
- `expected:` 접두사로 에러 유형 구분
- 구조화된 에러 정보 출력

---

### API 7: POST /api/test-error (SingleExceptional - 시스템 에러)

**요청**:
```json
{
  "Scenario": "SingleExceptional",
  "TestMessage": "System error"
}
```

**응답** (400 Bad Request):
```json
{
  "StatusCode": 400,
  "Message": "System failure: System error",
  "Errors": ["System failure: System error"]
}
```

#### 콘솔 로그

```
[10:27:29 INF] application usecase.command TestErrorCommand.Handle {"Scenario": "SingleExceptional", "TestMessage": "System error", "$type": "Request"} requesting
[10:27:29 INF] Testing error scenario: SingleExceptional
[10:27:29 ERR] application usecase.command TestErrorCommand.Handle responded failure in 0.0009 s with exceptional:TestErrors.TestErrorCommand.SystemFailure {"ErrorType": "ErrorCodeExceptional", "ErrorCode": "TestErrors.TestErrorCommand.SystemFailure", "ErrorCodeId": -2146233079, "Message": "System failure: System error", "ExceptionDetails": {"TargetSite": null, "Message": "System failure: System error", "Data": [], "InnerException": null, "HelpLink": null, "Source": null, "HResult": -2146233079, "StackTrace": null, "$type": "InvalidOperationException"}}
```

#### JSON 로그

```json
{
  "@t": "2026-02-03T01:27:29.798Z",
  "@l": "Error",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "command",
  "HandlerName": "TestErrorCommand",
  "Method": "Handle",
  "Elapsed": 0.0009,
  "Status": "responded failure",
  "ErrorPrefix": "exceptional",
  "ErrorCode": "TestErrors.TestErrorCommand.SystemFailure",
  "ErrorInfo": {
    "ErrorType": "ErrorCodeExceptional",
    "ErrorCode": "TestErrors.TestErrorCommand.SystemFailure",
    "ErrorCodeId": -2146233079,
    "Message": "System failure: System error",
    "ExceptionDetails": {
      "Message": "System failure: System error",
      "HResult": -2146233079,
      "$type": "InvalidOperationException"
    }
  }
}
```

#### OpenTelemetry Activity (Trace)

```
Activity.TraceId:            259a4b291244dc287cb814063dc262d7
Activity.SpanId:             29f2f546eb8f04d7
Activity.TraceFlags:         Recorded
Activity.ParentSpanId:       c9699ee024f5ea28
Activity.DisplayName:        application usecase.command TestErrorCommand.Handle
Activity.Kind:               Internal
Activity.StartTime:          2026-02-03T01:27:29.7981232Z
Activity.Duration:           00:00:00.0042664
Activity.Tags:
    request.layer: application
    request.category: usecase
    request.category.type: command
    request.handler: TestErrorCommand
    request.handler.method: Handle
    response.status: failure
    error.code: TestErrors.TestErrorCommand.SystemFailure
```

**로그 분석**:
- **ERR (Error)** 레벨: Exceptional 에러는 오류로 처리
- `exceptional:` 접두사로 에러 유형 구분
- `ExceptionDetails` 포함 (Exception 정보)

---

### API 8: POST /api/test-error (ManyExpected - 복합 비즈니스 에러)

**요청**:
```json
{
  "Scenario": "ManyExpected",
  "TestMessage": "Multiple errors"
}
```

**응답** (400 Bad Request):
```json
{
  "StatusCode": 400,
  "Message": "[Business rule violated: Multiple errors - Error 1, Validation failed: Multiple errors - Error 2]",
  "Errors": [
    "Business rule violated: Multiple errors - Error 1",
    "Validation failed: Multiple errors - Error 2"
  ]
}
```

#### 콘솔 로그

```
[10:27:33 INF] application usecase.command TestErrorCommand.Handle {"Scenario": "ManyExpected", "TestMessage": "Multiple errors", "$type": "Request"} requesting
[10:27:33 INF] Testing error scenario: ManyExpected
[10:27:33 WRN] application usecase.command TestErrorCommand.Handle responded failure in 0.0032 s with aggregate:TestErrors.TestErrorCommand.BusinessRuleViolation {"ErrorType": "ManyErrors", "ErrorCodeId": -2000000006, "Count": 2, "Errors": [{"ErrorType": "ErrorCodeExpected", "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation", "ErrorCodeId": -1000, "ErrorCurrentValue": "Multiple errors - Error 1", "Message": "Business rule violated: Multiple errors - Error 1"}, {"ErrorType": "ErrorCodeExpected", "ErrorCode": "TestErrors.TestErrorCommand.ValidationFailed", "ErrorCodeId": -1000, "ErrorCurrentValue": "Multiple errors - Error 2", "Message": "Validation failed: Multiple errors - Error 2"}]}
```

#### JSON 로그

```json
{
  "@t": "2026-02-03T01:27:33.144Z",
  "@l": "Warning",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "command",
  "HandlerName": "TestErrorCommand",
  "Method": "Handle",
  "Elapsed": 0.0032,
  "Status": "responded failure",
  "ErrorPrefix": "aggregate",
  "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation",
  "ErrorInfo": {
    "ErrorType": "ManyErrors",
    "ErrorCodeId": -2000000006,
    "Count": 2,
    "Errors": [
      {
        "ErrorType": "ErrorCodeExpected",
        "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation",
        "ErrorCodeId": -1000,
        "ErrorCurrentValue": "Multiple errors - Error 1",
        "Message": "Business rule violated: Multiple errors - Error 1"
      },
      {
        "ErrorType": "ErrorCodeExpected",
        "ErrorCode": "TestErrors.TestErrorCommand.ValidationFailed",
        "ErrorCodeId": -1000,
        "ErrorCurrentValue": "Multiple errors - Error 2",
        "Message": "Validation failed: Multiple errors - Error 2"
      }
    ]
  }
}
```

#### OpenTelemetry Activity (Trace)

```
Activity.TraceId:            79e879fb0c5510db31740ff73b5b1047
Activity.SpanId:             2aa0e9e5878ad518
Activity.DisplayName:        application usecase.command TestErrorCommand.Handle
Activity.Tags:
    request.layer: application
    request.category: usecase
    request.category.type: command
    response.status: failure
    error.code: TestErrors.TestErrorCommand.BusinessRuleViolation
    error.count: 2
```

**로그 분석**:
- **WRN (Warning)** 레벨: 모든 에러가 Expected이면 경고
- `aggregate:` 접두사: 복합 에러 표시
- `Count`, `Errors` 배열로 다중 에러 정보 포함

---

### API 9: POST /api/test-error (ManyMixed - 혼합 에러)

**요청**:
```json
{
  "Scenario": "ManyMixed",
  "TestMessage": "Mixed errors"
}
```

**응답** (400 Bad Request):
```json
{
  "StatusCode": 400,
  "Message": "[Business rule violated: Mixed errors - Business Error, System failure: Mixed errors]",
  "Errors": [
    "Business rule violated: Mixed errors - Business Error",
    "System failure: Mixed errors"
  ]
}
```

#### 콘솔 로그

```
[10:27:36 INF] application usecase.command TestErrorCommand.Handle {"Scenario": "ManyMixed", "TestMessage": "Mixed errors", "$type": "Request"} requesting
[10:27:36 INF] Testing error scenario: ManyMixed
[10:27:37 ERR] application usecase.command TestErrorCommand.Handle responded failure in 0.0037 s with aggregate:TestErrors.TestErrorCommand.SystemFailure {"ErrorType": "ManyErrors", "ErrorCodeId": -2000000006, "Count": 2, "Errors": [{"ErrorType": "ErrorCodeExpected", "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation", "ErrorCodeId": -1000, "ErrorCurrentValue": "Mixed errors - Business Error", "Message": "Business rule violated: Mixed errors - Business Error"}, {"ErrorType": "ErrorCodeExceptional", "ErrorCode": "TestErrors.TestErrorCommand.SystemFailure", "ErrorCodeId": -2146233079, "Message": "System failure: Mixed errors", "ExceptionDetails": {"TargetSite": null, "Message": "System failure: Mixed errors", "Data": [], "InnerException": null, "HelpLink": null, "Source": null, "HResult": -2146233079, "StackTrace": null, "$type": "InvalidOperationException"}}]}
```

#### JSON 로그

```json
{
  "@t": "2026-02-03T01:27:37.000Z",
  "@l": "Error",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "command",
  "HandlerName": "TestErrorCommand",
  "Method": "Handle",
  "Elapsed": 0.0037,
  "Status": "responded failure",
  "ErrorPrefix": "aggregate",
  "ErrorCode": "TestErrors.TestErrorCommand.SystemFailure",
  "ErrorInfo": {
    "ErrorType": "ManyErrors",
    "ErrorCodeId": -2000000006,
    "Count": 2,
    "Errors": [
      {
        "ErrorType": "ErrorCodeExpected",
        "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation",
        "Message": "Business rule violated: Mixed errors - Business Error"
      },
      {
        "ErrorType": "ErrorCodeExceptional",
        "ErrorCode": "TestErrors.TestErrorCommand.SystemFailure",
        "Message": "System failure: Mixed errors",
        "ExceptionDetails": {
          "Message": "System failure: Mixed errors",
          "$type": "InvalidOperationException"
        }
      }
    ]
  }
}
```

#### OpenTelemetry Activity (Trace)

```
Activity.TraceId:            0fca2b07dcf61be8ec4ecc847e5faee6
Activity.SpanId:             bdd0f9ea99238827
Activity.DisplayName:        application usecase.command TestErrorCommand.Handle
Activity.Tags:
    response.status: failure
    error.code: TestErrors.TestErrorCommand.SystemFailure
    error.count: 2
    error.has_exceptional: true
```

**로그 분석**:
- **ERR (Error)** 레벨: Exceptional이 포함되면 오류로 격상
- `aggregate:` 접두사 + 대표 에러 코드 (SystemFailure)

---

### API 10: POST /api/test-error (GenericExpected - 제네릭 에러)

**요청**:
```json
{
  "Scenario": "GenericExpected",
  "TestMessage": "Generic error"
}
```

**응답** (400 Bad Request):
```json
{
  "StatusCode": 400,
  "Message": "Generic error occurred: Generic error (code: 42)",
  "Errors": ["Generic error occurred: Generic error (code: 42)"]
}
```

#### 콘솔 로그

```
[10:27:52 INF] application usecase.command TestErrorCommand.Handle {"Scenario": "GenericExpected", "TestMessage": "Generic error", "$type": "Request"} requesting
[10:27:52 INF] Testing error scenario: GenericExpected
[10:27:52 WRN] application usecase.command TestErrorCommand.Handle responded failure in 0.0012 s with expected:TestErrors.TestErrorCommand.GenericError {"ErrorType": "ErrorCodeExpected`1", "ErrorCodeId": -1000, "ErrorCode": "TestErrors.TestErrorCommand.GenericError", "Message": "Generic error occurred: Generic error (code: 42)", "ErrorCurrentValue": ["Generic error", 42]}
```

#### JSON 로그

```json
{
  "@t": "2026-02-03T01:27:52.000Z",
  "@l": "Warning",
  "Layer": "application",
  "Category": "usecase",
  "Cqrs": "command",
  "HandlerName": "TestErrorCommand",
  "Method": "Handle",
  "Elapsed": 0.0012,
  "Status": "responded failure",
  "ErrorPrefix": "expected",
  "ErrorCode": "TestErrors.TestErrorCommand.GenericError",
  "ErrorInfo": {
    "ErrorType": "ErrorCodeExpected`1",
    "ErrorCodeId": -1000,
    "ErrorCode": "TestErrors.TestErrorCommand.GenericError",
    "Message": "Generic error occurred: Generic error (code: 42)",
    "ErrorCurrentValue": ["Generic error", 42]
  }
}
```

**로그 분석**:
- 제네릭 타입 에러 (`ErrorCodeExpected\`1`)
- `ErrorCurrentValue` 배열로 여러 값 포함

---

## 로그 구조 요약

### 로그 레이어 구조

```
┌─────────────────────────────────────────────────────────────────────────┐
│ HTTP Request                                                            │
│ Activity.TraceId: bd3c5f055fad22be6307d3c9b18894bd                      │
├─────────────────────────────────────────────────────────────────────────┤
│ UsecaseLoggingPipeline (Application Layer)                              │
│ ├─ Request:  application usecase.{command|query} {Handler} requesting   │
│ └─ Response: application usecase.{command|query} {Handler} responded    │
│              ├─ success: INF 레벨                                       │
│              ├─ expected: WRN 레벨 (비즈니스 에러)                       │
│              └─ exceptional: ERR 레벨 (시스템 에러)                      │
├─────────────────────────────────────────────────────────────────────────┤
│ RepositoryLoggingPipeline (Adapter Layer)                               │
│ ├─ Request:  adapter repository {Repository}.{Method} requesting        │
│ └─ Response: adapter repository {Repository}.{Method} responded success │
├─────────────────────────────────────────────────────────────────────────┤
│ Domain Event Handler                                                    │
│ └─ [DomainEvent] {비즈니스 로그 메시지}                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 에러 로그 레벨 규칙

| 에러 유형 | 로그 레벨 | 접두사 | 설명 |
|-----------|-----------|--------|------|
| Expected (단일) | WRN | `expected:` | 비즈니스 규칙 위반, 유효성 검증 실패 |
| Exceptional (단일) | ERR | `exceptional:` | 시스템 장애, 예외 발생 |
| ManyExpected | WRN | `aggregate:` | 모든 에러가 Expected |
| ManyMixed | ERR | `aggregate:` | Exceptional 포함 시 ERR로 격상 |

### OpenTelemetry Activity Tags

| Tag | 설명 | 예시 |
|-----|------|------|
| `request.layer` | 레이어 | `application`, `adapter` |
| `request.category` | 카테고리 | `usecase`, `repository` |
| `request.handler` | 핸들러 이름 | `CreateProductCommand` |
| `request.category.type` | CQRS 유형 | `command`, `query` |
| `request.handler.method` | 메서드 이름 | `Handle` |
| `response.elapsed` | 실행 시간 (초) | `0.1818` |
| `response.status` | 응답 상태 | `success`, `failure` |
| `error.code` | 에러 코드 | `TestErrors.TestErrorCommand.SystemFailure` |

---

## 주요 발견사항

1. **DomainEvent 로그**: 상품 생성/수정 시 DomainEvent 로그가 **1회만 출력**됨 (중복 수정 완료)
2. **구조화된 로그**: Request/Response가 JSON 형식으로 출력되어 검색/분석 가능
3. **계층 분리**: Application, Adapter, Domain 계층별 로그 명확히 구분
4. **에러 분류 체계**: Expected/Exceptional 에러를 로그 레벨(WRN/ERR)로 구분
5. **OpenTelemetry 통합**: JSON 로그의 **최상위 필드**로 `TraceId`, `SpanId`가 자동 포함되어 분산 추적 가능
6. **HTTP 컨텍스트**: `RequestId`, `RequestPath`, `ConnectionId`가 Properties에 자동 포함
7. **메트릭 자동 수집**: 요청 수, 응답 수, 실행 시간 히스토그램이 자동으로 수집됨
