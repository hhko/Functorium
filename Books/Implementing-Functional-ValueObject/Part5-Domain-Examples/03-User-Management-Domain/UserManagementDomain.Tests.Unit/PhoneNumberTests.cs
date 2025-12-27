namespace UserManagementDomain.Tests.Unit;

/// <summary>
/// PhoneNumber 값 객체 테스트
///
/// 학습 목표:
/// 1. 전화번호 형식 검증
/// 2. 정규화 검증 (숫자만 추출)
/// 3. 파생 속성 검증 (Formatted, Masked)
/// </summary>
[Trait("Part5-User-Management-Domain", "PhoneNumberTests")]
public class PhoneNumberTests
{
    #region 생성 테스트

    [Theory]
    [InlineData("010-1234-5678")]
    [InlineData("01012345678")]
    [InlineData("010 1234 5678")]
    public void Create_ReturnsSuccess_WhenFormatIsValid(string phone)
    {
        // Act
        var actual = PhoneNumber.Create(phone);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_RemovesLeadingZero()
    {
        // Act
        var actual = PhoneNumber.Create("010-1234-5678");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: p => p.NationalNumber.ShouldBe("1012345678"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("123456789012")]
    public void Create_ReturnsFail_WhenLengthIsInvalid(string phone)
    {
        // Act
        var actual = PhoneNumber.Create(phone);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 속성 테스트

    [Fact]
    public void CountryCode_ReturnsDefaultValue()
    {
        // Arrange
        var phone = PhoneNumber.Create("010-1234-5678").Match(p => p, _ => null!);

        // Act & Assert
        phone.CountryCode.ShouldBe("82");
    }

    [Fact]
    public void Value_ReturnsInternationalFormat()
    {
        // Arrange
        var phone = PhoneNumber.Create("010-1234-5678").Match(p => p, _ => null!);

        // Act & Assert
        phone.Value.ShouldBe("+821012345678");
    }

    #endregion

    #region Formatted 테스트

    [Fact]
    public void Formatted_ReturnsKoreanFormat_For10DigitNumber()
    {
        // Arrange
        var phone = PhoneNumber.Create("010-123-5678").Match(p => p, _ => null!);

        // Act & Assert - 10자리
        phone.Formatted.ShouldBe("010-1235-678");
    }

    #endregion

    #region Masked 테스트

    [Fact]
    public void Masked_ReturnsPartiallyHiddenNumber()
    {
        // Arrange
        var phone = PhoneNumber.Create("010-1234-5678").Match(p => p, _ => null!);

        // Act & Assert
        phone.Masked.ShouldContain("***-****-");
        phone.Masked.ShouldEndWith("5678");
    }

    #endregion
}
