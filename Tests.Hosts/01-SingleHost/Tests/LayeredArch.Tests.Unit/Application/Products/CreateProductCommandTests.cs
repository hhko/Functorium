using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Commands;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Products;

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
    public async Task Handle_ShouldThrow_WhenNameIsEmpty()
    {
        // Arrange — 파이프라인 없이 직접 호출하면 Unwrap()에서 예외 발생
        var request = new CreateProductCommand.Request("", "Description", 100m, 10);

        // Act & Assert
        await Should.ThrowAsync<Exception>(
            () => _sut.Handle(request, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenPriceIsZero()
    {
        // Arrange — 파이프라인 없이 직접 호출하면 Unwrap()에서 예외 발생
        var request = new CreateProductCommand.Request("Test Product", "Description", 0m, 10);

        // Act & Assert
        await Should.ThrowAsync<Exception>(
            () => _sut.Handle(request, CancellationToken.None).AsTask());
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
