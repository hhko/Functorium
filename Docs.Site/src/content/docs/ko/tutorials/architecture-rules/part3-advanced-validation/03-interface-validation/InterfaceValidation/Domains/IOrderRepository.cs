namespace InterfaceValidation.Domains;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerName);
}
