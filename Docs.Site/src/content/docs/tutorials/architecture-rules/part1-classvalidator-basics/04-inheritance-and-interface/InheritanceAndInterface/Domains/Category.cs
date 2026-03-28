namespace InheritanceAndInterface.Domains;

public sealed class Category : Entity<Guid>, IAuditable
{
    public string Title { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ModifiedAt { get; }

    private Category(Guid id, string title)
    {
        Id = id;
        Title = title;
        CreatedAt = DateTime.UtcNow;
    }

    public static Category Create(string title) => new(Guid.NewGuid(), title);
}
