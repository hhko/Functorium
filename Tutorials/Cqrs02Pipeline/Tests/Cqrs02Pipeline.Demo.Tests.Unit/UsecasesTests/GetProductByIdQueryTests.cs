using Microsoft.Extensions.Logging;

namespace Cqrs02Pipeline.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// GetProductByIdQuery Handler 테스트
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
            .Returns(Task.FromResult(Fin.Succ(existingProduct)));

        // Act
        FinResponse<GetProductByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ProductId.ShouldBe(productId);
                response.Name.ShouldBe("Test Product");
                response.Price.ShouldBe(100m);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_NonExistingProduct_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new GetProductByIdQuery.Request(productId);

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Product>(Error.New($"Product with ID '{productId}' not found"))));

        // Act
        FinResponse<GetProductByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("not found"));
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
            .Returns(Task.FromResult(Fin.Fail<Product>(expectedError)));

        // Act
        FinResponse<GetProductByIdQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Database connection failed"));
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
            .Returns(Task.FromResult(Fin.Succ(existingProduct)));

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(productId, Arg.Any<CancellationToken>());
    }
}
