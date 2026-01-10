using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Functorium.Abstractions.Errors.DestructuringPolicies;
using Functorium.Adapters.Observabilities.Builders.Configurators;
using Functorium.Adapters.Observabilities.Configurations;
using Functorium.Adapters.Observabilities.Loggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Functorium.Adapters.Observabilities.Builders;

/// <summary>
/// OpenTelemetry 설정을 위한 메인 Builder 클래스
/// Serilog, Metrics, Traces 설정을 체이닝으로 구성
/// </summary>
public partial class OpenTelemetryBuilder
{
    // Aspire Dashboard OTLP 포트 (HTTP 트레이스 필터링용)
    private const int AspireDashboardOtlpPort = 18889;
    private const int AspireDashboardOtlpSecondaryPort = 18890;

    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;
    private readonly Assembly _projectAssembly;
    private readonly string _functoriumNamespaceRoot;
    private readonly string _projectNamespaceRoot;

    private Action<LoggingConfigurator>? _loggingConfigurator;
    private Action<MetricsConfigurator>? _metricsConfigurator;
    private Action<TracingConfigurator>? _tracingConfigurator;
    private Action<PipelineConfigurator>? _pipelineConfigurator;
    private Action<Microsoft.Extensions.Logging.ILogger>? _startupLoggerConfigurator;

    // AdapterObservability 설정
    private bool _enableAdapterObservability = true; // 기본값: 자동 활성화

    // Pipeline 설정
    private bool _usePipelinesWithDefaults = false;

    internal OpenTelemetryBuilder(
        IServiceCollection services,
        IConfiguration configuration,
        Assembly projectAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(projectAssembly);

        _services = services;
        _configuration = configuration;
        _projectAssembly = projectAssembly;

        // Functorium 네임스페이스 루트 이름 동적 추출
        // 예: "Functorium.Adapters.Observabilities" → "Functorium"
        _functoriumNamespaceRoot = ExtractNamespaceRoot(typeof(OpenTelemetryBuilder).Namespace);

        // 프로젝트 어셈블리의 네임스페이스 루트 추출
        // 예: "Observability.Adapters.Infrastructure" → "Observability"
        _projectNamespaceRoot = ExtractNamespaceRoot(projectAssembly.GetName().Name);
    }

    /// <summary>
    /// 네임스페이스에서 루트 이름을 추출합니다.
    /// </summary>
    /// <param name="fullNamespace">전체 네임스페이스 (예: "Functorium.Adapters.Observabilities")</param>
    /// <returns>루트 네임스페이스 (예: "Functorium")</returns>
    private static string ExtractNamespaceRoot(string? fullNamespace)
    {
        if (string.IsNullOrEmpty(fullNamespace))
            return "*"; // fallback

        int firstDotIndex = fullNamespace.IndexOf('.');
        return firstDotIndex > 0
            ? fullNamespace.Substring(0, firstDotIndex)
            : fullNamespace;
    }

    /// <summary>
    /// Logging 확장 설정 (Serilog 기반)
    /// </summary>
    /// <param name="configure">LoggingConfigurator를 사용한 설정 액션</param>
    public OpenTelemetryBuilder ConfigureLogging(Action<LoggingConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _loggingConfigurator = configure;
        return this;
    }

    /// <summary>
    /// OpenTelemetry Metrics 확장 설정
    /// </summary>
    /// <param name="configure">MetricsConfigurationBuilder를 사용한 설정 액션</param>
    public OpenTelemetryBuilder ConfigureMetrics(Action<MetricsConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _metricsConfigurator = configure;
        return this;
    }

    /// <summary>
    /// OpenTelemetry Tracing 확장 설정
    /// </summary>
    /// <param name="configure">TracingConfigurator를 사용한 설정 액션</param>
    public OpenTelemetryBuilder ConfigureTracing(Action<TracingConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _tracingConfigurator = configure;
        return this;
    }

