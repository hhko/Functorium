using Functorium.Domains.Entities;

namespace CommandUsecase;

public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Product(ProductId id, string name, decimal price, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Price = price;
        CreatedAt = createdAt;
    }

    public static Product Create(string name, decimal price)
    {
        return new Product(ProductId.New(), name, price, DateTime.UtcNow);
    }
}
