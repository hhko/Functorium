using Functorium.Applications.Events;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Products;

public class CreateProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly CreateProductCommand.Usecase _sut;

    public CreateProductCommandTests()
    {
        _sut = new CreateProductCommand.Usecase(_productRepository, _eventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        _productRepository.ExistsByName(Arg.Any<ProductName>(), Arg.Any<ProductId?>())
            .Returns(TestIO.Succ(false));
        _productRepository.Create(Arg.Any<Product>())
            .Returns(call => TestIO.Succ(call.Arg<Product>()));
        _eventPublisher.PublishEvents(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(TestIO.Succ(unit));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Test Product");
        actual.ThrowIfFail().Price.ShouldBe(100m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateProductCommand.Request("", "Description", 100m, 10);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPriceIsZero()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 0m, 10);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDuplicateName()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Existing Product", "Description", 100m, 10);

        _productRepository.ExistsByName(Arg.Any<ProductName>(), Arg.Any<ProductId?>())
            .Returns(TestIO.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
