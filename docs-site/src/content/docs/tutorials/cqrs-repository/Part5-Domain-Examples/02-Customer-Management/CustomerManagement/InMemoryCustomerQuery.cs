using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;

namespace CustomerManagement;

/// <summary>
/// 고객 InMemory Query Adapter.
/// Specification 기반 필터링과 페이지네이션을 제공합니다.
/// </summary>
public sealed class InMemoryCustomerQuery(
    ConcurrentDictionary<CustomerId, Customer> store) : InMemoryQueryBase<Customer, CustomerDto>
{
    protected override string DefaultSortField => "Name";

    protected override IEnumerable<CustomerDto> GetProjectedItems(Specification<Customer> spec) =>
        store.Values
            .Where(c => spec.IsSatisfiedBy(c))
            .Select(c => new CustomerDto(c.Id.ToString(), c.Name, c.Email, c.CreditLimit));

    protected override Func<CustomerDto, object> SortSelector(string fieldName) =>
        fieldName.ToUpperInvariant() switch
        {
            "NAME" => dto => dto.Name,
            "EMAIL" => dto => dto.Email,
            "CREDITLIMIT" => dto => dto.CreditLimit,
            _ => dto => dto.Name
        };
}
