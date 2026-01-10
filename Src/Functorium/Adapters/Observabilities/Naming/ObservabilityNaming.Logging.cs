using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Naming;

public static partial class ObservabilityNaming
{
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
}
