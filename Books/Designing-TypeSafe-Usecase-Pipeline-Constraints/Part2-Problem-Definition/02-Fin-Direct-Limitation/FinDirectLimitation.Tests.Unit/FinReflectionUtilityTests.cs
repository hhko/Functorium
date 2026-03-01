using LanguageExt;
using LanguageExt.Common;

namespace FinDirectLimitation.Tests.Unit;

public class FinReflectionUtilityTests
{
    [Fact]
    public void IsSucc_ReturnsTrue_WhenFinIsSuccess()
    {
        // Arrange
        Fin<string> fin = "Hello";

        // Act
        var actual = FinReflectionUtility.IsSucc(fin);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSucc_ReturnsFalse_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Error.New("error");

        // Act
        var actual = FinReflectionUtility.IsSucc(fin);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void CreateFail_ThrowsException_WhenTypeIsNotGeneric()
    {
        // Arrange & Act & Assert
        Should.Throw<InvalidOperationException>(
            () => FinReflectionUtility.CreateFail<string>(Error.New("error")));
    }
}
