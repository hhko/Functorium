---
title: "Source Generators Specification"
---

This is the API specification for source generators provided by the Functorium framework. All generators are based on Roslyn `IIncrementalGenerator` and included in the `Functorium.SourceGenerators` package. For practical usage, see the [Source Generator Observability Tutorial](../tutorials/sourcegen-observability/).

## Summary

### Key Types

| Generator | Trigger Attribute | Generation Target |
|--------|-----------------|----------|
| `EntityIdGenerator` | `[GenerateEntityId]` | `{Entity}Id` struct, Comparer, Converter |
| `ObservablePortGenerator` | `[GenerateObservablePort]` | `{Class}Observable` wrapper class (Tracing, Logging, Metrics) |
| `CtxEnricherGenerator` | _(auto-detected)_ | `IUsecaseCtxEnricher` implementation |
| `DomainEventCtxEnricherGenerator` | _(auto-detected)_ | `IDomainEventCtxEnricher` implementation |
| `UnionTypeGenerator` | `[UnionType]` | `Match`, `Switch`, `Is{Case}`, `As{Case}` methods |

### Auxiliary Attributes

| Attribute | Namespace | Target | Description |
|-----------|-------------|----------|------|
| `[ObservablePortIgnore]` | `Functorium.Adapters.SourceGenerators` | Method | Excludes the method from Observable generation |
| `[CtxIgnore]` | `Functorium.Applications.Usecases` | Class, Property, Parameter | Excluded from CtxEnricher generation |
| `[CtxRoot]` | `Functorium.Abstractions.Observabilities` | Interface, Property, Parameter | Promoted to ctx root level |

### Diagnostic Codes

| Code | Severity | Generator | Description |
|------|--------|--------|------|
| `FUNCTORIUM001` | Error | `ObservablePortGenerator` | Constructor parameter type duplication |
| `FUNCTORIUM002` | Warning | `CtxEnricherGenerator`, `DomainEventCtxEnricherGenerator` | ctx field type conflict (OpenSearch mapping) |
| `FUNCTORIUM003` | Warning | `CtxEnricherGenerator` | Request type inaccessible |
| `FUNCTORIUM004` | Warning | `DomainEventCtxEnricherGenerator` | Event type inaccessible |

---

## Common Infrastructure

### IncrementalGeneratorBase\<TValue\>

The abstract base class for all generators. Implements `IIncrementalGenerator` and standardizes pipeline registration and source output.

```csharp
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    bool AttachDebugger = false) : IIncrementalGenerator
```

| Parameter | Description |
|---------|------|
| `registerSourceProvider` | Registers the syntax/semantic analysis pipeline and returns `IncrementalValuesProvider<TValue>` |
| `generate` | Generates source files from the collected metadata array |
| `AttachDebugger` | Calls `Debugger.Launch()` in DEBUG builds when `true` |

**Execution flow**: `Initialize` -> Create `IncrementalValuesProvider<TValue>` via `registerSourceProvider` -> `null` filtering -> `Collect()` -> `generate` call

---

## EntityIdGenerator

Applying `[GenerateEntityId]` to an Entity class auto-generates a Ulid-based ID type, EF Core Comparer, and EF Core Converter.

### Trigger

```csharp
// Functorium.Domains.Entities
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateEntityIdAttribute : Attribute;
```

**Target**: `class` declarations with `[GenerateEntityId]` applied

### Generation Targets

Generates the following three types for the `{EntityName}` class in a single `.g.cs` file.

| Generated Type | Description |
|----------|------|
| `{EntityName}Id` | `readonly partial record struct` -- Ulid-based Entity ID |
| `{EntityName}IdComparer` | EF Core `ValueComparer<{EntityName}Id>` |
| `{EntityName}IdConverter` | EF Core `ValueConverter<{EntityName}Id, string>` |

### Generated Code Structure

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

    // Internal nested classes
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

### Diagnostic Codes

EntityIdGenerator does not currently emit dedicated diagnostic codes.

---

## ObservablePortGenerator

Applying `[GenerateObservablePort]` to an Adapter class auto-generates an Observable wrapper class that provides OpenTelemetry-based Observability (Tracing, Logging, Metrics).

