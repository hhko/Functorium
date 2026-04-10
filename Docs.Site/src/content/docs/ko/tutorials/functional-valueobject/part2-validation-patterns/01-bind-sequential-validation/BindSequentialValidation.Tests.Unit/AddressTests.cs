using BindSequentialValidation.ValueObjects;
using LanguageExt;

namespace BindSequentialValidation.Tests.Unit;

/// <summary>
/// Address 값 객체의 Bind 순차 검증 패턴 테스트
///
/// 학습 목표:
/// 1. Bind 연산자를 통한 순차 검증 동작 이해
/// 2. 첫 번째 실패 시 검증이 중단되는 것을 확인
/// 3. 의존성 있는 검증 규칙의 순차적 실행 검증
/// </summary>
[Trait("Part2-Validation", "01-Bind-Sequential")]
public class AddressTests
{
    // 테스트 시나리오: 모든 필드가 유효할 때 Address 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsAreValid()
    {
        // Arrange
        string street = "Main Street 123";
        string city = "Seoul";
        string postalCode = "12345";
        string country = "KR";

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode, country);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: address =>
            {
                address.Street.ShouldBe(street);
                address.City.ShouldBe(city);
                address.PostalCode.ShouldBe(postalCode);
                address.Country.ShouldBe(country);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 도로명이 너무 짧을 때 실패 반환 (첫 번째 검증 실패)
    [Fact]
    public void Create_ReturnsFail_WhenStreetTooShort()
    {
        // Arrange
        string street = "Ab";  // 5자 미만
        string city = "Seoul";
        string postalCode = "12345";
        string country = "KR";

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode, country);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 도시명이 너무 짧을 때 실패 반환 (두 번째 검증 실패)
    [Fact]
    public void Create_ReturnsFail_WhenCityTooShort()
    {
        // Arrange
        string street = "Main Street 123";
        string city = "S";  // 2자 미만
        string postalCode = "12345";
        string country = "KR";

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode, country);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 우편번호가 너무 짧을 때 실패 반환 (세 번째 검증 실패)
    [Fact]
    public void Create_ReturnsFail_WhenPostalCodeTooShort()
    {
        // Arrange
        string street = "Main Street 123";
        string city = "Seoul";
        string postalCode = "123";  // 5자 미만
        string country = "KR";

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode, country);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 국가와 우편번호가 일치하지 않을 때 실패 반환 (의존 검증 실패)
    [Fact]
    public void Create_ReturnsFail_WhenCountryPostalCodeMismatch()
    {
        // Arrange
        string street = "Main Street 123";
        string city = "Seoul";
        string postalCode = "12345";
        string country = "JP";  // 일본은 7자리 우편번호 필요

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode, country);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: Bind 패턴에서 첫 번째 실패 시 이후 검증이 실행되지 않음
    [Fact]
    public void Validate_StopsAtFirstError_WhenUsingBindPattern()
    {
        // Arrange - 모든 필드가 유효하지 않은 경우
        string street = "Ab";    // 첫 번째 실패
        string city = "S";       // 두 번째 실패 (실행되지 않아야 함)
        string postalCode = "1"; // 세 번째 실패 (실행되지 않아야 함)
        string country = "XX";   // 네 번째 실패 (실행되지 않아야 함)

        // Act
        var actual = Address.Validate(street, city, postalCode, country);

        // Assert - Bind 패턴은 첫 번째 에러만 반환
        actual.IsFail.ShouldBeTrue();
        // Bind 패턴은 첫 번째 에러에서 중단되므로 에러가 1개만 있어야 함
        actual.FailSpan().Length.ShouldBe(1);
    }

    // 테스트 시나리오: 미국 우편번호 형식 검증
    [Fact]
    public void Create_ReturnsSuccess_WhenUSPostalCodeIsValid()
    {
        // Arrange
        string street = "Main Street 123";
        string city = "New York";
        string postalCode = "10001";
        string country = "US";

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode, country);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: 일본 우편번호 형식 검증 (7자리)
    [Fact]
    public void Create_ReturnsSuccess_WhenJPPostalCodeIsValid()
    {
        // Arrange
        string street = "Main Street 123";
        string city = "Tokyo";
        string postalCode = "1234567";
        string country = "JP";

        // Act
        Fin<Address> actual = Address.Create(street, city, postalCode, country);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
