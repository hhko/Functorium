namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;

public class OrderModel
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
