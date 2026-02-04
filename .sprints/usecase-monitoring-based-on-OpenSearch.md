# OpenSearch 기반 Usecase 모니터링 시스템 구축 계획

**작성일**: 2026-01-06
**최종 업데이트**: 2026-01-07
**목표**: Prometheus 대신 OpenSearch를 사용한 Usecase 레벨 모니터링 시스템 구축
**범위**: 사전 집계(Pre-aggregation)를 포함한 전체 아키텍처 및 구현 계획
**권장 아키텍처**: C# 사전 집계 → OpenTelemetry Collector → OpenSearch

---

## 📋 Phase 1: 현황 분석

### 현재 메트릭 아키텍처
- **수집**: OpenTelemetry SDK (Counter, Histogram)
- **내보내기**: OTLP Exporter → Prometheus
- **쿼리**: PromQL (rate(), histogram_quantile() 등)
- **대시보드**: Grafana

### 현재 수집 중인 메트릭
```
application.usecase.command.requests     (Counter)
application.usecase.command.responses    (Counter)
application.usecase.command.duration     (Histogram)
application.usecase.query.requests       (Counter)
application.usecase.query.responses      (Counter)
application.usecase.query.duration       (Histogram)
```

### 주요 태그 구조
- `request.category.type`: command/query
- `request.handler`: Handler 이름
- `response.status`: success/failure
- `error.type`: expected/exceptional/aggregate
- `error.code`: 에러 코드
- `slo.latency`: ok/p95_exceeded/p99_exceeded

---

## 🎯 Phase 2: OpenSearch 아키텍처 설계

### 아키텍처 개요

**권장 아키텍처**: C# 사전 집계 → OpenTelemetry Collector → OpenSearch

```
┌──────────────────────────────────────────────────────────────────┐
│                     Functorium Application (C#)                   │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │            UsecaseMetricsPipeline (기존)                     │ │
│  │  - OpenTelemetry SDK로 원본 메트릭 수집                    │ │
│  │  - Counter: requests, responses                              │ │
│  │  - Histogram: duration                                       │ │
│  └──────────────────┬───────────────────────────────────────────┘ │
│                     │                                              │
│  ┌──────────────────▼───────────────────────────────────────────┐ │
│  │         OpenSearchMetricsProcessor (신규 - 핵심)            │ │
│  │                                                               │ │
│  │  [집계 로직]                                                 │ │
│  │  1. 60초 윈도우에 메트릭 수집                               │ │
│  │  2. Rate 계산 (requests/second)                             │ │
│  │  3. Percentile 계산 (P50, P95, P99)                        │ │
│  │  4. Saturation 계산 (복합 포화도)                           │ │
│  │                                                               │ │
│  │  [출력]                                                       │ │
│  │  - AggregatedMetric 객체 생성                               │ │
│  │  - Meter API로 집계된 메트릭 기록                           │ │
│  └──────────────────┬───────────────────────────────────────────┘ │
│                     │                                              │
│                     │ OTLP/HTTP (Protobuf)                        │
└─────────────────────┼──────────────────────────────────────────────┘
                      │
                      ▼
┌──────────────────────────────────────────────────────────────────┐
│              OpenTelemetry Collector (권장)                       │
│                                                                    │
│  [Receivers]                                                       │
│  └─ otlp (HTTP: 4318, gRPC: 4317)                                │
│                                                                    │
│  [Processors]                                                      │
│  ├─ memory_limiter (메모리 512MB 제한)                           │
│  ├─ batch (100건씩 묶어서 처리)                                  │
│  └─ filter (application.usecase.* 만 통과)                       │
│                                                                    │
│  [Exporters]                                                       │
│  ├─ elasticsearch (OpenSearch 호환)                               │
│  └─ logging (디버깅)                                              │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   │ HTTP/JSON (Bulk API)
                   ▼
┌──────────────────────────────────────────────────────────────────┐
│                      OpenSearch Cluster                           │
│                                                                    │
│  [Indices]                                                         │
│  ├─ metrics-aggregated-{yyyy.MM.dd}                              │
│  │   └─ 사전 집계된 메트릭 (Rate, Percentile, Saturation)       │
│  │                                                                 │
│  ├─ metrics-requests-{yyyy.MM.dd}                                 │
│  │   └─ 원본 Counter (requests) - 선택적                        │
│  │                                                                 │
│  └─ metrics-responses-{yyyy.MM.dd}                                │
│      └─ 원본 Counter (responses) - 선택적                       │
│                                                                    │
│  [Features]                                                        │
│  ├─ OpenSearch Dashboards (시각화)                               │
│  ├─ Alerting (SLO 위반 알림)                                     │
│  └─ ISM (Index Lifecycle Management)                              │
└──────────────────────────────────────────────────────────────────┘
```

### 아키텍처 선택 가이드

#### 옵션 A: C# → OpenSearch 직접 (PoC/소규모)
- **적합**: 소규모 서비스 (<100 RPS), 빠른 검증 필요
- **장점**: 가장 단순, 빠른 구현, 낮은 비용
- **단점**: 확장성 제한, 배치/재시도 직접 구현 필요

#### 옵션 D: C# → OTel Collector → OpenSearch (프로덕션 권장) ✅
- **적합**: 중대형 서비스 (1000+ RPS), 마이크로서비스
- **장점**: 표준 준수, 확장성, 운영 효율 (배치, 재시도, 필터링)
- **단점**: 중간 복잡도 (+인프라 컴포넌트)

**권장**: Phase 1에서 옵션 A로 검증 → Phase 2에서 옵션 D로 전환

---

## 📝 Phase 3: 구현 계획

### 구현 전략

**선택한 아키텍처**: C# 사전 집계 → OpenTelemetry Collector → OpenSearch (옵션 D)

**이유**:
- ✅ 표준 준수 (OpenTelemetry CNCF 표준)
- ✅ 확장성 (다중 서비스 지원, Collector 수평 확장)
- ✅ 운영 효율 (배치 처리, 자동 재시도, 필터링)
- ✅ 미래 지향성 (벤더 종속성 최소화)

### 구현 로드맵 (2-3주)

```
Week 1-2: 코드 구현 및 로컬 검증
  ├─ OpenSearchMetricsProcessor 구현 (OTLP Exporter)
  ├─ UsecaseMetricsPipeline 통합
  ├─ DI 등록 및 설정
  └─ 로컬 Docker Compose로 전체 스택 검증

Week 3: 인프라 구성 및 배포
  ├─ OpenSearch 클러스터 설정
  ├─ OpenTelemetry Collector 배포
  ├─ 인덱스 템플릿 생성
  ├─ 대시보드 및 알림 설정
  └─ 프로덕션 배포
```

---

### 3.1. NuGet 패키지 추가

**파일**: `Directory.Packages.props`

```xml
<ItemGroup Label="OpenTelemetry">
  <!-- OTLP Exporter for Metrics -->
  <PackageVersion Include="OpenTelemetry" Version="1.7.0" />
  <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.7.0" />
  <PackageVersion Include="System.Diagnostics.DiagnosticSource" Version="8.0.0" />
</ItemGroup>
```

> **참고**: Elasticsearch 클라이언트는 불필요 (OTLP Exporter만 사용)

---

### 3.2. 인덱스 설계

#### 3.2.1. metrics-requests 인덱스

**목적**: Counter 메트릭 (요청 수)

```json
PUT _index_template/metrics-requests
{
  "index_patterns": ["metrics-requests-*"],
  "template": {
    "settings": {
      "number_of_shards": 3,
      "number_of_replicas": 1,
      "refresh_interval": "10s",
      "index.lifecycle.name": "metrics-policy",
      "index.lifecycle.rollover_alias": "metrics-requests"
    },
    "mappings": {
      "properties": {
        "@timestamp": { "type": "date" },
        "metric_name": { "type": "keyword" },
        "metric_type": { "type": "keyword", "value": "counter" },

        "tags": {
          "properties": {
            "request_handler_cqrs": { "type": "keyword" },
            "request_handler": { "type": "keyword" },
            "request_layer": { "type": "keyword" },
            "request_category": { "type": "keyword" }
          }
        },

        "counter": {
          "properties": {
            "value": { "type": "long" },
            "rate_1m": { "type": "double" },
            "rate_5m": { "type": "double" }
          }
        }
      }
    }
  }
}
```

#### 3.2.2. metrics-responses 인덱스

**목적**: Counter 메트릭 (응답 수, 에러 분류)

```json
PUT _index_template/metrics-responses
{
  "index_patterns": ["metrics-responses-*"],
  "template": {
    "settings": {
      "number_of_shards": 3,
      "number_of_replicas": 1,
      "refresh_interval": "10s"
    },
    "mappings": {
      "properties": {
        "@timestamp": { "type": "date" },
        "metric_name": { "type": "keyword" },
        "metric_type": { "type": "keyword", "value": "counter" },

        "tags": {
          "properties": {
            "request_handler_cqrs": { "type": "keyword" },
            "request_handler": { "type": "keyword" },
            "response_status": { "type": "keyword" },
            "error_type": { "type": "keyword" },
            "error_code": { "type": "keyword" },
            "slo_latency": { "type": "keyword" }
          }
        },

        "counter": {
          "properties": {
            "value": { "type": "long" },
            "rate_1m": { "type": "double" },
            "rate_5m": { "type": "double" }
          }
        }
      }
    }
  }
}
```

#### 3.2.3. metrics-duration 인덱스

**목적**: Histogram 메트릭 (응답 시간)

