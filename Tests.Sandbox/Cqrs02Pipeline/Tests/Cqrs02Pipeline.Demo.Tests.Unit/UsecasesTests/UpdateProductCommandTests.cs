using Microsoft.Extensions.Logging;

namespace Cqrs02Pipeline.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// UpdateProductCommand Handler 테스트
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
            .Returns(Task.FromResult(Fin.Succ(existingProduct)));

        _productRepository
            .UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var product = callInfo.Arg<Product>();
                return Task.FromResult(Fin.Succ(product));
            });

        // Act
        FinResponse<UpdateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.Name.ShouldBe("New Name");
                response.Price.ShouldBe(200m);
                response.StockQuantity.ShouldBe(20);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductCommand.Request(productId, "New Name", "New Desc", 200m, 20);

        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Product>(Error.New($"Product with ID '{productId}' not found"))));

        // Act
        FinResponse<UpdateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("not found"));
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
            .Returns(Task.FromResult(Fin.Fail<Product>(expectedError)));

        // Act
        FinResponse<UpdateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Database error"));
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
            .Returns(Task.FromResult(Fin.Succ(existingProduct)));

        _productRepository
            .UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Fin.Fail<Product>(expectedError)));

        // Act
        FinResponse<UpdateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Update failed"));
    }
}
