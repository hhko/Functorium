namespace PropertyAndFieldValidation.Domains;

public sealed class OrderLine
{
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }

    private OrderLine(string productName, int quantity, decimal unitPrice)
    {
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderLine Create(string productName, int quantity, decimal unitPrice)
        => new(productName, quantity, unitPrice);
}