```json
PUT _index_template/metrics-duration
{
  "index_patterns": ["metrics-duration-*"],
  "template": {
    "settings": {
      "number_of_shards": 3,
      "number_of_replicas": 1,
      "refresh_interval": "10s"
    },
    "mappings": {
      "properties": {
        "@timestamp": { "type": "date" },
        "metric_name": { "type": "keyword" },
        "metric_type": { "type": "keyword", "value": "histogram" },

        "tags": {
          "properties": {
            "request_handler_cqrs": { "type": "keyword" },
            "request_handler": { "type": "keyword" },
            "slo_latency": { "type": "keyword" }
          }
        },

        "histogram": {
          "properties": {
            "count": { "type": "long" },
            "sum": { "type": "double" },
            "min": { "type": "double" },
            "max": { "type": "double" },
            "buckets": {
              "type": "nested",
              "properties": {
                "le": { "type": "double" },
                "count": { "type": "long" }
              }
            }
          }
        }
      }
    }
  }
}
```

#### 3.2.4. metrics-aggregated 인덱스 (사전 집계)

**목적**: 사전 계산된 메트릭 (Percentile, 포화도 등)

```json
PUT _index_template/metrics-aggregated
{
  "index_patterns": ["metrics-aggregated-*"],
  "template": {
    "settings": {
      "number_of_shards": 3,
      "number_of_replicas": 1,
      "refresh_interval": "5s"
    },
    "mappings": {
      "properties": {
        "@timestamp": { "type": "date" },
        "aggregation_window": { "type": "keyword" },
        "request_handler_cqrs": { "type": "keyword" },
        "request_handler": { "type": "keyword" },

        "latency": {
          "properties": {
            "p50": { "type": "double" },
            "p95": { "type": "double" },
            "p99": { "type": "double" },
            "avg": { "type": "double" },
            "count": { "type": "long" }
          }
        },

        "traffic": {
          "properties": {
            "rps": { "type": "double" },
            "total": { "type": "long" }
          }
        },

        "throughput": {
          "properties": {
            "rps": { "type": "double" },
            "total": { "type": "long" },
            "efficiency_percent": { "type": "double" }
          }
        },

        "errors": {
          "properties": {
            "total_rate": { "type": "double" },
            "expected_rate": { "type": "double" },
            "exceptional_rate": { "type": "double" },
            "error_percent": { "type": "double" }
          }
        },

        "availability": {
          "properties": {
            "percent": { "type": "double" },
            "success_count": { "type": "long" },
            "total_count": { "type": "long" }
          }
        },

        "saturation": {
          "properties": {
            "latency_saturation_percent": { "type": "double" },
            "throughput_saturation_percent": { "type": "double" },
            "error_saturation_percent": { "type": "double" },
            "composite_saturation_percent": { "type": "double" }
          }
        }
      }
    }
  }
}
```

---

### 3.3. 코드 구현

#### 3.3.1. 설정 모델 (Configuration)

**파일**: `Src/Functorium/Adapters/Observabilities/OpenTelemetryOptions.cs` (확장)

기존 `OpenTelemetryOptions`에 OpenSearch 관련 설정 추가:

```csharp
namespace Functorium.Adapters.Observabilities;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// 서비스 이름
    /// </summary>
    public string ServiceName { get; init; } = "Functorium";

    /// <summary>
    /// 서비스 버전
    /// </summary>
    public string ServiceVersion { get; init; } = "1.0.0";

    /// <summary>
    /// OTLP Collector 엔드포인트 (예: http://otel-collector:4318)
    /// </summary>
    public string OtlpEndpoint { get; init; } = "http://localhost:4318";

    /// <summary>
    /// 메트릭 전송 활성화 여부
    /// </summary>
    public bool EnableMetrics { get; init; } = true;

    /// <summary>
    /// 사전 집계 윈도우 (초)
    /// </summary>
    public int AggregationWindowSeconds { get; init; } = 60;

    /// <summary>
    /// Histogram 버킷 경계값 (밀리초)
    /// </summary>
    public double[] HistogramBuckets { get; init; } = new[]
    {
        0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1.0, 2.5, 5.0, 7.5, 10.0
    };
}
```

**appsettings.json 예시**:
```json
{
  "OpenTelemetry": {
    "ServiceName": "Functorium",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4318",
    "EnableMetrics": true,
    "AggregationWindowSeconds": 60,
    "HistogramBuckets": [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0]
  }
}
```

---

#### 3.3.2. 메트릭 문서 모델 (집계된 메트릭)

**파일**: `Src/Functorium/Adapters/Observabilities/Models/AggregatedMetrics.cs` (신규)

> **참고**: OTLP 모드에서는 집계된 메트릭만 전송하므로 원본 메트릭 문서 모델은 불필요합니다.

```csharp
namespace Functorium.Adapters.Observabilities.Models;

/// <summary>
/// 집계된 메트릭 (내부 사용)
/// </summary>
internal sealed record AggregatedMetrics
{
    public string Handler { get; init; } = string.Empty;
    public string Cqrs { get; init; } = string.Empty;

    // Rate
    public double RequestRate { get; init; }
    public double ResponseRate { get; init; }
    public double ErrorRate { get; init; }

    // Latency (Percentiles)
    public double LatencyP50 { get; init; }
    public double LatencyP95 { get; init; }
    public double LatencyP99 { get; init; }
    public double LatencyAvg { get; init; }

    // Throughput
    public double ThroughputRps { get; init; }
    public double EfficiencyPercent { get; init; }

    // Errors
    public double ExpectedErrorRate { get; init; }
    public double ExceptionalErrorRate { get; init; }
    public double ErrorPercent { get; init; }

    // Availability
    public double AvailabilityPercent { get; init; }

    // Saturation
    public double Saturation { get; init; }
}
```

---

#### 3.3.3. 사전 집계 프로세서 (핵심 로직)

**파일**: `Src/Functorium/Adapters/Observabilities/OpenSearchMetricsProcessor.cs` (신규)

> **상세 구현**: [`.claude/plans/luminous-growing-pascal.md`](c:\Users\hyungho.ko\.claude\plans\luminous-growing-pascal.md) 참조 (Line 1323-1511)

**구현 개요**:

```csharp
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Functorium.Adapters.Observabilities;

public sealed class OpenSearchMetricsProcessor : IDisposable
{
    private readonly Meter _meter;
    private readonly MeterProvider _meterProvider;
    private readonly ConcurrentDictionary<string, MetricWindow> _windows = new();
    private readonly Timer _aggregationTimer;

    public OpenSearchMetricsProcessor(IOptions<OpenTelemetryOptions> options, ...)
    {
        // Meter 생성
        _meter = new Meter("Functorium.Aggregated.Metrics", "1.0.0");

        // OTLP Exporter 설정
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("Functorium.Aggregated.Metrics")
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(options.Value.OtlpEndpoint); // http://otel-collector:4318
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                opt.ExportProcessorType = ExportProcessorType.Batch;
                opt.BatchExportProcessorOptions = new()
                {
                    MaxQueueSize = 2048,
                    ScheduledDelayMilliseconds = 10000, // 10초마다 전송
                    MaxExportBatchSize = 512
                };
            })
            .Build();

        // 60초마다 집계 수행
        _aggregationTimer = new Timer(
            _ => AggregateAndExport().GetAwaiter().GetResult(),
            null,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60));
    }

    // Public Methods
    public void RecordRequest(string metricName, string handlerCqrs, string handler, long value = 1) { ... }
    public void RecordResponse(...) { ... }
    public void RecordDuration(...) { ... }

    // Private Methods
    private async Task AggregateAndExport()
    {
        // 각 윈도우에 대해 집계 계산
        // Meter API로 기록 (ObservableGauge)
        // OTLP Exporter가 자동으로 배치 전송
    }

    private AggregatedMetrics CalculateAggregatedMetrics(MetricWindow window)
    {
        // Rate, Percentile, Saturation 계산
    }

    private void RecordAggregatedMetrics(AggregatedMetrics metrics)
    {
        // ObservableGauge로 기록
        _meter.CreateObservableGauge("application.usecase.request_rate", ...);
        _meter.CreateObservableGauge("application.usecase.latency_p95", ...);
        _meter.CreateObservableGauge("application.usecase.saturation", ...);
    }
}

// MetricWindow: 60초 동안 데이터 수집
internal sealed class MetricWindow
{
    // Thread-safe counters (Interlocked)
    // Duration list (lock)
    // Percentile 계산 메서드
}
```

