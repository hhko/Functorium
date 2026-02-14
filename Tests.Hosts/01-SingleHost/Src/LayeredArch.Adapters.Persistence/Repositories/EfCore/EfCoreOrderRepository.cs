using LayeredArch.Domain.AggregateRoots.Orders;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 주문 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class EfCoreOrderRepository : IOrderRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public string RequestCategory => "Repository";

    public EfCoreOrderRepository(LayeredArchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual FinT<IO, Order> Create(Order order)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Order> GetById(OrderId id)
    {
        return IO.liftAsync(async () =>
        {
            var order = await _dbContext.Orders.FindAsync(id);
            if (order is not null)
            {
                return Fin.Succ(order);
            }

            return AdapterError.For<EfCoreOrderRepository>(
                new NotFound(),
                id.ToString(),
                $"주문 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Order> Update(Order order)
    {
        return IO.liftAsync(async () =>
        {
            var exists = await _dbContext.Orders.AnyAsync(o => o.Id == order.Id);
            if (!exists)
            {
                return AdapterError.For<EfCoreOrderRepository>(
                    new NotFound(),
                    order.Id.ToString(),
                    $"주문 ID '{order.Id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Unit> Delete(OrderId id)
    {
        return IO.liftAsync(async () =>
        {
            var order = await _dbContext.Orders.FindAsync(id);
            if (order is null)
            {
                return AdapterError.For<EfCoreOrderRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"주문 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();
            return Fin.Succ(unit);
        });
    }
}
