using System.Linq.Expressions;
using EcommerceFiltering.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductNameUniqueSpec : ExpressionSpecification<Product>
{
    public ProductName Name { get; }

    public ProductNameUniqueSpec(ProductName name) => Name = name;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = Name;
        return product => (string)product.Name == nameStr;
    }
}
