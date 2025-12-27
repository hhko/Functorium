using LanguageExt;

namespace FunctoriumFramework.Tests.Unit;

/// <summary>
/// Email 값 객체 테스트 (SimpleValueObject 기반)
///
/// 테스트 목적:
/// 1. 값 객체 생성 검증 (Create 메서드)
/// 2. 동등성 비교 검증 (Equals, GetHashCode)
/// 3. 에러 코드 검증 (ErrorCodeFactory 통합)
/// </summary>
[Trait("Part4-Functorium-Framework", "EmailTests")]
public class EmailTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenEmailIsValid()
    {
        // Arrange
        var emailAddress = "user@example.com";

        // Act
        var actual = Email.Create(emailAddress);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: email => email.Value.ShouldBe(emailAddress),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void Create_NormalizesToLowercase_WhenEmailIsValid()
    {
        // Arrange
        var emailAddress = "User@EXAMPLE.COM";

        // Act
        var actual = Email.Create(emailAddress);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: email => email.Value.ShouldBe("user@example.com"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ReturnsFail_WhenEmailIsEmpty(string? emailAddress)
    {
        // Act
        var actual = Email.Create(emailAddress!);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Email.Empty")
        );
    }

    [Fact]
    public void Create_ReturnsFail_WhenEmailHasNoAtSymbol()
    {
        // Arrange
        var emailAddress = "invalid-email";

        // Act
        var actual = Email.Create(emailAddress);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Email.InvalidFormat")
        );
    }

    #endregion

    #region 동등성 테스트

    [Fact]
    public void Equals_ReturnsTrue_WhenEmailsHaveSameValue()
    {
        // Arrange
        var email1 = Email.Create("user@example.com").Match(e => e, _ => null!);
        var email2 = Email.Create("user@example.com").Match(e => e, _ => null!);

        // Act & Assert
        email1.Equals(email2).ShouldBeTrue();
        (email1 == email2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenEmailsHaveDifferentValues()
    {
        // Arrange
        var email1 = Email.Create("user1@example.com").Match(e => e, _ => null!);
        var email2 = Email.Create("user2@example.com").Match(e => e, _ => null!);

        // Act & Assert
        email1.Equals(email2).ShouldBeFalse();
        (email1 != email2).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_WhenEmailsAreEqual()
    {
        // Arrange
        var email1 = Email.Create("user@example.com").Match(e => e, _ => null!);
        var email2 = Email.Create("USER@EXAMPLE.COM").Match(e => e, _ => null!);

        // Act & Assert
        email1.GetHashCode().ShouldBe(email2.GetHashCode());
    }

    #endregion

    #region 암시적 변환 테스트

    [Fact]
    public void ImplicitConversion_ReturnsValue_WhenConvertedToString()
    {
        // Arrange
        var email = Email.Create("user@example.com").Match(e => e, _ => null!);

        // Act
        string value = email;

        // Assert
        value.ShouldBe("user@example.com");
    }

    #endregion
}
