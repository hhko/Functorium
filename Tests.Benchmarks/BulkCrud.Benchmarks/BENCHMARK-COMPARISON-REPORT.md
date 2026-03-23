# 벤치마크 비교 보고서: 벌크 도메인 이벤트 DDD 재설계

> 측정 환경: Intel Core i9-9900K 3.60GHz, .NET 10.0.4, x64 RyuJIT x86-64-v3, BenchmarkDotNet ShortRunJob
> 측정 일시: 2026-03-23
> 변경 사유: 에릭 에반스 DDD 관점 근본 재설계 + 개별 핸들러 미호출 버그 수정

---

## 1. 변경 요약

| 항목 | 기존 (Bulk) | 개선 (Individual) |
|------|------------|------------------|
| **발행 방식** | GroupBy → `BulkDomainEvent` 래핑 → 타입당 1회 Publish | 이벤트마다 개별 Publish |
| **IDomainEventHandler\<T\>** | 호출 안 됨 (버그) | 항상 호출됨 |
| **벌크 이벤트** | BulkDomainEvent, BulkDeletedEvent, IBulkEventInfo | Domain Service + TrackEvent 패턴 |
| **Domain 계층** | BulkDomainEvent, BulkDeletedEvent, IBulkEventInfo 포함 | 순수 도메인 타입만 (DDD 준수) |

---

## 2. NoOp Publisher 기준 파이프라인 비용 비교

> Observability 오버헤드가 없는 순수 파이프라인 비용. Mediator.Publish → NoOp 핸들러.

### 100 이벤트 (일반 트랜잭션 규모)

| 방식 | Mean (μs) | Ratio | Allocated |
|------|----------:|------:|----------:|
| **Old: 1 Publish/Type (Bulk)** | 5.54 | 1.00 | 5.65 KB |
| **New: N Publish/Event (Individual)** | 5.94 | 1.07 | 5.68 KB |
| New: Full Pipeline | 5.15 | 0.93 | 10.67 KB |

**→ 100개 규모: 차이 없음 (±7%)**

### 1,000 이벤트 (벌크 CRUD 규모)

| 방식 | Mean (μs) | Ratio | Allocated |
|------|----------:|------:|----------:|
| **Old: 1 Publish/Type (Bulk)** | 56.21 | 1.00 | 47.84 KB |
| **New: N Publish/Event (Individual)** | 57.63 | 1.03 | 47.88 KB |
| New: Full Pipeline | 45.40 | 0.81 | 96.00 KB |

**→ 1K 규모: 차이 없음 (±6%), Full Pipeline은 오히려 19% 빠름**

### 10,000 이벤트 (대량 벌크 규모)

| 방식 | Mean (μs) | Ratio | Allocated |
|------|----------:|------:|----------:|
| **Old: 1 Publish/Type (Bulk)** | 973.99 | 1.00 | 569.22 KB |
| **New: N Publish/Event (Individual)** | 1,005.93 | 1.04 | 569.29 KB |
| New: Full Pipeline | 1,151.41 | 1.19 | 892.99 KB |

**→ 10K 규모: 개별 발행은 ±4% (사실상 동일), Full Pipeline은 +19% (메모리 할당 차이)**

---

## 3. 핵심 분석

### NoOp 기준 발행 비용은 사실상 동일

```
기존 Bulk (10K):   ~974 μs
개선 Individual (10K): ~1,006 μs  (+3.3%)
```

**이유:** NoOp Publisher에서 `await _publisher.Publish(event)`의 비용은 `ValueTask.CompletedTask` 반환뿐이다.
실제 차이를 만드는 것은 **Observability 오버헤드(Activity span, Counter, Histogram, 로그)**이며,
이는 Mediator의 `INotificationPublisher` 수준에서 발생한다.

### Observability 포함 시 예상 비용 비교

| 오버헤드 구성요소 | Old Bulk (10K) | New Individual (10K) | 비고 |
|------------------|---------------:|---------------------:|------|
| Mediator dispatch | 1회 × ~10μs = **10μs** | 10,000회 × ~10μs = **100ms** | 10,000x 증가 |
| Activity span 생성 | 1개 × ~5μs = **5μs** | 10,000개 × ~5μs = **50ms** | 10,000x 증가 |
| Counter.Add | 2회 = **~0μs** | 20,000회 = **~60ms** | |
| Histogram.Record | 1회 = **~0μs** | 10,000회 = **~30ms** | |
| 로그 출력 | 2건 = **~10μs** | 20,000건 = **~100ms** | |
| **총 Observability 추정** | **<1ms** | **~340ms** | |

### 결론: 성능 트레이드오프

