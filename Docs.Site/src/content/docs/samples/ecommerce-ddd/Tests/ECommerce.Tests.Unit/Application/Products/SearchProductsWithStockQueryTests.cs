using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Application.Usecases.Products.Queries;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Tests.Unit.Application.Products;

public class SearchProductsWithStockQueryValidatorTests
{
    private readonly SearchProductsWithStockQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenNoFiltersProvided()
    {
        // Arrange
        var request = new SearchProductsWithStockQuery.Request();

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenBothPricesProvided()
    {
        // Arrange
        var request = new SearchProductsWithStockQuery.Request(MinPrice: 100m, MaxPrice: 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMinPriceProvided()
    {
        // Arrange
        var request = new SearchProductsWithStockQuery.Request(MinPrice: 100m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e =>
            e.PropertyName == "MaxPrice"
            && e.ErrorMessage.Contains("MaxPrice is required when MinPrice is specified"));
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMaxPriceProvided()
    {
        // Arrange
        var request = new SearchProductsWithStockQuery.Request(MaxPrice: 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e =>
            e.PropertyName == "MinPrice"
            && e.ErrorMessage.Contains("MinPrice is required when MaxPrice is specified"));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenValidSortByProvided()
    {
        // Arrange
        var request = new SearchProductsWithStockQuery.Request(SortBy: "StockQuantity");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenInvalidSortByProvided()
    {
        // Arrange
        var request = new SearchProductsWithStockQuery.Request(SortBy: "InvalidField");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "SortBy");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenInvalidSortDirectionProvided()
    {
        // Arrange
        var request = new SearchProductsWithStockQuery.Request(SortDirection: "invalid");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "SortDirection");
    }
}

public class SearchProductsWithStockQueryTests
{
    private readonly IProductWithStockQuery _readAdapter = Substitute.For<IProductWithStockQuery>();
    private readonly SearchProductsWithStockQuery.Usecase _sut;

    public SearchProductsWithStockQueryTests()
    {
        _sut = new SearchProductsWithStockQuery.Usecase(_readAdapter);
    }

    private static PagedResult<ProductWithStockDto> CreateSamplePagedResult(int totalCount = 3)
    {
        List<ProductWithStockDto> items =
        [
            new(ProductId.New().ToString(), "Cheap Item", 50m, 10),
            new(ProductId.New().ToString(), "Mid Item", 150m, 5),
            new(ProductId.New().ToString(), "Expensive Item", 500m, 2),
        ];

        return new PagedResult<ProductWithStockDto>(items, totalCount, 1, 20);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var pagedResult = CreateSamplePagedResult();
        var request = new SearchProductsWithStockQuery.Request();

        _readAdapter.Search(
                Arg.Any<Specification<Product>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(3);
        actual.ThrowIfFail().TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPriceRangeProvided()
    {
        // Arrange
        List<ProductWithStockDto> items = [new(ProductId.New().ToString(), "Mid Item", 150m, 5)];
        var pagedResult = new PagedResult<ProductWithStockDto>(items, 1, 1, 20);
        var request = new SearchProductsWithStockQuery.Request(MinPrice: 100m, MaxPrice: 200m);

        _readAdapter.Search(
                Arg.Any<Specification<Product>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(1);
        actual.ThrowIfFail().Products[0].Name.ShouldBe("Mid Item");
    }

    [Fact]
    public async Task Handle_ReturnsPaginationMetadata_WhenPageProvided()
    {
        // Arrange
        List<ProductWithStockDto> items = [new(ProductId.New().ToString(), "Item", 100m, 10)];
        var pagedResult = new PagedResult<ProductWithStockDto>(items, 50, 2, 10);
        var request = new SearchProductsWithStockQuery.Request(Page: 2, PageSize: 10);

        _readAdapter.Search(
                Arg.Any<Specification<Product>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var response = actual.ThrowIfFail();
        response.Page.ShouldBe(2);
        response.PageSize.ShouldBe(10);
        response.TotalCount.ShouldBe(50);
        response.TotalPages.ShouldBe(5);
        response.HasPreviousPage.ShouldBeTrue();
        response.HasNextPage.ShouldBeTrue();
    }
}
