# Phase 5: API 검증 상세

## 검증 방법
릴리스 노트에 사용된 모든 API를 Uber 파일(all-api-changes.txt)과 대조하여 검증

## Functorium 어셈블리 검증

### ErrorCodeFactory (검증됨)
```
Uber 파일 위치: Line 75-89
검증된 메서드:
  ✓ Create(string, string, string)
  ✓ Create<T>(string, T, string) where T : notnull
  ✓ Create<T1, T2>(string, T1, T2, string)
  ✓ Create<T1, T2, T3>(string, T1, T2, T3, string)
  ✓ CreateFromException(string, Exception)
  ✓ Format(params string[])
```

### ErrorsDestructuringPolicy (검증됨)
```
Uber 파일 위치: Line 61-66
검증된 메서드:
  ✓ TryDestructure(object, ILogEventPropertyValueFactory, out LogEventPropertyValue?)
  ✓ DestructureError(Error, ILogEventPropertyValueFactory) [static]
```

### IErrorDestructurer (검증됨)
```
Uber 파일 위치: Line 67-71
검증된 메서드:
  ✓ CanHandle(Error)
  ✓ Destructure(Error, ILogEventPropertyValueFactory)
```

### OpenTelemetryRegistration (검증됨)
```
Uber 파일 위치: Line 93-96
검증된 메서드:
  ✓ RegisterObservability(IServiceCollection, IConfiguration)
```

### OpenTelemetryBuilder (검증됨)
```
Uber 파일 위치: Line 152-164
검증된 메서드:
  ✓ Build()
  ✓ ConfigureMetrics(Action<MetricsConfigurator>)
  ✓ ConfigureSerilog(Action<LoggingConfigurator>)
  ✓ ConfigureStartupLogger(Action<ILogger>)
  ✓ ConfigureTraces(Action<TracingConfigurator>)
```

### Configurators (검증됨)
```
TracingConfigurator - Uber 파일 위치: Line 143-149
  ✓ AddSource(string)
  ✓ AddProcessor(BaseProcessor<Activity>)
  ✓ Configure(Action<TracerProviderBuilder>)

MetricsConfigurator - Uber 파일 위치: Line 136-142
  ✓ AddMeter(string)
  ✓ AddInstrumentation(Action<MeterProviderBuilder>)
  ✓ Configure(Action<MeterProviderBuilder>)

LoggingConfigurator - Uber 파일 위치: Line 126-135
  ✓ AddDestructuringPolicy<TPolicy>()
  ✓ AddEnricher(ILogEventEnricher)
  ✓ AddEnricher<TEnricher>()
  ✓ Configure(Action<LoggerConfiguration>)
```

### OpenTelemetryOptions (검증됨)
```
Uber 파일 위치: Line 172-204
검증된 속성:
  ✓ SectionName, ServiceName, ServiceVersion
  ✓ CollectorEndpoint, CollectorProtocol
  ✓ SamplingRate, EnablePrometheusExporter
  ✓ TracingCollectorEndpoint, TracingCollectorProtocol
  ✓ MetricsCollectorEndpoint, MetricsCollectorProtocol
  ✓ LoggingCollectorEndpoint, LoggingCollectorProtocol
```

### OptionsConfigurator (검증됨)
```
Uber 파일 위치: Line 221-228
검증된 메서드:
  ✓ RegisterConfigureOptions<TOptions, TValidator>(IServiceCollection, string)
  ✓ GetOptions<TOptions>(IServiceCollection)
```

### FinTUtilites (검증됨)
```
Uber 파일 위치: Line 232-241
검증된 메서드:
  ✓ Filter<A>(Fin<A>, Func<A, bool>)
  ✓ Filter<M, A>(FinT<M, A>, Func<A, bool>)
  ✓ SelectMany<A, B>(IO<A>, Func<A, B>)
  ✓ SelectMany<A, B, C>(IO<A>, Func<A, FinT<IO, B>>, Func<A, B, C>)
  ✓ SelectMany<M, A, B, C>(Fin<A>, Func<A, FinT<M, B>>, Func<A, B, C>)
```

