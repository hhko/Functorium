using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;

namespace EfCoreImpl;

public class SimulatedEfCoreProductRepository : IProductRepository
{
    private readonly List<ProductDbModel> _dbModels;
    private readonly PropertyMap<Product, ProductDbModel> _propertyMap;

    public SimulatedEfCoreProductRepository(
        IEnumerable<ProductDbModel> dbModels,
        PropertyMap<Product, ProductDbModel> propertyMap)
    {
        _dbModels = dbModels.ToList();
        _propertyMap = propertyMap;
    }

    public IEnumerable<Product> FindAll(Specification<Product> spec)
    {
        var query = BuildQuery(spec);
        return query.Select(m => new Product(m.ProductName, m.UnitPrice, m.StockQuantity, m.CategoryCode));
    }

    public bool Exists(Specification<Product> spec)
        => BuildQuery(spec).Any();

    private IQueryable<ProductDbModel> BuildQuery(Specification<Product> spec)
    {
        var expression = SpecificationExpressionResolver.TryResolve(spec);
        if (expression is null)
            throw new InvalidOperationException("Specification does not support expression resolution.");

        var translated = _propertyMap.Translate(expression);
        return _dbModels.AsQueryable().Where(translated);
    }
}
