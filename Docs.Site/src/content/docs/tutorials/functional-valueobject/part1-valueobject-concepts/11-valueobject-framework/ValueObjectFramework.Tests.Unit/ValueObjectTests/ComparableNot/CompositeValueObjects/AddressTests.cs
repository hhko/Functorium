
using ValueObjectFramework.ValueObjects.ComparableNot.CompositeValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.ComparableNot.CompositeValueObjects;

/// <summary>
/// Address 값 객체의 생성 및 검증 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 주소로 Address 생성 검증
/// 2. 무효한 거리명으로 Address 생성 실패 검증
/// 3. 무효한 도시명으로 Address 생성 실패 검증
/// 4. 무효한 우편번호로 Address 생성 실패 검증
/// 5. CreateFromValidated 메서드 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "AddressTests")]
public class AddressTests
{
    // 테스트 시나리오: 유효한 주소로 Address를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnSuccess_WhenAddressIsValid()
    {
        // Arrange
        string streetValue = "123 Main St";
        string cityValue = "Seoul";
        string postalCodeValue = "12345";

        // Act
        var actual = Address.Create(streetValue, cityValue, postalCodeValue);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(address =>
        {
            ((string)address.Street).ShouldBe(streetValue);
            ((string)address.City).ShouldBe(cityValue);
            ((string)address.PostalCode).ShouldBe(postalCodeValue);
        });
    }

    // 테스트 시나리오: 빈 거리명으로 Address 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenStreetIsEmpty()
    {
        // Arrange
        string streetValue = "";
        string cityValue = "Seoul";
        string postalCodeValue = "12345";

        // Act
        var actual = Address.Create(streetValue, cityValue, postalCodeValue);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("거리명은 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: 빈 도시명으로 Address 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenCityIsEmpty()
    {
        // Arrange
        string streetValue = "123 Main St";
        string cityValue = "";
        string postalCodeValue = "12345";

        // Act
        var actual = Address.Create(streetValue, cityValue, postalCodeValue);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("도시명은 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: 무효한 우편번호로 Address 생성 시 실패해야 한다
    [Theory]
    [InlineData("abc123", "우편번호는 5자리 숫자여야 합니다")]
    [InlineData("1234", "우편번호는 5자리 숫자여야 합니다")]
    [InlineData("123456", "우편번호는 5자리 숫자여야 합니다")]
    [InlineData("", "우편번호는 비어있을 수 없습니다")]
    public void Create_ShouldReturnFailure_WhenPostalCodeIsInvalid(string postalCodeValue, string expectedErrorMessage)
    {
        // Arrange
        string streetValue = "123 Main St";
        string cityValue = "Seoul";

        // Act
        var actual = Address.Create(streetValue, cityValue, postalCodeValue);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe(expectedErrorMessage));
    }

    // 테스트 시나리오: 동일한 주소를 가진 두 Address는 동등해야 한다
    [Fact]
    public void Equals_ShouldReturnTrue_WhenAddressesAreEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));
        var address2 = Address.Create("123 Main St", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = address1.Equals(address2);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 주소를 가진 두 Address는 동등하지 않아야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenAddressesAreDifferent()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));
        var address2 = Address.Create("456 Oak Ave", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = address1.Equals(address2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: == 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void EqualityOperator_ShouldReturnTrue_WhenAddressesAreEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));
        var address2 = Address.Create("123 Main St", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = address1 == address2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: != 연산자가 올바른 결과를 반환해야 한다
    [Fact]
    public void InequalityOperator_ShouldReturnTrue_WhenAddressesAreDifferent()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));
        var address2 = Address.Create("456 Oak Ave", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = address1 != address2;

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: ToString 메서드는 주소의 문자열 표현을 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnAddressStringRepresentation_WhenCalled()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Seoul", "12345").IfFail(_ => throw new Exception("생성 실패"));
        string expected = "123 Main St, Seoul 12345";

        // Act
        var actual = address.ToString();

        // Assert
        actual.ShouldBe(expected);
    }

}
