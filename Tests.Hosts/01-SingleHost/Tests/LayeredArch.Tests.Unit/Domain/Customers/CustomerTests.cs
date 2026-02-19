using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Arrange
        var name = CustomerName.Create("John").ThrowIfFail();
        var email = Email.Create("john@example.com").ThrowIfFail();
        var creditLimit = Money.Create(5000m).ThrowIfFail();

        // Act
        var sut = Customer.Create(name, email, creditLimit);

        // Assert
        sut.Id.ShouldNotBe(default);
        sut.DomainEvents.ShouldContain(e => e is Customer.CreatedEvent);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var name = CustomerName.Create("John").ThrowIfFail();
        var email = Email.Create("john@example.com").ThrowIfFail();
        var creditLimit = Money.Create(5000m).ThrowIfFail();

        // Act
        var sut = Customer.Create(name, email, creditLimit);

        // Assert
        ((string)sut.Name).ShouldBe("John");
        ((string)sut.Email).ShouldBe("john@example.com");
        ((decimal)sut.CreditLimit).ShouldBe(5000m);
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = CustomerId.New();
        var name = CustomerName.Create("John").ThrowIfFail();
        var email = Email.Create("john@example.com").ThrowIfFail();
        var creditLimit = Money.Create(5000m).ThrowIfFail();
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = Customer.CreateFromValidated(id, name, email, creditLimit, createdAt, updatedAt);

        // Assert
        sut.Id.ShouldBe(id);
        ((string)sut.Name).ShouldBe("John");
        ((string)sut.Email).ShouldBe("john@example.com");
        sut.CreatedAt.ShouldBe(createdAt);
        sut.UpdatedAt.ShouldBe(Some(updatedAt));
        sut.DomainEvents.ShouldBeEmpty();
    }
}
