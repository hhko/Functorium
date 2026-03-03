using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace CreateOnlyConstraint.Tests.Unit;

public class CreateOnlyConstraintTests
{
    [Fact]
    public void Validate_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var sut = new SimpleValidationPipeline<FinResponse<string>>();

        // Act
        var actual = sut.Validate(
            isValid: true,
            onSuccess: () => FinResponse.Succ("OK"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsFail_WhenInvalid()
    {
        // Arrange
        var sut = new SimpleValidationPipeline<FinResponse<string>>();

        // Act
        var actual = sut.Validate(
            isValid: false,
            onSuccess: () => FinResponse.Succ("OK"));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Execute_ReturnsSuccess_WhenNoException()
    {
        // Arrange
        var sut = new SimpleExceptionPipeline<FinResponse<string>>();

        // Act
        var actual = sut.Execute(() => FinResponse.Succ("OK"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Execute_ReturnsFail_WhenExceptionThrown()
    {
        // Arrange
        var sut = new SimpleExceptionPipeline<FinResponse<string>>();

        // Act
        var actual = sut.Execute(() => throw new InvalidOperationException("boom"));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