    /// <summary>
    /// 애플리케이션 시작 시 추가 로깅 설정
    /// 프로젝트별로 커스텀 설정 정보를 OpenTelemetry Configuration Report 마지막에 출력할 수 있습니다.
    /// </summary>
    /// <param name="configure">ILogger를 사용한 추가 로깅 액션</param>
    /// <returns>OpenTelemetryBuilder 인스턴스 (체이닝 지원)</returns>
    /// <example>
    /// <code>
    /// services
    ///     .RegisterOpenTelemetry(configuration)
    ///     .ConfigureStartupLogger(logger =>
    ///     {
    ///         logger.LogInformation("┌─ Application Configuration");
    ///         logger.LogInformation("│  Database:  {ConnectionString}", "...");
    ///         logger.LogInformation("└─");
    ///         logger.LogInformation("");
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public OpenTelemetryBuilder ConfigureStartupLogger(Action<Microsoft.Extensions.Logging.ILogger> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _startupLoggerConfigurator = configure;
        return this;
    }

    /// <summary>
    /// OpenTelemetryOptions 접근 (IServiceProvider에서 가져오기)
    /// </summary>
    /// <param name="serviceProvider">IServiceProvider</param>
    /// <returns>OpenTelemetryOptions</returns>
    public OpenTelemetryOptions GetOptions(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
    }

    /// <summary>
    /// Adapter 관찰 가능성 기능 활성화/비활성화
    /// ISpanFactory, IMetricRecorder를 Singleton으로 등록합니다.
    /// 기본값: true (자동 활성화)
    /// </summary>
    public OpenTelemetryBuilder WithAdapterObservability(bool enable = true)
    {
        _enableAdapterObservability = enable;
        return this;
    }

    /// <summary>
    /// 모든 기본 Usecase 파이프라인을 활성화합니다.
    /// Metrics, Tracing, Logging, Validation, Exception 파이프라인이 Scoped로 등록됩니다.
    /// </summary>
    /// <remarks>
    /// 파이프라인 실행 순서:
    /// Request → Metrics → Tracing → Logging → Validation → Exception → Handler
    /// </remarks>
    /// <example>
    /// <code>
    /// services
    ///     .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    ///     .ConfigurePipelines()
    ///     .Build();
    /// </code>
    /// </example>
    public OpenTelemetryBuilder ConfigurePipelines()
    {
        _usePipelinesWithDefaults = true;
        return this;
    }

    /// <summary>
    /// Usecase 파이프라인을 커스텀 설정으로 구성합니다.
    /// 개별 파이프라인 활성화/비활성화, Lifetime 설정, 커스텀 파이프라인 추가가 가능합니다.
    /// </summary>
    /// <param name="configure">PipelineConfigurator를 사용한 설정 액션</param>
    /// <remarks>
    /// 파이프라인 실행 순서:
    /// Request → Metrics → Tracing → Logging → Validation → Exception → Custom → Handler
    /// </remarks>
    /// <example>
    /// <code>
    /// services
    ///     .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    ///     .ConfigurePipelines(pipelines => pipelines
    ///         .UseMetrics()
    ///         .UseTracing()
    ///         .UseLogging()
    ///         .UseValidation()
    ///         .UseException()
    ///         .WithLifetime(ServiceLifetime.Scoped))
    ///     .Build();
    /// </code>
    /// </example>
    public OpenTelemetryBuilder ConfigurePipelines(Action<PipelineConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _pipelineConfigurator = configure;
        return this;
    }

    /// <summary>
    /// 모든 설정을 적용하고 IServiceCollection 반환
    /// </summary>
    public IServiceCollection Build()
    {
        // ServiceProvider를 통해 옵션 가져오기 (Builder 패턴에서는 임시 ServiceProvider 사용)
        using var tempServiceProvider = _services.BuildServiceProvider();
        var options = tempServiceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
        var sloConfiguration = tempServiceProvider.GetRequiredService<IOptions<SloConfiguration>>().Value;

        // Resource Attributes 공통 정의
        Dictionary<string, object> resourceAttributes = CreateResourceAttributes(options);

        // Serilog 설정 적용
        ConfigureSerilogInternal(resourceAttributes, options);

        // OpenTelemetry 설정 적용
        ConfigureOpenTelemetryInternal(resourceAttributes, options, sloConfiguration);

        // AdapterObservability 등록 (OpenTelemetry 설정 후)
        RegisterAdapterObservabilityInternal(options);

        // Usecase 파이프라인 등록
        RegisterPipelinesInternal();

        // OpenTelemetry 설정 정보 로거 등록 (IHostedService)
        // 애플리케이션 시작 시 자동으로 설정 정보를 로그로 출력
        // 추가 로거가 설정된 경우 함께 전달
        if (_startupLoggerConfigurator != null)
        {
            _services.AddHostedService(sp =>
                new StartupLogger(
                    sp.GetRequiredService<ILogger<StartupLogger>>(),
                    sp.GetRequiredService<IHostEnvironment>(),
                    sp.GetServices<IStartupOptionsLogger>(),
                    _startupLoggerConfigurator));
        }
        else
        {
            _services.AddHostedService(sp =>
                new StartupLogger(
                    sp.GetRequiredService<ILogger<StartupLogger>>(),
                    sp.GetRequiredService<IHostEnvironment>(),
                    sp.GetServices<IStartupOptionsLogger>()));
        }

        return _services;
    }

