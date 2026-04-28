using Functorium.Domains.Entities;
using Functorium.Domains.Events;

namespace TransactionPipeline;

public sealed record ProductCreatedEvent(string ProductId, string Name) : DomainEvent;

public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product(ProductId id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public static Product Create(string name, decimal price)
    {
        var product = new Product(ProductId.New(), name, price);
        product.AddDomainEvent(new ProductCreatedEvent(product.Id.ToString(), product.Name));
        return product;
    }
}
