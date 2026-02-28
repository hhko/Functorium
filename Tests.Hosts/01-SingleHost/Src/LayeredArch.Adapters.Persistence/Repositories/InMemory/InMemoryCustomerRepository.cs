using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Customers;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

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

    public virtual FinT<IO, int> Delete(CustomerId id)
    {
        return IO.lift(() =>
        {
            return Fin.Succ(Customers.TryRemove(id, out _) ? 1 : 0);
        });
    }

    public virtual FinT<IO, Seq<Customer>> CreateRange(IReadOnlyList<Customer> customers)
    {
        return IO.lift(() =>
        {
            foreach (var customer in customers)
                Customers[customer.Id] = customer;
            _eventCollector.TrackRange(customers);
            return Fin.Succ(toSeq(customers));
        });
    }

    public virtual FinT<IO, Seq<Customer>> GetByIds(IReadOnlyList<CustomerId> ids)
    {
        return IO.lift(() =>
        {
            var distinctIds = ids.Distinct().ToList();
            var result = distinctIds
                .Where(id => Customers.ContainsKey(id))
                .Select(id => Customers[id])
                .ToList();

            if (result.Count != distinctIds.Count)
            {
                var foundIds = result.Select(c => c.Id.ToString()).ToHashSet();
                var missingIds = distinctIds.Where(id => !foundIds.Contains(id.ToString())).ToList();
                var missingIdsStr = FormatIds(missingIds);
                return AdapterError.For<InMemoryCustomerRepository>(
                    new PartialNotFound(), missingIdsStr,
                    $"Requested {distinctIds.Count} but found {result.Count}. Missing IDs: {missingIdsStr}");
            }

            return Fin.Succ(toSeq(result));
        });
    }

    public virtual FinT<IO, Seq<Customer>> UpdateRange(IReadOnlyList<Customer> customers)
    {
        return IO.lift(() =>
        {
            foreach (var customer in customers)
                Customers[customer.Id] = customer;
            _eventCollector.TrackRange(customers);
            return Fin.Succ(toSeq(customers));
        });
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<CustomerId> ids)
    {
        return IO.lift(() =>
        {
            int affected = 0;
            foreach (var id in ids)
            {
                if (Customers.TryRemove(id, out _))
                    affected++;
            }
            return Fin.Succ(affected);
        });
    }

    private static string FormatIds<T>(IEnumerable<T> ids, int maxDisplay = 3)
    {
        var list = ids.Select(id => id!.ToString()!).ToList();
        if (list.Count <= maxDisplay)
            return string.Join(", ", list);

        return string.Join(", ", list.Take(maxDisplay)) + $" ... (+{list.Count - maxDisplay} more)";
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
