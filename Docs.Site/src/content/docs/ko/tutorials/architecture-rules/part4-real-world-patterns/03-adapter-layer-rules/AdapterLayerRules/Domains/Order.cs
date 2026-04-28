namespace AdapterLayerRules.Domains;

public sealed class Order
{
    public string Id { get; }

    private Order(string id) => Id = id;

    public static Order Create(string id) => new(id);
}
