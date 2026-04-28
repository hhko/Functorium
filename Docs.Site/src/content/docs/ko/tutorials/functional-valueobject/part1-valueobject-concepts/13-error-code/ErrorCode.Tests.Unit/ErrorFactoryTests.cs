/// <summary>
/// ErrorFactory 클래스의 에러 생성 기능 테스트
/// 
/// 테스트 목적:
/// 1. 다양한 타입의 에러 생성 메서드 검증
/// 2. 에러 코드 포맷팅 기능 검증
/// 3. 예외 기반 에러 생성 검증
/// </summary>
[Trait("Concept-13-Error-Code", "ErrorFactoryTests")]
public class ErrorFactoryTests
{
    // 테스트 시나리오: 문자열 에러 코드와 문자열 값을 사용하여 기본 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnExpectedError_WhenUsingStringParameters()
    {
        // Arrange
        string errorCode = "Domain.Name.TooShort";
        string errorCurrentValue = "a";

        string expectedErrorCode = "Domain.Name.TooShort";
        string expectedCurrentValue = "a";

        string errorMessage = "Name is too short. Current value: 'a'";

        // Act
        var actual = ErrorFactory.Create(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError>();
        var errorCodeExpected = actual as ExpectedError;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 문자열 에러 코드와 정수 값을 사용하여 제네릭 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnExpectedErrorInt_WhenUsingStringAndIntParameters()
    {
        // Arrange
        string errorCode = "Domain.Age.OutOfRange";
        int errorCurrentValue = 150;

        string expectedErrorCode = "Domain.Age.OutOfRange";
        int expectedCurrentValue = 150;
        string errorMessage = "Age is out of range. Current value: '150'";

        // Act
        var actual = ErrorFactory.Create(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError<int>>();
        var errorCodeExpected = actual as ExpectedError<int>;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 제네릭 타입을 사용하여 타입 안전한 에러를 생성해야 한다
    [Theory]
    [InlineData("Domain.Email.MissingAt", "not-an-email", "Email is missing '@' symbol. Current value: 'not-an-email'")]
    [InlineData("Domain.Phone.NotNumeric", "invalid-phone", "Phone number is not numeric. Current value: 'invalid-phone'")]
    [InlineData("Domain.Address.Empty", "empty-address", "Address is empty. Current value: 'empty-address'")]
    public void Create_ShouldReturnExpectedErrorWithGenericType_WhenUsingGenericMethod(string errorCode, string errorCurrentValue, string errorMessage)
    {
        // Arrange
        string expectedErrorCode = errorCode;
        string expectedCurrentValue = errorCurrentValue;

        // Act
        var actual = ErrorFactory.Create<string>(errorCode, errorCurrentValue, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError<string>>();
        var errorCodeExpected = actual as ExpectedError<string>;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue.ShouldBe(expectedCurrentValue);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 두 개의 제네릭 타입을 사용하여 다중 값 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnExpectedErrorWithTwoGenericTypes_WhenUsingTwoValueMethod()
    {
        // Arrange
        string errorCode = "Domain.Coordinate.XOutOfRange";
        int errorCurrentValue1 = 1500;
        int errorCurrentValue2 = 2000;
        string errorMessage = "Coordinate X is out of range. Current values: '1500', '2000'";

        string expectedErrorCode = "Domain.Coordinate.XOutOfRange";
        int expectedCurrentValue1 = 1500;
        int expectedCurrentValue2 = 2000;

        // Act
        var actual = ErrorFactory.Create(errorCode, errorCurrentValue1, errorCurrentValue2, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError<int, int>>();
        var errorCodeExpected = actual as ExpectedError<int, int>;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedCurrentValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedCurrentValue2);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 세 개의 제네릭 타입을 사용하여 다중 값 에러를 생성해야 한다
    [Fact]
    public void Create_ShouldReturnExpectedErrorWithThreeGenericTypes_WhenUsingThreeValueMethod()
    {
        // Arrange
        string errorCode = "Domain.Address.Empty";
        string errorCurrentValue1 = "Empty Street";
        string errorCurrentValue2 = "Invalid City";
        string errorCurrentValue3 = "12345";
        string errorMessage = "Address is empty. Street: 'Empty Street', City: 'Invalid City', PostalCode: '12345'";

        string expectedErrorCode = "Domain.Address.Empty";
        string expectedCurrentValue1 = "Empty Street";
        string expectedCurrentValue2 = "Invalid City";
        string expectedCurrentValue3 = "12345";

        // Act
        var actual = ErrorFactory.Create(errorCode, errorCurrentValue1, errorCurrentValue2, errorCurrentValue3, errorMessage);

        // Assert
        actual.ShouldBeOfType<ExpectedError<string, string, string>>();
        var errorCodeExpected = actual as ExpectedError<string, string, string>;
        errorCodeExpected!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExpected.ErrorCurrentValue1.ShouldBe(expectedCurrentValue1);
        errorCodeExpected.ErrorCurrentValue2.ShouldBe(expectedCurrentValue2);
        errorCodeExpected.ErrorCurrentValue3.ShouldBe(expectedCurrentValue3);
        errorCodeExpected.Message.ShouldBe(errorMessage);
    }

    // 테스트 시나리오: 예외를 사용하여 예외 기반 에러를 생성해야 한다
    [Fact]
    public void CreateFromException_ShouldReturnExceptionalError_WhenUsingException()
    {
        // Arrange
        string errorCode = "Domain.System.Exception";
        var exception = new InvalidOperationException("Test exception message");

        string expectedErrorCode = "Domain.System.Exception";
        string expectedMessage = "Test exception message";

        // Act
        var actual = ErrorFactory.CreateFromException(errorCode, exception);

        // Assert
        actual.ShouldBeOfType<ExceptionalError>();
        var errorCodeExceptional = actual as ExceptionalError;
        errorCodeExceptional!.ErrorCode.ShouldBe(expectedErrorCode);
        errorCodeExceptional.Message.ShouldBe(expectedMessage);
    }

    // 테스트 시나리오: 여러 문자열을 점으로 연결하여 에러 코드를 포맷해야 한다
    [Theory]
    [InlineData(new string[] { "Domain", "User", "AgeOutOfRange" }, "Domain.User.AgeOutOfRange")]
    [InlineData(new string[] { "Domain", "Payment", "Declined" }, "Domain.Payment.Declined")]
    [InlineData(new string[] { "Domain", "Order", "NotFound" }, "Domain.Order.NotFound")]
    public void Format_ShouldReturnFormattedErrorCode_WhenUsingStringArray(string[] parts, string expected)
    {
        // Arrange
        // Act
        var actual = ErrorFactory.Format(parts);

        // Assert
        actual.ShouldBe(expected);
    }
}
