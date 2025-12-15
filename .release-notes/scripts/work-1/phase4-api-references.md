# Phase 4: 사용된 API 참조

## Functorium 어셈블리

### ErrorCodeFactory
- Location: Functorium.Abstractions.Errors.ErrorCodeFactory
- Methods:
  - Create(string, string, string)
  - Create<T>(string, T, string) where T : notnull
  - Create<T1, T2>(string, T1, T2, string) where T1 : notnull, T2 : notnull
  - Create<T1, T2, T3>(string, T1, T2, T3, string) where T1-T3 : notnull
  - CreateFromException(string, Exception)
  - Format(params string[])
- Uber File: Line 75-89
- Status: ✓ 검증됨

### ErrorsDestructuringPolicy
- Location: Functorium.Abstractions.Errors.DestructuringPolicies
- Methods:
  - TryDestructure(object, ILogEventPropertyValueFactory, out LogEventPropertyValue?)
  - DestructureError(Error, ILogEventPropertyValueFactory) [static]
- Uber File: Line 61-66
- Status: ✓ 검증됨

### IErrorDestructurer
- Location: Functorium.Abstractions.Errors.DestructuringPolicies
- Methods:
  - CanHandle(Error)
  - Destructure(Error, ILogEventPropertyValueFactory)
- Uber File: Line 67-71
- Status: ✓ 검증됨

### OpenTelemetryRegistration
- Location: Functorium.Abstractions.Registrations
- Methods:
  - RegisterObservability(IServiceCollection, IConfiguration)
- Uber File: Line 93-96
- Status: ✓ 검증됨

### OpenTelemetryBuilder
- Location: Functorium.Adapters.Observabilities.Builders
- Properties:
  - Options
- Methods:
  - Build()
  - ConfigureMetrics(Action<MetricsConfigurator>)
  - ConfigureSerilog(Action<LoggingConfigurator>)
  - ConfigureStartupLogger(Action<ILogger>)
  - ConfigureTraces(Action<TracingConfigurator>)
  - CreateResourceAttributes(OpenTelemetryOptions) [static]
  - ToOtlpProtocolForExporter(OtlpCollectorProtocol) [static]
  - ToOtlpProtocolForSerilog(OtlpCollectorProtocol) [static]
- Uber File: Line 152-164
- Status: ✓ 검증됨

### TracingConfigurator
- Location: Functorium.Adapters.Observabilities.Builders.Configurators
- Properties:
  - Options
- Methods:
  - AddProcessor(BaseProcessor<Activity>)
  - AddSource(string)
  - Configure(Action<TracerProviderBuilder>)
- Uber File: Line 143-149
- Status: ✓ 검증됨

### MetricsConfigurator
- Location: Functorium.Adapters.Observabilities.Builders.Configurators
- Properties:
  - Options
- Methods:
  - AddInstrumentation(Action<MeterProviderBuilder>)
  - AddMeter(string)
  - Configure(Action<MeterProviderBuilder>)
- Uber File: Line 136-142
- Status: ✓ 검증됨

### LoggingConfigurator
- Location: Functorium.Adapters.Observabilities.Builders.Configurators
- Properties:
  - Options
- Methods:
  - AddDestructuringPolicy<TPolicy>() where TPolicy : IDestructuringPolicy, new()
  - AddEnricher(ILogEventEnricher)
  - AddEnricher<TEnricher>() where TEnricher : ILogEventEnricher, new()
  - Configure(Action<LoggerConfiguration>)
- Uber File: Line 126-135
- Status: ✓ 검증됨

### OpenTelemetryOptions
- Location: Functorium.Adapters.Observabilities
- Constants:
  - SectionName = "OpenTelemetry"
- Properties:
  - ServiceName, ServiceVersion, CollectorEndpoint, CollectorProtocol
  - SamplingRate, EnablePrometheusExporter
  - TracingCollectorEndpoint, TracingCollectorProtocol
  - MetricsCollectorEndpoint, MetricsCollectorProtocol
  - LoggingCollectorEndpoint, LoggingCollectorProtocol
- Methods:
  - GetLoggingEndpoint(), GetLogsProtocol()
  - GetMetricsEndpoint(), GetMetricsProtocol()
  - GetTracingEndpoint(), GetTracingProtocol()
  - LogConfiguration(ILogger)
- Uber File: Line 172-204
- Status: ✓ 검증됨

### OptionsConfigurator
- Location: Functorium.Adapters.Options
- Methods:
  - RegisterConfigureOptions<TOptions, TValidator>(IServiceCollection, string)
  - GetOptions<TOptions>(IServiceCollection)
- Uber File: Line 221-228
- Status: ✓ 검증됨

### FinTUtilites
- Location: Functorium.Applications.Linq
- Methods:
  - Filter<A>(Fin<A>, Func<A, bool>)
  - Filter<M, A>(FinT<M, A>, Func<A, bool>) where M : Monad<M>
  - SelectMany<A, B>(IO<A>, Func<A, B>)
  - SelectMany<A, B, C>(IO<A>, Func<A, FinT<IO, B>>, Func<A, B, C>)
  - SelectMany<M, A, B, C>(Fin<A>, Func<A, FinT<M, B>>, Func<A, B, C>) where M : Monad<M>
- Uber File: Line 231-241
- Status: ✓ 검증됨

### DictionaryUtilities
- Location: Functorium.Abstractions.Utilities
- Methods:
  - AddOrUpdate<TKey, TValue>(Dictionary<TKey, TValue>, TKey, TValue) where TKey : notnull
- Uber File: Line 100-104
- Status: ✓ 검증됨

