namespace FinanceDomain.Tests.Unit;

/// <summary>
/// ExchangeRate 값 객체 테스트
///
/// 학습 목표:
/// 1. 환율 생성 검증
/// 2. 통화 변환 검증 (Convert, ConvertBack)
/// 3. 역환율 검증 (Invert)
/// </summary>
[Trait("Part5-Finance-Domain", "ExchangeRateTests")]
public class ExchangeRateTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenInputsAreValid()
    {
        // Act
        var actual = ExchangeRate.Create("USD", "KRW", 1350.50m);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: r =>
            {
                r.BaseCurrency.ShouldBe("USD");
                r.QuoteCurrency.ShouldBe("KRW");
                r.Rate.ShouldBe(1350.50m);
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void Create_NormalizesCurrencies_ToUpperCase()
    {
        // Act
        var actual = ExchangeRate.Create("usd", "krw", 1350m);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: r =>
            {
                r.BaseCurrency.ShouldBe("USD");
                r.QuoteCurrency.ShouldBe("KRW");
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("", "KRW", 1350)]
    [InlineData("US", "KRW", 1350)]
    [InlineData("USD", "", 1350)]
    [InlineData("USD", "KR", 1350)]
    public void Create_ReturnsFail_WhenCurrencyIsInvalid(string baseCurrency, string quoteCurrency, decimal rate)
    {
        // Act
        var actual = ExchangeRate.Create(baseCurrency, quoteCurrency, rate);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ReturnsFail_WhenRateIsNotPositive(decimal rate)
    {
        // Act
        var actual = ExchangeRate.Create("USD", "KRW", rate);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenCurrenciesAreSame()
    {
        // Act
        var actual = ExchangeRate.Create("USD", "USD", 1.0m);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 변환 테스트

    [Fact]
    public void Convert_ReturnsCorrectAmount()
    {
        // Arrange
        var rate = ExchangeRate.Create("USD", "KRW", 1350m).Match(r => r, _ => null!);

        // Act
        var actual = rate.Convert(100);

        // Assert - 100 USD = 135,000 KRW
        actual.ShouldBe(135_000m);
    }

    [Fact]
    public void ConvertBack_ReturnsCorrectAmount()
    {
        // Arrange
        var rate = ExchangeRate.Create("USD", "KRW", 1350m).Match(r => r, _ => null!);

        // Act
        var actual = rate.ConvertBack(135_000);

        // Assert - 135,000 KRW = 100 USD
        actual.ShouldBe(100m);
    }

    #endregion

    #region 역환율 테스트

    [Fact]
    public void Invert_SwapsCurrencies()
    {
        // Arrange
        var rate = ExchangeRate.Create("USD", "KRW", 1350m).Match(r => r, _ => null!);

        // Act
        var inverted = rate.Invert();

        // Assert
        inverted.BaseCurrency.ShouldBe("KRW");
        inverted.QuoteCurrency.ShouldBe("USD");
    }

    [Fact]
    public void Invert_InvertsRate()
    {
        // Arrange
        var rate = ExchangeRate.Create("USD", "KRW", 1350m).Match(r => r, _ => null!);

        // Act
        var inverted = rate.Invert();

        // Assert - 1/1350 ≈ 0.000741
        (inverted.Rate * 1350m).ShouldBe(1m, 0.001m);
    }

    #endregion

    #region 속성 테스트

    [Fact]
    public void Pair_ReturnsFormattedString()
    {
        // Arrange
        var rate = ExchangeRate.Create("USD", "KRW", 1350m).Match(r => r, _ => null!);

        // Act & Assert
        rate.Pair.ShouldBe("USD/KRW");
    }

    #endregion
}
