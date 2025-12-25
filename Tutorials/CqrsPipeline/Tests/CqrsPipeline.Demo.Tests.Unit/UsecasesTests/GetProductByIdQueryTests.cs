using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// GetProductByIdQuery Usecase 테스트
/// </summary>
public sealed class GetProductByIdQueryTests
{
    private readonly ILogger<GetProductByIdQuery.Usecase> _logger;
    private readonly IProductRepository _productRepository;
    private readonly GetProductByIdQuery.Usecase _sut;

    public GetProductByIdQueryTests()
    {
        _logger = Substitute.For<ILogger<GetProductByIdQuery.Usecase>>();
        _productRepository = Substitute.For<IProductRepository>();
        _sut = new GetProductByIdQuery.Usecase(_logger, _productRepository);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsSuccessResponse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId, "Test Product", "Description", 100m, 10, DateTime.UtcNow);
        var request = new GetProductByIdQuery.Request(productId);

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<Product?>(existingProduct)));

        // Act
        GetProductByIdQuery.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.ProductId.ShouldBe(productId);
        result.Name.ShouldBe("Test Product");
        result.Price.ShouldBe(100m);
    }

    [Fact]
    public async Task Handle_NonExistingProduct_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new GetProductByIdQuery.Request(productId);

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<Product?>(null)));

        // Act
        GetProductByIdQuery.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldContain("찾을 수 없습니다");
    }

    [Fact]
    public async Task Handle_RepositoryFails_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new GetProductByIdQuery.Request(productId);
        var expectedError = Error.New("Database connection failed");

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Product?>(expectedError)));

        // Act
        GetProductByIdQuery.Response result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldBe("Database connection failed");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId, "Test Product", "Description", 100m, 10, DateTime.UtcNow);
        var request = new GetProductByIdQuery.Request(productId);

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<Product?>(existingProduct)));

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(productId, Arg.Any<CancellationToken>());
    }
}
