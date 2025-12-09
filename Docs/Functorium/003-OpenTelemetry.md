# OpenTelemetry Options 설정 가이드

이 문서는 `OpenTelemetryOptions` 클래스를 사용하여 OpenTelemetry 설정을 구성하는 방법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [속성 설명](#속성-설명)
- [Protocol 설정](#protocol-설정)
- [엔드포인트 설정](#엔드포인트-설정)
- [의존성 등록](#의존성-등록)
- [appsettings.json 설정](#appsettingsjson-설정)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

- OpenTelemetry Logs, Traces, Metrics 수집기 엔드포인트 설정
- OTLP Protocol (gRPC, HTTP/Protobuf) 선택
- 샘플링 및 Prometheus Exporter 설정
- 애플리케이션 시작 시 설정 정보 자동 로깅

### 관련 파일

| 파일 | 역할 |
|------|------|
| `OpenTelemetryOptions.cs` | OpenTelemetry 설정 Options 클래스 |
| `IOpenTelemetryOptions.cs` | OpenTelemetry 설정 인터페이스 |
| `IStartupOptionsLogger.cs` | 시작 시 자동 로깅 인터페이스 |
| `OptionsConfigurator.cs` | Options 등록 확장 메서드 |

<br/>

## 요약

### 주요 명령

```csharp
// 의존성 등록
services.RegisterConfigureOptions<OpenTelemetryOptions, OpenTelemetryOptions.Validator>(
    OpenTelemetryOptions.SectionName);

// IOptions<T>로 주입받아 사용
public class MyService(IOptions<OpenTelemetryOptions> options)
{
    private readonly OpenTelemetryOptions _options = options.Value;
}
```

### 주요 절차

1. `appsettings.json`에 `OpenTelemetry` 섹션 추가
2. `Program.cs`에서 `RegisterConfigureOptions` 호출
3. OpenTelemetry SDK 초기화 시 `IOptions<OpenTelemetryOptions>` 사용
4. 애플리케이션 시작 시 설정 정보 자동 로깅 확인

### 주요 개념

| 개념 | 설명 |
|------|------|
| 통합 엔드포인트 | `CollectorEndpoint`로 모든 신호(Logs, Traces, Metrics) 전송 |
| 개별 엔드포인트 | 신호별 독립적인 엔드포인트 설정 가능 |
| 폴백 패턴 | 개별 설정이 없으면 통합 설정 사용 |
| SmartEnum | `OtlpCollectorProtocol`로 타입 안전한 Protocol 관리 |

<br/>

## 속성 설명

### 서비스 정보

| 속성 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `ServiceName` | `string` | `""` | 서비스 이름 (필수) |
| `ServiceVersion` | `string` | 어셈블리 버전 | 자동 설정됨 |

```csharp
public string ServiceName { get; set; } = string.Empty;

// 어셈블리 버전으로 자동 설정
public string ServiceVersion { get; } =
    Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
```

### 통합 설정

| 속성 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `CollectorEndpoint` | `string` | `""` | OTLP Collector 엔드포인트 |
| `CollectorProtocol` | `string` | `"Grpc"` | OTLP Protocol (Grpc, HttpProtobuf) |

### 개별 엔드포인트 설정

| 속성 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `TracingCollectorEndpoint` | `string?` | `null` | Tracing 전용 엔드포인트 |
| `MetricsCollectorEndpoint` | `string?` | `null` | Metrics 전용 엔드포인트 |
| `LoggingCollectorEndpoint` | `string?` | `null` | Logging 전용 엔드포인트 |

### 개별 Protocol 설정

| 속성 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `TracingCollectorProtocol` | `string?` | `null` | Tracing 전용 Protocol |
| `MetricsCollectorProtocol` | `string?` | `null` | Metrics 전용 Protocol |
| `LoggingCollectorProtocol` | `string?` | `null` | Logging 전용 Protocol |

### 추가 설정

| 속성 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `SamplingRate` | `double` | `1.0` | 샘플링 비율 (0.0 ~ 1.0) |
| `EnablePrometheusExporter` | `bool` | `false` | Prometheus Exporter 활성화 |

<br/>

## Protocol 설정

### OtlpCollectorProtocol SmartEnum

```csharp
public sealed class OtlpCollectorProtocol : SmartEnum<OtlpCollectorProtocol>
{
    public static readonly OtlpCollectorProtocol Grpc = new(nameof(Grpc), 1);
    public static readonly OtlpCollectorProtocol HttpProtobuf = new(nameof(HttpProtobuf), 2);
}
```

| Protocol | 값 | 설명 |
|----------|-----|------|
| `Grpc` | 1 | gRPC 프로토콜 (기본값) |
| `HttpProtobuf` | 2 | HTTP/Protobuf 프로토콜 |

### Protocol 조회 메서드

```csharp
// 개별 설정 우선, 없으면 통합 Protocol 반환
public OtlpCollectorProtocol GetTracingProtocol()
public OtlpCollectorProtocol GetMetricsProtocol()
public OtlpCollectorProtocol GetLogsProtocol()
```

**폴백 순서:**
1. 개별 Protocol 설정 확인 (예: `TracingCollectorProtocol`)
2. 없으면 통합 Protocol 사용 (`CollectorProtocol`)
3. 파싱 실패 시 `Grpc` 기본값 반환

<br/>

## 엔드포인트 설정

### 엔드포인트 조회 메서드

```csharp
public string GetTracingEndpoint()
public string GetMetricsEndpoint()
public string GetLoggingEndpoint()
```

### 엔드포인트 값에 따른 동작

| 값 | 동작 |
|----|------|
| `null` | `CollectorEndpoint` 사용 (기본값) |
| `""` (빈 문자열) | 해당 신호 비활성화 |
| `"http://..."` | 해당 엔드포인트 사용 |

**예시:**
```csharp
// TracingCollectorEndpoint가 null → CollectorEndpoint 사용
// TracingCollectorEndpoint가 "" → Tracing 비활성화
// TracingCollectorEndpoint가 "http://localhost:4317" → 해당 값 사용
```

<br/>

## 의존성 등록

### Program.cs 설정

```csharp
// OpenTelemetryOptions 등록
services.RegisterConfigureOptions<OpenTelemetryOptions, OpenTelemetryOptions.Validator>(
    OpenTelemetryOptions.SectionName);

// StartupLogger 등록 (IStartupOptionsLogger 자동 수집)
services.AddHostedService<StartupLogger>();
```

### 검증 규칙

`OpenTelemetryOptions.Validator`에서 정의된 검증 규칙:

| 속성 | 규칙 |
|------|------|
| `ServiceName` | 필수 (비어있으면 안됨) |
| 엔드포인트 | 최소 하나의 엔드포인트 필수 |
| `SamplingRate` | 0.0 ~ 1.0 범위 |
| Protocol | `Grpc` 또는 `HttpProtobuf` |

```csharp
public sealed class Validator : AbstractValidator<OpenTelemetryOptions>
{
    public Validator()
    {
        RuleFor(x => x.ServiceName)
            .NotEmpty()
            .WithMessage($"{nameof(ServiceName)} is required.");

        RuleFor(x => x)
            .Must(options =>
                !string.IsNullOrWhiteSpace(options.CollectorEndpoint) ||
                !string.IsNullOrWhiteSpace(options.TracingCollectorEndpoint) ||
                !string.IsNullOrWhiteSpace(options.MetricsCollectorEndpoint) ||
                !string.IsNullOrWhiteSpace(options.LoggingCollectorEndpoint))
            .WithMessage("At least one OTLP endpoint must be configured.");

        RuleFor(x => x.SamplingRate)
            .InclusiveBetween(0.0, 1.0);
    }
}
```

<br/>

## appsettings.json 설정

### 기본 설정 (통합 엔드포인트)

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": false
  }
}
```

### 개별 엔드포인트 설정

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "TracingCollectorEndpoint": "http://localhost:21890",
    "MetricsCollectorEndpoint": "http://localhost:21891",
    "LoggingCollectorEndpoint": "http://localhost:21892",
    "SamplingRate": 0.5,
    "EnablePrometheusExporter": true
  }
}
```

### 특정 신호 비활성화

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317",
    "TracingCollectorEndpoint": "",
    "SamplingRate": 1.0
  }
}
```

> **참고**: `TracingCollectorEndpoint`를 빈 문자열로 설정하면 Tracing만 비활성화됩니다.

### Aspire Dashboard 연동

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://127.0.0.1:18889",
    "CollectorProtocol": "Grpc"
  }
}
```

<br/>

## 트러블슈팅

### 서비스 이름이 비어있다는 오류

**증상:**
```
Option Validation failed for 'OpenTelemetryOptions.ServiceName': ServiceName is required.
```

**해결:**
```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService"
  }
}
```

### 엔드포인트가 설정되지 않았다는 오류

**증상:**
```
At least one OTLP endpoint must be configured: CollectorEndpoint or individual endpoints.
```

**해결:**
```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317"
  }
}
```

### Protocol 검증 실패

**증상:**
```
CollectorProtocol must be one of: Grpc, HttpProtobuf
```

**해결:**
```json
{
  "OpenTelemetry": {
    "CollectorProtocol": "Grpc"
  }
}
```

> **유효한 값**: `Grpc`, `HttpProtobuf` (대소문자 구분)

### 시작 로그에 설정이 출력되지 않음

**원인**: `StartupLogger`가 등록되지 않았거나 `IStartupOptionsLogger`가 DI에 등록되지 않음

**해결:**
```csharp
// 1. RegisterConfigureOptions로 등록 (IStartupOptionsLogger 자동 등록)
services.RegisterConfigureOptions<OpenTelemetryOptions, OpenTelemetryOptions.Validator>(
    OpenTelemetryOptions.SectionName);

// 2. StartupLogger 등록
services.AddHostedService<StartupLogger>();
```

<br/>

## FAQ

### Q1. 통합 엔드포인트와 개별 엔드포인트를 동시에 설정하면 어떻게 되나요?

개별 엔드포인트가 우선됩니다. 개별 설정이 `null`이면 통합 엔드포인트를 사용하고, 빈 문자열(`""`)이면 해당 신호가 비활성화됩니다.

```json
{
  "OpenTelemetry": {
    "CollectorEndpoint": "http://localhost:4317",
    "TracingCollectorEndpoint": "http://trace-server:4317",
    "MetricsCollectorEndpoint": null,
    "LoggingCollectorEndpoint": ""
  }
}
```

| 신호 | 사용 엔드포인트 |
|------|----------------|
| Tracing | `http://trace-server:4317` (개별 설정) |
| Metrics | `http://localhost:4317` (통합 설정) |
| Logging | 비활성화 (빈 문자열) |

### Q2. SamplingRate 1.0은 무엇을 의미하나요?

`SamplingRate`는 0.0 ~ 1.0 범위의 값으로, 샘플링할 Trace의 비율을 나타냅니다.

| 값 | 의미 |
|----|------|
| `1.0` | 100% 샘플링 (모든 Trace 수집) |
| `0.5` | 50% 샘플링 |
| `0.0` | 0% 샘플링 (Trace 수집 안함) |

### Q3. ServiceVersion은 왜 자동 설정되나요?

`ServiceVersion`은 `Assembly.GetEntryAssembly()?.GetName().Version`으로 자동 설정됩니다. `Directory.Build.props`의 `<AssemblyVersion>` 값과 동기화되어 버전 관리가 일관됩니다.

```csharp
public string ServiceVersion { get; } =
    Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
```

### Q4. gRPC와 HTTP/Protobuf 중 어떤 것을 선택해야 하나요?

| Protocol | 장점 | 사용 시기 |
|----------|------|----------|
| `Grpc` | 효율적, 양방향 스트리밍 | 기본 선택, 내부 네트워크 |
| `HttpProtobuf` | 방화벽 친화적, 디버깅 용이 | 외부 네트워크, 프록시 환경 |

### Q5. Prometheus Exporter는 언제 사용하나요?

`EnablePrometheusExporter`를 `true`로 설정하면 Prometheus가 스크래핑할 수 있는 `/metrics` 엔드포인트가 노출됩니다. OTLP Push 방식 대신 Pull 방식으로 메트릭을 수집할 때 사용합니다.

```json
{
  "OpenTelemetry": {
    "EnablePrometheusExporter": true
  }
}
```

### Q6. 시작 시 로그 출력 형식은 어떻게 되나요?

`StartupLogger`가 `IStartupOptionsLogger.LogConfiguration()`을 호출하여 다음과 같은 형식으로 출력합니다:

```
OpenTelemetry Configuration

  Service Information
    Name                : MyService
    Version             : 1.0.0.0

  Logging Configuration
    Endpoint            : http://localhost:4317
    Protocol            : Grpc

  Tracing Configuration
    Endpoint            : http://localhost:4317
    Protocol            : Grpc

  Metrics Configuration
    Endpoint            : http://localhost:4317
    Protocol            : Grpc

  Additional Settings
    Sampling Rate       : 100%
    Prometheus Exporter : Disabled
```
