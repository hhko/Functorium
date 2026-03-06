namespace InterfaceValidation.Domains;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task SaveAsync(T entity);
}
