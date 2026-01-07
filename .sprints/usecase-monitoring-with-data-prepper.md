# OpenSearch Data Prepper 기반 Usecase 모니터링 시스템 구축 계획

**작성일**: 2026-01-07
**최종 업데이트**: 2026-01-07
**목표**: OpenTelemetry Collector 대신 OpenSearch Data Prepper를 사용한 메트릭 파이프라인 구축
**범위**: C# 사전 집계 → Data Prepper → OpenSearch 아키텍처
**대안 아키텍처**: OpenSearch 생태계 통합 솔루션
**공식 문서 기반**: [OpenSearch Data Prepper Documentation](https://docs.opensearch.org/latest/data-prepper/)

---

## 📋 개요

### Data Prepper란?

**OpenSearch Data Prepper**는 OpenSearch의 공식 서버 사이드 데이터 수집기로, 다운스트림 분석 및 시각화를 위해 데이터를 필터링, 보강, 변환, 정규화 및 집계할 수 있는 OpenSearch의 권장 데이터 수집 도구입니다.

> **공식 정의**: "OpenSearch Data Prepper is a server-side data collector capable of filtering, enriching, transforming, normalizing, and aggregating data for downstream analysis and visualization, and is the preferred data ingestion tool for OpenSearch."
> 출처: [OpenSearch Data Prepper Documentation](https://docs.opensearch.org/latest/data-prepper/)

**핵심 특징**:
- ✅ **OpenSearch 네이티브**: OpenSearch 팀에서 직접 개발 및 유지보수
- ✅ **OTLP 지원**: OpenTelemetry Protocol 완벽 지원 (Metrics, Traces, Logs)
- ✅ **관측성 통합**: 트레이스 분석 및 로그 분석에 최적화된 두 가지 주요 사용 사례
- ✅ **확장 가능**: 플러그인 아키텍처로 커스텀 프로세서 추가 가능
- ✅ **단일 생태계**: OpenSearch Dashboards, Alerting, ISM과 완벽 통합
- ✅ **독립 컴포넌트**: OpenSearch 플러그인이 아닌 독립 실행형 서비스

### OTel Collector vs Data Prepper 핵심 비교

| 항목 | OpenTelemetry Collector | OpenSearch Data Prepper |
|------|-------------------------|------------------------|
| **개발사** | CNCF (Cloud Native) | AWS/OpenSearch |
| **주 용도** | 범용 텔레메트리 수집 | OpenSearch 데이터 수집 |
| **강점** | 다중 백엔드 지원 | OpenSearch 네이티브 통합 |
| **OTLP 지원** | ✅ 완벽 지원 (4318 포트) | ✅ 완벽 지원 (21891 포트) |
| **메트릭 처리** | ✅ 우수 | ⚠️ 제한적 (트레이스 중심) |
| **배치 처리** | ✅ batch processor | ✅ bulk_size 설정 |
| **커뮤니티** | 매우 크고 활발 | OpenSearch 중심 |
| **확장성** | 수백 개 익스포터 | OpenSearch 중심 익스포터 |
| **언어** | Go (경량, 빠름) | Java (JVM, 더 많은 메모리) |
| **문서** | [opentelemetry.io](https://opentelemetry.io/docs/collector/) | [docs.opensearch.org](https://docs.opensearch.org/latest/data-prepper/) |

### 주요 설정 차이

| 설정 항목 | OTel Collector | Data Prepper |
|----------|----------------|--------------|
| **Endpoint 포트** | `4318` (OTLP/HTTP 표준) | `21891` (otel_metrics_source 기본값) |
| **설정 파일** | `config.yaml` (단일) | `data-prepper-config.yaml` + `pipelines.yaml` |
| **배치 처리** | `batch` processor | `bulk_size` + `flush_timeout` |
| **메모리 제한** | `memory_limiter` processor | `circuit_breakers` (JVM 힙) |
| **필터링** | `filter` processor | `drop_events` processor |
| **DLQ** | ❌ 없음 | ✅ `dlq_file` 설정 |

---

## 🎯 아키텍처 설계

### 전체 아키텍처

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
│  │         OpenSearchMetricsProcessor (사전 집계)              │ │
│  │                                                               │ │
│  │  [집계 로직]                                                 │ │
│  │  1. 60초 윈도우에 메트릭 수집                               │ │
│  │  2. Rate 계산 (requests/second)                             │ │
│  │  3. Percentile 계산 (P50, P95, P99)                        │ │
│  │  4. Saturation 계산 (복합 포화도)                           │ │
│  │                                                               │ │
│  │  [출력]                                                       │ │
│  │  - Meter API로 집계된 메트릭 기록                           │ │
│  └──────────────────┬───────────────────────────────────────────┘ │
│                     │                                              │
│                     │ OTLP/HTTP (Protobuf)                        │
└─────────────────────┼──────────────────────────────────────────────┘
                      │
                      ▼
┌──────────────────────────────────────────────────────────────────┐
│                  OpenSearch Data Prepper                          │
│                                                                    │
│  [Source]                                                          │
│  └─ otel_metrics_source                                           │
│      - HTTP: /opentelemetry.proto.collector.metrics.v1...        │
│      - Port: 21890                                                │
│                                                                    │
│  [Processors]                                                      │
│  ├─ service_map (선택)                                            │
│  ├─ aggregate                                                      │
│  │   └─ 추가 집계 (선택적)                                       │
│  └─ drop_events (필터링)                                          │
│                                                                    │
│  [Sinks]                                                           │
│  └─ opensearch                                                     │
│      - Index: metrics-functorium-%{yyyy.MM.dd}                   │
│      - Bulk: 100건씩                                              │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   │ HTTP/JSON (Bulk API)
                   ▼
┌──────────────────────────────────────────────────────────────────┐
│                      OpenSearch Cluster                           │
│                                                                    │
│  [Indices]                                                         │
│  └─ metrics-functorium-{yyyy.MM.dd}                              │
│      └─ 사전 집계된 메트릭                                       │
│                                                                    │
│  [Features]                                                        │
│  ├─ OpenSearch Dashboards (시각화)                               │
│  ├─ Alerting (SLO 위반 알림)                                     │
│  └─ ISM (Index Lifecycle Management)                              │
└──────────────────────────────────────────────────────────────────┘
```

---

## 📝 구현 계획

### 1. Data Prepper 설정

#### 1.1. Data Prepper 설치

**Docker Compose 방식**:

```yaml
version: '3.8'

services:
  # Functorium Application
  functorium-app:
    build: .
    environment:
      # Data Prepper OTel Metrics Source 기본 포트: 21891
      - OpenTelemetry__OtlpEndpoint=http://data-prepper:21891
      - OpenTelemetry__EnableOpenSearchMetrics=true
    depends_on:
      - data-prepper
    networks:
      - observability

  # OpenSearch Data Prepper
  data-prepper:
    image: opensearchproject/data-prepper:latest
    container_name: data-prepper
    volumes:
      - ./data-prepper-config.yaml:/usr/share/data-prepper/config/data-prepper-config.yaml
      - ./pipelines.yaml:/usr/share/data-prepper/pipelines/pipelines.yaml
    ports:
      - "21891:21891"  # OTLP Metrics HTTP (공식 기본값)
      - "21892:21892"  # OTLP Traces HTTP (선택)
      - "4900:4900"    # Data Prepper Server API (Health Check)
      - "2021:2021"    # Peer Forwarder (클러스터링, 선택)
    depends_on:
      - opensearch
    networks:
      - observability
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:4900/health"]
      interval: 10s
      timeout: 5s
      retries: 5

  # OpenSearch
  opensearch:
    image: opensearchproject/opensearch:latest
    container_name: opensearch
    environment:
      - discovery.type=single-node
      - OPENSEARCH_JAVA_OPTS=-Xms1g -Xmx1g
      - DISABLE_SECURITY_PLUGIN=true  # 개발용 (프로덕션에서는 제거)
    ports:
      - "9200:9200"
      - "9600:9600"
    volumes:
      - opensearch-data:/usr/share/opensearch/data
    networks:
      - observability
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9200"]
      interval: 10s
      timeout: 5s
      retries: 5

  # OpenSearch Dashboards
  opensearch-dashboards:
    image: opensearchproject/opensearch-dashboards:latest
    container_name: opensearch-dashboards
    ports:
      - "5601:5601"
    environment:
      - OPENSEARCH_HOSTS=http://opensearch:9200
      - DISABLE_SECURITY_DASHBOARDS_PLUGIN=true
    depends_on:
      - opensearch
    networks:
      - observability

volumes:
  opensearch-data:

networks:
  observability:
    driver: bridge
```

---

#### 1.2. Data Prepper Configuration

**파일**: `data-prepper-config.yaml`

```yaml
ssl: false

# Data Prepper 서버 설정
server:
  port: 4900
  health_check: true

# Circuit Breaker 설정
circuit_breakers:
  heap:
    usage: 0.7
    reset: 30s
```

---

#### 1.3. Pipeline Configuration

**파일**: `pipelines.yaml`

> **참고**: 공식 문서 [OTel metrics source](https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/sources/otel-metrics-source/)

```yaml
# Metrics Pipeline
metrics-pipeline:
  # Source: OTLP Metrics 수신
  source:
    otel_metrics_source:
      # 기본 네트워크 설정
      # 공식 문서: https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/sources/otel-metrics-source/
      port: 21891  # 공식 기본값 (21890이 아님)

      # 타임아웃 설정 (밀리초)
      request_timeout: 10000  # 기본값: 10000ms

      # 스레드 풀 설정
      thread_count: 200  # 기본값: 200

      # 연결 제한
      max_connection_count: 500  # 기본값: 500

      # 최대 요청 크기
      max_request_length: 10mb  # 기본값: 10mb (ByteCount 타입)

      # 출력 포맷 (otel | opensearch)
      # output_format: otel  # OpenTelemetry 포맷 유지 시

      # SSL/TLS 설정 (프로덕션 권장)
      ssl: false  # 개발 환경
      # ssl: true
      # sslKeyCertChainFile: "/path/to/cert.pem"  # 파일 경로 또는 S3 경로
      # sslKeyFile: "/path/to/key.pem"

      # ACM 인증서 사용 (AWS 환경)
      # useAcmCertForSSL: false
      # acmCertificateArn: "arn:aws:acm:..."
      # awsRegion: "us-east-1"

      # gRPC 서비스 옵션
      health_check_service: false  # 기본값: false
      proto_reflection_service: false  # 기본값: false

      # HTTP 기본 인증 (선택)
      # authentication:
      #   http_basic:
      #     username: "admin"
      #     password: "${DATA_PREPPER_PASSWORD}"

  # Processors: 메트릭 변환 및 필터링
  # 공식 문서: https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/processors/
  processor:
    # 1. OTel Metrics Raw Processor (메트릭 정규화)
    # 공식 문서: https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/processors/otel-metrics/
    - otel_metrics_raw_processor:
        # 기본 설정으로 충분 (Application에서 이미 사전 집계 완료)

    # 2. Drop Events (필터링)
    # application.usecase.* 메트릭만 통과
    - drop_events:
        drop_when: 'getMetadata("attributes[metric_name]") !~ /^application\.usecase\..*/'

    # 3. Mutate String (속성 추가)
    - mutate_string:
        entries:
          - set:
              key: "environment"
              value: "production"
          - set:
              key: "service_name"
              value: "functorium"

  # Sink: OpenSearch로 전송
  # 공식 문서: https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/sinks/opensearch/
  sink:
    - opensearch:
        # OpenSearch 엔드포인트 (필수)
        hosts:
          - "https://opensearch:9200"

        # 인증 (프로덕션 권장)
        username: "admin"
        password: "${OPENSEARCH_PASSWORD}"  # 환경 변수 사용

        # AWS IAM 인증 (AWS OpenSearch Service)
        # aws_sigv4: true
        # aws_region: "us-east-1"

        # SSL 설정
        insecure: true  # 개발 환경 (프로덕션에서는 false + cert 설정)
        # cert: "/path/to/ca-cert.pem"

        # 인덱스 설정
        index_type: "custom"  # 기본값: custom
        index: "metrics-functorium-%{yyyy.MM.dd}"  # Java 날짜 패턴
        # 또는 필드 참조: "metrics-${/service_name}-%{yyyy.MM.dd}"

        # Document ID 설정 (선택)
        # document_id: "${getMetadata(\"document_id\")}"

        # 벌크 설정 (성능 튜닝)
        bulk_size: 5  # 기본값: 5 MiB (최대 요청 크기)
        flush_timeout: 60000  # 기본값: 60000ms (1분)

        # 재시도 설정
        max_retries: 5  # 지수 백오프로 재시도
        # max_retries 미설정 시 무한 재시도

        # 네트워크 타임아웃
        socket_timeout: 30000  # 밀리초
        connect_timeout: 5000  # 밀리초

        # DLQ (Dead Letter Queue) - 실패한 이벤트 저장
        dlq_file: "/tmp/data-prepper/dlq/metrics-dlq-%{yyyy.MM.dd}.log"

        # 압축 (기본값: true, ES6 제외)
        enable_request_compression: true

        # 벌크 액션 타입
        action: "index"  # create | index | update | upsert | delete

        # 인덱스 템플릿 (선택)
        template_type: "index-template"  # v1 (레거시) | index-template (composable)
        template_file: "/path/to/index-template.json"
        # 또는 인라인 설정
        # template_content: |
        #   {
        #     "template": {
        #       "settings": {
        #         "number_of_shards": 3,
        #         "number_of_replicas": 1,
        #         "index.lifecycle.name": "metrics-policy",
        #         "refresh_interval": "30s"
        #       },
        #       "mappings": {
        #         "properties": {
        #           "@timestamp": { "type": "date" },
        #           "handler": { "type": "keyword" },
        #           "cqrs": { "type": "keyword" },
        #           "value": { "type": "double" }
        #         }
        #       }
        #     }
        #   }

# Traces Pipeline (선택적 - 트레이스도 수집하려면)
# traces-pipeline:
#   source:
#     otel_trace_source:
#       port: 21891
#
#   processor:
#     - otel_trace_raw:
#     - service_map:
#
#   sink:
#     - opensearch:
#         hosts:
#           - "https://opensearch:9200"
#         index: "traces-%{yyyy.MM.dd}"
```

---

### 2. Application 설정

**파일**: `appsettings.json`

```json
{
  "OpenTelemetry": {
    "ServiceName": "Functorium",
    "ServiceVersion": "1.0.0",

    // Data Prepper 엔드포인트
    // 포트: 21891 (otel_metrics_source 공식 기본값)
    "OtlpEndpoint": "http://data-prepper:21891",

    "EnableMetrics": true,
    "AggregationWindowSeconds": 60
  }
}
```

> **중요**: Application 코드는 OTel Collector 사용 시와 **동일**합니다. OTLP 프로토콜은 표준이므로 Endpoint만 변경하면 됩니다.
>
> **포트 차이**:
> - OpenTelemetry Collector: `4318` (OTLP/HTTP 기본값)
> - Data Prepper: `21891` (otel_metrics_source 기본값)

---

## 🔍 OTel Collector vs Data Prepper 비교

### 기능 비교

| 기능 | OTel Collector | Data Prepper |
|------|----------------|--------------|
| **OTLP 수신** | ✅ HTTP/gRPC | ✅ HTTP (gRPC 제한적) |
| **메트릭 필터링** | ✅ filter processor | ✅ drop_events processor |
| **배치 처리** | ✅ batch processor | ✅ bulk_size 설정 |
| **메모리 제한** | ✅ memory_limiter | ✅ circuit_breakers |
| **재시도** | ✅ exporters 설정 | ✅ max_retries 설정 |
| **OpenSearch 통합** | ⚠️ elasticsearch exporter | ✅ 네이티브 opensearch sink |
| **DLQ (실패 처리)** | ❌ 없음 | ✅ dlq 설정 |
| **트레이스 처리** | ✅ 우수 | ✅✅ 매우 우수 (최적화) |
| **메트릭 집계** | ⚠️ 제한적 | ⚠️ 제한적 (aggregate processor) |
| **다중 백엔드** | ✅ Prometheus, Jaeger 등 | ❌ OpenSearch 중심 |
| **커뮤니티** | ✅✅ 매우 활발 | ⚠️ OpenSearch 중심 |

---

### 언제 Data Prepper를 선택할까?

#### ✅ Data Prepper를 선택하는 경우

1. **OpenSearch 단일 백엔드**
   - Prometheus, Jaeger 등 다른 백엔드 불필요
   - OpenSearch만 사용하는 환경

2. **트레이스 중심 관측성**
   - 분산 트레이스가 주요 관심사
   - Service Map, Dependency 분석 필요

3. **OpenSearch 생태계 통합**
   - OpenSearch Dashboards 주로 사용
   - ISM, Alerting 등 OpenSearch 기능 활용

4. **AWS 환경**
   - AWS OpenSearch Service 사용
   - AWS 인프라와 통합

5. **DLQ 필요**
   - 실패한 이벤트를 별도 저장해야 함
   - 데이터 유실 방지가 중요

#### ❌ OTel Collector를 선택하는 경우

1. **다중 백엔드**
   - Prometheus, Grafana 병행 사용
   - 여러 관측성 도구 통합

2. **표준 준수 중요**
   - CNCF 표준 선호
   - 벤더 독립성 중요

3. **풍부한 익스포터**
   - Kafka, InfluxDB 등 다양한 백엔드
   - 복잡한 파이프라인 필요

4. **메트릭 중심**
   - 트레이스보다 메트릭이 주요 관심사
   - 메트릭 변환/집계 많이 필요

5. **커뮤니티 지원**
   - 활발한 커뮤니티 필요
   - 많은 레퍼런스와 예제

---

## 📊 성능 및 리소스 비교

| 항목 | OTel Collector | Data Prepper |
|------|----------------|--------------|
| **메모리 사용** | ~256MB | ~512MB (JVM) |
| **CPU 사용** | 낮음 (Go) | 중간 (Java) |
| **시작 시간** | 빠름 (~1초) | 중간 (~5-10초) |
| **처리량** | 높음 | 중간-높음 |
| **레이턴시** | 낮음 | 낮음-중간 |

**결론**: OTel Collector가 리소스 효율적이지만, Data Prepper도 프로덕션 워크로드에 충분합니다.

---

## 📈 메트릭 수집 Best Practices

> **출처**: [Metrics Ingestion with Data Prepper using OpenTelemetry](https://opensearch.org/blog/opentelemetry-metrics-for-data-prepper/)

### 지원되는 메트릭 타입

Data Prepper 1.4.0+는 다음 OpenTelemetry 메트릭 타입을 지원합니다:
- ✅ **Sum**: 누적 카운터 (requests, responses)
- ✅ **Gauge**: 순간 값 (CPU usage, memory)
- ✅ **Histogram**: 분포 데이터 (latency, request size)
- ✅ **ExponentialHistogram**: 고급 분포 (최신 버전)
- ✅ **Summary**: 사전 계산된 백분위수

**데이터 저장 방식**: Data Prepper는 각 메트릭 데이터 포인트를 개별 OpenSearch 문서로 저장합니다.

### 성능 고려사항

#### ⚠️ 리소스 효율성 트레이드오프

**OpenSearch는 시계열 DB가 아님**:
- 메트릭 저장에 전문 시계열 DB보다 더 많은 리소스 필요
- 장기 보관을 위해 인덱스 롤업 및 보존 정책 구현 필요

**권장 사항**:
```yaml
# OpenSearch 인덱스 설정 예시
{
  "settings": {
    "refresh_interval": "30s",  # 기본 1초 → 30초로 완화
    "number_of_shards": 3,
    "number_of_replicas": 1,
    "index.lifecycle.name": "metrics-7-days-retention"
  }
}
```

#### ✅ 고카디널리티 장점

**High-Cardinality Metrics 처리 우수**:
- 고유 속성 값이 많은 메트릭에서 OpenSearch 우위
- 전문 시계열 DB와 달리 카디널리티로 인한 성능 저하 최소

**적합한 사례**:
- 고객별 서비스 키가 많은 경우
- 다중 테넌트 환경
- 동적으로 생성되는 태그가 많은 경우

### 프로덕션 권장 사항

1. **이벤트 속도 모니터링**
   - Data Prepper 자체 메트릭 활성화
   - Circuit breaker 설정으로 메모리 보호
   ```yaml
   circuit_breakers:
     heap:
       usage: 0.7  # 힙 사용률 70% 초과 시 차단
       reset: 30s
   ```

2. **카디널리티 계획**
   - 프로젝트 초기부터 카디널리티 확장 고려
   - 불필요한 태그 제거

3. **인덱스 보존 정책**
   - ISM (Index State Management) 정책 구성
   - 예: 7일 후 warm tier 이동, 30일 후 삭제

4. **로그/트레이스 상관관계 활용**
   - 속성 및 trace/span ID로 관측성 데이터 통합
   - OpenSearch Dashboards에서 통합 분석

5. **Vega 시각화**
   - 고급 분석을 위한 Vega 통합 활용
   - 이상 탐지 및 알림

---

## 🛠️ 마이그레이션 가이드

### OTel Collector → Data Prepper 전환

#### 1. Application 변경 (Endpoint만)

```json
// Before (OTel Collector)
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://otel-collector:4318"  // OTLP/HTTP 표준 포트
  }
}

// After (Data Prepper)
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://data-prepper:21891"  // Data Prepper 기본 포트
  }
}
```

#### 2. 인프라 변경

**OTel Collector 설정**:
```yaml
receivers:
  otlp:
    protocols:
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
    timeout: 10s
  memory_limiter:
    limit_mib: 512

