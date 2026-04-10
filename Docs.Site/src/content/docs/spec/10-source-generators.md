---
title: "소스 생성기 사양"
---

Functorium 프레임워크가 제공하는 소스 생성기(Source Generator)의 API 사양입니다. 모든 생성기는 Roslyn `IIncrementalGenerator` 기반이며, `Functorium.SourceGenerators` 패키지에 포함되어 있습니다. 실전 활용법은 [Source Generator Observability 튜토리얼](../tutorials/sourcegen-observability/)을 참조하십시오.

## 요약

### 주요 타입

| 생성기 | 트리거 어트리뷰트 | 생성 대상 |
|--------|-----------------|----------|
| `EntityIdGenerator` | `[GenerateEntityId]` | `{Entity}Id` 구조체, Comparer, Converter |
| `ObservablePortGenerator` | `[GenerateObservablePort]` | `{Class}Observable` 래퍼 클래스 (Tracing, Logging, Metrics) |
| `CtxEnricherGenerator` | _(자동 감지)_ | `IUsecaseCtxEnricher` 구현체 |
| `DomainEventCtxEnricherGenerator` | _(자동 감지)_ | `IDomainEventCtxEnricher` 구현체 |
| `UnionTypeGenerator` | `[UnionType]` | `Match`, `Switch`, `Is{Case}`, `As{Case}` 메서드 |

### 보조 어트리뷰트

| 어트리뷰트 | 네임스페이스 | 적용 대상 | 설명 |
|-----------|-------------|----------|------|
| `[ObservablePortIgnore]` | `Functorium.Adapters.SourceGenerators` | 메서드 | Observable 생성에서 해당 메서드 제외 |
| `[CtxIgnore]` | `Functorium.Applications.Usecases` | 클래스, 프로퍼티, 파라미터 | CtxEnricher 생성에서 제외 |
| `[CtxRoot]` | `Functorium.Abstractions.Observabilities` | 인터페이스, 프로퍼티, 파라미터 | ctx 루트 레벨로 승격 |

### 진단 코드

| 코드 | 심각도 | 생성기 | 설명 |
|------|--------|--------|------|
| `FUNCTORIUM001` | Error | `ObservablePortGenerator` | 생성자 파라미터 타입 중복 |
| `FUNCTORIUM002` | Warning | `CtxEnricherGenerator`, `DomainEventCtxEnricherGenerator` | ctx 필드 타입 충돌 (OpenSearch 매핑) |
| `FUNCTORIUM003` | Warning | `CtxEnricherGenerator` | Request 타입 접근 불가 |
| `FUNCTORIUM004` | Warning | `DomainEventCtxEnricherGenerator` | Event 타입 접근 불가 |

---

## 공통 인프라

### IncrementalGeneratorBase\<TValue\>

모든 생성기의 추상 기반 클래스입니다. `IIncrementalGenerator`를 구현하며, 파이프라인 등록과 소스 출력을 표준화합니다.

```csharp
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    bool AttachDebugger = false) : IIncrementalGenerator
```

| 파라미터 | 설명 |
|---------|------|
| `registerSourceProvider` | 구문/시맨틱 분석 파이프라인을 등록하고 `IncrementalValuesProvider<TValue>`를 반환 |
| `generate` | 수집된 메타데이터 배열로부터 소스 파일을 생성 |
| `AttachDebugger` | `true`이면 DEBUG 빌드에서 `Debugger.Launch()` 호출 |

**동작 흐름**: `Initialize` -> `registerSourceProvider`로 `IncrementalValuesProvider<TValue>` 생성 -> `null` 필터링 -> `Collect()` -> `generate` 호출

---

## EntityIdGenerator

Entity 클래스에 `[GenerateEntityId]`를 적용하면 Ulid 기반 ID 타입, EF Core Comparer, EF Core Converter를 자동 생성합니다.

### 트리거

```csharp
// Functorium.Domains.Entities
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateEntityIdAttribute : Attribute;
```

**대상**: `[GenerateEntityId]`가 적용된 `class` 선언

### 생성 대상

`{EntityName}` 클래스에 대해 다음 세 타입을 단일 `.g.cs` 파일로 생성합니다.

