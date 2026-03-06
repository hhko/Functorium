namespace InterfaceValidation.Domains;

public sealed class Order
{
    public string Id { get; }
    public string CustomerName { get; }
    private Order(string id, string customerName) { Id = id; CustomerName = customerName; }
    public static Order Create(string id, string customerName) => new(id, customerName);
}
