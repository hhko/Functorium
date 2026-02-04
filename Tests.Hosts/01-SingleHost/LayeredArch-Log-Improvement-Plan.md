# LayeredArch 로그 개선 계획

## 개요

이 문서는 LayeredArch 프로젝트의 로그 시스템을 **콘솔 로그**와 **JSON 로그** 두 관점에서 분석하고 개선 계획을 제시합니다.

| 로그 유형 | 주요 용도 | 대상 |
|-----------|----------|------|
| 콘솔 로그 | 개발/디버깅 시 실시간 확인 | 개발자 |
| JSON 로그 | 로그 분석 시스템, 검색, 알림 | 운영팀, 모니터링 시스템 |

---

## 현재 구현 상태 (2026-02-03 기준)

### 완료된 기능

| 기능 | 설명 | 상태 |
|------|------|------|
| DomainEvent 중복 로그 수정 | `RegisterDomainEventHandlersFromAssembly()` 중복 호출 제거 | ✅ 완료 |
| DomainEvent Publisher 관찰 가능성 | `ObservableDomainEventPublisher` 활성화 | ✅ 완료 |
| DomainEvent Handler 관찰 가능성 | `ObservableDomainEventNotificationPublisher` 구현 | ✅ 완료 |

### 현재 로그 흐름 구조

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
│ DomainEvent Publisher (Application Layer)                               │ ← NEW
│ ├─ Start: application domain_event.publisher {Entity}.PublishEvents     │
│ │         {count} events requesting                                     │
│ └─ End:   application domain_event.publisher {Entity}.PublishEvents     │
│           {count} events responded success in {elapsed} s               │
├─────────────────────────────────────────────────────────────────────────┤
│ DomainEvent Handler (Application Layer)                                 │ ← NEW
│ ├─ Start: application domain_event.handler {Handler}.Handle {...}       │
│ │         requesting                                                    │
│ ├─ Logic: [DomainEvent] {비즈니스 로그 메시지}                            │
│ └─ End:   application domain_event.handler {Handler}.Handle responded   │
│           success in {elapsed} s                                        │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Part 1: 콘솔 로그 관점

### 현재 콘솔 로그 형식

```
[HH:mm:ss LEVEL] {layer} {category}.{cqrs} {Handler}.{Method} {data} {status}
```

**실제 예시 (상품 생성)**:
```
[10:56:16 INF] application usecase.command CreateProductCommand.Handle {"Name": "TestProduct", ...} requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName responded success in 0.0730 s
[10:56:16 INF] adapter repository InMemoryProductRepository.Create requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.Create responded success in 0.0174 s
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events requesting
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle {...} requesting
[10:56:16 INF] [DomainEvent] Product created: 01KGGKDXN48CHR54KF22P4AM9G, Name: TestProduct, Price: 100000
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle responded success in 0.0019 s
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events responded success in 0.0157 s
[10:56:16 INF] application usecase.command CreateProductCommand.Handle {...} responded success in 0.2487 s
```

### 콘솔 로그 문제점

#### 1.1 로그 메시지 과도한 길이

**현상**:
```
[10:56:16 INF] application usecase.command CreateProductCommand.Handle {"Value": {"ProductId": "01KGGKDXN48CHR54KF22P4AM9G", "Name": "TestProduct", "Description": "A test product", "Price": 100000, "StockQuantity": 0, "CreatedAt": "2026-02-03T01:56:16.1658878Z", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded success in 0.2487 s
```

**문제**:
- 한 줄에 200자 이상의 JSON 포함
- 터미널 가로 스크롤 필요
- 핵심 정보(성공/실패, 시간) 파악 어려움

**개선안**:
```
[10:56:16 INF] application usecase.command CreateProductCommand responded success in 0.2487 s
              └─ ProductId: 01KGGKDXN48CHR54KF22P4AM9G
```

#### 1.2 ~~DomainEvent 로그 비표준 형식~~ (해결됨)

**개선 전**:
```
[09:29:38 INF] [DomainEvent] Product created: 01KGGEFA2EA5765TP35Q4C8W1Y, Name: 노트북, Price: 1500000
```

**개선 후** (현재 상태):
```
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events requesting
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle {...} requesting
[10:56:16 INF] [DomainEvent] Product created: 01KGGKDXN48CHR54KF22P4AM9G, Name: TestProduct, Price: 100000
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle responded success in 0.0019 s
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events responded success in 0.0157 s
```

