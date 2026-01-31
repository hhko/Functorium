using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using InventoryService.Domain;
using InventoryService.Infrastructure;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace InventoryService.Tests.Unit.LayerTests.Adapters;

/// <summary>
/// InMemoryInventoryRepository 단위 테스트
/// </summary>
public sealed class InMemoryInventoryRepositoryTests
{
    private readonly ILogger<InMemoryInventoryRepository> _logger;
    private readonly InMemoryInventoryRepository _repository;

    public InMemoryInventoryRepositoryTests()
    {
        _logger = Substitute.For<ILogger<InMemoryInventoryRepository>>();
        _repository = new InMemoryInventoryRepository(_logger);
    }

    [Fact]
    public async Task Create_ReturnsSuccess_WhenValidInventoryItemIsProvided()
    {
        // Arrange
        var inventoryItem = new InventoryItem(Guid.NewGuid(), Guid.NewGuid(), 100, 0);

        // Act
        var result = await _repository.Create(inventoryItem).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: createdItem =>
            {
                createdItem.ShouldNotBeNull();
                createdItem.Id.ShouldBe(inventoryItem.Id);
                createdItem.ProductId.ShouldBe(inventoryItem.ProductId);
                createdItem.Quantity.ShouldBe(inventoryItem.Quantity);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task GetByProductId_ReturnsSuccess_WhenInventoryItemExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(Guid.NewGuid(), productId, 100, 0);
        await _repository.Create(inventoryItem).Run().RunAsync();

        // Act
        var result = await _repository.GetByProductId(productId).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: foundItem =>
            {
                foundItem.ShouldNotBeNull();
                foundItem.ProductId.ShouldBe(productId);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task GetByProductId_ReturnsFailure_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();

        // Act
        Fin<InventoryItem> result;
        try
        {
            result = await _repository.GetByProductId(nonExistentProductId).Run().RunAsync();
        }
        catch (Exception ex)
        {
            // RunAsync()가 실패 시 예외를 던질 수 있으므로, 예외를 Fin.Fail로 변환
            result = Fin.Fail<InventoryItem>(Error.New(ex.Message));
        }

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("찾을 수 없습니다"));
    }

    [Fact]
    public async Task ReserveQuantity_ReturnsSuccess_WhenQuantityIsAvailable()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(Guid.NewGuid(), productId, 100, 0);
        await _repository.Create(inventoryItem).Run().RunAsync();

        // Act
        var result = await _repository.ReserveQuantity(productId, 10).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: updatedItem =>
            {
                updatedItem.ShouldNotBeNull();
                updatedItem.ReservedQuantity.ShouldBe(10);
                updatedItem.AvailableQuantity.ShouldBe(90);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task ReserveQuantity_ReturnsFailure_WhenQuantityIsNotAvailable()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(Guid.NewGuid(), productId, 100, 50);
        await _repository.Create(inventoryItem).Run().RunAsync();

        // Act - 사용 가능한 수량(50)보다 많은 수량(60) 예약 시도
        Fin<InventoryItem> result;
        try
        {
            result = await _repository.ReserveQuantity(productId, 60).Run().RunAsync();
        }
        catch (Exception ex)
        {
            // RunAsync()가 실패 시 예외를 던질 수 있으므로, 예외를 Fin.Fail로 변환
            result = Fin.Fail<InventoryItem>(Error.New(ex.Message));
        }

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("재고"));
    }

    [Fact]
    public async Task ReserveQuantity_ReturnsFailure_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();

        // Act
        Fin<InventoryItem> result;
        try
        {
            result = await _repository.ReserveQuantity(nonExistentProductId, 10).Run().RunAsync();
        }
        catch (Exception ex)
        {
            // RunAsync()가 실패 시 예외를 던질 수 있으므로, 예외를 Fin.Fail로 변환
            result = Fin.Fail<InventoryItem>(Error.New(ex.Message));
        }

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("찾을 수 없습니다"));
    }
}

