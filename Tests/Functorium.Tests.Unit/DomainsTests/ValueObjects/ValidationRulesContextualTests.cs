using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Contextual;
using Functorium.Domains.ValueObjects.Validations.Typed;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

// 테스트용 Context Class
public sealed class ProductValidation : IValidationContext;

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class ValidationRulesContextualTests
{
    private const string TestContext = "TestContext";

    #region Named Context Entry Point Tests

    [Fact]
    public void For_CreatesValidationContext_WithCorrectName()
    {
        // Act
        var context = ValidationRules.For(TestContext);

        // Assert
        context.ContextName.ShouldBe(TestContext);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void For_ThrowsException_WhenContextNameIsEmpty(string contextName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => ValidationRules.For(contextName));
    }

    #endregion

    #region NotEmpty Tests

    [Fact]
    public void NotEmpty_ReturnsSuccess_WhenValueIsNotEmpty()
    {
        // Arrange
        var value = "valid";

        // Act
        var actual = ValidationRules.For(TestContext).NotEmpty(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.ContextName.ShouldBe(TestContext);
        actual.Value.Match(
            Succ: v => v.ShouldBe(value),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NotEmpty_ReturnsFailure_WhenValueIsEmpty(string value)
    {
        // Act
        var actual = ValidationRules.For(TestContext).NotEmpty(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.ContextName.ShouldBe(TestContext);
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                errors.Count.ShouldBe(1);
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.Empty");
            });
    }

    #endregion

    #region MinLength Tests

    [Fact]
    public void MinLength_ReturnsSuccess_WhenLengthMeetsMinimum()
    {
        // Arrange
        var value = "12345";

        // Act
        var actual = ValidationRules.For(TestContext).MinLength(value, 5);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MinLength_ReturnsFailure_WhenLengthBelowMinimum()
    {
        // Arrange
        var value = "1234";

        // Act
        var actual = ValidationRules.For(TestContext).MinLength(value, 5);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.TooShort");
            });
    }

    #endregion

    #region MaxLength Tests

    [Fact]
    public void MaxLength_ReturnsSuccess_WhenLengthWithinMaximum()
    {
        // Arrange
        var value = "12345";

        // Act
        var actual = ValidationRules.For(TestContext).MaxLength(value, 5);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MaxLength_ReturnsFailure_WhenLengthExceedsMaximum()
    {
        // Arrange
        var value = "123456";

        // Act
        var actual = ValidationRules.For(TestContext).MaxLength(value, 5);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.TooLong");
            });
    }

    #endregion

    #region ExactLength Tests

    [Fact]
    public void ExactLength_ReturnsSuccess_WhenLengthMatches()
    {
        // Arrange
        var value = "ABC";

        // Act
        var actual = ValidationRules.For(TestContext).ExactLength(value, 3);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("AB", 3)]
    [InlineData("ABCD", 3)]
    public void ExactLength_ReturnsFailure_WhenLengthDoesNotMatch(string value, int expected)
    {
        // Act
        var actual = ValidationRules.For(TestContext).ExactLength(value, expected);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.WrongLength");
            });
    }

    #endregion

    #region Numeric Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void NonNegative_ReturnsSuccess_WhenValueIsNonNegative(decimal value)
    {
        // Act
        var actual = ValidationRules.For(TestContext).NonNegative(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void NonNegative_ReturnsFailure_WhenValueIsNegative(decimal value)
    {
        // Act
        var actual = ValidationRules.For(TestContext).NonNegative(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<decimal>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.Negative");
            });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Positive_ReturnsSuccess_WhenValueIsPositive(decimal value)
    {
        // Act
        var actual = ValidationRules.For(TestContext).Positive(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positive_ReturnsFailure_WhenValueIsNotPositive(decimal value)
    {
        // Act
        var actual = ValidationRules.For(TestContext).Positive(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<decimal>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.NotPositive");
            });
    }

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void Between_ReturnsSuccess_WhenValueInRange(decimal value, decimal min, decimal max)
    {
        // Act
        var actual = ValidationRules.For(TestContext).Between(value, min, max);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(11, 1, 10)]
    public void Between_ReturnsFailure_WhenValueOutOfRange(decimal value, decimal min, decimal max)
    {
        // Act
        var actual = ValidationRules.For(TestContext).Between(value, min, max);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<decimal>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.OutOfRange");
            });
    }

    [Fact]
    public void NotZero_ReturnsSuccess_WhenValueIsNotZero()
    {
        // Arrange
        var value = 10m;

        // Act
        var actual = ValidationRules.For(TestContext).NotZero(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void NotZero_ReturnsFailure_WhenValueIsZero()
    {
        // Arrange
        var value = 0m;

        // Act
        var actual = ValidationRules.For(TestContext).NotZero(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<decimal>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.Zero");
            });
    }

    [Fact]
    public void AtMost_ReturnsSuccess_WhenValueWithinMaximum()
    {
        // Arrange
        var value = 50m;

        // Act
        var actual = ValidationRules.For(TestContext).AtMost(value, 100m);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AtMost_ReturnsFailure_WhenValueExceedsMaximum()
    {
        // Arrange
        var value = 101m;

        // Act
        var actual = ValidationRules.For(TestContext).AtMost(value, 100m);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<decimal>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.AboveMaximum");
            });
    }

    [Fact]
    public void AtLeast_ReturnsSuccess_WhenValueMeetsMinimum()
    {
        // Arrange
        var value = 50m;

        // Act
        var actual = ValidationRules.For(TestContext).AtLeast(value, 10m);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AtLeast_ReturnsFailure_WhenValueBelowMinimum()
    {
        // Arrange
        var value = 5m;

        // Act
        var actual = ValidationRules.For(TestContext).AtLeast(value, 10m);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<decimal>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.BelowMinimum");
            });
    }

    #endregion

    #region Format Tests

    private static readonly Regex EmailPattern = new(@"^[\w.-]+@[\w.-]+\.\w+$", RegexOptions.Compiled);

    [Fact]
    public void Matches_ReturnsSuccess_WhenPatternMatches()
    {
        // Arrange
        var value = "test@example.com";

        // Act
        var actual = ValidationRules.For(TestContext).Matches(value, EmailPattern);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Matches_ReturnsFailure_WhenPatternDoesNotMatch()
    {
        // Arrange
        var value = "invalid-email";

        // Act
        var actual = ValidationRules.For(TestContext).Matches(value, EmailPattern);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.InvalidFormat");
            });
    }

    [Fact]
    public void IsUpperCase_ReturnsSuccess_WhenValueIsUpperCase()
    {
        // Arrange
        var value = "UPPERCASE";

        // Act
        var actual = ValidationRules.For(TestContext).IsUpperCase(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void IsUpperCase_ReturnsFailure_WhenValueIsNotUpperCase()
    {
        // Arrange
        var value = "lowercase";

        // Act
        var actual = ValidationRules.For(TestContext).IsUpperCase(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.NotUpperCase");
            });
    }

    [Fact]
    public void IsLowerCase_ReturnsSuccess_WhenValueIsLowerCase()
    {
        // Arrange
        var value = "lowercase";

        // Act
        var actual = ValidationRules.For(TestContext).IsLowerCase(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void IsLowerCase_ReturnsFailure_WhenValueIsNotLowerCase()
    {
        // Arrange
        var value = "UPPERCASE";

        // Act
        var actual = ValidationRules.For(TestContext).IsLowerCase(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.NotLowerCase");
            });
    }

    #endregion

    #region NotNull Tests

    [Fact]
    public void NotNull_ReturnsSuccess_WhenReferenceTypeIsNotNull()
    {
        // Arrange
        string value = "valid";

        // Act
        var actual = ValidationRules.For(TestContext).NotNull(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe(value),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void NotNull_ReturnsFailure_WhenReferenceTypeIsNull()
    {
        // Arrange
        string? value = null;

        // Act
        var actual = ValidationRules.For(TestContext).NotNull(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.Null");
            });
    }

    [Fact]
    public void NotNull_ReturnsSuccess_WhenNullableValueTypeHasValue()
    {
        // Arrange
        int? value = 42;

        // Act
        var actual = ValidationRules.For(TestContext).NotNull(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe(42),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void NotNull_ReturnsFailure_WhenNullableValueTypeIsNull()
    {
        // Arrange
        int? value = null;

        // Act
        var actual = ValidationRules.For(TestContext).NotNull(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.Null");
            });
    }

    #endregion

    #region Must Tests

    [Fact]
    public void Must_ReturnsSuccess_WhenPredicatePasses()
    {
        // Arrange
        var value = "VALID";

        // Act
        var actual = ValidationRules.For(TestContext).Must(
            value,
            v => v == v.ToUpperInvariant(),
            new DomainErrorType.NotUpperCase(),
            "Value must be uppercase");

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Must_ReturnsFailure_WhenPredicateFails()
    {
        // Arrange
        var value = "invalid";

        // Act
        var actual = ValidationRules.For(TestContext).Must(
            value,
            v => v == v.ToUpperInvariant(),
            new DomainErrorType.NotUpperCase(),
            "Value must be uppercase");

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<string>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.NotUpperCase");
            });
    }

    #endregion

    #region DateTime Tests

    [Fact]
    public void NotDefault_ReturnsSuccess_WhenDateIsNotDefault()
    {
        // Arrange
        var value = DateTime.Now;

        // Act
        var actual = ValidationRules.For(TestContext).NotDefault(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void NotDefault_ReturnsFailure_WhenDateIsDefault()
    {
        // Arrange
        var value = DateTime.MinValue;

        // Act
        var actual = ValidationRules.For(TestContext).NotDefault(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<DateTime>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.DefaultDate");
            });
    }

    [Fact]
    public void InPast_ReturnsSuccess_WhenDateIsInPast()
    {
        // Arrange
        var value = DateTime.Now.AddDays(-1);

        // Act
        var actual = ValidationRules.For(TestContext).InPast(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void InPast_ReturnsFailure_WhenDateIsNotInPast()
    {
        // Arrange
        var value = DateTime.Now.AddDays(1);

        // Act
        var actual = ValidationRules.For(TestContext).InPast(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<DateTime>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.NotInPast");
            });
    }

    [Fact]
    public void InFuture_ReturnsSuccess_WhenDateIsInFuture()
    {
        // Arrange
        var value = DateTime.Now.AddDays(1);

        // Act
        var actual = ValidationRules.For(TestContext).InFuture(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void InFuture_ReturnsFailure_WhenDateIsNotInFuture()
    {
        // Arrange
        var value = DateTime.Now.AddDays(-1);

        // Act
        var actual = ValidationRules.For(TestContext).InFuture(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<DateTime>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.NotInFuture");
            });
    }

    [Fact]
    public void Before_ReturnsSuccess_WhenDateIsBefore()
    {
        // Arrange
        var value = new DateTime(2020, 1, 1);
        var boundary = new DateTime(2021, 1, 1);

        // Act
        var actual = ValidationRules.For(TestContext).Before(value, boundary);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Before_ReturnsFailure_WhenDateIsNotBefore()
    {
        // Arrange
        var value = new DateTime(2022, 1, 1);
        var boundary = new DateTime(2021, 1, 1);

        // Act
        var actual = ValidationRules.For(TestContext).Before(value, boundary);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<DateTime>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.TooLate");
            });
    }

    [Fact]
    public void After_ReturnsSuccess_WhenDateIsAfter()
    {
        // Arrange
        var value = new DateTime(2022, 1, 1);
        var boundary = new DateTime(2021, 1, 1);

        // Act
        var actual = ValidationRules.For(TestContext).After(value, boundary);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void After_ReturnsFailure_WhenDateIsNotAfter()
    {
        // Arrange
        var value = new DateTime(2020, 1, 1);
        var boundary = new DateTime(2021, 1, 1);

        // Act
        var actual = ValidationRules.For(TestContext).After(value, boundary);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<DateTime>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.TooEarly");
            });
    }

    [Fact]
    public void DateBetween_ReturnsSuccess_WhenDateIsInRange()
    {
        // Arrange
        var value = new DateTime(2021, 6, 1);
        var min = new DateTime(2021, 1, 1);
        var max = new DateTime(2021, 12, 31);

        // Act
        var actual = ValidationRules.For(TestContext).DateBetween(value, min, max);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void DateBetween_ReturnsFailure_WhenDateIsOutOfRange()
    {
        // Arrange
        var value = new DateTime(2022, 6, 1);
        var min = new DateTime(2021, 1, 1);
        var max = new DateTime(2021, 12, 31);

        // Act
        var actual = ValidationRules.For(TestContext).DateBetween(value, min, max);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<DateTime>)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.OutOfRange");
            });
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Chaining_PreservesContextName_ThroughMultipleValidations()
    {
        // Arrange
        var value = "valid";

        // Act
        var actual = ValidationRules.For(TestContext)
            .NotEmpty(value)
            .ThenMinLength(3)
            .ThenMaxLength(10);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.ContextName.ShouldBe(TestContext);
    }

    [Fact]
    public void Chaining_ReturnsFirstFailure_WhenFirstValidationFails()
    {
        // Arrange
        var value = "";

        // Act
        var actual = ValidationRules.For(TestContext)
            .NotEmpty(value)
            .ThenMinLength(3);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.Empty");
            });
    }

    [Fact]
    public void Chaining_ReturnsSecondFailure_WhenSecondValidationFails()
    {
        // Arrange
        var value = "ab";

        // Act
        var actual = ValidationRules.For(TestContext)
            .NotEmpty(value)
            .ThenMinLength(3);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError)errors.Head;
                error.ErrorCode.ShouldBe($"Domain.{TestContext}.TooShort");
            });
    }

    [Fact]
    public void NumericChaining_Works_Correctly()
    {
        // Arrange
        var value = 50m;

        // Act
        var actual = ValidationRules.For(TestContext)
            .Positive(value)
            .ThenAtMost(100m)
            .ThenAtLeast(10m);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.ContextName.ShouldBe(TestContext);
    }

    [Fact]
    public void ThenNormalize_TransformsValue_Correctly()
    {
        // Arrange
        var value = "  valid  ";

        // Act
        var actual = ValidationRules.For(TestContext)
            .NotEmpty(value)
            .ThenNormalize(s => s.Trim());

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe("valid"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    #endregion

    #region Context Class Pattern Tests

    [Fact]
    public void ContextClass_WorksWithValidationRulesT_SameAsValueObject()
    {
        // Arrange
        var value = 100m;

        // Act
        var actual = ValidationRules<ProductValidation>.Positive(value);

        // Assert
        actual.Value.IsSuccess.ShouldBeTrue();
        actual.Value.Match(
            Succ: v => v.ShouldBe(value),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ContextClass_GeneratesCorrectErrorCode()
    {
        // Arrange
        var value = -1m;

        // Act
        var actual = ValidationRules<ProductValidation>.Positive(value);

        // Assert
        actual.Value.IsFail.ShouldBeTrue();
        actual.Value.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ExpectedError<decimal>)errors.Head;
                error.ErrorCode.ShouldBe("Domain.ProductValidation.NotPositive");
            });
    }

    #endregion

    #region Apply Pattern Tests

    [Fact]
    public void Apply_CombinesTwoContextualValidations()
    {
        // Arrange
        var ctx = ValidationRules.For("Order");

        // Act
        var actual = (
            ctx.NotEmpty("ProductA"),
            ctx.Positive(100m)
        ).Apply((name, amount) => $"{name}: {amount}");

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v => v.ShouldBe("ProductA: 100"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Apply_CollectsAllErrors_WhenMultipleValidationsFail()
    {
        // Arrange
        var ctx = ValidationRules.For("Order");

        // Act
        var actual = (
            ctx.NotEmpty(""),
            ctx.Positive(-1m)
        ).Apply((name, amount) => $"{name}: {amount}");

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                errors.Count.ShouldBe(2);
            });
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToValidation_Works()
    {
        // Arrange
        var contextual = ValidationRules.For(TestContext).NotEmpty("valid");

        // Act
        LanguageExt.Validation<LanguageExt.Common.Error, string> validation = contextual;

        // Assert
        validation.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
