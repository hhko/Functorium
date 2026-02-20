namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;

public class OrderLineModel
{
    public string Id { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