**핵심 포인트**:
1. ✅ **Elasticsearch 클라이언트 불필요** → OpenTelemetry Meter API 사용
2. ✅ **버퍼링 불필요** → OTLP Exporter가 자동 배치 처리
3. ✅ **재시도 불필요** → OTel Collector가 자동 재시도
4. ✅ **집계된 메트릭만 전송** → 원본 메트릭은 생략 가능 (선택)
5. ✅ **60초 윈도우** → Rate, Percentile 정확히 계산

    private readonly Timer _flushTimer;
    private readonly Timer _aggregationTimer;

    public OpenSearchMetricsProcessor(
        OpenSearchMetricsClient client,
        IOptions<OpenSearchOptions> options,
        IOptions<SloConfiguration> sloConfig,
        ILogger<OpenSearchMetricsProcessor> logger)
    {
        _client = client;
        _options = options.Value;
        _sloConfig = sloConfig.Value;
        _logger = logger;

        // 배치 플러시 타이머
        _flushTimer = new Timer(
            _ => FlushBuffers().GetAwaiter().GetResult(),
            null,
            TimeSpan.FromSeconds(_options.FlushIntervalSeconds),
            TimeSpan.FromSeconds(_options.FlushIntervalSeconds));

        // 사전 집계 타이머
        if (_options.EnablePreAggregation)
        {
            _aggregationTimer = new Timer(
                _ => AggregateMetrics().GetAwaiter().GetResult(),
                null,
                TimeSpan.FromSeconds(_options.PreAggregationWindowSeconds),
                TimeSpan.FromSeconds(_options.PreAggregationWindowSeconds));
        }
    }

    #region Public Methods

    /// <summary>
    /// 요청 메트릭 기록
    /// </summary>
    public void RecordRequest(
        string metricName,
        string handlerCqrs,
        string handler,
        long value = 1)
    {
        if (!_options.Enabled) return;

        var document = new MetricRequestDocument
        {
            Timestamp = DateTimeOffset.UtcNow,
            MetricName = metricName,
            MetricType = "counter",
            Tags = new MetricTags
            {
                RequestCategoryType = handlerCqrs,
                RequestHandler = handler,
                RequestLayer = "application",
                RequestCategory = "usecase"
            },
            Counter = new CounterData
            {
                Value = value,
                Rate1m = 0, // 사전 집계에서 계산
                Rate5m = 0  // 사전 집계에서 계산
            }
        };

        _requestBuffer.Enqueue(document);

        // 사전 집계 윈도우 업데이트
        if (_options.EnablePreAggregation)
        {
            UpdateWindow(handlerCqrs, handler, window => window.IncrementRequests(value));
        }
    }

    /// <summary>
    /// 응답 메트릭 기록
    /// </summary>
    public void RecordResponse(
        string metricName,
        string handlerCqrs,
        string handler,
        bool isSuccess,
        string? errorType = null,
        string? errorCode = null,
        string sloLatency = "ok",
        long value = 1)
    {
        if (!_options.Enabled) return;

        var document = new MetricResponseDocument
        {
            Timestamp = DateTimeOffset.UtcNow,
            MetricName = metricName,
            MetricType = "counter",
            Tags = new ResponseTags
            {
                RequestCategoryType = handlerCqrs,
                RequestHandler = handler,
                RequestLayer = "application",
                RequestCategory = "usecase",
                ResponseStatus = isSuccess ? "success" : "failure",
                ErrorType = errorType,
                ErrorCode = errorCode,
                SloLatency = sloLatency
            },
            Counter = new CounterData
            {
                Value = value,
                Rate1m = 0,
                Rate5m = 0
            }
        };

        _responseBuffer.Enqueue(document);

        // 사전 집계 윈도우 업데이트
        if (_options.EnablePreAggregation)
        {
            UpdateWindow(handlerCqrs, handler, window =>
            {
                window.IncrementResponses(value);
                if (isSuccess)
                {
                    window.IncrementSuccessResponses(value);
                }
                else
                {
                    window.IncrementFailureResponses(value, errorType);
                }
            });
        }
    }

    /// <summary>
    /// Duration (Histogram) 메트릭 기록
    /// </summary>
    public void RecordDuration(
        string metricName,
        string handlerCqrs,
        string handler,
        double durationSeconds,
        string sloLatency = "ok")
    {
        if (!_options.Enabled) return;

        var document = new MetricDurationDocument
        {
            Timestamp = DateTimeOffset.UtcNow,
            MetricName = metricName,
            MetricType = "histogram",
            Tags = new MetricTags
            {
                RequestCategoryType = handlerCqrs,
                RequestHandler = handler,
                RequestLayer = "application",
                RequestCategory = "usecase"
            },
            Histogram = new HistogramData
            {
                Count = 1,
                Sum = durationSeconds,
                Min = durationSeconds,
                Max = durationSeconds,
                Buckets = CalculateBuckets(durationSeconds)
            }
        };

        _durationBuffer.Enqueue(document);

        // 사전 집계 윈도우 업데이트
        if (_options.EnablePreAggregation)
        {
            UpdateWindow(handlerCqrs, handler, window => window.RecordDuration(durationSeconds));
        }
    }

    #endregion

    #region Private Methods - Buffering & Flushing

    private async Task FlushBuffers()
    {
        try
        {
            var tasks = new List<Task>();

            // Requests 플러시
            if (_requestBuffer.Count > 0)
            {
                var batch = DequeueAll(_requestBuffer);
                if (batch.Any())
                {
                    tasks.Add(_client.BulkIndexAsync(
                        $"{_options.IndexPrefix}-requests-{DateTime.UtcNow:yyyy.MM.dd}",
                        batch));
                }
            }

            // Responses 플러시
            if (_responseBuffer.Count > 0)
            {
                var batch = DequeueAll(_responseBuffer);
                if (batch.Any())
                {
                    tasks.Add(_client.BulkIndexAsync(
                        $"{_options.IndexPrefix}-responses-{DateTime.UtcNow:yyyy.MM.dd}",
                        batch));
                }
            }

            // Duration 플러시
            if (_durationBuffer.Count > 0)
            {
                var batch = DequeueAll(_durationBuffer);
                if (batch.Any())
                {
                    tasks.Add(_client.BulkIndexAsync(
                        $"{_options.IndexPrefix}-duration-{DateTime.UtcNow:yyyy.MM.dd}",
                        batch));
                }
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing metric buffers");
        }
    }

    private static List<T> DequeueAll<T>(ConcurrentQueue<T> queue)
    {
        var items = new List<T>();
        while (queue.TryDequeue(out var item))
        {
            items.Add(item);
        }
        return items;
    }

    #endregion

    #region Private Methods - Pre-Aggregation

    private void UpdateWindow(string cqrs, string handler, Action<MetricWindow> update)
    {
        var key = $"{cqrs}:{handler}";
        var window = _windows.GetOrAdd(key, _ => new MetricWindow(cqrs, handler));
        update(window);
    }

    private async Task AggregateMetrics()
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var aggregatedDocuments = new List<AggregatedMetricDocument>();

            foreach (var kvp in _windows)
            {
                var window = kvp.Value;

                // 사전 집계 계산
                var aggregated = CalculateAggregatedMetrics(window, now);
                if (aggregated != null)
                {
                    aggregatedDocuments.Add(aggregated);
                }

                // 윈도우 리셋
                window.Reset();
            }

            // 집계된 메트릭 인덱싱
            if (aggregatedDocuments.Any())
            {
                await _client.BulkIndexAsync(
                    $"{_options.IndexPrefix}-aggregated-{now:yyyy.MM.dd}",
                    aggregatedDocuments);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating metrics");
        }
    }

    private AggregatedMetricDocument? CalculateAggregatedMetrics(MetricWindow window, DateTimeOffset timestamp)
    {
        if (window.RequestCount == 0) return null;

        var cqrs = window.Cqrs;
        var handler = window.Handler;

        // SLO 설정 가져오기
        var slo = _sloConfig.GetHandlerSlo(handler)
                  ?? _sloConfig.GetCqrsSlo(cqrs)
                  ?? _sloConfig.GlobalDefaults;

        // Latency 계산
        var latency = window.DurationCount > 0
            ? new LatencyMetrics
            {
                P50 = window.CalculatePercentile(0.50),
                P95 = window.CalculatePercentile(0.95),
                P99 = window.CalculatePercentile(0.99),
                Avg = window.DurationSum / window.DurationCount,
                Count = window.DurationCount
            }
            : null;

        // Traffic 계산
        var windowSeconds = (double)_options.PreAggregationWindowSeconds;
        var traffic = new TrafficMetrics
        {
            Rps = window.RequestCount / windowSeconds,
            Total = window.RequestCount
        };

        // Throughput 계산
        var throughputRps = window.ResponseCount / windowSeconds;
        var throughput = new ThroughputMetrics
        {
            Rps = throughputRps,
            Total = window.ResponseCount,
            EfficiencyPercent = window.RequestCount > 0
                ? (window.ResponseCount * 100.0 / window.RequestCount)
                : 100.0
        };

        // Error 계산
        var errors = new ErrorMetrics
        {
            TotalRate = window.FailureCount / windowSeconds,
            ExpectedRate = window.ExpectedErrorCount / windowSeconds,
            ExceptionalRate = window.ExceptionalErrorCount / windowSeconds,
            ErrorPercent = window.ResponseCount > 0
                ? (window.FailureCount * 100.0 / window.ResponseCount)
                : 0.0
        };

        // Availability 계산
        var availability = new AvailabilityMetrics
        {
            Percent = window.ResponseCount > 0
                ? (window.SuccessCount * 100.0 / window.ResponseCount)
                : 100.0,
            SuccessCount = window.SuccessCount,
            TotalCount = window.ResponseCount
        };

        // Saturation 계산
        var saturation = CalculateSaturation(latency, throughput, errors, slo);

        return new AggregatedMetricDocument
        {
            Timestamp = timestamp,
            AggregationWindow = $"{_options.PreAggregationWindowSeconds}s",
            RequestCategoryType = cqrs,
            RequestHandler = handler,
            Latency = latency,
            Traffic = traffic,
            Throughput = throughput,
            Errors = errors,
            Availability = availability,
            Saturation = saturation
        };
    }

    private SaturationMetrics CalculateSaturation(
        LatencyMetrics? latency,
        ThroughputMetrics throughput,
        ErrorMetrics errors,
        SloTarget slo)
    {
        // Latency 포화도 (P95 기준)
        var latencySaturation = latency != null
            ? Math.Min(100, Math.Max(0, ((latency.P95 * 1000 / slo.LatencyP95Milliseconds) - 1) * 100))
            : 0.0;

        // Throughput 포화도 (효율성 저하)
        var throughputSaturation = Math.Min(100, Math.Max(0, (100 - throughput.EfficiencyPercent) * 5));

        // Error 포화도 (Exceptional 에러 비율)
        var errorSaturation = Math.Min(100, errors.ExceptionalRate * 10000 / 0.01); // 0.01% = 100% 포화

        // 복합 포화도
        var compositeSaturation = (latencySaturation + throughputSaturation + errorSaturation) / 3.0;

        return new SaturationMetrics
        {
            LatencySaturationPercent = latencySaturation,
            ThroughputSaturationPercent = throughputSaturation,
            ErrorSaturationPercent = errorSaturation,
            CompositeSaturationPercent = compositeSaturation
        };
    }

    private List<HistogramBucket> CalculateBuckets(double durationSeconds)
    {
        var buckets = new List<HistogramBucket>();
        foreach (var boundary in _sloConfig.HistogramBuckets)
        {
            buckets.Add(new HistogramBucket
            {
                Le = boundary,
                Count = durationSeconds <= boundary ? 1 : 0
            });
        }
        return buckets;
    }

    #endregion

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _aggregationTimer?.Dispose();
        FlushBuffers().GetAwaiter().GetResult(); // 최종 플러시
    }
}
```

##### Phase 2 구현 (OTLP 모드 - OpenTelemetry Exporter)

**변경 사항**: Elasticsearch 클라이언트 대신 OTLP Exporter 사용

```csharp
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// OpenSearch 메트릭 사전 집계 및 전송 프로세서 (Phase 2: OTLP)
/// </summary>
public sealed class OpenSearchMetricsProcessor : IDisposable
{
    private readonly Meter _meter;
    private readonly MeterProvider _meterProvider;
    private readonly OpenSearchOptions _options;
    private readonly SloConfiguration _sloConfig;
    private readonly ILogger<OpenSearchMetricsProcessor> _logger;

