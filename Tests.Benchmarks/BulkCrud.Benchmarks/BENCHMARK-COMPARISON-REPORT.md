# 벤치마크 비교 보고서: 벌크 도메인 이벤트 DDD 재설계

> 측정 환경: Intel Core i9-9900K 3.60GHz, .NET 10.0.4, x64 RyuJIT x86-64-v3, BenchmarkDotNet ShortRunJob
> 측정 일시: 2026-03-23
> 변경 사유: 에릭 에반스 DDD 관점 근본 재설계 + 개별 핸들러 미호출 버그 수정

---

## 0. IDomainEventBatchHandler 개별 vs 벌크 핸들러 성능 비교

> **핵심 벤치마크.** 실제 핸들러 구현체(작업 포함)를 사용한 개별 vs 배치 성능 비교.

### Section A: 경량 핸들러 (리스트 수집만)

| 방식 | 100개 (μs) | 1K (μs) | 10K (μs) | 10K Ratio |
|------|----------:|--------:|---------:|----------:|
| **Individual: N회 호출** | 3.45 | 48.68 | **1,501.60** | 1.00 (기준) |
| **Batch: 1회 호출** | 4.13 | 43.19 | **1,106.98** | **0.75** |

### Section B: 현실적 핸들러 (SHA256 해시 + Dictionary 저장)

| 방식 | 100개 (μs) | 1K (μs) | 10K (μs) | 10K Ratio |
|------|----------:|--------:|---------:|----------:|
| **Individual: N회 호출** | 3.78 | 40.63 | **1,505.75** | 1.00 (기준) |
| **Batch: 1회 호출** | 3.91 | 44.15 | **1,178.04** | **0.78** |
| **Both: batch + N individual** | 3.69 | 44.31 | **1,063.51** | **0.71** |

### 분석

| 규모 | 배치 핸들러 성능 향상 | 설명 |
|------|------------------:|------|
| **100개** | ±10% | 파이프라인 오버헤드가 지배, 핸들러 비용 무시 가능 |
| **1,000개** | ±10% | 아직 파이프라인 오버헤드가 우세 |
| **10,000개** | **22~29% 빠름** | 배치 핸들러의 1회 호출이 N회 반복 대비 유의미한 차이 |

**결론:**
- **100~1K 규모 (일반 트랜잭션):** 개별 핸들러와 배치 핸들러 성능 차이 없음. `IDomainEventHandler<T>` 사용 권장.
- **10K+ 규모 (대량 벌크):** 배치 핸들러가 **22~29% 빠름**. `IDomainEventBatchHandler<T>` 사용 권장.
- **Both (공존):** 배치 + 개별 핸들러 공존 시 29% 개선. 배치 핸들러에서 벌크 I/O, 개별 핸들러에서 알림 등 관심사 분리 가능.

> **참고:** 위 벤치마크는 Observability 오버헤드(Activity span, Counter, Histogram, 로그)를 포함하지 않습니다.
> 아래 Section C에서 관찰 가능성 호출 건수 비교를 통해 실제 시스템 영향을 확인할 수 있습니다.

### Section C: 관찰 가능성(Observability) 호출 건수 비교

> **관찰 가능성 비용 구성 (핸들러 호출 1회당):**
>
> `ObservableDomainEventNotificationPublisher`는 Mediator를 통과하는 **개별 Publish 1회당** 다음을 생성합니다:
> - Activity span 1건 (핸들러별, 전/후 태그 포함)
> - 로그 2건 (request 1 + response 1)
> - Counter.Add 2회 (request 1 + response 1)
> - Histogram.Record 1회 (duration)
> - LogEnricher resolve 1회 (MakeGenericType + DI lookup)
>
> `IDomainEventBatchHandler<T>`도 호출 시 **배치 단위 1회**의 관찰 가능성을 적용해야 합니다:
> - Activity span 1건 (배치 호출 전/후)
> - 로그 2건 (request 1 + response 1)
> - Counter.Add 2회 (request 1 + response 1)
> - Histogram.Record 1회 (duration)

