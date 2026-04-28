using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Application.Usecases.Products.Queries;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Tests.Unit.Application.Products;

public class GetProductByIdQueryTests
{
    private readonly IProductDetailQuery _adapter = Substitute.For<IProductDetailQuery>();
    private readonly GetProductByIdQuery.Usecase _sut;

    public GetProductByIdQueryTests()
    {
        _sut = new GetProductByIdQuery.Usecase(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnProduct_WhenExists()
    {
        // Arrange
        var productId = ProductId.New();
        var dto = new ProductDetailDto(
            productId.ToString(), "Test Product", "Desc", 100m,
            DateTime.UtcNow, None);

        var request = new GetProductByIdQuery.Request(productId.ToString());

        _adapter.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(dto));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Test Product");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var request = new GetProductByIdQuery.Request(ProductId.New().ToString());

        _adapter.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<ProductDetailDto>(Error.New("Product not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
