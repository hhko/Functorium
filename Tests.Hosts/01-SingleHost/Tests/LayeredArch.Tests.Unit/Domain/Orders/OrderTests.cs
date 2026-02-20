using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Orders;

public class OrderTests
{
    private static Order CreateSampleOrder()
    {
        var line = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail()).ThrowIfFail();
        return Order.Create(
            CustomerId.New(),
            [line],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();
    }

    [Fact]
    public void Create_ShouldCalculateTotalAmount()
    {
        // Arrange
        var line1 = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(3).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail()).ThrowIfFail();
        var line2 = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(50m).ThrowIfFail()).ThrowIfFail();

        // Act
        var sut = Order.Create(
            CustomerId.New(),
            [line1, line2],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

        // Assert
        ((decimal)sut.TotalAmount).ShouldBe(400m); // 300 + 100
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Arrange
        var customerId = CustomerId.New();
        var line = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(50m).ThrowIfFail()).ThrowIfFail();

        // Act
        var sut = Order.Create(customerId, [line],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

        // Assert
        sut.Id.ShouldNotBe(default);
        var createdEvent = sut.DomainEvents.OfType<Order.CreatedEvent>().ShouldHaveSingleItem();
        createdEvent.CustomerId.ShouldBe(customerId);
        createdEvent.OrderLines.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var customerId = CustomerId.New();
        var line = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(150m).ThrowIfFail()).ThrowIfFail();

        // Act
        var sut = Order.Create(customerId, [line],
            ShippingAddress.Create("Busan, Korea").ThrowIfFail()).ThrowIfFail();

        // Assert
        sut.CustomerId.ShouldBe(customerId);
        sut.OrderLines.Count.ShouldBe(1);
        ((string)sut.ShippingAddress).ShouldBe("Busan, Korea");
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var sut = CreateSampleOrder();

        // Assert
        sut.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public void Create_ShouldFail_WhenOrderLinesEmpty()
    {
        // Act
        var actual = Order.Create(
            CustomerId.New(),
            [],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail());

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCalculateTotalAmount_WithMultipleLines()
    {
        // Arrange
        var line1 = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail()).ThrowIfFail();
        var line2 = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(3).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail()).ThrowIfFail();

        // Act
        var sut = Order.Create(
            CustomerId.New(),
            [line1, line2],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

        // Assert
        ((decimal)sut.TotalAmount).ShouldBe(800m); // 200 + 600
        sut.OrderLines.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = OrderId.New();
        var customerId = CustomerId.New();
        var line = OrderLine.CreateFromValidated(
            OrderLineId.New(),
            ProductId.New(),
            Quantity.CreateFromValidated(3),
            Money.CreateFromValidated(100m),
            Money.CreateFromValidated(300m));
        var totalAmount = Money.Create(300m).ThrowIfFail();
        var address = ShippingAddress.Create("Seoul, Korea").ThrowIfFail();
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = Order.CreateFromValidated(id, customerId, [line], totalAmount, address, OrderStatus.Confirmed, createdAt, updatedAt);

        // Assert
        sut.Id.ShouldBe(id);
        sut.CustomerId.ShouldBe(customerId);
        sut.OrderLines.Count.ShouldBe(1);
        ((decimal)sut.TotalAmount).ShouldBe(300m);
        sut.Status.ShouldBe(OrderStatus.Confirmed);
        sut.CreatedAt.ShouldBe(createdAt);
        sut.UpdatedAt.ShouldBe(Some(updatedAt));
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Confirm_ShouldTransitionFromPending()
    {
        // Arrange
        var sut = CreateSampleOrder();

        // Act
        var actual = sut.Confirm();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_ShouldPublishConfirmedEvent()
    {
        // Arrange
        var sut = CreateSampleOrder();
        sut.ClearDomainEvents();

        // Act
        sut.Confirm();

        // Assert
        sut.DomainEvents.OfType<Order.ConfirmedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Ship_ShouldTransitionFromConfirmed()
    {
        // Arrange
        var sut = CreateSampleOrder();
        sut.Confirm();

        // Act
        var actual = sut.Ship();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public void Cancel_ShouldTransitionFromPending()
    {
        // Arrange
        var sut = CreateSampleOrder();

        // Act
        var actual = sut.Cancel();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldTransitionFromConfirmed()
    {
        // Arrange
        var sut = CreateSampleOrder();
        sut.Confirm();

        // Act
        var actual = sut.Cancel();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Ship_ShouldFail_WhenPending()
    {
        // Arrange
        var sut = CreateSampleOrder();

        // Act
        var actual = sut.Ship();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Deliver_ShouldFail_WhenCancelled()
    {
        // Arrange
        var sut = CreateSampleOrder();
        sut.Cancel();

        // Act
        var actual = sut.Deliver();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Cancel_ShouldFail_WhenDelivered()
    {
        // Arrange
        var sut = CreateSampleOrder();
        sut.Confirm();
        sut.Ship();
        sut.Deliver();

        // Act
        var actual = sut.Cancel();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
