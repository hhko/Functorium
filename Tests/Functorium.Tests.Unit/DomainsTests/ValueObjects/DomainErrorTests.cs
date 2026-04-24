using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

// 테스트용 더미 값 객체
public sealed class TestValueObject : SimpleValueObject<string>
{
    private TestValueObject(string value) : base(value) { }
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class DomainErrorTests
{
    private sealed record StartAfterEnd : DomainErrorKind.Custom;
    private sealed record Unsupported : DomainErrorKind.Custom;
    private sealed record CustomError : DomainErrorKind.Custom;
    private sealed record MyError : DomainErrorKind.Custom;
    private sealed record MyCustomError : DomainErrorKind.Custom;

    #region DomainErrorKind 기본 테스트

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenEmpty()
    {
        // Arrange
        var currentValue = "";
        var message = "Value cannot be empty";

        // Act
        var actual = DomainError.For<TestValueObject>(new DomainErrorKind.Empty(), currentValue, message);

        // Assert
        actual.ShouldBeOfType<ExpectedError>();
        actual.Message.ShouldBe(message);
        actual.IsExpected.ShouldBeTrue();

        var errorCode = (ExpectedError)actual;
        errorCode.ErrorCode.ShouldBe("Domain.TestValueObject.Empty");
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenGenericValue()
    {
        // Arrange
        var currentValue = -5;
        var message = "Value cannot be negative";

        // Act
        var actual = DomainError.For<TestValueObject, int>(new DomainErrorKind.Negative(), currentValue, message);

        // Assert
        actual.ShouldBeOfType<ExpectedError<int>>();
        actual.Message.ShouldBe(message);

        var errorCode = (ExpectedError<int>)actual;
        errorCode.ErrorCode.ShouldBe("Domain.TestValueObject.Negative");
        errorCode.ErrorCurrentValue.ShouldBe(-5);
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenTwoValues()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);
        var message = "Start date cannot be after end date";

        // Act
        var actual = DomainError.For<TestValueObject, DateTime, DateTime>(
            new StartAfterEnd(),
            startDate,
            endDate,
            message);

        // Assert
        actual.ShouldBeOfType<ExpectedError<DateTime, DateTime>>();

        var errorCode = (ExpectedError<DateTime, DateTime>)actual;
        errorCode.ErrorCode.ShouldBe("Domain.TestValueObject.StartAfterEnd");
        errorCode.ErrorCurrentValue1.ShouldBe(startDate);
        errorCode.ErrorCurrentValue2.ShouldBe(endDate);
    }

    #endregion

    #region DomainErrorKind 컨텍스트 정보 테스트

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenTooShortWithContext()
    {
        // Arrange
        var currentValue = "abc";
        var message = "Password too short";

        // Act
        var actual = DomainError.For<TestValueObject>(new DomainErrorKind.TooShort(MinLength: 8), currentValue, message);

        // Assert
        actual.ShouldBeOfType<ExpectedError>();
        actual.Message.ShouldBe(message);

        var errorCode = (ExpectedError)actual;
        errorCode.ErrorCode.ShouldBe("Domain.TestValueObject.TooShort");
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenInvalidFormatWithPattern()
    {
        // Arrange
        var currentValue = "invalid-email";
        var message = "Email format is invalid";

        // Act
        var actual = DomainError.For<TestValueObject>(
            new DomainErrorKind.InvalidFormat(Pattern: @"^[^@]+@[^@]+\.[^@]+$"),
            currentValue,
            message);

        // Assert
        var errorCode = (ExpectedError)actual;
        errorCode.ErrorCode.ShouldBe("Domain.TestValueObject.InvalidFormat");
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenOutOfRangeWithBounds()
    {
        // Arrange
        var currentValue = 150;
        var message = "Value is out of range";

        // Act
        var actual = DomainError.For<TestValueObject, int>(
            new DomainErrorKind.OutOfRange(Min: "0", Max: "100"),
            currentValue,
            message);

        // Assert
        actual.ShouldBeOfType<ExpectedError<int>>();

        var errorCode = (ExpectedError<int>)actual;
        errorCode.ErrorCode.ShouldBe("Domain.TestValueObject.OutOfRange");
        errorCode.ErrorCurrentValue.ShouldBe(150);
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenCustomError()
    {
        // Arrange
        var currentValue = "USD";
        var message = "Currency not supported";

        // Act
        var actual = DomainError.For<TestValueObject>(
            new Unsupported(),
            currentValue,
            message);

        // Assert
        var errorCode = (ExpectedError)actual;
        errorCode.ErrorCode.ShouldBe("Domain.TestValueObject.Unsupported");
    }

    #endregion

    #region 모든 DomainErrorKind 에러 코드 생성 테스트

    [Theory]
    [MemberData(nameof(GetAllDomainErrorTypes))]
    public void For_WithDomainErrorType_GeneratesCorrectErrorCodeForAllRecordTypes(
        DomainErrorKind errorType,
        string expectedName)
    {
        // Act
        var actual = DomainError.For<TestValueObject>(errorType, "test", "Test message");

        // Assert
        var errorCode = (ExpectedError)actual;
        errorCode.ErrorCode.ShouldBe($"Domain.TestValueObject.{expectedName}");
    }

    public static TheoryData<DomainErrorKind, string> GetAllDomainErrorTypes() => new()
    {
        { new DomainErrorKind.Empty(), "Empty" },
        { new DomainErrorKind.Null(), "Null" },
        { new DomainErrorKind.TooShort(), "TooShort" },
        { new DomainErrorKind.TooShort(MinLength: 5), "TooShort" },
        { new DomainErrorKind.TooLong(), "TooLong" },
        { new DomainErrorKind.TooLong(MaxLength: 100), "TooLong" },
        { new DomainErrorKind.WrongLength(), "WrongLength" },
        { new DomainErrorKind.WrongLength(Expected: 10), "WrongLength" },
        { new DomainErrorKind.InvalidFormat(), "InvalidFormat" },
        { new DomainErrorKind.InvalidFormat(Pattern: "^[a-z]+$"), "InvalidFormat" },
        { new DomainErrorKind.NotUpperCase(), "NotUpperCase" },
        { new DomainErrorKind.NotLowerCase(), "NotLowerCase" },
        { new DomainErrorKind.Negative(), "Negative" },
        { new DomainErrorKind.NotPositive(), "NotPositive" },
        { new DomainErrorKind.OutOfRange(), "OutOfRange" },
        { new DomainErrorKind.OutOfRange(Min: "0", Max: "100"), "OutOfRange" },
        { new DomainErrorKind.BelowMinimum(), "BelowMinimum" },
        { new DomainErrorKind.BelowMinimum(Minimum: "10"), "BelowMinimum" },
        { new DomainErrorKind.AboveMaximum(), "AboveMaximum" },
        { new DomainErrorKind.AboveMaximum(Maximum: "1000"), "AboveMaximum" },
        { new DomainErrorKind.NotFound(), "NotFound" },
        { new DomainErrorKind.AlreadyExists(), "AlreadyExists" },
        { new DomainErrorKind.Duplicate(), "Duplicate" },
        { new DomainErrorKind.Mismatch(), "Mismatch" },
        { new CustomError(), "CustomError" },
        { new DomainErrorKind.InvalidTransition(), "InvalidTransition" },
        { new DomainErrorKind.InvalidTransition(FromState: "Paid", ToState: "Active"), "InvalidTransition" }
    };

    #endregion

    #region DomainErrorKind record 속성 테스트

    [Fact]
    public void DomainErrorType_ErrorName_ReturnsRecordTypeName()
    {
        // Assert
        new DomainErrorKind.Empty().Name.ShouldBe("Empty");
        new DomainErrorKind.TooShort(MinLength: 8).Name.ShouldBe("TooShort");
        new MyError().Name.ShouldBe("MyError");
    }

    [Fact]
    public void DomainErrorType_TooShort_ContainsMinLengthContext()
    {
        // Arrange
        var errorType = new DomainErrorKind.TooShort(MinLength: 8);

        // Assert
        errorType.MinLength.ShouldBe(8);
    }

    [Fact]
    public void DomainErrorType_TooLong_ContainsMaxLengthContext()
    {
        // Arrange
        var errorType = new DomainErrorKind.TooLong(MaxLength: 100);

        // Assert
        errorType.MaxLength.ShouldBe(100);
    }

    [Fact]
    public void DomainErrorType_WrongLength_ContainsExpectedContext()
    {
        // Arrange
        var errorType = new DomainErrorKind.WrongLength(Expected: 10);

        // Assert
        errorType.Expected.ShouldBe(10);
    }

    [Fact]
    public void DomainErrorType_InvalidFormat_ContainsPatternContext()
    {
        // Arrange
        var pattern = @"^[^@]+@[^@]+$";
        var errorType = new DomainErrorKind.InvalidFormat(Pattern: pattern);

        // Assert
        errorType.Pattern.ShouldBe(pattern);
    }

    [Fact]
    public void DomainErrorType_OutOfRange_ContainsBoundsContext()
    {
        // Arrange
        var errorType = new DomainErrorKind.OutOfRange(Min: "0", Max: "100");

        // Assert
        errorType.Min.ShouldBe("0");
        errorType.Max.ShouldBe("100");
    }

    [Fact]
    public void DomainErrorType_BelowMinimum_ContainsMinimumContext()
    {
        // Arrange
        var errorType = new DomainErrorKind.BelowMinimum(Minimum: "10");

        // Assert
        errorType.Minimum.ShouldBe("10");
    }

    [Fact]
    public void DomainErrorType_AboveMaximum_ContainsMaximumContext()
    {
        // Arrange
        var errorType = new DomainErrorKind.AboveMaximum(Maximum: "1000");

        // Assert
        errorType.Maximum.ShouldBe("1000");
    }

    [Fact]
    public void DomainErrorType_Custom_ContainsNameContext()
    {
        // Arrange
        var errorType = new MyCustomError();

        // Assert
        errorType.Name.ShouldBe("MyCustomError");
    }

    [Fact]
    public void DomainErrorType_InvalidTransition_ContainsStateContext()
    {
        // Arrange
        var errorType = new DomainErrorKind.InvalidTransition(FromState: "Paid", ToState: "Active");

        // Assert
        errorType.FromState.ShouldBe("Paid");
        errorType.ToState.ShouldBe("Active");
    }

    [Fact]
    public void DomainErrorType_InvalidTransition_DefaultsToNull()
    {
        // Arrange
        var errorType = new DomainErrorKind.InvalidTransition();

        // Assert
        errorType.FromState.ShouldBeNull();
        errorType.ToState.ShouldBeNull();
    }

    #endregion

    #region DomainErrorKind record equality 테스트

    [Fact]
    public void DomainErrorType_Records_SupportEquality()
    {
        // Assert - same type, same values
        new DomainErrorKind.Empty().ShouldBe(new DomainErrorKind.Empty());
        new DomainErrorKind.TooShort(8).ShouldBe(new DomainErrorKind.TooShort(8));

        // Assert - same type, different values
        new DomainErrorKind.TooShort(8).ShouldNotBe(new DomainErrorKind.TooShort(10));

        // Assert - different types
        ((DomainErrorKind)new DomainErrorKind.Empty()).ShouldNotBe(new DomainErrorKind.Null());
    }

    #endregion
}
