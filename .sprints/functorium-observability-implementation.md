# Functorium 관찰 가능성(Observability) 구현 분석

**분석일**: 2026-01-03
**상태**: 📋 분석 완료

## 개요

Functorium 프레임워크의 관찰 가능성(Observability) 구현을 분석한 문서입니다. 분산 트레이싱, 구조화된 로깅, 메트릭 수집의 세 가지 핵심 축을 중심으로 구현되어 있습니다.

---

## 1. 디렉토리 구조

### Applications 레이어 (기술 독립적 추상화)

```
Src/Functorium/Applications/Observabilities/
├── ObservabilityNaming.cs           # 통합 네이밍 규칙 (OpenTelemetry Semantic Conventions)
├── IAdapter.cs                       # 어댑터 마커 인터페이스
├── Context/
│   ├── IObservabilityContext.cs     # TraceId/SpanId 추상화
│   └── IContextPropagator.cs        # 컨텍스트 전파 인터페이스
├── Loggers/
│   └── UsecaseLoggerExtensions.cs   # Logger 확장 메서드
├── Metrics/
│   └── IMetricRecorder.cs           # 메트릭 기록 인터페이스
└── Spans/
    ├── ISpan.cs                     # Span 추상화 (Activity 래퍼)
    └── ISpanFactory.cs              # Span 팩토리 패턴
```

### Adapters 레이어 (OpenTelemetry 구현)

```
Src/Functorium/Adapters/Observabilities/
├── OpenTelemetryOptions.cs          # 설정 (ServiceName, Endpoints, Protocol)
├── IOpenTelemetryOptions.cs         # 옵션 인터페이스
├── Builders/
│   ├── OpenTelemetryBuilder.cs      # 메인 Fluent Builder
│   ├── OpenTelemetryBuilder.Protocols.cs   # 프로토콜 헬퍼
│   ├── OpenTelemetryBuilder.Resources.cs   # 리소스 설정
│   └── Configurators/
│       ├── LoggingConfigurator.cs   # Serilog 확장 설정
│       ├── MetricsConfigurator.cs   # 메트릭 확장 설정
│       └── TracingConfigurator.cs   # 트레이싱 확장 설정
├── Context/
│   ├── ObservabilityContext.cs      # ActivityContext 래퍼
│   ├── ActivityContextPropagator.cs # IContextPropagator 구현
│   └── ActivityContextHolder.cs     # AsyncLocal 컨텍스트 관리
├── Loggers/
│   ├── IStartupOptionsLogger.cs     # 시작 로거 인터페이스
│   └── StartupLogger.cs             # 시작 로깅 서비스
├── Metrics/
│   └── OpenTelemetryMetricRecorder.cs  # System.Diagnostics.Metrics 구현
└── Spans/
    ├── OpenTelemetrySpan.cs         # Activity 래퍼 구현
    └── OpenTelemetrySpanFactory.cs  # ActivitySource 기반 팩토리
```

### Pipeline 레이어 (통합 지점)

```
Src/Functorium/Applications/Pipelines/
├── UsecasePipelineBase.cs           # Base 클래스 (handler/CQRS 탐지)
├── UsecaseTracingPipeline.cs        # Activity 기반 분산 트레이싱
├── UsecaseLoggingPipeline.cs        # 구조화된 로깅
├── UsecaseMetricsPipeline.cs         # Counter 및 Histogram 메트릭
└── UsecaseMetricCustomPipelineBase.cs  # 사용자 정의 메트릭 베이스
```

---

## 2. 핵심 인터페이스 및 클래스

### 추상화 레이어 (Applications)

| 클래스/인터페이스 | 설명 |
|------------------|------|
| `ObservabilityNaming` | 네이밍 규칙의 단일 원천 (OpenTelemetry Semantic Conventions 준수) |
| `IObservabilityContext` | TraceId, SpanId 추상화 |
| `ISpan` | 작업 단위 추상화 - `SetTag()`, `SetSuccess()`, `SetFailure()` |
| `ISpanFactory` | Span 생성 팩토리 패턴 - `CreateChildSpan()` |
| `IMetricRecorder` | `RecordRequest()`, `RecordResponseSuccess()`, `RecordResponseFailure()` |
| `IContextPropagator` | Async 경계 간 컨텍스트 전파 - `Current`, `CreateScope()`, `ExtractContext()` |

### 구현 레이어 (Adapters)

