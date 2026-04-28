using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Customers.ValueObjects;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Customers;

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

    [Fact]
    public void UpdateCreditLimit_ShouldUpdateValue()
    {
        // Arrange
        var sut = Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());
        var newLimit = Money.Create(10000m).ThrowIfFail();

        // Act
        sut.UpdateCreditLimit(newLimit);

        // Assert
        ((decimal)sut.CreditLimit).ShouldBe(10000m);
    }

    [Fact]
    public void UpdateCreditLimit_ShouldPublishEvent()
    {
        // Arrange
        var sut = Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());
        sut.ClearDomainEvents();
        var newLimit = Money.Create(10000m).ThrowIfFail();

        // Act
        sut.UpdateCreditLimit(newLimit);

        // Assert
        var evt = sut.DomainEvents.OfType<Customer.CreditLimitUpdatedEvent>().ShouldHaveSingleItem();
        ((decimal)evt.OldCreditLimit).ShouldBe(5000m);
        ((decimal)evt.NewCreditLimit).ShouldBe(10000m);
    }

    [Fact]
    public void UpdateCreditLimit_ShouldSetUpdatedAt()
    {
        // Arrange
        var sut = Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());

        // Act
        sut.UpdateCreditLimit(Money.Create(10000m).ThrowIfFail());

        // Assert
        sut.UpdatedAt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void ChangeEmail_ShouldUpdateValue()
    {
        // Arrange
        var sut = Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());
        var newEmail = Email.Create("newemail@example.com").ThrowIfFail();

        // Act
        sut.ChangeEmail(newEmail);

        // Assert
        ((string)sut.Email).ShouldBe("newemail@example.com");
    }

    [Fact]
    public void ChangeEmail_ShouldPublishEvent()
    {
        // Arrange
        var sut = Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());
        sut.ClearDomainEvents();
        var newEmail = Email.Create("newemail@example.com").ThrowIfFail();

        // Act
        sut.ChangeEmail(newEmail);

        // Assert
        var evt = sut.DomainEvents.OfType<Customer.EmailChangedEvent>().ShouldHaveSingleItem();
        ((string)evt.OldEmail).ShouldBe("john@example.com");
        ((string)evt.NewEmail).ShouldBe("newemail@example.com");
    }

    [Fact]
    public void ChangeEmail_ShouldSetUpdatedAt()
    {
        // Arrange
        var sut = Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(5000m).ThrowIfFail());

        // Act
        sut.ChangeEmail(Email.Create("newemail@example.com").ThrowIfFail());

        // Assert
        sut.UpdatedAt.IsSome.ShouldBeTrue();
    }
}
