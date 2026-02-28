using System.Linq.Expressions;
using LayeredArch.Domain.AggregateRoots.Customers;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 고객 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreCustomerRepository
    : EfCoreRepositoryBase<Customer, CustomerId, CustomerModel>, ICustomerRepository
{
    private static readonly PropertyMap<Customer, CustomerModel> _propertyMap =
        new PropertyMap<Customer, CustomerModel>()
            .Map(c => (string)c.Email, m => m.Email)
            .Map(c => (string)c.Name, m => m.Name)
            .Map(c => (decimal)c.CreditLimit, m => m.CreditLimit)
            .Map(c => c.Id.ToString(), m => m.Id);

    private readonly LayeredArchDbContext _dbContext;

    public EfCoreCustomerRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector)
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbSet<CustomerModel> DbSet => _dbContext.Customers;

    protected override Customer ToDomain(CustomerModel model) => model.ToDomain();
    protected override CustomerModel ToModel(Customer customer) => customer.ToModel();

    protected override Expression<Func<CustomerModel, bool>> ByIdPredicate(CustomerId id)
    {
        var s = id.ToString();
        return m => m.Id == s;
    }

    protected override Expression<Func<CustomerModel, bool>> ByIdsPredicate(
        IReadOnlyList<CustomerId> ids)
    {
        var ss = ids.Select(id => id.ToString()).ToList();
        return m => ss.Contains(m.Id);
    }

    // ─── Customer 고유 메서드 ────────────────────────

    public virtual FinT<IO, bool> Exists(Specification<Customer> spec)
    {
        return IO.liftAsync(async () =>
        {
            bool exists = await BuildQuery(spec).AnyAsync();
            return Fin.Succ(exists);
        });
    }

    private IQueryable<CustomerModel> BuildQuery(Specification<Customer> spec)
    {
        var expression = SpecificationExpressionResolver.TryResolve(spec);
        if (expression is not null)
        {
            var modelExpression = _propertyMap.Translate(expression);
            return DbSet.Where(modelExpression);
        }

        throw new NotSupportedException(
            $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다. " +
            $"ExpressionSpecification<T>을 상속하고 ToExpression()을 구현하세요.");
    }
}
