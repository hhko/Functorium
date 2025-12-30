using Functorium.Applications.Linq;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Adapters.Messaging;
using CqrsObservability.Messages;
using Shouldly;
using Xunit;

namespace CqrsObservability.Tests.Integration.Messaging;

/// <summary>
/// Fire and Forget 패턴 통합 테스트
/// OrderService → InventoryService 재고 예약 알림 검증
/// </summary>
public class FireAndForgetTests : IClassFixture<MessagingTestFixture>
{
    private readonly MessagingTestFixture _fixture;

    public FireAndForgetTests(MessagingTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReserveInventory_SendsMessage_WhenCommandIsValid()
    {
        // Arrange
        var inventoryMessaging = _fixture.OrderService.Services
            .GetRequiredService<IInventoryMessaging>();

        var command = new ReserveInventoryCommand(
            OrderId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Quantity: 5);

        // Act
        var ioFin = inventoryMessaging.ReserveInventory(command);
        var result = await ioFin.Run().RunAsync();

        // Assert
        // Fire and Forget이므로 메시지 전송 성공 여부만 확인
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: _ => { },
            Fail: error => throw new Exception($"Should be success: {error.Message}"));
    }
}

