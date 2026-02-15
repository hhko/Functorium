using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.Specifications;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Customers;

public class CustomerEmailSpecTests
{
    private static Customer CreateSampleCustomer(
        string name = "John",
        string email = "john@example.com",
        decimal creditLimit = 5000m)
    {
        return Customer.Create(
            CustomerName.Create(name).ThrowIfFail(),
            Email.Create(email).ThrowIfFail(),
            Money.Create(creditLimit).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenEmailMatches()
    {
        // Arrange
        var customer = CreateSampleCustomer(email: "john@example.com");
        var sut = new CustomerEmailSpec(Email.Create("john@example.com").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(customer);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenEmailDoesNotMatch()
    {
        // Arrange
        var customer = CreateSampleCustomer(email: "john@example.com");
        var sut = new CustomerEmailSpec(Email.Create("jane@example.com").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(customer);

        // Assert
        actual.ShouldBeFalse();
    }
}
