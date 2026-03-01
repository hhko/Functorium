using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Application.Usecases.Products.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Products;

public class GetAllProductsQueryTests
{
    private readonly IProductQuery _productQuery = Substitute.For<IProductQuery>();
    private readonly GetAllProductsQuery.Usecase _sut;

    public GetAllProductsQueryTests()
    {
        _sut = new GetAllProductsQuery.Usecase(_productQuery);
    }

    [Fact]
    public async Task Handle_ReturnsProducts_WhenProductsExist()
    {
        // Arrange
        List<ProductSummaryDto> items =
        [
            new(ProductId.New().ToString(), "Product A", 100m),
            new(ProductId.New().ToString(), "Product B", 200m),
        ];
        var pagedResult = new PagedResult<ProductSummaryDto>(items, 2, 1, int.MaxValue);

        var request = new GetAllProductsQuery.Request();

        _productQuery.Search(
                Arg.Any<Specification<Product>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(2);
    }
}
