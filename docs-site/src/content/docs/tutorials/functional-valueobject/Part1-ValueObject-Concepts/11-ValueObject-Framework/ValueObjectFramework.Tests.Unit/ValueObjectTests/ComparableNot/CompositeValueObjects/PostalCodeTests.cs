using ValueObjectFramework.ValueObjects.ComparableNot.CompositeValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.ComparableNot.CompositeValueObjects;

/// <summary>
/// PostalCode 값 객체의 생성 및 검증 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 우편번호로 PostalCode 생성 검증
/// 2. 무효한 우편번호(빈 문자열, 잘못된 형식)로 PostalCode 생성 실패 검증
/// 3. 명시적 변환 연산자 검증
/// 4. 동등성 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "PostalCodeTests")]
public class PostalCodeTests
{
    // 테스트 시나리오: 유효한 우편번호로 PostalCode를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnSuccess_WhenPostalCodeIsValid()
    {
        // Arrange
        string postalCode = "12345";

        // Act
        var actual = PostalCode.Create(postalCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(postal => ((string)postal).ShouldBe(postalCode));
    }

    // 테스트 시나리오: 빈 우편번호로 PostalCode 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenPostalCodeIsEmpty()
    {
        // Arrange
        string postalCode = "";

        // Act
        var actual = PostalCode.Create(postalCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("우편번호는 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: 명시적 변환 연산자가 올바른 값을 반환해야 한다
    [Fact]
    public void ExplicitOperator_ShouldReturnCorrectValue_WhenConvertingToString()
    {
        // Arrange
        var postalCode = PostalCode.Create("10001").IfFail(_ => throw new Exception("생성 실패"));
        string expected = "10001";

        // Act
        string actual = (string)postalCode;

        // Assert
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 동일한 우편번호를 가진 두 PostalCode는 동등해야 한다
    [Fact]
    public void Equals_ShouldReturnTrue_WhenPostalCodesAreEqual()
    {
        // Arrange
        var postalCode1 = PostalCode.Create("12345").IfFail(_ => throw new Exception("생성 실패"));
        var postalCode2 = PostalCode.Create("12345").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = postalCode1.Equals(postalCode2);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 우편번호를 가진 두 PostalCode는 동등하지 않아야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenPostalCodesAreDifferent()
    {
        // Arrange
        var postalCode1 = PostalCode.Create("12345").IfFail(_ => throw new Exception("생성 실패"));
        var postalCode2 = PostalCode.Create("54321").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = postalCode1.Equals(postalCode2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: ToString 메서드는 우편번호의 문자열 표현을 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnPostalCodeStringRepresentation_WhenCalled()
    {
        // Arrange
        var postalCode = PostalCode.Create("12345").IfFail(_ => throw new Exception("생성 실패"));
        string expected = "12345";

        // Act
        var actual = postalCode.ToString();

        // Assert
        actual.ShouldBe(expected);
    }
}
