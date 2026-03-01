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
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreCustomerRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               propertyMap: new PropertyMap<Customer, CustomerModel>()
                   .Map(c => (string)c.Email, m => m.Email)
                   .Map(c => (string)c.Name, m => m.Name)
                   .Map(c => (decimal)c.CreditLimit, m => m.CreditLimit)
                   .Map(c => c.Id.ToString(), m => m.Id))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbSet<CustomerModel> DbSet => _dbContext.Customers;

    protected override Customer ToDomain(CustomerModel model) => model.ToDomain();
    protected override CustomerModel ToModel(Customer customer) => customer.ToModel();

    // ─── Customer 고유 메서드 ────────────────────────

    public virtual FinT<IO, bool> Exists(Specification<Customer> spec)
        => ExistsBySpec(spec);
}
