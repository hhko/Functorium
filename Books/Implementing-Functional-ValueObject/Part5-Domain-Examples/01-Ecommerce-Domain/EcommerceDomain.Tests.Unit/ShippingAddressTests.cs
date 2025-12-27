namespace EcommerceDomain.Tests.Unit;

/// <summary>
/// ShippingAddress 값 객체 테스트
///
/// 학습 목표:
/// 1. 복합 값 객체 생성 검증
/// 2. 모든 필드 검증 확인
/// 3. 우편번호 정규화 검증
/// </summary>
[Trait("Part5-Ecommerce-Domain", "ShippingAddressTests")]
public class ShippingAddressTests
{
    #region 생성 테스트

    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsAreValid()
    {
        // Act
        var actual = ShippingAddress.Create(
            "홍길동",
            "테헤란로 123",
            "서울",
            "06234",
            "KR"
        );

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: a =>
            {
                a.RecipientName.ShouldBe("홍길동");
                a.Street.ShouldBe("테헤란로 123");
                a.City.ShouldBe("서울");
                a.PostalCode.ShouldBe("06234");
                a.Country.ShouldBe("KR");
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void Create_NormalizesPostalCode_RemovingDashes()
    {
        // Act
        var actual = ShippingAddress.Create(
            "홍길동",
            "테헤란로 123",
            "서울",
            "062-34",
            "KR"
        );

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: a => a.PostalCode.ShouldBe("06234"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void Create_NormalizesCountry_ToUpperCase()
    {
        // Act
        var actual = ShippingAddress.Create(
            "홍길동",
            "테헤란로 123",
            "서울",
            "06234",
            "kr"
        );

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: a => a.Country.ShouldBe("KR"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    #endregion

    #region 실패 테스트

    [Theory]
    [InlineData("", "테헤란로", "서울", "06234", "KR")]
    [InlineData("홍길동", "", "서울", "06234", "KR")]
    [InlineData("홍길동", "테헤란로", "", "06234", "KR")]
    [InlineData("홍길동", "테헤란로", "서울", "", "KR")]
    [InlineData("홍길동", "테헤란로", "서울", "06234", "")]
    public void Create_ReturnsFail_WhenAnyFieldIsEmpty(
        string recipient, string street, string city, string postalCode, string country)
    {
        // Act
        var actual = ShippingAddress.Create(recipient, street, city, postalCode, country);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("12345678901")]
    public void Create_ReturnsFail_WhenPostalCodeLengthIsInvalid(string postalCode)
    {
        // Act
        var actual = ShippingAddress.Create(
            "홍길동",
            "테헤란로 123",
            "서울",
            postalCode,
            "KR"
        );

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 동등성 테스트

    [Fact]
    public void Equals_ReturnsTrue_WhenAllFieldsMatch()
    {
        // Arrange
        var address1 = ShippingAddress.Create("홍길동", "테헤란로", "서울", "06234", "KR").Match(a => a, _ => null!);
        var address2 = ShippingAddress.Create("홍길동", "테헤란로", "서울", "06234", "KR").Match(a => a, _ => null!);

        // Act & Assert
        address1.Equals(address2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenAnyFieldDiffers()
    {
        // Arrange
        var address1 = ShippingAddress.Create("홍길동", "테헤란로", "서울", "06234", "KR").Match(a => a, _ => null!);
        var address2 = ShippingAddress.Create("김철수", "테헤란로", "서울", "06234", "KR").Match(a => a, _ => null!);

        // Act & Assert
        address1.Equals(address2).ShouldBeFalse();
    }

    #endregion
}
