using ValueObjectFramework.ValueObjects.ComparableNot.CompositeValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.ComparableNot.CompositeValueObjects;

/// <summary>
/// Street 값 객체의 생성 및 검증 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 거리명으로 Street 생성 검증
/// 2. 무효한 거리명(빈 문자열)으로 Street 생성 실패 검증
/// 3. 명시적 변환 연산자 검증
/// 4. 동등성 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "StreetTests")]
public class StreetTests
{
    // 테스트 시나리오: 유효한 거리명으로 Street를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnSuccess_WhenStreetNameIsValid()
    {
        // Arrange
        string streetName = "123 Main St";

        // Act
        var actual = Street.Create(streetName);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(street => ((string)street).ShouldBe(streetName));
    }

    // 테스트 시나리오: 빈 거리명으로 Street 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenStreetNameIsEmpty()
    {
        // Arrange
        string streetName = "";

        // Act
        var actual = Street.Create(streetName);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("거리명은 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: 명시적 변환 연산자가 올바른 값을 반환해야 한다
    [Fact]
    public void ExplicitOperator_ShouldReturnCorrectValue_WhenConvertingToString()
    {
        // Arrange
        var street = Street.Create("Broadway").IfFail(_ => throw new Exception("생성 실패"));
        string expected = "Broadway";

        // Act
        string actual = (string)street;

        // Assert
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 동일한 거리명을 가진 두 Street는 동등해야 한다
    [Fact]
    public void Equals_ShouldReturnTrue_WhenStreetNamesAreEqual()
    {
        // Arrange
        var street1 = Street.Create("123 Main St").IfFail(_ => throw new Exception("생성 실패"));
        var street2 = Street.Create("123 Main St").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = street1.Equals(street2);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 거리명을 가진 두 Street는 동등하지 않아야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenStreetNamesAreDifferent()
    {
        // Arrange
        var street1 = Street.Create("123 Main St").IfFail(_ => throw new Exception("생성 실패"));
        var street2 = Street.Create("456 Oak Ave").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = street1.Equals(street2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: ToString 메서드는 거리명의 문자열 표현을 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnStreetNameStringRepresentation_WhenCalled()
    {
        // Arrange
        var street = Street.Create("Broadway").IfFail(_ => throw new Exception("생성 실패"));
        string expected = "Broadway";

        // Act
        var actual = street.ToString();

        // Assert
        actual.ShouldBe(expected);
    }
}
