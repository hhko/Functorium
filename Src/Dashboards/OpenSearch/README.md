# OpenSearch Dashboard 설정 가이드

이 문서는 OpenSearch 스택을 Docker Compose를 사용하여 실행하는 방법을 설명합니다.

## 개요

OpenSearch 스택은 로그, 메트릭, 트레이스 데이터를 수집, 저장, 시각화하는 통합 관측성(Observability) 솔루션입니다.

### 구성 요소

| 서비스 | 설명 |
|--------|------|
| OpenSearch | 분산 검색 및 분석 엔진 (Elasticsearch 호환) |
| OpenSearch Dashboards | 데이터 시각화 및 관리 UI (Kibana 호환) |
| Data Prepper | OpenTelemetry 데이터 수신 및 처리 파이프라인 |
| Fluent-bit | 경량 로그 수집기 |

## 포트 구성

### OpenSearch

| 포트 | 용도 | 설명 |
|------|------|------|
| 9200 | REST API | OpenSearch HTTP API |
| 9600 | Performance Analyzer | 성능 분석 API |

### OpenSearch Dashboards

| 포트 | 용도 | 설명 |
|------|------|------|
| 5601 | 프론트엔드 UI | 브라우저로 대시보드에 접속하는 포트 |

### Data Prepper (OTLP 수신)

| 포트 | 용도 | 설명 |
|------|------|------|
| 2021 | Logs | OTLP 로그 수신 (HTTP/Protobuf) |
| 2023 | Metrics | OTLP 메트릭 수신 (HTTP/Protobuf) |
| 21890 | Traces | OTLP 트레이스 수신 (gRPC) |
| 4900 | Server API | Data Prepper 관리 API |

### Fluent-bit

| 포트 | 용도 | 설명 |
|------|------|------|
| 2020 | Health API | Fluent-bit 상태 확인 API |

## 시작하기

### 1. OpenSearch 스택 실행

```powershell
# PowerShell 스크립트 사용
./start-dashboard.ps1

# 또는 직접 Docker Compose 사용
docker compose up -d
```

### 2. 대시보드 접속

브라우저에서 다음 주소로 접속:
```
http://localhost:5601
```

### 3. 상태 확인

```bash
# 컨테이너 상태 확인
docker compose ps

# 전체 로그 확인
docker compose logs -f

# 특정 서비스 로그 확인
docker compose logs -f opensearch
docker compose logs -f opensearch-dashboards
docker compose logs -f data-prepper
docker compose logs -f fluent-bit
```

### 4. OpenSearch API 확인

```bash
# 클러스터 상태 확인
curl http://localhost:9200/_cluster/health?pretty

# 인덱스 목록 확인
curl http://localhost:9200/_cat/indices?v

# 노드 정보 확인
curl http://localhost:9200/_nodes?pretty
```

## 애플리케이션 설정

애플리케이션에서 Data Prepper로 원격 분석을 전송하려면 `appsettings.json`에 다음과 같이 설정:

```json
{
  "OpenTelemetry": {
    "ServiceName": "YourServiceName",
    "CollectorEndpoint": "http://127.0.0.1:21890",
    "CollectorProtocol": "Grpc",
    "TracingCollectorEndpoint": "http://127.0.0.1:21890",
    "MetricsCollectorEndpoint": "http://127.0.0.1:2023",
    "LoggingCollectorEndpoint": "http://127.0.0.1:2021"
  }
}
```

## Data Prepper 파이프라인

Data Prepper는 다음과 같은 파이프라인을 사용합니다:

### 트레이스 파이프라인

```yaml
otel-trace-pipeline:
  source:
    otel_trace_source:
      port: 21890
  sink:
    - opensearch:
        hosts: ["http://opensearch:9200"]
        index: otel-traces
```

### 로그 파이프라인

```yaml
otel-logs-pipeline:
  source:
    otel_logs_source:
      port: 2021
  sink:
    - opensearch:
        hosts: ["http://opensearch:9200"]
        index: otel-logs
```

### 메트릭 파이프라인

```yaml
otel-metrics-pipeline:
  source:
    otel_metrics_source:
      port: 2023
  sink:
    - opensearch:
        hosts: ["http://opensearch:9200"]
        index: otel-metrics
```

## 중지 및 제거

```powershell
# PowerShell 스크립트 사용
./stop-dashboard.ps1

# 또는 직접 Docker Compose 사용
docker compose stop

# 중지 및 컨테이너 제거
docker compose down

# 중지, 컨테이너 및 볼륨 제거
docker compose down -v
```

## 보안 주의사항

현재 설정은 개발 환경용으로 보안이 비활성화되어 있습니다:

- `DISABLE_SECURITY_PLUGIN=true` (OpenSearch)
- `DISABLE_SECURITY_DASHBOARDS_PLUGIN=true` (Dashboards)

**프로덕션 환경**에서는 반드시 다음을 설정해야 합니다:

- TLS/SSL 인증서 설정
- 사용자 인증 및 권한 관리
- 네트워크 보안 설정

## 리소스 요구사항

| 서비스 | 최소 메모리 | 권장 메모리 |
|--------|-------------|-------------|
| OpenSearch | 512MB | 1GB+ |
| OpenSearch Dashboards | 256MB | 512MB |
| Data Prepper | 512MB | 1GB |
| Fluent-bit | 64MB | 128MB |

## 트러블슈팅

### OpenSearch가 시작되지 않을 때

```bash
# vm.max_map_count 확인 (Linux)
sysctl vm.max_map_count

# 값이 262144 미만이면 설정 필요
sudo sysctl -w vm.max_map_count=262144
```

### 메모리 부족 오류

`docker-compose.yml`에서 JVM 힙 크기를 조정:

```yaml
environment:
  - "OPENSEARCH_JAVA_OPTS=-Xms256m -Xmx256m"
```

### Data Prepper 연결 오류

OpenSearch가 완전히 시작된 후 Data Prepper가 시작되는지 확인:

```bash
# OpenSearch 상태 확인
curl http://localhost:9200/_cluster/health

# Data Prepper 파이프라인 상태 확인
curl http://localhost:4900/list
```

## 참고 자료

- [OpenSearch 공식 문서](https://opensearch.org/docs/latest/)
- [OpenSearch Dashboards 가이드](https://opensearch.org/docs/latest/dashboards/)
- [Data Prepper 문서](https://opensearch.org/docs/latest/data-prepper/)
- [Fluent-bit 문서](https://docs.fluentbit.io/)
- [OpenTelemetry 문서](https://opentelemetry.io/docs/)
