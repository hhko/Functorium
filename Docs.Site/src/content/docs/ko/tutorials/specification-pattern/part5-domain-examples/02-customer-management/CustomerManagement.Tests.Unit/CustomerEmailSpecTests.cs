using CustomerManagement.Domain;
using CustomerManagement.Domain.Specifications;
using CustomerManagement.Domain.ValueObjects;

namespace CustomerManagement.Tests.Unit;

public class CustomerEmailSpecTests
{
    private static readonly Customer _김철수 = new(
        CustomerId.New(), new CustomerName("김철수"), new Email("chulsoo@example.com"), IsActive: true);

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenEmailMatches()
    {
        // Arrange
        var spec = new CustomerEmailSpec(new Email("chulsoo@example.com"));

        // Act & Assert
        spec.IsSatisfiedBy(_김철수).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenEmailDoesNotMatch()
    {
        // Arrange
        var spec = new CustomerEmailSpec(new Email("unknown@example.com"));

        // Act & Assert
        spec.IsSatisfiedBy(_김철수).ShouldBeFalse();
    }
}
