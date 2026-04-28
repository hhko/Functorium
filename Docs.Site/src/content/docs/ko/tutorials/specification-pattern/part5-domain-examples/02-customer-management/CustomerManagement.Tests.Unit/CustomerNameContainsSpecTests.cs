using CustomerManagement.Domain;
using CustomerManagement.Domain.Specifications;
using CustomerManagement.Domain.ValueObjects;

namespace CustomerManagement.Tests.Unit;

public class CustomerNameContainsSpecTests
{
    private static readonly Customer _김철수 = new(
        CustomerId.New(), new CustomerName("김철수"), new Email("chulsoo@example.com"), IsActive: true);

    private static readonly Customer _이영희 = new(
        CustomerId.New(), new CustomerName("이영희"), new Email("younghee@example.com"), IsActive: true);

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenNameContainsSearchTerm()
    {
        // Arrange
        var spec = new CustomerNameContainsSpec(new CustomerName("철수"));

        // Act & Assert
        spec.IsSatisfiedBy(_김철수).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenNameDoesNotContainSearchTerm()
    {
        // Arrange
        var spec = new CustomerNameContainsSpec(new CustomerName("철수"));

        // Act & Assert
        spec.IsSatisfiedBy(_이영희).ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeCaseInsensitive()
    {
        // Arrange
        var customer = new Customer(
            CustomerId.New(), new CustomerName("John Smith"), new Email("john@example.com"), IsActive: true);
        var spec = new CustomerNameContainsSpec(new CustomerName("john"));

        // Act & Assert
        spec.IsSatisfiedBy(customer).ShouldBeTrue();
    }
}
