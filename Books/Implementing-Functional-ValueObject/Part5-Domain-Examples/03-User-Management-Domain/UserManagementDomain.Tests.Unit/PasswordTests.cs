namespace UserManagementDomain.Tests.Unit;

/// <summary>
/// Password 값 객체 테스트
///
/// 학습 목표:
/// 1. 비밀번호 강도 검증 (대문자, 소문자, 숫자, 특수문자)
/// 2. 길이 검증 (최소 8자, 최대 128자)
/// 3. 해시 저장 및 검증
/// </summary>
[Trait("Part5-User-Management-Domain", "PasswordTests")]
public class PasswordTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenPasswordIsStrong()
    {
        // Arrange - 대문자, 소문자, 숫자, 특수문자 포함
        var password = "MySecure@Pass123";

        // Act
        var actual = Password.Create(password);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Abcdefg1")]     // 대문자, 소문자, 숫자 (3가지)
    [InlineData("abcdefg1@")]    // 소문자, 숫자, 특수문자 (3가지)
    [InlineData("ABCDEFG1@")]    // 대문자, 숫자, 특수문자 (3가지)
    public void Create_ReturnsSuccess_WhenThreeTypesIncluded(string password)
    {
        // Act
        var actual = Password.Create(password);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("abcdefg")]
    public void Create_ReturnsFail_WhenTooShort(string password)
    {
        // Act
        var actual = Password.Create(password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenTooLong()
    {
        // Arrange - 129자
        var password = new string('A', 100) + new string('a', 20) + "12345@!!!";

        // Act
        var actual = Password.Create(password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("abcdefgh")]     // 소문자만
    [InlineData("ABCDEFGH")]     // 대문자만
    [InlineData("12345678")]     // 숫자만
    [InlineData("abcd1234")]     // 소문자, 숫자 (2가지만)
    public void Create_ReturnsFail_WhenTooWeak(string password)
    {
        // Act
        var actual = Password.Create(password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 검증 테스트

    [Fact]
    public void Verify_ReturnsTrue_WhenPasswordMatches()
    {
        // Arrange
        var plainText = "MySecure@Pass123";
        var password = Password.Create(plainText).Match(p => p, _ => null!);

        // Act
        var actual = password.Verify(plainText);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var password = Password.Create("MySecure@Pass123").Match(p => p, _ => null!);

        // Act
        var actual = password.Verify("WrongPassword");

        // Assert
        actual.ShouldBeFalse();
    }

    #endregion

    #region 표시 테스트

    [Fact]
    public void ToString_ReturnsMaskedString()
    {
        // Arrange
        var password = Password.Create("MySecure@Pass123").Match(p => p, _ => null!);

        // Act & Assert
        password.ToString().ShouldBe("********");
    }

    [Fact]
    public void Value_DoesNotContainPlainText()
    {
        // Arrange
        var plainText = "MySecure@Pass123";
        var password = Password.Create(plainText).Match(p => p, _ => null!);

        // Act & Assert
        password.Value.ShouldNotContain(plainText);
    }

    #endregion
}
