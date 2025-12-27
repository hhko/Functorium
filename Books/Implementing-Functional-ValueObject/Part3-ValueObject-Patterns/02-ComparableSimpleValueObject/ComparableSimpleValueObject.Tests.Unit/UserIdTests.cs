using ComparableSimpleValueObject.ValueObjects;
using LanguageExt;

namespace ComparableSimpleValueObject.Tests.Unit;

/// <summary>
/// UserId 값 객체의 ComparableSimpleValueObject 패턴 테스트
///
/// 학습 목표:
/// 1. 비교 가능한 primitive 값 객체 패턴 이해
/// 2. ComparableSimpleValueObject<T> 기반 값 객체 비교 검증
/// 3. 암시적 변환 연산자 동작 확인
/// </summary>
[Trait("Part3-Patterns", "02-ComparableSimpleValueObject")]
public class UserIdTests
{
    // 테스트 시나리오: 양수 값으로 UserId 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenValueIsPositive()
    {
        // Arrange
        int value = 1;

        // Act
        Fin<UserId> actual = UserId.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: userId =>
            {
                userId.Id.ShouldBe(value);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 0으로 UserId 생성 실패
    [Fact]
    public void Create_ReturnsFail_WhenValueIsZero()
    {
        // Arrange
        int value = 0;

        // Act
        Fin<UserId> actual = UserId.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 음수로 UserId 생성 실패
    [Fact]
    public void Create_ReturnsFail_WhenValueIsNegative()
    {
        // Arrange
        int value = -1;

        // Act
        Fin<UserId> actual = UserId.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 동일한 값의 두 UserId는 동등해야 함
    [Fact]
    public void Equals_ReturnsTrue_WhenUserIdsHaveSameValue()
    {
        // Arrange
        var userId1 = UserId.Create(1).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));
        var userId2 = UserId.Create(1).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        userId1.Equals(userId2).ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 값의 두 UserId는 동등하지 않아야 함
    [Fact]
    public void Equals_ReturnsFalse_WhenUserIdsHaveDifferentValues()
    {
        // Arrange
        var userId1 = UserId.Create(1).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));
        var userId2 = UserId.Create(2).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        userId1.Equals(userId2).ShouldBeFalse();
    }

    // 테스트 시나리오: UserId 비교 연산 (작음)
    [Fact]
    public void CompareTo_ReturnsNegative_WhenFirstUserIdIsSmaller()
    {
        // Arrange
        var userId1 = UserId.Create(1).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));
        var userId2 = UserId.Create(2).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = userId1.CompareTo(userId2);

        // Assert
        actual.ShouldBeLessThan(0);
    }

    // 테스트 시나리오: UserId 비교 연산 (큼)
    [Fact]
    public void CompareTo_ReturnsPositive_WhenFirstUserIdIsLarger()
    {
        // Arrange
        var userId1 = UserId.Create(2).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));
        var userId2 = UserId.Create(1).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = userId1.CompareTo(userId2);

        // Assert
        actual.ShouldBeGreaterThan(0);
    }

    // 테스트 시나리오: UserId 비교 연산 (같음)
    [Fact]
    public void CompareTo_ReturnsZero_WhenUserIdsAreEqual()
    {
        // Arrange
        var userId1 = UserId.Create(1).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));
        var userId2 = UserId.Create(1).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = userId1.CompareTo(userId2);

        // Assert
        actual.ShouldBe(0);
    }

    // 테스트 시나리오: 암시적 변환 연산자 동작
    [Fact]
    public void ImplicitConversion_ReturnsIntValue_WhenUserIdIsConverted()
    {
        // Arrange
        var userId = UserId.Create(42).Match(
            Succ: u => u,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = userId;

        // Assert
        actual.ShouldBe(42);
    }

    // 테스트 시나리오: UserId 정렬 동작 검증
    [Fact]
    public void Sort_SortsUserIdsCorrectly_WhenListContainsMultipleIds()
    {
        // Arrange
        var userIds = new List<UserId>
        {
            UserId.Create(3).Match(Succ: u => u, Fail: _ => throw new Exception("생성 실패")),
            UserId.Create(1).Match(Succ: u => u, Fail: _ => throw new Exception("생성 실패")),
            UserId.Create(2).Match(Succ: u => u, Fail: _ => throw new Exception("생성 실패"))
        };

        // Act
        userIds.Sort();

        // Assert
        userIds[0].Id.ShouldBe(1);
        userIds[1].Id.ShouldBe(2);
        userIds[2].Id.ShouldBe(3);
    }
}
