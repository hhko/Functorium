using Functorium.Domains.Entities;

namespace EntityAndIdentity;

/// <summary>
/// 상품 Entity.
/// Entity&lt;TId&gt;를 상속하여 ID 기반 동등성을 자동으로 제공받습니다.
/// </summary>
public sealed class Product : Entity<ProductId>
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
        return new Product(ProductId.New(), name, price);
    }

    public static Product CreateFromValidated(ProductId id, string name, decimal price)
    {
        return new Product(id, name, price);
    }
}
