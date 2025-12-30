using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using LanguageExt;

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

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsSuccessResponse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId, "Test Product", "Description", 100m, 10, DateTime.UtcNow);
        var request = new GetProductByIdQuery.Request(productId);

        _productRepository
            .GetById(productId)
            .Returns(CreateSuccessFinT(existingProduct));

        // Act - Usecase Handle 메서드 직접 호출
        var actual = await _sut.Handle(request, CancellationToken.None);

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
            .GetById(productId)
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
        var productId = Guid.NewGuid();
        var request = new GetProductByIdQuery.Request(productId);
        var expectedError = Error.New("Database connection failed");

        _productRepository
            .GetById(productId)
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
    public async Task Handle_ValidRequest_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId, "Test Product", "Description", 100m, 10, DateTime.UtcNow);
        var request = new GetProductByIdQuery.Request(productId);

        _productRepository
            .GetById(productId)
            .Returns(CreateSuccessFinT(existingProduct));

        // Act - Usecase Handle 메서드 직접 호출
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        _productRepository.Received(1).GetById(productId);
    }
}
