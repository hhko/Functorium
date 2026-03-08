using ConstrainedTypes.ValueObjects;

namespace ConstrainedTypes.Tests.Unit;

/// <summary>
/// String50 값 객체의 경계값 테스트
///
/// 테스트 목적:
/// 1. 50자 이하 문자열 생성 성공
/// 2. 51자 이상 문자열 생성 실패
/// 3. 빈 문자열 생성 실패
/// </summary>
[Trait("Part1-Semantic-Types", "03-ConstrainedTypes")]
public class String50Tests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenLengthIs50()
    {
        // Arrange
        var value = new string('a', 50);

        // Act
        var actual = String50.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenLengthIs51()
    {
        // Arrange
        var value = new string('a', 51);

        // Act
        var actual = String50.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenEmpty()
    {
        // Act
        var actual = String50.Create("");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsSuccess_WhenSingleChar()
    {
        // Act
        var actual = String50.Create("a");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