    // 사전 집계를 위한 윈도우 데이터
    private readonly ConcurrentDictionary<string, MetricWindow> _windows = new();
    private readonly Timer _aggregationTimer;

    public OpenSearchMetricsProcessor(
        IOptions<OpenSearchOptions> options,
        IOptions<SloConfiguration> sloConfig,
        ILogger<OpenSearchMetricsProcessor> logger)
    {
        _options = options.Value;
        _sloConfig = sloConfig.Value;
        _logger = logger;

        // Meter 생성 (집계된 메트릭 기록용)
        _meter = new Meter("Functorium.Aggregated.Metrics", "1.0.0");

        // MeterProvider 설정 (OTLP Exporter)
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("Functorium.Aggregated.Metrics")
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(_options.OtlpEndpoint); // http://otel-collector:4318
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                opt.ExportProcessorType = ExportProcessorType.Batch;
                opt.BatchExportProcessorOptions = new()
                {
                    MaxQueueSize = 2048,
                    ScheduledDelayMilliseconds = 10000, // 10초마다 전송
                    ExporterTimeoutMilliseconds = 30000,
                    MaxExportBatchSize = 512
                };
            })
            .Build();

        // 60초마다 집계 수행
        _aggregationTimer = new Timer(
            _ => AggregateAndExport().GetAwaiter().GetResult(),
            null,
            TimeSpan.FromSeconds(_options.PreAggregationWindowSeconds),
            TimeSpan.FromSeconds(_options.PreAggregationWindowSeconds));
    }

    // RecordRequest, RecordResponse, RecordDuration 메서드는 동일
    // (윈도우에 데이터만 수집)

    public void RecordRequest(string metricName, string handlerCqrs, string handler, long value = 1)
    {
        if (!_options.Enabled) return;

        var key = $"{handlerCqrs}:{handler}";
        var window = _windows.GetOrAdd(key, _ => new MetricWindow(handlerCqrs, handler));
        window.IncrementRequests(value);
    }

    public void RecordResponse(
        string metricName, string handlerCqrs, string handler,
        bool isSuccess, string? errorType, string? errorCode, string sloLatency, long value = 1)
    {
        if (!_options.Enabled) return;

        var key = $"{handlerCqrs}:{handler}";
        var window = _windows.GetOrAdd(key, _ => new MetricWindow(handlerCqrs, handler));

        window.IncrementResponses(value);
        if (isSuccess)
            window.IncrementSuccessResponses(value);
        else
            window.IncrementFailureResponses(value, errorType);
    }

    public void RecordDuration(string metricName, string handlerCqrs, string handler, double durationSeconds, string sloLatency = "ok")
    {
        if (!_options.Enabled) return;

        var key = $"{handlerCqrs}:{handler}";
        var window = _windows.GetOrAdd(key, _ => new MetricWindow(handlerCqrs, handler));
        window.RecordDuration(durationSeconds);
    }

    // 60초마다 자동 호출
    private async Task AggregateAndExport()
    {
        foreach (var kvp in _windows)
        {
            var window = kvp.Value;

            // 집계 계산
            var aggregated = CalculateAggregatedMetrics(window);
            if (aggregated != null)
            {
                // Meter API로 집계된 메트릭 기록
                RecordAggregatedMetrics(aggregated);
            }

            // 윈도우 리셋
            window.Reset();
        }

        // OTLP Exporter가 자동으로 배치 전송
        await Task.CompletedTask;
    }

    private AggregatedMetrics CalculateAggregatedMetrics(MetricWindow window)
    {
        if (window.RequestCount == 0) return null;

        var windowSeconds = (double)_options.PreAggregationWindowSeconds;

        return new AggregatedMetrics
        {
            Handler = window.Handler,
            Cqrs = window.Cqrs,

            // Rate 계산
            RequestRate = window.RequestCount / windowSeconds,
            ResponseRate = window.ResponseCount / windowSeconds,

            // Percentile 계산
            LatencyP50 = window.CalculatePercentile(0.50),
            LatencyP95 = window.CalculatePercentile(0.95),
            LatencyP99 = window.CalculatePercentile(0.99),

            // Error 계산
            ErrorRate = window.FailureCount / windowSeconds,

            // Saturation 계산
            Saturation = CalculateSaturation(window)
        };
    }

    // Meter API로 기록 (OTLP로 자동 전송됨)
    private void RecordAggregatedMetrics(AggregatedMetrics metrics)
    {
        var tags = new TagList
        {
            { "handler", metrics.Handler },
            { "cqrs", metrics.Cqrs }
        };

        // ObservableGauge로 기록 (집계된 값)
        _meter.CreateObservableGauge("application.usecase.request_rate",
            () => new Measurement<double>(metrics.RequestRate, tags),
            "rps", "Request rate per second");

        _meter.CreateObservableGauge("application.usecase.latency_p95",
            () => new Measurement<double>(metrics.LatencyP95 * 1000, tags),
            "ms", "P95 latency");

        _meter.CreateObservableGauge("application.usecase.saturation",
            () => new Measurement<double>(metrics.Saturation, tags),
            "%", "Saturation percentage");
    }

    private double CalculateSaturation(MetricWindow window)
    {
        // SLO 기반 포화도 계산 로직 (Phase 1과 동일)
        return 0.0; // 구현 생략
    }

    public void Dispose()
    {
        _aggregationTimer?.Dispose();
        _meterProvider?.Dispose();
        _meter?.Dispose();
    }
}
```

**Phase 2의 주요 변경점**:
1. ✅ **Elasticsearch 클라이언트 제거** → `Meter`/`MeterProvider` 사용
2. ✅ **버퍼링 제거** → OTLP Exporter가 자동 배치 처리
3. ✅ **FlushBuffers 제거** → `MeterProvider`가 자동 전송
4. ✅ **집계된 메트릭만 전송** → 원본 메트릭은 제거 (선택)

---

### Phase 1 vs Phase 2 비교

| 항목 | Phase 1 (Direct) | Phase 2 (OTLP) |
|------|------------------|----------------|
| **클라이언트** | Elasticsearch 클라이언트 | OpenTelemetry Meter API |
| **전송 방식** | HTTP/JSON (Bulk API) | OTLP/HTTP (Protobuf) |
| **배치 처리** | Application 구현 | OTel Collector |
| **재시도** | Application 구현 | OTel Collector |
| **필터링** | Application 구현 | OTel Collector |
| **코드 복잡도** | 중간 | 낮음 |
| **표준 준수** | OpenSearch API | OpenTelemetry 표준 |
| **확장성** | 제한적 | 우수 |

---

##### 공통: 메트릭 윈도우 (Phase 1/2 동일)

/// <summary>
/// 메트릭 집계 윈도우
/// </summary>
internal sealed class MetricWindow
{
    public string Cqrs { get; }
    public string Handler { get; }

    private long _requestCount;
    private long _responseCount;
    private long _successCount;
    private long _failureCount;
    private long _expectedErrorCount;
    private long _exceptionalErrorCount;

    private readonly List<double> _durations = new();
    private double _durationSum;

    public long RequestCount => _requestCount;
    public long ResponseCount => _responseCount;
    public long SuccessCount => _successCount;
    public long FailureCount => _failureCount;
    public long ExpectedErrorCount => _expectedErrorCount;
    public long ExceptionalErrorCount => _exceptionalErrorCount;
    public long DurationCount => _durations.Count;
    public double DurationSum => _durationSum;

    public MetricWindow(string cqrs, string handler)
    {
        Cqrs = cqrs;
        Handler = handler;
    }

    public void IncrementRequests(long count = 1)
        => Interlocked.Add(ref _requestCount, count);

    public void IncrementResponses(long count = 1)
        => Interlocked.Add(ref _responseCount, count);

    public void IncrementSuccessResponses(long count = 1)
        => Interlocked.Add(ref _successCount, count);

    public void IncrementFailureResponses(long count, string? errorType)
    {
        Interlocked.Add(ref _failureCount, count);

        if (errorType == "expected")
            Interlocked.Add(ref _expectedErrorCount, count);
        else if (errorType == "exceptional")
            Interlocked.Add(ref _exceptionalErrorCount, count);
    }

    public void RecordDuration(double durationSeconds)
    {
        lock (_durations)
        {
            _durations.Add(durationSeconds);
            _durationSum += durationSeconds;
        }
    }

    public double CalculatePercentile(double percentile)
    {
        lock (_durations)
        {
            if (_durations.Count == 0) return 0;

            var sorted = _durations.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            return sorted[Math.Max(0, index)];
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _requestCount, 0);
        Interlocked.Exchange(ref _responseCount, 0);
        Interlocked.Exchange(ref _successCount, 0);
        Interlocked.Exchange(ref _failureCount, 0);
        Interlocked.Exchange(ref _expectedErrorCount, 0);
        Interlocked.Exchange(ref _exceptionalErrorCount, 0);

        lock (_durations)
        {
            _durations.Clear();
            _durationSum = 0;
        }
    }
}
```

