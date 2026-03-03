namespace SchedulingDomain.Tests.Unit;

/// <summary>
/// DateRange 값 객체 테스트
///
/// 학습 목표:
/// 1. 날짜 범위 검증 (시작 ≤ 종료)
/// 2. 포함 검사 (Contains)
/// 3. 겹침 검사 (Overlaps, Intersect)
/// </summary>
[Trait("Part5-Scheduling-Domain", "DateRangeTests")]
public class DateRangeTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenEndIsAfterStart()
    {
        // Arrange
        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2025, 1, 10);

        // Act
        var actual = DateRange.Create(start, end);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsSuccess_WhenStartEqualsEnd()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 1);

        // Act
        var actual = DateRange.Create(date, date);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenEndIsBeforeStart()
    {
        // Arrange
        var start = new DateOnly(2025, 1, 10);
        var end = new DateOnly(2025, 1, 1);

        // Act
        var actual = DateRange.Create(start, end);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region TotalDays 테스트

    [Fact]
    public void TotalDays_Returns1_WhenStartEqualsEnd()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 1);
        var range = DateRange.Create(date, date).Match(r => r, _ => null!);

        // Act & Assert
        range.TotalDays.ShouldBe(1);
    }

    [Fact]
    public void TotalDays_ReturnsCorrectValue()
    {
        // Arrange
        var range = DateRange.Create(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 10)
        ).Match(r => r, _ => null!);

        // Act & Assert
        range.TotalDays.ShouldBe(10);
    }

    #endregion

    #region Contains 테스트

    [Fact]
    public void Contains_ReturnsTrue_WhenDateIsInRange()
    {
        // Arrange
        var range = DateRange.Create(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 10)
        ).Match(r => r, _ => null!);

        // Act & Assert
        range.Contains(new DateOnly(2025, 1, 5)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsTrue_WhenDateIsOnBoundary()
    {
        // Arrange
        var range = DateRange.Create(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 10)
        ).Match(r => r, _ => null!);

        // Act & Assert
        range.Contains(new DateOnly(2025, 1, 1)).ShouldBeTrue();
        range.Contains(new DateOnly(2025, 1, 10)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenDateIsOutsideRange()
    {
        // Arrange
        var range = DateRange.Create(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 10)
        ).Match(r => r, _ => null!);

        // Act & Assert
        range.Contains(new DateOnly(2024, 12, 31)).ShouldBeFalse();
        range.Contains(new DateOnly(2025, 1, 11)).ShouldBeFalse();
    }

    #endregion

    #region Overlaps 테스트

    [Fact]
    public void Overlaps_ReturnsTrue_WhenRangesOverlap()
    {
        // Arrange
        var range1 = DateRange.Create(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)).Match(r => r, _ => null!);
        var range2 = DateRange.Create(new DateOnly(2025, 1, 8), new DateOnly(2025, 1, 15)).Match(r => r, _ => null!);

        // Act & Assert
        range1.Overlaps(range2).ShouldBeTrue();
    }

    [Fact]
    public void Overlaps_ReturnsTrue_WhenRangesTouchOnBoundary()
    {
        // Arrange
        var range1 = DateRange.Create(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)).Match(r => r, _ => null!);
        var range2 = DateRange.Create(new DateOnly(2025, 1, 10), new DateOnly(2025, 1, 15)).Match(r => r, _ => null!);

        // Act & Assert
        range1.Overlaps(range2).ShouldBeTrue();
    }

    [Fact]
    public void Overlaps_ReturnsFalse_WhenRangesDoNotOverlap()
    {
        // Arrange
        var range1 = DateRange.Create(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)).Match(r => r, _ => null!);
        var range2 = DateRange.Create(new DateOnly(2025, 1, 20), new DateOnly(2025, 1, 25)).Match(r => r, _ => null!);

        // Act & Assert
        range1.Overlaps(range2).ShouldBeFalse();
    }

    #endregion

    #region Intersect 테스트

    [Fact]
    public void Intersect_ReturnsIntersection_WhenRangesOverlap()
    {
        // Arrange
        var range1 = DateRange.Create(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)).Match(r => r, _ => null!);
        var range2 = DateRange.Create(new DateOnly(2025, 1, 8), new DateOnly(2025, 1, 15)).Match(r => r, _ => null!);

        // Act
        var intersection = range1.Intersect(range2);

        // Assert
        intersection.IsSome.ShouldBeTrue();
        intersection.Match(
            Some: r =>
            {
                r.Start.ShouldBe(new DateOnly(2025, 1, 8));
                r.End.ShouldBe(new DateOnly(2025, 1, 10));
            },
            None: () => throw new Exception("Expected Some")
        );
    }

    [Fact]
    public void Intersect_ReturnsNone_WhenRangesDoNotOverlap()
    {
        // Arrange
        var range1 = DateRange.Create(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)).Match(r => r, _ => null!);
        var range2 = DateRange.Create(new DateOnly(2025, 1, 20), new DateOnly(2025, 1, 25)).Match(r => r, _ => null!);

        // Act
        var intersection = range1.Intersect(range2);

        // Assert
        intersection.IsNone.ShouldBeTrue();
    }

    #endregion
}
