using Functorium.Domains.Entities;

namespace UnitOfWork;

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
    }

    public static Product Create(string name, decimal price)
    {
        return new Product(ProductId.New(), name, price);
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
    }
}
