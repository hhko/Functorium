using CustomerManagement.Domain;
using CustomerManagement.Domain.Specifications;
using CustomerManagement.Domain.ValueObjects;

namespace CustomerManagement.Tests.Unit;

public class CustomerActiveSpecTests
{
    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenCustomerIsActive()
    {
        // Arrange
        var customer = new Customer(
            CustomerId.New(), new CustomerName("김철수"), new Email("test@example.com"), IsActive: true);
        var spec = new CustomerActiveSpec();

        // Act & Assert
        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenCustomerIsInactive()
    {
        // Arrange
        var customer = new Customer(
            CustomerId.New(), new CustomerName("박지민"), new Email("test@example.com"), IsActive: false);
        var spec = new CustomerActiveSpec();

        // Act & Assert
        spec.IsSatisfiedBy(customer).ShouldBeFalse();
    }
}
