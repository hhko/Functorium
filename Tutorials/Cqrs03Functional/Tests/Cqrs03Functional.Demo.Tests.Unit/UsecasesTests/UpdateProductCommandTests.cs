using Microsoft.Extensions.Logging;

namespace Cqrs03Functional.Demo.Tests.Unit.UsecasesTests;

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

    // 테스트용 헬퍼 메서드 - 실제 IO 실행 없이 FinT를 생성
    private static FinT<IO, T> CreateTestFinT<T>(Fin<T> fin) =>
        FinT.lift<IO, T>(IO.pure(fin));

    private static FinT<IO, T> CreateSuccessFinT<T>(T value) =>
        CreateTestFinT(Fin.Succ(value));

    private static FinT<IO, T> CreateFailureFinT<T>(Error error) =>
        CreateTestFinT(Fin.Fail<T>(error));

    private static Product CreateTestProduct(string name = "Old Name", string description = "Old Desc", decimal price = 100m, int stockQuantity = 10)
    {
        var productName = ProductName.Create(name).IfFail(_ => throw new Exception());
        var priceVo = Price.Create(price).IfFail(_ => throw new Exception());
        var stockQuantityVo = StockQuantity.Create(stockQuantity).IfFail(_ => throw new Exception());
        return Product.Create(productName, description, priceVo, stockQuantityVo)
            .IfFail(_ => throw new Exception());
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var request = new UpdateProductCommand.Request(existingProduct.Id.ToString(), "New Name", "New Desc", 200m, 20);

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateSuccessFinT(existingProduct));

        _productRepository
            .Update(Arg.Any<Product>())
            .Returns(callInfo =>
            {
                var product = callInfo.Arg<Product>();
                return CreateSuccessFinT(product);
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
        var productId = ProductId.New();
        var request = new UpdateProductCommand.Request(productId.ToString(), "New Name", "New Desc", 200m, 20);

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateFailureFinT<Product>(Error.New($"Product with ID '{productId}' not found")));

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
        var productId = ProductId.New();
        var request = new UpdateProductCommand.Request(productId.ToString(), "New Name", "New Desc", 200m, 20, SimulateException: true);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.Handle(request, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GetFails_ReturnsFailure()
    {
        // Arrange
        var productId = ProductId.New();
        var request = new UpdateProductCommand.Request(productId.ToString(), "New Name", "New Desc", 200m, 20);
        var expectedError = Error.New("Database error");

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateFailureFinT<Product>(expectedError));

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
        var existingProduct = CreateTestProduct();
        var request = new UpdateProductCommand.Request(existingProduct.Id.ToString(), "New Name", "New Desc", 200m, 20);
        var expectedError = Error.New("Update failed");

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateSuccessFinT(existingProduct));

        _productRepository
            .Update(Arg.Any<Product>())
            .Returns(CreateFailureFinT<Product>(expectedError));

        // Act
        FinResponse<UpdateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Update failed"));
    }

    [Fact]
    public async Task Handle_InvalidProductId_ReturnsFailure()
    {
        // Arrange
        var request = new UpdateProductCommand.Request("invalid-id", "New Name", "New Desc", 200m, 20);

        // Act
        FinResponse<UpdateProductCommand.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("Invalid ProductId format"));
    }
}
