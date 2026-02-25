# Histogram Exploration Tutorial

Histogram 이해를 위한 단계별 예제 프로젝트입니다. 초보자부터 고급 수준까지의 예제를 포함하며, Microsoft Learn 문서와 Functorium 코드베이스의 실제 구현을 참고합니다.

## 📚 학습 목표

1. **Histogram 기본 개념**: 분포 측정, 백분위수
2. **백분위수 이해**: P50, P95, P99의 의미와 활용 (Basic04)
3. **버킷 전략**: 기본 버킷 vs 커스텀 버킷
4. **SLO 정렬**: 목표값과 버킷 경계 정렬의 중요성
5. **성능 고려사항**: 메모리 사용, 태그 카디널리티
6. **실전 적용**: 실제 시나리오에서의 활용 방법

## 📊 백분위수(Percentile) 이해

### 백분위수란?

백분위수는 데이터 집합에서 특정 비율의 값이 그보다 작거나 같은 값을 나타냅니다.

**주요 백분위수:**
- **P50 (중앙값)**: 50%의 값이 이 값보다 작거나 같음
- **P90**: 90%의 값이 이 값보다 작거나 같음
- **P95**: 95%의 값이 이 값보다 작거나 같음
- **P99**: 99%의 값이 이 값보다 작거나 같음

### 백분위수 값의 의미

각 백분위수 값이 실제로 무엇을 의미하는지 구체적으로 이해해봅시다.

#### P50 (중앙값, Median)

**의미**: 데이터를 정렬했을 때 정확히 중간에 있는 값

**예시**: P50 = 200ms
- 100개 요청 중 50개는 200ms 이하
- 100개 요청 중 50개는 200ms 이상
- "절반의 요청은 빠르고, 절반은 느리다"는 의미

**활용**:
- 전체 성능의 대표값으로 사용
- 평균값보다 이상치(outlier)의 영향을 덜 받음
- 일반적인 사용자 경험을 나타냄

#### P90

**의미**: 90%의 요청이 이 값 이하

**예시**: P90 = 900ms
- 100개 요청 중 90개는 900ms 이하
- 100개 요청 중 10개만 900ms 초과
- "대부분의 요청은 빠르지만, 일부는 느리다"

**활용**:
- 일반적인 성능 목표 설정
- 대부분의 사용자 경험을 나타냄
- SLO 설정에 자주 사용됨

#### P95

**의미**: 95%의 요청이 이 값 이하

**예시**: P95 = 2000ms
- 100개 요청 중 95개는 2000ms 이하
- 100개 요청 중 5개만 2000ms 초과
- "거의 모든 요청은 빠르지만, 극소수는 매우 느리다"

**활용**:
- **가장 많이 사용되는 백분위수**
- SLO 설정의 표준 (예: P95 ≤ 500ms)
- 대부분의 사용자 경험을 나타냄
- Tail latency (꼬리 지연시간) 분석에 중요

**왜 P95를 많이 사용하나?**
- P50은 너무 낙관적 (절반만 고려)
- P99는 너무 보수적 (극단적인 경우까지 고려)
- P95는 균형잡힌 지표 (대부분의 사용자 + 일부 예외 고려)

#### P99

**의미**: 99%의 요청이 이 값 이하

**예시**: P99 = 4640ms (10개 요청 기준)
- 10개 요청 중 9.9개는 4640ms 이하
- 10개 요청 중 0.1개만 4640ms 초과
- "거의 모든 요청은 빠르지만, 극히 일부는 매우 느리다"
- 실제 데이터: [100, 120, 150, 180, 200, 250, 300, 500, 1000, 5000]에서 거의 10번째 값(5000ms)에 가까움

**활용**:
- 최악의 경우를 제외한 성능 분석
- 인프라 용량 계획 (최악의 경우 대비)
- 사용자 불만 최소화 목표
- Critical 서비스의 엄격한 SLO 설정

**주의사항**:
- P99는 극단적인 값에 민감함
- 일부 이상치가 전체 분포를 왜곡할 수 있음
- P99.9, P99.99 등 더 높은 백분위수도 사용 가능

#### 백분위수 비교 예시

**시나리오**: API 응답 시간 측정 결과

