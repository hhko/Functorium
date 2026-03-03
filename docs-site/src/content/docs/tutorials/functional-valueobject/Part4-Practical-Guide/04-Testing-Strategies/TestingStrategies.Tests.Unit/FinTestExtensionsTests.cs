using LanguageExt;
using LanguageExt.Common;

namespace TestingStrategies.Tests.Unit;

/// <summary>
/// Fin<T> 테스트 헬퍼 검증
///
/// 테스트 목적:
/// 1. ShouldBeSuccess 헬퍼 동작 검증
/// 2. ShouldBeFail 헬퍼 동작 검증
/// 3. GetSuccessValue/GetFailError 헬퍼 검증
/// </summary>
[Trait("Part4-Testing-Strategies", "FinTestExtensionsTests")]
public class FinTestExtensionsTests
{
    #region ShouldBeSuccess 테스트

    [Fact]
    public void ShouldBeSuccess_DoesNotThrow_WhenFinIsSucc()
    {
        // Arrange
        Fin<string> fin = "성공 값";

        // Act & Assert
        Should.NotThrow(() => fin.ShouldBeSuccess());
    }

    [Fact]
    public void ShouldBeSuccess_Throws_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Error.New("실패");

        // Act & Assert
        Should.Throw<Exception>(() => fin.ShouldBeSuccess());
    }

    #endregion

    #region ShouldBeFail 테스트

    [Fact]
    public void ShouldBeFail_DoesNotThrow_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Error.New("실패");

        // Act & Assert
        Should.NotThrow(() => fin.ShouldBeFail());
    }

    [Fact]
    public void ShouldBeFail_Throws_WhenFinIsSucc()
    {
        // Arrange
        Fin<string> fin = "성공 값";

        // Act & Assert
        Should.Throw<Exception>(() => fin.ShouldBeFail());
    }

    #endregion

    #region GetSuccessValue 테스트

    [Fact]
    public void GetSuccessValue_ReturnsValue_WhenFinIsSucc()
    {
        // Arrange
        Fin<string> fin = "성공 값";

        // Act
        var actual = fin.GetSuccessValue();

        // Assert
        actual.ShouldBe("성공 값");
    }

    [Fact]
    public void GetSuccessValue_Throws_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Error.New("실패");

        // Act & Assert
        Should.Throw<Exception>(() => fin.GetSuccessValue());
    }

    #endregion

    #region GetFailError 테스트

    [Fact]
    public void GetFailError_ReturnsError_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Error.New("오류 메시지");

        // Act
        var actual = fin.GetFailError();

        // Assert
        actual.Message.ShouldBe("오류 메시지");
    }

    [Fact]
    public void GetFailError_Throws_WhenFinIsSucc()
    {
        // Arrange
        Fin<string> fin = "성공 값";

        // Act & Assert
        Should.Throw<Exception>(() => fin.GetFailError());
    }

    #endregion

    #region 통합 사용 예시 테스트

    [Fact]
    public void EmailCreation_CanBeTestedWithHelpers()
    {
        // Arrange
        var validEmail = Email.Create("user@example.com");
        var invalidEmail = Email.Create("invalid");

        // Act & Assert - 성공 케이스
        validEmail.ShouldBeSuccess();
        var email = validEmail.GetSuccessValue();
        ((string)email).ShouldBe("user@example.com");

        // Act & Assert - 실패 케이스
        invalidEmail.ShouldBeFail();
        var error = invalidEmail.GetFailError();
        error.Message.ShouldContain("Invalid email format");
    }

    #endregion
}
