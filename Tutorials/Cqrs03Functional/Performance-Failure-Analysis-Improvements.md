# 성능 분석 및 장애 분석을 위한 데이터 개선 사항

이 문서는 **성능 분석**과 **장애 분석**을 효과적으로 수행하기 위해 현재 Log, Metrics, Trace 데이터에서 **추가되거나 개선되어야 할 데이터**를 정리합니다.

## 목차

1. [성능 분석 데이터 개선 사항](#성능-분석-데이터-개선-사항)
2. [장애 분석 데이터 개선 사항](#장애-분석-데이터-개선-사항)
3. [통합 분석을 위한 데이터 개선 사항](#통합-분석을-위한-데이터-개선-사항)
4. [우선순위별 개선 사항 요약](#우선순위별-개선-사항-요약)

---

## 성능 분석 데이터 개선 사항

### 1. 리소스 사용량 메트릭 부재

**문제점**:
- 현재 처리 시간(Elapsed)과 처리량(Counter)만 수집됨
- CPU, Memory, GC, Thread Pool 등 리소스 사용량 정보 없음
- 성능 병목의 원인 파악이 어려움

**현재 상태**:
- Duration Histogram: 처리 시간만 기록
- Request/Response Counter: 처리량만 기록
- 리소스 메트릭: 없음 ❌

**개선 방안**:
- **CPU 사용률**: `process.cpu.usage` (프로세스 CPU 사용률)
- **메모리 사용량**: `process.memory.usage` (힙 메모리 사용량)
- **GC 통계**: `dotnet.gc.collections`, `dotnet.gc.pause` (GC 횟수 및 일시정지 시간)
- **Thread Pool 통계**: `dotnet.thread_pool.threads` (활성 스레드 수)
- **I/O 통계**: `process.io.read_bytes`, `process.io.write_bytes` (디스크 I/O)

**영향도**: 높음 - 성능 병목 원인 분석에 필수적

**수집 시점**:
- 각 요청 처리 전후에 리소스 스냅샷 기록
- 또는 주기적으로 리소스 메트릭 수집 (예: 10초마다)

---

### 2. 세부 처리 시간(Sub-duration) 부재

**문제점**:
- 전체 처리 시간(Elapsed)만 기록됨
- Pipeline 단계별, Adapter 호출별 세부 시간 없음
- 병목 지점 식별 불가능

**현재 상태**:
```
Usecase: Elapsed = 200.4511 ms (전체 시간만)
  - Validation Pipeline: ? ms
  - Exception Pipeline: ? ms
  - Handler 실행: ? ms
  - Adapter 호출: ? ms
```

**개선 방안**:
- **Pipeline 단계별 시간**: 각 Pipeline의 실행 시간을 별도로 기록
  - `pipeline.validation.duration`
  - `pipeline.exception.duration`
  - `pipeline.metric.duration`
  - `pipeline.logger.duration`
- **Handler 실행 시간**: 실제 비즈니스 로직 실행 시간
  - `handler.execution.duration`
- **Adapter 호출 시간**: 각 Adapter 호출별 시간 (이미 있으나 Trace에만 존재)
  - `adapter.{category}.{handler}.{method}.duration`

**영향도**: 높음 - 성능 병목 지점 정확한 식별에 필수

**구현 방법**:
- Trace의 각 Span에 Duration 정보 활용
- 또는 Metrics에 세부 Duration Histogram 추가

---

### 3. 동시성(Concurrency) 정보 부재

**문제점**:
- 동시 처리 중인 요청 수 정보 없음
- 동시성 제한으로 인한 성능 저하 파악 불가능

**현재 상태**:
- 동시 처리 중인 요청 수: 없음 ❌
- 동시 처리 제한 설정: 없음 ❌

**개선 방안**:
- **동시 요청 수**: `application.usecase.{cqrs}.concurrent_requests` (Gauge)
- **동시 Adapter 호출 수**: `adapter.{category}.concurrent_requests` (Gauge)
- **최대 동시 처리 수**: 설정값 기록 (태그로)

**영향도**: 중간 - 동시성 문제 진단에 유용

**수집 시점**:
- 요청 시작 시: `concurrent_requests++`
- 요청 완료 시: `concurrent_requests--`

---

### 4. 데이터 크기 정보 부재

**문제점**:
- Request/Response 객체의 크기 정보 없음
- 대용량 데이터 처리로 인한 성능 저하 파악 불가능

**현재 상태**:
- Request 크기: 없음 ❌
- Response 크기: 없음 ❌
- 컬렉션 요소 수: 없음 ❌

**개선 방안**:
- **Request 크기**: `request.size_bytes` (Histogram)
- **Response 크기**: `response.size_bytes` (Histogram)
- **컬렉션 요소 수**: `response.collection.count` (Histogram, 컬렉션 반환 시)

**영향도**: 중간 - 대용량 데이터 처리 성능 분석에 유용

**구현 방법**:
- JSON 직렬화 후 바이트 크기 측정
- 또는 객체 직렬화 전 크기 추정

---

### 5. 큐 대기 시간 부재

**문제점**:
- 요청이 큐에서 대기한 시간 정보 없음
- 처리 지연의 원인 파악 불가능

**현재 상태**:
- 큐 대기 시간: 없음 ❌
- 요청 수신 시간: 없음 ❌

**개선 방안**:
- **큐 대기 시간**: `request.queue.wait_duration` (Histogram)
- **요청 수신 시간**: `request.received_timestamp` (태그 또는 이벤트)

**영향도**: 중간 - 처리 지연 원인 분석에 유용

**구현 방법**:
- 요청 수신 시점과 처리 시작 시점의 차이 계산

---

### 6. 처리량(Throughput) 세분화 부재

**문제점**:
- 전체 처리량만 기록됨
- 시간대별, Handler별 처리량 분석 어려움

**현재 상태**:
- Request Counter: 전체 요청 수만 기록
- 시간대별 집계: 없음 ❌

**개선 방안**:
- **시간대별 처리량**: Prometheus의 `rate()` 함수 활용 (이미 가능)
- **Handler별 처리량**: 태그로 이미 구분 가능 ✅
- **처리량 추이**: 시계열 데이터로 이미 가능 ✅

**영향도**: 낮음 - 현재 Metrics로도 분석 가능

**참고**: 현재 Metrics 구조로도 처리량 분석이 가능하나, 대시보드 구성 가이드 필요

---

### 7. 캐시 히트율 정보 부재

**문제점**:
- 캐시 사용 시 히트/미스 정보 없음
- 캐시 효율성 분석 불가능

**현재 상태**:
- 캐시 히트율: 없음 ❌
- 캐시 미스율: 없음 ❌

**개선 방안**:
- **캐시 히트**: `cache.{name}.hits` (Counter)
- **캐시 미스**: `cache.{name}.misses` (Counter)
- **캐시 히트율**: `cache.{name}.hit_rate` (히트/전체)

**영향도**: 낮음 - 캐시 사용 시에만 필요

**구현 방법**:
- 캐시 레이어에서 직접 메트릭 수집

---

## 장애 분석 데이터 개선 사항

### 8. 에러 발생 시점의 시스템 상태 부재

**문제점**:
- 에러 발생 시점의 CPU, Memory 등 시스템 상태 정보 없음
- 리소스 부족으로 인한 에러인지 판단 불가능

**현재 상태**:
- 에러 정보: Error 객체만 기록 ✅
- 시스템 상태: 없음 ❌

**개선 방안**:
- **에러 발생 시점 리소스 스냅샷**: 에러 발생 시 CPU, Memory, GC 통계 기록
  - `error.cpu.usage`
  - `error.memory.usage`
  - `error.gc.collections`
- **에러 태그에 리소스 정보 추가**: Trace/Log의 에러 태그에 리소스 정보 포함

**영향도**: 높음 - 장애 원인 분석에 필수적

**구현 방법**:
- 에러 발생 시점에 리소스 메트릭 수집
- 또는 에러 태그에 리소스 정보 추가

---

### 9. 에러 발생 전후 컨텍스트 부족

**문제점**:
- 에러 발생 직전의 요청/응답 정보 부족
- 에러 재현을 위한 충분한 정보 없음

**현재 상태**:
- 에러 발생 시점의 Request: 있음 ✅
- 에러 발생 직전의 상태: 없음 ❌
- 에러 발생 경로: Trace에 있음 ✅

**개선 방안**:
- **에러 발생 직전 로그**: 에러 발생 전 마지막 N개 로그 기록
- **에러 발생 경로**: Trace의 Span 정보 활용 (이미 가능) ✅
- **에러 발생 시점의 요청 ID**: Trace ID로 이미 가능 ✅

**영향도**: 중간 - 에러 재현 및 분석에 유용

**구현 방법**:
- 로그 수집 도구의 컨텍스트 기능 활용
- 또는 에러 발생 시점의 최근 로그를 에러 태그에 포함

---

### 10. 에러 발생 빈도 및 패턴 분석 데이터 부족

**문제점**:
- 에러 발생 빈도는 Metrics에 있으나 패턴 분석 데이터 부족
- 시간대별, Handler별 에러 패턴 분석 어려움

**현재 상태**:
- 에러 발생 수: Response Failure Counter로 기록 ✅
- 에러 타입별 분류: Error 태그로 구분 가능 ✅
- 에러 발생 패턴: 없음 ❌

**개선 방안**:
- **에러 타입별 카운터**: `error.{type}.count` (Counter)
- **에러 발생 시간대**: 태그로 시간대 정보 추가
- **에러 발생 빈도 추이**: 시계열 데이터로 분석 가능 ✅

**영향도**: 중간 - 에러 패턴 분석에 유용

**구현 방법**:
- 에러 타입별 별도 Counter 생성
- 또는 기존 Failure Counter에 에러 타입 태그 추가

---

### 11. 에러 발생 시점의 외부 의존성 상태 부재

**문제점**:
- 에러 발생 시점의 외부 서비스(DB, API 등) 상태 정보 없음
- 외부 의존성 문제로 인한 에러인지 판단 불가능

**현재 상태**:
- 외부 의존성 상태: 없음 ❌
- 외부 서비스 응답 시간: 없음 ❌

**개선 방안**:
- **외부 서비스 응답 시간**: `external.{service}.duration` (Histogram)
- **외부 서비스 에러율**: `external.{service}.error_rate` (Counter)
- **외부 서비스 연결 상태**: `external.{service}.connected` (Gauge)

**영향도**: 중간 - 외부 의존성 문제 진단에 유용

**구현 방법**:
- Adapter 레벨에서 외부 서비스 호출 정보 수집
- 또는 Health Check 메트릭 활용

---

### 12. 에러 재현을 위한 상세 정보 부족

**문제점**:
- 에러 재현을 위한 충분한 정보가 로그에 없을 수 있음
- 특히 IAdapter Information 레벨에서는 파라미터 정보 없음

**현재 상태**:
- Usecase: Request 전체 객체 기록 ✅
- IAdapter Debug: 파라미터 기록 ✅
- IAdapter Information: 파라미터 없음 ❌

**개선 방안**:
- **에러 발생 시 파라미터 강제 기록**: Information 레벨에서도 에러 발생 시 파라미터 기록
- **에러 발생 시점의 환경 정보**: OS, .NET 버전, 서비스 버전 등
- **에러 발생 시점의 설정값**: 중요 설정값 기록

**영향도**: 중간 - 에러 재현에 유용

**구현 방법**:
- 에러 발생 시점에만 상세 정보 기록하는 옵션 추가

---

### 13. 에러 발생 시점의 트랜잭션 컨텍스트 부족

**문제점**:
- 분산 트랜잭션 환경에서 에러 발생 시점의 전체 트랜잭션 컨텍스트 부족
- 여러 서비스에 걸친 에러 추적 어려움

**현재 상태**:
- Trace ID: 있음 ✅
- Parent Span: 있음 ✅
- 트랜잭션 ID: 없음 ❌

**개선 방안**:
- **트랜잭션 ID**: 분산 트랜잭션 추적을 위한 고유 ID
- **트랜잭션 상태**: 태그로 트랜잭션 상태 기록
- **트랜잭션 참여 서비스**: 태그로 참여 서비스 목록 기록

**영향도**: 낮음 - 분산 트랜잭션 환경에서만 필요

**구현 방법**:
- Trace의 Baggage 또는 태그로 트랜잭션 정보 전파

---

## 통합 분석을 위한 데이터 개선 사항

### 14. Log/Metrics/Trace 간 상관관계 식별자 부족

**문제점**:
- Log, Metrics, Trace 간 상관관계를 명확히 식별할 수 있는 공통 식별자 부족
- 통합 분석 시 데이터 연결 어려움

**현재 상태**:
- Trace ID: Trace에만 있음
- Request ID: 없음 ❌
- 공통 식별자: 없음 ❌

**개선 방안**:
- **Trace ID를 Log/Metrics에 포함**: 모든 Log와 Metrics에 Trace ID 추가
- **Request ID 생성**: 요청별 고유 ID 생성하여 Log/Metrics/Trace에 모두 포함
- **Span ID를 Log에 포함**: 특정 Span과 연관된 Log 식별

**영향도**: 높음 - 통합 분석에 필수적

**구현 방법**:
- Log Scope에 Trace ID 추가
- Metrics 태그에 Trace ID 추가
- 또는 OpenTelemetry의 Baggage 활용

---

### 15. 시간 동기화 문제

**문제점**:
- Log, Metrics, Trace의 타임스탬프가 정확히 일치하지 않을 수 있음
- 시간 기반 통합 분석 시 오차 발생 가능

**현재 상태**:
- Log Timestamp: Serilog이 자동 생성
- Metrics Timestamp: OpenTelemetry가 자동 생성
- Trace Timestamp: Activity가 자동 생성
- 시간 동기화: 보장되지 않음 ❌

**개선 방안**:
- **공통 타임스탬프**: 요청 처리 시작 시점의 타임스탬프를 모든 데이터에 포함
- **타임스탬프 정확도**: 나노초 단위 정확도 보장
- **시간 오프셋 보정**: 시스템 간 시간 오프셋 보정

**영향도**: 중간 - 시간 기반 통합 분석 시 중요

**구현 방법**:
- 요청 처리 시작 시점의 타임스탬프를 Context에 저장
- 모든 Log/Metrics/Trace에 동일한 타임스탬프 사용

---

### 16. 데이터 샘플링 전략 부재

**문제점**:
- 대량의 요청 처리 시 모든 데이터를 수집하면 비용 증가
- 샘플링 전략이 없어 비용 최적화 불가능

**현재 상태**:
- 샘플링 전략: 없음 ❌
- 모든 요청 기록: 기본 동작

**개선 방안**:
- **적응형 샘플링**: 에러 발생 시 100% 샘플링, 정상 요청은 낮은 비율 샘플링
- **시간 기반 샘플링**: 특정 시간대만 상세 수집
- **Handler별 샘플링**: 중요 Handler는 높은 비율, 일반 Handler는 낮은 비율

**영향도**: 중간 - 대규모 시스템에서 비용 최적화에 중요

**구현 방법**:
- OpenTelemetry의 샘플링 전략 활용
- 또는 커스텀 샘플링 로직 구현

---

### 17. 데이터 보존 정책 부재

**문제점**:
- 데이터 보존 기간 및 보관 정책이 명시되지 않음
- 장기 분석 시 데이터 손실 가능

**현재 상태**:
- 보존 정책: 없음 ❌
- 보관 전략: 없음 ❌

**개선 방안**:
- **Hot Storage**: 최근 N일 데이터 (빠른 조회)
- **Warm Storage**: N일~M일 데이터 (일반 조회)
- **Cold Storage**: M일 이상 데이터 (아카이브)
- **에러 데이터 우선 보존**: 에러 발생 데이터는 더 오래 보존

**영향도**: 낮음 - 운영 정책 문제이지만 문서화 필요

**구현 방법**:
- 로그 수집 도구의 보존 정책 설정
- 또는 커스텀 보관 스크립트 구현

---

## 우선순위별 개선 사항 요약

| 우선순위 | 개선 사항 | 데이터 관점 | 예상 영향 | 구현 난이도 |
|---------|----------|------------|----------|------------|
| **높음** | 리소스 사용량 메트릭 | 성능 분석 | 병목 원인 분석 | 중간 |
| **높음** | 세부 처리 시간 | 성능 분석 | 병목 지점 식별 | 중간 |
| **높음** | Log/Metrics/Trace 상관관계 식별자 | 통합 분석 | 통합 분석 가능 | 낮음 |
| **높음** | 에러 발생 시점 시스템 상태 | 장애 분석 | 장애 원인 분석 | 중간 |
| **중간** | 동시성 정보 | 성능 분석 | 동시성 문제 진단 | 낮음 |
| **중간** | 데이터 크기 정보 | 성능 분석 | 대용량 데이터 분석 | 낮음 |
| **중간** | 에러 발생 전후 컨텍스트 | 장애 분석 | 에러 재현 | 중간 |
| **중간** | 에러 발생 시점 외부 의존성 상태 | 장애 분석 | 외부 의존성 진단 | 중간 |
| **중간** | 시간 동기화 | 통합 분석 | 시간 기반 분석 | 낮음 |
| **낮음** | 큐 대기 시간 | 성능 분석 | 처리 지연 분석 | 중간 |
| **낮음** | 캐시 히트율 | 성능 분석 | 캐시 효율성 | 낮음 |
| **낮음** | 에러 발생 패턴 분석 | 장애 분석 | 에러 패턴 분석 | 낮음 |
| **낮음** | 트랜잭션 컨텍스트 | 장애 분석 | 분산 트랜잭션 추적 | 높음 |
| **낮음** | 데이터 샘플링 전략 | 비용 최적화 | 비용 절감 | 중간 |
| **낮음** | 데이터 보존 정책 | 운영 정책 | 장기 분석 | 낮음 |

---

## 권장 개선 방향

### 1단계: 필수 데이터 추가 (높은 우선순위)

1. **리소스 사용량 메트릭 수집**
   - OpenTelemetry의 Resource 메트릭 활용
   - 또는 .NET Runtime 메트릭 수집

2. **세부 처리 시간 기록**
   - Trace의 각 Span Duration 활용
   - 또는 Pipeline별 Duration Metrics 추가

3. **상관관계 식별자 추가**
   - Log Scope에 Trace ID 추가
   - Metrics 태그에 Trace ID 추가

4. **에러 발생 시점 시스템 상태 기록**
   - 에러 발생 시점에 리소스 메트릭 스냅샷
   - 에러 태그에 리소스 정보 추가

### 2단계: 분석 효율성 향상 (중간 우선순위)

1. **동시성 정보 수집**
   - Gauge 메트릭으로 동시 요청 수 추적

2. **데이터 크기 정보 수집**
   - Request/Response 크기 Histogram 추가

3. **에러 컨텍스트 강화**
   - 에러 발생 시점의 상세 정보 기록
   - 외부 의존성 상태 정보 수집

### 3단계: 고급 분석 기능 (낮은 우선순위)

1. **캐시 히트율 메트릭**
2. **트랜잭션 컨텍스트 추적**
3. **샘플링 전략 구현**
4. **데이터 보존 정책 수립**

---

## 참고사항

### 성능 분석 체크리스트

성능 분석 시 다음 데이터를 확인해야 합니다:

- ✅ 처리 시간 (Elapsed) - 현재 있음
- ✅ 처리량 (Throughput) - 현재 있음
- ❌ 리소스 사용량 (CPU, Memory) - 추가 필요
- ❌ 세부 처리 시간 - 추가 필요
- ❌ 동시성 정보 - 추가 필요
- ❌ 데이터 크기 - 추가 필요

### 장애 분석 체크리스트

장애 분석 시 다음 데이터를 확인해야 합니다:

- ✅ 에러 정보 (Error 객체) - 현재 있음
- ✅ 에러 발생 빈도 - 현재 있음
- ✅ 에러 발생 경로 (Trace) - 현재 있음
- ❌ 에러 발생 시점 시스템 상태 - 추가 필요
- ❌ 에러 발생 전후 컨텍스트 - 부분적으로 있음
- ❌ 에러 발생 시점 외부 의존성 상태 - 추가 필요

### 통합 분석 체크리스트

통합 분석 시 다음을 확인해야 합니다:

- ✅ Trace ID - 현재 있음
- ❌ Log/Metrics에 Trace ID 포함 - 추가 필요
- ❌ 시간 동기화 - 개선 필요
- ❌ 샘플링 전략 - 추가 필요

---

## 관련 코드 위치

- **Usecase Pipeline**: `Src/Functorium/Applications/Pipelines/UsecasePipelineBase.cs`
- **IAdapter Pipeline 생성기**: `Src/Functorium.Adapters.SourceGenerator/AdapterPipelineGenerator.cs`
- **Metrics 수집**: `Src/Functorium/Applications/Pipelines/UsecaseMetricPipeline.cs`, `Src/Functorium/Adapters/Observabilities/Metrics/AdapterMetric.cs`
- **Trace 수집**: `Src/Functorium/Applications/Pipelines/UsecaseTracePipeline.cs`, `Src/Functorium/Adapters/Observabilities/Tracing/AdapterTrace.cs`
- **Log 수집**: `Src/Functorium/Applications/Pipelines/UsecaseLoggerPipeline.cs`

