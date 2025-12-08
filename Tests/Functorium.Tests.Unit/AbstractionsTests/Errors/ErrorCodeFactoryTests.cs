using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AbstractionsTests.Errors;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class ErrorCodeFactoryTests
{
    [Fact]
    public void Create_ReturnsErrorCodeExpected_WhenStringValueProvided()
    {
        // Arrange
        var errorCode = "User.NotFound";
        var errorCurrentValue = "user123";
        var errorMessage = "User not found";

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
        actual.IsExceptional.ShouldBeFalse();
    }

    [Fact]
    public void Create_ReturnsErrorCodeExpectedGeneric_WhenGenericValueProvided()
    {
        // Arrange
        var errorCode = "Temperature.OutOfRange";
        var errorCurrentValue = 150;
        var errorMessage = "Temperature is out of range";

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<int>>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsErrorCodeExpectedT1T2_WhenTwoValuesProvided()
    {
        // Arrange
        var errorCode = "Range.Invalid";
        var min = 10;
        var max = 5;
        var errorMessage = "Min cannot be greater than max";

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, min, max, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<int, int>>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsErrorCodeExpectedT1T2T3_WhenThreeValuesProvided()
    {
        // Arrange
        var errorCode = "Date.Invalid";
        var year = 2025;
        var month = 13;
        var day = 32;
        var errorMessage = "Invalid date components";

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, year, month, day, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<int, int, int>>();
        actual.Message.ShouldBe(errorMessage);
        actual.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromException_ReturnsErrorCodeExceptional_WhenExceptionProvided()
    {
        // Arrange
        var errorCode = "Database.ConnectionFailed";
        var exception = new InvalidOperationException("Connection failed");

        // Act
        var actual = ErrorCodeFactory.CreateFromException(errorCode, exception);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExceptional>();
        actual.Message.ShouldBe(exception.Message);
        actual.IsExceptional.ShouldBeTrue();
        actual.IsExpected.ShouldBeFalse();
    }

    [Fact]
    public void Format_ReturnsJoinedString_WhenMultiplePartsProvided()
    {
        // Arrange
        var parts = new[] { "Domain", "User", "NotFound" };

        // Act
        var actual = ErrorCodeFactory.Format(parts);

        // Assert
        actual.ShouldBe("Domain.User.NotFound");
    }

    [Fact]
    public void Format_ReturnsSinglePart_WhenOnePartProvided()
    {
        // Arrange
        var parts = new[] { "Error" };

        // Act
        var actual = ErrorCodeFactory.Format(parts);

        // Assert
        actual.ShouldBe("Error");
    }

    [Fact]
    public void Format_ReturnsEmptyString_WhenNoPartsProvided()
    {
        // Arrange
        var parts = System.Array.Empty<string>();

        // Act
        var actual = ErrorCodeFactory.Format(parts);

        // Assert
        actual.ShouldBe(string.Empty);
    }
}
