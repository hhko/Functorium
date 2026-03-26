using Functorium.Adapters.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Customers;

public class CustomerModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
