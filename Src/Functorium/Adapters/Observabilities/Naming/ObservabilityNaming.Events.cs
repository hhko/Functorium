using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Naming;

public static partial class ObservabilityNaming
{
    /// <summary>
    /// 로그 이벤트 ID (범위 분리)
    /// - Application: 1001-1004
    /// - Adapter: 2001-2004
    /// - DomainEvent: 3001-3004
    /// </summary>
    public static class EventIds
    {
        public static class Application
        {
            public static readonly EventId ApplicationRequest = new(1001, "application.request");
            public static readonly EventId ApplicationResponseSuccess = new(1002, "application.response.success");
            public static readonly EventId ApplicationResponseWarning = new(1003, "application.response.warning");
            public static readonly EventId ApplicationResponseError = new(1004, "application.response.error");
        }

        public static class Adapter
        {
            public static readonly EventId AdapterRequest = new(2001, "adapter.request");
            public static readonly EventId AdapterResponseSuccess = new(2002, "adapter.response.success");
            public static readonly EventId AdapterResponseWarning = new(2003, "adapter.response.warning");
            public static readonly EventId AdapterResponseError = new(2004, "adapter.response.error");
        }

        public static class DomainEvent
        {
            public static readonly EventId DomainEventRequest = new(3001, "domain_event.request");
            public static readonly EventId DomainEventResponseSuccess = new(3002, "domain_event.response.success");
            public static readonly EventId DomainEventResponseWarning = new(3003, "domain_event.response.warning");
            public static readonly EventId DomainEventResponseError = new(3004, "domain_event.response.error");
        }

        public static class DomainEventHandler
        {
            public static readonly EventId Request = new(3101, "domain_event_handler.request");
            public static readonly EventId ResponseSuccess = new(3102, "domain_event_handler.response.success");
            public static readonly EventId ResponseWarning = new(3103, "domain_event_handler.response.warning");
            public static readonly EventId ResponseError = new(3104, "domain_event_handler.response.error");
        }
    }
}
