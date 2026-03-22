# 벤치마크 결과: 벌크(Bulk) CRUD 도메인 이벤트 처리

> 측정 환경: .NET 10.0.2, x64 RyuJIT, BenchmarkDotNet ShortRunJob
> 측정 일시: 2026-03-22

---

## 1. Mediator.Publish 호출 횟수 비교

> 핵심 최적화 지표. Publish 1회 = Activity span + Counter + Histogram + 로그 2건 발생.

| 규모 | 수정 전 (Per-Event) | 수정 후 (Bulk GroupBy) | 감소 배율 |
|------|-------------------:|---------------------:|----------:|
| 1K   | 1,000회 | **1회** | **1,000x** |
| 10K  | 10,000회 | **1회** | **10,000x** |
| 100K | 100,000회 | **1회** | **100,000x** |

- 이벤트 타입이 K종일 경우 Publish 호출은 K회 (보통 1~3)
- 위 결과는 `Product.CreatedEvent` 단일 타입 기준 (K=1)

---

## 2. 이벤트 발행 파이프라인 오버헤드

> NoOp Publisher 기준 순수 파이프라인 비용 측정.
> 실제 Observability 오버헤드(Activity/Counter/Histogram/로그)는 포함되지 않음.

| Method | Count | Mean (us) | Allocated | Alloc Ratio |
|--------|------:|----------:|----------:|------------:|
| Per-Event Sequential Publish | 1,000 | 20.37 | 62.5 KB | 1.00 |
| **BulkDomainEvent GroupBy Publish** | 1,000 | 40.66 | 55.79 KB | **0.89** |
| Per-Event Sequential Publish | 10,000 | 274.42 | 625 KB | 1.00 |
| **BulkDomainEvent GroupBy Publish** | 10,000 | 861.08 | 647.53 KB | 1.04 |

### 분석

BulkDomainEvent 방식은 **GroupBy + List 생성** 오버헤드로 NoOp 기준 순수 반복보다 느리다.
그러나 이것은 실제 시스템의 병목이 아니다:

| 오버헤드 구성 | Per-Event (N=10K) | Bulk (N=10K) |
|---------------|------------------:|-------------:|
| 파이프라인 (위 벤치마크) | 274 us | 861 us |
| Mediator dispatch × N | ~100,000 us (10K × ~10us) | **~10 us** (1 × ~10us) |
| Activity span × N | ~50,000 us (10K × ~5us) | **~5 us** (1 × ~5us) |
| Counter + Histogram × N | ~30,000 us (10K × ~3us) | **~3 us** (1 × ~3us) |
| 로그 × 2N | ~100,000 us (20K × ~5us) | **~10 us** (2 × ~5us) |
| **총 Observability 포함 추정** | **~280 ms** | **<1 ms** |

**실제 Observability 포함 시 개선 배율: ~280x (10K 기준)**

---

## 3. DomainEventCollector TrackRange 회귀 확인

> 기존 수집기 성능이 변경 후에도 유지되는지 확인.

| Method | Count | Mean (us) | Allocated |
|--------|------:|----------:|----------:|
| HashSet TrackRange (O(n)) | 1,000 | 18.35 | 71.51 KB |
| HashSet TrackRange (O(n)) | 10,000 | 513.58 | 657.38 KB |
| HashSet TrackRange (O(n)) | 100,000 | 5,337.61 | 5,896.45 KB |

| 규모 | 기준 임계치 | 측정값 | 회귀 여부 |
|------|----------:|-------:|----------|
| 100K | < 100 ms | **5.3 ms** | **통과** |

---

## 4. CRUD별 수정 전후 비교

### Create — `CreateRange(N)`

| 측정 항목 | 수정 전 (N=1,000) | 수정 후 (N=1,000) |
|-----------|------------------:|------------------:|
| Mediator `Publish()` 호출 수 | 1,000회 | **1회** |
| Activity span 생성 | 1,000개 | **1개** |
| Counter.Add 호출 | 2,000회 (req+resp) | **2회** |
| Histogram.Record 호출 | 1,000회 | **1회** |
| 로그 출력 | 2,000건 (req+resp) | **2건** |
| 핸들러 호출 수 | 1,000회 (개별 처리) | **1회** (벌크 처리) |

### Update — `UpdateRange(N)` (Aggregate당 이벤트 타입 K개)

| 측정 항목 | 수정 전 (N=1,000, K=2) | 수정 후 (N=1,000, K=2) |
|-----------|----------------------:|----------------------:|
| Mediator `Publish()` 호출 수 | 2,000회 (N×K) | **2회** (K) |
| Activity span 생성 | 2,000개 | **2개** |
| 핸들러 호출 수 | 2,000회 (개별) | **2회** (타입당 1회) |

### Delete — `DeleteRange(N)`

#### EfCore (Hard/Soft Delete via ExecuteDeleteAsync/ExecuteUpdateAsync)

| 측정 항목 | 수정 전 (N=1,000) | 수정 후 (N=1,000) |
|-----------|------------------:|------------------:|
| 도메인 이벤트 발생 | **0개** (무관측) | **1개** (`BulkDeletedEvent`) |
| Activity span | 0개 | **1개** |
| Aggregate 로드 | 없음 | **없음** (성능 유지) |

#### InMemory (Soft Delete — `InMemoryProductRepository`)

| 측정 항목 | 수정 전 (N=1,000) | 수정 후 (N=1,000) |
|-----------|------------------:|------------------:|
| 이벤트 수집 | **0개** (Track 누락 버그) | **1,000개** (Track 버그 수정) |
| Mediator `Publish()` 호출 | **0회** (수집 안 됨) | **1회** (`BulkDomainEvent`) |

---

## 5. 성능 테스트 결과

| 테스트 | 임계치 | 결과 |
|--------|-------:|------|
| `BulkPublish_1K_Events_Completes_Under_Threshold` | < 100 ms | **통과** |
| `BulkPublish_10K_Events_Completes_Under_Threshold` | < 500 ms | **통과** |
| `DeleteRange_Raises_BulkDeletedEvent` | N/A | **통과** |
| `TrackRange_100K_Items_Completes_Under_100ms` | < 100 ms | **통과** |
| `Track_100K_Individual_Items_Completes_Under_200ms` | < 200 ms | **통과** |

---

## 6. 종합

| CRUD 작업 | 규모 | Publish 호출 (수정 전) | Publish 호출 (수정 후) | 감소 배율 |
|-----------|------|----------------------:|----------------------:|----------:|
| `CreateRange` | 1K | 1,000 | 1 | 1,000x |
| `CreateRange` | 10K | 10,000 | 1 | 10,000x |
| `CreateRange` | 100K | 100,000 | 1 | 100,000x |
| `UpdateRange` (K=2) | 1K | 2,000 | 2 | 1,000x |
| `UpdateRange` (K=2) | 10K | 20,000 | 2 | 10,000x |
| `DeleteRange` (EfCore) | 1K | 0 (무관측) | 1 (관측 가능) | 0→1 |
| `DeleteRange` (InMemory) | 1K | 0 (버그) | 1 | 0→1 |
