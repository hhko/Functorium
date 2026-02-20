using Functorium.Adapters.Errors;
using Functorium.Testing.Assertions.Errors;
using LanguageExt;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.TestingTests.Assertions;

// 테스트용 더미 어댑터
public sealed class DummyAdapter { }

[Trait(nameof(UnitTest), UnitTest.Functorium_Testing)]
public class AdapterErrorAssertionsTests
{
    private sealed record RateLimited : AdapterErrorType.Custom;
    private sealed record ServiceError : AdapterErrorType.Custom;

    #region Error - ShouldBeAdapterError<TAdapter>

    [Fact]
    public void ShouldBeAdapterError_ReturnsSuccess_WhenErrorMatchesExpectedType()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation("PropertyName"),
            currentValue: "invalid-value",
            message: "Validation failed");

        // Act & Assert (should not throw)
        error.ShouldBeAdapterError<DummyAdapter>(new AdapterErrorType.PipelineValidation("PropertyName"));
    }

    [Fact]
    public void ShouldBeAdapterError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeAdapterError<DummyAdapter>(new AdapterErrorType.PipelineException()));
    }

    [Fact]
    public void ShouldBeAdapterError_ReturnsSuccess_WhenCustomErrorMatches()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new RateLimited(),
            currentValue: "https://api.example.com",
            message: "Rate limit exceeded");

        // Act & Assert (should not throw)
        error.ShouldBeAdapterError<DummyAdapter>(new RateLimited());
    }

    [Fact]
    public void ShouldBeAdapterError_ReturnsSuccess_WhenTimeoutMatches()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.Timeout(TimeSpan.FromSeconds(30)),
            currentValue: "https://api.example.com",
            message: "Request timed out");

        // Act & Assert (should not throw)
        error.ShouldBeAdapterError<DummyAdapter>(new AdapterErrorType.Timeout(TimeSpan.FromSeconds(30)));
    }

    #endregion

    #region Error - ShouldBeAdapterError<TAdapter, TValue>

    [Fact]
    public void ShouldBeAdapterError_WithValue_ReturnsSuccess_WhenErrorAndValueMatch()
    {
        // Arrange
        var url = "https://api.example.com/resource";
        var error = AdapterError.For<DummyAdapter, string>(
            new AdapterErrorType.ConnectionFailed("API"),
            currentValue: url,
            message: "Connection failed");

        // Act & Assert (should not throw)
        error.ShouldBeAdapterError<DummyAdapter, string>(
            new AdapterErrorType.ConnectionFailed("API"),
            expectedCurrentValue: url);
    }

    [Fact]
    public void ShouldBeAdapterError_WithValue_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter, string>(
            new AdapterErrorType.ConnectionFailed(),
            currentValue: "actual-url",
            message: "Connection failed");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeAdapterError<DummyAdapter, string>(
                new AdapterErrorType.ConnectionFailed(),
                expectedCurrentValue: "expected-url"));
    }

    #endregion

    #region Error - ShouldBeAdapterError<TAdapter, T1, T2>

    [Fact]
    public void ShouldBeAdapterError_WithTwoValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var serviceName = "PaymentService";
        var endpoint = "/api/payments";
        var error = AdapterError.For<DummyAdapter, string, string>(
            new AdapterErrorType.ExternalServiceUnavailable(serviceName),
            serviceName,
            endpoint,
            message: "External service unavailable");

        // Act & Assert (should not throw)
        error.ShouldBeAdapterError<DummyAdapter, string, string>(
            new AdapterErrorType.ExternalServiceUnavailable(serviceName),
            expectedValue1: serviceName,
            expectedValue2: endpoint);
    }

    [Fact]
    public void ShouldBeAdapterError_WithTwoValues_ThrowsException_WhenValue1DoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter, string, string>(
            new AdapterErrorType.ExternalServiceUnavailable(),
            "actual-service",
            "/endpoint",
            message: "External service unavailable");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeAdapterError<DummyAdapter, string, string>(
                new AdapterErrorType.ExternalServiceUnavailable(),
                expectedValue1: "expected-service",
                expectedValue2: "/endpoint"));
    }

    #endregion

    #region Error - ShouldBeAdapterError<TAdapter, T1, T2, T3>

    [Fact]
    public void ShouldBeAdapterError_WithThreeValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var serviceName = "PaymentService";
        var endpoint = "/api/payments";
        var statusCode = 503;
        var error = AdapterError.For<DummyAdapter, string, string, int>(
            new ServiceError(),
            serviceName,
            endpoint,
            statusCode,
            message: "Service returned error");

        // Act & Assert (should not throw)
        error.ShouldBeAdapterError<DummyAdapter, string, string, int>(
            new ServiceError(),
            expectedValue1: serviceName,
            expectedValue2: endpoint,
            expectedValue3: statusCode);
    }

    [Fact]
    public void ShouldBeAdapterError_WithThreeValues_ThrowsException_WhenValue3DoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter, string, string, int>(
            new ServiceError(),
            "service",
            "/endpoint",
            503,
            message: "Service returned error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeAdapterError<DummyAdapter, string, string, int>(
                new ServiceError(),
                expectedValue1: "service",
                expectedValue2: "/endpoint",
                expectedValue3: 500)); // Wrong value
    }

    #endregion

    #region Error - ShouldBeAdapterExceptionalError<TAdapter>

    [Fact]
    public void ShouldBeAdapterExceptionalError_ReturnsSuccess_WhenErrorMatchesExpectedType()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = AdapterError.FromException<DummyAdapter>(
            new AdapterErrorType.PipelineException(),
            exception);

        // Act & Assert (should not throw)
        error.ShouldBeAdapterExceptionalError<DummyAdapter>(new AdapterErrorType.PipelineException());
    }

    [Fact]
    public void ShouldBeAdapterExceptionalError_WithExceptionType_ReturnsSuccess_WhenExceptionTypeMatches()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = AdapterError.FromException<DummyAdapter>(
            new AdapterErrorType.PipelineException(),
            exception);

        // Act & Assert (should not throw)
        error.ShouldBeAdapterExceptionalError<DummyAdapter, InvalidOperationException>(
            new AdapterErrorType.PipelineException());
    }

    [Fact]
    public void ShouldBeAdapterExceptionalError_WithExceptionType_ThrowsException_WhenExceptionTypeMismatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = AdapterError.FromException<DummyAdapter>(
            new AdapterErrorType.PipelineException(),
            exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeAdapterExceptionalError<DummyAdapter, ArgumentException>(
                new AdapterErrorType.PipelineException()));
    }

    #endregion

    #region Fin<T> - ShouldBeAdapterError<TAdapter, T>

    [Fact]
    public void Fin_ShouldBeAdapterError_ReturnsSuccess_WhenFinFailsWithExpectedError()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");
        Fin<string> fin = error;

        // Act & Assert (should not throw)
        fin.ShouldBeAdapterError<DummyAdapter, string>(new AdapterErrorType.PipelineValidation());
    }

    [Fact]
    public void Fin_ShouldBeAdapterError_ThrowsException_WhenFinSucceeds()
    {
        // Arrange
        Fin<string> fin = "success";

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldBeAdapterError<DummyAdapter, string>(new AdapterErrorType.PipelineValidation()));
    }

    [Fact]
    public void Fin_ShouldBeAdapterError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");
        Fin<string> fin = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldBeAdapterError<DummyAdapter, string>(new AdapterErrorType.PipelineException()));
    }

    [Fact]
    public void Fin_ShouldBeAdapterError_WithValue_ReturnsSuccess_WhenFinFailsWithExpectedErrorAndValue()
    {
        // Arrange
        var url = "https://api.example.com";
        var error = AdapterError.For<DummyAdapter, string>(
            new AdapterErrorType.Timeout(),
            currentValue: url,
            message: "Request timed out");
        Fin<string> fin = error;

        // Act & Assert (should not throw)
        fin.ShouldBeAdapterError<DummyAdapter, string, string>(
            new AdapterErrorType.Timeout(),
            expectedCurrentValue: url);
    }

    [Fact]
    public void Fin_ShouldBeAdapterExceptionalError_ReturnsSuccess_WhenFinFailsWithExpectedException()
    {
        // Arrange
        var exception = new TimeoutException("Request timed out");
        var error = AdapterError.FromException<DummyAdapter>(
            new AdapterErrorType.PipelineException(),
            exception);
        Fin<string> fin = error;

        // Act & Assert (should not throw)
        fin.ShouldBeAdapterExceptionalError<DummyAdapter, string>(new AdapterErrorType.PipelineException());
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveAdapterError<TAdapter, T>

    [Fact]
    public void Validation_ShouldHaveAdapterError_ReturnsSuccess_WhenValidationFailsWithExpectedError()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveAdapterError<DummyAdapter, string>(new AdapterErrorType.PipelineValidation());
    }

    [Fact]
    public void Validation_ShouldHaveAdapterError_ThrowsException_WhenValidationSucceeds()
    {
        // Arrange
        Validation<Error, string> validation = "success";

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveAdapterError<DummyAdapter, string>(new AdapterErrorType.PipelineValidation()));
    }

    [Fact]
    public void Validation_ShouldHaveAdapterError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveAdapterError<DummyAdapter, string>(new AdapterErrorType.PipelineException()));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveOnlyAdapterError<TAdapter, T>

    [Fact]
    public void Validation_ShouldHaveOnlyAdapterError_ReturnsSuccess_WhenExactlyOneErrorMatches()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.Serialization("JSON"),
            currentValue: "invalid-json",
            message: "Serialization failed");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveOnlyAdapterError<DummyAdapter, string>(new AdapterErrorType.Serialization("JSON"));
    }

    [Fact]
    public void Validation_ShouldHaveOnlyAdapterError_ThrowsException_WhenMultipleErrors()
    {
        // Arrange - Use Apply pattern to combine errors
        var error1 = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value1",
            message: "Validation failed 1");
        var error2 = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.Timeout(),
            currentValue: "value2",
            message: "Timeout");

        Validation<Error, string> validation1 = error1;
        Validation<Error, string> validation2 = error2;

        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            combined.ShouldHaveOnlyAdapterError<DummyAdapter, string>(new AdapterErrorType.PipelineValidation()));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveAdapterErrors<TAdapter, T>

    [Fact]
    public void Validation_ShouldHaveAdapterErrors_ReturnsSuccess_WhenAllExpectedErrorsPresent()
    {
        // Arrange - Use Apply pattern to combine errors
        var error1 = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value1",
            message: "Validation failed");
        var error2 = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.Timeout(),
            currentValue: "value2",
            message: "Timeout");

        Validation<Error, string> validation1 = error1;
        Validation<Error, string> validation2 = error2;

        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert (should not throw)
        combined.ShouldHaveAdapterErrors<DummyAdapter, string>(
            new AdapterErrorType.PipelineValidation(),
            new AdapterErrorType.Timeout());
    }

    [Fact]
    public void Validation_ShouldHaveAdapterErrors_ThrowsException_WhenExpectedErrorMissing()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveAdapterErrors<DummyAdapter, string>(
                new AdapterErrorType.PipelineValidation(),
                new AdapterErrorType.Timeout())); // Timeout not present
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveAdapterError<TAdapter, T, TValue>

    [Fact]
    public void Validation_ShouldHaveAdapterError_WithValue_ReturnsSuccess_WhenErrorAndValueMatch()
    {
        // Arrange
        var url = "https://api.example.com";
        var error = AdapterError.For<DummyAdapter, string>(
            new AdapterErrorType.ConnectionFailed(),
            currentValue: url,
            message: "Connection failed");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveAdapterError<DummyAdapter, string, string>(
            new AdapterErrorType.ConnectionFailed(),
            expectedCurrentValue: url);
    }

    [Fact]
    public void Validation_ShouldHaveAdapterError_WithValue_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter, string>(
            new AdapterErrorType.ConnectionFailed(),
            currentValue: "actual-url",
            message: "Connection failed");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveAdapterError<DummyAdapter, string, string>(
                new AdapterErrorType.ConnectionFailed(),
                expectedCurrentValue: "expected-url"));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveAdapterError<TAdapter, T, T1, T2>

    [Fact]
    public void Validation_ShouldHaveAdapterError_WithTwoValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var serviceName = "PaymentService";
        var endpoint = "/api/payments";
        var error = AdapterError.For<DummyAdapter, string, string>(
            new AdapterErrorType.ExternalServiceUnavailable(),
            serviceName,
            endpoint,
            message: "External service unavailable");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveAdapterError<DummyAdapter, string, string, string>(
            new AdapterErrorType.ExternalServiceUnavailable(),
            expectedValue1: serviceName,
            expectedValue2: endpoint);
    }

    [Fact]
    public void Validation_ShouldHaveAdapterError_WithTwoValues_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter, string, string>(
            new AdapterErrorType.ExternalServiceUnavailable(),
            "actual-service",
            "/endpoint",
            message: "External service unavailable");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveAdapterError<DummyAdapter, string, string, string>(
                new AdapterErrorType.ExternalServiceUnavailable(),
                expectedValue1: "expected-service",
                expectedValue2: "/endpoint"));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveAdapterError<TAdapter, T, T1, T2, T3>

    [Fact]
    public void Validation_ShouldHaveAdapterError_WithThreeValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var serviceName = "PaymentService";
        var endpoint = "/api/payments";
        var statusCode = 503;
        var error = AdapterError.For<DummyAdapter, string, string, int>(
            new ServiceError(),
            serviceName,
            endpoint,
            statusCode,
            message: "Service returned error");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveAdapterError<DummyAdapter, string, string, string, int>(
            new ServiceError(),
            expectedValue1: serviceName,
            expectedValue2: endpoint,
            expectedValue3: statusCode);
    }

    [Fact]
    public void Validation_ShouldHaveAdapterError_WithThreeValues_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = AdapterError.For<DummyAdapter, string, string, int>(
            new ServiceError(),
            "service",
            "/endpoint",
            503,
            message: "Service returned error");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveAdapterError<DummyAdapter, string, string, string, int>(
                new ServiceError(),
                expectedValue1: "service",
                expectedValue2: "/endpoint",
                expectedValue3: 500)); // Wrong value
    }

    #endregion

    #region Type Safety - Different Adapters

    public sealed class ValidationPipeline { }

    public sealed class ExceptionPipeline { }

    [Fact]
    public void ShouldBeAdapterError_ThrowsException_WhenAdapterTypeMismatch()
    {
        // Arrange - Error is for ValidationPipeline
        var error = AdapterError.For<ValidationPipeline>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");

        // Act & Assert - Checking for ExceptionPipeline should fail
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeAdapterError<ExceptionPipeline>(new AdapterErrorType.PipelineValidation()));
    }

    [Fact]
    public void ShouldBeAdapterError_ReturnsSuccess_WhenAdapterTypeMatches()
    {
        // Arrange
        var error = AdapterError.For<ValidationPipeline>(
            new AdapterErrorType.PipelineValidation(),
            currentValue: "value",
            message: "Validation failed");

        // Act & Assert (should not throw)
        error.ShouldBeAdapterError<ValidationPipeline>(new AdapterErrorType.PipelineValidation());
    }

    #endregion
}
