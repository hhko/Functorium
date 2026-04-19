using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;

namespace LayeredArch.Adapters.Persistence.Repositories.Customers;

[GenerateSetters]
public partial class CustomerModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
