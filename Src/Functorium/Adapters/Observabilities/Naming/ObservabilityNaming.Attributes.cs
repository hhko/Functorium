namespace Functorium.Adapters.Observabilities.Naming;

public static partial class ObservabilityNaming
{
    /// <summary>
    /// OpenTelemetry 표준 속성 키
    /// https://opentelemetry.io/docs/specs/semconv/
    /// </summary>
    public static class OTelAttributes
    {
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
    /// 커스텀 속성 키 (3-Pillar 공통: Logging, Tracing, Metrics)
    /// </summary>
    public static class CustomAttributes
    {
        // Request 속성
        public const string RequestMessage = "request.message";
        public const string RequestLayer = "request.layer";
        public const string RequestCategory = "request.category";
        public const string RequestHandler = "request.handler";
        public const string RequestHandlerCqrs = "request.handler.cqrs";
        public const string RequestHandlerMethod = "request.handler.method";

        // Response 속성
        public const string ResponseMessage = "response.message";
        public const string ResponseStatus = "response.status";
        public const string ResponseElapsed = "response.elapsed";

        // Error 속성
        public const string ErrorCode = "error.code";

        // SLO 속성
        public const string SloLatency = "slo.latency";
    }

    /// <summary>
    /// 메트릭 이름 생성 유틸리티
    /// 성공/실패 구분은 response.status 태그로 수행
    /// </summary>
    public static class Metrics
    {
        // 범용 메트릭 (레이어별)
        public static string Requests(string layer, string category) =>
            $"{layer}.{category}.requests";

        public static string Responses(string layer, string category) =>
            $"{layer}.{category}.responses";

        public static string Duration(string layer, string category) =>
            $"{layer}.{category}.duration";


        // Application Usecase 메트릭 (CQRS 타입별)
        public static string UsecaseRequest(string cqrsType) =>
            $"application.usecase.{cqrsType}.requests";

        public static string UsecaseResponse(string cqrsType) =>
            $"application.usecase.{cqrsType}.responses";

        public static string UsecaseDuration(string cqrsType) =>
            $"application.usecase.{cqrsType}.duration";


        // Adapter 메트릭 (카테고리별)
        public static string AdapterRequest(string category) =>
            $"adapter.{category}.requests";

        public static string AdapterResponse(string category) =>
            $"adapter.{category}.responses";

        public static string AdapterDuration(string category) =>
            $"adapter.{category}.duration";
    }

    /// <summary>
    /// Span 이름 생성 유틸리티 (Adapter Layer 전용)
    /// Application Layer는 UsecaseTracingPipeline에서 직접 생성
    /// </summary>
    public static class Spans
    {
        /// <summary>
        /// Adapter Layer Span 작업 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어 (adapter)</param>
        /// <param name="category">카테고리 (repository 등)</param>
        /// <param name="handler">핸들러 이름</param>
        /// <param name="method">메서드 이름</param>
        /// <returns>Span 이름 (예: adapter repository OrderRepository.GetById)</returns>
        public static string OperationName(string layer, string category, string handler, string method) =>
            $"{layer} {category} {handler}.{method}";
    }
}
