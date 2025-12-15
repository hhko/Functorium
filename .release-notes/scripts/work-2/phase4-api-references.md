# Phase 4: 사용된 API 참조

Generated: 2025-12-16

## Functorium 어셈블리

### ErrorCodeFactory
- Location: Functorium.Abstractions.Errors.ErrorCodeFactory
- Methods:
  - Create(string, string, string)
  - Create<T>(string, T, string)
  - Create<T1, T2>(string, T1, T2, string)
  - Create<T1, T2, T3>(string, T1, T2, T3, string)
  - CreateFromException(string, Exception)
  - Format(params string[])
- Uber File: Line 75-89
- Status: ✓ 검증됨

### ErrorsDestructuringPolicy
- Location: Functorium.Abstractions.Errors.DestructuringPolicies.ErrorsDestructuringPolicy
- Methods:
  - TryDestructure(object, ILogEventPropertyValueFactory, out LogEventPropertyValue?)
  - DestructureError(Error, ILogEventPropertyValueFactory)
- Uber File: Line 61-66
- Status: ✓ 검증됨

### IErrorDestructurer
- Location: Functorium.Abstractions.Errors.DestructuringPolicies.IErrorDestructurer
- Methods:
  - CanHandle(Error)
  - Destructure(Error, ILogEventPropertyValueFactory)
- Uber File: Line 67-71
- Status: ✓ 검증됨

### Error Destructurers
- ErrorCodeExceptionalDestructurer: Line 22-27
- ErrorCodeExpectedDestructurer: Line 28-33
- ErrorCodeExpectedTDestructurer: Line 34-39
- ExceptionalDestructurer: Line 40-45
- ExpectedDestructurer: Line 46-51
- ManyErrorsDestructurer: Line 52-57
- Status: ✓ 모두 검증됨

### OpenTelemetryRegistration
- Location: Functorium.Abstractions.Registrations.OpenTelemetryRegistration
- Methods:
  - RegisterObservability(IServiceCollection, IConfiguration)
- Uber File: Line 93-96
- Status: ✓ 검증됨

### OpenTelemetryBuilder
- Location: Functorium.Adapters.Observabilities.Builders.OpenTelemetryBuilder
- Properties:
  - Options
- Methods:
  - Build()
  - ConfigureMetrics(Action<MetricsConfigurator>)
  - ConfigureSerilog(Action<LoggingConfigurator>)
  - ConfigureStartupLogger(Action<ILogger>)
  - ConfigureTraces(Action<TracingConfigurator>)
  - CreateResourceAttributes(OpenTelemetryOptions)
  - ToOtlpProtocolForExporter(OtlpCollectorProtocol)
  - ToOtlpProtocolForSerilog(OtlpCollectorProtocol)
- Uber File: Line 153-164
- Status: ✓ 검증됨

### OpenTelemetryOptions
- Location: Functorium.Adapters.Observabilities.OpenTelemetryOptions
- Properties:
  - SectionName
  - ServiceName
  - ServiceVersion
  - CollectorEndpoint
  - CollectorProtocol
  - SamplingRate
  - EnablePrometheusExporter
  - LoggingCollectorEndpoint
  - MetricsCollectorEndpoint
  - TracingCollectorEndpoint
  - LoggingCollectorProtocol
  - MetricsCollectorProtocol
  - TracingCollectorProtocol
- Uber File: Line 172-204
- Status: ✓ 검증됨

### LoggingConfigurator
- Location: Functorium.Adapters.Observabilities.Builders.Configurators.LoggingConfigurator
- Methods:
  - AddDestructuringPolicy<TPolicy>()
  - AddEnricher(ILogEventEnricher)
  - AddEnricher<TEnricher>()
  - Configure(Action<LoggerConfiguration>)
- Uber File: Line 126-135
- Status: ✓ 검증됨

### MetricsConfigurator
- Location: Functorium.Adapters.Observabilities.Builders.Configurators.MetricsConfigurator
- Methods:
  - AddInstrumentation(Action<MeterProviderBuilder>)
  - AddMeter(string)
  - Configure(Action<MeterProviderBuilder>)
- Uber File: Line 136-142
- Status: ✓ 검증됨

### TracingConfigurator
- Location: Functorium.Adapters.Observabilities.Builders.Configurators.TracingConfigurator
- Methods:
  - AddProcessor(BaseProcessor<Activity>)
  - AddSource(string)
  - Configure(Action<TracerProviderBuilder>)
- Uber File: Line 143-149
- Status: ✓ 검증됨

### OptionsConfigurator
- Location: Functorium.Adapters.Options.OptionsConfigurator
- Methods:
  - GetOptions<TOptions>(IServiceCollection)
  - RegisterConfigureOptions<TOptions, TValidator>(IServiceCollection, string)
- Uber File: Line 221-228
- Status: ✓ 검증됨

