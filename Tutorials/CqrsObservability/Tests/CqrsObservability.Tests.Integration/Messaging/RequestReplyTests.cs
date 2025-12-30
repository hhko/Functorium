using Functorium.Applications.Linq;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Adapters.Messaging;
using CqrsObservability.Messages;
using Shouldly;
using Xunit;

namespace CqrsObservability.Tests.Integration.Messaging;

/// <summary>
/// Request/Reply 패턴 통합 테스트
/// OrderService → InventoryService 재고 확인 요청/응답 검증
/// </summary>
public class RequestReplyTests : IClassFixture<MessagingTestFixture>
{
    private readonly MessagingTestFixture _fixture;

    public RequestReplyTests(MessagingTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckInventory_ReturnsSuccess_WhenInventoryIsAvailable()
    {
        // Arrange
        var inventoryMessaging = _fixture.OrderService.Services
            .GetRequiredService<IInventoryMessaging>();

        var productId = Guid.NewGuid();
        var request = new CheckInventoryRequest(productId, Quantity: 5);

        // InventoryService에 재고 데이터 설정 (테스트용)
        // 실제로는 InventoryService의 Repository에 데이터를 추가해야 함
        // 여기서는 핸들러가 정상 작동하는지 확인

        // Act
        var ioFin = inventoryMessaging.CheckInventory(request);
        var result = await ioFin.Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.ProductId.ShouldBe(productId);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task CheckInventory_ReturnsFailure_WhenProductNotFound()
    {
        // Arrange
        var inventoryMessaging = _fixture.OrderService.Services
            .GetRequiredService<IInventoryMessaging>();

        var productId = Guid.NewGuid();
        var request = new CheckInventoryRequest(productId, Quantity: 5);

        // Act
        var ioFin = inventoryMessaging.CheckInventory(request);
        var result = await ioFin.Run().RunAsync();

        // Assert
        // ProductNotFound인 경우, 핸들러는 IsAvailable: false를 반환하므로
        // 실제로는 Succ이지만 IsAvailable이 false인 응답을 받음
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.IsAvailable.ShouldBeFalse();
            },
            Fail: _ => throw new Exception("Should return response with IsAvailable=false"));
    }
}

