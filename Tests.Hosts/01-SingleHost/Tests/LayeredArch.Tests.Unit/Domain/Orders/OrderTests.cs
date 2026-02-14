using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Orders;

public class OrderTests
{
    [Fact]
    public void Create_ShouldCalculateTotalAmount()
    {
        // Arrange
        var productId = ProductId.New();
        var quantity = Quantity.Create(3).ThrowIfFail();
        var unitPrice = Money.Create(100m).ThrowIfFail();
        var address = ShippingAddress.Create("Seoul, Korea").ThrowIfFail();

        // Act
        var sut = Order.Create(productId, quantity, unitPrice, address);

        // Assert
        ((decimal)sut.TotalAmount).ShouldBe(300m);
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Arrange
        var productId = ProductId.New();
        var quantity = Quantity.Create(2).ThrowIfFail();
        var unitPrice = Money.Create(50m).ThrowIfFail();
        var address = ShippingAddress.Create("Seoul, Korea").ThrowIfFail();

        // Act
        var sut = Order.Create(productId, quantity, unitPrice, address);

        // Assert
        sut.Id.ShouldNotBe(default);
        sut.DomainEvents.ShouldContain(e => e is Order.CreatedEvent);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var productId = ProductId.New();
        var quantity = Quantity.Create(2).ThrowIfFail();
        var unitPrice = Money.Create(150m).ThrowIfFail();
        var address = ShippingAddress.Create("Busan, Korea").ThrowIfFail();

        // Act
        var sut = Order.Create(productId, quantity, unitPrice, address);

        // Assert
        sut.ProductId.ShouldBe(productId);
        ((int)sut.Quantity).ShouldBe(2);
        ((decimal)sut.UnitPrice).ShouldBe(150m);
        ((string)sut.ShippingAddress).ShouldBe("Busan, Korea");
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = OrderId.New();
        var productId = ProductId.New();
        var quantity = Quantity.Create(3).ThrowIfFail();
        var unitPrice = Money.Create(100m).ThrowIfFail();
        var totalAmount = Money.Create(300m).ThrowIfFail();
        var address = ShippingAddress.Create("Seoul, Korea").ThrowIfFail();
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = Order.CreateFromValidated(id, productId, quantity, unitPrice, totalAmount, address, createdAt, updatedAt);

        // Assert
        sut.Id.ShouldBe(id);
        sut.ProductId.ShouldBe(productId);
        ((decimal)sut.TotalAmount).ShouldBe(300m);
        sut.CreatedAt.ShouldBe(createdAt);
        sut.UpdatedAt.ShouldBe(updatedAt);
        sut.DomainEvents.ShouldBeEmpty();
    }
}
