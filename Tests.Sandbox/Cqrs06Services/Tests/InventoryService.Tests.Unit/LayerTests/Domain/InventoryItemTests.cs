using InventoryService.Domain;
using Shouldly;
using Xunit;

namespace InventoryService.Tests.Unit.LayerTests.Domain;

/// <summary>
/// InventoryItem 도메인 모델 단위 테스트
/// </summary>
public sealed class InventoryItemTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenValidInventoryItemIsProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 100;
        var reservedQuantity = 0;

        // Act
        var item = new InventoryItem(id, productId, quantity, reservedQuantity);

        // Assert
        item.ShouldNotBeNull();
        item.Id.ShouldBe(id);
        item.ProductId.ShouldBe(productId);
        item.Quantity.ShouldBe(quantity);
        item.ReservedQuantity.ShouldBe(reservedQuantity);
        item.AvailableQuantity.ShouldBe(quantity - reservedQuantity);
    }

    [Fact]
    public void ReserveQuantity_ReturnsSuccess_WhenQuantityIsAvailable()
    {
        // Arrange
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 100;
        var reservedQuantity = 0;
        var item = new InventoryItem(id, productId, quantity, reservedQuantity);
        var reserveAmount = 10;

        // Act
        var result = item.ReserveQuantity(reserveAmount);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: updatedItem =>
            {
                updatedItem.ReservedQuantity.ShouldBe(reservedQuantity + reserveAmount);
                updatedItem.AvailableQuantity.ShouldBe(quantity - (reservedQuantity + reserveAmount));
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public void ReserveQuantity_ReturnsFailure_WhenQuantityIsNotAvailable()
    {
        // Arrange
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 100;
        var reservedQuantity = 50;
        var item = new InventoryItem(id, productId, quantity, reservedQuantity);
        var reserveAmount = 60; // AvailableQuantity는 50이므로 실패해야 함

        // Act
        var result = item.ReserveQuantity(reserveAmount);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("재고"));
    }

    [Fact]
    public void ReserveQuantity_ReturnsFailure_WhenReserveAmountIsNegative()
    {
        // Arrange
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 100;
        var reservedQuantity = 0;
        var item = new InventoryItem(id, productId, quantity, reservedQuantity);
        var reserveAmount = -1;

        // Act
        var result = item.ReserveQuantity(reserveAmount);

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}