**개선 결과**:
- ✅ Publisher/Handler 관점 로깅 추가
- ✅ `application domain_event.{publisher|handler}` 형식으로 표준화
- ✅ 소요 시간 추적 가능
- 📝 Handler 내부 로그(`[DomainEvent]`)는 개발자 커스텀 로그로 유지

#### 1.3 에러 로그 가독성 저하

**현상**:
```
[09:30:30 ERR] application usecase.command TestErrorCommand.Handle responded failure in 0.0004 s with aggregate:TestErrors.TestErrorCommand.SystemFailure {"ErrorType": "ManyErrors", "ErrorCodeId": -2000000006, "Count": 2, "Errors": [...]}
```

**개선안**:
```
[09:30:30 ERR] application usecase.command TestErrorCommand responded failure in 0.0004 s
              └─ aggregate error (2): BusinessRuleViolation, SystemFailure
```

#### 1.4 Repository 로그 파라미터 부재

**현상**:
```
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName responded success in 0.0730 s
```

**문제**:
- 어떤 Name으로 조회했는지 알 수 없음
- 디버깅 시 입력값 추적 불가

**개선안**:
```
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName(TestProduct) responded success (false) in 0.0730 s
```

#### 1.5 Correlation ID 부재

**현상**:
- 동일 HTTP 요청의 로그들을 연결할 식별자 없음
- 동시 요청 시 로그 추적 어려움

**개선안**:
```
[10:56:16 INF] [3c536c3f] application usecase.command CreateProductCommand requesting
[10:56:16 INF] [3c536c3f] adapter repository InMemoryProductRepository.ExistsByName responded success
[10:56:16 INF] [3c536c3f] application domain_event.handler OnProductCreated responded success
```

### 콘솔 로그 개선안 종합

```
# Before
[10:56:16 INF] application usecase.command CreateProductCommand.Handle {"Name": "TestProduct", ...} requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.ExistsByName responded success in 0.0730 s
[10:56:16 INF] adapter repository InMemoryProductRepository.Create requesting
[10:56:16 INF] adapter repository InMemoryProductRepository.Create responded success in 0.0174 s
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events requesting
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle {...} requesting
[10:56:16 INF] [DomainEvent] Product created: 01KGGKDXN48CHR54KF22P4AM9G, Name: TestProduct, Price: 100000
[10:56:16 INF] application domain_event.handler OnProductCreated.Handle responded success in 0.0019 s
[10:56:16 INF] application domain_event.publisher Product.PublishEvents 1 events responded success in 0.0157 s
[10:56:16 INF] application usecase.command CreateProductCommand.Handle {...} responded success in 0.2487 s

# After (개선안)
[10:56:16 INF] [3c536c3f] usecase CreateProductCommand requesting (Name: TestProduct, Price: 100000)
[10:56:16 INF] [3c536c3f]   repository ExistsByName(TestProduct) → false in 0.0730s
[10:56:16 INF] [3c536c3f]   repository Create(01KGGKDX) → success in 0.0174s
[10:56:16 INF] [3c536c3f]   event.publisher Product.PublishEvents (1 events) in 0.0157s
[10:56:16 INF] [3c536c3f]     handler OnProductCreated → success in 0.0019s
[10:56:16 INF] [3c536c3f] usecase CreateProductCommand responded success in 0.2487s (ProductId: 01KGGKDX)
```

---

## Part 2: JSON 로그 관점

### 현재 JSON 로그 형식

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

**주요 특징**:
- `TraceId`, `SpanId`가 **최상위 필드**로 자동 포함 (Activity.Current에서 추출)
- `RequestId`, `RequestPath`, `ConnectionId`가 Properties에 자동 포함 (ASP.NET Core 통합)
- `EventId`로 로그 유형 구분 가능

### JSON 로그 문제점

#### 2.1 Request/Response 데이터가 단일 속성에 중첩

**현상**:
```json
{
  "Properties": {
    "Data": {
      "Value": {
        "ProductId": "01KGGKDX...",
        "Name": "TestProduct",
        "Price": 100000
      },
      "IsSucc": true,
      "IsFail": false,
      "$type": "Succ"
    }
  }
}
```

**문제**:
- 로그 분석 시스템에서 `ProductId` 필드로 직접 필터링 불가
- `Data.Value.ProductId`처럼 깊은 경로 탐색 필요
- 검색 인덱싱 비효율

