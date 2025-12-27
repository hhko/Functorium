
using ValueObjectFramework.ValueObjects.ComparableNot.CompositeValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.ComparableNot.CompositeValueObjects;

/// <summary>
/// City 값 객체의 생성 및 검증 기능 테스트
/// 
/// 테스트 목적:
/// 1. 유효한 도시명으로 City 생성 검증
/// 2. 무효한 도시명(빈 문자열)으로 City 생성 실패 검증
/// 3. 명시적 변환 연산자 검증
/// 4. 동등성 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "CityTests")]
public class CityTests
{
    // 테스트 시나리오: 유효한 도시명으로 City를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnSuccess_WhenCityNameIsValid()
    {
        // Arrange
        string cityName = "Seoul";

        // Act
        var actual = City.Create(cityName);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(city => ((string)city).ShouldBe(cityName));
    }

    // 테스트 시나리오: 빈 도시명으로 City 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailure_WhenCityNameIsEmpty()
    {
        // Arrange
        string cityName = "";

        // Act
        var actual = City.Create(cityName);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("도시명은 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: 명시적 변환 연산자가 올바른 값을 반환해야 한다
    [Fact]
    public void ExplicitOperator_ShouldReturnCorrectValue_WhenConvertingToString()
    {
        // Arrange
        var city = City.Create("New York").IfFail(_ => throw new Exception("생성 실패"));
        string expected = "New York";

        // Act
        string actual = (string)city;

        // Assert
        actual.ShouldBe(expected);
    }

    // 테스트 시나리오: 동일한 도시명을 가진 두 City는 동등해야 한다
    [Fact]
    public void Equals_ShouldReturnTrue_WhenCityNamesAreEqual()
    {
        // Arrange
        var city1 = City.Create("Seoul").IfFail(_ => throw new Exception("생성 실패"));
        var city2 = City.Create("Seoul").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = city1.Equals(city2);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 도시명을 가진 두 City는 동등하지 않아야 한다
    [Fact]
    public void Equals_ShouldReturnFalse_WhenCityNamesAreDifferent()
    {
        // Arrange
        var city1 = City.Create("Seoul").IfFail(_ => throw new Exception("생성 실패"));
        var city2 = City.Create("Busan").IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = city1.Equals(city2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: ToString 메서드는 도시명의 문자열 표현을 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnCityNameStringRepresentation_WhenCalled()
    {
        // Arrange
        var city = City.Create("Seoul").IfFail(_ => throw new Exception("생성 실패"));
        string expected = "Seoul";

        // Act
        var actual = city.ToString();

        // Assert
        actual.ShouldBe(expected);
    }
}
