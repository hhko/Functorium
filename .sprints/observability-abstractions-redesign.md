# Observability Abstractions 재설계

**작성일**: 2026-01-10
**상태**: ✅ 완료

## 개요

Adapter 레이어를 순수 기술 관심사 레이어로 정립하기 위해 `Adapters/Observabilities/Abstractions` 폴더의 인터페이스들을 제거하고 OpenTelemetry API를 직접 사용하도록 재설계했습니다.

### 핵심 변경

1. **인터페이스 제거** - IObservabilityContext, ISpan, ISpanFactory, IMetricRecorder
2. **OpenTelemetry 직접 사용** - ActivitySource, IMeterFactory, Counter, Histogram
3. **구현체 제거** - OpenTelemetrySpan, OpenTelemetrySpanFactory, OpenTelemetryMetricRecorder
4. **DI 등록 단순화** - ActivatorUtilities.CreateInstance 직접 사용

---

## 1. 배경

### 기존 구조의 문제점

Observabilities 모듈이 Application 레이어에서 Adapter 레이어로 이동된 후, 기존 인터페이스 추상화가 불필요해졌습니다:

```
기존 구조:
Adapters/Observabilities/
├── Abstractions/           ← Application 레이어에서 정의된 인터페이스
│   ├── IObservabilityContext.cs
│   ├── ISpan.cs
│   ├── ISpanFactory.cs
│   └── IMetricRecorder.cs
├── Context/
│   └── ObservabilityContext.cs
├── Spans/
│   ├── OpenTelemetrySpan.cs
│   └── OpenTelemetrySpanFactory.cs
└── Metrics/
    └── OpenTelemetryMetricRecorder.cs
```

### 설계 원칙

> **"Adapter는 기술 관심사 레이어야"**

Adapter 레이어는 외부 기술(OpenTelemetry, Database, HTTP 등)과의 통합을 담당합니다. 이 레이어에서 자체 인터페이스 추상화는 불필요한 간접 계층을 만들 뿐입니다.

---

## 2. 변경 사항

### 삭제된 파일

| 파일 | 이유 |
|------|------|
| `Abstractions/IObservabilityContext.cs` | 불필요한 추상화 |
| `Abstractions/ISpan.cs` | Activity를 직접 사용 |
| `Abstractions/ISpanFactory.cs` | ActivitySource를 직접 사용 |
| `Abstractions/IMetricRecorder.cs` | IMeterFactory를 직접 사용 |
| `Context/ObservabilityContext.cs` | IObservabilityContext 제거로 불필요 |
| `Spans/OpenTelemetrySpan.cs` | ISpan 제거로 불필요 |
| `Spans/OpenTelemetrySpanFactory.cs` | ISpanFactory 제거로 불필요 |
| `Metrics/OpenTelemetryMetricRecorder.cs` | IMetricRecorder 제거로 불필요 |

### 수정된 파일

#### AdapterPipelineGenerator.cs (Source Generator)

생성되는 Pipeline 클래스가 인터페이스 대신 OpenTelemetry API를 직접 사용하도록 변경:

**변경 전**:
```csharp
// 생성자 파라미터
allParameters.Add(new ParameterInfo("parentContext",
    "global::Functorium.Adapters.Observabilities.Abstractions.IObservabilityContext", RefKind.None));
allParameters.Add(new ParameterInfo("spanFactory",
    "global::Functorium.Adapters.Observabilities.Abstractions.ISpanFactory", RefKind.None));
allParameters.Add(new ParameterInfo("metricRecorder",
    "global::Functorium.Adapters.Observabilities.Abstractions.IMetricRecorder", RefKind.None));

// 필드
private readonly ISpanFactory _spanFactory;
private readonly IMetricRecorder _metricRecorder;

// 사용
using ISpan? span = _spanFactory.CreateChildSpan(...);
_metricRecorder.RecordRequest(...);
```

**변경 후**:
```csharp
// 생성자 파라미터
allParameters.Add(new ParameterInfo("activitySource",
    "global::System.Diagnostics.ActivitySource", RefKind.None));
allParameters.Add(new ParameterInfo("meterFactory",
    "global::System.Diagnostics.Metrics.IMeterFactory", RefKind.None));

// 필드
private readonly ActivitySource _activitySource;
private readonly Counter<long> _requestCounter;
private readonly Counter<long> _responseCounter;
private readonly Histogram<double> _durationHistogram;

// 사용
using Activity? activity = _activitySource.StartActivity(...);
_requestCounter.Add(1, requestTags);
```

#### AdapterPipelineRegistration.cs (DI 등록)