| 클래스 | 설명 |
|--------|------|
| `OpenTelemetrySpan` | System.Diagnostics.Activity 래퍼 (~85 lines) |
| `OpenTelemetrySpanFactory` | ActivitySource 기반 팩토리 (~109 lines) |
| `OpenTelemetryMetricRecorder` | System.Diagnostics.Metrics 사용 (~154 lines) |
| `ActivityContextHolder` | AsyncLocal 기반 컨텍스트 관리 (~88 lines) |
| `ActivityContextPropagator` | IContextPropagator 구현 (~57 lines) |
| `OpenTelemetryBuilder` | Fluent API 설정 빌더 (~395 lines) |

---

## 3. 관찰 가능성 구현 패턴

### A. 분산 트레이싱 (UsecaseTracingPipeline)

**파일**: `Src/Functorium/Applications/Pipelines/UsecaseTracingPipeline.cs`

- MediatR `IPipelineBehavior`로 구현
- 각 유스케이스 요청에 Activity 생성
- **요청 태그**: layer, category, CQRS type, handler, function
- **응답 태그**: status (success/failure), error details
- 경과 시간 측정

**에러 타입별 처리**:
- `ErrorCodeExpected`: error.code, error.message 태그 설정
- `ErrorCodeExceptional`: error.code, error.message 태그 설정
- `ManyErrors`: error.count 태그 설정
- Default: error.type, error.message 태그 설정

**부모 컨텍스트 해결 우선순위**:
1. `Activity.Current` (표준 OpenTelemetry)
2. `ActivityContextHolder` (FinT AsyncLocal 해결책)
3. 명시적 parentContext 파라미터
4. Default (새 루트 span)

### B. 구조화된 로깅 (UsecaseLoggingPipeline)

**파일**: `Src/Functorium/Applications/Pipelines/UsecaseLoggingPipeline.cs`

| 로그 레벨 | 상황 |
|-----------|------|
| Information | 요청 및 성공 응답 |
| Warning | 예상된 에러 (`ErrorCodeExpected`) |
| Error | 예외적 에러 (`ErrorCodeExceptional`) |

**Logger 확장 메서드** (`UsecaseLoggerExtensions.cs`):
- `LogRequestMessage()`
- `LogResponseMessageSuccess()`
- `LogResponseMessageWarning()`
- `LogResponseMessageError()`

### C. 메트릭 수집 (UsecaseMetricsPipeline)

**파일**: `Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs`

**Counter 메트릭**:
- `functorium.application.usecase.{cqrs}.requests`
- `functorium.application.usecase.{cqrs}.responses.success`
- `functorium.application.usecase.{cqrs}.responses.failure`

**Histogram 메트릭**:
- `functorium.application.usecase.{cqrs}.duration` (초 단위)

**태그**: layer, category, CQRS type, handler, status

---

## 4. 주요 설계 패턴

| 패턴 | 적용 위치 | 설명 |
|------|----------|------|
| **Adapter Factory** | `ISpanFactory`, `IMetricRecorder` | 추상화를 통한 구현체 교체 가능 |
| **Builder** | `OpenTelemetryBuilder` | Fluent API 설정 |
| **Configurator** | `LoggingConfigurator`, `MetricsConfigurator`, `TracingConfigurator` | 확장 지점 제공 |
| **Strategy** | 에러 처리 | 에러 타입별 다른 태그 설정 전략 |
| **Scope** | `ISpan`, `ActivityContextHolder` | IDisposable로 생명주기 관리 |
| **AsyncLocal** | `ActivityContextHolder` | Async 경계 간 컨텍스트 전파 |
| **Single Source of Truth** | `ObservabilityNaming` | 네이밍 규칙 통일 |
| **Semantic Conventions** | 전체 | OpenTelemetry 표준 속성 준수 |

---

## 5. 테스트 커버리지

### 테스트 파일 위치

`Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/`

| 테스트 클래스 | 설명 | 테스트 케이스 |
|--------------|------|--------------|
| `OpenTelemetrySpanFactoryTests.cs` | 부모 컨텍스트 해결 로직 | 14개 |
| `LoggingConfiguratorTests.cs` | 로깅 설정 | - |
| `MetricsConfiguratorTests.cs` | 메트릭 설정 | - |
| `TracingConfiguratorTests.cs` | 트레이싱 설정 | - |
| `OpenTelemetryOptionsTests.cs` | 옵션 검증 | - |
| `OtlpCollectorProtocolTests.cs` | 프로토콜 enum | - |

### OpenTelemetrySpanFactoryTests 주요 테스트 시나리오

