using ContextualValidation.Examples;
using LanguageExt;
using Shouldly;
using Xunit;

namespace ContextualValidation.Tests.Unit;

public sealed class PhoneNumberValidationTests
{
    [Fact]
    public void Validate_ReturnsSuccess_WhenValidPhoneNumber()
    {
        var result = PhoneNumberValidation.Validate("010-1234-5678");
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsFail_WhenEmpty()
    {
        var result = PhoneNumberValidation.Validate("");
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsFail_WhenNull()
    {
        var result = PhoneNumberValidation.Validate(null);
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ErrorContainsFieldName_WhenFail()
    {
        var result = PhoneNumberValidation.Validate(null);
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected fail"),
            Fail: errors => errors.Head.ToString().ShouldContain("PhoneNumber"));
    }
}

public sealed class AddressValidationTests
{
    [Fact]
    public void Validate_ReturnsSuccess_WhenAllFieldsValid()
    {
        var result = AddressValidation.Validate("서울", "강남대로 123", "06000");
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CollectsAllErrors_WhenMultipleFieldsInvalid()
    {
        var result = AddressValidation.Validate(null, "", "1");
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected fail"),
            Fail: errors => errors.Count.ShouldBeGreaterThanOrEqualTo(2));
    }
}
