using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AbstractionsTests.Errors;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class ErrorCodeExpectedTests
{
    [Fact]
    public void ErrorCode_ReturnsCorrectValue_WhenCreated()
    {
        // Arrange
        var errorCode = "User.NotFound";

        // Act
        var sut = new ErrorCodeExpected(errorCode, "value", "message");

        // Assert
        sut.ErrorCode.ShouldBe(errorCode);
    }

    [Fact]
    public void ErrorCurrentValue_ReturnsCorrectValue_WhenCreated()
    {
        // Arrange
        var errorCurrentValue = "user123";

        // Act
        var sut = new ErrorCodeExpected("code", errorCurrentValue, "message");

        // Assert
        sut.ErrorCurrentValue.ShouldBe(errorCurrentValue);
    }

    [Fact]
    public void Message_ReturnsErrorMessage_WhenCreated()
    {
        // Arrange
        var errorMessage = "User not found";

        // Act
        var sut = new ErrorCodeExpected("code", "value", errorMessage);

        // Assert
        sut.Message.ShouldBe(errorMessage);
    }

    [Fact]
    public void Code_ReturnsDefaultValue_WhenNotSpecified()
    {
        // Arrange & Act
        var sut = new ErrorCodeExpected("code", "value", "message");

        // Assert
        sut.Code.ShouldBe(-1000);
    }

    [Fact]
    public void Code_ReturnsSpecifiedValue_WhenProvided()
    {
        // Arrange
        var errorCodeId = 404;

        // Act
        var sut = new ErrorCodeExpected("code", "value", "message", errorCodeId);

        // Assert
        sut.Code.ShouldBe(errorCodeId);
    }

    [Fact]
    public void IsExpected_ReturnsTrue_Always()
    {
        // Arrange & Act
        var sut = new ErrorCodeExpected("code", "value", "message");

        // Assert
        sut.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void IsExceptional_ReturnsFalse_Always()
    {
        // Arrange & Act
        var sut = new ErrorCodeExpected("code", "value", "message");

        // Assert
        sut.IsExceptional.ShouldBeFalse();
    }

    [Fact]
    public void HasException_ReturnsFalse_Always()
    {
        // Arrange
        var sut = new ErrorCodeExpected("code", "value", "message");

        // Act
        var actual = sut.HasException<InvalidOperationException>();

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void ToString_ReturnsMessage_WhenCalled()
    {
        // Arrange
        var errorMessage = "User not found";
        var sut = new ErrorCodeExpected("code", "value", errorMessage);

        // Act
        var actual = sut.ToString();

        // Assert
        actual.ShouldBe(errorMessage);
    }

    [Fact]
    public void Throw_ThrowsWrappedErrorExpectedException_WhenCalled()
    {
        // Arrange
        var sut = new ErrorCodeExpected("code", "value", "message");

        // Act & Assert
        Should.Throw<WrappedErrorExpectedException>(() => sut.Throw<int>());
    }

    [Fact]
    public void ToErrorException_ReturnsWrappedErrorExpectedException_WhenCalled()
    {
        // Arrange
        var sut = new ErrorCodeExpected("code", "value", "message");

        // Act
        var actual = sut.ToErrorException();

        // Assert
        actual.ShouldBeOfType<WrappedErrorExpectedException>();
    }

    [Fact]
    public void Inner_ReturnsNone_WhenNotProvided()
    {
        // Arrange & Act
        var sut = new ErrorCodeExpected("code", "value", "message");

        // Assert
        sut.Inner.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Inner_ReturnsSome_WhenProvided()
    {
        // Arrange
        Error innerError = Error.New("Inner error");

        // Act
        var sut = new ErrorCodeExpected("code", "value", "message", Inner: innerError);

        // Assert
        sut.Inner.IsSome.ShouldBeTrue();
    }
}

public class ErrorCodeExpectedGenericTests
{
    [Fact]
    public void ErrorCurrentValue_ReturnsTypedValue_WhenGenericTypeProvided()
    {
        // Arrange
        var errorCurrentValue = 42;

        // Act
        var sut = new ErrorCodeExpected<int>("code", errorCurrentValue, "message");

        // Assert
        sut.ErrorCurrentValue.ShouldBe(errorCurrentValue);
    }

    [Fact]
    public void ErrorCurrentValues_ReturnTypedValues_WhenTwoGenericTypesProvided()
    {
        // Arrange
        var value1 = 10;
        var value2 = "test";

        // Act
        var sut = new ErrorCodeExpected<int, string>("code", value1, value2, "message");

        // Assert
        sut.ErrorCurrentValue1.ShouldBe(value1);
        sut.ErrorCurrentValue2.ShouldBe(value2);
    }

    [Fact]
    public void ErrorCurrentValues_ReturnTypedValues_WhenThreeGenericTypesProvided()
    {
        // Arrange
        var value1 = 10;
        var value2 = "test";
        var value3 = 3.14;

        // Act
        var sut = new ErrorCodeExpected<int, string, double>("code", value1, value2, value3, "message");

        // Assert
        sut.ErrorCurrentValue1.ShouldBe(value1);
        sut.ErrorCurrentValue2.ShouldBe(value2);
        sut.ErrorCurrentValue3.ShouldBe(value3);
    }

    [Fact]
    public void IsExpected_ReturnsTrue_ForAllGenericVariants()
    {
        // Arrange
        var sut1 = new ErrorCodeExpected<int>("code", 1, "message");
        var sut2 = new ErrorCodeExpected<int, string>("code", 1, "a", "message");
        var sut3 = new ErrorCodeExpected<int, string, double>("code", 1, "a", 1.0, "message");

        // Assert
        sut1.IsExpected.ShouldBeTrue();
        sut2.IsExpected.ShouldBeTrue();
        sut3.IsExpected.ShouldBeTrue();
    }

    [Fact]
    public void IsExceptional_ReturnsFalse_ForAllGenericVariants()
    {
        // Arrange
        var sut1 = new ErrorCodeExpected<int>("code", 1, "message");
        var sut2 = new ErrorCodeExpected<int, string>("code", 1, "a", "message");
        var sut3 = new ErrorCodeExpected<int, string, double>("code", 1, "a", 1.0, "message");

        // Assert
        sut1.IsExceptional.ShouldBeFalse();
        sut2.IsExceptional.ShouldBeFalse();
        sut3.IsExceptional.ShouldBeFalse();
    }
}
