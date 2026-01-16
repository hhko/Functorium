using static Functorium.Adapters.Observabilities.Naming.ObservabilityNaming;

namespace Functorium.Adapters.Observabilities.Builders;

public partial class OpenTelemetryBuilder
{
    /// <summary>
    /// OpenTelemetry Resource Attributes를 생성합니다.
    /// Serilog와 OpenTelemetry 모두에서 공통으로 사용됩니다.
    /// </summary>
    public static Dictionary<string, object> CreateResourceAttributes(OpenTelemetryOptions options)
    {
        var attributes = new Dictionary<string, object>
        {
            [OTelAttributes.ServiceName] = options.ServiceName,
            [OTelAttributes.ServiceVersion] = options.ServiceVersion
        };

        // service.namespace: OpenTelemetry 표준 - 빈 문자열은 미지정과 동일
        if (!string.IsNullOrWhiteSpace(options.ServiceNamespace))
        {
            attributes[OTelAttributes.ServiceNamespace] = options.ServiceNamespace;
        }

        return attributes;
    }
}
