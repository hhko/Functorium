namespace PropertyAndFieldValidation.Domains;

public sealed class Product
{
    public string Name { get; }
    public decimal Price { get; }
    public int Quantity { get; }

    private Product(string name, decimal price, int quantity)
    {
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    public static Product Create(string name, decimal price, int quantity)
        => new(name, price, quantity);
}
