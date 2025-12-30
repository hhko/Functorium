namespace EcommerceDomain.Tests.Unit;

/// <summary>
/// Quantity 값 객체 테스트
///
/// 학습 목표:
/// 1. 수량 범위 검증 (0 ~ 10000)
/// 2. 산술 연산 검증 (Add, Subtract)
/// 3. 비교 가능성 검증 (CompareTo, 정렬)
/// </summary>
[Trait("Part5-Ecommerce-Domain", "QuantityTests")]
public class QuantityTests
{
    #region 생성 테스트

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(10000)]
    public void Create_ReturnsSuccess_WhenValueIsInRange(int value)
    {
        // Act
        var actual = Quantity.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ReturnsFail_WhenValueIsNegative(int value)
    {
        // Act
        var actual = Quantity.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData(10001)]
    [InlineData(99999)]
    public void Create_ReturnsFail_WhenValueExceedsLimit(int value)
    {
        // Act
        var actual = Quantity.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 상수 테스트

    [Fact]
    public void Zero_ReturnsQuantityWithZeroValue()
    {
        // Act & Assert
        Quantity.Zero.Amount.ShouldBe(0);
    }

    [Fact]
    public void One_ReturnsQuantityWithOneValue()
    {
        // Act & Assert
        Quantity.One.Amount.ShouldBe(1);
    }

    #endregion

    #region 산술 연산 테스트

    [Fact]
    public void Add_ReturnsCombinedQuantity()
    {
        // Arrange
        var qty1 = Quantity.Create(5).Match(q => q, _ => Quantity.Zero);
        var qty2 = Quantity.Create(3).Match(q => q, _ => Quantity.Zero);

        // Act
        var actual = qty1.Add(qty2);

        // Assert
        actual.Amount.ShouldBe(8);
    }

    [Fact]
    public void Subtract_ReturnsSubtractedQuantity()
    {
        // Arrange
        var qty1 = Quantity.Create(5).Match(q => q, _ => Quantity.Zero);
        var qty2 = Quantity.Create(3).Match(q => q, _ => Quantity.Zero);

        // Act
        var actual = qty1.Subtract(qty2);

        // Assert
        actual.Amount.ShouldBe(2);
    }

    [Fact]
    public void Subtract_ReturnsZero_WhenResultWouldBeNegative()
    {
        // Arrange
        var qty1 = Quantity.Create(3).Match(q => q, _ => Quantity.Zero);
        var qty2 = Quantity.Create(5).Match(q => q, _ => Quantity.Zero);

        // Act
        var actual = qty1.Subtract(qty2);

        // Assert
        actual.Amount.ShouldBe(0);
    }

    [Fact]
    public void PlusOperator_ReturnsCombinedQuantity()
    {
        // Arrange
        var qty1 = Quantity.Create(5).Match(q => q, _ => Quantity.Zero);
        var qty2 = Quantity.Create(3).Match(q => q, _ => Quantity.Zero);

        // Act
        var actual = qty1 + qty2;

        // Assert
        actual.Amount.ShouldBe(8);
    }

    #endregion

    #region 비교 테스트

    [Fact]
    public void LessThan_ReturnsTrue_WhenLeftIsSmaller()
    {
        // Arrange
        var qty1 = Quantity.Create(3).Match(q => q, _ => Quantity.Zero);
        var qty2 = Quantity.Create(5).Match(q => q, _ => Quantity.Zero);

        // Act & Assert
        (qty1 < qty2).ShouldBeTrue();
    }

    [Fact]
    public void Sort_OrdersQuantitiesAscending()
    {
        // Arrange
        var quantities = new[]
        {
            Quantity.Create(5).Match(q => q, _ => Quantity.Zero),
            Quantity.Create(1).Match(q => q, _ => Quantity.Zero),
            Quantity.Create(3).Match(q => q, _ => Quantity.Zero)
        };

        // Act
        Array.Sort(quantities);

        // Assert
        quantities[0].Amount.ShouldBe(1);
        quantities[1].Amount.ShouldBe(3);
        quantities[2].Amount.ShouldBe(5);
    }

    #endregion
}
