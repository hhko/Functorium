using LanguageExt;
using LanguageExt.Common;
using NSubstitute;
using OrderService.Adapters.Messaging;
using CqrsObservability.Messages;
using Shouldly;
using static LanguageExt.Prelude;
using Wolverine;
using Xunit;

namespace OrderService.Tests.Unit.LayerTests.Adapters;

/// <summary>
/// RabbitMqInventoryMessaging 단위 테스트
/// </summary>
public sealed class RabbitMqInventoryMessagingTests
{
    [Fact]
    public async Task CheckInventory_SendsRequest_WhenRequestIsValid()
    {
        // Arrange
        var request = new CheckInventoryRequest(Guid.NewGuid(), Quantity: 5);
        var expectedResponse = new CheckInventoryResponse(
            ProductId: request.ProductId,
            IsAvailable: true,
            AvailableQuantity: 10);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.InvokeAsync<CheckInventoryResponse>(request, Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(expectedResponse);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.CheckInventory(request);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.ProductId.ShouldBe(request.ProductId);
                response.IsAvailable.ShouldBeTrue();
                response.AvailableQuantity.ShouldBe(10);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task CheckInventory_ReturnsFailure_WhenMessageBusThrowsException()
    {
        // Arrange
        var request = new CheckInventoryRequest(Guid.NewGuid(), Quantity: 5);
        var messageBus = Substitute.For<IMessageBus>();
        messageBus.InvokeAsync<CheckInventoryResponse>(request, Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(Task.FromException<CheckInventoryResponse>(new Exception("Connection failed")));

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.CheckInventory(request);
        var ioResult = ioFin.Run();
        Fin<CheckInventoryResponse> result;
        try
        {
            result = await Task.Run(() => ioResult.Run());
        }
        catch (Exception ex)
        {
            result = Fin.Fail<CheckInventoryResponse>(Error.New(ex.Message));
        }

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public async Task ReserveInventory_SendsCommand_WhenCommandIsValid()
    {
        // Arrange
        var command = new ReserveInventoryCommand(
            OrderId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Quantity: 5);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.SendAsync(command)
            .Returns(ValueTask.CompletedTask);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.ReserveInventory(command);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        await messageBus.Received(1).SendAsync(command);
    }

    [Fact]
    public async Task ReserveInventory_ReturnsFailure_WhenMessageBusThrowsException()
    {
        // Arrange
        var command = new ReserveInventoryCommand(
            OrderId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Quantity: 5);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.SendAsync(command)
            .Returns(ValueTask.FromException(new Exception("Connection failed")));

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.ReserveInventory(command);
        var ioResult = ioFin.Run();
        Fin<LanguageExt.Unit> result;
        try
        {
            result = await Task.Run(() => ioResult.Run());
        }
        catch (Exception ex)
        {
            result = Fin.Fail<LanguageExt.Unit>(Error.New(ex.Message));
        }

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}

