using OpenTelemetry.Exporter;
using Serilog.Sinks.OpenTelemetry;

using static Functorium.Adapters.Observabilities.OpenTelemetryOptions;

namespace Functorium.Adapters.Observabilities.Builders;

public partial class OpenTelemetryBuilder
{
    public static OtlpProtocol ToOtlpProtocolForSerilog(OtlpCollectorProtocol protocol)
    {
        return protocol == OtlpCollectorProtocol.HttpProtobuf
            ? OtlpProtocol.HttpProtobuf
            : OtlpProtocol.Grpc;
    }

    public static OtlpExportProtocol ToOtlpProtocolForExporter(OtlpCollectorProtocol protocol)
    {
        return protocol == OtlpCollectorProtocol.HttpProtobuf
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
    }
}
