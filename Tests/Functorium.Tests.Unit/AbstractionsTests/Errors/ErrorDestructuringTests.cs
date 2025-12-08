using Functorium.Abstractions.Errors.DestructuringPolicies;
using Serilog.Core;
using Serilog.Events;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AbstractionsTests.Errors;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class ErrorDestructuringTests
{
    private readonly ErrorsDestructuringPolicy _sut = new();
    private readonly TestPropertyValueFactory _factory = new();

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
        return Verify(ToAnonymous(actual!));
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
        return Verify(ToAnonymous(actual!));
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
        return Verify(ToAnonymous(actual!));
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
        return Verify(ToAnonymous(actual!));
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
        return Verify(ToAnonymous(actual!))
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
        return Verify(ToAnonymous(actual!));
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
        return Verify(ToAnonymous(actual!));
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
        return Verify(ToAnonymous(actual!));
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
        return Verify(ToAnonymous(actual!));
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

    private static object ToAnonymous(LogEventPropertyValue value)
    {
        return value switch
        {
            StructureValue sv => sv.Properties.ToDictionary(
                p => p.Name,
                p => ToAnonymous(p.Value)),
            SequenceValue seq => seq.Elements.Select(ToAnonymous).ToArray(),
            ScalarValue scalar => scalar.Value!,
            _ => value.ToString()
        };
    }

    private class TestPropertyValueFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
        {
            return value switch
            {
                null => new ScalarValue(null),
                string s => new ScalarValue(s),
                int i => new ScalarValue(i),
                long l => new ScalarValue(l),
                double d => new ScalarValue(d),
                bool b => new ScalarValue(b),
                Exception ex => CreateExceptionValue(ex),
                _ when IsTupleType(value.GetType()) => CreateTupleValue(value),
                _ when destructureObjects => CreateObjectValue(value),
                _ => new ScalarValue(value.ToString())
            };
        }

        private static bool IsTupleType(Type type)
        {
            return type.IsGenericType &&
                   type.FullName?.StartsWith("System.ValueTuple") == true;
        }

        private LogEventPropertyValue CreateTupleValue(object tuple)
        {
            var type = tuple.GetType();
            var fields = type.GetFields();
            var props = new List<LogEventProperty>();

            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(tuple);
                props.Add(new LogEventProperty(
                    field.Name,
                    CreatePropertyValue(fieldValue, true)));
            }

            return new StructureValue(props, type.Name);
        }

        private LogEventPropertyValue CreateObjectValue(object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties();
            var props = new List<LogEventProperty>();

            foreach (var prop in properties)
            {
                var propValue = prop.GetValue(obj);
                props.Add(new LogEventProperty(
                    prop.Name,
                    CreatePropertyValue(propValue, true)));
            }

            return new StructureValue(props, type.Name);
        }

        private static StructureValue CreateExceptionValue(Exception ex)
        {
            var props = new List<LogEventProperty>
            {
                new("Type", new ScalarValue(ex.GetType().Name)),
                new("Message", new ScalarValue(ex.Message)),
                new("HResult", new ScalarValue(ex.HResult))
            };

            if (ex.StackTrace != null)
                props.Add(new("StackTrace", new ScalarValue(ex.StackTrace)));

            if (ex.InnerException != null)
                props.Add(new("InnerException", CreateExceptionValue(ex.InnerException)));

            return new StructureValue(props, ex.GetType().Name);
        }
    }
}

public record Point(int X, int Y);
