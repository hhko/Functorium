using ValueObjectFramework.ValueObjects.Comparable.PrimitiveValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.Comparable.PrimitiveValueObjects;

/// <summary>
/// Denominator 값 객체의 생성 및 검증 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 값으로 Denominator 생성 검증
/// 2. 무효한 값(0)으로 Denominator 생성 실패 검증
/// 3. 비교 연산자 동작 검증
/// 4. 명시적 변환 연산자 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "DenominatorTests")]
public class DenominatorTests
{
    // 테스트 시나리오: 유효한 값으로 Denominator를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnSuccess_WhenValueIsValid()
    {
        // Arrange
        int value = 5;

        // Act
        var actual = Denominator.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(denominator => ((int)denominator).ShouldBe(value));
    }

    // 테스트 시나리오: 0 값으로 Denominator 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenValueIsZero()
    {
        // Arrange
        int value = 0;

        // Act
        var actual = Denominator.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("0은 허용되지 않습니다"));
    }

    // 테스트 시나리오: 명시적 변환 연산자가 올바른 값을 반환해야 한다
    [Fact]
    public void ExplicitOperator_ShouldReturnCorrectValue_WhenConvertingToInt()
    {
        // Arrange
        var denominator = Denominator.Create(7).IfFail(_ => throw new Exception("생성 실패"));
        int expected = 7;

        // Act
        int actual = (int)denominator;

        // Assert
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 동일한 값을 가진 두 Denominator는 동등해야 한다
    [Fact]
    public void Equals_ShouldReturnTrue_WhenValuesAreEqual()
    {
        // Arrange
        var denominator1 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));
        var denominator2 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = denominator1.Equals(denominator2);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 값을 가진 두 Denominator는 동등하지 않아야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenValuesAreDifferent()
    {
        // Arrange
        var denominator1 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));
        var denominator2 = Denominator.Create(3).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = denominator1.Equals(denominator2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: CompareTo 메서드가 올바른 비교 결과를 반환해야 한다
    [Fact]
    public void CompareTo_ShouldReturnCorrectComparison_WhenComparingValues()
    {
        // Arrange
        var denominator1 = Denominator.Create(3).IfFail(_ => throw new Exception("생성 실패"));
        var denominator2 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = denominator1.CompareTo(denominator2);

        // Assert
        actual.ShouldBe(-1); // 3 < 5
    }

    // 테스트 시나리오: < 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void LessThanOperator_ShouldReturnTrue_WhenLeftIsLessThanRight()
    {
        // Arrange
        var denominator1 = Denominator.Create(3).IfFail(_ => throw new Exception("생성 실패"));
        var denominator2 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = denominator1 < denominator2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: > 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void GreaterThanOperator_ShouldReturnTrue_WhenLeftIsGreaterThanRight()
    {
        // Arrange
        var denominator1 = Denominator.Create(7).IfFail(_ => throw new Exception("생성 실패"));
        var denominator2 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = denominator1 > denominator2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: <= 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void LessThanOrEqualOperator_ShouldReturnTrue_WhenLeftIsLessThanOrEqualRight()
    {
        // Arrange
        var denominator1 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));
        var denominator2 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = denominator1 <= denominator2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: >= 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void GreaterThanOrEqualOperator_ShouldReturnTrue_WhenLeftIsGreaterThanOrEqualRight()
    {
        // Arrange
        var denominator1 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));
        var denominator2 = Denominator.Create(5).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = denominator1 >= denominator2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: ToString 메서드는 래핑된 값의 문자열 표현을 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnValueStringRepresentation_WhenCalled()
    {
        // Arrange
        var denominator = Denominator.Create(42).IfFail(_ => throw new Exception("생성 실패"));
        string expected = "42";

        // Act
        var actual = denominator.ToString();

        // Assert
        actual.ShouldBe(expected);
    }
}
