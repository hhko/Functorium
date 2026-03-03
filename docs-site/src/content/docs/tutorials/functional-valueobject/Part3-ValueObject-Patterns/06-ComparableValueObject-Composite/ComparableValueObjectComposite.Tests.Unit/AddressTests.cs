using ComparableValueObjectComposite.ValueObjects;
using LanguageExt;

namespace ComparableValueObjectComposite.Tests.Unit;

/// <summary>
/// Address 값 객체의 ComparableValueObject (복합 값 객체) 패턴 테스트
///
/// 학습 목표:
/// 1. 비교 가능한 복합 값 객체 패턴 이해
/// 2. 여러 값 객체 조합의 값 객체 비교 검증
/// 3. 비교 순서 (City -> PostalCode -> Street) 확인
/// </summary>
[Trait("Part3-Patterns", "06-ComparableValueObject-Composite")]
public class AddressTests
{
    // 테스트 시나리오: 유효한 주소로 Address 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenAddressIsValid()
    {
        // Arrange
        string street = "Main Street 123";
        string city = "Seoul";
        string postalCode = "12345";

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: address =>
            {
                // 명시적 형변환을 통해 Value 접근
                ((string)address.Street).ShouldBe(street);
                ((string)address.City).ShouldBe(city);
                ((string)address.PostalCode).ShouldBe(postalCode);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 동일한 주소의 두 Address는 동등해야 함
    [Fact]
    public void Equals_ReturnsTrue_WhenAddressesAreEqual()
    {
        // Arrange
        var address1 = Address.Create("Main Street", "Seoul", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));
        var address2 = Address.Create("Main Street", "Seoul", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        address1.Equals(address2).ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 주소의 두 Address는 동등하지 않아야 함
    [Fact]
    public void Equals_ReturnsFalse_WhenAddressesAreDifferent()
    {
        // Arrange
        var address1 = Address.Create("Main Street", "Seoul", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));
        var address2 = Address.Create("Other Street", "Busan", "67890").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        address1.Equals(address2).ShouldBeFalse();
    }

    // 테스트 시나리오: Address 비교 연산 (City 기준 우선)
    [Fact]
    public void CompareTo_ReturnsNegative_WhenFirstAddressCityIsAlphabeticallyFirst()
    {
        // Arrange
        var address1 = Address.Create("Main Street", "Busan", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));
        var address2 = Address.Create("Main Street", "Seoul", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = address1.CompareTo(address2);

        // Assert - Busan < Seoul 알파벳순
        actual.ShouldBeLessThan(0);
    }

    // 테스트 시나리오: 같은 City일 때 PostalCode로 비교
    [Fact]
    public void CompareTo_UsesPostalCode_WhenCitiesAreEqual()
    {
        // Arrange
        var address1 = Address.Create("Main Street", "Seoul", "10000").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));
        var address2 = Address.Create("Other Street", "Seoul", "20000").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = address1.CompareTo(address2);

        // Assert - 10000 < 20000
        actual.ShouldBeLessThan(0);
    }

    // 테스트 시나리오: 같은 City와 PostalCode일 때 Street으로 비교
    [Fact]
    public void CompareTo_UsesStreet_WhenCityAndPostalCodeAreEqual()
    {
        // Arrange
        var address1 = Address.Create("A Street", "Seoul", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));
        var address2 = Address.Create("B Street", "Seoul", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        int actual = address1.CompareTo(address2);

        // Assert - A Street < B Street
        actual.ShouldBeLessThan(0);
    }

    // 테스트 시나리오: Address 정렬 동작 검증
    [Fact]
    public void Sort_SortsAddressesCorrectly_WhenListContainsMultipleAddresses()
    {
        // Arrange
        var addresses = new List<Address>
        {
            Address.Create("Main Street", "Seoul", "12345").Match(Succ: a => a, Fail: _ => throw new Exception("생성 실패")),
            Address.Create("Other Street", "Busan", "67890").Match(Succ: a => a, Fail: _ => throw new Exception("생성 실패")),
            Address.Create("Third Street", "Daegu", "11111").Match(Succ: a => a, Fail: _ => throw new Exception("생성 실패"))
        };

        // Act
        addresses.Sort();

        // Assert - 알파벳순: Busan < Daegu < Seoul
        ((string)addresses[0].City).ShouldBe("Busan");
        ((string)addresses[1].City).ShouldBe("Daegu");
        ((string)addresses[2].City).ShouldBe("Seoul");
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식 반환
    [Fact]
    public void ToString_ReturnsFormattedString_WhenAddressIsValid()
    {
        // Arrange
        var address = Address.Create("Main Street", "Seoul", "12345").Match(
            Succ: a => a,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        string actual = address.ToString();

        // Assert
        actual.ShouldBe("Main Street, Seoul 12345");
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string street = "Main Street";
        string city = "Seoul";
        string postalCode = "12345";

        // Act
        Fin<Address> actual1 = Address.Create(street, city, postalCode);
        Fin<Address> actual2 = Address.Create(street, city, postalCode);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }
}
