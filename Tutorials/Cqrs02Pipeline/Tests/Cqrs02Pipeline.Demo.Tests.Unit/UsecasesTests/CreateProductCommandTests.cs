using Microsoft.Extensions.Logging;

namespace Cqrs02Pipeline.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// CreateProductCommand Handler 테스트
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
        FinResponse<CreateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.Name.ShouldBe("Test Product");
                response.Price.ShouldBe(100m);
                response.StockQuantity.ShouldBe(10);
            },
            Fail: _ => throw new Exception("Should be success"));
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
        FinResponse<CreateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("이미 존재합니다"));
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
        FinResponse<CreateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Database connection failed"));
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
        FinResponse<CreateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Failed to create product"));
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