| 생성 타입 | 설명 |
|----------|------|
| `{EntityName}Id` | `readonly partial record struct` -- Ulid 기반 Entity ID |
| `{EntityName}IdComparer` | EF Core `ValueComparer<{EntityName}Id>` |
| `{EntityName}IdConverter` | EF Core `ValueConverter<{EntityName}Id, string>` |

### 생성 코드 구조

#### {EntityName}Id

```csharp
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof({EntityName}IdJsonConverter))]
[TypeConverter(typeof({EntityName}IdTypeConverter))]
public readonly partial record struct {EntityName}Id :
    IEntityId<{EntityName}Id>,
    IParsable<{EntityName}Id>
{
    public const string Name = "{EntityName}Id";
    public const string Namespace = "{Namespace}";
    public static readonly {EntityName}Id Empty;
    public Ulid Value { get; init; }

    // Factory Methods
    public static {EntityName}Id New();
    public static {EntityName}Id Create(Ulid id);
    public static {EntityName}Id Create(string id);

    // IComparable<T>
    public int CompareTo({EntityName}Id other);

    // IParsable<T>
    public static {EntityName}Id Parse(string s, IFormatProvider? provider);
    public static bool TryParse(string? s, IFormatProvider? provider, out {EntityName}Id result);

    public override string ToString();

    // 내부 중첩 클래스
    internal sealed class {EntityName}IdJsonConverter : JsonConverter<{EntityName}Id>;
    internal sealed class {EntityName}IdTypeConverter : TypeConverter;
}
```

#### {EntityName}IdComparer

```csharp
public sealed class {EntityName}IdComparer : ValueComparer<{EntityName}Id>;
```

#### {EntityName}IdConverter

```csharp
public sealed class {EntityName}IdConverter : ValueConverter<{EntityName}Id, string>;
```

### 진단 코드

EntityIdGenerator는 현재 전용 진단 코드를 발행하지 않습니다.

---

## ObservablePortGenerator

Adapter 클래스에 `[GenerateObservablePort]`를 적용하면 OpenTelemetry 기반 Observability(Tracing, Logging, Metrics)를 제공하는 Observable 래퍼 클래스를 자동 생성합니다.

### 트리거

```csharp
// Functorium.Adapters.SourceGenerators
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateObservablePortAttribute : Attribute;
```

**대상**: `[GenerateObservablePort]`가 적용된 `class`이며, `IObservablePort`를 상속하는 인터페이스의 메서드 중 `FinT<IO, T>` 반환 타입을 가진 메서드를 대상으로 합니다.

**제외 조건**: 메서드에 `[ObservablePortIgnore]`가 적용된 경우 해당 메서드는 생성에서 제외됩니다.

```csharp
// Functorium.Adapters.SourceGenerators
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ObservablePortIgnoreAttribute : Attribute;
```

### 생성 대상

`{ClassName}`에 대해 다음을 생성합니다.

| 생성 타입 | 설명 |
|----------|------|
| `{ClassName}Observable` | 원본 클래스를 상속하는 Observable 래퍼 클래스 |
| `{ClassName}ObservableLoggers` | `LoggerMessage.Define` 기반 고성능 로깅 확장 메서드 static 클래스 |

### 생성 코드 구조

#### {ClassName}Observable

```csharp
public class {ClassName}Observable : {ClassName}
{
    // 인프라 필드
    private readonly ActivitySource _activitySource;
    private readonly ILogger<{ClassName}Observable> _logger;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    // 생성자 (DI 파라미터 + 부모 생성자 파라미터)
    public {ClassName}Observable(
        ActivitySource activitySource,
        ILogger<{ClassName}Observable> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        ... /* 부모 생성자 파라미터 */);

    // IObservablePort 인터페이스 메서드 override
    public override FinT<IO, TResult> {MethodName}(...);
}
```

**각 override 메서드가 제공하는 Observability:**

| 항목 | 내용 |
|------|------|
| **Tracing** | `ActivitySource.StartActivity`로 span 생성, 성공/실패 상태 기록 |
| **Logging** | Request(Debug/Info), Response 성공(Debug/Info), Response 실패(Warning/Error) 4단계 |
| **Metrics** | `adapter.{category}.requests` Counter, `adapter.{category}.responses` Counter, `adapter.{category}.duration` Histogram |