### Trigger

```csharp
// Functorium.Adapters.SourceGenerators
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateObservablePortAttribute : Attribute;
```

**Target**: `class` with `[GenerateObservablePort]` applied, targeting methods with `FinT<IO, T>` return type among methods of interfaces inheriting `IObservablePort`.

**Exclusion condition**: Methods with `[ObservablePortIgnore]` applied are excluded from generation.

```csharp
// Functorium.Adapters.SourceGenerators
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ObservablePortIgnoreAttribute : Attribute;
```

### Generation Targets

Generates the following for `{ClassName}`.

| Generated Type | Description |
|----------|------|
| `{ClassName}Observable` | Observable wrapper class that inherits the original class |
| `{ClassName}ObservableLoggers` | `LoggerMessage.Define`-based high-performance logging extension method static class |

### Generated Code Structure

#### {ClassName}Observable

```csharp
public class {ClassName}Observable : {ClassName}
{
    // Infrastructure fields
    private readonly ActivitySource _activitySource;
    private readonly ILogger<{ClassName}Observable> _logger;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    // Constructor (DI parameters + parent constructor parameters)
    public {ClassName}Observable(
        ActivitySource activitySource,
        ILogger<{ClassName}Observable> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        ... /* parent constructor parameters */);

    // IObservablePort interface method override
    public override FinT<IO, TResult> {MethodName}(...);
}
```

**Observability provided by each override method:**

| Item | Content |
|------|------|
| **Tracing** | Creates span via `ActivitySource.StartActivity`, records success/failure status |
| **Logging** | 4 levels: Request (Debug/Info), Response success (Debug/Info), Response failure (Warning/Error) |
| **Metrics** | `adapter.{category}.requests` Counter, `adapter.{category}.responses` Counter, `adapter.{category}.duration` Histogram |

**Constructor parameter name conflict resolution**: If the parent class constructor parameter name conflicts with reserved names(`activitySource`, `logger`, `meterFactory`, `openTelemetryOptions`), a `base` prefix is added. Example: `logger` -> `baseLogger`

#### {ClassName}ObservableLoggers

Generates high-performance static logging methods using `LoggerMessage.Define`.

```csharp
internal static class {ClassName}ObservableLoggers
{
    // LoggerMessage.Define-based delegate fields (6 parameters or fewer)
    private static readonly Action<ILogger, ...> _logAdapterRequest_{ClassName}_{MethodName};
    private static readonly Action<ILogger, ...> _logAdapterRequestDebug_{ClassName}_{MethodName};
    private static readonly Action<ILogger, ...> _logAdapterResponseSuccess_{ClassName}_{MethodName};

    // Extension methods
    public static void LogAdapterRequest_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterRequestDebug_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseSuccessDebug_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseSuccess_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseWarning_{ClassName}_{MethodName}(this ILogger logger, ...);
    public static void LogAdapterResponseError_{ClassName}_{MethodName}(this ILogger logger, ...);
}
```

### Diagnostic Codes

| Code | Severity | Message |
|------|--------|--------|
| **FUNCTORIUM001** | Error | `Observable constructor for '{ClassName}' contains multiple parameters of the same type '{TypeName}'.` -- Code generation is aborted because having the same type in constructor parameters (parent + Observable unique) causes DI resolution conflicts. |

---

## CtxEnricherGenerator

Auto-detects `record` types implementing `ICommandRequest<TSuccess>` or `IQueryRequest<TSuccess>` and generates `IUsecaseCtxEnricher` implementations. Works through interface implementation alone without a separate trigger attribute.

### Trigger

**Auto-detection conditions:**

1. Must be a `record` declaration.
2. Must implement the `ICommandRequest<TSuccess>` or `IQueryRequest<TSuccess>` interface.
3. `[CtxIgnore]` must not be applied at the class level.
4. The type must have `public` or `internal` accessibility (`private`/`protected` triggers FUNCTORIUM003 warning).

**Exclusion attribute:**

```csharp
// Functorium.Applications.Usecases
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false, Inherited = false)]
public sealed class CtxIgnoreAttribute : Attribute;
```

**Promotion attribute:**

