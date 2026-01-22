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
    #region DomainErrorType 기본 테스트

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenEmpty()
    {
        // Arrange
        var currentValue = "";
        var message = "Value cannot be empty";

        // Act
        var actual = DomainError.For<TestValueObject>(new DomainErrorType.Empty(), currentValue, message);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected>();
        actual.Message.ShouldBe(message);
        actual.IsExpected.ShouldBeTrue();

        var errorCode = (ErrorCodeExpected)actual;
        errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.Empty");
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenGenericValue()
    {
        // Arrange
        var currentValue = -5;
        var message = "Value cannot be negative";

        // Act
        var actual = DomainError.For<TestValueObject, int>(new DomainErrorType.Negative(), currentValue, message);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<int>>();
        actual.Message.ShouldBe(message);

        var errorCode = (ErrorCodeExpected<int>)actual;
        errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.Negative");
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
            new DomainErrorType.Custom("StartAfterEnd"),
            startDate,
            endDate,
            message);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<DateTime, DateTime>>();

        var errorCode = (ErrorCodeExpected<DateTime, DateTime>)actual;
        errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.StartAfterEnd");
        errorCode.ErrorCurrentValue1.ShouldBe(startDate);
        errorCode.ErrorCurrentValue2.ShouldBe(endDate);
    }

    #endregion

    #region DomainErrorType 컨텍스트 정보 테스트

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenTooShortWithContext()
    {
        // Arrange
        var currentValue = "abc";
        var message = "Password too short";

        // Act
        var actual = DomainError.For<TestValueObject>(new DomainErrorType.TooShort(MinLength: 8), currentValue, message);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected>();
        actual.Message.ShouldBe(message);

        var errorCode = (ErrorCodeExpected)actual;
        errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.TooShort");
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenInvalidFormatWithPattern()
    {
        // Arrange
        var currentValue = "invalid-email";
        var message = "Email format is invalid";

        // Act
        var actual = DomainError.For<TestValueObject>(
            new DomainErrorType.InvalidFormat(Pattern: @"^[^@]+@[^@]+\.[^@]+$"),
            currentValue,
            message);

        // Assert
        var errorCode = (ErrorCodeExpected)actual;
        errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.InvalidFormat");
    }

    [Fact]
    public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenOutOfRangeWithBounds()
    {
        // Arrange
        var currentValue = 150;
        var message = "Value is out of range";

        // Act
        var actual = DomainError.For<TestValueObject, int>(
            new DomainErrorType.OutOfRange(Min: "0", Max: "100"),
            currentValue,
            message);

        // Assert
        actual.ShouldBeOfType<ErrorCodeExpected<int>>();

        var errorCode = (ErrorCodeExpected<int>)actual;
        errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.OutOfRange");
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
            new DomainErrorType.Custom("Unsupported"),
            currentValue,
            message);

        // Assert
        var errorCode = (ErrorCodeExpected)actual;
        errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.Unsupported");
    }

    #endregion

    #region 모든 DomainErrorType 에러 코드 생성 테스트

    [Theory]
    [MemberData(nameof(GetAllDomainErrorTypes))]
    public void For_WithDomainErrorType_GeneratesCorrectErrorCodeForAllRecordTypes(
        DomainErrorType errorType,
        string expectedName)
    {
        // Act
        var actual = DomainError.For<TestValueObject>(errorType, "test", "Test message");

        // Assert
        var errorCode = (ErrorCodeExpected)actual;
        errorCode.ErrorCode.ShouldBe($"DomainErrors.TestValueObject.{expectedName}");
    }

    public static TheoryData<DomainErrorType, string> GetAllDomainErrorTypes() => new()
    {
        { new DomainErrorType.Empty(), "Empty" },
        { new DomainErrorType.Null(), "Null" },
        { new DomainErrorType.TooShort(), "TooShort" },
        { new DomainErrorType.TooShort(MinLength: 5), "TooShort" },
        { new DomainErrorType.TooLong(), "TooLong" },
        { new DomainErrorType.TooLong(MaxLength: 100), "TooLong" },
        { new DomainErrorType.WrongLength(), "WrongLength" },
        { new DomainErrorType.WrongLength(Expected: 10), "WrongLength" },
        { new DomainErrorType.InvalidFormat(), "InvalidFormat" },
        { new DomainErrorType.InvalidFormat(Pattern: "^[a-z]+$"), "InvalidFormat" },
        { new DomainErrorType.NotUpperCase(), "NotUpperCase" },
        { new DomainErrorType.NotLowerCase(), "NotLowerCase" },
        { new DomainErrorType.Negative(), "Negative" },
        { new DomainErrorType.NotPositive(), "NotPositive" },
        { new DomainErrorType.OutOfRange(), "OutOfRange" },
        { new DomainErrorType.OutOfRange(Min: "0", Max: "100"), "OutOfRange" },
        { new DomainErrorType.BelowMinimum(), "BelowMinimum" },
        { new DomainErrorType.BelowMinimum(Minimum: "10"), "BelowMinimum" },
        { new DomainErrorType.AboveMaximum(), "AboveMaximum" },
        { new DomainErrorType.AboveMaximum(Maximum: "1000"), "AboveMaximum" },
        { new DomainErrorType.NotFound(), "NotFound" },
        { new DomainErrorType.AlreadyExists(), "AlreadyExists" },
        { new DomainErrorType.Duplicate(), "Duplicate" },
        { new DomainErrorType.Mismatch(), "Mismatch" },
        { new DomainErrorType.Custom("CustomError"), "CustomError" }
    };

    #endregion

    #region DomainErrorType record 속성 테스트

    [Fact]
    public void DomainErrorType_ErrorName_ReturnsRecordTypeName()
    {
        // Assert
        new DomainErrorType.Empty().ErrorName.ShouldBe("Empty");
        new DomainErrorType.TooShort(MinLength: 8).ErrorName.ShouldBe("TooShort");
        new DomainErrorType.Custom("MyError").ErrorName.ShouldBe("MyError");
    }

    [Fact]
    public void DomainErrorType_TooShort_ContainsMinLengthContext()
    {
        // Arrange
        var errorType = new DomainErrorType.TooShort(MinLength: 8);

        // Assert
        errorType.MinLength.ShouldBe(8);
    }

    [Fact]
    public void DomainErrorType_TooLong_ContainsMaxLengthContext()
    {
        // Arrange
        var errorType = new DomainErrorType.TooLong(MaxLength: 100);

        // Assert
        errorType.MaxLength.ShouldBe(100);
    }

    [Fact]
    public void DomainErrorType_WrongLength_ContainsExpectedContext()
    {
        // Arrange
        var errorType = new DomainErrorType.WrongLength(Expected: 10);

        // Assert
        errorType.Expected.ShouldBe(10);
    }

    [Fact]
    public void DomainErrorType_InvalidFormat_ContainsPatternContext()
    {
        // Arrange
        var pattern = @"^[^@]+@[^@]+$";
        var errorType = new DomainErrorType.InvalidFormat(Pattern: pattern);

        // Assert
        errorType.Pattern.ShouldBe(pattern);
    }

    [Fact]
    public void DomainErrorType_OutOfRange_ContainsBoundsContext()
    {
        // Arrange
        var errorType = new DomainErrorType.OutOfRange(Min: "0", Max: "100");

        // Assert
        errorType.Min.ShouldBe("0");
        errorType.Max.ShouldBe("100");
    }

    [Fact]
    public void DomainErrorType_BelowMinimum_ContainsMinimumContext()
    {
        // Arrange
        var errorType = new DomainErrorType.BelowMinimum(Minimum: "10");

        // Assert
        errorType.Minimum.ShouldBe("10");
    }

    [Fact]
    public void DomainErrorType_AboveMaximum_ContainsMaximumContext()
    {
        // Arrange
        var errorType = new DomainErrorType.AboveMaximum(Maximum: "1000");

        // Assert
        errorType.Maximum.ShouldBe("1000");
    }

    [Fact]
    public void DomainErrorType_Custom_ContainsNameContext()
    {
        // Arrange
        var errorType = new DomainErrorType.Custom("MyCustomError");

        // Assert
        errorType.Name.ShouldBe("MyCustomError");
        errorType.ErrorName.ShouldBe("MyCustomError");
    }

    #endregion

    #region DomainErrorType record equality 테스트

    [Fact]
    public void DomainErrorType_Records_SupportEquality()
    {
        // Assert - same type, same values
        new DomainErrorType.Empty().ShouldBe(new DomainErrorType.Empty());
        new DomainErrorType.TooShort(8).ShouldBe(new DomainErrorType.TooShort(8));

        // Assert - same type, different values
        new DomainErrorType.TooShort(8).ShouldNotBe(new DomainErrorType.TooShort(10));

        // Assert - different types
        ((DomainErrorType)new DomainErrorType.Empty()).ShouldNotBe(new DomainErrorType.Null());
    }

    #endregion
}
