using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Commands;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Tests.Unit.Application.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommand.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateProductCommand.Request("", "Description", 100m, 10);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenPriceIsZero()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 0m, 10);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "Price");
    }
}

public class CreateProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly CreateProductCommand.Usecase _sut;

    public CreateProductCommandTests()
    {
        _sut = new CreateProductCommand.Usecase(_productRepository, _inventoryRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        _productRepository.Exists(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(false));
        _productRepository.Create(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _inventoryRepository.Create(Arg.Any<Inventory>())
            .Returns(call => FinTFactory.Succ(call.Arg<Inventory>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Test Product");
        actual.ThrowIfFail().Price.ShouldBe(100m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDuplicateName()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Existing Product", "Description", 100m, 10);

        _productRepository.Exists(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
