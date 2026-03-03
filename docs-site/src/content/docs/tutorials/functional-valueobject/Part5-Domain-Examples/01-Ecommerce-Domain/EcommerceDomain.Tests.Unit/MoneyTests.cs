namespace EcommerceDomain.Tests.Unit;

/// <summary>
/// Money 값 객체 테스트
///
/// 학습 목표:
/// 1. 복합 값 객체 생성 검증 (Amount + Currency)
/// 2. 산술 연산 검증 (Add, Subtract, Multiply)
/// 3. 통화 불일치 예외 처리 확인
/// </summary>
[Trait("Part5-Ecommerce-Domain", "MoneyTests")]
public class MoneyTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenInputsAreValid()
    {
        // Act
        var actual = Money.Create(10000, "KRW");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: m =>
            {
                m.Amount.ShouldBe(10000);
                m.Currency.ShouldBe("KRW");
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void Create_NormalizesCurrency_ToUpperCase()
    {
        // Act
        var actual = Money.Create(100, "usd");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: m => m.Currency.ShouldBe("USD"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void Create_ReturnsFail_WhenAmountIsNegative()
    {
        // Act
        var actual = Money.Create(-100, "KRW");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("KR")]
    [InlineData("KOREA")]
    public void Create_ReturnsFail_WhenCurrencyIsInvalid(string currency)
    {
        // Act
        var actual = Money.Create(100, currency);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 산술 연산 테스트

    [Fact]
    public void Add_ReturnsCombinedAmount_WhenCurrencyMatches()
    {
        // Arrange
        var money1 = Money.Create(1000, "KRW").Match(m => m, _ => null!);
        var money2 = Money.Create(500, "KRW").Match(m => m, _ => null!);

        // Act
        var actual = money1.Add(money2);

        // Assert
        actual.Amount.ShouldBe(1500);
        actual.Currency.ShouldBe("KRW");
    }

    [Fact]
    public void Add_ThrowsException_WhenCurrencyDiffers()
    {
        // Arrange
        var krw = Money.Create(1000, "KRW").Match(m => m, _ => null!);
        var usd = Money.Create(100, "USD").Match(m => m, _ => null!);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => krw.Add(usd));
    }

    [Fact]
    public void Subtract_ReturnsSubtractedAmount_WhenCurrencyMatches()
    {
        // Arrange
        var money1 = Money.Create(1000, "KRW").Match(m => m, _ => null!);
        var money2 = Money.Create(300, "KRW").Match(m => m, _ => null!);

        // Act
        var actual = money1.Subtract(money2);

        // Assert
        actual.Amount.ShouldBe(700);
    }

    [Fact]
    public void Multiply_ReturnsMultipliedAmount()
    {
        // Arrange
        var money = Money.Create(1000, "KRW").Match(m => m, _ => null!);

        // Act
        var actual = money.Multiply(3);

        // Assert
        actual.Amount.ShouldBe(3000);
    }

    #endregion

    #region 비교 테스트

    [Fact]
    public void CompareTo_ReturnsNegative_WhenAmountIsLess()
    {
        // Arrange
        var money1 = Money.Create(1000, "KRW").Match(m => m, _ => null!);
        var money2 = Money.Create(2000, "KRW").Match(m => m, _ => null!);

        // Act & Assert
        money1.CompareTo(money2).ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_ThrowsException_WhenCurrencyDiffers()
    {
        // Arrange
        var krw = Money.Create(1000, "KRW").Match(m => m, _ => null!);
        var usd = Money.Create(100, "USD").Match(m => m, _ => null!);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => krw.CompareTo(usd));
    }

    #endregion
}
