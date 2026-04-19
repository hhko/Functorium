using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;

namespace LayeredArch.Adapters.Persistence.Repositories.Tags;

[GenerateSetters]
public partial class TagModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
