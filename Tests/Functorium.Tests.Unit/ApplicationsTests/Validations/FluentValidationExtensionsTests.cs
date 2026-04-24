using Ardalis.SmartEnum;
using FluentValidation;
using Functorium.Applications.Validations;
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using static Functorium.Domains.Errors.DomainErrorType;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.ApplicationsTests.Validations;

// 테스트용 값 객체
public sealed class TestProductName : SimpleValueObject<string>
{
    private TestProductName(string value) : base(value) { }

    public static Fin<TestProductName> Create(string value) =>
        CreateFromValidation(Validate(value), v => new TestProductName(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<TestProductName>.NotEmpty(value)
            .ThenMaxLength(100);
}

public sealed class TestPrice : ComparableSimpleValueObject<decimal>
{
    private TestPrice(decimal value) : base(value) { }

    public static Fin<TestPrice> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new TestPrice(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<TestPrice>.Positive(value)
            .ThenAtMost(1_000_000);
}

// 테스트용 Request
public sealed record TestRequest(string Name, decimal Price);

// 테스트용 Request (MustSatisfyValidationOf 테스트용 - 입력 != 출력 타입)
// 실제 시나리오: API에서 age를 string으로 받아 int로 검증/변환
public sealed record TestAgeRequest(string Age);

// 테스트용 Age Value Object (string → int 변환)
// MustSatisfyValidationOf<TValueObject> 테스트를 위한 명확한 타입 변환 예제
// 실제 시나리오: 외부에서 string으로 입력받고 내부에서 int로 관리
public sealed class TestAge : ComparableSimpleValueObject<int>
{
    public const int MinAge = 0;
    public const int MaxAge = 150;

    private TestAge(int value) : base(value) { }

    public static Fin<TestAge> Create(int value) =>
        CreateFromValidation(ValidateInt(value), v => new TestAge(v));

    // 내부용: int → Validation<Error, int>
    private static Validation<Error, int> ValidateInt(int value) =>
        ValidationRules<TestAge>.Between(value, MinAge, MaxAge);

    // MustSatisfyValidationOf용: string → Validation<Error, int> (입력 타입 != 출력 타입)
    // FluentValidation에서 string 속성을 int로 검증/변환할 때 사용
    public static Validation<Error, int> Validate(string value) =>
        int.TryParse(value, out var parsed)
            ? ValidateInt(parsed)
            : DomainError.For<TestAge>(
                new InvalidFormat(),
                value,
                $"'{value}'은(는) 유효한 숫자가 아닙니다");
}

// 테스트용 SmartEnum (string 기반)
public sealed class TestCurrency : SmartEnum<TestCurrency, string>
{
    public static readonly TestCurrency KRW = new(nameof(KRW), "KRW");
    public static readonly TestCurrency USD = new(nameof(USD), "USD");
    public static readonly TestCurrency EUR = new(nameof(EUR), "EUR");

    private TestCurrency(string name, string value) : base(name, value) { }
}

// 테스트용 SmartEnum (int 기반)
public sealed class TestStatus : SmartEnum<TestStatus, int>
{
    public static readonly TestStatus Pending = new(nameof(Pending), 0);
    public static readonly TestStatus Active = new(nameof(Active), 1);
    public static readonly TestStatus Completed = new(nameof(Completed), 2);

    private TestStatus(string name, int value) : base(name, value) { }
}

// 테스트용 SmartEnum Request
public sealed record TestSmartEnumRequest(string CurrencyCode, string CurrencyName, int StatusValue);

// 테스트용 SmartEnum Validator
public sealed class TestSmartEnumRequestValidator : AbstractValidator<TestSmartEnumRequest>
{
    public TestSmartEnumRequestValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .MustBeEnum<TestSmartEnumRequest, TestCurrency, string>();

        RuleFor(x => x.CurrencyName)
            .MustBeEnumName<TestSmartEnumRequest, TestCurrency, string>();

        RuleFor(x => x.StatusValue)
            .MustBeEnum<TestSmartEnumRequest, TestStatus>();
    }
}

// 테스트용 Validator (MustSatisfyValidation - 입력 == 출력 타입)
public sealed class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        // MustSatisfyValidation: 입력 타입 == 출력 타입 (타입 추론 작동)
        RuleFor(x => x.Name)
            .MustSatisfyValidation(TestProductName.Validate);

        RuleFor(x => x.Price)
            .MustSatisfyValidation(TestPrice.Validate);
    }
}

