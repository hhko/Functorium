using FluentValidation;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Application.Usecases.Products.Queries;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Products;

public class SearchProductsQueryValidatorTests
{
    private readonly SearchProductsQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenNoFiltersProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request();

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenBothPricesProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(MinPrice: 100m, MaxPrice: 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMinPriceProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(MinPrice: 100m);

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
        var request = new SearchProductsQuery.Request(MaxPrice: 200m);

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
        var request = new SearchProductsQuery.Request(SortBy: "Name");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenInvalidSortByProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(SortBy: "InvalidField");

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
        var request = new SearchProductsQuery.Request(SortDirection: "invalid");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "SortDirection");
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenValidNameProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(Name: "Test Product");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenNameIsEmpty()
    {
        // Arrange — empty name means "no filter", should be valid
        var request = new SearchProductsQuery.Request(Name: "");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }
}

public class SearchProductsQueryTests
{
    private readonly IProductQuery _readAdapter = Substitute.For<IProductQuery>();
    private readonly SearchProductsQuery.Usecase _sut;

    public SearchProductsQueryTests()
    {
        _sut = new SearchProductsQuery.Usecase(_readAdapter);
    }

    private static PagedResult<ProductSummaryDto> CreateSamplePagedResult(int totalCount = 3)
    {
        List<ProductSummaryDto> items =
        [
            new(ProductId.New().ToString(), "Cheap Item", 50m),
            new(ProductId.New().ToString(), "Mid Item", 150m),
            new(ProductId.New().ToString(), "Expensive Item", 500m),
        ];

        return new PagedResult<ProductSummaryDto>(items, totalCount, 1, 20);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var pagedResult = CreateSamplePagedResult();
        var request = new SearchProductsQuery.Request();

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
        List<ProductSummaryDto> items = [new(ProductId.New().ToString(), "Mid Item", 150m)];
        var pagedResult = new PagedResult<ProductSummaryDto>(items, 1, 1, 20);
        var request = new SearchProductsQuery.Request(MinPrice: 100m, MaxPrice: 200m);

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
    public async Task Handle_ReturnsSuccess_WhenNameProvided()
    {
        // Arrange
        List<ProductSummaryDto> items = [new(ProductId.New().ToString(), "Test Product", 100m)];
        var pagedResult = new PagedResult<ProductSummaryDto>(items, 1, 1, 20);
        var request = new SearchProductsQuery.Request(Name: "Test Product");

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
        actual.ThrowIfFail().Products[0].Name.ShouldBe("Test Product");
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNameAndPriceRangeProvided()
    {
        // Arrange
        List<ProductSummaryDto> items = [new(ProductId.New().ToString(), "Test Product", 150m)];
        var pagedResult = new PagedResult<ProductSummaryDto>(items, 1, 1, 20);
        var request = new SearchProductsQuery.Request(Name: "Test Product", MinPrice: 100m, MaxPrice: 200m);

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
    }

    [Fact]
    public async Task Handle_ReturnsPaginationMetadata_WhenPageProvided()
    {
        // Arrange
        List<ProductSummaryDto> items = [new(ProductId.New().ToString(), "Item", 100m)];
        var pagedResult = new PagedResult<ProductSummaryDto>(items, 50, 2, 10);
        var request = new SearchProductsQuery.Request(Page: 2, PageSize: 10);

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
