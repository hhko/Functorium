using AdapterLayerRules.Domains;
using AdapterLayerRules.Domains.Ports;

namespace AdapterLayerRules.Adapters.Persistence;

public class OrderRepository : IOrderRepository, IObservablePort
{
    public virtual Task<Order?> GetByIdAsync(string id) => Task.FromResult<Order?>(null);
    public virtual Task SaveAsync(Order order) => Task.CompletedTask;
}
