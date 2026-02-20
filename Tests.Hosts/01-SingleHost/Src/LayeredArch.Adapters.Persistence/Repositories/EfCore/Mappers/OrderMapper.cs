using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;

internal static class OrderMapper
{
    public static OrderModel ToModel(this Order order)
    {
        var orderId = order.Id.ToString();
        return new()
        {
            Id = orderId,
            CustomerId = order.CustomerId.ToString(),
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt.ToNullable(),
            OrderLines = order.OrderLines.Select(l => l.ToModel(orderId)).ToList()
        };
    }

    public static Order ToDomain(this OrderModel model) =>
        Order.CreateFromValidated(
            OrderId.Create(model.Id),
            CustomerId.Create(model.CustomerId),
            model.OrderLines.Select(l => l.ToDomain()),
            Money.CreateFromValidated(model.TotalAmount),
            ShippingAddress.CreateFromValidated(model.ShippingAddress),
            OrderStatus.CreateFromValidated(model.Status),
            model.CreatedAt,
            Optional(model.UpdatedAt));
}
