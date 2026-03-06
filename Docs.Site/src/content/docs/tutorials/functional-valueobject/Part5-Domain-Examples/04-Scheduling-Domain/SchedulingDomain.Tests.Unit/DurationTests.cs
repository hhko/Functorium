namespace SchedulingDomain.Tests.Unit;

/// <summary>
/// Duration 값 객체 테스트
///
/// 학습 목표:
/// 1. 기간 생성 검증 (분, 시간, 일)
/// 2. 단위 변환 검증
/// 3. 산술 연산 검증 (Add, Subtract)
/// </summary>
[Trait("Part5-Scheduling-Domain", "DurationTests")]
public class DurationTests
{
    #region 생성 테스트

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(525600)]
    public void FromMinutes_ReturnsSuccess_WhenValueIsValid(int minutes)
    {
        // Act
        var actual = Duration.FromMinutes(minutes);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void FromMinutes_ReturnsFail_WhenValueIsNegative()
    {
        // Act
        var actual = Duration.FromMinutes(-10);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void FromMinutes_ReturnsFail_WhenValueExceedsMaximum()
    {
        // Act - 1년 초과
        var actual = Duration.FromMinutes(525601);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void FromHours_ConvertsToMinutes()
    {
        // Act
        var actual = Duration.FromHours(2);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: d => d.TotalMinutes.ShouldBe(120),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void FromDays_ConvertsToMinutes()
    {
        // Act
        var actual = Duration.FromDays(1);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: d => d.TotalMinutes.ShouldBe(1440),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    #endregion

    #region 단위 변환 테스트

    [Fact]
    public void TotalHours_ReturnsCorrectValue()
    {
        // Arrange
        var duration = Duration.FromMinutes(90).Match(d => d, _ => Duration.Zero);

        // Act & Assert
        duration.TotalHours.ShouldBe(1.5);
    }

    [Fact]
    public void TotalDays_ReturnsCorrectValue()
    {
        // Arrange
        var duration = Duration.FromMinutes(1440).Match(d => d, _ => Duration.Zero);

        // Act & Assert
        duration.TotalDays.ShouldBe(1.0);
    }

    #endregion

    #region 산술 연산 테스트

    [Fact]
    public void Add_ReturnsCombinedDuration()
    {
        // Arrange
        var d1 = Duration.FromMinutes(60).Match(d => d, _ => Duration.Zero);
        var d2 = Duration.FromMinutes(30).Match(d => d, _ => Duration.Zero);

        // Act
        var actual = d1.Add(d2);

        // Assert
        actual.TotalMinutes.ShouldBe(90);
    }

    [Fact]
    public void Subtract_ReturnsSubtractedDuration()
    {
        // Arrange
        var d1 = Duration.FromMinutes(60).Match(d => d, _ => Duration.Zero);
        var d2 = Duration.FromMinutes(30).Match(d => d, _ => Duration.Zero);

        // Act
        var actual = d1.Subtract(d2);

        // Assert
        actual.TotalMinutes.ShouldBe(30);
    }

    [Fact]
    public void Subtract_ReturnsZero_WhenResultWouldBeNegative()
    {
        // Arrange
        var d1 = Duration.FromMinutes(30).Match(d => d, _ => Duration.Zero);
        var d2 = Duration.FromMinutes(60).Match(d => d, _ => Duration.Zero);

        // Act
        var actual = d1.Subtract(d2);

        // Assert
        actual.TotalMinutes.ShouldBe(0);
    }

    #endregion

    #region 비교 테스트

    [Fact]
    public void CompareTo_ReturnsNegative_WhenShorter()
    {
        // Arrange
        var d1 = Duration.FromMinutes(30).Match(d => d, _ => Duration.Zero);
        var d2 = Duration.FromMinutes(60).Match(d => d, _ => Duration.Zero);

        // Act & Assert
        d1.CompareTo(d2).ShouldBeLessThan(0);
    }

    #endregion

    #region ToString 테스트

    [Fact]
    public void ToString_ReturnsMinutes_WhenLessThanHour()
    {
        // Arrange
        var duration = Duration.FromMinutes(45).Match(d => d, _ => Duration.Zero);

        // Act & Assert
        duration.ToString().ShouldBe("45분");
    }

    [Fact]
    public void ToString_ReturnsHours_WhenExactHours()
    {
        // Arrange
        var duration = Duration.FromMinutes(120).Match(d => d, _ => Duration.Zero);

        // Act & Assert
        duration.ToString().ShouldBe("2시간");
    }

    [Fact]
    public void ToString_ReturnsHoursAndMinutes_WhenMixed()
    {
        // Arrange
        var duration = Duration.FromMinutes(90).Match(d => d, _ => Duration.Zero);

        // Act & Assert
        duration.ToString().ShouldBe("1시간 30분");
    }

    #endregion
}
