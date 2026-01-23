using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
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
        // Arrange
        Validation<Error, string> initial = "valid";

        // Act
        var actual = initial.ThenNotEmpty<SampleValueObject>();

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenNotEmpty_ReturnsOriginalFailure_WhenChainedAfterFailure()
    {
        // Arrange
        var originalError = Error.New("Original error");
        Validation<Error, string> initial = originalError;

        // Act
        var actual = initial.ThenNotEmpty<SampleValueObject>();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Head.ShouldBe(originalError));
    }

    [Fact]
    public void ThenMinLength_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, string> initial = "12345";

        // Act
        var actual = initial.ThenMinLength<SampleValueObject>(5);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenMaxLength_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, string> initial = "12345";

        // Act
        var actual = initial.ThenMaxLength<SampleValueObject>(10);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenExactLength_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, string> initial = "ABC";

        // Act
        var actual = initial.ThenExactLength<SampleValueObject>(3);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenMatches_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, string> initial = "110-1234567890";

        // Act
        var actual = initial.ThenMatches<SampleValueObject>(AccountPattern);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenNormalize_TransformsValue()
    {
        // Arrange
        Validation<Error, string> initial = "  trimmed  ";

        // Act
        var actual = initial.ThenNormalize(v => v.Trim());

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v => v.ShouldBe("trimmed"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    #endregion

    #region Numeric Chaining Tests

    [Fact]
    public void ThenNonNegative_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, decimal> initial = 0m;

        // Act
        var actual = initial.ThenNonNegative<NumericValueObject, decimal>();

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenPositive_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, decimal> initial = 1m;

        // Act
        var actual = initial.ThenPositive<NumericValueObject, decimal>();

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenBetween_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, decimal> initial = 50m;

        // Act
        var actual = initial.ThenBetween<NumericValueObject, decimal>(0m, 100m);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenAtMost_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, decimal> initial = 50m;

        // Act
        var actual = initial.ThenAtMost<NumericValueObject, decimal>(100m);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenAtLeast_ChainsCorrectly()
    {
        // Arrange
        Validation<Error, decimal> initial = 50m;

        // Act
        var actual = initial.ThenAtLeast<NumericValueObject, decimal>(10m);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region ThenMust Tests

    [Fact]
    public void ThenMust_ChainsCorrectlyWithStaticMessage()
    {
        // Arrange
        Validation<Error, string> initial = "UPPERCASE";

        // Act
        var actual = initial.ThenMust<SampleValueObject, string>(
            v => v == v.ToUpperInvariant(),
            new DomainErrorType.NotUpperCase(),
            "Value must be uppercase");

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThenMust_ChainsCorrectlyWithMessageFactory()
    {
        // Arrange
        Validation<Error, string> initial = "lowercase";

        // Act
        var actual = initial.ThenMust<SampleValueObject, string>(
            v => v == v.ToUpperInvariant(),
            new DomainErrorType.NotUpperCase(),
            v => $"Value '{v}' must be uppercase");

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
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
        var actual = ValidationRules.NotEmpty<SampleValueObject>(value)
            .ThenNormalize(v => v.Trim())
            .ThenMatches<SampleValueObject>(AccountPattern);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v => v.ShouldBe("110-1234567890"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ComplexChain_StopsAtFirstFailure_WhenSequentialValidation()
    {
        // Arrange
        var value = "";

        // Act
        var actual = ValidationRules.NotEmpty<SampleValueObject>(value)
            .ThenMatches<SampleValueObject>(AccountPattern);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                errors.Count.ShouldBe(1);
                var error = (ErrorCodeExpected)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.SampleValueObject.Empty");
            });
    }

    [Fact]
    public void ComplexNumericChain_ValidatesMultipleConditions()
    {
        // Arrange
        var value = 50m;

        // Act
        var actual = ValidationRules.NonNegative<NumericValueObject, decimal>(value)
            .ThenAtMost<NumericValueObject, decimal>(100m);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