### FinTUtilites
- Location: Functorium.Applications.Linq.FinTUtilites
- Methods:
  - Filter<A>(Fin<A>, Func<A, bool>)
  - Filter<M, A>(FinT<M, A>, Func<A, bool>)
  - SelectMany<A, B>(IO<A>, Func<A, B>)
  - SelectMany<A, B, C>(IO<A>, Func<A, FinT<IO, B>>, Func<A, B, C>)
  - SelectMany<M, A, B, C>(Fin<A>, Func<A, FinT<M, B>>, Func<A, B, C>)
- Uber File: Line 232-241
- Status: ✓ 검증됨

### Utility Classes
- DictionaryUtilities.AddOrUpdate: Line 100-104
- IEnumerableUtilities (Any, IsEmpty, Join): Line 105-111
- StringUtilities (ConvertToDouble, ConvertToInt, Empty, NotContains, NotEmpty, NotEquals, Replace, TryConvertToDouble): Line 112-122
- Status: ✓ 모두 검증됨

---

## Functorium.Testing 어셈블리

### ArchitectureValidationEntryPoint
- Location: Functorium.Testing.ArchitectureRules.ArchitectureValidationEntryPoint
- Methods:
  - ValidateAllClasses(IObjectProvider<Class>, Architecture, Action<ClassValidator>)
  - ValidateAllClasses(IObjectProvider<Class>, Architecture, Action<ClassValidator>, bool)
- Uber File: Line 261-265
- Status: ✓ 검증됨

### ClassValidator
- Location: Functorium.Testing.ArchitectureRules.ClassValidator
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
- Location: Functorium.Testing.ArchitectureRules.MethodValidator
- Methods:
  - RequireReturnType(Type)
  - RequireReturnTypeOfDeclaringClass()
  - RequireStatic()
  - RequireVisibility(Visibility)
- Uber File: Line 284-291
- Status: ✓ 검증됨

### ValidationResultSummary
- Location: Functorium.Testing.ArchitectureRules.ValidationResultSummary
- Methods:
  - ThrowIfAnyFailures(string)
- Uber File: Line 292-296
- Status: ✓ 검증됨

### HostTestFixture<TProgram>
- Location: Functorium.Testing.Arrangements.Hosting.HostTestFixture
- Properties:
  - Client
  - Services
  - EnvironmentName
- Methods:
  - ConfigureHost(IWebHostBuilder)
  - DisposeAsync()
  - GetTestProjectPath()
  - InitializeAsync()
- Uber File: Line 300-311
- Status: ✓ 검증됨

### QuartzTestFixture<TProgram>
- Location: Functorium.Testing.Arrangements.ScheduledJobs.QuartzTestFixture
- Properties:
  - Scheduler
  - Services
  - JobListener
  - EnvironmentName
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
- Location: Functorium.Testing.Arrangements.ScheduledJobs.JobCompletionListener
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
- Location: Functorium.Testing.Arrangements.ScheduledJobs.JobExecutionResult
- Properties:
  - JobName
  - Success
  - Result
  - Exception
  - ExecutionTime
- Uber File: Line 344-352
- Status: ✓ 검증됨

### StructuredTestLogger<T>
- Location: Functorium.Testing.Arrangements.Logging.StructuredTestLogger
- Methods:
  - BeginScope<TState>(TState)
  - IsEnabled(LogLevel)
  - Log<TState>(LogLevel, EventId, TState, Exception?, Func<TState, Exception?, string>)
- Uber File: Line 323-330
- Status: ✓ 검증됨

### TestSink
- Location: Functorium.Testing.Arrangements.Loggers.TestSink
- Methods:
  - Emit(LogEvent)
- Uber File: Line 315-319
- Status: ✓ 검증됨

### LogEventPropertyExtractor
- Location: Functorium.Testing.Assertions.Logging.LogEventPropertyExtractor
- Methods:
  - ExtractLogData(LogEvent)
  - ExtractLogData(IEnumerable<LogEvent>)
  - ExtractValue(LogEventPropertyValue)
- Uber File: Line 380-385
- Status: ✓ 검증됨

### LogEventPropertyValueConverter
- Location: Functorium.Testing.Assertions.Logging.LogEventPropertyValueConverter
- Methods:
  - ToAnonymousObject(LogEventPropertyValue)
- Uber File: Line 386-389
- Status: ✓ 검증됨

### SerilogTestPropertyValueFactory
- Location: Functorium.Testing.Assertions.Logging.SerilogTestPropertyValueFactory
- Methods:
  - CreatePropertyValue(object?, bool)
- Uber File: Line 390-394
- Status: ✓ 검증됨

---

## 검증 요약

- 총 API 타입: 30개
- 검증 완료: 30개
- 검증 실패: 0개
- 상태: ✓ 모두 검증됨
