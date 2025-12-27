/// <summary>
/// PriceRange 클래스의 복합 값 객체 테스트
/// 
/// 테스트 목적:
/// 1. SmartEnum과 기존 값 객체의 통합 검증
/// 2. LINQ Expression을 활용한 복합 검증 검증
/// 3. 복합 값 객체의 비교 기능 검증
/// </summary>
[Trait("Concept-12-Type-Safe-Enums", "PriceRangeTests")]
public class PriceRangeTests
{
    // 테스트 시나리오: 유효한 가격 범위로 PriceRange 인스턴스를 생성할 수 있어야 한다
    [Theory]
    [InlineData(10000, 50000, "KRW")]
    [InlineData(100, 500, "USD")]
    [InlineData(80, 400, "EUR")]
    public void Create_ShouldReturnSuccessResult_WhenValidPriceRange(decimal minPrice, decimal maxPrice, string currencyCode)
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
            range.MinPrice.Currency.GetCode().ShouldBe(currencyCode);
            range.MaxPrice.Currency.GetCode().ShouldBe(currencyCode);
        });
    }

    // 테스트 시나리오: 최소 가격이 최대 가격보다 클 때 PriceRange 생성 시 실패해야 한다
    [Theory]
    [InlineData(50000, 10000, "KRW")]
    [InlineData(500, 100, "USD")]
    [InlineData(400, 80, "EUR")]
    public void Create_ShouldReturnFailureResult_WhenMinPriceGreaterThanMaxPrice(decimal minPrice, decimal maxPrice, string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain("최소 가격은 최대 가격보다 작거나 같아야 합니다"));
    }

    // 테스트 시나리오: 음수 가격으로 PriceRange 생성 시 실패해야 한다
    [Theory]
    [InlineData(-1000, 50000, "KRW")]
    [InlineData(10000, -5000, "KRW")]
    [InlineData(-1000, -5000, "KRW")]
    public void Create_ShouldReturnFailureResult_WhenNegativePrice(decimal minPrice, decimal maxPrice, string currencyCode)
    {
        // Arrange
        // (Theory 매개변수 사용)

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain("금액은 0 이상 999,999.99 이하여야 합니다"));
    }

    // 테스트 시나리오: 지원하지 않는 3자리 통화 코드로 PriceRange 생성 시 실패해야 한다
    [Theory]
    [InlineData("abc")]
    [InlineData("XYZ")]
    [InlineData("ABC")]
    public void Create_ShouldReturnFailureResult_WhenUnsupportedCurrencyCode(string currencyCode)
    {
        // Arrange
        var minPrice = 10000m;
        var maxPrice = 50000m;

        // Act
        var actual = PriceRange.Create(minPrice, maxPrice, currencyCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldContain($"지원하지 않는 통화 코드입니다: {currencyCode}"));
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 값 객체들로 PriceRange를 생성해야 한다
    [Theory]
    [InlineData(10000, 50000, "KRW")]
    [InlineData(100, 500, "USD")]
    [InlineData(80, 400, "EUR")]
    public void CreateFromValidated_ShouldReturnPriceRangeInstance_WhenValidatedValueObjects(decimal minPrice, decimal maxPrice, string currencyCode)
    {
        // Arrange
        var minPriceValidated = Price.Validate(minPrice, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
        var maxPriceValidated = Price.Validate(maxPrice, currencyCode).IfFail(_ => throw new Exception("검증 실패"));
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
    [InlineData(10000, 50000, "KRW", true)]
    [InlineData(50000, 10000, "KRW", false)]
    [InlineData(-1000, 50000, "KRW", false)]
    [InlineData(10000, 50000, "INVALID", false)]
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
    [InlineData(80, 400, "EUR", "EUR (유로) € 80.00 ~ EUR (유로) € 400.00")]
    public void ToString_ShouldReturnFormattedPriceRangeInfo_WhenCalled(decimal minPrice, decimal maxPrice, string currencyCode, string expectedFormat)
    {
        // Arrange
        var priceRange = PriceRange.Create(minPrice, maxPrice, currencyCode).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = priceRange.ToString();

        // Assert
        actual.ShouldBe(expectedFormat);
    }

    // 테스트 시나리오: PriceRange 인스턴스들이 올바르게 비교되어야 한다
    [Theory]
    [InlineData(10000, 30000, "KRW", 20000, 40000, "KRW", -1)] // 첫 번째가 더 작음
    [InlineData(20000, 40000, "KRW", 10000, 30000, "KRW", 1)]  // 첫 번째가 더 큼
    [InlineData(10000, 30000, "KRW", 10000, 30000, "KRW", 0)]  // 같음
    public void CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingPriceRanges(
        decimal minPrice1, decimal maxPrice1, string currencyCode1,
        decimal minPrice2, decimal maxPrice2, string currencyCode2,
        int expectedComparison)
    {
        // Arrange
        var range1 = PriceRange.Create(minPrice1, maxPrice1, currencyCode1).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = PriceRange.Create(minPrice2, maxPrice2, currencyCode2).IfFail(_ => throw new Exception("생성 실패"));

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
        var range1 = PriceRange.Create(minPrice1, maxPrice1, currencyCode1).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = PriceRange.Create(minPrice2, maxPrice2, currencyCode2).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = range1.Equals(range2);

        // Assert
        actual.ShouldBe(expectedEquality);
    }

    // 테스트 시나리오: PriceRange 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Theory]
    [InlineData(10000, 30000, "KRW", 20000, 40000, "KRW", true, false, false, true, false, true)]  // 첫 번째가 더 작음
    [InlineData(20000, 40000, "KRW", 10000, 30000, "KRW", false, true, false, false, true, true)]  // 첫 번째가 더 큼
    [InlineData(10000, 30000, "KRW", 10000, 30000, "KRW", false, false, true, true, true, false)]  // 같음
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenComparingPriceRanges(
        decimal minPrice1, decimal maxPrice1, string currencyCode1,
        decimal minPrice2, decimal maxPrice2, string currencyCode2,
        bool expectedLessThan, bool expectedGreaterThan, bool expectedEqual,
        bool expectedLessThanOrEqual, bool expectedGreaterThanOrEqual, bool expectedNotEqual)
    {
        // Arrange
        var range1 = PriceRange.Create(minPrice1, maxPrice1, currencyCode1).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = PriceRange.Create(minPrice2, maxPrice2, currencyCode2).IfFail(_ => throw new Exception("생성 실패"));

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
        var range = PriceRange.Create(10000, 50000, "KRW").IfFail(_ => throw new Exception("생성 실패"));

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
        var range = PriceRange.Create(10000, 50000, "KRW").IfFail(_ => throw new Exception("생성 실패"));
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
        var range1 = PriceRange.Create(10000, 50000, "KRW").IfFail(_ => throw new Exception("생성 실패"));
        var range2 = PriceRange.Create(10000, 50000, "KRW").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual1 = range1.GetHashCode();
        var actual2 = range2.GetHashCode();

        // Assert
        actual1.ShouldBe(actual2);
    }

    // 테스트 시나리오: PriceRange의 속성들이 올바르게 접근되어야 한다
    [Theory]
    [InlineData(10000, 50000, "KRW")]
    [InlineData(100, 500, "USD")]
    [InlineData(80, 400, "EUR")]
    public void Properties_ShouldBeAccessible_WhenPriceRangeCreated(decimal minPrice, decimal maxPrice, string currencyCode)
    {
        // Arrange
        var range = PriceRange.Create(minPrice, maxPrice, currencyCode).IfFail(_ => throw new Exception("생성 실패"));

        // Act & Assert
        range.MinPrice.ShouldNotBeNull();
        range.MaxPrice.ShouldNotBeNull();
        
        ((decimal)range.MinPrice.Amount).ShouldBe(minPrice);
        ((decimal)range.MaxPrice.Amount).ShouldBe(maxPrice);
        range.MinPrice.Currency.GetCode().ShouldBe(currencyCode);
        range.MaxPrice.Currency.GetCode().ShouldBe(currencyCode);
    }

    // 테스트 시나리오: 동일한 통화의 PriceRange들이 올바르게 정렬되어야 한다
    [Fact]
    public void Sort_ShouldOrderCorrectly_WhenSameCurrencyPriceRanges()
    {
        // Arrange
        var ranges = new[]
        {
            PriceRange.Create(20000, 40000, "KRW").IfFail(_ => throw new Exception("생성 실패")),
            PriceRange.Create(10000, 30000, "KRW").IfFail(_ => throw new Exception("생성 실패")),
            PriceRange.Create(15000, 35000, "KRW").IfFail(_ => throw new Exception("생성 실패"))
        };

        // Act
        Array.Sort(ranges);

        // Assert
        var expectedMinPrice1 = Price.CreateFromValidated(Price.Validate(10000, "KRW").IfFail(_ => throw new Exception("검증 실패")));
        var expectedMinPrice2 = Price.CreateFromValidated(Price.Validate(15000, "KRW").IfFail(_ => throw new Exception("검증 실패")));
        var expectedMinPrice3 = Price.CreateFromValidated(Price.Validate(20000, "KRW").IfFail(_ => throw new Exception("검증 실패")));
        
        ranges[0].MinPrice.ShouldBe(expectedMinPrice1);
        ranges[1].MinPrice.ShouldBe(expectedMinPrice2);
        ranges[2].MinPrice.ShouldBe(expectedMinPrice3);
    }
}
