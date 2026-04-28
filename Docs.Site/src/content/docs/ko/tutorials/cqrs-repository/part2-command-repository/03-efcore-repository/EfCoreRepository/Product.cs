using Functorium.Domains.Entities;

namespace EfCoreRepository;

/// <summary>
/// Product Aggregate Root (Domain Model).
/// EF Core에 직접 매핑하지 않고, ProductModel을 통해 영속화합니다.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }

    private Product() : base()
    {
        Name = string.Empty;
    }

    public Product(ProductId id, string name, decimal price, bool isActive = true) : base(id)
    {
        Name = name;
        Price = price;
        IsActive = isActive;
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
