using ConstrainedTypes.ValueObjects;

namespace ConstrainedTypes.Tests.Unit;

/// <summary>
/// NonNegativeInt 값 객체의 경계값 테스트
///
/// 테스트 목적:
/// 1. 0 이상의 값으로 생성 성공
/// 2. 음수 값으로 생성 실패
/// </summary>
[Trait("Part1-Semantic-Types", "03-ConstrainedTypes")]
public class NonNegativeIntTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenZero()
    {
        // Act
        var actual = NonNegativeInt.Create(0);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsSuccess_WhenPositive()
    {
        // Act
        var actual = NonNegativeInt.Create(42);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenNegative()
    {
        // Act
        var actual = NonNegativeInt.Create(-1);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
