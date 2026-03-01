using ValidatedValueCreation.ValueObjects;

/// <summary>
/// Address 클래스의 3가지 메서드 패턴 테스트
/// 
/// 테스트 목적:
/// 1. Create 메서드의 복합 검증 후 객체 생성 기능 검증
/// 2. Validate 메서드의 복합 검증 기능 검증
/// 3. CreateFromValidated 메서드의 검증된 값 객체들로 직접 생성 기능 검증
/// 4. 복합 검증에서 하나의 검증이 실패하면 전체가 실패하는 "All or Nothing" 방식 검증
/// </summary>
[Trait("Concept-10-Validated-Value-Creation", "AddressTests")]
public class AddressTests
{
    // 테스트 시나리오: 모든 구성 요소가 유효한 경우 Address 객체가 정상 생성되어야 한다
    [Theory]
    [InlineData("123 Main St", "Seoul", "12345")]
    [InlineData("Broadway", "New York", "10001")]
    [InlineData("서울시 강남구 테헤란로", "서울", "123456")]
    public void Create_ShouldReturnSuccess_WhenAllComponentsAreValid(string street, string city, string postalCode)
    {
        // Arrange
        string expectedStreet = street;
        string expectedCity = city;
        string expectedPostalCode = postalCode;

        // Act
        var actual = Address.Create(street, city, postalCode);

        // Assert
        actual.Match(
            Succ: address =>
            {
                ((string)address.Street).ShouldBe(expectedStreet);
                ((string)address.City).ShouldBe(expectedCity);
                ((string)address.PostalCode).ShouldBe(expectedPostalCode);
            },
            Fail: error => throw new Exception($"Expected success but got error: {error.Message}")
        );
    }

    // 테스트 시나리오: 모든 구성 요소가 유효한 경우 Validate 메서드가 성공 결과를 반환해야 한다
    [Theory]
    [InlineData("123 Main St", "Seoul", "12345")]
    [InlineData("Broadway", "New York", "10001")]
    [InlineData("서울시 강남구 테헤란로", "서울", "123456")]
    public void Validate_ShouldReturnSuccess_WhenAllComponentsAreValid(string street, string city, string postalCode)
    {
        // Arrange
        string expectedStreet = street;
        string expectedCity = city;
        string expectedPostalCode = postalCode;

        // Act
        var actual = Address.Validate(street, city, postalCode);

        // Assert
        actual.Match(
            Succ: validatedValues =>
            {
                ((string)validatedValues.Street).ShouldBe(expectedStreet);
                ((string)validatedValues.City).ShouldBe(expectedCity);
                ((string)validatedValues.PostalCode).ShouldBe(expectedPostalCode);
            },
            Fail: error => throw new Exception($"Expected success but got error: {error.Message}")
        );
    }

    // 테스트 시나리오: CreateFromValidated 메서드가 검증된 값 객체들로 직접 Address 객체를 생성해야 한다
    [Fact]
    public void CreateFromValidated_ShouldCreateAddress_WhenValidatedValueObjectsAreProvided()
    {
        // Arrange
        var street = Street.CreateFromValidated("123 Main St");
        var city = City.CreateFromValidated("Seoul");
        var postalCode = PostalCode.CreateFromValidated("12345");

        // Act
        var actual = Address.CreateFromValidated(street, city, postalCode);

        // Assert
        ((string)actual.Street).ShouldBe("123 Main St");
        ((string)actual.City).ShouldBe("Seoul");
        ((string)actual.PostalCode).ShouldBe("12345");
    }

    // 테스트 시나리오: Validate 메서드가 순수 함수로 동작하는지 검증해야 한다
    [Fact]
    public void Validate_ShouldBePureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string street = "123 Main St";
        string city = "Seoul";
        string postalCode = "12345";

        // Act
        var result1 = Address.Validate(street, city, postalCode);
        var result2 = Address.Validate(street, city, postalCode);

        // Assert
        string street1 = "";
        string city1 = "";
        string postalCode1 = "";
        string street2 = "";
        string city2 = "";
        string postalCode2 = "";

