using Functorium.Domains.Repositories;

namespace CommandUsecase;

public interface IProductRepository : IRepository<Product, ProductId>
{
}
