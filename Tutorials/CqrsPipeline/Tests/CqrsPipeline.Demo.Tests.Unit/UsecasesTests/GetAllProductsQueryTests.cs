using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// GetAllProductsQuery Usecase 테스트
/// </summary>
public sealed class GetAllProductsQueryTests
{
    private readonly ILogger<GetAllProductsQuery.Usecase> _logger;
    private readonly IProductRepository _productRepository;
    private readonly GetAllProductsQuery.Usecase _sut;

    public GetAllProductsQueryTests()
    {
        _logger = Substitute.For<ILogger<GetAllProductsQuery.Usecase>>();
        _productRepository = Substitute.For<IProductRepository>();
        _sut = new GetAllProductsQuery.Usecase(_logger, _productRepository);
    }

    [Fact]
    public async Task Handle_ProductsExist_ReturnsAllProducts()
    {
        // Arrange
        var products = toSeq(new[]
        {
            new Product(Guid.NewGuid(), "Product 1", "Desc 1", 100m, 10, DateTime.UtcNow),
            new Product(Guid.NewGuid(), "Product 2", "Desc 2", 200m, 20, DateTime.UtcNow),
            new Product(Guid.NewGuid(), "Product 3", "Desc 3", 300m, 30, DateTime.UtcNow)
        });
        var request = new GetAllProductsQuery.Request();

        _productRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(products)));

        // Act
        GetAllProductsQuery.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Products.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        var request = new GetAllProductsQuery.Request();

        _productRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(LanguageExt.Seq<Product>.Empty)));

        // Act
        GetAllProductsQuery.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Products.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_RepositoryFails_ReturnsFailure()
    {
        // Arrange
        var request = new GetAllProductsQuery.Request();
        var expectedError = Error.New("Database connection failed");

        _productRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<LanguageExt.Seq<Product>>(expectedError)));

        // Act
        GetAllProductsQuery.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldBe("Database connection failed");
    }

    [Fact]
    public async Task Handle_MapsProductToDto_Correctly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var products = toSeq(new[]
        {
            new Product(productId, "Test Product", "Test Description", 150m, 25, DateTime.UtcNow)
        });
        var request = new GetAllProductsQuery.Request();

        _productRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(products)));

        // Act
        GetAllProductsQuery.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dto = result.Products.First();
        dto.ProductId.ShouldBe(productId);
        dto.Name.ShouldBe("Test Product");
        dto.Price.ShouldBe(150m);
        dto.StockQuantity.ShouldBe(25);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepository()
    {
        // Arrange
        var request = new GetAllProductsQuery.Request();

        _productRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ(LanguageExt.Seq<Product>.Empty)));

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _productRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
