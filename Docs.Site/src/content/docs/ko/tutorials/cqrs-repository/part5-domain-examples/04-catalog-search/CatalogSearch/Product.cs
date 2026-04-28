using Functorium.Domains.Entities;
using LanguageExt;
using LanguageExt.Common;

namespace CatalogSearch;

/// <summary>
/// 카탈로그 상품 Aggregate Root.
/// 검색 예제에 초점을 맞춘 단순 구조입니다.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public string Category { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    private Product(ProductId id, string name, string category, decimal price, int stock)
    {
        Id = id;
        Name = name;
        Category = category;
        Price = price;
        Stock = stock;
    }

    public static Fin<Product> Create(string name, string category, decimal price, int stock = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.New("Name is required.");

        if (string.IsNullOrWhiteSpace(category))
            return Error.New("Category is required.");

        if (price < 0)
            return Error.New("Price cannot be negative.");

        if (stock < 0)
            return Error.New("Stock cannot be negative.");

        return Fin.Succ(new Product(ProductId.New(), name, category, price, stock));
    }
}
