using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Products;

public class CreateProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly CreateProductCommand.Usecase _sut;

    public CreateProductCommandTests()
    {
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _sut = new CreateProductCommand.Usecase(_productRepository, _unitOfWork, _eventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        _productRepository.ExistsByName(Arg.Any<ProductName>(), Arg.Any<ProductId?>())
            .Returns(FinTFactory.Succ(false));
        _productRepository.Create(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _eventPublisher.PublishEvents(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));

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
            .Returns(FinTFactory.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesBeforePublishEvents_WhenRequestIsValid()
    {
        // Arrange
        var callOrder = new List<string>();
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        _productRepository.ExistsByName(Arg.Any<ProductName>(), Arg.Any<ProductId?>())
            .Returns(FinTFactory.Succ(false));
        _productRepository.Create(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit))
            .AndDoes(_ => callOrder.Add("SaveChanges"));
        _eventPublisher.PublishEvents(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit))
            .AndDoes(_ => callOrder.Add("PublishEvents"));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        callOrder.ShouldBe(["SaveChanges", "PublishEvents"]);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSaveChanges_WhenValidationFails()
    {
        // Arrange
        var request = new CreateProductCommand.Request("", "Description", 100m, 10);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
        _unitOfWork.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
    }
}