### Utilities (검증됨)
```
DictionaryUtilities - Uber 파일 위치: Line 100-104
  ✓ AddOrUpdate<TKey, TValue>

IEnumerableUtilities - Uber 파일 위치: Line 105-111
  ✓ Any(IEnumerable)
  ✓ IsEmpty(IEnumerable)
  ✓ Join<TValue>(IEnumerable<TValue>, char)
  ✓ Join<TValue>(IEnumerable<TValue>, string)

StringUtilities - Uber 파일 위치: Line 112-122
  ✓ ConvertToInt, ConvertToDouble, TryConvertToDouble
  ✓ Empty, NotEmpty
  ✓ NotContains, NotEquals
  ✓ Replace
```

## Functorium.Testing 어셈블리 검증

### ArchitectureValidationEntryPoint (검증됨)
```
Uber 파일 위치: Line 261-265
검증된 메서드:
  ✓ ValidateAllClasses(IObjectProvider<Class>, Architecture, Action<ClassValidator>)
  ✓ ValidateAllClasses(IObjectProvider<Class>, Architecture, Action<ClassValidator>, bool)
```

### ClassValidator (검증됨)
```
Uber 파일 위치: Line 266-283
검증된 메서드:
  ✓ RequireSealed(), RequireImmutable(), RequirePublic(), RequireInternal()
  ✓ RequireAllPrivateConstructors(), RequirePrivateAnyParameterlessConstructor()
  ✓ RequireImplements(Type), RequireImplementsGenericInterface(string)
  ✓ RequireInherits(Type)
  ✓ RequireMethod(string, Action<MethodValidator>)
  ✓ RequireAllMethods(Action<MethodValidator>)
  ✓ RequireNestedClass, RequireNestedClassIfExists
  ✓ ValidateAndThrow()
```

### MethodValidator (검증됨)
```
Uber 파일 위치: Line 284-291
검증된 메서드:
  ✓ RequireStatic()
  ✓ RequireVisibility(Visibility)
  ✓ RequireReturnType(Type)
  ✓ RequireReturnTypeOfDeclaringClass()
```

### ValidationResultSummary (검증됨)
```
Uber 파일 위치: Line 292-296
검증된 메서드:
  ✓ ThrowIfAnyFailures(string)
```

### HostTestFixture (검증됨)
```
Uber 파일 위치: Line 300-311
검증된 속성/메서드:
  ✓ Client, Services, EnvironmentName
  ✓ ConfigureHost(IWebHostBuilder)
  ✓ GetTestProjectPath()
  ✓ InitializeAsync(), DisposeAsync()
```

### QuartzTestFixture (검증됨)
```
Uber 파일 위치: Line 353-369
검증된 속성/메서드:
  ✓ Scheduler, Services, JobListener, EnvironmentName
  ✓ ExecuteJobOnceAsync<TJob>(TimeSpan)
  ✓ ExecuteJobOnceAsync<TJob>(string, string, TimeSpan)
  ✓ ConfigureWebHost(IWebHostBuilder)
  ✓ GetTestProjectPath()
  ✓ InitializeAsync(), DisposeAsync()
```

### JobCompletionListener (검증됨)
```
Uber 파일 위치: Line 334-343
검증된 속성/메서드:
  ✓ Name
  ✓ JobExecutionVetoed, JobToBeExecuted, JobWasExecuted
  ✓ Reset()
  ✓ WaitForJobCompletionAsync(string, TimeSpan, CancellationToken)
```

### JobExecutionResult (검증됨)
```
Uber 파일 위치: Line 344-352
검증된 속성:
  ✓ JobName, Success, Result, Exception, ExecutionTime
```

### Logging 테스트 유틸리티 (검증됨)
```
StructuredTestLogger<T> - Uber 파일 위치: Line 323-330
  ✓ BeginScope<TState>, IsEnabled, Log<TState>

TestSink - Uber 파일 위치: Line 315-319
  ✓ Emit(LogEvent)

LogEventPropertyExtractor - Uber 파일 위치: Line 380-385
  ✓ ExtractLogData(LogEvent)
  ✓ ExtractLogData(IEnumerable<LogEvent>)
  ✓ ExtractValue(LogEventPropertyValue)

LogEventPropertyValueConverter - Uber 파일 위치: Line 386-389
  ✓ ToAnonymousObject(LogEventPropertyValue)

SerilogTestPropertyValueFactory - Uber 파일 위치: Line 390-394
  ✓ CreatePropertyValue(object?, bool)
```

## 검증 요약

- 총 검증된 타입: 30+
- 총 검증된 메서드: 100+
- 발견된 오류: 0
- 상태: ✓ 모든 API 검증 통과
