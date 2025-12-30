using OrderService.Domain;
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
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 10;
        var createdAt = DateTime.UtcNow;

        // Act
        var order = new Order(id, productId, quantity, createdAt);

        // Assert
        order.ShouldNotBeNull();
        order.Id.ShouldBe(id);
        order.ProductId.ShouldBe(productId);
        order.Quantity.ShouldBe(quantity);
        order.CreatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void Create_ThrowsException_WhenQuantityIsNegative()
    {
        // Arrange
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = -1;
        var createdAt = DateTime.UtcNow;

        // Act & Assert
        Should.Throw<ArgumentException>(() => new Order(id, productId, quantity, createdAt));
    }

    [Fact]
    public void Create_ThrowsException_WhenQuantityIsZero()
    {
        // Arrange
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 0;
        var createdAt = DateTime.UtcNow;

        // Act & Assert
        Should.Throw<ArgumentException>(() => new Order(id, productId, quantity, createdAt));
    }
}

