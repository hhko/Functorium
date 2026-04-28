using ConstructorHandling.Generated;

namespace ConstructorHandling.Usage;

[AutoFactory]
public partial class Order(string orderId, string customerName, int itemCount)
{
    public string OrderId { get; } = orderId;
    public string CustomerName { get; } = customerName;
    public int ItemCount { get; } = itemCount;
}

[AutoFactory]
public partial class Product(string name, decimal price)
{
    public string Name { get; } = name;
    public decimal Price { get; } = price;
}
