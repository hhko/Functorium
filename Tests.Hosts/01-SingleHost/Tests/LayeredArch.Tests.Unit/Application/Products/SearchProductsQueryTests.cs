using FluentValidation;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Application.Usecases.Products.Dtos;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class SearchProductsQueryValidatorTests
{
    private readonly SearchProductsQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenNoPricesProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(null, null);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenBothPricesProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(100m, 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMinPriceProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(100m, null);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e =>
            e.PropertyName == "MaxPrice"
            && e.ErrorMessage.Contains("최소 가격을 지정할 때는 최대 가격도 함께 지정해야 합니다"));
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMaxPriceProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(null, 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e =>
            e.PropertyName == "MinPrice"
            && e.ErrorMessage.Contains("최대 가격을 지정할 때는 최소 가격도 함께 지정해야 합니다"));
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenValidSortByProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(null, null, SortBy: "Name");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenInvalidSortByProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(null, null, SortBy: "InvalidField");

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
        var request = new SearchProductsQuery.Request(null, null, SortDirection: "invalid");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "SortDirection");
    }
}

public class SearchProductsQueryTests
{
    private readonly IProductQueryAdapter _readAdapter = Substitute.For<IProductQueryAdapter>();
    private readonly SearchProductsQuery.Usecase _sut;

    public SearchProductsQueryTests()
    {
        _sut = new SearchProductsQuery.Usecase(_readAdapter);
    }

    private static PagedResult<ProductSummaryDto> CreateSamplePagedResult(int totalCount = 3)
    {
        var items = Seq(
            new ProductSummaryDto(ProductId.New().ToString(), "Cheap Item", 50m),
            new ProductSummaryDto(ProductId.New().ToString(), "Mid Item", 150m),
            new ProductSummaryDto(ProductId.New().ToString(), "Expensive Item", 500m));

        return new PagedResult<ProductSummaryDto>(items, totalCount, 1, 20);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var pagedResult = CreateSamplePagedResult();
        var request = new SearchProductsQuery.Request(null, null);

        _readAdapter.Search(
                Arg.Any<Specification<Product>?>(),
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
        var items = Seq(new ProductSummaryDto(ProductId.New().ToString(), "Mid Item", 150m));
        var pagedResult = new PagedResult<ProductSummaryDto>(items, 1, 1, 20);
        var request = new SearchProductsQuery.Request(100m, 200m);

        _readAdapter.Search(
                Arg.Any<Specification<Product>?>(),
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
        var items = Seq(new ProductSummaryDto(ProductId.New().ToString(), "Item", 100m));
        var pagedResult = new PagedResult<ProductSummaryDto>(items, 50, 2, 10);
        var request = new SearchProductsQuery.Request(null, null, Page: 2, PageSize: 10);

        _readAdapter.Search(
                Arg.Any<Specification<Product>?>(),
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
