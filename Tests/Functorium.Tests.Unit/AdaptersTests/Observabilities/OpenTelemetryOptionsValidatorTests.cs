using FluentValidation.Results;
using Functorium.Adapters.Observabilities;
using static Functorium.Adapters.Observabilities.OpenTelemetryOptions;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OpenTelemetryOptionsValidatorTests
{
    private readonly Validator _sut = new();

    #region ServiceName Validation Tests

    [Fact]
    public void Validate_ReturnsValidationError_WhenServiceNameIsEmpty()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ServiceName = string.Empty;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.ServiceName));
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenServiceNameIsWhitespace()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ServiceName = "   ";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.ServiceName));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenServiceNameIsValid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ServiceName = "MyService";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.Errors.ShouldNotContain(e => e.PropertyName == nameof(OpenTelemetryOptions.ServiceName));
    }

    #endregion

    #region ServiceNamespace Validation Tests

    [Fact]
    public void Validate_ReturnsValidationError_WhenServiceNamespaceIsEmpty()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ServiceNamespace = string.Empty;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.ServiceNamespace));
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenServiceNamespaceIsWhitespace()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ServiceNamespace = "   ";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.ServiceNamespace));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenServiceNamespaceIsValid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ServiceNamespace = "MyCompany.Production";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.Errors.ShouldNotContain(e => e.PropertyName == nameof(OpenTelemetryOptions.ServiceNamespace));
    }

    #endregion

    #region Endpoint Validation Tests

    [Fact]
    public void Validate_ReturnsValidationError_WhenNoEndpointsConfigured()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "MyService",
            ServiceNamespace = "Test.Namespace",
            CollectorEndpoint = string.Empty,
            TracingEndpoint = null,
            MetricsEndpoint = null,
            LoggingEndpoint = null
        };

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.ErrorMessage.Contains("endpoint"));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenCollectorEndpointIsSet()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenOnlyTracingEndpointIsSet()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "MyService",
            ServiceNamespace = "Test.Namespace",
            CollectorEndpoint = string.Empty,
            TracingEndpoint = "http://localhost:21890"
        };

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenOnlyMetricsEndpointIsSet()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "MyService",
            ServiceNamespace = "Test.Namespace",
            CollectorEndpoint = string.Empty,
            MetricsEndpoint = "http://localhost:21891"
        };

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenOnlyLoggingEndpointIsSet()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "MyService",
            ServiceNamespace = "Test.Namespace",
            CollectorEndpoint = string.Empty,
            LoggingEndpoint = "http://localhost:21892"
        };

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    #endregion

    #region SamplingRate Validation Tests

    [Fact]
    public void Validate_ReturnsValidationError_WhenSamplingRateIsNegative()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SamplingRate = -0.1;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.SamplingRate));
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenSamplingRateExceedsOne()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SamplingRate = 1.1;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.SamplingRate));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_ReturnsNoError_WhenSamplingRateIsWithinRange(double samplingRate)
    {
        // Arrange
        var options = CreateValidOptions();
        options.SamplingRate = samplingRate;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Protocol Validation Tests

    [Fact]
    public void Validate_ReturnsValidationError_WhenCollectorProtocolIsInvalid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.CollectorProtocol = "InvalidProtocol";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.CollectorProtocol));
    }

    [Theory]
    [InlineData("Grpc")]
    [InlineData("HttpProtobuf")]
    public void Validate_ReturnsNoError_WhenCollectorProtocolIsValid(string protocol)
    {
        // Arrange
        var options = CreateValidOptions();
        options.CollectorProtocol = protocol;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenTracingProtocolIsInvalid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.TracingProtocol = "InvalidProtocol";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.TracingProtocol));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenTracingProtocolIsNull()
    {
        // Arrange
        var options = CreateValidOptions();
        options.TracingProtocol = null;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenMetricsProtocolIsInvalid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MetricsProtocol = "InvalidProtocol";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.MetricsProtocol));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenMetricsProtocolIsNull()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MetricsProtocol = null;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenLoggingProtocolIsInvalid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.LoggingProtocol = "InvalidProtocol";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.LoggingProtocol));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenLoggingProtocolIsNull()
    {
        // Arrange
        var options = CreateValidOptions();
        options.LoggingProtocol = null;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Full Validation Tests

    [Fact]
    public void Validate_ReturnsNoError_WhenAllOptionsAreValid()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "MyService",
            ServiceNamespace = "MyCompany.Production",
            CollectorEndpoint = "http://localhost:4317",
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            SamplingRate = 0.5,
            EnablePrometheusExporter = true,
            TracingEndpoint = "http://localhost:21890",
            MetricsEndpoint = "http://localhost:21891",
            LoggingEndpoint = "http://localhost:21892",
            TracingProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            MetricsProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            LoggingProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
        };

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private static OpenTelemetryOptions CreateValidOptions() => new()
    {
        ServiceName = "TestService",
        ServiceNamespace = "Test.Namespace",
        CollectorEndpoint = "http://localhost:4317",
        CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
        SamplingRate = 1.0
    };

    #endregion
}
