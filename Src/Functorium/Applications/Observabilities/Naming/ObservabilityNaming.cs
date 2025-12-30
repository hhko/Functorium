namespace Functorium.Applications.Observabilities;

/// <summary>
/// 관찰 가능성 관련 통합 네이밍 규칙을 정의합니다.
/// 메트릭 이름, 태그 키, Span 이름 등의 단일 진실 공급원(Single Source of Truth)입니다.
/// </summary>
public static class ObservabilityNaming
{
    /// <summary>
    /// 레이어 상수
    /// </summary>
    public static class Layers
    {
        public const string Application = "application";
        public const string Adapter = "adapter";
    }

    /// <summary>
    /// 카테고리 상수
    /// </summary>
    public static class Categories
    {
        public const string Usecase = "usecase";
        public const string Query = "query";
        public const string Command = "command";
    }

    /// <summary>
    /// 상태 상수
    /// </summary>
    public static class Status
    {
        public const string Success = "success";
        public const string Failure = "failure";
    }

    /// <summary>
    /// CQRS 타입 상수
    /// </summary>
    public static class Cqrs
    {
        public const string Query = "query";
        public const string Command = "command";
        public const string Unknown = "unknown";
    }

    /// <summary>
    /// 메트릭 이름 생성 유틸리티
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// 요청 수 메트릭 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어 (application 또는 adapter)</param>
        /// <param name="category">카테고리</param>
        /// <returns>메트릭 이름 (예: application.usecase.requests)</returns>
        public static string Requests(string layer, string category)
            => $"{layer}.{category}.requests";

        /// <summary>
        /// 처리 시간 메트릭 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="category">카테고리</param>
        /// <returns>메트릭 이름 (예: application.usecase.duration)</returns>
        public static string Duration(string layer, string category)
            => $"{layer}.{category}.duration";

        /// <summary>
        /// 성공 응답 수 메트릭 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="category">카테고리</param>
        /// <returns>메트릭 이름 (예: application.usecase.responses.success)</returns>
        public static string ResponseSuccess(string layer, string category)
            => $"{layer}.{category}.responses.success";

        /// <summary>
        /// 실패 응답 수 메트릭 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="category">카테고리</param>
        /// <returns>메트릭 이름 (예: application.usecase.responses.failure)</returns>
        public static string ResponseFailure(string layer, string category)
            => $"{layer}.{category}.responses.failure";

        /// <summary>
        /// Adapter용 요청 메트릭 이름을 생성합니다.
        /// </summary>
        /// <param name="adapterCategory">어댑터 카테고리 (예: db, http)</param>
        /// <returns>메트릭 이름 (예: adapter.db.op.request)</returns>
        public static string AdapterRequest(string adapterCategory)
            => $"adapter.{adapterCategory}.op.request";

        /// <summary>
        /// Adapter용 성공 응답 메트릭 이름을 생성합니다.
        /// </summary>
        public static string AdapterResponseSuccess(string adapterCategory)
            => $"adapter.{adapterCategory}.op.response.success";

        /// <summary>
        /// Adapter용 실패 응답 메트릭 이름을 생성합니다.
        /// </summary>
        public static string AdapterResponseFailure(string adapterCategory)
            => $"adapter.{adapterCategory}.op.response.failure";

        /// <summary>
        /// Adapter용 처리 시간 메트릭 이름을 생성합니다.
        /// </summary>
        public static string AdapterDuration(string adapterCategory)
            => $"adapter.{adapterCategory}.op.duration";

        /// <summary>
        /// CQRS 타입별 Usecase 요청 메트릭 이름을 생성합니다.
        /// </summary>
        /// <param name="cqrsType">CQRS 타입 (query 또는 command)</param>
        /// <returns>메트릭 이름 (예: application.usecase.query.requests)</returns>
        public static string UsecaseRequest(string cqrsType)
            => $"application.usecase.{cqrsType}.requests";

        /// <summary>
        /// CQRS 타입별 Usecase 성공 응답 메트릭 이름을 생성합니다.
        /// </summary>
        public static string UsecaseResponseSuccess(string cqrsType)
            => $"application.usecase.{cqrsType}.responses.success";

        /// <summary>
        /// CQRS 타입별 Usecase 실패 응답 메트릭 이름을 생성합니다.
        /// </summary>
        public static string UsecaseResponseFailure(string cqrsType)
            => $"application.usecase.{cqrsType}.responses.failure";

        /// <summary>
        /// CQRS 타입별 Usecase 처리 시간 메트릭 이름을 생성합니다.
        /// </summary>
        public static string UsecaseDuration(string cqrsType)
            => $"application.usecase.{cqrsType}.duration";
    }

    /// <summary>
    /// Span 이름 생성 유틸리티
    /// </summary>
    public static class Spans
    {
        /// <summary>
        /// Span 작업 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="category">카테고리</param>
        /// <param name="handler">핸들러 이름</param>
        /// <param name="method">메서드 이름</param>
        /// <returns>Span 이름 (예: Application Usecase UserService.GetUser)</returns>
        public static string OperationName(string layer, string category, string handler, string method)
            => $"{layer} {category} {handler}.{method}";
    }

    /// <summary>
    /// 텔레메트리 태그 키 상수
    /// </summary>
    public static class Tags
    {
        // Request 관련 태그
        public const string Layer = "observability.layer";
        public const string Category = "observability.category";
        public const string Handler = "observability.handler";
        public const string HandlerCqrs = "observability.handler.cqrs";
        public const string Method = "observability.method";

        // Response 관련 태그
        public const string Status = "observability.status";
        public const string Elapsed = "observability.elapsed";

        // Error 관련 태그
        public const string ErrorType = "observability.error.type";
        public const string ErrorCode = "observability.error.code";
        public const string ErrorMessage = "observability.error.message";
        public const string ErrorCount = "observability.error.count";
    }

    /// <summary>
    /// 로그 필드 키 상수
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
    /// 로그 이벤트 ID 상수
    /// </summary>
    public static class EventIds
    {
        public static class Application
        {
            public static readonly Microsoft.Extensions.Logging.EventId ApplicationRequest = new(1001, nameof(ApplicationRequest));
            public static readonly Microsoft.Extensions.Logging.EventId ApplicationResponseSuccess = new(1002, nameof(ApplicationResponseSuccess));
            public static readonly Microsoft.Extensions.Logging.EventId ApplicationResponseWarning = new(1003, nameof(ApplicationResponseWarning));
            public static readonly Microsoft.Extensions.Logging.EventId ApplicationResponseError = new(1004, nameof(ApplicationResponseError));
        }

        public static class Adapter
        {
            public static readonly Microsoft.Extensions.Logging.EventId AdapterRequest = new(2001, nameof(AdapterRequest));
            public static readonly Microsoft.Extensions.Logging.EventId AdapterResponseSuccess = new(2002, nameof(AdapterResponseSuccess));
            public static readonly Microsoft.Extensions.Logging.EventId AdapterResponseWarning = new(2003, nameof(AdapterResponseWarning));
            public static readonly Microsoft.Extensions.Logging.EventId AdapterResponseError = new(2004, nameof(AdapterResponseError));
        }
    }
}