```csharp
// Functorium.Abstractions.Observabilities
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false, Inherited = false)]
public sealed class CtxRootAttribute : Attribute;
```

### Generation Targets

| Generated Type | Description |
|----------|------|
| `{ContainingTypes}{RequestTypeName}CtxEnricher` | `partial class`, `IUsecaseCtxEnricher<TRequest, FinResponse<TSuccess>>` implementation |

### Generated Code Structure

```csharp
public partial class {ContainingTypes}{RequestTypeName}CtxEnricher
    : IUsecaseCtxEnricher<{RequestFullType}, FinResponse<{ResponseFullType}>>
{
    // Push Request properties to LogContext
    public IDisposable? EnrichRequestLog({RequestFullType} request);

    // Push Response properties to LogContext (Succ Pattern matching)
    public IDisposable? EnrichResponseLog(
        {RequestFullType} request,
        FinResponse<{ResponseFullType}> response);

    // Extension points (user can implement as partial)
    partial void OnEnrichRequestLog(
        {RequestFullType} request,
        List<IDisposable> disposables);

    partial void OnEnrichResponseLog(
        {RequestFullType} request,
        FinResponse<{ResponseFullType}> response,
        List<IDisposable> disposables);

    // Helper methods
    private static void PushRequestCtx(List<IDisposable> disposables, string fieldName, object? value);
    private static void PushResponseCtx(List<IDisposable> disposables, string fieldName, object? value);
    private static void PushRootCtx(...);  // Generated only when [CtxRoot] attribute exists
}
```

**ctx field naming rules:**

| Condition | ctx Field Pattern | Example |
|------|-------------|------|
| Default | `ctx.{containing_types}.request.{snake_case_name}` | `ctx.place_order_command.request.customer_id` |
| `[CtxRoot]` applied | `ctx.{snake_case_name}` | `ctx.customer_id` |
| Inherited from interface | `ctx.{interface_name}.{snake_case_name}` | `ctx.operator_context.operator_id` |
| Collection type | `...{snake_case_name}_count` | `ctx.place_order_command.request.items_count` |

**Property filtering rules:**

- Scalar types (primitive, string, DateTime, Guid, enum, Option\<T\>, etc.): output value as-is
- Collection type (List, Array, Seq, etc.): output count only with `_count` suffix
- Complex types (class, record, struct): excluded
- `[CtxIgnore]` applied properties: excluded

### Diagnostic Codes

| Code | Severity | Message |
|------|--------|--------|
| **FUNCTORIUM002** | Warning | `ctx field '{FieldName}' has conflicting types: '{Type1}' ({Group1}) in '{Enricher1}' vs '{Type2}' ({Group2}) in '{Enricher2}'.` -- Dynamic mapping conflicts occur when different Enrichers assign different OpenSearch type groups to the same ctx field name. |
| **FUNCTORIUM003** | Warning | `'{RequestType}' implements ICommandRequest/IQueryRequest but CtxEnricher cannot be generated because '{TypeName}' is {accessibility}.` -- Enrichers cannot be generated for `private` or `protected` types. Apply `[CtxIgnore]` to suppress the warning. |

---

## DomainEventCtxEnricherGenerator

Auto-detects classes implementing `IDomainEventHandler<TEvent>` and generates `IDomainEventCtxEnricher` implementations for `TEvent`. Even if multiple Handlers exist for the same event type, the Enricher is generated only once.

### Trigger

**Auto-detection conditions:**

1. Must be a `class` declaration.
2. Must implement the `IDomainEventHandler<TEvent>` interface.
3. `TEvent` must not be `abstract`.
4. `[CtxIgnore]` must not be applied at the class level on `TEvent`.
5. `TEvent` must have `public` or `internal` accessibility (`private`/`protected` triggers FUNCTORIUM004 warning).

### Generation Targets

| Generated Type | Description |
|----------|------|
| `{ContainingTypes}{EventTypeName}CtxEnricher` | `partial class`, `IDomainEventCtxEnricher<TEvent>` implementation |

### Generated Code Structure

