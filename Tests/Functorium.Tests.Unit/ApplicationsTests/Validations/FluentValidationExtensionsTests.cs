using Ardalis.SmartEnum;
using FluentValidation;
using Functorium.Applications.Validations;
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.ApplicationsTests.Validations;

// 테스트용 값 객체
public sealed class TestProductName : SimpleValueObject<string>
{
    private TestProductName(string value) : base(value) { }

    public static Fin<TestProductName> Create(string value) =>
        CreateFromValidation(Validate(value), v => new TestProductName(v));

    public static Validation<Error, string> Validate(string value) =>
        Validate<TestProductName>.NotEmpty(value)
            .ThenMaxLength(100);
}

public sealed class TestPrice : ComparableSimpleValueObject<decimal>
{
    private TestPrice(decimal value) : base(value) { }

    public static Fin<TestPrice> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new TestPrice(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        Validate<TestPrice>.Positive(value)
            .ThenAtMost(1_000_000);
}

// 테스트용 Request
public sealed record TestRequest(string Name, decimal Price);

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

// 테스트용 Validator
public sealed class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.Name)
            .MustSatisfyValueObjectValidation<TestRequest, string, string>(TestProductName.Validate);

        RuleFor(x => x.Price)
            .MustSatisfyValueObjectValidation<TestRequest, decimal, decimal>(TestPrice.Validate);
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
        actual.Errors[0].ErrorMessage.ShouldContain("DomainErrors.TestProductName.Empty");
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
        actual.Errors[0].ErrorMessage.ShouldContain("DomainErrors.TestProductName.TooLong");
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
        actual.Errors[0].ErrorMessage.ShouldContain("DomainErrors.TestPrice.NotPositive");
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
        actual.Errors[0].ErrorMessage.ShouldContain("DomainErrors.TestPrice.NotPositive");
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
        actual.Errors[0].ErrorMessage.ShouldContain("DomainErrors.TestPrice.AboveMaximum");
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
        errorMessage.ShouldStartWith("[DomainErrors.");
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