| 규모 | NoOp 차이 | Observability 포함 추정 차이 | DDD 정합성 |
|------|----------|---------------------------|-----------|
| 1~10개 (일반 트랜잭션) | 무시 가능 | 무시 가능 | ✅ 개선 |
| 100개 | ±7% | ~3ms vs <1ms | ✅ 개선 |
| 1,000개 | ±6% | ~34ms vs <1ms | ✅ 개선 |
| 10,000개 | ±4% | **~340ms vs <1ms** | ✅ 개선 |

---

## 4. 성능 최적화 전략

### 일반 트랜잭션 (1~100개 이벤트)
- **개선된 방식 그대로 사용**: Observability 포함해도 수 밀리초 수준
- 개별 핸들러가 정확한 타입으로 호출됨 ✅
- LogEnricher가 정확한 이벤트 타입으로 동작 ✅

### 대량 처리 (1,000개 이상)
Observability 오버헤드가 우려되는 경우 다음 전략 적용 가능:

1. **Domain Service 패턴 사용**: 벌크 연산을 Domain Service에서 처리하고 `TrackEvent`로 이벤트 직접 등록
2. **Observability 샘플링**: OpenTelemetry의 Sampler를 사용하여 대량 이벤트 시 Activity 생성 비율 조절
3. **이벤트 핸들러 경량화**: 대량 이벤트의 핸들러를 최소한의 로직만 포함하도록 설계

---

## 5. DDD 관점 개선 효과

| 항목 | Before | After |
|------|--------|-------|
| `request.event.type` 정확성 | `"BulkDomainEvent"` ❌ | `"CreatedEvent"` ✅ |
| `request.event.id` 정확성 | 래퍼 EventId ❌ | 실제 EventId ✅ |
| IDomainEventHandler 호출 | 호출 안 됨 ❌ | 항상 호출 ✅ |
| IDomainEventLogEnricher 동작 | BulkDomainEvent 타입으로 resolve ❌ | 실제 이벤트 타입으로 resolve ✅ |
| Domain 계층 순수성 | BulkDomainEvent, BulkDeletedEvent, IBulkEventInfo 혼재 | 순수 도메인 타입만 ✅ |
| 핸들러 Activity Span | N이벤트 → 1 span | N이벤트 → N span ✅ |

---

## 6. 벤치마크 원본 데이터

```
BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5737/22H2/2022Update)
.NET SDK 10.0.104
  [Host]   : .NET 10.0.4, X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.4, X64 RyuJIT x86-64-v3

| Method                                             | Count | Mean         | Ratio | Allocated | Alloc Ratio |
|--------------------------------------------------- |------ |-------------:|------:|----------:|------------:|
| 'Old: 1 Publish per EventType (Bulk)'              | 100   |     5.538 us |  1.00 |   5.65 KB |        1.00 |
| 'New: N Publish per Event (Individual)'            | 100   |     5.940 us |  1.07 |   5.68 KB |        1.01 |
| 'New: BatchHandler + N Publish (Individual+Batch)' | 100   |     5.778 us |  1.04 |   6.48 KB |        1.15 |
| 'Baseline: N Publish (No GroupBy)'                 | 100   |     2.502 us |  0.45 |   6.25 KB |        1.11 |
| 'New: Full PublishTrackedEvents Pipeline'          | 100   |     5.148 us |  0.93 |  10.67 KB |        1.89 |
|                                                    |       |              |       |           |             |
| 'Old: 1 Publish per EventType (Bulk)'              | 1000  |    56.205 us |  1.00 |  47.84 KB |        1.00 |
| 'New: N Publish per Event (Individual)'            | 1000  |    57.633 us |  1.03 |  47.88 KB |        1.00 |
| 'New: BatchHandler + N Publish (Individual+Batch)' | 1000  |    59.206 us |  1.06 |  55.71 KB |        1.16 |
| 'Baseline: N Publish (No GroupBy)'                 | 1000  |    25.940 us |  0.46 |  62.5  KB |        1.31 |
| 'New: Full PublishTrackedEvents Pipeline'          | 1000  |    45.403 us |  0.81 |  96.0  KB |        2.01 |
|                                                    |       |              |       |           |             |
| 'Old: 1 Publish per EventType (Bulk)'              | 10000 |   973.985 us |  1.00 | 569.22 KB |        1.00 |
| 'New: N Publish per Event (Individual)'            | 10000 | 1,005.928 us |  1.04 | 569.29 KB |        1.00 |
| 'New: BatchHandler + N Publish (Individual+Batch)' | 10000 |   863.484 us |  0.89 | 647.45 KB |        1.14 |
| 'Baseline: N Publish (No GroupBy)'                 | 10000 |   360.528 us |  0.37 |  625.0 KB |        1.10 |
| 'New: Full PublishTrackedEvents Pipeline'          | 10000 | 1,151.410 us |  1.19 | 892.99 KB |        1.57 |
```
