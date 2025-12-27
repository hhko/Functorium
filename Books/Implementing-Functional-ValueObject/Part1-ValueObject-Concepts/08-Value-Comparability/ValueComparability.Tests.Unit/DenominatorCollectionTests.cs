using ValueComparability.ValueObjects;

namespace ValueComparability.Tests.Unit;

/// <summary>
/// Denominator 값 객체의 컬렉션 비교 기능 테스트
/// 
/// 테스트 목적:
/// 1. 정렬 기능 검증
/// 2. Min/Max 기능 검증
/// 3. 이진 검색 기능 검증
/// 4. 컬렉션 성능 최적화 검증
/// </summary>
[Trait("Concept-08-Value-Comparability", "DenominatorCollectionTests")]
public class DenominatorCollectionTests
{
    // 테스트 시나리오: List에서 Sort 메서드가 올바르게 정렬해야 한다
    [Fact]
    public void Sort_ShouldOrderDenominatorsCorrectly_WhenUsingListSort()
    {
        // Arrange
        var values = new[] { 10, 3, 7, 1, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var expectedOrder = new[] { 1, 3, 7, 10, 15 };

        // Act
        denominators.Sort();

        // Assert
        var actualOrder = denominators.Select(d => (int)d).ToArray();
        actualOrder.ShouldBe(expectedOrder);
    }

    // 테스트 시나리오: Min 메서드가 최소값을 올바르게 반환해야 한다
    [Fact]
    public void Min_ShouldReturnSmallestValue_WhenUsingMinMethod()
    {
        // Arrange
        var values = new[] { 10, 3, 7, 1, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var expected = 1;

        // Act
        var actual = denominators.Min();

        // Assert
        ((int)actual).ShouldBe(expected);
    }

    // 테스트 시나리오: Max 메서드가 최대값을 올바르게 반환해야 한다
    [Fact]
    public void Max_ShouldReturnLargestValue_WhenUsingMaxMethod()
    {
        // Arrange
        var values = new[] { 10, 3, 7, 1, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var expected = 15;

        // Act
        var actual = denominators.Max();

        // Assert
        ((int)actual).ShouldBe(expected);
    }

    // 테스트 시나리오: 정렬된 리스트에서 BinarySearch가 올바른 인덱스를 반환해야 한다
    [Fact]
    public void BinarySearch_ShouldReturnCorrectIndex_WhenSearchingInSortedList()
    {
        // Arrange
        var values = new[] { 1, 3, 5, 7, 9, 11, 13, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var target = Denominator.Create(7).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패"));
        var expected = 3; // 7의 인덱스

        // Act
        var actual = denominators.BinarySearch(target);

        // Assert
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 존재하지 않는 값을 BinarySearch할 때 음수 인덱스를 반환해야 한다
    [Fact]
    public void BinarySearch_ShouldReturnNegativeIndex_WhenValueNotFound()
    {
        // Arrange
        var values = new[] { 1, 3, 5, 7, 9, 11, 13, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var target = Denominator.Create(6).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패"));

        // Act
        var actual = denominators.BinarySearch(target);

        // Assert
        actual.ShouldBeNegative();

        // BinarySearch에서 음수 리턴값에 ~를 적용하면, 실제 삽입될 인덱스를 얻을 수 있음
        //   값이 없으면: (~actual) (즉, actual의 비트 보수 값)을 반환.
        //   여기서 actual는 새 값을 삽입했을 때 들어갈 자리의 인덱스.
        //
        // 6은 5와 7 사이에 있어야 하므로, ~actual은 3 (5의 위치)가 되어야 함
        (~actual).ShouldBe(3);
    }

    // 테스트 시나리오: OrderBy를 사용한 정렬이 올바르게 동작해야 한다
    [Fact]
    public void OrderBy_ShouldOrderDenominatorsCorrectly_WhenUsingLinqOrderBy()
    {
        // Arrange
        var values = new[] { 10, 3, 7, 1, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var expectedOrder = new[] { 1, 3, 7, 10, 15 };

        // Act
        var actual = denominators.OrderBy(d => d).ToList();

        // Assert
        var actualOrder = actual.Select(d => (int)d).ToArray();
        actualOrder.ShouldBe(expectedOrder);
    }

    // 테스트 시나리오: OrderByDescending을 사용한 내림차순 정렬이 올바르게 동작해야 한다
    [Fact]
    public void OrderByDescending_ShouldOrderDenominatorsCorrectly_WhenUsingLinqOrderByDescending()
    {
        // Arrange
        var values = new[] { 10, 3, 7, 1, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var expectedOrder = new[] { 15, 10, 7, 3, 1 };

        // Act
        var actual = denominators.OrderByDescending(d => d).ToList();

        // Assert
        var actualOrder = actual.Select(d => (int)d).ToArray();
        actualOrder.ShouldBe(expectedOrder);
    }

    // 테스트 시나리오: 범위 계산이 올바르게 동작해야 한다
    [Fact]
    public void Range_ShouldCalculateCorrectRange_WhenUsingMaxAndMin()
    {
        // Arrange
        var values = new[] { 10, 3, 7, 1, 15 };
        var denominators = values.Select(v => Denominator.Create(v).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                 .ToList();
        var expected = 14; // 15 - 1

        // Act
        var max = denominators.Max();
        var min = denominators.Min();
        var actual = max - min;

        // Assert
        actual.ShouldBe(expected);
    }
}
