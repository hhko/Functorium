using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class ValidationRulesExtensionsTests
{
    private static readonly Regex AccountPattern = new(@"^\d{3}-\d{10,14}$", RegexOptions.Compiled);

    #region String Chaining Tests

    [Fact]
    public void ThenNotEmpty_ReturnsSuccess_WhenChainedAfterSuccess()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("valid")
            .ThenNotEmpty();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenNotEmpty_ReturnsOriginalFailure_WhenChainedAfterFailure()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("")
            .ThenNotEmpty();

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe("Domain.SampleValueObject.Empty");
            });
    }

    [Fact]
    public void ThenMinLength_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("12345")
            .ThenMinLength(5);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenMaxLength_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("12345")
            .ThenMaxLength(10);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenExactLength_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("ABC")
            .ThenExactLength(3);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenMatches_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("110-1234567890")
            .ThenMatches(AccountPattern);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenNormalize_TransformsValue()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("  trimmed  ")
            .ThenNormalize(v => v.Trim());

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe("trimmed"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    #endregion

    #region Numeric Chaining Tests

    [Fact]
    public void ThenNonNegative_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.NonNegative(0m)
            .ThenNonNegative();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenPositive_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.Positive(1m)
            .ThenPositive();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenBetween_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.NonNegative(50m)
            .ThenBetween(0m, 100m);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenAtMost_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.NonNegative(50m)
            .ThenAtMost(100m);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenAtLeast_ChainsCorrectly()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.NonNegative(50m)
            .ThenAtLeast(10m);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region ThenMust Tests

    [Fact]
    public void ThenMust_ChainsCorrectlyWithStaticMessage()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("UPPERCASE")
            .ThenMust(
                v => v == v.ToUpperInvariant(),
                new DomainErrorKind.NotUpperCase(),
                "Value must be uppercase");

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenMust_ChainsCorrectlyWithMessageFactory()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("lowercase")
            .ThenMust(
                v => v == v.ToUpperInvariant(),
                new DomainErrorKind.NotUpperCase(),
                v => $"Value '{v}' must be uppercase");

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Head.Message.ShouldContain("lowercase"));
    }

    #endregion

    #region Complex Chaining Tests

    [Fact]
    public void ComplexChain_ValidatesAndTransforms_WhenAllConditionsMet()
    {
        // Arrange
        var value = "  110-1234567890  ";

        // Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty(value)
            .ThenNormalize(v => v.Trim())
            .ThenMatches(AccountPattern);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe("110-1234567890"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ComplexChain_StopsAtFirstFailure_WhenSequentialValidation()
    {
        // Arrange
        var value = "";

        // Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty(value)
            .ThenMatches(AccountPattern);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                errors.Count.ShouldBe(1);
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe("Domain.SampleValueObject.Empty");
            });
    }

    [Fact]
    public void ComplexNumericChain_ValidatesMultipleConditions()
    {
        // Arrange
        var value = 50m;

        // Act
        var actual = ValidationRules<NumericValueObject>.NonNegative(value)
            .ThenAtMost(100m);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenNormalize_BeforeMaxLength_ValidatesNormalizedValue()
    {
        // Arrange: "  ab  " → Trim → "ab"(2자) → MaxLength(5) 통과
        var actual = ValidationRules<SampleValueObject>.NotEmpty("  ab  ")
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(5);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe("ab"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ThenNormalize_BeforeMaxLength_FailsOnNormalizedValueExceedingLimit()
    {
        // Arrange: "abc" → Trim → "abc"(3자) → MaxLength(2) 실패
        var actual = ValidationRules<SampleValueObject>.NotEmpty("abc")
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(2);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ThenNormalize_AfterMaxLength_ValidatesRawValue()
    {
        // Arrange: "  ab  "(6자) → MaxLength(3) 실패 (원시 입력 기준)
        var actual = ValidationRules<SampleValueObject>.NotEmpty("  ab  ")
            .ThenMaxLength(3)
            .ThenNormalize(v => v.Trim());

        // Assert: 원시 입력 6자가 MaxLength(3) 초과하므로 실패
        actual.Value.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ThenNormalize_BeforeMinLength_ValidatesNormalizedValue()
    {
        // Arrange: "  a  " → Trim → "a"(1자) → MinLength(3) 실패
        var actual = ValidationRules<SampleValueObject>.NotEmpty("  a  ")
            .ThenNormalize(v => v.Trim())
            .ThenMinLength(3);

        // Assert: 정규화된 값 "a"(1자)가 MinLength(3) 미달
        actual.Value.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ThenNormalize_AfterMinLength_ValidatesRawValue_AllowsInvalidNormalizedResult()
    {
        // Arrange: "  a  "(5자) → MinLength(3) 통과 → Trim → "a"(1자)
        // 이것이 정규화 순서 버그: 원시 값은 통과하지만 정규화된 값은 MinLength 위반
        var actual = ValidationRules<SampleValueObject>.NotEmpty("  a  ")
            .ThenMinLength(3)
            .ThenNormalize(v => v.Trim());

        // Assert: 원시 입력 5자가 MinLength(3) 통과하므로 성공 — 하지만 결과는 "a"(1자)
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe("a"),  // 1자 — MinLength(3) 의도 위반
            Fail: _ => Assert.Fail("Should succeed (but result violates intent)"));
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void TypedValidation_CanBeImplicitlyConvertedToValidation()
    {
        // Arrange
        var typedValidation = ValidationRules<SampleValueObject>.NotEmpty("valid");

        // Act - implicit conversion
        Validation<Error, string> validation = typedValidation;

        // Assert
        validation.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ChainResult_CanBeAssignedToValidation()
    {
        // Arrange & Act
        Validation<Error, string> actual = ValidationRules<SampleValueObject>.NotEmpty("test")
            .ThenMaxLength(100)
            .ThenNormalize(v => v.ToUpperInvariant());

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v => v.ShouldBe("TEST"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    #endregion

    #region Format Chaining Tests

    [Fact]
    public void ThenIsUpperCase_ChainsCorrectly_WhenValueIsUpperCase()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("UPPERCASE")
            .ThenIsUpperCase();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenIsUpperCase_ReturnsFailure_WhenValueIsNotUpperCase()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("MixedCase")
            .ThenIsUpperCase();

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe("Domain.SampleValueObject.NotUpperCase");
            });
    }

    [Fact]
    public void ThenIsLowerCase_ChainsCorrectly_WhenValueIsLowerCase()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("lowercase")
            .ThenIsLowerCase();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenIsLowerCase_ReturnsFailure_WhenValueIsNotLowerCase()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmpty("MixedCase")
            .ThenIsLowerCase();

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe("Domain.SampleValueObject.NotLowerCase");
            });
    }

    #endregion

    #region Range Chaining Tests

    [Fact]
    public void ThenValidRange_ChainsCorrectly_WhenRangeIsValid()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.ValidRange(1, 10)
            .ThenValidRange();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => { v.Min.ShouldBe(1); v.Max.ShouldBe(10); },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ThenValidRange_ReturnsFailure_WhenRangeIsInverted()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.ValidRange(10, 1)
            .ThenValidRange();

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe("Domain.NumericValueObject.RangeInverted");
            });
    }

    [Fact]
    public void ThenValidStrictRange_ChainsCorrectly_WhenRangeIsStrictlyValid()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.ValidRange(1, 10)
            .ThenValidStrictRange();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenValidStrictRange_ReturnsFailure_WhenRangeIsEmpty()
    {
        // Arrange & Act
        var actual = ValidationRules<NumericValueObject>.ValidRange(5, 5)
            .ThenValidStrictRange();

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe("Domain.NumericValueObject.RangeEmpty");
            });
    }

    #endregion

    #region Collection Chaining Tests

    [Fact]
    public void ThenNotEmptyArray_ChainsCorrectly_WhenArrayIsNotEmpty()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmptyArray(new[] { "a", "b" })
            .ThenNotEmptyArray();

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.Length.ShouldBe(2),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ThenNotEmptyArray_ReturnsFailure_WhenArrayIsEmpty()
    {
        // Arrange & Act
        var actual = ValidationRules<SampleValueObject>.NotEmptyArray(new string[0])
            .ThenNotEmptyArray();

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe("Domain.SampleValueObject.Empty");
            });
    }

    #endregion
}