- Activity.Current vs ActivityContextHolder 우선순위
- 다중 Span 생성 시 올바른 부모 연결
- Dispose 후 Activity.Current 복원
- 다양한 어댑터 타입 (Repository, MessageBroker, HttpClient)
- FinT AsyncLocal 시나리오

---

## 6. 설정 및 통합

### OpenTelemetryOptions 설정 항목

```csharp
public class OpenTelemetryOptions
{
    public string ServiceName { get; set; }           // 애플리케이션 식별자
    public string ServiceVersion { get; set; }        // 어셈블리에서 자동 설정
    public string ServiceNamespace { get; set; }      // 선택적 커스텀 네임스페이스
    public string CollectorEndpoint { get; set; }     // 통합 OTLP 엔드포인트 (기본값)
    public string TracingEndpoint { get; set; }       // 신호별 엔드포인트
    public string MetricsEndpoint { get; set; }
    public string LoggingEndpoint { get; set; }
    public OtlpCollectorProtocol CollectorProtocol { get; set; }  // gRPC 또는 HTTP/Protobuf
    public double SamplingRate { get; set; }          // 0.0-1.0 트레이스 샘플링
    public bool EnablePrometheusExporter { get; set; } // 선택적 Prometheus 메트릭 내보내기
}
```

### 통합 예제

```csharp
services
    .RegisterOpenTelemetry(configuration)
    .ConfigureLogging(config => { /* 커스텀 Serilog 설정 */ })
    .ConfigureMetrics(config => { /* 커스텀 instrumentation */ })
    .ConfigureTracing(config => { /* 커스텀 processors */ })
    .ConfigureStartupLogger(logger => { /* 시작 로그 */ })
    .Build();
```

---

## 7. 핵심 특징

### 기술 독립성
- 추상화 레이어로 구현체 교체 가능 (Activity → 다른 트레이싱 라이브러리)

### 성능 최적화
- `TagList` struct 사용으로 힙 할당 감소
- Lazy Meter 초기화 (thread-safe double-check 패턴)
- `Logger.IsEnabled()` 체크로 불필요한 객체 생성 방지

### FinT 프레임워크 통합
- FinT 모나딕 체인에서의 AsyncLocal 컨텍스트 이슈 해결
- `ActivityContextHolder`를 통한 워크어라운드

### Semantic Convention 준수
- OpenTelemetry semantic conventions 표준 속성 사용

### 확장 가능한 설정
- 3개 Configurator (Logging, Metrics, Tracing)로 커스텀 instrumentation 지원

### 네임스페이스 자동 탐지
- Builder가 프로젝트 네임스페이스를 자동 탐지하여 Meter/ActivitySource 필터링

### 멀티 프로토콜 지원
- gRPC, HTTP/Protobuf OTLP 프로토콜 모두 지원

### 구조화된 로깅 통합
- OpenTelemetry sink와 함께 Serilog 사용
- 에러 destructuring 정책

### 소스 생성기 지원
- 어댑터 파이프라인에 관찰 가능성 자동 주입

---

## 8. 통계 요약

| 항목 | 수량 |
|------|------|
| 총 관찰 가능성 코드 | ~3,200 lines |
| Applications 레이어 인터페이스/추상화 | 8개 |
| Adapters 레이어 구현 | 16개 |
| 관찰 가능성 파이프라인 | 3개 (Tracing, Logging, Metrics) |
| Configurator 확장 지점 | 3개 |
| 테스트 케이스 | 14개+ |

---

## 9. 핵심 파일 참조

| 파일 | 설명 |
|------|------|
| [ObservabilityNaming.cs](Src/Functorium/Applications/Observabilities/ObservabilityNaming.cs) | 네이밍 규칙 원천 |
| [OpenTelemetryBuilder.cs](Src/Functorium/Adapters/Observabilities/Builders/OpenTelemetryBuilder.cs) | 설정 허브 |
| [OpenTelemetrySpanFactory.cs](Src/Functorium/Adapters/Observabilities/Spans/OpenTelemetrySpanFactory.cs) | 트레이싱 구현 |
| [UsecaseTracingPipeline.cs](Src/Functorium/Applications/Pipelines/UsecaseTracingPipeline.cs) | 애플리케이션 레벨 트레이싱 |
| [UsecaseLoggingPipeline.cs](Src/Functorium/Applications/Pipelines/UsecaseLoggingPipeline.cs) | 구조화된 로깅 |
| [UsecaseMetricsPipeline.cs](Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs) | 메트릭 수집 |
| [ActivityContextHolder.cs](Src/Functorium/Adapters/Observabilities/Context/ActivityContextHolder.cs) | AsyncLocal 컨텍스트 |
