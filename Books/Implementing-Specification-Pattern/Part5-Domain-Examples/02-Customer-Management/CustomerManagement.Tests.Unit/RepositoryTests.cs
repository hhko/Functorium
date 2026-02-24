using CustomerManagement.Domain.Specifications;
using CustomerManagement.Domain.ValueObjects;
using CustomerManagement.Infrastructure;

namespace CustomerManagement.Tests.Unit;

public class RepositoryTests
{
    private readonly InMemoryCustomerRepository _repository = new(SampleCustomers.All);

    [Fact]
    public void FindAll_ShouldReturnActiveCustomers()
    {
        // Arrange
        var spec = new ActiveCustomerSpec();

        // Act
        var results = _repository.FindAll(spec).ToList();

        // Assert
        results.Count.ShouldBe(4);
        results.ShouldAllBe(c => c.IsActive);
    }

    [Fact]
    public void Exists_ShouldReturnTrue_WhenEmailExists()
    {
        // Arrange
        var spec = new CustomerEmailSpec(new Email("chulsoo@example.com"));

        // Act & Assert
        _repository.Exists(spec).ShouldBeTrue();
    }

    [Fact]
    public void Exists_ShouldReturnFalse_WhenEmailDoesNotExist()
    {
        // Arrange
        var spec = new CustomerEmailSpec(new Email("nonexistent@example.com"));

        // Act & Assert
        _repository.Exists(spec).ShouldBeFalse();
    }

    [Fact]
    public void FindAll_ShouldReturnMatchingCustomers_WhenNameContainsUsed()
    {
        // Arrange
        var spec = new CustomerNameContainsSpec(new CustomerName("민"));

        // Act
        var results = _repository.FindAll(spec).ToList();

        // Assert
        results.Count.ShouldBe(2); // 박지민, 정민호
    }

    [Fact]
    public void FindAll_ShouldReturnFilteredCustomers_WhenCompositeSpecUsed()
    {
        // Arrange: 활성 AND 이름에 '수' 포함
        var spec = new ActiveCustomerSpec()
            & new CustomerNameContainsSpec(new CustomerName("수"));

        // Act
        var results = _repository.FindAll(spec).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(c => c.IsActive);
    }
}
