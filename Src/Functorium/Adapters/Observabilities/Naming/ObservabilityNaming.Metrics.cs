namespace Functorium.Adapters.Observabilities.Naming;

public static partial class ObservabilityNaming
{
    /// <summary>
    /// 메트릭 이름 생성 유틸리티
    /// functorium.* 네임스페이스 사용
    /// </summary>
    public static class Metrics
    {
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

        public static string AdapterResponseSuccess(string category) =>
            $"adapter.{category}.responses.success";

        public static string AdapterResponseFailure(string category) =>
            $"adapter.{category}.responses.failure";

        public static string AdapterDuration(string category) =>
            $"adapter.{category}.duration";

        // 범용 메트릭 (레이어별)
        public static string Requests(string layer, string category) =>
            $"{layer}.{category}.requests";

        public static string ResponseSuccess(string layer, string category) =>
            $"{layer}.{category}.responses.success";

        public static string ResponseFailure(string layer, string category) =>
            $"{layer}.{category}.responses.failure";

        public static string Duration(string layer, string category) =>
            $"{layer}.{category}.duration";
    }
}