exporters:
  elasticsearch:
    endpoints: ["https://opensearch:9200"]

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [elasticsearch]
```

**Data Prepper 설정** (동등한 기능):
```yaml
metrics-pipeline:
  source:
    otel_metrics_source:
      port: 21890
      request_timeout: 10000

  processor:
    - drop_events:
        drop_when: 'condition'

  sink:
    - opensearch:
        hosts: ["https://opensearch:9200"]
        bulk_size: 100
        flush_timeout: 10000
```

---

## ✅ 추천 사항

### 프로젝트에 맞는 선택

**현재 Functorium 프로젝트 상황**:
- ✅ OpenSearch 단일 백엔드
- ✅ 메트릭 중심 (트레이스는 부가적)
- ✅ 사전 집계 이미 구현 (Application에서)

**추천**: **OpenTelemetry Collector** ✅

**이유**:
1. **표준 준수**: CNCF 표준으로 장기 지속 가능성 높음
2. **리소스 효율**: Go 기반으로 메모리 적고 빠름
3. **메트릭 중심**: 메트릭 처리에 최적화
4. **커뮤니티**: 더 활발한 커뮤니티와 많은 레퍼런스
5. **미래 확장**: 나중에 Prometheus, Grafana 추가 가능

**Data Prepper 고려 시**:
- 분산 트레이스를 많이 사용하는 경우
- AWS OpenSearch Service 사용하는 경우
- OpenSearch 생태계에만 집중하는 경우

---

## 📚 참고 자료

### 공식 문서 (OpenSearch)

#### 핵심 문서
- **[OpenSearch Data Prepper 메인 문서](https://docs.opensearch.org/latest/data-prepper/)**
  - Data Prepper 개요 및 아키텍처
- **[Getting Started with Data Prepper](https://docs.opensearch.org/latest/data-prepper/getting-started/)**
  - 설치 및 기본 설정 가이드
- **[Configuring Data Prepper](https://docs.opensearch.org/latest/data-prepper/managing-data-prepper/configuring-data-prepper/)**
  - data-prepper-config.yaml 상세 설정

#### Pipeline 설정
- **[Configuring Data Prepper Pipelines](https://docs.opensearch.org/latest/data-prepper/pipelines/pipelines/)**
  - pipelines.yaml 설정 가이드 및 예제
  - 조건부 라우팅, 여러 파이프라인 구성

#### Sources (수신)
- **[OTel Metrics Source](https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/sources/otel-metrics-source/)**
  - otel_metrics_source 전체 설정 옵션
  - SSL/TLS, 인증, 네트워크 설정
- **[OTLP Source (통합)](https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/sources/otlp-source/)**
  - Logs, Metrics, Traces를 하나의 엔드포인트로 수신
- **[OTel Trace Source](https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/sources/otel-trace-source/)**
  - 트레이스 수집 (참고용)

#### Processors (변환)
- **[Processors Overview](https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/processors/)**
  - 사용 가능한 모든 프로세서 목록
- **[OTel Metrics Processor](https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/processors/otel-metrics/)**
  - otel_metrics_raw_processor 상세 설정

#### Sinks (전송)
- **[OpenSearch Sink](https://docs.opensearch.org/latest/data-prepper/pipelines/configuration/sinks/opensearch/)**
  - opensearch sink 전체 설정 옵션
  - 벌크, 재시도, DLQ, 템플릿 설정

### 블로그 및 가이드
- **[Metrics Ingestion with Data Prepper using OpenTelemetry](https://opensearch.org/blog/opentelemetry-metrics-for-data-prepper/)**
  - Data Prepper 1.4.0 메트릭 지원 발표
  - 성능 고려사항 및 Best Practices
  - 고카디널리티 메트릭 처리

### GitHub 및 커뮤니티
- **[opensearch-project/data-prepper](https://github.com/opensearch-project/data-prepper)**
  - 공식 GitHub 저장소
  - 이슈 트래킹 및 기여 가이드
- **[Docker Hub: opensearchproject/data-prepper](https://hub.docker.com/r/opensearchproject/data-prepper)**
  - 공식 Docker 이미지

### 비교 자료
- **[OpenTelemetry Collector Documentation](https://opentelemetry.io/docs/collector/)**
  - OTel Collector 공식 문서 (비교용)

---

## 📋 구현 체크리스트

### Data Prepper 도입 시

- [ ] Data Prepper 설치 (Docker/K8s)
- [ ] `data-prepper-config.yaml` 작성
- [ ] `pipelines.yaml` 작성 (metrics-pipeline)
- [ ] Application Endpoint 변경 (21890 포트)
- [ ] 메트릭 전송 테스트
- [ ] OpenSearch 인덱스 확인
- [ ] 대시보드 정상 동작 확인
- [ ] DLQ 로그 확인 (실패 이벤트)
- [ ] 성능 모니터링 (메모리/CPU)

### 롤백 계획

- OTel Collector 설정으로 복귀 (Endpoint만 변경)
- 코드 변경 없음 (OTLP 표준 사용)

---

## 🎯 결론

**Functorium 프로젝트 권장 선택**: **OpenTelemetry Collector** ✅

**이유 요약**:
1. ✅ 표준 준수 및 커뮤니티 지원
2. ✅ 리소스 효율성 (Go 기반)
3. ✅ 메트릭 처리 최적화
4. ✅ 미래 확장 가능성

**Data Prepper는 다음 경우에 고려**:
- 분산 트레이스 중심 관측성
- AWS OpenSearch Service 사용
- OpenSearch 생태계만 사용

**다음 단계**: [usecase-monitoring-based-on-OpenSearch.md](./usecase-monitoring-based-on-OpenSearch.md) 참조하여 OTel Collector 기반 구현 진행

---

## 📌 버전 정보 및 최신 동향

### Data Prepper 최신 버전

> **출처**: [OpenSearch Documentation Version History](https://docs.opensearch.org/latest/version-history/)

**Data Prepper 2.13** (최신 안정 버전, 2025):
- ✅ Prometheus sink 지원 추가
- ✅ 네이티브 OpenSearch data streams 지원
- ✅ 교차 리전 S3 수집
- ✅ 20% 성능 개선

**메트릭 지원 히스토리**:
- Data Prepper 1.4.0: 메트릭 수집 기능 도입 (2022)
- Data Prepper 2.0+: ExponentialHistogram 지원 추가
- Data Prepper 2.13: 성능 최적화 및 Prometheus 통합

### 프로덕션 체크리스트

**배포 전 확인사항** (공식 권장):
- [ ] JVM 힙 크기 설정 (최소 512MB, 권장 1-2GB)
- [ ] Circuit breaker 설정 (힙 사용률 70%)
- [ ] SSL/TLS 인증서 구성 (프로덕션 필수)
- [ ] 인증 설정 (HTTP Basic Auth 또는 AWS IAM)
- [ ] DLQ 경로 설정 및 모니터링
- [ ] Health check endpoint 활성화 (포트 4900)
- [ ] OpenSearch 인덱스 템플릿 생성
- [ ] ISM 정책 구성 (데이터 보존)

**모니터링 항목** (Data Prepper 자체):
- JVM 힙 메모리 사용률
- 처리된 이벤트 수 (records processed)
- 처리 실패 이벤트 수 (records failed)
- 백엔드 전송 지연 시간 (latency)
- DLQ 크기 증가 추이

---

## 📎 추가 자료

### OpenSearch 공식 블로그 포스트
- **[Introducing Data Prepper](https://aws.amazon.com/blogs/opensource/introducing-data-prepper/)**
  - Data Prepper 출시 발표 및 비전
- **[What's New in Data Prepper 2.0](https://opensearch.org/blog/data-prepper-2-0/)**
  - 주요 기능 업데이트

### Community 및 Support
- **[OpenSearch Forum](https://forum.opensearch.org/)**
  - 커뮤니티 Q&A
- **[OpenSearch Slack](https://opensearch.org/slack.html)**
  - 실시간 지원 채널

---

**문서 최종 업데이트**: 2026-01-07
**기반 공식 문서**: OpenSearch Data Prepper 2.13+ (최신 안정 버전)
**검증된 구성**: Docker Compose 환경, OpenSearch 2.x 호환
