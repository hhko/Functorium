using ValueComparability.ValueObjects;

namespace ValueComparability.Tests.Unit;

/// <summary>
/// Denominator 값 객체의 IComparable<T> 구현 테스트
/// 
/// 테스트 목적:
/// 1. CompareTo 메서드의 정확한 동작 검증
/// 2. 비교 연산자 오버로딩 검증
/// 3. null 안전성 검증
/// 4. 컬렉션에서의 정렬 및 검색 기능 검증
/// </summary>
[Trait("Concept-08-Value-Comparability", "DenominatorComparabilityTests")]
public class DenominatorComparabilityTests
{
    // 테스트 시나리오: 유효한 두 Denominator를 비교할 때 올바른 CompareTo 결과를 반환해야 한다
    [Theory]
    [InlineData(5, 10, -1)]   // a < b = -1
    [InlineData(10, 5, 1)]    // a > b = 1
    [InlineData(5, 5, 0)]     // a == b = 0
    [InlineData(-5, 5, -1)]   // 음수 < 양수 = -1
    [InlineData(5, -5, 1)]    // 양수 > 음수 = 1
    [InlineData(-10, -5, -1)] // 음수 < 음수(음수끼리 비교) = -1
    public void CompareTo_ShouldReturnCorrectResult_WhenComparingValidDenominators(int value1, int value2, int expected)
    {
        // Arrange
        var result1 = Denominator.Create(value1);
        var result2 = Denominator.Create(value2);

        // Act
        var actual = from a in result1
                     from b in result2
                     select a.CompareTo(b);

        // Assert
        actual.Match(
            Succ: compareResult => compareResult.ShouldBe(expected),
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: null과 비교할 때 모든 값이 null보다 크다는 결과를 반환해야 한다
    [Fact]
    public void CompareTo_ShouldReturnPositive_WhenComparingWithNull()
    {
        // Arrange
        var result = Denominator.Create(5);
        Denominator? nullValue = null;

        // Act
        var actual = from denominator in result
                     select denominator.CompareTo(nullValue);

        // Assert
        actual.Match(
            Succ: compareResult => compareResult.ShouldBe(1),
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 작음 연산자가 올바르게 동작해야 한다
    [Theory]
    [InlineData(5, 10, true)]   // 5 < 10
    [InlineData(10, 5, false)]  // 10 < 5
    [InlineData(5, 5, false)]   // 5 < 5
    [InlineData(5, null, false)] // 5 < null
    [InlineData(null, 5, true)]  // null < 5
    [InlineData(null, null, false)] // null < null
    public void LessThanOperator_ShouldReturnCorrectResult_WhenComparingDenominators(int? value1, int? value2, bool expected)
    {
        // Arrange
        var denominator1 = value1.HasValue ? Denominator.Create(value1.Value) : Fin.Fail<Denominator>(Error.New("null"));
        var denominator2 = value2.HasValue ? Denominator.Create(value2.Value) : Fin.Fail<Denominator>(Error.New("null"));

        // Act
        var actual = from a in denominator1
                     from b in denominator2
                     select a < b;

        // Assert
        if (value1.HasValue && value2.HasValue)
        {
            actual.Match(
                Succ: result => result.ShouldBe(expected),
                Fail: error => throw new Exception($"생성 실패: {error.Message}")
            );
        }
        else
        {
            // null 케이스는 직접 테스트
            Denominator? d1 = value1.HasValue ? Denominator.Create(value1.Value).Match(Succ: x => x, Fail: _ => null) : null;
            Denominator? d2 = value2.HasValue ? Denominator.Create(value2.Value).Match(Succ: x => x, Fail: _ => null) : null;
            var result = d1 < d2;
            result.ShouldBe(expected);
        }
    }

    // 테스트 시나리오: 큼 연산자가 올바르게 동작해야 한다
    [Theory]
    [InlineData(10, 5, true)]   // 10 > 5
    [InlineData(5, 10, false)]  // 5 > 10
    [InlineData(5, 5, false)]   // 5 > 5
    [InlineData(5, null, true)]  // 5 > null
    [InlineData(null, 5, false)] // null > 5
    [InlineData(null, null, false)] // null > null
    public void GreaterThanOperator_ShouldReturnCorrectResult_WhenComparingDenominators(int? value1, int? value2, bool expected)
    {
        // Arrange
        var denominator1 = value1.HasValue ? Denominator.Create(value1.Value) : Fin.Fail<Denominator>(Error.New("null"));
        var denominator2 = value2.HasValue ? Denominator.Create(value2.Value) : Fin.Fail<Denominator>(Error.New("null"));

        // Act
        var actual = from a in denominator1
                     from b in denominator2
                     select a > b;

        // Assert
        if (value1.HasValue && value2.HasValue)
        {
            actual.Match(
                Succ: result => result.ShouldBe(expected),
                Fail: error => throw new Exception($"생성 실패: {error.Message}")
            );
        }
        else
        {
            // null 케이스는 직접 테스트
            Denominator? d1 = value1.HasValue ? Denominator.Create(value1.Value).Match(Succ: x => x, Fail: _ => null) : null;
            Denominator? d2 = value2.HasValue ? Denominator.Create(value2.Value).Match(Succ: x => x, Fail: _ => null) : null;
            var result = d1 > d2;
            result.ShouldBe(expected);
        }
    }

    // 테스트 시나리오: 작거나 같음 연산자가 올바르게 동작해야 한다
    [Theory]
    [InlineData(5, 10, true)]   // 5 <= 10
    [InlineData(10, 5, false)]  // 10 <= 5
    [InlineData(5, 5, true)]    // 5 <= 5
    [InlineData(5, null, false)] // 5 <= null
    [InlineData(null, 5, true)]  // null <= 5
    [InlineData(null, null, true)] // null <= null
    public void LessThanOrEqualOperator_ShouldReturnCorrectResult_WhenComparingDenominators(int? value1, int? value2, bool expected)
    {
        // Arrange
        var denominator1 = value1.HasValue ? Denominator.Create(value1.Value) : Fin.Fail<Denominator>(Error.New("null"));
        var denominator2 = value2.HasValue ? Denominator.Create(value2.Value) : Fin.Fail<Denominator>(Error.New("null"));

        // Act
        var actual = from a in denominator1
                     from b in denominator2
                     select a <= b;

        // Assert
        if (value1.HasValue && value2.HasValue)
        {
            actual.Match(
                Succ: result => result.ShouldBe(expected),
                Fail: error => throw new Exception($"생성 실패: {error.Message}")
            );
        }
        else
        {
            // null 케이스는 직접 테스트
            Denominator? d1 = value1.HasValue ? Denominator.Create(value1.Value).Match(Succ: x => x, Fail: _ => null) : null;
            Denominator? d2 = value2.HasValue ? Denominator.Create(value2.Value).Match(Succ: x => x, Fail: _ => null) : null;
            var result = d1 <= d2;
            result.ShouldBe(expected);
        }
    }

    // 테스트 시나리오: 크거나 같음 연산자가 올바르게 동작해야 한다
    [Theory]
    [InlineData(10, 5, true)]   // 10 >= 5
    [InlineData(5, 10, false)]  // 5 >= 10
    [InlineData(5, 5, true)]    // 5 >= 5
    [InlineData(5, null, true)]  // 5 >= null
    [InlineData(null, 5, false)] // null >= 5
    [InlineData(null, null, true)] // null >= null
    public void GreaterThanOrEqualOperator_ShouldReturnCorrectResult_WhenComparingDenominators(int? value1, int? value2, bool expected)
    {
        // Arrange
        var denominator1 = value1.HasValue ? Denominator.Create(value1.Value) : Fin.Fail<Denominator>(Error.New("null"));
        var denominator2 = value2.HasValue ? Denominator.Create(value2.Value) : Fin.Fail<Denominator>(Error.New("null"));

        // Act
        var actual = from a in denominator1
                     from b in denominator2
                     select a >= b;

        // Assert
        if (value1.HasValue && value2.HasValue)
        {
            actual.Match(
                Succ: result => result.ShouldBe(expected),
                Fail: error => throw new Exception($"생성 실패: {error.Message}")
            );
        }
        else
        {
            // null 케이스는 직접 테스트
            Denominator? d1 = value1.HasValue ? Denominator.Create(value1.Value).Match(Succ: x => x, Fail: _ => null) : null;
            Denominator? d2 = value2.HasValue ? Denominator.Create(value2.Value).Match(Succ: x => x, Fail: _ => null) : null;
            var result = d1 >= d2;
            result.ShouldBe(expected);
        }
    }
}

