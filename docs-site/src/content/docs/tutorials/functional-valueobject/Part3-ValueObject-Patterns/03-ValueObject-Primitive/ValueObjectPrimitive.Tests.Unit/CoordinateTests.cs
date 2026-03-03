using ValueObjectPrimitive.ValueObjects;
using LanguageExt;

namespace ValueObjectPrimitive.Tests.Unit;

/// <summary>
/// Coordinate 값 객체의 ValueObject (복합 primitive) 패턴 테스트
///
/// 학습 목표:
/// 1. 비교 불가능한 복합 primitive 값 객체 패턴 이해
/// 2. 여러 primitive 값 조합의 값 객체 생성 검증
/// 3. 동등성 비교만 제공하는 패턴 확인
/// </summary>
[Trait("Part3-Patterns", "03-ValueObject-Primitive")]
public class CoordinateTests
{
    // 테스트 시나리오: 유효한 좌표로 Coordinate 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenCoordinatesAreValid()
    {
        // Arrange
        int x = 10;
        int y = 20;

        // Act
        Fin<Coordinate> actual = Coordinate.Create(x, y);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: coordinate =>
            {
                coordinate.X.ShouldBe(x);
                coordinate.Y.ShouldBe(y);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: X 좌표가 음수일 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenXIsNegative()
    {
        // Arrange
        int x = -1;
        int y = 20;

        // Act
        Fin<Coordinate> actual = Coordinate.Create(x, y);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: Y 좌표가 음수일 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenYIsNegative()
    {
        // Arrange
        int x = 10;
        int y = -1;

        // Act
        Fin<Coordinate> actual = Coordinate.Create(x, y);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: Y 좌표가 1000 초과일 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenYExceedsMaximum()
    {
        // Arrange
        int x = 10;
        int y = 1001;

        // Act
        Fin<Coordinate> actual = Coordinate.Create(x, y);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 경계값 (0, 0) 좌표 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenCoordinatesAreZero()
    {
        // Arrange
        int x = 0;
        int y = 0;

        // Act
        Fin<Coordinate> actual = Coordinate.Create(x, y);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: 경계값 (max, 1000) 좌표 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenYIsAtMaximum()
    {
        // Arrange
        int x = 100;
        int y = 1000;

        // Act
        Fin<Coordinate> actual = Coordinate.Create(x, y);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: 동일한 좌표의 두 Coordinate는 동등해야 함
    [Fact]
    public void Equals_ReturnsTrue_WhenCoordinatesHaveSameValues()
    {
        // Arrange
        var coord1 = Coordinate.Create(10, 20).Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));
        var coord2 = Coordinate.Create(10, 20).Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        coord1.Equals(coord2).ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 좌표의 두 Coordinate는 동등하지 않아야 함
    [Fact]
    public void Equals_ReturnsFalse_WhenCoordinatesHaveDifferentValues()
    {
        // Arrange
        var coord1 = Coordinate.Create(10, 20).Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));
        var coord2 = Coordinate.Create(30, 40).Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        coord1.Equals(coord2).ShouldBeFalse();
    }

    // 테스트 시나리오: X만 다른 경우 동등하지 않음
    [Fact]
    public void Equals_ReturnsFalse_WhenOnlyXDiffers()
    {
        // Arrange
        var coord1 = Coordinate.Create(10, 20).Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));
        var coord2 = Coordinate.Create(11, 20).Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        coord1.Equals(coord2).ShouldBeFalse();
    }

    // 테스트 시나리오: ToString 메서드가 좌표 형식 문자열 반환
    [Fact]
    public void ToString_ReturnsFormattedString_WhenCoordinateIsValid()
    {
        // Arrange
        var coordinate = Coordinate.Create(10, 20).Match(
            Succ: c => c,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        string actual = coordinate.ToString();

        // Assert
        actual.ShouldBe("(10, 20)");
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        int x = 10;
        int y = 20;

        // Act
        Fin<Coordinate> actual1 = Coordinate.Create(x, y);
        Fin<Coordinate> actual2 = Coordinate.Create(x, y);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }
}
