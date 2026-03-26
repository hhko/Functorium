using LayeredArch.Adapters.Persistence.Repositories.Customers;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Persistence.Mappers;

public class CustomerMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        // Arrange
        var customer = Customer.Create(
            CustomerName.Create("Hong Gildong").ThrowIfFail(),
            Email.Create("hong@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());

        // Act
        var actual = customer.ToModel().ToDomain();

        // Assert
        actual.Id.ToString().ShouldBe(customer.Id.ToString());
        ((string)actual.Name).ShouldBe(customer.Name);
        ((string)actual.Email).ShouldBe(customer.Email);
        ((decimal)actual.CreditLimit).ShouldBe(customer.CreditLimit);
        actual.CreatedAt.ShouldBe(customer.CreatedAt);
        actual.UpdatedAt.ShouldBe(customer.UpdatedAt);
    }
}
