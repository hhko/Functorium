using System.Linq.Expressions;
using EcommerceFiltering.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductPriceRangeSpec(Money min, Money max) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal minValue = min;
        decimal maxValue = max;
        return product => (decimal)product.Price >= minValue && (decimal)product.Price <= maxValue;
    }
}