**개선안**:
```json
{
  "Properties": {
    "Layer": "application",
    "Category": "usecase.command",
    "Handler": "CreateProductCommand",
    "Status": "success",
    "DurationMs": 248.7,
    "Request": {
      "Name": "TestProduct",
      "Price": 100000
    },
    "Response": {
      "ProductId": "01KGGKDXN48CHR54KF22P4AM9G"
    },
    "ProductId": "01KGGKDXN48CHR54KF22P4AM9G",
    "ProductName": "TestProduct"
  }
}
```

#### 2.2 ~~에러 정보 구조화 부족~~ (해결됨)

**현재 상태**: `ErrorsDestructuringPolicy`가 `OpenTelemetryBuilder.Build()`에서 자동 등록되어
JSON 로그에서 `@error` 필드가 구조화된 형태로 출력됩니다.

**실제 JSON 로그 출력 (Expected Error)**:
```json
{
  "@error": {
    "ErrorType": "ErrorCodeExpected",
    "ErrorCode": "TestErrors.TestErrorCommand.BusinessRuleViolation",
    "ErrorCodeId": -1000000001,
    "ErrorCurrentValue": "currentValue",
    "Message": "Business rule violated"
  }
}
```

**ManyErrors (Aggregate) 예시**:
```json
{
  "@error": {
    "ErrorType": "ManyErrors",
    "ErrorCodeId": -2000000006,
    "Count": 2,
    "Errors": [
      {
        "ErrorType": "ErrorCodeExpected",
        "ErrorCode": "TestErrors.BusinessRuleViolation",
        "ErrorCodeId": -1000000001,
        "ErrorCurrentValue": "value1",
        "Message": "First error"
      },
      {
        "ErrorType": "ErrorCodeExpected",
        "ErrorCode": "TestErrors.ValidationFailed",
        "ErrorCodeId": -1000000002,
        "ErrorCurrentValue": "value2",
        "Message": "Second error"
      }
    ]
  }
}
```

**Exceptional 에러 (예외 포함)**:
```json
{
  "@error": {
    "ErrorType": "Exceptional",
    "ErrorCodeId": -3000000001,
    "ExceptionDetails": {
      "Type": "System.InvalidOperationException",
      "Message": "Operation failed",
      "StackTrace": "..."
    }
  }
}
```

**관련 구현**:
- `OpenTelemetryBuilder.cs:298-299`: `logging.Destructure.With<ErrorsDestructuringPolicy>()`
- `Src/Functorium/Abstractions/Errors/DestructuringPolicies/` 폴더의 Destructurer들

#### 2.3 ~~DomainEvent 로그 구조화 부재~~ (부분적으로 해결됨)

**개선 전**:
```json
{
  "MessageTemplate": "[DomainEvent] Product created: {ProductId}, Name: {Name}, Price: {Price}",
  "Properties": {
    "ProductId": "01KGGKDX...",
    "Name": "TestProduct",
    "Price": 100000
  }
}
```

**개선 후** (현재 상태):
Publisher/Handler 관점 로그가 추가됨:

