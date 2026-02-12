using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using Functorium.Adapters.Errors;
using Functorium.SourceGenerators;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories;

/// <summary>
/// 메모리 기반 고객 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class InMemoryCustomerRepository : ICustomerRepository
{
    private static readonly ConcurrentDictionary<CustomerId, Customer> _customers = new();

    public string RequestCategory => "Repository";

    public InMemoryCustomerRepository()
    {
    }

    public virtual FinT<IO, Customer> Create(Customer customer)
    {
        return IO.lift(() =>
        {
            _customers[customer.Id] = customer;
            return Fin.Succ(customer);
        });
    }

    public virtual FinT<IO, Customer> GetById(CustomerId id)
    {
        return IO.lift(() =>
        {
            if (_customers.TryGetValue(id, out Customer? customer))
            {
                return Fin.Succ(customer);
            }

            return AdapterError.For<InMemoryCustomerRepository>(
                new NotFound(),
                id.ToString(),
                $"고객 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, bool> ExistsByEmail(Email email)
    {
        return IO.lift(() =>
        {
            bool exists = _customers.Values.Any(c =>
                ((string)c.Email).Equals(email, StringComparison.OrdinalIgnoreCase));
            return Fin.Succ(exists);
        });
    }
}
