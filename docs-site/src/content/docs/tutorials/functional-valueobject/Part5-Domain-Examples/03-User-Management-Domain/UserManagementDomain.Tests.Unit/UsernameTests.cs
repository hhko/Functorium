namespace UserManagementDomain.Tests.Unit;

/// <summary>
/// Username 값 객체 테스트
///
/// 학습 목표:
/// 1. 사용자명 형식 검증 (영문자로 시작)
/// 2. 길이 검증 (3-30자)
/// 3. 예약어 검증
/// </summary>
[Trait("Part5-User-Management-Domain", "UsernameTests")]
public class UsernameTests
{
    #region 생성 테스트

    [Theory]
    [InlineData("john")]
    [InlineData("john_doe")]
    [InlineData("john-doe123")]
    [InlineData("a12")]
    public void Create_ReturnsSuccess_WhenFormatIsValid(string username)
    {
        // Act
        var actual = Username.Create(username);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_NormalizesToLowerCase()
    {
        // Act
        var actual = Username.Create("JohnDoe");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: u => u.Name.ShouldBe("johndoe"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Create_ReturnsFail_WhenTooShort(string username)
    {
        // Act
        var actual = Username.Create(username);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenTooLong()
    {
        // Arrange - 31자
        var username = "a" + new string('b', 30);

        // Act
        var actual = Username.Create(username);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("123start")]
    [InlineData("_underscore")]
    [InlineData("-hyphen")]
    public void Create_ReturnsFail_WhenNotStartingWithLetter(string username)
    {
        // Act
        var actual = Username.Create(username);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("john@doe")]
    [InlineData("john.doe")]
    [InlineData("john doe")]
    public void Create_ReturnsFail_WhenContainsInvalidCharacters(string username)
    {
        // Act
        var actual = Username.Create(username);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 예약어 테스트

    [Theory]
    [InlineData("admin")]
    [InlineData("administrator")]
    [InlineData("root")]
    [InlineData("system")]
    [InlineData("ADMIN")]
    public void Create_ReturnsFail_WhenUsernameIsReserved(string username)
    {
        // Act
        var actual = Username.Create(username);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("reserved")
        );
    }

    #endregion

    #region 동등성 테스트

    [Fact]
    public void Equals_ReturnsTrue_WhenUsernamesMatch()
    {
        // Arrange
        var username1 = Username.Create("johndoe").Match(u => u, _ => null!);
        var username2 = Username.Create("JOHNDOE").Match(u => u, _ => null!);

        // Act & Assert - 소문자로 정규화되므로 동일
        username1.Equals(username2).ShouldBeTrue();
    }

    #endregion
}