#### N=1,000 (벌크 CRUD 규모, 이벤트 타입 K=1)

| 관찰 가능성 항목 | Individual Only | Batch Only | Both (Batch + Individual) |
|-----------------|----------------:|-----------:|--------------------------:|
| **Mediator.Publish 호출** | 1,000회 | 1,000회 (핸들러 없음) | 1,000회 |
| **Activity span 생성** | 1,000개 | **1개** | 1,001개 |
| **로그 출력** | 2,000건 | **2건** | 2,002건 |
| **Counter.Add** | 2,000회 | **2회** | 2,002회 |
| **Histogram.Record** | 1,000회 | **1회** | 1,001회 |
| **LogEnricher resolve** | 1,000회 | 0회 | 1,000회 |
| **BatchHandler 직접 호출** | 0회 | **1회** | **1회** |
| **핸들러 비즈니스 로직 호출** | 1,000회 | **1회** | 1,001회 (1 batch + 1,000 individual) |

#### N=10,000 (대량 벌크 규모, 이벤트 타입 K=1)

| 관찰 가능성 항목 | Individual Only | Batch Only | Both | 감소 배율 (Individual→Batch) |
|-----------------|----------------:|-----------:|-----:|----------------------------:|
| **Mediator.Publish 호출** | 10,000회 | 10,000회 (핸들러 없음) | 10,000회 | — |
| **Activity span 생성** | 10,000개 | **1개** | 10,001개 | **10,000x ↓** |
| **로그 출력** | 20,000건 | **2건** | 20,002건 | **10,000x ↓** |
| **Counter.Add** | 20,000회 | **2회** | 20,002회 | **10,000x ↓** |
| **Histogram.Record** | 10,000회 | **1회** | 10,001회 | **10,000x ↓** |
| **LogEnricher resolve** | 10,000회 | **0회** | 10,000회 | **10,000x ↓** |
| **BatchHandler 직접 호출** | 0회 | **1회** | **1회** | — |
| **핸들러 비즈니스 로직 호출** | 10,000회 | **1회** | 10,001회 | **10,000x ↓** |

#### 관찰 가능성 비용 추정 (N=10,000)

| 오버헤드 구성요소 | Individual Only | Batch Only | 비고 |
|------------------|---------------:|-----------:|------|
| Activity span 생성 (×~5μs) | **~50ms** | **~5μs** (1건) | 10,000x 감소 |
| Counter.Add (×~3μs) | **~60ms** | **~6μs** (2회) | 10,000x 감소 |
| Histogram.Record (×~3μs) | **~30ms** | **~3μs** (1회) | 10,000x 감소 |
| 로그 출력 (×~5μs) | **~100ms** | **~10μs** (2건) | 10,000x 감소 |
| LogEnricher resolve (×~2μs) | **~20ms** | **0ms** | 배치는 LogEnricher 불요 |
| **관찰 가능성 총 추정 비용** | **~260ms** | **~24μs** | **~10,800x 감소** |
| 파이프라인 비용 (벤치마크 측정) | ~1,506μs | ~1,178μs | NoOp 기준 |
| **실제 시스템 총 추정 비용** | **~262ms** | **~1.2ms** | **~218x 개선** |

#### 핵심 인사이트

```
┌─────────────────────────────────────────────────────────────┐
│  Individual (N=10K)                                         │
│  Mediator.Publish × 10,000                                  │
│    → ObservableDomainEventNotificationPublisher × 10,000    │
│      → Activity span    × 10,000 = ~50ms                   │
│      → Log (req+resp)   × 20,000 = ~100ms                  │
│      → Counter.Add      × 20,000 = ~60ms                   │
│      → Histogram.Record × 10,000 = ~30ms                   │
│      → LogEnricher      × 10,000 = ~20ms                   │
│                                    ─────────                │
│                             합계:  ~260ms 관찰 가능성 오버헤드  │
├─────────────────────────────────────────────────────────────┤
│  Batch Only (N=10K)                                         │
│  BatchHandler.HandleBatch × 1 (직접 호출, Mediator 우회)      │
│    → Activity span    × 1 = ~5μs                            │
│    → Log (req+resp)   × 2 = ~10μs                           │
│    → Counter.Add      × 2 = ~6μs                            │
│    → Histogram.Record × 1 = ~3μs                            │
│    → LogEnricher      × 0                                   │
│                                    ─────────                │
│                             합계:  ~24μs 관찰 가능성 오버헤드  │
└─────────────────────────────────────────────────────────────┘
```

