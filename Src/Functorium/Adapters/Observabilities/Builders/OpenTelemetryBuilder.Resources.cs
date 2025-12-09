namespace Functorium.Adapters.Observabilities.Builders;

public partial class OpenTelemetryBuilder
{
    /// <summary>
    /// OpenTelemetry Resource Attributes를 생성합니다.
    /// Serilog와 OpenTelemetry 모두에서 공통으로 사용됩니다.
    /// </summary>
    public static Dictionary<string, object> CreateResourceAttributes(OpenTelemetryOptions options)
    {
        return new Dictionary<string, object>
        {
            ["service.name"] = options.ServiceName,
            ["service.version"] = options.ServiceVersion
        };
    }
}
