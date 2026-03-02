namespace Functorium.Adapters.Observabilities;

public interface IOpenTelemetryOptions
{
    string ServiceNamespace { get; }

    bool EnablePrometheusExporter { get; }
}
