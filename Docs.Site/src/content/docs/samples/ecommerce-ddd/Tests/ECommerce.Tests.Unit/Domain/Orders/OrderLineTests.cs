using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Orders;

public class OrderLineTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenQuantityIsPositive()
    {
        // Arrange
        var productId = ProductId.New();
        var quantity = Quantity.Create(3).ThrowIfFail();
        var unitPrice = Money.Create(100m).ThrowIfFail();

        // Act
        var actual = OrderLine.Create(productId, quantity, unitPrice);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCalculateLineTotal()
    {
        // Arrange
        var productId = ProductId.New();
        var quantity = Quantity.Create(3).ThrowIfFail();
        var unitPrice = Money.Create(100m).ThrowIfFail();

        // Act
        var sut = OrderLine.Create(productId, quantity, unitPrice).ThrowIfFail();

        // Assert
        ((decimal)sut.LineTotal).ShouldBe(300m);
    }

    [Fact]
    public void Create_ShouldFail_WhenQuantityIsZero()
    {
        // Arrange
        var productId = ProductId.New();
        var quantity = Quantity.Create(0).ThrowIfFail();
        var unitPrice = Money.Create(100m).ThrowIfFail();

        // Act
        var actual = OrderLine.Create(productId, quantity, unitPrice);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreAllFields()
    {
        // Arrange
        var id = OrderLineId.New();
        var productId = ProductId.New();
        var quantity = Quantity.CreateFromValidated(5);
        var unitPrice = Money.CreateFromValidated(200m);
        var lineTotal = Money.CreateFromValidated(1000m);

        // Act
        var sut = OrderLine.CreateFromValidated(id, productId, quantity, unitPrice, lineTotal);

        // Assert
        sut.Id.ShouldBe(id);
        sut.ProductId.ShouldBe(productId);
        ((int)sut.Quantity).ShouldBe(5);
        ((decimal)sut.UnitPrice).ShouldBe(200m);
        ((decimal)sut.LineTotal).ShouldBe(1000m);
    }
}