```csharp
public partial class {ContainingTypes}{EventTypeName}CtxEnricher
    : IDomainEventCtxEnricher<{EventFullType}>
{
    // Push event properties to LogContext
    public IDisposable? EnrichLog({EventFullType} domainEvent);

    // Extension points (user can implement as partial)
    partial void OnEnrichLog(
        {EventFullType} domainEvent,
        List<IDisposable> disposables);

    // Helper methods
    private static void PushEventCtx(List<IDisposable> disposables, string fieldName, object? value);
    private static void PushRootCtx(...);  // Generated only when [CtxRoot] attribute exists
}
```

**Property filtering rules:**

Follows the same rules as `CtxEnricherGenerator`, with the addition that `IDomainEvent` default properties (`OccurredAt`, `EventId`, `CorrelationId`, `CausationId`) are automatically excluded. Properties implementing `IValueObject` or `IEntityId<T>` are converted to keyword via `.ToString()` call.

**ctx field naming rules:**

| Condition | ctx Field Pattern | Example |
|------|-------------|------|
| Top-level event | `ctx.{snake_case_event}.{snake_case_name}` | `ctx.order_placed_event.order_id` |
| Nested event | `ctx.{containing}.{snake_case_event}.{snake_case_name}` | `ctx.order.created_event.amount` |
| `[CtxRoot]` applied | `ctx.{snake_case_name}` | `ctx.order_id` |

### Diagnostic Codes

| Code | Severity | Message |
|------|--------|--------|
| **FUNCTORIUM002** | Warning | ctx field type conflict (shares same ID with CtxEnricherGenerator) |
| **FUNCTORIUM004** | Warning | `'{EventType}' implements IDomainEvent but DomainEventCtxEnricher cannot be generated because '{TypeName}' is {accessibility}.` -- Enrichers cannot be generated for `private` or `protected` event types. Apply `[CtxIgnore]` to suppress the warning. |

---

## UnionTypeGenerator

Applying `[UnionType]` to an `abstract partial record` analyzes the internal `sealed record` cases and auto-generates `Match`/`Switch` pattern matching methods.

### Trigger

```csharp
// Functorium.Domains.ValueObjects.Unions
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UnionTypeAttribute : Attribute;
```

**Target**: `abstract partial record` declarations with `[UnionType]` applied, requiring at least one directly inheriting `sealed record` case.

### Generation Targets

Generates the following members as `partial` extensions for the `{TypeName}` record.

| Generated Member | Signature |
|----------|---------|
| `Match<TResult>` | Accepts Func parameters for all cases and returns `TResult` |
| `Switch` | Accepts Action parameters for all cases and executes them |
| `Is{CaseName}` | `bool` property -- `this is {CaseName}` |
| `As{CaseName}()` | `{CaseName}?` return -- `this as {CaseName}` |

### Generated Code Structure

```csharp
public abstract partial record {TypeName}
{
    // Pattern matching (with return value)
    public TResult Match<TResult>(
        Func<Case1, TResult> case1,
        Func<Case2, TResult> case2,
        ...);

    // Pattern matching (no return value)
    public void Switch(
        Action<Case1> case1,
        Action<Case2> case2,
        ...);

    // Type check properties
    public bool IsCase1 => this is Case1;
    public bool IsCase2 => this is Case2;

    // Safe type conversion
    public Case1? AsCase1() => this as Case1;
    public Case2? AsCase2() => this as Case2;
}
```

**Unreachable case handling**: The `default` branch of `Match`/`Switch` throws `UnreachableCaseException`.

```csharp
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
```

### Diagnostic Codes

UnionTypeGenerator does not currently emit dedicated diagnostic codes. If there are no internal `sealed record` cases, code generation is skipped.

---

## Related Documents

- [Source Generator Observability Tutorial](../tutorials/sourcegen-observability/) -- From Roslyn API basics to practical generator implementation
- [Entity and Aggregate Specification](./01-entity-aggregate) -- `IEntityId<T>`, `GenerateEntityIdAttribute` definition
- [Observability Specification](./08-observability) -- `IUsecaseCtxEnricher`, `IDomainEventCtxEnricher` interface definitions
- [Adapter Pipeline and DI Guide](../guides/adapter/14a-adapter-pipeline-di) -- Observable wrapper DI registration pattern
- [Testing Library Guide](../guides/testing/16-testing-library) -- Writing unit tests for source generators