| 백분위수 | 값 | 해석 |
|---------|-----|------|
| **P50** | 150ms | 절반의 사용자는 150ms 이내 응답 받음 ✅ |
| **P90** | 300ms | 90%의 사용자는 300ms 이내 응답 받음 ✅ |
| **P95** | 500ms | 95%의 사용자는 500ms 이내 응답 받음 ✅ |
| **P99** | 2000ms | 99%의 사용자는 2초 이내 응답 받음 ⚠️ |
| **평균** | 250ms | 전체 평균은 빠르지만... |

**분석**:
- P50~P95는 모두 양호한 수준 (500ms 이하)
- 하지만 P99가 2000ms로 높음
- → 극소수의 요청이 매우 느림 (1% = 100개 중 1개)
- → 이 1%의 요청이 사용자 불만의 원인일 수 있음

**조치**:
- P99를 개선하기 위해 느린 요청의 원인 조사 필요
- 데이터베이스 쿼리 최적화, 캐시 추가 등
- 목표: P99를 2000ms → 1000ms로 개선

### 왜 백분위수가 중요한가?

평균값만으로는 전체 분포를 이해하기 어렵습니다:

```
평균: 100ms
하지만 일부 요청은 1000ms 이상 걸릴 수 있음
→ P95를 보면 '대부분의 요청'이 얼마나 걸리는지 알 수 있음
```

### 구체적인 예제

**시나리오**: 100개의 HTTP 요청 처리 시간을 측정했습니다.

**측정값 예시** (밀리초):
```
[50, 52, 55, 58, 60, 62, 65, 68, 70, 72, 75, 78, 80, 82, 85, 88, 90, 92, 95, 98,
 100, 105, 110, 115, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175,
 180, 185, 190, 195, 200, 210, 220, 230, 240, 250, 260, 270, 280, 290, 300, 320,
 340, 360, 380, 400, 420, 440, 460, 480, 500, 550, 600, 650, 700, 750, 800, 850,
 900, 950, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000,
 2200, 2400, 2600, 2800, 3000, 3500, 4000, 4500, 5000, 6000, 7000, 8000, 9000, 10000]
```

**백분위수 계산 결과**:

| 백분위수 | 값 | 의미 |
|---------|-----|------|
| **P50 (중앙값)** | 200ms | 절반의 요청(50개)이 200ms 이내에 완료됨 |
| **P90** | 900ms | 90%의 요청(90개)이 900ms 이내에 완료됨 |
| **P95** | 2000ms | 95%의 요청(95개)이 2000ms 이내에 완료됨 |
| **P99** | 7000ms | 99%의 요청(99개)이 7000ms 이내에 완료됨 |
| **평균** | 1,050ms | 모든 요청의 평균 처리 시간 |

**해석**:
- **평균값(1,050ms)**: 모든 요청의 평균 → "전체적으로 느리다"고 오해할 수 있음
- **P50(200ms)**: 절반의 요청이 200ms 이내 → "대부분의 요청은 빠르다"
- **P90(900ms)**: 90%의 요청이 900ms 이내 → "거의 모든 요청은 빠르다"
- **P95(2000ms)**: 95%의 요청이 2초 이내 → "대부분의 사용자는 2초 이내 응답"
- **P99(7000ms)**: 99%의 요청이 7초 이내 → "극소수의 요청(1%)만 매우 느리다"

**핵심 인사이트**:
- 평균값은 느린 요청(극단값)에 의해 왜곡됨
- P50은 "일반적인" 성능을 보여줌
- P95는 "대부분의 사용자"가 경험하는 성능
- P99는 "거의 모든 사용자"가 경험하는 성능 (극단 제외)

**SLO 설정 예시**:
```
SLO 목표: P95 ≤ 500ms
현재 P95: 2000ms
→ SLO 위반! (2000ms > 500ms)
→ 성능 최적화가 필요함
```

**성능 개선 후**:
```
개선 전: P95 = 2000ms
개선 후: P95 = 450ms
→ SLO 달성! (450ms ≤ 500ms)
```

### Histogram과 백분위수의 관계

1. **Histogram에 측정값 기록**: 각 요청의 처리 시간을 기록
2. **버킷으로 집계**: 값들을 버킷 단위로 그룹화
3. **백분위수 계산**: 버킷 분포를 기반으로 백분위수 계산

**예시: 버킷 분포와 백분위수**

측정값: `[100, 120, 150, 180, 200, 250, 300, 500, 1000, 5000]` ms

