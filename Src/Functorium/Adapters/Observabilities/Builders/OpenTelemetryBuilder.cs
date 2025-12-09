using Functorium.Abstractions.Errors.DestructuringPolicies;
using Functorium.Adapters.Observabilities.Builders.Configurators;
using Functorium.Adapters.Observabilities.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    private readonly OpenTelemetryOptions _options;
    private readonly string _frameworkNamespaceRoot;

    private Action<LoggingConfigurator>? _loggingConfigurator;
    private Action<MetricsConfigurator>? _metricsConfigurator;
    private Action<TracingConfigurator>? _tracingConfigurator;
    private Action<Microsoft.Extensions.Logging.ILogger>? _startupLoggerConfigurator;

    internal OpenTelemetryBuilder(
        IServiceCollection services,
        IConfiguration configuration,
        OpenTelemetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);

        _services = services;
        _configuration = configuration;
        _options = options;

        // 네임스페이스 루트 이름 동적 추출
        // 예: "Framework.Adapters.Observabilities.Abstractions.Registrations" → "Framework"
        _frameworkNamespaceRoot = ExtractNamespaceRoot(typeof(OpenTelemetryBuilder).Namespace);
    }

    /// <summary>
    /// 네임스페이스에서 루트 이름을 추출합니다.
    /// </summary>
    /// <param name="fullNamespace">전체 네임스페이스 (예: "Framework.Adapters.Observabilities")</param>
    /// <returns>루트 네임스페이스 (예: "Framework")</returns>
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
    /// Serilog 확장 설정
    /// </summary>
    /// <param name="configure">LoggerConfigurationBuilder를 사용한 설정 액션</param>
    public OpenTelemetryBuilder ConfigureSerilog(Action<LoggingConfigurator> configure)
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
    /// OpenTelemetry Traces 확장 설정
    /// </summary>
    /// <param name="configure">TracesConfigurationBuilder를 사용한 설정 액션</param>
    public OpenTelemetryBuilder ConfigureTraces(Action<TracingConfigurator> configure)
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
    ///     .RegisterObservability(configuration)
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
    /// OpenTelemetryOptions 접근
    /// </summary>
    public OpenTelemetryOptions Options => _options;

    /// <summary>
    /// 모든 설정을 적용하고 IServiceCollection 반환
    /// </summary>
    public IServiceCollection Build()
    {
        // Resource Attributes 공통 정의
        Dictionary<string, object> resourceAttributes = CreateResourceAttributes(_options);

        // Serilog 설정 적용
        ConfigureSerilogInternal(resourceAttributes);

        // OpenTelemetry 설정 적용
        ConfigureOpenTelemetryInternal(resourceAttributes);

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

    private void ConfigureSerilogInternal(Dictionary<string, object> resourceAttributes)
    {
        _services
            .AddSerilog(logging =>
            {
                // 기본 설정: ReadFrom.Configuration으로 appsettings.json 읽기
                logging.ReadFrom.Configuration(_configuration);

                // WriteTo.OpenTelemetry 설정 (LoggingCollectorEndpoint가 설정된 경우에만)
                string loggingEndpoint = _options.GetLoggingEndpoint();
                if (!string.IsNullOrWhiteSpace(loggingEndpoint))
                {
                    logging.WriteTo.OpenTelemetry(options =>
                    {
                        options.Endpoint = loggingEndpoint;
                        options.Protocol = ToOtlpProtocolForSerilog(_options.GetLogsProtocol());
                        options.ResourceAttributes = resourceAttributes;

                        options.IncludedData = IncludedData.MessageTemplateTextAttribute    // 기본
                                             | IncludedData.TraceIdField                    // 기본
                                             | IncludedData.SpanIdField                     // 기본
                                             | IncludedData.SpecRequiredResourceAttributes  // 기본
                                             | IncludedData.SourceContextAttribute;         // 확장: SourceContext
                    });
                }

                // Framework 기본 확장 설정
                // - ErrorsDestructuringPolicy: Error 로그 구조화
                logging.Destructure.With<ErrorsDestructuringPolicy>();

                // 프로젝트별 추가 확장 설정 적용
                if (_loggingConfigurator != null)
                {
                    LoggingConfigurator configurator = new(_options);
                    _loggingConfigurator(configurator);
                    configurator.Apply(logging);
                }
            });
    }

    private void ConfigureOpenTelemetryInternal(Dictionary<string, object> resourceAttributes)
    {
        _services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: _options.ServiceName,
                    serviceVersion: _options.ServiceVersion);
                resource.AddAttributes(resourceAttributes);
            })
            .WithMetrics(metrics =>
            {
                // 기본 Instrumentation
                metrics.AddRuntimeInstrumentation();

                // 기본 Meter 등록
                // - ServiceName*: 프로젝트별 Meter (와일드카드로 모든 하위 네임스페이스 포함)
                // - Functorium.*: Functorium 프로젝트의 Meter (네임스페이스 루트 이름 자동 추출)
                metrics
                    .AddMeter($"{_options.ServiceName}*")
                    .AddMeter($"{_frameworkNamespaceRoot}.*");

                // OTLP Exporter 설정 (MetricsCollectorEndpoint가 설정된 경우에만)
                string metricsEndpoint = _options.GetMetricsEndpoint();
                if (!string.IsNullOrWhiteSpace(metricsEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(metricsEndpoint);
                        options.Protocol = ToOtlpProtocolForExporter(_options.GetMetricsProtocol());
                    });
                }

                // Prometheus Exporter (선택적)
                if (_options.EnablePrometheusExporter)
                {
                    metrics.AddPrometheusExporter();
                }

                // 프로젝트별 확장 설정 적용
                if (_metricsConfigurator != null)
                {
                    MetricsConfigurator configurator = new(_options);
                    _metricsConfigurator(configurator);
                    configurator.Apply(metrics);
                }
            })
            .WithTracing(tracing =>
            {
                // 기본 Instrumentation
                tracing.AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    // OTLP exporter의 내부 호출 제외
                    options.FilterHttpRequestMessage = (httpRequestMessage) =>
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
                // - Functorium: Functorium 프로젝트의 ActivitySource (네임스페이스 루트 이름 자동 추출)
                // - ServiceName: 프로젝트별 ActivitySource
                tracing
                    .AddSource($"{_frameworkNamespaceRoot}.*")
                    .AddSource($"{_options.ServiceName}*");

                // Sampler 설정
                tracing.SetSampler(new TraceIdRatioBasedSampler(_options.SamplingRate));

                // OTLP Exporter 설정 (TracingCollectorEndpoint가 설정된 경우에만)
                string tracingEndpoint = _options.GetTracingEndpoint();
                if (!string.IsNullOrWhiteSpace(tracingEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(tracingEndpoint);
                        options.Protocol = ToOtlpProtocolForExporter(_options.GetTracingProtocol());
                    });
                }

                // 프로젝트별 확장 설정 적용
                if (_tracingConfigurator != null)
                {
                    TracingConfigurator configurator = new(_options);
                    _tracingConfigurator(configurator);
                    configurator.Apply(tracing);
                }
            });
    }
}

