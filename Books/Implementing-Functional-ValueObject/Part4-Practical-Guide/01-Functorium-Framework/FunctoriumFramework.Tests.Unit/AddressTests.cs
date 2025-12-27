using LanguageExt;

namespace FunctoriumFramework.Tests.Unit;

/// <summary>
/// Address 값 객체 테스트 (복합 ValueObject)
///
/// 테스트 목적:
/// 1. 복합 값 객체 생성 검증 (Create 메서드)
/// 2. 동등성 비교 검증 (Equals, GetHashCode)
/// 3. 에러 코드 검증 (ErrorCodeFactory 통합)
/// </summary>
[Trait("Part4-Functorium-Framework", "AddressTests")]
public class AddressTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsAreValid()
    {
        // Arrange
        var city = "서울";
        var street = "강남구 테헤란로 123";
        var postalCode = "06234";

        // Act
        var actual = Address.Create(city, street, postalCode);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: address =>
            {
                address.City.ShouldBe(city);
                address.Street.ShouldBe(street);
                address.PostalCode.ShouldBe(postalCode);
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("", "강남구 테헤란로 123", "06234")]
    [InlineData("   ", "강남구 테헤란로 123", "06234")]
    [InlineData(null, "강남구 테헤란로 123", "06234")]
    public void Create_ReturnsFail_WhenCityIsEmpty(string? city, string street, string postalCode)
    {
        // Act
        var actual = Address.Create(city!, street, postalCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Address.CityEmpty")
        );
    }

    [Theory]
    [InlineData("서울", "", "06234")]
    [InlineData("서울", "   ", "06234")]
    [InlineData("서울", null, "06234")]
    public void Create_ReturnsFail_WhenStreetIsEmpty(string city, string? street, string postalCode)
    {
        // Act
        var actual = Address.Create(city, street!, postalCode);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Address.StreetEmpty")
        );
    }

    [Theory]
    [InlineData("서울", "강남구 테헤란로 123", "")]
    [InlineData("서울", "강남구 테헤란로 123", "   ")]
    [InlineData("서울", "강남구 테헤란로 123", null)]
    public void Create_ReturnsFail_WhenPostalCodeIsEmpty(string city, string street, string? postalCode)
    {
        // Act
        var actual = Address.Create(city, street, postalCode!);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Address.PostalCodeEmpty")
        );
    }

    #endregion

    #region 동등성 테스트

    [Fact]
    public void Equals_ReturnsTrue_WhenAllFieldsAreEqual()
    {
        // Arrange
        var address1 = Address.Create("서울", "강남구", "06234").Match(a => a, _ => null!);
        var address2 = Address.Create("서울", "강남구", "06234").Match(a => a, _ => null!);

        // Act & Assert
        address1.Equals(address2).ShouldBeTrue();
        (address1 == address2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenAnyFieldIsDifferent()
    {
        // Arrange
        var address1 = Address.Create("서울", "강남구", "06234").Match(a => a, _ => null!);
        var address2 = Address.Create("서울", "서초구", "06234").Match(a => a, _ => null!);

        // Act & Assert
        address1.Equals(address2).ShouldBeFalse();
        (address1 != address2).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_WhenAddressesAreEqual()
    {
        // Arrange
        var address1 = Address.Create("서울", "강남구", "06234").Match(a => a, _ => null!);
        var address2 = Address.Create("서울", "강남구", "06234").Match(a => a, _ => null!);

        // Act & Assert
        address1.GetHashCode().ShouldBe(address2.GetHashCode());
    }

    #endregion

    #region ToString 테스트

    [Fact]
    public void ToString_ReturnsFormattedString_WhenCalled()
    {
        // Arrange
        var address = Address.Create("서울", "강남구 테헤란로 123", "06234").Match(a => a, _ => null!);

        // Act
        var actual = address.ToString();

        // Assert
        actual.ShouldBe("서울 강남구 테헤란로 123 (06234)");
    }

    #endregion
}
