using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Application.Usecases.Products.Queries;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Tests.Unit.Application.Products;

public class SearchProductsWithOptionalStockQueryValidatorTests
{
    private readonly SearchProductsWithOptionalStockQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenNoFiltersProvided()
    {
        // Arrange
        var request = new SearchProductsWithOptionalStockQuery.Request();

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenBothPricesProvided()
    {
        // Arrange
        var request = new SearchProductsWithOptionalStockQuery.Request(MinPrice: 100m, MaxPrice: 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMinPriceProvided()
    {
        // Arrange
        var request = new SearchProductsWithOptionalStockQuery.Request(MinPrice: 100m);

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
        var request = new SearchProductsWithOptionalStockQuery.Request(MaxPrice: 200m);

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
        var request = new SearchProductsWithOptionalStockQuery.Request(SortBy: "StockQuantity");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenInvalidSortByProvided()
    {
        // Arrange
        var request = new SearchProductsWithOptionalStockQuery.Request(SortBy: "InvalidField");

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
        var request = new SearchProductsWithOptionalStockQuery.Request(SortDirection: "invalid");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "SortDirection");
    }
}

public class SearchProductsWithOptionalStockQueryTests
{
    private readonly IProductWithOptionalStockQuery _readAdapter = Substitute.For<IProductWithOptionalStockQuery>();
    private readonly SearchProductsWithOptionalStockQuery.Usecase _sut;

    public SearchProductsWithOptionalStockQueryTests()
    {
        _sut = new SearchProductsWithOptionalStockQuery.Usecase(_readAdapter);
    }

    private static PagedResult<ProductWithOptionalStockDto> CreateSamplePagedResult(int totalCount = 3)
    {
        List<ProductWithOptionalStockDto> items =
        [
            new(ProductId.New().ToString(), "Cheap Item", 50m, 10),
            new(ProductId.New().ToString(), "Mid Item", 150m, null),
            new(ProductId.New().ToString(), "Expensive Item", 500m, 2),
        ];

        return new PagedResult<ProductWithOptionalStockDto>(items, totalCount, 1, 20);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var pagedResult = CreateSamplePagedResult();
        var request = new SearchProductsWithOptionalStockQuery.Request();

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
    public async Task Handle_ReturnsProductsWithNullStock_WhenNoInventoryExists()
    {
        // Arrange
        List<ProductWithOptionalStockDto> items =
            [new(ProductId.New().ToString(), "No Stock Item", 100m, null)];
        var pagedResult = new PagedResult<ProductWithOptionalStockDto>(items, 1, 1, 20);
        var request = new SearchProductsWithOptionalStockQuery.Request();

        _readAdapter.Search(
                Arg.Any<Specification<Product>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products[0].StockQuantity.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPriceRangeProvided()
    {
        // Arrange
        List<ProductWithOptionalStockDto> items = [new(ProductId.New().ToString(), "Mid Item", 150m, 5)];
        var pagedResult = new PagedResult<ProductWithOptionalStockDto>(items, 1, 1, 20);
        var request = new SearchProductsWithOptionalStockQuery.Request(MinPrice: 100m, MaxPrice: 200m);

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
        List<ProductWithOptionalStockDto> items = [new(ProductId.New().ToString(), "Item", 100m, 10)];
        var pagedResult = new PagedResult<ProductWithOptionalStockDto>(items, 50, 2, 10);
        var request = new SearchProductsWithOptionalStockQuery.Request(Page: 2, PageSize: 10);

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
