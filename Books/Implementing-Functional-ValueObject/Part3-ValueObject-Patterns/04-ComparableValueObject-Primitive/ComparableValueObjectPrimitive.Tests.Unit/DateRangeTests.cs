using ComparableValueObjectPrimitive.ValueObjects;
using LanguageExt;

namespace ComparableValueObjectPrimitive.Tests.Unit;

/// <summary>
/// DateRange 값 객체의 ComparableValueObject (복합 primitive) 패턴 테스트
///
/// 학습 목표:
/// 1. 비교 가능한 복합 primitive 값 객체 패턴 이해
/// 2. 날짜 범위 유효성 검증 동작 확인
/// 3. 비교 기능 자동 제공 검증
/// </summary>
[Trait("Part3-Patterns", "04-ComparableValueObject-Primitive")]
public class DateRangeTests
{
    // 테스트 시나리오: 유효한 날짜 범위로 DateRange 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenDateRangeIsValid()
    {
        // Arrange
        DateTime startDate = new DateTime(2024, 1, 1);
        DateTime endDate = new DateTime(2024, 12, 31);

        // Act
        Fin<DateRange> actual = DateRange.Create(startDate, endDate);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: dateRange =>
            {
                dateRange.StartDate.ShouldBe(startDate);
                dateRange.EndDate.ShouldBe(endDate);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 시작일과 종료일이 같을 때 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenStartDateEqualsEndDate()
    {
        // Arrange
        DateTime date = new DateTime(2024, 6, 15);

        // Act
        Fin<DateRange> actual = DateRange.Create(date, date);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: 시작일이 종료일보다 클 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenStartDateAfterEndDate()
    {
        // Arrange
        DateTime startDate = new DateTime(2024, 12, 31);
        DateTime endDate = new DateTime(2024, 1, 1);

        // Act
        Fin<DateRange> actual = DateRange.Create(startDate, endDate);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 동일한 날짜 범위의 두 DateRange는 동등해야 함
    [Fact]
    public void Equals_ReturnsTrue_WhenDateRangesAreEqual()
    {
        // Arrange
        DateTime startDate = new DateTime(2024, 1, 1);
        DateTime endDate = new DateTime(2024, 12, 31);
        var dateRange1 = DateRange.Create(startDate, endDate).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));
        var dateRange2 = DateRange.Create(startDate, endDate).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        dateRange1.Equals(dateRange2).ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 날짜 범위의 두 DateRange는 동등하지 않아야 함
    [Fact]
    public void Equals_ReturnsFalse_WhenDateRangesAreDifferent()
    {
        // Arrange
        var dateRange1 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));
        var dateRange2 = DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 12, 31)).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        dateRange1.Equals(dateRange2).ShouldBeFalse();
    }

    // 테스트 시나리오: DateRange 비교 연산 (시작일 기준)
    [Fact]
    public void CompareTo_ReturnsNegative_WhenFirstDateRangeStartsEarlier()
    {
        // Arrange
        var dateRange1 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));
        var dateRange2 = DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 12, 31)).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = dateRange1.CompareTo(dateRange2);

        // Assert
        actual.ShouldBeLessThan(0);
    }

    // 테스트 시나리오: DateRange 비교 연산 (동일한 시작일, 종료일 기준)
    [Fact]
    public void CompareTo_ReturnsNegative_WhenSameStartButEarlierEnd()
    {
        // Arrange
        var dateRange1 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 3, 31)).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));
        var dateRange2 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30)).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = dateRange1.CompareTo(dateRange2);

        // Assert
        actual.ShouldBeLessThan(0);
    }

    // 테스트 시나리오: DateRange 정렬 동작 검증
    [Fact]
    public void Sort_SortsDateRangesCorrectly_WhenListContainsMultipleRanges()
    {
        // Arrange
        var dateRanges = new List<DateRange>
        {
            DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 9, 30)).Match(Succ: dr => dr, Fail: _ => throw new Exception("생성 실패")),
            DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 3, 31)).Match(Succ: dr => dr, Fail: _ => throw new Exception("생성 실패")),
            DateRange.Create(new DateTime(2024, 4, 1), new DateTime(2024, 6, 30)).Match(Succ: dr => dr, Fail: _ => throw new Exception("생성 실패"))
        };

        // Act
        dateRanges.Sort();

        // Assert
        dateRanges[0].StartDate.Month.ShouldBe(1);
        dateRanges[1].StartDate.Month.ShouldBe(4);
        dateRanges[2].StartDate.Month.ShouldBe(7);
    }

    // 테스트 시나리오: ToString 메서드가 날짜 범위 형식 문자열 반환
    [Fact]
    public void ToString_ReturnsFormattedString_WhenDateRangeIsValid()
    {
        // Arrange
        var dateRange = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).Match(
            Succ: dr => dr,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        string actual = dateRange.ToString();

        // Assert
        actual.ShouldBe("2024-01-01 ~ 2024-12-31");
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        DateTime startDate = new DateTime(2024, 1, 1);
        DateTime endDate = new DateTime(2024, 12, 31);

        // Act
        Fin<DateRange> actual1 = DateRange.Create(startDate, endDate);
        Fin<DateRange> actual2 = DateRange.Create(startDate, endDate);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }
}
