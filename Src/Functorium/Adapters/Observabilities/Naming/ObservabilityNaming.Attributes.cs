namespace Functorium.Adapters.Observabilities.Naming;

public static partial class ObservabilityNaming
{
    /// <summary>
    /// OpenTelemetry 표준 attributes
    /// https://opentelemetry.io/docs/specs/semconv/
    /// </summary>
    public static class OTelAttributes
    {
        // Code attributes
        public const string CodeFunction = "code.function";
        public const string CodeNamespace = "code.namespace";
        public const string CodeFilepath = "code.filepath";
        public const string CodeLineno = "code.lineno";

        // Exception attributes
        public const string ExceptionType = "exception.type";
        public const string ExceptionMessage = "exception.message";
        public const string ExceptionStacktrace = "exception.stacktrace";

        // Error attributes
        public const string ErrorType = "error.type";

        // Service attributes (Resource)
        public const string ServiceName = "service.name";
        public const string ServiceVersion = "service.version";
        public const string ServiceNamespace = "service.namespace";

        // Deployment attributes
        public const string DeploymentEnvironment = "deployment.environment";
    }

    /// <summary>
    /// 커스텀 attributes (request.*, response.*, error.* 네임스페이스)
    /// </summary>
    public static class CustomAttributes
    {
        // Request attributes
        public const string RequestLayer = "request.layer";
        public const string RequestCategory = "request.category";
        public const string RequestHandler = "request.handler";
        public const string RequestHandlerCqrs = "request.handler.cqrs";
        public const string RequestHandlerMethod = "request.handler.method";

        // Response attributes
        public const string ResponseStatus = "response.status";
        public const string ResponseElapsed = "response.elapsed";

        // Error attributes
        public const string ErrorCode = "error.code";
        public const string ErrorMessage = "error.message";
        public const string ErrorCount = "error.count";

        // SLO attributes
        public const string SloLatency = "slo.latency";
    }

    /// <summary>
    /// SLO 상태 값 (3단계 심각도)
    /// </summary>
    public static class SloStatus
    {
        /// <summary>정상: elapsed &lt;= P95</summary>
        public const string Ok = "ok";

        /// <summary>경고: P95 &lt; elapsed &lt;= P99</summary>
        public const string P95Exceeded = "p95_exceeded";

        /// <summary>심각: elapsed &gt; P99</summary>
        public const string P99Exceeded = "p99_exceeded";
    }
}
