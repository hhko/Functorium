using ValueObjectFramework.ValueObjects.Comparable.CompositeValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.Comparable.CompositeValueObjects;

/// <summary>
/// Price 값 객체 테스트
/// ComparableSimpleValueObject<decimal> 기반으로 비교 가능한 primitive 값 객체 구현
/// 
/// 테스트 목적:
/// 1. 기본 값 객체 생성 및 검증 검증
/// 2. LINQ Expression을 활용한 함수형 체이닝 검증
/// 3. 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "PriceTests")]
public class PriceTests
{
    // 테스트 시나리오: 유효한 가격으로 Price 인스턴스를 생성할 수 있어야 한다
    [Theory]
    [InlineData(0, "KRW")]
    [InlineData(100, "USD")]
    [InlineData(999999.99, "EUR")]
    public void Create_ShouldReturnSuccessResult_WhenValidPrice(decimal priceValue, string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Price.Create(priceValue, currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(price => ((decimal)price.Amount).ShouldBe(priceValue));
        actual.IfSucc(price => price.Currency.GetCode().ShouldBe(currencyCode.ToUpperInvariant()));
    }

    // 테스트 시나리오: 음수 가격으로 Price 생성 시 실패해야 한다
    [Theory]
    [InlineData(-1, "KRW")]
    [InlineData(-100.50, "USD")]
    [InlineData(-999999.99, "EUR")]
    public void Create_ShouldReturnFailureResult_WhenNegativePrice(decimal priceValue, string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = Price.Create(priceValue, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain("금액은 0 이상"));
    }

    // 테스트 시나리오: Validate 메서드가 올바른 검증 결과를 반환해야 한다
    [Theory]
    [InlineData(0, "KRW", true)]
    [InlineData(100, "USD", true)]
    [InlineData(-1, "EUR", false)]
    [InlineData(-100.50, "KRW", false)]
    public void Validate_ShouldReturnCorrectValidationResult_WhenVariousPrices(decimal priceValue, string currencyCode, bool expectedIsSuccess)
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
    [InlineData(999999.99, "EUR")]
    public void CreateFromValidated_ShouldReturnPriceInstance_WhenValidatedPrice(decimal priceValue, string currencyCode)
    {
        // Arrange
        var validatedValues = (Amount: MoneyAmount.CreateFromValidated(priceValue), 
                              Currency: Currency.CreateFromValidated(currencyCode));

        // Act
        var actual = Price.CreateFromValidated(validatedValues);

        // Assert
        actual.ShouldNotBeNull();
        ((decimal)actual.Amount).ShouldBe(priceValue);
        actual.Currency.GetCode().ShouldBe(currencyCode.ToUpperInvariant());
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식으로 가격 정보를 반환해야 한다
    [Theory]
    [InlineData(0, "KRW", "KRW (한국 원화) ₩ 0.00")]
    [InlineData(100, "USD", "USD (미국 달러) $ 100.00")]
    [InlineData(1000, "EUR", "EUR (유로) € 1,000.00")]
    public void ToString_ShouldReturnFormattedPriceInfo_WhenCalled(decimal priceValue, string currencyCode, string expectedFormat)
    {
        // Arrange
        var validatedValues = (Amount: MoneyAmount.CreateFromValidated(priceValue), 
                              Currency: Currency.CreateFromValidated(currencyCode));
        var price = Price.CreateFromValidated(validatedValues);

        // Act
        var actual = price.ToString();

        // Assert
        actual.ShouldBe(expectedFormat);
    }

    // 테스트 시나리오: Price 인스턴스들이 올바르게 비교되어야 한다
    [Theory]
    [InlineData(100, 200, "USD", -1)] // 100 < 200
    [InlineData(200, 100, "USD", 1)]  // 200 > 100
    [InlineData(100, 100, "USD", 0)]  // 100 == 100
    public void CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingPrices(decimal priceValue1, decimal priceValue2, string currencyCode, int expectedComparison)
    {
        // Arrange
        var validatedValues1 = (Amount: MoneyAmount.CreateFromValidated(priceValue1), 
                               Currency: Currency.CreateFromValidated(currencyCode));
        var validatedValues2 = (Amount: MoneyAmount.CreateFromValidated(priceValue2), 
                               Currency: Currency.CreateFromValidated(currencyCode));
        var price1 = Price.CreateFromValidated(validatedValues1);
        var price2 = Price.CreateFromValidated(validatedValues2);

        // Act
        var actual = price1.CompareTo(price2);

        // Assert
        actual.ShouldBe(expectedComparison);
    }

    // 테스트 시나리오: Price 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Theory]
    [InlineData(100, 100, "USD", true)]
    [InlineData(100, 200, "USD", false)]
    [InlineData(200, 200, "USD", true)]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenComparingPrices(decimal priceValue1, decimal priceValue2, string currencyCode, bool expectedEquality)
    {
        // Arrange
        var validatedValues1 = (Amount: MoneyAmount.CreateFromValidated(priceValue1), 
                               Currency: Currency.CreateFromValidated(currencyCode));
        var validatedValues2 = (Amount: MoneyAmount.CreateFromValidated(priceValue2), 
                               Currency: Currency.CreateFromValidated(currencyCode));
        var price1 = Price.CreateFromValidated(validatedValues1);
        var price2 = Price.CreateFromValidated(validatedValues2);

        // Act
        var actual = price1.Equals(price2);

        // Assert
        actual.ShouldBe(expectedEquality);
    }

    // 테스트 시나리오: Price 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Theory]
    [InlineData(100, 200, "USD", true, false, false, true, false, true)] // 100 < 200
    [InlineData(200, 100, "USD", false, true, false, false, true, true)] // 200 > 100
    [InlineData(100, 100, "USD", false, false, true, true, true, false)] // 100 == 100
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenComparingPrices(
        decimal priceValue1, decimal priceValue2, string currencyCode,
        bool expectedLessThan, bool expectedGreaterThan, bool expectedEqual, 
        bool expectedLessThanOrEqual, bool expectedGreaterThanOrEqual, bool expectedNotEqual)
    {
        // Arrange
        var validatedValues1 = (Amount: MoneyAmount.CreateFromValidated(priceValue1), 
                               Currency: Currency.CreateFromValidated(currencyCode));
        var validatedValues2 = (Amount: MoneyAmount.CreateFromValidated(priceValue2), 
                               Currency: Currency.CreateFromValidated(currencyCode));
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
        var validatedValues = (Amount: MoneyAmount.CreateFromValidated(100), 
                              Currency: Currency.CreateFromValidated("USD"));
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
        var validatedValues = (Amount: MoneyAmount.CreateFromValidated(100), 
                              Currency: Currency.CreateFromValidated("USD"));
        var price = Price.CreateFromValidated(validatedValues);
        var otherObject = "not a price";

        // Act
        var actual = price.Equals(otherObject);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: Price 인스턴스가 올바르게 해시 코드를 생성해야 한다
    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenSamePrice()
    {
        // Arrange
        var validatedValues1 = (Amount: MoneyAmount.CreateFromValidated(100), 
                               Currency: Currency.CreateFromValidated("USD"));
        var validatedValues2 = (Amount: MoneyAmount.CreateFromValidated(100), 
                               Currency: Currency.CreateFromValidated("USD"));
        var price1 = Price.CreateFromValidated(validatedValues1);
        var price2 = Price.CreateFromValidated(validatedValues2);

        // Act
        var actual1 = price1.GetHashCode();
        var actual2 = price2.GetHashCode();

        // Assert
        actual1.ShouldBe(actual2);
    }

    // 테스트 시나리오: Price 인스턴스의 Amount 속성이 올바르게 접근되어야 한다
    [Theory]
    [InlineData(0, "USD")]
    [InlineData(100, "EUR")]
    [InlineData(999999.99, "KRW")]
    public void AmountProperty_ShouldReturnCorrectValue_WhenPriceInstance(decimal priceValue, string currencyCode)
    {
        // Arrange
        var validatedValues = (Amount: MoneyAmount.CreateFromValidated(priceValue), 
                              Currency: Currency.CreateFromValidated(currencyCode));
        var price = Price.CreateFromValidated(validatedValues);

        // Act
        var actual = (decimal)price.Amount;

        // Assert
        actual.ShouldBe(priceValue);
    }

    // 테스트 시나리오: LINQ Expression을 활용한 검증 파이프라인이 올바르게 동작해야 한다
    [Theory]
    [InlineData(100, "USD", true)]
    [InlineData(0, "EUR", true)]
    [InlineData(-1, "KRW", false)]
    [InlineData(-100.50, "USD", false)]
    public void ValidationPipeline_ShouldWorkCorrectly_WhenUsingLINQExpression(decimal priceValue, string currencyCode, bool expectedSuccess)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var validationResult = Price.Validate(priceValue, currencyCode);

        // Assert
        validationResult.Match(
            Succ: _ => expectedSuccess.ShouldBeTrue(),
            Fail: _ => expectedSuccess.ShouldBeFalse()
        );
    }
}