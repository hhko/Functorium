namespace UserManagementDomain.Tests.Unit;

/// <summary>
/// Email 값 객체 테스트
///
/// 학습 목표:
/// 1. 이메일 형식 검증 (정규식)
/// 2. 정규화 검증 (소문자 변환)
/// 3. 파생 속성 검증 (LocalPart, Domain, Masked)
/// </summary>
[Trait("Part5-User-Management-Domain", "EmailTests")]
public class EmailTests
{
    #region 생성 테스트

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.kr")]
    [InlineData("admin123@company.org")]
    public void Create_ReturnsSuccess_WhenFormatIsValid(string email)
    {
        // Act
        var actual = Email.Create(email);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_NormalizesToLowerCase()
    {
        // Act
        var actual = Email.Create("User@EXAMPLE.COM");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: e => e.Value.ShouldBe("user@example.com"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@.com")]
    public void Create_ReturnsFail_WhenFormatIsInvalid(string email)
    {
        // Act
        var actual = Email.Create(email);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenTooLong()
    {
        // Arrange - 255자 이상
        var longEmail = new string('a', 250) + "@example.com";

        // Act
        var actual = Email.Create(longEmail);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 파생 속성 테스트

    [Fact]
    public void LocalPart_ReturnsPartBeforeAt()
    {
        // Arrange
        var email = Email.Create("user@example.com").Match(e => e, _ => null!);

        // Act & Assert
        email.LocalPart.ShouldBe("user");
    }

    [Fact]
    public void Domain_ReturnsPartAfterAt()
    {
        // Arrange
        var email = Email.Create("user@example.com").Match(e => e, _ => null!);

        // Act & Assert
        email.Domain.ShouldBe("example.com");
    }

    [Fact]
    public void Masked_ReturnsPartiallyHiddenEmail()
    {
        // Arrange
        var email = Email.Create("user@example.com").Match(e => e, _ => null!);

        // Act & Assert
        email.Masked.ShouldBe("u***r@example.com");
    }

    [Fact]
    public void Masked_HandlesShortLocalPart()
    {
        // Arrange
        var email = Email.Create("ab@example.com").Match(e => e, _ => null!);

        // Act & Assert
        email.Masked.ShouldBe("**@example.com");
    }

    #endregion
}
