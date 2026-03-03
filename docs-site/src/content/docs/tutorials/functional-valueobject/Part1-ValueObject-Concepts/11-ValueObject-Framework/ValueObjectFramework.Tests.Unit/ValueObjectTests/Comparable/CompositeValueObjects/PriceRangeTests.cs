using ValueObjectFramework.ValueObjects.Comparable.CompositeValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.Comparable.CompositeValueObjects;

/// <summary>
/// PriceRange 복합 값 객체 테스트
/// ComparableValueObject 기반으로 비교 가능한 composite 값 객체 구현
/// Price와 Currency 값 객체를 조합하여 구성
/// 
/// 테스트 목적:
/// 1. 복합 값 객체 생성 및 검증 검증
/// 2. LINQ Expression을 활용한 함수형 체이닝 검증
/// 3. 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "PriceRangeTests")]
public class PriceRangeTests
{
    // 테스트 시나리오: 유효한 가격 범위와 통화 코드로 PriceRange 생성 시 성공해야 한다
    [Theory]
    [InlineData(100, 200, "KRW")]
    [InlineData(0, 1000, "USD")]
    [InlineData(50, 500, "EUR")]
    public void Create_ShouldReturnSuccessResult_WhenValidInputs(decimal minPrice, decimal maxPrice, string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(range =>
        {
            ((decimal)range.MinPrice.Amount).ShouldBe(minPrice);
            ((decimal)range.MaxPrice.Amount).ShouldBe(maxPrice);
            range.MinPrice.Currency.GetCode().ShouldBe(currencyCode.ToUpperInvariant());
        });
    }

