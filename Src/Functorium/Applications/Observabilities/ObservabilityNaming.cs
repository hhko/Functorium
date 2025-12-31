using Microsoft.Extensions.Logging;

namespace Functorium.Applications.Observabilities;

/// <summary>
/// 관찰 가능성 관련 통합 네이밍 규칙을 정의합니다.
/// 메트릭 이름, 태그 키, Span 이름 등의 단일 진실 공급원(Single Source of Truth)입니다.
///
/// OpenTelemetry Semantic Conventions 준수:
/// - 표준 attributes: code.function, error.type, exception.message 등
/// - 커스텀 attributes: request.*, response.*, error.* 네임스페이스
/// </summary>
public static class ObservabilityNaming
{
    /// <summary>
    /// 레이어 상수 (소문자)
    /// </summary>
    public static class Layers
    {
        public const string Application = "application";
        public const string Adapter = "adapter";
    }

    /// <summary>
    /// 카테고리 상수 (소문자)
    /// </summary>
    public static class Categories
    {
        public const string Usecase = "usecase";
        public const string Repository = "repository";
        public const string Adapter = "adapter";
    }

    /// <summary>
    /// 상태 상수 (소문자)
    /// </summary>
    public static class Status
    {
        public const string Success = "success";
        public const string Failure = "failure";
    }

    /// <summary>
    /// CQRS 타입 상수 (소문자)
    /// </summary>
    public static class Cqrs
    {
        public const string Command = "command";
        public const string Query = "query";
        public const string Unknown = "unknown";
    }

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
    }

    /// <summary>
    /// 구조화된 로깅을 위한 로그 필드 키 (PascalCase)
    /// </summary>
    public static class LogKeys
    {
        // Request 관련
        public const string RequestLayer = "RequestLayer";
        public const string RequestCategory = "RequestCategory";
        public const string RequestHandler = "RequestHandler";
        public const string RequestHandlerCqrs = "RequestHandlerCqrs";
        public const string RequestHandlerMethod = "RequestHandlerMethod";
        public const string RequestData = "Request";

        // Response 관련
        public const string ResponseData = "Response";
        public const string ResponseStatus = "Status";
        public const string ResponseElapsed = "Elapsed";

        // Error 관련
        public const string ErrorData = "Error";
    }

    /// <summary>
    /// 로그 이벤트 ID (범위 분리)
    /// - Application: 1001-1004
    /// - Adapter: 2001-2004
    /// </summary>
    public static class EventIds
    {
        public static class Application
        {
            public static readonly EventId ApplicationRequest = new(1001, nameof(ApplicationRequest));
            public static readonly EventId ApplicationResponseSuccess = new(1002, nameof(ApplicationResponseSuccess));
            public static readonly EventId ApplicationResponseWarning = new(1003, nameof(ApplicationResponseWarning));
            public static readonly EventId ApplicationResponseError = new(1004, nameof(ApplicationResponseError));
        }

        public static class Adapter
        {
            public static readonly EventId AdapterRequest = new(2001, nameof(AdapterRequest));
            public static readonly EventId AdapterResponseSuccess = new(2002, nameof(AdapterResponseSuccess));
            public static readonly EventId AdapterResponseWarning = new(2003, nameof(AdapterResponseWarning));
            public static readonly EventId AdapterResponseError = new(2004, nameof(AdapterResponseError));
        }
    }

    /// <summary>
    /// 메트릭 이름 생성 유틸리티
    /// functorium.* 네임스페이스 사용
    /// </summary>
    public static class Metrics
    {
        // Application Usecase 메트릭 (CQRS 타입별)
        public static string UsecaseRequest(string cqrsType) =>
            $"functorium.application.usecase.{cqrsType}.requests";

        public static string UsecaseResponseSuccess(string cqrsType) =>
            $"functorium.application.usecase.{cqrsType}.responses.success";

        public static string UsecaseResponseFailure(string cqrsType) =>
            $"functorium.application.usecase.{cqrsType}.responses.failure";

        public static string UsecaseDuration(string cqrsType) =>
            $"functorium.application.usecase.{cqrsType}.duration";

        // Adapter 메트릭 (카테고리별)
        public static string AdapterRequest(string category) =>
            $"functorium.adapter.{category}.requests";

        public static string AdapterResponseSuccess(string category) =>
            $"functorium.adapter.{category}.responses.success";

        public static string AdapterResponseFailure(string category) =>
            $"functorium.adapter.{category}.responses.failure";

        public static string AdapterDuration(string category) =>
            $"functorium.adapter.{category}.duration";

        // 범용 메트릭 (레이어별)
        public static string Requests(string layer, string category) =>
            $"functorium.{layer}.{category}.requests";

        public static string ResponseSuccess(string layer, string category) =>
            $"functorium.{layer}.{category}.responses.success";

        public static string ResponseFailure(string layer, string category) =>
            $"functorium.{layer}.{category}.responses.failure";

        public static string Duration(string layer, string category) =>
            $"functorium.{layer}.{category}.duration";
    }

    /// <summary>
    /// Span 이름 생성 유틸리티
    /// </summary>
    public static class Spans
    {
        /// <summary>
        /// Span 작업 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어 (application/adapter)</param>
        /// <param name="category">카테고리 (usecase/repository)</param>
        /// <param name="handler">핸들러 이름</param>
        /// <param name="method">메서드 이름</param>
        /// <returns>Span 이름 (예: application usecase CreateProductCommand.Handle)</returns>
        public static string OperationName(string layer, string category, string handler, string method) =>
            $"{layer} {category} {handler}.{method}";
    }
}