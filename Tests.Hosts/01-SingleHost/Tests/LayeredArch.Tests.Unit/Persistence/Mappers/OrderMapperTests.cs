using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Persistence.Mappers;

public class OrderMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        // Arrange
        var order = Order.Create(
            ProductId.New(),
            Quantity.Create(3).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail(),
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail());

        // Act
        var actual = order.ToModel().ToDomain();

        // Assert
        actual.Id.ToString().ShouldBe(order.Id.ToString());
        actual.ProductId.ToString().ShouldBe(order.ProductId.ToString());
        ((int)actual.Quantity).ShouldBe(order.Quantity);
        ((decimal)actual.UnitPrice).ShouldBe(order.UnitPrice);
        ((decimal)actual.TotalAmount).ShouldBe(order.TotalAmount);
        ((string)actual.ShippingAddress).ShouldBe(order.ShippingAddress);
        actual.CreatedAt.ShouldBe(order.CreatedAt);
        actual.UpdatedAt.ShouldBe(order.UpdatedAt);
    }
}
