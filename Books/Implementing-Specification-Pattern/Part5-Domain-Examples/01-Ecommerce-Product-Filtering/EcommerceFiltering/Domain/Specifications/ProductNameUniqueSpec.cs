using System.Linq.Expressions;
using EcommerceFiltering.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductNameUniqueSpec(ProductName name) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = name;
        return product => (string)product.Name == nameStr;
    }
}
