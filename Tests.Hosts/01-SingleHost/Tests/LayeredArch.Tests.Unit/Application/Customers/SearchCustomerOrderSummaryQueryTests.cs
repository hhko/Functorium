using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Application.Usecases.Customers.Queries;
using LayeredArch.Domain.AggregateRoots.Customers;

namespace LayeredArch.Tests.Unit.Application.Customers;

public class SearchCustomerOrderSummaryQueryValidatorTests
{
    private readonly SearchCustomerOrderSummaryQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenNoFiltersProvided()
    {
        // Arrange
        var request = new SearchCustomerOrderSummaryQuery.Request();

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenValidSortByProvided()
    {
        // Arrange
        var request = new SearchCustomerOrderSummaryQuery.Request(SortBy: "TotalSpent");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenInvalidSortByProvided()
    {
        // Arrange
        var request = new SearchCustomerOrderSummaryQuery.Request(SortBy: "InvalidField");

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
        var request = new SearchCustomerOrderSummaryQuery.Request(SortDirection: "invalid");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "SortDirection");
    }
}

public class SearchCustomerOrderSummaryQueryTests
{
    private readonly ICustomerOrderSummaryQuery _readAdapter = Substitute.For<ICustomerOrderSummaryQuery>();
    private readonly SearchCustomerOrderSummaryQuery.Usecase _sut;

    public SearchCustomerOrderSummaryQueryTests()
    {
        _sut = new SearchCustomerOrderSummaryQuery.Usecase(_readAdapter);
    }

    private static PagedResult<CustomerOrderSummaryDto> CreateSamplePagedResult(int totalCount = 3)
    {
        var items = Seq(
            new CustomerOrderSummaryDto(CustomerId.New().ToString(), "Alice", 5, 1500m, DateTime.UtcNow.AddDays(-1)),
            new CustomerOrderSummaryDto(CustomerId.New().ToString(), "Bob", 0, 0m, null),
            new CustomerOrderSummaryDto(CustomerId.New().ToString(), "Charlie", 3, 750m, DateTime.UtcNow.AddDays(-7)));

        return new PagedResult<CustomerOrderSummaryDto>(items, totalCount, 1, 20);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var pagedResult = CreateSamplePagedResult();
        var request = new SearchCustomerOrderSummaryQuery.Request();

        _readAdapter.Search(
                Arg.Any<Specification<Customer>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Customers.Count.ShouldBe(3);
        actual.ThrowIfFail().TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_ReturnsCustomerWithNoOrders_WhenCustomerHasZeroOrders()
    {
        // Arrange
        var items = Seq(
            new CustomerOrderSummaryDto(CustomerId.New().ToString(), "New Customer", 0, 0m, null));
        var pagedResult = new PagedResult<CustomerOrderSummaryDto>(items, 1, 1, 20);
        var request = new SearchCustomerOrderSummaryQuery.Request();

        _readAdapter.Search(
                Arg.Any<Specification<Customer>>(),
                Arg.Any<PageRequest>(),
                Arg.Any<SortExpression>())
            .Returns(FinTFactory.Succ(pagedResult));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var customer = actual.ThrowIfFail().Customers[0];
        customer.OrderCount.ShouldBe(0);
        customer.TotalSpent.ShouldBe(0m);
        customer.LastOrderDate.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsPaginationMetadata_WhenPageProvided()
    {
        // Arrange
        var items = Seq(
            new CustomerOrderSummaryDto(CustomerId.New().ToString(), "Alice", 5, 1500m, DateTime.UtcNow));
        var pagedResult = new PagedResult<CustomerOrderSummaryDto>(items, 50, 2, 10);
        var request = new SearchCustomerOrderSummaryQuery.Request(Page: 2, PageSize: 10);

        _readAdapter.Search(
                Arg.Any<Specification<Customer>>(),
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
