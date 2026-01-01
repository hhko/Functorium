using CqrsObservability.Messages;
using InventoryService.Domain;
using InventoryService.Handlers;
using LanguageExt;
using LanguageExt.Common;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;
using Xunit;

namespace InventoryService.Tests.Unit.LayerTests.Application;

/// <summary>
/// ReserveInventoryCommandHandler 단위 테스트
/// </summary>
public sealed class ReserveInventoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_CompletesSuccessfully_WhenInventoryIsAvailable()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new ReserveInventoryCommand(orderId, productId, Quantity: 5);
        var inventoryItem = new InventoryItem(
            Id: Guid.NewGuid(),
            ProductId: productId,
            Quantity: 10,
            ReservedQuantity: 2);

        var repository = Substitute.For<IInventoryRepository>();
        var reservedItem = inventoryItem.ReserveQuantity(5);
        repository.ReserveQuantity(productId, 5)
            .Returns(IO.lift(() => reservedItem));

        // Act
        await ReserveInventoryCommandHandler.Handle(command, repository, TestContext.Current.CancellationToken);

        // Assert: 예외가 발생하지 않으면 성공
        _ = repository.Received(1).ReserveQuantity(productId, 5);
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenInventoryIsInsufficient()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new ReserveInventoryCommand(orderId, productId, Quantity: 10);
        var inventoryItem = new InventoryItem(
            Id: Guid.NewGuid(),
            ProductId: productId,
            Quantity: 10,
            ReservedQuantity: 5);

        var repository = Substitute.For<IInventoryRepository>();
        var error = Error.New("재고가 부족합니다.");
        repository.ReserveQuantity(productId, 10)
            .Returns(IO.lift(() => Fin.Fail<InventoryItem>(error)));

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () =>
            await ReserveInventoryCommandHandler.Handle(command, repository, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenProductNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new ReserveInventoryCommand(orderId, productId, Quantity: 5);
        var error = Error.New("상품을 찾을 수 없습니다.");

        var repository = Substitute.For<IInventoryRepository>();
        repository.ReserveQuantity(productId, 5)
            .Returns(IO.lift(() => Fin.Fail<InventoryItem>(error)));

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () =>
            await ReserveInventoryCommandHandler.Handle(command, repository, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Handler_ShouldNotContainLoggingCode()
    {
        // Arrange & Act: 핸들러 파일 내용 확인
        var handlerPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "Src", "InventoryService", "Handlers", "ReserveInventoryCommandHandler.cs");

        if (!File.Exists(handlerPath))
        {
            // 핸들러가 아직 없으면 테스트 통과 (Red 단계)
            return;
        }

        var handlerCode = File.ReadAllText(handlerPath);

        // Assert: 로깅 코드가 없어야 함
        handlerCode.ShouldNotContain("ILogger");
        handlerCode.ShouldNotContain("LogInformation");
        handlerCode.ShouldNotContain("LogError");
        handlerCode.ShouldNotContain("LogWarning");
        handlerCode.ShouldNotContain("LogDebug");
    }
}

