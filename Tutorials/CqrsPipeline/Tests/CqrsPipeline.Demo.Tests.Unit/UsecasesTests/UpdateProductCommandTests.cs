using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// UpdateProductCommand Usecase 테스트
/// </summary>
public sealed class UpdateProductCommandTests
{
    private readonly ILogger<UpdateProductCommand.Usecase> _logger;
    private readonly IProductRepository _productRepository;
    private readonly UpdateProductCommand.Usecase _sut;

    public UpdateProductCommandTests()
    {
        _logger = Substitute.For<ILogger<UpdateProductCommand.Usecase>>();
        _productRepository = Substitute.For<IProductRepository>();
        _sut = new UpdateProductCommand.Usecase(_logger, _productRepository);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId, "Old Name", "Old Desc", 100m, 10, DateTime.UtcNow);
        var request = new UpdateProductCommand.Request(productId, "New Name", "New Desc", 200m, 20);

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<Product?>(existingProduct)));

        _productRepository
            .UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var product = callInfo.Arg<Product>();
                return Task.FromResult(Fin.Succ(product));
            });

        // Act
        IFinResponse<UpdateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.Name.ShouldBe("New Name");
        result.Value.Price.ShouldBe(200m);
        result.Value.StockQuantity.ShouldBe(20);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductCommand.Request(productId, "New Name", "New Desc", 200m, 20);

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<Product?>(null)));

        // Act
        IFinResponse<UpdateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldContain("찾을 수 없습니다");
    }

    [Fact]
    public async Task Handle_SimulateException_ThrowsException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductCommand.Request(productId, "New Name", "New Desc", 200m, 20, SimulateException: true);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.Handle(request, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GetFails_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductCommand.Request(productId, "New Name", "New Desc", 200m, 20);
        var expectedError = Error.New("Database error");

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Product?>(expectedError)));

        // Act
        IFinResponse<UpdateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldBe("Database error");
    }

    [Fact]
    public async Task Handle_UpdateFails_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId, "Old Name", "Old Desc", 100m, 10, DateTime.UtcNow);
        var request = new UpdateProductCommand.Request(productId, "New Name", "New Desc", 200m, 20);
        var expectedError = Error.New("Update failed");

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Succ<Product?>(existingProduct)));

        _productRepository
            .UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Product>(expectedError)));

        // Act
        IFinResponse<UpdateProductCommand.Response> result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldBe("Update failed");
    }
}
