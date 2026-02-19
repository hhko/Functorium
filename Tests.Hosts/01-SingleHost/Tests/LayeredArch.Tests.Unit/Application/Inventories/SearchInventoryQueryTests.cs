using FluentValidation;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Application.Usecases.Inventories.Queries;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Inventories;

public class SearchInventoryQueryValidatorTests
{
    private readonly SearchInventoryQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenNoFiltersProvided()
    {
        // Arrange
        var request = new SearchInventoryQuery.Request();

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenValidLowStockThresholdProvided()
    {
        // Arrange
        var request = new SearchInventoryQuery.Request(LowStockThreshold: 10);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenLowStockThresholdIsZero()
    {
        // Arrange — 0은 "필터 없음"이므로 유효
        var request = new SearchInventoryQuery.Request(LowStockThreshold: 0);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenLowStockThresholdIsNegative()
    {
        // Arrange
        var request = new SearchInventoryQuery.Request(LowStockThreshold: -1);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "LowStockThreshold");
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenInvalidSortByProvided()
    {
        // Arrange
        var request = new SearchInventoryQuery.Request(SortBy: "InvalidField");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "SortBy");
    }
}

public class SearchInventoryQueryTests
{
    private readonly IInventoryQuery _readAdapter = Substitute.For<IInventoryQuery>();
    private readonly SearchInventoryQuery.Usecase _sut;

    public SearchInventoryQueryTests()
    {
        _sut = new SearchInventoryQuery.Usecase(_readAdapter);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var items = Seq(
            new InventorySummaryDto(InventoryId.New().ToString(), ProductId.New().ToString(), 100),
            new InventorySummaryDto(InventoryId.New().ToString(), ProductId.New().ToString(), 5));
        var pagedResult = new PagedResult<InventorySummaryDto>(items, 2, 1, 20);
        var request = new SearchInventoryQuery.Request();

        _readAdapter.Search(
                Arg.Any<Specification<Inventory>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Inventories.Count.ShouldBe(2);
        actual.ThrowIfFail().TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenLowStockThresholdProvided()
    {
        // Arrange
        var items = Seq(
            new InventorySummaryDto(InventoryId.New().ToString(), ProductId.New().ToString(), 3));
        var pagedResult = new PagedResult<InventorySummaryDto>(items, 1, 1, 20);
        var request = new SearchInventoryQuery.Request(LowStockThreshold: 10);

        _readAdapter.Search(
                Arg.Any<Specification<Inventory>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Inventories.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ReturnsPaginationMetadata_WhenPageProvided()
    {
        // Arrange
        var items = Seq(
            new InventorySummaryDto(InventoryId.New().ToString(), ProductId.New().ToString(), 50));
        var pagedResult = new PagedResult<InventorySummaryDto>(items, 30, 2, 10);
        var request = new SearchInventoryQuery.Request(Page: 2, PageSize: 10);

        _readAdapter.Search(
                Arg.Any<Specification<Inventory>>(),
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
        response.TotalCount.ShouldBe(30);
        response.TotalPages.ShouldBe(3);
        response.HasPreviousPage.ShouldBeTrue();
        response.HasNextPage.ShouldBeTrue();
    }
}
