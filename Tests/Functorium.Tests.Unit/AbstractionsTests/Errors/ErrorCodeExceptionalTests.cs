using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AbstractionsTests.Errors;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class ErrorCodeExceptionalTests
{
    [Fact]
    public void ErrorCode_ReturnsCorrectValue_WhenCreated()
    {
        // Arrange
        var errorCode = "Database.ConnectionFailed";
        var exception = new InvalidOperationException("Connection failed");

        // Act
        var sut = new ErrorCodeExceptional(errorCode, exception);

        // Assert
        sut.ErrorCode.ShouldBe(errorCode);
    }

    [Fact]
    public void Message_ReturnsExceptionMessage_WhenCreated()
    {
        // Arrange
        var exceptionMessage = "Connection failed";
        var exception = new InvalidOperationException(exceptionMessage);

        // Act
        var sut = new ErrorCodeExceptional("code", exception);

        // Assert
        sut.Message.ShouldBe(exceptionMessage);
    }

    [Fact]
    public void Code_ReturnsExceptionHResult_WhenCreated()
    {
        // Arrange
        var exception = new InvalidOperationException("test");

        // Act
        var sut = new ErrorCodeExceptional("code", exception);

        // Assert
        sut.Code.ShouldBe(exception.HResult);
    }

    [Fact]
    public void IsExpected_ReturnsFalse_Always()
    {
        // Arrange
        var exception = new InvalidOperationException("test");

        // Act
        var sut = new ErrorCodeExceptional("code", exception);

        // Assert
        sut.IsExpected.ShouldBeFalse();
    }

    [Fact]
    public void IsExceptional_ReturnsTrue_Always()
    {
        // Arrange
        var exception = new InvalidOperationException("test");

        // Act
        var sut = new ErrorCodeExceptional("code", exception);

        // Assert
        sut.IsExceptional.ShouldBeTrue();
    }

    [Fact]
    public void HasException_ReturnsTrue_WhenMatchingExceptionType()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        var sut = new ErrorCodeExceptional("code", exception);

        // Act
        var actual = sut.HasException<InvalidOperationException>();

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void HasException_ReturnsFalse_WhenDifferentExceptionType()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        var sut = new ErrorCodeExceptional("code", exception);

        // Act
        var actual = sut.HasException<ArgumentException>();

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void ToException_ReturnsOriginalException_WhenCreated()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        var sut = new ErrorCodeExceptional("code", exception);

        // Act
        var actual = sut.ToException();

        // Assert
        actual.ShouldBeSameAs(exception);
    }

    [Fact]
    public void ToString_ReturnsMessage_WhenCalled()
    {
        // Arrange
        var exceptionMessage = "Connection failed";
        var exception = new InvalidOperationException(exceptionMessage);
        var sut = new ErrorCodeExceptional("code", exception);

        // Act
        var actual = sut.ToString();

        // Assert
        actual.ShouldBe(exceptionMessage);
    }

    [Fact]
    public void Inner_ReturnsNone_WhenExceptionHasNoInnerException()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        var sut = new ErrorCodeExceptional("code", exception);

        // Act
        var actual = sut.Inner;

        // Assert
        actual.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Inner_ReturnsSome_WhenExceptionHasInnerException()
    {
        // Arrange
        var innerException = new ArgumentException("inner");
        var exception = new InvalidOperationException("outer", innerException);
        var sut = new ErrorCodeExceptional("code", exception);

        // Act
        var actual = sut.Inner;

        // Assert
        actual.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void Is_ReturnsTrue_WhenSameExceptionType()
    {
        // Arrange
        var exception1 = new InvalidOperationException("test");
        var exception2 = new InvalidOperationException("test2");
        var sut = new ErrorCodeExceptional("code", exception1);
        var other = new ErrorCodeExceptional("code2", exception2);

        // Act
        var actual = sut.Is(other);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Is_ReturnsFalse_WhenDifferentExceptionType()
    {
        // Arrange
        var exception1 = new InvalidOperationException("test");
        var exception2 = new ArgumentException("test");
        var sut = new ErrorCodeExceptional("code", exception1);
        var other = new ErrorCodeExceptional("code2", exception2);

        // Act
        var actual = sut.Is(other);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void Throw_ThrowsOriginalException_WhenCalled()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        var sut = new ErrorCodeExceptional("code", exception);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.Throw<int>());
    }

    [Fact]
    public void ToErrorException_ReturnsExceptionalException_WhenCalled()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        var sut = new ErrorCodeExceptional("code", exception);

        // Act
        var actual = sut.ToErrorException();

        // Assert
        actual.ShouldBeOfType<ExceptionalException>();
    }
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class ErrorCodeExceptionalInterfaceTests
{
    [Fact]
    public void ErrorCodeExceptional_ImplementsIHasErrorCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");
        var sut = new ErrorCodeExceptional("error.code", exception);

        // Assert
        sut.ShouldBeAssignableTo<IHasErrorCode>();
    }

    [Fact]
    public void ErrorCodeExceptional_IHasErrorCode_ReturnsCorrectErrorCode()
    {
        // Arrange
        var errorCode = "Database.ConnectionTimeout";
        var exception = new InvalidOperationException("Connection timed out");
        var sut = new ErrorCodeExceptional(errorCode, exception);
        IHasErrorCode hasErrorCode = sut;

        // Act & Assert
        hasErrorCode.ErrorCode.ShouldBe(errorCode);
    }
}
