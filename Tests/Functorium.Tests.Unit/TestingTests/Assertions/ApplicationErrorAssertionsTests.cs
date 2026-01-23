using Functorium.Applications.Errors;
using Functorium.Testing.Assertions.Errors;
using LanguageExt;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.TestingTests.Assertions;

// 테스트용 더미 유스케이스
public sealed class DummyUsecase { }

[Trait(nameof(UnitTest), UnitTest.Functorium_Testing)]
public class ApplicationErrorAssertionsTests
{
    #region Error - ShouldBeApplicationError<TUsecase>

    [Fact]
    public void ShouldBeApplicationError_ReturnsSuccess_WhenErrorMatchesExpectedType()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "test-id",
            message: "Already exists");

        // Act & Assert (should not throw)
        error.ShouldBeApplicationError<DummyUsecase>(new ApplicationErrorType.AlreadyExists());
    }

    [Fact]
    public void ShouldBeApplicationError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "test-id",
            message: "Already exists");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeApplicationError<DummyUsecase>(new ApplicationErrorType.NotFound()));
    }

    [Fact]
    public void ShouldBeApplicationError_ReturnsSuccess_WhenCustomErrorMatches()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.Custom("CannotProcess"),
            currentValue: "test",
            message: "Cannot process");

        // Act & Assert (should not throw)
        error.ShouldBeApplicationError<DummyUsecase>(new ApplicationErrorType.Custom("CannotProcess"));
    }

    [Fact]
    public void ShouldBeApplicationError_ReturnsSuccess_WhenValidationFailedMatches()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.ValidationFailed("Name"),
            currentValue: "",
            message: "Name is required");

        // Act & Assert (should not throw)
        error.ShouldBeApplicationError<DummyUsecase>(new ApplicationErrorType.ValidationFailed("Name"));
    }

    #endregion

    #region Error - ShouldBeApplicationError<TUsecase, TValue>

    [Fact]
    public void ShouldBeApplicationError_WithValue_ReturnsSuccess_WhenErrorAndValueMatch()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase, Guid>(
            new ApplicationErrorType.NotFound(),
            currentValue: Guid.Empty,
            message: "Entity not found");

        // Act & Assert (should not throw)
        error.ShouldBeApplicationError<DummyUsecase, Guid>(
            new ApplicationErrorType.NotFound(),
            expectedCurrentValue: Guid.Empty);
    }

    [Fact]
    public void ShouldBeApplicationError_WithValue_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var actualId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid>(
            new ApplicationErrorType.NotFound(),
            currentValue: actualId,
            message: "Entity not found");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeApplicationError<DummyUsecase, Guid>(
                new ApplicationErrorType.NotFound(),
                expectedCurrentValue: Guid.Empty));
    }

    #endregion

    #region Error - ShouldBeApplicationError<TUsecase, T1, T2>

    [Fact]
    public void ShouldBeApplicationError_WithTwoValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid, Guid>(
            new ApplicationErrorType.Custom("ProductNotInOrder"),
            orderId,
            productId,
            message: "Product not in order");

        // Act & Assert (should not throw)
        error.ShouldBeApplicationError<DummyUsecase, Guid, Guid>(
            new ApplicationErrorType.Custom("ProductNotInOrder"),
            expectedValue1: orderId,
            expectedValue2: productId);
    }

    [Fact]
    public void ShouldBeApplicationError_WithTwoValues_ThrowsException_WhenValue1DoesNotMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid, Guid>(
            new ApplicationErrorType.Custom("ProductNotInOrder"),
            orderId,
            productId,
            message: "Product not in order");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeApplicationError<DummyUsecase, Guid, Guid>(
                new ApplicationErrorType.Custom("ProductNotInOrder"),
                expectedValue1: Guid.Empty,
                expectedValue2: productId));
    }

    #endregion

    #region Error - ShouldBeApplicationError<TUsecase, T1, T2, T3>

    [Fact]
    public void ShouldBeApplicationError_WithThreeValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 10;
        var error = ApplicationError.For<DummyUsecase, Guid, Guid, int>(
            new ApplicationErrorType.Custom("InsufficientStock"),
            orderId,
            productId,
            quantity,
            message: "Insufficient stock");

        // Act & Assert (should not throw)
        error.ShouldBeApplicationError<DummyUsecase, Guid, Guid, int>(
            new ApplicationErrorType.Custom("InsufficientStock"),
            expectedValue1: orderId,
            expectedValue2: productId,
            expectedValue3: quantity);
    }

    [Fact]
    public void ShouldBeApplicationError_WithThreeValues_ThrowsException_WhenValue3DoesNotMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 10;
        var error = ApplicationError.For<DummyUsecase, Guid, Guid, int>(
            new ApplicationErrorType.Custom("InsufficientStock"),
            orderId,
            productId,
            quantity,
            message: "Insufficient stock");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeApplicationError<DummyUsecase, Guid, Guid, int>(
                new ApplicationErrorType.Custom("InsufficientStock"),
                expectedValue1: orderId,
                expectedValue2: productId,
                expectedValue3: 20)); // Wrong value
    }

    #endregion

    #region Fin<T> - ShouldBeApplicationError<TUsecase, T>

    [Fact]
    public void Fin_ShouldBeApplicationError_ReturnsSuccess_WhenFinFailsWithExpectedError()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "test-id",
            message: "Already exists");
        Fin<string> fin = error;

        // Act & Assert (should not throw)
        fin.ShouldBeApplicationError<DummyUsecase, string>(new ApplicationErrorType.AlreadyExists());
    }

    [Fact]
    public void Fin_ShouldBeApplicationError_ThrowsException_WhenFinSucceeds()
    {
        // Arrange
        Fin<string> fin = "success";

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldBeApplicationError<DummyUsecase, string>(new ApplicationErrorType.AlreadyExists()));
    }

    [Fact]
    public void Fin_ShouldBeApplicationError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "test-id",
            message: "Already exists");
        Fin<string> fin = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            fin.ShouldBeApplicationError<DummyUsecase, string>(new ApplicationErrorType.NotFound()));
    }

    [Fact]
    public void Fin_ShouldBeApplicationError_WithValue_ReturnsSuccess_WhenFinFailsWithExpectedErrorAndValue()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid>(
            new ApplicationErrorType.NotFound(),
            currentValue: entityId,
            message: "Entity not found");
        Fin<Guid> fin = error;

        // Act & Assert (should not throw)
        fin.ShouldBeApplicationError<DummyUsecase, Guid, Guid>(
            new ApplicationErrorType.NotFound(),
            expectedCurrentValue: entityId);
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveApplicationError<TUsecase, T>

    [Fact]
    public void Validation_ShouldHaveApplicationError_ReturnsSuccess_WhenValidationFailsWithExpectedError()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "test-id",
            message: "Already exists");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveApplicationError<DummyUsecase, string>(new ApplicationErrorType.AlreadyExists());
    }

    [Fact]
    public void Validation_ShouldHaveApplicationError_ThrowsException_WhenValidationSucceeds()
    {
        // Arrange
        Validation<Error, string> validation = "success";

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveApplicationError<DummyUsecase, string>(new ApplicationErrorType.AlreadyExists()));
    }

    [Fact]
    public void Validation_ShouldHaveApplicationError_ThrowsException_WhenErrorTypeDoesNotMatch()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "test-id",
            message: "Already exists");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveApplicationError<DummyUsecase, string>(new ApplicationErrorType.NotFound()));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveOnlyApplicationError<TUsecase, T>

    [Fact]
    public void Validation_ShouldHaveOnlyApplicationError_ReturnsSuccess_WhenExactlyOneErrorMatches()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.ConcurrencyConflict(),
            currentValue: "version-1",
            message: "Concurrency conflict");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveOnlyApplicationError<DummyUsecase, string>(new ApplicationErrorType.ConcurrencyConflict());
    }

    [Fact]
    public void Validation_ShouldHaveOnlyApplicationError_ThrowsException_WhenMultipleErrors()
    {
        // Arrange - Use Apply pattern to combine errors
        var error1 = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "id-1",
            message: "Already exists");
        var error2 = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.ValidationFailed("Name"),
            currentValue: "",
            message: "Name is required");

        Validation<Error, string> validation1 = error1;
        Validation<Error, string> validation2 = error2;

        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            combined.ShouldHaveOnlyApplicationError<DummyUsecase, string>(new ApplicationErrorType.AlreadyExists()));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveApplicationErrors<TUsecase, T>

    [Fact]
    public void Validation_ShouldHaveApplicationErrors_ReturnsSuccess_WhenAllExpectedErrorsPresent()
    {
        // Arrange - Use Apply pattern to combine errors
        var error1 = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "id-1",
            message: "Already exists");
        var error2 = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.ValidationFailed("Name"),
            currentValue: "",
            message: "Name is required");

        Validation<Error, string> validation1 = error1;
        Validation<Error, string> validation2 = error2;

        var combined = (validation1, validation2).Apply((a, b) => a + b).As();

        // Act & Assert (should not throw)
        combined.ShouldHaveApplicationErrors<DummyUsecase, string>(
            new ApplicationErrorType.AlreadyExists(),
            new ApplicationErrorType.ValidationFailed("Name"));
    }

    [Fact]
    public void Validation_ShouldHaveApplicationErrors_ThrowsException_WhenExpectedErrorMissing()
    {
        // Arrange
        var error = ApplicationError.For<DummyUsecase>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "id-1",
            message: "Already exists");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveApplicationErrors<DummyUsecase, string>(
                new ApplicationErrorType.AlreadyExists(),
                new ApplicationErrorType.ValidationFailed())); // ValidationFailed not present
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveApplicationError<TUsecase, T, TValue>

    [Fact]
    public void Validation_ShouldHaveApplicationError_WithValue_ReturnsSuccess_WhenErrorAndValueMatch()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid>(
            new ApplicationErrorType.NotFound(),
            currentValue: entityId,
            message: "Entity not found");
        Validation<Error, Guid> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveApplicationError<DummyUsecase, Guid, Guid>(
            new ApplicationErrorType.NotFound(),
            expectedCurrentValue: entityId);
    }

    [Fact]
    public void Validation_ShouldHaveApplicationError_WithValue_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid>(
            new ApplicationErrorType.NotFound(),
            currentValue: entityId,
            message: "Entity not found");
        Validation<Error, Guid> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveApplicationError<DummyUsecase, Guid, Guid>(
                new ApplicationErrorType.NotFound(),
                expectedCurrentValue: Guid.Empty));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveApplicationError<TUsecase, T, T1, T2>

    [Fact]
    public void Validation_ShouldHaveApplicationError_WithTwoValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid, Guid>(
            new ApplicationErrorType.Custom("ProductNotInOrder"),
            orderId,
            productId,
            message: "Product not in order");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveApplicationError<DummyUsecase, string, Guid, Guid>(
            new ApplicationErrorType.Custom("ProductNotInOrder"),
            expectedValue1: orderId,
            expectedValue2: productId);
    }

    [Fact]
    public void Validation_ShouldHaveApplicationError_WithTwoValues_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var error = ApplicationError.For<DummyUsecase, Guid, Guid>(
            new ApplicationErrorType.Custom("ProductNotInOrder"),
            orderId,
            productId,
            message: "Product not in order");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveApplicationError<DummyUsecase, string, Guid, Guid>(
                new ApplicationErrorType.Custom("ProductNotInOrder"),
                expectedValue1: Guid.Empty,
                expectedValue2: productId));
    }

    #endregion

    #region Validation<Error, T> - ShouldHaveApplicationError<TUsecase, T, T1, T2, T3>

    [Fact]
    public void Validation_ShouldHaveApplicationError_WithThreeValues_ReturnsSuccess_WhenErrorAndValuesMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 10;
        var error = ApplicationError.For<DummyUsecase, Guid, Guid, int>(
            new ApplicationErrorType.Custom("InsufficientStock"),
            orderId,
            productId,
            quantity,
            message: "Insufficient stock");
        Validation<Error, string> validation = error;

        // Act & Assert (should not throw)
        validation.ShouldHaveApplicationError<DummyUsecase, string, Guid, Guid, int>(
            new ApplicationErrorType.Custom("InsufficientStock"),
            expectedValue1: orderId,
            expectedValue2: productId,
            expectedValue3: quantity);
    }

    [Fact]
    public void Validation_ShouldHaveApplicationError_WithThreeValues_ThrowsException_WhenValueDoesNotMatch()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 10;
        var error = ApplicationError.For<DummyUsecase, Guid, Guid, int>(
            new ApplicationErrorType.Custom("InsufficientStock"),
            orderId,
            productId,
            quantity,
            message: "Insufficient stock");
        Validation<Error, string> validation = error;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            validation.ShouldHaveApplicationError<DummyUsecase, string, Guid, Guid, int>(
                new ApplicationErrorType.Custom("InsufficientStock"),
                expectedValue1: orderId,
                expectedValue2: productId,
                expectedValue3: 20)); // Wrong value
    }

    #endregion

    #region Type Safety - Different Usecases

    public sealed class CreateProductCommand { }

    public sealed class UpdateProductCommand { }

    [Fact]
    public void ShouldBeApplicationError_ThrowsException_WhenUsecaseTypeMismatch()
    {
        // Arrange - Error is for CreateProductCommand
        var error = ApplicationError.For<CreateProductCommand>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "product-id",
            message: "Product already exists");

        // Act & Assert - Checking for UpdateProductCommand should fail
        Should.Throw<ShouldAssertException>(() =>
            error.ShouldBeApplicationError<UpdateProductCommand>(new ApplicationErrorType.AlreadyExists()));
    }

    [Fact]
    public void ShouldBeApplicationError_ReturnsSuccess_WhenUsecaseTypeMatches()
    {
        // Arrange
        var error = ApplicationError.For<CreateProductCommand>(
            new ApplicationErrorType.AlreadyExists(),
            currentValue: "product-id",
            message: "Product already exists");

        // Act & Assert (should not throw)
        error.ShouldBeApplicationError<CreateProductCommand>(new ApplicationErrorType.AlreadyExists());
    }

    #endregion
}