        result1.Match(
            Succ: values =>
            {
                street1 = ((string)values.Street);
                city1 = (string)values.City;
                postalCode1 = (string)values.PostalCode;
            },
            Fail: _ => throw new Exception("Expected success")
        );

        result2.Match(
            Succ: values =>
            {
                street2 = ((string)values.Street);
                city2 = (string)values.City;
                postalCode2 = ((string)values.PostalCode);
            },
            Fail: _ => throw new Exception("Expected success")
        );

        street1.ShouldBe(street2);
        city1.ShouldBe(city2);
        postalCode1.ShouldBe(postalCode2);
    }

    // 테스트 시나리오: Create와 Validate가 동일한 검증 로직을 사용하는지 검증해야 한다
    [Theory]
    [InlineData("123 Main St", "Seoul", "12345")]
    [InlineData("", "Seoul", "12345")]
    [InlineData("123 Main St", "", "12345")]
    [InlineData("123 Main St", "Seoul", "1234a")]
    public void Create_ShouldUseSameValidationLogic_AsValidate(string street, string city, string postalCode)
    {
        // Arrange
        var validationResult = Address.Validate(street, city, postalCode);
        var creationResult = Address.Create(street, city, postalCode);

        // Act & Assert
        bool validationSuccess = false;
        bool creationSuccess = false;
        string validationError = "";
        string creationError = "";

        validationResult.Match(
            Succ: _ => validationSuccess = true,
            Fail: error => validationError = error.Message
        );

        creationResult.Match(
            Succ: _ => creationSuccess = true,
            Fail: error => creationError = error.Message
        );

        validationSuccess.ShouldBe(creationSuccess);

        if (!validationSuccess)
        {
            validationError.ShouldBe(creationError);
        }
    }


    // 테스트 시나리오: 거리명이 빈 경우 Address 생성 시 실패해야 한다
    // 복합 검증의 AND 조건 특성: 하나의 구성 요소라도 유효하지 않으면 전체 검증 실패
    [Theory]
    [InlineData("", "Seoul", "12345")]
    [InlineData("   ", "Seoul", "12345")]
    public void Create_ShouldReturnFailure_WhenStreetIsEmpty(string street, string city, string postalCode)
    {
        // Arrange
        string expectedMessage = "거리명은 비어있을 수 없습니다";

        // Act
        var actual = Address.Create(street, city, postalCode);

        // Assert
        actual.Match(
            Succ: address => throw new Exception($"Expected failure but got success: {address}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: 도시명이 빈 경우 Address 생성 시 실패해야 한다
    // 복합 검증의 AND 조건 특성: 하나의 구성 요소라도 유효하지 않으면 전체 검증 실패
    [Theory]
    [InlineData("123 Main St", "", "12345")]
    [InlineData("123 Main St", "   ", "12345")]
    [InlineData("123 Main St", null, "12345")]
    public void Create_ShouldReturnFailure_WhenCityIsEmpty(string street, string? city, string postalCode)
    {
        // Arrange
        string expectedMessage = "도시명은 비어있을 수 없습니다";

        // Act
        var actual = Address.Create(street, city!, postalCode);

        // Assert
        actual.Match(
            Succ: address => throw new Exception($"Expected failure but got success: {address}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }

    // 테스트 시나리오: 우편번호가 유효하지 않은 경우 Address 생성 시 실패해야 한다
    // 복합 검증의 AND 조건 특성: 하나의 구성 요소라도 유효하지 않으면 전체 검증 실패
    [Theory]
    [InlineData("123 Main St", "Seoul", "")]
    [InlineData("123 Main St", "Seoul", "   ")]
    [InlineData("123 Main St", "Seoul", null)]
    public void Create_ShouldReturnFailure_WhenPostalCodeIsEmpty(string street, string city, string? postalCode)
    {
        // Arrange
        string expectedMessage = "우편번호는 비어있을 수 없습니다";

        // Act
        var actual = Address.Create(street, city, postalCode!);

        // Assert
        actual.Match(
            Succ: address => throw new Exception($"Expected failure but got success: {address}"),
            Fail: error => error.Message.ShouldBe(expectedMessage)
        );
    }
}
