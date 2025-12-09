namespace Functorium.Adapters.Observabilities;

public interface IOpenTelemetryOptions
{
    // //string ServiceName { get; }
    // //string ServiceVersion { get; }
    // string ServiceNamespace { get; }

    bool EnablePrometheusExporter { get; }
}