---

#### 3.3.5. UsecaseMetricsPipeline 통합

**파일**: `Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs` (수정)

기존 파일에 OpenSearchMetricsProcessor 호출 추가:

```csharp
// 기존 코드 유지, OnBefore/OnAfter에 추가

private readonly OpenSearchMetricsProcessor? _openSearchProcessor;

public UsecaseMetricsPipeline(
    /* ... 기존 파라미터 ... */
    OpenSearchMetricsProcessor? openSearchProcessor = null) // 옵션으로 주입
{
    // ... 기존 초기화 ...
    _openSearchProcessor = openSearchProcessor;
}

protected override void OnBefore(TRequest request)
{
    // ... 기존 코드 (OpenTelemetry Counter) ...

    // OpenSearch 기록
    _openSearchProcessor?.RecordRequest(
        metricName: $"application.usecase.{cqrs}.requests",
        handlerCqrs: cqrs,
        handler: handlerName,
        value: 1);
}

protected override void OnAfter(TRequest request, TResponse response, TimeSpan elapsed)
{
    // ... 기존 코드 (OpenTelemetry Counter/Histogram) ...

    // OpenSearch 응답 기록
    var (errorType, errorCode) = response.IsSucc
        ? (null, null)
        : GetErrorInfo(response.Errors);

    _openSearchProcessor?.RecordResponse(
        metricName: $"application.usecase.{cqrs}.responses",
        handlerCqrs: cqrs,
        handler: handlerName,
        isSuccess: response.IsSucc,
        errorType: errorType,
        errorCode: errorCode,
        sloLatency: sloLatency,
        value: 1);

    // OpenSearch Duration 기록
    _openSearchProcessor?.RecordDuration(
        metricName: $"application.usecase.{cqrs}.duration",
        handlerCqrs: cqrs,
        handler: handlerName,
        durationSeconds: elapsed.TotalSeconds,
        sloLatency: sloLatency);
}
```

---

#### 3.3.6. DI 등록

**파일**: `Src/Functorium/Abstractions/Registrations/OpenSearchRegistration.cs` (신규)

```csharp
namespace Functorium.Abstractions.Registrations;

public static class OpenSearchRegistration
{
    /// <summary>
    /// OpenSearch 메트릭 시스템 등록
    /// </summary>
    public static IServiceCollection RegisterOpenSearchMetrics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options 등록
        services.Configure<OpenSearchOptions>(configuration.GetSection(OpenSearchOptions.SectionName));

        // 클라이언트 등록 (Singleton)
        services.AddSingleton<OpenSearchMetricsClient>();

        // 프로세서 등록 (Singleton)
        services.AddSingleton<OpenSearchMetricsProcessor>();

        // IHostedService로 등록하여 백그라운드 플러시 실행
        services.AddHostedService<OpenSearchMetricsBackgroundService>();

        return services;
    }
}

/// <summary>
/// 백그라운드 서비스 (Graceful Shutdown 지원)
/// </summary>
internal sealed class OpenSearchMetricsBackgroundService : IHostedService
{
    private readonly OpenSearchMetricsProcessor _processor;

    public OpenSearchMetricsBackgroundService(OpenSearchMetricsProcessor processor)
    {
        _processor = processor;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // 종료 시 최종 플러시
        _processor.Dispose();
        return Task.CompletedTask;
    }
}
```

---

### 3.4. OpenSearch 쿼리 가이드 문서

**파일**: `.sprints/opensearch-query-guide.md` (신규)

계획 문서와 별도로 쿼리 가이드 문서 생성 (PromQL → OpenSearch DSL/SQL 변환 예시 포함)

---

## 📋 Phase 4: 마이그레이션 체크리스트

### 4.1. 개발 단계
- [ ] NuGet 패키지 추가 (`Elastic.Clients.Elasticsearch`)
- [ ] OpenSearch 인덱스 템플릿 생성 (4개)
- [ ] 설정 모델 구현 (`OpenSearchOptions`)
- [ ] 클라이언트 래퍼 구현 (`OpenSearchMetricsClient`)
- [ ] 메트릭 문서 모델 구현
- [ ] 사전 집계 프로세서 구현 (`OpenSearchMetricsProcessor`)
- [ ] UsecaseMetricsPipeline 통합
- [ ] DI 등록 구현
- [ ] 단위 테스트 작성

### 4.2. 배포 준비
- [ ] appsettings.json에 OpenSearch 설정 추가
- [ ] OpenSearch 클러스터 설정 (개발/스테이징/프로덕션)
- [ ] 인덱스 라이프사이클 정책 설정 (ISM)
- [ ] 알림 규칙 설정 (OpenSearch Alerting)
- [ ] 대시보드 구성 (OpenSearch Dashboards)

### 4.3. 검증
- [ ] 메트릭 수집 확인 (OpenSearch에 데이터 유입)
- [ ] 사전 집계 정확성 검증 (Percentile, Rate 계산)
- [ ] 성능 테스트 (배치 처리, 메모리 사용량)
- [ ] 기존 Prometheus와 병행 운영 (데이터 비교)

### 4.4. 전환
- [ ] Prometheus 대시보드 → OpenSearch Dashboards 마이그레이션
- [ ] 알림 규칙 마이그레이션
- [ ] 문서 업데이트 (운영 가이드)
- [ ] Prometheus 제거 (선택적)

---

## 🎯 Phase 5: 예상 효과

### 장점
1. **통합 관측성**: 로그, 메트릭, 트레이스를 OpenSearch 하나로 통합
2. **비용 절감**: Prometheus 별도 운영 불필요
3. **유연한 쿼리**: SQL 지원으로 러닝 커브 감소
4. **장기 저장**: 메트릭 1년 이상 보관 가능 (Prometheus는 30일 권장)
5. **상세 분석**: 원본 데이터 유지로 드릴다운 분석 용이

### 단점 및 대응
1. **쿼리 복잡도**: PromQL보다 복잡 → 사전 집계로 해결
2. **시계열 최적화 부족**: Prometheus 대비 느림 → 인덱스 최적화, ISM 활용
3. **러닝 커브**: OpenSearch DSL 학습 필요 → SQL 인터페이스 제공

---

## 📚 Phase 6: 참고 자료

### 관련 문서
- `.sprints/usecase-monitoring-targets-and-promql.md`: 기존 PromQL 쿼리 가이드
- `.sprints/sli-slo-sla-metrics-enhancement-plan.md`: SLI/SLO/SLA 정의

### OpenSearch 공식 문서
- Index Templates: https://opensearch.org/docs/latest/im-plugin/index-templates/
- Alerting: https://opensearch.org/docs/latest/observing-your-data/alerting/
- SQL: https://opensearch.org/docs/latest/search-plugins/sql/

---

## ✅ 구현 순서 요약

1. **Phase 1**: NuGet 패키지 추가
2. **Phase 2**: 설정 모델 및 클라이언트 래퍼 구현
3. **Phase 3**: 메트릭 문서 모델 구현
4. **Phase 4**: 사전 집계 프로세서 구현 (핵심)
5. **Phase 5**: UsecaseMetricsPipeline 통합
6. **Phase 6**: DI 등록 및 백그라운드 서비스
7. **Phase 7**: OpenSearch 인덱스 템플릿 생성
8. **Phase 8**: 단위 테스트 작성
9. **Phase 9**: 통합 테스트 및 검증
10. **Phase 10**: 대시보드 및 알림 설정

---

## 🧪 Phase 7: 검증 계획

### 버전 요구사항

검증은 다음 버전을 기준으로 수행합니다:

- **OpenSearch**: 3.4.0
- **OpenTelemetry Collector**: 최신 안정 버전 (공식 사이트 기준)
- **.NET**: 8.0 이상
- **xUnit**: 2.6.0 이상
- **Testcontainers**: 3.x

### 검증 전략 개요

메트릭 시스템의 정확성과 안정성을 보장하기 위해 3단계 검증을 수행합니다:

```
Layer 1: C# Application 검증
  ├─ 단위 테스트: 개별 컴포넌트 정확성
  └─ 통합 테스트: 전체 파이프라인 동작

Layer 2: OpenTelemetry Collector 검증
  ├─ 수신 검증: OTLP 엔드포인트 동작
  ├─ 처리 검증: Processor 파이프라인
  └─ 전송 검증: OpenSearch 연결

Layer 3: OpenSearch 검증
  ├─ 인덱스 검증: 데이터 저장 확인
  ├─ 쿼리 검증: 집계 및 조회
  └─ 대시보드 검증: 시각화 및 알림
```

---

### 7.1. Layer 1: C# Application 검증

#### 7.1.1. 단위 테스트 (Unit Tests)