### IEnumerableUtilities
- Location: Functorium.Abstractions.Utilities
- Methods:
  - Any(IEnumerable)
  - IsEmpty(IEnumerable)
  - Join<TValue>(IEnumerable<TValue>, char)
  - Join<TValue>(IEnumerable<TValue>, string)
- Uber File: Line 105-111
- Status: ✓ 검증됨

### StringUtilities
- Location: Functorium.Abstractions.Utilities
- Methods:
  - ConvertToDouble(string)
  - ConvertToInt(string)
  - Empty(string?)
  - NotContains(string, string, StringComparison)
  - NotEmpty(string?)
  - NotEquals(string, string, StringComparison)
  - Replace(string, string[], string)
  - TryConvertToDouble(string)
- Uber File: Line 112-122
- Status: ✓ 검증됨

## Functorium.Testing 어셈블리

### ArchitectureValidationEntryPoint
- Location: Functorium.Testing.ArchitectureRules
- Methods:
  - ValidateAllClasses(IObjectProvider<Class>, Architecture, Action<ClassValidator>)
  - ValidateAllClasses(IObjectProvider<Class>, Architecture, Action<ClassValidator>, bool)
- Uber File: Line 261-265
- Status: ✓ 검증됨

### ClassValidator
- Location: Functorium.Testing.ArchitectureRules
- Methods:
  - RequireAllMethods(Action<MethodValidator>)
  - RequireAllPrivateConstructors()
  - RequireImmutable()
  - RequireImplements(Type)
  - RequireImplementsGenericInterface(string)
  - RequireInherits(Type)
  - RequireInternal()
  - RequireMethod(string, Action<MethodValidator>)
  - RequireNestedClass(string, Action<ClassValidator>?)
  - RequireNestedClassIfExists(string, Action<ClassValidator>?)
  - RequirePrivateAnyParameterlessConstructor()
  - RequirePublic()
  - RequireSealed()
  - ValidateAndThrow()
- Uber File: Line 266-283
- Status: ✓ 검증됨

### MethodValidator
- Location: Functorium.Testing.ArchitectureRules
- Methods:
  - RequireReturnType(Type)
  - RequireReturnTypeOfDeclaringClass()
  - RequireStatic()
  - RequireVisibility(Visibility)
- Uber File: Line 284-291
- Status: ✓ 검증됨

### ValidationResultSummary
- Location: Functorium.Testing.ArchitectureRules
- Methods:
  - ThrowIfAnyFailures(string)
- Uber File: Line 292-296
- Status: ✓ 검증됨

### HostTestFixture<TProgram>
- Location: Functorium.Testing.Arrangements.Hosting
- Properties:
  - Client, EnvironmentName, Services
- Methods:
  - ConfigureHost(IWebHostBuilder)
  - DisposeAsync()
  - GetTestProjectPath()
  - InitializeAsync()
- Uber File: Line 300-311
- Status: ✓ 검증됨

### QuartzTestFixture<TProgram>
- Location: Functorium.Testing.Arrangements.ScheduledJobs
- Properties:
  - EnvironmentName, JobListener, Scheduler, Services
- Methods:
  - ConfigureWebHost(IWebHostBuilder)
  - DisposeAsync()
  - ExecuteJobOnceAsync<TJob>(TimeSpan)
  - ExecuteJobOnceAsync<TJob>(string, string, TimeSpan)
  - GetTestProjectPath()
  - InitializeAsync()
- Uber File: Line 353-369
- Status: ✓ 검증됨

### JobCompletionListener
- Location: Functorium.Testing.Arrangements.ScheduledJobs
- Properties:
  - Name
- Methods:
  - JobExecutionVetoed(IJobExecutionContext, CancellationToken)
  - JobToBeExecuted(IJobExecutionContext, CancellationToken)
  - JobWasExecuted(IJobExecutionContext, JobExecutionException?, CancellationToken)
  - Reset()
  - WaitForJobCompletionAsync(string, TimeSpan, CancellationToken)
- Uber File: Line 334-343
- Status: ✓ 검증됨

### JobExecutionResult
- Location: Functorium.Testing.Arrangements.ScheduledJobs
- Properties:
  - Exception, ExecutionTime, JobName, Result, Success
- Uber File: Line 344-352
- Status: ✓ 검증됨

### StructuredTestLogger<T>
- Location: Functorium.Testing.Arrangements.Logging
- Methods:
  - BeginScope<TState>(TState) where TState : notnull
  - IsEnabled(LogLevel)
  - Log<TState>(LogLevel, EventId, TState, Exception?, Func<TState, Exception?, string>)
- Uber File: Line 323-330
- Status: ✓ 검증됨

### TestSink
- Location: Functorium.Testing.Arrangements.Loggers
- Methods:
  - Emit(LogEvent)
- Uber File: Line 315-319
- Status: ✓ 검증됨

### LogEventPropertyExtractor
- Location: Functorium.Testing.Assertions.Logging
- Methods:
  - ExtractLogData(LogEvent)
  - ExtractLogData(IEnumerable<LogEvent>)
  - ExtractValue(LogEventPropertyValue)
- Uber File: Line 380-385
- Status: ✓ 검증됨

### LogEventPropertyValueConverter
- Location: Functorium.Testing.Assertions.Logging
- Methods:
  - ToAnonymousObject(LogEventPropertyValue)
- Uber File: Line 386-389
- Status: ✓ 검증됨

### SerilogTestPropertyValueFactory
- Location: Functorium.Testing.Assertions.Logging
- Methods:
  - CreatePropertyValue(object?, bool)
- Uber File: Line 390-394
- Status: ✓ 검증됨
