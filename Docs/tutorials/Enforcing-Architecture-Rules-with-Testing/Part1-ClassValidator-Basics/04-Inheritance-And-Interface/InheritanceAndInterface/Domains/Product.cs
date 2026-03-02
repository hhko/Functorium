namespace InheritanceAndInterface.Domains;

public sealed class Product : Entity<Guid>, IAggregate, IAuditable
{
    public string Name { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ModifiedAt { get; }

    private Product(Guid id, string name)
    {
        Id = id;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public static Product Create(string name) => new(Guid.NewGuid(), name);
}
