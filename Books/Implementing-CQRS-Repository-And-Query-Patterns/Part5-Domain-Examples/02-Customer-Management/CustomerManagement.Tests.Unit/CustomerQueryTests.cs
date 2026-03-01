using System.Collections.Concurrent;
using CustomerManagement;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LanguageExt;

namespace CustomerManagement.Tests.Unit;

public sealed class CustomerQueryTests
{
    private readonly ConcurrentDictionary<CustomerId, Customer> _store = new();
    private readonly InMemoryCustomerQuery _query;

    public CustomerQueryTests()
    {
        _query = new InMemoryCustomerQuery(_store);

        // 테스트 데이터 생성
        var customers = new[]
        {
            Customer.Create("김철수", "kim@example.com", 1_000_000m).ThrowIfFail(),
            Customer.Create("이영희", "lee@example.com", 500_000m).ThrowIfFail(),
            Customer.Create("박민수", "park@example.com", 2_000_000m).ThrowIfFail(),
            Customer.Create("김지영", "kimjy@example.com", 750_000m).ThrowIfFail(),
        };

        foreach (var c in customers)
            _store[c.Id] = c;
    }

    [Fact]
    public async Task Search_AllSpec_ReturnsAll()
    {
        var result = await _query
            .Search(Specification<Customer>.All, new PageRequest(1, 10), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(4);
        paged.Items.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Search_NameSpec_FiltersCorrectly()
    {
        var result = await _query
            .Search(new CustomerNameSpec("김"), new PageRequest(1, 10), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(2);
        paged.Items.ShouldAllBe(dto => dto.Name.Contains("김"));
    }

    [Fact]
    public async Task Search_ComposedSpec_FiltersCorrectly()
    {
        // 이름에 "김" 포함 AND 신용한도 100만 이상
        var spec = new CustomerNameSpec("김")
            & new CreditLimitSpec(1_000_000m);

        var result = await _query
            .Search(spec, new PageRequest(1, 10), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(1);
        paged.Items[0].Name.ShouldBe("김철수");
    }

    [Fact]
    public async Task Search_Pagination_ReturnsCorrectPage()
    {
        var result = await _query
            .Search(Specification<Customer>.All, new PageRequest(1, 2), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(4);
        paged.Items.Count.ShouldBe(2);
        paged.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_SortByCreditLimit_OrdersCorrectly()
    {
        var result = await _query
            .Search(Specification<Customer>.All, new PageRequest(1, 10),
                SortExpression.By("CreditLimit", Functorium.Applications.Queries.SortDirection.Descending))
            .Run().RunAsync();

        var items = result.ThrowIfFail().Items;
        items[0].CreditLimit.ShouldBe(2_000_000m);
        items[^1].CreditLimit.ShouldBe(500_000m);
    }

    /// <summary>
    /// 테스트용 신용한도 Specification.
    /// </summary>
    private sealed class CreditLimitSpec(decimal minLimit) : Specification<Customer>
    {
        public override bool IsSatisfiedBy(Customer entity) =>
            entity.CreditLimit >= minLimit;
    }
}