**파일**: `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/OpenSearchMetricsProcessorTests.cs`

##### 테스트 1: MetricWindow - 동시성 안전성

**목적**: 다중 스레드에서 메트릭 기록 시 데이터 정확성 검증

```csharp
[Fact]
public async Task RecordRequest_WhenCalledConcurrently_ShouldCountAllRequests()
{
    // Arrange
    var window = new MetricWindow("command", "TestHandler");
    const int threadCount = 10;
    const int operationsPerThread = 1000;
    const int expectedTotal = threadCount * operationsPerThread;

    // Act
    var tasks = Enumerable.Range(0, threadCount)
        .Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < operationsPerThread; i++)
            {
                window.IncrementRequests();
            }
        }))
        .ToArray();

    await Task.WhenAll(tasks);

    // Assert
    window.RequestCount.Should().Be(expectedTotal);
}
```

##### 테스트 2: Percentile 계산 정확도

**목적**: Percentile 계산 알고리즘 정확성 검증

```csharp
[Theory]
[InlineData(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, 0.50, 3.0)] // P50 = 중앙값
[InlineData(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, 0.95, 5.0)] // P95 = 최댓값
[InlineData(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 }, 0.95, 10.0)] // P95
public void CalculatePercentile_WithKnownData_ShouldReturnCorrectValue(
    double[] durations,
    double percentile,
    double expected)
{
    // Arrange
    var window = new MetricWindow("command", "TestHandler");
    foreach (var duration in durations)
    {
        window.RecordDuration(duration);
    }

    // Act
    var result = window.CalculatePercentile(percentile);

    // Assert
    result.Should().BeApproximately(expected, 0.01);
}
```

##### 테스트 3: Rate 계산

**목적**: 60초 윈도우 기반 Rate 계산 정확성 검증

```csharp
[Fact]
public void CalculateAggregatedMetrics_WithKnownCounts_ShouldCalculateCorrectRates()
{
    // Arrange
    var processor = CreateProcessor();
    var window = new MetricWindow("command", "TestHandler");

    // 60초 동안 1200개 요청 = 20 RPS
    for (int i = 0; i < 1200; i++)
    {
        window.IncrementRequests();
    }

    // Act
    var aggregated = processor.CalculateAggregatedMetrics(window);

    // Assert
    aggregated.RequestRate.Should().BeApproximately(20.0, 0.01); // 1200 / 60 = 20
}
```

##### 테스트 4: 윈도우 리셋

**목적**: 60초 후 윈도우 리셋 시 데이터 초기화 검증

```csharp
[Fact]
public void Reset_AfterRecordingMetrics_ShouldClearAllData()
{
    // Arrange
    var window = new MetricWindow("command", "TestHandler");
    window.IncrementRequests();
    window.IncrementSuccess();
    window.RecordDuration(0.123);

    // Act
    window.Reset();

    // Assert
    window.RequestCount.Should().Be(0);
    window.SuccessCount.Should().Be(0);
    window.DurationCount.Should().Be(0);
    window.CalculatePercentile(0.95).Should().Be(0);
}
```

##### 테스트 5: OTLP Exporter 모킹

**목적**: OTLP 전송 로직 검증 (모킹)

```csharp
[Fact]
public async Task AggregateAndExport_WhenCalled_ShouldExportToOTLP()
{
    // Arrange
    var mockExporter = new Mock<IMetricsExporter>();
    var processor = CreateProcessorWithMockExporter(mockExporter.Object);

    processor.RecordRequest("TestHandler", "command");
    processor.RecordResponse("TestHandler", "command", true, null);
    processor.RecordDuration("TestHandler", "command", 0.123);

    // Act
    await processor.AggregateAndExport();

    // Assert
    mockExporter.Verify(
        x => x.Export(It.Is<Batch<Metric>>(
            batch => batch.Any(m => m.Name == "application.usecase.request_rate"))),
        Times.Once);
}
```

---

#### 7.1.2. 통합 테스트 (Integration Tests)

**파일**: `Tests/Functorium.Tests.Integration/AdaptersTests/Observabilities/OpenSearchMetricsIntegrationTests.cs`

##### 테스트 1: 전체 파이프라인 (E2E)

**목적**: UsecaseMetricsPipeline → OpenSearchMetricsProcessor → OTLP 전송 검증

```csharp
[Fact]
public async Task UsecaseMetricsPipeline_WhenProcessingCommand_ShouldRecordMetrics()
{
    // Arrange
    using var testServer = CreateTestServer(); // WebApplicationFactory
    var client = testServer.CreateClient();
    var metricsCollector = testServer.Services.GetRequiredService<TestMetricsCollector>();

    // Act
    var response = await client.PostAsync("/api/orders", CreateOrderRequest());
    await Task.Delay(100); // 메트릭 기록 대기

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var requestMetric = metricsCollector.GetMetric("application.usecase.requests");
    requestMetric.Should().NotBeNull();
    requestMetric.Tags["request.handler"].Should().Be("CreateOrderCommand");
    requestMetric.Tags["request.category.type"].Should().Be("command");
}
```

##### 테스트 2: 60초 윈도우 집계 검증

**목적**: 실제 시간 기반 집계 동작 검증 (시간 조작 가능한 환경)

```csharp
[Fact]
public async Task OpenSearchMetricsProcessor_After60Seconds_ShouldAggregateAndExport()
{
    // Arrange
    var fakeTimeProvider = new FakeTimeProvider();
    var processor = CreateProcessorWithFakeTime(fakeTimeProvider);
    var metricsCollector = new TestMetricsCollector();

    // Act: 0-59초 동안 메트릭 기록
    for (int i = 0; i < 1200; i++)
    {
        processor.RecordRequest("TestHandler", "command");
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(50)); // 시뮬레이션
    }

    // 60초 경과
    fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
    await Task.Delay(200); // 집계 완료 대기

    // Assert
    var rateMetric = metricsCollector.GetMetric("application.usecase.request_rate");
    rateMetric.Should().NotBeNull();
    rateMetric.Value.Should().BeApproximately(20.0, 0.1); // 1200 / 60 = 20 RPS
}
```

##### 테스트 3: OpenSearch 연결 검증 (Testcontainers)

**목적**: 실제 OpenSearch와 통신 검증 (Docker 기반)

```csharp
[Fact]
public async Task OpenSearchMetricsProcessor_WithRealOpenSearch_ShouldStoreMetrics()
{
    // Arrange
    await using var container = new OpenSearchBuilder()
        .WithImage("opensearchproject/opensearch:3.4.0")
        .WithPortBinding(9200, true)
        .Build();

    await container.StartAsync();

    var processor = CreateProcessorWithRealOpenSearch(container.GetConnectionString());

    // Act
    processor.RecordRequest("TestHandler", "command");
    processor.RecordDuration("TestHandler", "command", 0.123);
    await processor.AggregateAndExport();
    await Task.Delay(1000); // OpenSearch 인덱싱 대기

    // Assert
    var client = new OpenSearchClient(container.GetConnectionString());
    var searchResponse = await client.SearchAsync<dynamic>(s => s
        .Index("metrics-functorium-*")
        .Query(q => q.Term("handler.keyword", "TestHandler")));

    searchResponse.Documents.Should().NotBeEmpty();
}
```

##### 테스트 4: 에러 처리 검증

**목적**: 네트워크 오류, OpenSearch 장애 시 재시도 로직 검증

```csharp
[Fact]
public async Task OpenSearchMetricsProcessor_WhenOpenSearchUnavailable_ShouldRetry()
{
    // Arrange
    var mockExporter = new Mock<IMetricsExporter>();
    mockExporter
        .SetupSequence(x => x.Export(It.IsAny<Batch<Metric>>()))
        .Throws<HttpRequestException>() // 첫 번째 시도 실패
        .Throws<HttpRequestException>() // 두 번째 시도 실패
        .Returns(ExportResult.Success); // 세 번째 시도 성공

    var processor = CreateProcessorWithMockExporter(mockExporter.Object);

    // Act
    processor.RecordRequest("TestHandler", "command");
    await processor.AggregateAndExport();

    // Assert
    mockExporter.Verify(x => x.Export(It.IsAny<Batch<Metric>>()), Times.Exactly(3));
}
```

---

### 7.2. Layer 2: OpenTelemetry Collector 검증

#### 7.2.1. Collector 수신 검증

**목적**: OTLP 엔드포인트가 메트릭을 정상 수신하는지 확인

##### 방법 1: curl 명령으로 OTLP 전송 테스트

```bash
# OTLP/HTTP 엔드포인트 Health Check
curl -v http://localhost:4318/v1/metrics \
  -H "Content-Type: application/x-protobuf" \
  -d @test-metrics.pb

# 예상 응답: 200 OK
```

##### 방법 2: Collector Logs 확인

```bash
# Collector 로그에서 수신 확인
docker logs otel-collector 2>&1 | grep "metrics received"

# 예상 출력:
# 2026-01-07T10:15:00Z info MetricsExporter {"#metrics": 10}
```

##### 방법 3: Collector 자체 메트릭 확인

```bash
# Collector 자체 Prometheus 메트릭 확인
curl http://localhost:8888/metrics | grep otelcol_receiver_accepted_metric_points

# 예상 출력:
# otelcol_receiver_accepted_metric_points{receiver="otlp",transport="http"} 1200
```

---

#### 7.2.2. Collector 처리 검증

**목적**: Processor 파이프라인(batch, filter, memory_limiter)이 정상 동작하는지 확인

##### 방법 1: Logging Exporter 활성화

