using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Abstractions.Fields;

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
            public const string Adapter = "Adapter";
            public const string Application = "Application";
            public const string Domain = "Domain";
        }
    }

    public static class Response
    {
        public static class Status
        {
            public const string Success = "Success";
            public const string Failure = "Failure";
        }
    }

    public static class EventIds
    {
        public static class Adapter
        {
            public static readonly EventId AdapterRequest = new(1001, "AdapterRequest");
            public static readonly EventId AdapterResponse = new(1002, "AdapterResponse");
            public static readonly EventId AdapterResponseWarning = new(1003, "AdapterResponseWarning");
            public static readonly EventId AdapterResponseError = new(1004, "AdapterResponseError");
        }
    }
}
