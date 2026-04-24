using Functorium.Adapters.Abstractions.Errors.DestructuringPolicies;
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
    public Task Destructure_ReturnsExpectedJson_WhenExpectedError()
    {
        // Arrange
        Error error = ErrorFactory.CreateExpected(
            errorCode: "Domain.User.NotFound",
            errorCurrentValue: "user123",
            errorMessage: "사용자를 찾을 수 없습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenExpectedErrorT()
    {
        // Arrange
        Error error = ErrorFactory.CreateExpected(
            errorCode: "Domain.Sensor.TemperatureOutOfRange",
            errorCurrentValue: 150,
            errorMessage: "온도 범위 초과");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenExpectedErrorT1T2()
    {
        // Arrange
        Error error = ErrorFactory.CreateExpected(
            errorCode: "Domain.Range.InvalidBounds",
            errorCurrentValue1: 100,
            errorCurrentValue2: 50,
            errorMessage: "최소값이 최대값보다 큽니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenExpectedErrorT1T2T3()
    {
        // Arrange
        Error error = ErrorFactory.CreateExpected(
            errorCode: "Domain.Schedule.InvalidDate",
            errorCurrentValue1: 2025,
            errorCurrentValue2: 13,
            errorCurrentValue3: 32,
            errorMessage: "유효하지 않은 날짜입니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenExceptionalError()
    {
        // Arrange
        var exception = new InvalidOperationException("Connection failed");
        Error error = ErrorFactory.CreateExceptional(
            errorCode: "Application.Database.ConnectionFailed",
            exception: exception);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring")
            .ScrubMember("HResult")
            .ScrubMember("TargetSite")
            .ScrubMember("StackTrace")
            .ScrubMember("Source");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenExceptionalErrorWithInnerException()
    {
        // Arrange
        var innerException = new ArgumentNullException("connectionString", "Connection string cannot be null");
        var exception = new InvalidOperationException("Database connection failed", innerException);
        Error error = ErrorFactory.CreateExceptional(
            errorCode: "Application.Database.ConnectionFailedWithInner",
            exception: exception);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring")
            .ScrubMember("HResult")
            .ScrubMember("TargetSite")
            .ScrubMember("StackTrace")
            .ScrubMember("Source");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenExceptionalErrorWithNestedInnerExceptions()
    {
        // Arrange
        var rootCause = new TimeoutException("Network timeout after 30 seconds");
        var middleException = new IOException("Failed to read from socket", rootCause);
        var outerException = new InvalidOperationException("Database query execution failed", middleException);
        Error error = ErrorFactory.CreateExceptional(
            errorCode: "Application.Database.QueryExecutionFailed",
            exception: outerException);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring")
            .ScrubMember("HResult")
            .ScrubMember("TargetSite")
            .ScrubMember("StackTrace")
            .ScrubMember("Source");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenManyErrors()
    {
        // Arrange
        var error1 = ErrorFactory.CreateExpected(
            "Application.User.NameRequired", "name", "이름은 필수입니다");
        var error2 = ErrorFactory.CreateExpected(
            "Application.User.DescriptionTooLong", "description", "설명이 너무 깁니다");

        Error error = Error.Many(error1, error2);

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCurrentValueIsTuple()
    {
        // Arrange
        var tupleValue = (Id: 42, Name: "user123");
        Error error = ErrorFactory.CreateExpected(
            errorCode: "Domain.User.InvalidData",
            errorCurrentValue: tupleValue,
            errorMessage: "사용자 데이터가 유효하지 않습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCurrentValueIsRecord()
    {
        // Arrange
        var recordValue = new Point(X: 100, Y: 200);
        Error error = ErrorFactory.CreateExpected(
            errorCode: "Domain.Geometry.InvalidPoint",
            errorCurrentValue: recordValue,
            errorMessage: "좌표가 유효하지 않습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public Task Destructure_ReturnsExpectedJson_WhenErrorCurrentValuesAreMixedTypes()
    {
        // Arrange
        var point = new Point(X: 50, Y: 75);
        var range = (Min: 0, Max: 100);
        Error error = ErrorFactory.CreateExpected(
            errorCode: "Domain.Geometry.PointOutOfRange",
            errorCurrentValue1: point,
            errorCurrentValue2: range,
            errorMessage: "좌표가 범위를 벗어났습니다");

        // Act
        _sut.TryDestructure(error, _factory, out var actual);

        // Assert
        return Verify(ToAnonymousObject(actual!))
            .UseDirectory("Snapshots/ErrorDestructuring");
    }

    [Fact]
    public void TryDestructure_ReturnsTrue_WhenExpectedError()
    {
        // Arrange
        Error error = ErrorFactory.CreateExpected("Code", "value", "message");

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