**config.yaml 수정**:
```yaml
exporters:
  logging:
    verbosity: detailed  # 상세 로깅 활성화
  elasticsearch:
    # ... 기존 설정 ...

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch, filter/metrics]
      exporters: [logging, elasticsearch]  # logging 추가
```

**로그 확인**:
```bash
docker logs otel-collector 2>&1 | grep "application.usecase"

# 예상 출력:
# Metric #0
# Name: application.usecase.request_rate
# Data type: Gauge
# Value: 20.0
# Attributes:
#   handler: TestHandler
#   cqrs: command
```

##### 방법 2: Batch Processor 검증

```bash
# 배치 크기 확인 (100건씩 묶였는지)
docker logs otel-collector 2>&1 | grep "batch_size"

# 예상 출력:
# Exporting batch of 100 metrics
```

##### 방법 3: Filter Processor 검증

**테스트 메트릭 전송** (필터링 대상):
```bash
# application.usecase.* 아닌 메트릭 전송
curl -X POST http://localhost:4318/v1/metrics \
  -H "Content-Type: application/json" \
  -d '{
    "resourceMetrics": [{
      "scopeMetrics": [{
        "metrics": [{
          "name": "system.cpu.usage",
          "gauge": { "dataPoints": [{ "asDouble": 50.0 }] }
        }]
      }]
    }]
  }'

# OpenSearch에서 확인 (필터링되어 저장 안 됨)
curl "http://localhost:9200/metrics-*/_search?q=system.cpu.usage"
# 예상 결과: "hits": { "total": { "value": 0 } }
```

---

#### 7.2.3. Collector 전송 검증

**목적**: Collector가 OpenSearch로 정상 전송하는지 확인

##### 방법 1: Elasticsearch Exporter 메트릭 확인

```bash
curl http://localhost:8888/metrics | grep otelcol_exporter_sent_metric_points

# 예상 출력:
# otelcol_exporter_sent_metric_points{exporter="elasticsearch"} 1200
```

##### 방법 2: 재시도 로직 검증

**OpenSearch 임시 중단**:
```bash
docker stop opensearch

# Collector 로그에서 재시도 확인
docker logs otel-collector 2>&1 | grep "retry"

# 예상 출력:
# 2026-01-07T10:16:00Z warn Exporting failed. Will retry the request after interval. {"error": "connection refused"}
# 2026-01-07T10:16:05Z info Retry attempt 1
# 2026-01-07T10:16:10Z info Retry attempt 2

# OpenSearch 재시작
docker start opensearch

# 성공 로그 확인
# 2026-01-07T10:16:30Z info Exporting succeeded after retry
```

---

### 7.3. Layer 3: OpenSearch 검증

#### 7.3.1. 인덱스 검증

**목적**: 메트릭 데이터가 OpenSearch에 정상 저장되는지 확인

##### 방법 1: 인덱스 목록 확인

```bash
# 인덱스 목록 조회
curl "http://localhost:9200/_cat/indices/metrics-*?v"

# 예상 출력:
# health status index                         pri rep docs.count
# yellow open   metrics-functorium-2026.01.07   1   1       1200
```

##### 방법 2: 문서 개수 확인

```bash
# 오늘 날짜 인덱스의 문서 수 확인
curl "http://localhost:9200/metrics-functorium-2026.01.07/_count"

# 예상 응답:
{
  "count": 1200,
  "_shards": { "total": 1, "successful": 1, "failed": 0 }
}
```

##### 방법 3: 샘플 문서 조회

```bash
# 최신 문서 1개 조회
curl -X GET "http://localhost:9200/metrics-functorium-*/_search?pretty" \
  -H 'Content-Type: application/json' \
  -d '{
    "size": 1,
    "sort": [{ "@timestamp": { "order": "desc" } }]
  }'

# 예상 응답:
{
  "hits": {
    "hits": [{
      "_source": {
        "@timestamp": "2026-01-07T10:15:00Z",
        "metric_name": "application.usecase.request_rate",
        "value": 20.0,
        "handler": "TestHandler",
        "cqrs": "command"
      }
    }]
  }
}
```

---

#### 7.3.2. 쿼리 검증 (집계 정확성)

**목적**: 저장된 메트릭으로 Rate, Percentile 쿼리가 정상 동작하는지 확인

##### 쿼리 1: Request Rate 조회

```bash
curl -X POST "http://localhost:9200/metrics-functorium-*/_search?pretty" \
  -H 'Content-Type: application/json' \
  -d '{
    "size": 0,
    "query": {
      "bool": {
        "must": [
          { "term": { "metric_name.keyword": "application.usecase.request_rate" } },
          { "term": { "handler.keyword": "TestHandler" } },
          { "range": { "@timestamp": { "gte": "now-5m" } } }
        ]
      }
    },
    "aggs": {
      "avg_rate": { "avg": { "field": "value" } },
      "max_rate": { "max": { "field": "value" } }
    }
  }'

# 예상 응답:
{
  "aggregations": {
    "avg_rate": { "value": 20.5 },
    "max_rate": { "value": 25.0 }
  }
}
```

##### 쿼리 2: Latency P95 조회

```bash
curl -X POST "http://localhost:9200/metrics-functorium-*/_search?pretty" \
  -H 'Content-Type: application/json' \
  -d '{
    "size": 0,
    "query": {
      "bool": {
        "must": [
          { "term": { "metric_name.keyword": "application.usecase.latency_p95" } },
          { "range": { "@timestamp": { "gte": "now-1h" } } }
        ]
      }
    },
    "aggs": {
      "by_handler": {
        "terms": { "field": "handler.keyword" },
        "aggs": {
          "avg_p95": { "avg": { "field": "value" } }
        }
      }
    }
  }'

# 예상 응답:
{
  "aggregations": {
    "by_handler": {
      "buckets": [
        { "key": "CreateOrderCommand", "avg_p95": { "value": 450.0 } },
        { "key": "GetOrderQuery", "avg_p95": { "value": 120.0 } }
      ]
    }
  }
}
```

##### 쿼리 3: Saturation 조회 (복합 메트릭)

```bash
curl -X POST "http://localhost:9200/metrics-functorium-*/_search?pretty" \
  -H 'Content-Type: application/json' \
  -d '{
    "size": 0,
    "query": {
      "bool": {
        "must": [
          { "term": { "metric_name.keyword": "application.usecase.saturation" } },
          { "range": { "value": { "gte": 70 } } }  // 포화도 70% 이상
        ]
      }
    },
    "aggs": {
      "high_saturation_handlers": {
        "terms": { "field": "handler.keyword", "size": 10 }
      }
    }
  }'
```

---

#### 7.3.3. 대시보드 검증

**목적**: OpenSearch Dashboards에서 시각화 및 알림이 정상 동작하는지 확인

##### 1단계: Discover 페이지 검증

1. OpenSearch Dashboards 접속: `http://localhost:5601`
2. **Discover** 메뉴 클릭
3. 인덱스 패턴 생성: `metrics-functorium-*`
4. 시간 필드: `@timestamp`
5. 최근 15분 데이터 조회
6. **검증 항목**:
   - ✅ 메트릭 문서가 조회되는가?
   - ✅ `metric_name`, `handler`, `value` 필드가 보이는가?
   - ✅ 타임스탬프가 정확한가?

##### 2단계: Visualization 생성

**시각화 1: Request Rate 그래프 (Line Chart)**

```json
{
  "title": "Request Rate by Handler",
  "visState": {
    "type": "line",
    "params": {
      "addLegend": true,
      "addTooltip": true
    },
    "aggs": [
      {
        "type": "avg",
        "schema": "metric",
        "params": { "field": "value" }
      },
      {
        "type": "date_histogram",
        "schema": "segment",
        "params": { "field": "@timestamp", "interval": "1m" }
      },
      {
        "type": "terms",
        "schema": "group",
        "params": { "field": "handler.keyword", "size": 10 }
      }
    ]
  }
}
```

**검증**: X축(시간), Y축(Rate), Handler별 색상으로 구분된 그래프 표시

**시각화 2: Latency Heatmap**

```json
{
  "title": "Latency P95 Heatmap",
  "visState": {
    "type": "heatmap",
    "aggs": [
      {
        "type": "avg",
        "schema": "metric",
        "params": { "field": "value" }
      },
      {
        "type": "date_histogram",
        "schema": "segment",
        "params": { "field": "@timestamp", "interval": "5m" }
      },
      {
        "type": "terms",
        "schema": "group",
        "params": { "field": "handler.keyword" }
      }
    ]
  }
}
```

**검증**: Handler × 시간 축의 히트맵, 색상으로 레이턴시 표시

##### 3단계: Dashboard 구성

**대시보드 레이아웃**:

```
┌─────────────────────────────────────────────────────┐
│ Functorium Usecase Monitoring Dashboard            │
├─────────────────────────────────────────────────────┤
│ [Request Rate Line Chart]    [Error Rate Gauge]    │
│                                                      │
│ [Latency P95 Heatmap]         [Saturation Gauge]   │
│                                                      │
│ [Top 10 Slowest Handlers]     [Recent Errors]      │
└─────────────────────────────────────────────────────┘
```

**검증**:
- ✅ 모든 차트가 실시간 업데이트되는가?
- ✅ 시간 범위 변경 시 데이터가 갱신되는가?
- ✅ 필터 적용 시 모든 차트가 동기화되는가?

##### 4단계: 알림(Alerting) 검증

**알림 1: High Latency Alert**

