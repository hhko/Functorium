using Functorium.Domains.Entities;

namespace RepositoryInterface;

/// <summary>
/// Product Aggregate Root.
/// Repository를 통해 영속화되는 최소 단위입니다.
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
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
    }
}
