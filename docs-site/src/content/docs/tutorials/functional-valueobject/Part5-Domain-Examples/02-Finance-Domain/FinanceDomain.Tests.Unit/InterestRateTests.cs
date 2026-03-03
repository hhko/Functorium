namespace FinanceDomain.Tests.Unit;

/// <summary>
/// InterestRate 값 객체 테스트
///
/// 학습 목표:
/// 1. 퍼센트 값 검증 (0-100%)
/// 2. 이자 계산 검증 (단리, 복리)
/// 3. 비교 가능성 검증
/// </summary>
[Trait("Part5-Finance-Domain", "InterestRateTests")]
public class InterestRateTests
{
    #region 생성 테스트

    [Theory]
    [InlineData(0)]
    [InlineData(5.5)]
    [InlineData(100)]
    public void Create_ReturnsSuccess_WhenValueIsInRange(decimal value)
    {
        // Act
        var actual = InterestRate.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.1)]
    public void Create_ReturnsFail_WhenValueIsNegative(decimal value)
    {
        // Act
        var actual = InterestRate.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData(100.1)]
    [InlineData(150)]
    public void Create_ReturnsFail_WhenValueExceedsMaximum(decimal value)
    {
        // Act
        var actual = InterestRate.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 속성 테스트

    [Fact]
    public void Percentage_ReturnsOriginalValue()
    {
        // Arrange
        var rate = InterestRate.Create(5.5m).Match(r => r, _ => null!);

        // Act & Assert
        rate.Percentage.ShouldBe(5.5m);
    }

    [Fact]
    public void Decimal_ReturnsDividedValue()
    {
        // Arrange
        var rate = InterestRate.Create(5.5m).Match(r => r, _ => null!);

        // Act & Assert
        rate.Decimal.ShouldBe(0.055m);
    }

    #endregion

    #region 이자 계산 테스트

    [Fact]
    public void CalculateSimpleInterest_ReturnsCorrectValue()
    {
        // Arrange
        var rate = InterestRate.Create(5m).Match(r => r, _ => null!);
        var principal = 1_000_000m;
        var years = 3;

        // Act
        var actual = rate.CalculateSimpleInterest(principal, years);

        // Assert - 1,000,000 * 0.05 * 3 = 150,000
        actual.ShouldBe(150_000m);
    }

    [Fact]
    public void CalculateCompoundInterest_ReturnsCorrectValue()
    {
        // Arrange
        var rate = InterestRate.Create(5m).Match(r => r, _ => null!);
        var principal = 1_000_000m;
        var years = 3;

        // Act
        var actual = rate.CalculateCompoundInterest(principal, years);

        // Assert - 1,000,000 * (1.05^3 - 1) = 157,625
        actual.ShouldBe(157_625m, 1m); // 1원 오차 허용 (부동소수점)
    }

    [Fact]
    public void CompoundInterest_IsGreaterThan_SimpleInterest()
    {
        // Arrange
        var rate = InterestRate.Create(5m).Match(r => r, _ => null!);
        var principal = 1_000_000m;
        var years = 3;

        // Act
        var simple = rate.CalculateSimpleInterest(principal, years);
        var compound = rate.CalculateCompoundInterest(principal, years);

        // Assert
        compound.ShouldBeGreaterThan(simple);
    }

    #endregion

    #region 비교 테스트

    [Fact]
    public void CompareTo_ReturnsNegative_WhenRateIsLower()
    {
        // Arrange
        var rate3 = InterestRate.Create(3m).Match(r => r, _ => null!);
        var rate5 = InterestRate.Create(5m).Match(r => r, _ => null!);

        // Act & Assert
        rate3.CompareTo(rate5).ShouldBeLessThan(0);
    }

    #endregion
}
