/// <summary>
/// ErrorCodeFactory 클래스의 에러 생성 기능 테스트
/// 
/// 테스트 목적:
/// 1. 다양한 타입의 에러 생성 메서드 검증
/// 2. 에러 코드 포맷팅 기능 검증
/// 3. 예외 기반 에러 생성 검증
/// </summary>
[Trait("Concept-13-Error-Code", "ErrorCodeFactoryTests")]
public class ErrorCodeFactoryTests
{
    // 테스트 시나리오: 문자열 에러 코드와 문자열 값을 사용하여 기본 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnErrorCodeExpected_WhenUsingStringParameters()
    {
        // Arrange
        string errorCode = "DomainErrors.Name.TooShort";
        string errorCurrentValue = "a";

        string expectedErrorCode = "DomainErrors.Name.TooShort";
        string expectedCurrentValue = "a";

        string errorMessage = "Name is too short. Current value: 'a'";

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected>();
        var errorCodeExpected = actual as ErrorCodeExpected;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 문자열 에러 코드와 정수 값을 사용하여 기본 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnErrorCodeExpected_WhenUsingStringAndIntParameters()
    {
        // Arrange
        string errorCode = "DomainErrors.Age.OutOfRange";
        int errorCurrentValue = 150;

        string expectedErrorCode = "DomainErrors.Age.OutOfRange";
        string expectedCurrentValue = "150";
        string errorMessage = "Age is out of range. Current value: '150'";

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected>();
        var errorCodeExpected = actual as ErrorCodeExpected;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 제네릭 타입을 사용하여 타입 안전한 에러를 생성해야 한다
    [Theory]
    [InlineData("DomainErrors.Email.MissingAt", "not-an-email", "Email is missing '@' symbol. Current value: 'not-an-email'")]
    [InlineData("DomainErrors.Phone.NotNumeric", "invalid-phone", "Phone number is not numeric. Current value: 'invalid-phone'")]
    [InlineData("DomainErrors.Address.Empty", "empty-address", "Address is empty. Current value: 'empty-address'")]
    public void Create_ShouldReturnErrorCodeExpectedWithGenericType_WhenUsingGenericMethod(string errorCode, string errorCurrentValue, string errorMessage)
    {
        // Arrange
        string expectedErrorCode = errorCode;
        string expectedCurrentValue = errorCurrentValue;

        // Act
        var actual = ErrorCodeFactory.Create<string>(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<string>>();
        var errorCodeExpected = actual as ErrorCodeExpected<string>;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 두 개의 제네릭 타입을 사용하여 다중 값 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnErrorCodeExpectedWithTwoGenericTypes_WhenUsingTwoValueMethod()
    {
        // Arrange
        string errorCode = "DomainErrors.Coordinate.XOutOfRange";
        int errorCurrentValue1 = 1500;
        int errorCurrentValue2 = 2000;
        string errorMessage = "Coordinate X is out of range. Current values: '1500', '2000'";

        string expectedErrorCode = "DomainErrors.Coordinate.XOutOfRange";
        int expectedCurrentValue1 = 1500;
        int expectedCurrentValue2 = 2000;

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, errorCurrentValue1, errorCurrentValue2, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<int, int>>();
        var errorCodeExpected = actual as ErrorCodeExpected<int, int>;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedCurrentValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedCurrentValue2);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 세 개의 제네릭 타입을 사용하여 다중 값 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnErrorCodeExpectedWithThreeGenericTypes_WhenUsingThreeValueMethod()
    {
        // Arrange
        string errorCode = "DomainErrors.Address.Empty";
        string errorCurrentValue1 = "Empty Street";
        string errorCurrentValue2 = "Invalid City";
        string errorCurrentValue3 = "12345";
        string errorMessage = "Address is empty. Street: 'Empty Street', City: 'Invalid City', PostalCode: '12345'";

        string expectedErrorCode = "DomainErrors.Address.Empty";
        string expectedCurrentValue1 = "Empty Street";
        string expectedCurrentValue2 = "Invalid City";
        string expectedCurrentValue3 = "12345";

        // Act
        var actual = ErrorCodeFactory.Create(errorCode, errorCurrentValue1, errorCurrentValue2, errorCurrentValue3, errorMessage);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<string, string, string>>();
        var errorCodeExpected = actual as ErrorCodeExpected<string, string, string>;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedCurrentValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedCurrentValue2);
        errorCodeExpected.ErrorCurrentValue3.ShouldBe(expectedCurrentValue3);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 예외를 사용하여 예외 기반 에러를 생성해야 한다
    [Fact]
    public void CreateFromException_ShouldReturnErrorCodeExceptional_WhenUsingException()
    {
        // Arrange
        string errorCode = "DomainErrors.System.Exception";
        var exception = new InvalidOperationException("Test exception message");

        string expectedErrorCode = "DomainErrors.System.Exception";
        string expectedMessage = "Test exception message";

        // Act
        var actual = ErrorCodeFactory.CreateFromException(errorCode, exception);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExceptional>();
        var errorCodeExceptional = actual as ErrorCodeExceptional;
        errorCodeExceptional!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExceptional.Message.ShouldBe(expectedMessage);
    }

    // 테스트 시나리오: 여러 문자열을 점으로 연결하여 에러 코드를 포맷해야 한다
    [Theory]
    [InlineData(new string[] { "DomainErrors", "User", "AgeOutOfRange" }, "DomainErrors.User.AgeOutOfRange")]
    [InlineData(new string[] { "DomainErrors", "Payment", "Declined" }, "DomainErrors.Payment.Declined")]
    [InlineData(new string[] { "DomainErrors", "Order", "NotFound" }, "DomainErrors.Order.NotFound")]
    public void Format_ShouldReturnFormattedErrorCode_WhenUsingStringArray(string[] parts, string expected)
    {
        // Arrange
        // Act
        var actual = ErrorCodeFactory.Format(parts);

        // Assert
        actual.ShouldBe(expected);
    }
}
