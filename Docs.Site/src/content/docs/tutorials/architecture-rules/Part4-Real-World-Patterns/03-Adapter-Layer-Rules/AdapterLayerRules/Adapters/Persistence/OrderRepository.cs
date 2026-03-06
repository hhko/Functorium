using AdapterLayerRules.Domains;
using AdapterLayerRules.Domains.Ports;

namespace AdapterLayerRules.Adapters.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    public Task<Order?> GetByIdAsync(string id) => Task.FromResult<Order?>(null);
    public Task SaveAsync(Order order) => Task.CompletedTask;
}
