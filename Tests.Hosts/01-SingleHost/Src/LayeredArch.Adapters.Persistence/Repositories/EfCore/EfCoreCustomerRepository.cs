using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.Specifications;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
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
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public EfCoreCustomerRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
    {
        _dbContext = dbContext;
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Customer> Create(Customer customer)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Customers.Add(customer.ToModel());
            _eventCollector.Track(customer);
            return Fin.Succ(customer);
        });
    }

    public virtual FinT<IO, Customer> GetById(CustomerId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id.ToString());
            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());
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
            _dbContext.Customers.Update(customer.ToModel());
            _eventCollector.Track(customer);
            return Fin.Succ(customer);
        });
    }

    public virtual FinT<IO, Unit> Delete(CustomerId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Customers.FindAsync(id.ToString());
            if (model is null)
            {
                return AdapterError.For<EfCoreCustomerRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"고객 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Customers.Remove(model);
            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, bool> ExistsByEmail(Email email)
    {
        return IO.liftAsync(async () =>
        {
            var emailStr = (string)email;
            bool exists = await _dbContext.Customers.AnyAsync(c => c.Email == emailStr);

            return Fin.Succ(exists);
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Customer> spec)
    {
        return IO.liftAsync(async () =>
        {
            bool exists = spec switch
            {
                CustomerEmailSpec s => await _dbContext.Customers.AnyAsync(c =>
                    c.Email == (string)s.Email),
                _ => await _dbContext.Customers.AnyAsync(_ => true)
            };

            return Fin.Succ(exists);
        });
    }
}
