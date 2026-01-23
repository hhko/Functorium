using Functorium.Abstractions.Errors;
using Functorium.Testing.Assertions.Errors;
using LanguageExt;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.TestingTests.Assertions;

[Trait(nameof(UnitTest), UnitTest.Functorium_Testing)]
public class ErrorCodeExceptionalAssertionsTests
{
    private const string TestErrorCode = "TestErrors.Exception.Sample";

    #region Error - ShouldBeErrorCodeExceptional

    [Fact]
    public void ShouldBeErrorCodeExceptional_ReturnsSuccess_WhenErrorIsCorrectType()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        error.ShouldBeErrorCodeExceptional(TestErrorCode);
    }

    [Fact]
    public void ShouldBeErrorCodeExceptional_ThrowsException_WhenErrorIsNotExceptional()
    {
        // Arrange
        var error = ErrorCodeFactory.Create(TestErrorCode, "value", "Not exceptional");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeErrorCodeExceptional(TestErrorCode));
    }

    [Fact]
    public void ShouldBeErrorCodeExceptional_ThrowsException_WhenErrorCodeDoesNotMatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeErrorCodeExceptional("DifferentErrorCode"));
    }

    #endregion

    #region Error - ShouldBeErrorCodeExceptional<TException>

    [Fact]
    public void ShouldBeErrorCodeExceptional_Generic_ReturnsSuccess_WhenExceptionTypeMatches()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        error.ShouldBeErrorCodeExceptional<InvalidOperationException>(TestErrorCode);
    }

    [Fact]
    public void ShouldBeErrorCodeExceptional_Generic_ThrowsException_WhenExceptionTypeDoesNotMatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeErrorCodeExceptional<ArgumentException>(TestErrorCode));
    }

    #endregion

    #region Error - ShouldWrapException

    [Fact]
    public void ShouldWrapException_ReturnsSuccess_WhenExceptionTypeAndMessageMatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception message");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        error.ShouldWrapException<InvalidOperationException>(TestErrorCode, "Test exception message");
    }

    [Fact]
    public void ShouldWrapException_ReturnsSuccess_WhenOnlyExceptionTypeMatches()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        error.ShouldWrapException<InvalidOperationException>(TestErrorCode);
    }

    [Fact]
    public void ShouldWrapException_ThrowsException_WhenMessageDoesNotMatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Actual message");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldWrapException<InvalidOperationException>(TestErrorCode, "Different message"));
    }

    #endregion

    #region Error - ShouldBeErrorCodeExceptional with Action

    [Fact]
    public void ShouldBeErrorCodeExceptional_WithAction_ExecutesAssertion()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);
        var assertionExecuted = false;

        // Act
        error.ShouldBeErrorCodeExceptional(TestErrorCode, ex =>
        {
            ex.ShouldBeOfType<InvalidOperationException>();
            assertionExecuted = true;
        });

        // Assert
        assertionExecuted.ShouldBeTrue();
    }

    #endregion

    #region Fin<T> - ShouldFailWithException

    [Fact]
    public void Fin_ShouldFailWithException_ReturnsSuccess_WhenFinFailsWithExpectedError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Fin<int> fin = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        fin.ShouldFailWithException(TestErrorCode);
    }

    [Fact]
    public void Fin_ShouldFailWithException_ThrowsException_WhenFinSucceeds()
    {
        // Arrange
        Fin<int> fin = 42;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldFailWithException(TestErrorCode));
    }

    [Fact]
    public void Fin_ShouldFailWithException_ThrowsException_WhenErrorCodeDoesNotMatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Fin<int> fin = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldFailWithException("DifferentErrorCode"));
    }

    #endregion

    #region Fin<T> - ShouldFailWithException<TException>

    [Fact]
    public void Fin_ShouldFailWithException_Generic_ReturnsSuccess_WhenExceptionTypeMatches()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Fin<int> fin = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        fin.ShouldFailWithException<int, InvalidOperationException>(TestErrorCode);
    }

    [Fact]
    public void Fin_ShouldFailWithException_Generic_ThrowsException_WhenExceptionTypeDoesNotMatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Fin<int> fin = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldFailWithException<int, ArgumentException>(TestErrorCode));
    }

    #endregion

    #region Fin<T> - ShouldFailWithException with Action

    [Fact]
    public void Fin_ShouldFailWithException_WithAction_ExecutesAssertion()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception message");
        Fin<int> fin = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);
        var assertionExecuted = false;

        // Act
        fin.ShouldFailWithException(TestErrorCode, ex =>
        {
            ex.Message.ShouldBe("Test exception message");
            assertionExecuted = true;
        });

        // Assert
        assertionExecuted.ShouldBeTrue();
    }

    #endregion

    #region Validation<Error, T> - ShouldContainException

    [Fact]
    public void Validation_ShouldContainException_ReturnsSuccess_WhenExceptionExists()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Validation<Error, int> validation = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        validation.ShouldContainException(TestErrorCode);
    }

    [Fact]
    public void Validation_ShouldContainException_ThrowsException_WhenValidationSucceeds()
    {
        // Arrange
        Validation<Error, int> validation = 42;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldContainException(TestErrorCode));
    }

    [Fact]
    public void Validation_ShouldContainException_ThrowsException_WhenErrorCodeDoesNotExist()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Validation<Error, int> validation = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldContainException("DifferentErrorCode"));
    }

    [Fact]
    public void Validation_ShouldContainException_ThrowsException_WhenErrorIsNotExceptional()
    {
        // Arrange
        Validation<Error, int> validation = ErrorCodeFactory.Create(TestErrorCode, "value", "Not exceptional");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldContainException(TestErrorCode));
    }

    #endregion

    #region Validation<Error, T> - ShouldContainException<TException>

    [Fact]
    public void Validation_ShouldContainException_Generic_ReturnsSuccess_WhenExceptionTypeMatches()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Validation<Error, int> validation = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        validation.ShouldContainException<int, InvalidOperationException>(TestErrorCode);
    }

    [Fact]
    public void Validation_ShouldContainException_Generic_ThrowsException_WhenExceptionTypeDoesNotMatch()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Validation<Error, int> validation = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldContainException<int, ArgumentException>(TestErrorCode));
    }

    #endregion

    #region Validation<Error, T> - ShouldContainOnlyException

    [Fact]
    public void Validation_ShouldContainOnlyException_ReturnsSuccess_WhenOnlyOneExceptionExists()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        Validation<Error, int> validation = ErrorCodeFactory.CreateFromException(TestErrorCode, exception);

        // Act & Assert (should not throw)
        validation.ShouldContainOnlyException(TestErrorCode);
    }

    [Fact]
    public void Validation_ShouldContainOnlyException_ThrowsException_WhenMultipleErrorsExist()
    {
        // Arrange
        var error1 = ErrorCodeFactory.CreateFromException(TestErrorCode, new InvalidOperationException("Ex 1"));
        var error2 = ErrorCodeFactory.CreateFromException("SecondErrorCode", new ArgumentException("Ex 2"));
        Validation<Error, int> validation1 = error1;
        Validation<Error, int> validation2 = error2;
        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            combined.ShouldContainOnlyException(TestErrorCode));
    }

    #endregion
}
