using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

// 테스트용 값 객체
public sealed class ApplyTestValueObject1 : SimpleValueObject<string>
{
    private ApplyTestValueObject1(string value) : base(value) { }
    public static ApplyTestValueObject1 Create(string value) => new(value);
}

public sealed class ApplyTestValueObject2 : SimpleValueObject<int>
{
    private ApplyTestValueObject2(int value) : base(value) { }
    public static ApplyTestValueObject2 Create(int value) => new(value);
}

public sealed class ApplyTestValueObject3 : SimpleValueObject<decimal>
{
    private ApplyTestValueObject3(decimal value) : base(value) { }
    public static ApplyTestValueObject3 Create(decimal value) => new(value);
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class TypedValidationApplyTests
{
    #region 2-Tuple Apply - All TypedValidation

    [Fact]
    public void Apply_2Tuple_AllTyped_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;

        // Act - No .As() needed
        Validation<Error, (string, int)> actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1),
             ValidationRules<ApplyTestValueObject2>.Positive(value2))
                .Apply((v1, v2) => (v1, v2));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Item1.ShouldBe(value1);
                v.Item2.ShouldBe(value2);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_2Tuple_AllTyped_CollectsAllErrors_WhenBothFail()
    {
        // Arrange
        var value1 = ""; // Will fail
        var value2 = -1; // Will fail

        // Act
        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1),
             ValidationRules<ApplyTestValueObject2>.Positive(value2))
                .Apply((v1, v2) => (v1, v2));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(2)); // Both errors collected
    }

    #endregion

    #region 2-Tuple Apply - Mixed Types

    [Fact]
    public void Apply_2Tuple_TypedThenValidation_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;

        // Act
        Validation<Error, int> directValidation = value2 > 0
            ? value2
            : DomainError.For<ApplyTestValueObject2, int>(new DomainErrorType.NotPositive(), value2, "Must be positive");

        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1), directValidation)
                .Apply((v1, v2) => (v1, v2));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_2Tuple_ValidationThenTyped_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;

        // Act
        Validation<Error, string> directValidation = !string.IsNullOrEmpty(value1)
            ? value1
            : DomainError.For<ApplyTestValueObject1>(new DomainErrorType.Empty(), value1, "Cannot be empty");

        var actual =
            (directValidation, ValidationRules<ApplyTestValueObject2>.Positive(value2))
                .Apply((v1, v2) => (v1, v2));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region 3-Tuple Apply - All TypedValidation

    [Fact]
    public void Apply_3Tuple_AllTyped_ReturnsSuccess_WhenAllValidationsPass()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;

        // Act
        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1),
             ValidationRules<ApplyTestValueObject2>.Positive(value2),
             ValidationRules<ApplyTestValueObject3>.Positive(value3))
                .Apply((v1, v2, v3) => (v1, v2, v3));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Item1.ShouldBe(value1);
                v.Item2.ShouldBe(value2);
                v.Item3.ShouldBe(value3);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_3Tuple_AllTyped_CollectsAllErrors_WhenAllFail()
    {
        // Arrange
        var value1 = ""; // Will fail
        var value2 = -1; // Will fail
        var value3 = -100.5m; // Will fail

        // Act
        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1),
             ValidationRules<ApplyTestValueObject2>.Positive(value2),
             ValidationRules<ApplyTestValueObject3>.Positive(value3))
                .Apply((v1, v2, v3) => (v1, v2, v3));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Count.ShouldBe(3)); // All errors collected
    }

    #endregion

    #region 3-Tuple Apply - Validation + Validation + TypedValidation (ExchangeRate pattern)

    [Fact]
    public void Apply_3Tuple_VVT_ReturnsSuccess_WhenAllPass()
    {
        // Arrange
        var baseCurrency = "USD";
        var quoteCurrency = "KRW";
        var rate = 1350.5m;

        // Act - ExchangeRate.Validate pattern
        Validation<Error, string> validateCurrency1 = baseCurrency.Length == 3
            ? baseCurrency
            : DomainError.For<ApplyTestValueObject1>(new DomainErrorType.WrongLength(3), baseCurrency, "Must be 3 chars");

        Validation<Error, string> validateCurrency2 = quoteCurrency.Length == 3
            ? quoteCurrency
            : DomainError.For<ApplyTestValueObject1>(new DomainErrorType.WrongLength(3), quoteCurrency, "Must be 3 chars");

        var actual =
            (validateCurrency1, validateCurrency2, ValidationRules<ApplyTestValueObject3>.Positive(rate))
                .Apply((b, q, r) => (Base: b, Quote: q, Rate: r));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Base.ShouldBe(baseCurrency);
                v.Quote.ShouldBe(quoteCurrency);
                v.Rate.ShouldBe(rate);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    #endregion

    #region 3-Tuple Apply - Other Mixed Combinations

    [Fact]
    public void Apply_3Tuple_TVV_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;

        // Act
        Validation<Error, int> v2 = value2;
        Validation<Error, decimal> v3 = value3;

        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1), v2, v3)
                .Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_3Tuple_VTV_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;

        // Act
        Validation<Error, string> v1 = value1;
        Validation<Error, decimal> v3 = value3;

        var actual =
            (v1, ValidationRules<ApplyTestValueObject2>.Positive(value2), v3)
                .Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_3Tuple_TTV_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;

        // Act
        Validation<Error, decimal> v3 = value3;

        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1),
             ValidationRules<ApplyTestValueObject2>.Positive(value2),
             v3)
                .Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_3Tuple_TVT_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;

        // Act
        Validation<Error, int> v2 = value2;

        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1),
             v2,
             ValidationRules<ApplyTestValueObject3>.Positive(value3))
                .Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_3Tuple_VTT_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;

        // Act
        Validation<Error, string> v1 = value1;

        var actual =
            (v1,
             ValidationRules<ApplyTestValueObject2>.Positive(value2),
             ValidationRules<ApplyTestValueObject3>.Positive(value3))
                .Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region 4-Tuple Apply

    [Fact]
    public void Apply_4Tuple_AllTyped_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;
        var value4 = "extra";

        // Act
        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1),
             ValidationRules<ApplyTestValueObject2>.Positive(value2),
             ValidationRules<ApplyTestValueObject3>.Positive(value3),
             ValidationRules<ApplyTestValueObject1>.NotEmpty(value4))
                .Apply((a, b, c, d) => (a, b, c, d));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_4Tuple_FirstTyped_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;
        var value4 = "extra";

        // Act
        Validation<Error, int> v2 = value2;
        Validation<Error, decimal> v3 = value3;
        Validation<Error, string> v4 = value4;

        var actual =
            (ValidationRules<ApplyTestValueObject1>.NotEmpty(value1), v2, v3, v4)
                .Apply((a, b, c, d) => (a, b, c, d));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_4Tuple_LastTyped_ReturnsSuccess()
    {
        // Arrange
        var value1 = "test";
        var value2 = 42;
        var value3 = 100.5m;
        var value4 = 200m;

        // Act
        Validation<Error, string> v1 = value1;
        Validation<Error, int> v2 = value2;
        Validation<Error, decimal> v3 = value3;

        var actual =
            (v1, v2, v3, ValidationRules<ApplyTestValueObject3>.Positive(value4))
                .Apply((a, b, c, d) => (a, b, c, d));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Real-World Pattern Tests

    [Fact]
    public void Apply_MoneyPattern_WorksWithoutAs()
    {
        // Arrange - Simulating Money.Validate pattern
        var amount = 100m;
        var currency = "USD";

        // Act
        var actual =
            (ValidationRules<ApplyTestValueObject3>.NonNegative(amount),
             ValidationRules<ApplyTestValueObject1>.NotEmpty(currency))
                .Apply((a, c) => (Amount: a, Currency: c));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v =>
            {
                v.Amount.ShouldBe(amount);
                v.Currency.ShouldBe(currency);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_ExchangeRatePattern_WorksWithMixedTypes()
    {
        // Arrange - Simulating ExchangeRate.Validate pattern
        var baseCurrency = "USD";
        var quoteCurrency = "KRW";
        var rate = 1350.5m;

        // Simulating ValidateCurrency that returns Validation<Error, string>
        Validation<Error, string> ValidateCurrency(string value) =>
            !string.IsNullOrEmpty(value) && value.Length == 3
                ? value.ToUpperInvariant()
                : DomainError.For<ApplyTestValueObject1>(new DomainErrorType.InvalidFormat(), value, "Invalid currency");

        // Act - No casting needed for TypedValidation, no .As() needed at the end
        var actual =
            (ValidateCurrency(baseCurrency),
             ValidateCurrency(quoteCurrency),
             ValidationRules<ApplyTestValueObject3>.Positive(rate))
                .Apply((b, q, r) => (Base: b, Quote: q, Rate: r));

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