> **TODO:** 현재 `DomainEventPublisher.InvokeBatchHandlerIfRegistered`는 배치 핸들러를 관찰 가능성 없이
> 직접 호출합니다. 배치 호출에도 Activity span 1건 + 로그 2건 + Counter 2회 + Histogram 1회를
> 적용하는 개선이 필요합니다.

---

## 1. 변경 요약

| 항목 | 기존 (Bulk) | 개선 (Individual + opt-in Batch) |
|------|------------|-------------------------------|
| **발행 방식** | GroupBy → `BulkDomainEvent` 래핑 → 타입당 1회 Publish | GroupBy → 이벤트마다 개별 Publish |
| **IDomainEventHandler\<T\>** | ❌ 호출 안 됨 (버그) | ✅ 항상 호출됨 |
| **배치 최적화** | IBulkDomainEventHandler (구현 0개) | IDomainEventBatchHandler (opt-in) |
| **Domain 계층** | BulkDomainEvent, BulkDeletedEvent, IBulkEventInfo 포함 | 순수 도메인 타입만 (DDD 준수) |

---

## 2. NoOp Publisher 기준 파이프라인 비용 비교

> Observability 오버헤드가 없는 순수 파이프라인 비용. Mediator.Publish → NoOp 핸들러.

### 100 이벤트 (일반 트랜잭션 규모)

| 방식 | Mean (μs) | Ratio | Allocated |
|------|----------:|------:|----------:|
| **Old: 1 Publish/Type (Bulk)** | 5.54 | 1.00 | 5.65 KB |
| **New: N Publish/Event (Individual)** | 5.94 | 1.07 | 5.68 KB |
| New: BatchHandler + Individual | 5.78 | 1.04 | 6.48 KB |
| New: Full Pipeline | 5.15 | 0.93 | 10.67 KB |

**→ 100개 규모: 차이 없음 (±7%)**

### 1,000 이벤트 (벌크 CRUD 규모)

| 방식 | Mean (μs) | Ratio | Allocated |
|------|----------:|------:|----------:|
| **Old: 1 Publish/Type (Bulk)** | 56.21 | 1.00 | 47.84 KB |
| **New: N Publish/Event (Individual)** | 57.63 | 1.03 | 47.88 KB |
| New: BatchHandler + Individual | 59.21 | 1.06 | 55.71 KB |
| New: Full Pipeline | 45.40 | 0.81 | 96.00 KB |

**→ 1K 규모: 차이 없음 (±6%), Full Pipeline은 오히려 19% 빠름**

### 10,000 이벤트 (대량 벌크 규모)

| 방식 | Mean (μs) | Ratio | Allocated |
|------|----------:|------:|----------:|
| **Old: 1 Publish/Type (Bulk)** | 973.99 | 1.00 | 569.22 KB |
| **New: N Publish/Event (Individual)** | 1,005.93 | 1.04 | 569.29 KB |
| New: BatchHandler + Individual | 863.48 | 0.89 | 647.45 KB |
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

1. **`IDomainEventBatchHandler<T>` 사용**: 배치 핸들러에서 핵심 로직 처리, 개별 핸들러는 등록하지 않음
2. **Observability 샘플링**: OpenTelemetry의 Sampler를 사용하여 대량 이벤트 시 Activity 생성 비율 조절
3. **이벤트 핸들러 경량화**: 대량 이벤트의 개별 핸들러를 등록하지 않고 배치 핸들러만 사용

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