    private void ConfigureSerilogInternal(Dictionary<string, object> resourceAttributes, OpenTelemetryOptions options)
    {
        _services
            .AddSerilog(logging =>
            {
                // 기본 설정: ReadFrom.Configuration으로 appsettings.json 읽기
                logging.ReadFrom.Configuration(_configuration);

                // WriteTo.OpenTelemetry 설정 (LoggingCollectorEndpoint가 설정된 경우에만)
                string loggingEndpoint = options.GetLoggingEndpoint();
                if (!string.IsNullOrWhiteSpace(loggingEndpoint))
                {
                    logging.WriteTo.OpenTelemetry(otlpOptions =>
                    {
                        otlpOptions.Endpoint = loggingEndpoint;
                        otlpOptions.Protocol = ToOtlpProtocolForSerilog(options.GetLoggingProtocol());
                        otlpOptions.ResourceAttributes = resourceAttributes;

                        otlpOptions.IncludedData = IncludedData.MessageTemplateTextAttribute    // 기본
                                             | IncludedData.TraceIdField                    // 기본
                                             | IncludedData.SpanIdField                     // 기본
                                             | IncludedData.SpecRequiredResourceAttributes  // 기본
                                             | IncludedData.SourceContextAttribute;         // 확장: SourceContext
                    });
                }

                // Functorium 기본 확장 설정
                // - ErrorsDestructuringPolicy: Error 로그 구조화
                logging.Destructure.With<ErrorsDestructuringPolicy>();

                // 프로젝트별 추가 확장 설정 적용
                if (_loggingConfigurator != null)
                {
                    LoggingConfigurator configurator = new(options);
                    _loggingConfigurator(configurator);
                    configurator.Apply(logging);
                }
            });
    }

