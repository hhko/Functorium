using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;

internal static class OrderMapper
{
    public static OrderModel ToModel(this Order order) => new()
    {
        Id = order.Id.ToString(),
        ProductId = order.ProductId.ToString(),
        Quantity = order.Quantity,
        UnitPrice = order.UnitPrice,
        TotalAmount = order.TotalAmount,
        ShippingAddress = order.ShippingAddress,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt.ToNullable()
    };

    public static Order ToDomain(this OrderModel model) =>
        Order.CreateFromValidated(
            OrderId.Create(model.Id),
            ProductId.Create(model.ProductId),
            Quantity.CreateFromValidated(model.Quantity),
            Money.CreateFromValidated(model.UnitPrice),
            Money.CreateFromValidated(model.TotalAmount),
            ShippingAddress.CreateFromValidated(model.ShippingAddress),
            model.CreatedAt,
            Optional(model.UpdatedAt));
}
