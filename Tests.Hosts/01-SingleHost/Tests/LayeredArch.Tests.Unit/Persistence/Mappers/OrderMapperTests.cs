using LayeredArch.Adapters.Persistence.Repositories.Orders;
using LayeredArch.Domain.AggregateRoots.Customers;
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
        var line = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(3).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail()).ThrowIfFail();
        var order = Order.Create(
            CustomerId.New(),
            [line],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

        // Act
        var actual = order.ToModel().ToDomain();

        // Assert
        actual.Id.ToString().ShouldBe(order.Id.ToString());
        actual.CustomerId.ToString().ShouldBe(order.CustomerId.ToString());
        actual.OrderLines.Count.ShouldBe(1);
        actual.OrderLines[0].ProductId.ToString().ShouldBe(line.ProductId.ToString());
        ((int)actual.OrderLines[0].Quantity).ShouldBe(3);
        ((decimal)actual.OrderLines[0].UnitPrice).ShouldBe(100m);
        ((decimal)actual.OrderLines[0].LineTotal).ShouldBe(300m);
        ((decimal)actual.TotalAmount).ShouldBe((decimal)order.TotalAmount);
        ((string)actual.ShippingAddress).ShouldBe((string)order.ShippingAddress);
        ((string)actual.Status).ShouldBe((string)order.Status);
        actual.CreatedAt.ShouldBe(order.CreatedAt);
        actual.UpdatedAt.ShouldBe(order.UpdatedAt);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveStatus_WhenConfirmed()
    {
        // Arrange
        var line = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(50m).ThrowIfFail()).ThrowIfFail();
        var order = Order.Create(
            CustomerId.New(),
            [line],
            ShippingAddress.Create("Busan, Korea").ThrowIfFail()).ThrowIfFail();
        order.Confirm();

        // Act
        var actual = order.ToModel().ToDomain();

        // Assert
        actual.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveMultipleOrderLines()
    {
        // Arrange
        var line1 = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail()).ThrowIfFail();
        var line2 = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(3).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail()).ThrowIfFail();
        var order = Order.Create(
            CustomerId.New(),
            [line1, line2],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

        // Act
        var actual = order.ToModel().ToDomain();

        // Assert
        actual.OrderLines.Count.ShouldBe(2);
        ((decimal)actual.TotalAmount).ShouldBe(800m); // 200 + 600
    }
}