// 테스트용 Validator (MustSatisfyValidationOf - 입력 != 출력 타입)
public sealed class TestAgeRequestValidator : AbstractValidator<TestAgeRequest>
{
    public TestAgeRequestValidator()
    {
        // MustSatisfyValidationOf<TRequest, TProperty, TValueObject>: 입력 타입(string) != 출력 타입(int)
        // string으로 입력받은 Age를 int로 검증/변환
        // C# 14 extension members의 제네릭 타입 추론 제한으로 모든 타입 파라미터 명시 필요
        RuleFor(x => x.Age)
            .MustSatisfyValidationOf<TestAgeRequest, string, int>(TestAge.Validate);
    }
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class FluentValidationExtensionsTests
{
    private readonly TestRequestValidator _validator = new();

    #region Success Cases

    [Fact]
    public void Validate_ReturnsNoError_WhenAllValuesAreValid()
    {
        // Arrange
        var request = new TestRequest("Valid Product Name", 100m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
        actual.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("A", 0.01)]
    [InlineData("Product", 1000000)]
    [InlineData("가나다라마바사아자차", 500)]
    public void Validate_ReturnsNoError_WhenValuesAreAtBoundary(string name, decimal price)
    {
        // Arrange
        var request = new TestRequest(name, price);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Single Error Cases

    [Fact]
    public void Validate_ReturnsValidationError_WhenNameIsEmpty()
    {
        // Arrange
        var request = new TestRequest("", 100m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Name");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestProductName.Empty");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenNameExceedsMaxLength()
    {
        // Arrange
        var longName = new string('A', 101);
        var request = new TestRequest(longName, 100m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Name");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestProductName.TooLong");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenPriceIsZero()
    {
        // Arrange
        var request = new TestRequest("Valid Name", 0m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Price");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestPrice.NotPositive");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenPriceIsNegative()
    {
        // Arrange
        var request = new TestRequest("Valid Name", -100m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Price");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestPrice.NotPositive");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenPriceExceedsMaximum()
    {
        // Arrange
        var request = new TestRequest("Valid Name", 1_000_001m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Price");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestPrice.AboveMaximum");
    }

    #endregion

    #region Multiple Error Cases

    [Fact]
    public void Validate_ReturnsMultipleErrors_WhenBothValuesAreInvalid()
    {
        // Arrange
        var request = new TestRequest("", -100m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(2);

        var nameError = actual.Errors.FirstOrDefault(e => e.PropertyName == "Name");
        var priceError = actual.Errors.FirstOrDefault(e => e.PropertyName == "Price");

        nameError.ShouldNotBeNull();
        priceError.ShouldNotBeNull();
    }

    #endregion

    #region Null Handling Cases

    [Fact]
    public void Validate_SkipsValidation_WhenValueIsNull()
    {
        // Arrange
        var request = new TestRequest(null!, 100m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Error Code Format Cases

    [Fact]
    public void Validate_ReturnsErrorWithErrorCode_WhenValidationFails()
    {
        // Arrange
        var request = new TestRequest("", 100m);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        var errorMessage = actual.Errors[0].ErrorMessage;

        // ErrorCode가 대괄호 안에 포함되어야 함
        errorMessage.ShouldStartWith("[Domain.");
        errorMessage.ShouldContain("]");
    }

    #endregion
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class FluentValidationSmartEnumExtensionsTests
{
    private readonly TestSmartEnumRequestValidator _validator = new();

    #region MustBeEnum (Value) Tests

    [Theory]
    [InlineData("KRW")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void MustBeEnum_ReturnsNoError_WhenValueIsValid(string currencyCode)
    {
        // Arrange
        var request = new TestSmartEnumRequest(currencyCode, "KRW", 1);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.Errors.Where(e => e.PropertyName == "CurrencyCode").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("GBP")]
    [InlineData("INVALID")]
    public void MustBeEnum_ReturnsError_WhenValueIsInvalid(string currencyCode)
    {
        // Arrange
        var request = new TestSmartEnumRequest(currencyCode, "KRW", 1);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        var error = actual.Errors.FirstOrDefault(e => e.PropertyName == "CurrencyCode");
        error.ShouldNotBeNull();
        error.ErrorMessage.ShouldContain(currencyCode);
        error.ErrorMessage.ShouldContain("TestCurrency");
    }

    [Fact]
    public void MustBeEnum_SkipsValidation_WhenValueIsNull()
    {
        // Arrange
        var request = new TestSmartEnumRequest(null!, "KRW", 1);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.Errors.Where(e => e.PropertyName == "CurrencyCode").ShouldBeEmpty();
    }

    #endregion

    #region MustBeEnumName Tests

    [Theory]
    [InlineData("KRW")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void MustBeEnumName_ReturnsNoError_WhenNameIsValid(string currencyName)
    {
        // Arrange
        var request = new TestSmartEnumRequest("KRW", currencyName, 1);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.Errors.Where(e => e.PropertyName == "CurrencyName").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("InvalidName")]
    [InlineData("krw")] // Case sensitive
    [InlineData("UNKNOWN")]
    public void MustBeEnumName_ReturnsError_WhenNameIsInvalid(string currencyName)
    {
        // Arrange
        var request = new TestSmartEnumRequest("KRW", currencyName, 1);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        var error = actual.Errors.FirstOrDefault(e => e.PropertyName == "CurrencyName");
        error.ShouldNotBeNull();
        error.ErrorMessage.ShouldContain(currencyName);
        error.ErrorMessage.ShouldContain("TestCurrency");
        error.ErrorMessage.ShouldContain("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MustBeEnumName_SkipsValidation_WhenNameIsNullOrWhitespace(string? currencyName)
    {
        // Arrange
        var request = new TestSmartEnumRequest("KRW", currencyName!, 1);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.Errors.Where(e => e.PropertyName == "CurrencyName").ShouldBeEmpty();
    }

    #endregion

    #region MustBeEnum (int overload) Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MustBeEnum_IntOverload_ReturnsNoError_WhenValueIsValid(int statusValue)
    {
        // Arrange
        var request = new TestSmartEnumRequest("KRW", "KRW", statusValue);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.Errors.Where(e => e.PropertyName == "StatusValue").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public void MustBeEnum_IntOverload_ReturnsError_WhenValueIsInvalid(int statusValue)
    {
        // Arrange
        var request = new TestSmartEnumRequest("KRW", "KRW", statusValue);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        var error = actual.Errors.FirstOrDefault(e => e.PropertyName == "StatusValue");
        error.ShouldNotBeNull();
        error.ErrorMessage.ShouldContain(statusValue.ToString());
        error.ErrorMessage.ShouldContain("TestStatus");
    }

    #endregion

    #region All Valid Cases

    [Fact]
    public void Validate_ReturnsNoError_WhenAllSmartEnumValuesAreValid()
    {
        // Arrange
        var request = new TestSmartEnumRequest("USD", "USD", 1);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
        actual.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Multiple Error Cases

    [Fact]
    public void Validate_ReturnsMultipleErrors_WhenMultipleSmartEnumValuesAreInvalid()
    {
        // Arrange
        var request = new TestSmartEnumRequest("INVALID", "invalid", 999);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(3);

        actual.Errors.ShouldContain(e => e.PropertyName == "CurrencyCode");
        actual.Errors.ShouldContain(e => e.PropertyName == "CurrencyName");
        actual.Errors.ShouldContain(e => e.PropertyName == "StatusValue");
    }

    #endregion
}

/// <summary>
/// MustSatisfyValidationOf&lt;TValueObject&gt; 메서드 테스트
/// 입력 타입(string)과 출력 타입(int)이 다른 경우 테스트
/// 실제 시나리오: API에서 age를 string으로 받아 int로 검증/변환
/// </summary>
[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class MustSatisfyValidationOfTests
{
    private readonly TestAgeRequestValidator _validator = new();

    #region Success Cases

    [Theory]
    [InlineData("0")]
    [InlineData("25")]
    [InlineData("100")]
    [InlineData("150")]
    public void MustSatisfyValidationOf_ReturnsNoError_WhenAgeIsValid(string age)
    {
        // Arrange
        var request = new TestAgeRequest(age);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
        actual.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Failure Cases - Invalid Format

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5")]
    [InlineData("")]
    [InlineData("   ")]
    public void MustSatisfyValidationOf_ReturnsError_WhenAgeFormatIsInvalid(string age)
    {
        // Arrange
        var request = new TestAgeRequest(age);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Age");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestAge.InvalidFormat");
    }

    #endregion

    #region Failure Cases - Out of Range

    [Theory]
    [InlineData("-1")]
    [InlineData("-100")]
    public void MustSatisfyValidationOf_ReturnsError_WhenAgeIsBelowMinimum(string age)
    {
        // Arrange
        var request = new TestAgeRequest(age);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Age");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestAge.OutOfRange");
    }

    [Theory]
    [InlineData("151")]
    [InlineData("200")]
    public void MustSatisfyValidationOf_ReturnsError_WhenAgeIsAboveMaximum(string age)
    {
        // Arrange
        var request = new TestAgeRequest(age);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.Count.ShouldBe(1);
        actual.Errors[0].PropertyName.ShouldBe("Age");
        actual.Errors[0].ErrorMessage.ShouldContain("Domain.TestAge.OutOfRange");
    }

    #endregion

    #region Null Handling Cases

    [Fact]
    public void MustSatisfyValidationOf_SkipsValidation_WhenValueIsNull()
    {
        // Arrange
        var request = new TestAgeRequest(null!);

        // Act
        var actual = _validator.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    #endregion
}
