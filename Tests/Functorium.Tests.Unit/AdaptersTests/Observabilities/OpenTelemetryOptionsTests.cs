using Functorium.Adapters.Observabilities;
using static Functorium.Adapters.Observabilities.OpenTelemetryOptions;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OpenTelemetryOptionsTests
{
    #region GetTracingProtocol Tests

    [Fact]
    public void GetTracingProtocol_ReturnsTracingProtocol_WhenTracingProtocolIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            TracingProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
        };

        // Act
        var actual = sut.GetTracingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetTracingProtocol_ReturnsCollectorProtocol_WhenTracingProtocolIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            TracingProtocol = null
        };

        // Act
        var actual = sut.GetTracingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetTracingProtocol_ReturnsCollectorProtocol_WhenTracingProtocolIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            TracingProtocol = ""
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
            TracingProtocol = null
        };

        // Act
        var actual = sut.GetTracingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.Grpc);
    }

    #endregion

    #region GetMetricsProtocol Tests

    [Fact]
    public void GetMetricsProtocol_ReturnsMetricsProtocol_WhenMetricsProtocolIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            MetricsProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
        };

        // Act
        var actual = sut.GetMetricsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetMetricsProtocol_ReturnsCollectorProtocol_WhenMetricsProtocolIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            MetricsProtocol = null
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
            MetricsProtocol = null
        };

        // Act
        var actual = sut.GetMetricsProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.Grpc);
    }

    #endregion

    #region GetLoggingProtocol Tests

    [Fact]
    public void GetLoggingProtocol_ReturnsLoggingProtocol_WhenLoggingProtocolIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            LoggingProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
        };

        // Act
        var actual = sut.GetLoggingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetLoggingProtocol_ReturnsCollectorProtocol_WhenLoggingProtocolIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            LoggingProtocol = null
        };

        // Act
        var actual = sut.GetLoggingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.HttpProtobuf);
    }

    [Fact]
    public void GetLoggingProtocol_ReturnsGrpc_WhenInvalidProtocolName()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorProtocol = "InvalidProtocol",
            LoggingProtocol = null
        };

        // Act
        var actual = sut.GetLoggingProtocol();

        // Assert
        actual.ShouldBe(OtlpCollectorProtocol.Grpc);
    }

    #endregion

    #region GetTracingEndpoint Tests

    [Fact]
    public void GetTracingEndpoint_ReturnsTracingEndpoint_WhenTracingEndpointIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingEndpoint = "http://localhost:21890"
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:21890");
    }

    [Fact]
    public void GetTracingEndpoint_ReturnsCollectorEndpoint_WhenTracingEndpointIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingEndpoint = null
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:4317");
    }

    [Fact]
    public void GetTracingEndpoint_ReturnsEmptyString_WhenTracingEndpointIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingEndpoint = ""
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetTracingEndpoint_ReturnsEmptyString_WhenTracingEndpointIsWhitespace()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            TracingEndpoint = "   "
        };

        // Act
        var actual = sut.GetTracingEndpoint();

        // Assert
        actual.ShouldBe(string.Empty);
    }

    #endregion

    #region GetMetricsEndpoint Tests

    [Fact]
    public void GetMetricsEndpoint_ReturnsMetricsEndpoint_WhenMetricsEndpointIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            MetricsEndpoint = "http://localhost:21891"
        };

        // Act
        var actual = sut.GetMetricsEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:21891");
    }

    [Fact]
    public void GetMetricsEndpoint_ReturnsCollectorEndpoint_WhenMetricsEndpointIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            MetricsEndpoint = null
        };

        // Act
        var actual = sut.GetMetricsEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:4317");
    }

    [Fact]
    public void GetMetricsEndpoint_ReturnsEmptyString_WhenMetricsEndpointIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            MetricsEndpoint = ""
        };

        // Act
        var actual = sut.GetMetricsEndpoint();

        // Assert
        actual.ShouldBe(string.Empty);
    }

    #endregion

    #region GetLoggingEndpoint Tests

    [Fact]
    public void GetLoggingEndpoint_ReturnsLoggingEndpoint_WhenLoggingEndpointIsSet()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            LoggingEndpoint = "http://localhost:21892"
        };

        // Act
        var actual = sut.GetLoggingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:21892");
    }

    [Fact]
    public void GetLoggingEndpoint_ReturnsCollectorEndpoint_WhenLoggingEndpointIsNull()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            LoggingEndpoint = null
        };

        // Act
        var actual = sut.GetLoggingEndpoint();

        // Assert
        actual.ShouldBe("http://localhost:4317");
    }

    [Fact]
    public void GetLoggingEndpoint_ReturnsEmptyString_WhenLoggingEndpointIsEmpty()
    {
        // Arrange
        var sut = new OpenTelemetryOptions
        {
            CollectorEndpoint = "http://localhost:4317",
            LoggingEndpoint = ""
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
        sut.TracingEndpoint.ShouldBeNull();
        sut.MetricsEndpoint.ShouldBeNull();
        sut.LoggingEndpoint.ShouldBeNull();
        sut.TracingProtocol.ShouldBeNull();
        sut.MetricsProtocol.ShouldBeNull();
        sut.LoggingProtocol.ShouldBeNull();
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
