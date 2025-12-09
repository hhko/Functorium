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

    #region Endpoint Validation Tests

    [Fact]
    public void Validate_ReturnsValidationError_WhenNoEndpointsConfigured()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "MyService",
            CollectorEndpoint = string.Empty,
            TracingCollectorEndpoint = null,
            MetricsCollectorEndpoint = null,
            LoggingCollectorEndpoint = null
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
            CollectorEndpoint = string.Empty,
            TracingCollectorEndpoint = "http://localhost:21890"
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
            CollectorEndpoint = string.Empty,
            MetricsCollectorEndpoint = "http://localhost:21891"
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
            CollectorEndpoint = string.Empty,
            LoggingCollectorEndpoint = "http://localhost:21892"
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
    public void Validate_ReturnsValidationError_WhenTracingCollectorProtocolIsInvalid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.TracingCollectorProtocol = "InvalidProtocol";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.TracingCollectorProtocol));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenTracingCollectorProtocolIsNull()
    {
        // Arrange
        var options = CreateValidOptions();
        options.TracingCollectorProtocol = null;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenMetricsCollectorProtocolIsInvalid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MetricsCollectorProtocol = "InvalidProtocol";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.MetricsCollectorProtocol));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenMetricsCollectorProtocolIsNull()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MetricsCollectorProtocol = null;

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenLoggingCollectorProtocolIsInvalid()
    {
        // Arrange
        var options = CreateValidOptions();
        options.LoggingCollectorProtocol = "InvalidProtocol";

        // Act
        ValidationResult actual = _sut.Validate(options);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == nameof(OpenTelemetryOptions.LoggingCollectorProtocol));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenLoggingCollectorProtocolIsNull()
    {
        // Arrange
        var options = CreateValidOptions();
        options.LoggingCollectorProtocol = null;

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
            CollectorEndpoint = "http://localhost:4317",
            CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
            SamplingRate = 0.5,
            EnablePrometheusExporter = true,
            TracingCollectorEndpoint = "http://localhost:21890",
            MetricsCollectorEndpoint = "http://localhost:21891",
            LoggingCollectorEndpoint = "http://localhost:21892",
            TracingCollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            MetricsCollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name,
            LoggingCollectorProtocol = OtlpCollectorProtocol.HttpProtobuf.Name
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
        CollectorEndpoint = "http://localhost:4317",
        CollectorProtocol = OtlpCollectorProtocol.Grpc.Name,
        SamplingRate = 1.0
    };

    #endregion
}
