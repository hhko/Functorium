namespace AdapterLayerRules.Domains.Ports;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string id);
    Task SaveAsync(Order order);
}
