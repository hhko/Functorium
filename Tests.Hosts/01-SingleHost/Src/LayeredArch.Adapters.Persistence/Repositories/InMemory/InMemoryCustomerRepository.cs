using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Customers;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 고객 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class InMemoryCustomerRepository : ICustomerRepository
{
    internal static readonly ConcurrentDictionary<CustomerId, Customer> Customers = new();
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public InMemoryCustomerRepository(IDomainEventCollector eventCollector)
    {
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Customer> Create(Customer customer)
    {
        return IO.lift(() =>
        {
            Customers[customer.Id] = customer;
            _eventCollector.Track(customer);
            return Fin.Succ(customer);
        });
    }

    public virtual FinT<IO, Customer> GetById(CustomerId id)
    {
        return IO.lift(() =>
        {
            if (Customers.TryGetValue(id, out Customer? customer))
            {
                return Fin.Succ(customer);
            }

            return AdapterError.For<InMemoryCustomerRepository>(
                new NotFound(),
                id.ToString(),
                $"고객 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Customer> Update(Customer customer)
    {
        return IO.lift(() =>
        {
            if (!Customers.ContainsKey(customer.Id))
            {
                return AdapterError.For<InMemoryCustomerRepository>(
                    new NotFound(),
                    customer.Id.ToString(),
                    $"고객 ID '{customer.Id}'을(를) 찾을 수 없습니다");
            }

            Customers[customer.Id] = customer;
            _eventCollector.Track(customer);
            return Fin.Succ(customer);
        });
    }

    public virtual FinT<IO, Unit> Delete(CustomerId id)
    {
        return IO.lift(() =>
        {
            if (!Customers.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryCustomerRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"고객 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Customer> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Customers.Values.Any(c => spec.IsSatisfiedBy(c));
            return Fin.Succ(exists);
        });
    }
}
