namespace DomainLayerRules.Domains;

[GenerateEntityId]
public sealed class Order : AggregateRoot<Guid>
{
    public string CustomerName { get; }
    public IReadOnlyList<string> Items { get; }

    private Order(Guid id, string customerName, IReadOnlyList<string> items) : base(id)
    {
        CustomerName = customerName;
        Items = items;
    }

    public static Order Create(string customerName, IReadOnlyList<string> items)
        => new(Guid.NewGuid(), customerName, items);

    public static Order CreateFromValidated(Guid id, string customerName, IReadOnlyList<string> items)
        => new(id, customerName, items);
}
