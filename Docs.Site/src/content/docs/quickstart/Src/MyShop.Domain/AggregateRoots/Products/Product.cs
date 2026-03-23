namespace MyShop.Domain.AggregateRoots.Products;

[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>
{
    #region Domain Events

    public sealed record CreatedEvent(ProductId ProductId, ProductName Name, Money Price) : DomainEvent;

    #endregion

    public ProductName Name { get; private set; }
    public Money Price { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Product(ProductId id, ProductName name, Money price) : base(id)
    {
        Name = name;
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }

    public static Product Create(ProductName name, Money price)
    {
        var product = new Product(ProductId.New(), name, price);
        product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
        return product;
    }

    public static Product CreateFromValidated(
        ProductId id, ProductName name, Money price, DateTime createdAt)
    {
        return new Product(id, name, price) { CreatedAt = createdAt };
    }
}