```json
{
  "name": "High Latency P95 Alert",
  "type": "monitor",
  "enabled": true,
  "schedule": { "period": { "interval": 1, "unit": "MINUTES" } },
  "inputs": [{
    "search": {
      "indices": ["metrics-functorium-*"],
      "query": {
        "bool": {
          "must": [
            { "term": { "metric_name.keyword": "application.usecase.latency_p95" } },
            { "range": { "@timestamp": { "gte": "now-5m" } } }
          ]
        }
      },
      "aggregations": {
        "by_handler": {
          "terms": { "field": "handler.keyword" },
          "aggs": {
            "max_latency": { "max": { "field": "value" } }
          }
        }
      }
    }
  }],
  "triggers": [{
    "name": "Latency exceeds 500ms",
    "severity": "2",
    "condition": {
      "script": {
        "source": "ctx.results[0].aggregations.by_handler.buckets.stream().anyMatch(b -> b.max_latency.value > 500)"
      }
    },
    "actions": [{
      "name": "Slack Notification",
      "destination_id": "slack-channel",
      "message_template": {
        "source": "Handler {{ctx.results[0].aggregations.by_handler.buckets[0].key}} has high latency: {{ctx.results[0].aggregations.by_handler.buckets[0].max_latency.value}}ms"
      }
    }]
  }]
}
```

**검증 방법**:
1. 부하 테스트로 레이턴시 임계값 초과 유발
2. 1분 이내 알림 발생 확인
3. Slack/Email에 알림 수신 확인

**알림 2: High Saturation Alert**

```json
{
  "name": "High Saturation Alert",
  "triggers": [{
    "condition": {
      "script": {
        "source": "ctx.results[0].hits.hits[0]._source.value > 80"
      }
    }
  }]
}
```

---

### 7.4. 통합 검증 시나리오

#### 시나리오 1: 정상 동작 검증

**목적**: 전체 파이프라인이 정상 동작하는지 E2E 검증

```bash
# 1. C# Application 시작
dotnet run --project Src/YourApp

# 2. 부하 생성 (1분간 요청 전송)
for i in {1..60}; do
  curl -X POST http://localhost:5000/api/orders \
    -H "Content-Type: application/json" \
    -d '{"productId": 1, "quantity": 1}'
  sleep 1
done

# 3. 60초 후 OpenSearch에서 확인
sleep 65

curl "http://localhost:9200/metrics-functorium-*/_search?pretty" \
  -H 'Content-Type: application/json' \
  -d '{
    "query": {
      "bool": {
        "must": [
          { "term": { "metric_name.keyword": "application.usecase.request_rate" } },
          { "term": { "handler.keyword": "CreateOrderCommand" } }
        ]
      }
    },
    "size": 1,
    "sort": [{ "@timestamp": "desc" }]
  }'

# 예상 결과:
# - Request Rate: ~1.0 RPS (60 requests / 60 seconds)
# - Latency P95: < 200ms (정상 범위)
# - Error Rate: 0 (에러 없음)
```

---

#### 시나리오 2: 정확도 검증 (Prometheus 비교)

**목적**: OpenSearch 메트릭이 기존 Prometheus 메트릭과 일치하는지 확인

```bash
# 1. Prometheus와 OpenSearch 동시 수집 (병행 운영)
# appsettings.json에서 두 Exporter 모두 활성화

# 2. 1시간 데이터 수집 후 비교
# Prometheus 쿼리
curl "http://localhost:9090/api/v1/query" \
  --data-urlencode 'query=rate(application_usecase_requests_total{handler="CreateOrderCommand"}[5m])'

# OpenSearch 쿼리
curl -X POST "http://localhost:9200/metrics-functorium-*/_search" \
  -d '{
    "query": { "term": { "handler.keyword": "CreateOrderCommand" } },
    "aggs": { "avg_rate": { "avg": { "field": "value" } } }
  }'

# 3. 값 비교 (허용 오차: ±5%)
# - Prometheus Rate: 20.0 RPS
# - OpenSearch Rate: 20.1 RPS (오차 0.5% ✅)
```

---

#### 시나리오 3: 장애 복구 검증

**목적**: 네트워크 장애 시 재시도 및 데이터 유실 방지 검증

```bash
# 1. OpenSearch 중단
docker stop opensearch

# 2. 1분간 요청 전송 (메트릭 수집 계속됨)
for i in {1..60}; do
  curl -X POST http://localhost:5000/api/orders
  sleep 1
done

# 3. OTel Collector 로그에서 재시도 확인
docker logs otel-collector 2>&1 | grep -A 5 "retry"

# 4. OpenSearch 재시작
docker start opensearch
sleep 30

# 5. 데이터 유실 확인 (재시도로 전송 성공했는지)
curl "http://localhost:9200/metrics-functorium-*/_count"

# 예상 결과: count >= 60 (유실 없음 ✅)
```

---

### 7.5. 검증 체크리스트

#### ✅ C# Application 검증

- [ ] **단위 테스트**
  - [ ] MetricWindow 동시성 테스트 통과
  - [ ] Percentile 계산 정확도 테스트 통과
  - [ ] Rate 계산 정확도 테스트 통과
  - [ ] 윈도우 리셋 테스트 통과
  - [ ] OTLP Exporter 모킹 테스트 통과

- [ ] **통합 테스트**
  - [ ] UsecaseMetricsPipeline E2E 테스트 통과
  - [ ] 60초 윈도우 집계 테스트 통과
  - [ ] Testcontainers 기반 OpenSearch 연동 테스트 통과
  - [ ] 에러 처리 및 재시도 테스트 통과

- [ ] **코드 커버리지**
  - [ ] OpenSearchMetricsProcessor: 80% 이상
  - [ ] MetricWindow: 90% 이상
  - [ ] UsecaseMetricsPipeline 통합: 70% 이상

---

#### ✅ OpenTelemetry Collector 검증

- [ ] **수신 검증**
  - [ ] OTLP/HTTP 엔드포인트 응답 확인 (200 OK)
  - [ ] Collector 로그에서 메트릭 수신 확인
  - [ ] Collector 자체 메트릭에서 수신 카운트 확인

- [ ] **처리 검증**
  - [ ] Logging Exporter로 메트릭 출력 확인
  - [ ] Batch Processor 배치 크기 확인 (100건)
  - [ ] Filter Processor 필터링 동작 확인
  - [ ] Memory Limiter 메모리 제한 확인

- [ ] **전송 검증**
  - [ ] Elasticsearch Exporter 전송 메트릭 확인
  - [ ] 재시도 로직 동작 확인 (OpenSearch 장애 시)
  - [ ] 지수 백오프 재시도 간격 확인

---

#### ✅ OpenSearch 검증

- [ ] **인덱스 검증**
  - [ ] 인덱스 생성 확인 (`metrics-functorium-*`)
  - [ ] 문서 개수 확인 (기대값과 일치)
  - [ ] 샘플 문서 조회 및 스키마 확인

- [ ] **쿼리 검증**
  - [ ] Request Rate 쿼리 정확성 확인
  - [ ] Latency P95 쿼리 정확성 확인
  - [ ] Saturation 쿼리 정확성 확인
  - [ ] Handler별 집계 쿼리 확인

- [ ] **대시보드 검증**
  - [ ] Discover 페이지에서 메트릭 조회 확인
  - [ ] Request Rate Line Chart 정상 표시
  - [ ] Latency Heatmap 정상 표시
  - [ ] Dashboard 실시간 업데이트 확인

- [ ] **알림 검증**
  - [ ] High Latency Alert 발생 확인
  - [ ] High Saturation Alert 발생 확인
  - [ ] Slack/Email 알림 수신 확인

---

#### ✅ 통합 시나리오 검증

- [ ] **정상 동작 E2E 검증**
  - [ ] 1분간 요청 → 60초 후 OpenSearch에 데이터 확인
  - [ ] Rate, Percentile, Saturation 값 정확성 확인

- [ ] **정확도 검증 (Prometheus 비교)**
  - [ ] 병행 운영 1시간 후 데이터 비교
  - [ ] 오차 범위 5% 이내 확인

- [ ] **장애 복구 검증**
  - [ ] OpenSearch 중단 → 재시도 동작 확인
  - [ ] OpenSearch 재시작 → 데이터 유실 없음 확인

---

### 7.6. 검증 도구 및 환경

#### 필수 도구

| 도구 | 용도 | 설치 방법 |
|------|------|-----------|
| **xUnit** | C# 단위/통합 테스트 | NuGet: `xunit`, `xunit.runner.visualstudio` |
| **FluentAssertions** | 테스트 Assertion | NuGet: `FluentAssertions` |
| **Moq** | Mocking 프레임워크 | NuGet: `Moq` |
| **Testcontainers** | Docker 기반 통합 테스트 | NuGet: `Testcontainers` |
| **curl** | HTTP 요청 테스트 | 기본 설치 (Linux/Mac), Windows: `choco install curl` |
| **jq** | JSON 파싱 | `apt install jq` (Linux), `brew install jq` (Mac) |
| **k6** | 부하 테스트 (선택) | https://k6.io/docs/getting-started/installation/ |

#### 테스트 환경 구성

**Docker Compose for Testing**:
```yaml
version: '3.8'

services:
  opensearch-test:
    image: opensearchproject/opensearch:3.4.0
    environment:
      - discovery.type=single-node
      - OPENSEARCH_JAVA_OPTS=-Xms512m -Xmx512m
    ports:
      - "9201:9200"  # 테스트용 포트

  otel-collector-test:
    image: otel/opentelemetry-collector-contrib:latest
    volumes:
      - ./otel-collector-test-config.yaml:/etc/otelcol/config.yaml
    ports:
      - "4319:4318"  # 테스트용 포트
```

---

**다음 단계**: 이 검증 계획에 따라 단위 테스트부터 시작하여 단계별로 검증을 수행합니다.
