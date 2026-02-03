# LayeredArch 로그 개선 방향

이 문서는 현재 LayeredArch 프로젝트의 로그 출력을 분석하고 개선 방향을 제안합니다.

## 현재 로그 분석

### 강점

1. **계층별 명확한 구분**: `application`, `adapter`, `domain` 레이어가 로그에서 명확히 구분됨
2. **CQRS 구분**: Command와 Query가 `usecase.command`, `usecase.query`로 구분됨
3. **에러 분류 체계**: Expected/Exceptional 에러를 로그 레벨(WRN/ERR)로 구분
4. **구조화된 로그**: JSON 형식의 Request/Response 데이터 포함
5. **실행 시간 측정**: 각 요청의 처리 시간이 기록됨

### 개선이 필요한 영역

#### 1. DomainEvent 중복 로그

**현상**:
```
[09:29:38 INF] [DomainEvent] Product created: 01KGGEFA2EA5765TP35Q4C8W1Y, Name: 노트북, Price: 1500000
[09:29:38 INF] [DomainEvent] Product created: 01KGGEFA2EA5765TP35Q4C8W1Y, Name: 노트북, Price: 1500000
```

**분석**:
- 동일한 DomainEvent가 2회 출력되는 것으로 보아, Handler가 2번 호출되거나 로깅이 2번 발생하는 것으로 추정
- `OnProductCreated`, `OnProductUpdated` Handler 구현 확인 필요

**개선 방향**:
- DomainEvent Handler 등록 중복 확인
- MediatorGenerator의 Handler 등록 로직 검토
- 또는 의도된 동작이라면 로그에 구분자 추가 (예: Handler 이름)

---

#### 2. DomainEvent 로그 표준화 부재

**현상**:
```
[09:29:38 INF] [DomainEvent] Product created: 01KGGEFA2EA5765TP35Q4C8W1Y, Name: 노트북, Price: 1500000
```
vs
```
[09:29:38 INF] adapter repository InMemoryProductRepository.Create requesting
```

**분석**:
- Application/Adapter 계층은 표준화된 로그 포맷 사용
- DomainEvent는 `[DomainEvent]` 태그만 있고 비표준 포맷

**개선 방향**:
```
// 현재
[DomainEvent] Product created: ...

// 개선안
domain event.publish OnProductCreated.Handle {"ProductId": "...", "Name": "..."} requesting
domain event.publish OnProductCreated.Handle responded success in 0.0001 s
```

---

#### 3. 로그 메시지 길이 문제

**현상**:
```
[09:29:38 INF] application usecase.command CreateProductCommand.Handle {"Value": {"ProductId": "01KGGEFA2EA5765TP35Q4C8W1Y", "Name": "노트북", "Description": "고성능 개발용 노트북", "Price": 1500000, "StockQuantity": 10, "CreatedAt": "2026-02-03T00:29:38.7676716Z", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded success in 0.1750 s
```

**분석**:
- Response 로그에 전체 JSON이 포함되어 로그가 매우 길어짐
- 콘솔 가독성 저하
- 로그 저장소 용량 증가

**개선 방향**:
- Request/Response 데이터를 별도 속성(structured logging)으로 분리
- 콘솔에는 요약만 출력, 상세는 OpenTelemetry로 전송
- 또는 로그 레벨에 따른 상세도 조절

```
// 개선안 1: 요약 로그
[09:29:38 INF] application usecase.command CreateProductCommand.Handle responded success in 0.1750 s (ProductId: 01KGGEFA2EA5765TP35Q4C8W1Y)

// 개선안 2: 구조화된 속성 활용
Log.Information("CreateProductCommand.Handle responded {Status} in {Elapsed:F4} s",
    "success", 0.1750,
    new { Response.ProductId, Response.Name });
```

---

#### 4. Repository 로그의 응답 데이터 부재

**현상**:
```
[09:29:38 INF] adapter repository InMemoryProductRepository.ExistsByName requesting
[09:29:38 INF] adapter repository InMemoryProductRepository.ExistsByName responded success in 0.0555 s
```

**분석**:
- Repository 요청에서 입력 파라미터가 없음
- Repository 응답에서 결과 값(예: `exists: true/false`)이 없음

**개선 방향**:
```
// 개선안
[09:29:38 INF] adapter repository InMemoryProductRepository.ExistsByName {"Name": "노트북"} requesting
[09:29:38 INF] adapter repository InMemoryProductRepository.ExistsByName responded success (false) in 0.0555 s
```

---

#### 5. 에러 로그의 복잡한 JSON 구조

