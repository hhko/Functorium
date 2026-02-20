namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;

public class TagModel
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
