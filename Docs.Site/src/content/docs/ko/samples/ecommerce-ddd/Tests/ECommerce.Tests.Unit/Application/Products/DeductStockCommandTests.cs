using ECommerce.Application.Usecases.Products.Commands;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Products;

public class DeductStockCommandValidatorTests
{
    private readonly DeductStockCommand.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenRequestIsValid()
    {
        // Arrange
        var request = new DeductStockCommand.Request(ProductId.New().ToString(), 3);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenQuantityIsInvalid()
    {
        // Arrange
        var request = new DeductStockCommand.Request(ProductId.New().ToString(), -1);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "Quantity");
    }
}

public class DeductStockCommandTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly DeductStockCommand.Usecase _sut;

    public DeductStockCommandTests()
    {
        _sut = new DeductStockCommand.Usecase(_inventoryRepository);
    }

    private static Inventory CreateInventoryWithStock(int stock)
    {
        return Inventory.Create(
            ProductId.New(),
            Quantity.Create(stock).ThrowIfFail());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSufficientStock()
    {
        // Arrange
        var inventory = CreateInventoryWithStock(10);
        var request = new DeductStockCommand.Request(inventory.ProductId.ToString(), 3);

        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(inventory));
        _inventoryRepository.Update(Arg.Any<Inventory>())
            .Returns(call => FinTFactory.Succ(call.Arg<Inventory>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().RemainingStock.ShouldBe(7);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenInsufficientStock()
    {
        // Arrange
        var inventory = CreateInventoryWithStock(2);
        var request = new DeductStockCommand.Request(inventory.ProductId.ToString(), 5);

        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(inventory));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var request = new DeductStockCommand.Request(ProductId.New().ToString(), 3);

        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<Inventory>(Error.New("Inventory not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
