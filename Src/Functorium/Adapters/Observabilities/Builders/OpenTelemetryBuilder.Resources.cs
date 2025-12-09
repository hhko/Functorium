namespace Functorium.Adapters.Observabilities.Builders;

public partial class OpenTelemetryBuilder
{
    public static Dictionary<string, object> CreateResourceAttributes(OpenTelemetryOptions options)
    {
        return new Dictionary<string, object>
        {
            ["service.name"] = options.ServiceName,
            ["service.version"] = options.ServiceVersion
        };
    }

    public static Dictionary<string, object> CreateCommonResourceAttributes(OpenTelemetryOptions options)
    {
        return new Dictionary<string, object>
        {
            ["service.name"] = options.ServiceName,
            ["service.version"] = options.ServiceVersion
        };
    }
}
