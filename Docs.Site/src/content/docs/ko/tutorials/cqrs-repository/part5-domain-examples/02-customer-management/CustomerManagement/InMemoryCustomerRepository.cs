using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using LanguageExt;

namespace CustomerManagement;

/// <summary>
/// 고객 InMemory Repository 구현.
/// Specification 기반 Exists를 추가 구현합니다.
/// </summary>
public sealed class InMemoryCustomerRepository : InMemoryRepositoryBase<Customer, CustomerId>, ICustomerRepository
{
    private readonly ConcurrentDictionary<CustomerId, Customer> _store = new();
    protected override ConcurrentDictionary<CustomerId, Customer> Store => _store;

    public InMemoryCustomerRepository(IDomainEventCollector eventCollector) : base(eventCollector) { }

    public FinT<IO, bool> Exists(Specification<Customer> spec)
    {
        return IO.lift(() =>
        {
            var exists = _store.Values.Any(c => spec.IsSatisfiedBy(c));
            return Fin.Succ(exists);
        });
    }
}
