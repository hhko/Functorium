using DomainEvents;
using Functorium.Domains.Events;

namespace DomainEvents.Tests.Unit;

public sealed class OrderDomainEventTests
{
    [Fact]
    public void Create_RaisesOrderCreatedEvent()
    {
        // Arrange & Act
        var order = Order.Create("홍길동", 100_000m);

        // Assert
        order.DomainEvents.Count.ShouldBe(1);
        var evt = order.DomainEvents[0].ShouldBeOfType<OrderCreatedEvent>();
        evt.OrderId.ShouldBe(order.Id);
        evt.CustomerName.ShouldBe("홍길동");
        evt.TotalAmount.ShouldBe(100_000m);
    }

    [Fact]
    public void Create_RaisesEventWithEventIdAndOccurredAt()
    {
        // Arrange & Act
        var order = Order.Create("홍길동", 100_000m);

        // Assert
        var evt = order.DomainEvents[0].ShouldBeOfType<OrderCreatedEvent>();
        evt.EventId.ShouldNotBe(Ulid.Empty);
        evt.OccurredAt.ShouldNotBe(default);
    }

    [Fact]
    public void Confirm_RaisesOrderConfirmedEvent()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);

        // Act
        order.Confirm();

        // Assert
        order.DomainEvents.Count.ShouldBe(2);
        var evt = order.DomainEvents[1].ShouldBeOfType<OrderConfirmedEvent>();
        evt.OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void Confirm_DoesNotRaiseEvent_WhenTransitionFails()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();
        var eventCountBeforeSecondConfirm = order.DomainEvents.Count;

        // Act
        order.Confirm();

        // Assert
        order.DomainEvents.Count.ShouldBe(eventCountBeforeSecondConfirm);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();
        order.DomainEvents.Count.ShouldBeGreaterThan(0);

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void DomainEvents_EachHaveUniqueEventId()
    {
        // Arrange
        var order = Order.Create("홍길동", 100_000m);
        order.Confirm();

        // Act
        var eventIds = order.DomainEvents.Select(e => e.EventId).ToList();

        // Assert
        eventIds.Distinct().Count().ShouldBe(eventIds.Count);
    }
}
