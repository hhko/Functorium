using Functorium.Adapters.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Inventories;

public class InventoryModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public int StockQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
