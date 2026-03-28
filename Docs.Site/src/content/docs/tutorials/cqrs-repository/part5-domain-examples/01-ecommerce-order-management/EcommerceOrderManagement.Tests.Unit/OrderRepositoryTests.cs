using EcommerceOrderManagement;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;

namespace EcommerceOrderManagement.Tests.Unit;

public sealed class OrderRepositoryTests
{
    private readonly InMemoryOrderRepository _repository;

    public OrderRepositoryTests()
    {
        _repository = new InMemoryOrderRepository(new NoOpDomainEventCollector());
    }

    private static Order CreateOrder(string customerName = "홍길동")
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create("노트북", 1, 1_500_000m),
        };
        return Order.Create(customerName, lines).ThrowIfFail();
    }

    [Fact]
    public async Task Create_And_GetById_ReturnsOrder()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        await _repository.Create(order).Run().RunAsync();
        var result = await _repository.GetById(order.Id).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().Id.ShouldBe(order.Id);
    }

    [Fact]
    public async Task GetById_NotFound_Throws()
    {
        await Should.ThrowAsync<Exception>(
            async () => await _repository.GetById(OrderId.New()).Run().RunAsync());
    }

    [Fact]
    public async Task Update_ExistingOrder_ReturnsSucc()
    {
        // Arrange
        var order = CreateOrder();
        await _repository.Create(order).Run().RunAsync();
        order.Confirm();

        // Act
        var result = await _repository.Update(order).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        var fetched = await _repository.GetById(order.Id).Run().RunAsync();
        fetched.ThrowIfFail().Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task Delete_ExistingOrder_ReturnsAffectedCount()
    {
        // Arrange
        var order = CreateOrder();
        await _repository.Create(order).Run().RunAsync();

        // Act
        var result = await _repository.Delete(order.Id).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().ShouldBe(1);
    }

    [Fact]
    public async Task Delete_NonExistingOrder_ReturnsZero()
    {
        var result = await _repository.Delete(OrderId.New()).Run().RunAsync();
        result.ThrowIfFail().ShouldBe(0);
    }

    private sealed class NoOpDomainEventCollector : IDomainEventCollector
    {
        public void Track(IHasDomainEvents aggregate) { }
        public void TrackRange(IEnumerable<IHasDomainEvents> aggregates) { }
        public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates() => [];
        public void TrackEvent(IDomainEvent domainEvent) { }
        public IReadOnlyList<IDomainEvent> GetDirectlyTrackedEvents() => [];
    }
}
