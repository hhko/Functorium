using AggregateRoot;

namespace AggregateRoot.Tests.Unit;

public sealed class OrderTests
{
    [Fact]
    public void Create_SetsStatusToPending()
    {
        // Arrange & Act
        var order = Order.Create("홍길동", 100_000m);

        // Assert
        order.Status.ShouldBe(OrderStatus.Pending);
        order.CustomerName.ShouldBe("홍길동");
        order.TotalAmount.ShouldBe(100_000m);
    }

    [Fact]
    public void Confirm_ReturnsSucc_WhenStatusIsPending()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);

        // Act
        var actual = order.Confirm();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_ReturnsFail_WhenStatusIsNotPending()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();

        // Act
        var actual = order.Confirm();

        // Assert
        actual.IsFail.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Ship_ReturnsSucc_WhenStatusIsConfirmed()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();

        // Act
        var actual = order.Ship();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_ReturnsFail_WhenStatusIsNotConfirmed()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);

        // Act
        var actual = order.Ship();

        // Assert
        actual.IsFail.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public void Deliver_ReturnsSucc_WhenStatusIsShipped()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();
        order.Ship();

        // Act
        var actual = order.Deliver();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Delivered);
    }

    [Fact]
    public void Deliver_ReturnsFail_WhenStatusIsNotShipped()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();

        // Act
        var actual = order.Deliver();

        // Assert
        actual.IsFail.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Cancel_ReturnsSucc_WhenStatusIsPending()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);

        // Act
        var actual = order.Cancel();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ReturnsSucc_WhenStatusIsConfirmed()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();

        // Act
        var actual = order.Cancel();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ReturnsFail_WhenStatusIsDelivered()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();
        order.Ship();
        order.Deliver();

        // Act
        var actual = order.Cancel();

        // Assert
        actual.IsFail.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Delivered);
    }

    [Fact]
    public void Cancel_ReturnsFail_WhenAlreadyCancelled()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Cancel();

        // Act
        var actual = order.Cancel();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
