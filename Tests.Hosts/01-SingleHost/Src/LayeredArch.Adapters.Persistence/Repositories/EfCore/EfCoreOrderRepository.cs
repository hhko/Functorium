using LayeredArch.Domain.AggregateRoots.Orders;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 주문 리포지토리 구현
/// </summary>
[GeneratePortObservable]
public class EfCoreOrderRepository : IOrderRepository
{
    private readonly LayeredArchDbContext _dbContext;
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public EfCoreOrderRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
    {
        _dbContext = dbContext;
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Order> Create(Order order)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Orders.Add(order.ToModel());
            _eventCollector.Track(order);
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Order> GetById(OrderId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Orders.AsNoTracking()
                .Include(o => o.OrderLines)
                .FirstOrDefaultAsync(o => o.Id == id.ToString());
            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());
            }

            return AdapterError.For<EfCoreOrderRepository>(
                new NotFound(),
                id.ToString(),
                $"주문 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Order> Update(Order order)
    {
        return IO.lift(() =>
        {
            _dbContext.Orders.Update(order.ToModel());
            _eventCollector.Track(order);
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Unit> Delete(OrderId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Orders.FindAsync(id.ToString());
            if (model is null)
            {
                return AdapterError.For<EfCoreOrderRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"주문 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Orders.Remove(model);
            return Fin.Succ(unit);
        });
    }
}
