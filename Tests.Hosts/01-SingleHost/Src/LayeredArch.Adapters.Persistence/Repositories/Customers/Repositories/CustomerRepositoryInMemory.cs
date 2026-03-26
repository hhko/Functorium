using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Customers;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;

namespace LayeredArch.Adapters.Persistence.Repositories.Customers.Repositories;

/// <summary>
/// 메모리 기반 고객 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class CustomerRepositoryInMemory
    : InMemoryRepositoryBase<Customer, CustomerId>, ICustomerRepository
{
    internal static readonly ConcurrentDictionary<CustomerId, Customer> Customers = new();
    protected override ConcurrentDictionary<CustomerId, Customer> Store => Customers;

    public CustomerRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // ─── Customer 고유 메서드 ────────────────────────

    public virtual FinT<IO, bool> Exists(Specification<Customer> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Customers.Values.Any(c => spec.IsSatisfiedBy(c));
            return Fin.Succ(exists);
        });
    }
}