**DomainEvent Handler Request 로그**:
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
    "request.message": {
      "ProductId": { "Value": { ... } },
      "Name": { "_typeTag": "ProductName" },
      "Price": { "_typeTag": "Money" },
      "OccurredAt": "2026-02-03T02:55:00.8214756+00:00",
      "EventId": "f604c31b-7b2e-4a8c-94d1-65e1c088a0f0",
      "_typeTag": "CreatedEvent"
    },
    "EventId": { "Id": 3101, "Name": "domain_event_handler.request" },
    "SourceContext": "LayeredArch.Application.Usecases.Products.OnProductCreated"
  }
}
```

**DomainEvent Publisher 로그**:
```json
{
  "Timestamp": "2026-02-03T11:55:01.1033751+09:00",
  "Level": "Information",
  "MessageTemplate": "{request.layer} {request.category} {request.handler}.{request.handler.method} {event.count} events responded {response.status} in {response.elapsed:0.0000} s",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "request.layer": "application",
    "request.category": "domain_event.publisher",
    "request.handler": "Product",
    "request.handler.method": "PublishEvents",
    "event.count": 1,
    "response.status": "success",
    "response.elapsed": 0.013446,
    "EventId": { "Id": 3002, "Name": "domain_event.response.success" }
  }
}
```

**추가 개선 가능**:
- Handler 내부의 `[DomainEvent]` 로그는 개발자 커스텀 로그로 유지 (비즈니스 로직 확인용)

#### 2.4 ~~Trace Context 속성 부재~~ (해결됨)

**현재 상태**: Serilog가 `Activity.Current`에서 TraceId/SpanId를 자동으로 추출하여
JSON 로그의 **최상위 필드**로 출력합니다.

**실제 JSON 로그 출력**:
```json
{
  "Timestamp": "2026-02-03T11:55:00.7285957+09:00",
  "Level": "Information",
  "MessageTemplate": "...",
  "TraceId": "1997ee25f08ad7926f7805ef5230431c",
  "SpanId": "2246a9ab3f6aa320",
  "Properties": {
    "RequestId": "0HNJ2PUFC1KBF:00000001",
    "RequestPath": "/api/products",
    "ConnectionId": "0HNJ2PUFC1KBF",
    ...
  }
}
```

**포함되는 컨텍스트 정보**:
| 필드 | 위치 | 설명 |
|------|------|------|
| `TraceId` | 최상위 | OpenTelemetry 32자리 Trace ID |
| `SpanId` | 최상위 | OpenTelemetry 16자리 Span ID |
| `RequestId` | Properties | ASP.NET Core HTTP 요청 ID |
| `RequestPath` | Properties | HTTP 요청 경로 |
| `ConnectionId` | Properties | HTTP 연결 ID |

**관련 구현**:
- Serilog가 `Activity.Current`를 자동 감지하여 TraceId/SpanId 추출
- ASP.NET Core 통합으로 RequestId/RequestPath/ConnectionId 자동 포함

#### 2.5 타임스탬프 정밀도 및 형식

**현상**:
```json
{
  "@t": "2026-02-03T01:56:16.000Z"
}
```

**개선안**:
```json
{
  "@t": "2026-02-03T01:56:16.1658878Z",
  "TimestampUnixMs": 1770221776165
}
```

### JSON 로그 스키마 제안

```json
{
  "$schema": "LayeredArch Log Schema v1.1",

  "Timestamp": "2026-02-03T01:56:16.1658878Z",
  "TimestampUnixMs": 1770221776165,
  "Level": "Information",

  "Context": {
    "TraceId": "3c536c3f1234567890abcdef12345678",
    "SpanId": "abcdef1234567890",
    "RequestId": "req-12345",
    "UserId": "user-001",
    "TenantId": "tenant-001"
  },

  "Source": {
    "Layer": "application",
    "Category": "usecase",
    "Type": "command",
    "Handler": "CreateProductCommand",
    "Method": "Handle",
    "Assembly": "LayeredArch.Application"
  },

  "Operation": {
    "Status": "success",
    "DurationMs": 248.7,
    "Phase": "responded"
  },

  "Request": {
    "Name": "TestProduct",
    "Description": "A test product",
    "Price": 100000,
    "StockQuantity": 10
  },

  "Response": {
    "ProductId": "01KGGKDXN48CHR54KF22P4AM9G",
    "Name": "TestProduct",
    "CreatedAt": "2026-02-03T01:56:16.1658878Z"
  },

  "DomainEvents": [
    {
      "EventType": "ProductCreatedEvent",
      "EventId": "c61a25d3-d919-4601-8ad0-e03e0aa83140",
      "Handler": "OnProductCreated",
      "DurationMs": 1.9,
      "Status": "success"
    }
  ],

  "Error": null,

  "Tags": {
    "ProductId": "01KGGKDXN48CHR54KF22P4AM9G",
    "ProductName": "TestProduct"
  }
}
```

---

## Part 3: 구현 계획

### Phase 1: 긴급 수정 (버그) ✅ 완료

| 항목 | 설명 | 상태 |
|------|------|------|
| DomainEvent 중복 로그 | `RegisterDomainEventHandlersFromAssembly()` 중복 호출 제거 | ✅ 완료 |
| DomainEvent Publisher 관찰 가능성 | `RegisterDomainEventPublisher(enableObservability: true)` | ✅ 완료 |
| DomainEvent Handler 관찰 가능성 | `NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher)` | ✅ 완료 |

### Phase 2: 콘솔 로그 개선

| 순서 | 항목 | 수정 대상 | 난이도 | 상태 |
|------|------|----------|--------|------|
| 2.1 | 로그 메시지 간소화 | `UsecaseLoggerExtensions.cs` | 중 | 대기 |
| 2.2 | ~~DomainEvent 로그 표준화~~ | `DomainEventHandlerLoggerExtensions.cs` | 중 | ✅ 완료 |
| 2.3 | Repository 로그 파라미터 추가 | `RepositoryLoggerExtensions.cs` | 낮음 | 대기 |
| 2.4 | Correlation ID 추가 | Serilog 템플릿 수정 | 낮음 | 대기 |
| 2.5 | 에러 로그 포맷 개선 | `UsecaseLoggerExtensions.cs` | 중 | 대기 |

### Phase 3: JSON 로그 개선

| 순서 | 항목 | 수정 대상 | 난이도 | 상태 |
|------|------|----------|--------|------|
| 3.1 | Request/Response 평탄화 | 로그 확장 메서드 전체 | 높음 | 대기 |
| 3.2 | ~~에러 정보 구조화~~ | `ErrorsDestructuringPolicy` | 중 | ✅ 완료 |
| 3.3 | ~~DomainEvent 구조화~~ | `DomainEventLogEnricher.cs` | 중 | ✅ 부분 완료 |
| 3.4 | ~~Trace Context Enricher~~ | Serilog 자동 감지 (Activity.Current) | 낮음 | ✅ 완료 |
| 3.5 | 주요 엔티티 ID 태깅 | `EntityTaggingEnricher.cs` 신규 | 중 | 대기 |

### Phase 4: 인프라 구성

| 순서 | 항목 | 설명 | 상태 |
|------|------|------|------|
| 4.1 | 환경별 로그 설정 분리 | Development: 콘솔 상세, Production: JSON 간소 | 대기 |
| 4.2 | 로그 레벨별 상세도 조절 | Debug: 전체 데이터, Info: 요약만 | 대기 |
| 4.3 | 로그 싱크 분리 | 콘솔: 사람용, 파일/ELK: JSON | 대기 |

---

## Part 4: 설정 예시

### Serilog 설정 (appsettings.json)

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId:l8}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.json",
          "rollingInterval": "Day",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithSpan"
    ]
  }
}
```

