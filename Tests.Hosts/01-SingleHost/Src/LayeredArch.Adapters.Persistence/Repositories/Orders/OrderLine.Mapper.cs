using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Adapters.Persistence.Repositories.Orders;

internal static class OrderLineMapper
{
    public static OrderLineModel ToModel(this OrderLine orderLine, string orderId) => new()
    {
        Id = orderLine.Id.ToString(),
        OrderId = orderId,
        ProductId = orderLine.ProductId.ToString(),
        Quantity = orderLine.Quantity,
        UnitPrice = orderLine.UnitPrice,
        LineTotal = orderLine.LineTotal
    };

    public static OrderLine ToDomain(this OrderLineModel model) =>
        OrderLine.CreateFromValidated(
            OrderLineId.Create(model.Id),
            ProductId.Create(model.ProductId),
            Quantity.CreateFromValidated(model.Quantity),
            Money.CreateFromValidated(model.UnitPrice),
            Money.CreateFromValidated(model.LineTotal));
}
