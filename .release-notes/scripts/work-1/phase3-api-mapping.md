# Phase 3: API와 커밋 매핑

## Functorium 어셈블리

### 1. Functorium.Abstractions.Errors
| API | 커밋 | 상태 |
|-----|------|------|
| ErrorCodeFactory | cda0a33 | 신규 |
| ErrorCodeFactory.Create(string, string, string) | cda0a33 | 신규 |
| ErrorCodeFactory.Create<T>(string, T, string) | cda0a33 | 신규 |
| ErrorCodeFactory.Create<T1,T2>(string, T1, T2, string) | cda0a33 | 신규 |
| ErrorCodeFactory.Create<T1,T2,T3>(string, T1, T2, T3, string) | cda0a33 | 신규 |
| ErrorCodeFactory.CreateFromException(string, Exception) | cda0a33 | 신규 |
| ErrorCodeFactory.Format(params string[]) | cda0a33 | 신규 |

### 2. Functorium.Abstractions.Errors.DestructuringPolicies
| API | 커밋 | 상태 |
|-----|------|------|
| IErrorDestructurer | cda0a33 | 신규 |
| ErrorsDestructuringPolicy | cda0a33 | 신규 |
| ErrorCodeExceptionalDestructurer | cda0a33 | 신규 |
| ErrorCodeExpectedDestructurer | cda0a33 | 신규 |
| ErrorCodeExpectedTDestructurer | cda0a33 | 신규 |
| ExceptionalDestructurer | cda0a33 | 신규 |
| ExpectedDestructurer | cda0a33 | 신규 |
| ManyErrorsDestructurer | cda0a33 | 신규 |

### 3. Functorium.Abstractions.Registrations
| API | 커밋 | 상태 |
|-----|------|------|
| OpenTelemetryRegistration.RegisterObservability | 7d9f182 | 신규 |

### 4. Functorium.Abstractions.Utilities
| API | 커밋 | 상태 |
|-----|------|------|
| DictionaryUtilities.AddOrUpdate | cda0a33 | 신규 |
| IEnumerableUtilities.Any | cda0a33 | 신규 |
| IEnumerableUtilities.IsEmpty | cda0a33 | 신규 |
| IEnumerableUtilities.Join | cda0a33 | 신규 |
| StringUtilities.* | cda0a33 | 신규 |

### 5. Functorium.Adapters.Observabilities
| API | 커밋 | 상태 |
|-----|------|------|
| OpenTelemetryBuilder | 1790c73 | 신규 |
| OpenTelemetryOptions | 1790c73 | 신규 |
| IOpenTelemetryOptions | 1790c73 | 신규 |
| LoggingConfigurator | 08a1af8 | 신규 |
| MetricsConfigurator | 08a1af8 | 신규 |
| TracingConfigurator | 08a1af8 | 신규 |
| StartupLogger | 1790c73 | 신규 |
| IStartupOptionsLogger | 1790c73 | 신규 |

### 6. Functorium.Adapters.Options
| API | 커밋 | 상태 |
|-----|------|------|
| OptionsConfigurator.RegisterConfigureOptions | 4edcf7f | 신규 |
| OptionsConfigurator.GetOptions | 4edcf7f | 신규 |

### 7. Functorium.Applications.Linq
| API | 커밋 | 상태 |
|-----|------|------|
| FinTUtilites.Filter | cda0a33 | 신규 |
| FinTUtilites.SelectMany | cda0a33 | 신규 |

## Functorium.Testing 어셈블리

### 1. Functorium.Testing.ArchitectureRules
| API | 커밋 | 상태 |
|-----|------|------|
| ArchitectureValidationEntryPoint.ValidateAllClasses | 0282d23 | 신규 |
| ClassValidator | 0282d23 | 신규 |
| MethodValidator | 0282d23 | 신규 |
| ValidationResultSummary | 0282d23 | 신규 |

### 2. Functorium.Testing.Arrangements.Hosting
| API | 커밋 | 상태 |
|-----|------|------|
| HostTestFixture<TProgram> | 0282d23 | 신규 |

### 3. Functorium.Testing.Arrangements.ScheduledJobs
| API | 커밋 | 상태 |
|-----|------|------|
| QuartzTestFixture<TProgram> | 0282d23 | 신규 |
| JobCompletionListener | 0282d23 | 신규 |
| JobExecutionResult | 0282d23 | 신규 |

### 4. Functorium.Testing.Arrangements.Logging
| API | 커밋 | 상태 |
|-----|------|------|
| StructuredTestLogger<T> | 0282d23 | 신규 |
| TestSink | 0282d23 | 신규 |

### 5. Functorium.Testing.Assertions.Logging
| API | 커밋 | 상태 |
|-----|------|------|
| LogEventPropertyExtractor | 0282d23 | 신규 |
| LogEventPropertyValueConverter | 0282d23 | 신규 |
| SerilogTestPropertyValueFactory | 0282d23 | 신규 |
