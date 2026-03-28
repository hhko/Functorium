namespace FinanceDomain.Tests.Unit;

/// <summary>
/// AccountNumber 값 객체 테스트
///
/// 학습 목표:
/// 1. 형식 검증 (은행코드-계좌번호)
/// 2. 파생 속성 검증 (BankCode, Number, Masked)
/// </summary>
[Trait("Part5-Finance-Domain", "AccountNumberTests")]
public class AccountNumberTests
{
    #region 생성 테스트

    [Theory]
    [InlineData("110-1234567890")]
    [InlineData("020-12345678901234")]
    public void Create_ReturnsSuccess_WhenFormatIsValid(string accountNumber)
    {
        // Act
        var actual = AccountNumber.Create(accountNumber);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("1101234567890")]
    [InlineData("11-1234567890")]
    [InlineData("110-123456789")]
    public void Create_ReturnsFail_WhenFormatIsInvalid(string accountNumber)
    {
        // Act
        var actual = AccountNumber.Create(accountNumber);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_NormalizesSpaces()
    {
        // Act
        var actual = AccountNumber.Create("110 1234567890");

        // Assert - 공백은 제거되지만 형식이 맞지 않아 실패
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 파생 속성 테스트

    [Fact]
    public void BankCode_ReturnsFirst3Digits()
    {
        // Arrange
        var account = AccountNumber.Create("110-1234567890").Match(a => a, _ => null!);

        // Act & Assert
        account.BankCode.ShouldBe("110");
    }

    [Fact]
    public void Number_ReturnsAccountPart()
    {
        // Arrange
        var account = AccountNumber.Create("110-1234567890").Match(a => a, _ => null!);

        // Act & Assert
        account.Number.ShouldBe("1234567890");
    }

    [Fact]
    public void Masked_ReturnsPartiallyHiddenNumber()
    {
        // Arrange
        var account = AccountNumber.Create("110-1234567890").Match(a => a, _ => null!);

        // Act & Assert
        account.Masked.ShouldBe("110-****7890");
    }

    #endregion

    #region 동등성 테스트

    [Fact]
    public void Equals_ReturnsTrue_WhenNumbersMatch()
    {
        // Arrange
        var account1 = AccountNumber.Create("110-1234567890").Match(a => a, _ => null!);
        var account2 = AccountNumber.Create("110-1234567890").Match(a => a, _ => null!);

        // Act & Assert
        account1.Equals(account2).ShouldBeTrue();
    }

    #endregion
}
