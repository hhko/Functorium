using Functorium.Adapters.Observabilities;
using static Functorium.Adapters.Observabilities.OpenTelemetryOptions;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OpenTelemetryOptionsTests
{
    #region GetTracingProtocol Tests

    [Fact]
    public void GetTracingProtocol_ReturnsTracingProtocol_WhenTracingCollectorProtocolIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            TracingCollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
        };

        // Act
        var actual = sut.GetTracingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetTracingProtocol_ReturnsCollectorProtocol_WhenTracingCollectorProtocolIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            TracingCollectorProtocol = null
        };

        // Act
        var actual = sut.GetTracingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetTracingProtocol_ReturnsCollectorProtocol_WhenTracingCollectorProtocolIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            TracingCollectorProtocol = ""
        };

        // Act
        var actual = sut.GetTracingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetTracingProtocol_ReturnsGrpc_WhenInvalidProtocolName()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = "InvalidProtocol",
            TracingCollectorProtocol = null
        };

        // Act
        var actual = sut.GetTracingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.Grpc);
    }

    #endregion

    #region GetMetricsProtocol Tests

    [Fact]
    public void GetMetricsProtocol_ReturnsMetricsProtocol_WhenMetricsCollectorProtocolIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            MetricsCollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
        };

        // Act
        var actual = sut.GetMetricsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetMetricsProtocol_ReturnsCollectorProtocol_WhenMetricsCollectorProtocolIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            MetricsCollectorProtocol = null
        };

        // Act
        var actual = sut.GetMetricsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetMetricsProtocol_ReturnsGrpc_WhenInvalidProtocolName()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = "InvalidProtocol",
            MetricsCollectorProtocol = null
        };

        // Act
        var actual = sut.GetMetricsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.Grpc);
    }

    #endregion

    #region GetLogsProtocol Tests

    [Fact]
    public void GetLogsProtocol_ReturnsLoggingProtocol_WhenLoggingCollectorProtocolIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            LoggingCollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
        };

        // Act
        var actual = sut.GetLogsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetLogsProtocol_ReturnsCollectorProtocol_WhenLoggingCollectorProtocolIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            LoggingCollectorProtocol = null
        };

        // Act
        var actual = sut.GetLogsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetLogsProtocol_ReturnsGrpc_WhenInvalidProtocolName()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = "InvalidProtocol",
            LoggingCollectorProtocol = null
        };

        // Act
        var actual = sut.GetLogsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.Grpc);
    }

    #endregion

    #region GetTracingEndpoint Tests

    [Fact]
    public void GetTracingEndpoint_ReturnsTracingEndpoint_WhenTracingCollectorEndpointIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingCollectorEndpoint = "http://localhost:21890"
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:21890");
    }

    [Fact]
    public void GetTracingEndpoint_ReturnsCollectorEndpoint_WhenTracingCollectorEndpointIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingCollectorEndpoint = null
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:4317");
    }

    [Fact]
    public void GetTracingEndpoint_ReturnsEmptyString_WhenTracingCollectorEndpointIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingCollectorEndpoint = ""
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetTracingEndpoint_ReturnsEmptyString_WhenTracingCollectorEndpointIsWhitespace()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingCollectorEndpoint = "   "
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe(string.Empty);
    }

    #endregion

    #region GetMetricsEndpoint Tests

    [Fact]
    public void GetMetricsEndpoint_ReturnsMetricsEndpoint_WhenMetricsCollectorEndpointIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            MetricsCollectorEndpoint = "http://localhost:21891"
        };

        // Act
        var actual = sut.GetMetricsEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:21891");
    }

    [Fact]
    public void GetMetricsEndpoint_ReturnsCollectorEndpoint_WhenMetricsCollectorEndpointIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            MetricsCollectorEndpoint = null
        };

        // Act
        var actual = sut.GetMetricsEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:4317");
    }

    [Fact]
    public void GetMetricsEndpoint_ReturnsEmptyString_WhenMetricsCollectorEndpointIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            MetricsCollectorEndpoint = ""
        };

        // Act
        var actual = sut.GetMetricsEndpoint();

        // Assert
        actual.ShouldBe(string.Empty);
    }

    #endregion

    #region GetLoggingEndpoint Tests

    [Fact]
    public void GetLoggingEndpoint_ReturnsLoggingEndpoint_WhenLoggingCollectorEndpointIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            LoggingCollectorEndpoint = "http://localhost:21892"
        };

        // Act
        var actual = sut.GetLoggingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:21892");
    }

    [Fact]
    public void GetLoggingEndpoint_ReturnsCollectorEndpoint_WhenLoggingCollectorEndpointIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            LoggingCollectorEndpoint = null
        };

        // Act
        var actual = sut.GetLoggingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:4317");
    }

    [Fact]
    public void GetLoggingEndpoint_ReturnsEmptyString_WhenLoggingCollectorEndpointIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            LoggingCollectorEndpoint = ""
        };

        // Act
        var actual = sut.GetLoggingEndpoint();

        // Assert
        actual.ShouldBe(string.Empty);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void Constructor_SetsDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var sut = new OpenTelemetryOptions();

        // Assert
        sut.ServiceName.ShouldBe(string.Empty);
        sut.CollectorEndpoint.ShouldBe(string.Empty);
        sut.CollectorProtocol.ShouldBe(OtlpCollectorProtocol.Grpc.Name);
        sut.SamplingRate.ShouldBe(1.0);
        sut.EnablePrometheusExporter.ShouldBeFalse();
        sut.TracingCollectorEndpoint.ShouldBeNull();
        sut.MetricsCollectorEndpoint.ShouldBeNull();
        sut.LoggingCollectorEndpoint.ShouldBeNull();
        sut.TracingCollectorProtocol.ShouldBeNull();
        sut.MetricsCollectorProtocol.ShouldBeNull();
        sut.LoggingCollectorProtocol.ShouldBeNull();
    }

    [Fact]
    public void ServiceVersion_ReturnsAssemblyVersion_WhenCreated()
    {
        // Arrange & Act
        var sut = new OpenTelemetryOptions();

        // Assert
        sut.ServiceVersion.ShouldNotBeNullOrEmpty();
    }

    #endregion
}