**현상**:
```
[09:30:30 ERR] application usecase.command TestErrorCommand.Handle responded failure in 0.0004 s with aggregate:TestErrors.TestErrorCommand.SystemFailure {"ErrorType": "ManyErrors", "ErrorCodeId": -2000000006, "Count": 2, "Errors": [{"ErrorType": "ErrorCodeExpected", ...}, {"ErrorType": "ErrorCodeExceptional", "ExceptionDetails": {...}}]}
```

**분석**:
- 에러 정보가 단일 행에 모두 포함되어 가독성 저하
- 중첩된 JSON 구조로 디버깅 어려움

**개선 방향**:
```
// 개선안: 에러 요약 + 상세 분리
[09:30:30 ERR] application usecase.command TestErrorCommand.Handle responded failure in 0.0004 s
              └─ aggregate error (2 errors): [BusinessRuleViolation, SystemFailure]
              └─ details: {"ErrorType": "ManyErrors", ...}
```

---

#### 6. 상관관계(Correlation) 정보 부재

**현상**:
- 동일 요청에서 발생한 로그들을 연결할 TraceId/SpanId가 로그 메시지에 없음
- OpenTelemetry Activity 정보는 별도로 출력됨

**개선 방향**:
- 로그 템플릿에 TraceId 포함
```
// 개선안
[09:29:38 INF] [3c536c3f] application usecase.command CreateProductCommand.Handle requesting
```

---

## 개선 우선순위

| 순위 | 항목 | 난이도 | 영향도 | 비고 |
|------|------|--------|--------|------|
| 1 | DomainEvent 중복 로그 수정 | 중 | 높음 | 버그 수정 필요 |
| 2 | DomainEvent 로그 표준화 | 중 | 중 | 일관성 향상 |
| 3 | 로그 메시지 길이 최적화 | 중 | 중 | 가독성 향상 |
| 4 | Repository 로그 파라미터 추가 | 낮음 | 낮음 | 디버깅 편의성 |
| 5 | 에러 로그 가독성 개선 | 중 | 중 | 디버깅 편의성 |
| 6 | Correlation ID 추가 | 낮음 | 중 | 분산 추적 개선 |

---

## 구현 제안

### 1. DomainEvent 중복 로그 수정

**조사 대상 파일**:
- `LayeredArch.Application/Usecases/Products/OnProductCreated.cs`
- `LayeredArch.Application/Usecases/Products/OnProductUpdated.cs`
- `Functorium/Adapters/Events/ObservableDomainEventPublisher.cs`

**확인 사항**:
- Handler가 중복 등록되어 있는지
- MediatorGenerator가 Handler를 2번 생성하는지
- DomainEventPublisher에서 Handler를 2번 호출하는지

### 2. DomainEvent 로그 표준화

**구현 위치**:
- `Functorium/Adapters/Events/` 또는 새로운 Pipeline 추가

**구현 방식**:
```csharp
// DomainEventLoggingPipeline 추가 또는
// ObservableDomainEventPublisher에서 표준 로그 출력

public class DomainEventLoggingDecorator<TEvent> : IDomainEventHandler<TEvent>
{
    public async Task Handle(TEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("domain event.handler {HandlerName}.Handle {Event} requesting",
            typeof(THandler).Name, @event);

        await _inner.Handle(@event, ct);

        _logger.LogInformation("domain event.handler {HandlerName}.Handle responded success in {Elapsed:F4} s",
            typeof(THandler).Name, elapsed);
    }
}
```

### 3. 로그 출력 레벨 기반 상세도 조절

**구현 위치**:
- `UsecaseLoggingPipeline.cs`
- `UsecaseLoggerExtensions.cs`

**구현 방식**:
```csharp
// Serilog 설정에서 로그 레벨별 템플릿 분리
if (_logger.IsEnabled(LogLevel.Debug))
{
    _logger.LogDebug("Full response: {Response}", JsonSerializer.Serialize(response));
}
_logger.LogInformation("{Layer} {Category}.{Handler} responded {Status} in {Elapsed:F4} s (Id: {Id})",
    layer, category, handler, status, elapsed, ExtractId(response));
```

---

## 결론

현재 LayeredArch의 로깅 시스템은 계층 구조와 에러 분류가 잘 되어 있으나, DomainEvent 중복 로그 문제와 로그 메시지 가독성 개선이 필요합니다. 특히 DomainEvent 중복 로그는 잠재적인 버그일 가능성이 있어 우선 조사가 필요합니다.

장기적으로는 OpenTelemetry와의 통합을 강화하여 콘솔 로그는 요약 정보만 출력하고, 상세 정보는 분산 추적 시스템(Jaeger, Zipkin 등)에서 확인하는 방식을 권장합니다.
