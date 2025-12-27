namespace SchedulingDomain.Tests.Unit;

/// <summary>
/// TimeSlot 값 객체 테스트
///
/// 학습 목표:
/// 1. 시간 슬롯 검증 (시작 < 종료)
/// 2. 포함 검사 (Contains)
/// 3. 충돌 검사 (Conflicts)
/// </summary>
[Trait("Part5-Scheduling-Domain", "TimeSlotTests")]
public class TimeSlotTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenEndIsAfterStart()
    {
        // Arrange
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(10, 30);

        // Act
        var actual = TimeSlot.Create(start, end);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenEndEqualsStart()
    {
        // Arrange
        var time = new TimeOnly(9, 0);

        // Act
        var actual = TimeSlot.Create(time, time);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenEndIsBeforeStart()
    {
        // Arrange
        var start = new TimeOnly(10, 0);
        var end = new TimeOnly(9, 0);

        // Act
        var actual = TimeSlot.Create(start, end);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region Duration 테스트

    [Fact]
    public void Duration_ReturnsCorrectValue()
    {
        // Arrange
        var slot = TimeSlot.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 30)
        ).Match(s => s, _ => null!);

        // Act & Assert
        slot.Duration.TotalMinutes.ShouldBe(90);
    }

    #endregion

    #region Contains 테스트

    [Fact]
    public void Contains_ReturnsTrue_WhenTimeIsInSlot()
    {
        // Arrange
        var slot = TimeSlot.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 30)
        ).Match(s => s, _ => null!);

        // Act & Assert
        slot.Contains(new TimeOnly(9, 30)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsTrue_WhenTimeIsOnStart()
    {
        // Arrange
        var slot = TimeSlot.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 30)
        ).Match(s => s, _ => null!);

        // Act & Assert
        slot.Contains(new TimeOnly(9, 0)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenTimeIsOnEnd()
    {
        // Arrange - End는 exclusive
        var slot = TimeSlot.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 30)
        ).Match(s => s, _ => null!);

        // Act & Assert
        slot.Contains(new TimeOnly(10, 30)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenTimeIsOutside()
    {
        // Arrange
        var slot = TimeSlot.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 30)
        ).Match(s => s, _ => null!);

        // Act & Assert
        slot.Contains(new TimeOnly(8, 30)).ShouldBeFalse();
        slot.Contains(new TimeOnly(11, 0)).ShouldBeFalse();
    }

    #endregion

    #region Conflicts 테스트

    [Fact]
    public void Conflicts_ReturnsTrue_WhenSlotsOverlap()
    {
        // Arrange
        var slot1 = TimeSlot.Create(new TimeOnly(9, 0), new TimeOnly(10, 30)).Match(s => s, _ => null!);
        var slot2 = TimeSlot.Create(new TimeOnly(10, 0), new TimeOnly(11, 0)).Match(s => s, _ => null!);

        // Act & Assert
        slot1.Conflicts(slot2).ShouldBeTrue();
    }

    [Fact]
    public void Conflicts_ReturnsFalse_WhenSlotsDoNotOverlap()
    {
        // Arrange
        var slot1 = TimeSlot.Create(new TimeOnly(9, 0), new TimeOnly(10, 0)).Match(s => s, _ => null!);
        var slot2 = TimeSlot.Create(new TimeOnly(10, 0), new TimeOnly(11, 0)).Match(s => s, _ => null!);

        // Act & Assert - 경계가 같으면 충돌하지 않음
        slot1.Conflicts(slot2).ShouldBeFalse();
    }

    [Fact]
    public void Conflicts_ReturnsFalse_WhenSlotsAreSeparate()
    {
        // Arrange
        var slot1 = TimeSlot.Create(new TimeOnly(9, 0), new TimeOnly(10, 0)).Match(s => s, _ => null!);
        var slot2 = TimeSlot.Create(new TimeOnly(14, 0), new TimeOnly(15, 0)).Match(s => s, _ => null!);

        // Act & Assert
        slot1.Conflicts(slot2).ShouldBeFalse();
    }

    #endregion
}
