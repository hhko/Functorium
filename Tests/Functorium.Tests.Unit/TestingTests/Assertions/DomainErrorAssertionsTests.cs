using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using Functorium.Testing.Assertions.Errors;
using LanguageExt;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.TestingTests.Assertions;

// 테스트용 더미 값 객체
public sealed class DummyValueObject : SimpleValueObject<string>
{
    private DummyValueObject(string value) : base(value) { }
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Testing)]
public class DomainErrorAssertionsTests
{
    #region Error - ShouldBeDomainError<TValueObject>

    [Fact]
    public void ShouldBeDomainError_ReturnsSuccess_WhenErrorMatchesExpectedType()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");

        // Act & Assert (should not throw)
        error.ShouldBeDomainError<DummyValueObject>(new DomainErrorType.Empty());
    }

    [Fact]
    public void ShouldBeDomainError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeDomainError<DummyValueObject>(new DomainErrorType.Null()));
    }

    [Fact]
    public void ShouldBeDomainError_ReturnsSuccess_WhenCustomErrorMatches()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Custom("Unsupported"),
            currentValue: "test",
            message: "Not supported");

        // Act & Assert (should not throw)
        error.ShouldBeDomainError<DummyValueObject>(new DomainErrorType.Custom("Unsupported"));
    }

    #endregion

    #region Error - ShouldBeDomainError<TValueObject, TValue>

    [Fact]
    public void ShouldBeDomainError_WithValue_ReturnsSuccess_WhenErrorAndValueMatch()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject, int>(
            new DomainErrorType.Negative(),
            currentValue: -5,
            message: "Value cannot be negative");

        // Act & Assert (should not throw)
        error.ShouldBeDomainError<DummyValueObject, int>(
            new DomainErrorType.Negative(),
            expectedCurrentValue: -5);
    }

    [Fact]
    public void ShouldBeDomainError_WithValue_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject, int>(
            new DomainErrorType.Negative(),
            currentValue: -5,
            message: "Value cannot be negative");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeDomainError<DummyValueObject, int>(
                new DomainErrorType.Negative(),
                expectedCurrentValue: -10));
    }

    #endregion

    #region Error - ShouldBeDomainError<TValueObject, T1, T2>

    [Fact]
    public void ShouldBeDomainError_WithTwoValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);
        var error = DomainError.For<DummyValueObject, DateTime, DateTime>(
            new DomainErrorType.Custom("StartAfterEnd"),
            startDate,
            endDate,
            message: "Start date cannot be after end date");

        // Act & Assert (should not throw)
        error.ShouldBeDomainError<DummyValueObject, DateTime, DateTime>(
            new DomainErrorType.Custom("StartAfterEnd"),
            expectedValue1: startDate,
            expectedValue2: endDate);
    }

    [Fact]
    public void ShouldBeDomainError_WithTwoValues_ThrowsException_WhenValue1DoesNotMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);
        var error = DomainError.For<DummyValueObject, DateTime, DateTime>(
            new DomainErrorType.Custom("StartAfterEnd"),
            startDate,
            endDate,
            message: "Start date cannot be after end date");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeDomainError<DummyValueObject, DateTime, DateTime>(
                new DomainErrorType.Custom("StartAfterEnd"),
                expectedValue1: new DateTime(2024, 1, 1),
                expectedValue2: endDate));
    }

    #endregion

    #region Error - ShouldBeDomainError<TValueObject, T1, T2, T3>

    [Fact]
    public void ShouldBeDomainError_WithThreeValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var duration = 365;
        var error = DomainError.For<DummyValueObject, DateTime, DateTime, int>(
            new DomainErrorType.Custom("DateRangeWithDuration"),
            startDate,
            endDate,
            duration,
            message: "Date range spans 365 days");

        // Act & Assert (should not throw)
        error.ShouldBeDomainError<DummyValueObject, DateTime, DateTime, int>(
            new DomainErrorType.Custom("DateRangeWithDuration"),
            expectedValue1: startDate,
            expectedValue2: endDate,
            expectedValue3: duration);
    }

    [Fact]
    public void ShouldBeDomainError_WithThreeValues_ThrowsException_WhenValue3DoesNotMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var duration = 365;
        var error = DomainError.For<DummyValueObject, DateTime, DateTime, int>(
            new DomainErrorType.Custom("DateRangeWithDuration"),
            startDate,
            endDate,
            duration,
            message: "Date range spans 365 days");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeDomainError<DummyValueObject, DateTime, DateTime, int>(
                new DomainErrorType.Custom("DateRangeWithDuration"),
                expectedValue1: startDate,
                expectedValue2: endDate,
                expectedValue3: 366)); // Wrong value
    }

    #endregion

    #region Fin<T> - ShouldBeDomainError<TValueObject, T>

    [Fact]
    public void Fin_ShouldBeDomainError_ReturnsSuccess_WhenFinFailsWithExpectedError()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");
        Fin<string> fin = error;

        // Act & Assert (should not throw)
        fin.ShouldBeDomainError<DummyValueObject, string>(new DomainErrorType.Empty());
    }

    [Fact]
    public void Fin_ShouldBeDomainError_ThrowsException_WhenFinSucceeds()
    {
        // Arrange
        Fin<string> fin = "success";

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldBeDomainError<DummyValueObject, string>(new DomainErrorType.Empty()));
    }

    [Fact]
    public void Fin_ShouldBeDomainError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");
        Fin<string> fin = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldBeDomainError<DummyValueObject, string>(new DomainErrorType.Null()));
    }

    [Fact]
    public void Fin_ShouldBeDomainError_WithValue_ReturnsSuccess_WhenFinFailsWithExpectedErrorAndValue()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject, int>(
            new DomainErrorType.Negative(),
            currentValue: -5,
            message: "Value cannot be negative");
        Fin<int> fin = error;

        // Act & Assert (should not throw)
        fin.ShouldBeDomainError<DummyValueObject, int, int>(
            new DomainErrorType.Negative(),
            expectedCurrentValue: -5);
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveDomainError<TValueObject, T>

    [Fact]
    public void Validation_ShouldHaveDomainError_ReturnsSuccess_WhenValidationFailsWithExpectedError()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveDomainError<DummyValueObject, string>(new DomainErrorType.Empty());
    }

    [Fact]
    public void Validation_ShouldHaveDomainError_ThrowsException_WhenValidationSucceeds()
    {
        // Arrange
        Validation<Error, string> validation = "success";

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveDomainError<DummyValueObject, string>(new DomainErrorType.Empty()));
    }

    [Fact]
    public void Validation_ShouldHaveDomainError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveDomainError<DummyValueObject, string>(new DomainErrorType.Null()));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveOnlyDomainError<TValueObject, T>

    [Fact]
    public void Validation_ShouldHaveOnlyDomainError_ReturnsSuccess_WhenExactlyOneErrorMatches()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.TooShort(),
            currentValue: "a",
            message: "Value is too short");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveOnlyDomainError<DummyValueObject, string>(new DomainErrorType.TooShort());
    }

    [Fact]
    public void Validation_ShouldHaveOnlyDomainError_ThrowsException_WhenMultipleErrors()
    {
        // Arrange - Use Apply pattern to combine errors
        var error1 = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");
        var error2 = DomainError.For<DummyValueObject>(
            new DomainErrorType.InvalidFormat(),
            currentValue: "",
            message: "Invalid format");

        Validation<Error, string> validation1 = error1;
        Validation<Error, string> validation2 = error2;

        // Combine using Apply pattern (As() converts K<Validation<Error>, T> to Validation<Error, T>)
        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            combined.ShouldHaveOnlyDomainError<DummyValueObject, string>(new DomainErrorType.Empty()));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveDomainErrors<TValueObject, T>

    [Fact]
    public void Validation_ShouldHaveDomainErrors_ReturnsSuccess_WhenAllExpectedErrorsPresent()
    {
        // Arrange - Use Apply pattern to combine errors
        var error1 = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");
        var error2 = DomainError.For<DummyValueObject>(
            new DomainErrorType.InvalidFormat(),
            currentValue: "",
            message: "Invalid format");

        Validation<Error, string> validation1 = error1;
        Validation<Error, string> validation2 = error2;

        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert (should not throw)
        combined.ShouldHaveDomainErrors<DummyValueObject, string>(
            new DomainErrorType.Empty(),
            new DomainErrorType.InvalidFormat());
    }

    [Fact]
    public void Validation_ShouldHaveDomainErrors_ThrowsException_WhenExpectedErrorMissing()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Value cannot be empty");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveDomainErrors<DummyValueObject, string>(
                new DomainErrorType.Empty(),
                new DomainErrorType.InvalidFormat())); // InvalidFormat not present
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveDomainError<TValueObject, T, TValue>

    [Fact]
    public void Validation_ShouldHaveDomainError_WithValue_ReturnsSuccess_WhenErrorAndValueMatch()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject, int>(
            new DomainErrorType.Negative(),
            currentValue: -5,
            message: "Value cannot be negative");
        Validation<Error, int> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveDomainError<DummyValueObject, int, int>(
            new DomainErrorType.Negative(),
            expectedCurrentValue: -5);
    }

    [Fact]
    public void Validation_ShouldHaveDomainError_WithValue_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var error = DomainError.For<DummyValueObject, int>(
            new DomainErrorType.Negative(),
            currentValue: -5,
            message: "Value cannot be negative");
        Validation<Error, int> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveDomainError<DummyValueObject, int, int>(
                new DomainErrorType.Negative(),
                expectedCurrentValue: -10));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveDomainError<TValueObject, T, T1, T2>

    [Fact]
    public void Validation_ShouldHaveDomainError_WithTwoValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);
        var error = DomainError.For<DummyValueObject, DateTime, DateTime>(
            new DomainErrorType.Custom("StartAfterEnd"),
            startDate,
            endDate,
            message: "Start date cannot be after end date");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveDomainError<DummyValueObject, string, DateTime, DateTime>(
            new DomainErrorType.Custom("StartAfterEnd"),
            expectedValue1: startDate,
            expectedValue2: endDate);
    }

    [Fact]
    public void Validation_ShouldHaveDomainError_WithTwoValues_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);
        var error = DomainError.For<DummyValueObject, DateTime, DateTime>(
            new DomainErrorType.Custom("StartAfterEnd"),
            startDate,
            endDate,
            message: "Start date cannot be after end date");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveDomainError<DummyValueObject, string, DateTime, DateTime>(
                new DomainErrorType.Custom("StartAfterEnd"),
                expectedValue1: new DateTime(2024, 1, 1),
                expectedValue2: endDate));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveDomainError<TValueObject, T, T1, T2, T3>

    [Fact]
    public void Validation_ShouldHaveDomainError_WithThreeValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var duration = 365;
        var error = DomainError.For<DummyValueObject, DateTime, DateTime, int>(
            new DomainErrorType.Custom("DateRangeWithDuration"),
            startDate,
            endDate,
            duration,
            message: "Date range spans 365 days");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveDomainError<DummyValueObject, string, DateTime, DateTime, int>(
            new DomainErrorType.Custom("DateRangeWithDuration"),
            expectedValue1: startDate,
            expectedValue2: endDate,
            expectedValue3: duration);
    }

    [Fact]
    public void Validation_ShouldHaveDomainError_WithThreeValues_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var duration = 365;
        var error = DomainError.For<DummyValueObject, DateTime, DateTime, int>(
            new DomainErrorType.Custom("DateRangeWithDuration"),
            startDate,
            endDate,
            duration,
            message: "Date range spans 365 days");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveDomainError<DummyValueObject, string, DateTime, DateTime, int>(
                new DomainErrorType.Custom("DateRangeWithDuration"),
                expectedValue1: startDate,
                expectedValue2: endDate,
                expectedValue3: 366)); // Wrong value
    }

    #endregion

    #region Type Safety - Different Value Objects

    public sealed class EmailValueObject : SimpleValueObject<string>
    {
        private EmailValueObject(string value) : base(value) { }
    }

    public sealed class PasswordValueObject : SimpleValueObject<string>
    {
        private PasswordValueObject(string value) : base(value) { }
    }

    [Fact]
    public void ShouldBeDomainError_ThrowsException_WhenValueObjectTypeMismatch()
    {
        // Arrange - Error is for EmailValueObject
        var error = DomainError.For<EmailValueObject>(
            new DomainErrorType.Empty(),
            currentValue: "",
            message: "Email cannot be empty");

        // Act & Assert - Checking for PasswordValueObject should fail
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeDomainError<PasswordValueObject>(new DomainErrorType.Empty()));
    }

    [Fact]
    public void ShouldBeDomainError_ReturnsSuccess_WhenValueObjectTypeMatches()
    {
        // Arrange
        var error = DomainError.For<EmailValueObject>(
            new DomainErrorType.InvalidFormat(),
            currentValue: "invalid",
            message: "Invalid email format");

        // Act & Assert (should not throw)
        error.ShouldBeDomainError<EmailValueObject>(new DomainErrorType.InvalidFormat());
    }

    #endregion
}
