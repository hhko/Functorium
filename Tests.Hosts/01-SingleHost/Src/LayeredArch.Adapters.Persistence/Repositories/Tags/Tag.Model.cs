using Functorium.Adapters.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Tags;

public class TagModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
