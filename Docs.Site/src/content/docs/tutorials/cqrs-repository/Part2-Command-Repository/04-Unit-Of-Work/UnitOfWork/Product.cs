using Functorium.Domains.Entities;

namespace UnitOfWork;

public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    private Product() : base()
    {
        Name = string.Empty;
    }

    public Product(ProductId id, string name, decimal price, int stock = 0) : base(id)
    {
        Name = name;
        Price = price;
        Stock = stock;
    }

    public static Product Create(string name, decimal price, int stock = 0)
    {
        return new Product(ProductId.New(), name, price, stock);
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
    }

    public void DeductStock(int quantity)
    {
        if (quantity > Stock)
            throw new InvalidOperationException($"재고 부족. 현재: {Stock}, 요청: {quantity}");
        Stock -= quantity;
    }
}
