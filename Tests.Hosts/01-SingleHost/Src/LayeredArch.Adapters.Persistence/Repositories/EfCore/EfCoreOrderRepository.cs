using LayeredArch.Domain.AggregateRoots.Orders;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 주문 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreOrderRepository
    : EfCoreRepositoryBase<Order, OrderId, OrderModel>, IOrderRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreOrderRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector, q => q.Include(o => o.OrderLines))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbSet<OrderModel> DbSet => _dbContext.Orders;

    protected override Order ToDomain(OrderModel model) => model.ToDomain();
    protected override OrderModel ToModel(Order order) => order.ToModel();
}
