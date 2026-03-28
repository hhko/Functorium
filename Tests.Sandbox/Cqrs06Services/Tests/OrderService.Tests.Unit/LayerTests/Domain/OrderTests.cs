using OrderService.Domain;
using OrderService.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace OrderService.Tests.Unit.LayerTests.Domain;

/// <summary>
/// Order 도메인 모델 단위 테스트
/// </summary>
public sealed class OrderTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenValidOrderIsProvided()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var quantityResult = Quantity.Create(10);
        quantityResult.IsSucc.ShouldBeTrue();
        var quantity = quantityResult.Match(Succ: v => v, Fail: _ => null!);

        // Act
        var result = Order.Create(productId, quantity);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: order =>
            {
                order.ShouldNotBeNull();
                order.Id.ShouldNotBe(default);
                order.ProductId.ShouldBe(productId);
                ((int)order.Quantity).ShouldBe(10);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public void Quantity_Create_ReturnsFailure_WhenQuantityIsNegative()
    {
        // Arrange & Act
        var result = Quantity.Create(-1);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Quantity_Create_ReturnsFailure_WhenQuantityIsZero()
    {
        // Arrange & Act
        var result = Quantity.Create(0);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ReturnsOrder_WhenValidDataIsProvided()
    {
        // Arrange
        var orderId = OrderId.New();
        var productId = Guid.NewGuid();
        var quantityResult = Quantity.Create(5);
        quantityResult.IsSucc.ShouldBeTrue();
        var quantity = quantityResult.Match(Succ: v => v, Fail: _ => null!);
        var createdAt = DateTime.UtcNow;

        // Act
        var order = Order.CreateFromValidated(orderId, productId, quantity, createdAt);

        // Assert
        order.ShouldNotBeNull();
        order.Id.ShouldBe(orderId);
        order.ProductId.ShouldBe(productId);
        ((int)order.Quantity).ShouldBe(5);
        order.CreatedAt.ShouldBe(createdAt);
    }
}

