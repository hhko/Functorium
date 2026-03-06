using LanguageExt.Common;
using Shouldly;

namespace FinResponseFactoryCrtp.Tests.Unit;

public class FinResponseFactoryCrtpTests
{
    [Fact]
    public void CreateFail_ReturnsFail_WhenErrorProvided()
    {
        // Arrange
        var error = Error.New("validation error");

        // Act
        var actual = FactoryResponse<string>.CreateFail(error);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ValidateAndCreate_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = ValidationPipelineExample.ValidateAndCreate(
            isValid: true,
            onSuccess: () => FactoryResponse<string>.Succ("OK"),
            errorMessage: "error");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Value.ShouldBe("OK");
    }

    [Fact]
    public void ValidateAndCreate_ReturnsFail_WhenInvalid()
    {
        // Act
        var actual = ValidationPipelineExample.ValidateAndCreate(
            isValid: false,
            onSuccess: () => FactoryResponse<string>.Succ("OK"),
            errorMessage: "validation failed");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
