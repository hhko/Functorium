using LayeredArch.Domain.AggregateRoots.Customers;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 고객 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreCustomerRepository : ICustomerRepository
{
    private static readonly PropertyMap<Customer, CustomerModel> _propertyMap =
        new PropertyMap<Customer, CustomerModel>()
            .Map(c => (string)c.Email, m => m.Email)
            .Map(c => (string)c.Name, m => m.Name)
            .Map(c => (decimal)c.CreditLimit, m => m.CreditLimit)
            .Map(c => c.Id.ToString(), m => m.Id);

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

    public virtual FinT<IO, Seq<Customer>> CreateRange(IReadOnlyList<Customer> customers)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Customers.AddRange(customers.Select(c => c.ToModel()));
            _eventCollector.TrackRange(customers);
            return Fin.Succ(toSeq(customers));
        });
    }

    public virtual FinT<IO, Seq<Customer>> GetByIds(IReadOnlyList<CustomerId> ids)
    {
        return IO.liftAsync(async () =>
        {
            var idStrings = ids.Select(id => id.ToString()).ToList();
            var models = await _dbContext.Customers.AsNoTracking()
                .Where(c => idStrings.Contains(c.Id))
                .ToListAsync();
            return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
        });
    }

    public virtual FinT<IO, Seq<Customer>> UpdateRange(IReadOnlyList<Customer> customers)
    {
        return IO.lift(() =>
        {
            _dbContext.Customers.UpdateRange(customers.Select(c => c.ToModel()));
            _eventCollector.TrackRange(customers);
            return Fin.Succ(toSeq(customers));
        });
    }

    public virtual FinT<IO, Unit> DeleteRange(IReadOnlyList<CustomerId> ids)
    {
        return IO.liftAsync(async () =>
        {
            var idStrings = ids.Select(id => id.ToString()).ToList();
            await _dbContext.Customers
                .Where(c => idStrings.Contains(c.Id))
                .ExecuteDeleteAsync();
            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Customer> spec)
    {
        return IO.liftAsync(async () =>
        {
            var expression = SpecificationExpressionResolver.TryResolve(spec);
            if (expression is not null)
            {
                var modelExpression = _propertyMap.Translate(expression);
                return Fin.Succ(await _dbContext.Customers.AnyAsync(modelExpression));
            }

            throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다. " +
                $"ExpressionSpecification<T>을 상속하고 ToExpression()을 구현하세요.");
        });
    }
}
