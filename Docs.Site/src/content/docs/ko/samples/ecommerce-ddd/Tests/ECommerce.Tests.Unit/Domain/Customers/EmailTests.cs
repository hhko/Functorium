using ECommerce.Domain.AggregateRoots.Customers.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Customers;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.kr")]
    public void Create_ShouldSucceed_WhenValueIsValidEmail(string value)
    {
        // Act
        var actual = Email.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldNormalizeToLowerCase()
    {
        // Act
        var actual = Email.Create("Test@Example.COM").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = Email.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', Email.MaxLength) + "@example.com";

        // Act
        var actual = Email.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("no-at-sign")]
    [InlineData("@no-local-part.com")]
    [InlineData("no-domain@")]
    public void Create_ShouldFail_WhenFormatIsInvalid(string value)
    {
        // Act
        var actual = Email.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