    private void ConfigureOpenTelemetryInternal(Dictionary<string, object> resourceAttributes, OpenTelemetryOptions options, SloConfiguration sloConfiguration)
    {
        _services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion);
                resource.AddAttributes(resourceAttributes);
            })
            .WithMetrics(metrics =>
            {
                // 기본 Instrumentation
                metrics.AddRuntimeInstrumentation();

                // 기본 Meter 등록
                // - ServiceName*: 프로젝트별 Meter (와일드카드로 모든 하위 네임스페이스 포함)
                // - Functorium.*: Functorium 프레임워크의 Meter
                metrics
                    .AddMeter($"{options.ServiceName}*")
                    .AddMeter($"{_functoriumNamespaceRoot}.*");

                // 프로젝트 어셈블리가 전달된 경우 해당 네임스페이스 루트 자동 등록
                // 예: "Observability.*"
                if (_projectNamespaceRoot != null)
                {
                    metrics.AddMeter($"{_projectNamespaceRoot}.*");
                }

                // SLO 정렬 Histogram 버킷 설정
                // application.usecase.*.duration 메트릭에 커스텀 버킷 적용
                // SloConfiguration.HistogramBuckets 값 사용 (기본값: 1ms ~ 10s)
                metrics.AddView(
                    instrumentName: "application.usecase.command.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = sloConfiguration.HistogramBuckets
                    });

                metrics.AddView(
                    instrumentName: "application.usecase.query.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = sloConfiguration.HistogramBuckets
                    });

                // OTLP Exporter 설정 (MetricsCollectorEndpoint가 설정된 경우에만)
                string metricsEndpoint = options.GetMetricsEndpoint();
                if (!string.IsNullOrWhiteSpace(metricsEndpoint))
                {
                    metrics.AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = new Uri(metricsEndpoint);
                        exporterOptions.Protocol = ToOtlpProtocolForExporter(options.GetMetricsProtocol());
                    });
                }

                // Prometheus Exporter (선택적)
                if (options.EnablePrometheusExporter)
                {
                    metrics.AddPrometheusExporter();
                }

                // 프로젝트별 확장 설정 적용
                if (_metricsConfigurator != null)
                {
                    MetricsConfigurator configurator = new(options);
                    _metricsConfigurator(configurator);
                    configurator.Apply(metrics);
                }
            })
            .WithTracing(tracing =>
            {
                // 기본 Instrumentation
                tracing.AddHttpClientInstrumentation(instrumentationOptions =>
                {
                    instrumentationOptions.RecordException = true;
                    // OTLP exporter의 내부 호출 제외
                    instrumentationOptions.FilterHttpRequestMessage = (httpRequestMessage) =>
                    {
                        Uri? uri = httpRequestMessage.RequestUri;
                        if (uri == null)
                            return true;

                        bool isOtlpEndpoint = (uri.Host == "127.0.0.1" || uri.Host == "localhost") &&
                                             (uri.Port == AspireDashboardOtlpPort || uri.Port == AspireDashboardOtlpSecondaryPort);

                        return !isOtlpEndpoint;
                    };
                });

                // 기본 ActivitySource 등록
                // - Functorium.*: Functorium 프레임워크의 ActivitySource
                // - ServiceName*: 프로젝트별 ActivitySource
                tracing
                    .AddSource($"{_functoriumNamespaceRoot}.*")
                    .AddSource($"{options.ServiceName}*");

                // 프로젝트 어셈블리가 전달된 경우 해당 네임스페이스 루트 자동 등록
                // 예: "Observability.*"
                if (_projectNamespaceRoot != null)
                {
                    tracing.AddSource($"{_projectNamespaceRoot}.*");
                }

                // Sampler 설정
                tracing.SetSampler(new TraceIdRatioBasedSampler(options.SamplingRate));

                // OTLP Exporter 설정 (TracingEndpoint가 설정된 경우에만)
                string tracingEndpoint = options.GetTracingEndpoint();
                if (!string.IsNullOrWhiteSpace(tracingEndpoint))
                {
                    tracing.AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = new Uri(tracingEndpoint);
                        exporterOptions.Protocol = ToOtlpProtocolForExporter(options.GetTracingProtocol());
                    });
                }

                // 프로젝트별 확장 설정 적용
                if (_tracingConfigurator != null)
                {
                    TracingConfigurator configurator = new(options);
                    _tracingConfigurator(configurator);
                    configurator.Apply(tracing);
                }
            });
    }

    private void RegisterAdapterObservabilityInternal(OpenTelemetryOptions options)
    {
        if (!_enableAdapterObservability)
            return;

        // ActivitySource 등록 (Singleton)
        // 프로젝트별 ActivitySource를 생성하여 추적에 사용
        // ServiceNamespace가 비어있으면 ServiceName 사용
        _services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
            string serviceNamespace = !string.IsNullOrWhiteSpace(opts.ServiceNamespace)
                ? opts.ServiceNamespace
                : opts.ServiceName;
            return new ActivitySource(serviceNamespace);
        });

        // IMeterFactory 등록 (Singleton) - Source Generator로 생성된 Pipeline에서 사용
        // Microsoft.Extensions.Diagnostics에서 기본 구현 제공
        _services.AddSingleton<IMeterFactory>(sp => new DefaultMeterFactory());
    }

    /// <summary>
    /// 기본 IMeterFactory 구현체
    /// </summary>
    private sealed class DefaultMeterFactory : IMeterFactory
    {
        private readonly Dictionary<string, Meter> _meters = new();
        private readonly object _lock = new();

        public Meter Create(MeterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            lock (_lock)
            {
                if (_meters.TryGetValue(options.Name, out var existingMeter))
                    return existingMeter;

                var meter = new Meter(options.Name, options.Version);
                _meters[options.Name] = meter;
                return meter;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var meter in _meters.Values)
                {
                    meter.Dispose();
                }
                _meters.Clear();
            }
        }
    }

    private void RegisterPipelinesInternal()
    {
        if (_usePipelinesWithDefaults)
        {
            // 기본값: 모든 파이프라인 활성화
            var configurator = new PipelineConfigurator();
            configurator.UseAll();
            configurator.Apply(_services);
        }
        else if (_pipelineConfigurator != null)
        {
            // 커스텀 설정 적용
            var configurator = new PipelineConfigurator();
            _pipelineConfigurator(configurator);
            configurator.Apply(_services);
        }
    }
}

