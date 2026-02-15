using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 고객 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class EfCoreCustomerRepository : ICustomerRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public string RequestCategory => "Repository";

    public EfCoreCustomerRepository(LayeredArchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual FinT<IO, Customer> Create(Customer customer)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Customers.Add(customer);
            return Fin.Succ(customer);
        });
    }

    public virtual FinT<IO, Customer> GetById(CustomerId id)
    {
        return IO.liftAsync(async () =>
        {
            var customer = await _dbContext.Customers.FindAsync(id);
            if (customer is not null)
            {
                return Fin.Succ(customer);
            }

            return AdapterError.For<EfCoreCustomerRepository>(
                new NotFound(),
                id.ToString(),
                $"고객 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Customer> Update(Customer customer)
    {
        return IO.lift(() =>
        {
            _dbContext.Customers.Update(customer);
            return Fin.Succ(customer);
        });
    }

    public virtual FinT<IO, Unit> Delete(CustomerId id)
    {
        return IO.liftAsync(async () =>
        {
            var customer = await _dbContext.Customers.FindAsync(id);
            if (customer is null)
            {
                return AdapterError.For<EfCoreCustomerRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"고객 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Customers.Remove(customer);
            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, bool> ExistsByEmail(Email email)
    {
        return IO.liftAsync(async () =>
        {
            var emailStr = (string)email;
            bool exists = await _dbContext.Customers.AnyAsync(c =>
                EF.Property<string>(c, nameof(Customer.Email)) == emailStr);

            return Fin.Succ(exists);
        });
    }
}
