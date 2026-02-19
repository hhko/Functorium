using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;

internal static class InventoryMapper
{
    public static InventoryModel ToModel(this Inventory inventory) => new()
    {
        Id = inventory.Id.ToString(),
        ProductId = inventory.ProductId.ToString(),
        StockQuantity = inventory.StockQuantity,
        RowVersion = inventory.RowVersion,
        CreatedAt = inventory.CreatedAt,
        UpdatedAt = inventory.UpdatedAt.ToNullable()
    };

    public static Inventory ToDomain(this InventoryModel model) =>
        Inventory.CreateFromValidated(
            InventoryId.Create(model.Id),
            ProductId.Create(model.ProductId),
            Quantity.CreateFromValidated(model.StockQuantity),
            model.RowVersion,
            model.CreatedAt,
            Optional(model.UpdatedAt));
}