**생성자 파라미터 이름 충돌 해결**: 부모 클래스의 생성자 파라미터 이름이 예약된 이름(`activitySource`, `logger`, `meterFactory`, `openTelemetryOptions`)과 충돌하면 `base` 접두사가 붙습니다. 예: `logger` -> `baseLogger`

#### {ClassName}ObservableLoggers

`LoggerMessage.Define`을 사용한 고성능 정적 로깅 메서드를 생성합니다.

```csharp
internal static class {ClassName}ObservableLoggers
{
    // LoggerMessage.Define 기반 delegate 필드 (파라미터 6개 이하)
    private static readonly Action<ILogger, ...> _logAdapterRequest_{ClassName}_{MethodName};
    private static readonly Action<ILogger, ...> _logAdapterRequestDebug_{ClassName}_{MethodName};
    private static readonly Action<ILogger, ...> _logAdapterResponseSuccess_{ClassName}_{MethodName};

    // 확장 메서드
    public static void LogAdapterRequest_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterRequestDebug_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseSuccessDebug_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseSuccess_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseWarning_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseError_{ClassName}_{MethodName}(this ILogger logger, ...);
}
```

### 진단 코드

| 코드 | 심각도 | 메시지 |
|------|--------|--------|
| **FUNCTORIUM001** | Error | `Observable constructor for '{ClassName}' contains multiple parameters of the same type '{TypeName}'.` -- 생성자 파라미터(부모 + Observable 고유)에 동일 타입이 존재하면 DI 해석 충돌이 발생하므로 코드 생성을 중단합니다. |

---

## CtxEnricherGenerator

`ICommandRequest<TSuccess>` 또는 `IQueryRequest<TSuccess>`를 구현하는 `record`를 자동 감지하여 `IUsecaseCtxEnricher` 구현체를 생성합니다. 별도의 트리거 어트리뷰트 없이 인터페이스 구현만으로 동작합니다.

### 트리거

**자동 감지 조건:**

1. `record` 선언이어야 합니다.
2. `ICommandRequest<TSuccess>` 또는 `IQueryRequest<TSuccess>` 인터페이스를 구현해야 합니다.
3. `[CtxIgnore]`가 클래스 레벨에 적용되지 않아야 합니다.
4. 타입이 `public` 또는 `internal` 접근성이어야 합니다 (`private`/`protected`이면 FUNCTORIUM003 경고).

**제외 어트리뷰트:**

```csharp
// Functorium.Applications.Usecases
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false, Inherited = false)]
public sealed class CtxIgnoreAttribute : Attribute;
```

**승격 어트리뷰트:**

```csharp
// Functorium.Abstractions.Observabilities
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false, Inherited = false)]
public sealed class CtxRootAttribute : Attribute;
```

### 생성 대상

| 생성 타입 | 설명 |
|----------|------|
| `{ContainingTypes}{RequestTypeName}CtxEnricher` | `partial class`, `IUsecaseCtxEnricher<TRequest, FinResponse<TSuccess>>` 구현체 |

### 생성 코드 구조

```csharp
public partial class {ContainingTypes}{RequestTypeName}CtxEnricher
    : IUsecaseCtxEnricher<{RequestFullType}, FinResponse<{ResponseFullType}>>
{
    // Request 속성을 LogContext에 Push
    public IDisposable? EnrichRequestLog({RequestFullType} request);

    // Response 속성을 LogContext에 Push (Succ 패턴 매칭)
    public IDisposable? EnrichResponseLog(
        {RequestFullType} request,
        FinResponse<{ResponseFullType}> response);

    // 확장 포인트 (사용자가 partial로 구현 가능)
    partial void OnEnrichRequestLog(
        {RequestFullType} request,
        List<IDisposable> disposables);

    partial void OnEnrichResponseLog(
        {RequestFullType} request,
        FinResponse<{ResponseFullType}> response,
        List<IDisposable> disposables);

    // 헬퍼 메서드
    private static void PushRequestCtx(List<IDisposable> disposables, string fieldName, object? value);
    private static void PushResponseCtx(List<IDisposable> disposables, string fieldName, object? value);
    private static void PushRootCtx(...);  // [CtxRoot] 속성이 있을 때만 생성
}
```

