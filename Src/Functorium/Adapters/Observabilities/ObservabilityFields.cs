using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// Observability 관련 상수 및 필드 정의 (Mock).
/// Source Generator가 생성하는 코드에서 사용됩니다.
/// </summary>
public static class ObservabilityFields
{
    public static class Request
    {
        public static class Layer
        {
            public const string Adapter = nameof(Adapter);
            public const string Application = nameof(Application);
        }

        public static class Category
        {
            public const string Usecase = nameof(Usecase);
            public const string Adapter = nameof(Adapter);
        }

        public static class HandlerCqrs
        {
            public const string Command = nameof(Command);
            public const string Query = nameof(Query);

            public const string Unknown = nameof(Unknown);
        }

        public static class TelemetryKeys
        {
            public const string Layer = "request.layer";
            public const string Category = "request.category";
            public const string Handler = "request.handler";
            public const string HandlerCqrs = "request.handler.cqrs";
            public const string HandlerMethod = "request.handler.method";
        }

        public static class TelemetryLogKeys
        {
            public const string Layer = "RequestLayer";
            public const string Category = "RequestCategory";
            public const string Handler = "RequestHandler";
            public const string HandlerCqrs = "RequestHandlerCqrs";
            public const string HandlerMethod = "RequestHandlerMethod";
            public const string Data = "Request";
        }
    }

    public static class Response
    {
        public static class Status
        {
            public const string Success = nameof(Success);
            public const string Failure = nameof(Failure);
        }

        public static class TelemetryKeys
        {
            public const string Status = "response.status";
            public const string Elapsed = "response.elapsed";
        }

        public static class TelemetryLogKeys
        {
            public const string Data = "Response";
            public const string Status = nameof(Status);
            public const string Elapsed = nameof(Elapsed);
        }
    }

    public static class Errors
    {
        public static class TelemetryKeys
        {
            public const string Type = "error.type";
            public const string Code = "error.code";
            public const string Message = "error.message";
            public const string Count = "error.count";
        }

        public static class Keys
        {
            public const string Data = "Error";
        }
    }

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
            public static readonly EventId AdapterRequest = new(1001, nameof(AdapterRequest));
            public static readonly EventId AdapterResponseSuccess = new(1002, nameof(AdapterResponseSuccess));
            public static readonly EventId AdapterResponseWarning = new(1003, nameof(AdapterResponseWarning));
            public static readonly EventId AdapterResponseError = new(1004, nameof(AdapterResponseError));
        }
    }

    public static class Metrics
    {
        public static string GetRequest(string category) => $"adapter.{category}.op.request";
        public static string GetResponseSuccess(string category) => $"adapter.{category}.op.response.success";
        public static string GetResponseFailure(string category) => $"adapter.{category}.op.response.failure";
        public static string GetDuration(string category) => $"adapter.{category}.op.duration";
    }
}
