/// <summary>
/// Price 클래스의 복합 값 객체 테스트
/// 
/// 테스트 목적:
/// 1. ComparableValueObject 기반 복합 값 객체 검증
/// 2. MoneyAmount와 Currency 조합 검증
/// 3. 복합 값 객체의 비교 기능 검증
/// </summary>
[Trait("Concept-12-Type-Safe-Enums", "PriceTests")]
public class PriceTests
{
    // 테스트 시나리오: 유효한 가격 값과 통화 코드로 Price 인스턴스를 생성할 수 있어야 한다
    [Theory]
    [InlineData(0, "KRW")]
    [InlineData(100, "USD")]
    [InlineData(1000.50, "EUR")]
    [InlineData(999999.99, "KRW")]
    public void Create_ShouldReturnSuccessResult_WhenValidPriceValueAndCurrency(decimal priceValue, string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Price.Create(priceValue, currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(price => 
        {
            ((decimal)price.Amount).ShouldBe(priceValue);
            price.Currency.GetCode().ShouldBe(currencyCode);
        });
    }

    // 테스트 시나리오: 음수 가격 값으로 Price 생성 시 실패해야 한다
    [Theory]
    [InlineData(-1, "KRW")]
    [InlineData(-100, "USD")]
    [InlineData(-0.01, "EUR")]
    public void Create_ShouldReturnFailureResult_WhenNegativePriceValue(decimal priceValue, string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Price.Create(priceValue, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain("금액은 0 이상 999,999.99 이하여야 합니다"));
    }

    // 테스트 시나리오: Validate 메서드가 올바른 검증 결과를 반환해야 한다
    [Theory]
    [InlineData(0, "KRW", true)]
    [InlineData(100, "USD", true)]
    [InlineData(-1, "EUR", false)]
    [InlineData(-100, "KRW", false)]
    public void Validate_ShouldReturnCorrectValidationResult_WhenVariousPriceValues(decimal priceValue, string currencyCode, bool expectedIsSuccess)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Price.Validate(priceValue, currencyCode);

        // Assert
        actual.Match(
            Succ: _ => expectedIsSuccess.ShouldBeTrue(),
            Fail: _ => expectedIsSuccess.ShouldBeFalse()
        );
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 값으로 Price를 생성해야 한다
    [Theory]
    [InlineData(0, "KRW")]
    [InlineData(100, "USD")]
    [InlineData(1000.50, "EUR")]
    public void CreateFromValidated_ShouldReturnPriceInstance_WhenValidatedPriceValue(decimal priceValue, string currencyCode)
    {
        // Arrange
        var validatedValues = Price.Validate(priceValue, currencyCode).IfFail(_ => throw new Exception("검증 실패"));

        // Act
        var actual = Price.CreateFromValidated(validatedValues);

        // Assert
        actual.ShouldNotBeNull();
        ((decimal)actual.Amount).ShouldBe(priceValue);
        actual.Currency.GetCode().ShouldBe(currencyCode);
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식으로 가격 정보를 반환해야 한다
    [Theory]
    [InlineData(0, "KRW", "KRW (한국 원화) ₩ 0.00")]
    [InlineData(1000, "USD", "USD (미국 달러) $ 1,000.00")]
    [InlineData(12345, "EUR", "EUR (유로) € 12,345.00")]
    public void ToString_ShouldReturnFormattedPriceInfo_WhenCalled(decimal priceValue, string currencyCode, string expectedFormat)
    {
        // Arrange
        var validatedValues = Price.Validate(priceValue, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var price = Price.CreateFromValidated(validatedValues);

        // Act
        var actual = price.ToString();

        // Assert
        actual.ShouldBe(expectedFormat);
    }

    // 테스트 시나리오: 같은 통화의 Price 인스턴스들이 올바르게 비교되어야 한다
    [Theory]
    [InlineData(100, 200, "KRW", -1)] // 100 < 200
    [InlineData(200, 100, "USD", 1)]  // 200 > 100
    [InlineData(100, 100, "EUR", 0)]  // 100 == 100
    public void CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingSameCurrencyPrices(decimal priceValue1, decimal priceValue2, string currencyCode, int expectedComparison)
    {
        // Arrange
        var validatedValues1 = Price.Validate(priceValue1, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var validatedValues2 = Price.Validate(priceValue2, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var price1 = Price.CreateFromValidated(validatedValues1);
        var price2 = Price.CreateFromValidated(validatedValues2);

        // Act
        var actual = price1.CompareTo(price2);

        // Assert
        actual.ShouldBe(expectedComparison);
    }

    // 테스트 시나리오: Price 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Theory]
    [InlineData(100, 100, "KRW", true)]
    [InlineData(100, 200, "USD", false)]
    [InlineData(0, 0, "EUR", true)]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenComparingPrices(decimal priceValue1, decimal priceValue2, string currencyCode, bool expectedEquality)
    {
        // Arrange
        var validatedValues1 = Price.Validate(priceValue1, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var validatedValues2 = Price.Validate(priceValue2, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var price1 = Price.CreateFromValidated(validatedValues1);
        var price2 = Price.CreateFromValidated(validatedValues2);

        // Act
        var actual = price1.Equals(price2);

        // Assert
        actual.ShouldBe(expectedEquality);
    }

    // 테스트 시나리오: Price 인스턴스들이 올바르게 해시 코드를 생성해야 한다
    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenSamePriceValue()
    {
        // Arrange
        var validatedValues1 = Price.Validate(100, "KRW").IfFail(_ => throw new Exception("검증 실패"));
        var validatedValues2 = Price.Validate(100, "KRW").IfFail(_ => throw new Exception("검증 실패"));
        var price1 = Price.CreateFromValidated(validatedValues1);
        var price2 = Price.CreateFromValidated(validatedValues2);

        // Act
        var actual1 = price1.GetHashCode();
        var actual2 = price2.GetHashCode();

        // Assert
        actual1.ShouldBe(actual2);
    }

    // 테스트 시나리오: Price 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Theory]
    [InlineData(100, 200, "KRW", true, false, false, true, false, true)]  // 100 < 200
    [InlineData(200, 100, "USD", false, true, false, false, true, true)]  // 200 > 100
    [InlineData(100, 100, "EUR", false, false, true, true, true, false)]  // 100 == 100
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenComparingPrices(
        decimal priceValue1, decimal priceValue2, string currencyCode,
        bool expectedLessThan, bool expectedGreaterThan, bool expectedEqual, 
        bool expectedLessThanOrEqual, bool expectedGreaterThanOrEqual, bool expectedNotEqual)
    {
        // Arrange
        var validatedValues1 = Price.Validate(priceValue1, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var validatedValues2 = Price.Validate(priceValue2, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var price1 = Price.CreateFromValidated(validatedValues1);
        var price2 = Price.CreateFromValidated(validatedValues2);

        // Act & Assert
        (price1 < price2).ShouldBe(expectedLessThan);
        (price1 > price2).ShouldBe(expectedGreaterThan);
        (price1 == price2).ShouldBe(expectedEqual);
        (price1 <= price2).ShouldBe(expectedLessThanOrEqual);
        (price1 >= price2).ShouldBe(expectedGreaterThanOrEqual);
        (price1 != price2).ShouldBe(expectedNotEqual);
    }

    // 테스트 시나리오: Price 인스턴스가 null과 비교될 때 올바르게 처리되어야 한다
    [Fact]
    public void ComparisonWithNull_ShouldReturnCorrectResults_WhenComparingWithNull()
    {
        // Arrange
        var validatedValues = Price.Validate(100, "KRW").IfFail(_ => throw new Exception("검증 실패"));
        var price = Price.CreateFromValidated(validatedValues);

        // Act & Assert
        (price == null).ShouldBeFalse();
        (price != null).ShouldBeTrue();
        (null == price).ShouldBeFalse();
        (null != price).ShouldBeTrue();
    }

    // 테스트 시나리오: Price 인스턴스가 다른 타입과 비교될 때 올바르게 처리되어야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        // Arrange
        var validatedValues = Price.Validate(100, "KRW").IfFail(_ => throw new Exception("검증 실패"));
        var price = Price.CreateFromValidated(validatedValues);
        var otherObject = "not a price";

        // Act
        var actual = price.Equals(otherObject);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: Price 인스턴스의 Amount 속성을 통해 decimal 값에 접근할 수 있어야 한다
    [Theory]
    [InlineData(0, "KRW")]
    [InlineData(100, "USD")]
    [InlineData(1000.50, "EUR")]
    public void AmountProperty_ShouldReturnDecimalValue_WhenPriceInstance(decimal priceValue, string currencyCode)
    {
        // Arrange
        var validatedValues = Price.Validate(priceValue, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var price = Price.CreateFromValidated(validatedValues);

        // Act
        var actual = (decimal)price.Amount;

        // Assert
        actual.ShouldBe(priceValue);
    }

    // 테스트 시나리오: Price 인스턴스의 Currency 속성을 통해 통화 정보에 접근할 수 있어야 한다
    [Theory]
    [InlineData(100, "KRW")]
    [InlineData(200, "USD")]
    [InlineData(300, "EUR")]
    public void CurrencyProperty_ShouldReturnCurrencyInfo_WhenPriceInstance(decimal priceValue, string currencyCode)
    {
        // Arrange
        var validatedValues = Price.Validate(priceValue, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var price = Price.CreateFromValidated(validatedValues);

        // Act
        var actual = price.Currency.GetCode();

        // Assert
        actual.ShouldBe(currencyCode);
    }
}