버킷 경계: `[0.1s, 0.25s, 0.5s, 1s, 2.5s, 5s]` (초 단위)

측정값을 버킷으로 분류:
```
[0s - 0.1s):    0개 요청 (0%)   - 없음
[0.1s - 0.25s): 4개 요청 (40%)  - 100ms, 120ms, 150ms, 180ms
[0.25s - 0.5s): 3개 요청 (30%)  - 200ms, 250ms, 300ms
[0.5s - 1s):    1개 요청 (10%)  - 500ms
[1s - 2.5s):    1개 요청 (10%)  - 1000ms
[2.5s - 5s):    0개 요청 (0%)   - 없음
[5s 이상]:       1개 요청 (10%)  - 5000ms
```

백분위수 계산:
- **P50**: 40% (0.1s-0.25s 버킷)만으로는 부족 → 다음 버킷(0.25s-0.5s)에서 추가로 10% 필요 → `0.25s` 버킷 내부의 특정 값
- **P90**: 40% + 30% + 10% = 80% → 다음 버킷(0.5s-1s)에서 추가로 10% 필요 → `0.5s` 버킷 내부
- **P95**: 40% + 30% + 10% + 10% = 90% → 다음 버킷(1s-2.5s)에서 추가로 5% 필요 → `1s` 버킷 내부
- **P99**: 거의 모든 요청 포함 → `2.5s` 버킷 또는 그 이상

**직관적 이해**:
- 10개 요청 중 4개는 빠름 (100-180ms)
- 10개 요청 중 3개는 보통 (200-300ms)
- 10개 요청 중 2개는 느림 (500-1000ms)
- 10개 요청 중 1개는 매우 느림 (5000ms)
- → 이 1개의 매우 느린 요청이 평균값을 왜곡시킴

### 실전 활용 예제

#### 1. SLO 설정 및 모니터링

**시나리오**: API 서비스의 응답 시간 SLO 설정

```yaml
SLO 목표:
  - P95 ≤ 500ms  # 95%의 요청이 500ms 이내
  - P99 ≤ 1000ms # 99%의 요청이 1초 이내
```

**모니터링 쿼리** (Prometheus 예시):
```promql
# P95 계산
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# P99 계산
histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m]))

# SLO 위반 알림
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 0.5
```

#### 2. 성능 저하 감지

**시나리오**: 시간에 따른 P95 추이 관찰

```
시간대별 P95 값:
09:00 - P95: 200ms ✅ (정상)
10:00 - P95: 250ms ✅ (정상)
11:00 - P95: 450ms ⚠️  (주의)
12:00 - P95: 600ms ❌ (SLO 위반!)
13:00 - P95: 550ms ❌ (여전히 위반)
14:00 - P95: 300ms ✅ (복구됨)
```

**분석**: 11시~13시 사이에 성능 저하 발생 → 원인 조사 필요

#### 3. 용량 계획

**시나리오**: 인프라 용량 결정

```
현재 상태:
- 평균 RPS: 1000 req/s
- P99: 2000ms
- 목표: P99 ≤ 500ms

계산:
- 현재 처리 용량: 1000 req/s
- P99가 4배 개선 필요 (2000ms → 500ms)
- 예상 필요 용량: 4000 req/s

→ 인프라를 4배 확장 필요
```

#### 4. 사용자 경험 분석

**시나리오**: 사용자가 경험하는 실제 성능

```
평균 응답 시간: 500ms
P50 (중앙값): 200ms
P95: 1500ms
P99: 3000ms

해석:
- 평균(500ms)은 "전체적으로 느리다"고 보이지만
- P50(200ms)은 "절반의 사용자는 빠른 응답을 받는다"
- P95(1500ms)는 "대부분의 사용자(95%)는 1.5초 이내 응답"
- P99(3000ms)는 "극소수 사용자만 3초 이상 대기"

→ P95가 사용자 경험을 더 잘 나타냄
```

**예제 실행:**
```bash
dotnet run -- 4  # Basic04: Understanding Percentiles
```

## 🎯 핵심 개념: 버킷 정렬의 중요성

이 튜토리얼의 가장 중요한 개념은 **버킷 정렬(Bucket Alignment)**입니다.

### 문제 이해

`.sprints/sli-slo-sla-metrics-enhancement-plan.md`에서 언급된 다음 문장들을 이해하는 것이 목표입니다:

- ✅ **P95/P99 계산 정확도 향상**
- ✅ **SLO 임계값(예: 500ms) 정확히 측정 가능**

### 왜 중요한가?

Histogram은 버킷(bucket) 단위로 값을 집계합니다. 버킷 경계 위치가 측정 정확도에 직접적인 영향을 미칩니다.

**나쁜 예:**
- 버킷: `[0, 1, 2, 5, 10]`초
- SLO 임계값: 500ms (0.5초)
- 문제: 500ms가 버킷 경계에 없어서 부정확한 측정

**좋은 예:**
- 버킷: `[0.001, 0.005, 0.01, ..., 0.5, 1, ...]`초
- SLO 임계값: 500ms (0.5초)
- 장점: 0.5초가 버킷 경계에 있어 정확한 측정 가능

### Functorium의 접근

Functorium의 `DefaultHistogramBuckets`는 주요 SLO 임계값을 버킷 경계로 포함합니다:

- `0.5초 (500ms)` = Command SLO P95 목표값
- `1초 (1000ms)` = Command SLO P99 목표값
- `0.2초 (200ms)` = Query SLO P95 목표값
- `0.5초 (500ms)` = Query SLO P99 목표값

이렇게 하면 SLO 위반 여부를 정확하게 판단할 수 있습니다.

## 📁 프로젝트 구조

```
HistogramExploration/
├── Src/
│   └── HistogramExploration.Demo/
│       ├── Program.cs                    # 메인 진입점
│       ├── Basic/                        # 초보자 레벨
│       │   ├── Basic01_SimpleHistogram.cs
│       │   ├── Basic02_HistogramWithTags.cs
│       │   └── Basic03_HistogramUnits.cs
│       ├── Intermediate/                 # 중급 레벨
│       │   ├── Intermediate01_CustomBuckets.cs
│       │   ├── Intermediate02_MultipleHistograms.cs
│       │   └── Intermediate03_TagCombinations.cs
│       ├── Advanced/                      # 고급 레벨
│       │   ├── Advanced01_InstrumentAdvice.cs
│       │   ├── Advanced02_SloAlignedBuckets.cs
│       │   ├── Advanced03_RequestLatencyScenario.cs
│       │   ├── Advanced04_DatabaseQueryScenario.cs
│       │   ├── Advanced05_OrderProcessingScenario.cs
│       │   └── Advanced06_BucketAlignmentImpact.cs ⭐
│       └── Shared/
│           ├── ScenarioHelpers.cs
│           └── MetricViewer.cs
└── README.md
```

## 🚀 실행 방법

### 1. 프로젝트 빌드

```bash
cd Tutorials/HistogramExploration/Src/HistogramExploration.Demo
dotnet build
```

### 2. 예제 실행

**대화형 메뉴:**
```bash
dotnet run
```

**특정 예제 직접 실행:**
```bash
dotnet run -- 4   # Basic04 (백분위수 이해) 실행
dotnet run -- 13  # Advanced06 실행
```

### 3. 메트릭 모니터링 (선택사항)

다른 터미널에서 `dotnet-counters`를 사용하여 메트릭을 모니터링할 수 있습니다:

```bash
# dotnet-counters 설치 (한 번만)
dotnet tool update -g dotnet-counters

# 메트릭 모니터링
dotnet-counters monitor -n HistogramExploration.Demo --counters HistogramExploration.*
```

## 📖 예제 설명

### Basic Level

#### Basic01: Simple Histogram
- 가장 기본적인 Histogram 생성 및 기록
- `CreateHistogram<double>()` 사용
- `Record()` 메서드로 값 기록

#### Basic02: Histogram with Tags
- 태그를 사용한 다차원 메트릭
- `TagList` 사용 (Functorium 패턴)
- 색상, 크기 등으로 분류된 측정값 예제

#### Basic03: Histogram Units
- 단위(unit) 지정 방법
- UCUM 표준 준수
- 설명(description) 추가

#### Basic04: Understanding Percentiles ⭐
- 백분위수(Percentile) 개념 이해
- P50, P90, P95, P99의 의미
- Histogram과 백분위수의 관계
- 실제 데이터로 백분위수 계산 및 해석
- SLO 설정에서의 활용

### Intermediate Level

#### Intermediate01: Custom Buckets
- 커스텀 버킷 경계 설정
- `InstrumentAdvice` API 사용
- 버킷 선택 전략 설명

