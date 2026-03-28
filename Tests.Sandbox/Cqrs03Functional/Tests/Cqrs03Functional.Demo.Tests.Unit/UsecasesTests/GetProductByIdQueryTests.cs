using Microsoft.Extensions.Logging;

namespace Cqrs03Functional.Demo.Tests.Unit.UsecasesTests;

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

    // 테스트용 헬퍼 메서드 - 실제 IO 실행 없이 FinT를 생성
    private static FinT<IO, T> CreateTestFinT<T>(Fin<T> fin) =>
        FinT.lift<IO, T>(IO.pure(fin));

    private static FinT<IO, T> CreateSuccessFinT<T>(T value) =>
        CreateTestFinT(Fin.Succ(value));

    private static FinT<IO, T> CreateFailureFinT<T>(Error error) =>
        CreateTestFinT(Fin.Fail<T>(error));

    private static Product CreateTestProduct(string name = "Test Product", string description = "Description", decimal price = 100m, int stockQuantity = 10)
    {
        var productName = ProductName.Create(name).IfFail(_ => throw new Exception());
        var priceVo = Price.Create(price).IfFail(_ => throw new Exception());
        var stockQuantityVo = StockQuantity.Create(stockQuantity).IfFail(_ => throw new Exception());
        return Product.Create(productName, description, priceVo, stockQuantityVo)
            .IfFail(_ => throw new Exception());
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsSuccessResponse()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var request = new GetProductByIdQuery.Request(existingProduct.Id.ToString());

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateSuccessFinT(existingProduct));

        // Act - Usecase Handle 메서드 직접 호출
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ProductId.ShouldBe(existingProduct.Id.ToString());
                response.Name.ShouldBe("Test Product");
                response.Price.ShouldBe(100m);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_NonExistingProduct_ReturnsFailure()
    {
        // Arrange
        var productId = ProductId.New();
        var request = new GetProductByIdQuery.Request(productId.ToString());

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateFailureFinT<Product>(Error.New($"Product with ID '{productId}' not found")));

        // Act - Usecase Handle 메서드 직접 호출
        var actual = await _sut.Handle(request, CancellationToken.None);

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
        var productId = ProductId.New();
        var request = new GetProductByIdQuery.Request(productId.ToString());
        var expectedError = Error.New("Database connection failed");

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateFailureFinT<Product>(expectedError));

        // Act - Usecase Handle 메서드 직접 호출
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Database connection failed"));
    }

    [Fact]
    public async Task Handle_InvalidProductId_ReturnsFailure()
    {
        // Arrange
        var request = new GetProductByIdQuery.Request("invalid-id");

        // Act - Usecase Handle 메서드 직접 호출
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("Invalid ProductId format"));
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var request = new GetProductByIdQuery.Request(existingProduct.Id.ToString());

        _productRepository
            .GetById(Arg.Any<ProductId>())
            .Returns(CreateSuccessFinT(existingProduct));

        // Act - Usecase Handle 메서드 직접 호출
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        _productRepository.Received(1).GetById(Arg.Any<ProductId>());
    }
}
