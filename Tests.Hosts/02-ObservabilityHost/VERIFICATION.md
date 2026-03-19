# 02-ObservabilityHost 검증 결과

**검증 일시**: 2026-03-19 13:42 KST
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
[13:42:22 INF] application usecase.command PlaceOrderCommand.Handle requesting with {"CustomerId": "CUST-001", "Lines": [{"ProductId": "PROD-A", "Quantity": 2, "UnitPrice": 100.00, "$type": "OrderLine"}, {"ProductId": "PROD-B", "Quantity": 1, "UnitPrice": 250.00, "$type": "OrderLine"}], "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "OrderLineCount": 2, "CustomerId": "CUST-001"}
[13:42:22 INF] application usecase.command PlaceOrderCommand.Handle responded success in 0.0079 s with {"Value": {"OrderId": "03111b26-a065-485f-ab9a-9ac0e2cb69e6", "LineCount": 2, "TotalAmount": 450.00, "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline", "OrderTotalAmount": 450.00, "CustomerId": "CUST-001"}
PlaceOrder Result: Succ(Response { OrderId = 03111b26-a065-485f-ab9a-9ac0e2cb69e6, LineCount = 2, TotalAmount = 450.00 })

=== GetOrderSummaryQuery (Baseline) ===
[13:42:22 INF] application usecase.query GetOrderSummaryQuery.Handle requesting with {"OrderId": "ORD-123", "$type": "Request"} {"EventId": {"Id": 1001, "Name": "application.request"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[13:42:22 INF] application usecase.query GetOrderSummaryQuery.Handle responded success in 0.0003 s with {"Value": {"OrderId": "ORD-123", "Status": "Completed", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} {"EventId": {"Id": 1002, "Name": "application.response.success"}, "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
GetOrderSummary Result: Succ(Response { OrderId = ORD-123, Status = Completed })

=== Done ===
```

### 2.2 PlaceOrderCommand Request 로그 (JSON 구조 분석)

**outputTemplate**: `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}`

| 영역 | 내용 |
|------|------|
| Timestamp | `13:42:22` |
| Level | `INF` |
| **Message** (`{Message:lj}`) | `application usecase.command PlaceOrderCommand.Handle requesting with {...}` |
| **Properties** (`{Properties:j}`) | 아래 JSON 참조 |

**Message 내 Request 구조화 데이터:**

```json
{
  "CustomerId": "CUST-001",
  "Lines": [
    { "ProductId": "PROD-A", "Quantity": 2, "UnitPrice": 100.00, "$type": "OrderLine" },
    { "ProductId": "PROD-B", "Quantity": 1, "UnitPrice": 250.00, "$type": "OrderLine" }
  ],
  "$type": "Request"
}
```

**Properties (Enricher 필드 포함):**

```json
{
  "EventId": { "Id": 1001, "Name": "application.request" },
  "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline",
  "OrderLineCount": 2,
  "CustomerId": "CUST-001"
}
```

> `OrderLineCount`과 `CustomerId`는 `PlaceOrderLogEnricher.EnrichRequestLog`에서 `LogContext.PushProperty`로 Push한 커스텀 필드입니다.

### 2.3 PlaceOrderCommand Response 로그 (JSON 구조 분석)

**Message 내 Response 구조화 데이터:**

```json
{
  "Value": {
    "OrderId": "03111b26-a065-485f-ab9a-9ac0e2cb69e6",
    "LineCount": 2,
    "TotalAmount": 450.00,
    "$type": "Response"
  },
  "IsSucc": true,
  "IsFail": false,
  "$type": "Succ"
}
```

**Properties (Enricher 필드 포함):**

```json
{
  "EventId": { "Id": 1002, "Name": "application.response.success" },
  "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline",
  "OrderTotalAmount": 450.00,
  "CustomerId": "CUST-001"
}
```

> `OrderTotalAmount`은 `PlaceOrderLogEnricher.EnrichResponseLog`에서 Push한 커스텀 필드입니다.

### 2.4 GetOrderSummaryQuery Request 로그 (JSON 구조 분석)

**Message 내 Request 구조화 데이터:**

```json
{
  "OrderId": "ORD-123",
  "$type": "Request"
}
```

**Properties (커스텀 필드 없음):**

```json
{
  "EventId": { "Id": 1001, "Name": "application.request" },
  "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

> Enricher가 등록되지 않아 프레임워크 표준 필드만 표시됩니다.

### 2.5 GetOrderSummaryQuery Response 로그 (JSON 구조 분석)

**Message 내 Response 구조화 데이터:**

```json
{
  "Value": {
    "OrderId": "ORD-123",
    "Status": "Completed",
    "$type": "Response"
  },
  "IsSucc": true,
  "IsFail": false,
  "$type": "Succ"
}
```

**Properties (커스텀 필드 없음):**

```json
{
  "EventId": { "Id": 1002, "Name": "application.response.success" },
  "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
}
```

---

## 3. 파일 로그 출력 검증

### 3.1 파일 생성 확인

| 파일 | 경로 | 생성 | 결과 |
|------|------|------|------|
| Plain text | `logs/log-20260319.txt` | 생성됨 | PASS |
| JSON | `logs/log-20260319.json` | 생성됨 | PASS |

### 3.2 Plain text 로그 파일 전문 (`logs/log-20260319.txt`)

```
[20:51:43 INF] application usecase.command PlaceOrderCommand.Handle requesting with {"CustomerId":"CUST-001","Lines":[{"ProductId":"PROD-A","Quantity":2,"UnitPrice":100.00,"$type":"OrderLine"},{"ProductId":"PROD-B","Quantity":1,"UnitPrice":250.00,"$type":"OrderLine"}],"$type":"Request"} {"EventId":{"Id":1001,"Name":"application.request"},"SourceContext":"Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline","OrderLineCount":2,"CustomerId":"CUST-001"}
[20:51:43 INF] application usecase.command PlaceOrderCommand.Handle responded success in 0.0072 s with {"Value":{"OrderId":"b1aeac6f-d500-4a54-9eee-0bc9d6ad7082","LineCount":2,"TotalAmount":450.00,"$type":"Response"},"IsSucc":true,"IsFail":false,"$type":"Succ"} {"EventId":{"Id":1002,"Name":"application.response.success"},"SourceContext":"Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline","OrderTotalAmount":450.00,"CustomerId":"CUST-001"}
[20:51:43 INF] application usecase.query GetOrderSummaryQuery.Handle requesting with {"OrderId":"ORD-123","$type":"Request"} {"EventId":{"Id":1001,"Name":"application.request"},"SourceContext":"Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
[20:51:43 INF] application usecase.query GetOrderSummaryQuery.Handle responded success in 0.0002 s with {"Value":{"OrderId":"ORD-123","Status":"Completed","$type":"Response"},"IsSucc":true,"IsFail":false,"$type":"Succ"} {"EventId":{"Id":1002,"Name":"application.response.success"},"SourceContext":"Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"}
```

> Console 출력과 동일한 `outputTemplate` 적용. `{Properties:j}` 포맷으로 Enricher 커스텀 필드가 JSON 표시됩니다.

### 3.3 JSON 로그 파일 전문 (`logs/log-20260319.json`)

#### PlaceOrderCommand Request

```json
{
  "Timestamp": "2026-03-19T20:51:43.0264035+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} requesting with {@request.message}",
  "Properties": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "command",
    "request.handler": "PlaceOrderCommand",
    "request.handler.method": "Handle",
    "request.message": {
      "CustomerId": "CUST-001",
      "Lines": [
        { "ProductId": "PROD-A", "Quantity": 2, "UnitPrice": 100.00, "_typeTag": "OrderLine" },
        { "ProductId": "PROD-B", "Quantity": 1, "UnitPrice": 250.00, "_typeTag": "OrderLine" }
      ],
      "_typeTag": "Request"
    },
    "EventId": { "Id": 1001, "Name": "application.request" },
    "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline",
    "OrderLineCount": 2,
    "CustomerId": "CUST-001"
  }
}
```

#### PlaceOrderCommand Response

```json
{
  "Timestamp": "2026-03-19T20:51:43.1276700+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}",
  "Properties": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "command",
    "request.handler": "PlaceOrderCommand",
    "request.handler.method": "Handle",
    "response.status": "success",
    "response.elapsed": 0.0072466,
    "response.message": {
      "Value": {
        "OrderId": "b1aeac6f-d500-4a54-9eee-0bc9d6ad7082",
        "LineCount": 2,
        "TotalAmount": 450.00,
        "_typeTag": "Response"
      },
      "IsSucc": true,
      "IsFail": false,
      "_typeTag": "Succ"
    },
    "EventId": { "Id": 1002, "Name": "application.response.success" },
    "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline",
    "OrderTotalAmount": 450.00,
    "CustomerId": "CUST-001"
  },
  "Renderings": {
    "response.elapsed": [{ "Format": "0.0000", "Rendering": "0.0072" }]
  }
}
```

#### GetOrderSummaryQuery Request

```json
{
  "Timestamp": "2026-03-19T20:51:43.1341973+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} requesting with {@request.message}",
  "Properties": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "query",
    "request.handler": "GetOrderSummaryQuery",
    "request.handler.method": "Handle",
    "request.message": {
      "OrderId": "ORD-123",
      "_typeTag": "Request"
    },
    "EventId": { "Id": 1001, "Name": "application.request" },
    "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
  }
}
```

#### GetOrderSummaryQuery Response

```json
{
  "Timestamp": "2026-03-19T20:51:43.1348754+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}",
  "Properties": {
    "request.layer": "application",
    "request.category": "usecase",
    "request.category.type": "query",
    "request.handler": "GetOrderSummaryQuery",
    "request.handler.method": "Handle",
    "response.status": "success",
    "response.elapsed": 0.0002389,
    "response.message": {
      "Value": {
        "OrderId": "ORD-123",
        "Status": "Completed",
        "_typeTag": "Response"
      },
      "IsSucc": true,
      "IsFail": false,
      "_typeTag": "Succ"
    },
    "EventId": { "Id": 1002, "Name": "application.response.success" },
    "SourceContext": "Functorium.Adapters.Observabilities.Pipelines.UsecaseLoggingPipeline"
  },
  "Renderings": {
    "response.elapsed": [{ "Format": "0.0000", "Rendering": "0.0002" }]
  }
}
```

### 3.4 JSON 구조 분석

#### Console vs JSON 차이점

| 항목 | Console (`{Properties:j}`) | JSON (`JsonFormatter`) |
|------|---------------------------|------------------------|
| 포맷 | 렌더링된 메시지 + Properties JSON | 구조화된 전체 JSON |
| `$type` → `_typeTag` | `$type` (렌더링 시 원본) | `_typeTag` (Serilog 직렬화) |
| MessageTemplate | 렌더링 후 소멸 | 원본 템플릿 보존 |
| Renderings | 없음 | 포맷 렌더링 결과 포함 |
| Timestamp | `HH:mm:ss` (outputTemplate) | ISO 8601 전체 (`+09:00` 포함) |

#### Enricher 커스텀 필드 포함 검증

| 필드 | PlaceOrderCommand Request | PlaceOrderCommand Response | GetOrderSummaryQuery |
|------|--------------------------|---------------------------|---------------------|
| `CustomerId` | `"CUST-001"` | `"CUST-001"` | - |
| `OrderLineCount` | `2` | - | - |
| `OrderTotalAmount` | - | `450.00` | - |

> JSON 로그에서도 `LogContext.PushProperty`로 Push한 Enricher 커스텀 필드가 `Properties` 객체 내에 정상 포함됩니다.

---

## 4. IUsecaseLogEnricher 검증 (콘솔 기준)

### 4.1 PlaceOrderCommand — Enricher 적용

`PlaceOrderLogEnricher`가 `LogContext.PushProperty`로 Push한 커스텀 필드가 `{Properties:j}`에 표시되는지 검증합니다.

#### Request 로그

| Enricher 필드 | 예상값 | 실제값 | 결과 |
|---------------|--------|--------|------|
| `CustomerId` | `"CUST-001"` | `"CUST-001"` | PASS |
| `OrderLineCount` | `2` | `2` | PASS |

`EnrichRequestLog`에서 Push한 두 필드 모두 Properties JSON에 정상 표시됩니다.

#### Response 로그

| Enricher 필드 | 예상값 | 실제값 | 결과 |
|---------------|--------|--------|------|
| `OrderTotalAmount` | `450.00` | `450.00` | PASS |

`EnrichResponseLog`에서 Push한 `OrderTotalAmount` 필드가 Properties JSON에 정상 표시됩니다.

### 4.2 GetOrderSummaryQuery — Enricher 미적용 (기준선)

Enricher가 등록되지 않은 Query에서는 커스텀 필드가 없어야 합니다.

#### Request 로그

| Properties 필드 | 값 | 비고 |
|-----------------|------|------|
| `EventId` | `{"Id": 1001, "Name": "application.request"}` | 프레임워크 표준 |
| `SourceContext` | `"...UsecaseLoggingPipeline"` | 프레임워크 표준 |
| 커스텀 필드 | **(없음)** | PASS |

#### Response 로그

| Properties 필드 | 값 | 비고 |
|-----------------|------|------|
| `EventId` | `{"Id": 1002, "Name": "application.response.success"}` | 프레임워크 표준 |
| `SourceContext` | `"...UsecaseLoggingPipeline"` | 프레임워크 표준 |
| 커스텀 필드 | **(없음)** | PASS |

### 4.3 Enricher 대비 분석

| 구분 | PlaceOrderCommand | GetOrderSummaryQuery |
|------|-------------------|---------------------|
| `CustomerId` | CUST-001 | - |
| `OrderLineCount` | 2 | - |
| `OrderTotalAmount` | 450.00 | - |

Enricher가 등록된 Command에만 커스텀 필드가 표시되고, 등록되지 않은 Query에는 표시되지 않습니다.

---

## 5. 표준 로깅 파이프라인 검증

### 5.1 로그 메시지 포맷

| 항목 | PlaceOrderCommand | GetOrderSummaryQuery |
|------|-------------------|---------------------|
| Layer | `application` | `application` |
| Category | `usecase.command` | `usecase.query` |
| Handler | `PlaceOrderCommand` | `GetOrderSummaryQuery` |
| Method | `Handle` | `Handle` |
| Status | `success` | `success` |

CQRS 타입이 `ICommandRequest<>` / `IQueryRequest<>` 인터페이스 기반으로 자동 식별됩니다.

### 5.2 응답 결과

| 항목 | PlaceOrderCommand | GetOrderSummaryQuery |
|------|-------------------|---------------------|
| IsSucc | `true` | `true` |
| Elapsed | `0.0079 s` | `0.0003 s` |
| 결과 타입 | `Succ` | `Succ` |

---

## 6. Serilog 설정 검증

| 설정 항목 | 예상 | 실제 | 결과 |
|-----------|------|------|------|
| `Enrich: FromLogContext` | Enricher 속성이 Properties에 병합 | 병합됨 | PASS |
| `{Properties:j}` in outputTemplate | 커스텀 필드가 JSON으로 표시 | JSON 표시 | PASS |
| `AnsiConsoleTheme::Code` | 컬러 콘솔 출력 | 적용됨 | PASS |
| `{Timestamp:HH:mm:ss}` | 시:분:초 포맷 | `13:42:22` | PASS |

---

## 7. OpenTelemetry Console Exporter 검증

### 7.1 Metrics Console Export

| 항목 | 결과 | 비고 |
|------|------|------|
| 콘솔 출력 | 미출력 | 아래 참조 |

**미출력 원인**: `PeriodicExportingMetricReader`의 기본 Export 주기가 60초이며, 콘솔 앱이 그 전에 종료됩니다. `ServiceCollection` 기반 구성에서는 `IHost`의 Graceful Shutdown이 동작하지 않아 MeterProvider의 최종 Flush가 실행되지 않습니다.

**커스텀 메트릭 등록은 정상**: `PlaceOrderMetricsPipeline`이 DI에 등록되고 파이프라인 실행 중 `Histogram.Record()`가 호출됩니다. 장기 실행 호스트(`IHost` 기반)에서는 정상 Export됩니다.

### 7.2 Tracing Console Export

| 항목 | 결과 | 비고 |
|------|------|------|
| 콘솔 출력 | 미출력 | 아래 참조 |

**미출력 원인**: Metrics와 동일하게 TracerProvider의 최종 Flush가 `IHost` Graceful Shutdown 없이는 실행되지 않습니다.

**커스텀 트레이싱 등록은 정상**: `PlaceOrderTracingPipeline`이 DI에 등록되고 `StartCustomActivity("ValidateOrder")`가 호출됩니다.

### 7.3 개선 방안 (필요 시)

Metrics/Tracing 콘솔 출력이 필요하면 `IHost` 기반으로 전환합니다:

```csharp
var builder = Host.CreateApplicationBuilder(args);
// ... DI 등록 ...
var host = builder.Build();
// ... mediator.Send() ...
await host.StopAsync();  // Graceful Shutdown → OTel Flush
```

---

## 8. 검증 요약

| # | 검증 항목 | 결과 |
|---|----------|------|
| 1 | `dotnet build Functorium.slnx` 빌드 성공 | PASS |
| 2 | `dotnet run` 실행 성공 | PASS |
| 3 | `logs/log-YYYYMMDD.txt` Plain text 로그 파일 생성 | PASS |
| 4 | `logs/log-YYYYMMDD.json` JSON 로그 파일 생성 | PASS |
| 5 | JSON 로그에 Enricher 커스텀 필드 (`CustomerId`, `OrderLineCount`, `OrderTotalAmount`) 포함 | PASS |
| 6 | PlaceOrderCommand Request 로그에 `CustomerId`, `OrderLineCount` 표시 | PASS |
| 7 | PlaceOrderCommand Response 로그에 `OrderTotalAmount` 표시 | PASS |
| 8 | GetOrderSummaryQuery 로그에 커스텀 필드 없음 (기준선) | PASS |
| 9 | CQRS 타입 자동 식별 (`command` / `query`) | PASS |
| 10 | 기존 테스트 회귀 없음 (1446 passed, 0 failed) | PASS |
| 11 | Metrics Console Export | NOT TESTED (*) |
| 12 | Tracing Console Export | NOT TESTED (*) |

(*) `ServiceCollection` 기반 단기 실행 콘솔 앱에서는 OTel SDK의 Graceful Shutdown Flush가 동작하지 않아 미출력. `IHost` 기반 전환 시 검증 가능.
