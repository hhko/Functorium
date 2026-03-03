namespace SchedulingDomain.Tests.Unit;

/// <summary>
/// RecurrenceRule 값 객체 테스트
///
/// 학습 목표:
/// 1. 반복 규칙 생성 검증 (Daily, Weekly, Monthly)
/// 2. 발생일 계산 검증 (GetOccurrences)
/// </summary>
[Trait("Part5-Scheduling-Domain", "RecurrenceRuleTests")]
public class RecurrenceRuleTests
{
    #region Daily 테스트

    [Fact]
    public void Daily_ReturnsSuccess_WhenIntervalIsValid()
    {
        // Act
        var actual = RecurrenceRule.Daily();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Daily_ReturnsFail_WhenIntervalIsInvalid()
    {
        // Act
        var actual = RecurrenceRule.Daily(0);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Daily_GetOccurrences_ReturnsConsecutiveDays()
    {
        // Arrange
        var rule = RecurrenceRule.Daily().Match(r => r, _ => null!);
        var startDate = new DateOnly(2025, 1, 1);

        // Act
        var occurrences = rule.GetOccurrences(startDate, 5).ToList();

        // Assert
        occurrences.Count.ShouldBe(5);
        occurrences[0].ShouldBe(new DateOnly(2025, 1, 1));
        occurrences[1].ShouldBe(new DateOnly(2025, 1, 2));
        occurrences[2].ShouldBe(new DateOnly(2025, 1, 3));
    }

    #endregion

    #region Weekly 테스트

    [Fact]
    public void Weekly_ReturnsSuccess_WhenDaysAreSpecified()
    {
        // Act
        var actual = RecurrenceRule.Weekly(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Weekly_ReturnsFail_WhenNoDaysSpecified()
    {
        // Act
        var actual = RecurrenceRule.Weekly();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Weekly_GetOccurrences_ReturnsSpecifiedDays()
    {
        // Arrange - 월, 수, 금
        var rule = RecurrenceRule.Weekly(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday).Match(r => r, _ => null!);
        var startDate = new DateOnly(2025, 1, 1); // 수요일

        // Act
        var occurrences = rule.GetOccurrences(startDate, 5).ToList();

        // Assert
        occurrences.Count.ShouldBe(5);
        occurrences[0].DayOfWeek.ShouldBe(DayOfWeek.Wednesday); // 1/1
        occurrences[1].DayOfWeek.ShouldBe(DayOfWeek.Friday);    // 1/3
        occurrences[2].DayOfWeek.ShouldBe(DayOfWeek.Monday);    // 1/6
    }

    [Fact]
    public void Weekdays_ReturnsWeekdayRule()
    {
        // Act
        var actual = RecurrenceRule.Weekdays();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: r =>
            {
                r.DaysOfWeek.ShouldContain(DayOfWeek.Monday);
                r.DaysOfWeek.ShouldContain(DayOfWeek.Tuesday);
                r.DaysOfWeek.ShouldContain(DayOfWeek.Wednesday);
                r.DaysOfWeek.ShouldContain(DayOfWeek.Thursday);
                r.DaysOfWeek.ShouldContain(DayOfWeek.Friday);
                r.DaysOfWeek.ShouldNotContain(DayOfWeek.Saturday);
                r.DaysOfWeek.ShouldNotContain(DayOfWeek.Sunday);
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    #endregion

    #region Monthly 테스트

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(31)]
    public void Monthly_ReturnsSuccess_WhenDayIsValid(int dayOfMonth)
    {
        // Act
        var actual = RecurrenceRule.Monthly(dayOfMonth);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Monthly_ReturnsFail_WhenDayIsInvalid(int dayOfMonth)
    {
        // Act
        var actual = RecurrenceRule.Monthly(dayOfMonth);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Monthly_GetOccurrences_ReturnsSpecifiedDay()
    {
        // Arrange - 매월 15일
        var rule = RecurrenceRule.Monthly(15).Match(r => r, _ => null!);
        var startDate = new DateOnly(2025, 1, 1);

        // Act
        var occurrences = rule.GetOccurrences(startDate, 3).ToList();

        // Assert
        occurrences.Count.ShouldBe(3);
        occurrences[0].ShouldBe(new DateOnly(2025, 1, 15));
        occurrences[1].ShouldBe(new DateOnly(2025, 2, 15));
        occurrences[2].ShouldBe(new DateOnly(2025, 3, 15));
    }

    #endregion

    #region ToString 테스트

    [Fact]
    public void ToString_ReturnsKorean_ForDaily()
    {
        // Arrange
        var rule = RecurrenceRule.Daily().Match(r => r, _ => null!);

        // Act & Assert
        rule.ToString().ShouldBe("매일");
    }

    [Fact]
    public void ToString_ReturnsKorean_ForWeekly()
    {
        // Arrange
        var rule = RecurrenceRule.Weekly(DayOfWeek.Monday, DayOfWeek.Wednesday).Match(r => r, _ => null!);

        // Act & Assert
        rule.ToString().ShouldContain("매주");
        rule.ToString().ShouldContain("월");
        rule.ToString().ShouldContain("수");
    }

    [Fact]
    public void ToString_ReturnsKorean_ForMonthly()
    {
        // Arrange
        var rule = RecurrenceRule.Monthly(15).Match(r => r, _ => null!);

        // Act & Assert
        rule.ToString().ShouldBe("매월 15일");
    }

    #endregion
}