**ctx 필드 네이밍 규칙:**

| 조건 | ctx 필드 패턴 | 예시 |
|------|-------------|------|
| 기본 | `ctx.{containing_types}.request.{snake_case_name}` | `ctx.place_order_command.request.customer_id` |
| `[CtxRoot]` 적용 | `ctx.{snake_case_name}` | `ctx.customer_id` |
| 인터페이스에서 상속 | `ctx.{interface_name}.{snake_case_name}` | `ctx.operator_context.operator_id` |
| 컬렉션 타입 | `...{snake_case_name}_count` | `ctx.place_order_command.request.items_count` |

**속성 필터링 규칙:**

- 스칼라 타입(primitive, string, DateTime, Guid, enum, Option\<T\> 등): 값 그대로 출력
- 컬렉션 타입(List, Array, Seq 등): `_count` 접미사로 개수만 출력
- 복합 타입(class, record, struct): 제외
- `[CtxIgnore]` 적용 프로퍼티: 제외

### 진단 코드

| 코드 | 심각도 | 메시지 |
|------|--------|--------|
| **FUNCTORIUM002** | Warning | `ctx field '{FieldName}' has conflicting types: '{Type1}' ({Group1}) in '{Enricher1}' vs '{Type2}' ({Group2}) in '{Enricher2}'.` -- 서로 다른 Enricher에서 같은 ctx 필드명에 다른 OpenSearch 타입 그룹을 할당하면 동적 매핑 충돌이 발생합니다. |
| **FUNCTORIUM003** | Warning | `'{RequestType}' implements ICommandRequest/IQueryRequest but CtxEnricher cannot be generated because '{TypeName}' is {accessibility}.` -- `private`이나 `protected` 타입에는 Enricher를 생성할 수 없습니다. `[CtxIgnore]`를 적용하여 경고를 억제하십시오. |

---

## DomainEventCtxEnricherGenerator

`IDomainEventHandler<TEvent>`를 구현하는 클래스를 자동 감지하여 `TEvent`에 대한 `IDomainEventCtxEnricher` 구현체를 생성합니다. 같은 이벤트 타입에 여러 Handler가 있어도 Enricher는 한 번만 생성됩니다.

### 트리거

**자동 감지 조건:**

1. `class` 선언이어야 합니다.
2. `IDomainEventHandler<TEvent>` 인터페이스를 구현해야 합니다.
3. `TEvent`가 `abstract`가 아니어야 합니다.
4. `TEvent`에 `[CtxIgnore]`가 클래스 레벨에 적용되지 않아야 합니다.
5. `TEvent`가 `public` 또는 `internal` 접근성이어야 합니다 (`private`/`protected`이면 FUNCTORIUM004 경고).

### 생성 대상

| 생성 타입 | 설명 |
|----------|------|
| `{ContainingTypes}{EventTypeName}CtxEnricher` | `partial class`, `IDomainEventCtxEnricher<TEvent>` 구현체 |

### 생성 코드 구조

```csharp
public partial class {ContainingTypes}{EventTypeName}CtxEnricher
    : IDomainEventCtxEnricher<{EventFullType}>
{
    // 이벤트 속성을 LogContext에 Push
    public IDisposable? EnrichLog({EventFullType} domainEvent);

    // 확장 포인트 (사용자가 partial로 구현 가능)
    partial void OnEnrichLog(
        {EventFullType} domainEvent,
        List<IDisposable> disposables);

    // 헬퍼 메서드
    private static void PushEventCtx(List<IDisposable> disposables, string fieldName, object? value);
    private static void PushRootCtx(...);  // [CtxRoot] 속성이 있을 때만 생성
}
```

**속성 필터링 규칙:**

`CtxEnricherGenerator`와 동일한 규칙을 따르며, 추가로 `IDomainEvent` 기본 속성(`OccurredAt`, `EventId`, `CorrelationId`, `CausationId`)은 자동 제외됩니다. `IValueObject` 또는 `IEntityId<T>` 구현체 프로퍼티는 `.ToString()` 호출로 keyword 변환됩니다.

