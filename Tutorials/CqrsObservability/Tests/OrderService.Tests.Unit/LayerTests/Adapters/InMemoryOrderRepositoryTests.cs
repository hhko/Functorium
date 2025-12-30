using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderService.Domain;
using OrderService.Infrastructure;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace OrderService.Tests.Unit.LayerTests.Adapters;

/// <summary>
/// InMemoryOrderRepository 단위 테스트
/// </summary>
public sealed class InMemoryOrderRepositoryTests
{
    private readonly ILogger<InMemoryOrderRepository> _logger;
    private readonly InMemoryOrderRepository _repository;

    public InMemoryOrderRepositoryTests()
    {
        _logger = Substitute.For<ILogger<InMemoryOrderRepository>>();
        _repository = new InMemoryOrderRepository(_logger);
    }

    [Fact]
    public async Task Create_ReturnsSuccess_WhenValidOrderIsProvided()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), 10, DateTime.UtcNow);

        // Act
        var result = await _repository.Create(order).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: createdOrder =>
            {
                createdOrder.ShouldNotBeNull();
                createdOrder.Id.ShouldBe(order.Id);
                createdOrder.ProductId.ShouldBe(order.ProductId);
                createdOrder.Quantity.ShouldBe(order.Quantity);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task GetById_ReturnsSuccess_WhenOrderExists()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), 10, DateTime.UtcNow);
        await _repository.Create(order).Run().RunAsync();

        // Act
        var result = await _repository.GetById(order.Id).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: foundOrder =>
            {
                foundOrder.ShouldNotBeNull();
                foundOrder.Id.ShouldBe(order.Id);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task GetById_ReturnsFailure_WhenOrderDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        Fin<Order> result;
        try
        {
            result = await _repository.GetById(nonExistentId).Run().RunAsync();
        }
        catch (Exception ex)
        {
            // RunAsync()가 실패 시 예외를 던질 수 있으므로, 예외를 Fin.Fail로 변환
            result = Fin.Fail<Order>(Error.New(ex.Message));
        }

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("찾을 수 없습니다"));
    }
}

