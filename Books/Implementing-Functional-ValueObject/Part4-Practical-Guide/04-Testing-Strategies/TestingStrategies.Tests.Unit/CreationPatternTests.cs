using LanguageExt;

namespace TestingStrategies.Tests.Unit;

/// <summary>
/// 생성 테스트 패턴 검증
///
/// 테스트 목적:
/// 1. 값 객체 생성 성공/실패 패턴 학습
/// 2. 에러 코드 검증 패턴 학습
/// 3. 경계값 테스트 패턴 학습
/// </summary>
[Trait("Part4-Testing-Strategies", "CreationPatternTests")]
public class CreationPatternTests
{
    #region 유효한 입력 테스트

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.kr")]
    [InlineData("admin@company.org")]
    public void Create_ReturnsSuccess_WhenEmailIsValid(string emailAddress)
    {
        // Act
        var actual = Email.Create(emailAddress);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    #endregion

    #region 유효하지 않은 입력 테스트

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
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("no-at-symbol")]
    [InlineData("missing@")]
    public void Create_ReturnsFail_WhenEmailFormatIsInvalid(string emailAddress)
    {
        // Act
        var actual = Email.Create(emailAddress);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 에러 코드 검증 테스트

    [Fact]
    public void Create_ReturnsEmptyError_WhenEmailIsNull()
    {
        // Act
        var actual = Email.Create(null!);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Email.Empty")
        );
    }

    [Fact]
    public void Create_ReturnsInvalidFormatError_WhenEmailHasNoAtSymbol()
    {
        // Act
        var actual = Email.Create("invalid-email");

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Email.InvalidFormat")
        );
    }

    #endregion

    #region 경계값 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenAgeIsZero()
    {
        // Act
        var actual = Age.Create(0);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsSuccess_WhenAgeIsMaximum()
    {
        // Act
        var actual = Age.Create(150);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenAgeIsJustBelowMinimum()
    {
        // Act
        var actual = Age.Create(-1);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenAgeIsJustAboveMaximum()
    {
        // Act
        var actual = Age.Create(151);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion
}
