using Functorium.Abstractions.Errors;
using Functorium.Testing.Assertions.Errors;
using LanguageExt;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.TestingTests.Assertions;

[Trait(nameof(UnitTest), UnitTest.Functorium_Testing)]
public class ExpectedErrorAssertionsTests
{
    private const string TestErrorCode = "TestErrors.Example.SampleError";

    #region Error State - ShouldHaveErrorCode

    [Fact]
    public void ShouldHaveErrorCode_ReturnsIHasErrorCode_WhenErrorImplementsInterface()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act
        var result = error.ShouldHaveErrorCode();

        // Assert
        result.ShouldNotBeNull();
        result.ErrorCode.ShouldBe(TestErrorCode);
    }

    [Fact]
    public void ShouldHaveErrorCode_ThrowsException_WhenErrorDoesNotImplementInterface()
    {
        // Arrange
        var error = Error.New("Plain error without error code");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => error.ShouldHaveErrorCode());
    }

    #endregion

    #region Error State - ShouldBeExpected / ShouldBeExceptional

    [Fact]
    public void ShouldBeExpected_ReturnsSuccess_WhenErrorIsExpected()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Expected error");

        // Act & Assert (should not throw)
        error.ShouldBeExpected();
    }

    [Fact]
    public void ShouldBeExpected_ThrowsException_WhenErrorIsExceptional()
    {
        // Arrange
        var error = ErrorFactory.CreateExceptional(TestErrorCode, new InvalidOperationException("Test"));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => error.ShouldBeExpected());
    }

    [Fact]
    public void ShouldBeExceptional_ReturnsSuccess_WhenErrorIsExceptional()
    {
        // Arrange
        var error = ErrorFactory.CreateExceptional(TestErrorCode, new InvalidOperationException("Test"));

        // Act & Assert (should not throw)
        error.ShouldBeExceptional();
    }

    [Fact]
    public void ShouldBeExceptional_ThrowsException_WhenErrorIsExpected()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Expected error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => error.ShouldBeExceptional());
    }

    #endregion

    #region ErrorCode Matching - ShouldHaveErrorCode(string)

    [Fact]
    public void ShouldHaveErrorCode_WithString_ReturnsSuccess_WhenCodeMatches()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert (should not throw)
        error.ShouldHaveErrorCode(TestErrorCode);
    }

    [Fact]
    public void ShouldHaveErrorCode_WithString_ThrowsException_WhenCodeDoesNotMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldHaveErrorCode("DifferentErrorCode"));
    }

    #endregion

    #region ErrorCode Matching - ShouldHaveErrorCodeStartingWith

    [Fact]
    public void ShouldHaveErrorCodeStartingWith_ReturnsSuccess_WhenCodeStartsWithPrefix()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert (should not throw)
        error.ShouldHaveErrorCodeStartingWith("TestErrors.Example");
    }

    [Fact]
    public void ShouldHaveErrorCodeStartingWith_ThrowsException_WhenCodeDoesNotStartWithPrefix()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldHaveErrorCodeStartingWith("DifferentPrefix"));
    }

    #endregion

    #region ErrorCode Matching - ShouldHaveErrorCode(predicate)

    [Fact]
    public void ShouldHaveErrorCode_WithPredicate_ReturnsSuccess_WhenPredicateMatches()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert (should not throw)
        error.ShouldHaveErrorCode(code => code.Contains("Example"));
    }

    [Fact]
    public void ShouldHaveErrorCode_WithPredicate_ThrowsException_WhenPredicateDoesNotMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldHaveErrorCode(code => code.Contains("NonExistent")));
    }

    #endregion

    #region ExpectedError - ShouldBeExpectedError (non-generic)

    [Fact]
    public void ShouldBeExpectedError_ReturnsSuccess_WhenCodeAndValueMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "test-value", "Test error");

        // Act & Assert (should not throw)
        error.ShouldBeExpectedError(TestErrorCode, "test-value");
    }

    [Fact]
    public void ShouldBeExpectedError_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "test-value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeExpectedError(TestErrorCode, "different-value"));
    }

    #endregion

    #region ExpectedError<T> - ShouldBeExpectedError<T>

    [Fact]
    public void ShouldBeExpectedError_Generic_ReturnsSuccess_WhenCodeAndValueMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, 42, "Test error");

        // Act & Assert (should not throw)
        error.ShouldBeExpectedError(TestErrorCode, 42);
    }

    [Fact]
    public void ShouldBeExpectedError_Generic_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, 42, "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeExpectedError(TestErrorCode, 100));
    }

    [Fact]
    public void ShouldBeExpectedError_WithPredicate_ReturnsSuccess_WhenPredicateMatches()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, 42, "Test error");

        // Act & Assert (should not throw)
        error.ShouldBeExpectedError<int>(TestErrorCode, value => value > 0);
    }

    [Fact]
    public void ShouldBeExpectedError_WithPredicate_ThrowsException_WhenPredicateDoesNotMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, 42, "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeExpectedError<int>(TestErrorCode, value => value < 0));
    }

    #endregion

    #region ExpectedError<T1, T2> - ShouldBeExpectedError<T1, T2>

    [Fact]
    public void ShouldBeExpectedError_TwoValues_ReturnsSuccess_WhenAllValuesMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "first", 42, "Test error");

        // Act & Assert (should not throw)
        error.ShouldBeExpectedError(TestErrorCode, "first", 42);
    }

    [Fact]
    public void ShouldBeExpectedError_TwoValues_ThrowsException_WhenSecondValueDoesNotMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "first", 42, "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeExpectedError(TestErrorCode, "first", 100));
    }

    #endregion

    #region ExpectedError<T1, T2, T3> - ShouldBeExpectedError<T1, T2, T3>

    [Fact]
    public void ShouldBeExpectedError_ThreeValues_ReturnsSuccess_WhenAllValuesMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "first", 42, 3.14, "Test error");

        // Act & Assert (should not throw)
        error.ShouldBeExpectedError(TestErrorCode, "first", 42, 3.14);
    }

    [Fact]
    public void ShouldBeExpectedError_ThreeValues_ThrowsException_WhenThirdValueDoesNotMatch()
    {
        // Arrange
        var error = ErrorFactory.CreateExpected(TestErrorCode, "first", 42, 3.14, "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeExpectedError(TestErrorCode, "first", 42, 2.71));
    }

    #endregion

    #region Fin<T> - ShouldSucceed

    [Fact]
    public void Fin_ShouldSucceed_ReturnsValue_WhenFinSucceeds()
    {
        // Arrange
        Fin<int> fin = 42;

        // Act
        var result = fin.ShouldSucceed();

        // Assert
        result.ShouldBe(42);
    }

    [Fact]
    public void Fin_ShouldSucceed_ThrowsException_WhenFinFails()
    {
        // Arrange
        Fin<int> fin = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => fin.ShouldSucceed());
    }

    [Fact]
    public void Fin_ShouldSucceedWith_ReturnsSuccess_WhenValueMatches()
    {
        // Arrange
        Fin<int> fin = 42;

        // Act & Assert (should not throw)
        fin.ShouldSucceedWith(42);
    }

    [Fact]
    public void Fin_ShouldSucceedWith_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        Fin<int> fin = 42;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => fin.ShouldSucceedWith(100));
    }

    [Fact]
    public void Fin_ShouldSucceed_WithPredicate_ReturnsSuccess_WhenPredicateMatches()
    {
        // Arrange
        Fin<int> fin = 42;

        // Act & Assert (should not throw)
        fin.ShouldSucceed(value => value > 0);
    }

    #endregion

    #region Fin<T> - ShouldFail

    [Fact]
    public void Fin_ShouldFail_ReturnsSuccess_WhenFinFails()
    {
        // Arrange
        Fin<int> fin = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert (should not throw)
        fin.ShouldFail();
    }

    [Fact]
    public void Fin_ShouldFail_ThrowsException_WhenFinSucceeds()
    {
        // Arrange
        Fin<int> fin = 42;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => fin.ShouldFail());
    }

    [Fact]
    public void Fin_ShouldFail_WithAction_ExecutesAssertion()
    {
        // Arrange
        Fin<int> fin = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");
        var assertionExecuted = false;

        // Act
        fin.ShouldFail(error =>
        {
            error.ShouldHaveErrorCode(TestErrorCode);
            assertionExecuted = true;
        });

        // Assert
        assertionExecuted.ShouldBeTrue();
    }

    [Fact]
    public void Fin_ShouldFailWithErrorCode_ReturnsSuccess_WhenErrorCodeMatches()
    {
        // Arrange
        Fin<int> fin = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert (should not throw)
        fin.ShouldFailWithErrorCode(TestErrorCode);
    }

    [Fact]
    public void Fin_ShouldFailWithErrorCode_ThrowsException_WhenErrorCodeDoesNotMatch()
    {
        // Arrange
        Fin<int> fin = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldFailWithErrorCode("DifferentErrorCode"));
    }

    #endregion

    #region Validation<Error, T> - ShouldBeValid

    [Fact]
    public void Validation_ShouldBeValid_ReturnsValue_WhenValidationSucceeds()
    {
        // Arrange
        Validation<Error, int> validation = 42;

        // Act
        var result = validation.ShouldBeValid();

        // Assert
        result.ShouldBe(42);
    }

    [Fact]
    public void Validation_ShouldBeValid_ThrowsException_WhenValidationFails()
    {
        // Arrange
        Validation<Error, int> validation = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => validation.ShouldBeValid());
    }

    #endregion

    #region Validation<Error, T> - ShouldBeInvalid

    [Fact]
    public void Validation_ShouldBeInvalid_ExecutesAssertion_WhenValidationFails()
    {
        // Arrange
        Validation<Error, int> validation = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");
        var assertionExecuted = false;

        // Act
        validation.ShouldBeInvalid(errors =>
        {
            errors.Count.ShouldBe(1);
            assertionExecuted = true;
        });

        // Assert
        assertionExecuted.ShouldBeTrue();
    }

    [Fact]
    public void Validation_ShouldBeInvalid_ThrowsException_WhenValidationSucceeds()
    {
        // Arrange
        Validation<Error, int> validation = 42;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldBeInvalid(_ => { }));
    }

    #endregion

    #region Validation<Error, T> - ShouldContainErrorCode

    [Fact]
    public void Validation_ShouldContainErrorCode_ReturnsSuccess_WhenErrorCodeExists()
    {
        // Arrange
        Validation<Error, int> validation = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert (should not throw)
        validation.ShouldContainErrorCode(TestErrorCode);
    }

    [Fact]
    public void Validation_ShouldContainErrorCode_ThrowsException_WhenErrorCodeDoesNotExist()
    {
        // Arrange
        Validation<Error, int> validation = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldContainErrorCode("DifferentErrorCode"));
    }

    #endregion

    #region Validation<Error, T> - ShouldContainOnlyErrorCode

    [Fact]
    public void Validation_ShouldContainOnlyErrorCode_ReturnsSuccess_WhenOnlyOneErrorExists()
    {
        // Arrange
        Validation<Error, int> validation = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert (should not throw)
        validation.ShouldContainOnlyErrorCode(TestErrorCode);
    }

    [Fact]
    public void Validation_ShouldContainOnlyErrorCode_ThrowsException_WhenMultipleErrorsExist()
    {
        // Arrange
        var error1 = ErrorFactory.CreateExpected(TestErrorCode, "value1", "Test error 1");
        var error2 = ErrorFactory.CreateExpected("SecondErrorCode", "value2", "Test error 2");
        Validation<Error, int> validation1 = error1;
        Validation<Error, int> validation2 = error2;
        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            combined.ShouldContainOnlyErrorCode(TestErrorCode));
    }

    #endregion

    #region Validation<Error, T> - ShouldContainErrorCodes

    [Fact]
    public void Validation_ShouldContainErrorCodes_ReturnsSuccess_WhenAllErrorCodesExist()
    {
        // Arrange
        var error1 = ErrorFactory.CreateExpected(TestErrorCode, "value1", "Test error 1");
        var error2 = ErrorFactory.CreateExpected("SecondErrorCode", "value2", "Test error 2");
        Validation<Error, int> validation1 = error1;
        Validation<Error, int> validation2 = error2;
        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert (should not throw)
        combined.ShouldContainErrorCodes(TestErrorCode, "SecondErrorCode");
    }

    [Fact]
    public void Validation_ShouldContainErrorCodes_ThrowsException_WhenErrorCodeIsMissing()
    {
        // Arrange
        Validation<Error, int> validation = ErrorFactory.CreateExpected(TestErrorCode, "value", "Test error");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldContainErrorCodes(TestErrorCode, "MissingErrorCode"));
    }

    #endregion
}