IObservabilityContext 의존성 제거로 단순화:

**변경 전**:
```csharp
public static IServiceCollection RegisterScopedAdapterPipeline<TService, TImplementation>(
    this IServiceCollection services)
{
    return services.AddScoped<TService>(sp =>
    {
        IObservabilityContext? observabilityContext = ObservabilityContext.FromActivity(Activity.Current);
        var factory = ActivatorUtilities.CreateFactory(
            typeof(TImplementation),
            new[] { typeof(IObservabilityContext) });
        return (TImplementation)factory(sp, new object?[] { observabilityContext });
    });
}
```

**변경 후**:
```csharp
public static IServiceCollection RegisterScopedAdapterPipeline<TService, TImplementation>(
    this IServiceCollection services)
{
    return services.AddScoped<TService>(sp =>
        ActivatorUtilities.CreateInstance<TImplementation>(sp));
}
```

#### OpenTelemetryBuilder.cs (빌더 수정)

인터페이스 등록 제거 및 DefaultMeterFactory 추가:

**변경 전**:
```csharp
// DI 등록
_services.AddSingleton<ISpanFactory>(new OpenTelemetrySpanFactory(...));
_services.AddSingleton<IMetricRecorder>(new OpenTelemetryMetricRecorder(...));
```

**변경 후**:
```csharp
// DefaultMeterFactory 내부 구현 추가
private sealed class DefaultMeterFactory : IMeterFactory
{
    private readonly Dictionary<string, Meter> _meters = new();
    private readonly object _lock = new();

    public Meter Create(MeterOptions options) { ... }
    public void Dispose() { ... }
}

// DI 등록
_services.AddSingleton<IMeterFactory>(new DefaultMeterFactory());
```

---

## 3. 생성 코드 비교

### Pipeline 클래스 (Source Generator 출력)

**변경 전** (인터페이스 사용):
```csharp
public sealed class InMemoryProductRepositoryPipeline : IProductRepository
{
    private readonly InMemoryProductRepository _inner;
    private readonly ISpanFactory _spanFactory;
    private readonly IMetricRecorder _metricRecorder;

    public InMemoryProductRepositoryPipeline(
        InMemoryProductRepository inner,
        IObservabilityContext parentContext,
        ISpanFactory spanFactory,
        IMetricRecorder metricRecorder)
    {
        _inner = inner;
        _spanFactory = spanFactory;
        _metricRecorder = metricRecorder;
    }

    public async ValueTask<Fin<Product>> GetById(ProductId id, CancellationToken ct)
    {
        using ISpan? span = _spanFactory.CreateChildSpan(...);
        _metricRecorder.RecordRequest(...);

        var result = await _inner.GetById(id, ct);

        _metricRecorder.RecordResponse(...);
        return result;
    }
}
```

**변경 후** (OpenTelemetry 직접 사용):
```csharp
public sealed class InMemoryProductRepositoryPipeline : IProductRepository
{
    private readonly InMemoryProductRepository _inner;
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    public InMemoryProductRepositoryPipeline(
        InMemoryProductRepository inner,
        ActivitySource activitySource,
        IMeterFactory meterFactory)
    {
        _inner = inner;
        _activitySource = activitySource;

        var meter = meterFactory.Create(new MeterOptions("..."));
        _requestCounter = meter.CreateCounter<long>("...");
        _responseCounter = meter.CreateCounter<long>("...");
        _durationHistogram = meter.CreateHistogram<double>("...");
    }

    public async ValueTask<Fin<Product>> GetById(ProductId id, CancellationToken ct)
    {
        using Activity? activity = _activitySource.StartActivity(...);
        _requestCounter.Add(1, requestTags);

        var result = await _inner.GetById(id, ct);

        _responseCounter.Add(1, responseTags);
        _durationHistogram.Record(elapsed, tags);
        return result;
    }
}
```

---

## 4. 검증 결과

### 빌드 검증

```
dotnet build Functorium.slnx
  경고 0개
  오류 0개
```

### 단위 테스트

```
dotnet test --solution Functorium.slnx
  합계: 304
  실패: 0
  성공: 304
```

### E2E 검증 (Tutorial 프로젝트)

#### Cqrs03Functional

콘솔 애플리케이션 실행 결과:
- ✅ Tracing: Activity span 생성 정상 (TraceId, SpanId, ParentSpanId 계층 구조)
- ✅ Logging: 레이어별 로그 출력 정상 (INF, WRN, ERR)
- ✅ Metrics: 태그 기록 정상 (request.layer, request.category, response.status)