#### Intermediate02: Multiple Histograms
- 여러 Histogram 동시 사용
- Meter별 그룹화
- 카테고리별 메트릭 관리 (OpenTelemetryMetricRecorder 패턴)

#### Intermediate03: Tag Combinations
- 복잡한 태그 조합
- 태그 카디널리티 주의사항
- 메모리 최적화 팁

### Advanced Level

#### Advanced01: InstrumentAdvice API
- `InstrumentAdvice<T>` API 상세 설명
- 권장 버킷 경계 설정
- OpenTelemetry SDK 통합

#### Advanced02: SLO-Aligned Buckets
- SloConfiguration.DefaultHistogramBuckets 사용
- SLO 목표값과 버킷 정렬
- 백분위수 계산 정확도 향상

#### Advanced03: Request Latency Scenario
- HTTP 요청 지연시간 측정 시나리오
- 실제 웹 API 패턴 시뮬레이션
- P95, P99 백분위수 분석

#### Advanced04: Database Query Scenario
- 데이터베이스 쿼리 실행시간 측정
- 쿼리 타입별 분류 (SELECT, INSERT, UPDATE)
- 느린 쿼리 감지

#### Advanced05: Order Processing Scenario
- 주문 처리 시간 측정 (SLO 정렬)
- Functorium의 UsecaseMetricsPipeline 패턴 적용
- SLO 위반 감지 및 알림

#### Advanced06: Bucket Alignment Impact ⭐
**이 예제가 가장 중요합니다!**

- **핵심 개념**: "P95/P99 계산 정확도 향상"과 "SLO 임계값 정확히 측정 가능" 설명
- 문제 상황 시뮬레이션:
  - 시나리오: 100개의 요청 처리 시간 측정 (대부분 450-550ms 범위)
  - SLO 목표: P95 ≤ 500ms
  - 나쁜 버킷: `[0, 1, 2, 5, 10]`초 → 500ms가 버킷 경계에 없어 부정확한 P95 계산
  - 좋은 버킷: `[..., 0.25, 0.5, 1, ...]`초 → 0.5초(500ms)가 버킷 경계에 있어 정확한 P95 계산
- 실제 측정값으로 비교:
  - 같은 데이터셋으로 두 가지 버킷 설정으로 측정
  - P95/P99 값의 차이를 시각적으로 보여줌
  - 버킷 분포 히스토그램 출력으로 차이 명확히 표시

## 🔗 참고 자료

### Microsoft Learn 문서
- [Creating Metrics - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation)
- [System.Diagnostics.Metrics Namespace](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics)

### Functorium 코드베이스
- `Src/Functorium/Adapters/Observabilities/Metrics/OpenTelemetryMetricRecorder.cs`: Histogram 생성 및 사용 패턴
- `Src/Functorium/Applications/Observabilities/SloConfiguration.cs`: DefaultHistogramBuckets 및 SLO 정렬 버킷 설정
- `Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs`: 실제 프로덕션 사용 예제

### 관련 문서
- `.sprints/sli-slo-sla-metrics-enhancement-plan.md`: SLO/SLI/SLA 메트릭 개선 계획
- OpenTelemetry Histogram 문서

## 💡 주요 학습 포인트

1. **Histogram 기본 개념**: 분포 측정, 백분위수
2. **버킷 전략**: 기본 버킷 vs 커스텀 버킷
3. **SLO 정렬**: 목표값과 버킷 경계 정렬의 중요성
   - **핵심 이해**: 버킷 경계가 SLO 임계값과 정렬되어야 P95/P99 계산이 정확해짐
   - **예시**: SLO가 500ms인데 버킷이 [0, 1, 2, 5]초로 설정되면 500ms 근처 값들이 부정확하게 측정됨
   - **해결**: 버킷에 0.5초(500ms) 경계를 포함하면 해당 임계값을 정확히 측정 가능
4. **성능 고려사항**: 메모리 사용, 태그 카디널리티
5. **실전 적용**: 실제 시나리오에서의 활용 방법

## 🛠️ 요구사항

- .NET 10.0 SDK
- System.Diagnostics.DiagnosticSource 패키지 (자동 포함)
- Functorium 프로젝트 참조 (SloConfiguration 사용)

## 📝 라이선스

이 튜토리얼은 Functorium 프로젝트의 일부입니다.
