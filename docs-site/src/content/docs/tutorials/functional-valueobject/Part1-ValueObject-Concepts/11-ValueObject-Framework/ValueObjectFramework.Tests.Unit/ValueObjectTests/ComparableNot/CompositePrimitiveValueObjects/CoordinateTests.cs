using ValueObjectFramework.ValueObjects.ComparableNot.CompositePrimitiveValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.ComparableNot.CompositePrimitiveValueObjects;

/// <summary>
/// Coordinate 값 객체의 생성 및 검증 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 좌표로 Coordinate 생성 검증
/// 2. 무효한 X 좌표로 Coordinate 생성 실패 검증
/// 3. 무효한 Y 좌표로 Coordinate 생성 실패 검증
/// 4. 동등성 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "CoordinateTests")]
public class CoordinateTests
{
    // 테스트 시나리오: 유효한 좌표로 Coordinate를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnSuccess_WhenCoordinatesAreValid()
    {
        // Arrange
        int x = 100;
        int y = 200;

        // Act
        var actual = Coordinate.Create(x, y);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(coordinate =>
        {
            coordinate.X.ShouldBe(x);
            coordinate.Y.ShouldBe(y);
        });
    }

    // 테스트 시나리오: X 좌표가 범위를 벗어나면 실패해야 한다
    [Theory]
    [InlineData(-1, 200, "X 좌표는 0-1000 범위여야 합니다")]
    [InlineData(1001, 200, "X 좌표는 0-1000 범위여야 합니다")]
    public void Create_ShouldReturnFailure_WhenXCoordinateIsOutOfRange(int x, int y, string expectedErrorMessage)
    {
        // Arrange
        // Act
        var actual = Coordinate.Create(x, y);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe(expectedErrorMessage));
    }

    // 테스트 시나리오: Y 좌표가 범위를 벗어나면 실패해야 한다
    [Theory]
    [InlineData(100, -1, "Y 좌표는 0-1000 범위여야 합니다")]
    [InlineData(100, 1001, "Y 좌표는 0-1000 범위여야 합니다")]
    public void Create_ShouldReturnFailure_WhenYCoordinateIsOutOfRange(int x, int y, string expectedErrorMessage)
    {
        // Arrange
        // Act
        var actual = Coordinate.Create(x, y);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe(expectedErrorMessage));
    }

    // 테스트 시나리오: 동일한 좌표를 가진 두 Coordinate는 동등해야 한다
    [Fact]
    public void Equals_ShouldReturnTrue_WhenCoordinatesAreEqual()
    {
        // Arrange
        var coordinate1 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));
        var coordinate2 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = coordinate1.Equals(coordinate2);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 좌표를 가진 두 Coordinate는 동등하지 않아야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenCoordinatesAreDifferent()
    {
        // Arrange
        var coordinate1 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));
        var coordinate2 = Coordinate.Create(200, 100).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = coordinate1.Equals(coordinate2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: == 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void EqualityOperator_ShouldReturnTrue_WhenCoordinatesAreEqual()
    {
        // Arrange
        var coordinate1 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));
        var coordinate2 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = coordinate1 == coordinate2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: != 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void InequalityOperator_ShouldReturnTrue_WhenCoordinatesAreDifferent()
    {
        // Arrange
        var coordinate1 = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));
        var coordinate2 = Coordinate.Create(200, 100).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = coordinate1 != coordinate2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: ToString 메서드는 좌표의 문자열 표현을 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnCoordinateStringRepresentation_WhenCalled()
    {
        // Arrange
        var coordinate = Coordinate.Create(100, 200).IfFail(_ => throw new Exception("생성 실패"));
        string expected = "(100, 200)";

        // Act
        var actual = coordinate.ToString();

        // Assert
        actual.ShouldBe(expected);
    }
}