### 개발 환경 콘솔 템플릿

```
[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId:l8}] {SourceContext:l20} {Message:lj}{NewLine}{Exception}
```

### 프로덕션 JSON 로그 예시

```json
{"@t":"2026-02-03T01:56:16.165Z","@l":"Information","@mt":"{Layer} {Category} {Handler} {Status}","Layer":"application","Category":"usecase.command","Handler":"CreateProductCommand","Status":"success","DurationMs":248.7,"TraceId":"3c536c3f","ProductId":"01KGGKDX"}
```

---

## Part 5: 기대 효과

### 콘솔 로그 개선 효과

| Before | After |
|--------|-------|
| 한 줄 200자+ | 한 줄 80자 이내 |
| JSON 전체 노출 | 핵심 정보만 표시 |
| ~~비표준 DomainEvent~~ | ✅ 통일된 형식 (완료) |
| 요청 추적 불가 | Correlation ID로 추적 |

### JSON 로그 개선 효과

| 분석 작업 | Before | After |
|-----------|--------|-------|
| ProductId로 검색 | `Data.Value.ProductId:*` | `ProductId:*` |
| 에러 코드 집계 | 불가 (문자열 파싱) | `ErrorCode` 필드로 집계 |
| ~~DomainEvent 필터~~ | ✅ `Layer:application AND Category:domain_event.*` |
| 분산 추적 연동 | 수동 매칭 | TraceId로 자동 연결 |

---

## 결론

### 현재 상태 (2026-02-03)

LayeredArch의 로그 시스템은 다음이 **완료**되었습니다:
- ✅ DomainEvent 중복 로그 수정
- ✅ DomainEvent Publisher 관점 관찰 가능성 (`domain_event.publisher`)
- ✅ DomainEvent Handler 관점 관찰 가능성 (`domain_event.handler`)

### 우선 적용 권장 (다음 단계)

1. 콘솔 로그 메시지 간소화
2. JSON 로그 Request/Response 평탄화
3. Trace Context Enricher 추가
4. Repository 로그 파라미터 추가

### 장기 목표

- **콘솔 로그**: 개발자가 한눈에 흐름을 파악할 수 있는 간결한 형식
- **JSON 로그**: 로그 분석 시스템에서 즉시 검색/집계 가능한 구조화된 형식
