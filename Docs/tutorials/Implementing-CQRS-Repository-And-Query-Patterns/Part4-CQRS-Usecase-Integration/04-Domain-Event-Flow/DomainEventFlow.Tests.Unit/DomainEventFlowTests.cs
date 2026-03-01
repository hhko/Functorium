using DomainEventFlow;
using Functorium.Domains.Events;

namespace DomainEventFlow.Tests.Unit;

public sealed class DomainEventFlowTests
{
    [Fact]
    public void Create_RaisesProductCreatedEvent()
    {
        // Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        product.DomainEvents.Count.ShouldBe(1);
        product.DomainEvents[0].ShouldBeOfType<ProductCreatedEvent>();
    }

    [Fact]
    public void Create_SetsEventProperties()
    {
        // Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        var e = product.DomainEvents[0].ShouldBeOfType<ProductCreatedEvent>();
        e.ProductId.ShouldBe(product.Id.ToString());
        e.Name.ShouldBe("노트북");
        e.Price.ShouldBe(1_500_000m);
    }

    [Fact]
    public void UpdatePrice_RaisesPriceChangedEvent()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);

        // Act
        product.UpdatePrice(1_300_000m);

        // Assert
        product.DomainEvents.Count.ShouldBe(2);
        var e = product.DomainEvents[1].ShouldBeOfType<ProductPriceChangedEvent>();
        e.OldPrice.ShouldBe(1_500_000m);
        e.NewPrice.ShouldBe(1_300_000m);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);
        product.UpdatePrice(1_300_000m);

        // Act
        product.ClearDomainEvents();

        // Assert
        product.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void Collector_TracksAggregateWithEvents()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);
        var collector = new SimpleDomainEventCollector();

        // Act
        collector.Track(product);

        // Assert
        collector.GetTrackedAggregates().Count.ShouldBe(1);
    }

    [Fact]
    public void Collector_ExcludesAggregateAfterClear()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);
        var collector = new SimpleDomainEventCollector();
        collector.Track(product);

        // Act
        product.ClearDomainEvents();

        // Assert
        collector.GetTrackedAggregates().Count.ShouldBe(0);
    }

    [Fact]
    public void DomainEvent_HasEventIdAndOccurredAt()
    {
        // Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        var e = product.DomainEvents[0];
        e.EventId.ToString().ShouldNotBeNullOrEmpty();
        e.OccurredAt.ShouldNotBe(default);
    }
}
