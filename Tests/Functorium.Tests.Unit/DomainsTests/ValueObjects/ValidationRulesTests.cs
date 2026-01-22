using System.Text.RegularExpressions;
using Functorium.Domains.ValueObjects;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

// 테스트용 더미 값 객체 (ValidationRules 테스트용)
public sealed class SampleValueObject : SimpleValueObject<string>
{
    private SampleValueObject(string value) : base(value) { }
}

public sealed class NumericValueObject : ComparableSimpleValueObject<decimal>
{
    private NumericValueObject(decimal value) : base(value) { }
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class ValidationRulesTests
{
    #region NotEmpty Tests

    [Fact]
    public void NotEmpty_ReturnsSuccess_WhenValueIsNotEmpty()
    {
        // Arrange
        var value = "valid";

        // Act
        var actual = ValidationRules.NotEmpty<SampleValueObject>(value);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Match(
            Succ: v => v.ShouldBe(value),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NotEmpty_ReturnsFailure_WhenValueIsEmpty(string? value)
    {
        // Arrange
        var testValue = value ?? "";

        // Act
        var actual = ValidationRules.NotEmpty<SampleValueObject>(testValue);

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

    #endregion

    #region MinLength Tests

    [Fact]
    public void MinLength_ReturnsSuccess_WhenLengthMeetsMinimum()
    {
        // Arrange
        var value = "12345";

        // Act
        var actual = ValidationRules.MinLength<SampleValueObject>(value, 5);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MinLength_ReturnsFailure_WhenLengthBelowMinimum()
    {
        // Arrange
        var value = "1234";

        // Act
        var actual = ValidationRules.MinLength<SampleValueObject>(value, 5);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.SampleValueObject.TooShort");
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
        var actual = ValidationRules.MaxLength<SampleValueObject>(value, 5);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MaxLength_ReturnsFailure_WhenLengthExceedsMaximum()
    {
        // Arrange
        var value = "123456";

        // Act
        var actual = ValidationRules.MaxLength<SampleValueObject>(value, 5);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.SampleValueObject.TooLong");
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
        var actual = ValidationRules.ExactLength<SampleValueObject>(value, 3);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("AB", 3)]
    [InlineData("ABCD", 3)]
    public void ExactLength_ReturnsFailure_WhenLengthDoesNotMatch(string value, int expected)
    {
        // Act
        var actual = ValidationRules.ExactLength<SampleValueObject>(value, expected);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.SampleValueObject.WrongLength");
            });
    }

    #endregion

    #region Matches Tests

    private static readonly Regex EmailPattern = new(@"^[\w.-]+@[\w.-]+\.\w+$", RegexOptions.Compiled);

    [Fact]
    public void Matches_ReturnsSuccess_WhenPatternMatches()
    {
        // Arrange
        var value = "test@example.com";

        // Act
        var actual = ValidationRules.Matches<SampleValueObject>(value, EmailPattern);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Matches_ReturnsFailure_WhenPatternDoesNotMatch()
    {
        // Arrange
        var value = "invalid-email";

        // Act
        var actual = ValidationRules.Matches<SampleValueObject>(value, EmailPattern);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.SampleValueObject.InvalidFormat");
            });
    }

    [Fact]
    public void Matches_UsesCustomMessage_WhenProvided()
    {
        // Arrange
        var value = "invalid";
        var customMessage = "Custom error message";

        // Act
        var actual = ValidationRules.Matches<SampleValueObject>(value, EmailPattern, customMessage);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors => errors.Head.Message.ShouldBe(customMessage));
    }

    #endregion

    #region NonNegative Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void NonNegative_ReturnsSuccess_WhenValueIsNonNegative(decimal value)
    {
        // Act
        var actual = ValidationRules.NonNegative<NumericValueObject, decimal>(value);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void NonNegative_ReturnsFailure_WhenValueIsNegative(decimal value)
    {
        // Act
        var actual = ValidationRules.NonNegative<NumericValueObject, decimal>(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected<decimal>)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.NumericValueObject.Negative");
            });
    }

    #endregion

    #region Positive Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Positive_ReturnsSuccess_WhenValueIsPositive(decimal value)
    {
        // Act
        var actual = ValidationRules.Positive<NumericValueObject, decimal>(value);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positive_ReturnsFailure_WhenValueIsNotPositive(decimal value)
    {
        // Act
        var actual = ValidationRules.Positive<NumericValueObject, decimal>(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected<decimal>)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.NumericValueObject.NotPositive");
            });
    }

    #endregion

    #region Between Tests

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void Between_ReturnsSuccess_WhenValueInRange(decimal value, decimal min, decimal max)
    {
        // Act
        var actual = ValidationRules.Between<NumericValueObject, decimal>(value, min, max);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(11, 1, 10)]
    public void Between_ReturnsFailure_WhenValueOutOfRange(decimal value, decimal min, decimal max)
    {
        // Act
        var actual = ValidationRules.Between<NumericValueObject, decimal>(value, min, max);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected<decimal>)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.NumericValueObject.OutOfRange");
            });
    }

    #endregion

    #region AtMost Tests

    [Theory]
    [InlineData(50, 100)]
    [InlineData(100, 100)]
    public void AtMost_ReturnsSuccess_WhenValueWithinMaximum(decimal value, decimal max)
    {
        // Act
        var actual = ValidationRules.AtMost<NumericValueObject, decimal>(value, max);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AtMost_ReturnsFailure_WhenValueExceedsMaximum()
    {
        // Arrange
        var value = 101m;
        var max = 100m;

        // Act
        var actual = ValidationRules.AtMost<NumericValueObject, decimal>(value, max);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected<decimal>)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.NumericValueObject.AboveMaximum");
            });
    }

    #endregion

    #region AtLeast Tests

    [Theory]
    [InlineData(50, 10)]
    [InlineData(10, 10)]
    public void AtLeast_ReturnsSuccess_WhenValueMeetsMinimum(decimal value, decimal min)
    {
        // Act
        var actual = ValidationRules.AtLeast<NumericValueObject, decimal>(value, min);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AtLeast_ReturnsFailure_WhenValueBelowMinimum()
    {
        // Arrange
        var value = 5m;
        var min = 10m;

        // Act
        var actual = ValidationRules.AtLeast<NumericValueObject, decimal>(value, min);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected<decimal>)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.NumericValueObject.BelowMinimum");
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
        var actual = ValidationRules.Must<SampleValueObject, string>(
            value,
            v => v == v.ToUpperInvariant(),
            new DomainErrorType.NotUpperCase(),
            "Value must be uppercase");

        // Assert
        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Must_ReturnsFailure_WhenPredicateFails()
    {
        // Arrange
        var value = "invalid";

        // Act
        var actual = ValidationRules.Must<SampleValueObject, string>(
            value,
            v => v == v.ToUpperInvariant(),
            new DomainErrorType.NotUpperCase(),
            "Value must be uppercase");

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: errors =>
            {
                var error = (ErrorCodeExpected<string>)errors.Head;
                error.ErrorCode.ShouldBe("DomainErrors.SampleValueObject.NotUpperCase");
            });
    }

    #endregion
}
