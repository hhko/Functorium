using Functorium.Domains.Entities;

namespace QueryUsecase;

public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }

    private Product(ProductId id, string name, decimal price, bool isActive)
    {
        Id = id;
        Name = name;
        Price = price;
        IsActive = isActive;
    }

    public static Product Create(string name, decimal price)
    {
        return new Product(ProductId.New(), name, price, isActive: true);
    }
}
