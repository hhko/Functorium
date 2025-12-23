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
            public const string HandlerCqrs = "request.handler.cqrs";
            public const string Handler = "request.handler";
            public const string HandlerMethod = "request.handler.method";
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
    }

    public static class EventIds
    {
        public static class Adapter
        {
            public static readonly EventId AdapterRequest = new(1001, nameof(AdapterRequest));
            public static readonly EventId AdapterResponse = new(1002, nameof(AdapterResponse));
            public static readonly EventId AdapterResponseWarning = new(1003, nameof(AdapterResponseWarning));
            public static readonly EventId AdapterResponseError = new(1004, nameof(AdapterResponseError));
        }
    }
}
