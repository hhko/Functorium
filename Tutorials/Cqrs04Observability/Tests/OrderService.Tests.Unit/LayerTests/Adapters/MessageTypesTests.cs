using CqrsObservability.Messages;
using Shouldly;
using Xunit;

namespace OrderService.Tests.Unit.LayerTests.Adapters;

/// <summary>
/// 메시지 타입 단위 테스트
/// </summary>
public sealed class MessageTypesTests
{
    [Fact]
    public void CheckInventoryRequest_Create_ReturnsSuccess_WhenValidRequestIsProvided()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var quantity = 10;

        // Act
        var request = new CheckInventoryRequest(productId, quantity);

        // Assert
        request.ShouldNotBeNull();
        request.ProductId.ShouldBe(productId);
        request.Quantity.ShouldBe(quantity);
    }

    [Fact]
    public void CheckInventoryResponse_Create_ReturnsSuccess_WhenValidResponseIsProvided()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var isAvailable = true;
        var availableQuantity = 100;

        // Act
        var response = new CheckInventoryResponse(productId, isAvailable, availableQuantity);

        // Assert
        response.ShouldNotBeNull();
        response.ProductId.ShouldBe(productId);
        response.IsAvailable.ShouldBe(isAvailable);
        response.AvailableQuantity.ShouldBe(availableQuantity);
    }

    [Fact]
    public void ReserveInventoryCommand_Create_ReturnsSuccess_WhenValidCommandIsProvided()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var quantity = 10;

        // Act
        var command = new ReserveInventoryCommand(orderId, productId, quantity);

        // Assert
        command.ShouldNotBeNull();
        command.OrderId.ShouldBe(orderId);
        command.ProductId.ShouldBe(productId);
        command.Quantity.ShouldBe(quantity);
    }
}

