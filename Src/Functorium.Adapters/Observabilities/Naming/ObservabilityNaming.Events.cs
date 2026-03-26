using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Naming;

public static partial class ObservabilityNaming
{
    /// <summary>
    /// 로그 이벤트 ID (범위 분리)
    /// - Application: 1001-1004 (Usecase, DomainEventHandler 포함)
    /// - Adapter: 2001-2004 (DomainEvent Publisher 포함)
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

            // ObservableSignal: 개발자가 Adapter 구현 내부에서 직접 출력하는 로그
            public static readonly EventId ObservableSignalDebug = new(2021, "adapter.signal.debug");
            public static readonly EventId ObservableSignalWarning = new(2022, "adapter.signal.warning");
            public static readonly EventId ObservableSignalError = new(2023, "adapter.signal.error");
        }
    }
}