**ctx 필드 네이밍 규칙:**

| 조건 | ctx 필드 패턴 | 예시 |
|------|-------------|------|
| 최상위 이벤트 | `ctx.{snake_case_event}.{snake_case_name}` | `ctx.order_placed_event.order_id` |
| 중첩 이벤트 | `ctx.{containing}.{snake_case_event}.{snake_case_name}` | `ctx.order.created_event.amount` |
| `[CtxRoot]` 적용 | `ctx.{snake_case_name}` | `ctx.order_id` |

### 진단 코드

| 코드 | 심각도 | 메시지 |
|------|--------|--------|
| **FUNCTORIUM002** | Warning | ctx 필드 타입 충돌 (CtxEnricherGenerator와 동일 ID 공유) |
| **FUNCTORIUM004** | Warning | `'{EventType}' implements IDomainEvent but DomainEventCtxEnricher cannot be generated because '{TypeName}' is {accessibility}.` -- `private`이나 `protected` 이벤트 타입에는 Enricher를 생성할 수 없습니다. `[CtxIgnore]`를 적용하여 경고를 억제하십시오. |

---

## UnionTypeGenerator

`abstract partial record`에 `[UnionType]`을 적용하면 내부 `sealed record` 케이스를 분석하여 `Match`/`Switch` 패턴 매칭 메서드를 자동 생성합니다.

### 트리거

```csharp
// Functorium.Domains.ValueObjects.Unions
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UnionTypeAttribute : Attribute;
```

**대상**: `[UnionType]`이 적용된 `abstract partial record` 선언이며, 직접 상속하는 `sealed record` 케이스가 1개 이상 있어야 합니다.

### 생성 대상

`{TypeName}` record에 대해 `partial` 확장으로 다음 멤버를 생성합니다.

| 생성 멤버 | 시그니처 |
|----------|---------|
| `Match<TResult>` | 모든 케이스에 대한 Func 파라미터를 받아 `TResult` 반환 |
| `Switch` | 모든 케이스에 대한 Action 파라미터를 받아 실행 |
| `Is{CaseName}` | `bool` 프로퍼티 -- `this is {CaseName}` |
| `As{CaseName}()` | `{CaseName}?` 반환 -- `this as {CaseName}` |

### 생성 코드 구조

```csharp
public abstract partial record {TypeName}
{
    // 패턴 매칭 (반환값 있음)
    public TResult Match<TResult>(
        Func<Case1, TResult> case1,
        Func<Case2, TResult> case2,
        ...);

    // 패턴 매칭 (반환값 없음)
    public void Switch(
        Action<Case1> case1,
        Action<Case2> case2,
        ...);

    // 타입 검사 프로퍼티
    public bool IsCase1 => this is Case1;
    public bool IsCase2 => this is Case2;

    // 안전한 타입 변환
    public Case1? AsCase1() => this as Case1;
    public Case2? AsCase2() => this as Case2;
}
```

**도달 불가능한 케이스 처리**: `Match`/`Switch`의 `default` 분기는 `UnreachableCaseException`을 throw합니다.

```csharp
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
```

### 진단 코드

UnionTypeGenerator는 현재 전용 진단 코드를 발행하지 않습니다. 내부 `sealed record` 케이스가 없으면 코드 생성을 건너뜁니다.

---

## 관련 문서

- [Source Generator Observability 튜토리얼](../tutorials/sourcegen-observability/) -- Roslyn API 기초부터 실전 생성기 구현까지
- [엔티티와 애그리거트 사양](./01-entity-aggregate) -- `IEntityId<T>`, `GenerateEntityIdAttribute` 정의
- [관찰 가능성 사양](./08-observability) -- `IUsecaseCtxEnricher`, `IDomainEventCtxEnricher` 인터페이스 정의
- [Adapter 파이프라인과 DI 가이드](../guides/adapter/14a-adapter-pipeline-di) -- Observable 래퍼 DI 등록 패턴
- [테스트 라이브러리 가이드](../guides/testing/16-testing-library) -- 소스 생성기 단위 테스트 작성법
