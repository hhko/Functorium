using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using LanguageExt;

namespace Cqrs03Functional.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// GetAllProductsQuery Handler 테스트
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

        // 테스트용 헬퍼 메서드 - 실제 IO 실행 없이 FinT를 생성
        private static FinT<IO, T> CreateTestFinT<T>(Fin<T> fin) =>
            FinT.lift<IO, T>(IO.pure(fin));

        private static FinT<IO, T> CreateSuccessFinT<T>(T value) =>
            CreateTestFinT(Fin.Succ(value));

        private static FinT<IO, T> CreateFailureFinT<T>(Error error) =>
            CreateTestFinT(Fin.Fail<T>(error));

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
            .GetAll()
            .Returns(CreateSuccessFinT(products));

        // Act
        FinResponse<GetAllProductsQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response => response.Products.Count.ShouldBe(3),
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        var request = new GetAllProductsQuery.Request();

        _productRepository
            .GetAll()
            .Returns(CreateSuccessFinT(LanguageExt.Seq<Product>.Empty));

        // Act
        FinResponse<GetAllProductsQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response => response.Products.Count.ShouldBe(0),
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_RepositoryFails_ReturnsFailure()
    {
        // Arrange
        var request = new GetAllProductsQuery.Request();
        var expectedError = Error.New("Database connection failed");

        _productRepository
            .GetAll()
            .Returns(CreateFailureFinT<LanguageExt.Seq<Product>>(expectedError));

        // Act
        FinResponse<GetAllProductsQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldBe("Database connection failed"));
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
            .GetAll()
            .Returns(CreateSuccessFinT(products));

        // Act
        FinResponse<GetAllProductsQuery.Response> actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                var dto = response.Products.First();
                dto.ProductId.ShouldBe(productId);
                dto.Name.ShouldBe("Test Product");
                dto.Price.ShouldBe(150m);
                dto.StockQuantity.ShouldBe(25);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepository()
    {
        // Arrange
        var request = new GetAllProductsQuery.Request();

        _productRepository
            .GetAll()
            .Returns(CreateSuccessFinT(LanguageExt.Seq<Product>.Empty));

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        _productRepository.Received(1).GetAll();
    }
}
