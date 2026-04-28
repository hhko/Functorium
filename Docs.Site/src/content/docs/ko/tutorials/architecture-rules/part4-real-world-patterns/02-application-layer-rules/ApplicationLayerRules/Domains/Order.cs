namespace ApplicationLayerRules.Domains;

public sealed class Order : Entity<Guid>
{
    public string CustomerName { get; }

    private Order(Guid id, string customerName)
    {
        Id = id;
        CustomerName = customerName;
    }

    public static Order Create(string customerName)
        => new(Guid.NewGuid(), customerName);

    public static Order CreateFromValidated(Guid id, string customerName)
        => new(id, customerName);
}
