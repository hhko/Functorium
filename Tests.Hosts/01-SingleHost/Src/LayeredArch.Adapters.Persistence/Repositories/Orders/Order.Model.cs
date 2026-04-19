using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;

namespace LayeredArch.Adapters.Persistence.Repositories.Orders;

[GenerateSetters]
public partial class OrderModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<OrderLineModel> OrderLines { get; set; } = [];
}
