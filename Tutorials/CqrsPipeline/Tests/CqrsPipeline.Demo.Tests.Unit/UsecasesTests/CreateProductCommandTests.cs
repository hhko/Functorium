using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// CreateProductCommand Usecase 테스트
/// </summary>
public sealed class CreateProductCommandTests
{
    private readonly ILogger<CreateProductCommand.Usecase> _logger;
    private readonly IProductRepository _productRepository;
    private readonly CreateProductCommand.Usecase _sut;

    public CreateProductCommandTests()
    {
        _logger = Substitute.For<ILogger<CreateProductCommand.Usecase>>();
        _productRepository = Substitute.For<IProductRepository>();
        _sut = new CreateProductCommand.Usecase(_logger, _productRepository);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        _productRepository
            .ExistsByNameAsync(request.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(false)));

        _productRepository
            .CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var product = callInfo.Arg<Product>();
                return Task.FromResult(Fin.Succ(product));
            });

        // Act
        IFinResponse<CreateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Test Product");
        result.Value.Price.ShouldBe(100m);
        result.Value.StockQuantity.ShouldBe(10);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Existing Product", "Description", 100m, 10);

        _productRepository
            .ExistsByNameAsync(request.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(true)));

        // Act
        IFinResponse<CreateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldContain("이미 존재합니다");
    }

    [Fact]
    public async Task Handle_NameCheckFails_ReturnsFailure()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);
        var expectedError = Error.New("Database connection failed");

        _productRepository
            .ExistsByNameAsync(request.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<bool>(expectedError)));

        // Act
        IFinResponse<CreateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldBe("Database connection failed");
    }

    [Fact]
    public async Task Handle_CreateFails_ReturnsFailure()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);
        var expectedError = Error.New("Failed to create product");

        _productRepository
            .ExistsByNameAsync(request.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(false)));

        _productRepository
            .CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Product>(expectedError)));

        // Act
        IFinResponse<CreateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldBe("Failed to create product");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var request = new CreateProductCommand.Request("New Product", "New Description", 200m, 50);

        _productRepository
            .ExistsByNameAsync(request.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(false)));

        _productRepository
            .CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var product = callInfo.Arg<Product>();
                return Task.FromResult(Fin.Succ(product));
            });

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _productRepository.Received(1).ExistsByNameAsync("New Product", Arg.Any<CancellationToken>());
        await _productRepository.Received(1).CreateAsync(
            Arg.Is<Product>(p =>
                p.Name == "New Product" &&
                p.Description == "New Description" &&
                p.Price == 200m &&
                p.StockQuantity == 50),
            Arg.Any<CancellationToken>());
    }
}
