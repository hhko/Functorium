using Functorium.Domains.Entities;
using Functorium.Domains.Events;

namespace InMemoryRepository;

/// <summary>
/// Product Aggregate Root.
/// 도메인 이벤트를 발행하며, InMemoryRepository를 통해 영속화됩니다.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product() : base()
    {
        Name = string.Empty;
    }

    public Product(ProductId id, string name, decimal price) : base(id)
    {
        Name = name;
        Price = price;
        AddDomainEvent(new ProductCreatedEvent(id, name));
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedEvent(Id, newPrice));
    }
}

public sealed record ProductCreatedEvent(ProductId ProductId, string Name) : DomainEvent;

public sealed record ProductPriceChangedEvent(ProductId ProductId, decimal NewPrice) : DomainEvent;
