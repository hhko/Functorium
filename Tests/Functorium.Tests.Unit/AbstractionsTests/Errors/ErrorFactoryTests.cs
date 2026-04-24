using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AbstractionsTests.Errors;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class ErrorFactoryTests
{
    [Fact]
    public void CreateExpected_ReturnsExpectedError_WhenStringValueProvided()
    {
        // Arrange
        var errorCode = "User.NotFound";
        var errorCurrentValue = "user123";
        var errorMessage = "User not found";

        // Act
        var actual = ErrorFactory.CreateExpected(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
        actual.IsExceptional.ShouldBeFalse();
    }

    [Fact]
    public void CreateExpected_ReturnsExpectedErrorGeneric_WhenGenericValueProvided()
    {
        // Arrange
        var errorCode = "Temperature.OutOfRange";
        var errorCurrentValue = 150;
        var errorMessage = "Temperature is out of range";

        // Act
        var actual = ErrorFactory.CreateExpected(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError<int>>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void CreateExpected_ReturnsExpectedErrorT1T2_WhenTwoValuesProvided()
    {
        // Arrange
        var errorCode = "Range.Invalid";
        var min = 10;
        var max = 5;
        var errorMessage = "Min cannot be greater than max";

        // Act
        var actual = ErrorFactory.CreateExpected(errorCode, min, max, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError<int, int>>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void CreateExpected_ReturnsExpectedErrorT1T2T3_WhenThreeValuesProvided()
    {
        // Arrange
        var errorCode = "Date.Invalid";
        var year = 2025;
        var month = 13;
        var day = 32;
        var errorMessage = "Invalid date components";

        // Act
        var actual = ErrorFactory.CreateExpected(errorCode, year, month, day, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError<int, int, int>>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void CreateExceptional_ReturnsExceptionalError_WhenExceptionProvided()
    {
        // Arrange
        var errorCode = "Database.ConnectionFailed";
        var exception = new InvalidOperationException("Connection failed");

        // Act
        var actual = ErrorFactory.CreateExceptional(errorCode, exception);

        // Assert
        actual.ShouldBeOfType<ExceptionalError>();
        actual.Message.ShouldBe(exception.Message);
        actual.IsExceptional.ShouldBeTrue();
        actual.IsExpected.ShouldBeFalse();
    }
}
