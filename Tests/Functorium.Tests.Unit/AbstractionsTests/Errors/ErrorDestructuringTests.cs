using Functorium.Abstractions.Errors.DestructuringPolicies;
using Serilog.Core;
using Serilog.Events;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;
using static Functorium.Testing.Assertions.Logging.LogEventPropertyValueConverter;
using Functorium.Testing.Assertions.Logging;

namespace Functorium.Tests.Unit.AbstractionsTests.Errors;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class ErrorDestructuringTests
{
    private readonly ErrorsDestructuringPolicy _sut = new();
    private readonly SerilogTestPropertyValueFactory _factory = new();

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCodeExpected()
    {
        // Arrange
        Error error = ErrorCodeFactory.Create(
            errorCode: "DomainErrors.User.NotFound",
            errorCurrentValue: "user123",
            errorMessage: "사용자를 찾을 수 없습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCodeExpectedT()
    {
        // Arrange
        Error error = ErrorCodeFactory.Create(
            errorCode: "DomainErrors.Sensor.TemperatureOutOfRange",
            errorCurrentValue: 150,
            errorMessage: "온도 범위 초과");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCodeExpectedT1T2()
    {
        // Arrange
        Error error = ErrorCodeFactory.Create(
            errorCode: "DomainErrors.Range.InvalidBounds",
            errorCurrentValue1: 100,
            errorCurrentValue2: 50,
            errorMessage: "최소값이 최대값보다 큽니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCodeExpectedT1T2T3()
    {
        // Arrange
        Error error = ErrorCodeFactory.Create(
            errorCode: "DomainErrors.Schedule.InvalidDate",
            errorCurrentValue1: 2025,
            errorCurrentValue2: 13,
            errorCurrentValue3: 32,
            errorMessage: "유효하지 않은 날짜입니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCodeExceptional()
    {
        // Arrange
        var exception = new InvalidOperationException("Connection failed");
        Error error = ErrorCodeFactory.CreateFromException(
            errorCode: "ApplicationErrors.Database.ConnectionFailed",
            exception: exception);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .ScrubMember("HResult")
            .ScrubMember("TargetSite")
            .ScrubMember("StackTrace")
            .ScrubMember("Source");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCodeExceptionalWithInnerException()
    {
        // Arrange
        var innerException = new ArgumentNullException("connectionString", "Connection string cannot be null");
        var exception = new InvalidOperationException("Database connection failed", innerException);
        Error error = ErrorCodeFactory.CreateFromException(
            errorCode: "ApplicationErrors.Database.ConnectionFailedWithInner",
            exception: exception);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .ScrubMember("HResult")
            .ScrubMember("TargetSite")
            .ScrubMember("StackTrace")
            .ScrubMember("Source");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCodeExceptionalWithNestedInnerExceptions()
    {
        // Arrange
        var rootCause = new TimeoutException("Network timeout after 30 seconds");
        var middleException = new IOException("Failed to read from socket", rootCause);
        var outerException = new InvalidOperationException("Database query execution failed", middleException);
        Error error = ErrorCodeFactory.CreateFromException(
            errorCode: "ApplicationErrors.Database.QueryExecutionFailed",
            exception: outerException);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .ScrubMember("HResult")
            .ScrubMember("TargetSite")
            .ScrubMember("StackTrace")
            .ScrubMember("Source");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenManyErrors()
    {
        // Arrange
        var error1 = ErrorCodeFactory.Create(
            "ApplicationErrors.User.NameRequired", "name", "이름은 필수입니다");
        var error2 = ErrorCodeFactory.Create(
            "ApplicationErrors.User.DescriptionTooLong", "description", "설명이 너무 깁니다");

        Error error = Error.Many(error1, error2);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCurrentValueIsTuple()
    {
        // Arrange
        var tupleValue = (Id: 42, Name: "user123");
        Error error = ErrorCodeFactory.Create(
            errorCode: "DomainErrors.User.InvalidData",
            errorCurrentValue: tupleValue,
            errorMessage: "사용자 데이터가 유효하지 않습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCurrentValueIsRecord()
    {
        // Arrange
        var recordValue = new Point(X: 100, Y: 200);
        Error error = ErrorCodeFactory.Create(
            errorCode: "DomainErrors.Geometry.InvalidPoint",
            errorCurrentValue: recordValue,
            errorMessage: "좌표가 유효하지 않습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCurrentValuesAreMixedTypes()
    {
        // Arrange
        var point = new Point(X: 50, Y: 75);
        var range = (Min: 0, Max: 100);
        Error error = ErrorCodeFactory.Create(
            errorCode: "DomainErrors.Geometry.PointOutOfRange",
            errorCurrentValue1: point,
            errorCurrentValue2: range,
            errorMessage: "좌표가 범위를 벗어났습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!));
    }

    [Fact]
    public void TryDestructure_ReturnsTrue_WhenErrorCodeExpected()
    {
        // Arrange
        Error error = ErrorCodeFactory.Create("Code", "value", "message");

        // Act
        var actual = _sut.TryDestructure(error, _factory, out _);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void TryDestructure_ReturnsFalse_WhenNotError()
    {
        // Arrange
        var notError = "not an error";

        // Act
        var actual = _sut.TryDestructure(notError, _factory, out var result);

        // Assert
        actual.ShouldBeFalse();
        result.ShouldBeNull();
    }
}

public record Point(int X, int Y);
