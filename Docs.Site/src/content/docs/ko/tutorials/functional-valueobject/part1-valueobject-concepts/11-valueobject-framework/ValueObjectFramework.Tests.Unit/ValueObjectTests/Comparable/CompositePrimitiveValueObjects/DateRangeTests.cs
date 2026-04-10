using ValueObjectFramework.ValueObjects.Comparable.CompositePrimitiveValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.Comparable.CompositePrimitiveValueObjects;

/// <summary>
/// DateRange 값 객체 테스트
/// ComparableValueObject 기반으로 비교 가능한 복합 primitive 값 객체 구현
/// 
/// 테스트 목적:
/// 1. 날짜 범위 생성 및 검증 검증
/// 2. LINQ Expression을 활용한 함수형 체이닝 검증
/// 3. 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "DateRangeTests")]
public class DateRangeTests
{
    // 테스트 시나리오: 유효한 날짜 범위로 DateRange 인스턴스를 생성할 수 있어야 한다
    [Fact]
    public void Create_ShouldReturnSuccessResult_WhenValidDateRange()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        var actual = DateRange.Create(startDate, endDate);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(range =>
        {
            range.StartDate.ShouldBe(startDate);
            range.EndDate.ShouldBe(endDate);
        });
    }

    // 테스트 시나리오: 시작일이 종료일보다 늦을 때 DateRange 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailureResult_WhenStartDateAfterEndDate()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);

        // Act
        var actual = DateRange.Create(startDate, endDate);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("시작일은 종료일보다 이전이어야 합니다"));
    }

    // 테스트 시나리오: 시작일과 종료일이 같을 때 DateRange 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailureResult_WhenSameStartAndEndDate()
    {
        // Arrange
        var sameDate = new DateTime(2024, 6, 15);

        // Act
        var actual = DateRange.Create(sameDate, sameDate);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("시작일은 종료일보다 이전이어야 합니다"));
    }

    // 테스트 시나리오: DateRange 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Fact]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenSameDateRange()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var range1 = DateRange.Create(startDate, endDate).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = DateRange.Create(startDate, endDate).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = range1.Equals(range2);

        // Assert
        actual.ShouldBeTrue();
        range1.GetHashCode().ShouldBe(range2.GetHashCode());
    }

    // 테스트 시나리오: DateRange 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Fact]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenDifferentDateRange()
    {
        // Arrange
        var startDate1 = new DateTime(2024, 1, 1);
        var endDate1 = new DateTime(2024, 6, 30);
        var startDate2 = new DateTime(2024, 7, 1);
        var endDate2 = new DateTime(2024, 12, 31);
        var range1 = DateRange.Create(startDate1, endDate1).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = DateRange.Create(startDate2, endDate2).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = range1.Equals(range2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: DateRange 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Fact]
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenEarlierRange()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 12, 31)).IfFail(_ => throw new Exception("생성 실패"));

        // Act & Assert
        (range1 < range2).ShouldBeTrue();
        (range1 <= range2).ShouldBeTrue();
        (range1 > range2).ShouldBeFalse();
        (range1 >= range2).ShouldBeFalse();
    }

    // 테스트 시나리오: DateRange 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Fact]
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenSameRange()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 6, 30);
        var range1 = DateRange.Create(startDate, endDate).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = DateRange.Create(startDate, endDate).IfFail(_ => throw new Exception("생성 실패"));

        // Act & Assert
        (range1 == range2).ShouldBeTrue();
        (range1 != range2).ShouldBeFalse();
        (range1 <= range2).ShouldBeTrue();
        (range1 >= range2).ShouldBeTrue();
    }

    // 테스트 시나리오: DateRange 인스턴스들이 올바르게 비교 연산자를 사용해야 한다
    [Fact]
    public void ComparisonOperators_ShouldReturnCorrectResults_WhenLaterRange()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 12, 31)).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).IfFail(_ => throw new Exception("생성 실패"));

        // Act & Assert
        (range1 > range2).ShouldBeTrue();
        (range1 >= range2).ShouldBeTrue();
        (range1 < range2).ShouldBeFalse();
        (range1 <= range2).ShouldBeFalse();
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식으로 날짜 범위 정보를 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnFormattedDateRangeInfo_WhenCalled()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var range = DateRange.Create(startDate, endDate).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = range.ToString();

        // Assert
        actual.ShouldBe("2024-01-01 ~ 2024-12-31");
    }

    // 테스트 시나리오: DateRange 인스턴스들이 올바르게 비교되어야 한다
    [Fact]
    public void CompareTo_ShouldReturnCorrectComparisonResult_WhenComparingDateRanges()
    {
        // Arrange
        var range1 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).IfFail(_ => throw new Exception("생성 실패"));
        var range2 = DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 12, 31)).IfFail(_ => throw new Exception("생성 실패"));
        var range3 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).IfFail(_ => throw new Exception("생성 실패"));

        // Act & Assert
        range1.CompareTo(range2).ShouldBeLessThan(0);
        range2.CompareTo(range1).ShouldBeGreaterThan(0);
        range1.CompareTo(range3).ShouldBe(0);
    }
}


