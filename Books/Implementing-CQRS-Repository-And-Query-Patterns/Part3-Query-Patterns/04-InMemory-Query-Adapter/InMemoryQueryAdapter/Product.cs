using Functorium.Domains.Entities;

namespace InMemoryQueryAdapter;

/// <summary>
/// Product Aggregate Root.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; }

    private Product() : base()
    {
        Name = string.Empty;
        Category = string.Empty;
    }

    public Product(ProductId id, string name, decimal price, int stock, string category)
        : base(id)
    {
        Name = name;
        Price = price;
        Stock = stock;
        Category = category;
    }

    public bool IsInStock => Stock > 0;
}