```
Activity.TraceId:            cb84b2f6ae26268b643a1b3c067dccb8
Activity.SpanId:             639233ead582230d
Activity.ParentSpanId:       b5acbfd8820b6824
Activity.DisplayName:        adapter Repository InMemoryProductRepository.ExistsByName
Activity.Tags:
    request.layer: adapter
    request.category: Repository
    response.elapsed: 85.27940000000001
    response.status: success
```

#### Cqrs04Endpoint

WebApi 실행 및 HTTP 요청 테스트:
- ✅ API 응답 정상
- ✅ TraceId/SpanId 계층 구조 유지
- ✅ Metrics 수집 정상 (requests, responses, duration)

```
Metric Name: application.usecase.query.requests
Metric Name: application.usecase.query.responses
Metric Name: application.usecase.query.duration
```

---

## 5. 아키텍처 비교

### 변경 전

```
Application Layer                    Adapter Layer
┌─────────────────┐                 ┌─────────────────────────────┐
│                 │                 │ Abstractions/               │
│                 │◀────────────────│   IObservabilityContext     │
│   Usecase       │                 │   ISpan                     │
│   Pipeline      │                 │   ISpanFactory              │
│                 │                 │   IMetricRecorder           │
│                 │                 ├─────────────────────────────┤
│                 │                 │ Implementations/            │
└─────────────────┘                 │   ObservabilityContext      │
                                    │   OpenTelemetrySpan         │
                                    │   OpenTelemetrySpanFactory  │
                                    │   OpenTelemetryMetricRecorder│
                                    └─────────────────────────────┘
                                                  │
                                                  ▼
                                    ┌─────────────────────────────┐
                                    │     OpenTelemetry API       │
                                    │   Activity, ActivitySource  │
                                    │   Meter, Counter, Histogram │
                                    └─────────────────────────────┘
```

### 변경 후

```
Application Layer                    Adapter Layer
┌─────────────────┐                 ┌─────────────────────────────┐
│                 │                 │                             │
│   Usecase       │                 │   Generated Pipeline        │
│   Pipeline      │                 │   (Source Generator)        │
│                 │                 │                             │
│                 │                 │   직접 사용:                │
│                 │                 │   - ActivitySource          │
└─────────────────┘                 │   - IMeterFactory           │
                                    │   - Counter<long>           │
                                    │   - Histogram<double>       │
                                    └─────────────────────────────┘
                                                  │
                                                  ▼
                                    ┌─────────────────────────────┐
                                    │     OpenTelemetry API       │
                                    │   Activity, ActivitySource  │
                                    │   Meter, Counter, Histogram │
                                    └─────────────────────────────┘
```

---

## 6. 결론

### 달성 목표

| 목표 | 결과 |
|------|------|
| 인터페이스 제거 | ✅ 4개 인터페이스 삭제 |
| 구현체 제거 | ✅ 4개 클래스 삭제 |
| OpenTelemetry 직접 사용 | ✅ ActivitySource, IMeterFactory |
| DI 등록 단순화 | ✅ ActivatorUtilities.CreateInstance |
| 기능 동일성 유지 | ✅ Tutorial 프로젝트 검증 완료 |

### 코드 감소

- **삭제**: 8개 파일, 약 500줄
- **수정**: 3개 파일 (Source Generator, DI Registration, Builder)
- **테스트 업데이트**: 25개 스냅샷 파일

### 하위 호환성

> **하위 호환성 무시** (요구사항에 따름)

기존 `ISpanFactory`, `IMetricRecorder`를 사용하던 코드는 컴파일 에러가 발생합니다.
Source Generator로 생성된 코드 재생성이 필요합니다.

---

## 7. 관련 커밋

```
74b1853 refactor(observabilities): Abstractions 인터페이스 제거 및 OpenTelemetry 직접 사용
```

---

## 8. 관련 파일

| 파일 | 설명 |
|------|------|
| [AdapterPipelineGenerator.cs](../Src/Functorium.Adapters.SourceGenerator/AdapterPipelineGenerator.cs) | Source Generator |
| [AdapterPipelineRegistration.cs](../Src/Functorium/Abstractions/Registrations/AdapterPipelineRegistration.cs) | DI 등록 |
| [OpenTelemetryBuilder.cs](../Src/Functorium/Adapters/Observabilities/Builders/OpenTelemetryBuilder.cs) | OpenTelemetry 빌더 |
| [Cqrs03Functional](../Tutorials/Cqrs03Functional/) | 콘솔 검증 프로젝트 |
| [Cqrs04Endpoint](../Tutorials/Cqrs04Endpoint/) | WebApi 검증 프로젝트 |
