# Phase 3: API와 커밋 매핑

Generated: 2025-12-16

## Functorium 어셈블리

### Functorium.Abstractions.Errors

| API | 타입 | 커밋 |
|-----|------|------|
| ErrorCodeFactory | static class | cda0a33 |
| ErrorCodeFactory.Create(string, string, string) | method | cda0a33 |
| ErrorCodeFactory.Create<T>(string, T, string) | method | cda0a33 |
| ErrorCodeFactory.Create<T1, T2>(string, T1, T2, string) | method | cda0a33 |
| ErrorCodeFactory.Create<T1, T2, T3>(string, T1, T2, T3, string) | method | cda0a33 |
| ErrorCodeFactory.CreateFromException(string, Exception) | method | cda0a33 |
| ErrorCodeFactory.Format(params string[]) | method | cda0a33 |

### Functorium.Abstractions.Errors.DestructuringPolicies

| API | 타입 | 커밋 |
|-----|------|------|
| ErrorsDestructuringPolicy | class | cda0a33 |
| IErrorDestructurer | interface | cda0a33 |

### Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes

| API | 타입 | 커밋 |
|-----|------|------|
| ErrorCodeExceptionalDestructurer | class | cda0a33 |
| ErrorCodeExpectedDestructurer | class | cda0a33 |
| ErrorCodeExpectedTDestructurer | class | cda0a33 |
| ExceptionalDestructurer | class | cda0a33 |
| ExpectedDestructurer | class | cda0a33 |
| ManyErrorsDestructurer | class | cda0a33 |

### Functorium.Abstractions.Registrations

| API | 타입 | 커밋 |
|-----|------|------|
| OpenTelemetryRegistration | static class | 7d9f182 |
| OpenTelemetryRegistration.RegisterObservability() | method | 7d9f182 |

### Functorium.Abstractions.Utilities

| API | 타입 | 커밋 |
|-----|------|------|
| DictionaryUtilities | static class | cda0a33 |
| IEnumerableUtilities | static class | cda0a33 |
| StringUtilities | static class | cda0a33 |

### Functorium.Adapters.Observabilities.Builders

| API | 타입 | 커밋 |
|-----|------|------|
| OpenTelemetryBuilder | class | 1790c73 |
| OpenTelemetryBuilder.Build() | method | 1790c73 |
| OpenTelemetryBuilder.ConfigureMetrics() | method | 1790c73 |
| OpenTelemetryBuilder.ConfigureSerilog() | method | 1790c73 |
| OpenTelemetryBuilder.ConfigureStartupLogger() | method | 1790c73 |
| OpenTelemetryBuilder.ConfigureTraces() | method | 1790c73 |

### Functorium.Adapters.Observabilities.Builders.Configurators

| API | 타입 | 커밋 |
|-----|------|------|
| LoggingConfigurator | class | 08a1af8 |
| MetricsConfigurator | class | 08a1af8 |
| TracingConfigurator | class | 08a1af8 |

### Functorium.Adapters.Observabilities

| API | 타입 | 커밋 |
|-----|------|------|
| OpenTelemetryOptions | class | 1790c73 |
| OpenTelemetryOptions.Validator | class | 1790c73 |
| IOpenTelemetryOptions | interface | 1790c73 |

### Functorium.Adapters.Observabilities.Logging

| API | 타입 | 커밋 |
|-----|------|------|
| StartupLogger | class | 1790c73 |
| IStartupOptionsLogger | interface | 1790c73 |

### Functorium.Adapters.Options

| API | 타입 | 커밋 |
|-----|------|------|
| OptionsConfigurator | static class | 4edcf7f |

### Functorium.Applications.Linq

| API | 타입 | 커밋 |
|-----|------|------|
| FinTUtilites | static class | cda0a33 |

---

## Functorium.Testing 어셈블리

### Functorium.Testing.ArchitectureRules

| API | 타입 | 커밋 |
|-----|------|------|
| ArchitectureValidationEntryPoint | static class | 0282d23 |
| ClassValidator | class | 0282d23 |
| MethodValidator | class | 0282d23 |
| ValidationResultSummary | class | 0282d23 |

### Functorium.Testing.Arrangements.Hosting

| API | 타입 | 커밋 |
|-----|------|------|
| HostTestFixture<TProgram> | class | 0282d23, 9094097 |

### Functorium.Testing.Arrangements.ScheduledJobs

| API | 타입 | 커밋 |
|-----|------|------|
| QuartzTestFixture<TProgram> | class | 0282d23 |
| JobCompletionListener | class | 0282d23 |
| JobExecutionResult | record | 0282d23 |

### Functorium.Testing.Arrangements.Logging

| API | 타입 | 커밋 |
|-----|------|------|
| StructuredTestLogger<T> | class | 0282d23 |

### Functorium.Testing.Arrangements.Loggers

| API | 타입 | 커밋 |
|-----|------|------|
| TestSink | class | 0282d23 |

### Functorium.Testing.Assertions.Logging

| API | 타입 | 커밋 |
|-----|------|------|
| LogEventPropertyExtractor | static class | 0282d23 |
| LogEventPropertyValueConverter | static class | 0282d23 |
| SerilogTestPropertyValueFactory | class | 0282d23 |
