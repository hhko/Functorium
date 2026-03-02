using System.Reflection;
using Ardalis.SmartEnum;
using FluentValidation;
using Functorium.Adapters.Observabilities.Loggers;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities;

public sealed class OpenTelemetryOptions
    : IStartupOptionsLogger
    , IOpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// 서비스 네임스페이스(그룹)
    /// </summary>
    public string ServiceNamespace { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 서비스 버전은 어셈블리 버전으로 자동 설정됩니다.
    /// Directory.Build.props의 "<AssemblyVersion>" 값과 동기화됩니다.
    /// </summary>
    public string ServiceVersion { get; } =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

    /// <summary>
    /// 서비스 인스턴스 ID는 호스트네임으로 자동 설정됩니다.
    /// - Kubernetes: Pod 이름 (예: myapp-7b9d8c6f5d-abc12)
    /// - Docker: 컨테이너 ID 또는 설정된 hostname
    /// - Windows/Linux: 머신 이름
    /// </summary>
    public string ServiceInstanceId { get; } =
        Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;

    /// <summary>
    /// 통합 OTLP Collector 엔드포인트 (모든 신호를 동일 엔드포인트로 전송)
    /// 예: Aspire Dashboard (http://127.0.0.1:18889)
    /// </summary>
    public string CollectorEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Tracing 전용 OTLP 엔드포인트 (선택적)
    /// 설정 시 CollectorEndpoint 대신 이 엔드포인트 사용
    /// 예: Data Prepper OTel Tracing Source (http://localhost:21890)
    /// </summary>
    public string? TracingEndpoint { get; set; }

    /// <summary>
    /// Metrics 전용 OTLP 엔드포인트 (선택적)
    /// 설정 시 OtlpCollectorHost 대신 이 엔드포인트 사용
    /// 예: Data Prepper OTel Metrics Source (http://localhost:21891)
    /// </summary>
    public string? MetricsEndpoint { get; set; }

    /// <summary>
    /// Logging 전용 OTLP 엔드포인트 (선택적)
    /// 설정 시 CollectorEndpoint 대신 이 엔드포인트 사용
    /// 예: Data Prepper OTel Logging Source (http://localhost:21892)
    /// </summary>
    public string? LoggingEndpoint { get; set; }

    /// <summary>
    /// 통합 OTLP Protocol 설정 (모든 신호를 동일 Protocol로 전송)
    /// 개별 Protocol이 설정되지 않은 경우 사용
    /// </summary>
    public string CollectorProtocol { get; set; } = OtlpCollectorProtocol.Grpc.Name;

    /// <summary>
    /// Tracing 전용 OTLP Protocol 설정 (선택적)
    /// 설정 시 CollectorProtocol 대신 이 Protocol 사용
    /// </summary>
    public string? TracingProtocol { get; set; }

    /// <summary>
    /// Metrics 전용 OTLP Protocol 설정 (선택적)
    /// 설정 시 CollectorProtocol 대신 이 Protocol 사용
    /// </summary>
    public string? MetricsProtocol { get; set; }

    /// <summary>
    /// Logging 전용 OTLP Protocol 설정 (선택적)
    /// 설정 시 CollectorProtocol 대신 이 Protocol 사용
    /// </summary>
    public string? LoggingProtocol { get; set; }

    public double SamplingRate { get; set; } = 1.0; // 0.0 ~ 1.0 (0% ~ 100%)
    public bool EnablePrometheusExporter { get; set; } = false;

    /// <summary>
    /// OTLP Protocol SmartEnum
    /// OpenTelemetry Protocol 설정을 타입 안전하게 관리
    /// </summary>
    public sealed class OtlpCollectorProtocol : SmartEnum<OtlpCollectorProtocol>
    {
        /// <summary>
        /// gRPC 프로토콜 (기본값)
        /// </summary>
        public static readonly OtlpCollectorProtocol Grpc = new(nameof(Grpc), 1);

        /// <summary>
        /// HTTP/Protobuf 프로토콜
        /// </summary>
        public static readonly OtlpCollectorProtocol HttpProtobuf = new(nameof(HttpProtobuf), 2);

        private OtlpCollectorProtocol(string name, int value) : base(name, value)
        {
        }
    }

    /// <summary>
    /// Tracing Protocol 반환 (개별 설정 우선, 없으면 통합 Protocol)
    /// </summary>
    public OtlpCollectorProtocol GetTracingProtocol()
    {
        string protocolName = !string.IsNullOrWhiteSpace(TracingProtocol)
            ? TracingProtocol
            : CollectorProtocol;

        return SmartEnum<OtlpCollectorProtocol>.TryFromName(protocolName, out OtlpCollectorProtocol? protocol)
            ? protocol
            : OtlpCollectorProtocol.Grpc;
    }

    /// <summary>
    /// Metrics Protocol 반환 (개별 설정 우선, 없으면 통합 Protocol)
    /// </summary>
    public OtlpCollectorProtocol GetMetricsProtocol()
    {
        string protocolName = !string.IsNullOrWhiteSpace(MetricsProtocol)
            ? MetricsProtocol
            : CollectorProtocol;

        return SmartEnum<OtlpCollectorProtocol>.TryFromName(protocolName, out OtlpCollectorProtocol? protocol)
            ? protocol
            : OtlpCollectorProtocol.Grpc;
    }

    /// <summary>
    /// Logging Protocol 반환 (개별 설정 우선, 없으면 통합 Protocol)
    /// </summary>
    public OtlpCollectorProtocol GetLoggingProtocol()
    {
        string protocolName = !string.IsNullOrWhiteSpace(LoggingProtocol)
            ? LoggingProtocol
            : CollectorProtocol;

        return SmartEnum<OtlpCollectorProtocol>.TryFromName(protocolName, out OtlpCollectorProtocol? protocol)
            ? protocol
            : OtlpCollectorProtocol.Grpc;
    }

    /// <summary>
    /// Tracing 엔드포인트 반환
    /// - null: CollectorEndpoint 사용 (기본값)
    /// - "": 비활성화 (빈 문자열 반환)
    /// - "http://...": 해당 엔드포인트 사용
    /// </summary>
    public string GetTracingEndpoint()
    {
        // null인 경우: CollectorEndpoint 사용
        if (TracingEndpoint == null)
            return CollectorEndpoint;

        // 빈 문자열이거나 whitespace: 비활성화 (빈 문자열 반환)
        if (string.IsNullOrWhiteSpace(TracingEndpoint))
            return string.Empty;

        // 값이 있는 경우: 해당 값 사용
        return TracingEndpoint;
    }

    /// <summary>
    /// Metrics 엔드포인트 반환
    /// - null: CollectorEndpoint 사용 (기본값)
    /// - "": 비활성화 (빈 문자열 반환)
    /// - "http://...": 해당 엔드포인트 사용
    /// </summary>
    public string GetMetricsEndpoint()
    {
        // null인 경우: CollectorEndpoint 사용
        if (MetricsEndpoint == null)
            return CollectorEndpoint;

        // 빈 문자열이거나 whitespace: 비활성화 (빈 문자열 반환)
        if (string.IsNullOrWhiteSpace(MetricsEndpoint))
            return string.Empty;

        // 값이 있는 경우: 해당 값 사용
        return MetricsEndpoint;
    }

    /// <summary>
    /// Logging 엔드포인트 반환
    /// - null: CollectorEndpoint 사용 (기본값)
    /// - "": 비활성화 (빈 문자열 반환)
    /// - "http://...": 해당 엔드포인트 사용
    /// </summary>
    public string GetLoggingEndpoint()
    {
        // null인 경우: CollectorEndpoint 사용
        if (LoggingEndpoint == null)
            return CollectorEndpoint;

        // 빈 문자열이거나 whitespace: 비활성화 (빈 문자열 반환)
        if (string.IsNullOrWhiteSpace(LoggingEndpoint))
            return string.Empty;

        // 값이 있는 경우: 해당 값 사용
        return LoggingEndpoint;
    }

    /// <summary>
    /// StartupLogger에 OpenTelemetry 설정 정보를 출력합니다.
    /// </summary>
    public void LogConfiguration(ILogger logger)
    {
        const int labelWidth = 20;
        string tracingEndpoint = GetTracingEndpoint();
        string metricsEndpoint = GetMetricsEndpoint();
        string loggingEndpoint = GetLoggingEndpoint();

        // 대주제: OpenTelemetry Configuration
        logger.LogInformation("OpenTelemetry Configuration");
        logger.LogInformation("");

        // 세부주제: Service Information
        logger.LogInformation("  Service Information");
        logger.LogInformation("    {Label}: {Value}", "Namespace".PadRight(labelWidth), ServiceNamespace);
        logger.LogInformation("    {Label}: {Value}", "Name".PadRight(labelWidth), ServiceName);
        logger.LogInformation("    {Label}: {Value}", "Version".PadRight(labelWidth), ServiceVersion);
        logger.LogInformation("    {Label}: {Value}", "Instance ID".PadRight(labelWidth), ServiceInstanceId);
        logger.LogInformation("");

        // 세부주제: Logging Configuration
        logger.LogInformation("  Logging Configuration");
        logger.LogInformation("    {Label}: {Value}", "Endpoint".PadRight(labelWidth), string.IsNullOrWhiteSpace(loggingEndpoint) ? "(disabled)" : loggingEndpoint);
        logger.LogInformation("    {Label}: {Value}", "Protocol".PadRight(labelWidth), GetLoggingProtocol().Name);
        logger.LogInformation("");

        // 세부주제: Tracing Configuration
        logger.LogInformation("  Tracing Configuration");
        logger.LogInformation("    {Label}: {Value}", "Endpoint".PadRight(labelWidth), string.IsNullOrWhiteSpace(tracingEndpoint) ? "(disabled)" : tracingEndpoint);
        logger.LogInformation("    {Label}: {Value}", "Protocol".PadRight(labelWidth), GetTracingProtocol().Name);
        logger.LogInformation("");

        // 세부주제: Metrics Configuration
        logger.LogInformation("  Metrics Configuration");
        logger.LogInformation("    {Label}: {Value}", "Endpoint".PadRight(labelWidth), string.IsNullOrWhiteSpace(metricsEndpoint) ? "(disabled)" : metricsEndpoint);
        logger.LogInformation("    {Label}: {Value}", "Protocol".PadRight(labelWidth), GetMetricsProtocol().Name);
        logger.LogInformation("");

        // 세부주제: Additional Settings
        logger.LogInformation("  Additional Settings");
        logger.LogInformation("    {Label}: {Value}", "Sampling Rate".PadRight(labelWidth), $"{SamplingRate:P0}");
        logger.LogInformation("    {Label}: {Value}", "Prometheus Exporter".PadRight(labelWidth), EnablePrometheusExporter ? "Enabled" : "Disabled");
    }

    public sealed class Validator : AbstractValidator<OpenTelemetryOptions>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceNamespace)
                .NotEmpty()
                .WithMessage($"{nameof(ServiceNamespace)} is required.");

            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .WithMessage($"{nameof(ServiceName)} is required.");

            // OtlpCollectorHost 또는 개별 엔드포인트 중 하나는 설정되어야 함
            RuleFor(x => x)
                .Must(options =>
                    !string.IsNullOrWhiteSpace(options.CollectorEndpoint) ||
                    !string.IsNullOrWhiteSpace(options.TracingEndpoint) ||
                    !string.IsNullOrWhiteSpace(options.MetricsEndpoint) ||
                    !string.IsNullOrWhiteSpace(options.LoggingEndpoint))
                .WithMessage($"At least one OTLP endpoint must be configured: {nameof(CollectorEndpoint)} or individual endpoints.");

            RuleFor(x => x.SamplingRate)
                .InclusiveBetween(0.0, 1.0)
                .WithMessage($"{nameof(SamplingRate)} must be between 0.0 and 1.0.");

            // Protocol 검증 (SmartEnum을 사용한 타입 안전한 검증)
            string validProtocols = string.Join(", ", SmartEnum<OtlpCollectorProtocol>.List.Select(p => p.Name));

            // 통합 Protocol 검증
            RuleFor(x => x.CollectorProtocol)
                .Must(protocol => SmartEnum<OtlpCollectorProtocol>.TryFromName(protocol, out _))
                .WithMessage($"{nameof(CollectorProtocol)} must be one of: {validProtocols}");

            // 개별 Protocol 검증 (설정된 경우에만)
            RuleFor(x => x.TracingProtocol)
                .Must(protocol => SmartEnum<OtlpCollectorProtocol>.TryFromName(protocol!, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.TracingProtocol))
                .WithMessage($"{nameof(TracingProtocol)} must be one of: {validProtocols}");

            RuleFor(x => x.MetricsProtocol)
                .Must(protocol => SmartEnum<OtlpCollectorProtocol>.TryFromName(protocol!, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.MetricsProtocol))
                .WithMessage($"{nameof(MetricsProtocol)} must be one of: {validProtocols}");

            RuleFor(x => x.LoggingProtocol)
                .Must(protocol => SmartEnum<OtlpCollectorProtocol>.TryFromName(protocol!, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.LoggingProtocol))
                .WithMessage($"{nameof(LoggingProtocol)} must be one of: {validProtocols}");
        }
    }
}
