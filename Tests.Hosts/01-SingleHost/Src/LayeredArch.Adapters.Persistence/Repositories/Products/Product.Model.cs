using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;

namespace LayeredArch.Adapters.Persistence.Repositories.Products;

[GenerateSetters]
public partial class ProductModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public List<ProductTagModel> ProductTags { get; set; } = [];
}