    // 테스트 시나리오: 음수 최소 가격으로 PriceRange 생성 시 실패해야 한다
    [Theory]
    [InlineData(-1000, 50000, "KRW", "금액은 0 이상 999,999.99 이하여야 합니다")]
    [InlineData(-1, 100, "USD", "금액은 0 이상 999,999.99 이하여야 합니다")]
    public void Create_ShouldReturnFailureResult_WhenNegativeMinPrice(decimal minPrice, decimal maxPrice, string currencyCode, string expectedErrorMessage)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain(expectedErrorMessage));
    }

    // 테스트 시나리오: 음수 최대 가격으로 PriceRange 생성 시 실패해야 한다
    [Theory]
    [InlineData(10000, -5000, "KRW", "금액은 0 이상 999,999.99 이하여야 합니다")]
    [InlineData(100, -50, "USD", "금액은 0 이상 999,999.99 이하여야 합니다")]
    public void Create_ShouldReturnFailureResult_WhenNegativeMaxPrice(decimal minPrice, decimal maxPrice, string currencyCode, string expectedErrorMessage)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain(expectedErrorMessage));
    }

    // 테스트 시나리오: 최소 가격이 최대 가격보다 클 때 PriceRange 생성 시 실패해야 한다
    [Theory]
    [InlineData(50000, 10000, "KRW", "최소 가격은 최대 가격보다 작거나 같아야 합니다")]
    [InlineData(200, 100, "USD", "최소 가격은 최대 가격보다 작거나 같아야 합니다")]
    public void Create_ShouldReturnFailureResult_WhenMinPriceGreaterThanMaxPrice(decimal minPrice, decimal maxPrice, string currencyCode, string expectedErrorMessage)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain(expectedErrorMessage));
    }

    // 테스트 시나리오: 잘못된 형식의 통화 코드로 PriceRange 생성 시 실패해야 한다
    [Theory]
    [InlineData(10000, 50000, "INVALID", "통화 코드는 3자리 영문자여야 합니다")]
    [InlineData(100, 500, "US", "통화 코드는 3자리 영문자여야 합니다")]
    [InlineData(100, 500, "US1", "통화 코드는 3자리 영문자여야 합니다")]
    public void Create_ShouldReturnFailureResult_WhenInvalidCurrencyCode(decimal minPrice, decimal maxPrice, string currencyCode, string expectedErrorMessage)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain(expectedErrorMessage));
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 값 객체들로 PriceRange를 생성해야 한다
    [Theory]
    [InlineData(100, 200, "KRW")]
    [InlineData(0, 1000, "USD")]
    public void CreateFromValidated_ShouldReturnPriceRangeInstance_WhenValidatedValueObjects(decimal minPrice, decimal maxPrice, string currencyCode)
    {
        // Arrange
        var minPriceValidated = (Amount: MoneyAmount.CreateFromValidated(minPrice), 
                                Currency: Currency.CreateFromValidated(currencyCode));
        var maxPriceValidated = (Amount: MoneyAmount.CreateFromValidated(maxPrice), 
                                Currency: Currency.CreateFromValidated(currencyCode));
        var minPriceObj = Price.CreateFromValidated(minPriceValidated);
        var maxPriceObj = Price.CreateFromValidated(maxPriceValidated);

        // Act
        var actual = PriceRange.CreateFromValidated(minPriceObj, maxPriceObj);

        // Assert
        actual.MinPrice.ShouldBe(minPriceObj);
        actual.MaxPrice.ShouldBe(maxPriceObj);
    }

    // 테스트 시나리오: Validate 메서드가 올바른 검증 결과를 반환해야 한다
    [Theory]
    [InlineData(100, 200, "KRW", true)]
    [InlineData(0, 1000, "USD", true)]
    [InlineData(-100, 200, "KRW", false)]
    [InlineData(100, -200, "KRW", false)]
    [InlineData(200, 100, "KRW", false)]
    [InlineData(100, 200, "INVALID", false)]
    public void Validate_ShouldReturnCorrectValidationResult_WhenVariousInputs(decimal minPrice, decimal maxPrice, string currencyCode, bool expectedIsSuccess)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Validate(minPrice, maxPrice, currencyCode);

        // Assert
        actual.Match(
            Succ: _ => expectedIsSuccess.ShouldBeTrue(),
            Fail: _ => expectedIsSuccess.ShouldBeFalse()
        );
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식으로 가격 범위 정보를 반환해야 한다
    [Theory]
    [InlineData(10000, 50000, "KRW", "KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 50,000.00")]
    [InlineData(100, 500, "USD", "USD (미국 달러) $ 100.00 ~ USD (미국 달러) $ 500.00")]
    [InlineData(0, 1000, "EUR", "EUR (유로) € 0.00 ~ EUR (유로) € 1,000.00")]
    public void ToString_ShouldReturnFormattedPriceRangeInfo_WhenCalled(decimal minPrice, decimal maxPrice, string currencyCode, string expectedFormat)
    {
        // Arrange
        var minPriceValidated = (Amount: MoneyAmount.CreateFromValidated(minPrice), 
                                Currency: Currency.CreateFromValidated(currencyCode));
        var maxPriceValidated = (Amount: MoneyAmount.CreateFromValidated(maxPrice), 
                                Currency: Currency.CreateFromValidated(currencyCode));
        var minPriceObj = Price.CreateFromValidated(minPriceValidated);
        var maxPriceObj = Price.CreateFromValidated(maxPriceValidated);
        var range = PriceRange.CreateFromValidated(minPriceObj, maxPriceObj);

        // Act
        var actual = range.ToString();

        // Assert
        actual.ShouldBe(expectedFormat);
    }

    // 테스트 시나리오: PriceRange 인스턴스들이 올바르게 비교되어야 한다
    [Theory]
    [InlineData(10000, 30000, "KRW", 20000, 40000, "KRW", -1)] // range1 < range2
    [InlineData(20000, 40000, "KRW", 10000, 30000, "KRW", 1)]  // range1 > range2
    [InlineData(10000, 30000, "KRW", 10000, 30000, "KRW", 0)]  // range1 == range2
    public void CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingPriceRanges(
        decimal minPrice1, decimal maxPrice1, string currencyCode1,
        decimal minPrice2, decimal maxPrice2, string currencyCode2,
        int expectedComparison)
    {
        // Arrange
        var minPrice1Validated = (Amount: MoneyAmount.CreateFromValidated(minPrice1), 
                                 Currency: Currency.CreateFromValidated(currencyCode1));
        var maxPrice1Validated = (Amount: MoneyAmount.CreateFromValidated(maxPrice1), 
                                 Currency: Currency.CreateFromValidated(currencyCode1));
        var minPrice2Validated = (Amount: MoneyAmount.CreateFromValidated(minPrice2), 
                                 Currency: Currency.CreateFromValidated(currencyCode2));
        var maxPrice2Validated = (Amount: MoneyAmount.CreateFromValidated(maxPrice2), 
                                 Currency: Currency.CreateFromValidated(currencyCode2));
        
        var range1 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPrice1Validated),
            Price.CreateFromValidated(maxPrice1Validated)
        );

        var range2 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPrice2Validated),
            Price.CreateFromValidated(maxPrice2Validated)
        );

        // Act
        var actual = range1.CompareTo(range2);

        // Assert
        actual.ShouldBe(expectedComparison);
    }

    // 테스트 시나리오: PriceRange 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Theory]
    [InlineData(10000, 30000, "KRW", 10000, 30000, "KRW", true)]
    [InlineData(10000, 30000, "KRW", 20000, 40000, "KRW", false)]
    [InlineData(10000, 30000, "KRW", 10000, 30000, "USD", false)]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenComparingPriceRanges(
        decimal minPrice1, decimal maxPrice1, string currencyCode1,
        decimal minPrice2, decimal maxPrice2, string currencyCode2,
        bool expectedEquality)
    {
        // Arrange
        var minPrice1Validated = (Amount: MoneyAmount.CreateFromValidated(minPrice1), 
                                 Currency: Currency.CreateFromValidated(currencyCode1));
        var maxPrice1Validated = (Amount: MoneyAmount.CreateFromValidated(maxPrice1), 
                                 Currency: Currency.CreateFromValidated(currencyCode1));
        var minPrice2Validated = (Amount: MoneyAmount.CreateFromValidated(minPrice2), 
                                 Currency: Currency.CreateFromValidated(currencyCode2));
        var maxPrice2Validated = (Amount: MoneyAmount.CreateFromValidated(maxPrice2), 
                                 Currency: Currency.CreateFromValidated(currencyCode2));
        
        var range1 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPrice1Validated),
            Price.CreateFromValidated(maxPrice1Validated)
        );

        var range2 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPrice2Validated),
            Price.CreateFromValidated(maxPrice2Validated)
        );

        // Act
        var actual = range1.Equals(range2);

        // Assert
        actual.ShouldBe(expectedEquality);
    }

    // 테스트 시나리오: PriceRange 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Theory]
    [InlineData(10000, 30000, "KRW", 20000, 40000, "KRW", true, false, false, true, false, true)] // range1 < range2
    [InlineData(20000, 40000, "KRW", 10000, 30000, "KRW", false, true, false, false, true, true)] // range1 > range2
    [InlineData(10000, 30000, "KRW", 10000, 30000, "KRW", false, false, true, true, true, false)] // range1 == range2
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenComparingPriceRanges(
        decimal minPrice1, decimal maxPrice1, string currencyCode1,
        decimal minPrice2, decimal maxPrice2, string currencyCode2,
        bool expectedLessThan, bool expectedGreaterThan, bool expectedEqual, 
        bool expectedLessThanOrEqual, bool expectedGreaterThanOrEqual, bool expectedNotEqual)
    {
        // Arrange
        var minPrice1Validated = (Amount: MoneyAmount.CreateFromValidated(minPrice1), 
                                 Currency: Currency.CreateFromValidated(currencyCode1));
        var maxPrice1Validated = (Amount: MoneyAmount.CreateFromValidated(maxPrice1), 
                                 Currency: Currency.CreateFromValidated(currencyCode1));
        var minPrice2Validated = (Amount: MoneyAmount.CreateFromValidated(minPrice2), 
                                 Currency: Currency.CreateFromValidated(currencyCode2));
        var maxPrice2Validated = (Amount: MoneyAmount.CreateFromValidated(maxPrice2), 
                                 Currency: Currency.CreateFromValidated(currencyCode2));
        
        var range1 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPrice1Validated),
            Price.CreateFromValidated(maxPrice1Validated)
        );

        var range2 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPrice2Validated),
            Price.CreateFromValidated(maxPrice2Validated)
        );

        // Act & Assert
        (range1 < range2).ShouldBe(expectedLessThan);
        (range1 > range2).ShouldBe(expectedGreaterThan);
        (range1 == range2).ShouldBe(expectedEqual);
        (range1 <= range2).ShouldBe(expectedLessThanOrEqual);
        (range1 >= range2).ShouldBe(expectedGreaterThanOrEqual);
        (range1 != range2).ShouldBe(expectedNotEqual);
    }

    // 테스트 시나리오: PriceRange 인스턴스가 null과 비교될 때 올바르게 처리되어야 한다
    [Fact]
    public void ComparisonWithNull_ShouldReturnCorrectResults_WhenComparingWithNull()
    {
        // Arrange
        var minPriceValidated = (Amount: MoneyAmount.CreateFromValidated(10000), 
                                Currency: Currency.CreateFromValidated("KRW"));
        var maxPriceValidated = (Amount: MoneyAmount.CreateFromValidated(30000), 
                                Currency: Currency.CreateFromValidated("KRW"));
        var range = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPriceValidated),
            Price.CreateFromValidated(maxPriceValidated)
        );

        // Act & Assert
        (range == null).ShouldBeFalse();
        (range != null).ShouldBeTrue();
        (null == range).ShouldBeFalse();
        (null != range).ShouldBeTrue();
    }

    // 테스트 시나리오: PriceRange 인스턴스가 다른 타입과 비교될 때 올바르게 처리되어야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        // Arrange
        var minPriceValidated = (Amount: MoneyAmount.CreateFromValidated(10000), 
                                Currency: Currency.CreateFromValidated("KRW"));
        var maxPriceValidated = (Amount: MoneyAmount.CreateFromValidated(30000), 
                                Currency: Currency.CreateFromValidated("KRW"));
        var range = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPriceValidated),
            Price.CreateFromValidated(maxPriceValidated)
        );

        var otherObject = "not a price range";

        // Act
        var actual = range.Equals(otherObject);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: PriceRange 인스턴스가 올바르게 해시 코드를 생성해야 한다
    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenSamePriceRange()
    {
        // Arrange
        var minPriceValidated = (Amount: MoneyAmount.CreateFromValidated(10000), 
                                Currency: Currency.CreateFromValidated("KRW"));
        var maxPriceValidated = (Amount: MoneyAmount.CreateFromValidated(30000), 
                                Currency: Currency.CreateFromValidated("KRW"));
        
        var range1 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPriceValidated),
            Price.CreateFromValidated(maxPriceValidated)
        );

        var range2 = PriceRange.CreateFromValidated(
            Price.CreateFromValidated(minPriceValidated),
            Price.CreateFromValidated(maxPriceValidated)
        );

        // Act
        var actual1 = range1.GetHashCode();
        var actual2 = range2.GetHashCode();

        // Assert
        actual1.ShouldBe(actual2);
    }

    // 테스트 시나리오: LINQ Expression을 활용한 검증 파이프라인이 올바르게 동작해야 한다
    [Theory]
    [InlineData(100, 200, "KRW", true)]
    [InlineData(0, 1000, "USD", true)]
    [InlineData(-100, 200, "KRW", false)]
    [InlineData(100, -200, "KRW", false)]
    [InlineData(200, 100, "KRW", false)]
    [InlineData(100, 200, "INVALID", false)]
    public void ValidationPipeline_ShouldWorkCorrectly_WhenUsingLINQExpression(decimal minPrice, decimal maxPrice, string currencyCode, bool expectedSuccess)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var validationResult = PriceRange.Validate(minPrice, maxPrice, currencyCode);

        // Assert
        validationResult.Match(
            Succ: _ => expectedSuccess.ShouldBeTrue(),
            Fail: _ => expectedSuccess.ShouldBeFalse()
        );
    }
}