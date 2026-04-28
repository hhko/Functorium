using EcommerceOrderManagement;

namespace EcommerceOrderManagement.Tests.Unit;

public sealed class OrderTests
{
    private static List<OrderLine> CreateLines() =>
    [
        OrderLine.Create("노트북", 1, 1_500_000m),
        OrderLine.Create("마우스", 2, 35_000m),
    ];

    // ─── Create ─────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_ReturnsSucc()
    {
        // Arrange & Act
        var result = Order.Create("홍길동", CreateLines());

        // Assert
        result.IsSucc.ShouldBeTrue();
        var order = result.ThrowIfFail();
        order.CustomerName.ShouldBe("홍길동");
        order.TotalAmount.ShouldBe(1_570_000m);
        order.Status.ShouldBe(OrderStatus.Pending);
        order.OrderLines.Count.ShouldBe(2);
    }

    [Fact]
    public void Create_EmptyCustomerName_ReturnsFail()
    {
        var result = Order.Create("", CreateLines());
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_EmptyLines_ReturnsFail()
    {
        var result = Order.Create("홍길동", []);
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_RaisesDomainEvent()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.DomainEvents.Count.ShouldBe(1);
        order.DomainEvents[0].ShouldBeOfType<Order.OrderCreatedEvent>();
    }

    // ─── Confirm ────────────────────────────────────────

    [Fact]
    public void Confirm_Pending_ReturnsSucc()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();

        var result = order.Confirm();

        result.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_NotPending_ReturnsFail()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();

        var result = order.Confirm();

        result.IsFail.ShouldBeTrue();
    }

    // ─── Ship ───────────────────────────────────────────

    [Fact]
    public void Ship_Confirmed_ReturnsSucc()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();

        var result = order.Ship();

        result.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_NotConfirmed_ReturnsFail()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();

        var result = order.Ship();

        result.IsFail.ShouldBeTrue();
    }

    // ─── Deliver ────────────────────────────────────────

    [Fact]
    public void Deliver_Shipped_ReturnsSucc()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();
        order.Ship();

        var result = order.Deliver();

        result.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Delivered);
    }

    [Fact]
    public void Deliver_NotShipped_ReturnsFail()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();

        var result = order.Deliver();

        result.IsFail.ShouldBeTrue();
    }

    // ─── Cancel ─────────────────────────────────────────

    [Fact]
    public void Cancel_Pending_ReturnsSucc()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();

        var result = order.Cancel();

        result.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Confirmed_ReturnsSucc()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();

        var result = order.Cancel();

        result.IsSucc.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Delivered_ReturnsFail()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();
        order.Ship();
        order.Deliver();

        var result = order.Cancel();

        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ReturnsFail()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Cancel();

        var result = order.Cancel();

        result.IsFail.ShouldBeTrue();
    }

    // ─── Full Lifecycle Domain Events ───────────────────

    [Fact]
    public void FullLifecycle_CollectsAllDomainEvents()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();
        order.Ship();
        order.Deliver();

        order.DomainEvents.Count.ShouldBe(4);
        order.DomainEvents[0].ShouldBeOfType<Order.OrderCreatedEvent>();
        order.DomainEvents[1].ShouldBeOfType<Order.OrderConfirmedEvent>();
        order.DomainEvents[2].ShouldBeOfType<Order.OrderShippedEvent>();
        order.DomainEvents[3].ShouldBeOfType<Order.OrderDeliveredEvent>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var order = Order.Create("홍길동", CreateLines()).ThrowIfFail();
        order.Confirm();

        order.ClearDomainEvents();

        order.DomainEvents.Count.ShouldBe(0);
    }
}
